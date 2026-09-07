"""`learn` -- consolidation. PROTOCOL ONLY; the LoRA and the gate are Phase 3.

PHASE 3 IS THE BET AND EVERYTHING BEFORE IT IS SCAFFOLDING. The doc says so
plainly. What goes here is the slow path: replay sampled out of the store,
trained into a LoRA adapter over the frozen base, gated on a fixed held-out set,
rolled back when it regresses, and merged into the base every K cycles.

THE THREE ARMS, named before anything runs, because what gets trained on is the
open question and not the machinery around it:

  (a) raw episode text;
  (b) declaratives the core extracts from an episode window, each paraphrased
      several ways;
  (c) both.

The literature says models learn facts from a handful of raw exposures badly and
from paraphrased declaratives much better, so (b) is EXPECTED to win -- and it
is still an arm, because an expectation written in a design document is not a
reading. Each arm's refutation is in the doc and is repeated in
`tests/outstanding`.

CALIBRATE THE GATE ON NOISE FIRST. Two identical cycles, read the spread, then
choose a threshold. A threshold chosen before the noise is known is a prediction
dressed as a check, and this project has already shipped one of those.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Literal, Protocol

Arm = Literal["raw", "declaratives", "both"]


@dataclass(frozen=True)
class GateResult:
    """What the regression gate saw after a cycle, and what it did about it.

    BOTH NUMBERS ARE ABSOLUTE AND FIXED FOR THE LIFE OF THE BRANCH. A held-out
    perplexity set and a small general-QA set that never change; a gate whose
    yardstick moves cannot tell a regression from a new yardstick.
    """

    perplexity: float
    qa_score: float
    perplexity_threshold: float
    qa_threshold: float
    tripped: bool
    rolled_back: bool


@dataclass(frozen=True)
class Cycle:
    """One consolidation cycle: what it trained on, what it cost, what it kept."""

    index: int
    arm: Arm
    fragments: int
    tokens: int
    seconds: float
    flops: float
    gate: GateResult | None = None
    merged: bool = False


class Consolidator(Protocol):
    """Turns what is in the store into what is in the weights.

    A cycle that trips the gate is ROLLED BACK AND THE ROLLBACK IS A READING.
    The doc's refutation for the gate is any arm that trips on more than a third
    of its cycles: at that rate replay is not protecting the base at this scale,
    which is refutation 2 of the whole branch -- the branch would have
    rediscovered why training is split from inference.
    """

    def cycle(self, arm: Arm) -> Cycle: ...

    def merge(self) -> None: ...


# At the bottom: these import the types above.
from .consolidate import TrainingSet, build_training_set, run_cycle, train
from .extract import Declarative, declaratives_from, paraphrase, templates
from .gate import Gate, Measurement, calibrate, measure
from .heldout import GENERAL_QA, PERPLEXITY_TEXT, fingerprint
from .lora import DEFAULT_TARGETS, LoraAdapter, adapter_for

__all__ = [
    "DEFAULT_TARGETS",
    "GENERAL_QA",
    "PERPLEXITY_TEXT",
    "Arm",
    "Consolidator",
    "Cycle",
    "Declarative",
    "Gate",
    "GateResult",
    "LoraAdapter",
    "Measurement",
    "TrainingSet",
    "adapter_for",
    "build_training_set",
    "calibrate",
    "declaratives_from",
    "fingerprint",
    "measure",
    "paraphrase",
    "run_cycle",
    "templates",
    "train",
]
