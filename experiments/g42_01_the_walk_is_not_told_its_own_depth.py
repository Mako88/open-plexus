"""g42-01: price the chain-length aid, and try to do without it.

`note 091`'s 0.8578 and `g41-01`'s per-bucket version both hand the walk
`len(chain)` — the true chain is parsed out of the puzzle and its length passed to
`search.beam` as an exact depth. **Published CLUTRR systems are not given it**, and
nothing in this repository has ever measured what it is worth.

Two questions, and the first is the one that makes the second readable:

  - **What is the aid worth?** The `wrong-1` arm is handed `len(chain) + 1`. It is
    not a mechanism anyone would ship; it exists to turn *"the walk is told its
    depth"* into a number. Decision 85 measured overshoot at **0.000 in every
    direction** for the model's own `hops`, which is a different object, and that
    is exactly why this needs measuring here rather than inheriting.
  - **Can the walk find its own end?** `free-10` and `free-15` treat depth as a
    MAXIMUM and select across walk lengths by the endpoint score.

## The stopping rule adds no information the task withholds

A CLUTRR query names its object. `openplexus/search.py` already selects the final
walk by scoring its endpoint against that object and its docstring calls the
object *"the disambiguator"*. The only change is that the score is consulted at
**every** hop instead of at one — and that costs no extra reads, because the
endpoint of a length-k walk is the value hop k+1 already fetches to follow.

**The budget is a residual aid and is named rather than denied.** `free-15`
overshoots the data's deepest chain, so a gap between the two arms says the
maximum is doing the work.

## What this does NOT duplicate, and what was searched

Searched by capability — halt, stop, depth, terminate, when to stop, maximum
hops — across `openplexus/`, `tools/`, `tests/`, `experiments/` and
`docs/archive/`.

- **`LocalMemoryConfig.halt_gate` is the closest existing thing and is a
  DIFFERENT object.** It makes the model's `hops` a maximum and learns which
  hop's RETRIEVAL to read, mixing retrievals inside one read. This selects among
  WALKS of different lengths in a traversal, where there is no mixing and the
  candidates are whole routes. `docs/options/halt-gate.md` is its record and it
  is not reimplemented, imported or competed with.
- **`search.margin`** already ranks finished walks against each other; the
  depth-free selection reuses that ordering rather than inventing a second one.
- **`experiments/g41_01_the_pipeline_on_the_published_protocol.py`** owns the
  reporting protocol — per hop bucket, both subsets, the achievable floor as an
  arm — and its `achievable_floor` and `report` are imported, not copied.
- **`tools/generation_delta.py`** owns the fold, the deltas and the rule table.

Predictions: `experiments/sweeps/g42-01-the-walk-is-not-told-its-own-depth.txt`
"""

from __future__ import annotations

import json
import pathlib
import sys
import time
from collections import defaultdict

ROOT = pathlib.Path(__file__).resolve().parents[1]
for candidate in (ROOT, ROOT / "tools"):
    if str(candidate) not in sys.path:
        sys.path.insert(0, str(candidate))

import numpy as np  # noqa: E402

import clutrr_recovery as cr  # noqa: E402
import generation_delta as gd  # noqa: E402

from experiments import harness  # noqa: E402
from experiments.g41_01_the_pipeline_on_the_published_protocol import (  # noqa: E402
    CLEAN, CONFIG, DATA, achievable_floor)
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.search import beam  # noqa: E402
from openplexus.tasks.clutrr import (  # noqa: E402
    FACT, RELATIONS, ClutrrConfig, load)

SEEDS = tuple(range(8))

#: FROM g41-01, and named that way so the provenance travels with the value.
#: Set from its result rather than from note 065's carried 64, which is the whole
#: point of having run it: at 10 hops the arm reads 0.7185 at width 64 against
#: 0.9076 at 256. **256 rather than 512** because 512 buys 0.0157 of mean while
#: its WORST seed falls, and loses to 256 at 5 and 6 hops -- inside the seed
#: spread, at four times the cost.
WIDTH_FROM_G41 = 256
#: g41-01's best beam at every width it tried. Chosen for this run.
BEAM_FROM_G41 = 8

