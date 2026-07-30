"""Relations between things that resemble each other.

## Why this exists

Every task in this project works on interchangeable symbols. Entity 4 is no more
like entity 5 than like entity 40, so **there is nothing for a concept to be**,
and `concepts.Shared`, `grouping.cluster` and `keys.ByConcept` — all built, all
tested — have nowhere to show whether they mean anything. g17-01 recorded this in
July and chose to build the fix; the project went to word-level text instead, and
decisions 135–142 are what that cost.

[Note 048](../../docs/archive/notes/048-a-task-where-concepts-can-mean-something.md) is
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

    DIRECT      the entity's own fact was stated, and agrees with its family
    TRANSFER    it was NOT stated -- but siblings' were
    EXCEPTION   its own fact WAS stated and CONTRADICTS its family's value

**TRANSFER is the whole point.** An entity treated as an arbitrary symbol has had
nothing said about it and can only be guessed at. An entity grouped with its
family can be answered from what was stated about its siblings. That is awareness
of the interrelation between concepts, made scoreable.

**EXCEPTION is the falsifier for the mechanism that answers TRANSFER**, and it
was added after decision 143 rather than before, so it is a test of that result
and not part of it. Grouping works by giving a family ONE store address. An
entity whose own stated fact contradicts its family's therefore collides with its
siblings at that address, and the same superposition that carries transfer may
make the exception unrepresentable.

    a system that cannot hold "birds fly, but not this one" does not
    understand birds

`ungrouped` should find EXCEPTION easy -- the fact was stated about that very
entity, so for a model with no grouping it is ordinary recall. **If `concept`
scores worse than `ungrouped` there, grouping buys transfer by spending
specificity, and the price is now measured rather than argued.**

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
#: Marks a family-to-family link, and **only exists when `family_links` is on**
#: -- note 050's instrument. Adding a marker unconditionally would shift
#: `entity_base` and stop every number in decisions 143-151 reproducing, which
#: is decision 74's failure exactly. So the reservation is a property of the
#: config rather than a constant, and `config.reserved` is what to read.
LINK = 2

#: How many token ids are spoken for before entities begin, WITHOUT links.
#: Read `config.reserved`, never this, unless you mean the link-free layout.
RESERVED = 2

#: Marks a SET-VALUED question: `ASK_ALL entity`, and **no answer token follows
#: it** -- the true answer is a set and lives in `Sequence.answer_sets` instead.
#: Only exists when `set_queries` is on, and its id depends on whether `LINK` took
#: one first, so **read `config.ask_all` and never this constant**. The name is
#: here for grep-ability and for the same reason `LINK` is: the layout decision
#: should be visible at the top of the file.
ASK_ALL_NAME = "ASK_ALL"


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
        exceptions_per_family: Members whose stated value CONTRADICTS their
            family's. 0 reproduces the task as decision 143 measured it, so
            that result is not silently changed by this field existing.
        queries_per_kind: Queries of each kind per sequence.
        seed: Draws everything.
    """

    n_families: int = 8
    family_size: int = 4
    n_attributes: int = 3
    n_values: int = 8
    stated_per_family: int = 2
    exceptions_per_family: int = 0
    #: State a LINK between families and ask a fourth kind of question --
    #: note 050's instrument.
    #:
    #: **REFUTED AS LAID OUT — decision 155, and left in place deliberately.**
    #: A link is written `LINK here there` with ENTITY endpoints, which binds
    #: `key(here) -> there` and so overwrites the stated fact living at that
    #: very address. Every column collapses to chance. The byte-identity rail
    #: and the index calibration both still hold, which is why this is kept
    #: rather than deleted -- they are the expensive parts and they are
    #: independent of the endpoint choice.
    #:
    #: False reproduces the task decisions 143-151 measured, token for token.
    #: `tests/test_families.py` asserts that rather than intending it: the link
    #: permutation is drawn from a SEPARATE rng so the main draw sequence is
    #: untouched, and the marker is reserved only when this is on.
    family_links: bool = False
    #: How many of each family's attribute tokens are SHARED with the next family,
    #: making the families genuinely confusable rather than merely under-observed.
    #:
    #: **This exists because note 056 measured the wrong axis.** It degraded index
    #: purity by starving the index of background streams, which conflates two
    #: different situations: an index that has not seen enough, and a concept space
    #: where the concepts really do overlap. Only the second is what a real grouping
    #: looks like — a system with plenty of data about categories that genuinely
    #: share properties. A cliff rule that survives ambiguity but not starvation is
    #: in much better shape than note 056's table alone can say.
    #:
    #: It is also note 053's silent-merge risk in miniature, at the level where it
    #: can be measured: two things that should stay distinct, described by
    #: overlapping evidence.
    #:
    #: 0 gives every family its own private block, which is what decisions 143-167
    #: measured. Must stay below `n_attributes` or a family has nothing of its own.
    shared_attributes: int = 0
    attribute_mentions: int = 2
    queries_per_kind: int = 2
    #: Ask a question whose answer is a SET -- ARCHITECTURE row F3, and the first
    #: task in this project to do so.
    #:
    #: The question is "what values were stated about this entity's family", and
    #: the answer is every distinct one: the family's own value **and** its
    #: exceptions. **A single token cannot express that answer.** It has to pick
    #: one and lose the other, which is why this is not merely a scoring change --
    #: it makes a fact the task already contained expressible for the first time.
    #:
    #:     a system that cannot hold "birds fly, but not this one" does not
    #:     understand birds
    #:
    #: The module docstring above states that as the reason EXCEPTION exists. A
    #: one-token answer can report the rule or the exception; only a set can
    #: report that both are true, which is what the sentence actually asks for.
    #:
    #: **No answer token is written into the stream.** A set has no single next
    #: token, so `ASK_ALL entity` is two tokens and the truth lives in
    #: `Sequence.answer_sets`. That keeps the training convention untouched --
    #: `targets = roll(tokens, -1)` still scores the stated facts and the
    #: single-token questions exactly as before -- and makes this a read-only
    #: probe rather than a change to what the model learns from.
    #:
    #: Off by default, and `tests/test_families.py` asserts the off path is byte
    #: identical to what decisions 143-151 measured.
    set_queries: bool = False
    seed: int = 0

    def __post_init__(self) -> None:
        if self.family_size <= self.stated_per_family:
            raise ValueError(
                f"family_size {self.family_size} must exceed "
                f"stated_per_family {self.stated_per_family}, or every entity "
                f"has its fact stated and TRANSFER has no positions to score")
        if self.n_families < 2:
            raise ValueError("one family is not a similarity structure")
        if self.exceptions_per_family >= self.stated_per_family:
            raise ValueError(
                f"{self.exceptions_per_family} exceptions of "
                f"{self.stated_per_family} stated facts leaves no member "
                f"agreeing with its family, so the family has no value and "
                f"TRANSFER has no answer")
        if self.queries_per_kind > self.stated_per_family:
            raise ValueError(
                f"cannot ask {self.queries_per_kind} DIRECT questions when only "
                f"{self.stated_per_family} facts are stated per family")
        if not 0 <= self.shared_attributes < self.n_attributes:
            raise ValueError(
                f"shared_attributes {self.shared_attributes} must be in "
                f"[0, {self.n_attributes}) -- a family with no private attribute "
                f"is not a distinguishable family, and the grouping would be "
                f"unrecoverable by construction rather than merely hard")
        if self.set_queries and self.exceptions_per_family < 1:
            # REFUSED RATHER THAN ALLOWED TO BE TRIVIAL. With no exceptions every
            # stated fact in a family carries the same value, so every answer set
            # has exactly one element -- and the whole task would report a
            # set-valued measurement that is the single-token measurement wearing
            # a different column heading. It would also score WELL, because a
            # mechanism emitting one token is right.
            raise ValueError(
                "set_queries needs exceptions_per_family >= 1. Without an "
                "exception every member of a family states the same value, so "
                "every answer set is a singleton and the measurement is the "
                "single-token one relabelled -- passing for a reason that has "
                "nothing to do with emitting a set")

    @property
    def n_entities(self) -> int:
        return self.n_families * self.family_size

    @property
    def reserved(self) -> int:
        """Token ids spoken for before entities begin.

        One more when links are on, because `LINK` needs an id, and one more
        again for `ASK_ALL`. Everything downstream is derived from this, so a
        layout with neither is byte identical to what decisions 143-151 measured.
        """
        return (RESERVED + (1 if self.family_links else 0)
                + (1 if self.set_queries else 0))

    @property
    def ask_all(self) -> int:
        """Token id marking a set-valued question. Read this, not a constant.

        Its value depends on whether `LINK` claimed an id first, which is exactly
        the trap `RESERVED` carries a warning about: a module-level constant for a
        CONDITIONALLY reserved marker is right in one configuration and silently
        an entity id in another.
        """
        if not self.set_queries:
            raise ValueError(
                "ask_all has no id when set_queries is off -- that id belongs to "
                "an entity, and reading it anyway would address a real entity "
                "while looking like a marker")
        return RESERVED + (1 if self.family_links else 0)

    @property
    def linked_family(self) -> tuple[int, ...]:
        """Which family each family points at, as a derangement.

        Drawn from a SEPARATE generator seeded off `seed`, so switching links on
        does not disturb the main draw sequence and the link-free task keeps
        reproducing. A family never links to itself -- a self-link makes the
        question identical to TRANSFER and would quietly dilute the arm.
        """
        if not self.family_links:
            return ()
        rng = np.random.default_rng(self.seed + 104729)
        for _ in range(64):
            order = rng.permutation(self.n_families)
            if all(int(order[f]) != f for f in range(self.n_families)):
                return tuple(int(x) for x in order)
        # Rotation by one is a derangement for any n >= 2, and `n_families >= 2`
        # is enforced above. Reached only if 64 draws all fixed a point, which
        # for n >= 4 is rarer than one in a million -- but a task that sometimes
        # returns a self-link is worse than one that is occasionally regular.
        return tuple((f + 1) % self.n_families for f in range(self.n_families))

    @property
    def entity_base(self) -> int:
        return self.reserved

    @property
    def attribute_base(self) -> int:
        return self.entity_base + self.n_entities

    @property
    def shared_base(self) -> int:
        """First token of the pool every family shares. Read only when on.

        Sits after the private blocks and before the values, so with
        `shared_attributes` at 0 it claims no ids and `value_base` does not move —
        which is what keeps the link-free, share-free layout byte identical to what
        decisions 143-167 measured.
        """
        if not self.shared_attributes:
            raise ValueError(
                "shared_base has no id when shared_attributes is 0 -- that id "
                "belongs to a VALUE, and reading it anyway would name a value "
                "while looking like an attribute")
        return self.attribute_base + self.n_families * self.n_attributes

    @property
    def value_base(self) -> int:
        return (self.attribute_base + self.n_families * self.n_attributes
                + self.shared_attributes)

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
    #: Same order again. True where the entity's own stated fact CONTRADICTS
    #: its family's value -- the falsifier for the mechanism that carries
    #: transfer. Never true at the same position as `is_transfer`.
    is_exception: tuple[bool, ...] = ()
    #: Same order again. True where the answer is the LINKED family's value
    #: rather than this entity's own or its family's -- note 050's arm. The
    #: entity's fact was never stated, so answering needs the gate to notice the
    #: empty address AND the link to be followed. Empty unless `family_links`.
    is_linked: tuple[bool, ...] = ()
    #: Positions holding a SET-VALUED question's ENTITY, kept SEPARATE from
    #: `query_positions` on purpose: nothing follows these in the stream, so a
    #: script that scored them as `roll(tokens, -1)` would compare the model
    #: against whatever token happens to come next. Empty unless `set_queries`.
    set_query_positions: tuple[int, ...] = ()
    #: Same order as `set_query_positions`. Every distinct value stated about the
    #: asked entity's family -- the family's own and its exceptions. Score with
    #: `openplexus.answers`, whose `exact` and `f1` are the reportable numbers and
    #: whose falsifier is that emitting everything must not look good.
    answer_sets: tuple[frozenset[int], ...] = ()


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
            if config.shared_attributes:
                # A POOL EVERY FAMILY DRAWS FROM, replacing that many of this
                # family's own. So a family is described by a shrinking private
                # remainder plus evidence common to all of them, which is what
                # genuine ambiguity looks like.
                #
                # THE FIRST VERSION WAS INERT BY CONSTRUCTION and its own P1 caught
                # it. It had each family borrow its NEIGHBOUR'S attributes, so
                # family f used {f0, g1, g2, g3} -- a set no other family used, and
                # therefore still uniquely identifying. Purity stayed at 1.000
                # sharing three of four attributes. A condition guaranteed absent
                # by the way it is built is not a condition.
                #
                # Entirely inside the conditional and consuming no randomness, so
                # the layout decisions 143-167 were measured on stays byte
                # identical -- the rail in `tests/test_families.py` asserts that.
                keep = config.n_attributes - config.shared_attributes
                attributes = attributes[:keep] + [
                    config.shared_base + s
                    for s in range(config.shared_attributes)]
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
    exceptions: dict[int, int] = {}
    for family in range(config.n_families):
        order = rng.permutation(config.family_size)
        for rank, index in enumerate(order):
            entity = config.entity_base + family * config.family_size + int(index)
            if rank < config.exceptions_per_family:
                # A DIFFERENT value, drawn to differ. The exception must
                # contradict rather than coincide, or the arm measures nothing.
                other = int(rng.integers(0, config.n_values - 1))
                other += int(other >= int(values[family]))
                stated[entity] = config.value_base + other
                exceptions[entity] = config.value_base + other
            elif rank < config.stated_per_family:
                stated[entity] = config.value_base + int(values[family])
            else:
                unstated.append(entity)

    facts = list(stated.items())
    rng.shuffle(facts)
    for entity, value in facts:
        tokens.extend((FACT, entity, value))

    # THE LINKS, note 050's instrument. Stated as `LINK a b` where a and b are
    # REPRESENTATIVE ENTITIES of the two families -- there is no family token,
    # and adding one would hand the model the grouping the task exists to make
    # it discover.
    #
    # Stated AFTER the facts and never in a background stream, so the link
    # cannot reach `ContentIndex`. That is what the calibration in note 050
    # checked: the index carries no family-to-family structure, by construction
    # rather than by luck.
    linked_value: dict[int, int] = {}
    if config.family_links:
        pairs = []
        for family in range(config.n_families):
            other = config.linked_family[family]
            # The representative is the family's FIRST entity, fixed rather than
            # drawn, so the link is a property of the families and not another
            # thing to recover per sequence.
            pairs.append((config.entity_base + family * config.family_size,
                          config.entity_base + other * config.family_size))
            linked_value[family] = config.value_base + int(values[other])
        rng.shuffle(pairs)
        for here, there in pairs:
            tokens.extend((LINK, here, there))

    # QUESTIONS. DIRECT draws from entities whose fact was stated, TRANSFER from
    # those whose was not -- and both answer with their FAMILY's value, which is
    # what makes the two comparable.
    asked: list[tuple[int, int, bool, bool]] = []
    agreeing = [e for e in stated if e not in exceptions]
    rng.shuffle(agreeing)
    for entity in agreeing[:config.queries_per_kind]:
        asked.append((entity, stated[entity], False, False))
    rng.shuffle(unstated)
    for entity in unstated[:config.queries_per_kind]:
        family = config.family_of(entity)
        asked.append((entity, config.value_base + int(values[family]),
                      True, False))
    odd = list(exceptions)
    rng.shuffle(odd)
    for entity in odd[:config.queries_per_kind]:
        asked.append((entity, exceptions[entity], False, True))

    # THE LINKED ARM. Drawn from entities whose own fact was NOT stated, exactly
    # like TRANSFER -- so the gate must fire on both and the difference between
    # them is purely how far the answer is. TRANSFER stops at the family;
    # LINKED follows the link one step further.
    #
    # Taken from the TAIL of `unstated` so a LINKED entity is never also asked
    # as TRANSFER in the same sequence, which would put two different correct
    # answers on one address.
    linked_asked: list[tuple[int, int, bool, bool]] = []
    if config.family_links:
        spare = unstated[config.queries_per_kind:]
        for entity in spare[:config.queries_per_kind]:
            linked_asked.append(
                (entity, linked_value[config.family_of(entity)], False, False))
    if config.family_links:
        asked_all = ([(a, False) for a in asked]
                     + [(a, True) for a in linked_asked])
        rng.shuffle(asked_all)
        asked = [a for a, _ in asked_all]
        linked_flags = [flag for _, flag in asked_all]
    else:
        # THE BYTE-IDENTITY RAIL. The link-free path must make exactly the draws
        # decisions 143-151 measured, in exactly this order -- so it shuffles
        # `asked` itself rather than a list of pairs, because shuffling a
        # different object consumes the generator differently and every one of
        # those numbers would stop reproducing. Tested, not intended.
        rng.shuffle(asked)
        linked_flags = []

    positions: list[int] = []
    transfer: list[bool] = []
    exceptional: list[bool] = []
    for entity, answer, is_transfer, is_exception in asked:
        tokens.append(QUERY)
        if config.family_links:
            # THE QUESTION MUST END ON THE PAIR THE FACT WROTE.
            #
            # `family_links` only pays with `context_keys`, where the key at a
            # position is the PAIR (previous, current) -- that is the whole
            # point, since it is what keeps `key(entity, FACT)` and
            # `key(entity, LINK)` apart. But it also changes what the QUERY
            # reads: `QUERY entity` keys on (QUERY, entity), while the fact
            # wrote (FACT, entity). Measured: cosine 0.0701 between them, so
            # the query would read an address nothing ever wrote.
            #
            # Kinship hit this first and its layout is the fix -- decision 100
            # measured the wrong version at 0.020 against 0.713. So the
            # question ends `... FACT entity`, and the pair matches.
            #
            # Only when links are on, so the link-free task keeps the layout
            # decisions 143-151 measured, byte for byte.
            tokens.append(FACT)
        positions.append(len(tokens))
        tokens.extend((entity, answer))
        transfer.append(is_transfer)
        exceptional.append(is_exception)

    # THE SET-VALUED QUESTIONS, ARCHITECTURE row F3. Last, and entirely inside the
    # conditional, so the off path makes exactly the draws decisions 143-151
    # measured -- the byte-identity rail above applies to this block too.
    #
    # `ASK_ALL entity` and nothing after it. A set has no single next token, so the
    # truth goes in `answer_sets` and the stream carries no target here. That is
    # what keeps this a read-only probe: training still learns from the stated
    # facts, and this asks what the model can be made to say afterwards.
    set_positions: list[int] = []
    answer_sets: list[frozenset[int]] = []
    if config.set_queries:
        # ONE PER FAMILY AT MOST, and drawn across DISTINCT families, because two
        # questions about one family have the same answer set -- which would let a
        # mechanism that memorised one answer score twice for it and would make the
        # mean a statistic over fewer independent items than `n` claims (rule 8).
        families_asked = rng.permutation(config.n_families)
        for family in families_asked[:config.queries_per_kind]:
            family = int(family)
            members = [config.entity_base + family * config.family_size + i
                       for i in range(config.family_size)]
            # EVERY DISTINCT VALUE STATED ABOUT THE FAMILY. The family's own value
            # is in here because at least one member agrees -- `__post_init__`
            # refuses a configuration where none does -- and each exception adds
            # its own. Unstated members contribute nothing: nothing was said about
            # them, and their family's value is already present.
            values_stated = frozenset(
                stated[entity] for entity in members if entity in stated)
            # ASKED THROUGH A MEMBER, not through a family token. There is no
            # family token in this task and adding one would hand over the
            # grouping the task exists to make discoverable -- the same reason the
            # links are stated between representative ENTITIES.
            asked_through = int(rng.choice(members))
            tokens.append(config.ask_all)
            set_positions.append(len(tokens))
            tokens.append(asked_through)
            answer_sets.append(values_stated)

    return Sequence(tuple(tokens), tuple(positions), tuple(transfer),
                    tuple(exceptional), tuple(linked_flags),
                    tuple(set_positions), tuple(answer_sets))


def dataset(config: FamilyConfig, count: int) -> list[Sequence]:
    """`count` sequences, each with its own draw of family values."""
    rng = np.random.default_rng(config.seed)
    return [generate(replace(config, seed=int(s)))
            for s in rng.integers(0, 2**31 - 1, size=count)]
