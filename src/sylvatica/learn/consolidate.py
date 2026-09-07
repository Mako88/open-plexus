"""One consolidation cycle: replay out of the store, into the adapter, past the gate.

THE SHAPE OF A CYCLE, and the order is not negotiable:

  1. measure the base on the fixed held-out sets;
  2. sample replay out of the store -- recent, rehearsal, and general text;
  3. train the adapter on it, base frozen;
  4. measure again;
  5. if the gate trips, ROLL BACK by discarding the adapter's step, and the
     rollback is a reading.

STEP 5 IS WHY THE ADAPTER IS THE ONLY THING THAT MOVES. Rolling back a LoRA is
restoring a few million numbers that were saved before the step. Rolling back a
merged base would mean holding a second copy of 1.5B parameters, which does not
fit on this card beside the model -- so the merge, which the doc calls the sleep,
is the one irreversible act and happens only after a run of clean cycles.

THE THREE TRAINING SHAPES ARE ARMS AND THE DOC NAMES THEIR REFUTATIONS:

  raw           episode text as it was said.
                REFUTED BY: Tier C at blind after ten cycles.
  declaratives  facts the core extracts from an episode window, paraphrased.
                REFUTED BY: the extractor at 1.5B producing facts that are wrong
                more than a fifth of the time, read by hand on a sample of 50.
  both          REFUTED BY: no better than the better of the other two.

`raw` is implemented. `declaratives` needs an extractor and is owed work --
`tests/outstanding` carries it -- and building it before `raw` has a reading
would be choosing the expected winner before running the race.

THE GENERAL SLICE IS NOT THE STORE'S TO GIVE. The doc calls for "a fixed local
corpus of a few million tokens" so the adapter stays anchored to language it did
not learn in this house. `sample_for_replay` honours the share by returning
fewer rows, so the gap is VISIBLE rather than quietly filled with house text --
which would be the worst possible substitution, since anchoring against the house
is what the anchor exists to prevent.
"""

from __future__ import annotations

import time
from dataclasses import dataclass, replace
from typing import Any

from ..core.base import training_flops
from ..store import ReplaySpec
from . import Arm, Cycle
from .gate import Gate, Measurement, measure


@dataclass
class TrainingSet:
    """What one cycle actually trained on, and where it came from."""

    texts: list[str]
    arm: Arm
    fragments: int
    general: int
    tokens: int = 0

    def row(self) -> dict[str, Any]:
        return {
            "arm": self.arm,
            "fragments": self.fragments,
            "general": self.general,
            "tokens": self.tokens,
            "sample": self.texts[:3],
        }


def build_training_set(
    store: Any,
    spec: ReplaySpec,
    arm: Arm = "raw",
    general: list[str] | None = None,
    core: Any = None,
) -> TrainingSet:
    """Turn a replay sample into text to train on.

    THE MIX IS BY TOKENS, NOT BY LIST LENGTH, and that distinction cost a run.
    `ReplaySpec` names three shares -- recent 0.4, rehearsal 0.4, general 0.2 --
    and the first two are counted in FRAGMENTS while a general chunk is a
    hundred-odd words. Passing 32 chunks beside 48 fragments looked balanced and
    was 87% general by token: the adapter would have spent its cycles on English
    it already knew and barely seen the house, and Tier C landing at blind would
    have read as the doc's named refutation of the `raw` arm rather than as a
    mixing error.

    So when `core` is given, the general slice is sized from the house half's
    actual token count to hit `spec.general`.
    """
    if arm == "declaratives":
        raise NotImplementedError(
            "The declaratives arm needs an extractor that turns an episode "
            "window into paraphrased facts. It is owed work -- see "
            "tests/outstanding -- and building it before `raw` has a reading "
            "would be choosing the expected winner before running the race."
        )

    fragments = store.sample_for_replay(spec)
    texts = [f.text for f in fragments]

    if general is None:
        from .general import load

        house_tokens = (
            sum(len(core.encode(t)) for t in texts) if core is not None else 20 * len(texts)
        )
        share = min(max(spec.general, 0.0), 0.9)
        want_tokens = house_tokens * share / max(1e-6, 1.0 - share)
        # A hundred and twenty words is roughly a hundred and sixty tokens for
        # this vocabulary; the chunk size is a passage worth of context rather
        # than a sentence, so the anchor is prose and not fragments of prose.
        chunk_words = 120
        n_chunks = max(1, round(want_tokens / 160))
        general = load(n_chunks=n_chunks, chunk_words=chunk_words, seed=spec.seed)

    general = list(general)
    return TrainingSet(
        texts=texts + general, arm=arm, fragments=len(fragments), general=len(general)
    )


