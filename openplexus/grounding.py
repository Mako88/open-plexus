"""Learning that two surfaces are one thing, by counting what turns up together.

The mechanism [`GOALS.md`](../GOALS.md) §1.2b commits to: identity between
modalities is **not computed, it is learned from co-occurrence.** A picture, a
bark and the word *dog* become one concept because they keep arriving together
while the sofa and the face do not.

This is the arithmetic half of that, and it is deliberately the whole of what is
built so far.

## What this contains, and what it does NOT contain yet

**It contains no distribution at all**, and that is a decision rather than an
omission. [`time-bucket-join.md`](../docs/options/time-bucket-join.md) designs a
transient join at `owner(time bucket)` writing links out to `owner(surface id)`;
none of it is here. The reason is that the two halves answer different questions:

    the statistic   can counting separate "always there" from "is the thing"
    the join        can two machines discover a coincidence without asking

**Only the first can refute the design cheaply**, and it refutes it completely if
it fails: distribution cannot add information to a count, so every difference the
join introduces — a boundary splitting one moment in two, a late arrival landing
in the wrong bucket, a departed node taking counts with it — can only *lose*
signal. A statistic that cannot separate the distractor here cannot separate it
spread across machines. **The converse does not hold**, so a pass here is
necessary and not sufficient, and the join is what has to be built and run in
containers before anything is claimed about C1.

## What this does NOT duplicate, and what was searched

Searched by capability — co-occurrence, counting, pair statistics, grouping,
walk, equivalence — across `openplexus/`, `tools/`, `tests/` and `experiments/`.

- **`openplexus/content.py` (`ContentIndex`) already accumulates co-occurrence,
  and the difference is the reason this file exists.** That one sums neighbour
  vectors into one superposed vector per token. A superposition **cannot hold a
  per-neighbour count** — it holds their sum — so no statistic that needs
  `count(x, y)` separately from `count(x)` is reachable from it. That is exactly
  the shape every escape from a persistent distractor takes, and
  [note 045](../docs/archive/notes/045-addresses-that-mean-something.md) named
  PPMI and subsampling as untried for that reason. **A table can hold what a
  superposition cannot**, and this file is that table. `ContentIndex` is not
  replaced and stays the representation used for similarity.
- **`openplexus/grouping.py`** turns content *vectors* into groups by spherical
  k-means. It needs vectors, needs `k`, and produces a partition of everything.
  This walks an explicit count graph and produces classes that may overlap
  nothing and leave surfaces alone — which is what *"the distractor was pruned"*
  has to look like.
- **`openplexus/search.py`** already walks, but it walks a *store* by keyed
  retrieval, committing to a token per step. There is no store here and nothing
  is retrieved; the graph is the counts themselves.
- **`openplexus/ownership.py` and `openplexus/partitioned.py`** are what the join
  would be built from and are deliberately not imported. See above.
- **`openplexus/sketch.py`** answers occupancy — whether an address was ever
  written — not how often two things met.

## Why mutual top-k rather than a threshold

An edge is kept only if each surface is in the other's top `k`. Mutuality is not
a new idea here: it is the merge gate measured on OpenEA, where **a confidence
gate made alignment worse and mutuality was what worked** (`DECISIONS.md` §10).
A one-sided rule lets a hub — which is precisely what a distractor present every
time is — attach itself to every surface in the world, since it is in everyone's
top list and nobody is in its.

**`k` is SUPPLIED, and that is generous to the mechanism on purpose.** Telling it
how large a class is hands it something the real problem does not, so a failure
under this rule is a strong refutation and a pass is a weak confirmation.
Discovering the size instead is already an open option — *bound the enumeration
by the biggest similarity gap* in `DECISIONS.md` §6 — and it is the follow-up,
not a gap in this file.

## Determinism

Neighbours are ordered by `(-score, surface)`, never by set iteration. Ties are
everywhere in raw counts, and CLAUDE.md rule 3's calibration is a published
figure that moved between runs because a `set` of strings iterated in hash order.
Same failure, one layer down, so it is closed by construction here.
"""

from __future__ import annotations

import math
from itertools import combinations
from typing import Callable, Iterable


