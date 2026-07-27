"""The two refusals, in one place, because five hand-copies had already drifted.

Every sweep here reports the same ratio — how much of the oracle's advantage an
arm without foresight recovers:

    (arm - none) / (oracle - none)

and every summariser must refuse to report it in the same two situations. Both
refusals exist because both failures have already cost this project a result:

**The floor arm has to work.** If `none` is at or below the trivial floor — the
accuracy of guessing — then the arm is not a floor, it is a second failure, and
the difference between two failures is not an advantage to recover a fraction of.
[g8-01's seq-1536 row](../experiments/sweeps/g8-01-a-gate-without-an-oracle.txt)
was withdrawn for this after being reported.

**The gap has to beat the noise.** If `oracle - none` is no larger than the
spread between seeds, the denominator is inside the measurement error and the
ratio is arithmetic on noise.

There is a third rule and it is the one that bit hardest: **never pick a cell by
maximising `oracle - none`.** A cell whose floor arm has collapsed has an
enormous gap and wins that comparison every time, so a summariser that selects
that way actively seeks out the broken cells. `best_by` exists so that the
selection happens *after* the refusals rather than before.

The floor is a **parameter**, not a constant. It is `1/n_pairs + (1 - 1/n_pairs)
/ n_values` = 0.34375 for MQAR and `1/8` for `reward_recall`; freezing either one
in here would be the same class of mistake as the drift this module replaces.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict
from typing import Iterable, NamedTuple

#: MQAR with 4 pairs and 8 values: name one of the pairs, or guess a value.
MQAR_FLOOR = 1 / 4 + (1 - 1 / 4) / 8
#: reward_recall asks only about rewarded cues, so guessing is 1 in n_values.
REWARD_RECALL_FLOOR = 1 / 8


def load(pattern: str | None = None) -> list[dict]:
    """Every record from every matching file, or an empty list."""
    if pattern is None:
        pattern = sys.argv[1] if len(sys.argv) > 1 else "out/*.json"
    return [record for path in sorted(glob.glob(pattern))
            for record in json.load(open(path))]


def by_cell(rows: Iterable[dict], *fields: str,
            metric: str = "accuracy") -> dict[tuple, dict[int, float]]:
    """One metric per seed, keyed by the swept fields and then the arm.

    The arm is always the last key, because every caller needs to compare arms
    within a cell and none needs to compare cells within an arm.

    `metric` names the quantity, and it is a parameter rather than a second
    function because g9-02 reports two of them -- `accuracy` is first asks, which
    is retention, and `accuracy_all` includes repeats, which is short-term echo.
    Averaging them hides the number that matters, so that sweep prints both and
    calls this twice. Every other caller wants the default.
    """
    cells: dict[tuple, dict[int, float]] = defaultdict(dict)
    for row in rows:
        key = tuple(row[field] for field in fields) + (row["arm"],)
        cells[key][row["seed"]] = row[metric]
    return cells


class Cell(NamedTuple):
    """One cell's verdict. `ratios` is empty exactly when `refused` is set."""

    ratios: dict[str, float]
    means: dict[str, float]
    spread: float
    gap: float
    #: Why there is no ratio, in words, or None if there is one.
    refused: str | None

    def ratio(self, arm: str) -> float | None:
        return self.ratios.get(arm)

    def text(self, arm: str, width: int = 9) -> str:
        """The cell as a column, so refusals are visible rather than blank."""
        value = self.ratios.get(arm)
        return ("undefined" if value is None else f"{value:.2f}").rjust(width)


def assess(cells: dict[tuple, dict[int, float]], key: tuple, arms: Iterable[str],
           floor: float) -> Cell | None:
    """Ratios for every arm in one cell, or a Cell saying why there are none.

    Returns None only when the cell is missing entirely — a job that did not
    return is a different thing from a job whose result cannot be interpreted,
    and printing them the same way hides dispatch failures.
    """
    arms = list(arms)
    means: dict[str, float] = {}
    spread = 0.0
    for arm in arms:
        by_seed = cells.get(key + (arm,), {})
        if not by_seed:
            return None
        values = list(by_seed.values())
        means[arm] = sum(values) / len(values)
        spread = max(spread, max(values) - min(values))

    gap = means["oracle"] - means["none"]
    if means["none"] <= floor:
        return Cell({}, means, spread, gap,
                    f"floor arm {means['none']:.3f} at or below the trivial "
                    f"floor {floor:.3f}: two failures, not a difficulty")
    if gap <= spread:
        return Cell({}, means, spread, gap,
                    f"oracle's advantage {gap:.3f} is not larger than the seed "
                    f"spread {spread:.3f}: the denominator is noise")
    return Cell({arm: (means[arm] - means["none"]) / gap for arm in arms},
                means, spread, gap, None)


def margin(cell: Cell) -> float:
    """The seed spread in ratio units: how much of a LEAD means nothing.

    `assess` refuses a cell whose gap is inside the seed spread. This is the
    same quantity one step further on: a *difference between two ratios* is
    noise by the same standard. Without it, taking `max` over a swept axis
    breaks near-ties arbitrarily and reports a moving optimum from numbers that
    are the same number — which g9-12's summariser did on its first smoke run,
    on fabricated records where every rate was identical by construction.
    """
    return cell.spread / cell.gap if cell.gap else float("inf")


def winner(cells: dict, arm: str, incumbent) -> tuple:
    """(best key, its lead over `incumbent`, the noise floor on that lead).

    `cells` maps the swept value to its Cell. Returns None when the incumbent
    is missing, because a lead has to be a lead over something. A caller that
    finds `lead <= noise` must say "tied" rather than name a winner.
    """
    if incumbent not in cells:
        return None
    key = max(cells, key=lambda k: cells[k].ratios[arm])
    return key, cells[key].ratios[arm] - cells[incumbent].ratios[arm], \
        margin(cells[incumbent])


def best_by(candidates: Iterable[tuple[object, Cell | None]], arm: str):
    """The candidate an arm recovers most from, among those not refused.

    Selecting on `gap` instead is how a summariser ends up preferring the cells
    where the floor arm collapsed, because collapse maximises the gap. Refused
    and missing candidates are never eligible.
    """
    usable = [(label, cell) for label, cell in candidates
              if cell is not None and cell.refused is None and arm in cell.ratios]
    if not usable:
        return None
    return max(usable, key=lambda pair: pair[1].ratios[arm])
