"""Answering a composition nobody stated, by counting the ones somebody did.

## The question this exists for

`experiments/clutrr_ceiling.py` measured that CLUTRR-symbolic is answered in full
by 62 facts counted from its two-hop training rows plus a search over bracketings.
So the benchmark as shipped cannot say whether anything was understood — a table
does it.

**Withholding facts makes it say something again.** Hold out 20 of the 62 and the
ceiling for any symbolic reasoner over what remains is 0.3138, with 68% of test
puzzles having no reachable answer at all under any bracketing. Anything above
that line was not deducible from what was stated.

This is the mechanism aimed at that line, and it is the project's own: count, then
walk.

## What it counts, and the whole idea is in the layout

A two-hop row states *walk `left` then `right`, and the ends are `target`*. Stored
as one surface for the pair, counting would learn the table and nothing else — a
pair never seen would have an empty row and there would be nowhere for an answer
to come from.

**So the roles are separate surfaces.** One moment holds three:

    left(father)   right(sister)   target(aunt)

Now `left(father)` accumulates evidence across every row it appears in, whatever
sat beside it. A pair that was never stated still has both of its halves counted,
and the answer is whatever both halves point at — which is generalisation across
relations rather than retrieval of a pair.

**This is the substantive claim and it is exactly what can fail.** If the two
halves are independent of each other in the data — if what `father` composes into
depends entirely on what follows it — then no combination of the two rows can
recover the answer, and the arm returns the marginal. That is the null this is
built to be able to report.

## What it does NOT duplicate, and what was searched

Searched by capability — composition, relation algebra, pair prediction, role,
walk — across `openplexus/`, `tools/`, `tests/` and `experiments/`.

- **`openplexus/grounding.py` supplies all of the scoring**, including the
  combiner set. Nothing here computes a statistic; it decides what counts as a
  moment and what a query is, which is the part grounding deliberately does not
  know about.
- **`openplexus/tasks/clutrr.py`** supplies `composition_table` and `reachable`.
  Those are properties of the corpus and are the arms this is measured against —
  the table is what counting a pair surface would give, and the ceiling is what
  no amount of search over stated facts can beat.
- **`openplexus/grouping.py` and `openplexus/surfaces.py`** are the input path
  and are not involved: CLUTRR arrives symbolic, so there is nothing to quantise.
"""

from __future__ import annotations

from openplexus.grounding import COMBINERS, CoOccurrence, Statistic

#: The three positions a relation can hold in a stated composition. Fixed and
#: ordered, because the surface id is `role index * relations + relation` and a
#: reordering would silently rename every surface in a stored index.
ROLES = ("left", "right", "target")


