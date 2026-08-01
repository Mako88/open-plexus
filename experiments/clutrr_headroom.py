"""Withholding composition facts does not make CLUTRR an instrument either.

`clutrr_ceiling.py` established that the shipped benchmark is answered in full by
62 facts counted from its two-hop rows plus a search over bracketings. The obvious
repair is to withhold some of those facts: hold out 20 and the ceiling for any
reasoner over what remains falls to 0.3138, with 68% of test puzzles unreachable.
That looked like headroom, and **it is not there**.

The training set contains 4,998 THREE-hop rows, and a three-hop row whose first
pair is known determines a second pair. Propagating that to a fixpoint — pure
deduction, no model, no scoring — recovers **every withheld fact** and returns the
test ceiling to about 0.99, even with 40 of the 62 held out.

So the facts are not really withheld. They are stated in a different shape.

Four arms per ablation, and the third is the one that matters:

    kept          the ceiling over the facts left after the ablation
    propagated    the same, after the three-hop rows have been folded in
    recovered     the share of WITHHELD facts propagation gets back, and how
                  many it gets wrong, which is the cost of the deduction
    counted       `composition.Composition` — the project's own mechanism, on
                  the withheld facts only. Reported against the marginal, which
                  is what returning nothing useful looks like

    python experiments/clutrr_headroom.py --json out/clutrr-headroom.json
"""

from __future__ import annotations

import argparse
import json
import pathlib
import random
import sys
import time
from collections import Counter

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from openplexus.composition import Composition  # noqa: E402
from openplexus.grounding import STATISTICS  # noqa: E402
from openplexus.tasks.clutrr import (ClutrrConfig, RELATIONS,  # noqa: E402
                                     composition_table, load, reachable)

DATA = ROOT / "data" / "clutrr"

#: How many of the 62 facts to withhold. Swept, and it brackets the range: at 40
#: the un-propagated ceiling is 0.0457, which is below guessing, so if headroom
#: existed anywhere it would be visible here.
HOLD = (10, 20, 30, 40)

#: Seeds. Three is this project's floor and is chosen here as that floor; the
#: propagated ceiling varies by under 0.01 across them, so the effect is not one
#: three seeds could miss.
SEEDS = (0, 1, 2)

#: The statistic and the combiner for the counted arm. `conditional` is the one
#: that refuses an ever-present distractor (g39-04) and `min` is the demanding
#: combiner; both are swept in the printed sweep below rather than pinned
#: silently.
STATISTIC, COMBINE = "conditional", "min"


def propagate(table, triples):
    """Every pair a three-hop row determines, to a fixpoint. **Deduction only.**

    A row `(a, b, c) -> t` says the whole chain reaches `t`. If `a . b` is known
    to be `x`, then `x . c` must be `t`; if `b . c` is known to be `y`, then
    `a . y` must be `t`. Neither step estimates anything.

    **An existing entry is never overwritten.** A contradiction would mean the
    algebra is not a function, and quietly taking the newer value would hide
    that; the wrong-answer column exists to measure how often the deduction
    disagrees with the fact it replaced.
    """
    grown = dict(table)
    changed = True
    while changed:
        changed = False
        for (first, second, third), target in triples:
            if (first, second) in grown:
                key = (grown[(first, second)], third)
                if key not in grown:
                    grown[key] = target
                    changed = True
            if (second, third) in grown:
                key = (first, grown[(second, third)])
                if key not in grown:
                    grown[key] = target
                    changed = True
    return grown


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    args = parser.parse_args()

    config = ClutrrConfig(root=DATA, split="train")
    if not config.path.exists():
        raise SystemExit(f"no data at {config.path}: python tools/fetch_clutrr.py")

    started = time.time()
    train = load(config)
    base = config.relation_base
    facts = {(left - base, right - base): target - base
             for (left, right), target in composition_table(train).items()}
    triples = [(tuple(r - base for r in p.chain), p.target - base)
               for p in train if p.hops == 3]
    test = load(ClutrrConfig(root=DATA, split="test"))
    print(f"{len(facts)} facts over {len(RELATIONS)} relations, "
          f"{len(triples)} three-hop rows, {len(test)} test puzzles\n")

    header = (f"{'held':>5}{'ceiling kept':>14}{'ceiling propagated':>20}"
              f"{'recovered':>11}{'wrong':>8}{'counted':>9}{'marginal':>10}")
    print(header)
    print("-" * len(header))
    rows = []
    for hold in HOLD:
        gathered = []
        for seed in SEEDS:
            keys = list(facts)
            random.Random(seed).shuffle(keys)
            held, kept = keys[:hold], keys[hold:]
            table = {key: facts[key] for key in kept}
            grown = propagate(table, triples)

            counts = Composition(len(RELATIONS))
            for key in kept:
                counts.observe(key[0], key[1], facts[key])
            statistic = STATISTICS[STATISTIC]
            counted = sum(counts.answer(a, b, statistic, COMBINE) == facts[(a, b)]
                          for a, b in held) / hold
            commonest = Counter(facts[key] for key in kept).most_common(1)[0][0]
            gathered.append({
                "hold": hold, "seed": seed,
                "ceiling_kept": _ceiling(table, test, base),
                "ceiling_propagated": _ceiling(grown, test, base),
                "recovered": sum(key in grown for key in held) / hold,
                "wrong": sum(key in grown and grown[key] != facts[key]
                             for key in held) / hold,
                "counted": counted,
                "marginal": sum(facts[key] == commonest for key in held) / hold,
            })
        rows.extend(gathered)
        mean = lambda name: sum(g[name] for g in gathered) / len(gathered)  # noqa: E731
        print(f"{hold:>5}{mean('ceiling_kept'):>14.4f}"
              f"{mean('ceiling_propagated'):>20.4f}{mean('recovered'):>11.4f}"
              f"{mean('wrong'):>8.4f}{mean('counted'):>9.4f}"
              f"{mean('marginal'):>10.4f}")

    print("\nTHE COUNTED ARM, swept rather than pinned. 20 held out, "
          "share of withheld facts recovered.")
    for name in ("conditional", "ppmi", "local", "weighted"):
        line = []
        for combine in ("min", "geometric", "mean", "max"):
            got = []
            for seed in SEEDS:
                keys = list(facts)
                random.Random(seed).shuffle(keys)
                held, kept = keys[:20], keys[20:]
                counts = Composition(len(RELATIONS))
                for key in kept:
                    counts.observe(key[0], key[1], facts[key])
                got.append(sum(counts.answer(a, b, STATISTICS[name], combine)
                               == facts[(a, b)] for a, b in held) / 20)
            share = sum(got) / len(got)
            rows.append({"arm": "counted", "statistic": name,
                         "combine": combine, "hold": 20, "recovered": share})
            line.append(f"{combine} {share:.4f}")
        print(f"  {name:>12}: " + "   ".join(line))

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"\n{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


def _ceiling(table, puzzles, base: int) -> float:
    """The share of test puzzles reachable under a table of relation-id pairs."""
    shifted = {(left + base, right + base): target + base
               for (left, right), target in table.items()}
    return sum(p.target in reachable(p.chain, shifted)
               for p in puzzles) / len(puzzles)


if __name__ == "__main__":
    raise SystemExit(main())
