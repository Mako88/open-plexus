"""g37-02: does CLUTRR-symbolic pass G0, and where does our rule land on it?

`GOALS.md` §4, the gate the predecessor project failed at a cost of about a year:

> before any learning mechanism is written, the task must be shown to have
> substantial headroom between what a random frozen substrate achieves and what
> a strong non-local reference achieves, with both measured, multi-seed, and
> with the base rate of a constant predictor reported alongside.

`g37-01` computed the third of those three from the data. **This measures the
other two**, which is what G0 actually asks for: a *cited* reference is not a
measured one, and the borrowed numbers in `g37-01` came from an HTML rendering
via a summarising fetch and are marked as such.

**Why this instrument and not `closure`.** `closure` was designed by this
project, and its usable band against the honest floor is 0.092 (`g14-01`).
CLUTRR is somebody else's benchmark, its published evaluation is on exactly the
split fetched here, and `g37-01` puts the floor at 0.1370 overall against
references reported from 0.39 to 0.97.

## The arms — deliberately the same four as `g14-01`

    majority    always answer the commonest relation in TRAIN    the base rate
    frozen      our model, random Wo, NO LEARNING                the substrate
    local       our model under the delta rule                   the candidate
    attention   backprop, softmax over positions, Adam           the reference

`attention` is given every advantage the local rule is not: a real optimiser,
softmax over positions, gradients reaching every parameter. **That is the point.**
It measures what the TASK admits, not what our rule achieves, and `g14-01`'s
calibration is the reason the width and epoch count are treated as load-bearing:
at width 64 / 4 epochs that experiment reported `closure` FAILING G0, and the
failure was the reference being undersized rather than the task being unlearnable.

## The split that must not be averaged over

Results are reported **per hop bucket**, never pooled, for two reasons that
`g37-01` measured:

  - The 2-hop test bucket is 38 puzzles at a floor of 0.5000 and the 3-hop is 105
    at 0.4286. **They are also the only depths in TRAIN**, so they are recall and
    the rest is generalisation. Pooling puts the two on one line.
  - The floor MOVES with `max_appearances` and in the unhelpful direction —
    0.3810 against 0.2336 at 6 hops — because removing repeated entities removes
    the harder puzzles. So every cell prints its own floor.

**The data does not move with the seed.** CLUTRR is a fixed file, so seeds vary
model initialisation and training order only. Variance here is a statement about
the model, not about the task, and it is narrower than `g14-01`'s for that reason.

Predictions: `experiments/sweeps/g37-02-does-clutrr-pass-g0.txt`
"""

from __future__ import annotations

import argparse
import json
import pathlib
import sys
import time
from collections import Counter

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from openplexus.models.attention import (  # noqa: E402
    Adam, AttentionConfig, ShiftedAttention)
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks import clutrr  # noqa: E402
from openplexus.tasks.kinship import IGNORE  # noqa: E402

DATA = ROOT / "data" / "clutrr"
WIDTH = 256
EPOCHS = 4
SEEDS = (0, 1, 2)
ARMS = ("majority", "frozen", "local", "attention")

#: Carried from `g14-01`, WHERE IT WAS CHOSEN FOR `closure` AT 120 TOKENS.
#: CLUTRR sequences are 4*hops+3, so 11 to 43 tokens — a different regime, and
#: `CLAUDE.md`'s calibration on carried constants is explicit that naming the
#: risk does not remove it. **The cost probe reports what one epoch costs so the
#: choice can be revisited against a measurement rather than assumed.**
ATTENTION_WIDTH, ATTENTION_EPOCHS = 128, 16

#: How many training puzzles to use. **`None` means all 9,074**, and that is what
#: the cost probe chose: it measured the expensive arm at **1.68 ms** per
#: training sequence, so the full split is 12.2 minutes for three seeds. A
#: subsample would have needed a defence of its size and buys nothing here.
#:
#: CLUTRR sequences are 11 to 43 tokens against `closure`'s 120, which is why
#: this is affordable where `g14-01` had to cap at 300.
N_TRAIN = None


