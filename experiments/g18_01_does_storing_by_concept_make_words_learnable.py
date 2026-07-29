"""Does storing by CONCEPT rather than by surface make word-level text learnable?

## What this follows from

g17-01 tried to measure note 045's index at word level and could not, because the
model does not learn word-level text at all: 90,000 words buys 0.038 bits over
uniform, and it sits **2.65 bits worse than a word unigram**. The reason it found
is address sparsity — with pair keys, 1,733 words make tens of thousands of
distinct addresses and almost every one is seen once, so the store memorises
hapaxes.

**The 2.65 is a correction to g17-01's own record**, which quoted the word
unigram at 9.323 where the project's `NGram` scores the same training words on
the same test positions at 8.068. The finding is unchanged and larger than it was
written down as; `counting_bars` below computes both bars through `NGram` so the
sweep cannot inherit a hand-rolled bar again.

**The store is no longer required to be addressed by surfaces.** `concepts.Shared`
expresses a grouping of surfaces into concepts, `grouping.cluster` builds one from
`ContentIndex` vectors, and `keys.ByConcept` makes the store use it. If a concept
is a group of words rather than one word, the address space collapses and each
address recurs many times.

**The readout is untouched: store by concept, emit by word.** Nothing is lost on
the output side; what is spent is resolution, because two words in one group are
indistinguishable as context.

## The arms

    floor         one concept per word            g17-01's configuration
    concept-K     K groups from content vectors   the proposal
    permuted-K    the same group SIZES, members shuffled
    shuffled-K    groups from an index fitted on SHUFFLED text

**Two controls, because there are two ways to be wrong.** `permuted` matches the
address-space statistics exactly and destroys the meaning, so it separates "fewer
addresses helped" from "the grouping meant something". `shuffled` destroys the
meaning at the source instead, leaving the clusterer to find whatever structure
survives — which is mostly frequency. An arm that beats `floor` but not its
controls is a real finding and a much smaller one.

## PREDICTIONS (registered before running)

  P1  THE GATE. Some `concept-K` beats `floor` by more than 0.10 bits/word. If
      no K beats the floor at all, storing by concept does not make word-level
      text learnable and this line stops here rather than being retuned.
  P2  THE CONTROL. The best `concept-K` beats `permuted-K` and `shuffled-K` at
      the same K by more than 0.05 bits. If the controls match it, the gain is
      the address space shrinking and the content vectors are not doing the
      work -- a different finding, and one that says the cheap fix is fewer
      addresses however chosen.
  P3  Bits per word is non-monotonic in K, with a minimum at an intermediate
      value. Small K destroys the context resolution the store needs; large K
      restores the sparsity that caused the problem.
  P4  A RAIL, not a finding. Distinct training addresses fall by roughly the
      square of the grouping ratio and mean recurrence rises to match. If it
      does not, `ByConcept` is not reaching the store and every arm is measuring
      the same model.
  P5  No arm reaches the word unigram, which `NGram` puts at 8.068 for these
      training words. The mechanism is worth less than the 2.65 bits that would
      take. If it IS reached, that is the headline and every claim about what
      this model cannot do at word level needs rewriting.

**What would refute the proposal:** P1 failing at every K, with the rail P4
passing. That says the address space collapsed exactly as designed and the model
still learned nothing, so sparsity was not the binding constraint and note 042's
account of the wall needs the next explanation rather than a bigger sweep.

## Calibrate before dispatching

`--calibrate` runs `floor` and a few K locally on one seed. Decision 63's rule and
the reason g17-01 cost twenty minutes instead of a matrix: probe the bottom of the
range locally before spending CI on it.
"""

from __future__ import annotations

import argparse
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from experiments.harness import bits  # noqa: E402
from openplexus.concepts import OneConceptPerToken, Shared  # noqa: E402
from openplexus.content import ContentIndex  # noqa: E402
from openplexus.grouping import cluster  # noqa: E402
from openplexus.keys import ByConcept  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.ngram import NGram  # noqa: E402
from openplexus.tasks.corpus import build_stream, words  # noqa: E402

