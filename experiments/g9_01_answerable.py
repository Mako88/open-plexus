"""Is the reward-recall task answerable, and is there room between floor and ceiling?

Run BEFORE anything is measured on this task. [Note 006](../docs/archive/notes/006-verifying-the-reservoir-claims.md)
exists because a benchmark was adopted without this check and turned out to be
already solved in the variant specified.

**The first configuration failed this check**, and it is worth recording how,
because the number that failed looked excellent:

    trivial floor  0.125    frozen 0.000    trained-ungated 0.999    ORACLE 1.000

An ungated model -- no selectivity of any kind, storing every consecutive pair --
scored 0.999 against the oracle's 1.000. **A gating experiment needs the gate to
be worth something**, and there was nothing to recover. Every arm would have
scored 1.000 and the sweep would have reported a clean, meaningless flat line.

Two causes:

1. **The memory was not under load.** d_model 64, decay 1.0, 8 pairs over 192
   steps. Nothing had to be forgotten, so nothing had to be chosen.
2. **Repeat queries answer themselves in autoregressive mode.** The answer
   follows the query in the stream, so the first query of a cue RE-BINDS it. With
   two rewarded cues asked three times each, four of six queries were asked about
   a binding that had just been rewritten a few steps earlier. That is the same
   trap that killed the first design of the task, wearing a different hat.

So this now searches for a configuration with real headroom rather than asserting
one, and reports the frozen baseline honestly: an untrained model has `wo = 0`,
predicts token 0 forever, and scores 0.000 rather than the floor. That is not a
fair floor -- it is a degenerate model -- and it is labelled as such.

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

N_TRAIN, N_TEST, EPOCHS, LR, KEY_SCALE = 200, 80, 6, 0.05, 0.5


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
        built.append((tokens, targets, scored, keep, sequence.query_positions,
                      sequence))
    return built


def first_asks(entry) -> list[int]:
    """Query positions asking a cue for the FIRST time.

    A repeat is answerable from the re-binding the previous answer performed, so
    scoring repeats measures short-term echo rather than retention.
    """
    tokens, _, _, _, queries, _ = entry
    seen: set[int] = set()
    firsts = []
    for q in queries:
        cue = int(tokens[q])
        if cue not in seen:
            firsts.append(q)
            seen.add(cue)
    return firsts


def measure(config: RewardConfig, d_model: int, decay: float, gated: bool,
            train: bool, seed: int) -> tuple[float, float]:
    train_set = build(config, N_TRAIN, seed)
    test_set = build(config, N_TEST, seed + 99_991)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=config.vocab_size, d_model=d_model, lr=LR,
        key_scale=KEY_SCALE, decay=decay, seed=seed))
    if train:
        rng = np.random.default_rng(seed)
        order = np.arange(len(train_set))
        for _ in range(EPOCHS):
            rng.shuffle(order)
            for index in order:
                tokens, targets, scored, keep, _, _ = train_set[index]
                model.run(tokens, targets, scored, learn=True,
                          store=keep if gated else None)

    right = total = right_first = total_first = 0
    for entry in test_set:
        tokens, _, _, keep, queries, _ = entry
        predicted = model.run(tokens, store=keep if gated else None)
        firsts = set(first_asks(entry))
        for q in queries:
            hit = predicted[q] == tokens[q + 1]
            right += hit
            total += 1
            if q in firsts:
                right_first += hit
                total_first += 1
    return right / total, right_first / max(1, total_first)


CANDIDATES = [
    ("as first written", RewardConfig(n_pairs=8, n_rewarded=2, n_cues=32,
                                      n_values=8, seq_len=192, delay=4,
                                      seed=20260726), 64, 1.0),
    ("narrower", RewardConfig(n_pairs=8, n_rewarded=2, n_cues=32, n_values=8,
                              seq_len=192, delay=4, seed=20260726), 16, 1.0),
    ("longer + more pairs", RewardConfig(n_pairs=24, n_rewarded=4, n_cues=64,
                                         n_values=8, seq_len=768, delay=8,
                                         seed=20260726), 32, 1.0),
    ("longer + narrow", RewardConfig(n_pairs=24, n_rewarded=4, n_cues=64,
                                     n_values=8, seq_len=768, delay=8,
                                     seed=20260726), 16, 1.0),
]

print(f"{'configuration':<22}{'floor':>7}{'ungated':>9}{'oracle':>8}"
      f"{'gap':>7}   {'ungated(1st)':>13}{'oracle(1st)':>12}")
for name, config, d_model, decay in CANDIDATES:
    ungated, ungated_first = measure(config, d_model, decay, False, True, 1)
    oracle, oracle_first = measure(config, d_model, decay, True, True, 1)
    print(f"{name:<22}{config.trivial_floor:>7.3f}{ungated:>9.3f}"
          f"{oracle:>8.3f}{oracle - ungated:>7.3f}   "
          f"{ungated_first:>13.3f}{oracle_first:>12.3f}")
