"""Shared plumbing so a sweep can run one seed per machine and be recombined.

A sweep is a grid of conditions measured at many seeds, and the seeds are
independent. Locally that is a serial loop; on CI it is one job per seed running
at once. This is the seam that lets the same script do both without the
experiment itself knowing which is happening.

    python experiments/g1_05_local.py                    # every seed, serial
    python experiments/g1_05_local.py --seed 3 --json out/3.json
    python experiments/harness.py --aggregate out/*.json

The aggregation deliberately reports **solved / stuck counts, not means**.
g1-03 established outcomes on this task are bimodal, so a mean describes a
mixture of two populations and no run that actually happened.
"""

from __future__ import annotations

import argparse
import glob
import json
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

#: A run at or above this counts as having solved the task; at or below STUCK,
#: as never having got near it. The band between is reported separately rather
#: than being split, because how often runs land in it is itself a finding.
SOLVED, STUCK = 0.9, 0.2


def oracle_mask(kinds) -> "np.ndarray":
    """Positions whose arriving binding is worth storing. **AN ORACLE.**

    Keeps a binding only where the PREVIOUS position was a pair, which is the
    task telling the model which of its own positions matter. A deployed system
    has no such signal, so every number measured through this is a **ceiling on
    what a real gate could achieve, not a result about one**.

    It is why the g7-02 rows are identical across sequence length: gating holds
    the number of stored bindings at twice the pair count whatever the length,
    and retrieval goes as sqrt(width / stored). Removing it is the whole subject
    of g8-01.

    Lives here rather than in each sweep because it had been copied verbatim
    into three of them, and a caveat this heavy should not have four homes.
    """
    import numpy as np
    return np.array([i > 0 and kinds[i - 1] == "pair" for i in range(len(kinds))])


def refuse_if_mutating() -> None:
    """Stop if tools/mutate.py currently has a file edited.

    The harness writes a sibling `.py.bak` before each edit and removes it
    after, so one lying around means either a run is in flight or a run was
    killed. Either way the source on disk is not the source anyone means to
    measure.

    This exists because a control was nearly run against a deliberately broken
    model. It would have produced numbers, they would have looked plausible, and
    **nothing in the output would have said otherwise** -- which is the failure
    mode this project's standards are written against.
    """
    leftovers = sorted(ROOT.glob("**/*.py.bak"))
    if leftovers:
        raise SystemExit(
            "REFUSING TO RUN: tools/mutate.py has the source edited.\n"
            + "\n".join(f"  {p.relative_to(ROOT)}" for p in leftovers)
            + "\n\nWait for the harness to finish, or if it was killed, run it "
              "again -- it restores any leftover .bak on startup before doing "
              "anything else."
        )


