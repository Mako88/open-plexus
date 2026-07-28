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
import statistics
import sys
from collections import defaultdict
from typing import Iterable, NamedTuple, Sequence

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


def require(rows: Iterable[dict], *fields: str) -> list[dict]:
    """Only the records carrying every named field, saying what it dropped.

    Every workflow here uploads `out/*.json` wholesale and `load` reads whatever
    matches, so one stray file puts foreign records into a sweep's results. That
    happened: a local diagnostic's output was committed to `out/`, and from then
    on EVERY artifact of every sweep carried it. Each artifact held eight files
    where four were results.

    Filtered rather than refused, because a job may legitimately write more than
    one kind of record — and **printed rather than silently dropped**, because a
    summariser that quietly discards half its input reports a confident number
    computed from whatever survived.
    """
    rows = list(rows)
    fields = tuple(fields)
    kept = [row for row in rows if all(field in row for field in fields)]
    if len(kept) != len(rows):
        print(f"  !! dropped {len(rows) - len(kept)} of {len(rows)} records "
              f"lacking {', '.join(fields)} -- something else wrote to the same "
              f"output directory")
    return kept


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

    **Records that do not carry every field this asks for are skipped**, and the
    count is printed. Every workflow uploads `out/*.json` wholesale and `load`
    reads whatever matches, so one stray file puts foreign records into a
    sweep's results — which happened, when a local diagnostic's output was
    committed to `out/` and from then on rode along in EVERY artifact of every
    sweep.

    The filtering lives here rather than in each summariser because this is the
    one function all of them go through, and patching twenty callers protects
    whichever nineteen were remembered.
    """
    cells: dict[tuple, dict[int, float]] = defaultdict(dict)
    for row in require(rows, *fields, "arm", "seed", metric):
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


def per_seed(cells: dict[tuple, dict[int, float]], key: tuple, arm: str,
             floor: float) -> list[float]:
    """The recovery ratio computed SEPARATELY for each seed, then returned.

    `assess` averages accuracies across seeds and divides once. That mixes two
    different things into one number: how much better this arm is, and how hard
    this seed's data happened to be. A seed whose `none` ran low and whose
    `oracle` ran high has a large gap for reasons that have nothing to do with
    the arm being scored.

    Computing the ratio within a seed — against THAT seed's own floor and its
    own ceiling — pairs the comparison and removes the second thing entirely.
    CLAUDE.md's rule about per-seed values rather than means says to do this;
    the ratio was the one place it had not been done, because the ratio was
    defined on the means before the rule existed.

    Seeds whose own gap is at or below zero are dropped rather than returned as
    enormous ratios. A seed where the oracle did not beat the floor is a seed
    that measured nothing, and one of them can otherwise dominate a mean.
    """
    usable: list[float] = []
    seeds = cells.get(key + ("none",), {})
    for seed in sorted(seeds):
        try:
            none = cells[key + ("none",)][seed]
            oracle = cells[key + ("oracle",)][seed]
            value = cells[key + (arm,)][seed]
        except KeyError:
            continue
        gap = oracle - none
        if none <= floor or gap <= 0.0:
            continue
        usable.append((value - none) / gap)
    return usable


def mean_and_error(values: Sequence[float]) -> tuple[float, float]:
    """Mean and the standard error OF THAT MEAN, which shrinks with more seeds.

    This is the quantity `margin` is not. `margin` divides the seed RANGE by
    the gap, and a range grows with sample size — so a run at twelve seeds
    reports a wider one than the same experiment at three even when nothing
    about the underlying variability changed.

    That is fine for what `margin` is used for, comparing arms measured at the
    same seed count, and wrong for the thing it looks like it does. Anything
    asking whether MORE SEEDS would sharpen a comparison has to use this.

    A single value has no error to report and gets `inf`, so a caller comparing
    against it can never conclude a difference is real from one seed.
    """
    if not values:
        raise ValueError("no values to average")
    mean = sum(values) / len(values)
    if len(values) < 2:
        return mean, float("inf")
    variance = sum((v - mean) ** 2 for v in values) / (len(values) - 1)
    return mean, (variance / len(values)) ** 0.5


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


def spread(values: list[float], places: int = 3) -> str:
    """Mean and standard error, or a plain value when there is only one.

    Every g13 summariser wrote this and two of them wrote it identically, which
    `check_duplication` caught. It lives here for the same reason `load` does:
    the shared thing gets extracted rather than parallelised.

    NaN is dropped rather than propagated -- an AUC over an empty bucket is not
    a measurement, and one such cell should not erase seven good ones.
    """
    clean = [v for v in values if v is not None and v == v]
    if not clean:
        return "--"
    if len(clean) < 2:
        return f"{clean[0]:.{places}f} (1 seed)"
    error = statistics.stdev(clean) / len(clean) ** 0.5
    return f"{statistics.mean(clean):.{places}f} +/-{error:.{places}f}"


def paired_difference(cells: dict, a: str, b: str,
                      field: str = "accuracy") -> tuple[float, float]:
    """`a` minus `b`, computed INSIDE each seed and then averaged.

    **Not the difference of the means.** A seed whose data ran easy inflates
    every arm in it, so dividing once at the end charges the mechanism for
    variation that has nothing to do with it. CLAUDE.md's *per-seed values, not
    means* rule -- and the correction that found it (the retired backlog's item
    0b) measured the paired bound fifteen times tighter than the unpaired one.

    Returns `(mean difference, standard error)`; the error is 0.0 when only one
    seed is shared, which is honest rather than convenient -- there is no
    spread to report from a single pair.
    """
    left = {r["seed"]: r[field] for r in cells.get(a, [])}
    right = {r["seed"]: r[field] for r in cells.get(b, [])}
    shared = sorted(set(left) & set(right))
    if not shared:
        return 0.0, 0.0
    diffs = [left[s] - right[s] for s in shared]
    if len(diffs) < 2:
        return diffs[0], 0.0
    return (statistics.mean(diffs),
            statistics.stdev(diffs) / len(diffs) ** 0.5)


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
