"""Count-based baselines, and the metric that makes them comparable.

Everything this project has measured so far is **accuracy on a task with a known
trivial floor** — 0.34375 for MQAR, 0.125 for `reward_recall`. That works because
those generators say what guessing is worth. Real text does not: the floor is a
property of the data, and it is not uniform, and it is not one number.

So a corpus benchmark needs three reference points rather than one, and the gap
between them is most of the result:

    uniform     log(V)              knowing nothing, including the alphabet
    unigram     the base rate       knowing which characters are common
    bigram      the real baseline   knowing which character follows which

**Beating uniform is not evidence of anything.** A model that learns only that
`e` is common and `q` is rare beats uniform comfortably and has learned no
structure at all. [Note 013](../docs/archive/notes/013-the-base-rate.md) blamed exactly
this class of confusion for the salience gate's apparent success, and g8-02 could
not move the base rate to check it. Here the base rate is printable, so it gets
printed.

**Bigram is the bar BACKLOG names** and it is a fair one: an order-1 count model
is roughly what a single associative store *should* be able to do, since binding
the previous token to the current one is precisely a bigram in vector form. A
result at or below bigram says this memory is doing what counting does. Above it,
and something is being carried that a count cannot carry.

## Cross-entropy rather than accuracy

Accuracy on next-character prediction is dominated by spaces and `e`, and two
models can share an argmax while disagreeing completely about everything else.
Cross-entropy in **bits per character** reads the whole distribution, is the
standard quantity for this benchmark, and is directly comparable across
vocabularies in a way accuracy is not.

Perplexity is `2 ** bits` and is reported beside it because the literature uses
both; it carries no extra information.

## Smoothing is a choice and it is stated

An unsmoothed count model assigns probability zero to any pair it never saw,
which makes cross-entropy infinite on the first unseen pair in the test set —
not a large number, an undefined one. Add-k covers that, and `k` is a parameter
rather than a constant because the right value depends on how much text there
is, and a frozen one would be the seventh frozen constant in this project.
"""

from __future__ import annotations

import math
from collections import defaultdict
from typing import Iterable, Sequence

#: Enough smoothing to make the arithmetic defined, small enough not to be the
#: model. A default, not a finding -- see the module docstring.
DEFAULT_K = 0.1


class NGram:
    """An order-`n` count model over token ids, with add-k smoothing.

    Order 0 is the unigram base rate and order 1 is the bigram baseline. Higher
    orders are allowed because refusing them would make "is the memory doing
    what counting does" answerable only against the weakest counter.
    """

    def __init__(self, vocab_size: int, order: int = 1,
                 k: float = DEFAULT_K) -> None:
        if vocab_size < 1:
            raise ValueError(f"vocab_size must be positive, got {vocab_size}")
        if order < 0:
            raise ValueError(f"order must not be negative, got {order}")
        if k <= 0.0:
            # Zero is not "no smoothing" here, it is "undefined on unseen
            # context", and a benchmark that silently returns inf is worse than
            # one that refuses.
            raise ValueError(
                f"k must be positive; k=0 leaves cross-entropy undefined on "
                f"any context the training text did not contain, got {k}")
        self.vocab_size = vocab_size
        self.order = order
        self.k = k
        self._counts: dict[tuple, list[float]] = defaultdict(
            lambda: [0.0] * vocab_size)
        self._totals: dict[tuple, float] = defaultdict(float)

    def _context(self, tokens: Sequence[int], position: int) -> tuple:
        """The `order` tokens before `position`, short at the start.

        A short context is its own context rather than being padded to a fixed
        one. Padding would merge every sequence start with whatever token was
        chosen as padding, which is a small systematic lie about the data.
        """
        return tuple(tokens[max(0, position - self.order):position])

    def fit(self, streams: Iterable[Sequence[int]]) -> "NGram":
        for tokens in streams:
            for position, token in enumerate(tokens):
                if not 0 <= token < self.vocab_size:
                    raise ValueError(
                        f"token {token} outside vocab of {self.vocab_size}")
                context = self._context(tokens, position)
                self._counts[context][token] += 1.0
                self._totals[context] += 1.0
        return self

    def probability(self, context: tuple, token: int) -> float:
        counts = self._counts.get(context)
        total = self._totals.get(context, 0.0)
        seen = counts[token] if counts is not None else 0.0
        return (seen + self.k) / (total + self.k * self.vocab_size)

    def distribution(self, context: tuple) -> list[float]:
        return [self.probability(context, t) for t in range(self.vocab_size)]

    def bits_per_token(self, streams: Iterable[Sequence[int]]) -> float:
        """Mean bits to encode each token, given what came before it."""
        total, count = 0.0, 0
        for tokens in streams:
            for position, token in enumerate(tokens):
                probability = self.probability(
                    self._context(tokens, position), token)
                total -= math.log2(probability)
                count += 1
        if not count:
            raise ValueError("no tokens to score")
        return total / count


