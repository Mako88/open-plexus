"""Raw input to a surface id, with nothing trained and nothing shared but a seed.

README §1 is *"how raw data becomes an id that can be counted"*, and until now the
answer was spherical k-means over the whole vocabulary of vectors. That answer has
two problems and only one of them is about accuracy.

## Why the trained quantiser had to go

**Clustering by similarity is an identity assignment**, and README §3 gives
identity to the walk: *"a concept is what you reach by walking"*. A front end that
decides two pictures are the same thing has already answered the question the
counting is supposed to answer, and it answers it invisibly — a good clusterer
makes everything downstream look better rather than worse.

**And two nodes fitting k-means to different samples get different codes.** Not
different labels for the same partition, which a permutation would fix: different
partitions, so surface 7 means one thing on this machine and another thing on
that one. Nothing in the system detects it — a write and a read simply go to
different places and the count that should have accumulated never does.
`tests/test_surfaces.py` measures that disagreement rather than asserting it,
because the claim is about behaviour.

A hash has neither problem. `signature` is a function of the planes alone, and the
planes are a function of `(width, bits, seed)`. Two nodes handed the same three
numbers cannot disagree, and the constant is handed out once and frozen, which is
what C1 permits.

## What this buys and what it costs

**Bits are the granularity dial.** Two inputs collide with probability
`(1 - theta/pi) ** bits` (Charikar 2002), so the same mechanism gives a coarse
bucket at few bits and a fine one at many — README §3's *"identity at more than one
grain"*, from one knob rather than a hierarchy bolted on.

**Over-segmentation is repairable; under-segmentation is not.** If the hash splits
one digit across fifty buckets, the buckets all co-occur with the same word and
`grounding.equivalence_classes` can merge them. If it puts two digits in one
bucket, nothing downstream can separate them again. So the front end wants to be
fine and stable rather than good, which is a hash and not a classifier.

**The honest cost.** A random hyperplane knows nothing about the data, so it
cannot spend its bits where the variance is. k-means can. Whether that costs
purity at a matched code count is not an argument, it is a measurement, and it is
what `experiments/surfaces_bits.py` runs — a clear loss there would say the
trained feature space was doing real work.

**And discretisation itself does not go away.** Two recordings of *six* share
almost no bytes, so something has to put perceptually near things near each other
or every count stays 1 and no statistic can form (README §1, the ruled-out
option). This replaces the *trained, global, semantic* part. It does not escape
the requirement.

## What this does NOT duplicate, and what was searched

Searched by capability — hyperplane, random projection, quantise, discretise,
code, bucket, front end — across `openplexus/`, `tools/`, `tests/` and `testbed/`.

- **`openplexus/sketch.py` had the only hyperplane hash in the tree**, and it is
  not copied: `AddressSketch` now takes its planes from `Hyperplanes` here, so
  the bit packing has one implementation. What that module does with a signature
  is a different question — *was this address written* — and stays there.
- **`openplexus/grouping.py`** is what this replaces on the input path. It is
  kept, because it is the arm this is measured against and README §1 records
  k-means as ruled out rather than deleted.
- **`openplexus/tasks/mnist.py` and `spoken.py`** deliberately refuse to hold a
  quantiser, so the ruler cannot depend on the mechanism it measures. `spectra`
  is here for exactly that reason: it is numpy, it is a model-layer choice, and
  `spoken.py` names this file's role in its own docstring.
"""

from __future__ import annotations

import numpy as np

#: Spectral summary shape: time segments by frequency bands. **Deliberately
#: crude**, and carried unchanged from g36-04, where it was chosen as the audio
#: counterpart of raw pixels — the point is a front end whose quality is known
#: and reported, not a good one. Swept nowhere; a sweep of it would be measuring
#: the features rather than the hash.
SEGMENTS, BANDS = 8, 16


