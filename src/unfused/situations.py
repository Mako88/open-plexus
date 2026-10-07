"""Situations: what was being heard when each word was heard (John's, 2026-10-06).

A word's meaning here is the situations it was heard in. Each word has a vector of its
own, random but fixed by its name, so every process agrees on it without asking
(Kanerva's random indexing; the hyperdimensional fork), with its spelling's trigrams
beside it so 'poppy' and 'poppies' start near. A sentence is its words' vectors, each
bound to the link it hangs by (a rotation per link, as BEAGLE binds order), so 'the dog
chased the cat' and 'the cat chased the dog' differ.

The situation is a fold over what is heard, as event sourcing projects a stream: each
sentence moves each context a step towards itself, at a rate of its own, so a slow one
holds the story's gist and a fast one the last sentence or two, and nothing is ever
read again to know where the story is (the temporal context model, Howard and Kahana;
several timescales, as Hasson's temporal receptive windows). Each word heard adds to
its meaning, at each timescale, the context it was heard in: the sentence without it,
then each fold as it stood before the sentence. At the story's end each word heard in
it adds the story's final context too, which is what the story turned out to be about.

Every story here has much the same feel, so a context shares one direction with every
other, and read raw every word's nearest is 'fun' or 'happy'. A meaning is read as its
mean context less the mean of every context heard, so what is left is what is the
word's own.

Counted, a meaning only grows, and grows slowly (THE ORDER, item 7c). So at a story's end
the story is replayed: each word heard is predicted from the context it was heard in,
among the words the story named, and every meaning is moved by its error, the word heard
towards that context by how far short its prediction fell, each other word away by how
far it was predicted (Rescorla and Wagner's rule; meanings shaped by prediction beat
counted ones, Baroni et al. 2014). A local rule over one layer: nothing is trained
through anything else. A story predicted well teaches little.

The scales are kept apart and read apart, so what each is worth can be weighed; which
scales there are is set by what they are worth (to come). Nothing here is trained by
gradient: hearing is addition."""

from __future__ import annotations

import hashlib
import math
from collections import Counter

import numpy as np

# dimensions of every vector: enough that a few thousand words' random vectors stay
# nearly orthogonal
DIM = 1024
# how much of its last state each fold keeps as a sentence is heard; the first, 0, is
# the sentence alone. The story's end is one more scale after these
RATES = (0.0, 0.5, 0.8, 0.95)
# the share of a word's own vector its spelling's trigrams hold
SPELLING = 0.5
# how far a story's replay moves each meaning by its error, in hearings: at 1 a word
# wholly unexpected moves as far as hearing it once more did, so a meaning settles as
# hearings accumulate, as a rate of one over its hearings would; 0 replays nothing
CONSOLIDATE = 1.0
# how sharply a replay's prediction picks among the story's words (cosines, softmax)
SHARPNESS = 0.1
# where the sum of every context heard is kept, beside the labels' own
EVERY = "\x00every"

SCHEMA = """
CREATE TABLE IF NOT EXISTS situated (label TEXT PRIMARY KEY, n INTEGER NOT NULL,
    vec BLOB NOT NULL);
"""


def _seeded(key: str) -> np.ndarray:
    seed = int.from_bytes(hashlib.sha256(key.encode()).digest()[:8], "little")
    v = np.random.default_rng(seed).integers(0, 2, DIM).astype(np.float32) * 2 - 1
    return v / math.sqrt(DIM)


def _unit(v: np.ndarray) -> np.ndarray:
    n = float(np.linalg.norm(v))
    return v / n if n else v


_words: dict[str, np.ndarray] = {}


def word(w: str) -> np.ndarray:
    """A word's own vector: its name's, with its spelling's beside it."""
    got = _words.get(w)
    if got is None:
        padded = f"#{w}#"
        grams = sum((_seeded(f"g|{padded[i:i + 3]}") for i in range(len(padded) - 2)),
                    np.zeros(DIM, np.float32))
        got = _words[w] = _unit(_seeded(f"w|{w}") + SPELLING * _unit(grams))
    return got


def bound(v: np.ndarray, link: str) -> np.ndarray:
    """A vector bound to the link it hangs by: rotated by a shift fixed by the link."""
    shift = int.from_bytes(hashlib.sha256(f"r|{link}".encode()).digest()[:4], "little")
    return np.roll(v, shift % DIM)


