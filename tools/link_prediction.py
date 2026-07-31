"""FB15k-237 link prediction: the data, the filter, the ranking and the metrics, once.

**What this does not duplicate — it DE-duplicates.** `experiments/g30_01_link_prediction.py`
held all of this inline. `g30-02` needs the identical protocol for a different scorer, and
two copies of a filtered-ranking convention is precisely the shape rule 9 warns about: a
fix applied to one copy leaves the other producing plausible numbers forever. So the
protocol moved here and `g30_01` now imports it.

`tools/fetch_fb15k237.py` still owns the data and its checksums, and is not touched.

## The one interface

A scorer is `f(heads, relations) -> (len(heads), n_entities)`, batched. The raw store and a
learned embedding both express themselves that way, which is why the protocol can be shared
without either arm knowing the other exists.

## The conventions, stated once so they cannot drift

    TRAIN ONLY      entity and relation vocabularies, and anything learned, come from
                    `train.txt`. Nothing reads `valid` or `test` except the filter
    FILTERED        other known-true tails for `(h, r)` are removed from the ranking,
                    using all three splits. This is the standard convention and is NOT
                    the same as training on them
    BOTH PRINTED    unfiltered is always returned beside filtered, because a number that
                    does not say which it is, is not a number
    UNANSWERABLE    a test triple whose head, relation or tail never appeared in train is
                    counted and reported, never silently dropped
"""

from __future__ import annotations

from collections import defaultdict
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / "data" / "fb15k237"
#: Test triples scored per matrix multiply. Large enough that numpy dominates the loop
#: overhead, small enough that the score matrix stays well under a gigabyte.
CHUNK = 512


def triples(split: str) -> list[tuple[str, str, str]]:
    rows = []
    for line in (DATA / f"{split}.txt").read_text(encoding="utf-8").splitlines():
        parts = line.split("\t")
        if len(parts) == 3:
            rows.append((parts[0], parts[1], parts[2]))
    return rows


class Task:
    """The dataset, the vocabularies and the filter. Built from TRAIN, filtered by all."""

    def __init__(self) -> None:
        self.train = triples("train")
        entities = sorted({e for h, _, t in self.train for e in (h, t)})
        relations = sorted({r for _, r, _ in self.train})
        self.ent = {e: i for i, e in enumerate(entities)}
        self.rel = {r: i for i, r in enumerate(relations)}

        self.known: dict = defaultdict(set)
        for split in ("train", "valid", "test"):
            for h, r, t in triples(split):
                if h in self.ent and r in self.rel and t in self.ent:
                    self.known[(self.ent[h], self.rel[r])].add(self.ent[t])

        self.heads, self.rels, self.tails = [], [], []
        self.unanswerable = 0
        for h, r, t in triples("test"):
            if h not in self.ent or r not in self.rel or t not in self.ent:
                self.unanswerable += 1
                continue
            self.heads.append(self.ent[h])
            self.rels.append(self.rel[r])
            self.tails.append(self.ent[t])
        self.heads = np.array(self.heads)
        self.rels = np.array(self.rels)
        self.tails = np.array(self.tails)

    @property
    def n_entities(self) -> int:
        return len(self.ent)

    def train_indices(self):
        """`(heads, relations, tails)` as index arrays, for a learner."""
        return (np.array([self.ent[h] for h, _, _ in self.train]),
                np.array([self.rel[r] for _, r, _ in self.train]),
                np.array([self.ent[t] for _, _, t in self.train]))

    def frequency_scorer(self):
        """The cheap opponent: how often each entity is a tail of this relation.

        No learning, no capacity, no width. It is here rather than in an experiment
        because **a baseline recomputed per run cannot be carried from another
        configuration**, which is the failure `g9-11` cost 0.58 of recovery to.
        """
        counts = np.zeros((len(self.rel), len(self.ent)))
        for h, r, t in self.train:
            counts[self.rel[r], self.ent[t]] += 1.0
        return lambda heads, rels: counts[rels]

    def evaluate(self, scorer) -> dict:
        """Ranks for one scorer: `filtered` / `unfiltered`, each OPTIMISTIC and TIED.

        **Ties are not a detail here, they decide the comparison.** A rank of
        `1 + how many candidates strictly outscore the true tail` puts every tie in
        the true tail's favour, so a scorer returning a constant ranks everything 1st
        and reads as perfect. `frequency` is exactly the shape that exploits this:
        it ignores the head, and most entities are never a tail of a given relation,
        so thousands of candidates sit at score 0 together.

        So both ends are returned for every arm and neither is chosen here:

            optimistic   1 + (strictly greater)   ties count as wins
            pessimistic  count of (>= true)       ties count as losses

        A scorer whose two numbers differ is a scorer whose result depends on the tie
        convention, and that has to be visible in the table rather than settled by
        whoever wrote the ranking loop.
        """
        out: dict = {k: [] for k in ("filtered", "unfiltered",
                                     "filtered_pessimistic", "unfiltered_pessimistic")}
        for start in range(0, len(self.heads), CHUNK):
            stop = start + CHUNK
            heads = self.heads[start:stop]
            rels = self.rels[start:stop]
            tails = self.tails[start:stop]
            scores = np.asarray(scorer(heads, rels), dtype=float)
            best, tied = ranks(scores, tails)
            out["unfiltered"].extend(best)
            out["unfiltered_pessimistic"].extend(tied)
            for row, (h, r, t) in enumerate(zip(heads, rels, tails)):
                others = self.known[(int(h), int(r))] - {int(t)}
                if others:
                    scores[row, np.fromiter(others, dtype=int,
                                            count=len(others))] = -np.inf
            best, tied = ranks(scores, tails)
            out["filtered"].extend(best)
            out["filtered_pessimistic"].extend(tied)
        return {k: np.array(v, dtype=float) for k, v in out.items()}


def ranks(scores: np.ndarray, targets) -> tuple[np.ndarray, np.ndarray]:
    """`(optimistic, pessimistic)` rank of each target within its row of `scores`.

    Split out of `Task.evaluate` so it can be tested without `data/`, which is
    gitignored and therefore absent in CI -- the same reason
    `tests/test_relation_contrastive.py` works on synthetic triangles.

    **Optimistic counts ties as wins and pessimistic counts them as losses.** For a
    scorer that separates candidates cleanly the two agree; for one that produces
    large tied blocks they diverge enormously, and which one is quoted then decides
    the comparison rather than reporting it.
    """
    rows = np.arange(len(targets))
    true = scores[rows, np.asarray(targets)][:, None]
    return ((scores > true).sum(axis=1) + 1, (scores >= true).sum(axis=1))


def metrics(ranks: np.ndarray) -> tuple[float, float, float, float]:
    """`(MRR, Hits@1, Hits@3, Hits@10)`."""
    return (float((1.0 / ranks).mean()), float((ranks <= 1).mean()),
            float((ranks <= 3).mean()), float((ranks <= 10).mean()))


def header() -> str:
    return f"{'arm':<22}{'MRR':>9}{'Hits@1':>9}{'Hits@3':>9}{'Hits@10':>9}"


def row(name: str, ranks: np.ndarray) -> str:
    mrr, h1, h3, h10 = metrics(ranks)
    return f"{name:<22}{mrr:>9.4f}{h1:>9.4f}{h3:>9.4f}{h10:>9.4f}"