class Hyperplanes:
    """`bits` random hyperplanes through the origin, and the code they give.

    Everything here is a function of `(width, bits, seed)`. Nothing is fitted,
    nothing is stored between calls, and no input a node has seen can change the
    code another node gives the same input — which is the property the trained
    quantiser could not have and the reason this exists.

    Attributes:
        width: How long an input vector must be.
        bits: How many planes, and so how fine the partition is.
        seed: The shared constant. Two nodes agree if and only if this matches.
    """

    def __init__(self, width: int, bits: int = 8, seed: int = 0) -> None:
        if width < 1:
            raise ValueError("a plane needs at least one dimension to cut")
        if bits < 1:
            raise ValueError("bits must be at least 1")
        if bits > 62:
            # The signature is packed into a Python int via shifts; past this
            # the arithmetic is still correct but the buckets outnumber any
            # plausible number of inputs, so every input is its own code and
            # nothing recurs -- which is the failure README §1 records for not
            # discretising at all.
            raise ValueError("bits above 62 buys nothing: every input would "
                             "get its own code and no count could recur")
        self.width = width
        self.bits = bits
        self.seed = seed
        self._planes = np.random.default_rng(seed).normal(
            size=(bits, width)) / np.sqrt(width)

    def signature(self, vector: np.ndarray) -> int:
        """The raw sign pattern, packed into an int. **Zero is not special.**

        `AddressSketch` wants this rather than `code`: an address is an address,
        and a key that happens to be zero is still a key to it.
        """
        value = 0
        for bit in self._planes @ vector > 0.0:
            value = (value << 1) | int(bit)
        return value

    def code(self, vector: np.ndarray) -> int:
        """The surface id for one input, or `-1` for an input with no content.

        A zero vector falls on the boundary of every plane at once, so its sign
        pattern is an artefact of the tie-break rather than anything about the
        input. Returning a code would build one large surface out of *nothing was
        there* — the same refusal `grouping.cluster` makes for a zero row, and
        for the same reason.
        """
        vector = np.asarray(vector, dtype=float)
        if vector.shape != (self.width,):
            raise ValueError(f"expected a vector of width {self.width}, got "
                             f"shape {vector.shape}")
        if not np.any(vector):
            return -1
        return self.signature(vector)

    def codes(self, vectors: np.ndarray) -> list[int]:
        """One code per row, in row order. `-1` marks a row with no content.

        Batched because a sweep quantises thousands of rows and one matrix
        product is the whole cost; the result must match `code` row for row,
        which `tests/test_surfaces.py` asserts rather than assumes.
        """
        vectors = np.asarray(vectors, dtype=float)
        if vectors.ndim != 2 or vectors.shape[1] != self.width:
            raise ValueError(f"expected rows of width {self.width}, got shape "
                             f"{vectors.shape}")
        bits = (vectors @ self._planes.T) > 0.0
        weights = 1 << np.arange(self.bits - 1, -1, -1)
        assigned = (bits * weights).sum(axis=1)
        empty = ~np.any(vectors, axis=1)
        return [-1 if blank else int(value)
                for value, blank in zip(assigned, empty)]


def centred(rows: np.ndarray) -> np.ndarray:
    """Subtract each row's OWN mean. Per item, so nothing is shared or fitted.

    **Without this the audio front end does not work at all**, and the reason is
    the one assumption a hyperplane through the origin makes: it cuts by ANGLE.
    Measured on 3,000 FSDD recordings at `SEGMENTS x BANDS`, the mean pairwise
    cosine between two spectra is **0.990 with a standard deviation of 0.006** —
    every recording points almost exactly the same way, because a log-energy
    spectrum is all-positive and dominated by a common offset. A random plane
    through the origin therefore puts nearly all of them on the same side, and
    the sweep measures it: 10 bits gave **6 distinct codes over 3,000
    recordings** and a purity of 0.157 against a chance of 0.100.

    Subtracting the row mean takes that offset out and leaves the shape. The same
    3,000 rows then have mean cosine 0.661 with standard deviation 0.153, and 10
    bits gives 265 codes at purity 0.466.

    **It is a per-item function and nothing else**, which is what makes it legal
    where a fitted mean would not be: two nodes need exchange nothing, and an
    arriving item is centred by itself with no reference to any other. A mean
    taken over a node's data would be a statistic of that node's sample, and two
    nodes would disagree — which is the failure this whole module exists to
    remove.

    Images are less affected and it still pays: MNIST at 8 bits goes 0.406 to
    0.421. `experiments/surfaces_bits.py` runs it as an axis rather than baking
    it in.
    """
    rows = np.asarray(rows, dtype=float)
    return rows - rows.mean(axis=-1, keepdims=True)


