"""Does storing the error instead of the value fix rebinding, and what else?

The mechanism is `corrective_writes` in `local_memory.py`:

    Hebbian      memory += outer(value, key)
    corrective   memory += outer(value - memory @ key, key) / (key @ key)

Predictions are registered in
[the sweep file](sweeps/g11-01-corrective-writes.txt) and are not repeated here.

## The three conditions do not share data, and their numbers are not comparable

**rebinding** -- 8 cues bound repeatedly to fresh values, scored on whether the
LAST binding is retrieved. This is what the mechanism is for, and g10-11 measured
Hebbian storage at 0.0x chance here.

**capacity** -- `n` distinct random pairs stored and queried, as in g10-10. It
shares no data with the rebinding condition and answers a different question:
whether reducing interference between DIFFERENT keys buys anything, which is a
claim about the projection rule rather than about overwriting.

Both are scored as a multiple of chance, because a condition with fewer distinct
values has a higher floor -- the error that inverted g10-11's first reading.

The Shakespeare cell is run separately through `g10_01 --corpus shakespeare`,
because it needs the training loop and takes ten minutes rather than seconds.
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

VOCAB, WIDTH = 4096, 64
CUES, REBINDINGS = 8, (64, 512)
ITEMS = (16, 64, 128, 256, 512)


def model_for(corrective: bool, decay: float):
    return LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5, decay=decay,
        derived_keys=True, corrective_writes=corrective, seed=1))


def write(model, memory, key, value, decay: float):
    """One store, exactly as `local_memory.run` does it."""
    if decay < 1.0:
        memory *= decay
    if model.config.corrective_writes:
        scale = float(key @ key)
        if scale > 0.0:
            memory += np.outer(value - memory @ key, key) / scale
    else:
        memory += np.outer(value, key)
    return memory


def rebinding(corrective: bool, decay: float, total: int, rng) -> float:
    model = model_for(corrective, decay)
    keys, values = np.array(model.wk), model.wv
    cues = rng.choice(VOCAB, size=CUES, replace=False)
    memory = np.zeros((WIDTH, WIDTH))
    latest: dict[int, int] = {}
    for _ in range(total):
        cue = int(cues[rng.integers(CUES)])
        item = int(rng.integers(VOCAB))
        memory = write(model, memory, keys[cue], values[item], decay)
        latest[cue] = item
    right = sum(int((values @ (memory @ keys[c])).argmax()) == v
                for c, v in latest.items())
    return right / len(latest)


def capacity(corrective: bool, n: int, rng) -> float:
    model = model_for(corrective, 1.0)
    keys, values = np.array(model.wk), model.wv
    cues = rng.choice(VOCAB, size=n, replace=False)
    items = rng.choice(VOCAB, size=n)
    memory = np.zeros((WIDTH, WIDTH))
    for cue, item in zip(cues, items):
        memory = write(model, memory, keys[cue], values[item], 1.0)
    picks = rng.integers(n, size=min(300, n * 4))
    right = sum(int((values @ (memory @ keys[cues[i]])).argmax()) == int(items[i])
                for i in picks)
    return right / len(picks)


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    rng = np.random.default_rng(1)
    floor = 1.0 / VOCAB
    records = []

    print(f"width {WIDTH}, vocabulary {VOCAB}, chance {floor:.5f}")
    print("\n== REBINDING: 8 cues bound repeatedly, is the LAST one retrieved? ==")
    print(f"{'decay':>8}{'rebindings':>12}{'Hebbian':>12}{'corrective':>13}")
    for decay in (1.0, 0.997):
        for total in REBINDINGS:
            plain = rebinding(False, decay, total, rng)
            fixed = rebinding(True, decay, total, rng)
            records.append({"condition": "rebinding", "decay": decay,
                            "rebindings": total, "hebbian": plain,
                            "corrective": fixed})
            print(f"{decay:>8}{total:>12}{plain:>12.3f}{fixed:>13.3f}")

    print("\n== CAPACITY: n distinct pairs, as a multiple of chance ==")
    print(f"{'items':>8}{'Hebbian':>12}{'corrective':>13}")
    for n in ITEMS:
        plain = capacity(False, n, rng)
        fixed = capacity(True, n, rng)
        records.append({"condition": "capacity", "items": n,
                        "hebbian": plain, "corrective": fixed})
        print(f"{n:>8}{plain / floor:>12.0f}{fixed / floor:>13.0f}")

    rebinds = [r for r in records if r["condition"] == "rebinding"]
    worst = min(r["corrective"] for r in rebinds)
    print()
    if worst > 0.5:
        print("  -> REBINDING IS FIXED, at every decay tested, including the")
        print("     0.997 the g9 line uses. Overwriting no longer costs")
        print("     retention, which is what prediction 1 asked for")
    else:
        print(f"  -> rebinding still fails somewhere: worst corrective cell is")
        print(f"     {worst:.3f}. The implementation is wrong, not the idea")

    big = [r for r in records if r["condition"] == "capacity"
           and r["items"] == ITEMS[-1]][0]
    print(f"\n  at {ITEMS[-1]} items: Hebbian {big['hebbian'] / floor:.0f}x "
          f"chance, corrective {big['corrective'] / floor:.0f}x")
    ratio = big["corrective"] / max(big["hebbian"], 1e-12)
    if ratio > 1.2:
        print("  -> and capacity improves too, so the correction reduces")
        print("     interference between DIFFERENT keys and not only repeats")
    elif ratio > 0.8:
        print("  -> capacity is unchanged, so this is an OVERWRITE fix and not")
        print("     a capacity fix. Prediction 2 was the one I was least sure of")
    else:
        print(f"  -> capacity is WORSE, at {ratio:.2f}x the Hebbian figure.")
        print("     Prediction 2 is refuted in the direction I did not consider,")
        print("     and the mechanism is a TRADE rather than an improvement.")
        print("     The single-shot correction subtracts `memory @ key`, which")
        print("     contains interference from other bindings as well as this")
        print("     key's own stale value -- so every write quietly erases a")
        print("     share of its neighbours. Overwriting is bought with capacity,")
        print("     the same shape as decay buying it with retention.")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=1))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
