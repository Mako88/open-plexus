"""Non-learning baselines for the MQAR benchmark — G0's floor and its sanity check.

Every number this project ever reports has to be read against these. A score is
meaningless without knowing what a system that has learned *nothing* achieves on
the same data, and several distinct kinds of nothing are worth distinguishing:

* `constant` is the base rate. Without it a weak positive is indistinguishable
  from an elaborate way of guessing.
* `positional` and `most_recent_value` are cheap heuristics that would score
  well only if the task were accidentally easy in a specific way. They are
  probes for defects in the generator, not candidate models.
* `oracle` must score exactly 1.0. It is a connection test on the *task*: if
  perfect information cannot answer the question, the question is ill-posed and
  every measurement taken on it is void.

A baseline is a callable `(sequence, position) -> int`, returning a predicted
value token. Apart from `oracle`, a baseline must read only `tokens[:position+1]`
— predictions that peek at the future are not baselines, they are bugs, and
`tests/test_baselines.py` asserts causality by scrambling the future and
requiring the prediction to be unchanged.

Standard library only. Nothing here learns.
"""

from __future__ import annotations

import random
from collections import Counter
from collections.abc import Callable, Iterable

from openplexus.tasks.mqar import MqarConfig, MqarSequence

Baseline = Callable[[MqarSequence, int], int]


def oracle(sequence: MqarSequence, position: int) -> int:
    """Answer with perfect knowledge of the pairs.

    Must score exactly 1.0 on any well-posed configuration. Anything less means
    the same key is being asked two different questions, or a query has no pair
    — the defect class that made the generator's first output unanswerable.
    """
    return sequence.pairs[sequence.tokens[position]]


def fit_constant(sequences: Iterable[MqarSequence], config: MqarConfig) -> Baseline:
    """Always answer with the most common value in the fitting data.

    This is **the base rate**, and it is fitted rather than assumed: the most
    common target is an empirical property of the generator, and reasoning about
    it invites being wrong about the very number every other result is compared
    against.
    """
    counts = Counter(v for s in sequences for v in s.scored_targets())
    if not counts:
        raise ValueError("no scored targets to fit a base rate on")
    most_common = counts.most_common(1)[0][0]

    def predict(sequence: MqarSequence, position: int) -> int:
        return most_common

    return predict


def uniform_random(config: MqarConfig, seed: int = 0) -> Baseline:
    """Answer with a uniformly random value token.

    Distinct from `fit_constant` because they only coincide when the value
    distribution is flat. Where they differ, the difference is the generator's
    imbalance, which is worth seeing.
    """
    rng = random.Random(seed)

    def predict(sequence: MqarSequence, position: int) -> int:
        return config.n_keys + rng.randrange(config.n_values)

    return predict


def most_recent_value(config: MqarConfig) -> Baseline:
    """Answer with the most recent value token seen before this position.

    A probe, not a model. It scores well only if queries tend to follow their own
    pair closely — which would mean recall distance is short and the benchmark is
    not testing retention at all.
    """
    def predict(sequence: MqarSequence, position: int) -> int:
        for token in reversed(sequence.tokens[:position]):
            if token >= config.n_keys and token != config.pad_token:
                return token
        return config.n_keys

    return predict


def positional(config: MqarConfig) -> Baseline:
    """Answer with the value of the pair at the same ordinal index as this query.

    A probe for one specific generator defect: if queries were emitted in the
    same order the pairs were presented, this would score 1.0 while knowing
    nothing about content. It is the reason `generate` shuffles query order, and
    this baseline is what would notice if that shuffle were ever removed.
    """
    def predict(sequence: MqarSequence, position: int) -> int:
        rank = sequence.query_positions.index(position)
        values = [sequence.tokens[2 * i + 1] for i in range(len(sequence.pairs))]
        return values[rank] if rank < len(values) else config.n_keys

    return predict


def accuracy(baseline: Baseline, sequences: Iterable[MqarSequence]) -> float:
    """Fraction of scored positions answered correctly.

    Scored positions only — averaging over the whole sequence would dilute the
    measurement with a large number of positions where no answer is required,
    and inflate every baseline towards each other.
    """
    correct = total = 0
    for sequence in sequences:
        for position in sequence.query_positions:
            correct += baseline(sequence, position) == sequence.targets[position]
            total += 1
    if total == 0:
        raise ValueError("no scored positions to measure")
    return correct / total
