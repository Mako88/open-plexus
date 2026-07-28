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

from experiments import components, harness  # noqa: E402
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

#: Training characters, capped well below the corpus.
#:
#: g11-03 lost four of six cells to a 240-minute timeout because the store is
#: `d x d` and the per-step work is a matvec, so cost goes as **d squared** --
#: width 256 is sixteen times width 64, not four. The estimate had been taken
#: from the cheapest cell that had been run locally, which is exactly the wrong
#: one.
#:
#: Capping the stream looked like the right fix rather than a compromise: this
#: experiment fits loss against WIDTH at fixed data, which is a standard
#: model-size scaling curve, and holding the data fixed is what makes the
#: exponent comparable across arms. What it forfeits is any claim about
#: data scaling, which this sweep was never going to make.
#:
#: **That reasoning was wrong, and the sweep it produced answered nothing.**
#: The backprop control reached 4.20 bits by width 16 and did not improve —
#: fitted exponent -0.0021, R2 0.13. It was DATA-limited, not width-limited, so
#: the reference had no trend to compare against and the whole matrix was spent
#: on an unresolvable comparison. This is the default, not the value: `--chars`
#: is the axis g11-05 moves, and 250_000 is kept here so g11-04 reproduces.
TRAIN_CHARS = 250_000


def split(corpus, chunk: int, chars: int = TRAIN_CHARS):
    """Fitting text, calibration text, test text — the same rule as g10-01."""
    stream = corpus.train[0][:chars]
    cut = int(len(stream) * 0.8)
    return (chunks((stream[:cut],), chunk), chunks((stream[cut:],), chunk),
            chunks(corpus.test, chunk))


#: `matched` is the SAME MODEL as `single`, under a different name.
#:
#: It exists because the state-matched control runs at a wider width, and an arm
#: is identified by its name: two `single` rows at widths 64 and 143 would be
#: averaged into one column, and `width` would then vary WITHIN an arm, which is
#: the confound `summarise_scaling_exponent.axis_of` refuses. A separate name
#: keeps the control a control instead of contaminating the thing it controls.
ARMS = frozenset({"single", "context", "cache", "matched", "backprop"})


def numbers_held(width: int, slots: int) -> int:
    """State the model carries: the `d x d` store, plus the cache's pairs.

    **The cache is not free state**, and comparing "with cache" against
    "without" at equal width compares a bigger model to a smaller one -- the
    mistake g10-08 made with width and g10-09 made with a cache, and the latter
    had to be retracted for it. Every arm reports this so a comparison can be
    read against equal state rather than equal width.
    """
    return width * width + 2 * slots * width


def ours(corpus, width: int, chunk: int, seed: int, context: bool,
         chars: int = TRAIN_CHARS, slots: int = 0, **extra) -> float:
    # Every default this script sets goes in the dict, so a component spec can
    # OVERRIDE one rather than collide with it. `decay` was passed as a keyword
    # AND carried by the `consolidating` choice -- which needs decay < 1 -- and
    # LocalMemoryConfig got it twice. Four of twelve cells died; the eight that
    # did not override a default were fine.
    settings = dict(derived_keys=True, context_keys=context,
                    cache_slots=slots, decay=0.997, memory_cap=5.0)
    settings.update(extra)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=corpus.vocab_size, d_model=width, lr=0.05, key_scale=0.5,
        seed=seed, **settings))
    # Start the readout AT the value projection, so a retrieval that lands on
    # `wv[token]` already scores that token. Meaningless with a hidden layer --
    # `wo` then reads hidden units, not retrieval dimensions, and the shapes do
    # not even match. Left at zero in that case, which is what the offline
    # probes used.
    if not settings.get("hidden"):
        model.wo[:] = model.wv
    fitting, calibration, test = split(corpus, chunk, chars)
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


