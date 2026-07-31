"""g41-01: the composition pipeline, reported the way `g37-01` says it must be.

`note 091` puts the end-to-end CLUTRR number at **0.8578**, and
`tools/generation_delta.py --end-to-end` reproduces it exactly. That figure is
POOLED over all 1,146 test puzzles, taken at one seed, at constants carried from
`note 065`, and reported against no floor.

`g37-01` established four requirements for any CLUTRR number, and the pooled
figure satisfies none of them:

  - **Per hop bucket, never pooled.** Hops 2-3 are the only depths in TRAIN, so
    they are recall and 4-10 are generalisation. Pooling puts the two on one line.
  - **Split on `max_appearances`,** and the floor MOVES with it in the unhelpful
    direction (`note 059`, `g37-01`).
  - **The floor is the commonest TRAINING answer, not the bucket's own majority.**
    `g37-03` found `g37-01`'s floor row was an ORACLE floor: `brother` is 0 of 38
    at 2 hops and 0 of 105 at 3.
  - **The local arm's constants must be swept.** `g37-03` swept the reference and
    not the local arm, which is the failure `CLAUDE.md` names by name. `width 64`,
    `beam width 4` and `branches 4` all arrive from `note 065`'s configuration.

**This changes no mechanism.** It is the same pipeline `note 090`/`091` measured,
reported against a floor and split on the two axes the data has. A number that
survives that is quotable against the published table in `g37-01`; the pooled one
is not.

## The two aids are KEPT here, and named rather than removed

The walk is handed `len(chain)` — the true chain is parsed to get its depth, so
the model is told how many hops to take. And *"deltas add"* is arithmetic supplied
by hand (`note 090` says so in its own text). **Both stay in this run**, so every
number here is directly comparable to `note 091`'s.

Removing them is `g42-01` and is a different question. Mixing the two would make a
fall unattributable between *"the protocol was generous"* and *"the aid was doing
the work"* — `g37-02`'s exact mistake, one level up.

## What this does NOT duplicate, and what was searched

Searched by capability — clutrr, fold, delta, chain recovery, hop bucket,
max_appearances, floor — across `experiments/`, `tools/`, `openplexus/`, `tests/`
and `docs/archive/`.

- **`tools/generation_delta.py` is IMPORTED, not reimplemented.** `learn_deltas`,
  `rule_table`, `make_fold`, `contrastive_vectors` and `MODES` all come from it,
  so the arms here are the same objects that produce the published figure. What
  this adds is the per-puzzle bookkeeping the tool does not keep: it prints one
  pooled ratio per mode and discards which puzzle was which.
- **`tools/clutrr_recovery.py`** supplies `true_chain` and is the harness whose
  0.8770 recovery this reproduces. Its own sweep is over `search` against `beam`;
  this holds `beam` and sweeps the constants underneath it.
- **`experiments/g37_02_does_clutrr_pass_g0.py`** ran a local arm that is
  deliberately one hop with NO search, to isolate the objective from the search
  mechanism. That arm is not this pipeline and its numbers are not a bound on it.
- **`experiments/harness.py`** carries `--seed`/`--json`/`--aggregate`, so no
  matrix plumbing is written here.

Predictions: `experiments/sweeps/g41-01-the-pipeline-on-the-published-protocol.txt`
"""

from __future__ import annotations

import json
import pathlib
import sys
import time
from collections import Counter, defaultdict

ROOT = pathlib.Path(__file__).resolve().parents[1]
for candidate in (ROOT, ROOT / "tools"):
    if str(candidate) not in sys.path:
        sys.path.insert(0, str(candidate))

import numpy as np  # noqa: E402

import clutrr_recovery as cr  # noqa: E402
import generation_delta as gd  # noqa: E402

from experiments import harness  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.search import beam  # noqa: E402
from openplexus.tasks.clutrr import (  # noqa: E402
    FACT, RELATIONS, ClutrrConfig, load)

DATA = ROOT / "data" / "clutrr"
CONFIG = "gen_train23_test2to10"

