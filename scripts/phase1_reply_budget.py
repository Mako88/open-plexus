"""What does the core's own chatter cost it? Sweeping the reply budget.

WHY THIS IS OWED RATHER THAN OPTIONAL. `reply_budget` is 40 because a session
typed 40. The doc's DIALS section says every one of them "is a number read on an
exam, never a constant chosen once", and this one has never been read.

WHY IT IS INTERESTING RATHER THAN HOUSEKEEPING. The Phase 1 reading contains an
accidental ablation. Tier A's state carries the preamble, the user lines AND the
core's own replies, and scored 0.456. The full-context row is the same core with
the same memory mechanism carrying the preamble and the user lines ONLY -- no
replies -- and scored 0.648. Nineteen points, and the only difference is the
core's own chatter sitting in its fixed-size state.

SO THE BUDGET MAY TRADE COST AND ACCURACY IN THE SAME DIRECTION, which almost
nothing does. Shorter replies are fewer decode tokens (most of Tier A's runtime)
AND a cleaner state. If that holds, complaint 5 -- "time is cheap and words are
not" -- turns out to be about MEMORY as much as about cost, and the open fork
"brevity as a trained target" gets a reason it did not have.

WOULD REFUTE THAT READING: Tier A flat or rising with the budget. Then the
19-point gap is about something else in the full-context arm -- the absence of
the "Assistant:" turns entirely, say, rather than their length -- and cutting the
budget buys speed only.

  uv run python scripts/phase1_reply_budget.py --size 1.5 --budgets 8 16 40 64
"""

from __future__ import annotations

import argparse
import json
from datetime import UTC, datetime
from pathlib import Path

from sylvatica.exam import (
    DEFAULT_DELAYS,
    BlindBaseline,
    Tier,
    generate_house,
    run_blind,
    run_exam,
    spread,
)
from sylvatica.loop.turn import PREAMBLE

REFUTES = (
    "Tier A flat or rising as the reply budget grows. Then the 19-point gap to "
    "the no-replies arm is not about how long the replies are, and cutting the "
    "budget buys speed only."
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=float, default=1.5)
    parser.add_argument("--facts", type=int, default=50)
    parser.add_argument("--turns", type=int, default=300)
    parser.add_argument("--seeds", type=int, nargs="+", default=[0, 1])
    parser.add_argument("--budgets", type=int, nargs="+", default=[8, 16, 24, 40, 64])
    parser.add_argument("--negatives", type=int, default=20)
    parser.add_argument("--delays", type=int, nargs="+", default=list(DEFAULT_DELAYS))
    parser.add_argument("--answer-budget", type=int, default=32)
    parser.add_argument("--out", type=Path, default=Path("readings"))
    args = parser.parse_args()

    from sylvatica.core.checkpoints import ensure
    from sylvatica.core.rwkv_core import RwkvCore

    core = RwkvCore(ensure(args.size))

    houses = {
        seed: generate_house(
            seed=seed,
            n_facts=args.facts,
            n_turns=args.turns,
            delays=tuple(args.delays),
            negatives=args.negatives,
        )
        for seed in args.seeds
    }
    blind = {seed: run_blind(h).score().score for seed, h in houses.items()}

    runs = []
    for budget in args.budgets:
        for seed, house in houses.items():
            arm = run_exam(
                core,
                house,
                Tier.A,
                reply_budget=budget,
                answer_budget=args.answer_budget,
                state_path=Path("state") / "exam" / f"budget-{budget}-{seed}.pt",
                progress=False,
            )
            score = arm.score()
            row = {
                "reply_budget": budget,
                "seed": seed,
                "score": round(score.score, 4),
                "by_delay": score.by_delay,
                "blind": round(blind[seed], 4),
                "invented": score.invented,
                "echoed": score.echoed,
                "seconds": round(arm.seconds, 1),
                "tokens_out": arm.cost.tokens_out if arm.cost else None,
                "flops": arm.cost.flops if arm.cost else None,
            }
            runs.append(row)
            print(json.dumps(row), flush=True)

    by_budget = {
        b: {
            "score": spread([r["score"] for r in runs if r["reply_budget"] == b]),
            "seconds": spread([r["seconds"] for r in runs if r["reply_budget"] == b]),
        }
        for b in args.budgets
    }

    # The verdict, computed rather than typed: does the score fall as the budget
    # grows, and does it fall by more than the seed-to-seed spread?
    scores = [by_budget[b]["score"]["mean"] for b in args.budgets]
    widest_noise = max(by_budget[b]["score"]["range"] for b in args.budgets)
    verdict = {
        "score_at_smallest_budget": scores[0],
        "score_at_largest_budget": scores[-1],
        "drop_across_the_sweep": round(scores[0] - scores[-1], 4),
        "widest_seed_range": widest_noise,
        "chatter_costs_more_than_noise": (scores[0] - scores[-1]) > widest_noise,
        "seconds_at_smallest": by_budget[args.budgets[0]]["seconds"]["mean"],
        "seconds_at_largest": by_budget[args.budgets[-1]]["seconds"]["mean"],
    }

    reading = {
        "phase": 1,
        "kind": "reply-budget",
        "tier": "A",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": "whether the core's own replies in its state cost it accuracy as well as time",
        "would_refute": REFUTES,
        "core": core.name,
        "size_b": args.size,
        "house": {
            "facts": args.facts,
            "turns": args.turns,
            "delays": args.delays,
            "negatives": args.negatives,
            "seeds": args.seeds,
        },
        "answer_budget": args.answer_budget,
        "preamble": PREAMBLE,
        "entropy_bits": {s: BlindBaseline(h).entropy for s, h in houses.items()},
        "runs": runs,
        "by_budget": by_budget,
        "verdict": verdict,
    }

    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase1-reply-budget-{stamp}.json"
    path.write_text(json.dumps(reading, indent=2) + "\n", encoding="utf-8")

    print("\nby budget:", json.dumps(by_budget, indent=2))
    print("verdict:", json.dumps(verdict, indent=2))
    print(f"wrote {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
