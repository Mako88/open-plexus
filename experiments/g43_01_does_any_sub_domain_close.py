"""g43-01: does any SUBSET of a real graph's relations close, when the whole does not?

`g23-03` named this computation and left it unbuilt: *"the replacement question —
does some SUBSET of a graph's relations close consistently — is a
largest-consistent-subset search rather than a null space over all relations, and
it is unbuilt."*

FB15k-237 has **no** additive invariant over all 237 relations, and not
approximately one — its smallest singular values cluster with a 1.006x gap where
CLUTRR's sits fourteen orders below its largest. But a knowledge graph covers
films and sports and geography at once, and there is no reason those should share
one accounting system. A closing sub-domain would make the displacement mechanism
pointable at parts of a real problem rather than a property of family trees.

## The degenerate case, which is the whole difficulty

    dim >= |S| - rank        and        rank <= min(loops, |S|)

so **any subset with fewer loops than relations closes by arithmetic alone.**
That is this metric's null recovery and it is emphatically not zero. Every dim
reported here carries `loops/|S|` beside it, and a cell below 1.0 is
uninformative by construction rather than by judgement.

`CLAUDE.md`'s g32-01 calibration is the same failure one level up: a control
predicted near zero came in at 0.3189 to 0.5078 because the metric's do-nothing
value was 0.5, and the floor was one line of arithmetic available before the run.

## What this does NOT duplicate, and what was searched

Searched by capability — invariant, null space, cycle, subset, closure, conserved
— across `tools/`, `experiments/`, `openplexus/` and `docs/archive/`.

- **`tools/invariant_dimension.py` is IMPORTED, not reimplemented.** It owns the
  cycle extraction, the constraint matrix and the rank tolerance; this filters its
  rows by support and re-runs its `dimension` on the restriction. `relation_names`
  was added there rather than here so the column ordering has one definition.
- **`tools/generation_delta.py`** consumes an invariant once it exists. This asks
  whether one exists at all, and nothing here trains, walks or folds.
- **`openplexus/grouping.py`** partitions by vector similarity. The partition here
  comes from the relation NAMES, which the data supplies.

Predictions: `experiments/sweeps/g43-01-does-any-sub-domain-close.txt`
"""

from __future__ import annotations

import collections
import pathlib
import sys
import time

ROOT = pathlib.Path(__file__).resolve().parents[1]
for candidate in (ROOT, ROOT / "tools"):
    if str(candidate) not in sys.path:
        sys.path.insert(0, str(candidate))

import numpy as np  # noqa: E402

import invariant_dimension as inv  # noqa: E402

from experiments import harness  # noqa: E402

GRAPH = ROOT / "data" / "fb15k237" / "train.txt"

#: Evidence per unknown below which a `dim >= 1` says nothing. Chosen for this
#: run and stated in its record before dispatch: at a ratio below 1 the null
#: space is non-empty by arithmetic, and 10 is an order of magnitude of margin
#: over that boundary rather than a fitted threshold.
INFORMATIVE = 10.0

#: Chosen here, arbitrarily: the graph is a fixed file, so this seeds only the
#: shuffled control and nothing about the real arm moves with it.
SEED = 0

#: Subset sizes for the ladder, chosen here as powers of two down from the full
#: 237. Geometric rather than linear because `loops/|S|` falls fast and the
#: interesting region is where it crosses 1.
SIZES = (237, 128, 64, 32, 16, 8, 4, 2)


def restricted(rows: np.ndarray, columns: np.ndarray) -> tuple[int, int, int, int]:
    """`(loops, constrained |S|, dim, unconstrained)` for the subset `columns`.

    A loop belongs to the subset only if EVERY relation it uses is inside it.
    Keeping a loop that leaves the subset would silently drop its outside terms
    and assert a constraint the graph never made.

    **`dim` comes from `inv.dimension` and is NOT recomputed here.** The first
    version of this function took only the rank and worked out `len(columns) -
    rank` itself, which puts back the all-zero columns `dimension` deliberately
    excludes — a relation appearing in no within-subset loop joins the null space
    for free.

    It reported the whole of FB15k-237 as **dim 3** where the tool says **dim 0**,
    and it decorated four domains with spurious invariants. `dimension`'s own
    docstring names this failure and the graph it first appeared on: DBpedia read
    as *"two conserved quantities"* and both were relations that never close a
    loop. **Reproduced here by discarding the answer and recomputing it worse.**

    `|S|` is therefore the CONSTRAINED count, so `loops/|S|` is evidence per
    unknown that actually has evidence, and the excluded count is carried
    alongside rather than folded in.
    """
    inside = np.zeros(rows.shape[1], dtype=bool)
    inside[columns] = True
    wholly = ~np.any((rows != 0) & ~inside, axis=1)
    block = rows[wholly][:, columns]
    if block.size == 0 or block.shape[0] == 0:
        return 0, 0, 0, len(columns)
    dim, _, loose = inv.dimension(list(block), block.shape[1])
    return block.shape[0], len(columns) - loose, dim, loose


def by_domain(names: list[str]) -> dict[str, list[int]]:
    """Group relation columns by the leading segment of the path name.

    `/people/person/nationality` and `/people/person/profession` share `people`.
    **A partition the data supplies**, which is what keeps this from being a
    search over 2**237 subsets chosen to make something come out.
    """
    groups: dict[str, list[int]] = collections.defaultdict(list)
    for i, name in enumerate(names):
        groups[name.strip("/").split("/")[0] or "?"].append(i)
    return dict(groups)


