"""Shaper: a question as a template with its names cut out, and as a shape with its slots.

A question's noun phrases are cut out of it (`template`), and those that are slots rather than
words of its frame are numbered (`shape`): the shape is what lessons are kept under. A place in
a template whose word varies across lessons is a slot; one that held the same word every time,
twice or more, is frame. `signature` is a question's parse as a set of grammar triples, so two
wordings of one question share what their grammar shares.
"""

from __future__ import annotations

from unfused.individuals import Individuals
from unfused.reader import Reader


class Shaper:
    def __init__(self, db, reader: Reader, individuals: Individuals) -> None:
        self.db = db
        self.reader = reader
        self.individuals = individuals

    def template(self, question: str) -> tuple[str, list]:
        """The question with every noun phrase cut out, and the phrases. A phrase is
        cut to the longest ending of it the graph knows: 'how many walking sticks' holds
        'walking sticks', with no list of words like 'many'."""
        spans = []
        for a, b, name, verb_at, verb in sorted(self.reader.names_in(question)):
            words = name.split()
            for i in range(len(words)):
                tail = " ".join(words[i:])
                if self.individuals.known(tail):
                    at = question.lower().find(tail, a)
                    if at >= 0:
                        # the words before it are a name of their own where the graph
                        # knows them: 'Who is Silfem cousins with?' is parsed as one
                        # compound, 'Silfem cousins', as 'the Smith cousins' would be
                        head = " ".join(words[:i])
                        if head and self.individuals.known(head) and (
                                h := question.lower().find(head, a)) >= 0 and h < at:
                            spans.append((h, h + len(head), head))
                        a, b, name = at, at + len(tail), tail
                    break
            # 'the person who repairs clocks': a name's own verb, said just before it, is
            # cut with it, as the taught arm's `joined` does, so every trade is one shape
            held = self.individuals.nodes(name)
            if verb and self.db.execute(
                    "SELECT 1 FROM edges JOIN events ON events.id = edges.event WHERE "
                    f"edges.node IN ({','.join('?' * len(held))}) AND events.lemma = ? "
                    "LIMIT 1", (*held, verb)).fetchone():
                a = verb_at
            spans.append((a, b, name))
        out, at = "", 0
        for i, (a, b, _) in enumerate(spans):
            out += question[at:a] + f"<{i}>"
            at = b
        return out + question[at:], spans

    def proper_at(self, text: str, at: int) -> bool:
        """Whether the word a text has at this offset is a proper name, by the part of
        speech the parse gives it: a capital letter is a mark of some scripts only."""
        return any(r[0] <= at < r[0] + len(r[1]) and r[5] == "PROPN"
                   for r in self.reader.spans(text))

    def slot(self, template: str, pos: int, name: str, capital: bool) -> bool:
        """Whether a noun phrase is a slot of its question or a word of its frame. A
        place whose word varies across lessons of one template is a slot; one that held
        the same word every time, twice or more, is frame ('Whose cousin is X?', 'do
        for a living'); one never taught is a slot if the graph knows the name."""
        seen = self.db.execute("SELECT filler, n FROM positions WHERE template = ? AND"
                               " pos = ?", (template, pos)).fetchall()
        if len(seen) == 1 and seen[0][1] >= 2:
            return False
        # a place that varies holds a slot only where its word names something here:
        # 'colour' and 'shade' share a place across lessons and name nothing
        return self.individuals.known(name) or capital

    def shape(self, question: str) -> tuple[str, list[str]]:
        template, every = self.template(question)
        spans = [(a, b, n) for i, (a, b, n) in enumerate(every)
                 if self.slot(template, i, n, self.proper_at(question, a))]
        shape, fillers, at = "", [], 0
        for a, b, n in sorted(spans):
            shape += question[at:a] + f"<{len(fillers)}>"
            fillers.append(n)
            at = b
        return shape + question[at:], fillers

    def heard_at(self, question: str) -> None:
        """A lesson's noun phrases counted by their place in its template."""
        template, spans = self.template(question)
        for i, (_, _, n) in enumerate(spans):
            self.db.execute("INSERT INTO positions VALUES (?, ?, ?, 1) ON CONFLICT"
                            "(template, pos, filler) DO UPDATE SET n = n + 1",
                            (template, i, n))

    def signature(self, question: str, fillers: list[str]) -> list[str]:
        """A question's parse as a set of (word, link, head) triples and words, each name
        replaced by its slot and each wh-word kept as itself, so two wordings of one
        question share what their grammar shares: 'Where are <0>'s <1> kept?' and 'Where
        does <0> keep the <1>?' share 'keep', 'where' and 'where advmod keep'."""
        tokens = self.reader.tokens(question)
        heads = {f.split()[-1]: i for i, f in enumerate(fillers)}
        inside = {w for f in fillers for w in f.split()[:-1]}

        def label(t) -> str | None:
            lower, lemma, tag, dep, _ = t
            if lower in heads:
                return f"<{heads[lower]}>"
            if lower in inside or dep in ("punct", "det", "aux", "case"):
                return None
            return lower if tag == "ask" else lemma

        out = set()
        for i, t in enumerate(tokens):
            me = label(t)
            if me is None:
                continue
            out.add(me)
            head = label(tokens[t[4]]) if t[4] != i else "ROOT"
            if head is not None:
                out.add(f"{me} {t[3]} {head}")
        return sorted(out)

    def wh_word(self, question: str) -> str:
        """The question's first word that asks, by what the parse marks, or ''."""
        return next((t[0] for t in self.reader.tokens(question) if t[2] == "ask"), "")
