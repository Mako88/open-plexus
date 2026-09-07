"""Phase 1's reading: how fast does the bare state forget?

THE DOC'S QUESTION, VERBATIM: on a house of 50 facts over 300 turns at 1.5B, how
does Tier A score at each delay against full-context and blind?

WOULD REFUTE THE WHOLE CORE CHOICE: Tier A at delay 20 turns below blind. Then
the state holds nothing usable and a recurrent core was the wrong part. That is
named here, before the run, because a threshold written after a number is not a
threshold.

  uv run python scripts/phase1_exam.py --size 1.5 --facts 50 --turns 300
  uv run python scripts/phase1_exam.py --size 0.4 --facts 12 --turns 60   # a rehearsal
  uv run python scripts/phase1_exam.py --baseline full-context            # hours; see below

THE FULL-CONTEXT BASELINE IS NOT RUN BY DEFAULT AND THAT IS STANDING OBJECTION
8, handled in the open rather than by omission. It re-feeds millions of tokens
and costs hours on this card; `--estimate` prints the number before anything
runs. A Tier A reading committed without it is INCOMPLETE and says so in its own
`baselines` field, so nothing downstream can mistake a partial reading for the
comparison the doc asked for.
"""

from __future__ import annotations

import argparse
import json
import platform
from datetime import UTC, datetime
from pathlib import Path

from sylvatica.exam import (
    DEFAULT_DELAYS,
    BlindBaseline,
    Tier,
    estimate_full_context_tokens,
    generate_house,
    run_blind,
    run_exam,
    run_full_context,
)
from sylvatica.loop.turn import PREAMBLE

# Named before the run, per the standing rule.
REFUTES = (
    "Tier A at delay 20 turns below the blind baseline. Then the state holds "
    "nothing usable and a recurrent core was the wrong part."
)


def result_rows(result, label: str) -> dict:
    """One arm's row, WITH every answer it gave.

    A READING WITHOUT ITS ANSWERS CANNOT BE AUDITED, and the first Tier A run
    proved it: the score was 0.008 at every delay including delay 1, which is a
    fact told one turn earlier. That is a harness fault and not a finding about
    memory, and telling the two apart needed the text the core actually said --
    which the reading did not carry. Two hundred and seventy rows is fifty
    kilobytes; a number nobody can check is worth less than that.
    """
    score = result.score()
    return {
        "label": label,
        "answers": [a.row() for a in result.answers],
        "tier": score.tier.value,
        "score": round(score.score, 4),
        "correct": score.correct,
        "asked": score.asked,
        "invented": score.invented,
        "echoed": score.echoed,
        "negatives": len([a for a in result.answers if a.fact_id is None]),
        "by_delay": score.by_delay,
        "by_kind": score.by_kind,
        "seconds": round(result.seconds, 1),
        "cost": result.cost.row() if result.cost else None,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=float, default=1.5)
    parser.add_argument("--facts", type=int, default=50)
    parser.add_argument("--turns", type=int, default=300)
    parser.add_argument("--seed", type=int, default=0)
    parser.add_argument("--negatives", type=int, default=20)
    parser.add_argument(
        "--delays",
        type=int,
        nargs="+",
        default=list(DEFAULT_DELAYS),
        help="turns between a fact being told and being asked; a rehearsal house needs short ones",
    )
    parser.add_argument("--reply-budget", type=int, default=40)
    parser.add_argument("--answer-budget", type=int, default=32)
    parser.add_argument(
        "--baseline",
        choices=["blind", "full-context", "both"],
        default="blind",
        help="full-context costs hours; --estimate says how many tokens first",
    )
    parser.add_argument("--estimate", action="store_true", help="print costs and exit")
    parser.add_argument("--out", type=Path, default=Path("readings"))
    args = parser.parse_args()

    house = generate_house(
        seed=args.seed,
        n_facts=args.facts,
        n_turns=args.turns,
        delays=tuple(args.delays),
        negatives=args.negatives,
    )
    blind = BlindBaseline(house)
    print(
        f"house: {len(house.facts)} facts over {args.turns} turns, "
        f"{len(house.questions)} questions ({args.negatives} negatives), "
        f"delays {house.delays}"
    )
    print(f"answer entropy by kind (bits): {blind.entropy}")

    from sylvatica.core.checkpoints import ensure
    from sylvatica.core.rwkv_core import RwkvCore

    checkpoint = ensure(args.size)
    core = RwkvCore(checkpoint)

    fc_tokens = estimate_full_context_tokens(core, house)
    print(
        f"full-context baseline would re-feed {fc_tokens:,} tokens "
        f"(~{fc_tokens / 157 / 3600:.1f} h at 157 tok/s)"
    )
    if args.estimate:
        return 0

    print("\n-- blind --", flush=True)
    blind_result = run_blind(house)
    print(json.dumps(result_rows(blind_result, "blind"), indent=2))

    print("\n-- Tier A --", flush=True)
    arm = run_exam(
        core,
        house,
        Tier.A,
        reply_budget=args.reply_budget,
        answer_budget=args.answer_budget,
    )

    rows = [result_rows(blind_result, "blind"), result_rows(arm, "tier-a")]
    baselines_run = ["blind"]

    if args.baseline in ("full-context", "both"):
        print("\n-- full-context --", flush=True)
        fc = run_full_context(core, house, answer_budget=args.answer_budget)
        rows.append(result_rows(fc, "full-context"))
        baselines_run.append("full-context")

    import torch

    reading = {
        "phase": 1,
        "kind": "exam",
        "tier": "A",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": "how fast the bare state forgets, against blind and full-context",
        "would_refute": REFUTES,
        "core": core.name,
        "params": core.params,
        "size_b": args.size,
        "house": {
            "seed": args.seed,
            "facts": len(house.facts),
            "turns": args.turns,
            "questions": len(house.questions),
            "negatives": args.negatives,
            "delays": list(house.delays),
            "answer_entropy_bits": blind.entropy,
            "modal_answers": blind.table,
        },
        "budgets": {"reply": args.reply_budget, "answer": args.answer_budget},
        # THE PREAMBLE IS A DIAL AND IT MOVED A SCORE FROM 0.008 TO WHATEVER
        # THIS READING SAYS, so it goes in the reading verbatim. A number taken
        # under one framing is not comparable to a number taken under another,
        # and the only way a later session can tell is if the framing is here.
        "preamble": PREAMBLE,
        # WHICH BASELINES ACTUALLY RAN, so a partial reading cannot be mistaken
        # for the comparison the doc asked for. Standing objection 8.
        "baselines": baselines_run,
        "full_context_tokens_if_run": fc_tokens,
        "rows": rows,
        "machine": {"gpu": "GTX 1080 Ti", "torch": torch.__version__},
    }

    # The verdict, computed rather than typed.
    arm_score = arm.score()
    blind_score = blind_result.score()
    at20_arm = arm_score.by_delay.get(20)
    at20_blind = blind_score.by_delay.get(20)
    reading["verdict"] = {
        "delay_20_tier_a": at20_arm,
        "delay_20_blind": at20_blind,
        "core_choice_refuted": (
            None if at20_arm is None or at20_blind is None else at20_arm < at20_blind
        ),
    }

    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase1-exam-{stamp}.json"
    path.write_text(json.dumps(reading, indent=2) + "\n", encoding="utf-8")

    print("\n" + json.dumps({r["label"]: r["by_delay"] for r in rows}, indent=2))
    print(json.dumps(reading["verdict"], indent=2))
    print(f"wrote {path}  [{platform.node()}]")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
