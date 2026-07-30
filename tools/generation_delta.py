"""Learn kinship's additive invariant from cycles, and fold with it end to end.

## What this is for

CLUTRR withholds 31 composition rules, and every attempt to *name* a missing rule failed —
note 088 measured the best learned readout scoring **below random guessing**.

**Naming was the wrong goal.** A chain needs the right DISPLACEMENT for a missing step, not
the right name: fill a gap with any relation that moves the correct number of generations
and the chain stays arithmetically consistent, so the steps that ARE known finish it.

## How the invariant is learned, and why it is not a profile

A puzzle's chain plus its question forms a **closed loop**, so the chain's displacements must
sum to the answer's:

    sum of the chain's deltas  -  delta(target)  =  0

One homogeneous equation per puzzle, 9,074 of them, in 20 unknowns. The null space has
dimension **1** — a global choice of origin and unit — and fixing `brother = 0`,
`father = +1` recovers all twenty deltas exactly.

**Note 089 measured generation at 0.350 from extensional profiles and called it the blocker.**
It was right that profiles cannot see it: a profile is ADJACENCY and generation is GLOBAL.
The fix was a different kind of signal, not a better regressor — which is the transferable
part of this file.

## The numbers it reproduces

    symbolic fold, TRUE chains (note 090)
    gap (no fill)                            0.5201
    random relation                          0.6073
    CONTROL: deliberately WRONG delta        0.5681   <- below random
    correct delta, arbitrary relation        0.9668
    oracle, true rules                       1.0000

    END TO END, model recovers the chain (note 091)
    chain recovery                           0.8770
    gaps unfilled                            0.5279
    delta-filled                             0.8578

**The wrong-delta control is what makes the result readable.** Without it, "filling helps"
would be the finding, and note 088 measured that filling at random helps a little on its own.

## What this does NOT establish

Kinship has an additive invariant. **Whether an arbitrary relational domain has a conserved
quantity of this kind is unknown**, and a domain without one gets nothing here. The rule
*"deltas add"* is also a design choice rather than something discovered from data.
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

import clutrr_recovery as cr  # noqa: E402
import relation_profiles as rp  # noqa: E402

from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.search import beam  # noqa: E402
from openplexus.tasks.clutrr import (  # noqa: E402
    FACT, RELATIONS, ClutrrConfig, load)

#: The gauge. Two relations pin the origin and the unit; every other delta follows.
#: `brother` is level by inspection and `father` is one generation up, which is the
#: minimum that has to be asserted rather than learned.
GAUGE = (("brother", 0.0), ("father", 1.0))


def learn_deltas(root: Path, config: str) -> dict[str, int]:
    """Recover each relation's generation delta from loop constraints."""
    index = {r: i for i, r in enumerate(RELATIONS)}
    rows = []
    for edges, types, query, target in rp.rows(root, config, "train"):
        # The chain must actually run from the query's subject to its object, or the
        # loop is not closed and the equation is about a different walk.
        if edges[0][0] != query[0] or edges[-1][1] != query[1]:
            continue
        if any(edges[i][1] != edges[i + 1][0] for i in range(len(edges) - 1)):
            continue
        row = np.zeros(len(RELATIONS))
        for relation in types:
            row[index[relation]] += 1
        row[index[target]] -= 1
        rows.append(row)
    if not rows:
        raise SystemExit("no closed loops found; nothing to solve")
    _, singular, right = np.linalg.svd(np.array(rows), full_matrices=False)
    null = right[len(singular) - 1:]
    if null.shape[0] != 1:
        raise SystemExit(
            f"expected a null space of dimension 1 -- one global choice of origin "
            f"and unit -- and found {null.shape[0]}. More than one means the "
            f"constraints do not pin the deltas and the gauge below is arbitrary "
            f"rather than a normalisation.")
    matrix = np.array([[null[0][index[name]]] for name, _ in GAUGE])
    wanted = np.array([value for _, value in GAUGE])
    coefficients, *_ = np.linalg.lstsq(matrix, wanted, rcond=None)
    solved = null.T @ coefficients
    return {r: int(round(float(solved[index[r]]))) for r in RELATIONS}


