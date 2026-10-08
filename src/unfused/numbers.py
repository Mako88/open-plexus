"""Numbers: what each number word is worth, learnt from lessons.

Nothing here knows English: a word is a number where lessons paired it with a count of the
same walk every time (see `numbers`).
"""

from __future__ import annotations


class Numbers:
    def __init__(self, db) -> None:
        self.db = db

    def number(self, text: str) -> int | None:
        """A number said as digits, or as a word whose value lessons settled."""
        t = text.strip().lower()
        if t.isdigit():
            return int(t)
        return self.values().get(t)

    def say_number(self, n: int) -> str:
        """A number in the word lessons said it with, or as digits."""
        return next((w for w, v in self.values().items() if v == n), str(n))

    def values(self) -> dict[str, int]:
        """What each number word is worth, learnt as a child learns to count: over the
        lessons whose answer was a word nothing heard holds, one walk's count went with
        each word, each word always with the same count and each count with the same
        word. That walk is the counting, and the words' values are its counts. Two words
        at least, since one cannot show a pairing."""
        total = self.db.execute("SELECT COUNT(*) FROM counts").fetchone()[0]
        if getattr(self, "_numbers", (None,))[0] == total:
            return self._numbers[1]
        by: dict = {}
        for shape, walk, word, size in self.db.execute("SELECT * FROM counts"):
            by.setdefault((shape, walk), []).append((word, size))
        best: tuple = (0, {})
        for items in by.values():
            fwd: dict = {}
            back: dict = {}
            if all(fwd.setdefault(w, n) == n and back.setdefault(n, w) == w
                   for w, n in items) and len(fwd) >= 2 and len(items) > best[0]:
                best = (len(items), fwd)
        self._numbers = (total, best[1])
        return best[1]