class CoOccurrence:
    """Counts of what turned up with what, and how often each thing turned up.

    This is the durable accumulator the design puts at `owner(surface id)`, with
    the ownership left out — every surface's row is independent of every other's,
    which is the property that makes sharding it later a routing change rather
    than a redesign.

    Attributes:
        occasions: How many moments have been observed.
    """

    def __init__(self) -> None:
        self.occasions = 0
        self._seen: dict[int, int] = {}
        self._pairs: dict[int, dict[int, int]] = {}

    def observe(self, surfaces: Iterable[int]) -> None:
        """Record one moment: everything present met everything else present.

        A surface appearing twice in one moment is counted once — an occasion is
        a *set*, and double-counting would make a repeated surface look like a
        stronger partner to everything.
        """
        present = sorted(set(surfaces))
        self.moment()
        for surface in present:
            self.note(surface)
        for one, other in combinations(present, 2):
            self.pair(one, other)

    def moment(self) -> None:
        """Record that one occasion happened, whatever was in it.

        **This is the one GLOBAL quantity in the whole mechanism, and naming it
        is the point of separating it.** `ppmi` divides by it, so a node
        computing PPMI needs to know how many occasions the entire system has
        seen — which no node can know without a collective, and amended C1
        forbids collectives.

        `conditional` needs none of it: `count(x,y) / count(y)` is `owner(x)`'s
        own row plus a bounded message to `owner(y)`. Since `g32-01` measured
        the two as giving identical rankings above chance, **the C1-safe
        statistic is the one to deploy and PPMI is the one to compare against.**

        **And "one hop" is the wrong count, corrected here rather than left to
        propagate.** It is one hop *per candidate partner*, because ranking needs
        `count(y)` for every `y` under consideration — bounded per message and
        growing with a surface's partner list, which is `peer.py`'s profile
        rather than a collective's. The version needing no remote read at all is
        `local_conditional`, and it is an arm precisely so that *"the free one
        cannot work"* is a measurement rather than an argument.
        """
        self.occasions += 1

    def note(self, surface: int) -> None:
        """Record that a surface was present, without any partner.

        Split out of `observe` because a distributed join cannot hand over a
        whole moment at once — it discovers pairs one bucket at a time, and has
        to count a surface's own appearances exactly once somewhere else. Both
        halves must stay in ONE implementation or the two paths drift and every
        chance-corrected statistic silently uses a different denominator on each.
        """
        self._seen[surface] = self._seen.get(surface, 0) + 1
        self._pairs.setdefault(surface, {})

    def pair(self, one: int, other: int) -> None:
        """Record that two surfaces met, symmetrically.

        Does NOT touch either marginal — see `note`. A caller that counts a
        marginal here as well would count it once per partner.
        """
        self.observed_with(one, other)
        self.observed_with(other, one)

    def observed_with(self, surface: int, other: int) -> None:
        """Record ONE direction: this surface's row gains this partner.

        `pair` is this twice, and the split exists because **a sharded
        accumulator cannot do both halves.** When rows live at `owner(surface)`,
        the node holding `x` may write `x`'s row and must not write `y`'s — that
        row is another machine's, and touching it is the shared state C1
        forbids. A node that quietly kept both would look identical from outside
        and would be holding data it does not own.
        """
        if surface == other:
            raise ValueError(
                "a surface cannot be its own partner; counting one would make "
                "every statistic read its own presence as evidence")
        row = self._pairs.setdefault(surface, {})
        row[other] = row.get(other, 0) + 1

    def surfaces(self) -> list[int]:
        """Every surface seen at least once, in ascending order."""
        return sorted(self._seen)

    def rows(self) -> list[int]:
        """Every surface this table holds ANYTHING about, in ascending order.

        Wider than `surfaces`, and the difference matters exactly once: a
        sharded owner may be told about a pair before it is ever told the
        surface was present, so it holds a row with no marginal yet. **A
        locality check written over `surfaces` would not see that row**, and a
        row invisible to the check is the one place a node could hold data it
        does not own without anything noticing.
        """
        return sorted(set(self._seen) | set(self._pairs))

    def seen(self, surface: int) -> int:
        """How many occasions a surface was present on."""
        return self._seen.get(surface, 0)

    def together(self, one: int, other: int) -> int:
        """How many occasions two surfaces were both present on."""
        return self._pairs.get(one, {}).get(other, 0)

    def partners(self, surface: int) -> list[int]:
        """Every surface ever seen alongside this one, in ascending order."""
        return sorted(self._pairs.get(surface, {}))


