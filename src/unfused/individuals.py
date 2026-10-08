"""Individuals: what a name or an id stands for.

A name is a label and an individual is an id (`i:<turn>.<n>`) opened where it was first
heard, so a name stands for the individuals it labels, the latest first. This holds how an
individual is called and said (its label, its description, what was said of it), which
individual a mention joins, what a pronoun refers to and what pronouns a label has been
called. Each is read from the tables once and kept; the graph above says when what it
touched is no longer so (`forget_*`).
"""

from __future__ import annotations

from unfused.said import said_in


class Individuals:
    def __init__(self, db, mind) -> None:
        self.db = db
        self.mind = mind
        # every name's mark, dropped only for names a sentence names
        self._heard: dict = {}
        # each individual's label and how it is said, and each name's individuals
        self._labels: dict = {}
        self._described: dict = {}
        self._said: dict = {}
        self._nodes: dict = {}
        # each word's names and descriptions held in those caches, so a sentence drops
        # only those sharing a word with what it touched, never by scanning them all
        self._words: dict[str, set] = {}
        # each label's agreement with pronouns
        self._agreed: dict = {}

    def indexed(self, name: str) -> None:
        """A name whose steps or individuals are kept is found again by each of its words."""
        for w in name.split():
            self._words.setdefault(w, set()).add(name)

    def finds(self, name: str) -> set:
        """Every node a name stands for, as a set kept beside `nodes`."""
        self.nodes(name)
        return self._nodes[("set", name)]

    def forget_said(self, node: str) -> None:
        self._said.pop(node, None)

    def forget_described(self, node: str) -> None:
        self._described.pop(node, None)

    def forget_heard_as(self, name: str) -> None:
        self._heard.pop(name, None)

    def forget_agreed(self, label: str | None) -> None:
        self._agreed.pop(label, None)

    def forget_words(self, touched: set[str]) -> set[str]:
        """A name or a description holding a word a sentence touched may stand for something
        else now: those are dropped, and returned so what is kept by name can be too."""
        names = {n for w in touched for n in self._words.pop(w, ())}
        for name in names:
            self._nodes.pop(name, None)
            self._nodes.pop(("set", name), None)
        return names

    def label(self, node: str) -> str | None:
        """What a node is called: a name's own words, an individual's label, and None for
        an event."""
        if node.startswith("n:"):
            return node[2:]
        if not node.startswith("i:"):
            return None
        got = self._labels.get(node)
        if got is None:
            got = self._labels[node] = self.db.execute(
                "SELECT name FROM called WHERE node = ? ORDER BY turn DESC LIMIT 1",
                (node,)).fetchone()[0]
        return got

    def nodes(self, name: str) -> list[str]:
        """Everything a name stands for: the name itself, where it is held as a word
        ('red', '3'), and each individual it labels, the latest first. An index, so a
        walk from a name reaches every individual it labels without holding a copy.
        Where no individual has the name as its label, it is read as a description:
        the longest run of its words that is a label, and the individuals of that label
        of which every other word was said ('still room' finds the room said to be
        still, in whatever order the words came)."""
        got = self._nodes.get(name)
        if got is None:
            got = [f"n:{name}"] + self.of(name)
            words = name.split()
            for size in range(len(words) - 1, 0, -1) if len(got) == 1 else ():
                found = []
                for at in range(len(words) - size + 1):
                    rest = set(words[:at] + words[at + size:])
                    found += [n for n in self.of(" ".join(words[at:at + size]))
                              if rest <= set(self.said_of(n))]
                if found:
                    got += found
                    break
            self._nodes[name] = got
            self._nodes[("set", name)] = set(got)
            for w in name.split():
                self._words.setdefault(w, set()).add(name)
        return got


    def said_of(self, node: str) -> list[str]:
        """What was said of an individual, the latest first: the adjectives it was heard
        with, and what a clause made it or called it ('painted the ball blue', 'the ball
        was red'), by the links the parse gives those. Kept until an event holding the node
        is given another edge."""
        got = self._said.get(node)
        if got is None:
            got = self._said[node] = [n[2:] for (n,) in self.db.execute(
                "SELECT b.node FROM edges a JOIN edges b ON a.event = b.event WHERE a.node = ? "
                "AND b.label IN ('amod', 'compound', 'flat', 'acomp', 'xcomp') "
                "AND b.node LIKE 'n:%' GROUP BY b.node ORDER BY MAX(b.event) DESC", (node,))]
        return list(got)

    def describe(self, node: str) -> str:
        """How an individual is said: its own words in the order first heard, then its
        label ('still room'), so an answer tells apart what its label alone would not."""
        if not node.startswith("i:"):
            return self.label(node) or node
        got = self._described.get(node)
        if got is None:
            mods = [n[2:] for (n,) in self.db.execute(
                "SELECT b.node FROM edges a JOIN edges b ON a.event = b.event WHERE "
                "a.node = ? AND a.label = 'self' AND b.label IN ('amod', 'compound', 'flat') "
                "GROUP BY b.node "
                "ORDER BY MIN(b.event)", (node,))]
            got = self._described[node] = " ".join(mods + [self.label(node)])
        return got

    def stands_for(self, node: str, goal: str) -> bool:
        """Whether a node is the goal, or an individual the goal's name finds."""
        if node == goal:
            return True
        if not (goal.startswith("n:") and node.startswith("i:")):
            return False
        self.nodes(goal[2:])
        return node in self._nodes[("set", goal[2:])]

    def visited(self, node: str, nodes) -> bool:
        """Whether a walk has been at a node already: at it, or at a name or description
        that finds it, so a walk from 'lily' does not come back through a Lily."""
        return node in nodes or (node.startswith("i:") and (
            f"n:{self.label(node)}" in nodes or f"n:{self.describe(node)}" in nodes))

    def in_mind(self, name: str, mods=()) -> str | None:
        """The individual a mention joins: the one with this label, opened in this
        episode, mentioned latest, of which every adjective the mention has was said
        already. Until concepts say which adjectives exclude each other, one never said
        of it is read as contradicting it, so an unsure mention opens rather than
        merges. Across episodes nothing joins until joining is learnt."""
        for (node,) in self.db.execute(
                "SELECT edges.node FROM edges JOIN called ON called.node = edges.node WHERE "
                f"called.name = ? AND {self.mind.within('called.turn')} GROUP BY edges.node "
                "ORDER BY MAX(edges.event) DESC LIMIT 20", (name, *self.mind.bounds())):
            if set(mods) <= set(self.said_of(node)):
                return node
        return None

    def of(self, name: str, episode: bool = False) -> list[str]:
        """The individuals a name labels, the latest first, this episode's alone with
        `episode`."""
        if not episode:
            return [n for (n,) in self.db.execute(
                "SELECT node FROM called WHERE name = ? ORDER BY turn DESC", (name,))]
        return [n for (n,) in self.db.execute(
            f"SELECT node FROM called WHERE name = ? AND {self.mind.within('turn')} ORDER BY "
            "turn DESC", (name, *self.mind.bounds()))]

    def holding(self, word: str) -> list[str]:
        """The names that hold a word whole: 'ochre colour' for 'ochre', and a
        description that finds something: 'still room'."""
        first = word.split()[0] if word.split() else word
        out = [f"n:{n}" for (n,) in self.db.execute(
            "SELECT name FROM named WHERE word = ?", (first,)) if said_in(word, n)]
        if f"n:{word}" not in out and self.known(word):
            out.append(f"n:{word}")
        return out


    def names_in_mind(self) -> list[str]:
        """The names heard in this episode: of the individuals opened in it, and the
        words its events hold themselves."""
        return [n for (n,) in self.db.execute(
            f"SELECT name FROM called WHERE {self.mind.within('turn')} UNION SELECT "
            f"substr(node, 3) FROM edges WHERE {self.mind.within('event', events=True)} AND node "
            "LIKE 'n:%'", (*self.mind.bounds(), *self.mind.bounds(events=True)))]

    def known(self, name: str) -> bool:
        return len(self.nodes(name)) > 1 or self.db.execute(
            "SELECT 1 FROM edges WHERE node = ? LIMIT 1", (f"n:{name}",)).fetchone() is not None

    def heard_as(self, name: str) -> str | None:
        """How a name is heard: the part of speech its mentions were given most ('PROPN'
        for Lily, 'NOUN' for the ball), or None where nothing was heard of it by that
        name. A capital letter is a mark of some scripts only."""
        if name not in self._heard:
            self._heard[name] = self._heard_as(name)
        return self._heard[name]

    def _heard_as(self, name: str) -> str | None:
        row = self.db.execute("SELECT pos FROM heard_as WHERE name = ? ORDER BY n DESC "
                              "LIMIT 1", (name.split()[-1] if name.strip() else "",)
                              ).fetchone()
        return row[0] if row else None

    def agreed(self, name: str) -> dict:
        """What pronouns a label has been called, and how often."""
        got = self._agreed.get(name)
        if got is None:
            got = self._agreed[name] = dict(self.db.execute(
                "SELECT pronoun, n FROM agreement WHERE name = ?", (f"n:{name}",)).fetchall())
        return got

    def referent(self, pronoun: str) -> str | None:
        """What a pronoun refers to, from this episode: the latest individual in the slot
        it fills, a subject for a subject and a thing that was not one for anything else,
        as parallelism in centering has it. Given as its slot and what it agrees in
        ('nsubj|Gender=Fem|Number=Sing|Person=3'), by what the parse marks. A label that
        has been called something else and never this is passed over, so agreement is
        learnt from what pronouns have been resolved to."""
        slot, agrees = pronoun.split("|", 1)
        subject = slot.startswith("nsubj")
        unsure = None
        for node, label in self.db.execute(
                "SELECT node, label FROM edges WHERE "
                f"{self.mind.within('event', events=True)} AND node LIKE 'i:%' "
                "ORDER BY event DESC LIMIT 200", self.mind.bounds(events=True)):
            if label == "self" or (self.parallel and label.startswith("nsubj") != subject):
                continue
            called = self.agreed(self.label(node))
            if called and agrees not in called and max(called.values()) >= 2:
                continue
            if not self.agreeing_first or (called and agrees in called):
                return node
            unsure = unsure or node
        return unsure

    # arms under comparison (PreCo, 2026-10-08): whether a pronoun looks only among
    # things in its own slot, and whether one known to agree comes before a later one
    # never called anything
    parallel = True
    agreeing_first = False
