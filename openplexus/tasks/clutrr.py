"""CLUTRR's relational layer, as token sequences. The first external instrument.

## IN PLAIN TERMS

Every task this project measures on, it wrote itself. That has been the standing
weakness — *"until an external benchmark runs, this project is grading its own
homework"* — and [note 058](../../docs/notes/058-real-co-occurrence-has-no-cliff.md)
put a number on what it costs.

CLUTRR is a benchmark someone else designed. Each puzzle states a chain of family
relationships and asks about a pair the chain never states directly. It trains on
chains of 2 and 3 steps and tests on chains up to 10, so it asks the same question
decision 92 answered on this project's own `chains.py`: **does a model that learned
short compositions handle long ones it never saw?**

## What is used, and what is deliberately not

CLUTRR ships each puzzle **twice**: as crowd-authored prose, and as a graph. **This
uses the graph.** The substrate addresses token ids rather than sentences, so parsing
the prose would measure a text front-end this project does not have and GOALS §2 does
not want.

**So a result here is "CLUTRR-symbolic", never "CLUTRR."** The published numbers are
on the text task and are not comparable. Said at the top because a benchmark name is
exactly the kind of borrowed authority rule 1 is about.

## The layout, and CLUTRR fits the one that already works

    FACT  subject  object  relation
    QUERY subject  object            <- the relation is the answer

That is `closure.py`'s ordering, and for `closure.py`'s reason: with `context_keys`
the store binds the previous position's key to this position's value, so
`FACT S O R` writes **`key(S, O) -> R`** and the pair key forms itself because the
object sits one token earlier. Decision 107 named that binding as the thing the
relational task needed and could not form.

**CLUTRR is natively that shape** — its question is *which relation holds between
these two people*, so the relation is the value and the entity pair is the address.
Nothing had to be bent to fit.

## Three properties of the data that the loader has to carry

**1. The graphs are not paths.** 433 of 10,220 puzzles are walks that revisit a node
— `0->1->2->1->3` — so an implementation assuming `(i, i+1)` edges would silently
mis-read 4% of them, all in the test split.

**2. `max_appearances` is computed and exposed, and it is not decoration.**
[Note 059](../../docs/notes/059-clutrr-confounds-depth-with-entity-repetition.md):
train and validation contain **zero** puzzles where an entity appears in more than two
edges; test contains **37.8%**, rising with depth. Repeated entities are this
project's measured weak point (103: 0.884 → 0.303; 104: 0.628 with pair keys), so a
single average across that split would credit *depth* for an *addressing* failure.
**The primary arm is `max_appearances == 2`.**

**3. The answer vocabulary is larger than the input vocabulary.** Six relations —
`nephew`, `niece` and four in-laws — appear only as targets, never as a stated edge.
A model here must emit relations it has never seen written down.

## Dependency-free

`openplexus/tasks/` is the ruler and takes no dependencies, so this uses `csv` and
`ast` from the standard library and returns plain tuples. The model layer may have
numpy; the thing measuring it may not.
"""

from __future__ import annotations

import ast
import collections
import csv
from dataclasses import dataclass
from pathlib import Path

#: Marks a stated fact and a question. Two ids, exactly as `closure.py`.
FACT, QUERY = 0, 1
RESERVED = 2

#: Every relation CLUTRR uses, as an EDGE or as a TARGET, fixed and sorted.
#:
#: **Fixed rather than read from the file, and that is the load-bearing choice.**
#: Deriving ids from whichever split is open would give train and test different
#: vocabularies, so a model trained on one would be reading different tokens on the
#: other — an error that produces numbers and no exception. `load` refuses a relation
#: outside this list rather than extending it, so a new configuration fails loudly
#: instead of silently remapping.
#:
#: The last six never appear as an edge; they are targets only.
RELATIONS = (
    "aunt", "brother", "daughter", "father", "granddaughter", "grandfather",
    "grandmother", "grandson", "husband", "mother", "sister", "son", "uncle",
    "wife",
    "daughter-in-law", "father-in-law", "mother-in-law", "nephew", "niece",
    "son-in-law",
)