def backprop(corpus, width: int, chunk: int, seed: int,
             chars: int = TRAIN_CHARS) -> float:
    """The reference. Softmax attention, trained by actual backpropagation.

    Deliberately given every advantage our model does not have: a real
    optimiser, a softmax over positions, and gradients that reach every
    parameter. If the exponent gap is real it should show here.
    """
    model = ShiftedAttention(AttentionConfig(
        vocab_size=corpus.vocab_size, d_model=width, seed=seed))
    optimiser = Adam(model.params, lr=3e-3)
    fitting, _, test = split(corpus, chunk, chars)
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
    seed, width, chunk, which, arm, chars, slots, extra = args
    corpus = corpus_named(which)
    available = len(corpus.train[0])
    if chars > available:
        raise SystemExit(
            f"asked for {chars} training characters and the corpus holds "
            f"{available}. A cell silently truncated to the corpus length is a "
            f"cell at a different point on the axis than the grid says.")
    if extra is not None:
        # A spec names its own model, so the arm-name checks below -- which
        # exist to stop `--mode` being misused -- do not apply to it.
        value = ours(corpus, width, chunk, seed, False, chars, 0, **extra)
        broken = absurd(value, corpus.vocab_size)
        if broken:
            raise SystemExit(f"{arm} chars {chars} seed {seed}: {broken}")
        return [{"seed": seed, "width": width, "chunk": chunk, "arm": arm,
                 "chars": chars, "slots": extra.get("cache_slots", 0),
                 "bits_calibrated": value, "vocab_size": corpus.vocab_size,
                 "uniform": uniform_bits(corpus.vocab_size),
                 "corpus": which or "notes",
                 "condition": f"{arm}-w{width}-c{chars}-s{seed}"}]
    if arm not in ARMS:
        raise SystemExit(
            f"unknown arm {arm!r}; expected one of {', '.join(sorted(ARMS))}. "
            f"An unrecognised mode would otherwise fall through to the "
            f"single-token model and be recorded under its own name.")
    # THE ARM NAME AND THE CACHE HAVE TO AGREE, both ways.
    #
    # A `cache` arm with no slots is a cache arm that never had a cache. A
    # `single` arm WITH slots records itself as `single` and lands in the same
    # column as the no-cache arm, so the summariser averages two different
    # models into one number. Both run cleanly and neither announces itself.
    if (arm == "cache") != bool(slots):
        raise SystemExit(
            f"arm {arm!r} with slots {slots}: the arm name and the cache must "
            f"agree. `cache` needs --slots, and any other arm must not have "
            f"them, or two different models are recorded under one name.")
    if arm == "backprop":
        value = backprop(corpus, width, chunk, seed, chars)
    else:
        value = ours(corpus, width, chunk, seed, arm == "context", chars, slots)
    broken = absurd(value, corpus.vocab_size)
    if broken:
        raise SystemExit(f"{arm} width {width} chars {chars} seed {seed}: {broken}")
    # `condition` is written from what actually ran, so a summariser can assert
    # a run's identity from the DATA rather than from the directory it came out
    # of -- CLAUDE.md rule 11b, bought by g9-11's near-miss.
    return [{"seed": seed, "width": width, "chunk": chunk, "arm": arm,
             "chars": chars, "slots": slots, "bits_calibrated": value,
             "numbers_held": numbers_held(width, slots),
             "vocab_size": corpus.vocab_size,
             "uniform": uniform_bits(corpus.vocab_size),
             "corpus": which or "notes",
             "condition": f"{arm}-w{width}-k{slots}-c{chars}-s{seed}"}]


def main() -> int:
    args = harness.parse_args(__doc__.splitlines()[0])
    seeds = [args.seed] if args.seed is not None else [1, 2]
    width = args.width if args.width else 64
    chunk = int(args.scale) if args.scale is not None else 256
    arm = args.mode or "single"
    # A COMPONENT SPEC OVERRIDES THE ARM NAME, because the spec IS the name.
    # `--mode` conflated which arm a row belongs to with what the model is made
    # of, which is why g11-06 needed a duplicate arm to keep a control out of
    # the column it was controlling.
    # None means "no spec given". An EMPTY DICT means "a spec was given and it
    # happens to need no overrides" -- which is exactly the baseline,
    # keys=dense,retrieval=plain,readout=linear. Branching on truthiness sent
    # that one cell down the arm-name path and killed it, while all seventeen
    # cells that differ from the baseline ran fine. The identity check was the
    # only casualty, which is the cell whose failure says least and matters most.
    extra: dict | None = None
    if args.components:
        extra, arm = components.parse(args.components)
    chars = args.chars if args.chars else TRAIN_CHARS
    slots = args.slots or 0
    jobs = [(seed, width, chunk, args.corpus, arm, chars, slots, extra)
            for seed in seeds]
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
