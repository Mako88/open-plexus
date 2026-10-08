"""The mouth: an answer said in the words it was heard in (THE ORDER, the graphed arm,
item 2; John's, 2026-10-08, from FairytaleQA).

The answer the system finds is a node, said by its label ('wretched hut'). A person asked
'Where did the poor woman live?' says the phrase they heard it in ('in a wretched hut far
away from the village'). So the mouth finds the told event the answer rests on, among
those in mind that hold it, the one sharing most with the question, the latest on a tie,
and says the answer's mention there whole: its own words and every word hanging from it,
in the order heard, its preposition with it. It composes nothing yet: a reply is one
fragment of one heard sentence, which is data-oriented parsing's first step.
"""

from __future__ import annotations

from unfused.parsing import extract, parse


class Mouth:
    def __init__(self, arm) -> None:
        self.arm = arm

    def say(self, question: str, answer: str) -> str:
        """The answer as heard, or its label where no told event in mind holds it."""
        found = self.heard(question, answer)
        return found or answer

    def heard(self, question: str, answer: str) -> str | None:
        arm = self.arm
        asked = set(arm.words(question))
        best, best_key = None, None
        for node in arm.individuals(answer, episode=True) + [f"n:{answer}"]:
            for event, label in arm.store.edges_into(node):
                row = arm.db.execute("SELECT turn, heard FROM events WHERE id = ?",
                                     (event,)).fetchone()
                if row is None or not arm.held(row[0]):
                    continue
                key = (len(asked & set(arm.words(row[1]))), event)
                if best_key is None or key > best_key:
                    best, best_key = (event, label, node, row), key
        if best is None:
            return None
        event, label, node, (turn, text) = best
        # the event's place among its sentence's events, which are written in order
        ids = [e for (e,) in arm.db.execute(
            "SELECT id FROM events WHERE turn = ? AND heard = ? ORDER BY id", (turn, text))]
        doc = parse(arm.model, text)
        events = extract(doc)
        if len(ids) != len(events) or event not in ids:
            return None
        ev = events[ids.index(event)]
        mention = next((m for (lab, t), m in zip(ev["edges"], ev["mentions"])
                        if lab == label and m is not None
                        and ev["things"].get(str(m), [None])[0] == answer), None)
        if mention is None:
            return None
        tok = doc[mention]
        return doc[tok.left_edge.i:tok.right_edge.i + 1].text
