"""Does a persistent slow store break decision 63's 16,000-character wall?

**This is note 042's falsifier, and it is deliberately the first thing run.**
The note argues that the model has nowhere to keep a concept map -- the store is
rebuilt every sequence and the only durable parameter is one `vocab x d` linear
map (decision 62) -- and that this single fact explains decision 63 (data stops
helping at 16k characters), decision 115 (effective rank ~3 whatever the width)
and g14-01 (the local rule at 0.097 where attention reaches 0.277).

If a persistent slow store does not move that wall, the account is wrong and the
architectural proposal that rests on it goes with it. Cheaper to find out now
than after building concept-partitioning on top.

## Decision 63's numbers, which this reproduces as its control

    chars     4,000   8,000  16,000  32,000  62,500  125,000
    bits      5.570   5.543   5.527   5.523   5.531    5.531     spread ~0.04

Total movement 4,000 to 125,000 is 0.039 bits against a seed spread of 0.04. The
backprop baseline moves 0.95 bits over the same kind of range.

## The arms, and why the middle one exists

    baseline      consolidation OFF, persistence OFF     decision 63's model
    consolidate   consolidation ON,  persistence OFF     the control that matters
    persist       consolidation ON,  persistence ON      the proposal

**`consolidate` is the arm that makes a positive result readable.** Without it,
`persist` beating `baseline` could be consolidation helping rather than
persistence -- two changes in one arm, which is the confound decision 79 was
caught by. The claim is specifically that the store must SURVIVE THE SEQUENCE,
and only the middle arm isolates that.

## The attribution rail

Consolidation fires on `predictions[t-1] == token`: **it promotes what the model
already got right.** So a persistent store cannot bootstrap a model that predicts
badly, and "persistence does not help" and "the gate never opened" would be the
same number without a counter. `model.consolidations` is that counter and P4 is
the rail built on it.

**Registered before the run**: this is a real risk, not a hedge written after
seeing a null. A confirmation-gated store on a model that is wrong most of the
time may accumulate almost nothing.

## PREDICTIONS (registered before running)

  P1  CONTROL. `baseline` reproduces decision 63 -- total movement from 4,000 to
      125,000 characters under 0.05 bits. If it does not, this instrument
      disagrees with the record and nothing else here is readable.
  P2  `consolidate` is also flat. Consolidation without persistence is a
      within-sequence mechanism, so more data cannot help it either.
  P3  THE GATE. `persist` keeps improving past 16,000 characters: its 62,500 to
      125,000 movement exceeds the seed spread, where `baseline`'s does not.
  P4  RAIL. `consolidations` grows roughly linearly with characters seen and is
      well above zero at every point. If it is near zero, a null in P3 says
      nothing about persistence and only that the gate stayed shut.
  P5  `persist` does not reach the backprop baseline (4.049 at 1,000,000
      characters). A single linear readout plus a slow store is not a
      transformer, and a result at or past it would suggest a leak.

P3 is the decision. P4 is what makes a REFUTED P3 interpretable.

COST: 3 arms x 6 data points x 3 seeds = 54 cells. Estimated from the MOST
EXPENSIVE cell -- 125,000 characters with consolidation and persistence both on,
which is the largest stream through the heaviest write path. Printed by
`--cost`.

MEASURED ON: Tiny Shakespeare, width 64, chunk 128 -- decision 63's exact
configuration, so its numbers are the control rather than a re-derivation.
"""

from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402
from experiments.g10_01_first_language import (  # noqa: E402
    TEMPERATURES, bits, corpus_named, scores_and_targets)
from experiments.g11_04_scaling_exponent import split  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)

WIDTH, CHUNK, EPOCHS = 64, 128, 2
SEEDS = (1, 2, 3)

#: Decision 63's exact points, so its table is the control.
CHARS = (4_000, 8_000, 16_000, 32_000, 62_500, 125_000)

#: consolidation and lasting_cap are the settings the g8 line measured the
#: mechanism at. Not tuned here: this asks whether PERSISTENCE changes anything,
#: and tuning consolidation at the same time would be two changes in one arm.
ARMS = {
    "baseline": dict(),
    "consolidate": dict(consolidation=0.5, lasting_cap=5.0),
    "persist": dict(consolidation=0.5, lasting_cap=5.0,
                    persistent_lasting=True),
}


def build(seed: int, arm: str, vocab: int) -> LocalAssociativeMemory:
    return LocalAssociativeMemory(LocalMemoryConfig(
        d_model=WIDTH, vocab_size=vocab, seed=seed, lr=0.05, key_scale=0.5,
        decay=0.997, derived_keys=True, **ARMS[arm]))


def one_cell(arm: str, chars: int, seed: int) -> dict:
    corpus = corpus_named("shakespeare")
    fitting, calibration, test = split(corpus, CHUNK, chars)
    model = build(seed, arm, corpus.vocab_size)

    started = time.time()
    for _ in range(EPOCHS):
        for tokens in fitting:
            targets = np.concatenate([tokens[1:], tokens[-1:]])
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, targets, scored, learn=True)
    trained = time.time() - started

    # The temperature is fitted on CALIBRATION text and applied to TEST text, so
    # nothing is tuned on what it is scored against. Decision 117: without a
    # temperature the delta rule's scores sit in about [0, 1] and a softmax over
    # them is near-uniform whatever the model knows -- the number would measure
    # the SCALE of the scores rather than their information.
    scores, wanted = scores_and_targets(model, calibration)
    temperature = min(TEMPERATURES, key=lambda t: bits(scores, wanted, t))
    scores, wanted = scores_and_targets(model, test)

    return {
        "arm": arm, "chars": chars, "seed": seed,
        "bits": round(bits(scores, wanted, temperature), 4),
        "temperature": temperature,
        # The attribution rail. A null in P3 means nothing if this is ~0.
        "consolidations": model.consolidations,
        "lasting_norm": (0.0 if model._lasting is None
                         else round(float(np.linalg.norm(model._lasting)), 4)),
        "train_seconds": round(trained, 1),
        "condition": (f"{arm}|d{WIDTH}|chars{chars}|seed{seed}"
                      f"|chunk{CHUNK}x{EPOCHS}"),
    }


def cost_probe() -> None:
    started = time.time()
    one_cell("persist", 8_000, 1)
    per = time.time() - started
    largest = per * (max(CHARS) / 8_000)
    print("most expensive cell: persist at 125,000 characters")
    print(f"  {per:.1f} s at 8,000 characters")
    print(f"  ~{largest / 60:.1f} min extrapolated to 125,000")
    print(f"  one job per seed is 3 arms x 6 points; worst job "
          f"~{largest * 3 * 2 / 60:.0f} min, since the two smaller points "
          f"together cost about as much again as the largest")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--arm", choices=sorted(ARMS), default=None,
                        help="one arm only, so a job is ~5 min rather than ~16")
    parser.add_argument("--json", type=str, default=None)
    parser.add_argument("--cost", action="store_true")
    args = parser.parse_args()

    harness.refuse_if_mutating()
    if args.cost:
        cost_probe()
        return

    seeds = (args.seed,) if args.seed is not None else SEEDS
    arms = (args.arm,) if args.arm else tuple(ARMS)
    records = [one_cell(arm, chars, seed)
               for seed in seeds for arm in arms for chars in CHARS]

    for record in records:
        print(f"{record['condition']}  bits {record['bits']:.4f}  "
              f"consolidations {record['consolidations']:,}  "
              f"lasting {record['lasting_norm']:.3f}")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=2),
                                   encoding="utf-8")


if __name__ == "__main__":
    main()
