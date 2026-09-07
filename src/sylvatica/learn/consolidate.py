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
from dataclasses import dataclass
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
    store: Any, spec: ReplaySpec, arm: Arm = "raw", general: list[str] | None = None
) -> TrainingSet:
    """Turn a replay sample into text to train on.

    The general slice is passed IN rather than fetched here, because what counts
    as "language this house did not produce" is a decision about the corpus and
    not about consolidation.
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
    general = list(general or [])
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
) -> tuple[Cycle, Measurement, TrainingSet]:
    """One full cycle, gated, with rollback. THE ROLLBACK IS A READING.

    The adapter's parameters are snapshotted before training and restored if the
    gate trips. Restoring is cheap because the adapter is the only thing that
    moved -- which is the reason the base is frozen and the merge is separate.
    """
    spec = spec or ReplaySpec()
    before = {k: v.clone() for k, v in adapter.state_dict().items()}

    training = build_training_set(store, spec, arm=arm, general=general)
    _loss, tokens, seconds = train(core, adapter, training, lr=lr, seq_len=seq_len)

    after = measure(core, adapter)
    result = gate.check(after)

    if result.tripped:
        adapter.load_state_dict(before)

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
