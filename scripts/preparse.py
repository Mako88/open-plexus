"""Parse a stream's stories ahead of hearing them, in batches, into the store of
readings: one sentence at a time leaves the card two-thirds idle, and a batch reads about
130 sentences a second where single calls read about eight. Every later run, and every
change to what is extracted, then reads from the store.

    uv run python scripts/preparse.py --stories 3000
"""

import argparse
import os
import sys
import time
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")
sys.path.insert(0, str(Path(__file__).resolve().parent))

from stories import BREAK  # noqa: E402

from unfused.exam.stories import stream  # noqa: E402
from unfused.exam.world import RIGHT, WRONG  # noqa: E402
from unfused.graph import PARSER, parse_many  # noqa: E402


def texts_of(stories) -> list[str]:
    """Everything a run of these stories says to the system: its tellings, its questions,
    and the teacher's reactions."""
    out = [BREAK, RIGHT]
    for s in stories:
        out += s.told + s.after
        if s.question is not None:
            out += [s.question, WRONG.format(answer=s.answer)]
        for c in s.checks:
            out += [c.question, WRONG.format(answer=c.answer)]
    return out


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--stories", type=int, default=300)
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--model", default=PARSER)
    args = p.parse_args()
    started = time.perf_counter()
    texts = texts_of(stream(args.stories, args.seed))
    read = parse_many(args.model, texts)
    took = time.perf_counter() - started
    print(f"{len(texts)} texts, {read} read in {took:.0f}s")


if __name__ == "__main__":
    main()
