"""Typed relations that COMPOSE BY RULE, modelled on CLUTRR.

[CLUTRR](https://arxiv.org/abs/1908.06177) (Sinha et al., 2019) is the design
this borrows: a family graph is described, a relation between two people is
asked for, and the model must compose the stated relations to answer. Its
protocol is **train on short chains, test on longer** — systematic
generalisation is the measurement, not accuracy at a fixed length.

## What this adds that `chains.py` cannot test

The chain task is **pure transitive chaining**. `a -> b -> c` means the answer
is `c`, and following an edge is the whole operation — there is only one
relation and composing it with itself is the identity. Decision 92 measured the
hop mechanism generalising to unseen depths zero-shot, which is real, but it is
generalisation over *how many times to do the same thing*.

Kinship is not like that. `mother` of `brother` is `mother`; `mother` of
`mother` is `grandmother`; `brother` of `mother` is `uncle`. **Composition is a
lookup in a table the model must learn**, not a repetition of one operation. Two
paths of the same length compose to different relations, and the same relation
is reachable by paths of different lengths.

That is the capability gap: a model can be perfect at "follow the arrow k times"
and have no way to represent "these two relation types combine into that third
one".

## What is faithful and what is not

**Faithful:** typed relations, composition by rule, answers that no single
stated fact gives, and the train-short/test-long protocol.

**Not faithful, and not claimed:** this is not CLUTRR's dataset and not its
exact rule set. CLUTRR states its facts in natural language generated from
templates and uses a larger kinship inventory. This states them symbolically and
uses the closure of the rules below. **Calling a number here a "CLUTRR score"
would be wrong** — it is this task, whose design is borrowed.

The rules are the ordinary ones and are checked for consistency by
`tests/test_kinship.py` rather than trusted: every composition it generates is
verified against an independently computed ground truth built from the family
tree, so a wrong rule is a failing test rather than a silently easier task.
"""

from __future__ import annotations

import random
from dataclasses import dataclass

#: Positions that are not questions carry no target.
IGNORE = -1

#: The relations a fact can state. **Gender-free on purpose.**
#:
#: The first version of this used gendered names -- mother, father, son,
#: daughter, and so on, sixteen of them. That was the cause of the prefix
#: shortcut measured in decision 99: `mother` and `father` compose IDENTICALLY
#: (both give a grandparent), so gender multiplied the inventory without adding
#: any compositional structure, and every first relation was left with exactly
#: two reachable answers. Guessing from the prefix alone was worth 0.546.
#:
#: Keeping gender orthogonal is CLUTRR's design (Sinha et al., 2019,
#: https://github.com/facebookresearch/clutrr) and it is the fix: `child` now
#: reaches five different answers depending on what follows it. Gender, if it is
#: ever wanted, belongs as a separate attribute of a person rather than as part
#: of a relation's name.
#:
#: `inv-` is the inverse direction: `child` is "X is a child of Y", `inv-child`
#: is "X is a parent of Y". Naming the inverse rather than adding `parent`,
#: `grandparent` and so on keeps the table's symmetry visible.
RELATIONS = (
    "child", "inv-child",
    "SO", "sibling",
    "grand", "inv-grand",
    "un", "inv-un",
    "in-law", "inv-in-law",
)

#: `COMPOSE[(a, b)]` is the relation that holds when X is `a` of Y and Y is `b`
#: of Z. Read left to right: `("mother", "mother")` is X is mother of Y, Y is
#: mother of Z, so X is grandmother of Z.
#:
#: Deliberately PARTIAL, and CLUTRR reached the same conclusion independently.
#: Many pairs compose to something ambiguous, and inventing an answer for those
#: would be inventing a rule the model is then scored on. A pair with no entry
#: is never generated.
#:
#: Their `rules_store.yaml` carries a commented-out rule with the reasoning:
#: `grand` then `inv-child` is not `child`, because the person reached could be
#: an in-law. That is the same argument, arrived at from the same problem, and
#: it is worth recording that a partial table is the considered position rather
#: than an unfinished one.
COMPOSE: dict[tuple[str, str], str] = {
    ("child", "child"): "grand",
    ("child", "SO"): "in-law",
    ("child", "sibling"): "child",
    ("child", "inv-un"): "sibling",
    ("child", "inv-grand"): "inv-child",
    ("inv-child", "child"): "sibling",
    ("inv-child", "inv-child"): "inv-grand",
    ("inv-child", "sibling"): "inv-un",
    ("SO", "inv-child"): "inv-in-law",
    ("SO", "grand"): "grand",
    ("SO", "child"): "child",
    ("sibling", "sibling"): "sibling",
    ("sibling", "inv-grand"): "inv-grand",
    ("sibling", "child"): "un",
    ("sibling", "inv-child"): "inv-child",
    ("grand", "sibling"): "grand",
}

