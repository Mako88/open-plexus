"""Does a gate work once the filler statistics stop being adversarial?

Note 013 measured the salience gate as genuinely selective -- query positions
fire at 7.6x the filler rate -- and losing anyway. The diagnosis was the base
rate: filler is 92% of an MQAR sequence and, being drawn UNIFORMLY, is also the
most surprising content in it.

**That diagnosis has never been tested**, and it is the difference between
"surprise-driven storage does not work" and "surprise-driven storage does not
work on this benchmark". Everything g8-01 measures rests on which is true.

Real language is Zipfian, and the property that matters is that the rare token is
the INFORMATIVE one. MQAR has it backwards by construction. This sweeps the
filler exponent and asks whether recovery moves.

**Only the filler distribution changes.** Values stay uniform, so the trivial
floor and every baseline built on it remain valid -- see the pre-registration in
experiments/sweeps/g8-02-when-the-statistics-are-real.txt.

The arms, the training loop and the recovery metric are imported from g8-01
rather than copied. Two sweeps asking the same question of different data are
only comparable if that machinery is literally the same code.

    python experiments/g8_02_zipf_gate.py --scale 1.0 --lr 0.05 --json out/x.json
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.g8_01_real_gate import (  # noqa: E402
    ARMS, BASE, N_TEST, N_TRAIN, build, run)
from experiments.harness import emit, parse_args  # noqa: E402

# Pinned rather than swept, and this is a real narrowing of the question. 768 is
# where g8-01's control put the oracle's advantage at 0.293, large enough for a
# ratio to mean something; 192 was 0.043 and near-vacuous. Half-life 0.25 is
# mid-range from g7-04. This asks whether the exponent moves recovery at ONE
# operating point, not whether it moves it everywhere -- sweeping all four axes
# would be several hundred jobs to answer what a single column answers if the
# effect exists at all.
SEQ_LEN, HALF_LIFE = 768, 0.25

# 0.0 is uniform, so the grid contains its own control and no separate arm is
# needed to show the dial does something.
EXPONENTS = (0.0, 0.5, 1.0, 1.5, 2.0)
LEARNING_RATES = (0.02, 0.05, 0.1)
SEEDS = (1, 2, 3)


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    exponents = (args.scale,) if args.scale is not None else EXPONENTS
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed else SEEDS

    records = []
    for zipf_s in exponents:
        task = replace(BASE, seq_len=SEQ_LEN, filler="zipf", zipf_s=zipf_s)
        for seed in seeds:
            # One dataset per (exponent, seed), shared by every arm, so the arms
            # differ only in the mechanism and never in the data they saw.
            train_set = build(task, N_TRAIN, seed)
            test_set = build(replace(task, seed=task.seed + 99_991), N_TEST, seed)
            for lr in rates:
                for arm in ARMS:
                    records.append(run(task, HALF_LIFE, lr, arm, seed,
                                       train_set, test_set,
                                       extra={"zipf_s": zipf_s}))
    emit(records, Path(args.json) if args.json else None)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
