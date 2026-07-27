"""When a node is lost, which structure degrades better?

[Note 030](../docs/notes/030-the-benchmark-does-not-discriminate.md) argued that
neither `reward_recall` nor character text can tell a superposed store from a
cache, and named four properties that would. It put **graceful degradation under
node loss** first, on the grounds that the machinery already exists, and made a
claim about how it would come out:

> Losing a node from a dimension-sliced store degrades every answer slightly;
> losing a node from a key-sharded table loses those keys completely.

**That is a prediction I wrote and did not measure**, which is the same shape as
g10-03's recommendation that g10-04 had to refute. So it gets measured before it
is repeated.

## The two architectures under the same loss

**DIMENSION-SLICED.** Each node holds a slice of every vector; the driver sums
their votes. A missing node omits its term, so every answer is computed from a
narrower vector and degrades a little. This is `distributed.Network` with
`absent`, which already exists and is already tested.

**KEY-SHARDED.** Each node holds whole entries for the cues that hash to it — a
DHT. A missing node takes its cues with it: those queries fail completely and
every other query is untouched.

## The prediction this is testing, stated so it can lose

Note 030 implies the dimension-sliced store degrades more gracefully and is
therefore the better structure under churn. **The arithmetic suggests otherwise
and it is worth being explicit about that before running.**

A cache at 1.000 losing a quarter of its keys should land near
`0.75 * 1.0 + 0.25 * floor` = 0.78. A store at 0.65 losing a quarter of its
dimensions lands wherever it lands. If it lands below 0.78, then **the cache
degrades to a better place even though it degrades less gracefully** — and note
030's "churn is the discriminating axis" is right that it discriminates and wrong
about which way.

Graceful is not the same as good, and the note conflated them.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments.g9_02_reward_gate import EPOCHS, N_TRAIN, build  # noqa: E402
from experiments.harness import parse_args  # noqa: E402
from openplexus.distributed import Network  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.reward_recall import RewardConfig, dataset  # noqa: E402

NODES = 4
N_SEQUENCES = 8


def cache_under_loss(sequences, capacity: int, nodes: int, lost: int,
                     floor: float) -> float:
    """First-ask accuracy for a key-sharded table missing one shard.

    A cue lands on `hash(cue) % nodes`. Losing a node removes its cues entirely,
    so those queries fall to guessing and the rest are unaffected.
    """
    right = total = 0
    for sequence in sequences:
        tokens = list(sequence.tokens)
        kinds = sequence.position_kinds()
        body = len(kinds) - len(sequence.query_positions) * 2
        table: dict[int, int] = {}
        order: list[int] = []
        for t in range(1, body):
            if kinds[t] != "value":
                continue
            cue, item = tokens[t - 1], tokens[t]
            if cue % nodes == lost:
                continue                      # that shard is gone
            if cue not in table and len(table) >= capacity:
                table.pop(order.pop(0), None)
            if cue not in table:
                order.append(cue)
            table[cue] = item
        asked: set[int] = set()
        for q in sequence.query_positions:
            cue = tokens[q]
            if cue in asked:
                continue
            asked.add(cue)
            total += 1
            if cue in table:
                right += table[cue] == tokens[q + 1]
            else:
                right += floor            # guessing, in expectation
    return right / max(1, total)


def trained(config: RewardConfig, width: int):
    """A model whose readout has actually been trained.

    **The first version of this file skipped this**, handed an untrained readout
    to the network, and measured 0.031 -- below the trivial floor of 0.125. It
    then reported that a trained cache beats it, which is true and meaningless:
    the comparison was against a model that had not learned anything.

    That is the same unfair-comparison error g10-03 caught in g10-02 and g10-04
    caught in g10-03, committed here in new code. It was visible only because
    the number fell below the floor.
    """
    memory = LocalMemoryConfig(
        vocab_size=config.vocab_size, d_model=width, lr=0.05, key_scale=0.5,
        decay=0.997, derived_keys=True, seed=1)
    model = LocalAssociativeMemory(memory)
    model.wo[:] = model.wv
    train_set = build(config, N_TRAIN, 1)
    rng = np.random.default_rng(1)
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, _, _, _ = train_set[index]
            model.run(tokens, targets, scored, learn=True)
    return memory, model


def store_under_loss(sequences, config: RewardConfig, width: int,
                     nodes: int, absent: set[int] | None,
                     prepared=None) -> float:
    """First-ask accuracy for a dimension-sliced store missing one node."""
    memory, model = prepared if prepared else trained(config, width)
    right = total = 0
    with Network(memory, nodes, model.wv, model.wo) as network:
        for sequence in sequences:
            predicted = network.run(np.asarray(sequence.tokens), absent=absent)
            asked: set[int] = set()
            for q in sequence.query_positions:
                cue = sequence.tokens[q]
                if cue in asked:
                    continue
                asked.add(cue)
                right += predicted[q] == sequence.tokens[q + 1]
                total += 1
    return right / max(1, total)


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    width = args.width if args.width else 32
    config = RewardConfig(delay=8)
    sequences = dataset(config, n_sequences=N_SEQUENCES)
    floor = config.trivial_floor

    print(f"{NODES} nodes, width {width}, {N_SEQUENCES} sequences, "
          f"trivial floor {floor:.3f}\n")
    print(f"{'structure':>18}{'intact':>10}{'one node lost':>16}{'fall':>9}")

    records = []
    prepared = trained(config, width)
    whole = store_under_loss(sequences, config, width, NODES, None, prepared)
    hurt = store_under_loss(sequences, config, width, NODES, {0}, prepared)
    if whole <= floor:
        raise SystemExit(
            f"the intact store scores {whole:.3f}, at or below the trivial "
            f"floor {floor:.3f}. It has not learned the task, so comparing a "
            f"trained cache against it measures nothing. Check the training "
            f"loop before reading any number below.")
    records.append({"structure": "dimension-sliced", "intact": whole,
                    "lost": hurt})
    print(f"{'dimension-sliced':>18}{whole:>10.3f}{hurt:>16.3f}"
          f"{hurt - whole:>+9.3f}")

    for capacity in (config.n_pairs, config.n_rewarded):
        intact = cache_under_loss(sequences, capacity, NODES, -1, floor)
        broken = cache_under_loss(sequences, capacity, NODES, 0, floor)
        records.append({"structure": f"key-sharded ({capacity})",
                        "intact": intact, "lost": broken})
        print(f"{'key-sharded (' + str(capacity) + ')':>18}{intact:>10.3f}"
              f"{broken:>16.3f}{broken - intact:>+9.3f}")

    store = records[0]
    best_cache = max(records[1:], key=lambda r: r["lost"])
    print()
    if best_cache["lost"] > store["lost"]:
        print("  -> THE CACHE DEGRADES TO A BETTER PLACE, even if it degrades")
        print("     less gracefully. Note 030 is right that churn discriminates")
        print("     and WRONG about which way, and 'graceful' is not 'good'.")
    else:
        print("  -> the dimension-sliced store ends up ahead under loss, which")
        print("     is the first measured property favouring it over a table")
    print(f"     store {store['lost']:.3f} against cache "
          f"{best_cache['lost']:.3f} after losing 1 node of {NODES}")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=1))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
