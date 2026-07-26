"""Why does the readout diverge under skewed input?

Reproducible: with warnings as errors, training raises at zipf_s 1.5 in the
readout update, `invalid value encountered in add`.

The hypothesis is arithmetic rather than statistical. The fast store is
`memory = decay * memory + outer(value, key)`. Repeating ONE binding drives a
geometric series, so its entry approaches `1 / (1 - decay)`. At the half-life
used here, decay is 0.5 ** (1 / 192) = 0.9964, and `1 / (1 - decay)` is about
277 -- so a token that keeps recurring can push the memory two orders of
magnitude above a single binding.

Retrieval is linear in the memory, the readout error is linear in the retrieval,
and the delta-rule update is `lr * error * retrieval` -- **quadratic** in the
memory norm. Enough repetition and it runs away.

If that is right, the divergence is not about Zipf at all. Zipf just supplies
repetition. It would be a general instability of the fast store under any
recurring input, and the fix is the compensatory process Zenke & Gerstner's title
is about -- which this project implemented for the LASTING store (`lasting_cap`)
and never for the fast one.

Measures the memory norm and the readout norm as skew rises, without training, so
the two effects are separable.
"""
import sys
from dataclasses import replace

import numpy as np

sys.path.insert(0, "D:/repos/open-plexus")
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

SEQ_LEN, WIDTH = 768, 32
HALF_LIFE = 0.25
DECAY = float(0.5 ** (1.0 / (HALF_LIFE * SEQ_LEN)))

BASE = MqarConfig(n_pairs=4, seq_len=SEQ_LEN, n_keys=32, n_values=8,
                  autoregressive=True, seed=20260726, queries_per_pair=3)

print(f"decay {DECAY:.6f}   1/(1-decay) = {1 / (1 - DECAY):.1f}")
print(f"{'zipf_s':>7}{'top token share':>17}{'final |memory|':>16}"
      f"{'max |retrieved|':>17}")

for zipf_s in (0.0, 0.5, 1.0, 1.5, 2.0):
    task = (replace(BASE, filler="random") if zipf_s == 0.0
            else replace(BASE, filler="zipf", zipf_s=zipf_s))
    sequence = dataset(task, 1)[0]
    tokens = np.asarray(sequence.tokens)

    rng = np.random.default_rng(4)
    wk = rng.normal(scale=0.5, size=(WIDTH, task.vocab_size))
    wv = rng.normal(scale=0.5, size=(WIDTH, task.vocab_size))

    memory = np.zeros((WIDTH, WIDTH))
    previous = None
    biggest = 0.0
    for token in tokens:
        key = wk[:, int(token)]
        if previous is not None:
            memory *= DECAY
            memory += np.outer(wv[:, int(token)], previous)
        biggest = max(biggest, float(np.linalg.norm(memory @ key)))
        previous = key

    counts = np.bincount(tokens, minlength=task.vocab_size)
    share = counts.max() / counts.sum()
    print(f"{zipf_s:>7}{share:>17.1%}{np.linalg.norm(memory):>16.1f}"
          f"{biggest:>17.1f}")
