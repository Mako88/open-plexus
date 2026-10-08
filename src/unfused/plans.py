"""Plans: the plans lessons left, and which taught shapes a question is nearest.

A plan is a path kept under a question's shape, with how often it held and failed. Only the
plans that held more often than they failed are followed, and the shapes that have one are the
taught shapes: each has a signature (the grammar its wordings share), and a question no lesson
was worded as is read as the nearest of them. What is read from the table is kept until a
lesson changes which plans are followed: `begin` marks a lesson, `changed` counts a write to
`learnt`, `refollowed` counts a change of which plans are followed or a new one, and the
caches are rebuilt at the next read only if that count moved.
"""

from __future__ import annotations

import json
from collections import Counter
from itertools import combinations, permutations

from unfused.shaper import Shaper


class Plans:
    def __init__(self, db, shaper: Shaper) -> None:
        self.db = db
        self.shaper = shaper
        # taught shapes' signatures, rebuilt after a lesson
        self._sigs: list | None = None
        # how many times `learnt` has been written
        self.lessons = 0
        # how many times a lesson changed which plans are followed, or added one, and the
        # count the taught shapes' caches were last rebuilt at; a lesson marks them stale,
        # and they are rebuilt at the next read only if that count moved
        self._shapes = 0
        self._built = 0
        self._stale = False
        # the taught shapes of each number of slots, indexed by their signatures' parts,
        # and the nearest shapes already found for a signature; both until a lesson
        self._by_slots: dict = {}
        self._near: dict = {}
        # each stored plan's text, decoded (`read_plan`)
        self._read: dict[str, dict] = {}

    def begin(self) -> None:
        """A lesson is being taught: the taught shapes are read again at the next use if it
        changes which plans are followed."""
        self._stale = True

    def changed(self) -> None:
        self.lessons += 1

    def refollowed(self) -> None:
        self._shapes += 1

    def ranked(self, shape: str, counts: bool = True) -> list[str]:
        """The texts of a shape's followed plans, the one that held most often first; a plan
        that counts a set is left out with `counts` false."""
        rows = [p for (p,) in self.db.execute(
            "SELECT plan FROM learnt WHERE shape = ? AND hits > misses "
            "ORDER BY hits - misses DESC, hits DESC", (shape,))]
        return rows if counts else [p for p in rows if not self.read_plan(p).get("count")]

    def of(self, shape: str, counts: bool = True) -> list[dict]:
        """A shape's followed plans, decoded, in the order the table gives them; a plan that
        counts a set is left out with `counts` false."""
        plans = [self.read_plan(p) for (p,) in self.db.execute(
            "SELECT plan FROM learnt WHERE shape = ? AND hits > misses", (shape,))]
        return plans if counts else [p for p in plans if not p.get("count")]

    def read_plan(self, text: str) -> dict:

        """A stored plan, decoded once for every reader: answering reads every taught
        plan many times a question and a lesson changes one shape's. Shared, so only
        `teach`, which changes plans, decodes its own."""
        got = self._read.get(text)
        if got is None:
            # a lesson rewrites a plan's text, so the texts it replaced are let go in bulk
            if len(self._read) > 100_000:
                self._read.clear()
            got = self._read[text] = json.loads(text)
        return got

    def settled(self) -> None:
        """After a lesson, the taught shapes are read again at the next use, unless the
        lesson changed none of the plans followed, which is all they are read from."""
        if self._stale:
            self._stale = False
            if self._built != self._shapes:
                self._sigs, self._by_slots, self._near = None, {}, {}
                self._built = self._shapes

    def taught(self) -> list[tuple[str, set]]:
        """Every taught shape's plans' signatures, rebuilt after a lesson."""
        self.settled()
        if self._sigs is None:
            self._sigs = []
            for shape, plan in self.db.execute(
                    "SELECT shape, plan FROM learnt WHERE hits > misses").fetchall():
                sig = self.read_plan(plan).get("sig")
                if sig:
                    self._sigs.append((shape, set(sig)))
        return self._sigs

    def nearest(self, question: str, fillers: list[str],
                skip: str | None = None) -> tuple[float, list[str]]:
        """The taught shapes nearest a question no lesson was worded as: those of its
        number of slots whose signature overlaps the question's most, and by how much."""
        mine = frozenset(self.shaper.signature(question, fillers))
        key = (mine, len(fillers), skip)
        self.settled()
        got = self._near.get(key)
        if got is None:
            # each shape's overlap is counted through the index, so a shape sharing
            # nothing with the question costs nothing, and scores 0
            rows, index = self.taught_by(len(fillers))
            shared = Counter(i for part in mine for i in index.get(part, ()))
            near = [(shape, shared[i] / (len(mine) + len(sig) - shared[i]))
                    for i, (shape, sig) in enumerate(rows) if shape != skip]
            best = max((n for _, n in near), default=0.0)
            got = self._near[key] = (best, list(dict.fromkeys(
                shape for shape, n in near if n == best)))
        return got[0], list(got[1])

    def taught_by(self, slots: int) -> tuple[list, dict]:
        """The taught shapes of this many slots with their signatures, in the order
        `taught` gives them, and where each part of a signature is held among them."""
        self.settled()
        got = self._by_slots.get(slots)
        if got is None:
            rows = [(shape, sig) for shape, sig in self.taught() if shape.count("<") == slots]
            index: dict = {}
            for i, (_, sig) in enumerate(rows):
                for part in sig:
                    index.setdefault(part, []).append(i)
            got = self._by_slots[slots] = (rows, index)
        return got

    def borrowed(self, question: str,
                 skip: str | None = None) -> list[tuple[list[str], list[str]]]:
        """A wording no lesson used has no history saying which of its names are slots
        and which are frame ('cousin' in 'Which person is X's cousin?'), nor in what order
        its slots run. Every reading is scored, each name a slot or frame and the slots in
        any order, nearest a taught shape first."""
        names = self.shaper.names(question)
        readings = []
        for k in range(1, len(names) + 1):
            for chosen in combinations(names, k):
                for order in permutations(chosen):
                    near, shapes = self.nearest(question, list(order), skip)
                    if shapes:
                        readings.append((near, list(order), shapes))
        readings.sort(key=lambda r: -r[0])
        return [(f, sh) for _, f, sh in readings]
