"""G0 step 1 — what does knowing nothing score on MQAR?

Produces the floor every later number is read against, plus the probes that
would reveal the benchmark being accidentally easy.

    python experiments/g0_01_baselines.py

Reports, per configuration:

    base    the constant predictor. THE base rate.
    rand    a uniformly random value.
    recent  the most recent value token seen. High means recall distance is
            short and the task is not testing retention.
    posn    the value of the pair at the same ordinal index. High means query
            order is leaking pair order.
    oracle  perfect information. Anything but 1.000 means the task is
            unanswerable and every number beside it is void.

No model is involved. Nothing here learns.
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from openplexus import baselines  # noqa: E402
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

N_SEQUENCES = 400
BASE = MqarConfig(n_pairs=4, seq_len=64, n_keys=32, n_values=8, seed=20260725)


def conditions():
    """One dial moved at a time from a fixed reference, so any difference is
    attributable to the dial rather than to a bundle of changes."""
    yield "reference", BASE
    for n_values in (2, 4, 16):
        yield f"n_values={n_values}", replace(BASE, n_values=n_values)
    for n_pairs in (1, 2, 8, 16):
        yield f"n_pairs={n_pairs}", replace(BASE, n_pairs=n_pairs, seq_len=96)
    for filler in ("none", "random"):
        yield f"filler={filler}", replace(BASE, filler=filler)
    for seq_len in (32, 128, 256):
        yield f"seq_len={seq_len}", replace(BASE, seq_len=seq_len)


def main() -> int:
    print(f"MQAR baselines — {N_SEQUENCES} sequences per condition")
    print(f"reference: {BASE}\n")
    header = (f"{'condition':<16}{'base':>8}{'rand':>8}{'recent':>8}{'posn':>8}"
              f"{'oracle':>8}{'FLOOR':>9}")
    print(header)
    print("-" * len(header))
    print("  FLOOR = the score a one-line heuristic gets. THIS is the bar, not base.")

    broken = []
    for name, config in conditions():
        seqs = dataset(config, N_SEQUENCES)
        row = {
            "base": baselines.accuracy(baselines.fit_constant(seqs, config), seqs),
            "rand": baselines.accuracy(baselines.uniform_random(config, seed=1), seqs),
            "recent": baselines.accuracy(baselines.most_recent_value(config), seqs),
            "posn": baselines.accuracy(baselines.positional(config), seqs),
            "oracle": baselines.accuracy(baselines.oracle, seqs),
        }
        if row["oracle"] != 1.0:
            broken.append(name)
        print(f"{name:<16}" + "".join(f"{row[k]:>8.3f}" for k in
              ("base", "rand", "recent", "posn", "oracle"))
              + f"{config.trivial_floor:>9.3f}")

    if broken:
        print(f"\nTASK IS UNANSWERABLE in: {', '.join(broken)}")
        print("Every number above those rows is void. Fix the generator.")
        return 1
    print("\noracle is 1.000 everywhere: the task is answerable in every condition.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