class Situations:
    """The folds over what is heard, and each label's meaning: per scale, the sum of
    the contexts it was heard in."""

    def __init__(self, db) -> None:
        self.db = db
        db.executescript(SCHEMA)
        self.scales = len(RATES) + 1
        self.contexts = np.zeros((len(RATES), DIM), np.float32)
        self._meanings: dict[str, np.ndarray] = {}
        self._counts: dict[str, int] = {}
        self._dirty: set[str] = set()
        self._episode: Counter = Counter()
        # each word heard this story with the contexts it was heard in, for its replay
        self._told: list[tuple[str, list[np.ndarray]]] = []

    def meaning(self, label: str) -> np.ndarray:
        got = self._meanings.get(label)
        if got is None:
            row = self.db.execute("SELECT n, vec FROM situated WHERE label = ?",
                                  (label,)).fetchone()
            if row is None:
                got, n = np.zeros((self.scales, DIM), np.float32), 0
            else:
                n = row[0]
                got = np.frombuffer(row[1], np.float16).astype(np.float32).reshape(
                    self.scales, DIM).copy()
            self._meanings[label], self._counts[label] = got, n
        return got

    def sentence(self, args: list[tuple[str, str]], verbs: list[str]) -> list[np.ndarray]:
        """Each part of a sentence: its arguments bound to their links, then its verbs."""
        return [bound(word(label), link) for link, label in args] + [word(v) for v in verbs]

    def heard(self, args: list[tuple[str, str]], verbs: list[str]) -> None:
        """A sentence told: each argument's label adds the contexts it was heard in, and
        then the sentence is folded into every context."""
        if not args and not verbs:
            return
        parts = self.sentence(args, verbs)
        whole = np.sum(parts, axis=0)
        for i, (_, label) in enumerate(args):
            m = self.meaning(label)
            heard_in = [_unit(whole - parts[i])] + [_unit(self.contexts[k])
                                                    for k in range(1, len(RATES))]
            for k, c in enumerate(heard_in):
                m[k] += c
            self._told.append((label, heard_in))
            self._counts[label] += 1
            self._dirty.add(label)
            self._episode[label] += 1
            every = self.meaning(EVERY)
            every[0] += _unit(whole - parts[i])
            for k in range(1, len(RATES)):
                every[k] += _unit(self.contexts[k])
            self._counts[EVERY] += 1
            self._dirty.add(EVERY)
        s = _unit(whole)
        for k, r in enumerate(RATES):
            self.contexts[k] = r * self.contexts[k] + (1 - r) * s

    def ended(self) -> None:
        """A story's end: each label heard in it adds, once a hearing, the slowest
        context as it ended, then every context starts again."""
        gist = _unit(self.contexts[-1])
        for label, n in self._episode.items():
            self.meaning(label)[-1] += n * gist
            self.meaning(EVERY)[-1] += n * gist
        if CONSOLIDATE:
            self.replay()
        self._told.clear()
        self._episode.clear()
        self.contexts[:] = 0
        self.save()

    def replay(self) -> None:
        """The story replayed: each word heard predicted, at each scale it was heard in,
        among the words the story named, from the context it was heard in, and each
        meaning moved by its error. Not the story's end: every word of a story was heard
        in that one context, so it cannot say which of them was heard, and replayed it
        only moved the story's words against one another (063405Z). The predictions are
        read from the meanings as the story left them, so the order of the replay changes
        nothing."""
        labels = sorted({label for label, _ in self._told})
        if len(labels) < 2:
            return
        at = {label: i for i, label in enumerate(labels)}
        mean = self.meaning(EVERY) / max(1, self._counts[EVERY])
        heard = [max(1, self._counts[label]) for label in labels]
        for k in range(len(RATES)):
            known = np.stack([_unit(self.meaning(label)[k] / n - mean[k])
                              for label, n in zip(labels, heard)])
            moved = np.zeros_like(known)
            for label, heard_in in self._told:
                c = _unit(heard_in[k] - mean[k])
                said = known @ c / SHARPNESS
                guess = np.exp(said - said.max())
                guess /= guess.sum()
                error = -guess
                error[at[label]] += 1
                moved += np.outer(error, c)
            for label, step in zip(labels, moved):
                self.meaning(label)[k] += CONSOLIDATE * step
                self._dirty.add(label)

    def fit(self, labels: list[str], args: list[tuple[str, str]],
            verbs: list[str]) -> dict[str, list[float]]:
        """How each label fits the situation now, with a sentence of these parts heard
        next: per scale, the cosine of its meaning there with the context it would be
        heard in. The sentence alone at the first scale; each fold as the sentence
        would move it after; the slowest fold again against what stories it ended in
        were about."""
        whole = np.sum(self.sentence(args, verbs), axis=0) if args or verbs else \
            np.zeros(DIM, np.float32)
        s = _unit(whole)
        now = [s] + [_unit(r * self.contexts[k] + (1 - r) * s)
                     for k, r in enumerate(RATES) if k] + [None]
        now[-1] = now[-2]
        every = self.meaning(EVERY)
        mean = every / max(1, self._counts[EVERY])
        here = [_unit(now[k] - mean[k]) for k in range(self.scales)]
        out = {}
        for label in labels:
            m, n = self.meaning(label), self._counts[label]
            out[label] = [float(_unit(m[k] / n - mean[k]) @ here[k]) if n else 0.0
                          for k in range(self.scales)]
        return out

    def save(self) -> None:
        self.db.executemany(
            "INSERT INTO situated VALUES (?, ?, ?) ON CONFLICT(label) DO UPDATE SET "
            "n = excluded.n, vec = excluded.vec",
            [(label, self._counts[label], self._meanings[label].astype(np.float16).tobytes())
             for label in self._dirty])
        self._dirty.clear()
