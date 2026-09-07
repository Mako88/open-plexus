"""The differentiable forward must produce what the reference produces.

NOT A GUARD, AND THAT IS WHY IT IS IN `tests/exam/`: it loads a 1.5B checkpoint
onto the card and takes tens of seconds. `pyproject.toml` excludes this directory
from the default run, and exams are dispatched by hand. Run it after any change
to `core/wkv7.py`:

    uv run pytest tests/exam/test_wkv7_matches_reference.py -s

WHAT IT IS FOR. `DifferentiableRwkv7` was transcribed by hand from the `rwkv`
package's `RWKV_x070_TMix_seq`. A transcription error does not raise -- it
produces a model that runs, answers slightly worse, and trains towards the wrong
thing. That is the most expensive class of bug this branch can have, because
Phase 3 would spend GPU-days optimising against a subtly wrong forward and every
Tier C reading would be measuring it.

So the check is against the reference on the SAME checkpoint and the SAME tokens:
the logits must agree, the argmax must agree, and the state must agree. And the
gradient must exist, because a forward that matches perfectly and has no graph
is exactly as useless for Phase 3 as one that does not match.
"""

from __future__ import annotations

import pytest

torch = pytest.importorskip("torch")

pytestmark = pytest.mark.skipif(
    not torch.cuda.is_available(), reason="needs the card and a real checkpoint"
)

TEXT = (
    "User: Marta Halloway keeps eleven beehives behind the shed on Ferrin Lane, "
    "and her brother Osric repairs clocks in the front room.\n\nAssistant:"
)


@pytest.fixture(scope="module")
def core():
    from sylvatica.core.checkpoints import ensure
    from sylvatica.core.rwkv_core import RwkvCore

    return RwkvCore(ensure(0.4))


def test_the_logits_match_the_reference(core):
    from sylvatica.core.wkv7 import DifferentiableRwkv7

    tokens = core.encode(TEXT)
    reference, ref_state = core._model.forward(list(tokens), None)

    mine = DifferentiableRwkv7(core)
    logits, state = mine.forward(list(tokens), None)
    last = logits[-1]

    gap = float((last.float() - reference.float()).abs().max())
    scale = float(reference.float().abs().max())
    print(f"\nmax |dlogit| = {gap:.3e} over a range of {scale:.3f}")
    assert gap / scale < 1e-3, (
        f"the differentiable forward disagrees with the reference by {gap:.3e}; "
        "a transcription error here would train Phase 3 towards the wrong thing"
    )

    assert int(last.argmax()) == int(reference.argmax()), "different next token"

    top_mine = [int(i) for i in last.float().topk(5).indices]
    top_ref = [int(i) for i in reference.float().topk(5).indices]
    assert top_mine == top_ref, f"top-5 differs: {top_mine} vs {top_ref}"

    # THE STATE TOO, because Phase 3 trains from states the inference path
    # produced. A forward that agreed on logits and diverged on state would
    # break the moment a training batch resumed from a saved thread.
    worst = 0.0
    for a, b in zip(state, ref_state):
        worst = max(worst, float((a.float() - b.float()).abs().max()))
    print(f"max |dstate| = {worst:.3e}")
    assert worst < 1e-2, f"state diverged by {worst:.3e}"


def test_a_gradient_actually_flows(core):
    """A forward that matches perfectly and has no graph is exactly as useless
    for Phase 3 as one that does not match."""
    from sylvatica.core.wkv7 import DifferentiableRwkv7

    mine = DifferentiableRwkv7(core)
    tokens = core.encode("User: hello there\n\nAssistant:")

    # Stand in for the LoRA: a trainable scale on one time-mix weight.
    name = "blocks.0.att.key.weight"
    scale = torch.ones(1, device="cuda", requires_grad=True)
    mine.adapter = lambda n, base: base * scale if n == name else base

    logits, _ = mine.forward(list(tokens), None)
    loss = torch.nn.functional.cross_entropy(
        logits[:-1].float(), torch.tensor(tokens[1:], device=logits.device)
    )
    loss.backward()

    assert scale.grad is not None, "no gradient reached the adapter"
    assert torch.isfinite(scale.grad).all(), f"gradient is not finite: {scale.grad}"
    assert float(scale.grad.abs()) > 0, "gradient is exactly zero"
    print(f"\nloss = {float(loss):.4f}  d(loss)/d(scale) = {float(scale.grad):.6f}")


