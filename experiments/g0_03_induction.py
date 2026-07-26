"""G0 step 3 (partial) — what does one content-addressed lookup buy?

Takes the frozen substrate from g0-02, which sits at chance, and adds exactly one
input-dependent operation: look back for the last position holding the current
token, and report what came next. Nothing else changes — same substrate, same
readout, same train/test split.

    python experiments/g0_03_induction.py

The lookup is hand-specified. A high score shows the headroom is **reachable**
and localises what reaches it. It does **not** show the task is learnable, and
G0 step 3 proper still needs a model trained from scratch on this generator.
"""

from __future__ import annotations

import sys
import time
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from openplexus.models.induction import concatenate, induction_features  # noqa: E402
from openplexus.models.readout import RidgeReadout  # noqa: E402
from openplexus.models.reservoir import Reservoir, ReservoirConfig  # noqa: E402
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

N_TRAIN, N_TEST = 240, 160
TASK = MqarConfig(n_pairs=4, seq_len=64, n_keys=32, n_values=8, seed=20260725)
SUBSTRATE = ReservoirConfig(n_units=64, spectral_radius=0.9, leak=0.3, seed=1)

VARIANTS = ("frozen", "+lookup-state", "+lookup-token", "lookup-only")


def score(task: MqarConfig, substrate: ReservoirConfig, variant: str) -> float:
    reservoir = Reservoir(substrate, task.vocab_size)
    train = dataset(task, N_TRAIN)
    test = dataset(replace(task, seed=task.seed + 99_991), N_TEST)

    def features(sequence):
        states = reservoir.run(sequence.tokens)
        if variant == "frozen":
            return states
        mode = "token" if variant == "+lookup-token" else "state"
        lookup = induction_features(sequence.tokens, states, task.vocab_size, mode)
        if variant == "lookup-only":
            return lookup
        return concatenate(states, lookup)

    def collect(sequences):
        rows, labels = [], []
        for sequence in sequences:
            f = features(sequence)
            for position in sequence.query_positions:
                rows.append(f[position])
                labels.append(sequence.targets[position])
        return rows, labels

    train_rows, train_labels = collect(train)
    readout = RidgeReadout(ridge=1e-2).fit(train_rows, train_labels)
    test_rows, test_labels = collect(test)
    correct = sum(readout.predict(r) == y for r, y in zip(test_rows, test_labels))
    return correct / len(test_labels)


def conditions():
    yield "reference", TASK
    yield "n_pairs=8", replace(TASK, n_pairs=8, seq_len=96)


def main() -> int:
    print(f"One content lookup on a frozen substrate — {N_TRAIN} train / {N_TEST} held out")
    print(f"substrate: {SUBSTRATE}\n")
    header = f"{'condition':<14}{'floor':>7}" + "".join(f"{v:>15}" for v in VARIANTS)
    print(header)
    print("-" * len(header))

    for name, task in conditions():
        started = time.time()
        scores = {v: score(task, SUBSTRATE, v) for v in VARIANTS}
        print(f"{name:<14}{task.trivial_floor:>7.3f}"
              + "".join(f"{scores[v]:>15.3f}" for v in VARIANTS)
              + f"   ({time.time() - started:.0f}s)")
    print("\nThe lookup is hand-specified. This shows the headroom is REACHABLE,")
    print("not that the task is LEARNABLE. G0 step 3 proper is still open.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
