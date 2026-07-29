"""Where on text does the store pay, if the aggregate says nowhere?

## What this follows from

Decision 142 measured the two pathways against each other and found the task
decides which one wins:

    on TEXT    the prior wins and the store adds nothing   (139, 141)
    on MQAR    the store wins and the prior costs 0.279    (142)

MQAR is *all* binding: every answer was bound earlier in the same sequence and
nothing else predicts it. Text is a mixture — most of it is marginals a linear
prior handles, and a thin slice of it is a name or a rare word recurring, which
is binding and nothing else.

**So "the store adds nothing on text" may be a MIXTURE result rather than an
absence.** If the store does its job on the slice where binding is the answer,
and that slice is small, the mean moves by ~nothing while the mechanism works
exactly as designed. That is a different finding from the store being useless,
and the two are indistinguishable in an aggregate.

## The split, and it is the whole design

Every scored position is labelled by one question: **has this token already
appeared earlier in this chunk?**

    REPEAT   the token occurred earlier in the same chunk -- the store has
             something to have bound, and this is where binding can pay
    NOVEL    it did not -- the store has nothing to recall and only the prior
             can help

**And REPEAT alone is confounded, which the first local run showed rather than
argued.** Repeats are 56% of positions and score 7.92 bits against novel's 10.81
-- but `nostore` scores 7.92 on them too. "Occurred earlier in this chunk"
correlates hard with "is a common word", so the repeat slice is easy for MARGINAL
reasons and does not isolate binding at all.

    RARE REPEAT   a repeat whose token is rare in TRAINING (below `RARE`
                  occurrences in 90,000 words). The prior has almost nothing to
                  say about it, so anything predicting it is binding.

That third class is the one with teeth, and it is what P1 is scored on.

The store is rebuilt per chunk (decision 62), so "earlier in this chunk" is
exactly the window it can have written. Any other split would ask about memory
the model does not have.

## PREDICTIONS (a gate, a rail, a falsifier)

  P1  THE GATE. On RARE REPEAT positions, `floor` beats `nostore` by more than
      0.10 bits. The store does its job where binding is the only route, and the
      aggregate null is a mixture.

  P2  THE RAIL. On NOVEL positions, `floor` and `nostore` are within 0.05 bits.
      The store has nothing to recall there, so a gap would mean it is helping
      by some route that is not binding and the split is not measuring what it
      claims.

  P3  THE FALSIFIER. The REPEAT gap is larger than the aggregate gap. If they
      are the same, the store is not concentrated on binding at all and decision
      141's account -- that its whole contribution is prior-shaped -- stands
      without qualification.

**What would refute the whole idea:** P1 failing. The store would contribute
nothing even where the task is exactly what it was built for, at the one unit
where a prior cannot help, and "text is the wrong instrument" would stop being
the explanation.

## RESULT: P1 REFUTED, and it sharpens decision 142 rather than softening it

Seed 0, bias 1, lr 5e-6, cap 5.0:

                    floor      nostore      gap
    all             9.1857     9.1873     +0.0016
    repeat          7.9178     7.9215     +0.0037
    RARE repeat    11.0963    11.0947     -0.0016      6.3% of positions
    novel          10.8058    10.8046     -0.0012

**Nothing, anywhere** -- including where the token appeared earlier in the same
chunk AND is rare enough in training that the prior has almost nothing to say.
The mixture account is wrong: the store is not concentrated on binding in text,
it is absent from all of it.

So the difference between MQAR (0.995) and text is not that text has marginals
the prior can take. **It is that on text the store fails at the very task it aces
on MQAR.**

## THE MECHANICAL CANDIDATE -- TESTED, AND NOT SUPPORTED

    single keys, RARE-repeat gap (nostore - floor; positive = the store helps)
      seed 0    +0.0984
      seed 1    -0.2132
      seed 2    +0.1125
      mean      -0.0008

**Zero, with a 0.2 swing across seeds.** Single keys do not rescue the store on
the one slice where binding is the only route, so the address-shape explanation
below is a hypothesis that did not survive contact with three seeds.

**And it was reported as confirmed off seed 0 alone before the other two
landed.** One cell, +0.098, called "the first time all night the store has helped
on text". That is the failure this whole file exists downstream of, committed by
its author within minutes of writing a memory about it. Left here rather than
tidied away.

BOTH SCHEMES, THREE SEEDS, and this is the complete answer:

                       pair keys                    single keys
                    floor  nostore     gap       floor  nostore     gap
    all             9.1858   9.1873  +0.0015    9.3162   9.1873  -0.1289
    repeat          7.9179   7.9215  +0.0036    8.1256   7.9215  -0.2041
    RARE repeat    11.0962  11.0947  -0.0015   11.0955  11.0947  -0.0008
    novel          10.8059  10.8046  -0.0013   10.8375  10.8046  -0.0329

**Every slice is within 0.004 of zero**, except where single keys are actively
harmful on common repeats. The store contributes nothing on text anywhere, under
either addressing scheme, on any class of position.

One thing the two columns do say: **pair keys are stable across seeds and single
keys are not.** The 0.2 swing that produced the false positive is a single-key
property -- there the store writes recurring addresses, so seed-dependent
interference actually bites. Under pair keys almost every address is written
once and there is nothing for a seed to change.

## The candidate itself, for the record

With `context_keys` the address at position `t` is `hash(t-1, t)`. To retrieve
what followed an earlier occurrence of a rare word, **the preceding token must
match too** -- the same word in a different context has a different address and
the earlier binding is simply not reachable. MQAR does not have this problem
because its query is a bare key with a marker before it, so the pair repeats
exactly.

If that is the explanation, the store is not failing at binding on text; it is
being asked for an address that in-context recall can almost never produce.

**The test is this same split under SINGLE keys**, where the address is the token
alone and the earlier binding is reachable by construction. g18-02 measured
single keys as worse in aggregate (9.276 against 9.186) -- but the aggregate is
exactly the mixture this file exists to look past, and the rare-repeat column is
where the answer would be.

That needs a `--keys` flag here and one more sweep. **Not built**: it is the next
question rather than this one, and naming it beats half-running it.
"""

