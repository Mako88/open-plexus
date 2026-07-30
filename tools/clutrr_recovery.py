"""Note 065's chain recovery, made reproducible — and gated on its own numbers.

## Why this exists

Note 065 reports the largest single mechanism gain in this project: `beam` over `search`,
**+0.219 chain recovery, 713/713 on the clean subset.** Note 074 found that no committed
script produces it, so the configuration behind 0.8805 was unrecorded and a partitioned
measurement had nothing to be a difference *from*.

**This script is not trusted until it reproduces 065's numbers**, and it does NOT --
note 075. It prints 065's figures beside its own and says so. `beam` lands within 0.007
of the mean; `search` is high by 0.12, so the +0.219 gain comes out at +0.107. A harness that disagrees with a known
result is a harness, not a finding — the rule that fired three times in one session
(069's baseline, 070's protocol, 071's gate).

    seed        search(b=4)     beam(w=4,b=4)
       0             0.6588            0.8735
       1             0.6632            0.8831
       2             0.6623            0.8848
    plain subset                        713/713

## What chain recovery is

Not end-task accuracy. The puzzle states a chain of edges and asks about its two ends;
recovery asks whether the traversal commits to **the true sequence of relations along that
chain.** A recovered chain is not an answer — naming what it composes to is the fold, and
note 066 measures that separately.

## What is reconstructed and why

`Puzzle` carries tokens, not the graph, so the true chain is rebuilt from the token stream:
parse `FACT s r o` blocks, walk from the query's subject to its object, read off the
relations in path order. **Rebuilt rather than added to the loader** because the loader's
job is tokens and a second output would have to be kept in step with the layout.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.search import beam, search  # noqa: E402
from openplexus.tasks.clutrr import (  # noqa: E402
    FACT, RELATIONS, ClutrrConfig, load)

#: What note 065 reported, per seed. The gate.
EXPECTED = {
    0: (0.6588, 0.8735),
    1: (0.6632, 0.8831),
    2: (0.6623, 0.8848),
}


def triples(puzzle, config: ClutrrConfig):
    """`(subject, relation, object)` per stated fact, from the token stream."""
    tokens = puzzle.tokens
    out = []
    for start in range(0, len(tokens) - 3, 4):
        if tokens[start] != FACT:
            break
        # kinship is `FACT s r o`; this script is kinship-only, which
        # `walk_from` requires -- it reads key(entity, relation).
        out.append((int(tokens[start + 1]), int(tokens[start + 2]),
                    int(tokens[start + 3])))
    return out


def true_chain(puzzle, config: ClutrrConfig) -> tuple[int, ...] | None:
    """The relations along the path from the query's subject to its object.

    Returns None when no path exists in the stated direction, which is not a
    failure of anything measured here -- the walk is directed and some puzzles
    state an edge the other way round. Counted and reported rather than silently
    scored as a miss.
    """
    subject = int(puzzle.tokens[puzzle.query_position - 1])
    target = int(puzzle.tokens[puzzle.query_position])
    edges = triples(puzzle, config)
    forward: dict[int, list[tuple[int, int]]] = {}
    for head, relation, tail in edges:
        forward.setdefault(head, []).append((relation, tail))

    # Depth-first, since the stated graph is a chain and any path IS the path.
    stack = [(subject, (), frozenset({subject}))]
    while stack:
        node, chain, seen = stack.pop()
        if node == target and chain:
            return chain
        for relation, tail in forward.get(node, ()):
            if tail not in seen:
                stack.append((tail, chain + (relation,), seen | {tail}))
    return None


def recovery(config: ClutrrConfig, seed: int, width: int, branches: int,
             beam_width: int, decay: float):
    """Chain recovery for `search` and `beam` over one split."""
    puzzles = load(config)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=config.vocab_size, d_model=width, seed=seed,
        context_keys=True, derived_keys=True, decay=decay))
    allowed = np.arange(config.relation_base,
                        config.relation_base + len(RELATIONS))

    hits = {"search": 0, "beam": 0}
    scored = 0
    unreachable = 0
    plain_beam = plain_total = 0
    for puzzle in puzzles:
        chain = true_chain(puzzle, config)
        if chain is None:
            unreachable += 1
            continue
        scored += 1
        model.run(np.asarray(puzzle.tokens))
        store = model._final
        subject = int(puzzle.tokens[puzzle.query_position - 1])
        target = model.wv[int(puzzle.tokens[puzzle.query_position])]
        args = (store, model.retrieval, model.key_source, model.wv,
                FACT, subject, target, len(chain))
        found = {
            "search": search(*args, branches=branches, allowed=allowed),
            "beam": beam(*args, width=beam_width, branches=branches,
                         allowed=allowed),
        }
        for name, walks in found.items():
            if walks and walks[0].relations == chain:
                hits[name] += 1
        if puzzle.max_appearances <= 2:
            plain_total += 1
            if found["beam"] and found["beam"][0].relations == chain:
                plain_beam += 1
    return (hits["search"] / scored, hits["beam"] / scored,
            plain_beam, plain_total, scored, unreachable)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT / "data" / "clutrr")
    parser.add_argument("--split", default="test")
    parser.add_argument("--width", type=int, default=64)
    parser.add_argument("--decay", type=float, default=1.0)
    parser.add_argument("--branches", type=int, default=4)
    parser.add_argument("--beam-width", type=int, default=4)
    parser.add_argument("--seeds", type=int, nargs="+", default=[0, 1, 2])
    args = parser.parse_args()

    config = ClutrrConfig(root=args.root, split=args.split, layout="kinship")
    print(f"width {args.width}, decay {args.decay}, branches {args.branches}, "
          f"beam width {args.beam_width}, layout kinship")
    print(f"{'seed':>5} {'search':>8} {'beam':>8}   "
          f"{'065 search':>11} {'065 beam':>9}   {'plain':>10}")
    searches, beams = [], []
    for seed in args.seeds:
        got_search, got_beam, plain, plain_n, scored, skipped = recovery(
            config, seed, args.width, args.branches, args.beam_width,
            args.decay)
        searches.append(got_search)
        beams.append(got_beam)
        want = EXPECTED.get(seed, (float("nan"), float("nan")))
        print(f"{seed:5d} {got_search:8.4f} {got_beam:8.4f}   "
              f"{want[0]:11.4f} {want[1]:9.4f}   {plain:5d}/{plain_n:<4d}")
    print(f"\nscored {scored} puzzles, {skipped} skipped as unreachable "
          f"in the stated direction")
    print(f"mean search {np.mean(searches):.4f}, beam {np.mean(beams):.4f}, "
          f"gain {np.mean(beams) - np.mean(searches):+.4f}")
    print(f"065 reported mean search 0.6614, beam 0.8805, gain +0.2190")
    # EVERY number, not just beam. The first version of this gate checked beam
    # alone and printed "configuration recovered" while `search` was off by 0.13
    # and the plain subset by 12 rows -- a subset declared as the whole, which is
    # the mistake this whole file exists to guard against.
    close = (all(abs(g - EXPECTED[s][1]) < 0.005
                 for s, g in zip(args.seeds, beams) if s in EXPECTED)
             and all(abs(g - EXPECTED[s][0]) < 0.005
                     for s, g in zip(args.seeds, searches) if s in EXPECTED)
             and plain == plain_n)
    print("\nGATE: " + ("MATCHES note 065 -- the configuration is recovered"
                        if close else
                        "does NOT match note 065. This is a harness, not a "
                        "finding: the configuration is still unknown"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
