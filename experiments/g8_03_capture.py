"""Does a finite pool hold N constant, and does that recover anything?

[Note 015](../docs/notes/015-we-implemented-the-tag-and-not-the-competition.md):
synaptic capture is competitive over a finite pool, note 010 implemented the tag
and left the pool unbounded, and that omission predicts g8-01's worst number.

Retrieval goes as `sqrt(d / N)`. A threshold fires at a RATE, so `N` grows with
sequence length and recovery must fall with it -- measured, 0.05 at seq 192 to
-0.00 at 1536. A pool of `k` slots sets a QUANTITY, so `N = k` whatever the
length.

**The measurement is the SHAPE of the recovery curve, not its height.** Recovery
could stay flat at 0.02 and still show the mechanism doing exactly what it was
built for while being useless, and those are different findings that must not be
merged.

The training loop, the metric and the floor and ceiling arms are imported from
g8-01, so the two grids are comparable by construction rather than by
inspection. The pool size travels in the arm spec rather than in a module global.

    python experiments/g8_03_capture.py --seqlen 768 --lr 0.05 --workers 3 --json out/x.json
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.g8_01_real_gate import (  # noqa: E402
    ARMS as BASE_ARMS, BASE, CONSOLIDATION, N_TEST, N_TRAIN, build, run)
from experiments.harness import emit, parse_args, spread  # noqa: E402

HALF_LIFE = 0.125                # NOT mid-range: this is where the decline is.
# g8-03 first ran at 0.25 and could not test its own hypothesis, because g8-01's
# recovery was ALREADY FLAT there -- 0.00/0.01/-0.02/-0.00 across the four
# lengths. The 0.05-to-0.00 decline the pool is supposed to flatten lives at
# 0.125. A grid that freezes the load-bearing axis cannot contain its own answer.
SEQ_LENS = (192, 384, 768, 1536)
POOLS = (0, 4, 16)               # 0 is unbounded: g8-01's failing on-use arm
LEARNING_RATES = (0.02, 0.05, 0.1)
SEEDS = (1, 2, 3)

# The floor and the ceiling come from g8-01 unchanged, so this grid contains the
# result it is trying to beat rather than a re-derivation of it. The tag is
# confirmation-on-use throughout, so the ONLY thing varying across the capture
# arms is the pool. Salience is deliberately absent -- note 015 separates tagging
# from capture, and moving both dials at once would confound which did anything.
ARMS = {
    "none": BASE_ARMS["none"],
    "oracle": BASE_ARMS["oracle"],
    **{f"capture-{k}": (False, CONSOLIDATION, 0.0, 0.0, k) for k in POOLS},
}


def one_seed(work: tuple) -> list[dict]:
    """Everything for one (seq_len, seed). Module-level for `spread`'s spawn."""
    seq_len, seed, rates = work
    task = replace(BASE, seq_len=seq_len)
    # One dataset per (length, seed), shared by every arm, so the arms differ
    # only in the mechanism and never in the data they saw.
    train_set = build(task, N_TRAIN, seed)
    test_set = build(replace(task, seed=task.seed + 99_991), N_TEST, seed)

    return [run(task, HALF_LIFE, lr, name, seed, train_set, test_set,
                extra={"capture_slots": spec[4]}, spec=spec)
            for lr in rates for name, spec in ARMS.items()]


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    seq_lens = (args.seqlen,) if args.seqlen else SEQ_LENS
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed else SEEDS

    work = [(seq_len, seed, tuple(rates))
            for seq_len in seq_lens for seed in seeds]
    records = [record for batch in spread(one_seed, work, args.workers)
               for record in batch]
    emit(records, Path(args.json) if args.json else None)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
