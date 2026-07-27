"""If it is not width and not epochs, what IS stopping it?

[g10-02](sweeps/g10-02-underfit-or-undertrained.txt) ruled out two explanations
by measurement. The model peaks after ONE pass, so the epoch budget is not the
constraint; and a fourfold wider node gives the same curve to 0.005 bits, so the
node size is not the constraint either. It sits 2.18 bits short of a bigram on
text it has already seen.

"It underfits" is a description. This asks what it is underfitting *against*, by
building the baselines the architecture could actually reach and seeing which one
it matches.

## What this memory can possibly know

The store holds `sum over past of outer(value_i, key_{i-1})`, and a read is
`memory @ key(current)`. With derived keys drawn independently per token,
`<key_i, key_j>` is near zero for different tokens and large for the same one, so
a read returns approximately **the values that followed this character before,
summed and decayed** — within the current chunk, because the store resets between
chunks.

That is a bigram, with two specific handicaps:

1. **Per-chunk memory.** It knows only what happened in the last `chunk` tokens,
   not what happened in 210,000 of training text.
2. **Superposition.** Several past occurrences of `e` collapse into one vector.
   It gets an average successor, not a distribution over successors.

## The four baselines, in order of what they are allowed to know

    bigram (full)        counts over all training text        the published bar
    bigram (chunk)       counts restricted to the SAME chunk  handicap 1 only
    last-occurrence      the single most recent successor     handicaps 1 and 2
    unigram              base rates only                      the weak bar

**Where the model lands says which handicap is binding.** Matching
`bigram (chunk)` means the per-chunk reset is the whole story and superposition
costs nothing. Matching `last-occurrence` means superposition is costing the
distribution. Falling below both means the limit is the readout rather than
either.

No training and no model here: these are counts. The model's own number comes
from g10-02 and g10-01 and is quoted rather than recomputed, so this measures the
CEILING and not the model.
"""

from __future__ import annotations

import json
import sys
from collections import defaultdict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments.g10_01_first_language import NOTES  # noqa: E402
from experiments.harness import parse_args  # noqa: E402
from openplexus.ngram import (  # noqa: E402
    DEFAULT_K, NGram, absurd, bits_from_distributions, uniform_bits)
from openplexus.tasks.corpus import build, chunks, read  # noqa: E402

#: What the model reaches, PER CHUNK LENGTH, because the model's number is not a
#: constant and the baselines it is compared against move fast with chunk.
#:
#: The first version of this file carried one number, 5.83, measured at chunk 64,
#: and applied it at chunk 256 as well -- where the real value is 5.73 and the
#: verdict flips. A number with a caveat attached is still the wrong number when
#: it is used somewhere the caveat does not hold.
#:
#:   64  -- g10-02, widths 32 and 128, epoch-1 peak, cap off (safe at this chunk)
#:  256  -- g10-01's re-run, width 64 cap 1.0, the best completed cell
MODEL_BITS = {64: 5.830, 256: 5.734}


def within_chunk(pieces, vocab_size: int, k: float = DEFAULT_K,
                 newest_only: bool = False) -> float:
    """Bits per character using ONLY what is visible inside each chunk.

    The store resets between chunks, so a prediction at position `t` can rest on
    nothing before the start of this one. `newest_only` keeps just the most
    recent successor of the current character rather than counting them all,
    which is superposition's handicap in its harshest form.
    """
    distributions, targets = [], []
    for tokens in pieces:
        seen: dict[int, list[int]] = defaultdict(list)
        for t in range(len(tokens) - 1):
            here, following = int(tokens[t]), int(tokens[t + 1])
            history = seen[here][-1:] if newest_only else seen[here]
            counts = [k] * vocab_size
            for value in history:
                counts[value] += 1.0
            total = sum(counts)
            distributions.append([c / total for c in counts])
            targets.append(following)
            seen[here].append(following)
    return bits_from_distributions(distributions, targets)


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    chunk = int(args.scale) if args.scale is not None else 64

    if chunk not in MODEL_BITS:
        raise SystemExit(
            f"no measured model number at chunk {chunk}; have "
            f"{sorted(MODEL_BITS)}. Comparing baselines against a model number "
            f"taken at a DIFFERENT chunk is how the first version of this file "
            f"produced a verdict from mismatched values -- the baselines move "
            f"0.6 bits between chunk 64 and 256 and the model moves 0.1.")
    model_bits = MODEL_BITS[chunk]

    corpus = build(read(NOTES))
    test = chunks(corpus.test, chunk)
    vocab = corpus.vocab_size

    rows = {
        "uniform": uniform_bits(vocab),
        "unigram": NGram(vocab, 0).fit(corpus.train).bits_per_token(corpus.test),
        "bigram (full)": NGram(vocab, 1).fit(corpus.train).bits_per_token(
            corpus.test),
        "bigram (chunk)": within_chunk(test, vocab),
        "last-occurrence": within_chunk(test, vocab, newest_only=True),
    }
    for name, value in rows.items():
        broken = absurd(value, vocab)
        if broken:
            raise SystemExit(f"{name}: {broken}")

    print(f"chunk {chunk}, vocabulary {vocab}, {len(test)} test chunks\n")
    print(f"{'baseline':>18}{'bits/char':>12}   what it is allowed to know")
    notes = {
        "uniform": "nothing at all",
        "unigram": "base rates, from all training text",
        "bigram (full)": "transitions, from all training text",
        "bigram (chunk)": "transitions, from THIS CHUNK only",
        "last-occurrence": "the single most recent successor, this chunk",
    }
    for name, value in rows.items():
        print(f"{name:>18}{value:>12.3f}   {notes[name]}")
    print(f"{'THE MODEL':>18}{model_bits:>12.3f}   measured at THIS chunk, "
          f"not carried from another")

    print("\n== which handicap is binding? ==")
    chunked, newest = rows["bigram (chunk)"], rows["last-occurrence"]
    print(f"  the per-chunk reset costs "
          f"{chunked - rows['bigram (full)']:+.3f} bits")
    print(f"  superposition on top of it costs {newest - chunked:+.3f} bits")
    print(f"  the model is {model_bits - chunked:+.3f} bits against "
          f"within-chunk COUNTING, and {model_bits - newest:+.3f} against "
          f"keeping only the newest")
    if model_bits < chunked:
        print("  -> the model BEATS within-chunk counting. Its transition")
        print("     knowledge is per-chunk by construction, and given that it is")
        print("     doing better than counting, not worse. The gap to a full")
        print("     bigram is the PRICE OF THE RESET, not a failure to learn")
    elif model_bits > newest + 0.3:
        print("  -> the model is WORSE than a within-chunk model that keeps only")
        print("     the most recent successor. Neither handicap explains it, and")
        print("     the limit is the readout rather than what the store can hold")
    elif model_bits > chunked + 0.3:
        print("  -> SUPERPOSITION is the binding handicap: it is near the")
        print("     most-recent-successor baseline and far from counting")
    else:
        print("  -> the PER-CHUNK RESET is the whole story, and a store that")
        print("     persisted across chunks is the thing to build")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(
            [{"chunk": chunk, "baseline": n, "bits": v} for n, v in rows.items()],
            indent=1))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