#: Swept, because `note 065` chose them at one configuration and nothing since
#: has varied them. `width 64` and `(4, 4)` are the carried values.
WIDTHS = (32, 64, 128)

#: Swept. `note 065` chose 4 and `tools/clutrr_recovery.py` has carried it since.
BEAMS = (4, 8)

#: NOT SWEPT, and carried from `note 065`'s configuration exactly as `width` and
#: `beam width` were. **This is a third pin of the same kind as the two this run
#: exists to correct**, and saying so is the honest version -- three axes was as
#: much as one grid could hold. `tools/check_constants.py` is what made it
#: visible; it was written after this file and flagged it immediately.
BRANCHES = 4

#: EIGHT, not the usual three. A timing probe at width 64 returned **1.0000**
#: recovery in four hop buckets and 1.0000 on the `delta` arm in two, and
#: `CLAUDE.md`'s calibration on `g36-01` is explicit: a failure occurring in one
#: draw of eight is missed by three seeds about 67% of the time, so three clean
#: seeds is the EXPECTED outcome even when a one-in-eight failure exists. A cell
#: at 1.0000 is the case that must not be written up as "every". A whole grid is
#: about 12s per (seed, width), so this costs minutes.
SEEDS = tuple(range(8))

#: `max_appearances <= 2` is `note 059`'s clean arm. Reported BESIDE the whole
#: bucket rather than instead of it, because the floor differs between them.
CLEAN = 2

ARMS = ("majority", *gd.MODES)


def achievable_floor(root: pathlib.Path, config: str) -> str:
    """The commonest answer in TRAIN — the floor a model can actually reach.

    NOT the test bucket's own majority. `g37-03` found that reading in `g37-01`
    and corrected it: the bucket majority requires knowing the test labels, so a
    result read against it is judged against something no model can reach.
    """
    train = load(ClutrrConfig(root=root, split="train", layout="kinship"))
    names = {ClutrrConfig(root=root).relation_base + i: r
             for i, r in enumerate(RELATIONS)}
    counts = Counter(names[p.target] for p in train)
    return counts.most_common(1)[0][0]


def cell(width: int, beam_width: int, seed: int, floor: str,
         table, deltas, vectors) -> list[dict]:
    """One (width, beam, seed). Returns one record per (hops, arm, subset).

    Per-puzzle rather than pooled, because every split this run exists for is a
    partition of the puzzles and a pooled ratio cannot be split afterwards.
    """
    config = ClutrrConfig(root=DATA, split="test", layout="kinship")
    puzzles = load(config)
    names = {config.relation_base + i: r for i, r in enumerate(RELATIONS)}
    folds = {mode: gd.make_fold(table, deltas, mode, seed, vectors)
             for mode in gd.MODES}

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=config.vocab_size, d_model=width, seed=seed,
        context_keys=True, derived_keys=True, decay=1.0))
    allowed = np.arange(config.relation_base,
                        config.relation_base + len(RELATIONS))

    scored: dict = defaultdict(int)
    right: dict = defaultdict(int)
    recovered: dict = defaultdict(int)

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
            # The floor is an ARM, scored on the same puzzles as every other, so
            # it cannot drift out of step with what it is a floor for.
            right[(*key, "majority")] += answer == floor

        model.run(np.asarray(puzzle.tokens))
        subject = int(puzzle.tokens[puzzle.query_position - 1])
        target = model.wv[int(puzzle.tokens[puzzle.query_position])]
        # AID 1, kept and named: `len(chain)` tells the walk its own depth.
        walks = beam(model._final, model.retrieval, model.key_source, model.wv,
                     FACT, subject, target, len(chain), width=beam_width,
                     branches=BRANCHES, allowed=allowed)
        if not walks:
            continue
        for key in keys:
            recovered[key] += walks[0].relations == chain
        got = [names[t] for t in walks[0].relations if t in names]
        if len(got) != len(walks[0].relations):
            continue
        for mode, fold in folds.items():
            hit = fold(got) == answer
            for key in keys:
                right[(*key, mode)] += hit

    out = []
    for (hops, subset), total in sorted(scored.items()):
        for arm in ARMS:
            out.append({
                "width": width, "beam": beam_width, "seed": seed,
                "hops": hops, "subset": subset, "arm": arm,
                "puzzles": total,
                "correct": right[(hops, subset, arm)],
                "accuracy": right[(hops, subset, arm)] / total,
                "recovery": recovered[(hops, subset)] / total,
            })
    return out


