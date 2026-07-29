"""Relations between things that resemble each other.

## Why this exists

Every task in this project works on interchangeable symbols. Entity 4 is no more
like entity 5 than like entity 40, so **there is nothing for a concept to be**,
and `concepts.Shared`, `grouping.cluster` and `keys.ByConcept` — all built, all
tested — have nowhere to show whether they mean anything. g17-01 recorded this in
July and chose to build the fix; the project went to word-level text instead, and
decisions 135–142 are what that cost.

[Note 048](../../docs/notes/048-a-task-where-concepts-can-mean-something.md) is
the design. This is it.

## The shape

**Entities belong to families, and a family keeps company.** Each family has its
own attribute tokens, and an entity appears beside its family's attributes. That
is the only way the family is ever revealed: `ContentIndex` learns from
co-occurrence and nothing else, so the structure is **discoverable rather than
handed over**.

**Facts are stated once and arbitrarily.** Every family is assigned a value
*per sequence*, redrawn each time. No statistic over the corpus predicts it —
note 047's condition for the store to be able to pay at all. A model that learns
"family 3 answers 7" across sequences has learned nothing, because next sequence
it does not.

**Two kinds of question:**

    DIRECT     the entity's own fact was stated in this sequence
    TRANSFER   it was NOT -- but other members of its family had theirs stated

**TRANSFER is the whole point.** An entity treated as an arbitrary symbol has had
nothing said about it and can only be guessed at. An entity grouped with its
family can be answered from what was stated about its siblings. That is awareness
of the interrelation between concepts, made scoreable.

## Two streams, and separating them is the whole reason this works

**BACKGROUND** sequences carry the attribute mentions and nothing else. They are
what `ContentIndex` is fitted on, across many of them, and they are never run
through the model.

**TASK** sequences carry the stated facts and the questions, and nothing else.
They are short — a few dozen tokens — so the store holds a handful of bindings
rather than a thousand.

The first version put both in one sequence. At the exposure needed for the
families to be perfectly recoverable it ran to ~1500 tokens, which would have
asked the store to hold ~1500 bindings before the first fact arrived — the
over-subscription that decisions 141–142 spent a morning ruling out as an
explanation for something else. **The calibration found it before anything was
measured**, which is what a calibration is for.

The split also matches the architecture's own division rather than fighting it:
the concept map is durable and learned across sequences, the store is working
memory for this one.

## The division of labour this task is built to expose

    ContentIndex + grouping     durable, learned across sequences: which
                                entities are the same KIND of thing
    the store                   per-sequence working memory: what was said
                                about them THIS time

Neither half answers TRANSFER alone. The grouping knows the entity has siblings
but not what was said; the store knows what was said about the siblings but not
that they are siblings. **Only the composition answers**, which is why a null
here would be a finding about `concepts.py`'s indirection rather than about
either part.
"""

from __future__ import annotations

from dataclasses import dataclass, replace

import numpy as np

#: Marks the token after it as a stated fact's subject: `FACT entity value`.
FACT = 0
#: Marks a question: `QUERY entity value`, and the value is what is scored.
QUERY = 1
#: How many token ids are spoken for before entities begin.
RESERVED = 2


@dataclass(frozen=True)
class FamilyConfig:
    """One task instance.

    Attributes:
        n_families: How many kinds of thing there are.
        family_size: Entities per family. **Must exceed `stated_per_family`**,
            or no entity is ever left unstated and TRANSFER has no positions.
        n_attributes: Attribute tokens per family. These are what make a family
            discoverable — an entity is seen beside them and nowhere else.
        n_values: The answer alphabet. Chance on any query is `1 / n_values`.
        stated_per_family: How many members of a family have their fact stated.
            The rest are TRANSFER targets.
        attribute_mentions: How many times each entity is shown beside its
            attributes. **The discoverability dial.** Too few and no clustering
            is possible; too many and the task measures nothing but the
            clusterer. Calibrated before use — note 048's stated risk.
        queries_per_kind: Queries of each kind per sequence.
        seed: Draws everything.
    """

    n_families: int = 8
    family_size: int = 4
    n_attributes: int = 3
    n_values: int = 8
    stated_per_family: int = 2
    attribute_mentions: int = 2
    queries_per_kind: int = 2
    seed: int = 0

    def __post_init__(self) -> None:
        if self.family_size <= self.stated_per_family:
            raise ValueError(
                f"family_size {self.family_size} must exceed "
                f"stated_per_family {self.stated_per_family}, or every entity "
                f"has its fact stated and TRANSFER has no positions to score")
        if self.n_families < 2:
            raise ValueError("one family is not a similarity structure")
        if self.queries_per_kind > self.stated_per_family:
            raise ValueError(
                f"cannot ask {self.queries_per_kind} DIRECT questions when only "
                f"{self.stated_per_family} facts are stated per family")

    @property
    def n_entities(self) -> int:
        return self.n_families * self.family_size

    @property
    def entity_base(self) -> int:
        return RESERVED

    @property
    def attribute_base(self) -> int:
        return self.entity_base + self.n_entities

    @property
    def value_base(self) -> int:
        return self.attribute_base + self.n_families * self.n_attributes

    @property
    def vocab_size(self) -> int:
        return self.value_base + self.n_values

    @property
    def trivial(self) -> float:
        """What guessing scores. The answer is one of `n_values`."""
        return 1.0 / self.n_values

    def family_of(self, entity_token: int) -> int:
        return (int(entity_token) - self.entity_base) // self.family_size

    def families(self) -> list[list[int]]:
        """True family membership as token ids, for scoring a grouping.

        **The answer key, and nothing in the model may read it.** It exists so a
        calibration can ask whether `grouping.cluster` recovered the structure,
        which is a different question from whether the model can use it.
        """
        return [[self.entity_base + f * self.family_size + i
                 for i in range(self.family_size)]
                for f in range(self.n_families)]


