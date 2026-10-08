"""Aliaser: a word never heard read as a name that was.

A question may say 'the chipped dishes' where only cracked plates were told of. The conversation
holds no such word, but the question's other words and the plans of its nearest taught shape
say what could fill its place. Each hearing of an unheard word keeps only the names every
earlier one allowed (`heard_in`, as a child narrows a new word over the situations it is heard in);
a lesson that names the word counts the names its answer fits (`alias`); and a question's word
is put as the name it stood for where lessons settled one, or hearings pinned one, or MiniLM
voted for one (`unaliased`).
"""

from __future__ import annotations

import json
from itertools import combinations, permutations

from unfused.individuals import Individuals
from unfused.parsing import vectors
from unfused.plans import Plans
from unfused.said import said_in
from unfused.shaper import Shaper
from unfused.solver import Solver
from unfused.storage import Store

# how much nearer a word's likest candidate must be than the next for a vote: right picks'
# gaps sat at 0.14 to 0.21 and wrong ones' at 0.02 to 0.06 (readings/unheard-*)
MARGIN = 0.08


class Aliaser:
    def __init__(self, store: Store, shaper: Shaper, individuals: Individuals, plans: Plans,
                 solver: Solver) -> None:
        self.store = store
        self.db = store.db
        self.shaper = shaper
        self.individuals = individuals
        self.plans = plans
        self.solver = solver
        # the words never heard and answers already counted towards an alias, this world
        self.aliased: set = set()

    def unheard(self, question: str) -> list[tuple[int, int, str]]:
        """The question's common nouns the conversation never used, no ending of them
        either: 'the chipped dishes' where only cracked plates were told of. A proper
        name is never one, since a person never mentioned is someone nobody told of; nor
        is a word lessons held in its place every time ('do all day'), which is frame."""
        template, spans = self.shaper.template(question)
        out = []
        for i, (a, b, name) in enumerate(spans):
            words = name.split()
            if self.shaper.proper_at(question, a):
                continue
            if any(self.individuals.known(" ".join(words[j:])) for j in range(len(words))):
                continue
            seen = self.db.execute("SELECT filler, n FROM positions WHERE template = ? AND"
                                   " pos = ?", (template, i)).fetchall()
            if len(seen) == 1 and seen[0][1] >= 2:
                continue
            out.append((a, b, name))
        return out

    def aliases(self, word: str) -> list[str]:
        """The heard name a word stood for in lessons, if one: two facts' worth or more,
        since one fact cannot show two wordings agree, twice any other name's share, and
        found in more than half the lessons that found anything for the word. A word
        that is the question's frame ('shade', 'a wage') fits many names and none that
        often. The row named '' counts those lessons."""
        rows = self.db.execute("SELECT name, hits, found FROM aliases WHERE word = ? "
                               "ORDER BY name = '', hits DESC", (word,)).fetchall()
        tried = next((f for n, _, f in rows if n == ""), 0)
        rows = [r for r in rows if r[0] != ""]
        if not rows:
            return []
        name, hits, found = rows[0]
        second = rows[1][1] if len(rows) > 1 else 0
        return [name] if hits >= 2 and hits >= 2 * second and 2 * found > tried else []

    def unaliased(self, question: str, heard: bool = False) -> str:
        """The question with each word never heard put as the name it stood for, where
        lessons settled one and that name is heard here, and with `heard` where this
        conversation's hearings of it pinned one or MiniLM voted for one."""
        out, at = "", 0
        for a, b, w in self.unheard(question):
            # by its longest ending lessons settled: 'chipped dishes' before 'dishes'. A
            # name the question says already is never put again
            ws = w.split()
            for i in range(len(ws)):
                tail = " ".join(ws[i:])
                name = next((n for n in self.aliases(tail) if self.individuals.known(n)
                             and not said_in(n, out + question[at:])), None)
                if name is not None:
                    out += question[at:b - len(tail)] + name
                    at = b
                    break
            else:
                # no lesson settled it: what this conversation's hearings of it left
                name = (self.pinned(w) or self.voted(w)) if heard else None
                if name is not None and self.individuals.known(name) and not said_in(
                        name, out + question[at:]):
                    out += question[at:a] + name
                    at = b
        return out + question[at:]

    def orderings(self, question: str, spans: list) -> list[tuple[float, tuple, list]]:
        """Every reading of a question holding words never heard, each name a slot or
        frame and the slots in any order, nearest taught shapes with each. A slot left
        unbound claims less than one bound, so the readings with fewest unbound slots go
        first, nearest within that; each word is counted by the first reading whose
        solving finds anything for it, as borrowing reads a question."""
        names = self.shaper.names(question)
        out = []
        for k in range(1, len(names) + 1):
            for chosen in combinations(names, k):
                if not any(w in chosen for _, _, w in spans):
                    continue
                for order in permutations(chosen):
                    near, shapes = self.plans.nearest(question, list(order))
                    if shapes:
                        out.append((near, order, shapes))
        out.sort(key=lambda r: (sum(not self.individuals.known(n) for n in r[1]), -r[0]))
        return out

    def heard_in(self, question: str) -> None:
        """A question holding a word never heard is one context of it, answered or not:
        the names its slot could take given the question's other names, by the plans of
        the nearest taught shape with the answer left free. Each hearing keeps only the
        names every earlier one allowed, as a child narrows a new word over the
        situations it is heard in."""
        spans = self.unheard(question)
        if not spans:
            return
        names = self.shaper.names(question)
        unheard = {u for _, _, u in spans}
        done: set[str] = set()
        for _, order, shapes in self.orderings(question, spans):
            plans = [p for sh in shapes for p in self.plans.of(sh, counts=False)]
            for free, w in enumerate(order):
                if w not in unheard or w in done:
                    continue
                bound = {i: n for i, n in enumerate(order) if i != free and self.individuals.known(n)}
                if bound:
                    here = {e for p in plans for e, _ in self.solver.solve(p, bound, free)}
                else:
                    # with nothing else to pin it, a new word is narrowed by the situation
                    # it is heard in, never by every name ever heard
                    here = {n for n in self.individuals.names_in_mind()
                            if any(self.solver.solve(p, {free: n}, "a") for p in plans)}
                here -= set(names)
                if not here:
                    continue
                done.add(w)
                row = self.db.execute("SELECT names, heard FROM contexts WHERE word = ?",
                                      (w,)).fetchone()
                if row:
                    here &= set(json.loads(row[0]))
                self.db.execute("INSERT OR REPLACE INTO contexts VALUES (?, ?, ?)",
                                (w, json.dumps(sorted(here)), (row[1] if row else 0) + 1))
        self.store.written()

    def pinned(self, word: str) -> str | None:
        """The one name every hearing of a word left, where two or more agreed: one
        hearing cannot show a word names something rather than being frame, as one fact
        cannot settle an alias. Hearings that left nothing in common mark a word of the
        frame ('shade', 'a wage'), which names nothing and is never pinned."""
        row = self.db.execute("SELECT names, heard FROM contexts WHERE word = ?",
                              (word,)).fetchone()
        left = json.loads(row[0]) if row else []
        return left[0] if len(left) == 1 and row[1] >= 2 else None

    def voted(self, word: str) -> str | None:
        """Of the names a word's hearings still allow, the one MiniLM puts it nearest,
        where that is nearer than the next by `MARGIN`: what everyone knows of a word
        decides at its first hearing, and the hearings decide once they leave one name. A
        word of the frame ('shade') is about as near every name it allows, so it is never
        voted for any."""
        row = self.db.execute("SELECT names FROM contexts WHERE word = ?",
                              (word,)).fetchone()
        left = json.loads(row[0]) if row else []
        if len(left) < 2:
            return None
        w, *vs = vectors([word, *left])
        near = sorted(((float(w @ v), n) for v, n in zip(vs, left)), reverse=True)
        return near[0][1] if near[0][0] - near[1][0] >= MARGIN else None

    def alias(self, question: str, want: str) -> str:
        """A lesson naming words never heard: each is read as a slot of the nearest
        taught shape, and that shape's plans are solved with the answer bound and the
        word's slot free. Two wordings are one name where their solutions agree, so each
        name found counts a share of one, and a lesson that many names fit says little.
        A lesson that found other names counts against one. The question comes back
        with whatever lessons have settled put in."""
        spans = self.unheard(question)
        goals = self.individuals.holding(want)[:1]
        if not spans or not goals:
            return question
        names = self.shaper.names(question)
        unheard = {u for _, _, u in spans}
        found: dict[str, set] = {}
        for near, order, shapes in self.orderings(question, spans):
            plans = [p for sh in shapes for p in self.plans.of(sh)]

            for free, w in enumerate(order):
                if w not in unheard or found.get(w):
                    continue
                bound = {i: n for i, n in enumerate(order) if i != free and self.individuals.known(n)}
                bound["a"] = goals[0][2:]
                # a name the question says is never what another of its words stands for
                found[w] = {e for plan in plans if not plan.get("count")
                            for e, _ in self.solver.solve(plan, bound, free) if e not in names}
        # a name two of the words found is not evidence for either
        shared = [n for w, ends in found.items() for n in ends
                  if any(n in other for v, other in found.items() if v != w)]
        found = {w: ends - set(shared) for w, ends in found.items()}
        for w, ends in found.items():
            # one fact asked of several times is one piece of evidence
            if (w, want) in self.aliased:
                continue
            self.aliased.add((w, want))
            # credited to every ending of the word, so 'wax forms' learns from 'many wax
            # forms'
            if not ends:
                continue
            for tail in (" ".join(w.split()[i:]) for i in range(len(w.split()))):
                for n, share in [(n, 1 / len(ends)) for n in ends] + [("", 0)]:
                    self.db.execute("INSERT INTO aliases VALUES (?, ?, ?, 1) ON CONFLICT("
                                    "word, name) DO UPDATE SET hits = hits + excluded.hits, "
                                    "found = found + 1", (tail, n, share))
        return self.unaliased(question)