#: Relations that are their own inverse, and pairs that invert each other. Not
#: used by the generator -- recorded because the table above is only readable
#: alongside them, and because a future rule can be checked against them.
SYMMETRIC = ("sibling", "SO")
INVERSES = (
    ("child", "inv-child"),
    ("grand", "inv-grand"),
    ("un", "inv-un"),
    ("in-law", "inv-in-law"),
)


def compose(path: tuple[str, ...]) -> str | None:
    """Fold a path of relations left to right, or None if it does not compose.

    Returns None rather than raising: a path that leaves the inventory is not an
    error, it is a path this task declines to ask about.
    """
    if not path:
        return None
    current = path[0]
    for step in path[1:]:
        pair = COMPOSE.get((current, step))
        if pair is None:
            return None
        current = pair
    return current


@dataclass(frozen=True)
class KinshipConfig:
    """Shape of a kinship-composition task.

    Attributes:
        n_people: Distinct people named in a sequence.
        hops: Facts that must be composed to answer. **2 is the shortest that
            needs a rule at all** — one hop is a stated fact and measures
            recall, exactly as it does in `chains.py`.
        n_facts: Total facts stated, including the ones the question needs.
            Extra facts are distractors drawn from the same generator, so they
            are indistinguishable from relevant ones without doing the work.
        seq_len: Total length including the question.
        seed: Generator seed.
    """

    n_people: int = 12
    hops: int = 2
    n_facts: int = 10
    seq_len: int = 96
    seed: int = 0

    @property
    def n_relations(self) -> int:
        return len(RELATIONS)

    @property
    def query_token(self) -> int:
        """Marks the question. Outside both the person and relation ranges."""
        return self.n_people + self.n_relations

    @property
    def vocab_size(self) -> int:
        # People, then relations, then the query marker.
        return self.n_people + self.n_relations + 1

    def relation_token(self, name: str) -> int:
        return self.n_people + RELATIONS.index(name)

    @property
    def uniform_floor(self) -> float:
        """Guessing uniformly among RELATIONS. **This is NOT the floor to beat.**

        It is a weak lower bound, kept only because it is the number people
        reach for. `majority_floor` is the strategy a result actually has to
        clear, and it is higher at every depth — see the warning there.
        """
        return 1.0 / len(RELATIONS)

    def __post_init__(self) -> None:
        if self.hops < 1:
            raise ValueError("hops is a number of facts and must be at least 1")
        if self.n_facts < self.hops:
            raise ValueError(
                f"{self.hops} facts are needed to answer and only "
                f"{self.n_facts} are stated")
        if self.n_people < self.hops + 1:
            raise ValueError(
                f"a {self.hops}-hop path visits {self.hops + 1} people and only "
                f"{self.n_people} exist")
        if self.seq_len < self.min_seq_len:
            raise ValueError(
                f"seq_len {self.seq_len} cannot hold {self.min_seq_len} "
                f"positions of facts and question")

    @property
    def min_seq_len(self) -> int:
        """Three positions per fact, then the query marker, two people and the
        answer."""
        return self.n_facts * 3 + 4


@dataclass(frozen=True)
class KinshipSequence:
    """One generated example.

    Attributes:
        tokens: The input sequence.
        targets: The answer at the answer position, `IGNORE` everywhere else.
        facts: Every stated fact as `(subject, relation, object)`, for tests.
        path: The relations composed to answer, in order.
        asked: `(subject, object)` the question is about.
        answer: The composed relation.
        answer_position: Where the answer is emitted.
    """

    tokens: tuple[int, ...]
    targets: tuple[int, ...]
    facts: tuple[tuple[int, str, int], ...]
    path: tuple[str, ...]
    asked: tuple[int, int]
    answer: str
    answer_position: int