#: A statistic scores a candidate neighbour `other` of `surface`. Higher is a
#: stronger claim that the two are the same thing.
Statistic = Callable[[CoOccurrence, int, int], float]


def raw_count(index: CoOccurrence, surface: int, other: int) -> float:
    """How often they met. The mechanism exactly as the option records design it.

    **This is the arm the falsifier is aimed at.** A surface present on every
    occasion meets everything more often than any real partner that is present
    only sometimes, so this ranks the distractor first by construction whenever
    `presence` is below 1.
    """
    return float(index.together(surface, other))


def frequency_weighted(index: CoOccurrence, surface: int, other: int) -> float:
    """Meetings, discounted by how common the neighbour is: `c_xy / sqrt(c_y)`.

    Note 045's fix, which sharpened some queries on Shakespeare and destroyed
    others. It is here to be measured rather than to be believed, and the
    prediction attached to it is that it over-corrects once concept frequencies
    are uneven.
    """
    common = index.seen(other)
    if common <= 0:
        return 0.0
    return index.together(surface, other) / math.sqrt(common)


def conditional(index: CoOccurrence, surface: int, other: int) -> float:
    """`P(surface | other)` — of the times the neighbour showed up, how often did this.

    A thing present every time has this at the base rate of `surface` for every
    partner, so it cannot be anyone's best neighbour unless nothing else is.
    """
    common = index.seen(other)
    if common <= 0:
        return 0.0
    return index.together(surface, other) / common


def local_conditional(index: CoOccurrence, surface: int, other: int) -> float:
    """`P(other | surface)` — the only normalisation needing NO remote read.

    **This exists to be refuted, and it is the C1 question stated as an arm.**

    `owner(x)` holds `count(x,y)` and `count(x)` and nothing else. `conditional`
    divides by `count(y)`, which lives at `owner(y)` — so ranking a surface's
    partners costs one remote read *per candidate*, not one in total. This is
    the statistic that avoids all of them.

    It should not work, and the arithmetic says why: a distractor present on
    every occasion has `P(distractor | x) = 1.0` for every `x`, while a true
    partner present only sometimes has `P(partner | x) = presence`. **So the
    purely-local direction ranks the distractor first by construction**, which is
    the failure `raw_count` has, arriving through a different door.

    Kept as a measured arm rather than an argument, because *"the local version
    cannot work"* is exactly the kind of claim this project requires a number
    for — and because if it ever does work on some stream, the remote read goes
    away and the design gets cheaper.
    """
    mine = index.seen(surface)
    if mine <= 0:
        return 0.0
    return index.together(surface, other) / mine


def ppmi(index: CoOccurrence, surface: int, other: int) -> float:
    """Positive pointwise mutual information — how much likelier than chance.

    Named as untried in note 045 and untried since. A surface present on **every**
    occasion has `P(other) = 1`, so its PMI with anything is exactly zero and it
    is pruned analytically. That is why the interesting question is not whether
    this beats the distractor — it must — but whether it survives uneven concept
    frequencies, where PMI is known to over-reward rare events.
    """
    both = index.together(surface, other)
    if both <= 0:
        return 0.0
    mine, theirs = index.seen(surface), index.seen(other)
    if mine <= 0 or theirs <= 0:
        return 0.0
    lift = (both * index.occasions) / (mine * theirs)
    if lift <= 1.0:
        return 0.0
    return math.log(lift)


#: Every statistic, by the name a sweep reports it under.
STATISTICS: dict[str, Statistic] = {
    "count": raw_count,
    "weighted": frequency_weighted,
    "conditional": conditional,
    "local": local_conditional,
    "ppmi": ppmi,
}


