"""Does this model's loss fall with width the way a backprop model's does?

**The single most decision-relevant measurement available to the project**, and
the one that could say goal 1 is unreachable.

Filipovich et al. (arXiv:2210.14593) fit compute-optimal scaling laws for Direct
Feedback Alignment against backpropagation and found the local rule does not
merely lose a constant — it loses the EXPONENT. Backprop −0.071, DFA −0.040, a
shallow network −0.019. A rule that fails to propagate credit deeply behaves
like a shallower model, **the gap widens with scale, and it is invisible at
small scale.** Every number this project has is at small scale.

So: fit `bits = a · width^b + c` for our model and for a backprop-trained
attention baseline on the same corpus, same split, same widths, and compare `b`.

## What a flat exponent would and would not mean

**It would not immediately condemn local learning**, and confusing those two
readings is the trap this experiment exists to avoid.

The delta rule on `Wo` **is the exact gradient** for a single linear readout. We
are not approximating backprop; we have nothing to backpropagate *through* — the
store is activity, not parameters. So a flat exponent here is a statement about
the ARCHITECTURE, not about the learning rule, and note 035 already predicts one
for the reason that the store holds a bigram count table whose effective rank is
about 3 whatever the width.

The measurement that would bear on the learning rule is the CONTEXT-KEY arm,
whose ceiling is a trigram. If the exponent stays flat when the ceiling moves,
the problem is not the ceiling.

## PREDICTIONS (registered before running)

  P1  the single-token arm is FLAT — |b| < 0.02 — because note 035 measured the
      store's effective rank at ~3 independent of width
  P2  the attention baseline has a clearly negative exponent on the same widths
  P3  the context-key arm is steeper than the single-token arm, because its
      ceiling is a trigram and there is something for width to buy
  P4  NEITHER of our arms reaches the attention baseline's exponent

P1 and P2 together are the control: if the baseline is also flat, the width
range is too narrow to fit anything and the experiment says nothing.

COST: 4 widths x 3 arms x 2 seeds. The attention arm is the expensive one.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402
from experiments.g10_01_first_language import (  # noqa: E402
    bits, corpus_named, scores_and_targets)
from openplexus.models.attention import (  # noqa: E402
    Adam, AttentionConfig, ShiftedAttention)
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.ngram import absurd, uniform_bits  # noqa: E402
from openplexus.tasks.corpus import chunks  # noqa: E402

TEMPERATURES = tuple(round(0.01 * 1.3 ** i, 4) for i in range(0, 30))
EPOCHS = 2


def split(corpus, chunk: int):
    """Fitting text, calibration text, test text — the same rule as g10-01."""
    stream = corpus.train[0]
    cut = int(len(stream) * 0.8)
    return (chunks((stream[:cut],), chunk), chunks((stream[cut:],), chunk),
            chunks(corpus.test, chunk))


def ours(corpus, width: int, chunk: int, seed: int, context: bool) -> float:
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=corpus.vocab_size, d_model=width, lr=0.05, key_scale=0.5,
        decay=0.997, derived_keys=True, context_keys=context, memory_cap=5.0,
        seed=seed))
    model.wo[:] = model.wv
    fitting, calibration, test = split(corpus, chunk)
    rng = np.random.default_rng(seed)
    order = np.arange(len(fitting))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens = fitting[index]
            targets = np.concatenate([tokens[1:], tokens[-1:]])
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, targets, scored, learn=True)
    fit_scores, fit_targets = scores_and_targets(model, calibration)
    temperature = min(TEMPERATURES,
                      key=lambda t: bits(fit_scores, fit_targets, t))
    test_scores, test_targets = scores_and_targets(model, test)
    return bits(test_scores, test_targets, temperature)


def backprop(corpus, width: int, chunk: int, seed: int) -> float:
    """The reference. Softmax attention, trained by actual backpropagation.

    Deliberately given every advantage our model does not have: a real
    optimiser, a softmax over positions, and gradients that reach every
    parameter. If the exponent gap is real it should show here.
    """
    model = ShiftedAttention(AttentionConfig(
        vocab_size=corpus.vocab_size, d_model=width, seed=seed))
    optimiser = Adam(model.params, lr=3e-3)
    fitting, _, test = split(corpus, chunk)
    rng = np.random.default_rng(seed)
    order = np.arange(len(fitting))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens = fitting[index]
            targets = np.concatenate([tokens[1:], tokens[-1:]])
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            logits, cache = model.forward(tokens)
            _, grads = model.loss_and_backward(logits, cache, targets, scored)
            optimiser.step(grads)
    rows, wanted = [], []
    for tokens in test:
        logits, _ = model.forward(tokens)
        targets = np.concatenate([tokens[1:], tokens[-1:]])
        for position in range(1, len(tokens) - 1):
            rows.append(logits[position])
            wanted.append(int(targets[position]))
    # Logits from a softmax model are already on the right scale, so no
    # temperature is fitted -- fitting one would flatter the baseline.
    return bits(np.asarray(rows), np.asarray(wanted), 1.0)


def run_one(args) -> list[dict]:
    seed, width, chunk, which, arm = args
    corpus = corpus_named(which)
    if arm == "backprop":
        value = backprop(corpus, width, chunk, seed)
    else:
        value = ours(corpus, width, chunk, seed, arm == "context")
    broken = absurd(value, corpus.vocab_size)
    if broken:
        raise SystemExit(f"{arm} width {width} seed {seed}: {broken}")
    return [{"seed": seed, "width": width, "chunk": chunk, "arm": arm,
             "bits_calibrated": value, "vocab_size": corpus.vocab_size,
             "uniform": uniform_bits(corpus.vocab_size),
             "corpus": which or "notes"}]


def main() -> int:
    args = harness.parse_args(__doc__.splitlines()[0])
    seeds = [args.seed] if args.seed is not None else [1, 2]
    width = args.width if args.width else 64
    chunk = int(args.scale) if args.scale is not None else 256
    arm = args.mode or "single"
    jobs = [(seed, width, chunk, args.corpus, arm) for seed in seeds]
    records = [r for batch in harness.spread(run_one, jobs, args.workers)
               for r in batch]
    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(records, indent=1))
        return 0
    for record in records:
        print(record)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
