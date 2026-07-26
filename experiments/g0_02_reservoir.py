"""G0 step 2 — what does a frozen random substrate score on MQAR?

A random reservoir, never trained, with a ridge readout fitted on training
sequences and scored on held-out ones. The number wanted here is LOW: it is the
control every future learning claim is read against, and headroom above it is
what makes the benchmark able to show anything at all.

    python experiments/g0_02_reservoir.py

Columns:

    floor     the one-line heuristic's score (experiments/sweeps/g0-01)
    frozen    frozen reservoir + trained linear readout, held out
    headroom  1.000 - frozen. What a learning rule has to play for.

The oracle scores 1.000 by construction and is not re-reported. It shows the
task is *answerable*; it does not show it is *learnable*, and G0 step 3 — a
strong non-local reference trained on this generator — is not done.
"""

from __future__ import annotations

import sys
import time
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from openplexus.models.readout import RidgeReadout  # noqa: E402
from openplexus.models.reservoir import Reservoir, ReservoirConfig  # noqa: E402
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

N_TRAIN, N_TEST = 240, 160
TASK = MqarConfig(n_pairs=4, seq_len=64, n_keys=32, n_values=8, seed=20260725)
SUBSTRATE = ReservoirConfig(n_units=64, spectral_radius=0.9, leak=0.3, seed=1)


def frozen_score(task: MqarConfig, substrate: ReservoirConfig,
                 target: str = "recall") -> float:
    """Fit a readout on training sequences, score on held-out ones.

    `target="recall"` is the task: predict the value this key was paired with.

    `target="identity"` is the **control**, and it is not optional. It asks the
    same pipeline to decode the token *currently being presented* — information
    the state provably contains, since that token is the input driving it. If
    the control scores near ceiling, the substrate-to-readout path works and a
    low recall score is a real finding about recall. If the control ALSO scores
    at chance, the pipeline is broken and the recall number measures nothing.

    Without this, a disconnected readout and a substrate that genuinely cannot
    do associative recall produce identical output — and the second is what we
    expect, which is exactly the condition under which a broken pipeline gets
    written up as a result.
    """
    reservoir = Reservoir(substrate, task.vocab_size)
    train = dataset(task, N_TRAIN)
    test = dataset(replace(task, seed=task.seed + 99_991), N_TEST)

    def collect(sequences):
        states, labels = [], []
        for sequence in sequences:
            run = reservoir.run(sequence.tokens)
            for position in sequence.query_positions:
                states.append(run[position])
                labels.append(sequence.tokens[position] if target == "identity"
                              else sequence.targets[position])
        return states, labels

    train_states, train_labels = collect(train)
    readout = RidgeReadout(ridge=1e-2).fit(train_states, train_labels)
    test_states, test_labels = collect(test)
    correct = sum(readout.predict(s) == y for s, y in zip(test_states, test_labels))
    return correct / len(test_labels)


def conditions():
    yield "reference", TASK, SUBSTRATE
    for n_units in (16, 128):
        yield f"n_units={n_units}", TASK, replace(SUBSTRATE, n_units=n_units)
    for n_pairs in (2, 8):
        yield f"n_pairs={n_pairs}", replace(TASK, n_pairs=n_pairs, seq_len=96), SUBSTRATE
    yield "filler=random", replace(TASK, filler="random"), SUBSTRATE


def main() -> int:
    print(f"Frozen reservoir on MQAR — {N_TRAIN} train / {N_TEST} held out")
    print(f"task:      {TASK}")
    print(f"substrate: {SUBSTRATE}\n")
    header = (f"{'condition':<16}{'floor':>8}{'frozen':>9}{'CONTROL':>9}"
              f"{'headroom':>10}{'secs':>7}")
    print(header)
    print("-" * len(header))
    print("  CONTROL = same pipeline decoding the CURRENT token. Must be high,")
    print("  or the recall column is measuring a broken pipeline, not a substrate.")

    suspect = []
    for name, task, substrate in conditions():
        started = time.time()
        score = frozen_score(task, substrate, target="recall")
        control = frozen_score(task, substrate, target="identity")
        if control < 0.5:
            suspect.append(name)
        print(f"{name:<16}{task.trivial_floor:>8.3f}{score:>9.3f}{control:>9.3f}"
              f"{1.0 - score:>10.3f}{time.time() - started:>7.1f}")

    if suspect:
        print(f"\nCONTROL FAILED in: {', '.join(suspect)}")
        print("The readout cannot decode information the state definitely holds.")
        print("The recall numbers above measure the pipeline, not the substrate.")
        return 1
    print("\nControl passes everywhere: the readout can decode what the state holds,")
    print("so the recall scores are a property of the substrate.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
