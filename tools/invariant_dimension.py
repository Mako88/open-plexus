"""How many additive invariants a relational domain has, before any model is built.

## The question this makes cheap

`tools/generation_delta.py` closed CLUTRR's composition ceiling by supplying a missing
step's **displacement** rather than its name, and the displacements are recovered from
loop constraints: a chain plus its answer is a closed loop, so the relations' deltas must
sum to zero. That is one homogeneous equation per loop, and the deltas are the **null
space** of the resulting matrix.

It works because kinship happens to have a conserved quantity. **Whether an arbitrary
domain does is the open question**, and note 090 says so in as many words:
*"whether an arbitrary relational domain has a conserved quantity of this kind is
unknown, and a domain without one gets nothing here."*

The dimension of that null space answers it, and answers it **from the data alone** --
no model, no training, no walk:

    dim 0    only the zero solution. NO additive invariant, and the mechanism cannot
             work in this domain at all
    dim 1    unique up to a choice of origin and unit. Exactly one conserved quantity,
             which is what kinship has and what `GAUGE` normalises
    dim > 1  several independent invariants. Richer, but the gauge stops being a
             normalisation and becomes an arbitrary pick among them -- which is why
             `generation_delta.py` currently REFUSES this case rather than handling it

So a five-second linear-algebra check can say "this domain gets nothing" before anything
is built for it. That is a falsifier, which this project prefers to an encouraging
number.

## Where the loops come from

CLUTRR hands them over: each puzzle IS a closed loop. A general knowledge graph does
not, so the loops are the **fundamental cycles** of a spanning forest -- for every edge
not in the forest, the tree path between its endpoints plus that edge. Those form a
BASIS of the cycle space, so they capture every constraint the graph imposes without
enumerating cycles, of which there are exponentially many.

An edge walked against its direction contributes `-delta`, because a delta is a
displacement and walking a relation backwards displaces the other way. Treating it as
`+delta` would assert every relation is its own inverse, which is false for `father`
and would quietly change what is being measured.

## The control, which is not optional

**CLUTRR must come back as dimension 1.** Note 065's harness lesson -- reproduce a known
number before trusting the instrument -- has fired four times in this project. A cycle
extractor with a sign error would report dimension 0 for every domain and read as a
sweeping negative result.
"""

from __future__ import annotations

import argparse
import collections
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))
sys.path.insert(0, str(ROOT / "tools"))

#: Below this a singular value counts as zero. Relative to the largest, so it does not
#: depend on how many loops the domain happened to supply.
TOLERANCE = 1e-9


def dimension(rows: list[np.ndarray], relations: int) -> tuple[int, int, int]:
    """Null-space dimension over CONSTRAINED relations, the rank, and how many
    relations were excluded for appearing in no loop at all.

    ## Why the exclusion is not a detail

    **A relation in no cycle has an all-zero column, so it joins the null space for
    free.** Reported raw, DBpedia's English graph came back at dimension 2 and read as
    *"two conserved quantities"* -- and both came from two relations that never close a
    loop. Among the 167 that do, the dimension is 0.

    That is "not enough loops" wearing the clothes of structure, and it is the exact
    failure this project keeps recording: a measurement whose artifact is more
    interesting than its result. So the count is reported beside the dimension rather
    than folded into it, and `relations - rank` is taken over the constrained columns.
    """
    if not rows:
        return relations, 0, relations
    matrix = np.array(rows, dtype=float)
    constrained = matrix.any(axis=0)
    unconstrained = int((~constrained).sum())
    matrix = matrix[:, constrained]
    if not matrix.size:
        return 0, 0, unconstrained
    singular = np.linalg.svd(matrix, compute_uv=False)
    rank = int((singular > TOLERANCE * singular[0]).sum()) if singular.size else 0
    return int(constrained.sum()) - rank, rank, unconstrained


def clutrr_rows(root: Path, config: str):
    """CLUTRR's loops, exactly as `generation_delta.learn_deltas` builds them.

    Imported rather than reimplemented: a second copy of the constraint construction is
    a second thing to get subtly different, and this file's whole job is to be trusted
    about a number the other file depends on.
    """
    import relation_profiles as rp

    from openplexus.tasks.clutrr import RELATIONS

    index = {r: i for i, r in enumerate(RELATIONS)}
    rows = []
    for edges, types, query, target in rp.rows(root, config, "train"):
        if edges[0][0] != query[0] or edges[-1][1] != query[1]:
            continue
        if any(edges[i][1] != edges[i + 1][0] for i in range(len(edges) - 1)):
            continue
        row = np.zeros(len(RELATIONS))
        for relation in types:
            row[index[relation]] += 1
        row[index[target]] -= 1
        rows.append(row)
    return rows, len(RELATIONS)


def _triples(path: Path):
    for line in path.read_text(encoding="utf-8").splitlines():
        parts = line.split("\t")
        if len(parts) == 3:
            yield parts[0], parts[1], parts[2]