class Composition:
    """Counts over role-marked relations, and the query that reads them back.

    Attributes:
        relations: How many relation ids exist. The surface space is three times
            this, and ids outside it are refused rather than folded back in —
            wrapping would make `target(0)` and `left(0)` the same surface and
            every score would be computed against a mixture.
        index: The count graph. Public because every measurement in this project
            is taken from the counts rather than from a summary of them.
    """

    def __init__(self, relations: int, right: int | None = None,
                 target: int | None = None) -> None:
        """`relations` sizes every role unless `right` or `target` say otherwise.

        **The three alphabets need not be the same one**, and the second task
        this ran on is why. Composing family relations, all three roles are
        relations and one size does. Link prediction on a knowledge graph asks
        `(entity, relation) -> entity`, so the left is 14,541 wide, the right is
        237 and the target is 14,541 again. Same mechanism, same counting, same
        query — which is what makes the two results comparable rather than two
        different things wearing one name.
        """
        sizes = (relations, relations if right is None else right,
                 relations if target is None else target)
        if min(sizes) < 1:
            raise ValueError("a composition over an empty role has no answers")
        self.relations = relations
        self.sizes = dict(zip(ROLES, sizes))
        #: Where each role's block starts. Computed rather than multiplied out,
        #: because the blocks are no longer the same width and `role * size`
        #: would overlap them silently.
        self.offsets, running = {}, 0
        for role in ROLES:
            self.offsets[role] = running
            running += self.sizes[role]
        self.width = running
        self.index = CoOccurrence()

    def surface(self, role: str, relation: int) -> int:
        if role not in ROLES:
            raise ValueError(f"role must be one of {ROLES}, got {role!r}")
        if not 0 <= relation < self.sizes[role]:
            raise ValueError(
                f"{role} {relation} is outside 0..{self.sizes[role] - 1}. "
                f"Folding it back in would make two different things one "
                f"surface, and every count on it would be a mixture")
        return self.offsets[role] + relation

    def observe(self, left: int, right: int, target: int) -> None:
        """One stated composition: walking `left` then `right` reaches `target`."""
        self.index.observe((self.surface("left", left),
                            self.surface("right", right),
                            self.surface("target", target)))

    def ranked(self, left: int, right: int, statistic: Statistic,
               combine: str = "min") -> list[tuple[float, int]]:
        """Every candidate answer for a pair, best first, as `(score, relation)`.

        Each candidate is scored from BOTH halves and the two are combined. The
        combiner set is `grounding.COMBINERS`, reused rather than restated —
        though what it combines here is different, and the difference matters:
        there it is the two directions of one edge, here it is two independent
        sources of evidence about one candidate.

        **`min` is the default and it is the demanding one.** A candidate scores
        only as well as its weaker half, so a relation that follows `father`
        everywhere cannot win on that alone — it has to also be something
        `sister` leads to. `max` would let either half carry a candidate by
        itself, which is how an ever-present answer attaches to everything.

        **The candidate is the first argument and the half is the second**, which
        is `P(candidate | half)` under `conditional` — the forward direction,
        scored from the asking side, as README §4 records. Written the other way
        round it computes *how typical this half is of that answer*, which ranks
        rare answers first and got the sign of the whole mechanism wrong; the
        ordering test caught it.
        """
        return self.given({"left": left, "right": right}, "target",
                          statistic, combine)

    def given(self, known: dict[str, int], want: str, statistic: Statistic,
              combine: str = "min") -> list[tuple[float, int]]:
        """Rank the `want` role's candidates from whichever roles are `known`.

        **The general form, and the reason it is worth having is that the FLOOR
        falls out of it.** Asking for a target from the right role alone is
        exactly *rank answers by how often they follow this relation* — the
        marginal, with no reference to the entity being asked about. Asking from
        both is the mechanism. So the margin over the floor is not two programs
        compared, it is one program with a half switched off, and no separate
        baseline implementation can drift from it.

        Asking for the LEFT role from a known right and target is the other
        direction of link prediction, off the same counts. The graph is
        directed and the surfaces are role-marked, so nothing has to be stored
        twice for it.
        """
        if combine not in COMBINERS:
            raise ValueError(f"combine must be one of {sorted(COMBINERS)}")
        if want in known:
            raise ValueError(f"{want} is both asked for and given")
        if not known:
            raise ValueError("ranking from nothing known would return the "
                             "candidate order, which is not a prediction")
        rule = COMBINERS[combine]
        sources = [self.surface(role, value) for role, value in known.items()]
        scored = []
        for candidate in range(self.sizes[want]):
            answer = self.surface(want, candidate)
            values = [statistic(self.index, answer, source) for source in sources]
            score = values[0]
            for other in values[1:]:
                score = rule(score, other)
            if score > 0.0:
                scored.append((float(score), candidate))
        scored.sort(key=lambda pair: (-pair[0], pair[1]))
        return scored

    def answer(self, left: int, right: int, statistic: Statistic,
               combine: str = "min") -> int | None:
        """The best candidate, or `None` when nothing scores.

        **`None` is an answer and is not a wrong one.** A pair whose halves point
        at nothing in common is a refusal, and counting it as an error would
        merge *said nothing* with *said the wrong thing* — which are the two
        outcomes any claim about inference has to keep apart.
        """
        scored = self.ranked(left, right, statistic, combine)
        return scored[0][1] if scored else None

    def table(self, statistic: Statistic, combine: str = "min",
              floor: float = 0.0) -> dict[tuple[int, int], int]:
        """Every pair the counts will commit to, as `composition_table` returns.

        Built so the inferred algebra can be handed straight to
        `clutrr.reachable` and a full chain answered with it — the same search,
        over facts that were counted rather than stated. `floor` drops
        commitments weaker than a bound the caller sets; at 0.0 anything with
        evidence at all is kept.
        """
        found: dict[tuple[int, int], int] = {}
        for left in range(self.sizes["left"]):
            for right in range(self.sizes["right"]):
                scored = self.ranked(left, right, statistic, combine)
                if scored and scored[0][0] > floor:
                    found[(left, right)] = scored[0][1]
        return found