def train(
    core: Any,
    adapter: Any,
    training: TrainingSet,
    lr: float = 1e-4,
    seq_len: int = 256,
    epochs: int = 1,
) -> tuple[float, int, float]:
    """Train the adapter on the training set. Returns (loss, tokens, seconds).

    `checkpoint=True` IS NOT OPTIONAL HERE. The recurrence keeps every
    intermediate state in the graph -- about 6 GB for a 512-token sequence at
    1.5B over 24 layers -- which does not fit beside the model on 11 GB.
    Recomputing each layer in the backward pass costs roughly double the forward
    and is the difference between training and an out-of-memory error.
    """
    import torch

    from ..core.wkv7 import DifferentiableRwkv7

    model = DifferentiableRwkv7(core, adapter=adapter)
    opt = torch.optim.AdamW(adapter.parameters(), lr=lr)

    # Sequences are built per fragment rather than by concatenating everything
    # into one stream. A stream would teach the model that one episode follows
    # another, which is an artefact of the sampling order and not a fact about
    # the house.
    sequences: list[list[int]] = []
    for text in training.texts:
        tokens = core.encode(text.strip() + "\n\n")
        for i in range(0, len(tokens), seq_len):
            chunk = tokens[i : i + seq_len]
            if len(chunk) > 8:
                sequences.append(chunk)

    total_loss, total_tokens = 0.0, 0
    started = time.perf_counter()
    for _ in range(epochs):
        for chunk in sequences:
            opt.zero_grad(set_to_none=True)
            logits, _ = model.forward(chunk, None, checkpoint=True)
            targets = torch.tensor(chunk[1:], device=logits.device)
            loss = torch.nn.functional.cross_entropy(logits[:-1].float(), targets)
            loss.backward()
            opt.step()
            total_loss += float(loss) * (len(chunk) - 1)
            total_tokens += len(chunk) - 1

    training.tokens = total_tokens
    return (
        total_loss / max(1, total_tokens),
        total_tokens,
        time.perf_counter() - started,
    )


def run_cycle(
    core: Any,
    adapter: Any,
    store: Any,
    gate: Gate,
    index: int = 0,
    arm: Arm = "raw",
    spec: ReplaySpec | None = None,
    general: list[str] | None = None,
    lr: float = 1e-4,
    seq_len: int = 256,
    rollback: bool = True,
) -> tuple[Cycle, Measurement, TrainingSet]:
    """One full cycle, gated, with rollback. THE ROLLBACK IS A READING.

    The adapter's parameters are snapshotted before training and restored if the
    gate trips. Restoring is cheap because the adapter is the only thing that
    moved -- which is the reason the base is frozen and the merge is separate.

    EVERY CYCLE DRAWS A DIFFERENT REPLAY SAMPLE, and the first Phase 3 run is why
    this is spelled out. `ReplaySpec` carries a seed so a reading is
    reproducible; passing the SAME spec to every cycle made every cycle train on
    the identical 48 fragments, and the output proved it -- ten cycles reporting
    perplexity 27.559 and QA 0.800 to three decimal places, ten times. That was
    not ten cycles. It was one cycle run ten times, and "the gate tripped on 10
    of 10" would have been reported as a finding about consolidation.
    """
    spec = spec or ReplaySpec()
    if spec.seed is not None:
        spec = replace(spec, seed=spec.seed + index)
    before = {k: v.clone() for k, v in adapter.state_dict().items()}

    # THE ANCHOR IS NOT OPTIONAL. Without it the adapter sees only invented
    # people in invented rooms, and one cycle of that moved held-out perplexity
    # 20% in the first Phase 3 run. Refusing is better than quietly running the
    # arm the doc did not describe.
    from .general import available

    if general is None and not available():
        raise FileNotFoundError(
            "no general corpus for the replay anchor. Run "
            "`uv run python corpora/fetch.py`. Consolidating on house text "
            "alone is not the arm the doc describes -- see learn/general.py."
        )

    training = build_training_set(store, spec, arm=arm, general=general, core=core)
    _loss, tokens, seconds = train(core, adapter, training, lr=lr, seq_len=seq_len)

    after = measure(core, adapter)
    result = gate.check(after)

    # `rollback=False` RECORDS THE GATE'S VERDICT AND IGNORES IT, which is an
    # experiment rather than a policy and must never be how consolidation ships.
    #
    # The reason it exists: with rollback on, a run where every cycle trips
    # produces an adapter that never accumulates anything, so Tier C measures the
    # untouched base and says nothing about whether replay TEACHES. That
    # conflates two questions the branch needs separated -- does the adapter
    # learn the house, and does it cost the base too much -- and answers only the
    # second. Turning rollback off lets both be read off one run, at the price of
    # a model nobody would deploy.
    rolled_back = bool(result.tripped and rollback)
    if rolled_back:
        adapter.load_state_dict(before)

    # `Gate.check` sets `rolled_back = tripped` because that is the POLICY. What
    # actually happened is a different fact, and a reading that recorded the
    # policy as the event would claim an adapter had been restored when it had
    # not -- and every Tier C number after it would be unexplainable.
    result = replace(result, rolled_back=rolled_back)

    cycle = Cycle(
        index=index,
        arm=arm,
        fragments=training.fragments,
        tokens=tokens,
        seconds=seconds,
        flops=training_flops(core.params, tokens),
        gate=result,
        merged=False,
    )
    return cycle, after, training