#: For each relation, the steps that can legally follow it. Built once from
#: `COMPOSE` so a path is CONSTRUCTED rather than guessed at.
_NEXT: dict[str, tuple[str, ...]] = {
    relation: tuple(sorted({b for (a, b) in COMPOSE if a == relation}))
    for relation in RELATIONS
}


def paths_of_length(hops: int) -> dict[str, tuple[tuple[str, ...], ...]]:
    """Every composable path of `hops` steps, grouped by what it composes to.

    Enumerated rather than sampled. The table is small — 16 rules — so the whole
    set is cheap at the depths this task uses, and having it in hand is what
    makes balanced sampling possible.
    """
    reached: dict[str, list[tuple[str, ...]]] = {}
    frontier: list[tuple[tuple[str, ...], str]] = [
        ((relation,), relation) for relation in RELATIONS]
    for _ in range(hops - 1):
        nxt = []
        for path, current in frontier:
            for step in _NEXT.get(current, ()):
                nxt.append((path + (step,), COMPOSE[(current, step)]))
        frontier = nxt
    for path, answer in frontier:
        reached.setdefault(answer, []).append(path)
    return {answer: tuple(paths) for answer, paths in reached.items()}


def _composable_path(rng: random.Random, hops: int,
                     tries: int = 200) -> tuple[str, ...] | None:
    """A relation path of `hops` steps that composes to something named.

    **Constructed by walking `COMPOSE`, not sampled and filtered.** Only about
    24 of the 256 relation pairs compose, so a random three-step path almost
    never survives -- rejection sampling raised "no 3-hop path composes" on an
    ordinary seed, which is the generator failing rather than the depth being
    impossible. Each step is drawn from the relations that can legally follow
    the one composed so far, so a path of any reachable length is found on the
    first attempt.

    `tries` remains because a walk can still dead-end: a relation whose
    composed result has no legal successor ends the path early, and restarting
    is simpler than backtracking.
    """
    for _ in range(tries):
        path = [rng.choice(RELATIONS)]
        current = path[0]
        while len(path) < hops:
            options = _NEXT.get(current, ())
            if not options:
                break
            step = rng.choice(options)
            path.append(step)
            current = COMPOSE[(current, step)]
        if len(path) == hops:
            return tuple(path)
    return None


def _disagreeing_route(facts, source: int, target: int, depth: int,
                       answer: str) -> bool:
    """Is there a path from `source` to `target` composing to anything else?

    Paths that compose to `answer` are fine however many there are — a real
    family graph has several routes between two people and they agree. What
    breaks the task is a route that composes to a DIFFERENT relation, because
    then no answer is determined.
    """
    edges: dict[int, list[tuple[str, int]]] = {}
    for subject, relation, obj in facts:
        edges.setdefault(subject, []).append((relation, obj))

    stack = [(source, ())]
    while stack:
        node, so_far = stack.pop()
        if node == target and so_far:
            reached = compose(so_far)
            if reached is not None and reached != answer:
                return True
        if len(so_far) >= depth:
            continue
        for relation, nxt in edges.get(node, ()):
            stack.append((nxt, so_far + (relation,)))
    return False


