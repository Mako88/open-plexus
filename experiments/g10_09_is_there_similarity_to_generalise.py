"""Does this store have similarity generalisation, or nothing to generalise over?

[Note 030](../docs/archive/notes/030-the-benchmark-does-not-discriminate.md) lists four
properties separating a superposed store from a cache. Churn was measured and did
not favour the store ([g10-08](sweeps/g10-08-which-degrades-better.txt)). This
asks about the first and most-cited one, and it can be settled by arithmetic
before any task is built.

**A superposed store answers a key it has never been given exactly.** Retrieval is
`memory @ key`, which sums every stored value weighted by how much its key
overlaps this one. A cache returns nothing for a miss. That is the textbook
argument for the store, and it is the reason "graceful" keeps being reached for.

**But it only pays if keys OVERLAP in a way that means something.** With
`derived_keys` every token's key is drawn from `(seed, token)` — independent
gaussians, no relation between token 5 and token 6. If the overlaps are pure
noise, the store generalises to *nothing in particular*: a near-miss returns a
blend weighted by accidental correlations.

So there are two questions and the second only matters if the first says yes:

1. **Is there structure in the key overlaps at all?** Measurable directly.
2. **Does a partial or corrupted key retrieve the right value more often than
   chance?** This is the property itself.

## Why this is worth doing before building a task

Note 030 proposed finding a task that exercises similarity generalisation. **If
the store has none to exercise, that task cannot be built**, and the honest
conclusion about property 1 is available today for the cost of a matrix
multiply. Building the task first and discovering this afterwards would be the
same mistake as measuring churn on a task that could not settle it.
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

VOCAB, WIDTH = 73, 64
#: How much of the query key is replaced by noise. 0.0 is the exact key.
CORRUPTION = (0.0, 0.1, 0.25, 0.5, 0.75, 1.0)
PAIRS = 8


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    width = args.width if args.width else WIDTH
    rng = np.random.default_rng(1)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=width, lr=0.05, key_scale=0.5, decay=1.0,
        derived_keys=True, seed=1))
    keys = np.array(model.wk)
    values = model.wv

    # 1. Is there structure in the overlaps?
    overlap = keys @ keys.T
    off = overlap[~np.eye(VOCAB, dtype=bool)]
    diagonal = float(np.mean(np.diag(overlap)))
    print(f"vocabulary {VOCAB}, width {width}\n")
    print("== 1. is there structure in the key overlaps? ==")
    print(f"  a key with itself:   {diagonal:.4f}")
    print(f"  with any other:      mean {off.mean():+.4f}, "
          f"sd {off.std():.4f}, largest {np.abs(off).max():.4f}")
    print(f"  ratio of the largest off-diagonal to the diagonal: "
          f"{np.abs(off).max() / diagonal:.3f}")
    print("  keys are drawn independently per token, so any overlap is")
    print("  accidental -- there is no sense in which token 5 resembles token 6")

    # 2. Does a corrupted key still retrieve its value?
    cues = rng.choice(VOCAB, size=PAIRS, replace=False)
    items = rng.choice(VOCAB, size=PAIRS)
    memory = np.zeros((width, width))
    for cue, item in zip(cues, items):
        memory += np.outer(values[item], keys[cue])

    print("\n== 2. does a CORRUPTED key retrieve the right value? ==")
    print(f"  {PAIRS} bindings in one store, "
          f"{'chance is 1/' + str(VOCAB):>16} = {1 / VOCAB:.3f}")
    print(f"{'corruption':>12}{'accuracy':>11}")
    records = []
    for share in CORRUPTION:
        right = 0
        for _ in range(200):
            index = rng.integers(PAIRS)
            key = keys[cues[index]].copy()
            mask = rng.random(width) < share
            key[mask] = rng.normal(0.0, model.config.key_scale / np.sqrt(width),
                                   int(mask.sum()))
            scores = values @ (memory @ key)
            right += int(scores.argmax()) == int(items[index])
        accuracy = right / 200
        records.append({"corruption": share, "accuracy": accuracy})
        print(f"{share:>12.2f}{accuracy:>11.3f}")

    exact = records[0]["accuracy"]
    half = next(r for r in records if r["corruption"] == 0.5)["accuracy"]
    print()
    if exact < 0.5:
        print("  -> it cannot even retrieve on an EXACT key at this load, so")
        print("     there is nothing to say about near-misses")
    elif half > exact * 0.5:
        print("  -> a half-corrupted key still retrieves well. The store DOES")
        print("     generalise over key noise, which a cache cannot do at all")
        print("     -- and note that this is robustness to a DEGRADED QUERY,")
        print("     not similarity between different tokens")
    else:
        print("  -> retrieval falls away with corruption. What survives is")
        print("     tolerance of small noise, not generalisation to related")
        print("     keys -- because with derived keys there ARE no related keys")

    print("\n  THE LIMIT, STATED: `derived_keys` draws an independent vector per")
    print("  token, so there is no similarity STRUCTURE between tokens for the")
    print("  store to exploit. Property 1 of note 030 is about robustness to a")
    print("  corrupted query here, not about generalising between related items,")
    print("  and no task in this project ever presents a corrupted query.")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=1))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
