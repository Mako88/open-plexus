"""Phase 2's reading: does retrieval recover what the state lost?

THE DOC'S EXIT: Tier B beats Tier A at every delay past 20 turns, and the
retrieval precision at k is read.

THE BAR IS NOW A NUMBER RATHER THAN A DIRECTION, because Phase 1 put one there.
On the doc's house the same core scored 0.492 from the state alone and a
same-size ATTENTION model re-reading everything scored 0.908 -- and the attention
arm barely decays with delay (.98 .92 .92 .88 .84) where the state does (.54 .62
.56 .36 .38). Phase 2 has to close most of that, and at delay 150 it has to close
0.38 to 0.84. Beating Tier A is the floor, not the goal.

WOULD REFUTE PHASE 2: Tier B at or below Tier A past delay 20. Then retrieval
bought nothing over what the state already had, and the store is not the answer
to the decay Phase 1 measured.

AND A SECOND REFUTATION THAT MATTERS MORE, per standing objection 7: precision
holding up under `--ranker vector` but collapsing under `--ranker lexical`, or
the reverse. The generator asks 'How many lanterns are in the cellar?' about
'There are 65 lanterns in the cellar', so every content word is shared and FTS5
alone can find it. If the score is the lexical ranker's, Tier B is passing a
task easier than the one it is for, and Phase 3 would be built on a store nobody
has stressed. THE ARMS ARE RUN IN THE SAME READING so the comparison cannot be
skipped.

  uv run python scripts/phase2_exam.py --size 1.5 --seed 0
  uv run python scripts/phase2_exam.py --ranker vector lexical hybrid
"""

from __future__ import annotations

import argparse
import json
from datetime import UTC, datetime
from pathlib import Path

from sylvatica.exam import DEFAULT_DELAYS, Tier, generate_house, run_blind, run_exam
from sylvatica.exam.run import INJECT_HEADER
from sylvatica.loop.turn import PREAMBLE
from sylvatica.store import MiniLmEmbedder, SqliteStore

REFUTES = (
    "Tier B at or below Tier A past delay 20 -- retrieval bought nothing over "
    "the state. Or precision surviving on the lexical ranker alone, which would "
    "mean the exam's questions are paraphrases of its own facts and Phase 2 "
    "passed a task easier than the one it is for."
)


class OneRanker(SqliteStore):
    """A store with one half of the hybrid switched off, to see which half works.

    STANDING OBJECTION 7 IN EXECUTABLE FORM. Subclassing rather than adding a
    flag to `SqliteStore` on purpose: this is an instrument for one experiment,
    and a production store that can silently run with half its ranker disabled
    is a store that will one day be run that way by accident.
    """

    def __init__(self, *args, ranker: str = "hybrid", **kw) -> None:
        super().__init__(*args, **kw)
        self.ranker = ranker

    def _lexical(self, query_text, n, include_archived):
        if self.ranker == "vector":
            return []
        return super()._lexical(query_text, n, include_archived)

    def _vector(self, query_text, n, include_archived):
        if self.ranker == "lexical":
            return []
        return super()._vector(query_text, n, include_archived)


def precision(result) -> dict:
    rows = [r for r in result.retrieved if r["wanted_fragment"]]
    if not rows:
        return {"at_k": None, "n": 0}
    hits = [r for r in rows if r["hit"]]
    by_delay: dict[int, float] = {}
    for delay in sorted({r["delay"] for r in rows}):
        at = [r for r in rows if r["delay"] == delay]
        by_delay[delay] = round(sum(r["hit"] for r in at) / len(at), 4)
    return {
        "at_k": round(len(hits) / len(rows), 4),
        "n": len(rows),
        "by_delay": by_delay,
        "mean_rank_when_found": (
            round(sum(r["rank"] for r in hits) / len(hits), 2) if hits else None
        ),
    }


