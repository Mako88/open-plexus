"""Is the substrate predictable at all? — the gate on the credit-assignment scheme.

docs/notes/002 recommends self-supervised temporal prediction: each unit predicts
its own next input and learns from the difference. docs/notes/005 promoted the
gate on that recommendation to the most important unmeasured item in the
project, because the literature that appeared to support the scheme turned out
to describe a supervised variant we cannot use.

The gate: **if a frozen substrate's state does not predict its own next input,
a predictive objective has nothing to learn from and the scheme is dead.**

    python experiments/g1_01_predictability.py

`PLEXUSBRIEF.md` §6 records three defects in the predecessor's version of this
probe. All three are guarded here, deliberately and by name:

1. *Its state was the raw sparse spike vector, so the probe sat at the floor
   even at horizon 0 — it could not decode the burst happening at that moment.*
   → horizon 0 is reported for every condition. It is the connection control.

2. *No horizon-0 control and no majority-class floor: without k=0, "the future
   is unpredictable" and "this probe decodes nothing" produce the same table.*
   → both are reported, always, in every row.

3. *It conflated a schedule-driven cue — identical in every episode — with the
   cue groups carrying random content, and scored 0.797 while predicting no
   content whatever.*
   → results are split by position kind. Structured filler is a deterministic
   cycle and predictable by construction; pooling it with task content would
   report the filler's easiness as if it were a finding.
"""

from __future__ import annotations

import sys
import time
from collections import Counter
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from openplexus.models.readout import RidgeReadout  # noqa: E402
from openplexus.models.reservoir import Reservoir, ReservoirConfig  # noqa: E402
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

N_TRAIN, N_TEST = 200, 120
TASK = MqarConfig(n_pairs=4, seq_len=64, n_keys=32, n_values=8, seed=20260725)
SUBSTRATE = ReservoirConfig(n_units=64, spectral_radius=0.9, leak=0.3, seed=1)
HORIZONS = (0, 1, 2, 5)
KINDS = ("all", "filler", "answer")


def probe(task: MqarConfig, horizon: int, kind: str) -> tuple[float, float]:
    """Decode the token `horizon` steps ahead from the state now.

    Returns (accuracy, base rate). The base rate is the majority next-token at
    the scored positions, and is returned rather than assumed because a probe
    reporting 0.55 where a constant predictor gets 0.56 is reporting a negative
    result that looks positive.
    """
    reservoir = Reservoir(SUBSTRATE, task.vocab_size)
    train = dataset(task, N_TRAIN)
    test = dataset(replace(task, seed=task.seed + 99_991), N_TEST)

    def collect(sequences):
        rows, labels = [], []
        for sequence in sequences:
            states = reservoir.run(sequence.tokens)
            kinds = sequence.position_kinds()
            for t in range(len(sequence.tokens) - horizon):
                # Classify by the position being PREDICTED, not the position
                # being read from. The question is what kind of thing the
                # substrate can see coming.
                target_kind = kinds[t + horizon]
                if kind == "filler" and target_kind != "filler":
                    continue
                if kind == "answer" and target_kind != "answer":
                    continue
                rows.append(states[t])
                labels.append(sequence.tokens[t + horizon])
        return rows, labels

    train_rows, train_labels = collect(train)
    test_rows, test_labels = collect(test)
    if not test_rows:
        return float("nan"), float("nan")

    readout = RidgeReadout(ridge=1e-2).fit(train_rows, train_labels)
    correct = sum(readout.predict(r) == y for r, y in zip(test_rows, test_labels))
    majority = Counter(test_labels).most_common(1)[0][1]
    return correct / len(test_labels), majority / len(test_labels)


def main() -> int:
    print("Is a frozen substrate's state predictive of its own next input?")
    print(f"{N_TRAIN} train / {N_TEST} held out. 'base' is the majority "
          "next-token at those same positions.\n")

    for filler in ("structured", "random"):
        task = replace(TASK, filler=filler, autoregressive=True, seq_len=96)
        print(f"--- filler={filler} ---")
        header = f"{'horizon':<9}" + "".join(f"{k:>20}" for k in KINDS)
        print(header)
        print("-" * len(header))
        for horizon in HORIZONS:
            cells = []
            for kind in KINDS:
                started = time.time()
                accuracy, base = probe(task, horizon, kind)
                cells.append(f"{accuracy:.3f} (base {base:.3f})".rjust(20))
            print(f"{horizon:<9}" + "".join(cells))
        print()

    print("AUTOREGRESSIVE layout: each query is followed by its answer, so the")
    print("'answer' column at horizon 1 is the task itself -- predicting the next")
    print("token at a query position IS answering the query (docs/notes/001 P2).")
    print()
    print("horizon 0 is the connection control: decoding the CURRENT token.")
    print("If it is not near 1.000 the probe is broken and no other row means")
    print("anything. 'task' excludes filler positions -- structured filler is a")
    print("deterministic cycle, and pooling it in would report its easiness as")
    print("if it were a finding about content.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
