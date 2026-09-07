"""`exam` -- the instrument. The types are here; the machinery is in the modules below.

`world` generates a house, `baselines` holds the two controls, `run` holds the
conversation and scores it. Tier A runs today. Tiers B and C RAISE rather than
falling back to A, because a Tier B reading that was secretly Tier A is the most
expensive wrong number this branch could produce.

THE INSTRUMENT IS BUILT BEFORE THE THING IT MEASURES, and that ordering is the
whole of Phase 1. The two earlier branches lost to a rule that never looked at
the house, and they lost because the instrument arrived late enough that a weak
baseline was never in front of anybody. The BLIND baseline stays for that reason
and for no other.

THREE TIERS, and every reading names its tier:

  A -- same thread, state saved and reloaded across a process restart.
       Tests the STATE.
  B -- fresh state, store enabled. Tests RETRIEVAL.
  C -- fresh state, store DISABLED, adapter enabled. Tests what was learned into
       the WEIGHTS. Tier C is complaint 4's bar and the first north star's new line.

TWO BASELINES, ALWAYS RUN BESIDE AN ARM. Full-context is the same core fed the
whole transcript every turn, which is how a transformer would do it. Blind
answers the commonest answer for the question's kind. An arm reported without
both is not a reading.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from enum import Enum
from typing import Protocol


class Tier(str, Enum):
    """Which memory is under test. Every reading names one."""

    A = "A"  # the state, across a restart
    B = "B"  # the store, from a fresh state
    C = "C"  # the weights, store off


@dataclass(frozen=True)
class Fact:
    """One invented thing about an invented house.

    `answer` is the short canonical string scoring is a contains-match against.
    `kind` is what makes the blind baseline computable AND interpretable -- the
    blind rule is per-kind, so a reading must report the blind score per kind
    beside the arm's, or a low blind score cannot be told from a generous
    generator. That is standing objection 5.
    """

    id: str
    kind: str  # "name" | "number" | "place" | "relation" | ...
    told: str  # the natural sentence that delivers it
    question: str
    answer: str
    # THE SAME QUESTION WITH NONE OF THE TELLING SENTENCE'S WORDS.
    #
    # Phase 2's first reading showed the direct questions are near-copies of the
    # sentences that told the facts, so Tier B scored 0.988 on the LEXICAL ranker
    # alone with precision 1.000 -- better than the hybrid. The store was passing
    # a keyword lookup and being credited with retrieval. The oblique phrasing
    # keeps the entity, because without it the question is unanswerable rather
    # than harder, and changes everything else.
    oblique: str = ""


@dataclass(frozen=True)
class Question:
    """A fact asked at a delay, or a NEGATIVE that was never told.

    Negatives are in the exam from the first run. The right answer to one is
    "I don't know", and the rate of invented answers is scored SEPARATELY from
    the rate of correct ones -- a core that confabulates fluently would
    otherwise read as a core that remembers.
    """

    fact_id: str | None  # None for a negative
    text: str
    answer: str | None
    kind: str
    delay_turns: int


@dataclass
class House:
    """A fictional house: facts about invented people, places and numbers.

    INVENTED SO THEY CANNOT BE IN THE PRETRAINING. A world made of real facts
    measures what the checkpoint already knew, which is the one thing this
    branch is not asking about.
    """

    seed: int
    facts: list[Fact] = field(default_factory=list)
    turns: list[str] = field(default_factory=list)  # the told-and-filler conversation
    questions: list[Question] = field(default_factory=list)


@dataclass(frozen=True)
class Score:
    """One arm's result on one house, with everything a verdict needs beside it."""

    tier: Tier
    correct: int
    asked: int
    invented: int  # answers made up for questions nobody was told the answer to
    echoed: int  # the question handed back instead of answered
    by_delay: dict[int, float] = field(default_factory=dict)
    by_kind: dict[str, float] = field(default_factory=dict)

    @property
    def score(self) -> float:
        return self.correct / self.asked if self.asked else 0.0


class Baseline(Protocol):
    """What an arm is reported against. Never absent from a reading."""

    def answer(self, question: Question, house: House) -> str: ...


# IMPORTED AT THE BOTTOM BECAUSE `world`, `baselines` AND `run` IMPORT THE TYPES
# ABOVE FROM THIS MODULE. The types are the protocol layer and the three modules
# are what implement against it, so the cycle only resolves in this direction.
from .baselines import (
    BlindBaseline,
    FullContextBaseline,
    blind_baseline,
    estimate_full_context_tokens,
    full_context_baseline,
    is_refusal,
)
from .run import ExamResult, is_echo, judge, run_blind, run_exam, run_full_context, spread
from .world import DEFAULT_DELAYS, Generated, generate_house, questions_at

__all__ = [
    "DEFAULT_DELAYS",
    "Baseline",
    "BlindBaseline",
    "ExamResult",
    "Fact",
    "FullContextBaseline",
    "Generated",
    "House",
    "Question",
    "Score",
    "Tier",
    "blind_baseline",
    "estimate_full_context_tokens",
    "full_context_baseline",
    "generate_house",
    "is_echo",
    "is_refusal",
    "judge",
    "questions_at",
    "run_blind",
    "run_exam",
    "run_full_context",
    "spread",
]
