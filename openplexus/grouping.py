"""Turning content vectors into GROUPS, which is what a concept map needs.

## The problem this is built for, stated as g17-01 measured it

At word level the store is addressed by word PAIRS, and almost every address is
seen once. 1,733 words make tens of thousands of distinct pairs over 90,000
training words, so the store memorises hapaxes instead of superposing anything
reusable — the model lands 1.40 bits *worse* than a word unigram and there is no
headroom for any mechanism to occupy.

**The address space is too large and too thin, and `concepts.Shared` is the seam
that can shrink it.** If a concept is a GROUP of words rather than one word, the
address space collapses — a few hundred concepts instead of 1,733 words — and
each address recurs many times. The readout still predicts surfaces, so nothing
changes on the output side: **store by concept, emit by word.**

`ContentIndex` already says which words keep similar company. What did not exist
is the step from those vectors to a partition of the vocabulary. That is this
file.

## Why spherical k-means, and what it is NOT

`ContentIndex.nearest` scores by dot product on unit vectors, so cosine is the
space's own metric and the clustering should use the same one. Spherical k-means
is that: assign by largest cosine, re-centre on the mean, renormalise.

It is chosen for being the plainest thing that respects the metric, not for being
the best clusterer. **Nothing here claims the groups are good** — whether a
grouping means anything is what the shuffled control in the sweep is for, and a
better algorithm is a drop-in replacement behind the same signature.

## The honest cost: this is a GLOBAL step, and the first one

Every other component in this project is local by construction — a node is handed
token ids and computes. **This is not.** A grouping is a property of the whole
vocabulary's vectors at once, and two nodes that clustered different vectors
would disagree about which concept a word belongs to, sending a write and a read
to different machines with no error anywhere (`concepts.of` must be *pure and
agreed*).

That does not by itself break C1, which forbids a *read* that needs every node.
This is periodic and off the read path — the same shape as agreeing on a
vocabulary, or on `ContentIndex.count`'s frequencies. But it is a synchronisation
requirement the architecture did not have before, it is named here rather than
discovered later, and **the cost of maintaining agreement under churn is not
measured** by anything in this file.
"""

from __future__ import annotations

import numpy as np


def cluster(vectors: np.ndarray, k: int, seed: int = 0,
            iterations: int = 25) -> list[list[int]]:
    """Group the rows of `vectors` into at most `k` clusters by cosine.

    Args:
        vectors: One unit-length row per token, as `ContentIndex.vectors`
            returns. **Zero rows are tokens never observed** and are left out
            of every group — they have no content to group by, and putting them
            all together would build one large concept out of the fact that
            nothing is known about any of them.
        k: How many groups to aim for. Fewer come back when clusters empty.
        seed: Fixes the initialisation. The result must not depend on anything
            else, so that two nodes holding the same vectors produce the same
            grouping without exchanging a message.
        iterations: Lloyd steps. Assignment is checked for a fixed point and the
            loop stops early, so this is a bound rather than a schedule.

    Returns:
        Groups of token ids, each sorted, ordered by lowest member. Every token
        appears in at most one group; tokens left out are the caller's to treat
        as concepts of their own — which is exactly what `concepts.Shared` does
        with a token no group claims.
    """
    if k < 1:
        raise ValueError("a grouping into no groups has nowhere to put a token")
    vectors = np.asarray(vectors, dtype=float)
    if vectors.ndim != 2:
        raise ValueError(f"expected one row per token, got shape "
                         f"{vectors.shape}")
    live = np.flatnonzero(np.linalg.norm(vectors, axis=1) > 0.0)
    if live.size == 0:
        return []
    points = vectors[live]
    k = min(k, live.size)

    centres = _seeded(points, k, seed)
    assignment = np.full(live.size, -1)
    for _ in range(iterations):
        fresh = np.argmax(points @ centres.T, axis=1)
        if np.array_equal(fresh, assignment):
            break
        assignment = fresh
        for index in range(k):
            members = points[assignment == index]
            if members.size == 0:
                # AN EMPTY CLUSTER IS RE-SEEDED, NOT DROPPED MID-LOOP.
                #
                # Dropping it would change `k` inside the iteration and make the
                # result depend on which pass emptied it. Re-seeding on the point
                # currently worst served keeps the count fixed and is the
                # standard repair; the cluster empties again at the end if
                # nothing wants it, and the final pass drops it there.
                worst = int(np.argmin(np.max(points @ centres.T, axis=1)))
                centres[index] = points[worst]
                continue
            centre = members.mean(axis=0)
            norm = float(np.linalg.norm(centre))
            # A cluster whose members cancel out exactly has no direction to
            # point in. Keeping the old centre is stable and lets the next pass
            # empty it, where normalising a zero vector would raise.
            if norm > 0.0:
                centres[index] = centre / norm

    groups = [sorted(int(live[i]) for i in np.flatnonzero(assignment == index))
              for index in range(k)]
    return sorted((group for group in groups if group), key=lambda g: g[0])


def _seeded(points: np.ndarray, k: int, seed: int) -> np.ndarray:
    """k-means++ initialisation, on cosine distance.

    Plain random initialisation on a space with mean off-diagonal cosine 0.5
    puts several centres in the same crowd, and spherical k-means does not
    recover from that — it empties the duplicates and returns fewer, larger
    groups than asked for, which is indistinguishable from the data having no
    structure. Spreading the initial centres is what makes an empty result mean
    something.
    """
    rng = np.random.default_rng(seed)
    chosen = [int(rng.integers(len(points)))]
    # Cosine similarity to the nearest chosen centre, so `1 - closest` is the
    # distance k-means++ samples proportionally to the square of.
    closest = points @ points[chosen[0]]
    while len(chosen) < k:
        weight = np.maximum(1.0 - closest, 0.0) ** 2
        total = float(weight.sum())
        if total <= 0.0:
            # Every remaining point is identical to a centre already chosen.
            # Filling the rest arbitrarily would be inventing structure; taking
            # them in order is at least deterministic and the clusters they
            # start will empty on the first pass.
            chosen.extend(i for i in range(len(points))
                          if i not in chosen)
            break
        nxt = int(rng.choice(len(points), p=weight / total))
        chosen.append(nxt)
        closest = np.maximum(closest, points @ points[nxt])
    return points[chosen[:k]].copy()
