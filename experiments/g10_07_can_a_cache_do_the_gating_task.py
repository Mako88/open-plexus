"""Does a plain cache solve `reward_recall`, the task the whole g9 line is about?

[g10-06](sweeps/g10-06-a-cache-that-persists.txt) found a bounded per-key cache
beating the associative store by 1.58 bits on character text, and closed by
saying character text may be the wrong task to justify an associative store at
all. **The uncomfortable version of that question is about this project's own
task**, not the corpus.

`reward_recall` presents cue -> item bindings and asks about the rewarded ones.
A cache does that in one line: `table[cue] = item`, then look up the cue. If a
hash table answers the task the entire g9 gating line was built for, then the
difficulty g9 has been measuring is a property of the STORE that was chosen and
not of the problem.

That is worth knowing whether or not it is welcome.

## What is fair to compare

The model's `none` arm reaches about 0.65 on first asks and the oracle reaches
1.0 (g9-12, node 64). The cache here sees the same token stream and may use only
tokens before the query, which is the same rule.

**The cache is given no reward signal and no oracle.** It stores every binding it
sees. That is the point: if storing everything is enough, then selecting what to
store — which is the whole g9 line — was never required by the task.

## The capacity question does not disappear

A cache with room for every binding is not a fair stand-in for a device with
limits, so capacity is swept. `reward_recall` presents 24 bindings and rewards 4,
so a cache of 4 must choose, and choosing without knowing the future is exactly
the gating problem in a different container.

**Where the cache stops working is therefore more interesting than where it
works**, and a capacity sweep is what separates "the task is trivial" from "the
task is hard for a reason that is not about vectors".
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.harness import parse_args  # noqa: E402
from openplexus.tasks.reward_recall import RewardConfig, dataset  # noqa: E402

#: A cache of 24 holds every binding; 4 is the number that are rewarded.
CAPACITIES = (2, 4, 8, 16, 24, 64)
SEEDS = (1, 2, 3)


def score(sequences, capacity: int) -> tuple[float, float]:
    """First-ask accuracy for a cache holding the most recent `capacity` cues.

    Evicts the oldest, which is the only policy available to something that does
    not know which bindings will be asked about — the same blindness the g9
    gating line exists to address.
    """
    right = total = 0
    for sequence in sequences:
        tokens = list(sequence.tokens)
        kinds = sequence.position_kinds()
        body = len(kinds) - len(sequence.query_positions) * 2
        table: dict[int, int] = {}
        order: list[int] = []
        for t in range(1, body):
            # A binding is a value position, and its cue is the token before it.
            if kinds[t] != "value":
                continue
            cue, item = tokens[t - 1], tokens[t]
            if cue not in table and len(table) >= capacity:
                table.pop(order.pop(0), None)
            if cue not in table:
                order.append(cue)
            table[cue] = item
        asked: set[int] = set()
        for q in sequence.query_positions:
            cue = tokens[q]
            if cue in asked:
                continue                      # first asks only, as g9 scores it
            asked.add(cue)
            right += table.get(cue, -1) == tokens[q + 1]
            total += 1
    return right / max(1, total), total


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    delay = int(args.scale) if args.scale is not None else 8
    seeds = [args.seed] if args.seed is not None else list(SEEDS)

    config = RewardConfig(delay=delay)
    print(f"delay {delay}; {config.n_pairs} bindings per sequence, "
          f"{config.n_rewarded} rewarded; trivial floor "
          f"{config.trivial_floor:.3f}")
    print("the model, for reference: ungated about 0.65 on first asks, "
          "oracle 1.000\n")
    print(f"{'capacity':>10}{'first-ask accuracy':>21}")

    records = []
    for capacity in CAPACITIES:
        values = []
        for seed in seeds:
            sequences = dataset(RewardConfig(delay=delay, seed=seed),
                                n_sequences=24)
            accuracy, asked = score(sequences, capacity)
            values.append(accuracy)
        mean = sum(values) / len(values)
        records.append({"delay": delay, "capacity": capacity,
                        "accuracy": mean, "seeds": len(values)})
        print(f"{capacity:>10}{mean:>21.3f}")

    enough = next((r for r in records if r["accuracy"] > 0.99), None)
    print()
    if enough:
        print(f"  -> a cache of {enough['capacity']} answers the task PERFECTLY,")
        print("     with no reward signal and no oracle. Storing everything is")
        print("     enough, so SELECTING what to store -- the whole g9 line --")
        print("     is not required by the task, only by the vector store")
    else:
        best = max(records, key=lambda r: r["accuracy"])
        print(f"  -> the best cache reaches {best['accuracy']:.3f} at capacity "
              f"{best['capacity']}, so the task is not")
        print("     trivially answerable by storing everything")

    starved = [r for r in records if r["capacity"] < config.n_pairs]
    if starved:
        worst = min(starved, key=lambda r: r["accuracy"])
        print(f"\n  and at capacity {worst['capacity']}, below the "
              f"{config.n_pairs} bindings presented, it falls to "
              f"{worst['accuracy']:.3f}")
        print("  -- which is the gating problem in a different container: a")
        print("     bounded cache must also choose, and evicting the oldest is")
        print("     the only policy available without knowing the future")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=1))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