WIDTH = 128
CONTENT_WIDTH = 256
#: Words rarer than this become UNKNOWN, as in g17-01. A word seen twice is
#: vocabulary the model cannot learn and cannot be scored on fairly.
MIN_COUNT = 10
#: g17-01's calibration point, so `floor` here is comparable to its 10.721
#: rather than being a fourth new baseline.
TRAIN_WORDS = 90_000
EPOCHS = 1
CHUNK = 256
SEEDS = tuple(range(3))
GROUPS = (64, 128, 256, 512, 1024)
KINDS = ("concept", "stratified", "permuted", "shuffled")
#: How many of the commonest words `stratified` leaves ungrouped. Fixed rather
#: than swept: it is a way of asking whether grouping the tail alone is the
#: better shape, and sweeping it here would confound that question with tuning.
FREQUENT = 200
#: The readout bias, as an AXIS rather than a default. Off is the configuration
#: every number in the comparison set was measured with, and it leaves the model
#: unable to express a prior at all -- so it cannot reach a unigram there
#: whatever the addressing does. On is where "learnable at all" is a fair
#: question. Both are run, and a claim quoted from one says which.
BIASES = (False, True)
#: THE BRAKE, and it is an axis because the mechanism REQUIRES one.
#:
#: The first concept cell overflowed to NaN. Pair keys over surfaces never did,
#: and the reason is the defect itself: almost every address was written once, so
#: the sparsity that made the model useless was also what held the store's norm
#: down. Collapsing the address space restores recurrence -- the point of the
#: proposal -- and the same key now gets written tens of times with `decay=1.0`
#: and no cap.
#:
#: 1.0 stays in the grid so the divergence is RECORDED rather than assumed. A
#: cell that diverges reports NaN, which is a result about what the mechanism
#: needs and not a failed run.
DECAYS = (1.0, 0.997)
#: The index weighting. Fixed rather than swept: K is the axis this experiment
#: is about, and a result quoted at one weighting says so (g17-01's caveat).
#: 0.5 is the value that sharpened `king` to `richard, edward, henry`.
POWER = 0.5
#: WIDER THAN THE STOCK GRID, AND MEASURED RATHER THAN GUESSED.
#:
#: Every word-level number so far was calibrated over 0.05 to 20. At word level
#: the model's logits have a standard deviation of 0.006, so the fit wants to
#: amplify them far harder than at character level: with `readout_bias` on, the
#: stock grid chose its own smallest value -- **pinned at the edge** -- and
#: understated that arm by 0.057 bits. A pinned calibration understates whichever
#: arm pins, which is not a constant offset and can invert a comparison.
#:
#: `pinned` is recorded per cell, so this is a rail rather than a hope.
TEMPERATURES = tuple(np.exp(np.linspace(np.log(1e-4), np.log(20.0), 60)))


def corpus():
    text = (Path(__file__).resolve().parent.parent
            / "data" / "tinyshakespeare.txt").read_text(encoding="utf-8")
    return build_stream(text, test_share=0.1, min_count=MIN_COUNT, units=words)


def pieces(documents, chunk: int) -> list:
    return [document[start:start + chunk]
            for document in documents
            for start in range(0, len(document) - chunk, chunk)]


def fit_index(vocab: int, stream: np.ndarray, seed: int) -> ContentIndex:
    """The content space, learned from TRAINING text only.

    Fitting on the test half would let the index know which words keep company
    in the text the model is scored on -- a leak in the flattering direction,
    and one that raises no error.
    """
    counts = ContentIndex.count(vocab, stream)
    index = ContentIndex(vocab, width=CONTENT_WIDTH, seed=seed, power=POWER,
                         frequency=counts)
    for piece in pieces((stream,), CHUNK):
        index.observe(piece)
    return index