def cliff(scores: list[float]) -> int:
    """How many of a DESCENDING score list sit above its biggest drop.

    An argmax over consecutive gaps, so nothing is compared against a constant:
    the rule asks where a ranking falls off rather than whether a score clears a
    bar. Decision 171's mechanism, extracted here from
    `local_memory._cliff_candidates` so one implementation serves both — a fix in
    a duplicated copy is a fix that did not land, wearing the appearance of one
    that did.

    **A cliff rule needs a cliff, and note 058 measured a real one that has
    none**: language co-occurrence decays in steps of 0.02–0.03 where the
    families task falls 0.45 at once, and at no setting was that profile
    bimodal. So this is well-posed only where the ranking is genuinely bimodal,
    and *"it worked on our task"* is not evidence that it will elsewhere.

    **On an even slope the answer is decided by FLOATING POINT, which is worse
    than ill-posed.** `[0.5, 0.4, 0.3, 0.2, 0.1]` returns 2 and
    `[5.0, 4.0, 3.0, 2.0, 1.0]` returns 1 — the same ranking with the same gaps,
    because `0.5 - 0.4` and `0.4 - 0.3` differ in binary and an argmax has
    nothing else to separate them. A result taken from this rule on slope-shaped
    data would be unreproducible for a reason no seed controls.
    `test_grounding.TheCliff` asserts both, so this cannot be read as
    theoretical.

    Returns:
        At least 1, at most `len(scores)`. An empty or single-element list
        returns `len(scores)`, because there is no gap to take an argmax over
        and inventing one would be a rule about nothing.
    """
    if len(scores) < 2:
        return len(scores)
    gaps = [scores[i] - scores[i + 1] for i in range(len(scores) - 1)]
    return max(range(len(gaps)), key=gaps.__getitem__) + 1


def neighbours(index: CoOccurrence, surface: int, statistic: Statistic,
               k: int | None, look: int = 16) -> list[int]:
    """The strongest partners of a surface, best first.

    Args:
        k: How many to keep, or **`None` to derive it from the ranking itself**
            via `cliff`. A fixed `k` is one number applied to every surface, and
            `g33-02` measured what that costs: a hub needs `k` at least its own
            degree while a leaf needs 1, so on a star no single value works —
            too small and the hub cannot reach its spokes, large enough and every
            unrelated surface admits noise until the graph is one component
            holding 0.98 of everything.
        look: Ceiling on how many candidates the derived rule may consider.
            Ignored when `k` is given. **A ceiling and not a target** — being
            generous costs nothing because extra candidates fall below the cliff
            — but it must EXCEED the group, which is the one way to break it
            (decision 167 measured 0.500 at a look of 4 for a group of 6).

    Scores of zero are dropped rather than ranked: a statistic returning zero is
    saying *no evidence*, and padding a list out to `k` with things it refused
    would manufacture edges the statistic did not claim.
    """
    if k is not None and k < 1:
        raise ValueError("k must be at least 1")
    if look < 1:
        raise ValueError("look must be at least 1")
    scored = [(statistic(index, surface, other), other)
              for other in index.partners(surface)]
    scored = [(score, other) for score, other in scored if score > 0.0]
    scored.sort(key=lambda pair: (-pair[0], pair[1]))
    if k is None:
        window = scored[:look]
        keep = cliff([score for score, _ in window])
        return [other for _, other in window[:keep]]
    return [other for _, other in scored[:k]]


def equivalence_classes(index: CoOccurrence, statistic: Statistic,
                        k: int | None,
                        look: int = 16) -> dict[int, frozenset[int]]:
    """Walk the mutual-top-`k` graph; each surface's class is what it reaches.

    A concept is never stored, so this is the whole of what *"reaching a
    concept"* means: start at any surface, follow links, and the set you arrive
    at is the class. Connected components are that walk run to exhaustion.

    Returns:
        Every surface seen, mapped to the class containing it. A surface with no
        surviving edge maps to itself alone.
    """
    top = {surface: set(neighbours(index, surface, statistic, k, look))
           for surface in index.surfaces()}
    adjacency: dict[int, set[int]] = {surface: set() for surface in top}
    for surface, chosen in top.items():
        for other in chosen:
            if surface in top.get(other, ()):        # mutual, or no edge
                adjacency[surface].add(other)
                adjacency[other].add(surface)

    classes: dict[int, frozenset[int]] = {}
    for start in sorted(adjacency):
        if start in classes:
            continue
        component: set[int] = set()
        frontier = [start]
        while frontier:
            here = frontier.pop()
            if here in component:
                continue
            component.add(here)
            frontier.extend(sorted(adjacency[here] - component))
        frozen = frozenset(component)
        for member in component:
            classes[member] = frozen
    return classes


