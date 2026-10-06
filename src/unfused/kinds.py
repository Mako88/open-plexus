"""Concepts: kinds as the fixed point of relations and things (THE ORDER, the
foundation's item 3). Two labels are of a kind where they take part in alike relations
with things of alike kinds, so 'red' and 'blue' fall together by how they attach to
balls and hats, and 'ball' and 'hat' by how people have them and colours describe them.
No label names a kind; naming one later is one more label.

As colour refinement (Weisfeiler-Lehman) does, every label and every verb starts in one
kind, and each round reads a label's contexts by the kinds of what it meets, then each
verb's by the kinds of the labels it joins, until no kind moves: two relations are alike
where they join alike kinds (co-clustering). A label's contexts are read at three grains:
its link and the other argument's, then with the verb's kind, then with the other
argument's kind too. The coarsest is kept in every round, so a round splits a kind only
where the finer contexts outweigh what the coarse one shows; refined alone, the kinds
shattered with every round at 300 stories (from 348 kinds to 1403 of 1575 labels), since
two nouns rarely share a verb, while their links alone already put Lily with Tom, red
with blue and the park with the garden, and Lily and red apart from the ball. A label is the counts of its contexts, each weighted by how few labels share it
(as a rare word weighs more in retrieval), and two labels are alike by cosine. Exact
refinement splits a label heard in one context fewer than its kind-mates, and set
overlap punishes a label heard in many.

Kinds are formed as individuals are: the labels heard most go first, and each joins the
kind it is likest, at `NEAR` or more, else opens one of its own."""

import math
from collections import Counter, defaultdict

# how alike a label must be to a kind to join it (cosine)
NEAR = 0.5
# rounds of refinement at most; it stops sooner where nothing moves
ROUNDS = 4
# how much each finer grain of context weighs beside the one coarser
GRAIN = 0.5
# how alike two kinds must be, as wholes, to be merged into one (cosine): the likeness a
# label needs to join a kind; None merges none
MERGE = NEAR


def kinds(rows, near: float = NEAR, rounds: int = ROUNDS,
          merge: float | None = MERGE) -> dict[str, int]:
    """Each label's kind, from (event, lemma, link, label) for every argument of every
    event that names something."""
    by_event: dict = defaultdict(list)
    for event, lemma, link, label in rows:
        by_event[event].append((lemma, link, label))
    heard = Counter(label for _, _, _, label in rows)
    labels = sorted(heard, key=lambda w: (-heard[w], w))
    said = Counter(lemma for _, lemma, _, _ in rows)
    verbs = sorted(said, key=lambda v: (-said[v], v))
    kind = {w: 0 for w in labels}
    relation = {v: 0 for v in verbs}
    for _ in range(rounds):
        contexts: dict[str, Counter] = defaultdict(Counter)
        for args in by_event.values():
            for lemma, link, label in args:
                for _, other, w in args:
                    if w != label or other != link:
                        r = relation[lemma]
                        contexts[label][(link, other)] += 1
                        contexts[label][(r, link, other)] += 1
                        contexts[label][(r, link, other, kind[w])] += 1
        nxt = _cluster(labels, contexts, near, merge)
        joins: dict[str, Counter] = defaultdict(Counter)
        for args in by_event.values():
            for lemma, link, label in args:
                joins[lemma][(link, nxt[label])] += 1
        relation_nxt = _cluster(verbs, joins, near, merge)
        if _same(kind, nxt) and _same(relation, relation_nxt):
            break
        kind, relation = nxt, relation_nxt
    return kind


