"""Is the crossing set by the alphabet or by the load?

g1-06 refuted the interference account: holding four things or sixteen barely
moved the width the local rule needs, and at small widths more was *better*. The
replacement guess was that the crossing tracks `n_keys` — how many distinct
symbols exist — rather than `n_pairs` — how many are stored at once. It has been
untested since.

Run at the tuned `key_scale` from g1-08, not the default that made the earlier
curves unreliable.

    python experiments/g1_09_alphabet.py --keys 64 --width 32 --seed 3 --json out/x.json
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.g1_08_honest_price import score_local  # noqa: E402
from experiments.harness import emit, parse_args  # noqa: E402
from openplexus.tasks.mqar import MqarConfig  # noqa: E402

#: g1-08's best scale across the crossing region (widths 24-32). Using the tuned
#: value rather than the default is the whole reason this sweep is trustworthy
#: where g1-06's was not.
KEY_SCALE = 0.5
KEYS = (8, 16, 32, 64, 128)
WIDTHS = (16, 24, 32, 48, 64)
SEEDS = tuple(range(1, 6))


def main() -> int:
    args = parse_args(__doc__)
    seeds = (args.seed,) if args.seed is not None else SEEDS
    keys = (args.keys,) if args.keys else KEYS
    widths = (args.width,) if args.width else WIDTHS

    records = []
    for n_keys in keys:
        for width in widths:
            for seed in seeds:
                # score_local reads its task from g1_08's module-level TASK, so
                # the alphabet is varied by patching that one field — keeping a
                # single definition of how a model is trained and scored rather
                # than a second copy that could drift (rule 9).
                import experiments.g1_08_honest_price as g108
                g108.TASK = replace(
                    MqarConfig(n_pairs=4, seq_len=96, n_keys=32, n_values=8,
                               autoregressive=True, filler="random",
                               seed=20260725),
                    n_keys=n_keys)
                accuracy = score_local(width, KEY_SCALE, seed)
                records.append(dict(
                    condition=f"keys={n_keys} d={width}", seed=seed,
                    n_keys=n_keys, d_model=width, scale=KEY_SCALE,
                    accuracy=accuracy))
                print(f"  n_keys={n_keys:<5} d={width:<4} seed={seed:<3} "
                      f"{accuracy:.3f}", flush=True)
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
