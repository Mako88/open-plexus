"""Does the O(N) full-context baseline answer like the O(N-squared) one it replaces?

THE OPTIMISATION IS ONLY WORTH HAVING IF IT CHANGES NOTHING, and "the states
match to 1.9e-5" is not by itself proof of that -- `readings/phase1-chunk-
invariance-*.json` says the states match, this says the SCORES do.

THE CONFOUND THIS IS BUILT AROUND: generation is sampled, not greedy. Two runs
of the identical method on the identical state do not produce identical answers,
so "old and new disagreed on 12 answers" means nothing on its own. The
experiment therefore measures BOTH:

  old vs new   -- how much the method change moved things
  new vs new   -- how much running the same method twice moves things

If the first is no bigger than the second, the methods are the same within the
noise the exam already has. If it is bigger, the optimisation changed something
and must be reverted.

AND IT MEASURES A THING NOBODY HAD LOOKED AT: how much an exam score moves from
SAMPLING ALONE, on one house, with one method. That number belongs beside every
reading, and until now the only noise anyone had thought about was between
houses.

  uv run python scripts/spike_baseline_equivalence.py --size 1.5
"""

from __future__ import annotations

import argparse
import json
from datetime import UTC, datetime
from pathlib import Path
from typing import Any

from sylvatica.core.base import Sampling
from sylvatica.exam import Tier, generate_house, judge, questions_at
from sylvatica.exam.run import Answered, ExamResult, run_full_context
from sylvatica.loop.turn import (
    PREAMBLE,
    SEPARATOR,
    STOP_STRINGS,
    USER_PREFIX,
    format_prompt,
    trim_at_stop,
)


def run_full_context_monolithic(core: Any, house, answer_budget: int = 32) -> ExamResult:
    """The ORIGINAL baseline, kept here and nowhere else: re-read everything, per question.

    This is what `run_full_context` used to do and what it is being checked
    against. It is not in the package because nothing should call it -- it is a
    reference implementation for one experiment, and leaving a 2.5-hour code path
    in `exam/` invites somebody to run it by accident.
    """
    schedule = questions_at(house)
    result = ExamResult(tier=Tier.A)
    for turn in sorted(schedule):
        context = "\n\n".join(f"{USER_PREFIX}{line}" for line in house.turns[: turn + 1])
        for question in schedule[turn]:
            prompt = PREAMBLE + context + SEPARATOR + format_prompt(question.text)
            out, _, _ = core.generate(
                core.encode(prompt), None, answer_budget,
                Sampling(stop_strings=STOP_STRINGS),
            )
            said = trim_at_stop(core.decode(out), STOP_STRINGS)
            correct, invented, echoed = judge(question, said)
            result.answers.append(
                Answered(
                    fact_id=question.fact_id,
                    kind=question.kind,
                    delay_turns=question.delay_turns,
                    asked=question.text,
                    wanted=question.answer,
                    said=said,
                    correct=correct,
                    invented=invented,
                    echoed=echoed,
                )
            )
    return result


def agreement(a: ExamResult, b: ExamResult) -> dict[str, float]:
    """How often two runs said the same thing, and scored the same way."""
    pairs = list(zip(a.answers, b.answers))
    same_text = sum(x.said.strip() == y.said.strip() for x, y in pairs)
    same_verdict = sum(x.correct == y.correct for x, y in pairs)
    return {
        "n": len(pairs),
        "identical_answers": round(same_text / len(pairs), 4),
        "same_correct_verdict": round(same_verdict / len(pairs), 4),
        "score_a": round(a.score().score, 4),
        "score_b": round(b.score().score, 4),
        "score_gap": round(abs(a.score().score - b.score().score), 4),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=float, default=1.5)
    parser.add_argument("--facts", type=int, default=15)
    parser.add_argument("--turns", type=int, default=80)
    parser.add_argument("--delays", type=int, nargs="+", default=[1, 5, 20])
    parser.add_argument("--negatives", type=int, default=5)
    parser.add_argument("--out", type=Path, default=Path("readings"))
    args = parser.parse_args()

    from sylvatica.core.checkpoints import ensure
    from sylvatica.core.rwkv_core import RwkvCore

    core = RwkvCore(ensure(args.size))
    house = generate_house(
        seed=11,
        n_facts=args.facts,
        n_turns=args.turns,
        delays=tuple(args.delays),
        negatives=args.negatives,
    )

    print("-- new (incremental), run 1 --", flush=True)
    new1 = run_full_context(core, house, progress=False)
    print("-- new (incremental), run 2 --", flush=True)
    new2 = run_full_context(core, house, progress=False)
    print("-- old (monolithic, re-reads everything) --", flush=True)
    old = run_full_context_monolithic(core, house)

    old_vs_new = agreement(old, new1)
    new_vs_new = agreement(new1, new2)

    # The verdict, computed rather than typed. The method change is acceptable if
    # it moved the score no further than running the same method twice does.
    verdict = {
        "method_change_moved_score_by": old_vs_new["score_gap"],
        "sampling_alone_moves_score_by": new_vs_new["score_gap"],
        "method_change_within_sampling_noise": (
            old_vs_new["score_gap"] <= new_vs_new["score_gap"]
        ),
    }

    reading = {
        "phase": 1,
        "kind": "baseline-equivalence",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": (
            "whether the O(N) full-context baseline answers like the "
            "O(N-squared) one, against how much sampling alone moves a score"
        ),
        "would_refute": (
            "the method change moving the score further than running the same "
            "method twice does. Then the optimisation is not free and reverts."
        ),
        "core": core.name,
        "size_b": args.size,
        "house": {
            "seed": 11,
            "facts": args.facts,
            "turns": args.turns,
            "delays": args.delays,
            "negatives": args.negatives,
            "questions": len(house.questions),
        },
        "old_vs_new": old_vs_new,
        "new_vs_new": new_vs_new,
        "cost_spent_new": new1.cost.row() if new1.cost else None,
        "cost_charged_new": new1.charged.row() if new1.charged else None,
        "verdict": verdict,
    }

    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase1-baseline-equivalence-{stamp}.json"
    path.write_text(json.dumps(reading, indent=2) + "\n", encoding="utf-8")

    print("\nold vs new:", json.dumps(old_vs_new, indent=2))
    print("new vs new:", json.dumps(new_vs_new, indent=2))
    print("verdict:", json.dumps(verdict, indent=2))
    print(f"wrote {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
