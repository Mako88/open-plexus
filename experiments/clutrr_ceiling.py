"""What CLUTRR-symbolic scores with no model at all.

Kill-list #1 is *does a relational objective buy reasoning*, and CLUTRR is the
external instrument it was going to be answered on. **Before rebuilding that
measurement on the count graph, this asks what the benchmark can decide.**

The whole of the knowledge in `gen_train23_test2to10` is 62 composition facts,
and every one of them is stated outright by a two-hop training row: walk `father`
then `sister`, and the row says the ends are `aunt`. Count them into a table,
then evaluate a test chain as an expression in that algebra.

Two arms, and the gap between them is the finding:

    left to right     reduce the first pair, then the next, and so on
    any bracketing    every order, which is the CYK recurrence over spans

And two controls, because a search that reaches everything would contain the
answer for free:

    shuffled answers  the same 62 keys, their answers permuted
    shuffled keys     the same 62 answers, attached to permuted keys

    python experiments/clutrr_ceiling.py --json out/clutrr-ceiling.json
"""

from __future__ import annotations

import argparse
import json
import pathlib
import random
import sys
import time
from collections import defaultdict

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from openplexus.tasks.clutrr import (ClutrrConfig, RELATIONS,  # noqa: E402
                                     composition_table, load, reachable)

DATA = ROOT / "data" / "clutrr"

#: Control seeds. Three is chosen here as the floor this project accepts; the
#: controls differ from the real arm by 0.87, so more seeds would resolve nothing
#: that three do not.
SEEDS = (0, 1, 2)


def left_to_right(chain, table):
    """The obvious reduction. Returns the relation, or `None` at an unknown pair."""
    current = chain[0]
    for following in chain[1:]:
        if (current, following) not in table:
            return None
        current = table[(current, following)]
    return current


def score(puzzles, table) -> dict:
    """Both arms, per hop count, plus how large the reachable set gets."""
    per_hop: dict[int, dict] = defaultdict(
        lambda: {"n": 0, "ltr": 0, "any": 0, "empty": 0, "sizes": 0,
                 "ambiguous": 0})
    for puzzle in puzzles:
        row = per_hop[puzzle.hops]
        row["n"] += 1
        row["ltr"] += left_to_right(puzzle.chain, table) == puzzle.target
        found = reachable(puzzle.chain, table)
        row["sizes"] += len(found)
        if not found:
            row["empty"] += 1
        else:
            row["any"] += puzzle.target in found
            row["ambiguous"] += len(found) > 1
    return per_hop


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    parser.add_argument("--split", default="test")
    args = parser.parse_args()

    config = ClutrrConfig(root=DATA, split=args.split)
    if not config.path.exists():
        raise SystemExit(f"no data at {config.path}: python tools/fetch_clutrr.py")

    started = time.time()
    table = composition_table(load(ClutrrConfig(root=DATA, split="train")))
    puzzles = load(config)
    print(f"{len(table)} composition facts counted from two-hop TRAINING rows, "
          f"against {len(RELATIONS)} relations")
    print(f"{len(puzzles)} {args.split} puzzles. Guessing scores "
          f"{config.trivial:.4f}\n")

    per_hop = score(puzzles, table)
    header = (f"{'hops':>5}{'n':>7}{'left to right':>15}{'any bracketing':>16}"
              f"{'no result':>11}{'ambiguous':>11}{'reachable':>11}")
    print(header)
    print("-" * len(header))
    rows = []
    for hops in sorted(per_hop):
        got = per_hop[hops]
        rows.append({"arm": "real", "hops": hops, **got})
        print(f"{hops:>5}{got['n']:>7}{got['ltr'] / got['n']:>15.4f}"
              f"{got['any'] / got['n']:>16.4f}{got['empty'] / got['n']:>11.4f}"
              f"{got['ambiguous'] / got['n']:>11.4f}"
              f"{got['sizes'] / got['n']:>11.2f}")
    total = sum(got["n"] for got in per_hop.values())
    print(f"{'all':>5}{total:>7}"
          f"{sum(g['ltr'] for g in per_hop.values()) / total:>15.4f}"
          f"{sum(g['any'] for g in per_hop.values()) / total:>16.4f}")

    print("\nCONTROLS. The same search, on a table that says the wrong thing.")
    for kind in ("answers", "keys"):
        for seed in SEEDS:
            shuffled = _shuffle(table, kind, seed)
            got = score(puzzles, shuffled)
            n = sum(g["n"] for g in got.values())
            hit = sum(g["any"] for g in got.values()) / n
            size = sum(g["sizes"] for g in got.values()) / n
            empty = sum(g["empty"] for g in got.values()) / n
            rows.append({"arm": f"shuffled-{kind}", "seed": seed, "hit": hit,
                         "reachable": size, "empty": empty, "n": n})
            print(f"  shuffled {kind} seed {seed}: contains the answer "
                  f"{hit:.4f}, reachable {size:.2f}, no result {empty:.4f}")

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"\n{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


def _shuffle(table, kind: str, seed: int) -> dict:
    rng = random.Random(seed)
    keys, values = list(table.keys()), list(table.values())
    if kind == "answers":
        rng.shuffle(values)
    else:
        rng.shuffle(keys)
    return dict(zip(keys, values))


if __name__ == "__main__":
    raise SystemExit(main())
