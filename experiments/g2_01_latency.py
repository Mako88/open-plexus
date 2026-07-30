"""G2 — does the local rule survive a network that is late and out of order?

docs/archive/notes/002 §4 argued the predictive objective converts latency from a *race*
into a *buffer depth*. `openplexus/transport.py` implements the indexing scheme
and its own tests pin the reassembly property. This measures the **learning rule
running through it**, which has never been done.

    python experiments/g2_01_latency.py --sweep identity --seed 3 --json out/x.json
    python experiments/g2_01_latency.py --sweep degrade --jitter 16 --seed 3 --json out/y.json
    python experiments/g2_01_latency.py --sweep drops --drop 0.2 --seed 3 --json out/z.json

The identity sweep asserts something stronger than accuracy: that the learned
weights are **bit-identical** to a run with no network at all. C2 asks for a
stated bound with exactness below it, not graceful degradation, and only an
equality can test that.
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.harness import emit, parse_args  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402
from openplexus.transport import DeliveryConfig, delivered_order  # noqa: E402

TASK = MqarConfig(n_pairs=4, seq_len=96, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260725)
#: Comfortably above the crossing measured in g1-05/g1-06, so any damage here is
#: attributable to the network rather than to capacity.
D_MODEL, EPOCHS, N_TRAIN, N_TEST, LR = 64, 6, 300, 120, 0.05
SEEDS = tuple(range(1, 7))

BOUNDS = ((4, 0), (4, 2), (4, 4), (16, 8), (16, 16), (64, 64))
OVER = ((4, 8), (4, 16), (4, 32), (16, 64), (8, 64))
DROPS = (0.0, 0.05, 0.1, 0.2, 0.4)


def train_and_score(seed: int, delivery: DeliveryConfig | None):
    """Train through `delivery`, returning (accuracy, weights, surviving fraction).

    The network is applied to the token stream: events are emitted in order,
    arrive late and out of order, and the receiver reassembles what it can from a
    buffer `max_delay` deep. Whatever survives is what the model learns from.
    """
    rng = np.random.default_rng(seed)
    train_set = dataset(TASK, N_TRAIN)
    test_set = dataset(replace(TASK, seed=TASK.seed + 99_991), N_TEST)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=TASK.vocab_size, d_model=D_MODEL, lr=LR, seed=seed))

    kept = total = 0
    order = np.arange(len(train_set))
    for epoch in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            sequence = train_set[index]
            tokens = np.asarray(sequence.tokens)
            if delivery is None:
                received = np.arange(len(tokens))
            else:
                # A distinct seed per sequence, so the network misbehaves
                # differently each time rather than applying one fixed pattern
                # that the model could otherwise learn around.
                per_sequence = replace(
                    delivery, seed=delivery.seed * 1_000_003 + int(index) + epoch)
                received = np.asarray(delivered_order(len(tokens), per_sequence),
                                      dtype=int)
            kept += len(received)
            total += len(tokens)
            if len(received) < 2:
                continue
            arrived = tokens[received]
            targets = np.roll(arrived, -1)
            scored = np.ones(len(arrived), dtype=bool)
            scored[-1] = False
            model.run(arrived, targets, scored, learn=True)

    # Evaluation is always over a clean stream. The question is whether a model
    # TRAINED through a bad network still works, not whether it can also be
    # tested through one — conflating those would make a damaged score
    # un-attributable to either cause.
    correct = scored_total = 0
    for sequence in test_set:
        tokens = np.asarray(sequence.tokens)
        predicted = model.run(tokens)
        for q in sequence.query_positions:
            correct += predicted[q] == tokens[q + 1]
            scored_total += 1
    return correct / scored_total, model.wo, kept / total


def main() -> int:
    args = parse_args(__doc__)
    seeds = (args.seed,) if args.seed is not None else SEEDS
    sweep = args.sweep or "identity"
    records = []

    for seed in seeds:
        baseline_accuracy, baseline_weights, _ = train_and_score(seed, None)

        if sweep == "identity":
            for max_delay, jitter in BOUNDS:
                delivery = DeliveryConfig(max_delay=max_delay, jitter=jitter,
                                          seed=seed)
                accuracy, weights, kept = train_and_score(seed, delivery)
                identical = bool(np.array_equal(weights, baseline_weights))
                records.append(dict(
                    condition=f"d={max_delay} j={jitter}", seed=seed,
                    accuracy=accuracy, identical=identical, kept=kept,
                    within_bound=delivery.within_bound,
                    baseline=baseline_accuracy))
                print(f"  max_delay={max_delay:<4} jitter={jitter:<4} "
                      f"acc={accuracy:.3f} kept={kept:.3f} "
                      f"bit-identical={identical}", flush=True)

        elif sweep == "degrade":
            pairs = ((args.max_delay, args.jitter),) if args.jitter is not None else OVER
            for max_delay, jitter in pairs:
                delivery = DeliveryConfig(max_delay=max_delay, jitter=jitter,
                                          seed=seed)
                accuracy, _, kept = train_and_score(seed, delivery)
                records.append(dict(
                    condition=f"d={max_delay} j={jitter}", seed=seed,
                    accuracy=accuracy, kept=kept, within_bound=False,
                    baseline=baseline_accuracy))
                print(f"  max_delay={max_delay:<4} jitter={jitter:<4} "
                      f"acc={accuracy:.3f} kept={kept:.3f}", flush=True)

        else:  # drops
            drops = (args.drop,) if args.drop is not None else DROPS
            for drop in drops:
                delivery = DeliveryConfig(max_delay=8, jitter=0, drop=drop,
                                          seed=seed)
                accuracy, _, kept = train_and_score(seed, delivery)
                records.append(dict(condition=f"drop={drop}", seed=seed,
                                    accuracy=accuracy, kept=kept,
                                    baseline=baseline_accuracy))
                print(f"  drop={drop:<6} acc={accuracy:.3f} kept={kept:.3f}",
                      flush=True)

    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
