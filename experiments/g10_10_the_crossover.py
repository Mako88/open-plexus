"""At a FIXED memory budget, does the store ever beat a cache?

This is the argument for superposition stated as a measurement, and
[note 030](../docs/archive/notes/030-the-benchmark-does-not-discriminate.md) flagged it
as the thing nobody had checked:

> A cache's cost scales with the number of distinct keys. A superposed store's
> cost is FIXED at `w x d` however many items it holds -- it simply degrades.

At 24 bindings a cache wins overwhelmingly: 48 integers against 4,096 numbers
([g10-07](sweeps/g10-07-can-a-cache-do-the-gating-task.txt)). At a million
bindings a cache needs a million entries and the store still needs `w x d`. **If
there is a crossover, that number is the project's justification. If there is
not, the honest answer to John's third question is a DHT.**

## The comparison, made fair on purpose

Both structures get the SAME budget in numbers held. Neither is given the item
count in advance.

    STORE   a `w x d` matrix. Every binding is superposed into it. Cost is the
            matrix, whatever `n` is.
    CACHE   `budget / 2` entries, each a cue id and a value id. Holds the most
            recent it can and evicts the oldest -- the only policy available
            without knowing the future.

**Counting a token id and a float alike flatters the CACHE**, since an id at
this vocabulary fits in a byte where a weight takes eight. Understated against
the interesting option is the safe way round, and it is the same choice
`slot_cost.py` made.

## What the answer decides

    a crossover exists              -> superposition earns its place above some
                                       item count, and that number is the
                                       project's central justification
    the cache wins at every n       -> at a fixed budget a table is simply
                                       better here, and the honest answer to
                                       "can traditional computing replace it"
                                       is yes for this workload
    both collapse together          -> the budget is the binding constraint and
                                       neither structure is the question

## What this cannot say

Nothing about learning, generalisation or emergence. This is exact recall of
`n` arbitrary pairs, which is the one thing both structures do directly and the
only thing that can be compared without a readout in the way.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments.harness import parse_args  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB = 4096
#: Numbers held. A width-64 store at d_model 64 is 4,096, which is the g9 line's
#: working size and makes the comparison concrete rather than abstract.
BUDGETS = (1024, 4096, 16384)
ITEMS = (8, 16, 32, 64, 128, 256, 512, 1024, 2048)
TRIALS = 300


def store_recall(budget: int, n: int, rng) -> float:
    """Superpose `n` bindings into a square store of `budget` numbers."""
    width = int(round(budget ** 0.5))
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=width, lr=0.05, key_scale=0.5, decay=1.0,
        derived_keys=True, seed=1))
    keys, values = np.array(model.wk), model.wv
    cues = rng.choice(VOCAB, size=n, replace=False)
    items = rng.choice(VOCAB, size=n)
    memory = np.zeros((width, width))
    for cue, item in zip(cues, items):
        memory += np.outer(values[item], keys[cue])
    right = 0
    picks = rng.integers(n, size=min(TRIALS, n * 4))
    for index in picks:
        scores = values @ (memory @ keys[cues[index]])
        right += int(scores.argmax()) == int(items[index])
    return right / len(picks)


def cache_recall(budget: int, n: int) -> float:
    """A table of `budget // 2` entries, evicting the oldest.

    Exact by construction for whatever it still holds, so this is simply the
    share of bindings that survived -- and that is the point: a table's failure
    is entirely about capacity, never about interference.
    """
    capacity = budget // 2
    return min(1.0, capacity / n)


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    rng = np.random.default_rng(1)
    budgets = [int(args.scale)] if args.scale is not None else list(BUDGETS)

    print(f"vocabulary {VOCAB}, {TRIALS} probes per cell\n")
    records = []
    for budget in budgets:
        width = int(round(budget ** 0.5))
        print(f"== budget {budget} numbers: a {width}x{width} store, or "
              f"{budget // 2} cache entries ==")
        print(f"{'items':>8}{'store':>9}{'cache':>9}   winner")
        crossed = None
        for n in ITEMS:
            store = store_recall(budget, n, rng)
            cache = cache_recall(budget, n)
            records.append({"budget": budget, "items": n, "store": store,
                            "cache": cache})
            winner = ("store" if store > cache + 0.02 else
                      "cache" if cache > store + 0.02 else "tie")
            if winner == "store" and crossed is None:
                crossed = n
            print(f"{n:>8}{store:>9.3f}{cache:>9.3f}   {winner}")
        if crossed:
            print(f"  -> CROSSOVER at {crossed} items: above this the store "
                  f"wins at equal memory\n")
        else:
            print("  -> no crossover: the cache is at least as good at every "
                  "item count tested\n")

    any_crossover = any(
        r["store"] > r["cache"] + 0.02 for r in records)
    if any_crossover:
        print("SUPERPOSITION EARNS ITS PLACE above some item count, and that")
        print("count is the project's central justification.")
    else:
        print("NO CROSSOVER ANYWHERE TESTED. At a fixed budget a table is at")
        print("least as good at every item count here, and the honest answer to")
        print("'can traditional computing replace this' is yes for this")
        print("workload. Note the range tested: items up to "
              f"{ITEMS[-1]}, budgets up to {BUDGETS[-1]}.")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=1))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