#: The depth arms. `told` is the incumbent; `wrong-1` prices the aid; the two
#: `free` arms remove it at two budgets, so the budget itself is a variable
#: rather than a constant nobody varied -- which is the g9-11 failure.
DEPTH_ARMS = ("told", "wrong-1", "free-10", "free-15")

#: The fold arms carried from g41-01 so the two runs are comparable. `wrong-delta`
#: is the control and must sit at or below `random`; without it "filling helps"
#: is the finding rather than "this displacement helps".
FOLD_ARMS = ("random", "wrong-delta", "delta")


def depth_for(arm: str, true_length: int) -> tuple[int, bool]:
    """`(depth, free)` for a depth arm. `free` makes `depth` a MAXIMUM."""
    if arm == "told":
        return true_length, False
    if arm == "wrong-1":
        return true_length + 1, False
    if arm == "free-10":
        return 10, True
    if arm == "free-15":
        return 15, True
    raise ValueError(f"unknown depth arm {arm!r}")


def cell(width: int, beam_width: int, seed: int, floor: str,
         table, deltas, vectors) -> list[dict]:
    """One (width, beam, seed) across every depth arm and fold arm."""
    config = ClutrrConfig(root=DATA, split="test", layout="kinship")
    puzzles = load(config)
    names = {config.relation_base + i: r for i, r in enumerate(RELATIONS)}
    folds = {mode: gd.make_fold(table, deltas, mode, seed, vectors)
             for mode in FOLD_ARMS}

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=config.vocab_size, d_model=width, seed=seed,
        context_keys=True, derived_keys=True, decay=1.0))
    allowed = np.arange(config.relation_base,
                        config.relation_base + len(RELATIONS))

    scored: dict = defaultdict(int)
    right: dict = defaultdict(int)
    recovered: dict = defaultdict(int)
    length_right: dict = defaultdict(int)
    floor_right: dict = defaultdict(int)

    for puzzle in puzzles:
        chain = cr.true_chain(puzzle, config)
        if chain is None:
            continue
        answer = names[puzzle.target]
        keys = [(puzzle.hops, "all")]
        if puzzle.max_appearances <= CLEAN:
            keys.append((puzzle.hops, "clean"))
        for key in keys:
            scored[key] += 1
            floor_right[key] += answer == floor

        model.run(np.asarray(puzzle.tokens))
        subject = int(puzzle.tokens[puzzle.query_position - 1])
        target = model.wv[int(puzzle.tokens[puzzle.query_position])]

        for depth_arm in DEPTH_ARMS:
            depth, free = depth_for(depth_arm, len(chain))
            walks = beam(model._final, model.retrieval, model.key_source,
                         model.wv, FACT, subject, target, depth,
                         width=beam_width, branches=4, allowed=allowed,
                         any_length=free)
            if not walks:
                continue
            best = walks[0]
            for key in keys:
                recovered[(*key, depth_arm)] += best.relations == chain
                length_right[(*key, depth_arm)] += (
                    len(best.relations) == len(chain))
            got = [names[t] for t in best.relations if t in names]
            if len(got) != len(best.relations):
                continue
            for mode, fold in folds.items():
                hit = fold(got) == answer
                for key in keys:
                    right[(*key, depth_arm, mode)] += hit

    out = []
    for (hops, subset), total in sorted(scored.items()):
        out.append({
            "width": width, "beam": beam_width, "seed": seed, "hops": hops,
            "subset": subset, "depth_arm": "-", "fold_arm": "majority",
            "puzzles": total, "accuracy": floor_right[(hops, subset)] / total,
            "recovery": 0.0, "length": 0.0,
        })
        for depth_arm in DEPTH_ARMS:
            for fold_arm in FOLD_ARMS:
                out.append({
                    "width": width, "beam": beam_width, "seed": seed,
                    "hops": hops, "subset": subset, "depth_arm": depth_arm,
                    "fold_arm": fold_arm, "puzzles": total,
                    "accuracy": right[(hops, subset, depth_arm, fold_arm)] / total,
                    "recovery": recovered[(hops, subset, depth_arm)] / total,
                    "length": length_right[(hops, subset, depth_arm)] / total,
                })
    return out


