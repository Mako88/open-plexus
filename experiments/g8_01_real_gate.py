"""How much of the oracle gate's advantage can a real mechanism recover?

Three headline results -- g7-02, g7-03 and the tiny-node claim itself -- run
through `oracle_mask`, which reads `position_kinds()` and tells the model which
of its own positions are worth storing. A deployed system has no such signal.
This asks what is left when it is taken away.

**The two interventions are not the same shape, and the design turns on that.**
The oracle PREVENTS writing, holding the number of stored bindings at twice the
pair count whatever the sequence length -- and retrieval goes as
`sqrt(width / stored)`, which is the whole reason length stopped mattering in
g7-02. Consolidation does not prevent anything; it PROTECTS particular bindings
from fading.

So consolidation alone was never going to substitute for the oracle, and testing
it that way would have measured the mismatch rather than the mechanism. What can
substitute is the pair: **a fast store that forgets quickly keeps the effective
count small, and consolidation keeps the few that were confirmed useful.** That
is tagging and capture as note 010 describes it -- write everything weakly, let
a later signal decide what survives -- and it is why the decay grid is swept
here rather than held fixed.

    python experiments/g8_01_real_gate.py --seqlen 384 --decay 0.25 --lr 0.05 --json out/x.json
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.harness import emit, oracle_mask, parse_args  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

# queries_per_pair=3 is the point. Plain MQAR asks about each binding exactly
# once, at the end, so consolidate-on-use has no second occasion to be confirmed
# on and the arm cannot be tested at all. Verified bit-identical to the old
# generator at the default by tests/test_recurrent_mqar.py.
BASE = MqarConfig(n_pairs=4, seq_len=192, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260726,
                  queries_per_pair=3)

SEQ_LENS = (192, 384, 768)
HALF_LIVES = (0.5, 0.25, 0.125)
LEARNING_RATES = (0.02, 0.05, 0.1)
SEEDS = (1, 2, 3)
D_MODEL, N_TRAIN, N_TEST, EPOCHS, KEY_SCALE = 32, 400, 120, 8, 0.5

# Held fixed rather than swept, and this is a real limitation of the sweep.
# g7-04 measured consolidation at 1.0 as actively harmful and 0.1 as
# indistinguishable from none, so 0.1 is the gentle end where it has any chance.
# The salience bar and cap come from note 013: 2.5 is mid-range, and the cap has
# to BIND or the compensatory process does nothing -- 0.2 binds at this width,
# 0.5 does not. Three hand-chosen constants is three ways for this to be a test
# of the constants, which the write-up must say plainly.
CONSOLIDATION, SALIENCE, LASTING_CAP = 0.1, 2.5, 0.2

#: name -> (uses the oracle, consolidation rate, salience bar, cap)
ARMS = {
    "none":     (False, 0.0, 0.0, 0.0),          # the FLOOR
    "oracle":   (True, 0.0, 0.0, 0.0),           # the CEILING, and a cheat
    "on-use":   (False, CONSOLIDATION, 0.0, 0.0),
    "salience": (False, CONSOLIDATION, SALIENCE, LASTING_CAP),
}


def decay_for(seq_len: int, half_life: float) -> float:
    """Per-step decay giving a half-life of `half_life * seq_len` steps.

    g7-04 established that a sweep varying length cannot hold an ABSOLUTE decay
    fixed: 0.95 is a gentle fade at 192 and an erasure at 1536, so the arm would
    measure the grid rather than the mechanism.
    """
    return float(0.5 ** (1.0 / (half_life * seq_len)))


def build(task: MqarConfig, count: int, seed: int):
    """Sequences plus their oracle masks, built once and shared by all arms."""
    built = []
    for sequence in dataset(replace(task, seed=seed), count):
        tokens = np.asarray(sequence.tokens)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        built.append((tokens, targets, scored, oracle_mask(sequence.position_kinds()),
                      sequence.query_positions))
    return built


def run(seq_len: int, half_life: float, lr: float, arm: str, seed: int,
        train_set, test_set) -> dict:
    gated, consolidation, salience, cap = ARMS[arm]
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=BASE.vocab_size, d_model=D_MODEL, lr=lr,
        key_scale=KEY_SCALE, decay=decay_for(seq_len, half_life),
        consolidation=consolidation, salience=salience, lasting_cap=cap,
        seed=seed))

    rng = np.random.default_rng(seed)
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, keep, _ = train_set[index]
            model.run(tokens, targets, scored, learn=True,
                      store=keep if gated else None)

    right = total = 0
    for tokens, _, _, keep, queries in test_set:
        predicted = model.run(tokens, store=keep if gated else None)
        for q in queries:
            right += predicted[q] == tokens[q + 1]
            total += 1
    accuracy = right / total

    print(f"  seq={seq_len:<5} half={half_life:<6} lr={lr:<5} "
          f"arm={arm:<9} seed={seed}  {accuracy:.3f}", flush=True)
    return dict(condition=f"seq={seq_len} half={half_life} lr={lr} arm={arm}",
                seed=seed, seq_len=seq_len, half_life=half_life, lr=lr,
                arm=arm, accuracy=accuracy)


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    seq_lens = (args.seqlen,) if args.seqlen else SEQ_LENS
    half_lives = (args.decay,) if args.decay else HALF_LIVES
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed else SEEDS

    records = []
    for seq_len in seq_lens:
        task = replace(BASE, seq_len=seq_len)
        for seed in seeds:
            # One dataset per (length, seed), shared by every arm, so the arms
            # differ only in the mechanism and never in the data they saw.
            train_set = build(task, N_TRAIN, seed)
            test_set = build(replace(task, seed=task.seed + 99_991), N_TEST, seed)
            for half_life in half_lives:
                for lr in rates:
                    for arm in ARMS:
                        records.append(run(seq_len, half_life, lr, arm, seed,
                                           train_set, test_set))
    emit(records, Path(args.json) if args.json else None)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
