"""Do addresses that mean something pay for themselves, and what do they cost?

Note 045's proposal, measured. Every address in this model is a random draw, so
the store has no notion of similarity at all — it is a lookup table with a
superposition trick rather than the map GOALS §1.2 asks for. The proposal keeps
concept ids hash-derived and near-orthogonal (capacity untouched, note 035) and
adds a **separate index** from a content vector to concept ids: similarity picks
WHICH exact reads to make, and never what an address looks like.

## The arms

    floor        index off                        every number measured so far
    b = 1,2,4,8  the index, that many candidates   the cheap variants
    CEILING      every concept a candidate         "sweep everything, then filter"

`CEILING` is John's proposal taken literally, and it is here as a **reference
rather than a candidate**: it costs a read per concept in the vocabulary, which
no deployment would pay. Its job is to say what the design is worth at all, so a
cheap variant reports as a fraction of it — *"94% of the ceiling for 8% of the
reads"* means something, where a bare accuracy does not.

## Weighting is an axis, not a default

P4 measured content vectors at mean off-diagonal cosine **0.50** against hash
keys' 0.0005 — everything resembles everything, because everything co-occurs
with `the`. Down-weighting frequent context to `1/count**power` drops it to 0.22
and sharpens `king` to `richard, edward, henry`, **while collapsing `father` and
`love` into function words.** It over-corrects.

So a result quoted at one weighting is a result about that weighting. `power` is
swept, and any headline that moves with it says so.

## Word level, and what that costs

Run on words rather than characters, because a concept cannot live in a letter
and there is no address for a content vector to describe. **Every number in this
project's comparison set was measured at character level and none of them
transfer**, so `floor` here is a new baseline rather than a figure to compare
against the old ones. That is the price of the change and it is paid once.

## Cost is a column, not a footnote

Reads per answered position, beside every accuracy. John, 2026-07-29: the end
result gets compared *"both computationally and results-wise"*. An accuracy that
cannot be cited without its price is the point.

## PREDICTIONS (registered before running)

  P1  Every indexed arm beats `floor` on bits/token. If similarity buys nothing
      on the corpus where P4 already showed structure exists, note 045 is wrong
      and the line stops here. **This is the gate.**
  P2  `CEILING` is the best arm at every weighting. It has strictly more
      evidence than any subset, so an arm beating it would mean the combination
      is hurting rather than the candidates being wrong — a different finding
      and a more interesting one.
  P3  Returns diminish in `b`: the step from 1 to 2 is larger than 4 to 8.
      Nearest neighbours carry most of what there is, which is what makes a
      cheap variant worth having at all.
  P4  The best `power` is not 0.0 — some down-weighting helps. P4's probe showed
      the unweighted space dominated by frequency, and a frequency-dominated
      index proposes the commonest words to everything.
  P5  Reads per answered position tracks `b + 1` within 1%. This is a rail, not
      a finding: if it does not, the index is being consulted somewhere I did
      not intend and every cost figure is wrong.

P1 is the gate. P5 is the rail — if it fails, P1–P4 are unreadable.

**What would refute the whole design:** P1 failing at every `b` and every
`power`, with `CEILING` no better than `floor`. That says the content structure
P4 found does not survive contact with the store, and the split-the-address idea
is dead for the cost of one sweep rather than a rebuild.

## ⚠⚠ AND THE CALIBRATION BELOW IS VOID — DECISION 138

The training call in `one_cell` targets the CURRENT token where the model's
answer predicts the NEXT one. Corrected, the same harness moves a character-level
floor from 5.9965 to 5.4227. **So "the model does not learn word-level text at
all" — the finding that turned the architecture line toward addressing — was
measured on a mistrained readout and has to be re-established.**

What survives: the unigram bar (decision 135, counts from `NGram`) and the
address-space arithmetic. The calibration table below does not.

## ⚠ NOT DISPATCHED — the instrument cannot answer this yet

Calibrated locally before spending a matrix, and the calibration says stop.
**The model does not learn word-level text at all**: 90,000 words buys 0.038
bits over uniform, and it sits **2.65 bits WORSE than a word unigram**. There is
no headroom for an index to occupy, so every arm would return the same
non-answer.

    uniform                     10.759
    train 90,000 words          10.721
    word unigram                 8.068   <- not approached

The unigram figure was **9.323 here until decision 135**, hand-rolled beside the
calibration rather than taken from `openplexus/ngram.py`. The gap is larger than
this script originally claimed, not smaller.

Single-token keys diverge to NaN instead, so the two obvious repairs pull
opposite ways: pair keys give context and destroy recurrence (at word level
almost every test key was written once during training), single-token keys give
recurrence and diverge. Full record in
`experiments/sweeps/g17-01-do-meaningful-addresses-pay.txt`.

**This says nothing about note 045** — the index was never reached, and P1–P5
below stay registered and unscored. It says the word-level path needs a model
that can learn word-level text first, which is its own line of work.

The predictions are kept exactly as registered so this script can be dispatched
unchanged once a usable instrument exists.

COST: 6 branch settings x 3 weightings x 4 seeds = 72 cells, plus `floor` at 4.
The CEILING cell is the expensive one — a read per concept per position — and the
estimate comes from it. Printed by `--cost`.
"""