def test_it_resumes_from_a_state_the_inference_path_wrote(core):
    """Phase 3 replays from threads the REPL produced, so the two paths have to
    agree about what a state is -- not merely each be self-consistent."""
    from sylvatica.core.wkv7 import DifferentiableRwkv7

    first = core.encode("User: The kettle lives on the third shelf.\n\nAssistant:")
    second = core.encode(" Noted.\n\nUser: Where does it live?\n\nAssistant:")

    state, _ = core.feed(first, None)
    reference, _ = core._model.forward(list(second), core.copy_state(state))

    mine = DifferentiableRwkv7(core)
    logits, _ = mine.forward(list(second), core.copy_state(state))

    gap = float((logits[-1].float() - reference.float()).abs().max())
    print(f"\nresumed max |dlogit| = {gap:.3e}")
    assert int(logits[-1].argmax()) == int(reference.argmax())


def test_the_chunked_path_agrees_with_the_token_at_a_time_loop(core):
    """The fast path must be the same forward, not merely a plausible one.

    `_recurrence_chunked` replaces the loop with a triangular solve over C
    tokens at a time. It is 7x faster at the shipped C=32 and it is a hundred
    lines of algebra that no error message would catch: a wrong anchoring, a
    wrong tril, a state scaled by the wrong factor all produce a model that runs
    and answers slightly worse. So the loop stays, and this is what it is for.
    """
    from sylvatica.core.wkv7 import DifferentiableRwkv7

    tokens = core.encode(TEXT)
    mine = DifferentiableRwkv7(core)

    mine.chunk = 0
    loop, loop_state = mine.forward(list(tokens), None)

    scale = float(loop.float().abs().max())
    for chunk in (8, 16, 32, 64):
        mine.chunk = chunk
        out, state = mine.forward(list(tokens), None)
        gap = float((out[-1].float() - loop[-1].float()).abs().max()) / scale
        print(f"\nchunk {chunk:3d}: |dlogit| {gap:.3e} relative")
        assert gap < 1e-4, f"chunk {chunk} disagrees with the loop by {gap:.3e}"
        assert int(out[-1].argmax()) == int(loop[-1].argmax())

        worst = max(
            float((a.float() - b.float()).abs().max())
            for a, b in zip(state, loop_state)
        )
        assert worst < 1e-2, f"chunk {chunk} state diverged by {worst:.3e}"


def test_the_chunked_gradient_is_finite(core):
    """THE CHECK A FORWARD-ONLY TEST DOES NOT MAKE, and the reason it exists.

    At C=128 the chunked forward agrees with the loop to 1.1e-6 and its BACKWARD
    produces a non-finite gradient, because the anchored ratios reach 3.9e16 and
    fp32 has seven digits. A forward-only check passes that dial and Phase 3
    then spends GPU-hours training on NaN. Every chunk size this repository
    ships has to be checked here, not only there.
    """
    from sylvatica.core.wkv7 import DifferentiableRwkv7

    mine = DifferentiableRwkv7(core)
    tokens = core.encode(TEXT)
    name = "blocks.0.att.key.weight"

    for chunk in (16, 32, DifferentiableRwkv7.chunk):
        knob = torch.ones(1, device="cuda", requires_grad=True)
        mine.chunk = chunk
        mine.adapter = lambda n, base: base * knob if n == name else base

        logits, _ = mine.forward(list(tokens), None, checkpoint=True)
        loss = torch.nn.functional.cross_entropy(
            logits[:-1].float(), torch.tensor(tokens[1:], device=logits.device)
        )
        loss.backward()

        assert knob.grad is not None, f"chunk {chunk}: no gradient reached the adapter"
        assert torch.isfinite(knob.grad).all(), (
            f"chunk {chunk}: gradient is not finite ({knob.grad}) -- the anchored "
            "ratios have outrun fp32 and this dial must not ship"
        )
        assert float(knob.grad.abs()) > 0, f"chunk {chunk}: gradient is exactly zero"
        print(f"\nchunk {chunk:3d}: d(loss)/d(scale) = {float(knob.grad):.6f}")


