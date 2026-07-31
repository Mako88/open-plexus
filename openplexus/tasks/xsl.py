"""Cross-situational word learning trials, read from someone else's experiments.

Every instrument this project has measured a grounding claim on was written here,
and `DECISIONS.md` §10 names that as the standing weakness. These trials are not:
they are the stimuli from published cross-situational word-learning experiments,
collected in `kachergis/XSLmodels` and fetched by `tools/fetch_kachergis.py`.

## What a trial IS, and why it is the same problem

A line names the word-object pairs shown on one trial:

    5   12   1

means **three words and three objects were presented, unpaired.** The participant
sees all six and is told nothing about which goes with which, so on that trial
word 5 is equally consistent with objects 5, 12 and 1. Only the fact that word 5
keeps turning up with object 5 *across* trials separates them.

That is exactly the mechanism in `openplexus/grounding.py`, posed by
psychologists in 2008–2013 rather than by this project in 2026.

## The ground truth needs no human data, which is what makes this usable

Pair `n` means word `n` and object `n`, so the correct mapping is known from the
file. **Human accuracies are NOT available** — of 64 rows in the dataset table, 8
carry one, and not one of those 8 names an ordering that ships as text. So this
gives external *stimuli* and not an external *benchmark*, and the difference is
worth keeping in front of anyone quoting a number from it.

## What this does NOT duplicate, and what was searched

Searched by capability — trial, ordering, co-occurrence stream, external corpus —
across `openplexus/`, `openplexus/tasks/`, `tools/`, `tests/` and `experiments/`.

- **`openplexus/tasks/occasions.py` GENERATES a stream with knobs; this READS
  one that already exists.** Nothing here has a `presence`, a `zipf` or a
  distractor to turn, because the trials are fixed — and that is the point, since
  a knob is how a designed instrument gets made solvable. The two share an output
  shape deliberately, so the same recovery code scores both, and neither
  generates what the other reads.
- **`openplexus/tasks/corpus.py`** reads external text for a next-token
  objective, which `GOALS.md` §2 excludes as a score. This reads trials for a
  relational one.
- **`tools/fetch_clutrr.py`, `fetch_openea.py`, `fetch_fb15k237.py`** fetch other
  external data; `tools/fetch_kachergis.py` is the sibling for this one and this
  module does no fetching.

## Dependency-free, like the rest of `tasks/`

Per `CLAUDE.md`: this layer is the ruler and the ruler takes no dependencies. A
trial file is whitespace-separated integers, so nothing more is needed.
"""

from __future__ import annotations

import pathlib


class Condition:
    """One published condition: its trials, and what the right answer is.

    A word and an object are both given surface ids so the recovery code can
    treat them as it treats any other surfaces — **it is never told which is
    which**, and a mechanism that needed to be told would be answering an easier
    question than the participants were asked.

    Attributes:
        name: The condition's filename stem, as the dataset table names it.
        trials: Tuples of surface ids, one per trial. The set a mechanism sees.
        pairs: How many distinct word-object pairs the condition teaches.
    """

    def __init__(self, name: str, lines: list[list[int]]) -> None:
        if not lines:
            raise ValueError(f"{name} has no trials")
        ids = sorted({value for line in lines for value in line})
        if ids != list(range(min(ids), max(ids) + 1)):
            raise ValueError(
                f"{name} names pair ids with gaps ({min(ids)}..{max(ids)}), and "
                f"a gap means the file is not the format this reads")
        self.name = name
        self._base = min(ids)
        self.pairs = len(ids)
        self.trials = tuple(
            tuple(sorted([self.word(v) for v in line]
                         + [self.object(v) for v in line]))
            for line in lines)

    def word(self, pair: int) -> int:
        """Surface id for the WORD of a pair."""
        return pair - self._base

    def object(self, pair: int) -> int:
        """Surface id for the OBJECT of a pair."""
        return self.pairs + pair - self._base

    def classes(self) -> dict[int, frozenset[int]]:
        """The correct answer: each word with its own object, and nothing else."""
        answer: dict[int, frozenset[int]] = {}
        for index in range(self.pairs):
            both = frozenset({index, self.pairs + index})
            answer[index] = both
            answer[self.pairs + index] = both
        return answer

    def surfaces(self) -> int:
        """Words plus objects."""
        return 2 * self.pairs

    def appearances(self) -> dict[int, int]:
        """How many trials each pair appears on.

        Some conditions vary this deliberately — the `freq369` family shows pairs
        3, 6 or 9 times — which is the axis `g32-02` found normalisation matters
        on, arriving here from an experiment designed to test something else.
        """
        counts: dict[int, int] = {}
        for trial in self.trials:
            for surface in trial:
                if surface < self.pairs:
                    counts[surface] = counts.get(surface, 0) + 1
        return counts


def read(path: pathlib.Path) -> Condition:
    """Read one trial-ordering file."""
    lines = [[int(value) for value in line.split()]
             for line in path.read_text(encoding="utf-8").splitlines()
             if line.strip()]
    return Condition(path.stem, lines)


def available(root: pathlib.Path) -> list[pathlib.Path]:
    """Every fetched ordering, in a fixed order.

    Sorted rather than globbed-as-found, because a directory listing is not a
    stable order and a sweep that reports rows in filesystem order reports a
    different table on a different machine.
    """
    return sorted(root.glob("*.txt"))