def _cluster(labels: list[str], contexts: dict, near: float,
             merge: float | None = None) -> dict[str, int]:
    holders = Counter(c for w in labels for c in contexts[w])
    n = len(labels)

    def vector(w: str) -> dict:
        # each grain normalised on its own and weighted, coarse first, so the many rare
        # fine contexts refine the coarse likeness rather than drown it
        v = {c: math.log1p(k) * math.log(1 + n / holders[c]) for c, k in contexts[w].items()}
        grains: dict = defaultdict(float)
        for c, x in v.items():
            grains[len(c)] += x * x
        out = {c: GRAIN ** (len(c) - 2) * x / math.sqrt(grains[len(c)]) for c, x in v.items()}
        norm = math.sqrt(sum(x * x for x in out.values())) or 1.0
        return {c: x / norm for c, x in out.items()}

    centroids: list[dict] = []
    norms: list[float] = []
    squares: list[float] = []
    # each context's kinds, so a label is compared only with kinds it shares one with
    where: dict = defaultdict(set)
    out: dict[str, int] = {}
    vectors: dict = {}
    for w in labels:
        v = vectors[w] = vector(w)
        dots: dict[int, float] = defaultdict(float)
        for c, x in v.items():
            for k in where[c]:
                dots[k] += x * centroids[k][c]
        best = max(dots, key=lambda k: (dots[k] / norms[k], -k), default=None)
        if best is None or dots[best] / norms[best] < near:
            best = len(centroids)
            centroids.append({})
            norms.append(0.0)
            squares.append(0.0)
        cen = centroids[best]
        for c, x in v.items():
            cen[c] = cen.get(c, 0.0) + x
            where[c].add(best)
        # |cen + v|^2 from what is already known, never by summing the kind again
        squares[best] += 2 * dots.get(best, 0.0) + sum(x * x for x in v.values())
        norms[best] = math.sqrt(squares[best])
        out[w] = best
    return out if merge is None else _merged(labels, vectors, out, merge)


def _merged(labels: list[str], vectors: dict, kind: dict[str, int],
            merge: float) -> dict[str, int]:
    """Kinds merged where they are alike as wholes: one pass opens a kind for a label
    unlike every kind so far and never looks back, so two kinds that grew alike stay
    apart, as a toddler's first words do until they are seen to be used alike. Each
    round, every kind whose likest is likest back, at `merge` or more, is merged with it
    (a matching, so no chain runs through the middling), until none is."""
    import numpy as np
    from scipy.sparse import csr_matrix

    # the labels' vectors as one matrix, built once; a kind's vector is the sum of its
    # labels', so each round only regroups the rows
    index: dict = {}
    rows, cols, vals = [], [], []
    for r, w in enumerate(labels):
        for c, x in vectors[w].items():
            rows.append(r)
            cols.append(index.setdefault(c, len(index)))
            vals.append(x)
    by_label = csr_matrix((vals, (rows, cols)), shape=(len(labels), len(index)))
    while True:
        ids = sorted(set(kind.values()))
        at = {k: i for i, k in enumerate(ids)}
        into_kind = csr_matrix((np.ones(len(labels)), ([at[kind[w]] for w in labels],
                                                       range(len(labels)))),
                               shape=(len(ids), len(labels)))
        m = into_kind @ by_label
        norms = np.sqrt(np.asarray(m.multiply(m).sum(axis=1)).ravel())
        norms[norms == 0] = 1.0
        m = csr_matrix(m.multiply(1 / norms[:, None]))
        sim = (m @ m.T).toarray()
        np.fill_diagonal(sim, -1.0)
        best = sim.argmax(axis=1)
        into = {}
        for i, j in enumerate(best):
            if best[j] == i and i < j and sim[i, j] >= merge:
                into[ids[j]] = ids[i]
        if not into:
            break
        kind = {w: into.get(k, k) for w, k in kind.items()}
    # numbered by the first label of each, as one pass numbers them
    first: dict = {}
    for w in labels:
        first.setdefault(kind[w], len(first))
    return {w: first[kind[w]] for w in labels}


def _same(a: dict, b: dict) -> bool:
    """Whether two partitions are one, whatever the kinds are numbered."""
    pairs = {}
    for w in a:
        if pairs.setdefault(a[w], b[w]) != b[w]:
            return False
    return len(set(a.values())) == len(set(b.values()))