def parse_args(description: str) -> argparse.Namespace:
    """Parse the shared experiment arguments.

    Refuses to proceed while the mutation harness has the source edited, because
    every experiment in this project goes through here and that is the one place
    the check cannot be forgotten.
    """
    refuse_if_mutating()
    parser = argparse.ArgumentParser(description=description)
    parser.add_argument("--seed", type=int, default=None,
                        help="run this seed only; omit to run all of them")
    parser.add_argument("--width", type=int, default=None,
                        help="run this d_model only; omit to run all of them")
    parser.add_argument("--pairs", type=int, default=None,
                        help="run this n_pairs only; omit to run all of them")
    parser.add_argument("--decay", type=float, default=None,
                        help="run this memory decay only")
    parser.add_argument("--jitter", type=int, default=None,
                        help="delivery jitter in steps")
    parser.add_argument("--max-delay", type=int, default=4,
                        help="receiver buffer depth in steps")
    parser.add_argument("--seqlen", type=int, default=None,
                        help="sequence length, which sets how many bindings\n                             the memory superposes")
    parser.add_argument("--keys", type=int, default=None,
                        help="size of the key alphabet")
    parser.add_argument("--scale", type=float, default=None,
                        help="initialisation scale for the model under test")
    parser.add_argument("--mode", default=None,
                        help="which variant to run")
    parser.add_argument("--epochs", type=int, default=None,
                        help="training budget in epochs")
    parser.add_argument("--churn", type=float, default=None,
                        help="fraction of dimensions a departing machine takes")
    parser.add_argument("--drop", type=float, default=None,
                        help="fraction of events lost entirely")
    parser.add_argument("--lr", type=float, default=None,
                        help="learning rate; omitted means the script's own set")
    parser.add_argument("--partitions", type=int, default=None,
                        help="independent readout groups the width splits into")
    parser.add_argument("--sweep", default=None,
                        choices=("widths", "decay", "identity", "degrade", "drops"),
                        help="which sub-sweep to run when a script has more than one")
    parser.add_argument("--window", type=int, default=None,
                        help="how far back a gate may reach")
    parser.add_argument("--slots", type=int, default=None,
                        help="how many writes a tag may hold at once")
    parser.add_argument("--corpus", type=str, default=None,
                        help="which text to read: 'notes' for this project's "
                             "own docs/notes, or 'shakespeare' for the "
                             "standard char-level benchmark under data/")
    parser.add_argument("--cap", type=float, default=None,
                        help="largest norm the FAST store may reach; 0 leaves "
                             "it unbounded, which is the default in the model "
                             "and which lets a long sequence with dense "
                             "supervision drive the readout to 1e72 (g10-01)")
    parser.add_argument("--components", type=str, default=None,
                        help="name the model by what it is MADE OF, e.g. "
                             "'keys=sparse4,retrieval=cache128,"
                             "readout=hidden128'. The spec becomes the arm "
                             "label, so two different models can never share "
                             "one name -- which --mode allowed, and which "
                             "g11-06 had to work around with a duplicate arm")
    parser.add_argument("--chars", type=int, default=None,
                        help="training characters -- the DATA axis. g11-04 "
                             "capped this at 250k to fit its budget and that "
                             "cap is what made its backprop control flat: the "
                             "baseline was data-limited, not width-limited, so "
                             "the reference could not scale and the sweep "
                             "resolved nothing")
    parser.add_argument("--negatives", type=int, default=None,
                        help="how many negatives a contrastive update contrasts "
                             "against; omitted contrasts against EVERY symbol, "
                             "which is the original rule and is unaffordable "
                             "once the alphabet is 14,505 entities (g30-02)")
    parser.add_argument("--temperature", type=float, default=None,
                        help="softmax temperature for a contrastive update")
    parser.add_argument("--untrained", action="store_true",
                        help="THE GATE for any learned arm: run the identical "
                             "call with zero epochs, so a bug in the learner "
                             "cannot make the gate pass by accident")
    parser.add_argument("--fade", type=float, default=None,
                        help="per-step multiplier ageing a tag's marks toward "
                             "eviction; 1.0 never ages them")
    parser.add_argument("--workers", type=int, default=1,
                        help="processes to spread seeds across; 1 runs serially\n"
                             "                             and is the default")
    parser.add_argument("--json", type=Path, default=None,
                        help="write results here as JSON instead of a table")
    parser.add_argument("--aggregate", nargs="+", metavar="FILE",
                        help="combine JSON files from a matrix run into a table")
    return parser.parse_args()


def spread(function, items: list, workers: int) -> list:
    """Map `function` over `items`, in separate processes when asked.

    `workers <= 1` runs in this process and is the default everywhere, so a
    sweep behaves exactly as it did unless it is asked not to. **The results must
    not depend on the worker count**, which is what tests/test_spread.py checks:
    a sweep whose numbers move when it is parallelised is not faster, it is
    broken.

    `function` has to be importable by name, because the spawn start method
    pickles it -- fork would inherit the parent's memory instead, but it does not
    exist on Windows and a harness that only parallelises on one platform is a
    harness nobody trusts.

    **A worker that raises `SystemExit` HANGS THE POOL FOREVER**, and every
    guard in every experiment here raises exactly that. `SystemExit` inherits
    from `BaseException`, not `Exception`; `Pool` catches `Exception` in a
    worker and returns it as a result, while a `BaseException` kills the worker
    silently and `map` waits for a result that never comes.

    Measured: g11-07's baseline cell tripped a guard and sat for 23 minutes
    against an expected 2, and would have burned the full 300-minute timeout.
    **So every fail-fast guard in this project did the opposite in the
    configuration sweeps actually run in** -- `--workers 2` -- turning a
    one-second refusal into five hours of runner time and no diagnosis.

    Fixed here rather than in each guard, because the guards are right and there
    are a dozen of them.
    """
    if workers <= 1:
        return [function(item) for item in items]
    import multiprocessing as mp

    with mp.get_context("spawn").Pool(workers) as pool:
        return pool.map(_Guarded(function), items)