def generate(config: KinshipConfig) -> KinshipSequence:
    """One kinship-composition example.

    The facts of the asked path are stated in SHUFFLED order among distractors,
    so the path cannot be read off by position — following it requires the
    relations, which is the point.
    """
    rng = random.Random(config.seed)

    # BALANCED ON THE ANSWER, not on the path.
    #
    # Walking the table and taking whatever answer falls out skews the answer
    # distribution hard, because some relations are reachable by many more paths
    # than others. Measured that way: the majority answer was 0.433 of all
    # three-hop questions, and guessing from the LAST relation alone was worth
    # 0.708. Both are shortcuts that need no composition at all.
    #
    # Choosing the answer uniformly and then a path that reaches it makes the
    # majority floor 1/(reachable answers) by construction, and weakens every
    # conditional shortcut with it -- knowing one relation of the path no longer
    # tells you which answer is a priori likely, because none of them is.
    by_answer = paths_of_length(config.hops)
    if not by_answer:
        raise ValueError(
            f"no {config.hops}-hop path composes; the rule table cannot reach "
            f"that depth")
    answer = rng.choice(sorted(by_answer))
    path = rng.choice(by_answer[answer])

    # The chain of people the path runs along. X is path[0] of p1, p1 is path[1]
    # of p2, and so on -- so the question is about (p0, p_last).
    people = rng.sample(range(config.n_people), config.hops + 1)
    facts = [(people[i], path[i], people[i + 1]) for i in range(config.hops)]
    needed = set(facts)

    # DISTRACTORS from the same generator, so they are indistinguishable from
    # relevant facts without doing the work. A distractor that restated a needed
    # pair with a different relation would make the answer ambiguous, and one
    # that duplicated a needed fact would be free, so both are rejected.
    stated_pairs = {(subject, obj) for subject, _, obj in facts}
    # THE ASKED PAIR IS OFF LIMITS, IN BOTH DIRECTIONS. Found by generating
    # sequences and reading them: 7.0% stated the asked pair directly as a
    # distractor. One in three hundred stated the ANSWER outright -- free -- and
    # the rest stated something that CONTRADICTED the composed answer, which
    # makes the task inconsistent rather than merely easier. The reverse
    # direction is excluded too: "4 is uncle of 9" answers "9 is ? of 4" for
    # anyone who knows the inverse, which is a rule this task is supposed to be
    # testing rather than handing over.
    stated_pairs.add((people[0], people[-1]))
    stated_pairs.add((people[-1], people[0]))
    guard = 0
    while len(facts) < config.n_facts and guard < 1000:
        guard += 1
        subject, obj = rng.sample(range(config.n_people), 2)
        if (subject, obj) in stated_pairs:
            continue
        stated_pairs.add((subject, obj))
        facts.append((subject, rng.choice(RELATIONS), obj))

    # NO SECOND ANSWER. Distractors can accidentally connect the asked pair by
    # another route, and if that route composes to a different relation the
    # question has no determined answer -- unanswerable, which is the failure
    # direction that looks like "the model is bad" rather than "the task is
    # broken". Measured before this guard: 1.0% of 3-hop sequences.
    #
    # Extra routes that AGREE are kept. They are not a defect -- a real family
    # graph has them, and they still require composing something.
    if _disagreeing_route(facts, people[0], people[-1], config.hops, answer):
        facts = facts[:config.hops]
        guard = 0
        while len(facts) < config.n_facts and guard < 2000:
            guard += 1
            subject, obj = rng.sample(range(config.n_people), 2)
            if (subject, obj) in stated_pairs:
                continue
            candidate = (subject, rng.choice(RELATIONS), obj)
            if _disagreeing_route(facts + [candidate], people[0], people[-1],
                                  config.hops, answer):
                continue
            stated_pairs.add((subject, obj))
            facts.append(candidate)

    order = list(facts)
    rng.shuffle(order)

    tokens: list[int] = []
    for subject, relation, obj in order:
        tokens.extend((subject, config.relation_token(relation), obj))

    tokens.extend((config.query_token, people[0], people[-1]))
    answer_position = len(tokens) - 1
    tokens.append(config.relation_token(answer))

    targets = [IGNORE] * len(tokens)
    targets[answer_position] = config.relation_token(answer)
    return KinshipSequence(
        tuple(tokens), tuple(targets), tuple(order), path,
        (people[0], people[-1]), answer, answer_position)


def dataset(config: KinshipConfig, n_sequences: int) -> list[KinshipSequence]:
    """`n_sequences` examples, each with its own seed."""
    from dataclasses import replace
    return [generate(replace(config, seed=config.seed + i))
            for i in range(n_sequences)]


