"""Whether a round is kept: by its paired readings against the system before it, nothing tuned.
Every regression line (the skills the system already has) must not fall by `hold` standard
errors or more on the shared questions, and at least one gain line (what the round was for) must
rise by `gain` or more. A round that only holds is not kept either: it cost card time for nothing.
"""

from __future__ import annotations


def paired_se(made_right: int, made_wrong: int) -> float:
    n = made_right + made_wrong
    return 0.0 if n == 0 else (made_right - made_wrong) / n ** 0.5


def verdict(regressions: dict[str, tuple[int, int]], gains: dict[str, tuple[int, int]],
            hold: float = 2.0, gain: float = 2.0) -> dict:
    """`regressions` and `gains` map a reading's purpose to (made right, made wrong) against the
    system before the round. Returns {"keep", "why", "lines"}."""
    lines = {k: round(paired_se(*v), 1) for k, v in {**regressions, **gains}.items()}
    lost = [k for k in regressions if lines[k] <= -hold]
    won = [k for k in gains if lines[k] >= gain]
    if lost:
        return {"keep": False, "why": f"lost {', '.join(lost)} ({', '.join(str(lines[k]) for k in lost)} standard errors)", "lines": lines}
    if not won:
        return {"keep": False, "why": "held everything and gained nothing clear", "lines": lines}
    return {"keep": True, "why": f"gained {', '.join(won)} and held the rest", "lines": lines}
