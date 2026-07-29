"""Does the store carry the relational tasks, or is that an inference too?

## Why this exists

Five sweeps on the corrected harness say the store contributes ~nothing on text:
+0.002 bits at word level (139), 0.540 bits from address density that any
grouping captures and a prior subsumes (141), and -0.568 at the rate every text
sweep ever used (140).

Every one of those records then says the same protective sentence: *"this does
not touch the relational line — MQAR, kinship and the chains are solved through
this store and no prior solves them."*

**That sentence is an inference, and tonight is a lesson in what inferences are
worth.** It is a good inference — MQAR's symbols are drawn at random, so a prior
genuinely cannot help — but the `nostore` ablation has never been run on it. The
same ablation that overturned four sweeps of text results has not been pointed at
the results the project actually rests on.

So this points it there. **If the store carries MQAR, the whole text line is
about text being the wrong instrument.** If it does not, the problem is much
larger than addressing and the last week has been the least of it.

## The arms

    floor     the model as every MQAR number was measured
    nostore   nothing ever written; the readout has only what it can learn
              from a retrieval that is always zero

Both at `bias 0` — MQAR's marginals are uniform by construction, so a prior has
nothing to offer and the comparison set never had one — and at `bias 1`, because
a bias that helps *here* would mean the task has a base rate nobody intended.

## PREDICTIONS (a gate, a rail, a falsifier)

  P1  THE GATE. `floor` beats `nostore` by more than 0.30 accuracy. The store
      carries the task, the protective sentence in five records is true, and the
      text results are about text.

  P2  THE RAIL. `nostore` sits at the trivial floor -- `1/n_pairs + (1 -
      1/n_pairs)/n_values`, which is what guessing scores. With no store there is
      nothing to recall from, so anything above it is something else learning and
      every ablation in this line is misattributed.

  P3  THE FALSIFIER. The readout bias does NOT help: `floor` at bias 1 is within
      0.05 of `floor` at bias 0. MQAR's values are uniform by construction, so a
      prior that paid here would mean the generator has a base rate and every
      MQAR number in this project is partly a base-rate score.

**What would be the largest result:** P1 refuted. The store would carry neither
text nor the task it was built for, and the architecture question stops being
about addressing entirely.
"""

from __future__ import annotations

import argparse
import json
import sys
import time
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

#: g10-01's configuration, so a number here is comparable to the MQAR line
#: rather than being a fresh baseline.
#: `autoregressive=True` and `filler="random"` are g10-01's, and BOTH are
#: load-bearing rather than decoration. With `autoregressive=False` -- the
#: dataclass default -- the answer is supplied in `sequence.targets` and is NOT
#: the next token, so the `np.roll(tokens, -1)` every MQAR script uses scores
#: against the wrong symbol entirely. The first version of this file omitted the
#: flag and both arms returned 0.055 against a 0.34 trivial floor.
TASK = MqarConfig(n_pairs=4, seq_len=64, n_keys=16, n_values=8,
                  autoregressive=True, filler="random", seed=11)
WIDTH = 64
KEY_SCALE = 0.5
#: **0.99, FROM g10-01, AND THIS WAS WRONG ONCE ALREADY.** The first version set
#: decay 1.0 -- the value the word-level work settled on hours earlier -- and
#: both arms came back at 0.06 against a 0.34 trivial floor, i.e. below chance.
#: That is note 046's mistake for the fourth time in one night, and the trivial
#: floor is what caught it before anything was dispatched.
DECAY = 0.99
EPOCHS = 6
TRAIN = 200
TEST = 100
SEEDS = (0, 1, 2)
#: What guessing scores: the query key is one of `n_pairs` seen, and otherwise
#: one of `n_values`.
TRIVIAL = 1 / TASK.n_pairs + (1 - 1 / TASK.n_pairs) / TASK.n_values


def build(count: int, seed: int):
    """Shared with g10-01 and g4-04 via `harness.mqar_batch`.

    It was a fourth byte-identical copy until the duplication checker refused
    it, which is the check doing its job on the day it mattered.
    """
    return harness.mqar_batch(TASK, count, seed)


def silent(tokens: np.ndarray) -> np.ndarray:
    """A storage mask that writes nothing. The ablation."""
    return np.zeros(len(tokens), dtype=bool)


def one_cell(arm: str, bias: bool, seed: int) -> dict:
    started = time.time()
    writes = arm != "nostore"
    train_set = build(TRAIN, seed + 100)
    test_set = build(TEST, seed + 900)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=TASK.vocab_size, d_model=WIDTH, lr=0.05,
        key_scale=KEY_SCALE, decay=DECAY, readout_bias=bias, seed=seed))

    rng = np.random.default_rng(seed)
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, _ = train_set[index]
            model.run(tokens, targets, scored, learn=True,
                      store=None if writes else silent(tokens))

    right = total = 0
    for tokens, targets, _, queries in test_set:
        predictions = model.run(tokens,
                                store=None if writes else silent(tokens))
        for position in queries:
            right += int(predictions[position] == targets[position])
            total += 1

    return dict(
        arm=arm, bias=bias, seed=seed,
        accuracy=round(right / max(total, 1), 4),
        trivial=round(TRIVIAL, 4),
        scored=int(total),
        width=WIDTH, epochs=EPOCHS, train=TRAIN,
        vocab=TASK.vocab_size, n_pairs=TASK.n_pairs, seq_len=TASK.seq_len,
        seconds=round(time.time() - started, 1),
        condition=f"{arm}|bias{int(bias)}|d{WIDTH}|seed{seed}"
                  f"|pairs{TASK.n_pairs}|len{TASK.seq_len}|epochs{EPOCHS}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--arm", choices=("floor", "nostore"), default=None)
    parser.add_argument("--bias", type=int, choices=(0, 1), default=None)
    parser.add_argument("--json", type=str, default=None)
    args = parser.parse_args()

    harness.refuse_if_mutating()
    seeds = (args.seed,) if args.seed is not None else SEEDS
    arms = (args.arm,) if args.arm else ("floor", "nostore")
    biases = (bool(args.bias),) if args.bias is not None else (False, True)

    records = []
    for seed in seeds:
        for bias in biases:
            for arm in arms:
                record = one_cell(arm, bias, seed)
                print(f"  {record['condition']:52s} "
                      f"accuracy {record['accuracy']:.4f}",
                      file=sys.stderr, flush=True)
                records.append(record)

    if args.json:
        path = Path(args.json)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(records, indent=1), encoding="utf-8")
        print(f"wrote {len(records)} records to {path}")
    else:
        for record in records:
            print(f"{record['condition']}  accuracy {record['accuracy']}")
        print(f"trivial floor {TRIVIAL:.4f}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