def rule_table(root: Path, config: str) -> dict[tuple[str, str], str]:
    """Note 066's 97 rules: 2-hop chains, then 3-hop labelling `(derived, base)`."""
    table = dict(rp.base_rules(rp.rows(root, config, "train")))
    while True:
        found: dict = collections.defaultdict(collections.Counter)
        for edges, types, _, target in rp.rows(root, config, "train"):
            if len(edges) != 3:
                continue
            (_, first_end), (second_start, second_end), (third_start, _) = edges
            if not (first_end == second_start and second_end == third_start):
                continue
            derived = table.get((types[0], types[1]))
            if derived is not None and (derived, types[2]) not in table:
                found[(derived, types[2])][target] += 1
        new = {pair: answers.most_common(1)[0][0]
               for pair, answers in found.items() if len(answers) == 1}
        if not new:
            return table
        table.update(new)


def make_fold(table, deltas, mode: str, seed: int = 0):
    """Fold a chain of relation names, filling gaps according to `mode`."""
    by_delta: dict = collections.defaultdict(list)
    for relation, delta in deltas.items():
        by_delta[delta].append(relation)
    rng = np.random.default_rng(seed)

    def fill(left: str, right: str) -> str | None:
        if mode == "gap":
            return None
        if mode == "random":
            return RELATIONS[int(rng.integers(len(RELATIONS)))]
        if mode == "wrong-delta":
            wanted = deltas[left] + deltas[right]
            other = [r for r in RELATIONS if deltas[r] != wanted]
            return other[int(rng.integers(len(other)))] if other else None
        options = by_delta.get(deltas[left] + deltas[right])
        return options[0] if options else None

    def fold(chain):
        accumulated = chain[0]
        for step in chain[1:]:
            got = table.get((accumulated, step))
            if got is None:
                got = fill(accumulated, step)
                if got is None:
                    return None
            accumulated = got
        return accumulated

    return fold


MODES = ("gap", "random", "wrong-delta", "delta")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT / "data" / "clutrr")
    parser.add_argument("--config", default="gen_train23_test2to10")
    parser.add_argument("--width", type=int, default=64)
    parser.add_argument("--seed", type=int, default=0)
    parser.add_argument("--end-to-end", action="store_true",
                        help="run the MODEL and fold its recovered chains, not the "
                             "true ones")
    args = parser.parse_args()

    deltas = learn_deltas(args.root, args.config)
    table = rule_table(args.root, args.config)
    print(f"{len(table)} rules (note 066: 97), deltas learned from loop constraints")
    print("  " + "  ".join(f"{r}={deltas[r]:+d}" for r in
                           ("father", "mother", "son", "brother", "grandfather")))

    config = ClutrrConfig(root=args.root, split="test", layout="kinship")
    puzzles = load(config)
    names = {config.relation_base + i: r for i, r in enumerate(RELATIONS)}

    if not args.end_to_end:
        print(f"\nsymbolic fold over TRUE chains, {len(puzzles)} puzzles")
        print(f"{'arm':>14s} {'end-task':>9s}")
        for mode in MODES:
            fold = make_fold(table, deltas, mode, args.seed)
            right = sum(1 for p in puzzles
                        for chain in [cr.true_chain(p, config)]
                        if chain and fold([names[t] for t in chain])
                        == names[p.target])
            print(f"{mode:>14s} {right / len(puzzles):9.4f}")
        return 0

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=config.vocab_size, d_model=args.width, seed=args.seed,
        context_keys=True, derived_keys=True, decay=1.0))
    allowed = np.arange(config.relation_base,
                        config.relation_base + len(RELATIONS))
    folds = {mode: make_fold(table, deltas, mode, args.seed) for mode in MODES}
    scored = recovered = 0
    right = dict.fromkeys(MODES, 0)
    for puzzle in puzzles:
        chain = cr.true_chain(puzzle, config)
        if chain is None:
            continue
        scored += 1
        model.run(np.asarray(puzzle.tokens))
        subject = int(puzzle.tokens[puzzle.query_position - 1])
        target = model.wv[int(puzzle.tokens[puzzle.query_position])]
        walks = beam(model._final, model.retrieval, model.key_source, model.wv,
                     FACT, subject, target, len(chain), width=4, branches=4,
                     allowed=allowed)
        if not walks:
            continue
        recovered += walks[0].relations == chain
        got = [names[t] for t in walks[0].relations if t in names]
        if len(got) != len(walks[0].relations):
            continue
        for mode, fold in folds.items():
            right[mode] += fold(got) == names[puzzle.target]
    print(f"\nEND TO END, the model recovers the chain. {scored} puzzles, "
          f"width {args.width}")
    print(f"  chain recovery {recovered / scored:.4f}   "
          f"(tools/clutrr_recovery.py: 0.8770 at width 64, seed 0)")
    for mode in MODES:
        print(f"  {mode:>12s} {right[mode] / scored:.4f}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
