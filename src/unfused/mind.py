"""Mind: the episode being heard and what is in mind with it.

An episode is what was heard since the last break in the text. A break puts every recalled
episode away; recall by a cue puts an earlier one back beside the current one, and what is in
mind is the current episode and those. The episode index (each word's episodes, each episode's
words) lives here, read from its table once and added to as sentences are heard. What keeps a
cache of what was in mind (a name's steps) listens for a change with `listen`.
"""

from __future__ import annotations

import math
from collections.abc import Callable

from unfused.reader import Reader

# a word more than this share of episodes held says nothing of which one a cue means, and
# is not looked up
COMMON = 0.2


class Mind:
    def __init__(self, db, reader: Reader) -> None:
        self.db = db
        self.reader = reader
        # episodes put back in mind beside the one heard now, each as (its break's turn,
        # its last turn, its first event, its last event, how far its turns are moved
        # on so it reads as heard just now); a break puts them all away
        self.recalled: list[tuple[int, int, int, int, int]] = []
        # the episode index in memory, as the table has it: each word's episodes oldest
        # first (as an array beside, once read), and each episode's words in the order they
        # were first written; read from the table once, then added to as sentences are heard
        self._posted: dict = {}
        self._episode_words: dict = {}
        self._listeners: list[Callable[[], None]] = []

    def listen(self, heard: Callable[[], None]) -> None:
        """Call `heard` whenever what is in mind changes by a break or a recall."""
        self._listeners.append(heard)

    def changed(self) -> None:
        for heard in self._listeners:
            heard()

    def broke(self) -> None:
        """A break in the text: every recalled episode is put away."""
        self.recalled = []
        self.changed()

    def now(self) -> int:
        return self.db.execute("SELECT COALESCE(MAX(turn), 0) FROM events").fetchone()[0]

    def first_event(self) -> int:
        """The first event of this episode, so what reads the episode starts there and
        never looks back through the history before it."""
        ep = self.episode()
        if getattr(self, "_first", (None,))[0] != ep:
            row = self.db.execute("SELECT MIN(id) FROM events WHERE turn > ?", (ep,)).fetchone()
            if row[0] is None:
                # an episode with no event yet: nothing before now is in it, and what is
                # heard next is, so this is never kept
                return 1 << 62
            self._first = (ep, row[0])
        return self._first[1]

    def episode(self) -> int:
        """The turn of the last break in the text: what was heard after it is in focus."""
        return self.db.execute("SELECT COALESCE(MAX(turn), -1) FROM boundaries").fetchone()[0]

    def recall(self, lo: int, hi: int) -> None:
        """Put an episode back in mind beside the one heard now: what was heard after
        the break at turn `lo`, through turn `hi`. Its hearings read as if just heard,
        and a mention joins its individuals as it joins this episode's."""
        first, last = self.db.execute("SELECT MIN(id), MAX(id) FROM events WHERE turn > ? "
                                      "AND turn <= ?", (lo, hi)).fetchone()
        if first is None:
            return
        self.recalled.append((lo, hi, first, last, self.now() - hi))
        self.changed()

    def held(self, turn: int) -> bool:
        """Whether a turn is in mind: in this episode or one recalled."""
        return turn > self.episode() or any(lo < turn <= hi for lo, hi, *_ in self.recalled)

    def moved(self, turn: int) -> int:
        """A turn as it reads in mind: a recalled episode's as if it ended just now."""
        for lo, hi, _, _, shift in self.recalled:
            if lo < turn <= hi:
                return turn + shift
        return turn

    def within(self, column: str, events: bool = False) -> str:
        """SQL for a turn (or, with `events`, an event id) in mind, read with `bounds`."""
        here = f"{column} >= ?" if events else f"{column} > ?"
        return "(" + " OR ".join([here] + [f"{column} BETWEEN ? AND ?"] * len(self.recalled)) + ")"

    def bounds(self, events: bool = False) -> tuple:
        out = [self.first_event() if events else self.episode()]
        for lo, hi, first, last, _ in self.recalled:
            out += [first, last] if events else [lo + 1, hi]
        return tuple(out)

    def events_in_mind(self, lemma: str, mood: str, limit: int) -> list[int]:
        """The latest events of a verb in this mood that are in mind, newest first."""
        return [e for (e,) in self.db.execute(
            f"SELECT id FROM events WHERE lemma = ? AND mood = ? AND {self.within('turn')} "
            "ORDER BY id DESC LIMIT ?", (lemma, mood, *self.bounds(), limit))]

    def words_of(self, episode: int) -> list[str]:
        """The words an episode held, in the order they were first written."""
        got = self._episode_words.get(episode)
        if got is None:
            got = self._episode_words[episode] = [w for (w,) in self.db.execute(
                "SELECT word FROM episode_words WHERE episode = ?", (episode,))]
        return got

    def episodes_of(self, word: str) -> list:
        """The episodes that held a word, oldest first, and the same as an array."""
        got = self._posted.get(word)
        if got is None:
            import numpy as np

            rows = [e for (e,) in self.db.execute(
                "SELECT episode FROM episode_words WHERE word = ? ORDER BY episode", (word,))]
            got = self._posted[word] = [rows, np.array(rows, dtype=np.int64)]
        return got

    def keep_words(self, episode: int, words: list[str]) -> None:
        """Words just written for an episode, added to what is kept in memory."""
        mine = self._episode_words.get(episode)
        for w in words:
            if mine is not None and w not in mine:
                mine.append(w)
            got = self._posted.get(w)
            if got is not None and (not got[0] or got[0][-1] != episode):
                import numpy as np

                got[0].append(episode)
                got[1] = np.append(got[1], episode)

    def remind(self, question: str) -> None:
        """Recall by a cue: an earlier episode that explains what this one heard and the
        question better than this one does is put back in mind (hippocampal indexing).
        Each word weighs by how few episodes held it, and a word most episodes held is
        not looked up, so recall costs a few index lookups and never a scan. A story
        being heard explains its own questions best, so this fires only when what is
        asked is not here."""
        import numpy as np

        ep = self.episode()
        here = {w for w in self.words_of(ep)}
        cue = here | set(self.reader.words(question))
        episodes = self.db.execute("SELECT COUNT(*) FROM boundaries").fetchone()[0]
        if not cue or episodes < 2:
            return
        weight = {}
        for word in sorted(cue):
            n = len(self.episodes_of(word)[0])
            if n and n <= episodes * COMMON:
                weight[word] = math.log((episodes + 1) / (n + 1))
        if not weight:
            return
        mine = sum(weight.get(w, 0.0) for w in here)
        # each episode's score, summed word by word in the order the words are read, as
        # the rows came; a word names an episode once, so one add per episode is the sum
        scores = np.zeros(ep + 1)
        for word, w in weight.items():
            eps = self.episodes_of(word)[1]
            eps = eps[eps != ep]
            scores[eps] += w
        held = {lo for lo, *_ in self.recalled}
        # an earlier episode that explains the cue only as well as this one still holds
        # what this one lacks: a retold sentence is all of the episode it came from, and
        # a question adding nothing rare leaves the two tied. The latest wins a tie
        top = scores.max()
        if top > 0:
            score, other = float(top), int(np.flatnonzero(scores == top)[-1])
            if score < mine - 1e-9 or other in held:
                return
            end = self.db.execute("SELECT MIN(turn) FROM boundaries WHERE turn > ?",
                                  (other,)).fetchone()[0]
            self.recall(other, (end - 1) if end is not None else self.now())