def spectra(utterances, segments: int = SEGMENTS,
            bands: int = BANDS) -> np.ndarray:
    """Log energy in `bands` frequency bands across `segments` time segments.

    Carried from g36-04 unchanged, where it lived in the sweep and was the copy
    `tools/check_duplication.py` exists to refuse once a second caller wanted it.

    Recordings differ in length, so the segments are proportional rather than
    fixed — which throws away speaking RATE and keeps the spectral shape. That is
    a real loss and is the honest crude choice: a fixed window would instead make
    a long recording a different feature vector from a short one saying the same
    word, which is worse for this question.

    Args:
        utterances: Anything with a `samples` sequence, as `spoken.read` returns.
    """
    rows = []
    for utterance in utterances:
        signal = np.asarray(utterance.samples, dtype=np.float64)
        if len(signal) < segments * 2:
            signal = np.pad(signal, (0, segments * 2 - len(signal)))
        row = []
        for segment in np.array_split(signal, segments):
            magnitude = np.abs(np.fft.rfft(segment * np.hanning(len(segment))))
            edges = np.linspace(0, len(magnitude), bands + 1).astype(int)
            row.extend(np.log1p(magnitude[a:b].sum())
                       for a, b in zip(edges[:-1], edges[1:]))
        rows.append(row)
    return np.asarray(rows)


def waveform(utterances, width: int = 2048) -> np.ndarray:
    """The raw samples, stretched to one fixed length. **No transform at all.**

    Exists to answer a question that would otherwise be argued: the spectrum is
    a BORROWED feature, and a front end that claims to need no training should
    be asked whether it needs that either. It does.

    Measured on 3,000 FSDD recordings, mean pairwise cosine between two
    waveforms is **-0.000 with a standard deviation of 0.054** — two recordings
    are as near to orthogonal as two random vectors, because nothing aligns them
    in time. A hyperplane cutting by angle therefore has no angle to cut, and
    the hash behaves as a random assignment: at matched items per code it scores
    **0.254 against the spectrum's 0.466** (about 12 items per code), and the
    0.728 it reaches at 12 bits is bought at 1.5 items per code, where nothing
    recurs and no count can form.

    So the requirement does not go away by refusing to borrow: **something has to
    put perceptually near things near each other**, and for sound it is not the
    samples. That is a claim about the input, not about the hash.

    Stretched rather than windowed for the same reason `spectra` segments
    proportionally: a fixed window makes a long recording a different vector from
    a short one saying the same word.
    """
    rows = []
    for utterance in utterances:
        signal = np.asarray(utterance.samples, dtype=np.float64)
        if len(signal) < 2:
            signal = np.pad(signal, (0, 2 - len(signal)))
        rows.append(np.interp(np.linspace(0, len(signal) - 1, width),
                              np.arange(len(signal)), signal))
    return np.asarray(rows)


def _mel(hertz: np.ndarray) -> np.ndarray:
    return 2595.0 * np.log10(1.0 + hertz / 700.0)


def _hertz(mel: np.ndarray) -> np.ndarray:
    return 700.0 * (10.0 ** (mel / 2595.0) - 1.0)


def cochlea(utterances, frames: int = 16, bands: int = 24,
            lowest: float = 50.0) -> np.ndarray:
    """Log-spaced bands on fixed overlapping windows. **What an ear roughly does.**

    John's question, 2026-07-31: is there an equivalent to how ears hear that
    could be emulated? There is, and this is it — the basilar membrane resolves
    frequency roughly logarithmically, so the bands are spaced on the mel scale
    rather than evenly, and the energy is log-compressed.

    **It is a fixed physical model and not a fitted one**, which is the whole
    reason it is allowed here: like the planes, it is a constant handed out once
    and never updated, so two nodes computing it cannot disagree. Borrowing a
    *shape of ear* is not borrowing a *codebook*.

    **What it bought, measured on 3,000 FSDD recordings at about 8 per code.**
    Against `spectra`'s evenly spaced bands it moves k-means from 0.921 to 0.948
    and the hash from 0.534 to 0.496 — that is, it improves the front end that
    can spend its resolution where the data is, and does nothing for the one that
    cannot. `cepstrum` on top reaches 0.974 and 0.539.

    **So the hash's deficit is not the feature space.** In every representation
    with any structure in it, the trained quantiser is about 0.44 ahead; on raw
    waveform, where there is none, both fall to the floor. What separates them is
    where the codes are SPENT, and that is the one thing a data-free front end
    cannot decide.
    """
    rows = []
    for utterance in utterances:
        signal = np.asarray(utterance.samples, dtype=np.float64)
        edges = _hertz(np.linspace(_mel(np.array(lowest)),
                                   _mel(np.array(utterance.rate / 2.0)),
                                   bands + 1))
        # Windows overlap and are a fixed fraction of the recording, so a slow
        # speaker and a fast one are compared frame for frame -- the same
        # proportional choice `spectra` makes, and for the same reason.
        window = max(int(len(signal) / frames * 2), 8)
        row = []
        for start in np.linspace(0, max(len(signal) - window, 0),
                                 frames).astype(int):
            segment = signal[start:start + window]
            if len(segment) < window:
                segment = np.pad(segment, (0, window - len(segment)))
            power = np.abs(np.fft.rfft(segment * np.hanning(len(segment)))) ** 2
            hertz = np.fft.rfftfreq(len(segment), 1.0 / utterance.rate)
            for low, high in zip(edges[:-1], edges[1:]):
                inside = (hertz >= low) & (hertz < high)
                row.append(float(np.log1p(power[inside].sum()))
                           if inside.any() else 0.0)
        rows.append(row)
    return np.asarray(rows)


