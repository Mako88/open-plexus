"""The mouth: an answer said in the words it was heard in (THE ORDER, the graphed arm,
item 2; John's, 2026-10-08, from FairytaleQA).

The answer the system finds is a node, said by its label ('wretched hut'). A person asked
'Where did the poor woman live?' says the phrase they heard it in ('in a wretched hut far
away from the village'). So the mouth finds the told event the answer rests on, among
those in mind that hold it, the one sharing most with the question, the latest on a tie,
and says the answer's mention there whole: its own words and every word hanging from it,
in the order heard, its preposition with it. An answer that is an event ('What did the
fisherman do?') is said as its predicate, the verb and what hangs from it less its
subject, the clauses joined to it and its punctuation ('caught fish for the king's
table'). It composes nothing yet: a reply is one fragment of one heard sentence, which is
data-oriented parsing's first step.
"""

from __future__ import annotations

from unfused.parsing import extract, parse

# what a predicate is said without: its subject, the clauses joined to or set beside it,
# and the words that join them, by the links the parse gives in any language
APART = ("nsubj", "nsubj:pass", "csubj", "conj", "cc", "punct", "mark", "parataxis",
         "advcl", "discourse", "vocative")


class Mouth:
    def __init__(self, arm) -> None:
        self.arm = arm

    def say(self, question: str, answer: str) -> str:
        """The answer as heard, or its label where no told event in mind holds it."""
        if answer.startswith("e:"):
            return self.predicate(int(answer[2:])) or "I don't know."
        found = self.heard(question, answer)
        return found or answer

    def sentence(self, event: int):
        """The parse of the sentence an event was heard in, and the event as extracted."""
        arm = self.arm
        row = arm.db.execute("SELECT turn, heard FROM events WHERE id = ?", (event,)).fetchone()
        if row is None:
            return None, None
        turn, text = row
        # the event's place among its sentence's events, which are written in order
        ids = [e for (e,) in arm.db.execute(
            "SELECT id FROM events WHERE turn = ? AND heard = ? ORDER BY id", (turn, text))]
        doc = parse(arm.model, text)
        events = extract(doc)
        if len(ids) != len(events) or event not in ids:
            return None, None
        return doc, events[ids.index(event)]

    def predicate(self, event: int) -> str | None:
        doc, ev = self.sentence(event)
        if doc is None:
            return None
        head = doc[ev["head"]]
        keep = {head.i}
        for c in head.children:
            if c.dep_ not in APART:
                keep.update(t.i for t in c.subtree)
        return "".join(doc[i].text_with_ws for i in sorted(keep)).strip()

    def heard(self, question: str, answer: str) -> str | None:
        arm = self.arm
        asked = set(arm.reader.words(question))
        best, best_key = None, None
        for node in arm.individuals.of(answer, episode=True) + [f"n:{answer}"]:
            for event, label in arm.store.edges_into(node):
                row = arm.db.execute("SELECT turn, heard FROM events WHERE id = ?",
                                     (event,)).fetchone()
                if row is None or not arm.mind.held(row[0]):
                    continue
                key = (len(asked & set(arm.reader.words(row[1]))), event)
                if best_key is None or key > best_key:
                    best, best_key = (event, label, node, row), key
        if best is None:
            return None
        event, label, node, _ = best
        doc, ev = self.sentence(event)
        if doc is None:
            return None
        mention = next((m for (lab, t), m in zip(ev["edges"], ev["mentions"])
                        if lab == label and m is not None
                        and ev["things"].get(str(m), [None])[0] == answer), None)
        if mention is None:
            return None
        tok = doc[mention]
        return doc[tok.left_edge.i:tok.right_edge.i + 1].text
