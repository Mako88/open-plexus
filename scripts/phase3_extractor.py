"""How often does the extractor state something false? The declaratives arm's gate.

THE DOC'S REFUTATION FOR THIS ARM: "the extractor at 1.5B produces facts that are
wrong more than a fifth of the time, read by hand on a sample of 50."

WHY THIS MEASURES RATHER THAN ONLY READS. The doc says "by hand" because it
assumed no ground truth. On a GENERATED house there is: every fact was invented
by the generator and its answer is known, so a declarative that names an entity
and gives the wrong answer for it can be caught automatically, on all of them,
rather than on fifty. A sample of fifty is still written out for a human,
because an automatic checker can only catch the errors it was told to look for
and a person reading fifty lines catches the ones nobody anticipated.

WHY A WRONG DECLARATIVE IS WORSE THAN A RAW EPISODE, which is what makes this
worth gating on. The raw arm's failure is that it teaches form and not binding --
it produces "There are 18 flour sacks in the pantry" when the answer is 13, from
a model that never learned the binding. A wrong DECLARATIVE trains that same
false binding in deliberately, with the same weight as a true one, and the
resulting model will state it confidently. The arm can be worse than doing
nothing, and only this number says whether it is.

  uv run python scripts/phase3_extractor.py --size 1.5
"""

from __future__ import annotations

import argparse
import json
from collections import Counter
from datetime import UTC, datetime
from pathlib import Path

from sylvatica.exam import generate_house
from sylvatica.learn.extract import declaratives_from
from sylvatica.store import HashEmbedder, SqliteStore


def subject_of(fact) -> str:
    """The term a declarative must name to be ABOUT this fact.

    For most kinds it is the object or the person; the answer is what a
    declarative gets right or wrong. Matching on the subject is what lets a
    statement be judged rather than merely counted.
    """
    told = fact.told
    if fact.kind in ("trade", "relation"):
        return told.split()[0]  # the person
    if fact.kind == "number":
        # "There are 65 lanterns in the cellar." -> the thing counted
        parts = told.rstrip(".").split()
        return " ".join(parts[3:-3]) if len(parts) > 6 else parts[3]
    if fact.kind == "place":
        return told.split()[0]
    return told.replace("The ", "", 1).split(" are ")[0]  # colour


def judge_declarative(text: str, facts: list) -> tuple[str, str | None]:
    """(verdict, fact_id). One of supported / contradicted / unrelated.

    CONTRADICTED IS THE ONE THAT MATTERS. A declarative that names a fact's
    subject and gives a DIFFERENT answer of the same kind is a false binding
    about to be trained in. Unrelated is merely wasted tokens.
    """
    lowered = text.lower()
    for fact in facts:
        subject = subject_of(fact).lower()
        if not subject or subject not in lowered:
            continue
        if fact.answer.lower() in lowered:
            return "supported", fact.id
        # Does it assert a DIFFERENT answer of the same kind?
        others = {
            f.answer.lower() for f in facts if f.kind == fact.kind
        } - {fact.answer.lower()}
        if any(o in lowered for o in others):
            return "contradicted", fact.id
        return "unrelated", fact.id
    return "unrelated", None


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=float, default=1.5)
    parser.add_argument("--facts", type=int, default=50)
    parser.add_argument("--turns", type=int, default=300)
    parser.add_argument("--seed", type=int, default=0)
    parser.add_argument("--windows", type=int, default=25, help="episode windows to extract from")
    parser.add_argument("--out", type=Path, default=Path("readings"))
    args = parser.parse_args()

    from sylvatica.core.checkpoints import ensure
    from sylvatica.core.rwkv_core import RwkvCore

    core = RwkvCore(ensure(args.size))
    house = generate_house(
        seed=args.seed, n_facts=args.facts, n_turns=args.turns, negatives=0
    )

    # The store here holds the TOLD sentences only, which is what an episode
    # window of a real thread would contain plus filler. No embedder work is
    # needed -- nothing is searched -- so the cheap one is used deliberately.
    db = Path("state/exam") / f"p3-extract-{args.seed}.db"
    if db.exists():
        db.unlink()
    store = SqliteStore(db, embedder=HashEmbedder(dims=32))
    for turn, line in enumerate(house.turns):
        store.write_turn(line, f"extract#turn:{turn}")

    fragments = [
        store.get(r["id"])
        for r in store.db.execute(
            "SELECT id FROM fragments ORDER BY created_at LIMIT ?", (args.windows * 4,)
        )
    ]
    print(f"extracting from {len(fragments)} fragments in windows of 4", flush=True)

    extraction = declaratives_from(core, fragments, window_size=4)
    print(f"{len(extraction.declaratives)} declaratives from {extraction.windows} windows")

    rows = []
    for d in extraction.declaratives:
        verdict, fact_id = judge_declarative(d.text, house.facts)
        rows.append({**d.row(), "verdict": verdict, "fact_id": fact_id})

    counts = Counter(r["verdict"] for r in rows)
    by_source = {
        src: Counter(r["verdict"] for r in rows if r["source"] == src)
        for src in {r["source"] for r in rows}
    }
    decided = counts["supported"] + counts["contradicted"]
    error_rate = counts["contradicted"] / decided if decided else None

    reading = {
        "phase": 3,
        "kind": "extractor",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": "how often the extractor states something the house contradicts",
        "would_refute": (
            "an error rate above a fifth -- the doc's bar for this arm. A wrong "
            "declarative trains a false binding in with the same weight as a true "
            "one, so the arm can be worse than doing nothing."
        ),
        "core": core.name,
        "size_b": args.size,
        "house": {"seed": args.seed, "facts": args.facts, "turns": args.turns},
        "windows": extraction.windows,
        "declaratives": len(rows),
        "counts": dict(counts),
        "by_source": {k: dict(v) for k, v in by_source.items()},
        "error_rate": round(error_rate, 4) if error_rate is not None else None,
        "refutation_fired": (error_rate is not None and error_rate > 0.2),
        # FIFTY FOR A HUMAN, because an automatic checker only catches the errors
        # it was told to look for.
        "sample_for_hand_reading": rows[:50],
        "all": rows,
    }

    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase3-extractor-{stamp}.json"
    path.write_text(json.dumps(reading, indent=2) + "\n", encoding="utf-8")

    print(f"\ncounts: {dict(counts)}")
    for src, c in by_source.items():
        print(f"  {src}: {dict(c)}")
    print(f"error rate (contradicted / decided): {reading['error_rate']}")
    print(f"refutation fired: {reading['refutation_fired']}")
    print("\nfirst few declaratives:")
    for r in rows[:8]:
        print(f"  [{r['verdict']:12}] {r['source']:16} {r['text'][:70]!r}")
    print(f"\nwrote {path}")
    store.close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