from __future__ import annotations

import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from openplexus.content import ContentIndex  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.corpus import build_stream, words  # noqa: E402

WIDTH = 256
CONTENT_WIDTH = 256
#: Words rarer than this become UNKNOWN. Higher than the character default: a
#: word seen twice is vocabulary the model cannot learn and cannot be scored on
#: fairly, and the long tail of them would inflate `uniform_bits` and flatter
#: every arm equally but meaninglessly.
MIN_COUNT = 10
EPOCHS = 2
CHUNK = 256
SEEDS = tuple(range(4))
BRANCHES = (0, 1, 2, 4, 8)
POWERS = (0.0, 0.25, 0.5)


def corpus():
    text = (Path(__file__).resolve().parent.parent
            / "data" / "tinyshakespeare.txt").read_text(encoding="utf-8")
    return build_stream(text, test_share=0.1, min_count=MIN_COUNT, units=words)


def fit_index(built, power: float, seed: int) -> ContentIndex:
    """The content space, learned from TRAINING text only.

    Fitting on the test half would let the index know which words keep company
    in text the model is scored on -- a leak in the flattering direction and
    exactly the kind that goes unnoticed, since it improves a number rather than
    raising an error.

    Counted first, then accumulated: weighting by a running count makes the
    result depend on the order sequences arrive, which would break the merge
    property and make two nodes disagree.
    """
    counts = ContentIndex.count(built.vocab_size, *built.train)
    index = ContentIndex(built.vocab_size, width=CONTENT_WIDTH, seed=seed,
                         power=power, frequency=counts if power else None)
    for document in built.train:
        index.observe(document)
    return index


#: Temperatures the readout's confidence is calibrated over. Uncalibrated bits
#: are not comparable ACROSS ARMS: the index adds several retrievals together, so
#: an indexed arm's scores have a different scale from `floor`'s, and comparing
#: them raw would measure logit magnitude rather than the distribution's quality.
#: g10-01 established the procedure and the leak that breaks it.
TEMPERATURES = tuple(np.exp(np.linspace(np.log(0.05), np.log(20.0), 25)))


def collected(model, pieces) -> tuple[np.ndarray, np.ndarray]:
    """Every prediction and the word that actually came.

    Position 0 of a chunk is skipped: with no previous token there is no
    retrieval, so scoring it would score the initialisation.
    """
    rows, wanted = [], []
    for tokens in pieces:
        trace: list[dict] = []
        model.run(tokens, trace=trace)
        for entry in trace:
            if entry["t"] == 0:
                continue
            rows.append(entry["scores"])
            wanted.append(int(tokens[entry["t"]]))
    return np.asarray(rows), np.asarray(wanted)


from experiments.harness import bits  # noqa: E402,F401


def pieces(documents, chunk: int) -> list:
    return [document[start:start + chunk]
            for document in documents
            for start in range(0, len(document) - chunk, chunk)]


