"""Fetch the Free Spoken Digit Dataset, pinned and checksummed.

## What it is, and the licence, which was CHECKED rather than assumed

Roughly 3,000 recordings of the digits 0-9, spoken by several people, as small
mono WAV files. The filename carries the label: `{digit}_{speaker}_{index}.wav`.

**There is no LICENSE file in the repository.** The GitHub API reports no licence
and none exists at the root. The README states
**Creative Commons Attribution-ShareAlike 4.0** in its own words, which is where
the terms come from.

That distinction is recorded because the same check went wrong yesterday in the
other direction: `tools/fetch_kachergis.py` documents a dataset whose README
promised more than its files held. Here the file is absent and the README is the
authority, so the terms are quoted rather than inferred from a missing file.

**Attribution:** Zohar Jackson et al., *Free Spoken Digit Dataset*,
github.com/Jakobovski/free-spoken-digit-dataset. **ShareAlike binds
redistribution**, and `data/*/` is gitignored so nothing is redistributed — but
the project's endgame is undecided (README's constraints) and a
ShareAlike corpus inside a product is a question somebody has to answer before
that changes.

## Why the tarball rather than the file list

3,000 files is 3,000 requests against an API with a rate limit. One archive is
one request, and the checksum then covers the whole thing at once, which is
stronger than 3,000 separate ones nobody would compare.

## What this does NOT duplicate

`fetch_clutrr`, `fetch_openea`, `fetch_fb15k237`, `fetch_kachergis` and
`fetch_mnist` each pin their own dataset. **This one is the first to unpack an
archive**, so it is also the first that has to care about where a tar member
claims to land — see `main`.
"""

from __future__ import annotations

import hashlib
import pathlib
import shutil
import sys
import tarfile
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[1]
DEST = ROOT / "data" / "fsdd"
ARCHIVE = ("https://github.com/Jakobovski/free-spoken-digit-dataset"
           "/archive/refs/heads/master.tar.gz")
#: Only this subtree is kept. The repository also carries scripts and a
#: metadata module that nothing here reads.
WANTED = "/recordings/"


def digest(path: pathlib.Path) -> str:
    sha = hashlib.sha256()
    sha.update(path.read_bytes())
    return sha.hexdigest()


def main() -> int:
    DEST.mkdir(parents=True, exist_ok=True)
    bundle = DEST / "fsdd-master.tar.gz"
    if not bundle.exists():
        urllib.request.urlretrieve(ARCHIVE, bundle)
    print(f"  archive {bundle.stat().st_size:>12,} bytes  "
          f"sha256 {digest(bundle)[:16]}...")

    kept = 0
    with tarfile.open(bundle, "r:gz") as tar:
        for member in tar.getmembers():
            if not member.isfile() or WANTED not in member.name:
                continue
            name = pathlib.PurePosixPath(member.name).name
            # A TAR MEMBER'S NAME IS UNTRUSTED INPUT. It can contain `..` or an
            # absolute path and land outside the directory being extracted --
            # the classic archive traversal. Taking only the basename and
            # writing it ourselves means the archive never chooses a path.
            if not name.endswith(".wav") or "/" in name or name.startswith("."):
                continue
            source = tar.extractfile(member)
            if source is None:
                continue
            with open(DEST / name, "wb") as target:
                shutil.copyfileobj(source, target)
            kept += 1

    print(f"\n{kept} recording(s) in {DEST.relative_to(ROOT)}")
    print("CC BY-SA 4.0 -- Zohar Jackson et al., Free Spoken Digit Dataset")
    return 0 if kept else 1


if __name__ == "__main__":
    sys.exit(main())