def main() -> int:
    args = harness.parse_args(__doc__)
    started = time.time()

    floor = achievable_floor(DATA, CONFIG)
    deltas = gd.learn_deltas(DATA, CONFIG)
    table = gd.rule_table(DATA, CONFIG)

    widths = (args.width,) if args.width else WIDTHS
    seeds = (args.seed,) if args.seed is not None else SEEDS

    records: list[dict] = []
    for seed in seeds:
        vectors = gd.contrastive_vectors(DATA, CONFIG, table, width=32,
                                         seed=seed, epochs=8, lr=0.05,
                                         temperature=0.1)
        for width in widths:
            for beam_width in BEAMS:
                records.extend(cell(width, beam_width, seed, floor,
                                    table, deltas, vectors))

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(records), encoding="utf-8")
    else:
        report(records, floor)
    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")
    return 0


def report(records: list[dict], floor: str) -> None:
    """Per hop bucket, both subsets, never pooled."""
    print(f"achievable floor = the commonest TRAIN answer, `{floor}`. "
          f"It is an ARM, scored on the same puzzles.")
    by: dict = defaultdict(list)
    for row in records:
        by[(row["width"], row["beam"], row["subset"], row["hops"], row["arm"])
           ].append(row)
    for width, beam_width in sorted({(r["width"], r["beam"]) for r in records}):
        for subset in ("all", "clean"):
            print(f"\nwidth {width}  beam {beam_width}  subset {subset}")
            print(f"{'hops':>5}{'n':>7}{'recov':>8}"
                  + "".join(f"{a:>13}" for a in ARMS))
            for hops in sorted({r["hops"] for r in records}):
                cells = by[(width, beam_width, subset, hops, ARMS[0])]
                if not cells:
                    continue
                n = cells[0]["puzzles"]
                rec = sum(c["recovery"] for c in cells) / len(cells)
                line = f"{hops:>5}{n:>7}{rec:>8.4f}"
                for arm in ARMS:
                    got = by[(width, beam_width, subset, hops, arm)]
                    line += f"{sum(g['accuracy'] for g in got) / len(got):>13.4f}"
                print(line)

    # A MEAN HIDES A SINGLE BAD SEED, and this run has cells at 1.0000 where
    # that is exactly the risk. `g39-06` pooled a confound over ten words the
    # day after `g39-05` warned about it and hid a 47-fold collapse; the answer
    # there was to print the worst case beside the mean rather than to remember.
    print("\nDELTA ARM AND RECOVERY, WORST SEED against the mean")
    for width, beam_width in sorted({(r["width"], r["beam"]) for r in records}):
        for subset in ("all", "clean"):
            print(f"\nwidth {width}  beam {beam_width}  subset {subset}")
            print(f"{'hops':>5}{'delta mean':>12}{'delta worst':>13}"
                  f"{'recov mean':>12}{'recov worst':>13}{'seeds':>7}")
            for hops in sorted({r["hops"] for r in records}):
                got = by[(width, beam_width, subset, hops, "delta")]
                if not got:
                    continue
                accs = [g["accuracy"] for g in got]
                recs = [g["recovery"] for g in got]
                print(f"{hops:>5}{sum(accs) / len(accs):>12.4f}{min(accs):>13.4f}"
                      f"{sum(recs) / len(recs):>12.4f}{min(recs):>13.4f}"
                      f"{len(got):>7}")


if __name__ == "__main__":
    raise SystemExit(main())