from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from experiments.harness import bits  # noqa: E402
from experiments.g18_01_does_storing_by_concept_make_words_learnable import (  # noqa: E402
    CHUNK, TEMPERATURES, TRAIN_WORDS, corpus, counting_bars, min_count_for,
    pieces, silent)
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)

WIDTH = 128
SEEDS = (0, 1, 2)
#: A token seen fewer than this many times in the 90,000 training words is
#: "rare": the readout bias has almost no evidence about it, so a repeat of one
#: is a position where binding is the only thing that can help. 100 in 90,000 is
#: about one occurrence per 900 words.
RARE = 100


def labelled(model, chunks, counts, store: bool = True):
    """Scores, targets, and whether each target already occurred in its chunk.

    The label is about the TARGET -- the token being predicted -- and about the
    text before it in the same chunk. A token the chunk has already shown is one
    the store has had the chance to bind; one it has not is a token only a prior
    can speak to.
    """
    rows, wanted, repeat, rare = [], [], [], []
    for tokens in chunks:
        trace: list[dict] = []
        model.run(tokens, trace=trace,
                  store=None if store else silent(tokens))
        for entry in trace:
            index = entry["t"]
            rows.append(entry["scores"])
            wanted.append(int(tokens[index]))
            # STRICTLY BEFORE this position. Including it would label every
            # token a repeat of itself.
            repeat.append(bool((tokens[:index] == tokens[index]).any()))
            rare.append(bool(counts[int(tokens[index])] < RARE))
    return (np.asarray(rows), np.asarray(wanted), np.asarray(repeat),
            np.asarray(rare))


