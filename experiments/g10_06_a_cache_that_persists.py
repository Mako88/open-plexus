"""A bounded per-key cache that does NOT reset between chunks.

Two findings collide here.

[g10-04](sweeps/g10-04-does-context-help-this-model.txt) showed the VECTOR store
cannot use more context: handed eight times as much it captured 24% of what was
available. So persistence was ruled out as a fix — for that mechanism.

[g10-05](sweeps/g10-05-how-many-slots.txt) showed a bounded set of distinct
successors per key recovers 83-97% of what counting gains over one superposed
average, and `tools/slot_cost.py` showed it is affordable as token ids above node
width 3.

**But g10-05 reset its cache at every chunk boundary, because that is what the
model does.** A cache has no reason to. Clearing an array is not a saving; it was
an artefact of comparing against a store whose reset is structural.

So this asks the question neither run asked: **a bounded per-key cache, `slots`
deep, carried across the whole stream.** Persistence is free for a cache and was
only expensive for the vector store.

## Why this matters beyond the number

John's third question is whether traditional computing — caches, DHTs — can
replace biological mechanisms. A small per-key cache with keys regenerated from
token ids is exactly that: an LRU table, not an associative vector store, and the
most conventional data structure imaginable.

If it reaches the bigram it is a direct answer to that question, and an
uncomfortable one for the vector store.

## What it cannot show

Counting, again. No readout, no training, no vectors — a CEILING. And a cache
holding the last N successors of every character it has ever seen is closer to
"a bigram with bounded counts" than to a memory, which is the point and also the
caveat: reaching the bigram this way is not the model learning anything.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments.g10_01_first_language import NOTES  # noqa: E402
from experiments.g10_03_what_is_the_ceiling import within_chunk  # noqa: E402
from experiments.harness import parse_args  # noqa: E402
from openplexus.ngram import NGram, absurd, uniform_bits  # noqa: E402
from openplexus.tasks.corpus import build, chunks, read  # noqa: E402

SLOTS = (1, 2, 4, 8, 16, 32, 128)


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    chunk = int(args.scale) if args.scale is not None else 256

    corpus = build(read(NOTES))
    vocab = corpus.vocab_size
    full = NGram(vocab, 1).fit(corpus.train).bits_per_token(corpus.test)
    # ONE stream. The cache is never cleared, which is the whole difference from
    # g10-05 -- and a cache has no reason to clear.
    whole = (np.concatenate(corpus.test),)
    reset = chunks(corpus.test, chunk)

    print(f"vocabulary {vocab}, {len(whole[0])} test characters")
    print(f"uniform {uniform_bits(vocab):.3f}   bigram over all training text "
          f"{full:.3f}   (the bar)\n")
    print(f"{'slots':>7}{'persisting':>13}{'reset each ' + str(chunk):>18}"
          f"{'persistence buys':>19}")

    records = []
    for n in SLOTS:
        kept = within_chunk(whole, vocab, slots=n)
        cleared = within_chunk(reset, vocab, slots=n)
        for name, value in (("persisting", kept), ("reset", cleared)):
            broken = absurd(value, vocab)
            if broken:
                raise SystemExit(f"slots {n} {name}: {broken}")
        records.append({"slots": n, "persisting": kept, "reset": cleared,
                        "chunk": chunk})
        print(f"{n:>7}{kept:>13.3f}{cleared:>18.3f}{cleared - kept:>+19.3f}")

    best = min(records, key=lambda r: r["persisting"])
    print(f"\n  best persisting cache: {best['slots']} slots at "
          f"{best['persisting']:.3f} bits, against the bigram's {full:.3f}")
    short = best["persisting"] - full
    if short <= 0:
        print("  -> A PERSISTING CACHE REACHES THE BIGRAM. The bar this project")
        print("     set for goal 2 is met by an LRU table with derived keys,")
        print("     which is a traditional-computing answer to John's third")
        print("     question and an uncomfortable one for the vector store")
    elif short < 0.5:
        print(f"  -> within {short:.3f} bits of the bigram. A conventional")
        print("     cache gets most of the way; the vector store gets none of it")
    else:
        print(f"  -> still {short:.3f} bits short. Persistence helps a cache")
        print("     where it could not help the vector store, and it is not")
        print("     sufficient on its own")

    print(f"\n  THE COMPARISON THAT IS FAIR: against the MODEL, not the bigram.")
    print(f"  The cache and the model both run over the same test stream using")
    print(f"  only prior tokens, so they are directly comparable. The bigram is")
    print(f"  fitted on DISJOINT TRAINING DOCUMENTS and cannot see the test")
    print(f"  set's own repetition, which the cache exploits -- so 'within")
    print(f"  {short:.3f} bits of the bigram' is NOT apples to apples and part")
    print(f"  of that closeness is online adaptation rather than knowing English.")

    print("\n  A CEILING, not a model: counting only, no readout, no training.")
    print("  And a cache holding the last N successors of every character it")
    print("  has seen is closer to a bigram with bounded counts than to a")
    print("  memory -- reaching the bar this way is not the model learning.")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=1))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
