"""Focuser: the name in this episode that best fits the asked slot.

A question that asks for a thing is guessed at from what is in focus: each name's activation
(each hearing fading as ACT-R's base level does), times how often it has filled the asked verb's
slot out of everything it has filled, times how often the question's wh-word asked for its mark.
`focus` keeps the three factors apart so an instrument can weigh them.
"""

from __future__ import annotations

from unfused.individuals import Individuals
from unfused.meeter import DECAY
from unfused.mind import Mind
from unfused.reader import Reader
from unfused.said import said_in
from unfused.shaper import Shaper

# how many hearings a name's fit is shrunk by towards nothing, so one hearing is not a
# certainty
PRIOR = 2.0


class Focuser:
    def __init__(self, db, reader: Reader, mind: Mind, individuals: Individuals,
                 shaper: Shaper) -> None:
        self.db = db
        self.reader = reader
        self.mind = mind
        self.individuals = individuals
        self.shaper = shaper

    def in_focus(self, name: str) -> bool:
        held = self.individuals.nodes(name)
        return self.db.execute(
            "SELECT 1 FROM edges JOIN events ON events.id = edges.event WHERE edges.node IN "
            f"({','.join('?' * len(held))}) AND {self.mind.within('events.turn')} LIMIT 1",
            (*held, *self.mind.bounds())).fetchone() is not None

    def focus(self, question: str) -> dict[str, tuple[float, float, float]] | None:
        """Each name focus weighs for a question, with its three factors: how strongly it
        is in focus, how it fits the asked slot, and how often the wh-word asks for its
        mark. None where the question is not guessed at."""
        slot = self.reader.blank(question)
        # a question about someone never mentioned is not guessed at
        if slot is None or not all(self.individuals.known(f) for f in self.shaper.shape(question)[1]):
            return None
        now = self.mind.now()
        act: dict[str, float] = {}
        for node, turn in self.db.execute(
                "SELECT edges.node, events.turn FROM edges JOIN events ON events.id = "
                f"edges.event WHERE {self.mind.within('edges.event', events=True)} AND edges.node "
                "NOT LIKE 'e:%' AND edges.node NOT LIKE 'f:%'", self.mind.bounds(events=True)):
            act[node] = act.get(node, 0.0) + (now - self.mind.moved(turn) + 1) ** -DECAY
        labels = self.db.execute("SELECT COUNT(DISTINCT link) FROM filled").fetchone()[0] or 1

        def fit(name: str) -> float:
            # a share of what the name filled: in the asked verb's slot, and, much less,
            # by the link alone, so a slot no one here filled still ranks
            rows = self.db.execute("SELECT link, lemma = ?, n FROM filled WHERE name = ?",
                                   (slot[0], name)).fetchall()
            total = sum(n for _, _, n in rows)
            here = sum(n for lab, verb, n in rows if lab == slot[1] and verb)
            link = sum(n for lab, _, n in rows if lab == slot[1])
            return (here / (total + PRIOR)
                    + 0.1 * (link + 0.5) / (total + 0.5 * labels))

        asks = dict(self.db.execute("SELECT mark, n FROM asks WHERE wh = ?",
                                    (self.shaper.wh_word(question),)).fetchall())

        def asked_for(name: str) -> float:
            # how often this wh-word's answers bore the name's mark, as lessons have it
            mark = self.individuals.mark(name)
            return 0.5 if mark is None else (asks.get(mark, 0) + 1) / (sum(asks.values()) + 2)

        # the question's own things are not its answer: each of its names finds the
        # individual most in mind of those it labels, as a reader's 'the pig' finds the
        # pig the story is about, whatever more was said of it. A word that is no
        # individual ('red') is its own description
        own = set()
        for f in self.shaper.shape(question)[1]:
            found = [n for n in self.individuals.nodes(f) if n in act and n.startswith("i:")]
            if found:
                own.add(max(found, key=act.get))
        # in mind as an individual, said by its description; how it fits and what it
        # is asked for are what everyone knows, so they are read by its label
        out: dict[str, tuple[float, float, float]] = {}
        for node, a in act.items():
            said, name = self.individuals.describe(node), self.individuals.label(node)
            if node in own or (not node.startswith("i:") and said_in(said, question)):
                continue
            was = out.get(said)
            out[said] = (a + (was[0] if was else 0.0), fit(name), asked_for(name))
        return out
