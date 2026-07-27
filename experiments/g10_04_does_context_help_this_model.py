"""Can this model USE a longer memory, or only be given one?

[g10-03](sweeps/g10-03-what-is-the-ceiling.txt) measured the per-chunk reset
costing 2.277 bits at chunk 64 and 1.637 at chunk 256, and recommended building a
store that persists across chunks on the strength of it.

**That gap is what a COUNTER would gain, not what this model would.** In the same
two columns, counting improved 0.639 bits from chunk 64 to 256 and the model
improved 0.096. A mechanism that captures a seventh of the available gain at one
doubling may capture very little of the rest, and a recommendation resting on the
size of a gap rather than on the ability to close it is a recommendation to build
the wrong thing.

Chunk length IS the persistence horizon: the store accumulates within a chunk and
resets between them, so a longer chunk is precisely "more persistence" without
any new mechanism. That makes this cheap to settle.

## What the answer decides

    model tracks counting as chunk grows  -> persistence IS the fix, and
                                             g10-03's recommendation stands
    model flattens while counting climbs  -> persistence is NOT the fix for this
                                             mechanism. Superposition is the
                                             binding limit and g10-03's ordering
                                             is backwards

Registered before running, because this exists to check a recommendation I
already made and the temptation to read the numbers kindly is the whole risk.

## The cap is not optional here

Without `memory_cap` the store accumulates across a long chunk, the delta rule
feeds on the retrievals, and the readout reaches 1e72 with accuracy below chance
— which is exactly what g10-01's first chunk-256 cells measured. Every cell here
runs capped, and `absurd` refuses anything that still comes back broken.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.g10_01_first_language import run_one  # noqa: E402
from experiments.g10_03_what_is_the_ceiling import within_chunk  # noqa: E402
from experiments.harness import parse_args  # noqa: E402
from openplexus.ngram import NGram  # noqa: E402
from openplexus.tasks.corpus import build, chunks, read  # noqa: E402
from experiments.g10_01_first_language import NOTES  # noqa: E402

CHUNKS = (64, 128, 256, 512)


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    width = args.width if args.width else 32
    cap = args.cap if args.cap is not None else 5.0
    seed = args.seed if args.seed is not None else 1

    corpus = build(read(NOTES))
    full = NGram(corpus.vocab_size, 1).fit(corpus.train).bits_per_token(
        corpus.test)
    print(f"width {width}, cap {cap}, seed {seed}; "
          f"bigram over all training text {full:.3f}\n")
    print(f"{'chunk':>7}{'model':>10}{'counting':>11}{'gap':>9}"
          f"{'model acc':>11}")

    records = []
    for chunk in CHUNKS:
        counting = within_chunk(chunks(corpus.test, chunk), corpus.vocab_size)
        record = run_one((seed, width, chunk, cap))[0]
        model = record["bits_calibrated"]
        records.append({"chunk": chunk, "model": model, "counting": counting,
                        "width": width, "cap": cap, "seed": seed,
                        "accuracy": record["accuracy"]})
        print(f"{chunk:>7}{model:>10.3f}{counting:>11.3f}"
              f"{model - counting:>+9.3f}{record['accuracy']:>11.3f}",
              flush=True)

    first, last = records[0], records[-1]
    model_gain = first["model"] - last["model"]
    counting_gain = first["counting"] - last["counting"]
    print(f"\nfrom chunk {first['chunk']} to {last['chunk']}: "
          f"the model gained {model_gain:.3f} bits, counting gained "
          f"{counting_gain:.3f}")
    share = model_gain / counting_gain if counting_gain else 0.0
    print(f"the model captured {share:.0%} of what the extra context was worth")
    if share > 0.6:
        print("  -> PERSISTENCE IS THE FIX. It uses a longer memory when given")
        print("     one, so a store that survives between chunks is the build")
    else:
        print("  -> PERSISTENCE IS NOT THE FIX for this mechanism. It is handed")
        print("     more context and cannot use it, so a store that persists")
        print("     would hand it more of what it already fails to exploit.")
        print("     SUPERPOSITION is the binding limit and g10-03's ordering")
        print("     of what to build is backwards.")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=1))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
