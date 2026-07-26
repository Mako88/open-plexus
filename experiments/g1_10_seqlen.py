"""Is the load sequence length? — the variable nobody moved.

g1-05 explained the graded transition as superposition interference. g1-06
refuted it by sweeping `n_pairs`; g1-09 refuted its replacement by sweeping
`n_keys`. Reading the store afterwards showed why both were flat: it binds
*every* consecutive pair, so the memory holds `seq_len - 1` bindings and
`n_pairs` is a small fraction of them.

So the interference account may have been right in mechanism and wrong only in
which variable carries the load — and `seq_len` has been pinned at 96 in every
sweep this project has run, including the ones that refuted it.

    python experiments/g1_10_seqlen.py --seqlen 192 --width 64 --seed 3 --json out/x.json
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import experiments.g1_08_honest_price as g108  # noqa: E402
from experiments.harness import emit, parse_args  # noqa: E402
from openplexus.tasks.mqar import MqarConfig  # noqa: E402

KEY_SCALE = 0.5
SEQ_LENS = (48, 96, 192, 384)
WIDTHS = (16, 24, 32, 48, 64, 96, 128)
SEEDS = tuple(range(1, 6))
BASE = MqarConfig(n_pairs=4, seq_len=96, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260725)


def main() -> int:
    args = parse_args(__doc__)
    seeds = (args.seed,) if args.seed is not None else SEEDS
    seq_lens = (args.seqlen,) if args.seqlen else SEQ_LENS
    widths = (args.width,) if args.width else WIDTHS

    records = []
    for seq_len in seq_lens:
        for width in widths:
            for seed in seeds:
                # Patch the task g1_08.score_local reads, so there is one
                # definition of how a model is trained and scored (rule 9).
                g108.TASK = replace(BASE, seq_len=seq_len)
                accuracy = g108.score_local(width, KEY_SCALE, seed)
                records.append(dict(
                    condition=f"seq={seq_len} d={width}", seed=seed,
                    seq_len=seq_len, d_model=width, bindings=seq_len - 1,
                    accuracy=accuracy))
                print(f"  seq_len={seq_len:<5} d={width:<4} seed={seed:<3} "
                      f"{accuracy:.3f}", flush=True)
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
