"""Fetch the OpenEA entity-alignment benchmark, verifying the bytes.

## Why this benchmark

Note 076 established the requirement and found nothing in the project's task set met it:
**testing concept acquisition needs surfaces that RECUR, in contexts that overlap.** CLUTRR
gives each entity one or two edges — 64.4% have exactly two — so two surfaces of one concept
share no features and score zero by arithmetic.

OpenEA is the standard benchmark for knowledge-graph entity alignment, which is the same
problem `concepts.Merged` expresses: two graphs, and gold links saying which entities are
one thing. It meets the requirement outright:

    dataset          degree median   mean    entities with >=4 edges
    D_W_15K_V2                   7   9.86                     100.0%
    EN_FR_15K_V2                 8  12.84                     100.0%
    D_W_15K_V1                   3   5.10                      44.7%
    CLUTRR                       2   ~2                         5.9%

**And v2.0 encodes the entity URIs** (`E823797` rather than a readable name), which is the
name-bias fix the authors made — so string matching cannot cheat and relational structure is
the only signal available. That is precisely the test.

## What is fetched, and what is NOT

Only the **15K** datasets are extracted. The 100K sets are in the archive and are skipped:
280 MB for the eight 15K directories is already more than a first measurement needs, and
`--all` exists for when it does.

**The data is GPL-licensed.** Evaluation-only use is the intent here. Flagged rather than
buried because the project's endgame — commercial, open source, or both — is undecided, and
a licence nobody chose is a constraint discovered late.

## Verification

Size and sha256 of the archive are pinned below. **Both were computed from the downloaded
bytes**, not read off a page: a file whose job is verifying numbers must not contain invented
ones, which is a mistake `fetch_clutrr.py` was written to correct after making it.

Dropbox links rot, so figshare is recorded alongside as the durable source. If the digest
stops matching, the fix is to check what changed upstream rather than to update the constant.
"""

from __future__ import annotations

import argparse
import hashlib
import sys
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_OUT = ROOT / "data" / "openea"

#: Direct download. `dl=1` rather than `dl=0`, or Dropbox serves an HTML page.
URL = ("https://www.dropbox.com/s/xfehqm4pcd9yw0v/"
       "OpenEA_dataset_v2.0.zip?dl=1")
#: The durable alternative, recorded because the link above is a share URL.
FIGSHARE = "https://figshare.com/articles/dataset/OpenEA_dataset_v1_1/19258760/3"

#: Computed from the bytes this script downloaded on 2026-07-30.
EXPECTED_SIZE = 248_800_649
EXPECTED_SHA256 = (
    "37adcaf4a7ed33530a39b637da7329e891456bfc89557d8b1ac307c67eb8f5bc")

#: What a v2.0 dataset directory must contain for the loader to work.
REQUIRED = ("rel_triples_1", "rel_triples_2", "ent_links")


def download(destination: Path) -> bytes:
    if destination.exists():
        print(f"{destination} already here, verifying rather than re-fetching")
        return destination.read_bytes()
    print(f"fetching {URL}")
    with urllib.request.urlopen(URL) as response:
        payload = response.read()
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(payload)
    return payload


def verify(payload: bytes) -> None:
    size = len(payload)
    digest = hashlib.sha256(payload).hexdigest()
    if size != EXPECTED_SIZE or digest != EXPECTED_SHA256:
        raise SystemExit(
            f"the archive is not the one this script pins.\n"
            f"  size   got {size:,} want {EXPECTED_SIZE:,}\n"
            f"  sha256 got {digest}\n"
            f"         want {EXPECTED_SHA256}\n"
            f"Do NOT measure anything on it. Either the upstream file changed or "
            f"the download truncated, and both are things to find out rather than "
            f"paper over by updating the constant. Durable source: {FIGSHARE}")
    print(f"verified {size:,} bytes, sha256 matches")


def extract(archive: Path, out: Path, everything: bool) -> list[str]:
    with zipfile.ZipFile(archive) as bundle:
        wanted = [name for name in bundle.namelist()
                  if len(name.split("/")) > 1
                  and (everything or "15K" in name.split("/")[1])]
        for name in wanted:
            bundle.extract(name, out)
    datasets = sorted({name.split("/")[1] for name in wanted
                       if len(name.split("/")) > 1})
    return datasets


def check(out: Path, datasets: list[str]) -> None:
    """Every extracted dataset must have the three files a measurement reads."""
    missing = []
    for name in datasets:
        for directory in out.rglob(name):
            if not directory.is_dir():
                continue
            for required in REQUIRED:
                if not (directory / required).exists():
                    missing.append(f"{name}/{required}")
    if missing:
        raise SystemExit(
            f"extracted but incomplete, missing: {sorted(set(missing))}. A "
            f"partial dataset would produce a full set of numbers about a "
            f"subset nobody chose.")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", type=Path, default=DEFAULT_OUT)
    parser.add_argument("--archive", type=Path, default=None,
                        help="where to keep the zip; defaults under --out")
    parser.add_argument("--all", action="store_true",
                        help="also extract the 100K datasets")
    args = parser.parse_args()

    archive = args.archive or (args.out / "OpenEA_dataset_v2.0.zip")
    payload = download(archive)
    verify(payload)
    datasets = extract(archive, args.out, args.all)
    check(args.out, datasets)
    print(f"extracted {len(datasets)} dataset(s) into {args.out}:")
    for name in datasets:
        print(f"  {name}")
    print("\nGPL-licensed. Evaluation use.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
