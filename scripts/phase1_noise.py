"""How much does a Tier A score move between houses? Read this before believing one.

WHY THIS EXISTS, AND WHY IT IS NOT OPTIONAL. Phase 1's refutation is "Tier A at
delay 20 turns below blind". Blind scores 0.24 on the doc's house. If Tier A
comes in at 0.30 at delay 20, that clears the bar only if a Tier A score moves by
less than 0.06 between houses -- AND NOBODY KNOWS WHETHER IT DOES. A verdict read
off one house is a verdict read off one sample.

THIS IS THE SAME RULE THE DOC ALREADY APPLIES TO THE REGRESSION GATE: "Calibrate
the thresholds on noise FIRST: run two identical cycles and read the spread
before a threshold is chosen." The gate got that rule because a threshold chosen
before the noise is known is a prediction dressed as a check. An exam threshold
is no different, and this branch has already shipped one number that turned out
to be measuring the harness.

IT IS CHEAP, WHICH IS THE OTHER REASON. Tier A on the doc's house is about two
minutes of core time; five houses is ten minutes. The full-context baseline is
2.7 hours and is NOT re-run here -- its spread is a separate question and a much
more expensive one, and this reading says so rather than pretending otherwise.

  uv run python scripts/phase1_noise.py --size 1.5 --seeds 0 1 2 3 4

WOULD REFUTE THE PHASE 1 VERDICT AS READABLE AT ALL: a Tier A spread at delay 20
wider than the gap between Tier A and blind. Then one house cannot settle the
question and the exit needs several, which is a finding about the INSTRUMENT and
belongs in the commit that finds it.
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
    "A Tier A spread at delay 20 wider than the gap between Tier A and blind. "
    "Then one house cannot settle Phase 1 and the exit needs several."
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=float, default=1.5)
    parser.add_argument("--facts", type=int, default=50)
    parser.add_argument("--turns", type=int, default=300)
    parser.add_argument("--seeds", type=int, nargs="+", default=[0, 1, 2, 3, 4])
    parser.add_argument("--negatives", type=int, default=20)
    parser.add_argument("--delays", type=int, nargs="+", default=list(DEFAULT_DELAYS))
    parser.add_argument("--reply-budget", type=int, default=40)
    parser.add_argument("--answer-budget", type=int, default=32)
    parser.add_argument("--out", type=Path, default=Path("readings"))
    args = parser.parse_args()

    from sylvatica.core.checkpoints import ensure
    from sylvatica.core.rwkv_core import RwkvCore

    core = RwkvCore(ensure(args.size))

    runs = []
    for seed in args.seeds:
        house = generate_house(
            seed=seed,
            n_facts=args.facts,
            n_turns=args.turns,
            delays=tuple(args.delays),
            negatives=args.negatives,
        )
        print(f"\n-- seed {seed} --", flush=True)
        blind = run_blind(house).score()
        arm = run_exam(
            core,
            house,
            Tier.A,
            reply_budget=args.reply_budget,
            answer_budget=args.answer_budget,
            state_path=Path("state") / "exam" / f"noise-{seed}.pt",
            progress=False,
        )
        score = arm.score()
        runs.append(
            {
                "seed": seed,
                "entropy_bits": BlindBaseline(house).entropy,
                "blind": round(blind.score, 4),
                "blind_by_delay": blind.by_delay,
                "tier_a": round(score.score, 4),
                "tier_a_by_delay": score.by_delay,
                "tier_a_by_kind": score.by_kind,
                "invented": score.invented,
                "echoed": score.echoed,
                "truncated_replies": arm.truncated_replies,
                "conversation_turns": arm.conversation_turns,
                "negatives": len([a for a in arm.answers if a.fact_id is None]),
                "seconds": round(arm.seconds, 1),
            }
        )
        print(json.dumps(runs[-1]["tier_a_by_delay"], indent=2), flush=True)

    # INT KEYS, because `Score.by_delay` is keyed by the delay itself. They only
    # become strings when `json.dumps` writes them out, which is why the earlier
    # reading files show "1" and this code must not.
    delays = list(args.delays)
    by_delay = {
        d: spread([r["tier_a_by_delay"][d] for r in runs if d in r["tier_a_by_delay"]])
        for d in delays
    }
    blind_by_delay = {
        d: spread([r["blind_by_delay"][d] for r in runs if d in r["blind_by_delay"]])
        for d in delays
    }

    # The verdict, computed rather than typed: at each delay, is the gap between
    # Tier A and blind wider than the range Tier A moves across houses?
    readable = {}
    for d in delays:
        gap = by_delay[d]["mean"] - blind_by_delay[d]["mean"]
        readable[d] = {
            "gap_to_blind": round(gap, 4),
            "tier_a_range": by_delay[d]["range"],
            "gap_exceeds_noise": abs(gap) > by_delay[d]["range"],
        }

    reading = {
        "phase": 1,
        "kind": "exam-noise",
        "tier": "A",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": "how far a Tier A score moves between houses, so a gap can be read",
        "would_refute": REFUTES,
        "core": core.name,
        "size_b": args.size,
        "seeds": args.seeds,
        "house": {
            "facts": args.facts,
            "turns": args.turns,
            "negatives": args.negatives,
            "delays": args.delays,
        },
        "budgets": {"reply": args.reply_budget, "answer": args.answer_budget},
        "preamble": PREAMBLE,
        # THE FULL-CONTEXT BASELINE IS NOT IN THIS READING and its spread is
        # therefore unknown. At 2.7 hours a house, five houses is most of a
        # fortnight of card time. Saying so here is the point; a reading that
        # quietly omitted it would read as though the comparison had been made.
        "full_context_spread": None,
        "runs": runs,
        "tier_a_spread_by_delay": by_delay,
        "blind_spread_by_delay": blind_by_delay,
        "verdict": readable,
    }

    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase1-noise-{stamp}.json"
    path.write_text(json.dumps(reading, indent=2) + "\n", encoding="utf-8")

    print("\ntier A spread by delay:")
    print(json.dumps(by_delay, indent=2))
    print("\nis the gap to blind bigger than the noise?")
    print(json.dumps(readable, indent=2))
    print(f"\nwrote {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