def uniform_bits(vocab_size: int) -> float:
    """What knowing nothing costs. The only floor that needs no data."""
    if vocab_size < 1:
        raise ValueError(f"vocab_size must be positive, got {vocab_size}")
    return math.log2(vocab_size)


def bits_from_distributions(distributions: Iterable[Sequence[float]],
                            tokens: Sequence[int]) -> float:
    """Bits per token for a model that emits its own distributions.

    Kept separate from `NGram.bits_per_token` so the memory model is scored by
    exactly the same arithmetic as the baselines rather than by a second
    implementation of it -- which is the difference that made three summarisers
    disagree about the same number before `tools/recovery.py` existed.

    The distributions must be normalised; this checks rather than assumes,
    because an unnormalised one produces a plausible smaller number and would
    read as a better model.
    """
    total, count = 0.0, 0
    for distribution, token in zip(distributions, tokens):
        mass = math.fsum(distribution)
        if not 0.999 <= mass <= 1.001:
            raise ValueError(
                f"distribution at position {count} sums to {mass}, not 1; an "
                f"unnormalised distribution yields a smaller and meaningless "
                f"cross-entropy")
        probability = distribution[token]
        if probability <= 0.0:
            raise ValueError(
                f"probability 0 for the token that occurred at position "
                f"{count}; cross-entropy is undefined, not large")
        total -= math.log2(probability)
        count += 1
    if not count:
        raise ValueError("no tokens to score")
    return total / count


def absurd(value: float, vocab_size: int, slack: float = 1.0) -> str | None:
    """Why this cannot be a model's cross-entropy, or None if it can.

    **A value far above `uniform_bits` is not a bad model, it is a broken
    number.** Uniform is what assigning equal probability to everything costs,
    so beating it requires no knowledge at all. Losing to it by a wide margin
    requires being confidently, *specifically* wrong — putting almost all the
    mass on characters that did not arrive — which a runaway readout or a
    mis-fitted temperature can manufacture and a model cannot reach by being
    poor at its job.

    This exists because g10-01 reported **39.5 bits per character over an
    86-symbol vocabulary, and NaN**, and both were read off a results table as
    though they were measurements of a language model. They were a readout that
    had reached 1e72 with next-character accuracy of 0.005, below the 1/86
    chance rate.

    `slack` is the margin allowed above uniform before refusing. It is a
    parameter because a genuinely miscalibrated model can sit slightly above
    uniform and still be worth reporting; nothing can sit 40 bits above it.
    """
    ceiling = uniform_bits(vocab_size)
    if value != value or value in (float("inf"), float("-inf")):
        return f"not finite ({value})"
    if value > ceiling + slack:
        return (f"{value:.3f} bits is more than {slack} above uniform "
                f"({ceiling:.3f}); a model cannot be this wrong by accident, "
                f"so this is the calibration or the arithmetic, not the model")
    return None


def perplexity(bits: float) -> float:
    """`2 ** bits`. Reported because the literature uses it; adds nothing."""
    return 2.0 ** bits
