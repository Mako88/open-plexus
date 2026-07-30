"""Does relational structure alone say which entities are the same concept?

## The question and why it is this benchmark

`concepts.Merged` expresses a merge and never decides one. The proposed evidence is
**interchangeability** — two surfaces are the same concept if they relate the same way to
the same things — which note 070 measured working for RELATIONS and note 076 could not test
for ENTITIES, because CLUTRR gives each entity one or two edges and two surfaces then share
no features by arithmetic.

OpenEA is the standard entity-alignment benchmark and meets the requirement note 076 set:
every entity in the V2 sets has a real distribution. `EN_DE_15K_V2` is the instrument of
choice for a first measurement:

    74% of relations are SHARED between the two graphs, because both sides are
    DBpedia in different languages -- so profiles live in one feature space and
    are directly comparable. `D_W` and `D_Y` share 0% (DBpedia against Wikidata
    and YAGO), so those need bootstrapping first and are the harder setting

    the URIs are ENCODED in v2.0 (`E823797`, not a readable name), which is the
    authors' name-bias fix -- so string matching cannot cheat and relational
    structure is the only signal available. That is exactly the test

## What is measured, and what is deliberately not

**Zero seed alignments.** The standard setting supplies 3,000 of the 15,000 links as
supervision; this uses none, so the number is not comparable to published supervised
results and is not meant to be. What it establishes is whether the signal EXISTS.

**The crudest possible profile**: a bag of `(shared relation, direction)` counts. No
neighbour structure, no iteration, no attributes. Bootstrapping — align confidently, then
use aligned neighbours as features — is the standard next step and is not done here, because
a first-order signal is what a first measurement should isolate.

## What it found

    hits@1   0.0389    583 of 15,000, against chance 0.000067 -- a 583x lift
    hits@10  0.1565
    MRR      0.0787

And, by how much evidence the weaker side carries, which is note 076's claim tested rather
than argued:

    edges        n     hits@1
        0      609     0.0000
        1    2,485     0.0024
      2-3    4,481     0.0152
      4-7    5,268     0.0537
     8-15    1,611     0.0894
      16+      546     0.1502

**Monotone in every bucket, 60x from one edge to sixteen.** It also explains CLUTRR exactly:
its entities sit in the 1-2 edge range where hits@1 is 0.002-0.015, so that instrument was
not weak, it was in the region where this signal does not exist.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
DEFAULT = ROOT / "data" / "openea" / "OpenEA_dataset_v2.0"

#: Evidence buckets for the by-degree table. The weaker side is what bounds a pair,
#: so a rich entity matched against a bare one is limited by the bare one.
BUCKETS = ((0, 0), (1, 1), (2, 3), (4, 7), (8, 15), (16, 10 ** 6))


def triples(path: Path) -> list[tuple[str, str, str]]:
    if not path.exists():
        raise SystemExit(
            f"{path} is not there. OpenEA is fetched rather than committed: run "
            f"`python tools/fetch_openea.py`, which pins the URL and verifies "
            f"size and sha256")
    return [tuple(line.split("\t"))
            for line in path.read_text(encoding="utf-8").splitlines()]


def counts(rows, entities, features, shared) -> np.ndarray:
    """Unnormalised `(relation, direction)` counts per entity."""
    index = {entity: i for i, entity in enumerate(entities)}
    matrix = np.zeros((len(entities), len(features)), dtype=np.float32)
    for head, relation, tail in rows:
        if relation not in shared:
            continue
        if head in index:
            matrix[index[head], features[(relation, "H")]] += 1
        if tail in index:
            matrix[index[tail], features[(relation, "T")]] += 1
    return matrix


def unit(matrix: np.ndarray) -> np.ndarray:
    norms = np.linalg.norm(matrix, axis=1, keepdims=True)
    return matrix / np.where(norms == 0, 1.0, norms)


def ranks_of_gold(left: np.ndarray, right: np.ndarray,
                  chunk: int = 500) -> np.ndarray:
    """Rank of the true partner for each row, 1 being best.

    Chunked because the full 15,000 x 15,000 similarity is 900 MB dense, and
    materialising it to take one argmax per row is paying for the whole matrix to
    use a column of it.
    """
    found = np.zeros(len(left), dtype=np.int64)
    for start in range(0, len(left), chunk):
        similarity = left[start:start + chunk] @ right.T
        order = np.argsort(-similarity, axis=1)
        for row in range(similarity.shape[0]):
            gold = start + row
            found[gold] = int(np.where(order[row] == gold)[0][0]) + 1
    return found


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=DEFAULT)
    parser.add_argument("--dataset", default="EN_DE_15K_V2")
    args = parser.parse_args()

    directory = args.root / args.dataset
    first = triples(directory / "rel_triples_1")
    second = triples(directory / "rel_triples_2")
    links = triples(directory / "ent_links")
    links = [(row[0], row[1]) for row in links]

    shared = ({r for _, r, _ in first} & {r for _, r, _ in second})
    if not shared:
        raise SystemExit(
            f"{args.dataset} shares NO relations between its two graphs, so a "
            f"profile on one side has no coordinates in common with the other and "
            f"cosine similarity is meaningless. D_W and D_Y are like this "
            f"(DBpedia against Wikidata and YAGO) and need an alignment "
            f"bootstrapped from something vocabulary-free first. Try EN_DE_15K_V2 "
            f"or EN_FR_15K_V2, where both sides are DBpedia.")
    features = {(relation, side): i for i, (relation, side) in enumerate(
        [(r, s) for r in sorted(shared) for s in ("H", "T")])}

    raw_left = counts(first, [a for a, _ in links], features, shared)
    raw_right = counts(second, [b for _, b in links], features, shared)
    ranks = ranks_of_gold(unit(raw_left), unit(raw_right))

    total = len(links)
    print(f"{args.dataset}: {total:,} gold pairs, {len(shared)} shared "
          f"relations, {len(features)} features, ZERO seed alignments")
    print(f"  hits@1  {(ranks == 1).mean():.4f}   "
          f"({int((ranks == 1).sum()):,}/{total:,})")
    print(f"  hits@10 {(ranks <= 10).mean():.4f}")
    print(f"  MRR     {(1 / ranks).mean():.4f}")
    print(f"  chance  {1 / total:.6f}   "
          f"lift {((ranks == 1).mean()) / (1 / total):,.0f}x")

    # The weaker side bounds the pair: a rich entity matched against a bare one
    # cannot do better than the bare one allows.
    evidence = np.minimum(raw_left.sum(axis=1), raw_right.sum(axis=1))
    print("\nby shared-vocabulary edges on the WEAKER side "
          "(note 076's claim, measured):")
    print(f"{'edges':>10s} {'n':>7s} {'hits@1':>8s} {'hits@10':>8s} {'MRR':>7s}")
    for low, high in BUCKETS:
        chosen = (evidence >= low) & (evidence <= high)
        if not chosen.any():
            continue
        here = ranks[chosen]
        label = f"{low}" if low == high else (
            f"{low}+" if high > 10 ** 5 else f"{low}-{high}")
        print(f"{label:>10s} {int(chosen.sum()):7,} {(here == 1).mean():8.4f} "
              f"{(here <= 10).mean():8.4f} {(1 / here).mean():7.4f}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
