"""Scoring an answer that is a SET of tokens rather than one token.

## IN PLAIN TERMS

Every score in this project so far answers one question: did the model emit the
right single token? The project's actual goal — GOALS section 1, *"process a query
and respond from that awareness"* — needs answers with more than one thing in
them, and nothing here has ever scored one. This module is the ruler for that,
built before any mechanism that produces such an answer, because the ruler is
where this kind of measurement goes wrong.

## The one trap, and it is not subtle

**A model that emits EVERYTHING scores perfect recall.** Recall alone is a metric
that improves as the answer gets less useful, and it improves fastest for the
laziest possible mechanism. Precision is what the emptiness gate (decision 148) is
supposed to buy, so precision is exactly the quantity a set score must not be
allowed to hide.

So `SetScore` carries precision, recall and F1 together and **the reportable
number is `exact` or `f1`, never `recall`**. `emit_everything` exists in the tests
as the standing falsifier: it must score near zero, and if a change ever makes it
score well the change is wrong however good the headline looks.

This is CLAUDE.md rule 7 — a criterion that cancels its own input — caught at
design time rather than after a sweep. Recall does not *cancel* its input; it is
worse than that, being monotone in the wrong direction.

## Why it degenerates exactly

Every existing task emits one token, and every accuracy number in this project is
`predicted == truth` over query positions. `exact` on singleton sets is that same
comparison, so the whole comparison set stays interpretable: a task rewritten to
this convention must reproduce its old number to the last decimal. `is_singleton`
and the tests around it are that gate, and decision 138 is why it exists — a wrong
target survived four sweeps and 142 cells because every arm was wrong identically.

## Dependency-free on purpose

This is the ruler, and the ruler does not import numpy — see CLAUDE.md's
conventions and note 007. Pure Python here is auditable line by line, which is the
whole argument for the task layer being written this way.
"""

from __future__ import annotations

from collections.abc import Iterable
from dataclasses import dataclass


@dataclass(frozen=True)
class SetScore:
    """How well one predicted answer set matches one true answer set.

    The contract: `exact` and `f1` are reportable; `recall` is not, on its own,
    and the module docstring says why. All four are present because a precision
    and recall pair diagnoses *how* an answer is wrong in a way F1 alone cannot —
    over-emitting and under-emitting are different defects with different fixes.

    `size` and `truth_size` are carried so an aggregate can say what it is a
    statistic *of* (rule 8): a mean F1 over answers of wildly different sizes
    describes no particular answer.
    """

    exact: bool
    precision: float
    recall: float
    f1: float
    size: int
    truth_size: int

    @property
    def is_singleton(self) -> bool:
        """True when this is the one-token case every earlier task measured."""
        return self.truth_size == 1 and self.size == 1


def score_one(predicted: Iterable[int], truth: Iterable[int]) -> SetScore:
    """Score one answer set against one true set.

    Duplicates in either argument are collapsed: an answer is a set, and emitting
    a token twice is not two answers. That is a decision rather than a detail —
    a traversal that revisits a concept would otherwise inflate its own recall.
    """
    got = frozenset(int(token) for token in predicted)
    want = frozenset(int(token) for token in truth)
    if not want:
        raise ValueError(
            "an empty TRUE set has no defined precision or recall, and scoring "
            "it as 1.0 for an empty prediction would let a task with no answer "
            "raise the mean. If a query legitimately has no answer, that is the "
            "refusal-to-answer measurement (ARCHITECTURE row C4), scored as a "
            "binary rather than as a set")
    hits = len(got & want)
    precision = hits / len(got) if got else 0.0
    recall = hits / len(want)
    f1 = (2.0 * precision * recall / (precision + recall)
          if precision + recall > 0.0 else 0.0)
    return SetScore(exact=got == want, precision=precision, recall=recall,
                    f1=f1, size=len(got), truth_size=len(want))


@dataclass(frozen=True)
class SetScoreSummary:
    """An aggregate over many answers, with what it is a statistic of attached.

    `exact` is the headline. `mean_f1` is the partial-credit view and is reported
    beside it rather than instead of it, because a mechanism can move one without
    the other and which one moved is the finding.

    `mean_size` against `mean_truth_size` is the over-emission tell: a mechanism
    buying F1 by guessing more shows up here and nowhere else in the headline.
    """

    n: int
    exact: float
    mean_precision: float
    mean_recall: float
    mean_f1: float
    mean_size: float
    mean_truth_size: float
    #: How many of the scored answers were the single-token case. `n` when a task
    #: has been rewritten to this convention without changing its questions,
    #: which is the reproduction gate.
    singletons: int


def summarise(scores: Iterable[SetScore]) -> SetScoreSummary:
    """Aggregate scores, refusing to report a mean over nothing.

    An accumulator returning its own initial value is rule 8's named hazard, so
    this raises rather than returning zeros for an empty input.
    """
    scores = list(scores)
    if not scores:
        raise ValueError(
            "nothing to summarise. A zero returned here is indistinguishable "
            "from a mechanism that scored zero, which is the accumulator "
            "reporting its own initial value")
    n = len(scores)
    return SetScoreSummary(
        n=n,
        exact=sum(1 for s in scores if s.exact) / n,
        mean_precision=sum(s.precision for s in scores) / n,
        mean_recall=sum(s.recall for s in scores) / n,
        mean_f1=sum(s.f1 for s in scores) / n,
        mean_size=sum(s.size for s in scores) / n,
        mean_truth_size=sum(s.truth_size for s in scores) / n,
        singletons=sum(1 for s in scores if s.is_singleton),
    )


def single_token_accuracy(scores: Iterable[SetScore]) -> float:
    """The pre-set-valued number, recovered from set scores.

    **This is the reproduction gate, as a function rather than a promise.** Every
    accuracy in `experiments/` is `predicted == truth` over query positions. On
    singleton answers this returns exactly that, so a task rewritten to the set
    convention can be checked against its own published figure instead of being
    assumed comparable.

    Raises if any answer is not a singleton, because averaging a set score into a
    slot labelled "accuracy" is how a number stops meaning what its column says.
    """
    scores = list(scores)
    if not scores:
        raise ValueError("nothing to score")
    unsuitable = [s for s in scores if s.truth_size != 1]
    if unsuitable:
        raise ValueError(
            f"{len(unsuitable)} of {len(scores)} answers have more than one true "
            "token, so there is no single-token accuracy to recover. Report "
            "`exact` instead -- it is the same quantity where both are defined")
    return sum(1 for s in scores if s.exact) / len(scores)
