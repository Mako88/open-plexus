"""Does STRUCTURE in the data give superposition a crossover?

[g10-10](sweeps/g10-10-the-crossover.txt) found no crossover at any budget and
closed with a hypothesis:

> Superposition is a lossy compression scheme, and `n` independent random pairs
> contain `n` items of irreducible information. So this measures superposition on
> exactly the data where it cannot pay -- and the project's task suite is
> deliberately incompressible.

**That is a hypothesis I wrote, and g10-03 taught what happens when one of those
becomes doctrine before it is tested.** It is testable without any new corpus, so
it gets tested.

## The reason to doubt it before running

A store retrieves cue `i` correctly when interference from the other `n - 1`
items is small enough. That is a property of how many KEYS must be kept apart in
`d` dimensions -- note 020's `sqrt(d/N)` law. **Structure in the VALUES does not
obviously change how many keys can be separated.**

If capacity is key-limited rather than information-limited, then compressible
data buys the store nothing and g10-10's explanation is wrong. A cache would
still need one entry per distinct cue, so it gains nothing either.

## The three conditions

    random          n distinct values, drawn independently. g10-10's condition.
    few values      values drawn from 8 prototypes. The information content of
                    the pairs falls sharply; the number of keys does not.
    few cues        the same 8 cues repeated, each rebound. Both fall.

**If the store's capacity is key-limited, only the third helps it**, and the
compression story is wrong. If `few values` helps, the story is right.
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

VOCAB, BUDGET, PROTOTYPES = 4096, 4096, 8
ITEMS = (16, 64, 128, 256, 512)
TRIALS = 300


def build(kind: str, n: int, rng):
    """(cues, items) for one condition. Cues are what must be kept apart."""
    if kind == "random":
        return rng.choice(VOCAB, size=n, replace=False), rng.choice(VOCAB, n)
    if kind == "few values":
        values = rng.choice(VOCAB, size=PROTOTYPES, replace=False)
        return (rng.choice(VOCAB, size=n, replace=False),
                values[rng.integers(PROTOTYPES, size=n)])
    # few cues: the SAME handful of cues, each rebound repeatedly. Only the last
    # binding of each cue is recoverable, by either structure.
    cues = rng.choice(VOCAB, size=PROTOTYPES, replace=False)
    return cues[rng.integers(PROTOTYPES, size=n)], rng.choice(VOCAB, n)


def chance(items) -> float:
    """Guessing, given how many DISTINCT values this condition contains.

    `few values` draws from 8 prototypes, so guessing scores 0.125 where the
    random condition scores 1/4096. **Comparing raw accuracies across the two
    compares numbers measured against floors five hundred times apart**, and the
    first version of this file did exactly that and concluded that structure
    helps. It does not; the floor moved.
    """
    return 1.0 / len(set(int(i) for i in items))


def store_recall(cues, items, rng) -> float:
    width = int(round(BUDGET ** 0.5))
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=width, lr=0.05, key_scale=0.5, decay=1.0,
        derived_keys=True, seed=1))
    keys, values = np.array(model.wk), model.wv
    memory = np.zeros((width, width))
    for cue, item in zip(cues, items):
        memory += np.outer(values[item], keys[cue])
    # Only the LAST binding of a repeated cue is what anything could return.
    latest = {int(c): int(i) for c, i in zip(cues, items)}
    asked = list(latest)
    right = 0
    picks = rng.integers(len(asked), size=min(TRIALS, len(asked) * 8))
    for index in picks:
        cue = asked[index]
        right += int((values @ (memory @ keys[cue])).argmax()) == latest[cue]
    return right / len(picks)


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    rng = np.random.default_rng(1)
    width = int(round(BUDGET ** 0.5))
    print(f"budget {BUDGET} = a {width}x{width} store, or {BUDGET // 2} cache "
          f"entries; {PROTOTYPES} prototypes\n")
    print("  reported as ACCURACY / CHANCE, because the conditions have")
    print("  different numbers of distinct values and so different floors\n")
    print(f"{'items':>7}{'random':>18}{'few values':>18}{'few cues':>18}")

    records = []
    for n in ITEMS:
        row = {}
        for kind in ("random", "few values", "few cues"):
            cues, items = build(kind, n, rng)
            accuracy = store_recall(cues, items, rng)
            floor = chance(items)
            row[kind] = accuracy / floor
            records.append({"items": n, "condition": kind, "store": accuracy,
                            "chance": floor, "over_chance": row[kind]})
        print(f"{n:>7}" + "".join(
            f"{row[k]:>18.1f}" for k in ("random", "few values", "few cues")))

    big = {r["condition"]: r["over_chance"] for r in records
           if r["items"] == ITEMS[-1]}
    print(f"\n  at {ITEMS[-1]} items, as a multiple of chance: "
          f"random {big['random']:.0f}x, few values {big['few values']:.1f}x, "
          f"few cues {big['few cues']:.1f}x")
    if big["few values"] > big["random"]:
        print("  -> FEWER DISTINCT VALUES HELPS. Capacity is information-limited")
        print("     and g10-10's compression story stands: structured data is")
        print("     where superposition could pay")
    else:
        print("  -> fewer distinct values changes nothing. Capacity is")
        print("     KEY-limited, not information-limited, and g10-10's")
        print("     compression explanation is WRONG -- compressible data buys")
        print("     the store nothing, because the constraint is how many keys")
        print("     can be told apart in d dimensions")
    if big["few cues"] > big["random"] + 0.05:
        print("     (fewer distinct CUES does help, which is the same law seen")
        print("      from the other side: fewer keys to separate)")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=1))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