def class_f1(found: frozenset[int], correct: frozenset[int]) -> float:
    """How well one recovered class matches the true one.

    F1 rather than recall, so a class that is right but too large is penalised
    as heavily as one that is too small — otherwise a mechanism wins by
    answering *everything*.

    **The floor this implies is 0.5, not 0, and it is not obvious.** A surface
    recovered entirely alone is perfectly precise and a third recalled against a
    three-surface concept, which scores exactly 0.5. So *recovered nothing* and
    *recovered half of everything* are not far apart on this scale, and a score
    is only interpretable against the singleton floor for the concept size in
    play. `g32-01` predicted a shuffled control near zero and it came in at 0.32
    to 0.51, which is that floor plus the harm of grouping wrongly.
    """
    overlap = len(found & correct)
    if not overlap:
        return 0.0
    precision = overlap / len(found)
    recall = overlap / len(correct)
    return 2 * precision * recall / (precision + recall)


def reached_together(recovered: dict[int, frozenset[int]],
                     pairs: Iterable[tuple[int, int]]) -> float:
    """Share of the given surface pairs that ended up in one recovered class.

    **Scored over pairs chosen by the caller, and that is the whole point.**
    `score_classes` averages over every surface, so a class that is mostly right
    scores well even if the one link that had to be *inferred* was missed. This
    asks only about pairs the caller nominates — in practice, ones whose
    modalities never shared an occasion, so the only route between them is
    through something else.

    That is `GOALS.md` gate G7's question and
    `identity-without-a-global-id.md`'s central claim: a concept is reached by
    starting at any member and walking. Walking is only doing work when the
    answer was not directly observed.

    **THIS IS RECALL-SHAPED AND HAS RECALL'S FAILURE. It must never be read
    alone.** A recovery that puts every surface in the world into one class
    scores **1.0000** here, because every nominated pair is trivially together.
    That is not a hypothetical: `g33-02` produced exactly it — one class holding
    256 of 257 surfaces, reported as a perfect bridge — and the only thing that
    revealed it was `score_classes`, which fell to 0.0308 in the same cell.

    So report `largest` or `f1` beside it, always. `score_classes` uses F1 rather
    than recall for precisely this reason and this function could not, because
    the whole point is to score a nominated subset of pairs rather than a class.

    Returns:
        0.0 to 1.0. Raises if no pairs are given, because a rate over nothing
        reads as a score and is not one.
    """
    wanted = list(pairs)
    if not wanted:
        raise ValueError(
            "no pairs to score. A `complete` pairing has no modality pair that "
            "never co-occurs, so there is nothing here that a walk had to "
            "bridge -- and reporting 1.0 for that would be reporting the "
            "absence of the question as a perfect answer")
    hit = sum(1 for one, other in wanted
              if other in recovered.get(one, frozenset({one})))
    return hit / len(wanted)


def score_classes(recovered: dict[int, frozenset[int]],
                  truth: dict[int, frozenset[int]],
                  distractors: Iterable[int] = ()) -> dict[str, float]:
    """How well the walk recovered the concepts.

    Args:
        recovered: What the mechanism found, per surface.
        truth: The generator's answer, per surface.
        distractors: Surfaces that were present on every occasion.

    Returns:
        `f1` — mean over non-distractor surfaces of the F1 between the recovered
        class and the true one, so a class that is right but too large is
        penalised as well as one that is too small.

        `captured` — the share of non-distractor surfaces whose recovered class
        contains a distractor. **This is the registered falsifier, scored
        directly**: 0.0 means the distractor was pruned everywhere.

        **It cannot reach 1.0, and reading it as though it could understates the
        harm.** Mutuality caps a distractor's degree at `k` — every surface that
        points at it is one it did not point back at — so a surviving distractor
        poisons a few classes rather than all of them. What it does to the rest
        is *displacement*: it takes the top slot a true partner needed, so
        couples break without the distractor ever joining them. **`f1` is the
        quantity that sees that**, and it is the one to read first.
        `test_grounding.TheWalk` holds the worked case and the number.

        `largest` — the biggest recovered class as a share of all surfaces. It
        catches the other failure, where a statistic links so freely that
        everything chains into one component through ordinary-looking edges.
    """
    marked = frozenset(distractors)
    scored = [s for s in sorted(truth) if s not in marked]
    if not scored:
        raise ValueError(
            "every surface is a distractor, so there is no concept to recover")

    total, captured = 0.0, 0
    for surface in scored:
        found = recovered.get(surface, frozenset({surface}))
        total += class_f1(found, truth[surface])
        if found & marked:
            captured += 1

    sizes = {frozen for frozen in recovered.values()}
    biggest = max((len(frozen) for frozen in sizes), default=0)
    population = len(truth) or 1
    return {"f1": total / len(scored),
            "captured": captured / len(scored),
            "largest": biggest / population}
