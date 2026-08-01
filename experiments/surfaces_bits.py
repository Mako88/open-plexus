"""Does a hash front end match a trained one, and do two nodes agree?

Two questions, one grid, on real MNIST digits and real FSDD recordings.

**1. What the bit count buys.** More bits is a finer partition, so purity rises
for free and the number is meaningless alone — a code per item scores 1.0. Every
row therefore carries `distinct`, and every cell is matched: `kmeans` is run at
`k` equal to the number of codes the hash actually used, and `random` assigns the
same number of codes at random. **The x-axis is distinct codes, not bits.**

**2. Whether two nodes agree, which is what the hash is FOR.** Each arm is fitted
twice on disjoint samples, and a third disjoint set of items is quantised by both.
`routing` is the share of those items the two nodes send to the same code, and it
is the falsifier the front end has never been asked: if it is not 1.0 for the
hash, a write and a read go to different machines and nothing anywhere reports it.
`meaning` is the weaker statistical version — do the two nodes' codes hold the
same digit — and it is bounded by items per code rather than by the front end.

Nothing here decides anything. It emits one JSON row per cell, and the reading
goes in README §1 as one line.

    python experiments/surfaces_bits.py --json out/surfaces.json
    python experiments/surfaces_bits.py --quick        # one seed, small grid
"""

from __future__ import annotations

import argparse
import json
import pathlib
import sys
import time
from collections import Counter

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import numpy as np  # noqa: E402

from openplexus.grouping import codes as kmeans_codes  # noqa: E402
from openplexus.surfaces import (Hyperplanes, agreement,  # noqa: E402
                                 centred, cepstrum, cochlea, purity, spectra,
                                 waveform)
from openplexus.tasks import mnist, spoken  # noqa: E402

MNIST_DATA = ROOT / "data" / "mnist"
FSDD_DATA = ROOT / "data" / "fsdd"

#: How many images. Carried from g36-04, which used 4,000 against the same 3,000
#: recordings so the two modalities stay comparably sized; not swept here, and a
#: sweep of it would be measuring sample size rather than the front end.
IMAGES = 4000

#: The axis. Swept: 4 bits is coarser than ten digits and 12 is finer than the
#: 3,000-item audio set, so the grid brackets the useful range at both ends
#: rather than pinning at an edge.
BITS = (4, 6, 8, 10, 12)

#: Seeds. Three is the minimum this project accepts and it is chosen here as the
#: floor: the effects being read are large, and a cell whose arms differ by less
#: than the seed spread is reported as unresolved rather than as a result.
SEEDS = (0, 1, 2)

ARMS = ("lsh", "lsh-raw", "kmeans", "random")


def quantise_random(rows: np.ndarray, k: int, seed: int, node: int) -> list[int]:
    """DOING NOTHING, at the same code count. The floor every purity clears.

    **The draw depends on the node, and getting that wrong made the control
    read as the mechanism.** The first version of this seeded both nodes
    identically, so two nodes assigning codes with no reference to the data
    whatsoever agreed on 100% of the shared items — a routing column of 1.0000
    for a front end that is not one. It is exactly the failure `CLAUDE.md`
    records for controls: a control that removes the precondition for the thing
    it is testing, and then reports it absent.

    Seeded per node, two independent draws agree at 1/k, which is what the
    routing column has to be read against.
    """
    return [int(c) for c in
            np.random.default_rng([seed, node]).integers(k, size=len(rows))]


def fit_and_code(arm: str, mine: np.ndarray, shared: np.ndarray, bits: int,
                 k: int, seed: int, node: int) -> tuple[list[int], list[int]]:
    """One node's codes for its own items and for the shared ones.

    The shared items are quantised ALONGSIDE the node's own data rather than
    afterwards, because `cluster` has no out-of-sample assignment — which is
    itself part of what is wrong with it as a front end, and is why the
    comparison is arranged to be generous to it here.

    **`seed` is the same on both nodes for every arm that is allowed one.** That
    is the shared constant C1 permits, and handing it to k-means as well is the
    generous reading: the disagreement it shows is not a different seed, it is a
    different sample.
    """
    if arm in ("lsh", "lsh-raw"):
        rows = mine if arm == "lsh-raw" else centred(mine)
        with_shared = shared if arm == "lsh-raw" else centred(shared)
        hashed = Hyperplanes(rows.shape[1], bits=bits, seed=seed)
        return hashed.codes(rows), hashed.codes(with_shared)
    both = np.vstack([mine, shared])
    assigned = (kmeans_codes(both, k, seed) if arm == "kmeans"
                else quantise_random(both, k, seed, node))
    return assigned[:len(mine)], assigned[len(mine):]