class _Guarded:
    """Turns a worker's `SystemExit` into an ordinary exception.

    A class rather than a closure because `spawn` pickles what it is given, and
    a closure is not picklable. The wrapped function still has to be importable
    by name, which is the existing requirement.
    """

    def __init__(self, function) -> None:
        self.function = function

    def __call__(self, item):
        try:
            return self.function(item)
        except SystemExit as refusal:
            raise RuntimeError(f"worker refused: {refusal}") from None


def bits(scores, targets, temperature: float) -> float:
    """Cross-entropy in bits per token, at this temperature.

    Shared rather than copied. **Uncalibrated bits are not comparable across
    arms** -- a mechanism that sums several retrievals changes the scale of the
    logits, so raw cross-entropy would measure logit magnitude rather than the
    quality of the distribution. Every caller must fit `temperature` on held-out
    text that was not trained on; g10-01 records what happens when it is fitted
    on text inside the training region (37 bits per character over an 86-symbol
    vocabulary).
    """
    import numpy as np
    scaled = scores / temperature
    scaled = scaled - scaled.max(axis=1, keepdims=True)
    weights = np.exp(scaled)
    probability = weights[np.arange(len(targets)), targets] / weights.sum(axis=1)
    return float(-np.log2(np.maximum(probability, 1e-12)).mean())


def emit(records: list[dict], path: Path | None) -> None:
    """Write results, or print them as a table."""
    if path is None:
        table(records)
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(records, indent=1), encoding="utf-8")
    print(f"wrote {len(records)} records to {path}")


def load(patterns: list[str]) -> list[dict]:
    records: list[dict] = []
    for pattern in patterns:
        for name in sorted(glob.glob(pattern)):
            records.extend(json.loads(Path(name).read_text(encoding="utf-8")))
    if not records:
        raise SystemExit(f"no records matched {patterns}")
    return records


def table(records: list[dict]) -> None:
    """Print solved/stuck counts per condition, and every individual accuracy.

    The per-run line is not decoration. An aggregate hides bimodality, and
    bimodality is the thing this project keeps needing to see — g1-03's headline
    only became visible when the individual seeds were printed.
    """
    by_condition: dict[str, list[dict]] = defaultdict(list)
    for record in records:
        by_condition[record["condition"]].append(record)

    header = (f"{'condition':<18}{'solved':>9}{'stuck':>9}{'between':>10}"
              f"{'worst':>8}{'best':>7}")
    print(header)
    print("-" * len(header))
    for condition, runs in by_condition.items():
        accs = sorted(r["accuracy"] for r in runs)
        n = len(accs)
        solved = sum(a >= SOLVED for a in accs)
        stuck = sum(a <= STUCK for a in accs)
        print(f"{condition:<18}{f'{solved}/{n}':>9}{f'{stuck}/{n}':>9}"
              f"{f'{n-solved-stuck}/{n}':>10}{accs[0]:>8.3f}{accs[-1]:>7.3f}")
        print(f"    {' '.join(f'{a:.2f}' for a in accs)}")


def mqar_batch(task, count: int, seed: int) -> list:
    """MQAR sequences prepared for `model.run`, as three scripts had them.

    Returns `(tokens, targets, scored, query_positions)` per sequence.

    **Extracted rather than copied a fourth time**, at the duplication checker's
    insistence and correctly: g10-01, g4-04 and g18-05 each held a byte-identical
    copy of this, which is exactly the shape the check exists to stop. Rule 12's
    version of the argument is the one that matters -- a fix applied to one copy
    and not the others leaves the survivors producing plausible numbers.

    `targets` is the NEXT token, which is only valid for an `autoregressive`
    task. With that flag off the answer lives in `sequence.targets` and is not
    the next token at all; g18-05 was built without it and both its arms scored
    below chance until the trivial floor caught it. See decision 138 for the same
    defect at text level.
    """
    import numpy as np
    from dataclasses import replace
    from openplexus.tasks.mqar import dataset

    built = []
    for sequence in dataset(replace(task, seed=seed), count):
        tokens = np.asarray(sequence.tokens)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        built.append((tokens, targets, scored, sequence.query_positions))
    return built

