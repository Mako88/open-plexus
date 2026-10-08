"""Decomposer: a question the plans cannot answer, taken apart.

Plans are whole paths and do not compose; a question's grammar does. Where the plans of a
question's shape find nothing, its innermost noun phrase holding a name is asked as a question of
its own ('Who is Kroudroth's cousin?'), its answer put in its place, and what is left asked again
(`apart`); a clause about something ('the things X keeps in the cellar') is answered as a join
(`joined`); and where no plan reaches, a fact told in a shape no lesson's fact was is found by
the role its answer plays (`kinded`). A phrase holding a relative clause is put as what its
clause names (`resolved`).
"""

from __future__ import annotations

from unfused.follower import Follower
from unfused.individuals import Individuals
from unfused.meeter import Meeter
from unfused.plans import Plans
from unfused.reader import Reader
from unfused.said import said_in
from unfused.shaper import Shaper
from unfused.solver import Solver
from unfused.walker import Walker


class Decomposer:
    def __init__(self, db, reader: Reader, walker: Walker, individuals: Individuals,
                 plans: Plans, shaper: Shaper, follower: Follower, solver: Solver,
                 meeter: Meeter) -> None:
        self.db = db
        self.reader = reader
        self.walker = walker
        self.individuals = individuals
        self.plans = plans
        self.shaper = shaper
        self.follower = follower
        self.solver = solver
        self.meeter = meeter

    def answered(self, text: str, depth: int = 0) -> str | None:
        """An answer from the plans of the question's shape, or, where they find nothing,
        from the question taken apart: its innermost noun phrase holding a name is asked
        as a question of its own ('Who is Kroudroth's cousin?'), its answer put in its
        place, and what is left asked again. Plans are whole paths and do not compose; a
        question's grammar does."""
        shape, fillers = self.shaper.shape(text)
        found = self.follower.answers(shape, fillers, text, borrow=False)
        if found:
            return self.latest(found)
        if depth < 3 and (said := self.joined(text, depth)) is not None:
            return said
        found = self.follower.answers(shape, fillers, text)
        if found:
            return max(found, key=lambda f: f[1])[0]
        # borrowed before taken apart, by the first house: taking 'X's lanterns' apart
        # first read 0.756 0.784 0.779 against 0.760 0.788 0.809
        if depth >= 3 or not all(self.individuals.known(f) for f in fillers):
            return None
        said = self.apart(text, depth)
        if said is None and depth == 0 and (found := self.follower.reaching(text, shape)):
            said = self.latest(found)
        if said is None and depth == 0 and (found := self.kinded(text, shape, fillers)):
            said = self.latest(found)
        return said

    @staticmethod
    def latest(found: list) -> str:
        return max(found, key=lambda f: f[1])[0]

    def roles(self, shapes: list[str]) -> set[str]:
        """The links an answer hangs by at the end of these shapes' plans: 'prep:in' for
        where a thing is kept, 'nummod' for how many. A name's kind is the links it is
        held by, so an answer held by none of these is of another kind."""
        out = set()
        for shape in shapes:
            for (p,) in self.db.execute(
                    "SELECT plan FROM learnt WHERE shape = ? AND hits > misses", (shape,)):
                plan = self.plans.read_plan(p)
                if plan["steps"] and not plan.get("count") and plan["steps"][-1][1] == 1:
                    out.add(plan["steps"][-1][0])
        return out

    def kinded(self, text: str, shape: str, fillers: list[str],
               limit: int = 6) -> list[tuple[str, tuple]]:
        """Where no plan reaches, a fact told in a shape no lesson's fact was: the
        shortest path from the question's first name to a name in the role the answer
        plays at the end of the shape's plans, through nothing that did not happen, with
        each other name of the question two steps or fewer from the path. 'Gopaiff has the
        fishing floats tucked away in the dairy' has the dairy at the end of a 'prep:in'
        as 'keeps the floats in the dairy' has, by a longer way."""
        if not fillers:
            return []
        roles = self.roles([shape]) or self.roles(self.plans.nearest(text, fillers)[1])
        if not roles:
            return []
        start = f"n:{fillers[0]}"
        frontier, seen, found = [[start]], {start}, []
        # whether an event is two steps or fewer from a name, asked of the same events
        # for every way through them, so found once a question
        near: dict = {}

        def close(n: str, f: str) -> bool:
            got = near.get((n, f))
            if got is None:
                got = near[(n, f)] = bool(self.walker.paths(n, f"n:{f}", limit=2, most=1))
            return got

        for _ in range(limit):
            nxt = []
            for path in frontier:
                for label, direction, node in self.walker.around(path[-1]):
                    if self.individuals.among(node, seen):
                        continue
                    if node.startswith("e:") and self.walker.mood(node):
                        continue
                    way = path + [(label, direction), node]
                    if (not node.startswith("e:") and direction == 1 and label in roles
                            and not said_in(self.individuals.describe(node), text) and all(
                                any(close(n, f) for n in way[::2]
                                    if n.startswith("e:")) for f in fillers[1:])):
                        turns = [self.walker.event(n)[1] for n in way[::2] if n.startswith("e:")]
                        found.append((self.individuals.describe(node), (max(turns, default=0),)))
                    nxt.append(way)
            if found:
                return found
            for way in nxt:
                seen.add(way[-1])
            frontier = nxt[:2000]
        return []

    def joined(self, text: str, depth: int) -> str | None:
        """A question holding a clause about something ('the things X keeps in the
        cellar') answered as a join: the clause is a relation some taught shape holds,
        solved for the thing with the clause's names bound, and the thing put in the
        clause's place. A shape's plans are its relation's disjuncts, one a wording it was
        taught in, so the clause is found however the fact was told."""
        for a, b in self.inner(text):
            names = [n for _, _, n in self.shaper.template(text[a:b])[1] if self.individuals.known(n)]
            if not names:
                continue
            for thing in self.solver.related(text[a:b], names):
                said = self.answered(text[:a] + thing.title() + text[b:], depth + 1)
                if said is not None:
                    return said
        return None

    def apart(self, text: str, depth: int) -> str | None:
        for a, b in self.inner(text):
            phrase = text[a:b]
            for ask in (f"Who is {phrase}?", f"What is {phrase}?"):
                named = self.answered(ask, depth + 1)
                if named is None:
                    continue
                said = self.answered(text[:a] + named.title() + text[b:], depth + 1)
                if said is not None:
                    return said
        return None

    def inner(self, text: str) -> list[tuple[int, int]]:
        """The noun phrases of a question that hold a name and something said of it, as
        character spans, innermost first: 'the person who repairs clocks' before 'the
        cousin of the person who repairs clocks'. A phrase is a noun's whole subtree, kept
        where a possessor, a clause or a preposition hangs off it and the wh-word is not
        inside it."""
        rows = self.reader.spans(text)
        kids: dict[int, list[int]] = {}
        for i, r in enumerate(rows):
            if r[4] != i:
                kids.setdefault(r[4], []).append(i)

        def subtree(i: int) -> list[int]:
            out = [i]
            for k in kids.get(i, []):
                out += subtree(k)
            return out

        found = []
        for i, (_, _, tag, _, _, pos) in enumerate(rows):
            if pos not in ("NOUN", "PROPN"):
                continue
            if not any(rows[k][3] in ("nmod:poss", "acl:relcl", "acl", "nmod")
                       for k in kids.get(i, [])):
                continue
            span = sorted(subtree(i))
            # the question's own wh-word, not a relative one ('the person who repairs')
            if span[0] == 0:
                continue
            if len(span) >= len(rows) - 2 or not any(
                    rows[k][5] == "PROPN" or self.individuals.known(rows[k][1].lower()) for k in span
                    if k != i):
                continue
            a = rows[span[0]][0]
            b = rows[span[-1]][0] + len(rows[span[-1]][1])
            while text[a:b].lower().startswith(("the ", "a ")):
                a = text.index(" ", a) + 1
            found.append((b - a, a, b))
        return [(a, b) for _, a, b in sorted(found)]

    def resolved(self, text: str) -> str:
        """The question with a phrase holding a relative clause ('the one who found a
        shell') put as the individual its clause names. A relative clause is a question
        about the noun it hangs from, so its own words, the relative word first, are asked
        of the walk ('who found a shell?') and what they meet stands in the phrase's place.
        The relative word is the parse's (`PronType=Rel`); a clause the walk cannot answer
        leaves the question as it was. What the parse gives, each such phrase's span and its
        clause's, is kept with the parses."""
        for a, b, start, end in self.reader.relatives(text):
            who = self.meeter.met(text[start:end] + "?")
            if who:
                return text[:a] + who.title() + text[b:]
        return text
