"""Is width even the right unit? — the price re-examined.

The 4.0x figure compares *widths*. But the two systems hold different things:
attention keeps keys and values for every position it has seen (2·T·d), while the
local rule holds one d×d matrix regardless of how long the sequence is.

So "4x the width" sets a number that grows with the input against one that does
not. This measures the attention crossing across sequence lengths — it has only
ever been measured at seq_len=96 — so the comparison can be made in working
memory as well as width.

    python experiments/g1_11_right_unit.py --seqlen 192 --width 8 --seed 3 --json out/x.json
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import experiments.g1_08_honest_price as g108  # noqa: E402
from experiments.harness import emit, parse_args  # noqa: E402
from openplexus.tasks.mqar import MqarConfig  # noqa: E402

#: g1-08 found 0.4 best for attention at every width where it solved at all.
ATTENTION_SCALE = 0.4
SEQ_LENS = (48, 96, 192, 384)
WIDTHS = (4, 8, 16)
#: Three seeds rather than five. The seq_len=384 cells cost ~64x the 48 ones —
#: attention is quadratic in sequence length — and the crossing in g1-08 was
#: sharp enough (0.023 -> 0.925 between adjacent widths) that three suffice to
#: locate it. Stated rather than silently reduced.
SEEDS = (1, 2, 3)
BASE = MqarConfig(n_pairs=4, seq_len=96, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260725)

#: Measured in g1-10 at key_scale 0.5, interpolated to the 0.9 threshold.
LOCAL_CROSSINGS = {48: 21.8, 96: 26.0, 192: 34.0, 384: 47.5}


def main() -> int:
    args = parse_args(__doc__)
    seeds = (args.seed,) if args.seed is not None else SEEDS
    seq_lens = (args.seqlen,) if args.seqlen else SEQ_LENS
    widths = (args.width,) if args.width else WIDTHS

    records = []
    for seq_len in seq_lens:
        for width in widths:
            for seed in seeds:
                g108.TASK = replace(BASE, seq_len=seq_len)
                accuracy = g108.score_attention(width, ATTENTION_SCALE, seed)
                records.append(dict(
                    condition=f"seq={seq_len} d={width}", seed=seed,
                    seq_len=seq_len, d_model=width, accuracy=accuracy,
                    attention_memory=2 * seq_len * width,
                    local_crossing=LOCAL_CROSSINGS.get(seq_len)))
                print(f"  seq_len={seq_len:<5} d={width:<4} seed={seed:<3} "
                      f"{accuracy:.3f}", flush=True)
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