def ladder(rows: np.ndarray, columns: np.ndarray,
           sizes: tuple[int, ...]) -> list[tuple[int, int, int]]:
    """`(loops, |S|, dim)` at a geometric ladder of subset sizes.

    Relations are ordered by how many loops carry them, best-evidenced first, and
    each rung keeps that many. **The order is computed ONCE** — a greedy search
    that re-ranks after every removal costs one SVD per step, which at 234
    relations over 267,089 loops is hours, and the question does not need it.

    **A ladder rather than a binary search, deliberately.** Closure is not
    monotone in subset size: removing a relation drops constraints AND unknowns,
    so a smaller subset is not guaranteed to close if a larger one did not.
    Bisecting would assume exactly that. Reading every rung assumes nothing and
    shows where closure appears and at what evidence ratio, which is the thing
    P3 is actually about.
    """
    inside = np.zeros(rows.shape[1], dtype=bool)
    inside[columns] = True
    wholly = ~np.any((rows != 0) & ~inside, axis=1)
    carried = (rows[wholly][:, columns] != 0).sum(axis=0)
    order = [int(columns[i]) for i in np.argsort(carried)[::-1]]
    return [restricted(rows, np.array(order[:size]))
            for size in sizes if size <= len(order)]


def shuffle(rows: np.ndarray, seed: int) -> np.ndarray:
    """Permute each loop's coefficients across relations, preserving its shape.

    THE CONTROL. Every loop keeps its length and its signs; only which relation
    each term refers to is destroyed. So a `dim` that survives this is a property
    of how many constraints there are, not of what they say.
    """
    rng = np.random.default_rng(seed)
    out = np.zeros_like(rows)
    for i, row in enumerate(rows):
        support = np.nonzero(row)[0]
        moved = rng.choice(rows.shape[1], size=len(support), replace=False)
        out[i, moved] = row[support]
    return out


def main() -> int:
    harness.parse_args(__doc__)
    started = time.time()

    # THE GATE, and it runs before anything is reported. A subset search that
    # cannot find kinship's own invariant when handed kinship is broken, and that
    # failure would look exactly like a null on FB15k-237 -- which is the result
    # this run is written expecting, so the null must be earned rather than
    # available for free.
    kin_rows, kin_relations = inv.clutrr_rows(ROOT / "data" / "clutrr",
                                              "gen_train23_test2to10")
    kin = np.array(kin_rows, dtype=float)
    _, _, kin_dim, _ = restricted(kin, np.arange(kin_relations))
    print(f"GATE: CLUTRR kinship under the same restriction -> dim {kin_dim} "
          f"(note 104: 1)")
    if kin_dim != 1:
        print("  REFUSING TO REPORT. The search cannot reproduce a known "
              "invariant, so a null here would say nothing about the graph.")
        return 1

    names = inv.relation_names(GRAPH)
    raw, relations, edges = inv.graph_rows(GRAPH)
    rows = np.array(raw, dtype=float)
    print(f"{GRAPH.name}: {edges} edges, {relations} relations, "
          f"{rows.shape[0]} loops")

    whole = restricted(rows, np.arange(relations))
    print(f"\nWHOLE GRAPH   loops {whole[0]}  |S| {whole[1]}  "
          f"ratio {whole[0] / whole[1]:.1f}  dim {whole[2]}")

    control = shuffle(rows, SEED)
    for label, matrix in (("real", rows), ("shuffled", control)):
        print(f"\nBY DOMAIN, {label}. `informative` needs loops/|S| >= "
              f"{INFORMATIVE:.0f}")
        print(f"{'domain':<26}{'|S|':>6}{'noloop':>7}{'loops':>9}"
              f"{'loops/|S|':>11}{'dim':>6}   verdict")
        for domain, columns in sorted(by_domain(names).items(),
                                      key=lambda kv: -len(kv[1])):
            loops, size, dim, loose = restricted(matrix, np.array(columns))
            ratio = loops / size if size else 0.0
            verdict = ("closes, INFORMATIVE" if dim >= 1 and ratio >= INFORMATIVE
                       else "closes, degenerate" if dim >= 1
                       else "no invariant")
            print(f"{domain[:26]:<26}{size:>6}{loose:>7}{loops:>9}"
                  f"{ratio:>11.1f}{dim:>6}   {verdict}")

    print("\nBEST-EVIDENCED SUBSETS, at a ladder of sizes")
    print(f"{'arm':<12}{'|S|':>6}{'loops':>9}{'loops/|S|':>11}{'dim':>6}"
          f"   verdict")
    for label, matrix in (("real", rows), ("shuffled", control)):
        for loops, size, dim, _ in ladder(matrix, np.arange(relations), SIZES):
            ratio = loops / size if size else 0.0
            verdict = ("closes, INFORMATIVE" if dim >= 1 and ratio >= INFORMATIVE
                       else "closes, degenerate" if dim >= 1
                       else "no invariant")
            print(f"{label:<12}{size:>6}{loops:>9}{ratio:>11.1f}{dim:>6}"
                  f"   {verdict}")

    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
