"""Recounter: questions whose answer is something that happened (THE ORDER, the mouth;
John's, 2026-10-08, from FairytaleQA).

'What did the fisherman do for a living?' is answered by an event, 'caught fish for the
king's table', which no name holds. Which questions want one is learnt, never read off a
word: each lesson counts, under the question's frame (the verb and link its wh-word
fills, ('do', 'obj')), whether the teacher's answer was a told event or a thing. A
reaction's answer is an event where it holds a verb that a told event of the episode was
told with. Where a frame's lessons say events, the question is answered by the told event
of the episode, one with a subject, whose sentence shares most of the question's words,
the latest on a tie, never one told with the frame's own verb.
"""

from __future__ import annotations

from unfused.mind import Mind
from unfused.parsing import parse
from unfused.reader import Reader
from unfused.storage import Store

# how many lessons a frame needs before its questions are answered with events
LESSONS = 2


class Recounter:
    def __init__(self, store: Store, reader: Reader, mind: Mind) -> None:
        self.store = store
        self.db = store.db
        self.reader = reader
        self.mind = mind
        self.db.execute("CREATE TABLE IF NOT EXISTS recounts (lemma TEXT NOT NULL, link TEXT "
                        "NOT NULL, events INTEGER NOT NULL, things INTEGER NOT NULL, "
                        "PRIMARY KEY (lemma, link))")

    def candidates(self) -> list[tuple[int, str, str]]:
        """The told events of this episode that have a subject, as (id, lemma, sentence):
        what someone or something did. Not only those of the question's names, since what
        a name did is told by pronoun as often as not ('he unfastened the chains')."""
        return self.db.execute(
            "SELECT id, lemma, heard FROM events WHERE turn > ? AND EXISTS (SELECT 1 FROM "
            "edges WHERE event = events.id AND label LIKE 'nsubj%') ORDER BY id",
            (self.mind.episode(),)).fetchall()

    def learn(self, question: str, reaction: str, said: str) -> bool:
        """Count a lesson under the question's frame; whether its answer was an event."""
        frame = self.reader.blank(question)
        if frame is None:
            return False
        if said.startswith("e:") and not any(t.pos_ == "VERB" for t in parse(
                self.reader.model, reaction)):
            # a confirmation of an event given
            event = True
        else:
            verbs = {t.lemma_.lower() for t in parse(self.reader.model, reaction)
                     if t.pos_ == "VERB"}
            event = bool(verbs) and any(lemma in verbs and lemma != frame[0]
                                        for _, lemma, _ in self.candidates())
        self.db.execute("INSERT INTO recounts VALUES (?, ?, ?, ?) ON CONFLICT(lemma, link) DO "
                        "UPDATE SET events = events + ?, things = things + ?",
                        (*frame, int(event), int(not event), int(event), int(not event)))
        return event

    def answer(self, question: str) -> str | None:
        """The event a question whose frame wants one is answered by, as 'e:<id>'."""
        frame = self.reader.blank(question)
        if frame is None:
            return None
        got = self.db.execute("SELECT events, things FROM recounts WHERE lemma = ? AND link = ?",
                              frame).fetchone()
        if got is None or got[0] < LESSONS or got[0] <= got[1]:
            return None
        # the words the question shares with the event's sentence, its frame's verb aside
        asked = set(self.reader.words(question)) - {frame[0]}
        best = max(((len(asked & set(self.reader.words(text))), event)
                    for event, lemma, text in self.candidates() if lemma != frame[0]),
                   default=None)
        return None if best is None else f"e:{best[1]}"