def relation_names(path: Path, limit: int | None = None) -> list[str]:
    """The relation names, in the SAME order `graph_rows` indexes its columns by.

    Extracted so a caller that needs to know what column `i` means -- `g43-01`
    groups FB15k-237's relations by the leading segment of their path names --
    reads the order from here instead of re-deriving `sorted(set(...))` and
    hoping the two stay in step. `graph_rows` calls it, so there is one ordering
    and not two that agree today.
    """
    edges = list(_triples(path))
    if limit:
        edges = edges[:limit]
    return sorted({relation for _, relation, _ in edges})


def graph_rows(path: Path, limit: int | None = None):
    """Fundamental-cycle constraints for a knowledge graph of `(s, r, o)` triples.

    A spanning forest is grown by breadth-first search; every edge NOT in it closes
    exactly one cycle against the tree paths from its endpoints to their common
    ancestor. Signs follow the direction each edge is walked in.
    """
    edges = list(_triples(path))
    if limit:
        edges = edges[:limit]
    relations = relation_names(path, limit)
    index = {r: i for i, r in enumerate(relations)}

    adjacent: dict[str, list[tuple[str, str, int]]] = collections.defaultdict(list)
    for subject, relation, obj in edges:
        # +1 walking with the arrow, -1 against it.
        adjacent[subject].append((obj, relation, 1))
        adjacent[obj].append((subject, relation, -1))

    #: `(parent, relation, sign, depth)` per entity, so a tree path is a walk upward.
    tree: dict[str, tuple[str | None, str | None, int, int]] = {}
    used: set[tuple[str, str, str]] = set()
    rows = []

    for start in adjacent:
        if start in tree:
            continue
        tree[start] = (None, None, 0, 0)
        queue = collections.deque([start])
        while queue:
            here = queue.popleft()
            for there, relation, sign in adjacent[here]:
                if there not in tree:
                    tree[there] = (here, relation, sign, tree[here][3] + 1)
                    used.add((here, relation, there))
                    queue.append(there)

    def climb(entity: str, target_depth: int, row: np.ndarray, direction: int) -> str:
        while tree[entity][3] > target_depth:
            parent, relation, sign, _ = tree[entity]
            row[index[relation]] += direction * sign
            entity = parent
        return entity

    for subject, relation, obj in edges:
        if (subject, relation, obj) in used or subject == obj:
            continue
        if tree.get(subject) is None or tree.get(obj) is None:
            continue
        row = np.zeros(len(relations))
        # The closing edge, walked forward.
        row[index[relation]] += 1
        # Then back from `obj` to `subject` through the tree: up from each to their
        # common depth, then up together. `obj`'s leg is walked in reverse, hence -1.
        left, right = subject, obj
        shallow = min(tree[left][3], tree[right][3])
        left = climb(left, shallow, row, -1)
        right = climb(right, shallow, row, +1)
        while left != right:
            parent, rel_l, sign_l, _ = tree[left]
            row[index[rel_l]] -= sign_l
            left = parent
            parent, rel_r, sign_r, _ = tree[right]
            row[index[rel_r]] += sign_r
            right = parent
        if np.any(row):
            rows.append(row)
    return rows, len(relations), len(edges)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--clutrr-root", type=Path,
                        default=ROOT / "data" / "clutrr")
    parser.add_argument("--clutrr-config", default="gen_train23_test2to10")
    parser.add_argument("--graph", type=Path, action="append", default=None,
                        help="a tab-separated (s, r, o) file. Repeatable")
    parser.add_argument("--limit", type=int, default=None)
    args = parser.parse_args()

    print(f"{'domain':<28s} {'rels':>6s} {'loops':>8s} {'rank':>6s} {'dim':>5s} "
          f"{'noloop':>7s}  verdict")

    rows, relations = clutrr_rows(args.clutrr_root, args.clutrr_config)
    null, rank, loose = dimension(rows, relations)
    print(f"{'CLUTRR kinship (CONTROL)':<28s} {relations:6d} {len(rows):8d} "
          f"{rank:6d} {null:5d} {loose:7d}  "
          f"{'as expected' if null == 1 else 'HARNESS FAULT'}")
    if null != 1:
        print("\nThe control did not return 1, so nothing below is about the domains "
              "-- it is about this file. `generation_delta.py` recovers 20 deltas "
              "exactly from these same constraints, so a different answer here is a "
              "fault in the cycle extraction or the signs. Stop and fix it.")
        return 1

    for path in args.graph or []:
        rows, relations, edges = graph_rows(path, args.limit)
        null, rank, loose = dimension(rows, relations)
        verdict = ("NO invariant" if null == 0 else
                   "one invariant" if null == 1 else
                   f"{null} invariants -- gauge is arbitrary")
        print(f"{path.parent.name + '/' + path.name:<28s} {relations:6d} "
              f"{len(rows):8d} {rank:6d} {null:5d} {loose:7d}  {verdict}")
        print(f"{'':28s} {edges} edges read, {loose} relation(s) in no loop and "
              f"EXCLUDED -- an all-zero column joins the null space for free")

    print("\ndim 0 means the displacement mechanism gets nothing in that domain.\n"
          "dim > 1 means several conserved quantities, which generation_delta.py\n"
          "currently REFUSES rather than handles -- a gap, not a result.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