def _targets(puzzle) -> np.ndarray:
    """`IGNORE` everywhere but the query slot.

    The answer is scored at `query_position`, which is the LAST token: a model
    predicting position `i+1` from position `i` emits the relation there. The
    answer is never in the stream — a question whose answer follows it is not a
    question.
    """
    targets = np.full(len(puzzle.tokens), IGNORE, dtype=np.int64)
    targets[puzzle.query_position] = puzzle.target
    return targets


def _local(config, seed: int) -> LocalAssociativeMemory:
    """Pair keys, one hop, NO search — this measures the objective rather than
    the search mechanism, and mixing them makes the result unattributable."""
    return LocalAssociativeMemory(LocalMemoryConfig(
        d_model=WIDTH, vocab_size=config.vocab_size, seed=seed,
        derived_keys=True, context_keys=True))


def run_majority(train, test) -> list[int]:
    """Always answer the commonest relation in TRAINING. The base rate.

    Taken from TRAIN and not from test, because a constant fitted to the test
    split is not a floor, it is a model with one parameter tuned on the answers.
    """
    counts = Counter(puzzle.target for puzzle in train)
    answer = counts.most_common(1)[0][0]
    return [answer] * len(test)


def run_local(train, test, config, seed: int, learn: bool) -> list[int]:
    model = _local(config, seed)
    if learn:
        for _ in range(EPOCHS):
            for puzzle in train:
                tokens = np.asarray(puzzle.tokens, dtype=np.int64)
                targets = _targets(puzzle)
                model.run(tokens, targets, targets != IGNORE, learn=True)
    out = []
    for puzzle in test:
        predicted = model.run(np.asarray(puzzle.tokens, dtype=np.int64))
        out.append(int(predicted[puzzle.query_position]))
    return out


def run_attention(train, test, config, seed: int) -> list[int]:
    """The strong non-local reference, given every advantage."""
    model = ShiftedAttention(AttentionConfig(
        vocab_size=config.vocab_size, d_model=ATTENTION_WIDTH, seed=seed))
    optimiser = Adam(model.params, lr=3e-3)
    rng = np.random.default_rng(seed)
    order = np.arange(len(train))
    for _ in range(ATTENTION_EPOCHS):
        rng.shuffle(order)
        for index in order:
            puzzle = train[index]
            tokens = np.asarray(puzzle.tokens, dtype=np.int64)
            targets = _targets(puzzle)
            logits, cache = model.forward(tokens)
            _, grads = model.loss_and_backward(logits, cache, targets,
                                               targets != IGNORE)
            optimiser.step(grads)
    out = []
    for puzzle in test:
        predicted = model.predict(np.asarray(puzzle.tokens, dtype=np.int64))
        out.append(int(predicted[puzzle.query_position]))
    return out


def score(predictions, test) -> dict:
    """Accuracy per hop bucket, with each bucket's own floor beside it."""
    buckets: dict[int, list] = {}
    for predicted, puzzle in zip(predictions, test):
        buckets.setdefault(puzzle.hops, []).append((predicted, puzzle))
    out = {}
    for hops, rows in sorted(buckets.items()):
        got = sum(1 for predicted, puzzle in rows if predicted == puzzle.target)
        floor = Counter(p.target for _, p in rows).most_common(1)[0][1]
        clean = [(pred, p) for pred, p in rows if p.max_appearances <= 2]
        out[str(hops)] = {
            "n": len(rows),
            "accuracy": got / len(rows),
            "floor": floor / len(rows),
            "clean_n": len(clean),
            "clean_accuracy": (sum(1 for pred, p in clean
                                   if pred == p.target) / len(clean)
                               if clean else None),
        }
    return out


