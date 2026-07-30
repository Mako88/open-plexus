"""Fetch FB15k-237, pinned by URL and verified by size and checksum.

## What this does not duplicate

`tools/fetch_clutrr.py` and `tools/fetch_openea.py` are the pattern and this follows it
exactly: pin the URL, record size and sha256 on the first fetch, verify on every fetch
after. They fetch different corpora and share no code beyond that shape, which is a
handful of lines of `urlretrieve` and `hashlib` -- extracting it would put a fetcher's
error messages one indirection away from the fetcher, and `check_duplication.py` is the
guard if that judgement is wrong.

**No new parser either.** FB15k-237 ships as tab-separated `(subject, relation, object)`,
which is byte-for-byte the shape `tools/invariant_dimension.py --graph` and
`tools/relation_contrastive.py --graph` already read for OpenEA. So this is a new corpus
through existing instruments rather than a new code path.

## Why THIS benchmark

Kill-list item 3 is *"does a graph database or symbolic system already do this"*, and John's
instruction is to be delighted when a conventional system wins cheaply. **Every opponent
this project has beaten so far is trivial** -- majority class, counting, random filling, an
untrained copy of itself. It has never been compared to a real system.

FB15k-237 is the standard knowledge-graph completion benchmark and carries a decade of
published baselines, so the comparison exists without anyone here having to implement a
rival and get it wrong. It also extends `g23-03`'s question -- does the contrastive margin
track a graph's invariant dimension -- to a seventeenth graph from a different source.

**The published numbers are for LINK PREDICTION** (given a head and relation, rank the
tail). This project's measurement is RULE PREDICTION (given two composed relations, name
the third). They are different tasks on the same data and a number from one is not a number
from the other. Anything quoted across them has to say so.

## Provenance

Fetched under John's standing permission for benchmark data, 2026-07-30. The mirror is
pinned by commit-free raw URL, so **the checksum is the identity**, not the path -- rule
11b's concern, since a benchmark someone re-downloaded a different version of is the stale
artifact failure with a longer fuse.
"""

from __future__ import annotations

import hashlib
import sys
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
BASE = ("https://raw.githubusercontent.com/villmow/"
        "datasets_knowledge_embedding/master/FB15k-237/")
SPLITS = ("train", "valid", "test")

#: Size in bytes and sha256, recorded from the first fetch on 2026-07-30. Written after
#: the bytes arrived rather than before: a checksum invented in advance is a checksum of
#: nothing, and the fetcher says so loudly while this is empty.
EXPECTED: dict[str, tuple[int, str]] = {
    "train": (21005177,
              "61099230e4439f90885ca9767739e31e8e32f54736fa1c35952b27997bc7c08a"),
    "valid": (1285566,
              "749cbe9d923bac7b9354da5614ecfed2e0220256d442c3e04a6b303db1f273d9"),
    "test": (1499033,
             "e2e35e8e6113de220140b6f44dc71a5207b0fc6872d575e874aefe13259b655b"),
}


def digest(path: Path) -> tuple[int, str]:
    sha = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1 << 20), b""):
            sha.update(block)
    return path.stat().st_size, sha.hexdigest()


def main() -> int:
    root = ROOT / "data" / "fb15k237"
    root.mkdir(parents=True, exist_ok=True)
    for split in SPLITS:
        path = root / f"{split}.txt"
        if not path.exists():
            url = f"{BASE}{split}.txt"
            print(f"fetching {url}", file=sys.stderr)
            urllib.request.urlretrieve(url, path)      # noqa: S310 - pinned URL
        size, sha = digest(path)
        print(f"  {split:6s} {size:>10,} bytes  sha256 {sha[:16]}...")
        want = EXPECTED.get(split)
        if want is None:
            continue
        if (size, sha) != want:
            raise SystemExit(
                f"{split}.txt is {size} bytes / {sha}, expected {want[0]} / {want[1]}. "
                f"**Do not measure on this.** A benchmark whose bytes changed is not the "
                f"benchmark any earlier number was taken on, and the difference will not "
                f"announce itself in a score.")
    if not EXPECTED:
        print("\nEXPECTED is empty: this was the first fetch and the digests above are "
              "the record. Paste them in before anything is measured, or the next "
              "fetch verifies nothing.", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