def row_for(result, label: str, blind: float) -> dict:
    score = result.score()
    return {
        "label": label,
        "tier": score.tier.value,
        "score": round(score.score, 4),
        "blind": blind,
        "invented": score.invented,
        "echoed": score.echoed,
        "negatives": len([a for a in result.answers if a.fact_id is None]),
        "by_delay": score.by_delay,
        "by_kind": score.by_kind,
        "precision": precision(result),
        "seconds": round(result.seconds, 1),
        "truncated_replies": result.truncated_replies,
        "cost": result.cost.row() if result.cost else None,
        "answers": [a.row() for a in result.answers],
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=float, default=1.5)
    parser.add_argument("--facts", type=int, default=50)
    parser.add_argument("--turns", type=int, default=300)
    parser.add_argument("--seed", type=int, default=0)
    parser.add_argument("--negatives", type=int, default=20)
    parser.add_argument("--delays", type=int, nargs="+", default=list(DEFAULT_DELAYS))
    parser.add_argument("--k", type=int, default=5, help="hits injected; a DIAL")
    parser.add_argument("--reply-budget", type=int, default=40)
    parser.add_argument("--answer-budget", type=int, default=32)
    parser.add_argument(
        "--ranker",
        nargs="+",
        default=["hybrid", "lexical", "vector"],
        choices=["hybrid", "lexical", "vector"],
        help="which halves of the hybrid to run; all three settles objection 7",
    )
    parser.add_argument("--tier-a", action="store_true", help="run Tier A beside it")
    parser.add_argument("--out", type=Path, default=Path("readings"))
    args = parser.parse_args()

    house = generate_house(
        seed=args.seed,
        n_facts=args.facts,
        n_turns=args.turns,
        delays=tuple(args.delays),
        negatives=args.negatives,
    )
    blind_score = round(run_blind(house).score().score, 4)
    print(
        f"house: {args.facts} facts over {args.turns} turns, "
        f"{len(house.questions)} questions, blind {blind_score}"
    )

    from sylvatica.core.checkpoints import ensure
    from sylvatica.core.rwkv_core import RwkvCore

    core = RwkvCore(ensure(args.size))
    embedder = MiniLmEmbedder()
    print(f"embedder: {embedder.model_id} ({embedder.params / 1e6:.1f}M, {embedder.dims}d)")

    rows = []
    if args.tier_a:
        print("\n-- Tier A (state only) --", flush=True)
        a = run_exam(
            core, house, Tier.A,
            reply_budget=args.reply_budget, answer_budget=args.answer_budget,
            state_path=Path("state/exam/p2-tier-a.pt"), progress=False,
        )
        rows.append(row_for(a, "tier-a", blind_score))
        print(json.dumps(rows[-1]["by_delay"]))

    for ranker in args.ranker:
        # A FRESH DATABASE PER ARM. A store carried over from the previous arm
        # would already hold this house's fragments, so the second arm would be
        # searching a store the first one filled -- same rows, different ranker,
        # and no way to tell that from a fair run.
        db = Path("state/exam") / f"p2-{ranker}-{args.seed}.db"
        if db.exists():
            db.unlink()
        store = OneRanker(db, embedder=embedder, ranker=ranker)

        print(f"\n-- Tier B ({ranker}, k={args.k}) --", flush=True)
        b = run_exam(
            core, house, Tier.B,
            reply_budget=args.reply_budget, answer_budget=args.answer_budget,
            state_path=Path("state/exam") / f"p2-b-{ranker}.pt",
            progress=False, store=store, k=args.k,
            provenance=f"exam-seed{args.seed}",
        )
        row = row_for(b, f"tier-b-{ranker}", blind_score)
        row["tiers"] = store.tiers()
        rows.append(row)
        print(json.dumps({"score": row["score"], "by_delay": row["by_delay"],
                          "precision": row["precision"]["at_k"]}))
        store.close()

    reading = {
        "phase": 2,
        "kind": "exam",
        "tier": "B",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": "does retrieval recover what the state lost, and which ranker does it",
        "would_refute": REFUTES,
        "core": core.name,
        "size_b": args.size,
        "embedder": {
            "model": embedder.model_id,
            "params": embedder.params,
            "dims": embedder.dims,
        },
        "house": {
            "seed": args.seed,
            "facts": args.facts,
            "turns": args.turns,
            "delays": args.delays,
            "negatives": args.negatives,
        },
        "dials": {
            "k": args.k,
            "reply_budget": args.reply_budget,
            "answer_budget": args.answer_budget,
            "inject_header": INJECT_HEADER,
        },
        "preamble": PREAMBLE,
        "blind": blind_score,
        # The Phase 1 numbers this has to beat, carried into the reading so the
        # bar is visible beside the result rather than in a commit message.
        "phase1_reference": {
            "tier_a": 0.492,
            "full_context": 0.608,
            "attention_qwen3_1_7b": 0.908,
            "attention_by_delay": {"1": 0.98, "5": 0.92, "20": 0.92, "60": 0.88, "150": 0.84},
        },
        "rows": rows,
    }

    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase2-exam-{stamp}.json"
    path.write_text(json.dumps(reading, indent=2) + "\n", encoding="utf-8")

    print("\n" + "-" * 60)
    for r in rows:
        p = r["precision"]["at_k"]
        print(
            f"{r['label']:18s} score={r['score']:.3f}  "
            f"precision@k={p if p is not None else '-'}  "
            f"inv={r['invented']}/{r['negatives']}  {r['by_delay']}"
        )
    print(f"\nwrote {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
