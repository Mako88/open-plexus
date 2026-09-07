"""Which preamble, and does the first one really teach the core to echo?

THE CLAIM BEING TESTED. `PREAMBLES["v1-restating"]` has an Assistant turn that
paraphrases its own User turn, and that is the only example of a reply a fresh
state contains. The claim is that a recurrent core learns from it in context and
starts answering questions by repeating them back.

THE EVIDENCE THAT PROMPTED IT, from `readings/phase1-exam-20260907T044344Z.json`,
echo rate by position through the conversation on the worst house:

    95 100 100 98 89 63 68 64 55 22 percent

with the correct rate climbing 5 -> 56 over the same span. THE CORE GOT BETTER AS
THE CONVERSATION WENT ON, which no account of memory decay produces and a bad
one-shot example being diluted by real replies does.

WOULD REFUTE IT: `none` echoing as much as `v1-restating`. Then the echoing is
the core's own behaviour on a long thread of statements, the preamble is
incidental, and the story above is wrong.

WOULD ALSO REFUTE IT, differently: `v2-example` echoing as much as
`v1-restating`. Then it is having an example at all, not the example being a
restatement, and the fix is wrong even if the diagnosis is right.

This also settles standing objection 2, whose settlement clause is "the same
held-out question set run under two formats at 1.5B".

  uv run python scripts/phase1_preamble.py --size 1.5 --seeds 0 4
"""

from __future__ import annotations

import argparse
import json
from datetime import UTC, datetime
from pathlib import Path

from sylvatica.exam import DEFAULT_DELAYS, Tier, generate_house, run_blind, run_exam, spread
from sylvatica.loop.turn import PREAMBLES

REFUTES = (
    "`none` echoing as much as `v1-restating` (then the preamble is incidental "
    "and the diagnosis is wrong), or `v2-example` echoing as much as "
    "`v1-restating` (then it is having an example at all, and the fix is wrong)."
)

BUCKETS = 10


def echo_by_position(result, house, buckets: int = BUCKETS) -> list[float]:
    """Echo rate through the conversation. THE SHAPE IS THE EVIDENCE.

    A flat line is a property of the core. A line that starts high and falls is
    a bad example being diluted by real replies -- and it is also, read
    carelessly, indistinguishable from a memory that improves with age, which is
    how this went unnoticed for a session.
    """
    n = len(house.turns)
    width = max(1, n // buckets)
    rows: list[tuple[int, bool]] = []
    for a in result.answers:
        if a.fact_id is None:
            continue
        rows.append((house.told_at[a.fact_id] + a.delay_turns, a.echoed))
    out = []
    for i in range(buckets):
        lo, hi = i * width, (i + 1) * width
        got = [e for t, e in rows if lo <= t < hi]
        out.append(round(sum(got) / len(got), 3) if got else None)
    return out


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=float, default=1.5)
    parser.add_argument("--facts", type=int, default=50)
    parser.add_argument("--turns", type=int, default=300)
    parser.add_argument("--seeds", type=int, nargs="+", default=[0, 4])
    parser.add_argument(
        "--preambles", nargs="+", default=["none", "v1-restating", "v2-example"]
    )
    parser.add_argument("--negatives", type=int, default=20)
    parser.add_argument("--delays", type=int, nargs="+", default=list(DEFAULT_DELAYS))
    parser.add_argument("--reply-budget", type=int, default=40)
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
    blind = {seed: round(run_blind(h).score().score, 4) for seed, h in houses.items()}

    runs = []
    for name in args.preambles:
        for seed, house in houses.items():
            arm = run_exam(
                core,
                house,
                Tier.A,
                reply_budget=args.reply_budget,
                answer_budget=args.answer_budget,
                state_path=Path("state") / "exam" / f"preamble-{name}-{seed}.pt",
                progress=False,
                preamble=PREAMBLES[name],
            )
            score = arm.score()
            positives = [a for a in arm.answers if a.fact_id is not None]
            negatives = len(arm.answers) - len(positives)
            row = {
                "preamble": name,
                "seed": seed,
                "score": round(score.score, 4),
                "blind": blind[seed],
                "echo_rate": round(sum(a.echoed for a in positives) / len(positives), 4),
                "echo_by_position": echo_by_position(arm, house),
                "by_delay": score.by_delay,
                "invented": score.invented,
                "negatives": negatives,
                # THE TWO COLUMNS THE IDENTITY ARM IS ACTUALLY ABOUT.
                #
                # `invention_rate`: a frame that tells the core it remembers
                # gives it a reason to answer confidently instead of declining.
                # That would raise the score on the positives while destroying
                # the honesty signal, and it would read as an improvement.
                #
                # `mean_reply_tokens`: a frame that makes the core more
                # talkative poisons its own state. The same core given a
                # transcript with none of its replies in it scored 45 points
                # higher over the first fifty turns.
                #
                # A preamble is only better if it moves the score WITHOUT moving
                # these, and neither is visible in a score.
                "invention_rate": round(score.invented / negatives, 4) if negatives else None,
                "mean_reply_tokens": round(arm.reply_tokens / len(house.turns), 2),
                "seconds": round(arm.seconds, 1),
            }
            runs.append(row)
            print(json.dumps(row), flush=True)

    by_preamble = {
        name: {
            "score": spread([r["score"] for r in runs if r["preamble"] == name]),
            "echo_rate": spread([r["echo_rate"] for r in runs if r["preamble"] == name]),
        }
        for name in args.preambles
    }

    verdict = {
        name: {
            "mean_echo_rate": by_preamble[name]["echo_rate"]["mean"],
            "mean_score": by_preamble[name]["score"]["mean"],
            "mean_invention_rate": round(
                sum(r["invention_rate"] for r in runs if r["preamble"] == name)
                / max(1, len([r for r in runs if r["preamble"] == name])),
                4,
            ),
            "mean_reply_tokens": round(
                sum(r["mean_reply_tokens"] for r in runs if r["preamble"] == name)
                / max(1, len([r for r in runs if r["preamble"] == name])),
                2,
            ),
        }
        for name in args.preambles
    }
    if {"none", "v1-restating", "v2-example"} <= set(args.preambles):
        v1 = by_preamble["v1-restating"]["echo_rate"]["mean"]
        verdict["v1_echoes_more_than_none"] = v1 > by_preamble["none"]["echo_rate"]["mean"]
        verdict["v2_echoes_less_than_v1"] = by_preamble["v2-example"]["echo_rate"]["mean"] < v1

    reading = {
        "phase": 1,
        "kind": "preamble",
        "tier": "A",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": "whether the first preamble taught the core to answer by echoing the question",
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
        "budgets": {"reply": args.reply_budget, "answer": args.answer_budget},
        "preambles": {name: PREAMBLES[name] for name in args.preambles},
        "runs": runs,
        "by_preamble": by_preamble,
        "verdict": verdict,
    }

    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase1-preamble-{stamp}.json"
    path.write_text(json.dumps(reading, indent=2) + "\n", encoding="utf-8")

    print("\nby preamble:", json.dumps(by_preamble, indent=2))
    print("verdict:", json.dumps(verdict, indent=2))
    print(f"wrote {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