def one_cell(arm: str, seed: int, train, test, config) -> dict:
    started = time.time()
    if arm == "majority":
        predictions = run_majority(train, test)
    elif arm == "frozen":
        predictions = run_local(train, test, config, seed, learn=False)
    elif arm == "local":
        predictions = run_local(train, test, config, seed, learn=True)
    else:
        predictions = run_attention(train, test, config, seed)
    elapsed = time.time() - started

    return {
        "arm": arm,
        "seed": seed,
        "seconds": round(elapsed, 1),
        "buckets": score(predictions, test),
        # Written from the parameters actually used, so a stale artifact cannot
        # be analysed as a fresh one -- rule 11b.
        "condition": (f"{arm}|d{WIDTH}|seed{seed}|train{len(train)}x{EPOCHS}"
                      f"|att{ATTENTION_WIDTH}x{ATTENTION_EPOCHS}"
                      f"|test{len(test)}"),
    }


def cost_probe(train, test, config) -> None:
    """What the expensive arm costs, before anything is spent on it."""
    sample = train[:40]
    started = time.time()
    run_attention(sample, test[:5], config, 0)
    per = (time.time() - started) / (len(sample) * ATTENTION_EPOCHS)
    print("most expensive arm: attention")
    print(f"  {per * 1000:.2f} ms per training sequence")
    size = len(train) if N_TRAIN is None else N_TRAIN
    print(f"  one cell at {size} training puzzles: "
          f"{per * size * ATTENTION_EPOCHS / 60:.1f} min")
    print(f"  {len(SEEDS)} seeds: "
          f"{per * size * ATTENTION_EPOCHS * len(SEEDS) / 60:.1f} min")
    print("  majority and frozen are nearly free; local trains without a "
          "backward pass")


def main() -> None:
    # `harness.parse_args` is the usual route and carries a fixed flag set this
    # script does not use. The GUARD is the part that matters -- it is why the
    # rail exists -- so it is called explicitly here rather than skipped along
    # with the flags. An experiment run against a mutated tree produces
    # plausible numbers and says nothing about it.
    harness.refuse_if_mutating()
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cost", action="store_true")
    parser.add_argument("--json", type=str, default=None)
    parser.add_argument("--arms", type=str, default=",".join(ARMS))
    args = parser.parse_args()

    if not (DATA / "gen_train23_test2to10").exists():
        raise SystemExit(f"no data in {DATA}. Run: python tools/fetch_clutrr.py")

    train_config = clutrr.ClutrrConfig(root=DATA, split="train")
    test_config = clutrr.ClutrrConfig(root=DATA, split="test")
    full_train = clutrr.load(train_config)
    test = clutrr.load(test_config)
    train = full_train if N_TRAIN is None else full_train[:N_TRAIN]

    if args.cost:
        cost_probe(full_train, test, train_config)
        return

    arms = tuple(a for a in args.arms.split(",") if a)
    print(f"g37-02  CLUTRR gen_train23_test2to10, layout kinship, "
          f"{len(clutrr.RELATIONS)} relations")
    print(f"        train {len(train)} of {len(full_train)} (hops 2-3), "
          f"test {len(test)} (hops 2-10)")
    print(f"        local d{WIDTH} x{EPOCHS} epochs; attention "
          f"d{ATTENTION_WIDTH} x{ATTENTION_EPOCHS}; seeds {SEEDS}\n")

    records = [one_cell(arm, seed, train, test, train_config)
               for arm in arms for seed in SEEDS]

    hops = sorted({int(h) for r in records for h in r["buckets"]})
    header = f"{'arm':<11}" + "".join(f"{h:>8}" for h in hops)
    print(header)
    print("-" * len(header))
    for arm in arms:
        rows = [r for r in records if r["arm"] == arm]
        line = f"{arm:<11}"
        for hop in hops:
            values = [r["buckets"][str(hop)]["accuracy"] for r in rows
                      if str(hop) in r["buckets"]]
            line += f"{sum(values) / len(values):>8.4f}" if values else f"{'-':>8}"
        print(line)
    floors = records[0]["buckets"]
    print(f"{'floor':<11}" + "".join(
        f"{floors[str(h)]['floor']:>8.4f}" for h in hops))
    print(f"{'n':<11}" + "".join(f"{floors[str(h)]['n']:>8}" for h in hops))

    if args.json:
        pathlib.Path(args.json).write_text(json.dumps(records, indent=2),
                                           encoding="utf-8")
        print(f"\nwrote {args.json}")


if __name__ == "__main__":
    main()