def one_cell(arm: str, seed: int, bias: bool, lr: float, cap: float,
             built=None, keys: str = "pair") -> dict:
    started = time.time()
    built = built or corpus("words")
    stream = built.train[0][:TRAIN_WORDS]
    writes = arm != "nostore"
    model = LocalAssociativeMemory(LocalMemoryConfig(
        d_model=WIDTH, vocab_size=built.vocab_size, seed=seed,
        derived_keys=True, context_keys=(keys == "pair"), readout_bias=bias,
        decay=1.0, memory_cap=cap, lr=lr))

    cut = int(len(stream) * 0.8)
    for piece in pieces((stream[:cut],), CHUNK):
        targets = np.concatenate([piece[1:], piece[-1:]])
        scored = np.ones(len(piece), dtype=bool)
        scored[-1] = False
        model.run(piece, targets, scored, learn=True,
                  store=None if writes else silent(piece))

    counts = np.zeros(built.vocab_size)
    np.add.at(counts, stream, 1.0)
    fit = labelled(model, pieces((stream[cut:],), CHUNK), counts, writes)
    temperature = min(TEMPERATURES, key=lambda t: bits(fit[0], fit[1], t))
    test_chunks = pieces(built.test, CHUNK)
    scores, targets, repeat, rare = labelled(model, test_chunks, counts,
                                             writes)
    unigram, bigram = counting_bars(built.vocab_size, stream, test_chunks)

    def on(mask) -> float:
        return (round(bits(scores[mask], targets[mask], temperature), 4)
                if mask.any() else float("nan"))

    return dict(
        arm=arm, seed=seed, bias=bias, lr=lr, cap=cap, keys=keys,
        error=on(np.ones(len(targets), bool)),
        repeat_error=on(repeat),
        novel_error=on(~repeat),
        rare_repeat_error=on(repeat & rare),
        common_repeat_error=on(repeat & ~rare),
        repeat_share=round(float(repeat.mean()), 4),
        rare_repeat_share=round(float((repeat & rare).mean()), 4),
        temperature=round(float(temperature), 6),
        pinned=bool(temperature in (min(TEMPERATURES), max(TEMPERATURES))),
        unigram=round(unigram, 4), bigram=round(bigram, 4),
        uniform=round(float(np.log2(built.vocab_size)), 4),
        vocab=built.vocab_size, width=WIDTH, scored=int(len(targets)),
        seconds=round(time.time() - started, 1),
        condition=f"{arm}|{keys}|bias{int(bias)}|lr{lr}|cap{cap}"
                  f"|d{WIDTH}|seed{seed}|min{min_count_for('words')}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--arm", choices=("floor", "nostore"), default=None)
    parser.add_argument("--bias", type=int, choices=(0, 1), default=1)
    parser.add_argument("--lr", type=float, default=0.000005)
    parser.add_argument("--cap", type=float, default=5.0)
    parser.add_argument("--keys", choices=("pair", "single"), default="pair",
                        help="THE HYPOTHESIS. With pair keys the address is "
                             "hash(t-1, t), so recalling what followed an "
                             "earlier occurrence needs the PRECEDING token to "
                             "match too. With single keys the address is the "
                             "token alone and the earlier binding is reachable "
                             "by construction")
    parser.add_argument("--json", type=str, default=None)
    args = parser.parse_args()

    harness.refuse_if_mutating()
    seeds = (args.seed,) if args.seed is not None else SEEDS
    arms = (args.arm,) if args.arm else ("floor", "nostore")
    built = corpus("words")

    records = []
    for seed in seeds:
        for arm in arms:
            record = one_cell(arm, seed, bool(args.bias), args.lr, args.cap,
                              built, args.keys)
            print(f"  {record['condition']:52s} "
                  f"all {record['error']:.4f}  "
                  f"repeat {record['repeat_error']:.4f}  "
                  f"RARE-repeat {record['rare_repeat_error']:.4f}  "
                  f"novel {record['novel_error']:.4f}",
                  file=sys.stderr, flush=True)
            records.append(record)

    if args.json:
        path = Path(args.json)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(records, indent=1), encoding="utf-8")
        print(f"wrote {len(records)} records to {path}")
    else:
        for record in records:
            print(f"{record['condition']}  all {record['error']}  "
                  f"repeat {record['repeat_error']}  "
                  f"RARE-repeat {record['rare_repeat_error']}  "
                  f"novel {record['novel_error']}  "
                  f"rare-repeat share {record['rare_repeat_share']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