def stratified_groups(index: ContentIndex, stream: np.ndarray, vocab: int,
                      k: int, seed: int) -> list[list[int]]:
    """Group only the words that do not already recur.

    **The plain grouping puts over half the vocabulary in one concept.** At
    K=128, the largest cluster holds 899 of 1,733 words: content space is
    dominated by the rare words, which resemble each other because they resemble
    nothing. Merging them with the common words spends resolution exactly where
    the model could afford to keep it.

    The problem being fixed is hapaxes -- addresses seen once. A word seen 400
    times already recurs and needs no help; a word seen eleven times is the whole
    defect. So the frequent words keep their own concepts and only the tail is
    grouped, which is the same idea as `MIN_COUNT` one level up.
    """
    counts = ContentIndex.count(vocab, stream)
    common = np.argsort(-counts)[:FREQUENT]
    # Clustered on the TAIL ALONE, by zeroing the common rows -- `cluster`
    # already leaves zero rows out of every group, and `Shared` already gives an
    # ungrouped token its own concept, so the two existing rules compose into
    # this one without a special case. Clustering everything and splitting the
    # common words back out afterwards would leave the centres where the common
    # words put them, which is the arrangement being avoided.
    vectors = index.vectors.copy()
    vectors[common] = 0.0
    return cluster(vectors, k, seed=seed)


def surfaces_for(kind: str, k: int, vocab: int, stream: np.ndarray,
                 seed: int):
    """The grouping this arm addresses the store by.

    `permuted` keeps the learned group SIZES and reassigns which words fill
    them, so the address-space statistics are matched to the digit and only the
    meaning is gone. `shuffled` destroys the meaning at the source instead and
    lets the clusterer find whatever survives, which is mostly frequency. Two
    ways of being wrong need two controls.
    """
    if kind == "floor":
        return OneConceptPerToken(vocab), None
    if kind == "shuffled":
        # The SAME words in a different order, so unigram statistics are
        # untouched and only the company a word keeps is destroyed.
        scrambled = np.random.default_rng((seed, 91)).permutation(stream)
        index = fit_index(vocab, scrambled, seed)
        return Shared(vocab, cluster(index.vectors, k, seed=seed)), index
    index = fit_index(vocab, stream, seed)
    if kind == "stratified":
        return (Shared(vocab, stratified_groups(index, stream, vocab, k, seed)),
                index)
    groups = cluster(index.vectors, k, seed=seed)
    if kind == "concept":
        return Shared(vocab, groups), index
    if kind == "permuted":
        order = np.random.default_rng((seed, 92)).permutation(vocab)
        sizes = [len(group) for group in groups]
        cut, rebuilt = 0, []
        for size in sizes:
            rebuilt.append([int(t) for t in order[cut:cut + size]])
            cut += size
        return Shared(vocab, rebuilt), index
    raise ValueError(f"unknown arm kind {kind!r}")


def addressing(stream: np.ndarray, surfaces) -> tuple[int, float]:
    """Distinct pair addresses in the training stream, and mean recurrence.

    **The rail.** The whole proposal is that grouping collapses the address space
    and raises recurrence; if these two numbers do not move, nothing else in the
    row means anything. Computed from the stream rather than from the model, so
    a bug in the wiring cannot make them agree with the accuracy.
    """
    concepts = np.asarray([surfaces.of(int(t)) for t in stream])
    pairs = set(zip(concepts[:-1].tolist(), concepts[1:].tolist()))
    return len(pairs), (len(concepts) - 1) / max(len(pairs), 1)


def collected(model, chunks) -> tuple[np.ndarray, np.ndarray]:
    """Every prediction and the word that actually came.

    The trace starts at position 1: position 0 has no previous token, so there
    is no retrieval there and scoring it would score the initialisation.
    """
    rows, wanted = [], []
    for tokens in chunks:
        trace: list[dict] = []
        model.run(tokens, trace=trace)
        for entry in trace:
            rows.append(entry["scores"])
            wanted.append(int(tokens[entry["t"]]))
    return np.asarray(rows), np.asarray(wanted)


