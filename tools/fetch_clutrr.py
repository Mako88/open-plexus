"""Fetch CLUTRR, pinned by URL and verified by size and checksum.

## Why this exists rather than the data being committed

**The standing gap this closes:** every instrument in this project is self-designed,
so *"until an external benchmark runs, this project is grading its own homework."*
[Note 058](../docs/archive/notes/058-real-co-occurrence-has-no-cliff.md) put a number on what
that costs — real word co-occurrence carries ~7x less similarity spread than the
synthetic task the answer mechanism was built on, and a slope where the task has a
cliff — which moved an external benchmark from a completeness item to the measurement
that says whether the answer line means anything.

The data is **fetched, not vendored.** 10 MB of third-party CSV in the tree would be
carried by every clone forever, and the alternative costs one command. What IS
committed is this script: the exact URLs, and a checksum per file so *"did we measure
the same bytes"* is answerable. **That is rule 11b's concern** — a run's identity has
to be verifiable from the data, and a benchmark someone re-downloaded a different
version of is the stale-artifact failure with a longer fuse.

## Which configuration, and why this one

`gen_train23_test2to10` — **train on 2- and 3-hop chains, test on 2 through 10.**

That is systematic generalisation to unseen depths, and it is the direct external
analogue of a number this project already has: decision 92 measured the gate
generalising zero-shot to a depth it never trained on, at 0.992, on `chains.py` — a
task this project wrote itself, with out-degree 1 by construction (decision 108).
CLUTRR asks the same question over graphs someone else designed, with composition
rules written by crowd-workers and a deliberately partial rule table.

The other five configurations are robustness variants (irrelevant, supporting and
disconnected noise facts) and are one edit away. They are not first because noise
robustness is a second question and this one is the comparison that exists.

## The layer we use, stated so it is not mistaken for the harder claim

CLUTRR ships each puzzle **twice**: as crowd-authored prose, and as a graph —
`edge_types`, `story_edges`, `query_edge`, `target_text`. **This project uses the
graph.**

That is deliberate and it is a real narrowing. The substrate addresses token ids, not
sentences, so parsing the prose would measure a text front-end this project does not
have and is not trying to build (GOALS §2: text as input is fine, text-prediction as
the score is not). Using the relational layer takes CLUTRR's *reasoning* content
without its *language* content.

**So any result here is "CLUTRR-symbolic", never "CLUTRR"**, and must say so. The
published numbers are on the text task and are not comparable.
"""

from __future__ import annotations

import hashlib
import pathlib
import sys
import urllib.request

BASE = ("https://raw.githubusercontent.com/kliang5/"
        "CLUTRR_huggingface_dataset/main")
CONFIG = "gen_train23_test2to10"
SPLITS = ("train", "validation", "test")
TARGET = pathlib.Path(__file__).resolve().parent.parent / "data" / "clutrr"

#: Size in bytes and sha256, recorded from the first fetch on 2026-07-29. A
#: mismatch means the upstream file changed, which is a thing to know BEFORE a
#: measurement rather than after — the benchmark moving under a result is the same
#: class of failure as rule 11b's stale download.
EXPECTED = {
    "train": (6618113,
              "38963d01a20789acdd38bc4a338f59070457928072f018657c9cb59e6d7447de"),
    "validation": (1445724,
                   "418d712579e6c9bb3f0e835cb0971372c06bc7816ea97c5e2ac5f24d9b896e51"),
    "test": (1935837,
             "d49c7cbe4f575b48b279313e706ef43e127bfa85b0e860db94fdeb617aea07e9"),
}


def digest(path: pathlib.Path) -> str:
    sha = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1 << 20), b""):
            sha.update(block)
    return sha.hexdigest()


def main() -> int:
    out = TARGET / CONFIG
    out.mkdir(parents=True, exist_ok=True)
    problems: list[str] = []
    for split in SPLITS:
        path = out / f"{split}.csv"
        if not path.exists():
            url = f"{BASE}/{CONFIG}/{split}.csv"
            print(f"fetching {url}")
            urllib.request.urlretrieve(url, path)          # noqa: S310 - pinned URL
        size = path.stat().st_size
        want_size, want_sha = EXPECTED[split]
        got_sha = digest(path)
        print(f"  {split:11s} {size:>9,} bytes  sha256 {got_sha[:16]}...")
        if size != want_size:
            problems.append(
                f"{split}.csv is {size} bytes, expected {want_size}")
        if got_sha != want_sha:
            problems.append(
                f"{split}.csv sha256 is {got_sha}, expected {want_sha}")
    if problems:
        for problem in problems:
            print(f"FAIL fetch_clutrr: {problem}")
        print("\nThe upstream file changed. A measurement taken against it is not "
              "comparable to one taken before, and that is worth knowing BEFORE "
              "the run rather than after -- the benchmark moving under a result is "
              "rule 11b's stale download with a longer fuse. Delete the local copy "
              "and re-fetch deliberately, then update EXPECTED in the same commit "
              "as the numbers that used it.")
        return 1
    print(f"\nok - {CONFIG} verified in {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
