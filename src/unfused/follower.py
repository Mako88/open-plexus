"""Follower: a plan followed from a question's names, and an answer found by the plans.

A plan's steps are walked from the question's first name; each event passed must be of the lemma
and the mood the lessons saw there, in the order lessons showed, and a plan that asks about the
present passes no event a later one replaced. The ends it reaches are the answers, each with the
turns of the events passed. Of a shape's plans those that held more often than they failed
are followed, strictly (the lemmas of the lessons) and then loosely; where a wording has no plan
of its own the plans of the nearest taught shape are borrowed.
"""

from __future__ import annotations

from unfused.individuals import Individuals
from unfused.numbers import Numbers
from unfused.plans import Plans
from unfused.said import said_in
from unfused.shaper import Shaper
from unfused.walker import REACH, Walker


class Follower:
    def __init__(self, db, walker: Walker, individuals: Individuals, plans: Plans,
                 shaper: Shaper, numbers: Numbers) -> None:
        self.db = db
        self.walker = walker
        self.individuals = individuals
        self.plans = plans
        self.shaper = shaper
        self.numbers = numbers

    def follow(self, plan: dict, fillers: list[str], strict: bool,
               present: bool | None = None) -> list[tuple[str, int]]:
        """Every end the plan reaches from the question's first name, with the latest turn
        of the events it passed. A plan that asks about the present passes no event a later
        one replaced; whether it does is learnt from its lessons, since 'where was it
        before' needs exactly those."""
        if not fillers:
            return []
        if present is None:
            now, then = plan.get("present", (0, 0))
            present = now > then
        out = []
        def hooked(node: str, pos: int) -> bool:
            return all(self.walker.reaches(node, h[2], f"n:{fillers[h[0]]}")
                       for h in plan["attach"] if h[1] == pos and h[0] < len(fillers))

        def ordered(last, j: int, turn_j: int) -> bool:
            if last is None:
                return True
            i, lemma_i, turn_i = last
            before, after = plan.get("order", {}).get(f"{i}-{j}", {}).get(lemma_i, (0, 0))
            if before >= 3 and before > 2 * after:
                return turn_j <= turn_i
            if after >= 3 and after > 2 * before:
                return turn_j >= turn_i
            return True

        self.emptied = 0
        start = f"n:{fillers[0]}"
        # a walk: its nodes, the turns of its events in order, and its last event
        walks = [([start], (), None)] if hooked(start, 0) else []
        for i, (label, direction) in enumerate(plan["steps"]):
            nxt_walks = []
            for nodes, turns, last in walks:
                here = nodes[-1]
                # a hub ('Lily' in a thousand stories) is walked through its latest
                # `REACH` steps of the label, as recall searches the recent first
                ways = [(lab, d, nxt) for lab, d, nxt in self.walker.around(here)
                        if lab == label and d == direction][:REACH]
                for lab, d, nxt in ways:
                    if self.individuals.among(nxt, nodes):
                        continue
                    t, now = turns, last
                    if nxt.startswith("e:"):
                        lemma, et = self.walker.event(nxt)
                        pos = str(i + 1)
                        if strict and lemma not in plan["lemmas"].get(pos, [lemma]):
                            continue
                        # loosely, any verb will do, but never one that did not happen
                        # where the lessons' did, or the other way round
                        mood = self.walker.mood(nxt)
                        if mood not in plan.get("moods", {}).get(pos, [mood]):
                            continue
                        if not ordered(last, i + 1, et):
                            continue
                        if present and (by := self.walker.replaced(nxt)):
                            self.emptied = max(self.emptied, by)
                            continue
                        # what did not happen, or only might, changed nothing, so it is
                        # walked through and never makes an answer the latest
                        t, now = turns + ((0 if mood else et),), (i + 1, lemma, et)
                    if not hooked(nxt, i + 1):
                        continue
                    nxt_walks.append((nodes + [nxt], t, now))
            walks = nxt_walks[:2000]
        for nodes, turns, _ in walks:
            end = nodes[-1]
            if not end.startswith("e:"):
                # the most recent thing that happened to the first name first, then what
                # followed from it: the football's last event, then its carrier's move
                out.append((self.individuals.describe(end), turns))
        return out

    @staticmethod
    def visits(ends: list, name: str) -> tuple[list, int | None]:
        """A plan's ends as visits in turn order, and where the latest visit to a name
        sits among them: the apple's rooms, and the bathroom's place in that list."""
        seen = sorted({(t[-1] if t else 0, e) for e, t in ends})
        at = max((i for i, (_, e) in enumerate(seen) if e == name), default=None)
        return seen, at

    def relative(self, plan: dict, ends: list, fillers: list[str]) -> list | None:
        """Where lessons showed the answer one visit before or after another name of
        the question ('where was it before the bathroom'), that visit."""
        for k, (before, after) in plan.get("relative", {}).items():
            k = int(k)
            if k >= len(fillers):
                continue
            seen, at = self.visits(ends, fillers[k])
            if at is None:
                continue
            step = -1 if before >= 3 and before > 2 * after else (
                1 if after >= 3 and after > 2 * before else 0)
            if step and 0 <= at + step < len(seen):
                t, e = seen[at + step]
                return [(e, (t,))]
        return None

    def said(self, plan: dict, fillers: list[str], strict: bool,
             question: str, present: bool | None = None) -> list[tuple[str, int]]:
        ends = self.follow(plan, fillers, strict, present)
        chosen = self.relative(plan, ends, fillers)
        if chosen is not None:
            return chosen
        if plan.get("count"):
            if ends:
                return [(self.numbers.say_number(len({e for e, _ in ends})),
                         max(t for _, t in ends))]
            # nothing left to count is an answer, resting on what emptied the set: 'Mary
            # dropped the football' is later than her picking it up
            return [(self.numbers.say_number(0), (self.emptied,))] if self.emptied else []
        return [f for f in ends if not said_in(f[0], question)]

    def followed(self, ranked: list, fillers: list[str], question: str) -> list:
        for strict in (True, False):
            found = []
            for (plan,) in ranked:
                plan = self.plans.read_plan(plan)
                said = self.said(plan, fillers, strict, question)
                if said and plan.get("count"):
                    # a count is of a set, not of one latest event, so the plan that held
                    # most often answers it rather than whichever passed the latest turn
                    return said
                found += said
            if found:
                return found
        return []

    def answers(self, shape: str, fillers: list[str], question: str,
                borrow: bool = True) -> list[tuple[str, int]]:
        ranked = self.db.execute(
            "SELECT plan FROM learnt WHERE shape = ? AND hits > misses "
            "ORDER BY hits - misses DESC, hits DESC", (shape,)).fetchall()
        if ranked:
            found = self.followed(ranked, fillers, question)
            if found:
                return found
        if not borrow or not all(self.individuals.known(f) for f in fillers):
            # of a name the conversation never used nothing can be known, so its
            # question is not borrowed for
            return []
        # a wording no lesson used, or one whose plans reach nothing here, borrows the
        # plans of the nearest other taught shape under the nearest reading of it that
        # reaches anything in the graph: 'Who is X's cousin?' learnt one direction, and
        # 'Whose cousin is X?' holds the other
        for fillers, nearest in self.plans.borrowed(question, shape)[:12]:
            ranked = [r for near in nearest for r in self.db.execute(
                "SELECT plan FROM learnt WHERE shape = ? AND hits > misses "
                "ORDER BY hits - misses DESC, hits DESC", (near,)).fetchall()]
            found = self.followed(ranked, fillers, question)
            if found:
                return found
        return []

    def reaching(self, question: str, skip: str, tries: int = 60) -> list:
        """Where the nearest shapes reach nothing, every taught shape with as many slots
        as the question names known things, nearest first, read under each order of
        those names: the first whose plans reach anything is the relation asked, a count
        never, since a count of anything is a number. Last of all, after taking the
        question apart, which reads it more closely. Two
        wordings are one relation where the house holds a solution for both: 'What is
        the number of X in the Y?' shares little grammar with 'How many X are in the
        Y?' and the house relates X and Y by little else."""
        from itertools import combinations, permutations

        names = [n for _, _, n in self.shaper.template(question)[1] if self.individuals.known(n)][:4]
        readings = []
        for k in range(1, len(names) + 1):
            for chosen in combinations(names, k):
                for order in permutations(chosen):
                    mine = set(self.shaper.signature(question, list(order)))
                    readings += [(len(mine & sig) / len(mine | sig), list(order), shape)
                                 for shape, sig in self.plans.taught()
                                 if shape != skip and shape.count("<") == k]
        readings.sort(key=lambda r: -r[0])
        for _, fillers, shape in readings[:tries]:
            ranked = [(p,) for (p,) in self.db.execute(
                "SELECT plan FROM learnt WHERE shape = ? AND hits > misses "
                "ORDER BY hits - misses DESC, hits DESC", (shape,)) if not self.plans.read_plan(p).get("count")]
            found = self.followed(ranked, fillers, question)
            if found:
                return found
        return []
