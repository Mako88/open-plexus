"""Does a network of TRAINED, partition-owning nodes vote well?

g4-03 measured the token-vote protocol and got agreement falling as nodes were
added -- 0.608 at two, 0.217 at sixteen. That measurement was **invalid**, and
the reason is worth stating plainly because it is the second time the same
conflation has cost a result.

g4-03 uses an untrained model with `wo = wv`, a decoder trick that makes
predictions track the memory. For the SUM path that is harmless: summing every
node's contribution reconstructs the exact product whatever the readout is, so
bit-identity holds regardless. For the VOTE path it is fatal. A vote is only as
good as each node's own answer, and an untrained slice of a jointly-trained
readout has no reason to be a competent answer at all.

**[g4-01](sweeps/g4-01-no-global-readout.txt)'s 0.949 came from `partitions`**,
where each group is trained on its OWN error and therefore learns to answer
alone. Slicing a readout that was trained as a whole is a different thing that
happens to have the same shape.

So this trains with `partitions = nodes` and then distributes, which is the
architecture g4-01 actually validated.

    python experiments/g4_04_token_votes.py
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.harness import parse_args  # noqa: E402
from openplexus.distributed import Network  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

TASK = MqarConfig(n_pairs=4, seq_len=192, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260726)
# 128 wide so that eight partitions still leave 16 dimensions each. g5-04 put
# the per-node width floor near 20-24 at seq 384; four dimensions, which
# d_model 32 over eight nodes gives, is nowhere near it.
WIDTH, N_TRAIN, N_TEST, EPOCHS, LR = 128, 300, 60, 6, 0.05

#: Decay as a HALF-LIFE fraction of the sequence, never an absolute rate. A flat
#: 0.9 halves the store every seven steps, which is fine over the forty-step
#: sequences in the distributed tests and erases everything over 192. That is the
#: mistake this file was first written with, and g7-04 exists because the same
#: one cost a whole sweep.
HALF_LIFE = 0.25
DECAY = float(0.5 ** (1.0 / (HALF_LIFE * TASK.seq_len)))
NODE_COUNTS = (2, 4, 8)
RATES = (1.0, 0.5, 0.25, 0.125)
#: Guessing among values, the floor every number here sits against.
TRIVIAL_FLOOR = 1 / TASK.n_pairs + (1 - 1 / TASK.n_pairs) / TASK.n_values


def build(count: int, seed: int):
    built = []
    for sequence in dataset(replace(TASK, seed=seed), count):
        tokens = np.asarray(sequence.tokens)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        built.append((tokens, targets, scored, sequence.query_positions))
    return built


def trained(partitions: int, seed: int = 1) -> LocalAssociativeMemory:
    """A model whose every partition has learned to answer on its own."""
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=TASK.vocab_size, d_model=WIDTH, lr=LR, key_scale=0.5,
        decay=DECAY, derived_keys=True, partitions=partitions, seed=seed))
    rng = np.random.default_rng(seed)
    train_set = build(N_TRAIN, seed)
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, _ = train_set[index]
            model.run(tokens, targets, scored, learn=True)
    return model


def accuracy(predict, test_set) -> float:
    right = total = 0
    for tokens, _, _, queries in test_set:
        predicted = predict(tokens)
        for q in queries:
            right += predicted[q] == tokens[q + 1]
            total += 1
    return right / total


def main() -> int:
    parse_args(__doc__.splitlines()[0])
    test_set = build(N_TEST, 99_991)
    print(f"trivial floor {TRIVIAL_FLOOR:.3f}\n")

    print("ACCURACY of a token-vote network, nodes trained as partitions")
    print(f"{'nodes':>6}{'one process':>13}" + "".join(f"{r:>9}" for r in RATES))
    for nodes in NODE_COUNTS:
        model = trained(partitions=nodes)
        alone = accuracy(lambda t: model.run(t), test_set)
        if alone <= TRIVIAL_FLOOR:
            # REFUSE. A distributed answer is measured against this one, so if
            # this has not learned the task there is nothing to be distributed
            # and every number below would be noise about noise. Three
            # measurements in this line were invalid before this check existed.
            print(f"{nodes:>6}{alone:>13.3f}   REFUSING: the single-process "
                  f"model is at or below the trivial floor, so nothing "
                  f"distributed from it can mean anything")
            continue
        row = [f"{nodes:>6}{alone:>13.3f}"]
        for rate in RATES:
            with Network(model.config, nodes, model.wv, model.wo,
                         combine="vote") as net:
                voted = accuracy(lambda t: net.run(t, speak=rate), test_set)
            row.append(f"{voted:>9.3f}")
        print("".join(row), flush=True)

    print("\nSAME NETWORK, combine='sum' -- exact, and needs everyone")
    print(f"{'nodes':>6}" + "".join(f"{r:>9}" for r in RATES))
    for nodes in NODE_COUNTS:
        model = trained(partitions=nodes)
        row = [f"{nodes:>6}"]
        for rate in RATES:
            with Network(model.config, nodes, model.wv, model.wo,
                         combine="sum") as net:
                summed = accuracy(lambda t: net.run(t, speak=rate), test_set)
            row.append(f"{summed:>9.3f}")
        print("".join(row), flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
