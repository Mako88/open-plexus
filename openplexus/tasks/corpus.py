"""Character-level text, so something here finally sees language.

Every task in this project so far is a generator: MQAR emits random symbols,
`reward_recall` lays out bindings on a lattice. Both were built to isolate one
question, and both have a stated trivial floor because the generator knows what
guessing is worth.

**Real text has none of that.** Its statistics are not designed, its floor is a
property of the data rather than a constant, and nothing in it was placed to make
a mechanism visible. That is the point: goal 2 is a replacement for a language
model that does not need a data centre, and nothing in this repository has ever
been shown a sentence.

## What the corpus IS, stated plainly

`docs/notes` — this project's own written notes. 229,344 characters over 124
distinct symbols at the time of writing.

**It is not a standard benchmark and a number from it is not comparable to
published ones.** It is real English prose with real Zipfian statistics, it needs
no download and adds no data to the repository, and it is enough to answer the
question that actually blocks everything else: *does this memory beat a bigram at
all?* A standard corpus is a decision about what to commit to the repo and is
John's to make; this needs no decision and can run now.

Its vocabulary of 124 against MQAR's 73 also exercises the scale risk BACKLOG
names, for free.

## The split is by FILE, not by character

Splitting a single stream at an offset puts the same document on both sides, and
a character model trained on the first half of a sentence and tested on the
second half of it is being tested on itself. Whole files, disjoint.

Files are assigned by a hash of their name rather than by position, so the split
does not correlate with note number — the early notes and the late ones differ in
subject and in style, and taking the last N as test would measure drift as much
as generalisation.

## Chunking

The memory is per-sequence working state that resets between calls, so a corpus
has to be cut into chunks and the chunk length is a real parameter: it sets how
much context the store has accumulated when a prediction is made. It is
`chunk_size` and it is swept, not frozen.
"""

from __future__ import annotations

import hashlib
from dataclasses import dataclass
from pathlib import Path

import numpy as np

#: Characters rarer than this in the whole corpus become UNKNOWN. A long tail of
#: symbols appearing twice each is vocabulary the model cannot learn and cannot
#: be scored on fairly -- and it inflates `uniform_bits`, which would flatter
#: every model measured against it.
MIN_COUNT = 20
UNKNOWN = "�"


@dataclass(frozen=True)
class Corpus:
    """A character vocabulary and the documents encoded against it."""

    #: index -> character, so `len(symbols)` is the vocabulary size.
    symbols: tuple[str, ...]
    train: tuple[np.ndarray, ...]
    test: tuple[np.ndarray, ...]

    @property
    def vocab_size(self) -> int:
        return len(self.symbols)

    @property
    def train_tokens(self) -> int:
        return sum(len(d) for d in self.train)

    @property
    def test_tokens(self) -> int:
        return sum(len(d) for d in self.test)


def _is_test(name: str, share: float) -> bool:
    """Deterministic, name-based, and independent of note order.

    A hash rather than `sorted(...)[-n:]` because the notes are numbered in
    time: the last few differ from the first few in subject and style, so a
    positional split would measure drift as much as generalisation.
    """
    digest = hashlib.sha256(name.encode("utf-8")).digest()
    return int.from_bytes(digest[:4], "big") / 2 ** 32 < share


def build(texts: dict[str, str], test_share: float = 0.25,
          min_count: int = MIN_COUNT) -> Corpus:
    """Encode named documents into a train/test split over one vocabulary.

    The vocabulary is built from the TRAINING text only. Taking it from the
    whole corpus lets a symbol that appears solely in the test set occupy an
    index the model has never had a reason to predict, which is a small leak in
    the flattering direction and exactly the kind that goes unnoticed.
    """
    if not 0.0 < test_share < 1.0:
        raise ValueError(f"test_share must be in (0, 1), got {test_share}")
    train_names = [n for n in sorted(texts) if not _is_test(n, test_share)]
    test_names = [n for n in sorted(texts) if _is_test(n, test_share)]
    if not train_names or not test_names:
        raise ValueError(
            f"split left {len(train_names)} train and {len(test_names)} test "
            f"documents; one side is empty so nothing can be measured")

    counts: dict[str, int] = {}
    for name in train_names:
        for character in texts[name]:
            counts[character] = counts.get(character, 0) + 1
    kept = sorted(c for c, n in counts.items() if n >= min_count)
    symbols = (UNKNOWN,) + tuple(kept)
    index = {c: i for i, c in enumerate(symbols)}

    def encode(name: str) -> np.ndarray:
        return np.array([index.get(c, 0) for c in texts[name]], dtype=np.int64)

    return Corpus(symbols,
                  tuple(encode(n) for n in train_names),
                  tuple(encode(n) for n in test_names))


def build_stream(text: str, test_share: float = 0.1,
                 min_count: int = MIN_COUNT) -> Corpus:
    """One continuous text, split at an OFFSET rather than by document.

    **This breaks the rule the module docstring gives**, and does so
    deliberately for one case: a single-file benchmark where the published
    convention is a contiguous tail. Tiny Shakespeare, enwik8 and text8 are all
    scored that way, and matching the convention is the entire reason for using
    a standard corpus — a different split makes the number incomparable, which
    is the problem it was adopted to solve.

    The caveat the by-document rule exists to avoid is real and remains: the
    tail shares an author, a vocabulary and a register with the head. It is
    unseen text, not unseen *style*. Every published number for these corpora
    carries the same caveat, so the comparison is fair even though the absolute
    figure is easier than a truly held-out author would be.
    """
    if not 0.0 < test_share < 1.0:
        raise ValueError(f"test_share must be in (0, 1), got {test_share}")
    cut = int(len(text) * (1.0 - test_share))
    head, tail = text[:cut], text[cut:]
    if not head or not tail:
        raise ValueError(
            f"a text of {len(text)} characters at share {test_share} leaves "
            f"{len(head)} train and {len(tail)} test; one side is empty")

    counts: dict[str, int] = {}
    for character in head:
        counts[character] = counts.get(character, 0) + 1
    symbols = (UNKNOWN,) + tuple(sorted(c for c, n in counts.items()
                                        if n >= min_count))
    index = {c: i for i, c in enumerate(symbols)}

    def encode(part: str) -> np.ndarray:
        return np.array([index.get(c, 0) for c in part], dtype=np.int64)

    return Corpus(symbols, (encode(head),), (encode(tail),))


def read(directory: Path, pattern: str = "*.md") -> dict[str, str]:
    """Every matching file's text, keyed by file name."""
    found = {p.name: p.read_text(encoding="utf-8")
             for p in sorted(directory.glob(pattern))}
    if not found:
        raise ValueError(f"no files matching {pattern} in {directory}")
    return found


def chunks(documents: tuple[np.ndarray, ...], size: int) -> list[np.ndarray]:
    """Cut documents into fixed-length pieces, dropping any short remainder.

    The remainder is dropped rather than padded or kept short because chunk
    length sets how much context the store has accumulated, so a ragged final
    chunk is a systematically easier or harder sample mixed into every average.
    """
    if size < 2:
        raise ValueError(f"a chunk needs at least two tokens, got {size}")
    return [document[start:start + size]
            for document in documents
            for start in range(0, len(document) - size + 1, size)]