if __name__ == "__main__":
    args = parse_args(__doc__)
    if not args.aggregate:
        raise SystemExit("harness.py is only run directly to --aggregate")
    table(load(args.aggregate))


def kinship_out_degree(sequence, subject: int) -> int:
    """How many facts in `sequence` have `subject` on the left."""
    return sum(1 for s, _, _ in sequence.facts if s == subject)


def kinship_scored(model, data) -> dict:
    """Accuracy on the readout's answer, SPLIT ON OUT-DEGREE.

    The split is the point. Branching -- root-only in `search`, every step in
    `beam` -- exists for the case where the queried subject holds two or more
    relations, because `key(FACT, e)` then holds a SUM. An arm that gains only
    where the subject holds ONE relation is not resolving ambiguity, it got lucky
    in the endpoint score, and an overall column cannot tell those apart.

    Lives here rather than in each sweep because g13-03 and g21-01 both need it
    and `check_duplication` said so -- CLAUDE.md rule 19 arriving from the harness
    rather than from a review.
    """
    import numpy as np
    buckets: dict[str, list[int]] = {"1": [], "2+": []}
    hits = 0
    for sequence in data:
        predicted = model.run(np.array(sequence.tokens, dtype=np.int64))
        correct = int(predicted[sequence.answer_position]
                      == sequence.targets[sequence.answer_position])
        hits += correct
        degree = kinship_out_degree(sequence, sequence.asked[0])
        buckets["1" if degree <= 1 else "2+"].append(correct)
    return {
        "accuracy": hits / len(data),
        "by_out_degree": {
            k: {"n": len(v), "accuracy": (sum(v) / len(v)) if v else None}
            for k, v in buckets.items()},
    }


def kinship_cell(arm: str, seed: int, build, *, width: int, n_train: int,
                 n_test: int, epochs: int, hops: int = 2) -> dict:
    """Train one arm on kinship at one seed and score it.

    `build(arm, task, seed)` is the caller's, because the arm table is what a
    sweep is ABOUT and nothing here should know its keys. Everything else --
    the seed offsets, the train/test split, the condition string -- is shared, so
    two sweeps' numbers are comparable rather than accidentally alike.

    The test seed is the train seed plus 500,000: a fixed offset, so a sweep
    cannot silently evaluate on data it trained on.
    """
    import time
    from dataclasses import replace

    import numpy as np

    from openplexus.tasks.kinship import (IGNORE, KinshipConfig, dataset,
                                          shortcut_floors)

    task = KinshipConfig(hops=hops, seed=seed * 100_000)
    train = dataset(task, n_train)
    test = dataset(replace(task, seed=task.seed + 500_000), n_test)
    model = build(arm, task, seed)

    started = time.time()
    for _ in range(epochs):
        for sequence in train:
            tokens = np.array(sequence.tokens, dtype=np.int64)
            targets = np.array(sequence.targets, dtype=np.int64)
            model.run(tokens, targets, targets != IGNORE, learn=True)
    trained = time.time() - started

    result = kinship_scored(model, test)
    result.update(
        arm=arm, width=width, seed=seed, train_seconds=round(trained, 1),
        floors=shortcut_floors(task),
        condition=(f"{arm}|d{width}|seed{seed}|train{n_train}x{epochs}"
                   f"|test{n_test}"))
    return result