def counting_bars(vocab: int, stream: np.ndarray, chunks) -> tuple[float, float]:
    """The unigram and bigram bars, from the project's own counter.

    **Through `NGram` rather than a fresh implementation**, which is not
    tidiness: g17-01's record quotes a word unigram at 9.323 and `NGram` scores
    the same corpus, the same training words and the same test chunks at 8.068.
    The bar was 1.26 bits easier than the number every conclusion was drawn
    against. One implementation of the arithmetic is how that stops recurring.

    Scored on `chunk[1:]` so the bars see exactly the positions the model is
    scored on -- the trace has no entry at position 0, and a bar measured over
    a different set is not the same bar.
    """
    tails = [chunk[1:] for chunk in chunks]
    return (NGram(vocab, order=0).fit([stream]).bits_per_token(tails),
            NGram(vocab, order=1).fit([stream]).bits_per_token(tails))


def one_cell(kind: str, k: int, seed: int, built=None, bias: bool = False,
             decay: float = 0.997) -> dict:
    started = time.time()
    built = built or corpus()
    stream = built.train[0][:TRAIN_WORDS]
    surfaces, index = surfaces_for(kind, k, built.vocab_size, stream, seed)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        d_model=WIDTH, vocab_size=built.vocab_size, seed=seed,
        derived_keys=True, context_keys=True, readout_bias=bias,
        decay=decay))
    if kind != "floor":
        # The store is addressed by concept; `model.surfaces` keeps routing
        # consistent with it for the partitioned path, which is off here.
        model.key_source = ByConcept(model.key_source, surfaces,
                                     built.vocab_size)
        model.surfaces = surfaces
    model.content = index

    # CALIBRATION TEXT IS HELD OUT OF TRAINING, not split off the chunk list.
    # g10-01 fitted a temperature inside the training region and reported 37
    # bits per character.
    cut = int(len(stream) * 0.8)
    training = pieces((stream[:cut],), CHUNK)
    calibration = pieces((stream[cut:],), CHUNK)
    for _ in range(EPOCHS):
        for piece in training:
            model.run(piece, piece, np.ones(len(piece), bool), learn=True)

    fit_scores, fit_targets = collected(model, calibration)
    test_chunks = pieces(built.test, CHUNK)
    # DIVERGENCE IS A RESULT, NOT A CRASH. Without a brake the store's norm runs
    # away once addresses recur, and the readout goes with it. Reported as its
    # own field so a NaN in the table reads as "this configuration cannot be
    # run" rather than as a job that fell over.
    diverged = not (np.isfinite(fit_scores).all()
                    and np.isfinite(model.wo).all())
    if diverged:
        temperature, error = float("nan"), float("nan")
        test_targets = np.concatenate([chunk[1:] for chunk in test_chunks])
    else:
        temperature = min(TEMPERATURES,
                          key=lambda t: bits(fit_scores, fit_targets, t))
        test_scores, test_targets = collected(model, test_chunks)
        error = round(bits(test_scores, test_targets, temperature), 4)
    addresses, recurrence = addressing(stream, surfaces)
    unigram, bigram = counting_bars(built.vocab_size, stream, test_chunks)
    return dict(
        arm=f"{kind}-{k}" if kind != "floor" else "floor",
        kind=kind, groups=k, seed=seed, bias=bias, decay=decay,
        diverged=bool(diverged),
        error=error,
        temperature=round(float(temperature), 6),
        # THE CALIBRATION RAIL. True means the fit wanted a temperature outside
        # the grid, so this cell's bits are an overstatement of its error by an
        # unknown amount and it must not be compared with one that is not
        # pinned.
        pinned=bool(not diverged
                    and temperature in (min(TEMPERATURES),
                                        max(TEMPERATURES))),
        concepts=surfaces.concepts,
        vocab=built.vocab_size,
        # THE RAIL, beside the accuracy so neither can be cited without it.
        addresses=addresses,
        recurrence=round(recurrence, 3),
        unigram=round(unigram, 4),
        bigram=round(bigram, 4),
        uniform=round(float(np.log2(built.vocab_size)), 4),
        train_words=int(len(stream)),
        scored=int(len(test_targets)),
        index_numbers=index.numbers_held if index else 0,
        store_numbers=WIDTH * WIDTH,
        seconds=round(time.time() - started, 1),
        condition=f"{kind}{k if kind != 'floor' else ''}"
                  f"|bias{int(bias)}|decay{decay}|d{WIDTH}|seed{seed}"
                  f"|power{POWER}|min{MIN_COUNT}|epochs{EPOCHS}")


