"""Where each capability stands, and what it would need to reach — honestly.

`tests/test_component_capability.py` answers pass/fail. This answers **how far**,
because a component at 45% when the goal needs 80% is a place to work, and a
pass/fail test cannot say that.

## The distinction this file exists to protect

**CAPABILITY — what any solution must do.** Stated without reference to how it is
built. "Given a cue seen before, produce the value bound to it" is satisfied by a
hash table, a superposed store, or a transformer. A target on a capability
constrains the PROBLEM and forecloses nothing.

**COMPONENT — whether this implementation's parts work.** "Keys must be
distinguishable" presumes there are keys. A target here constrains the SOLUTION,
and a solution that does not have keys is not thereby worse — it is different.

**Component numbers below are DIAGNOSTICS, never requirements.** If someone
proposes a design with no readout, no key matrix, or no separable store, the
right response is to ask whether it delivers the capabilities — not whether it
scores well on rows that assume the current shape. Freezing an architecture into
a test suite is how a project stops being able to discover anything, and this
paragraph is the guard against it.

## Targets are mostly UNKNOWN and are printed that way

The end goal gives one hard number — beat a bigram, 3.583 bits per character on
Tiny Shakespeare — and **nobody knows what per-component figure that implies.**
There is no derivation from "the whole must reach 3.58" to "the store must hold N
items at M% accuracy", because the components trade against each other.

So a target is printed only where it is genuinely known, and marked `unknown`
otherwise. **Inventing one would be exactly the corner John warned about**: a
number nobody derived, treated as a requirement, ruling out designs that do not
need it.
"""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

import numpy as np  # noqa: E402

from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 48, 64


def build(**overrides):
    config = dict(vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5,
                  decay=1.0, derived_keys=True, seed=11)
    config.update(overrides)
    model = LocalAssociativeMemory(LocalMemoryConfig(**config))
    model.wo[:] = model.wv
    return model


def recall_at(n: int, corrective: bool = False) -> float:
    """CAPABILITY: given a cue seen before, produce the value bound to it."""
    model = build(corrective_writes=corrective)
    keys, values = np.array(model.wk), model.wv
    rng = np.random.default_rng(4)
    cues = rng.choice(VOCAB, size=min(n, VOCAB), replace=False)
    items = rng.choice(VOCAB, size=len(cues))
    memory = np.zeros((WIDTH, WIDTH))
    for cue, item in zip(cues, items):
        if corrective:
            scale = float(keys[cue] @ keys[cue])
            memory += np.outer(values[item] - memory @ keys[cue],
                               keys[cue]) / scale
        else:
            memory += np.outer(values[item], keys[cue])
    right = sum(int((values @ (memory @ keys[c])).argmax()) == int(v)
                for c, v in zip(cues, items))
    return right / len(cues)


def determined_sequence() -> float:
    """CAPABILITY: learn a mapping that is fully determined by the last token."""
    cycle = np.tile(np.arange(6), 40).astype(np.int64)
    targets = np.concatenate([cycle[1:], cycle[-1:]])
    scored = np.ones(len(cycle), dtype=bool)
    scored[-1] = False
    model = build(lr=0.1, vocab_size=8)
    for _ in range(30):
        model.run(cycle, targets, scored, learn=True)
    predicted = model.run(cycle)
    hits = sum(int(predicted[t] == targets[t]) for t in range(1, len(cycle) - 1))
    return hits / (len(cycle) - 2)


def readout_on_clean_input(epochs: int = 200) -> float:
    """COMPONENT: can the readout learn from inputs that are not degraded?"""
    model = build()
    values = np.array(model.wv)
    readout = np.zeros((VOCAB, WIDTH))
    rng = np.random.default_rng(2)
    order = np.arange(VOCAB)
    for _ in range(epochs):
        rng.shuffle(order)
        for token in order:
            answer = readout @ values[token]
            target = np.zeros(VOCAB)
            target[token] = 1.0
            readout += 0.05 * np.outer(target - answer, values[token])
    return sum(int((readout @ values[t]).argmax()) == t
               for t in range(VOCAB)) / VOCAB


def key_separation() -> float:
    """COMPONENT: how far a key is from its nearest neighbour, as a share."""
    keys = np.array(build().wk)
    overlap = keys @ keys.T
    diagonal = float(np.mean(np.diag(overlap)))
    np.fill_diagonal(overlap, 0.0)
    return 1.0 - float(np.abs(overlap).max()) / diagonal


def main() -> int:
    rows = [
        ("CAPABILITY", "recall one binding", recall_at(1), "1.00",
         "anything calling itself a memory"),
        ("CAPABILITY", "recall 8 bindings", recall_at(8), "unknown",
         "no derivation from the end goal exists"),
        ("CAPABILITY", "recall 32 bindings", recall_at(32), "unknown", ""),
        ("CAPABILITY", "recall 32, corrective", recall_at(32, True), "unknown",
         "g11-01: a trade, not an improvement"),
        ("CAPABILITY", "learn a determined sequence", determined_sequence(),
         "1.00", "fully determined by the previous token"),
        ("COMPONENT", "readout on clean input", readout_on_clean_input(),
         "n/a", "DIAGNOSTIC: not a requirement on any design"),
        ("COMPONENT", "key separation", key_separation(), "n/a",
         "DIAGNOSTIC: presumes the design has keys"),
    ]

    print(f"{'kind':>11}  {'what':<30}{'now':>7}{'target':>10}   note")
    for kind, what, value, target, note in rows:
        print(f"{kind:>11}  {what:<30}{value:>7.2f}{target:>10}   {note}")

    print("\nTHE ONE HARD NUMBER THE GOAL GIVES:")
    print("  beat a bigram on Tiny Shakespeare -- 3.583 bits per character.")
    print("  The model is at 5.256 (g11-01). Nothing here derives a")
    print("  per-component figure from that, because the components trade")
    print("  against each other and no such derivation exists.")
    print("\nWHY MOST TARGETS SAY `unknown`:")
    print("  inventing one would freeze this architecture into the test suite.")
    print("  A design with no keys and no readout is not thereby worse -- ask")
    print("  whether it delivers the CAPABILITIES, and ignore the COMPONENT")
    print("  rows entirely, which are diagnostics for the current shape.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
