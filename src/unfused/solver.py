"""Solver: a plan read as a pattern, matched with any of its variables free.

A plan is a path from a question's first name to its answer, with where each other name hangs
off it. Followed from its first name it finds the answer (that is `GraphArm.follow`); read as a
pattern its nodes are variables, and it can be matched outward from any that is bound, which
finds a name from the answer (`solve`) or what a phrase's clause leaves open (`related`).
"""

from __future__ import annotations

from itertools import combinations, permutations

from unfused.individuals import Individuals
from unfused.plans import Plans
from unfused.shaper import Shaper
from unfused.walker import Walker


class Solver:
    def __init__(self, db, walker: Walker, individuals: Individuals, plans: Plans,
                 shaper: Shaper) -> None:
        self.db = db
        self.walker = walker
        self.individuals = individuals
        self.plans = plans
        self.shaper = shaper
        # the plans of every followed shape as of the last lesson count `related` read them at
        self._followed: tuple | None = None

    def solve(self, plan: dict, bound: dict, free) -> list[tuple[str, tuple]]:
        """A plan read as a pattern and matched with any of its variables free: the
        path's first node is variable 0, its end 'a', and each hook's end the slot it
        names. Matched outward from a bound variable; the values found for `free`, each
        with the latest turn of what happened on the way."""
        steps = plan["steps"]
        edges = [(("p", i), ("p", i + 1), lab, d) for i, (lab, d) in enumerate(steps)]
        var = {0: ("p", 0), "a": ("p", len(steps))}
        for h, (k, i, hsteps) in enumerate(plan["attach"]):
            prev = ("p", i)
            for j, (lab, d) in enumerate(hsteps):
                edges.append((prev, ("h", h, j), lab, d))
                prev = ("h", h, j)
            var[k] = prev
        if free not in var or not bound or not all(v in var for v in bound):
            return []
        want = {var[v]: f"n:{n}" for v, n in bound.items()}
        root = next(iter(want))
        moods = plan.get("moods", {})
        partials = [{root: want[root]}]
        todo = list(edges)
        while todo and partials:
            seen = partials[0]
            edge = next((e for e in todo if (e[0] in seen) != (e[1] in seen)), None)
            if edge is None:
                break
            todo.remove(edge)
            u, v, lab, d = edge
            here, there, way = (u, v, d) if u in seen else (v, u, -d)
            nxt = []
            for got in partials:
                for node in self.walker.around_as(got[here], lab, way):
                    if self.individuals.visited(node, set(got.values())):
                        continue
                    if there in want and not self.individuals.stands_for(node, want[there]):
                        continue
                    # never through what did not happen where the lessons' did, or the
                    # other way round, as a plan is followed
                    if node.startswith("e:") and there[0] == "p" and self.walker.mood(node) not in \
                            moods.get(str(there[1]), [self.walker.mood(node)]):
                        continue
                    nxt.append({**got, there: node})
            partials = nxt[:2000]
        out = []
        for got in partials:
            end = got.get(var[free], "")
            if not end or end.startswith("e:") or len(got) < len(
                    {n for e in edges for n in e[:2]}):
                continue
            turns = [self.walker.event(n)[1] for n in got.values()
                     if n.startswith("e:") and not self.walker.mood(n)]
            out.append((self.individuals.describe(end), (max(turns, default=0),)))
        return out

    def related(self, phrase: str, names: list[str]) -> list[str]:
        """What a phrase's clause leaves open, from the taught shapes with a variable for
        some of its names and one more, nearest the phrase first. Some, because a name
        the graph knows may be the shape's frame ('cousin' in "X's cousin"). Each way of
        binding the names to its variables with the rest free, and what the first binding
        to reach anything finds, latest first."""
        if self._followed is None or self._followed[0] != self.plans.lessons:
            rows: dict[str, list] = {}
            for shape, plan in self.db.execute(
                    "SELECT shape, plan FROM learnt WHERE hits > misses "
                    "ORDER BY hits - misses DESC, hits DESC").fetchall():
                rows.setdefault(shape, []).append(self.plans.read_plan(plan))
            sigs = {shape: next((set(p["sig"]) for p in plans if p.get("sig")), set())
                    for shape, plans in rows.items()}
            self._followed = (self.plans.lessons, rows, sigs)
        _, rows, sigs = self._followed
        shapes = []
        for k in range(len(names), 0, -1):
            for chosen in combinations(names, k):
                mine = set(self.shaper.signature(phrase, list(chosen)))
                for shape, sig in sigs.items():
                    if shape.count("<") == k:
                        shapes.append((len(mine & sig) / max(1, len(mine | sig)), chosen, shape))
        shapes.sort(key=lambda r: -r[0])
        for _, chosen, shape in shapes[:8]:
            plans = [p for p in rows[shape] if not p.get("count")]
            variables = list(range(len(chosen))) + ["a"]
            for free in variables:
                rest = [v for v in variables if v != free]
                for order in permutations(chosen):
                    bound = dict(zip(rest, order))
                    found = [f for plan in plans for f in self.solve(plan, bound, free)
                             if f[0] not in names]
                    if found:
                        return [e for e, _ in sorted(found, key=lambda f: f[1], reverse=True)]
        return []
