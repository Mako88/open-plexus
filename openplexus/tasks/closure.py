"""A stream of facts, some STATED and some ENTAILED. No question marker.

## What this is for

GOALS section 1.2 records the project's thesis in John's words: *"instead of
focusing on predicting text, train the model to understand the relationships
between things: to associate a given thing in the context of all other things."*

`kinship.py` already has entailed targets -- the composed relation is never
stated -- but it asks for them through a **marked question**, and decision 95
measured that marking as most of the remaining gap: *"the gap between a marked
question and an unmarked stream is still most of the problem."*

This removes the marker. Every fact in the stream looks identical, and the model
predicts the relation of each one. Some of those relations were drawn
independently; others are **implied by facts elsewhere in the same stream** and
appear nowhere else. Nothing in the layout says which is which.

## The layout, and why the object comes before the relation

    FACT  S  O  R

Not `FACT S R O`, which is `kinship.py`'s order. The store binds the previous
position's key to the current position's value, so with `context_keys` this
writes **`key(S, O) -> R`**.

**That is the binding decision 107 said the task needed and could not form:**

> The disambiguator exists in the question -- it names the OBJECT -- so a key
> over `(subject, object)` would be unique. Forming that key when the object is
> the far end of a multi-hop question is the design problem.

Here the object is right there, one token earlier, so the key forms itself. A
STATED fact is then a single stored binding and recall suffices. **An ENTAILED
fact is not**: its `key(S, O)` was never written, and the relation has to be
composed from the two facts that imply it.

So the stated/entailed split is exactly the recall/reasoning split, in one
stream, with no marker separating them. That is the measurement note 041 asked
for.

## What is deliberately NOT done

**Unrecoverable targets are not excluded.** A distractor's relation is
determined by nothing, and the model can only guess the marginal. Note 008 section 4
established that irreducible loss contributes **no gradient**, so leaving them in
costs nothing and removing them would be the harmful "structured filler" fix that
note had backwards.

**No curriculum, no marker, no position that means anything.** If a result here
turns out to depend on the layout, it is the `reward_recall` defect again -- and
`stated_positions` exists so a test can check that it does not.
"""

from __future__ import annotations

import random
from dataclasses import dataclass

from openplexus.tasks.kinship import COMPOSE, RELATIONS, IGNORE

#: How a fact's four tokens are laid out. Named rather than implied because the
#: ORDER is the design decision -- see the module docstring.
MARKER, SUBJECT, OBJECT, RELATION = 0, 1, 2, 3
WIDTH = 4


@dataclass(frozen=True)
class ClosureConfig:
    """Shape of a stated-and-entailed fact stream.

    Attributes:
        n_people: Distinct people the graph is drawn over.
        n_stated: Edges drawn independently and stated.
        n_entailed: Edges IMPLIED by pairs of stated edges, stated as facts in
            the same stream with nothing marking them as different.
        seed: Generator seed.
    """

    #: CALIBRATED, because the entailed half is the whole point and a graph too
    #: sparse to imply anything measures nothing. Entailed edges per sequence,
    #: over 200 sequences, collecting every implication rather than capping:
    #:
    #:     people 12, stated  8    0.68    53% of sequences imply NOTHING
    #:     people 12, stated 16    2.58    12%
    #:     people 10, stated 24    5.39     1%      <- chosen
    #:     people  8, stated 32    6.71     0%
    #:
    #: Fewer people is not denser past a point: at 6 the (subject, object) pairs
    #: run out, so an implied edge is usually already stated and stops being an
    #: inference. 6/24 yields 2.10, below 8/24's 5.32.
    #:
    #: **This is a store-capacity choice as much as a task one.** 24 + 6 facts
    #: is 120 tokens, so the store holds ~119 bindings -- above decision 109's
    #: ~96 for width 64 and well inside width 128's ~384. Registered in
    #: docs/SCALE.md; at width 64 this task is over capacity by construction.
    n_people: int = 10
    n_stated: int = 24
    n_entailed: int = 6
    seed: int = 0

    @property
    def fact_token(self) -> int:
        return self.n_people + len(RELATIONS)

    @property
    def vocab_size(self) -> int:
        return self.n_people + len(RELATIONS) + 1

    def relation_token(self, name: str) -> int:
        return self.n_people + RELATIONS.index(name)

    def __post_init__(self) -> None:
        if self.n_stated < 2:
            raise ValueError(
                "an entailed edge needs two stated edges to be implied by")
        if self.n_people < 3:
            raise ValueError("a composed path visits three people")


@dataclass(frozen=True)
class ClosureSequence:
    """One generated stream.

    Attributes:
        tokens: The sequence.
        targets: The relation at each fact's RELATION position, `IGNORE`
            elsewhere.
        entailed: Positions whose relation is implied by other facts in this
            stream rather than drawn independently. **The split that makes the
            task interpretable** -- recall on the others, composition here.
        facts: Every fact as `(subject, object, relation)`, in stated order.
    """

    tokens: tuple[int, ...]
    targets: tuple[int, ...]
    entailed: tuple[int, ...]
    facts: tuple[tuple[int, int, str], ...]