def cepstrum(rows: np.ndarray, bands: int = 24, keep: int = 13) -> np.ndarray:
    """A DCT across each frame's bands, with the first coefficient DROPPED.

    The standard speech front end, and it earns its place here for one specific
    reason rather than for being standard: **coefficient zero IS the frame's mean
    level**, so dropping it removes the common offset per frame — a finer version
    of what `centred` does per item, and the offset is exactly what stops a
    hyperplane through the origin from cutting anything.

    Both steps are fixed transforms of a single item. Nothing is estimated from a
    corpus, so nothing has to be kept in sync.

    Args:
        rows: `cochlea` output, one row per item, frames concatenated.
    """
    rows = np.asarray(rows, dtype=float)
    if rows.shape[1] % bands:
        raise ValueError(f"a row of {rows.shape[1]} does not divide into "
                         f"{bands} bands per frame")
    frames = rows.shape[1] // bands
    grid = rows.reshape(len(rows), frames, bands)
    index = np.arange(bands)
    basis = np.cos(np.pi * (index[None, :] + 0.5) * index[:, None] / bands)
    return (grid @ basis.T)[:, :, 1:keep + 1].reshape(len(rows), -1)


def purity(assigned: list[int], labels: list[int]) -> tuple[float, dict[int, int]]:
    """Share of items sitting in a code whose MAJORITY label is their own.

    The *agreement WITHIN a modality* half of the identity question, which must
    not be budgeted together with alignment ACROSS modalities. Carried from the
    deleted `experiments/harness.py`, where two sweeps shared it.

    **It rises trivially as codes get finer** — one item per code scores 1.0 —
    so no caller may report it without the number of distinct codes beside it,
    and a random assignment into the same number of codes is the floor it has to
    beat. `experiments/surfaces_bits.py` prints all three.

    Returns:
        `(share, majority)` — the share, and each code's majority label, which
        callers need in order to score anything against the codes.
    """
    from collections import Counter

    holders: dict[int, Counter] = {}
    for code, label in zip(assigned, labels):
        if code >= 0:
            holders.setdefault(code, Counter())[label] += 1
    majority = {code: counts.most_common(1)[0][0]
                for code, counts in holders.items()}
    agreed = sum(counts[majority[code]] for code, counts in holders.items())
    total = sum(sum(counts.values()) for counts in holders.values())
    return (agreed / total if total else 0.0), majority


def agreement(one: dict[int, int], other: dict[int, int],
              weights: dict[int, int] | None = None) -> tuple[float, int]:
    """Do two nodes' codes MEAN the same thing? The falsifier, computed.

    Each argument is a node's `code -> majority label` map, fitted on that node's
    own sample. A code both nodes used agrees when both nodes' data says it holds
    the same thing.

    **Label permutation is not the question.** k-means could be forgiven for
    numbering its clusters differently, but a permutation cannot be discovered
    without exchanging data, and exchanging data is the thing C1 forbids. Two
    nodes must route surface 7 to the same place with no message passing at all,
    so the comparison is on the raw ids.

    Args:
        weights: How many items each code holds, if agreement should be weighted
            by how much traffic a disagreement would misroute. Unweighted counts
            every code once, which lets one item in a rare code count as much as
            a thousand in a common one.

    Returns:
        `(share, shared)` — the agreeing share, and how many codes both used. A
        share of 0.0 over 0 shared codes is not disagreement; it is two nodes
        with nothing in common, and the caller has to tell them apart.
    """
    shared = sorted(set(one) & set(other))
    if not shared:
        return 0.0, 0
    if weights is None:
        return sum(one[c] == other[c] for c in shared) / len(shared), len(shared)
    total = sum(weights.get(c, 0) for c in shared)
    if total == 0:
        return 0.0, len(shared)
    return (sum(weights.get(c, 0) for c in shared if one[c] == other[c])
            / total), len(shared)
