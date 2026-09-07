"""The general slice of the replay mix: language the house did not produce.

WHY IT IS IN THE MIX AT ALL. The doc: "a slice of general text (a fixed local
corpus of a few million tokens) so the adapter is anchored to language it did not
learn from this house." Without it the adapter sees only invented people in
invented rooms, and refutation 2 -- "adapter updates lose the core's baseline
abilities faster than they add" -- is not so much tested as arranged.

AND THE FIRST PHASE 3 RUN SHOWED EXACTLY THAT. With no general slice, one cycle
of about seven hundred tokens of house text moved held-out perplexity from 22.94
to 27.56. A 20% degradation, from almost nothing, in one step. The gate caught it
and rolled back -- correctly, and it would have rolled back all ten cycles, and
the reading would have said "replay does not protect the base at this scale" when
what was actually being measured was replay with an ingredient missing.

THE ONE THING IT MUST NOT BE IS THE GATE'S HELD-OUT TEXT, and `check_disjoint`
enforces it. Training on the yardstick would corrupt every gate reading in the
most flattering possible direction: perplexity would IMPROVE on the very text
used to decide whether the model had got worse, so a cycle that damaged the model
everywhere else would sail through.
"""

from __future__ import annotations

import random
import re
from pathlib import Path

CORPORA = Path("corpora")


def check_disjoint(sample: list[str]) -> None:
    """Refuse a general slice that overlaps the gate's held-out text.

    Compares on sentence-length shingles rather than whole documents, because
    the failure worth catching is a few shared paragraphs rather than the same
    file twice.
    """
    from .heldout import PERPLEXITY_TEXT

    def shingles(text: str) -> set[str]:
        words = re.findall(r"[a-z']+", text.lower())
        return {" ".join(words[i : i + 8]) for i in range(max(0, len(words) - 8))}

    held = shingles(PERPLEXITY_TEXT)
    for chunk in sample:
        overlap = shingles(chunk) & held
        if overlap:
            raise ValueError(
                "the general slice overlaps the gate's held-out text: "
                f"{sorted(overlap)[:2]}. Training on the yardstick would make "
                "every gate reading meaningless in the flattering direction."
            )


def load(
    n_chunks: int = 32,
    chunk_words: int = 120,
    seed: int | None = None,
    root: Path | None = None,
) -> list[str]:
    """Random passages from the local public-domain corpus.

    RANDOM PASSAGES RATHER THAN THE FIRST N. Taking the opening of each book
    would anchor the adapter to title pages, dedications and chapter one, which
    is a narrower slice of English than the corpus contains and would be the same
    slice every cycle.
    """
    root = root or CORPORA
    texts = sorted(root.glob("*.txt")) if root.exists() else []
    if not texts:
        raise FileNotFoundError(
            f"no general corpus under {root}. Run `uv run python corpora/fetch.py`. "
            "Training without it is not the arm the doc describes -- see this "
            "module's docstring for what happened when it was missing."
        )

    rng = random.Random(seed)
    words: list[str] = []
    for path in texts:
        words.extend(path.read_text(encoding="utf-8", errors="replace").split())

    out = []
    for _ in range(n_chunks):
        start = rng.randrange(0, max(1, len(words) - chunk_words))
        out.append(" ".join(words[start : start + chunk_words]))
    check_disjoint(out)
    return out


def available(root: Path | None = None) -> bool:
    root = root or CORPORA
    return bool(root.exists() and list(root.glob("*.txt")))
