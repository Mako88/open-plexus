"""The walk: a question answered by spreading activation, as an arm beside the stack.

John's (THE ORDER, item 5). Every node a question names fires at once, and what their
activations meet at is the answer, as spreading activation does (Quillian; ACT-R). A
node passes on its activation divided by its fan, so a hub passes almost none, and an
event passes on what it holds faded by how long ago it was heard, as ACT-R's base level
fades. What falls below `FLOOR` passes nothing on, which is what bounds what a question
touches however long the history grows: the claim this arm is read for.

No weights are learnt yet. The schemas, as the links' weights, are what should lift it.
"""

from __future__ import annotations

from unfused.graph import DECAY, GraphArm, Spent, said_in

# how many steps activation spreads, and what is passed on at each
STEPS = 3
PASS = 0.8
# activation below this is kept where it landed and spreads no further
FLOOR = 1e-3
# the latest events a verb's lemma reaches, as a name reaches its latest individuals
LEMMA = 300
# what a source that never reached a node counts for, in the product read where they meet
MISS = 1e-4


class WalkArm(GraphArm):
    name = "walked"

    def answer(self, question) -> str:
        self.spent, said, given_up = 0, None, False
        try:
            said = self.walked(question.text)
        except Spent:
            given_up = True
        self.spent = None
        self.last_notes = (["(gave up)"] if given_up else []) + (
            ["(nothing)"] if said is None else ["(found)"]) + ["by:walk"]
        return "I don't know." if said is None else said

    def sources(self, question: str):
        """The nodes a question names, each with the activation it starts with, the
        lemma it asks about, and the links the asked slot could be."""
        got = self.pattern(question)
        if got is not None:
            lemma, free, bound, named, mood, wh = got
            names = list(dict.fromkeys([n for _, n in bound] + list(named)))
            if free == ["prep:*"]:
                free = self.circumstances(wh) + ["prep:*"]
        else:
            lemma, mood, free = None, "", None
            names = self.shape(question)[1]
        if not all(self.known(n) for n in names):
            # a question about someone never mentioned is not guessed at
            return None
        # each name and the lemma is a source of its own, so where they meet is read
        groups: list[dict[str, float]] = []
        for name in names:
            # a name fires its individuals in mind, the way recall by a cue reaches what
            # is active; where none is, the name itself, which reaches its latest
            held = self.individuals(name, episode=True) or [f"n:{name}"]
            groups.append({n: 1.0 / len(held) for n in held})
        if lemma is not None:
            # a verb fires its events in mind the same way, and its latest where none is
            events = [f"e:{e}" for (e,) in self.db.execute(
                "SELECT id FROM events WHERE lemma = ? AND mood = ? AND turn > ? ORDER BY id "
                "DESC LIMIT ?", (lemma, mood, self.episode(), LEMMA))] or [
                f"e:{e}" for (e,) in self.db.execute(
                    "SELECT id FROM events WHERE lemma = ? AND mood = ? ORDER BY id DESC "
                    "LIMIT ?", (lemma, mood, LEMMA))]
            if events:
                groups.append({e: self.fresh(e) / len(events) for e in events})
        return groups, names, lemma, free

    def fresh(self, event: str) -> float:
        """How strongly an event is in mind now: ACT-R's base level of one hearing."""
        return (self._now - self.event(event)[1] + 1) ** -DECAY

    def spread(self, start: dict[str, float]) -> dict[str, float]:
        """Activation spread from the sources for `STEPS` steps: every node's total."""
        total = dict(start)
        frontier = dict(start)
        for _ in range(STEPS):
            nxt: dict[str, float] = {}
            for node, a in frontier.items():
                if a < FLOOR:
                    continue
                steps = [s for s in self.around(node) if not s[2].startswith("f:")]
                if not steps:
                    continue
                share = a * PASS / len(steps)
                for _, _, n in steps:
                    w = share * self.fresh(n) if n.startswith("e:") else share
                    nxt[n] = nxt.get(n, 0.0) + w
            for n, a in nxt.items():
                total[n] = total.get(n, 0.0) + a
            frontier = nxt
        return total

    def walked(self, question: str) -> str | None:
        self._now = self.now()
        got = self.sources(question)
        if got is None:
            return None
        groups, names, lemma, free = got
        spread = [self.spread(g) for g in groups]
        # what the sources meet at: the product of what each brought, so a node every
        # source reached outranks one only some did, however much those brought
        act: dict[str, float] = {}
        for n in {n for s in spread for n in s}:
            a = 1.0
            for s in spread:
                a *= s.get(n, 0.0) + MISS
            act[n] = a
        start = {n for g in groups for n in g}

        def rank(label: str) -> int | None:
            if free is None:
                return 0
            for r, f in enumerate(free):
                if label == f or (f == "prep:*" and label.startswith("prep:")
                                  and ">" not in label):
                    return r
            return None

        def named(node: str) -> bool:
            return any(self.is_(node, f"n:{n}") for n in names)

        # an event's activation is read at the asked slot: what it holds there is scored
        # by how much the event gathered, the slot lessons rank first scored most
        score: dict[str, float] = {}
        for e, a in act.items():
            if not e.startswith("e:"):
                continue
            for label, d, n in self.around(e):
                if d != 1 or n.startswith("f:") or (r := rank(label)) is None:
                    continue
                if n.startswith("e:"):
                    # a possessed noun ('her veil') is an event whose `self` is the name
                    n = next((m for lab, dd, m in self.around(n) if lab == "self" and dd == 1),
                             None)
                    if n is None:
                        continue
                if named(n):
                    continue
                score[n] = score.get(n, 0.0) + a / (1 + r)
        if not score:
            # nothing holds the slot: what the question's names brought most to mind
            score = {n: a for n, a in act.items() if n.startswith(("i:", "n:"))
                     and n not in start and not named(n)}
        best = None
        for n, s in sorted(score.items(), key=lambda x: -x[1]):
            said = self.describe(n)
            if said and not said_in(said, question):
                best = said
                break
        return best

