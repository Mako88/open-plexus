"""Hearer: a sentence heard becomes events and edges in the graph.

A turn holding no words is a break in the text, and what is heard after it is another episode.
Any other turn is read into events; each thing it names becomes an individual (one opened
here, or the one in mind it joins), each event is written with an edge to each of its
arguments, and what the sentence touched is dropped from the caches that kept it. Its id is
where it was first heard, so a run reproduces it.
"""

from __future__ import annotations

from unfused.individuals import Individuals
from unfused.mind import Mind
from unfused.reader import Reader
from unfused.situations import Situations
from unfused.storage import Store
from unfused.walker import Walker


class Hearer:
    def __init__(self, store: Store, reader: Reader, mind: Mind, individuals: Individuals,
                 walker: Walker, situations: Situations) -> None:
        self.store = store
        self.db = store.db
        self.reader = reader
        self.mind = mind
        self.individuals = individuals
        self.walker = walker
        self.situations = situations

    def hear(self, turn: int, text: str) -> None:
        # a turn holding no words is a break in the text ('***', a new page): what is
        # heard after it is another episode
        if not any(ch.isalnum() for ch in text):
            self.db.execute("INSERT OR IGNORE INTO boundaries VALUES (?)", (turn,))
            self.situations.ended()
            self.store.written(True)
            self.mind.broke()
            return
        # the episode's index: each word it held, so a cue can find it again
        ep = self.mind.episode()
        new = [(w, ep) for w in set(self.reader.words(text))]
        self.db.executemany("INSERT OR IGNORE INTO episode_words VALUES (?, ?)", new)
        self.mind.keep_words(ep, [w for w, _ in new])
        events = self.reader.read(text)
        resolved = {t: self.individuals.referent(t[2:]) for ev in events for _, t in ev["edges"]
                    if t.startswith("p:")}
        # each thing the sentence names, as an individual: one opened here, or the one
        # in mind it joins. Its id is where it was first heard, so a run reproduces it
        mentions: dict[str, list] = {}
        for ev in events:
            mentions.update(ev.get("things", {}))
        fresh = self.db.execute("SELECT COUNT(*) FROM called WHERE turn = ?",
                                (turn,)).fetchone()[0]
        who: dict[str, str] = {}
        here: list[tuple[str, set, str]] = []
        for m, (name, opens, mods, pos) in sorted(mentions.items(),
                                                  key=lambda kv: int(kv[0])):
            mods = [w for _, w in mods]
            self.db.execute("INSERT INTO heard_as VALUES (?, ?, 1) ON CONFLICT(name, pos) "
                            "DO UPDATE SET n = n + 1", (name, pos))
            node = None
            if not opens:
                # one named earlier in this sentence first, then the episode's
                node = next((n for lab, said, n in here if lab == name and set(mods) <= said),
                            None) or self.individuals.in_mind(name, mods)
            if node is None:
                node, fresh = f"i:{turn}.{fresh}", fresh + 1
                self.db.execute("INSERT INTO called VALUES (?, ?, ?)", (node, name, turn))
                self.store.opened(node)
            here.append((name, set(mods), node))
            who[m] = node
        ids = []
        # the words of what this sentence touched: a name or a description holding none
        # of them stands for what it stood for before
        touched: set[str] = set()
        # the sentence's arguments by the links they hang by, for the situations
        heard: list[tuple[str, str]] = []
        for ev in events:
            ids.append(self.store.new_event(turn, ev["lemma"], text, ev["mood"]))
        for ev, eid in zip(events, ids):
            for (label, t), m in zip(ev["edges"], ev.get("mentions", [None] * len(ev["edges"]))):
                node = (f"e:{ids[int(t[2:])]}" if t.startswith("e:") else
                        who[str(m)] if m is not None else resolved.get(t, t))
                if node is None:
                    continue
                self.store.add_edge(eid, label, node)
                # what is said of any node of this event may have changed
                self.individuals.forget_said(node)
                for _, other in self.store.outs(eid):
                    self.individuals.forget_said(other)
                if node[:2] in ("i:", "n:"):
                    name = self.individuals.label(node)
                    if name:
                        heard.append((label, name))
                    self.db.execute("INSERT INTO filled VALUES (?, ?, ?, 1) ON CONFLICT(name, "
                                    "lemma, link) DO UPDATE SET n = n + 1",
                                    (name, ev["lemma"], label))
                    self.db.executemany("INSERT OR IGNORE INTO named VALUES (?, ?)",
                                        [(w, name) for w in name.split()])
                self.walker.forget(node)
                self.walker.forget(f"e:{eid}")
                self.individuals.forget_described(node)
                if (name := self.individuals.label(node)) is not None:
                    self.walker.forget(f"n:{name}")
                    self.individuals.forget_heard_as(name)
                    touched.update(name.split())
        self.situations.heard(heard, [ev["lemma"] for ev in events])
        self.walker.happened(list(zip(events, ids)), turn)
        # a name or a description holding a word this sentence touched may stand for
        # something else now, and its steps are its individuals'
        for name in self.individuals.forget_words(touched):
            if " " in name:
                self.walker.forget(f"n:{name}")
        for t, node in resolved.items():
            if node is not None and t.startswith("p:"):
                # agreement is what everyone knows, so it is counted by label
                self.db.execute("INSERT INTO agreement VALUES (?, ?, 1) ON CONFLICT(name, "
                                "pronoun) DO UPDATE SET n = n + 1",
                                (f"n:{self.individuals.label(node)}", t[2:].split("|", 1)[1]))
                self.individuals.forget_agreed(self.individuals.label(node))
        self.store.written()
