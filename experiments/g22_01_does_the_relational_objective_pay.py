"""Does training RELATIONALLY beat training SEQUENTIALLY, at a matched budget?

**This is GOALS section 1's own refutation condition, and it has never been run.**

    The bet, stated so it can fail: a system whose training objective is
    *relational* rather than *sequential* will reason rather than continue. If a
    model with a good concept map turns out to reason no better than a next-token
    model of the same size, the central premise is wrong.

Every experiment in this repository is *conditioned* on that being true, and none
of them can reach it. That is the structure `CLAUDE.md` rule 1 describes for a
borrowed claim -- filed under established, sitting upstream of everything, with no
downstream measurement able to contradict it -- and the project is standing in
that position with respect to its own premise.

## Why closure, and why the contrast is exact here

`closure.py` emits `FACT S O R` with the target at the OBJECT position, so the
relational target at a scored position **is the next token at that position**.

That makes the two arms nest exactly:

    relational   train only where a relation must be produced
    sequential   train at EVERY position, on the next token throughout

The sequential arm's training signal is a strict SUPERSET of the relational
arm's. Same model, same width, same epochs, same optimiser, same data, same
scoring. The only difference is the positions the loss is taken at.

So a difference cannot be explained by the model, the data, or the readout, which
is what makes this a test of the objective rather than of a configuration.

## What each outcome means, written before the run

    sequential >= relational   the extra signal does not hurt, and relational
                               focus buys nothing. The premise is weakened, and
                               weakened at the one place it is stated to fail
    sequential <  relational   predicting everything degrades the ability to
                               compose. The premise survives its own test

## The third arm is a DIAGNOSTIC, not a competitor

`seq_probe` trains sequentially, freezes everything except the output projection,
then fits that projection at relation positions only.

It separates *"the objective did not learn the relations"* from *"the objective
learned them and the readout does not emit them."* Those are different failures
and only the first refutes anything.

**It gets more compute than the other arms and is therefore not comparable to
them**, deliberately. Decision 118 is the calibration: an offline probe on frozen
features is evidence about what the features CONTAIN, not about what the system
reaches, and this project already published one such number as a headline once.

## What this is NOT, and the prior work it is not re-running

Decisions 95-98 measured all-position training on **our local model**, over
chains, and found it costs composition 1.000 -> 0.40 because the halt GATE is
conflicted -- fixed by giving the gate its own objective (`gate_objective`).

That is a different question. It is about a gate, on a different model and a
different task, and its cause is mechanical. **This runs on the attention
reference precisely BECAUSE it has no gate**, so the known confound cannot
contaminate the objective comparison. Nothing here revives or re-tests 095-098.

## Resolution, stated in advance

g14-01 measured the usable band on this task at **0.092** -- attention 0.282
against a majority floor of 0.190 -- with a standard error of 0.011 at 8 seeds.
That resolves a large objective effect and not a modest one.

So this runs at **32 seeds**, halving the error to about 0.006, and the minimum
difference worth calling a result is registered below rather than chosen after.
"""

from __future__ import annotations

import sys
import time
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402
from experiments.g14_01_does_closure_pass_g0 import (  # noqa: E402
    ATTENTION_EPOCHS, ATTENTION_WIDTH, N_TEST, N_TRAIN, run_majority, score)
from openplexus.models.attention import (  # noqa: E402
    Adam, AttentionConfig, ShiftedAttention)
from openplexus.tasks.closure import ClosureConfig, dataset  # noqa: E402
from openplexus.tasks.kinship import IGNORE  # noqa: E402

ARMS = ("majority", "relational", "sequential", "seq_probe")
SEEDS = tuple(range(32))

#: Epochs of output-projection-only fitting after sequential training. Extra
#: compute on top of ATTENTION_EPOCHS, which is why `seq_probe` is a diagnostic
#: rather than an arm anything is compared against.
PROBE_EPOCHS = 8

#: The smallest entailed difference between `relational` and `sequential` that
#: counts as a result, registered before dispatch. At 32 seeds the standard
#: error is about 0.006, so this is roughly 5x it -- and it is a third of the
#: task's whole usable band, which is the honest statement of what this
#: instrument can and cannot see.
MIN_DIFFERENCE = 0.030


def _model(task: ClosureConfig, seed: int) -> ShiftedAttention:
    """The same reference g14-01 used, at the same width and seed."""
    return ShiftedAttention(AttentionConfig(
        vocab_size=task.vocab_size, d_model=ATTENTION_WIDTH, seed=seed))


