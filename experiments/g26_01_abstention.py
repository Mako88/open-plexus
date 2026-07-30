"""Can it decline to answer? The occupancy gate asked a question nobody has asked it.

**What this does not duplicate.** Searched across `openplexus/`, `tools/`, `tests/` and
`experiments/`: `openplexus/render.py` DECLINES on an empty set, and that is the only
abstention surface that exists. `index_prefer="inherit"` uses occupancy to choose BETWEEN
addresses (decision 148) and never to stay quiet. Nothing decides to abstain and no task
scores it -- `docs/options/declining-to-answer.md` says exactly that. So the gate is
reused, not rebuilt, and what is new is the decision and the scoring.

## The mechanism, and its stated limit

`AddressSketch` answers *"was anything ever written here"* exactly: an address never
written misses the hash table and reads **0.0**. That is a fact about storage rather than
a confidence, which is why decision 148 could put a structural bar at zero instead of a
fitted one.

**The limit is in the record and this does not test past it:** the gate is *"unable to say
'I do not know' about a question whose addresses are all occupied"*. A written-but-wrong
answer is invisible to it. What is in scope is the common case -- a known entity and a
known relation whose PAIR was never written.

## Why the key comes from the model

`model.context_key(previous, token)` is the model's own construction. Rebuilding it here
would risk asking the sketch about an address the store never used, which would return 0.0
for every question and read as perfect abstention.

Predictions are in `experiments/sweeps/g26-01-can-it-decline-to-answer.txt`, committed at
`b8e80a4` before this file existed.
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.kinship import KinshipConfig, dataset  # noqa: E402

WIDTH = 256
#: **P4's axis is NOT EXPOSED**, and finding that out is part of the result. Note 071
#: measured the sketch's false-hit rate at 0.0044-0.0100 on 16 bits and 0.0004-0.0007 on
#: 24, but `local_memory.py` constructs `AddressSketch(d, seed=...)` with no width in
#: `LocalMemoryConfig` -- so the default 16 is the only setting reachable from config, and
#: the trade note 071 measured cannot be made without editing the model.
BITS = (16,)


def one_cell(bits: int, seed: int) -> dict:
    task = KinshipConfig(seed=seed)
    sequences = dataset(task, 40)
    config = LocalMemoryConfig(
        d_model=WIDTH, vocab_size=task.vocab_size, seed=seed,
        derived_keys=True, context_keys=True,
        track_occupancy=True)
    model = LocalAssociativeMemory(config)

    tokens = np.array(sequences[0].tokens, dtype=np.int64)
    model.run(tokens)
    if model.occupied is None:
        raise SystemExit("track_occupancy did not keep a sketch; the arm is off")

    # ANSWERABLE: pairs the sequence actually wrote. UNANSWERABLE: pairs built from
    # the SAME token alphabet that never occur adjacently. Both halves of an
    # unanswerable pair are known -- only the pair is absent -- which is what stops
    # this measuring vocabulary rather than addressing.
    written = {(int(tokens[t - 1]), int(tokens[t])) for t in range(1, len(tokens))}
    known = sorted({int(v) for v in tokens})
    rng = np.random.default_rng(seed + 5000)
    unwritten = []
    while len(unwritten) < len(written) and len(unwritten) < 200:
        pair = (int(rng.choice(known)), int(rng.choice(known)))
        if pair not in written:
            unwritten.append(pair)

    def empty(pair) -> bool:
        return float(model.occupied.count(model.context_key(*pair))) == 0.0

    answerable = sorted(written)[:200]
    false_abstention = sum(empty(p) for p in answerable) / len(answerable)
    correct_abstention = (sum(empty(p) for p in unwritten) / len(unwritten)
                          if unwritten else float("nan"))
    return dict(bits=bits, seed=seed,
                false_abstention=false_abstention,
                correct_abstention=correct_abstention,
                answerable=len(answerable), unanswerable=len(unwritten),
                condition=f"bits{bits}|seed{seed}|d{WIDTH}")


def main() -> None:
    args = harness.parse_args(__doc__)
    seeds = (0, 1, 2) if args.seed is None else (args.seed,)
    records = [one_cell(bits, seed) for seed in seeds for bits in BITS]
    if args.json:
        harness.emit(records, Path(args.json))

    print(f"\n{'sketch bits':>12}{'false abstention':>19}{'correct abstention':>21}")
    for bits in BITS:
        rows = [r for r in records if r["bits"] == bits]
        false = sum(r["false_abstention"] for r in rows) / len(rows)
        right = sum(r["correct_abstention"] for r in rows) / len(rows)
        print(f"{bits:>12}{false:>19.4f}{right:>21.4f}")
    print(f"\n  answerable {records[0]['answerable']}, "
          f"unanswerable {records[0]['unanswerable']}, per seed")
    print("  BOTH are always printed: abstaining always scores 1.0000 correct "
          "and 1.0000 false, and degenerates exactly.")


if __name__ == "__main__":
    main()
