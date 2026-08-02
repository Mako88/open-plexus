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

## Why mutual top-k rather than a threshold

An edge is kept only if each surface is in the other's top `k`. Mutuality is not
a new idea here: it is the merge gate measured on OpenEA, where **a confidence
gate made alignment worse and mutuality was what worked** (README decision 10).
A one-sided rule lets a hub — which is precisely what a distractor present every
time is — attach itself to every surface in the world, since it is in everyone's
top list and nobody is in its.

**`k` is SUPPLIED, and that is generous to the mechanism on purpose.** Telling it
how large a class is hands it something the real problem does not, so a failure
under this rule is a strong refutation and a pass is a weak confirmation.
Discovering the size instead is already an open option — *bound the enumeration
by the biggest similarity gap* in README decision 2 — and it is the follow-up,
not a gap in this file.

## Determinism

Neighbours are ordered by `(-score, surface)`, never by set iteration. Ties are
everywhere in raw counts, and CLAUDE.md rule 3's calibration is a published
figure that moved between runs because a `set` of strings iterated in hash order.
Same failure, one layer down, so it is closed by construction here.
"""

from __future__ import annotations

from openplexus import wiring

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
        # ONE INSTANCE, COUNTED. `wiring.expect(graph=1)` is how a run states
        # that it uses a single shared graph, and three separate ones existed
        # here for as long as anyone can tell because nobody counted.
        wiring.touch("graph")
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


def damped(alpha: float) -> Statistic:
    """`c_xy / c_y**alpha` — the family the other statistics are points on.

    **John's question, 2026-07-31**, in his words: *"I suspect the reason the
    words are disappearing from the connection is the difference in volume —
    LOTS of images and LOTS of audio, but only 10 words. I'm wondering if we need
    some kind of scaling so connections matter more than frequency of a thing."*

    His diagnosis is right and is measured: in `g36-04`'s three-modality stream a
    word is present **845.4** times on average against **60.0** for any single
    image or audio code, because ten words carry the occasions that fifty codes
    split. `conditional` divides by the candidate's own count, so a word takes a
    fourteen-fold handicap for being shared across fewer types.

    This exposes the exponent so the question can be asked as one axis instead of
    as a choice between named statistics:

        alpha = 0.0    identical ranking to `raw_count`
        alpha = 0.5    identical to `frequency_weighted`
        alpha = 1.0    identical to `conditional`

    **The tension it exists to resolve is real and may have no solution.**
    `g32-01` measured that `alpha = 1` is what kills a distractor present on
    every occasion — raw counting loses 0.3044 of f1 to one and `conditional`
    loses 0.0000. `g36-05` measured that `alpha = 1` is also what evicts the
    word, which survives the bound for 0.0200 of image codes. Those pull in
    opposite directions, so an intermediate value either does both or neither,
    and nothing here assumes which.

    **This is a NEW MECHANISM and defaults to off**: it is not in `STATISTICS`,
    so no existing result changes and the comparison against not having it is
    free. Callers construct it explicitly.

    Args:
        alpha: The exponent on the neighbour's own count. Must not be negative —
            a negative exponent would REWARD a common neighbour, which is the
            failure every statistic here exists to avoid, and admitting it
            silently would produce a plausible number for an incoherent rule.

    Returns:
        A `Statistic`. It costs the same one remote read per candidate that
        `conditional` does for any `alpha > 0`, and none at `alpha == 0`.
    """
    if alpha < 0.0:
        raise ValueError(
            f"alpha must not be negative; {alpha} would score a MORE common "
            f"neighbour higher, which inverts the correction this family exists "
            f"to apply")

    def score(index: CoOccurrence, surface: int, other: int) -> float:
        if alpha == 0.0:
            return float(index.together(surface, other))
        common = index.seen(other)
        if common <= 0:
            return 0.0
        return index.together(surface, other) / (common ** alpha)

    return score


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


#: How the two directional scores of an edge are combined into one weight.
#:
#: **`forward` is the odd one and it is the one that works.** It discards the
#: backward direction entirely, so it is NOT symmetric — `strength(x, y)` and
#: `strength(y, x)` differ — and it is the only entry that keeps the link while
#: refusing an ever-present distractor (`g39-04`).
#:
#: The reason is arithmetic. For a word `w`, its own image code `c`, and a
#: distractor `d` present on every occasion:
#:
#:     conditional(w, c) ~ 1.00     conditional(c, w) ~ 0.07
#:     conditional(w, d) ~ 0.28     conditional(d, w) ~ 1.00
#:
#: The FORWARD view separates them cleanly — 1.00 against 0.28. Every
#: symmetrising rule mixes in the backward direction, where the distractor's
#: 1.00 is genuinely true, and inverts the order: `min` gives 0.07 against 0.28,
#: `mean` gives 0.53 against 0.64, `max` ties them at 1.00.
#:
#: **So symmetrising is what admitted the distractor**, and five sweeps looking
#: for a statistic that refuses it were looking on the wrong axis.
COMBINERS: dict[str, Callable[[float, float], float]] = {
    "min": min,
    "max": max,
    "mean": lambda a, b: (a + b) / 2.0,
    "geometric": lambda a, b: math.sqrt(a * b),
    "forward": lambda a, b: a,
}

#: The combiners that genuinely produce a symmetric weight. `forward` does not,
#: deliberately, and separating the two here means a caller relying on symmetry
#: can assert it rather than assume it.
SYMMETRIC = ("min", "max", "mean", "geometric")


def strength(index: CoOccurrence, statistic: Statistic,
             one: int, other: int, combine: str = "min") -> float:
    """An edge weight from the two directional scores. **Symmetric unless
    `combine` is `forward`.**

    **This generalises the rule `equivalence_classes` uses, without the hard
    cut.** There, an edge survives only if each surface is in the other's
    top-`k`; here every edge survives and carries how strongly both ends agree.

    **WHICH COMBINER IS RIGHT IS AN OPEN QUESTION AND THE DEFAULT IS NOT A
    FINDING.** `min` is the conservative choice — an edge is only as strong as
    its weaker direction, which is the soft analogue of mutuality, since a
    mutual rule also refuses an edge one side does not want.

    **And there is a specific reason to doubt it**, recorded here rather than
    discovered later. The directional scores of a HUB edge are lopsided: for a
    word `w` naming an image code `c`, `conditional(w, c)` is near 1.0 because
    `c` appears almost only with `w`, while `conditional(c, w)` is small because
    `w` is common. **`min` takes the small one**, so it weakens exactly the
    hub-to-spoke edges that `g36-05` found being evicted. `max` keeps them — and
    also keeps an ever-present distractor, whose backward direction is likewise
    1.0. So the two failure modes sit at opposite ends of this choice, which is
    the same shape as `damped`'s exponent and was resolved there by measuring.

    A first version of this docstring justified `min` by claiming a mean would
    rank a distractor above a real partner. **That was an unchecked assertion and
    a test refuted it on the first run** — on the natural fixture the mean ranks
    them correctly too. The claim is removed rather than softened.

    **AND THE ANSWER TURNED OUT TO BE NEITHER: it is `forward`, which does not
    combine at all.** `g39-03` measured every symmetrising rule admitting the
    distractor for every word, at three exponents and two stream lengths — 0 of
    24 — and `g39-04` measured `forward` refusing it at 0.0000 while keeping the
    link at 0.9800 and full coverage. See `COMBINERS`.

    **So the doubt recorded here about hub edges was right about `min` and wrong
    about the remedy.** It predicted the answer lay somewhere along the
    min-to-max axis. It lies off that axis entirely, at the point where the
    backward direction is discarded rather than weighed.

    Args:
        statistic: Any `Statistic`. Both directions are evaluated, so a
            statistic costing one remote read costs two here.
        combine: A key of `COMBINERS`. Unknown keys are refused rather than
            defaulted, because silently falling back would make a sweep arm that
            looks distinct and is not.
    """
    if combine not in COMBINERS:
        raise ValueError(
            f"unknown combiner {combine!r}; expected one of "
            f"{sorted(COMBINERS)}. Defaulting here would give a sweep an arm "
            f"that looks distinct and is not")
    return COMBINERS[combine](statistic(index, one, other),
                              statistic(index, other, one))


def reach(index: CoOccurrence, statistic: Statistic, start: int, *,
          beam: int = 8, depth: int = 3, floor: float = 0.0,
          combine: str = "min") -> dict[int, float]:
    """Best-first weighted walk from one surface. Everything reached, ranked.

    **The alternative to `equivalence_classes`, and the difference is where the
    budget sits.** That function bounds the REPRESENTATION — each surface keeps
    a few partners and the rest are discarded before anything is asked. This
    bounds the SEARCH instead: every edge in the table stays, and `beam` and
    `depth` limit how far one question may travel.

    John's instruction, 2026-07-31: *"I don't think we want a ceiling at all"*,
    with the reasoning that a concept IS a web of connections and traversal is
    therefore intrinsic rather than a cost to be minimised away. This is that,
    made concrete — and the distinction it turns on is that an unbounded
    representation with a bounded search is affordable, while an unbounded
    search over an unbounded representation is `O(N**depth)` and is not.

    **Why this is not simply better and must be measured.** `equivalence_classes`
    returns a partition, so two surfaces are in one concept or they are not.
    This returns a RANKING, which answers a different question and needs a
    different scorer. It also cannot collapse — there is no component to merge —
    so a metric that reads well under collapse will read well here for a reason
    having nothing to do with the mechanism working.

    Path strength MULTIPLIES along the path, so a long weak route loses to a
    short strong one without any explicit depth penalty. Scores above 1 would
    invert that; `conditional` and `damped(1.0)` are bounded by 1, `raw_count`
    is not, and passing an unbounded statistic here is a caller error the
    docstring names rather than the code guessing at a normalisation.

    Args:
        beam: How many of a surface's strongest partners to expand at each step.
            **A search budget, not a representation budget** — raising it costs
            time and changes no stored value.
        depth: How many hops from `start`.
        floor: Paths weaker than this are not expanded. `0.0` expands
            everything the beam admits.

    Returns:
        Every surface reached, mapped to its BEST path strength, excluding
        `start` itself. Empty when nothing clears the floor.
    """
    return {surface: strength_reached
            for surface, (strength_reached, _) in routed(
                index, statistic, start, beam=beam, depth=depth, floor=floor,
                combine=combine).items()}


def routed(index: CoOccurrence, statistic: Statistic, start: int, *,
           beam: int = 8, depth: int = 3, floor: float = 0.0,
           combine: str = "min") -> dict[int, tuple[float, tuple[int, ...]]]:
    """`reach`, keeping the ROUTE that got there as well as how strong it was.

    **`reach` computed this and threw it away**, which is a strange thing for a
    project whose position is that a concept is not stored but is what you reach
    by walking. If identity is a traversal then the traversal is the object, and
    a caller that has only the endpoint has the summary rather than the thing.

    Two callers need it concretely rather than philosophically. An answer's route
    is its explanation — *why* these two entities are related, not merely that
    the walk got from one to the other. And a walk that is to be told which
    relation types it passed through has to know which edges it used.

    `reach` delegates here and drops the second half, so there is one walk and
    the ranking cannot drift between them.

    Returns:
        Every surface reached, mapped to `(best path strength, route)`. The
        route lists the surfaces AFTER `start` in the order they were traversed,
        so its length is the number of hops and `start` is not repeated in it.
    """
    if beam < 1:
        raise ValueError("beam must be at least 1")
    if depth < 1:
        raise ValueError("depth must be at least 1")

    best: dict[int, tuple[float, tuple[int, ...]]] = {}
    frontier: list[tuple[int, float, tuple[int, ...]]] = [(start, 1.0, ())]
    for _ in range(depth):
        following: list[tuple[int, float, tuple[int, ...]]] = []
        for here, carried, route in frontier:
            scored = sorted(
                ((strength(index, statistic, here, other, combine), other)
                 for other in index.partners(here)),
                key=lambda pair: (-pair[0], pair[1]))
            for score, other in scored[:beam]:
                if score <= 0.0 or other == start:
                    continue
                travelled = carried * score
                if travelled <= floor or travelled <= best.get(
                        other, (0.0, ()))[0]:
                    continue
                best[other] = (travelled, route + (other,))
                following.append((other, travelled, route + (other,)))
        if not following:
            break
        frontier = following
    return best


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


def partner_rate(recovered: dict[int, frozenset[int]],
                 truth: dict[int, frozenset[int]],
                 among: Iterable[int] | None = None) -> float:
    """Share of surfaces whose class still holds at least one TRUE partner.

    **Floor-free, which is the entire reason it exists.** `class_f1`'s floor
    moves with the class size — a concept recovered alone scores 0.6667 at two
    surfaces, 0.5000 at three and 0.3333 at five — so an f1 column compared
    across a surface-count axis is three scales printed as one. `g35-02` read
    exactly that column as flat and could not say whether the flatness meant
    anything.

    This asks a yes-or-no question of every surface: *did you end up with any of
    your own?* Alone is 0 and connected is 1 at every size, so the number means
    the same thing in every cell.

    **It is recall-shaped and must not be read alone.** A recovery that puts
    everything in one class scores 1.0, exactly as `reached_together` does.
    Report `largest` beside it — that lesson cost `g33-02` a headline.

    Args:
        among: Which surfaces to score, defaulting to every key in `truth`. A
            caller measuring churn passes the SURVIVORS, because a surface whose
            owner departed has no class to judge and scoring it zero would fold
            two different failures into one number.
    """
    scored = list(truth if among is None else among)
    if not scored:
        raise ValueError(
            "no surfaces to score. A rate over nothing reads as a score")
    connected = 0
    for surface in scored:
        found = recovered.get(surface, frozenset({surface}))
        if (found & truth[surface]) - {surface}:
            connected += 1
    return connected / len(scored)


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
