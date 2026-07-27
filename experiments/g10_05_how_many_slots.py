"""How many slots would it take?

[g10-04](sweeps/g10-04-does-context-help-this-model.txt) established that
superposition is the binding limit: the model collapses every past occurrence of
a character into one averaged vector, and its shortfall against counting GROWS
with context. It pointed at a capacity-bounded set of distinct items — which the
g9 line has already built as `tag_slots`.

**That is where to look, and this asks what it would cost before anything is
built.** Note 024 costed the gate by arithmetic before the gate was measured;
this is the same move for the same reason. A mechanism needing a hundred slots
per character is not a mechanism for a tiny node, and finding that out by
counting is free.

## What is being counted

`within_chunk(..., slots=N)` keeps the N most recent successors of the current
character, within the current chunk only. N=1 is the harshest superposition
handicap; N=None is unbounded counting. Sweeping N between them says how much of
counting's advantage a bounded store recovers, and where the curve flattens.

**This is a CEILING, not a model.** It is what a perfect bounded store would
score, with no readout, no training and no vectors. A real mechanism reaches some
fraction of it, exactly as the tag reaches a fraction of the oracle in the g9
line. Reading it as a prediction of what the tag would score on text would be
the same error g10-03 made — a number measured on one thing applied to another.

## The decision it informs

    the curve flattens by 4-8 slots     -> the g9 tag's existing capacity range
                                           covers it and a tiny node can afford it
    it needs dozens                     -> per-character storage is the cost, and
                                           it is not a tiny-node mechanism
    unbounded barely beats 1 slot       -> superposition was not the limit after
                                           all, and g10-04 is wrong
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.g10_01_first_language import NOTES  # noqa: E402
from experiments.g10_03_what_is_the_ceiling import within_chunk  # noqa: E402
from experiments.harness import parse_args  # noqa: E402
from openplexus.ngram import NGram, absurd, uniform_bits  # noqa: E402
from openplexus.tasks.corpus import build, chunks, read  # noqa: E402

SLOTS = (1, 2, 4, 8, 16, 32)


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    chunk = int(args.scale) if args.scale is not None else 256

    corpus = build(read(NOTES))
    pieces = chunks(corpus.test, chunk)
    vocab = corpus.vocab_size
    full = NGram(vocab, 1).fit(corpus.train).bits_per_token(corpus.test)
    unbounded = within_chunk(pieces, vocab)
    one = within_chunk(pieces, vocab, slots=1)

    print(f"chunk {chunk}, vocabulary {vocab}, {len(pieces)} test chunks")
    print(f"uniform {uniform_bits(vocab):.3f}   bigram over all training text "
          f"{full:.3f}")
    print(f"\nwithin this chunk: 1 slot {one:.3f}, unbounded {unbounded:.3f}   "
          f"(the whole prize is {one - unbounded:.3f} bits)\n")
    print(f"{'slots':>7}{'bits':>10}{'of the prize':>15}")

    records = []
    for n in SLOTS:
        bits = within_chunk(pieces, vocab, slots=n)
        broken = absurd(bits, vocab)
        if broken:
            raise SystemExit(f"slots {n}: {broken}")
        share = (one - bits) / (one - unbounded) if one != unbounded else 0.0
        records.append({"chunk": chunk, "slots": n, "bits": bits,
                        "share": share})
        print(f"{n:>7}{bits:>10.3f}{share:>14.0%}")

    enough = next((r for r in records if r["share"] >= 0.9), None)
    print()
    if enough is None:
        print(f"  -> even {SLOTS[-1]} slots recovers under 90% of the prize.")
        print("     Per-character storage is the cost and this is not obviously")
        print("     a tiny-node mechanism")
    else:
        print(f"  -> {enough['slots']} slots recovers "
              f"{enough['share']:.0%} of what unbounded counting gains over a")
        print("     single superposed successor")
        if enough["slots"] <= 8:
            print("     That is inside the g9 tag's existing capacity range")
            print("     (4 to 32), so a tiny node can afford it")
    print("\n  CEILING, not a model: no readout, no training, no vectors.")
    print("  A real mechanism reaches a fraction of this, as the tag reaches a")
    print("  fraction of the oracle. Quoting it as the tag's expected score")
    print("  would be the same error g10-03 made.")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=1))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
