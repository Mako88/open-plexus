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
