"""Does this memory beat a bigram on real text?

The first time anything in this project sees language, and the first evidence
either way for goal 2 — a replacement for a language model that does not need a
data centre.

The corpus is this project's own notes, 210,216 training characters over 86
symbols. **It is not a standard benchmark** and a number from it is not
comparable to published ones; it is real English with real Zipfian statistics
that needs no download, and it answers the question that blocks everything else.
`openplexus/tasks/corpus.py` says what the split is and why.

## The bar, measured before this was built

    uniform   6.426 bits/char     knowing nothing, including the alphabet
    unigram   4.756 bits/char     knowing which characters are common
    bigram    3.711 bits/char     knowing which character follows which
    trigram   2.934 bits/char

**Beating uniform is not evidence of anything.** Beating unigram says only that
the base rate was learned. **Bigram is the bar**, and it is the fair one for this
model specifically: binding the previous token to the current one IS a bigram in
vector form, so at or below it the memory is doing what counting does, and above
it something is being carried that a count cannot carry.

## Calibration, which is a real problem and is handled openly

`Wo` is trained by the delta rule against one-hot targets. That makes it a
discriminative readout, not a probability model: its scores have no reason to be
on the scale a softmax expects, and cross-entropy is exquisitely sensitive to
that scale. Comparing raw softmax scores against an n-gram would understate this
model for a reason that has nothing to do with what it learned.

So a single scalar **temperature** is fitted, and:

- it is fitted on **held-out TRAINING chunks**, never on test
- it is **one parameter**, which cannot encode anything about the text
- both numbers are reported, raw and calibrated, because the gap between them
  is itself informative — a large one means the readout is badly scaled rather
  than badly informed

`surprise()` in the model is already the negative log probability of the arriving
token under a softmax of the scores, in nats, so the uncalibrated number needs no
new machinery at all: it is the trace's own quantity divided by ln 2.

## Accuracy is reported too, and it is the scale-free check

Next-character accuracy does not depend on the temperature. If accuracy is
respectable while bits are terrible, the readout is miscalibrated; if both are
bad, it did not learn. Reporting only one of them cannot tell those apart.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.ngram import NGram, uniform_bits  # noqa: E402
from openplexus.tasks.corpus import build, chunks, read  # noqa: E402

NOTES = Path(__file__).resolve().parent.parent / "docs" / "notes"
#: Held back from TRAINING to fit the temperature. Never test.
CALIBRATION_SHARE = 0.1
#: Spanning three orders of magnitude, because the first control pinned at the
#: BOTTOM of a 0.25-to-8 grid: a delta-rule readout's scores are far flatter than
#: a softmax expects, so the useful temperatures are small. A pinned calibration
#: makes the reported bits a bound rather than a value, which is the same grid
#: rule every sweep here follows.
TEMPERATURES = tuple(round(0.01 * 1.3 ** i, 4) for i in range(0, 30))
SEEDS = (1, 2, 3)
EPOCHS = 2


def scores_and_targets(model, pieces) -> tuple[np.ndarray, np.ndarray]:
    """Every prediction the model made, and the character that actually came.

    Position 0 of each chunk is skipped: with no previous token there is no
    retrieval and so no prediction, and scoring it would be scoring the
    initialisation.
    """
    rows, wanted = [], []
    for tokens in pieces:
        trace: list[dict] = []
        model.run(tokens, trace=trace)
        for entry in trace:
            index = entry["t"]
            if index == 0:
                continue
            rows.append(entry["scores"])
            wanted.append(int(tokens[index]))
    return np.asarray(rows), np.asarray(wanted)


def bits(scores: np.ndarray, targets: np.ndarray, temperature: float) -> float:
    """Cross-entropy in bits per character, at this temperature."""
    scaled = scores / temperature
    scaled = scaled - scaled.max(axis=1, keepdims=True)
    weights = np.exp(scaled)
    probability = weights[np.arange(len(targets)), targets] / weights.sum(axis=1)
    return float(-np.log2(np.maximum(probability, 1e-12)).mean())


def run_one(args) -> list[dict]:
    seed, width, chunk = args
    corpus = build(read(NOTES))
    train = chunks(corpus.train, chunk)
    held = max(1, int(len(train) * CALIBRATION_SHARE))
    fitting, calibration = train[:-held], train[-held:]
    test = chunks(corpus.test, chunk)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=corpus.vocab_size, d_model=width, lr=0.05,
        key_scale=0.5, decay=0.997, derived_keys=True, seed=seed))
    model.wo[:] = model.wv

    rng = np.random.default_rng(seed)
    order = np.arange(len(fitting))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens = fitting[index]
            # Predict the next character at every position, which is what a
            # language model is asked to do -- no position is privileged.
            targets = np.concatenate([tokens[1:], tokens[-1:]])
            # A mask over positions, not a list of them. The last position has
            # no next character, so its target is a repeat and it is excluded.
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, targets, scored, learn=True)

    # ONE scalar, fitted on held-out TRAINING chunks. Never on test.
    fit_scores, fit_targets = scores_and_targets(model, calibration)
    temperature = min(TEMPERATURES,
                      key=lambda t: bits(fit_scores, fit_targets, t))

    test_scores, test_targets = scores_and_targets(model, test)
    predicted = test_scores.argmax(axis=1)
    return [{
        "seed": seed, "width": width, "chunk": chunk,
        "vocab_size": corpus.vocab_size,
        "temperature": temperature,
        "bits_raw": bits(test_scores, test_targets, 1.0),
        "bits_calibrated": bits(test_scores, test_targets, temperature),
        "accuracy": float((predicted == test_targets).mean()),
        "uniform": uniform_bits(corpus.vocab_size),
        "unigram": NGram(corpus.vocab_size, 0).fit(
            corpus.train).bits_per_token(corpus.test),
        "bigram": NGram(corpus.vocab_size, 1).fit(
            corpus.train).bits_per_token(corpus.test),
        "trigram": NGram(corpus.vocab_size, 2).fit(
            corpus.train).bits_per_token(corpus.test),
        "test_characters": len(test_targets),
    }]


def control() -> int:
    """Shape only: one seed, one width, a short chunk, reduced training."""
    corpus = build(read(NOTES))
    print(f"vocabulary {corpus.vocab_size}; {corpus.train_tokens} train "
          f"characters, {corpus.test_tokens} test")
    print(f"uniform {uniform_bits(corpus.vocab_size):.3f}   "
          f"bigram {NGram(corpus.vocab_size, 1).fit(corpus.train).bits_per_token(corpus.test):.3f}")
    record = run_one((1, 32, 64))[0]
    print(f"model raw {record['bits_raw']:.3f}, calibrated "
          f"{record['bits_calibrated']:.3f} at temperature "
          f"{record['temperature']}, accuracy {record['accuracy']:.3f}")
    return 0


def main() -> int:
    args = harness.parse_args(__doc__.splitlines()[0])
    if args.sweep == "degrade":
        return control()
    seeds = [args.seed] if args.seed is not None else list(SEEDS)
    width = args.width if args.width else 64
    chunk = int(args.scale) if args.scale is not None else 256

    jobs = [(seed, width, chunk) for seed in seeds]
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