def cell(arm: str, rows: np.ndarray, labels: list[int], bits: int, k: int,
         seed: int, split: np.random.Generator) -> dict:
    """One arm at one bit count and one seed, both nodes, every column."""
    order = split.permutation(len(rows))
    half = len(order) // 2
    #: A tenth of the data is held out to be quantised by BOTH nodes. Chosen
    #: here: large enough that the routing share has a small error bar, small
    #: enough that neither node's own sample is materially reduced.
    keep = len(order) // 10
    a, b, shared = order[:half - keep], order[half:-keep], order[-keep:]

    began = time.time()
    mine, my_shared = fit_and_code(arm, rows[a], rows[shared], bits, k, seed, 0)
    yours, your_shared = fit_and_code(arm, rows[b], rows[shared], bits, k,
                                      seed, 1)

    scored = [(x, y) for x, y in zip(my_shared, your_shared) if x >= 0 and y >= 0]
    routing = (sum(x == y for x, y in scored) / len(scored)) if scored else 0.0

    share, my_major = purity(mine, [labels[i] for i in a])
    _, your_major = purity(yours, [labels[i] for i in b])
    held = Counter(c for c in mine if c >= 0)
    meaning, common = agreement(my_major, your_major, held)

    distinct = len(set(c for c in mine if c >= 0))
    return {
        "arm": arm, "bits": bits, "seed": seed,
        "distinct": distinct,
        # THE AXIS THE ARMS HAVE TO BE READ ON. Purity rises for free as codes
        # get finer -- one item per code scores 1.0 -- so a front end is only
        # ahead if it is ahead at the same items per code.
        "per_code": len(mine) / max(distinct, 1),
        "covered": sum(c >= 0 for c in mine) / len(mine),
        "purity": share, "routing": routing, "meaning": meaning,
        "shared_codes": common, "seconds": round(time.time() - began, 2),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None,
                        help="write one JSON row per cell here")
    parser.add_argument("--quick", action="store_true",
                        help="one seed and three bit counts, for a smoke run")
    parser.add_argument("--images", type=int, default=IMAGES)
    args = parser.parse_args()

    leftovers = sorted(ROOT.glob("**/*.py.bak"))
    if leftovers:
        raise SystemExit(
            "REFUSING TO RUN: tools/mutate.py has the source edited.\n"
            + "\n".join(f"  {p.relative_to(ROOT)}" for p in leftovers))
    if not (MNIST_DATA / "train-images-idx3-ubyte.gz").exists():
        raise SystemExit(f"no data in {MNIST_DATA}: python tools/fetch_mnist.py")
    if not FSDD_DATA.exists():
        raise SystemExit(f"no data in {FSDD_DATA}: python tools/fetch_fsdd.py")

    started = time.time()
    digits = mnist.read(MNIST_DATA, limit=args.images)
    pixels = (np.frombuffer(b"".join(digits.images), dtype=np.uint8)
              .reshape(len(digits), digits.pixels).astype(np.float64))
    # Every recording, not a prefix -- `spoken.available` is sorted by digit and
    # a prefix of it is one digit, which once produced a chance level of 0.20.
    paths = spoken.available(FSDD_DATA)
    heard = [spoken.read(path) for path in paths]

    modalities = (
        ("image", pixels, list(digits.labels)),
        ("audio", spectra(heard), [u.digit for u in heard]),
        # THE SAME RECORDINGS WITH NO TRANSFORM. The spectrum is a borrowed
        # feature, and a front end that needs no training should be asked
        # whether it needs that either. Read against `audio` row for row: same
        # items, same labels, same hash, different input.
        ("audio-wave", waveform(heard), [u.digit for u in heard]),
        # AND THE SAME RECORDINGS THROUGH AN EAR-SHAPED BANK. Log-spaced bands
        # on fixed overlapping windows, then the same with the level removed
        # per frame. Both are fixed transforms of one item, so neither brings
        # back the thing k-means was dropped for.
        ("audio-ear", cochlea(heard), [u.digit for u in heard]),
        ("audio-cepstrum", cepstrum(cochlea(heard)), [u.digit for u in heard]),
    )
    bits = BITS[1::2] if args.quick else BITS
    seeds = SEEDS[:1] if args.quick else SEEDS

    rows: list[dict] = []
    for name, data, labels in modalities:
        floor = max(Counter(labels).values()) / len(labels)
        print(f"\n{name}: {len(data)} items, {len(set(labels))} classes, "
              f"one code for everything scores {floor:.4f}")
        header = (f"{'bits':>5}{'arm':>10}{'distinct':>10}{'per_code':>10}"
                  f"{'purity':>9}{'routing':>9}{'meaning':>9}{'shared':>8}"
                  f"{'sec':>7}")
        print(header)
        print("-" * len(header))
        for width in bits:
            for seed in seeds:
                # `k` IS SET BY WHAT THE HASH ACTUALLY USED, on this split and
                # not on the whole set, so every arm in this cell partitions
                # into the same number of codes. Matching the granularity is
                # the only way the purity column compares two front ends rather
                # than two dial settings.
                k = 1
                for arm in ARMS:
                    row = cell(arm, data, labels, width, k, seed,
                               np.random.default_rng(seed))
                    if arm == "lsh":
                        k = max(row["distinct"], 1)
                    row["modality"] = name
                    row["floor"] = floor
                    row["k"] = k
                    rows.append(row)
                    print(f"{width:>5}{arm:>10}{row['distinct']:>10}"
                          f"{row['per_code']:>10.1f}{row['purity']:>9.4f}"
                          f"{row['routing']:>9.4f}{row['meaning']:>9.4f}"
                          f"{row['shared_codes']:>8}{row['seconds']:>7.1f}")

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"\n{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
