"""Meeter: what a question's sources meet at, by spreading activation.

Each name a question says fires its individuals in mind, and its verb the episode's events of
that lemma; each spreads `STEPS` steps (Quillian; ACT-R), divided by fan, an event faded by its
base level, and nothing below `FLOOR` spreads on, so what a question touches is bounded however
long the history. An event is read where every name reached it within two steps and it is of
the question's verb, scored by the product of what each source brought it. A question needs no
lesson in its wording, as a plan's shape does; what it asks is in its grammar.
"""

from __future__ import annotations

import math

from unfused.individuals import Individuals
from unfused.mind import Mind
from unfused.reader import Reader
from unfused.said import said_in
from unfused.walker import Walker

# how fast a hearing fades within an episode (ACT-R's base-level decay)
DECAY = 0.5
# the walk a question's names and verb spread by (`met`): how many steps, what is passed
# on at each, the activation below which nothing spreads on, and the latest events a
# verb reaches where none is in mind, as a name reaches its latest individuals
STEPS = 3
PASS = 0.8
FLOOR = 1e-3
LEMMA = 300


class Meeter:
    def __init__(self, db, reader: Reader, mind: Mind, individuals: Individuals,
                 walker: Walker) -> None:
        self.db = db
        self.reader = reader
        self.mind = mind
        self.individuals = individuals
        self.walker = walker
        self._now = 0

    def tick(self) -> None:
        """Read the turn it is now, which an event's base level fades from."""
        self._now = self.mind.now()

    def met(self, question: str) -> str | None:
        """What a question's sources meet at, read at the asked slot: spreading activation
        (Quillian; ACT-R, John's walk). Each name the question says fires its
        individuals in mind, and its verb the episode's events of that lemma; each
        spreads `STEPS` steps, divided by fan, an event faded by its base level, and
        nothing below `FLOOR` spreads on, so what a question touches is bounded however
        long the history. An event is read where every name reached it within two steps
        and it is of the question's verb, scored by the product of what each source
        brought it. A question needs no lesson in its wording, as a plan's shape does;
        what it asks is in its grammar."""
        self.tick()
        got = self.sources(question)
        if got is None:
            return None
        groups, names, lemma, free = got
        if not groups:
            return None
        spread, depth = zip(*[self.spread(g) for g in groups])
        def reached(e: str) -> bool:
            # a verb alone meets nothing: an event of another story is read only where a
            # name the question says met it there
            if not names and not self.mind.held(self.walker.event(e)[1]):
                return False
            return all(e in d and d[e] <= (0 if lemma is not None and i == len(depth) - 1
                                           else 2) for i, d in enumerate(depth))

        def rank(label: str) -> int | None:
            for r, f in enumerate(free):
                if label == f or (f == "prep:*" and label.startswith("prep:")
                                  and ">" not in label):
                    return r
            return None

        def named(node: str) -> bool:
            return any(self.individuals.stands_for(node, f"n:{n}") for n in names)

        # what each event holds at the asked slot is scored by the product of what each
        # source brought the event, the slot lessons rank first scored most
        score: dict[str, float] = {}
        for e in {n for s in spread for n in s if n.startswith("e:")}:
            if not reached(e):
                continue
            a = math.prod(s.get(e, 0.0) for s in spread)
            for label, d, n in self.walker.around(e):
                if d != 1 or n.startswith("f:") or (r := rank(label)) is None:
                    continue
                if n.startswith("e:"):
                    # a possessed noun ('her veil') is an event whose `self` is the name
                    n = next((m for lab, dd, m in self.walker.around(n) if lab == "self" and dd == 1),
                             None)
                    if n is None:
                        continue
                if named(n):
                    continue
                score[n] = score.get(n, 0.0) + a / (1 + r)
        for n, _ in sorted(score.items(), key=lambda x: -x[1]):
            said = self.individuals.describe(n)
            if said and not said_in(said, question):
                return said
        return None

    def sources(self, question: str):
        """Where a question's activation starts: a group a source (each name it says,
        and its verb), the names, the verb's lemma and the links the asked slot could
        be. None for a question that asks no slot, or about someone never mentioned."""
        got = self.reader.pattern(question)
        if got is None:
            # no slot asked, so nothing to read where the sources meet
            return None
        lemma, free, bound, named, mood_, wh = got
        names = list(dict.fromkeys([n for _, n in bound] + list(named)))
        if free == ["prep:*"]:
            free = self.circumstances(wh) + ["prep:*"]
        if not all(self.individuals.known(n) for n in names):
            return None
        groups: list[dict[str, float]] = []
        for name in names:
            # a name fires its individuals in mind, as recall by a cue reaches what is
            # active; where none is, the name itself, which reaches its latest. Every
            # individual it labels, each by its base level, lost late checks (the commit
            # that says so)
            held = self.individuals.of(name, episode=True) or [f"n:{name}"]
            groups.append({n: 1.0 / len(held) for n in held})
        if lemma is not None:
            # a verb fires its events in mind the same way, and its latest where none is
            events = [f"e:{e}" for e in self.mind.events_in_mind(lemma, mood_, LEMMA)] or [
                f"e:{e}" for (e,) in self.db.execute(
                    "SELECT id FROM events WHERE lemma = ? AND mood = ? ORDER BY id DESC "
                    "LIMIT ?", (lemma, mood_, LEMMA))]
            if events:
                groups.append({e: self.fresh(e) / len(events) for e in events})
        return groups, names, lemma, free

    def fresh(self, event: str) -> float:
        """How strongly an event is in mind now: ACT-R's base level of one hearing."""
        return (self._now - self.walker.event(event)[1] + 1) ** -DECAY

    def spread(self, start: dict[str, float]) -> tuple[dict[str, float], dict[str, int]]:
        """Activation spread from one source for `STEPS` steps: every node's total, and
        the step it was first reached at."""
        total, depth, frontier = dict(start), dict.fromkeys(start, 0), dict(start)
        for step in range(1, STEPS + 1):
            nxt: dict[str, float] = {}
            for node, a in frontier.items():
                if a < FLOOR:
                    continue
                steps = [s for s in self.walker.around(node) if not s[2].startswith("f:")]
                if not steps:
                    continue
                share = a * PASS / len(steps)
                for _, _, n in steps:
                    nxt[n] = nxt.get(n, 0.0) + (share * self.fresh(n) if n.startswith("e:")
                                                else share)
            for n, a in nxt.items():
                total[n] = total.get(n, 0.0) + a
                depth.setdefault(n, step)
            frontier = nxt
        return total, depth

    def circumstances(self, wh: str) -> list[str]:
        """The links a word asking for a circumstance ('where', 'when') has had its
        answers hang by, learnt from lessons, as the parse marks only that it asks, the
        commonest first. `met` tries them first and any other oblique after."""
        got = [lab for (lab,) in self.db.execute(
            "SELECT link FROM circumstances WHERE wh = ? ORDER BY n DESC", (wh,))]
        return got

    def learn_circumstance(self, question: str, want: str) -> None:
        """A lesson's answer to a word asking for a circumstance: the oblique the answer
        hangs by in the episode's event the question matches."""
        got = self.reader.pattern(question)
        if got is None or got[1] != ["prep:*"]:
            return
        lemma, _, bound, _, mood_, wh = got
        for eid in self.mind.events_in_mind(lemma, mood_, 50):
            edges = self.db.execute("SELECT label, node FROM edges WHERE event = ? AND node "
                                    "NOT LIKE 'f:%'", (eid,)).fetchall()
            if not all(any(lab == label and self.individuals.stands_for(n, f"n:{name}") for lab, n in edges)
                       for label, name in bound):
                continue
            for lab, n in edges:
                if lab.startswith("prep:") and not n.startswith("e:") and said_in(
                        want, self.individuals.describe(n)):
                    self.db.execute("INSERT INTO circumstances VALUES (?, ?, 1) ON CONFLICT"
                                    "(wh, link) DO UPDATE SET n = n + 1", (wh, lab))
                    return