def majority_floor(config: KinshipConfig, n_sequences: int = 600) -> float:
    """The score of always answering the most common relation. **Measured.**

    ## Why this is not a constant

    Composition is not uniform over relations, and it gets less uniform with
    depth because fewer relations are reachable by longer paths. Measured:

        hops 1   16 of 16 relations occur   most common 0.080
        hops 2   12 of 16                   most common 0.108
        hops 3    8 of 16                   most common 0.150

    So the uniform 1/16 = 0.062 flatters every result, by more than twice at
    three hops. A depth-dependent floor cannot be a constant on the config, and
    assuming one is exactly what withdrew g8-01's seq-1536 row.

    Sampled rather than derived from the rule table because the distribution
    depends on the generator's rejection behaviour too, not only on `COMPOSE` —
    and the number that matters is the one the model actually faces.
    """
    from collections import Counter
    answers = Counter(s.answer for s in dataset(config, n_sequences))
    return max(answers.values()) / sum(answers.values())


def shortcut_floor(config: KinshipConfig, n_sequences: int = 900) -> float:
    """The score of guessing from the FIRST relation alone. **This is the floor.**

    ## The defect this exists for, and it is in the rule table

    A model that cannot compose scored 0.407 on the two-hop task against a
    0.130 majority floor. It should have been near the floor. Counting explains
    it completely:

        hops 2   majority floor                        0.130
                 best guess from the FIRST relation    0.546
                 distinct answers per first relation   2, 2, 2, 2, 2, 2

    **Every first relation admits exactly two answers.** `mother` of anything in
    this table is `grandmother` or `mother`; nothing else is reachable. So the
    second relation barely matters and half the task can be had for free.

    That is a property of `COMPOSE` being small and regular — 16 relations and
    24 rules — where CLUTRR's larger inventory makes the same shortcut weak. It
    is a real limit on how much this instrument can show, and the honest
    response is to raise the floor rather than to quietly keep the low one:
    **a composition claim here must beat this number, not `majority_floor`.**

    Conditioning on the LAST relation is also computed and is much weaker
    (0.290), which is worth knowing: the leak is specifically that paths are
    prefix-determined, not that the answer is generally predictable.
    """
    floors = shortcut_floors(config, n_sequences)
    # ACHIEVABLE ones only. `last` and `ends` need the far end of the path,
    # which is the work this task is asking for -- see `shortcut_floors`.
    return max(floors["majority"], floors["first"])


def shortcut_floors(config: KinshipConfig,
                    n_sequences: int = 900) -> dict[str, float]:
    """Score of guessing from each part of the path.

    ## Only two of these are FLOORS. The others are information bounds.

    A floor is what a model can reach *without composing*. The path is **not
    observable** — the model sees facts and two people, and would have to search
    the graph to learn any of the path's relations. So:

    - `majority` is a floor: it needs nothing at all.
    - `first` is a floor: retrieving the relation stated for the queried subject
      gives `path[0]` directly, which is exactly what a one-hop model does. It
      predicted 0.465 at two hops and the model scored 0.407.
    - `last` is **not** a floor. Reaching the far end of the path is the work.
    - `ends` is **not** a floor, and at two hops it is 1.000 by definition —
      the path *is* its two ends there, so that number is a tautology rather
      than a leak.

    `shortcut_floor` returns the max of the achievable ones only. The bounds are
    reported alongside because they say how much of the answer lives in the ends
    versus the middle — at three hops `ends` is 0.724 and at four 0.629, so the
    middle carries real information and the depth axis is worth having.

    The first version of this treated all four as floors, which would have set
    an impossible bar and made any honest result look like a failure.
    """
    from collections import Counter, defaultdict
    data = dataset(config, n_sequences)
    tables: dict[str, dict] = {
        "majority": defaultdict(Counter),
        "first": defaultdict(Counter),
        "last": defaultdict(Counter),
        "ends": defaultdict(Counter),
    }
    for sequence in data:
        path, answer = sequence.path, sequence.answer
        tables["majority"][()][answer] += 1
        tables["first"][path[0]][answer] += 1
        tables["last"][path[-1]][answer] += 1
        tables["ends"][(path[0], path[-1])][answer] += 1
    return {
        name: sum(counts.most_common(1)[0][1]
                  for counts in table.values()) / len(data)
        for name, table in tables.items()
    }