def _next_token_targets(tokens: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    """Targets and mask for predicting the next token at EVERY position.

    The final position has no successor, so it is masked out rather than given a
    padding target -- a padding token would be a class the model learns to emit
    and would show up as an accuracy change having nothing to do with the
    objective.
    """
    targets = np.empty_like(tokens)
    targets[:-1] = tokens[1:]
    targets[-1] = IGNORE
    return targets, targets != IGNORE


def _train(model, train, seed, *, sequential: bool) -> None:
    optimiser = Adam(model.params, lr=3e-3)
    rng = np.random.default_rng(seed)
    order = np.arange(len(train))
    for _ in range(ATTENTION_EPOCHS):
        rng.shuffle(order)
        for index in order:
            sequence = train[index]
            tokens = np.array(sequence.tokens, dtype=np.int64)
            if sequential:
                targets, mask = _next_token_targets(tokens)
            else:
                targets = np.array(sequence.targets, dtype=np.int64)
                mask = targets != IGNORE
            logits, cache = model.forward(tokens)
            _, grads = model.loss_and_backward(logits, cache, targets, mask)
            optimiser.step(grads)


def _fit_readout_only(model, train, seed) -> None:
    """Continue training `wo` alone, at relation positions.

    Adam updates `self.params[name]` in place, so an optimiser constructed over a
    one-key view of the model's parameter dict writes to the model's own array.
    Passing only `wo`'s gradient is what freezes the rest -- no gradient reaches
    the other parameters, rather than reaching them and being discarded.
    """
    optimiser = Adam({"wo": model.params["wo"]}, lr=3e-3)
    rng = np.random.default_rng(seed + 7919)
    order = np.arange(len(train))
    for _ in range(PROBE_EPOCHS):
        rng.shuffle(order)
        for index in order:
            sequence = train[index]
            tokens = np.array(sequence.tokens, dtype=np.int64)
            targets = np.array(sequence.targets, dtype=np.int64)
            logits, cache = model.forward(tokens)
            _, grads = model.loss_and_backward(
                logits, cache, targets, targets != IGNORE)
            optimiser.step({"wo": grads["wo"]})


def one_cell(arm: str, seed: int) -> dict:
    task = ClosureConfig(seed=seed * 100_000)
    train = dataset(task, N_TRAIN)
    test = dataset(replace(task, seed=task.seed + 500_000), N_TEST)

    started = time.time()
    if arm == "majority":
        predictions = run_majority(train, test, task)
    else:
        model = _model(task, seed)
        _train(model, train, seed, sequential=arm != "relational")
        if arm == "seq_probe":
            _fit_readout_only(model, train, seed)
        predictions = [model.predict(np.array(s.tokens, dtype=np.int64))
                       for s in test]
    elapsed = time.time() - started

    result = score(predictions, test)
    result.update(
        arm=arm, width=ATTENTION_WIDTH, seed=seed, seconds=round(elapsed, 1),
        condition=(f"{arm}|d{ATTENTION_WIDTH}|seed{seed}"
                   f"|train{N_TRAIN}x{ATTENTION_EPOCHS}|test{N_TEST}"
                   f"|probe{PROBE_EPOCHS if arm == 'seq_probe' else 0}"))
    return result


def cost_probe() -> None:
    """Time EVERY arm, because they differ and a job runs all of them.

    The first version timed only the dearest and multiplied by the arm count.
    That is an upper bound rather than a price -- it read 72 min for a job that
    the per-arm numbers below put near a fifth of that, which is the difference
    between dispatching this and re-scoping it.

    `CLAUDE.md`: a local single-seed timing converts to a 2-worker
    `ubuntu-latest` job at about **8x**, measured, not guessed at 3x.
    """
    total = 0.0
    for arm in ARMS:
        started = time.time()
        one_cell(arm, 0)
        elapsed = time.time() - started
        total += elapsed
        print(f"  {arm:<12} {elapsed / 60:>6.2f} min local "
              f"{elapsed * 8 / 60:>7.2f} min at 8x")
    print(f"\none seed, all {len(ARMS)} arms: {total / 60:.2f} min local, "
          f"{total * 8 / 60:.1f} min on a hosted runner")
    print(f"one seed per job x {len(SEEDS)} seeds: {len(SEEDS)} jobs at "
          f"~{total * 8 / 60:.0f} min each")


def main() -> None:
    # `--cost` is intercepted before the shared parser, which does not define it
    # and would reject it. Costing has to be reachable: CLAUDE.md requires a
    # sweep to be priced before dispatch, and a probe nobody can run is a price
    # nobody has.
    if "--cost" in sys.argv:
        harness.refuse_if_mutating()
        cost_probe()
        return
    args = harness.parse_args(__doc__)
    seeds = SEEDS if args.seed is None else (args.seed,)
    records = [one_cell(arm, seed) for seed in seeds for arm in ARMS]
    harness.emit(records, Path(args.json) if args.json else None)
    harness.table(records)


if __name__ == "__main__":
    main()
