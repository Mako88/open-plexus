"""Note 072's measurement: which concept OWNS each binding, per CLUTRR layout?

`local_memory` writes with `memory = concepts.matrix(previous_concept)` and
`PairKeys.concept` returns `tokens[t]`, so a binding `key(t-1) -> value(t)` is owned by
`tokens[t-1]`. Per four-token fact block:

    kinship  FACT s r o    key(FACT,s)->r owner s    key(s,r)->o owner r   <-- RELATION
    closure  FACT s o r    key(FACT,s)->o owner s    key(s,o)->r owner o

`walk_from` and `beam` read `key(entity, relation) -> next entity`, which is kinship's
SECOND binding — so under `kinship` every binding the traversal reads is owned by a
relation, of which there are twenty and always will be. Concept partitioning's case is a
per-node capability that GROWS with the network (decision 134), and a twenty-node ceiling
is worse than the sixteen-node dimension ceiling it exists to fix.

## Read the CATEGORY, not the owner count

CLUTRR renumbers entities per puzzle by first appearance with `max_entities=11`, so the
distinct-owner count under `closure` is an artifact of that renumbering rather than a
distribution. **What the layouts differ on is whether the owner is an entity (grows with
the corpus) or a relation (capped at twenty).**

## What this does NOT answer

`Ring.balance` is the instrument for balance proper and has been pointed at neither
layout. And a domain with thousands of relation types would not have this problem, so the
defect is in the INTERACTION of two options, not in either alone.
"""

from __future__ import annotations

import argparse
import collections
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from openplexus.tasks.clutrr import (  # noqa: E402
    RELATIONS, RESERVED, ClutrrConfig, load)


def owners(config: ClutrrConfig) -> collections.Counter:
    """Owner of each traversal binding, i.e. the second binding of each fact block."""
    counted: collections.Counter = collections.Counter()
    for puzzle in load(config):
        tokens = puzzle.tokens
        for start in range(0, len(tokens) - 3, 4):
            if tokens[start] >= RESERVED:
                # Reached `QUERY s o`, which states no fact and writes no binding.
                break
            counted[int(tokens[start + 2])] += 1
    return counted


def name_of(token: int, config: ClutrrConfig) -> str:
    if token >= config.relation_base:
        return RELATIONS[token - config.relation_base]
    return f"entity#{token - config.entity_base}"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT / "data" / "clutrr")
    parser.add_argument("--split", default="test")
    args = parser.parse_args()

    for layout in ("closure", "kinship"):
        config = ClutrrConfig(root=args.root, split=args.split, layout=layout)
        counted = owners(config)
        total = sum(counted.values())
        relation_owned = sum(n for token, n in counted.items()
                             if token >= config.relation_base)
        covering = running = 0
        for _, n in counted.most_common():
            running += n
            covering += 1
            if running >= 0.9 * total:
                break
        print(f"\n=== layout={layout} ===  {total:,} traversal bindings")
        print(f"  owned by a RELATION: {relation_owned:,} "
              f"({relation_owned / total:.1%})")
        print(f"  distinct owners {len(counted)}, "
              f"{covering} of them cover 90%")
        for token, n in counted.most_common(3):
            print(f"    {name_of(token, config):16s} {n:7,}  {n / total:6.1%}")
    print("\nkinship routes every traversal binding to a RELATION -- twenty of them,")
    print("forever. closure routes to entities, which grow with the corpus. Read the")
    print("category and not the owner counts: CLUTRR renumbers entities per puzzle.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