def kinship_sweep(description: str, arms, build, *, width: int, n_train: int,
                  n_test: int, epochs: int, seeds, cost_arm: str,
                  cost_why: str, hops: int = 2) -> None:
    """Run a kinship sweep: `--cost` to price it, `--seed` for one cell, `--json`.

    `cost_arm` names the MOST EXPENSIVE arm and `cost_why` says why it is the
    most expensive, because a cost probe on a cheap arm is worse than none -- it
    reports a number that will be exceeded and looks like it was checked.
    """
    import time

    import numpy as np

    from openplexus.tasks.kinship import IGNORE, KinshipConfig, dataset

    parser = argparse.ArgumentParser(description=description)
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--json", type=str, default=None)
    parser.add_argument("--cost", action="store_true")
    args = parser.parse_args()

    refuse_if_mutating()

    if args.cost:
        task = KinshipConfig(hops=hops, seed=0)
        sample = dataset(task, 20)
        model = build(cost_arm, task, 0)
        started = time.time()
        for sequence in sample:
            tokens = np.array(sequence.tokens, dtype=np.int64)
            targets = np.array(sequence.targets, dtype=np.int64)
            model.run(tokens, targets, targets != IGNORE, learn=True)
        per_sequence = (time.time() - started) / len(sample)
        train_cost = per_sequence * n_train * epochs
        print(f"most expensive arm: {cost_arm} at width {width}")
        print(f"  {cost_why}")
        print(f"  {per_sequence * 1000:.1f} ms per training sequence")
        print(f"  {train_cost / 60:.1f} min to train one cell "
              f"({n_train} x {epochs})")
        print(f"  {len(arms)} arms per job, worst job "
              f"~{train_cost * len(arms) / 60:.0f} min if every arm were this one")
        return

    chosen = (args.seed,) if args.seed is not None else tuple(seeds)
    records = [kinship_cell(arm, seed, build, width=width, n_train=n_train,
                            n_test=n_test, epochs=epochs, hops=hops)
               for seed in chosen for arm in arms]

    for record in records:
        by = record["by_out_degree"]
        print(f"{record['condition']}  overall {record['accuracy']:.3f}  "
              f"[floor first {record['floors']['first']:.3f}]  "
              + "  ".join(
                  f"k={k} n={d['n']} "
                  + ("--" if d["accuracy"] is None else f"{d['accuracy']:.3f}")
                  for k, d in by.items()))

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=2),
                                   encoding="utf-8")


def occasions_cell(config, statistic, k, *, look: int = 16) -> tuple[dict, float | None]:
    """Build one occasion stream, recover its classes, and score them.

    Shared by every `g33` script that asks a question about the WALK rather than
    about the join, because `tools/check_duplication.py` caught the second copy
    of it the moment it was written — which is the rule working: a fix applied to
    one copy and not the other keeps producing plausible numbers.

    Args:
        config: An `occasions.OccasionConfig`.
        statistic: A `grounding.Statistic`.
        k: Fixed bound, or `None` to derive it per surface from the ranking.
        look: Ceiling for the derived bound. Ignored when `k` is given.

    Returns:
        `(scored, bridged)` where `scored` is `grounding.score_classes` output
        and `bridged` is `reached_together` over the modality pairs that never
        shared an occasion — or `None` when the pairing leaves nothing to bridge,
        because scoring an empty pair set would report the ABSENCE of the
        question as a perfect answer.
    """
    from openplexus.grounding import (CoOccurrence, equivalence_classes,
                                      partner_rate, reached_together,
                                      score_classes)
    from openplexus.tasks.occasions import generate

    index = CoOccurrence()
    for occasion in generate(config):
        index.observe(occasion.surfaces)
    recovered = equivalence_classes(index, statistic, k, look)
    truth = config.classes()
    scored = score_classes(recovered, truth,
                           distractors=[config.concept_surfaces])
    # ADDED as a key rather than a second return value, so every existing caller
    # is untouched. `connected` is floor-free where `f1` is not: a concept
    # recovered alone scores 0.6667, 0.5000 or 0.3333 depending on how many
    # surfaces it has, so an f1 column read across a surface-count axis is
    # several scales printed as one -- which is what `g35-02` had to correct.
    scored["connected"] = partner_rate(
        recovered, truth,
        among=[s for s in range(config.concept_surfaces)])

    apart = config.apart()
    if not apart:
        return scored, None
    pairs = [(concept * config.surfaces + one, concept * config.surfaces + other)
             for concept in range(config.concepts) for one, other in apart]
    return scored, reached_together(recovered, pairs)
