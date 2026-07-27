"""Is the memory undertrained, or is it not a language model?

[g10-01](sweeps/g10-01-does-this-memory-beat-a-bigram.txt) registered `EPOCHS 2`
as the frozen axis most likely to be wrong, and registered the follow-up: *"if
the result is negative, re-run one cell at many more epochs BEFORE concluding
anything structural."* Its control reached 5.891 bits/char against a bigram's
3.711, so that follow-up is this.

**Two failures look identical in a single test number and have opposite fixes.**

    still improving at the last epoch      -> undertrained. Train longer; the
                                              g10-01 numbers are a BOUND
    flat, and TRAIN is also bad            -> underfitting. The model cannot
                                              represent the text at all, and
                                              more epochs buy nothing
    flat, TRAIN good and TEST bad          -> overfitting, which for a 210k
                                              character corpus and a linear
                                              readout would itself be a finding

So this reports **bits per character on the TRAINING text and the test text
after every epoch**. The training curve is the whole output; the final number is
the least interesting part of it.

## Why the training number is the decisive one

An n-gram fitted on the training text and scored on it is close to a lower bound
on what counting can do there. If this memory cannot match a bigram **on text it
has already seen**, no amount of further training or context is going to help,
and the honest conclusion is about the architecture rather than the budget.

That is a cheaper question than the one g10-01 asks, and it should arguably have
been asked first.

## Deliberately one small cell

Width 32, chunk 64, one seed. This is a diagnostic and not a sweep: it is here to
tell two explanations apart, and a grid would not tell them apart any better.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments.g10_01_first_language import (  # noqa: E402
    NOTES, TEMPERATURES, bits, scores_and_targets)
from experiments.harness import parse_args  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.ngram import NGram, uniform_bits  # noqa: E402
from openplexus.tasks.corpus import build, chunks, read  # noqa: E402

EPOCHS = 10
#: A sample of the training chunks, scored the same way as test. Sampled rather
#: than scored whole because the point is the CURVE, and a curve wants ten cheap
#: points rather than one expensive one.
TRAIN_SAMPLE = 60


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    seed = args.seed if args.seed is not None else 1
    width = args.width if args.width else 32
    chunk = int(args.scale) if args.scale is not None else 64
    epochs = args.epochs if args.epochs else EPOCHS
    cap = args.cap if args.cap is not None else 5.0

    corpus = build(read(NOTES))
    train = chunks(corpus.train, chunk)
    held = max(1, int(len(train) * 0.1))
    fitting, calibration = train[:-held], train[-held:]
    test = chunks(corpus.test, chunk)
    watched = fitting[:TRAIN_SAMPLE]

    bigram = NGram(corpus.vocab_size, 1).fit(corpus.train)
    print(f"vocabulary {corpus.vocab_size}, {len(fitting)} training chunks of "
          f"{chunk}, {len(test)} test chunks")
    print(f"uniform {uniform_bits(corpus.vocab_size):.3f}   "
          f"unigram {NGram(corpus.vocab_size, 0).fit(corpus.train).bits_per_token(corpus.test):.3f}   "
          f"bigram {bigram.bits_per_token(corpus.test):.3f}   "
          f"(bigram ON TRAIN {bigram.bits_per_token(corpus.train):.3f})")

    model = LocalAssociativeMemory(LocalMemoryConfig(
        # The fast store's cap. The first run of this diagnostic left it at the
        # model's default of OFF, which was safe at chunk 64 and is NOT safe at
        # any longer chunk -- g10-01's chunk-256 cells reached 1e72 with
        # accuracy below chance. Defaulting it on here means a re-run at a
        # longer chunk measures a model rather than a runaway.
        memory_cap=cap,
        vocab_size=corpus.vocab_size, d_model=width, lr=0.05,
        key_scale=0.5, decay=0.997, derived_keys=True, seed=seed))
    model.wo[:] = model.wv
    rng = np.random.default_rng(seed)
    order = np.arange(len(fitting))

    print(f"\n{'epoch':>6}{'train bits':>12}{'test bits':>12}"
          f"{'temperature':>13}{'test acc':>10}")
    records = []
    for epoch in range(epochs + 1):
        if epoch:
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
        train_scores, train_targets = scores_and_targets(model, watched)
        test_scores, test_targets = scores_and_targets(model, test)
        record = {
            "epoch": epoch, "seed": seed, "width": width, "chunk": chunk,
            "cap": cap,
            "temperature": temperature,
            "train_bits": bits(train_scores, train_targets, temperature),
            "test_bits": bits(test_scores, test_targets, temperature),
            "test_accuracy": float(
                (test_scores.argmax(axis=1) == test_targets).mean()),
        }
        records.append(record)
        print(f"{epoch:>6}{record['train_bits']:>12.3f}"
              f"{record['test_bits']:>12.3f}{temperature:>13.4f}"
              f"{record['test_accuracy']:>10.3f}", flush=True)

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=1))

    first, last = records[0], records[-1]
    gained = first["test_bits"] - last["test_bits"]
    recent = records[-3]["test_bits"] - last["test_bits"]
    print(f"\ntest bits fell {gained:.3f} overall, {recent:.3f} over the last "
          f"two epochs")
    if recent > 0.05:
        print("  -> STILL IMPROVING. g10-01's numbers are a bound and the")
        print("     epoch budget is the binding constraint, not the model")
    elif last["train_bits"] > bigram.bits_per_token(corpus.train):
        short = last["train_bits"] - bigram.bits_per_token(corpus.train)
        print(f"  -> UNDERFITTING AT WIDTH {width}. On text it has already seen"
              f" it is\n     {short:.3f} bits short of a bigram on that same "
              f"text, so the epoch\n     budget is not what is stopping it.")
        print("     This is ONE width. Whether the limit is the architecture or")
        print("     just this node size is g10-01's question, not this one's --")
        print("     a wider node fitting the training text would make the")
        print("     diagnosis width-limited rather than structural.")
    else:
        print("  -> it fits training text and not test text, which for a linear")
        print("     readout on 210k characters is itself a finding")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