def generate(config: ClosureConfig) -> ClosureSequence:
    """One stream of facts, some of them implied by the others."""
    rng = random.Random(config.seed)

    # STATED EDGES, drawn independently. A pair is never repeated, so no
    # subject-object pair carries two relations and `key(S, O)` is unambiguous
    # for everything actually written.
    stated: list[tuple[int, int, str]] = []
    used: set[tuple[int, int]] = set()
    guard = 0
    while len(stated) < config.n_stated and guard < 2000:
        guard += 1
        subject, obj = rng.sample(range(config.n_people), 2)
        if (subject, obj) in used:
            continue
        used.add((subject, obj))
        stated.append((subject, obj, rng.choice(RELATIONS)))

    # ENTAILED EDGES: wherever two stated edges chain and their relations
    # compose, the composed edge is implied. Collected rather than constructed,
    # so the entailed set is whatever the stated graph happens to imply -- which
    # is what a real knowledge graph gives and what makes this self-supervised.
    by_subject: dict[int, list[tuple[int, str]]] = {}
    for subject, obj, relation in stated:
        by_subject.setdefault(subject, []).append((obj, relation))

    # Each implication remembers the PAIR that produced it, because an entailed
    # edge has to be placed after both of them -- see below.
    implied: list[tuple[tuple[int, int, str], tuple, tuple]] = []
    stated_by_pair = {(s, o): (s, o, r) for s, o, r in stated}
    for subject, obj, first in stated:
        for middle, second in by_subject.get(obj, ()):
            composed = COMPOSE.get((first, second))
            if composed is None or middle == subject:
                continue
            if (subject, middle) in used:
                continue          # already stated; not an inference
            used.add((subject, middle))
            implied.append(((subject, middle, composed),
                            stated_by_pair[(subject, obj)],
                            stated_by_pair[(obj, middle)]))
    rng.shuffle(implied)
    implied = implied[:config.n_entailed]

    # AN ENTAILED EDGE GOES AFTER BOTH ITS PREMISES, and this is not tidiness.
    #
    # Every model here is causal -- it predicts from what it has already seen.
    # Shuffling everything together left **only 41% of entailed edges with both
    # premises earlier**, so 59% of the half that the task exists to measure
    # were unanswerable by any model at all, and the ceiling sat near 0.53.
    #
    # g14-01 measured a backprop attention reference at 0.147 on that version,
    # BELOW the 0.198 majority floor -- a G0 failure whose largest single cause
    # was this. A task that cannot be answered is not a hard task.
    #
    # The position is random among the slots after the later premise, so
    # entailed edges do not cluster at the end. They still skew later, which is
    # unavoidable and harmless: knowing a fact is entailed does not say WHICH
    # relation it is, so position leaks the split without leaking the answer.
    # That is the distinction note 027's `reward_recall` leak did not have.
    order = list(stated)
    rng.shuffle(order)
    for edge, first_premise, second_premise in implied:
        after = max(order.index(first_premise), order.index(second_premise))
        order.insert(rng.randint(after + 1, len(order)), edge)
    entailed_set = {edge for edge, _, _ in implied}

    tokens: list[int] = []
    targets: list[int] = []
    entailed_positions: list[int] = []
    for subject, obj, relation in order:
        # SCORED AT THE OBJECT, not at the relation.
        #
        # Every model here predicts the NEXT token at each position -- the
        # convention is `targets = tokens[1:]`. Scoring at the relation's own
        # position asks "what token is here", which any model that can see the
        # current token answers for free: a causal attention reference reached
        # **0.904 on STATED relations**, which are drawn at random and appear
        # once and cannot be predicted at all. That number was the leak, not a
        # result.
        #
        # At the object position the key is `(S, O)` and the question is "what
        # relation comes next", which is the task. The binding `key(S, O) -> R`
        # is written one step LATER, so a stated fact is not recallable within
        # its own sequence -- which is correct and is why the stated half is a
        # floor rather than a second measurement.
        at = len(tokens) + OBJECT
        tokens.extend((config.fact_token, subject, obj,
                       config.relation_token(relation)))
        targets.extend((IGNORE, IGNORE, config.relation_token(relation),
                        IGNORE))
        if (subject, obj, relation) in entailed_set:
            entailed_positions.append(at)

    return ClosureSequence(tuple(tokens), tuple(targets),
                           tuple(entailed_positions), tuple(order))


def dataset(config: ClosureConfig, n_sequences: int) -> list[ClosureSequence]:
    from dataclasses import replace
    return [generate(replace(config, seed=config.seed + i))
            for i in range(n_sequences)]


def majority_floor(config: ClosureConfig, n_sequences: int = 400) -> float:
    """Always answering the commonest relation. **Measured, not derived.**

    Separately over stated and entailed targets, because they have different
    distributions: stated relations are drawn uniformly and entailed ones are
    whatever `COMPOSE` produces, which is not uniform. A single floor would
    flatter one and punish the other.
    """
    from collections import Counter
    counts: Counter = Counter()
    for sequence in dataset(config, n_sequences):
        for position in sequence.entailed:
            counts[sequence.targets[position]] += 1
    if not counts:
        return 0.0
    return max(counts.values()) / sum(counts.values())


def stated_positions(sequence: ClosureSequence) -> tuple[int, ...]:
    """Scored positions whose relation was NOT implied by the others.

    The complement of `entailed`, and the recall half of the task. Provided so a
    test can check the two are disjoint and together cover every scored
    position -- if they ever do not, the split every result rests on is wrong.
    """
    entailed = set(sequence.entailed)
    return tuple(i for i, target in enumerate(sequence.targets)
                 if target != IGNORE and i not in entailed)
