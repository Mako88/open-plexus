"""Is the reward-recall task answerable, and is it non-trivially so?

Run BEFORE anything is measured on this task. [Note 006](../docs/notes/006-verifying-the-reservoir-claims.md)
exists because a benchmark was adopted without this check and turned out to be
already solved in the variant specified.

Three numbers, and all three have to land where they should:

  trivial floor       1 / n_values -- what guessing gives
  frozen substrate    an untrained model. Must sit AT the floor, or the task is
                      answerable without learning anything
  oracle-gated        a model told which bindings matter. Must reach high, or
                      the task is not answerable at all and no mechanism result
                      measured on it means anything

    python experiments/g9_01_answerable.py
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.reward_recall import RewardConfig, dataset  # noqa: E402

BASE = RewardConfig(n_pairs=8, n_rewarded=2, n_cues=32, n_values=8,
                    seq_len=192, delay=4, seed=20260726)
D_MODEL, N_TRAIN, N_TEST, EPOCHS, LR, KEY_SCALE = 64, 300, 120, 8, 0.05, 0.5


def build(config: RewardConfig, count: int, seed: int):
    built = []
    for sequence in dataset(replace(config, seed=seed), count):
        tokens = np.asarray(sequence.tokens)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        kinds = sequence.position_kinds()
        # The oracle: keep a binding only where the previous position was the
        # cue of a REWARDED pair. Reads what no running system can read.
        keep = np.array([i > 0 and kinds[i - 1] == "rewarded"
                         for i in range(len(tokens))])
        built.append((tokens, targets, scored, keep, sequence.query_positions))
    return built


def score(model, test_set, keep_masks: bool) -> float:
    right = total = 0
    for tokens, _, _, keep, queries in test_set:
        predicted = model.run(tokens, store=keep if keep_masks else None)
        for q in queries:
            right += predicted[q] == tokens[q + 1]
            total += 1
    return right / total


def run(gated: bool, train: bool, seed: int = 1) -> float:
    train_set = build(BASE, N_TRAIN, seed)
    test_set = build(BASE, N_TEST, seed + 99_991)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=BASE.vocab_size, d_model=D_MODEL, lr=LR,
        key_scale=KEY_SCALE, decay=1.0, seed=seed))
    if train:
        rng = np.random.default_rng(seed)
        order = np.arange(len(train_set))
        for _ in range(EPOCHS):
            rng.shuffle(order)
            for index in order:
                tokens, targets, scored, keep, _ = train_set[index]
                model.run(tokens, targets, scored, learn=True,
                          store=keep if gated else None)
    return score(model, test_set, gated)


print(f"trivial floor         {BASE.trivial_floor:.3f}")
for seed in (1, 2, 3):
    frozen = run(gated=False, train=False, seed=seed)
    gated = run(gated=True, train=True, seed=seed)
    open_ = run(gated=False, train=True, seed=seed)
    print(f"seed {seed}   frozen {frozen:.3f}   trained-ungated {open_:.3f}   "
          f"trained-ORACLE {gated:.3f}")
