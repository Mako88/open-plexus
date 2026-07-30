"""Which binds the step rate: the arithmetic, or the network?

[Note 009](../docs/archive/notes/009-splitting-the-memory.md) §3 bounds the affordable
region by `d . rate`, and §5 lists `rate` as never measured -- so the whole
bandwidth argument rested on a product with one term unknown.

`rate` has two ceilings and the lower one wins:

- **Compute.** Per step a machine updates its own rows of the memory, retrieves
  from them, and reads out: about `2wd + vocab*w` multiply-accumulates for a
  machine `w` wide in a network `d` wide. Measured here rather than counted,
  because at these sizes the array overhead dominates the arithmetic.
- **Network.** Note 009 §3: forwarding the key as a tree of fan-out `F` costs
  `F*d*4` bytes per step per machine, independent of network size.

Run it:  python tools/step_rate.py
"""

from __future__ import annotations

import time

import numpy as np

VOCAB = 41              # the MQAR alphabet these measurements were taken against
FAN_OUT = 8             # note 009 §3, inside note 004's measured limit of D <= 15
UPLOAD_BYTES = 1.25e6   # 10 Mbps, a common home upload
REPEATS = 2000

# (network width, machine width) -- the first is g5-03's, the rest are guesses at
# what a deployment might want.
SHAPES = ((240, 16), (240, 30), (1024, 64), (1024, 128), (4096, 256))


def compute_hz(d: int, w: int, repeats: int = REPEATS) -> float:
    """Steps per second one machine can sustain, ignoring the network."""
    memory = np.zeros((w, d))
    key = np.random.default_rng(0).normal(size=d)
    value = np.random.default_rng(1).normal(size=w)
    readout = np.zeros((VOCAB, w))
    start = time.perf_counter()
    for _ in range(repeats):
        memory += np.outer(value, key)
        retrieved = memory @ key
        readout @ retrieved
    return repeats / (time.perf_counter() - start)


def network_hz(d: int) -> float:
    """Steps per second the key broadcast allows, on one home upload."""
    return UPLOAD_BYTES / (FAN_OUT * d * 4)


def main() -> int:
    print(f"fan-out {FAN_OUT}, upload {UPLOAD_BYTES / 1e6:.2f} MB/s, "
          f"float32 keys")
    print(f"{'d':>6}{'w':>5}{'compute Hz':>13}{'network Hz':>13}"
          f"{'binds':>9}{'margin':>9}")
    for d, w in SHAPES:
        fast, slow = compute_hz(d, w), network_hz(d)
        binds = "network" if slow < fast else "compute"
        print(f"{d:>6}{w:>5}{fast:>13,.0f}{slow:>13,.1f}{binds:>9}"
              f"{max(fast, slow) / min(fast, slow):>8.0f}x")
    print()
    print("The margin narrows as machines widen: compute cost per step grows as "
          "w*d while the broadcast grows as d, so their ratio goes as w. Nothing "
          "in the tested range comes close to crossing.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