_RELATION_ID = {name: i for i, name in enumerate(RELATIONS)}


@dataclass(frozen=True)
class ClutrrConfig:
    """Where the data is and how wide the token space has to be.

    Attributes:
        root: The directory `tools/fetch_clutrr.py` wrote, i.e. `data/clutrr`.
        config: Which CLUTRR configuration. `gen_train23_test2to10` trains on 2-3
            hops and tests on 2-10, which is the systematic-generalisation task.
        split: `train`, `validation` or `test`.
        max_entities: Entity slots reserved. 11 covers a 10-hop chain, which is the
            longest this configuration contains.
    """

    root: Path
    config: str = "gen_train23_test2to10"
    split: str = "test"
    max_entities: int = 11
    #: Which token ordering, and it decides which traversal is possible at all.
    #:
    #:     "closure"  FACT s o r  ->  key(FACT, s) -> o  and  key(s, o) -> r
    #:     "kinship"  FACT s r o  ->  key(FACT, s) -> r  and  key(s, r) -> o
    #:
    #: **Measured on this test split, and the reason `kinship` exists:**
    #:
    #:     closure collides -- an entity is SUBJECT of >1 edge      411 rows (35.9%)
    #:     kinship collides -- the same (entity, relation) twice     88 rows ( 7.7%)
    #:
    #: A 4.7x reduction, and it is decision 157's mechanism confirmed on someone
    #: else's data: typing the address separates a repeated entity's edges. 311 rows
    #: collide under `closure` and not at all under `kinship`. That matters because
    #: note 059 found the repeated-entity case is 38% of test and **absent from
    #: training**, so it is the confound most likely to be mistaken for depth.
    #:
    #: `kinship` also makes `openplexus/search.py` applicable — `walk_from` reads
    #: `key(entity, relation)` and `key(FACT, entity)`, which is this ordering — so
    #: the traversal becomes reuse of a proved mechanism (123, 125, 130) rather than
    #: new work.
    #:
    #: **What `closure` was for, and why it lost.** It puts the answer at
    #: `key(s, o)`, so a query reads it directly. That is right at ONE hop and CLUTRR
    #: tests two through ten, so the advantage was for the case the benchmark does not
    #: test. Kept under rule 14c: it is the measured comparison, and note 060's floor
    #: was taken with it.
    layout: str = "closure"

    def __post_init__(self) -> None:
        if self.split not in ("train", "validation", "test"):
            raise ValueError(
                f"split must be train, validation or test, got {self.split!r}")
        if self.layout not in ("closure", "kinship"):
            raise ValueError(
                f"layout must be 'closure' or 'kinship', got {self.layout!r}. An "
                f"unknown string would have to fall through to one of them, and a "
                f"typo would then be measured as the ordering nobody chose")
        if self.max_entities < 3:
            raise ValueError(
                "a two-hop chain needs three entities, so fewer than three "
                "entity slots cannot represent the shortest puzzle")

    @property
    def path(self) -> Path:
        return self.root / self.config / f"{self.split}.csv"

    @property
    def entity_base(self) -> int:
        return RESERVED

    @property
    def relation_base(self) -> int:
        return self.entity_base + self.max_entities

    @property
    def vocab_size(self) -> int:
        return self.relation_base + len(RELATIONS)

    @property
    def trivial(self) -> float:
        """What guessing scores: one relation out of all of them."""
        return 1.0 / len(RELATIONS)


@dataclass(frozen=True)
class Puzzle:
    """One CLUTRR puzzle as tokens, with the two axes a result must be split on.

    Attributes:
        tokens: `FACT s o r` per stated edge, then `QUERY s o`. The answer is NOT in
            the stream — a question whose answer follows it is not a question.
        query_position: Index of the query's OBJECT token, so the answer is scored at
            the position after it. Named rather than derived because
            `len(tokens) - 1` would silently follow any layout change.
        target: The relation id that answers the query.
        hops: Stated edges. The axis CLUTRR advertises.
        max_appearances: How often the most-repeated entity appears across the
            stated edges. **The axis CLUTRR does not advertise** — note 059.
    """

    tokens: tuple[int, ...]
    query_position: int
    target: int
    hops: int
    max_appearances: int


