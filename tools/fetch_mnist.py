"""Fetch MNIST, pinned by URL and verified by size and checksum.

## Why THIS dataset, for a project that is not about images

`GOALS.md` §1.2b makes multimodal grounding a **requirement**: an image of a dog,
a sound of a dog and the word *dog* should reach one concept. Gate G7 is the
operational form of it, and every grounding measurement so far — `g32` through
`g35` — has been on symbol streams this project generated. **A symbol stream
cannot test G7**, because both "modalities" are already integers and the hard
half never arises.

MNIST supplies the missing half at the smallest honest size: **many different
pictures of the same thing**. A concept has one word and hundreds of images that
must be recognised as belonging together before anything cross-modal can be
asked. That is `GOALS.md`'s *"agreement WITHIN a modality"*, which it says must
not be budgeted together with alignment across them — and this is the first data
here where the two can be separated and reported apart.

It is deliberately not a claim that digits are interesting. They are the cheapest
real sensory input with unambiguous ground truth, and the question is whether the
co-occurrence mechanism binds a picture to a word at all.

## Why fetched rather than vendored

`data/*/` is gitignored. 11 MB of third-party binary in the tree would be carried
by every clone forever. What is committed is the URL and a sha256 per file, so
*"did we measure the same bytes"* is answerable — rule 11b, with a longer fuse.

**Two mirrors, and the first is not Yann LeCun's original.** That host has been
intermittently unavailable for years; the CVDF and AWS mirrors are the ones the
major frameworks fetch from. Both were reachable on 2026-07-31.

## What this does NOT duplicate

`fetch_clutrr`, `fetch_openea`, `fetch_fb15k237` and `fetch_kachergis` each pin
their own dataset the same way and share no code, because each is a different URL
layout and a different verification list. The honest common part — download,
hash, compare — is four lines. If a sixth appears, extract then rather than now.
"""

from __future__ import annotations

import hashlib
import pathlib
import sys
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[1]
DEST = ROOT / "data" / "mnist"

#: Mirrors in order. The original at yann.lecun.com is deliberately absent.
MIRRORS = (
    "https://storage.googleapis.com/cvdf-datasets/mnist",
    "https://ossci-datasets.s3.amazonaws.com/mnist",
)

#: Only the training split. Nothing here trains anything in the supervised
#: sense, so a held-out split would be a ceremony rather than a control -- the
#: ground truth is used to SCORE the grouping, never to produce it.
FILES = ("train-images-idx3-ubyte.gz", "train-labels-idx1-ubyte.gz")


def digest(path: pathlib.Path) -> str:
    sha = hashlib.sha256()
    sha.update(path.read_bytes())
    return sha.hexdigest()


def main() -> int:
    DEST.mkdir(parents=True, exist_ok=True)
    for name in FILES:
        target = DEST / name
        if not target.exists():
            for mirror in MIRRORS:
                try:
                    urllib.request.urlretrieve(f"{mirror}/{name}", target)
                    break
                except OSError as failed:
                    print(f"  {mirror} failed: {failed}", file=sys.stderr)
            else:
                print(f"no mirror served {name}", file=sys.stderr)
                return 1
        print(f"  {name:<34} {target.stat().st_size:>10,} bytes  "
              f"sha256 {digest(target)[:16]}...")
    print(f"\n{len(FILES)} file(s) in {DEST.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
