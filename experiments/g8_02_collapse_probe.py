"""Is 0.000 a hard task, or a model that has collapsed onto one token?

g8-02 reports the `none`, `on-use` and `salience` arms at EXACTLY 0.000 for every
seed at zipf_s 1.5 and 2.0, while the oracle still scores 0.803-0.903. Exactly
zero across 120 test sequences is not difficulty -- chance alone should land
somewhere near the trivial floor. It looks like the model has learned to emit the
most common filler token, which is never an answer, so it scores zero by
construction rather than by being beaten.

That distinction decides what the sweep measured.
"""
import sys
from collections import Counter
from dataclasses import replace

import numpy as np

sys.path.insert(0, "D:/repos/open-plexus")
from experiments.g8_01_real_gate import (  # noqa: E402
    BASE, D_MODEL, EPOCHS, KEY_SCALE, N_TEST, N_TRAIN, build, decay_for)
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)

SEQ_LEN, HALF_LIFE, LR, SEED = 768, 0.25, 0.05, 1


def look(zipf_s: float) -> None:
    task = replace(BASE, seq_len=SEQ_LEN, filler="zipf", zipf_s=zipf_s)
    train_set = build(task, N_TRAIN, SEED)
    test_set = build(replace(task, seed=task.seed + 99_991), N_TEST, SEED)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=task.vocab_size, d_model=D_MODEL, lr=LR,
        key_scale=KEY_SCALE, decay=decay_for(SEQ_LEN, HALF_LIFE), seed=SEED))
    rng = np.random.default_rng(SEED)
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, _, _ = train_set[index]
            model.run(tokens, targets, scored, learn=True)

    at_queries: Counter = Counter()
    everywhere: Counter = Counter()
    right = total = 0
    for tokens, _, _, _, queries in test_set[:30]:
        predicted = model.run(tokens)
        everywhere.update(predicted.tolist())
        for q in queries:
            at_queries[int(predicted[q])] += 1
            right += predicted[q] == tokens[q + 1]
            total += 1

    filler_counts: Counter = Counter()
    for sequence in [s for s in [None]]:
        pass
    top_all = everywhere.most_common(3)
    top_q = at_queries.most_common(3)
    print(f"\nzipf_s={zipf_s}  accuracy={right / total:.3f}")
    print(f"  distinct predictions anywhere: {len(everywhere)}")
    print(f"  most common everywhere: {top_all}")
    print(f"  most common at queries:  {top_q}")
    share = top_q[0][1] / sum(at_queries.values())
    print(f"  one token accounts for {share:.1%} of all query predictions")


for s in (0.0, 1.0, 1.5, 2.0):
    look(s)
