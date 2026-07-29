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
    stratified-K  the 200 commonest words kept apart, only the tail grouped
    permuted-K    the same group SIZES, members shuffled
    shuffled-K    groups from an index fitted on SHUFFLED text

## The brake is part of the mechanism, not a setting

The first concept cell **overflowed to NaN**, and the reason is the defect
itself: with one address per word pair almost everything was written once, so the
sparsity that made the model useless was also the only thing holding the store's
norm down. Restoring recurrence removes that. `CAP` below records what was probed
and why a cap rather than a fade — and `cap 0`, the model's own default, is run
as an arm so the divergence is on the record.

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
  P4  A RAIL, not a finding. Distinct training addresses fall and mean
      recurrence rises to match, at every K. If they do not, `ByConcept` is not
      reaching the store and every arm is measuring the same model.

      **The first version of this said "by roughly the SQUARE of the grouping
      ratio", and the pre-dispatch measurement had already refuted it before
      anything was run.** Addresses fall almost exactly linearly: 0.103 of the
      vocabulary gives 0.095 of the addresses, and 0.325 gives 0.320. The
      square would be right if every pair of words occurred; in a Zipfian
      corpus the observed pairs are a thin subset of vocab², and merging words
      mostly merges pairs that already shared a member. Corrected here rather
      than scored as a confirmation, because it was checked against a number
      already in hand.
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
import json
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
from openplexus.tasks.corpus import (  # noqa: E402
    build_stream, characters, words)

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
KINDS = ("nostore", "concept", "stratified", "context", "current",
         "permuted", "shuffled")
#: Which coordinate of the pair key each arm groups. `context` and `current`
#: carry the SAME learned grouping as `concept` and differ only in where it is
#: applied, so a difference between the three is about the coordinate and not
#: about the clustering -- which is what makes them worth running beside it.
COORDINATES = {"context": "context", "current": "current"}
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
#: THE BRAKE, AND THE MECHANISM REQUIRES ONE. Not a tuning knob.
#:
#: The first concept cell overflowed to NaN, and pair keys over surfaces never
#: did. The reason is the defect itself: almost every address was written once,
#: so the sparsity that made the model useless was also the only thing holding
#: the store's norm down. Collapse the address space -- the point of the
#: proposal -- and the same key is written tens of times against the model's own
#: defaults of `decay=1.0` and no cap. `|Wo|` reached 1.6e63.
#:
#: Probed at 20,000 words before the sweep, concept-128 against a 10.759
#: uniform:
#:
#:     no brake          36.2      decay 0.99    10.781
#:     decay 0.999       36.0      cap 5.0       10.500   <- the only arm alive
#:     decay 0.997       39.6      cap 5 + decay 10.501
#:
#: **A cap holds where decay does not, and decay on top of it buys nothing.**
#: That is the right shape as well as the working one: decay shortens the store's
#: window, which throws away the recurrence the grouping just bought, where a cap
#: bounds the norm and keeps it. `cap 0` -- the model's own default -- is run as
#: its own arm so the divergence is RECORDED rather than assumed.
CAP = 5.0
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


def min_count_for(units: str) -> int:
    """The rarity threshold for a unit, defined ONCE.

    20 for characters -- `corpus.MIN_COUNT`, and what every character-level
    number in this project used -- and 10 for words. Shared between the corpus
    and the condition string so a record cannot say `min10` for a run that used
    20, which it did until this existed.
    """
    return MIN_COUNT if units == "words" else 20