def calibrate() -> None:
    """One seed, `floor` and a few K, locally. Does anything move at all?

    Both bias settings, because they answer different questions and only one of
    them is about this mechanism. With `readout_bias` off the model has no way
    to express a prior at all -- a unigram IS a bias over tokens -- so it cannot
    reach the bar however good the addressing gets, and the comparison that
    means something there is against `floor`. With it on the bar is reachable in
    principle, which is what "learnable at all" was asking.
    """
    built = corpus()
    print(f"vocab {built.vocab_size}, train {built.train_tokens} words, "
          f"using {TRAIN_WORDS}", flush=True)
    for bias in (False, True):
        for kind, k in [("floor", 0), ("concept", 128), ("concept", 512),
                        ("stratified", 128), ("permuted", 128)]:
            record = one_cell(kind, k, 0, built, bias=bias)
            print(f"  bias={int(bias)} {record['arm']:14s} "
                  f"bits {record['error']:7.3f}  "
                  f"concepts {record['concepts']:5d}  "
                  f"addresses {record['addresses']:7d}  "
                  f"recurrence {record['recurrence']:8.2f}  "
                  f"{'PINNED ' if record['pinned'] else ''}"
                  f"{record['seconds']:.0f}s", flush=True)
    print(f"  {'unigram':14s} bits {record['unigram']:7.3f}   <- the bar",
          flush=True)
    print(f"  {'bigram':14s} bits {record['bigram']:7.3f}", flush=True)
    print(f"  {'uniform':14s} bits {record['uniform']:7.3f}", flush=True)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--arm", choices=("floor",) + KINDS, default=None,
                        help="one kind of grouping only, so a job is one "
                             "column of the matrix rather than all of it")
    parser.add_argument("--json", type=str, default=None)
    parser.add_argument("--bias", type=int, choices=(0, 1), default=None,
                        help="readout bias: 0 reproduces the comparison set, "
                             "1 gives the model a way to express a prior at "
                             "all; omit to run both")
    parser.add_argument("--decay", type=float, default=None,
                        help="the store's brake. 1.0 is no brake, which is the "
                             "default the model has always had and which "
                             "DIVERGES once addresses recur; omit to run both")
    parser.add_argument("--calibrate", action="store_true",
                        help="one seed, a few K, locally -- does anything move")
    args = parser.parse_args()

    # Every experiment goes through this, and it is the one place the check
    # cannot be forgotten: the mutation harness edits source in place, and a
    # measurement taken through mutated source is not a measurement.
    harness.refuse_if_mutating()
    if args.calibrate:
        calibrate()
        return 0

    seeds = (args.seed,) if args.seed is not None else SEEDS
    kinds = (args.arm,) if args.arm else ("floor",) + KINDS
    biases = (bool(args.bias),) if args.bias is not None else BIASES
    decays = (args.decay,) if args.decay is not None else DECAYS
    built = corpus()
    records = []
    for seed in seeds:
        for bias in biases:
            for decay in decays:
                for kind in kinds:
                    for k in ((0,) if kind == "floor" else GROUPS):
                        record = one_cell(kind, k, seed, built, bias=bias,
                                          decay=decay)
                        print(f"  {record['condition']:44s} "
                              f"bits {record['error']:.4f}  "
                              f"addresses {record['addresses']}"
                              f"{'  DIVERGED' if record['diverged'] else ''}",
                              file=sys.stderr, flush=True)
                        records.append(record)
    harness.emit(records, Path(args.json) if args.json else None)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
