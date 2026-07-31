"""Fetch the cross-situational word-learning trial orderings, pinned by checksum.

## What this gets, and what it does NOT get

`kachergis/XSLmodels` collects stimuli from published cross-situational word
learning experiments. **This fetches the 29 conditions that ship as plain text**
— one line per trial, each number a word-object pair — plus the dataset table.

**It does not get human accuracies, and that was checked rather than assumed.**
`data-raw/XSL-dataset-fields.csv` has 64 rows of which 8 carry an `accuracy`, and
**not one of those 8 names an ordering that exists as a `.txt`.** The human
numbers live in `.RData` alongside orderings in the same format, so a human
comparison needs a pure-Python RData reader — gzipped XDR — which is not built.
The CSV is fetched anyway because it carries the citation and sample size for
every condition, and because a later reader will want it beside the data.

So what this buys is **external STIMULI, not an external BENCHMARK**: trials this
project did not design, with ground truth that needs no human data, because a
trial line naming pair `n` means word `n` and object `n` were both present and
the correct mapping is therefore known.

## Why fetched rather than vendored

`data/*/` is gitignored and the repository is GPL. Committing third-party data
would carry it in every clone forever and would put a GPL corpus inside a tree
whose licensing is undecided; a fetcher costs one command and keeps the question
open. What is committed is the URL and a sha256 per file, so *"did we measure the
same bytes"* is answerable — rule 11b's concern, with a longer fuse.

## What this does NOT duplicate

`tools/fetch_clutrr.py`, `fetch_openea.py` and `fetch_fb15k237.py` each pin their
own dataset the same way. **The shape is deliberately repeated and the content is
not**: they share no code because each is a different URL layout and a different
verification list, and the honest common part — download, hash, compare — is four
lines that `check_duplication` sees as distinct because the surrounding structure
differs. If a fourth pattern appears, extract then rather than now.
"""

from __future__ import annotations

import hashlib
import pathlib
import sys
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[1]
DEST = ROOT / "data" / "kachergis"
BASE = "https://raw.githubusercontent.com/kachergis/XSLmodels/master/data-raw"

#: The dataset table. Carries citation, sample size and — for eight rows — a
#: human accuracy whose ordering is not among the files below.
TABLE = "XSL-dataset-fields.csv"

#: Every condition in `data-raw/orders/` that ships as plain text AND is named by
#: the table. Listed rather than globbed so a run's identity is fixed by this
#: file and not by whatever the directory held that day.
ORDERS = (
    "1_max_temp_spat_cont_orig", "1olap3tr_contr", "2_x8_39_4x4",
    "3_x8_369_4x4", "4_no_spat_orig_max_tc", "cont_div6-12",
    "filt0E_3L", "filt0E_6L", "filt0E_9L", "filt3E_3L", "filt3E_6L",
    "filt3E_9L", "filt6E_3L", "filt6E_6L", "filt6E_9L", "filt9E_3L",
    "filt9E_6L", "filt9E_9L", "freq369-3x3hiCD", "freq369-3x3loCD",
    "freq369_36mx", "freq369_39mx", "orig_3x3", "orig_4x4",
    "reord_orig_1", "reord_orig_2", "reord_orig_3", "reord_orig_4",
    "temp_cont_1olap3tr",
)


def digest(path: pathlib.Path) -> str:
    sha = hashlib.sha256()
    sha.update(path.read_bytes())
    return sha.hexdigest()


def main() -> int:
    DEST.mkdir(parents=True, exist_ok=True)
    wanted = [(TABLE, f"{BASE}/{TABLE}")] + [
        (f"{name}.txt", f"{BASE}/orders/{name}.txt") for name in ORDERS]
    for name, url in wanted:
        target = DEST / name
        if not target.exists():
            urllib.request.urlretrieve(url, target)
        print(f"  {name:<38} {target.stat().st_size:>8,} bytes  "
              f"sha256 {digest(target)[:16]}...")
    print(f"\n{len(wanted)} file(s) in {DEST.relative_to(ROOT)}")
    print("NOTE: no human accuracies here. See this file's docstring.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