def corpus(units: str = "words"):
    """The stream, at whichever unit.

    **Characters are here for one question and it is not this script's.**
    Decision 137: at character level the model reaches 5.17 against a 6.00
    uniform and that looks like the store working, but every character-level run
    had `readout_bias` OFF -- so the store's contribution there was never
    measured against a model that could express a prior. The same `nostore` arm,
    at the same unit, is what settles it, and running it through this harness
    means the calibration procedure and the bars are the ones already in use
    rather than a second implementation.

    `min_count` follows the unit: 20 for characters, which is `corpus.MIN_COUNT`
    and what every character-level number used, and 10 for words.
    """
    text = (Path(__file__).resolve().parent.parent
            / "data" / "tinyshakespeare.txt").read_text(encoding="utf-8")
    return build_stream(text, test_share=0.1, min_count=min_count_for(units),
                        units=words if units == "words" else characters)


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
    if kind in ("floor", "nostore"):
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
    if kind in ("concept", "context", "current"):
        # The SAME grouping. These three differ only in which coordinate of the
        # pair key it is applied to, which is what makes a difference between
        # them readable as being about the coordinate.
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


def addressing(stream: np.ndarray, surfaces, vocab: int,
               coordinates: str = "both",
               keys: str = "pair") -> tuple[int, float]:
    """Distinct pair addresses in the training stream, and mean recurrence.

    **The rail.** The whole proposal is that grouping collapses the address space
    and raises recurrence; if these two numbers do not move, nothing else in the
    row means anything. Computed from the stream rather than from the model, so
    a bug in the wiring cannot make them agree with the accuracy.

    `coordinates` mirrors `ByConcept`: a one-sided arm groups one half of the
    pair, so counting both halves would report a collapse it did not make.
    """
    grouped = np.asarray([surfaces.of(int(t)) for t in stream])
    if keys == "single":
        # A single key addresses ONE token, so the address space is the
        # vocabulary rather than the pairs in it. Counting pairs here would
        # report a collapse the store never sees and make the rail read the
        # wrong mechanism entirely.
        addresses = len(set(grouped[:-1].tolist()))
        return addresses, (len(stream) - 1) / max(addresses, 1)
    # Shifted exactly as `ByConcept` shifts, so a concept and a surface cannot
    # be counted as one address here while being two in the model.
    shifted = grouped + (0 if coordinates == "both" else vocab)
    previous = shifted[:-1] if coordinates != "current" else stream[:-1]
    current = shifted[1:] if coordinates != "context" else stream[1:]
    pairs = set(zip(np.asarray(previous).tolist(),
                    np.asarray(current).tolist()))
    return len(pairs), (len(stream) - 1) / max(len(pairs), 1)


def silent(tokens: np.ndarray) -> np.ndarray:
    """A storage mask that writes NOTHING. The `nostore` ablation.

    **The control this run turned out to need.** As the learning rate falls,
    `floor` and `stratified-128` converge to the same bits per word to three
    decimals at four different rates — two different addressing schemes cannot
    agree that precisely unless what distinguishes them has stopped mattering.
    The suspicion is that the small-rate regime is one where the store barely
    contributes and the readout's bias is doing the work, which would make "the
    model learns word-level text better than we thought" a statement about a
    unigram-shaped prior rather than about the memory.

    Nothing is ever written, so every retrieval is near zero and the readout has
    only its bias. If that arm lands where the tuned floor lands, the store is
    inert there and every comparison at that rate is between two inert models.
    """
    return np.zeros(len(tokens), dtype=bool)