def test_training_through_the_chunked_path_goes_where_the_loop_goes(core):
    """Not "matches at a point" -- "optimises to the same place over steps".

    WHY THIS IS A SEPARATE TEST FROM THE ONES ABOVE. Every Phase 3 reading taken
    before the chunked recurrence existed was produced by the loop, and every one
    taken after will be produced by chunks. Somebody will compare them. Agreement
    on one forward and one gradient does not establish that, because an optimiser
    compounds: a 1e-6 disagreement per step, if it were biased rather than
    random, is a different adapter after a few hundred steps.

    So this runs the real training path -- LoRA, AdamW, gradient checkpointing --
    from an identical seed down both roads and compares the LOSS TRAJECTORY and
    the resulting weight deltas, which is the thing Phase 3 actually depends on.
    """
    from sylvatica.core.wkv7 import DifferentiableRwkv7
    from sylvatica.learn.lora import LoraAdapter

    tokens = list(core.encode(TEXT))
    target = torch.tensor(tokens[1:], device="cuda")

    def run(chunk: int) -> tuple[list[float], dict[str, torch.Tensor]]:
        # THE SEED IS RESET INSIDE, not once outside. The adapter's B matrix is
        # randomly initialised, and two runs from different inits would diverge
        # for a reason that has nothing to do with the recurrence.
        torch.manual_seed(20260907)
        adapter = LoraAdapter(core._model.z, rank=8)
        model = DifferentiableRwkv7(core, adapter=adapter)
        model.chunk = chunk
        opt = torch.optim.AdamW(adapter.parameters(), lr=1e-4)

        losses = []
        for _ in range(6):
            opt.zero_grad(set_to_none=True)
            logits, _ = model.forward(tokens, None, checkpoint=True)
            loss = torch.nn.functional.cross_entropy(logits[:-1].float(), target)
            loss.backward()
            opt.step()
            losses.append(float(loss))
        return losses, {n: adapter.delta(n).detach().clone() for n in adapter.names}

    loop_losses, loop_delta = run(0)
    chunk_losses, chunk_delta = run(DifferentiableRwkv7.chunk)

    print(f"\nloop   {['%.5f' % x for x in loop_losses]}")
    print(f"chunk  {['%.5f' % x for x in chunk_losses]}")

    # The loss must FALL, or this compares two ways of not training.
    assert loop_losses[-1] < loop_losses[0], "the loop did not learn; nothing to compare"

    worst = max(abs(a - b) for a, b in zip(loop_losses, chunk_losses))
    print(f"worst per-step loss gap = {worst:.3e}")
    assert worst < 1e-3, (
        f"the chunked path optimises differently: loss gap {worst:.3e}. Phase 3 "
        "readings taken before and after the kernel would not be comparable."
    )

    # AND THE WEIGHTS, because two paths can track on a scalar loss and still
    # arrive at different adapters.
    #
    # ON NORM AND DIRECTION, NOT ON THE WORST ELEMENT, and the reason is Adam
    # rather than convenience. This assertion was first written as a max-element
    # ratio and it failed at 1.57e-2 while the relative Frobenius norm was
    # 7.8e-5 and the cosine similarity was 0.99999994 -- six nines. AdamW
    # normalises its step to roughly `lr` whatever the gradient's size, so an
    # element whose gradient sits near zero takes a step decided by numerical
    # noise; the max-element ratio then divides that worst element by the
    # LARGEST element, which is fourteen times the typical one. It measures
    # Adam's behaviour near zero, not whether two adapters are the same map.
    # The max is still printed, because it is the number that would move first
    # if the chunked path ever developed a real bias.
    flat_loop = torch.cat([loop_delta[n].flatten() for n in loop_delta])
    flat_chunk = torch.cat([chunk_delta[n].flatten() for n in loop_delta])

    norm = float(flat_loop.norm())
    relative = float((flat_loop - flat_chunk).norm()) / norm
    cosine = float(torch.nn.functional.cosine_similarity(flat_loop, flat_chunk, dim=0))
    worst_element = max(
        float((loop_delta[n] - chunk_delta[n]).abs().max()) for n in loop_delta
    ) / max(float(v.abs().max()) for v in loop_delta.values())

    print(
        f"relative Frobenius {relative:.3e}  cosine {cosine:.9f}  "
        f"max-element {worst_element:.3e}"
    )
    assert norm > 0, "the optimiser never moved the weights"
    assert relative < 1e-3, f"adapters diverged by {relative:.2e} of their own norm"
    assert cosine > 0.9999, f"adapters point in different directions: cosine {cosine}"