@dataclass(frozen=True)
class Sequence:
    tokens: tuple[int, ...]
    #: Positions holding a query's ENTITY. The token after each is the answer,
    #: so `targets = roll(tokens, -1)` scores them -- the autoregressive
    #: convention every MQAR script uses, and decision 138's correction.
    query_positions: tuple[int, ...]
    #: Same order as `query_positions`. True where the entity's own fact was
    #: NOT stated, which is the arm the task exists to measure.
    is_transfer: tuple[bool, ...]


def background(config: FamilyConfig, count: int) -> list[np.ndarray]:
    """Streams of attribute mentions, for fitting `ContentIndex`.

    **Never run through the model.** This is where the family structure lives,
    and it is learned across many of these rather than inside any one task
    sequence.

    An entity sits BETWEEN two of its attributes rather than before them, so its
    immediate neighbours are both its own. The first version put it first, and at
    `window=1` its other neighbour was the previous mention's last token -- pure
    noise from a different family, and worth 0.375 purity against 0.875 once
    fixed. Order is shuffled across families so that adjacency alone never
    reveals the grouping.
    """
    rng = np.random.default_rng(config.seed + 7919)
    streams: list[np.ndarray] = []
    for _ in range(count):
        mentions: list[tuple[int, ...]] = []
        for family in range(config.n_families):
            attributes = [
                config.attribute_base + family * config.n_attributes + a
                for a in range(config.n_attributes)]
            for index in range(config.family_size):
                entity = (config.entity_base + family * config.family_size
                          + index)
                for _ in range(config.attribute_mentions):
                    drawn = rng.choice(attributes, size=2,
                                       replace=len(attributes) < 2)
                    mentions.append((int(drawn[0]), entity, int(drawn[1])))
        rng.shuffle(mentions)
        streams.append(np.asarray([t for m in mentions for t in m],
                                  dtype=np.int64))
    return streams


def generate(config: FamilyConfig, seed: int | None = None) -> Sequence:
    """One TASK sequence: stated facts, then questions. No attributes."""
    rng = np.random.default_rng(config.seed if seed is None else seed)
    tokens: list[int] = []

    # ONE VALUE PER FAMILY, REDRAWN EVERY SEQUENCE. Without the redraw a global
    # prior learns the mapping and TRANSFER becomes counting -- note 047's
    # failure one level up.
    values = rng.integers(0, config.n_values, size=config.n_families)

    stated: dict[int, int] = {}
    unstated: list[int] = []
    for family in range(config.n_families):
        order = rng.permutation(config.family_size)
        for rank, index in enumerate(order):
            entity = config.entity_base + family * config.family_size + int(index)
            if rank < config.stated_per_family:
                stated[entity] = config.value_base + int(values[family])
            else:
                unstated.append(entity)

    facts = list(stated.items())
    rng.shuffle(facts)
    for entity, value in facts:
        tokens.extend((FACT, entity, value))

    # QUESTIONS. DIRECT draws from entities whose fact was stated, TRANSFER from
    # those whose was not -- and both answer with their FAMILY's value, which is
    # what makes the two comparable.
    asked: list[tuple[int, int, bool]] = []
    direct_pool = list(stated)
    rng.shuffle(direct_pool)
    for entity in direct_pool[:config.queries_per_kind]:
        asked.append((entity, stated[entity], False))
    rng.shuffle(unstated)
    for entity in unstated[:config.queries_per_kind]:
        family = config.family_of(entity)
        asked.append((entity, config.value_base + int(values[family]), True))
    rng.shuffle(asked)

    positions: list[int] = []
    transfer: list[bool] = []
    for entity, answer, is_transfer in asked:
        tokens.append(QUERY)
        positions.append(len(tokens))
        tokens.extend((entity, answer))
        transfer.append(is_transfer)

    return Sequence(tuple(tokens), tuple(positions), tuple(transfer))


def dataset(config: FamilyConfig, count: int) -> list[Sequence]:
    """`count` sequences, each with its own draw of family values."""
    rng = np.random.default_rng(config.seed)
    return [generate(replace(config, seed=int(s)))
            for s in rng.integers(0, 2**31 - 1, size=count)]