def collected(model, chunks, store: bool = True) -> tuple[np.ndarray,
                                                          np.ndarray]:
    """Every prediction and the word that actually came.

    The trace starts at position 1: position 0 has no previous token, so there
    is no retrieval there and scoring it would score the initialisation.
    """
    rows, wanted = [], []
    for tokens in chunks:
        trace: list[dict] = []
        model.run(tokens, trace=trace,
                  store=None if store else silent(tokens))
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
             decay: float = 1.0, cap: float = 5.0, lr: float = 0.05,
             keys: str = "pair", width: int = WIDTH,
             units: str = "words", key_scale: float = 1.0) -> dict:
    started = time.time()
    built = built or corpus(units)
    stream = built.train[0][:TRAIN_WORDS]
    surfaces, index = surfaces_for(kind, k, built.vocab_size, stream, seed)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        d_model=width, vocab_size=built.vocab_size, seed=seed,
        derived_keys=True, context_keys=(keys == "pair"), readout_bias=bias,
        decay=decay, memory_cap=cap, lr=lr, key_scale=key_scale))
    coordinates = COORDINATES.get(kind, "both")
    if kind not in ("floor", "nostore"):
        # The store is addressed by concept; `model.surfaces` keeps routing
        # consistent with it for the partitioned path, which is off here.
        model.key_source = ByConcept(model.key_source, surfaces,
                                     built.vocab_size,
                                     coordinates=coordinates)
        model.surfaces = surfaces
    model.content = index

    # CALIBRATION TEXT IS HELD OUT OF TRAINING, not split off the chunk list.
    # g10-01 fitted a temperature inside the training region and reported 37
    # bits per character.
    cut = int(len(stream) * 0.8)
    training = pieces((stream[:cut],), CHUNK)
    calibration = pieces((stream[cut:],), CHUNK)
    writes = kind != "nostore"
    for _ in range(EPOCHS):
        for piece in training:
            model.run(piece, piece, np.ones(len(piece), bool), learn=True,
                      store=None if writes else silent(piece))

    fit_scores, fit_targets = collected(model, calibration, writes)
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
        test_scores, test_targets = collected(model, test_chunks, writes)
        error = round(bits(test_scores, test_targets, temperature), 4)
    # WHAT A SETTING MAY BE CHOSEN BY. `error` is measured on the test text, so
    # picking a learning rate or a K by it would be selection on the thing being
    # reported. This is the same quantity on the held-out TRAINING text the
    # temperature is already fitted on, and it is the only number a calibration
    # run is allowed to sort by.
    fit_error = (float("nan") if diverged
                 else round(bits(fit_scores, fit_targets, temperature), 4))
    # FINITE AND USELESS IS THE CASE THAT ALMOST GOT THROUGH.
    #
    # concept-128 at lr 0.005 and no cap returned 36.9 bits against a 10.759
    # uniform: no NaN anywhere, so `diverged` was False and a number went into
    # the table. A calibrated model cannot be much worse than uniform unless it
    # is unstable -- the temperature would flatten it -- so being above uniform
    # at all says the calibration text and the test text disagree about what
    # this model does, and the cell is not a measurement.
    uniform = float(np.log2(built.vocab_size))
    unstable = bool(not diverged and error > uniform + 0.05)
    addresses, recurrence = addressing(stream, surfaces, built.vocab_size,
                                       coordinates, keys)
    unigram, bigram = counting_bars(built.vocab_size, stream, test_chunks)
    return dict(
        arm=f"{kind}-{k}" if kind != "floor" else "floor",
        kind=kind, groups=k, seed=seed, bias=bias,
        decay=decay, cap=cap, lr=lr, coordinates=coordinates, keys=keys,
        units=units, key_scale=key_scale,
        diverged=bool(diverged),
        unstable=unstable,
        error=error,
        fit_error=fit_error,
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
        uniform=round(uniform, 4),
        train_words=int(len(stream)),
        scored=int(len(test_targets)),
        index_numbers=index.numbers_held if index else 0,
        store_numbers=width * width,
        width=width,
        seconds=round(time.time() - started, 1),
        condition=f"{kind}{k if kind != 'floor' else ''}"
                  f"|bias{int(bias)}|decay{decay}|cap{cap}|lr{lr}|{keys}"
                  f"|{units}|d{width}|seed{seed}"
                  # The ACTUAL threshold, not the module constant. Characters
                  # use 20 and words 10, and a condition string that says
                  # `min10` for a run that used 20 is a mislabelled record --
                  # which is the class of defect decisions 135 and 118 are both
                  # about.
                  f"|power{POWER}|min{min_count_for(units)}|epochs{EPOCHS}")


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
            record = one_cell(kind, k, 0, built, bias=bias, cap=CAP)
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
    parser.add_argument("--decay", type=float, default=1.0,
                        help="per-step fade on the store. 1.0 is the model's "
                             "own default")
    parser.add_argument("--cap", type=float, default=5.0,
                        help="largest norm the store may reach; 0 is "
                             "unbounded, which is the model's own default and "
                             "which DIVERGES once addresses recur")
    parser.add_argument("--lr", type=float, default=0.05,
                        help="readout learning rate; 0.05 is the model's own "
                             "default")
    parser.add_argument("--groups", type=int, default=None,
                        help="one K only. For a calibration that is about "
                             "something else -- the learning rate, say -- and "
                             "should not spend the whole K axis on it")
    parser.add_argument("--width", type=int, default=WIDTH,
                        help="d_model. 'the store holds nothing useful' and "
                             "'the store is too small to hold anything useful' "
                             "are different findings and only one is about the "
                             "architecture")
    parser.add_argument("--key-scale", type=float, default=1.0,
                        dest="key_scale",
                        help="spread of the key vectors. The character-level "
                             "comparison set uses 0.5 and the model defaults to "
                             "1.0, so reproducing a character number needs it "
                             "stated rather than inherited")
    parser.add_argument("--units", choices=("words", "characters"),
                        default="words",
                        help="characters is decision 137's open question: the "
                             "store LOOKS like it works there, and it was never "
                             "measured against a model with a prior")
    parser.add_argument("--keys", choices=("pair", "single"), default="pair",
                        help="pair keys address (t-1, t); single keys address "
                             "the previous token alone, which makes the store "
                             "a BIGRAM in vector form. The bigram bar is 7.848 "
                             "against the bias-only 9.185, so this asks "
                             "whether the store can reach what it is shaped "
                             "like. g17-01 found single keys diverge at word "
                             "level -- at lr 0.05 with no cap, both of which "
                             "decision 136 replaced")
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
    sizes = (args.groups,) if args.groups is not None else GROUPS
    built = corpus(args.units)
    records = []
    for seed in seeds:
        for bias in biases:
            for kind in kinds:
                # `nostore` has no grouping to vary any more than `floor` does,
                # so it gets ONE cell rather than one per K. Without this it
                # runs the K axis five times over and returns five identical
                # rows -- four wasted cells, and a table in which the ablation
                # looks like it was measured across a sweep it never saw.
                for k in ((0,) if kind in ("floor", "nostore") else sizes):
                    record = one_cell(kind, k, seed, built, bias=bias,
                                      decay=args.decay, cap=args.cap,
                                      lr=args.lr, keys=args.keys,
                                      width=args.width, units=args.units,
                                      key_scale=args.key_scale)
                    print(f"  {record['condition']:52s} "
                          f"bits {record['error']:.4f}  "
                          f"addresses {record['addresses']}"
                          f"{'  DIVERGED' if record['diverged'] else ''}",
                          file=sys.stderr, flush=True)
                    records.append(record)
    # NOT `harness.emit`. With no `--json` it falls through to `harness.table`,
    # which reads an `accuracy` field every cell here reports bits instead of --
    # so a local run does its four minutes of work and then dies on a KeyError
    # with the results still in memory. The shared writer is right for
    # accuracy-shaped experiments and this is not one.
    if args.json:
        path = Path(args.json)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(records, indent=1), encoding="utf-8")
        print(f"wrote {len(records)} records to {path}")
    else:
        for record in records:
            print(f"{record['condition']}  bits {record['error']}  "
                  f"fit {record['fit_error']}  "
                  f"addresses {record['addresses']:,}  "
                  f"recurrence {record['recurrence']}"
                  f"{'  DIVERGED' if record['diverged'] else ''}"
                  f"{'  UNSTABLE' if record['unstable'] else ''}")
        print(f"unigram {records[0]['unigram']}  "
              f"bigram {records[0]['bigram']}  "
              f"uniform {records[0]['uniform']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