def report(records: list[dict], floor: str) -> None:
    """Per hop bucket, both subsets, every depth arm. Never pooled."""
    print(f"achievable floor = the commonest TRAIN answer, `{floor}`.")
    by: dict = defaultdict(list)
    for row in records:
        by[(row["subset"], row["hops"], row["depth_arm"], row["fold_arm"])
           ].append(row)
    hops_seen = sorted({r["hops"] for r in records})
    for subset in ("all", "clean"):
        for fold_arm in FOLD_ARMS:
            print(f"\nsubset {subset}   fold `{fold_arm}`   "
                  f"(mean over {len(SEEDS)} seeds, worst in brackets)")
            print(f"{'hops':>5}{'n':>7}{'floor':>9}"
                  + "".join(f"{a:>22}" for a in DEPTH_ARMS))
            for hops in hops_seen:
                base = by[(subset, hops, "-", "majority")]
                if not base:
                    continue
                fl = sum(b["accuracy"] for b in base) / len(base)
                line = f"{hops:>5}{base[0]['puzzles']:>7}{fl:>9.4f}"
                for depth_arm in DEPTH_ARMS:
                    got = by[(subset, hops, depth_arm, fold_arm)]
                    accs = [g["accuracy"] for g in got]
                    line += (f"{sum(accs) / len(accs):>14.4f}"
                             f"{f'[{min(accs):.4f}]':>8}")
                print(line)

    for quantity in ("recovery", "length"):
        label = ("chain recovered exactly" if quantity == "recovery"
                 else "chain LENGTH right")
        print(f"\n{label}, subset all")
        print(f"{'hops':>5}" + "".join(f"{a:>14}" for a in DEPTH_ARMS))
        for hops in hops_seen:
            line = f"{hops:>5}"
            for depth_arm in DEPTH_ARMS:
                got = by[("all", hops, depth_arm, FOLD_ARMS[0])]
                line += f"{sum(g[quantity] for g in got) / len(got):>14.4f}"
            print(line)


def main() -> int:
    args = harness.parse_args(__doc__)
    started = time.time()

    floor = achievable_floor(DATA, CONFIG)
    deltas = gd.learn_deltas(DATA, CONFIG)
    table = gd.rule_table(DATA, CONFIG)

    # Both taken from g41-01, and the CELL they came from is written down beside
    # them rather than left as a bare number -- `CLAUDE.md`'s g9-11 calibration
    # cost 0.58 of recovery to a constant carried with no note of its origin.
    # No CLI knob for the beam: the harness has no argument that means "beam
    # width", and borrowing one that means something else (`--slots` is a tag's
    # capacity) is how two different quantities end up sharing a name.
    width = args.width or WIDTH_FROM_G41
    beam_width = BEAM_FROM_G41
    seeds = (args.seed,) if args.seed is not None else SEEDS

    records: list[dict] = []
    for seed in seeds:
        vectors = gd.contrastive_vectors(DATA, CONFIG, table, width=32,
                                         seed=seed, epochs=8, lr=0.05,
                                         temperature=0.1)
        records.extend(cell(width, beam_width, seed, floor, table, deltas,
                            vectors))

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(records), encoding="utf-8")
    else:
        report(records, floor)
    print(f"\nCOST: {time.time() - started:.1f}s wall, one process, "
          f"width {width}, beam {beam_width}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
