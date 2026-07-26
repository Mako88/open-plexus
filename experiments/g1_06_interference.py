"""Does the price of locality grow with the number of bindings held?

g1-05 measured the local rule crossing between d_model 48 and 64 at n_pairs=4,
and explained the graded transition as superposition interference: every binding
is layered into one matrix, so retrieval returns the wanted one plus a sum of the
others. If that account is right the crossing must MOVE as n_pairs changes —
and attention, which looks bindings up rather than storing them, was completely
flat in n_pairs across 56 runs (g1-04).

    python experiments/g1_06_interference.py --pairs 8 --width 96 --seed 3 --json out/x.json
    python experiments/g1_06_interference.py --decay 0.95 --seed 3 --json out/y.json

One cell per invocation so the Actions matrix can split on every axis; omit the
flags to run everything serially.
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.g1_05_local import BASE, relative_cost, run  # noqa: E402
from experiments.harness import emit, parse_args  # noqa: E402

SEEDS = tuple(range(1, 7))
PAIRS = (2, 4, 8)
WIDTHS = (24, 32, 48, 64, 96, 128, 192)

#: The decay question runs at one width, deliberately just below the n_pairs=8
#: crossing, where an effect either way has room to show. At a width that already
#: solves the task there would be nothing for decay to improve or spoil.
DECAY_PAIRS, DECAY_WIDTH = 8, 48
DECAYS = (1.0, 0.99, 0.95, 0.9)


def main() -> int:
    args = parse_args(__doc__)
    seeds = (args.seed,) if args.seed is not None else SEEDS

    records = []
    if args.decay is not None or args.sweep == "decay":
        decays = (args.decay,) if args.decay is not None else DECAYS
        task = replace(BASE, n_pairs=DECAY_PAIRS)
        print(f"decay sweep: {len(decays)} x {len(seeds)} seeds", flush=True)
        for decay in decays:
            for seed in seeds:
                accuracy = run(task, d_model=DECAY_WIDTH, seed=seed, decay=decay)
                records.append(dict(condition=f"decay={decay}", seed=seed,
                                    decay=decay, d_model=DECAY_WIDTH,
                                    n_pairs=DECAY_PAIRS, accuracy=accuracy))
                print(f"  decay={decay:<6} seed={seed:<3} {accuracy:.3f}",
                      flush=True)
    else:
        pairs = (args.pairs,) if args.pairs is not None else PAIRS
        widths = (args.width,) if args.width is not None else WIDTHS
        cost = sum(relative_cost(d) for d in widths) * len(pairs) * len(seeds)
        print(f"{len(pairs)} pairs x {len(widths)} widths x {len(seeds)} seeds; "
              f"cost ~{cost:.0f} units where the d_model=16 cell is 1", flush=True)
        for n_pairs in pairs:
            task = replace(BASE, n_pairs=n_pairs)
            for d_model in widths:
                for seed in seeds:
                    accuracy = run(task, d_model=d_model, seed=seed)
                    records.append(dict(
                        condition=f"n_pairs={n_pairs} d={d_model}", seed=seed,
                        n_pairs=n_pairs, d_model=d_model, accuracy=accuracy,
                        floor=task.trivial_floor))
                    print(f"  n_pairs={n_pairs:<3} d={d_model:<5} "
                          f"seed={seed:<3} {accuracy:.3f}", flush=True)

    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