def _entity_ids(edges, query, config: ClutrrConfig) -> dict[int, int]:
    """Map this puzzle's node numbers onto entity slots, compactly.

    CLUTRR's node numbers are per-puzzle and not always contiguous, so they are
    renumbered by first appearance rather than used directly — otherwise a puzzle
    numbered `(0, 3, 7)` would reserve eight slots to hold three people.
    """
    order: dict[int, int] = {}
    for node in [n for edge in edges for n in edge] + list(query):
        if node not in order:
            order[node] = config.entity_base + len(order)
    return order


def load(config: ClutrrConfig) -> list[Puzzle]:
    """Every puzzle in the split, as token sequences.

    Raises if the file is missing or names a relation outside `RELATIONS`, because
    both would otherwise produce a full set of numbers about the wrong thing.
    """
    if not config.path.exists():
        raise FileNotFoundError(
            f"{config.path} is not there. CLUTRR is fetched rather than committed: "
            f"run `python tools/fetch_clutrr.py`, which pins the URLs and verifies "
            f"size and sha256")
    puzzles: list[Puzzle] = []
    with config.path.open(encoding="utf-8", newline="") as handle:
        for row in csv.DictReader(handle):
            edges = ast.literal_eval(row["story_edges"])
            types = ast.literal_eval(row["edge_types"])
            query = ast.literal_eval(row["query_edge"])
            target = row["target_text"]
            if len(edges) != len(types):
                raise ValueError(
                    f"{len(edges)} edges but {len(types)} edge types in row "
                    f"{row.get('id')!r}: the graph and its labels disagree, and "
                    f"pairing them by position would mislabel every edge after "
                    f"the mismatch")
            unknown = [name for name in list(types) + [target]
                       if name not in _RELATION_ID]
            if unknown:
                raise ValueError(
                    f"relations not in RELATIONS: {sorted(set(unknown))}. The "
                    f"vocabulary is fixed so train and test share token ids; "
                    f"extending it here would give two splits different ids and "
                    f"produce numbers about different tokens")
            ids = _entity_ids(edges, query, config)
            if len(ids) > config.max_entities:
                raise ValueError(
                    f"puzzle needs {len(ids)} entity slots, config reserves "
                    f"{config.max_entities}")
            tokens: list[int] = []
            for (left, right), name in zip(edges, types):
                relation = config.relation_base + _RELATION_ID[name]
                if config.layout == "kinship":
                    # FACT s r o. key(FACT, s) -> r and key(s, r) -> o, so a repeated
                    # entity's edges land at DIFFERENT addresses when their relations
                    # differ -- 35.9% of test rows collide under `closure` and 7.7%
                    # here.
                    tokens.extend((FACT, ids[left], relation, ids[right]))
                else:
                    tokens.extend((FACT, ids[left], ids[right], relation))
            tokens.append(QUERY)
            tokens.append(ids[query[0]])
            position = len(tokens)
            tokens.append(ids[query[1]])
            appearances = collections.Counter(n for edge in edges for n in edge)
            puzzles.append(Puzzle(
                tokens=tuple(tokens),
                query_position=position,
                target=config.relation_base + _RELATION_ID[target],
                hops=len(edges),
                max_appearances=max(appearances.values()),
            ))
    return puzzles


def by_repetition(puzzles: list[Puzzle], repeated: bool) -> list[Puzzle]:
    """Split on note 059's confound.

    `repeated=False` is the primary arm: entities appear in at most two edges, which
    is the only condition training ever presents. `repeated=True` is the subset where
    a poor score is evidence about ADDRESSING rather than about composition.
    """
    return [p for p in puzzles
            if (p.max_appearances > 2) == repeated]
