"""Pre-dispatch control for g8-02: is skewed filler actually less surprising?

The hypothesis is one causal chain:

    skewed filler -> filler becomes predictable -> filler stops clearing the
    surprise bar -> the gate's enrichment ratio improves

The sweep measures the far end, which costs hours. This measures the near end,
which costs seconds. If the near end does not move, the far end cannot.

**This measures the DATA, not a model.** An earlier draft replayed the model's
inner loop to recover a surprise trace, which is the reimplementation pattern
this project has been burned by three times -- and it is not even necessary,
because the claim is about the statistics of the sequences. What a model can
predict about a token stream is bounded by how predictable the stream is, and
the unigram self-information is the honest floor on that: -log p(token) under
the empirical token distribution.

The caveat that goes with it: this is a UNIGRAM measure and the model is
associative, so it understates how predictable the task content is (a query's
answer is predictable from the binding, not from its frequency). That biases
AGAINST the hypothesis -- it makes task content look more surprising than the
model would find it -- so a positive result here is conservative.
"""
import sys
from collections import Counter, defaultdict
from dataclasses import replace

import numpy as np

sys.path.insert(0, "D:/repos/open-plexus")
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

BASE = MqarConfig(n_pairs=4, seq_len=768, n_keys=32, n_values=8,
                  autoregressive=True, seed=20260726, queries_per_pair=3)
SALIENCE = 2.5


def report(name: str, task: MqarConfig, n: int = 40) -> None:
    sequences = dataset(task, n)

    counts: Counter = Counter()
    for sequence in sequences:
        counts.update(sequence.tokens)
    total = sum(counts.values())

    by_kind = defaultdict(list)
    for sequence in sequences:
        for token, kind in zip(sequence.tokens, sequence.position_kinds()):
            by_kind[kind].append(-np.log(counts[token] / total))

    everything = [v for values in by_kind.values() for v in values]
    mean, deviation = float(np.mean(everything)), float(np.std(everything))

    print(f"\n=== {name} ===")
    print(f"{'kind':<10}{'n':>8}{'self-info':>12}{'over bar':>10}{'rate':>9}")
    rates = {}
    for kind in sorted(by_kind):
        values = by_kind[kind]
        over = sum(1 for v in values if abs(v - mean) > SALIENCE * deviation)
        rates[kind] = over / len(values)
        print(f"{kind:<10}{len(values):>8}{np.mean(values):>12.3f}"
              f"{over:>10}{rates[kind]:>9.4f}")
    if rates.get("filler"):
        print(f"query:filler enrichment = "
              f"{rates.get('query', 0.0) / rates['filler']:.2f}x")
    else:
        print("no filler cleared the bar at all")


report("uniform filler", replace(BASE, filler="random"))
for s in (0.5, 1.0, 1.5, 2.0):
    report(f"zipf s={s}", replace(BASE, filler="zipf", zipf_s=s))
