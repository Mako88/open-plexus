"""Build the lexical sense over the full-form store: BM25 (tantivy) over the titles and texts
`wiki_build.py` wrote, into `data/wiki-abstracts/bm25/`. No card; about ten minutes.

    .venv/Scripts/python scripts/lexical_build.py
"""

from __future__ import annotations

import argparse
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.lexical import LexicalIndex  # noqa: E402


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--store", default=str(ROOT / "data" / "wiki-abstracts" / "minilm"))
    p.add_argument("--out", default=str(ROOT / "data" / "wiki-abstracts" / "bm25"))
    args = p.parse_args()
    store = Path(args.store)
    LexicalIndex.build(Path(args.out), store / "titles.txt", store / "texts.txt",
                       log=lambda line: print(time.strftime("%H:%M:%S"), line, flush=True))
    print(f"built {args.out}", flush=True)


if __name__ == "__main__":
    main()
