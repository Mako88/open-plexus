"""Build step 6's full-form store: MiniLM keys for every Wikipedia abstract in
`data/wiki-abstracts/corpus.parquet` (5.23 million), written shard by shard into
`data/wiki-abstracts/minilm/` by `openplexus.wiki.build`. A run stopped for a game resumes from
its last shard. Needs the card; about an hour on a free GTX 1080 Ti.

    .venv/Scripts/python scripts/wiki_build.py --limit 5000 --out state/wiki-smoke   # the smoke
    .venv/Scripts/python scripts/wiki_build.py                                        # the store
"""

from __future__ import annotations

import argparse
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.wiki import build  # noqa: E402


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--parquet", default=str(ROOT / "data" / "wiki-abstracts" / "corpus.parquet"))
    p.add_argument("--out", default=str(ROOT / "data" / "wiki-abstracts" / "minilm"))
    p.add_argument("--shard", type=int, default=50_000)
    p.add_argument("--limit", type=int, default=None)
    p.add_argument("--batch", type=int, default=256)
    args = p.parse_args()
    from openplexus.embed import MiniLM

    embed = MiniLM(batch=args.batch)
    started = time.perf_counter()
    progress = build(Path(args.parquet), Path(args.out), embed, shard=args.shard, limit=args.limit,
                     log=lambda line: print(time.strftime("%H:%M:%S"), line, flush=True))
    print(f"built {progress['n']} keys in {progress['done']} shards at {args.out}, "
          f"{time.perf_counter() - started:.0f}s this run", flush=True)


if __name__ == "__main__":
    main()