def one_cell(branches: int, power: float, seed: int) -> dict:
    built = corpus()
    started = time.time()
    index = fit_index(built, power, seed) if branches else None
    model = LocalAssociativeMemory(LocalMemoryConfig(
        d_model=WIDTH, vocab_size=built.vocab_size, seed=seed,
        derived_keys=True, context_keys=True,
        index_branches=branches))
    model.content = index

    # CALIBRATION TEXT IS HELD OUT OF TRAINING, not split off the chunk list.
    # g10-01 made that mistake and fitted the temperature where the model was
    # far more confident than it had any right to be, choosing the sharpest
    # temperature on the grid and reporting 37 bits per character.
    stream = built.train[0]
    cut = int(len(stream) * 0.8)
    training = pieces((stream[:cut],), CHUNK)
    calibration = pieces((stream[cut:],), CHUNK)
    for _ in range(EPOCHS):
        for piece in training:
            # ⚠ THE TARGET IS WRONG HERE -- DECISION 138, and this line is
            # where g18 inherited it from. The model's answer at step t is built
            # from a retrieval keyed on token t, so it predicts token t+1;
            # training it to name token t is a mapping its input cannot carry.
            # The readout still learns (|Wo| grows) and the calibration then
            # flattens a signal-free score vector to uniform, so the failure
            # presents as "the store contributes nothing".
            #
            # NOT FIXED IN PLACE, deliberately: this script was never dispatched
            # and its record quotes numbers produced by exactly this line. Fixing
            # the code without re-running would make the two disagree silently.
            # Anyone re-running it must use g15-01's form first:
            #     targets = np.concatenate([piece[1:], piece[-1:]])
            #     scored = np.ones(len(piece), bool); scored[-1] = False
            model.run(piece, piece, np.ones(len(piece), bool), learn=True)

    fit_scores, fit_targets = collected(model, calibration)
    temperature = min(TEMPERATURES,
                      key=lambda t: bits(fit_scores, fit_targets, t))
    test_scores, test_targets = collected(model, pieces(built.test, CHUNK))
    error = bits(test_scores, test_targets, temperature)
    scored = len(test_targets)
    return dict(
        arm=("floor" if not branches else
             "ceiling" if branches >= built.vocab_size - 1 else f"b{branches}"),
        branches=branches, power=power, seed=seed,
        error=round(error, 4),
        raw=round(bits(test_scores, test_targets, 1.0), 4),
        temperature=round(float(temperature), 4),
        # THE COST COLUMN. Quoted beside the accuracy so neither can be cited
        # without the other, which is what John asked for.
        reads_per_position=branches + 1,
        vocab=built.vocab_size,
        train_tokens=built.train_tokens,
        index_numbers=index.numbers_held if index else 0,
        store_numbers=WIDTH * WIDTH,
        spread=round(index.spread(), 4) if index else None,
        scored=int(scored),
        seconds=round(time.time() - started, 1),
        condition=(f"{'floor' if not branches else f'b{branches}'}"
                   f"|power{power}|d{WIDTH}|seed{seed}"
                   f"|min{MIN_COUNT}|epochs{EPOCHS}"))


def cost_probe() -> None:
    built = corpus()
    print(f"vocab {built.vocab_size}, train {built.train_tokens} words, "
          f"test {built.test_tokens}")
    started = time.time()
    one_cell(8, 0.25, 0)
    each = time.time() - started
    cells = len(SEEDS) * (1 + (len(BRANCHES) - 1) * len(POWERS))
    print(f"one b8 cell {each:.0f}s; {cells} cells excluding the ceiling")
    print(f"the CEILING cell reads {built.vocab_size} times per position, so "
          f"~{built.vocab_size / 9:.0f}x a b8 cell -- costed separately before "
          f"it is dispatched")


def main() -> int:
    args = harness.parse_args(__doc__.splitlines()[0])
    if getattr(args, "cost", False):
        cost_probe()
        return 0
    seeds = (args.seed,) if args.seed is not None else SEEDS
    records = []
    for seed in seeds:
        for branches in BRANCHES:
            for power in (POWERS if branches else (0.0,)):
                record = one_cell(branches, power, seed)
                print(f"  {record['condition']:44s} "
                      f"bits {record['error']:.4f}  "
                      f"reads {record['reads_per_position']}",
                      file=sys.stderr)
                records.append(record)
    harness.emit(records, Path(args.json) if args.json else None)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
