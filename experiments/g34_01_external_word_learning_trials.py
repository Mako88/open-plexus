"""g34-01: does the mechanism solve trials this project did not design?

Every grounding number so far is from a stream written here, and `DECISIONS.md`
§10 names that as the standing weakness: *"the instruments are all self-designed"*.
These are the stimuli from published cross-situational word-learning experiments
(`kachergis/XSLmodels`, fetched by `tools/fetch_kachergis.py`).

A trial shows several words and several objects, unpaired. The correct mapping is
known from the file — pair `n` means word `n` and object `n` — so this scores
without any human data, which is fortunate because the human accuracies are not
reachable without an RData reader. **External stimuli, not an external benchmark**,
and `xsl.py` says so at more length.

Two questions, and the second is the one `g33-04` is waiting on:

  1. does the mechanism recover the pairings, and where does it fail;
  2. **is the ranking bimodal on data we did not generate** — because `cliff`
     needs a cliff, note 058 measured real language as a slope, and every
     bimodality this project has seen was in a world it built.

Predictions: `experiments/sweeps/g34-01-external-word-learning-trials.txt`
"""

from __future__ import annotations

import pathlib
import sys
import time

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from experiments import harness  # noqa: E402
from openplexus.grounding import (STATISTICS, CoOccurrence,  # noqa: E402
                                  cliff, equivalence_classes, neighbours,
                                  score_classes)
from openplexus.tasks import xsl  # noqa: E402

DATA = ROOT / "data" / "kachergis"
ARMS = ("count", "conditional")
BOUNDS = (1, None)


def _profile(index: CoOccurrence, statistic, surfaces: int) -> tuple[float, float]:
    """Mean largest gap in a surface's ranking, and the mean cut it implies.

    The quantity note 058 reported for real co-occurrence (0.059) against the
    designed families task (0.424), computed the same way so the three are
    comparable.
    """
    gaps, cuts = [], []
    for surface in range(surfaces):
        scores = sorted((statistic(index, surface, other)
                         for other in index.partners(surface)), reverse=True)
        scores = [s for s in scores if s > 0.0][:16]
        if len(scores) < 2:
            continue
        keep = cliff(scores)
        gaps.append(scores[keep - 1] - scores[keep] if keep < len(scores) else 0.0)
        cuts.append(keep)
    return (sum(gaps) / len(gaps) if gaps else 0.0,
            sum(cuts) / len(cuts) if cuts else 0.0)


def main() -> None:
    harness.parse_args(__doc__)
    started = time.time()
    paths = xsl.available(DATA)
    if not paths:
        raise SystemExit(
            f"no trial files in {DATA}. Run: python tools/fetch_kachergis.py")

    print(f"g34-01  {len(paths)} published conditions, ground truth from the "
          f"file, NO human data")
    print("        f1 floor for a two-surface concept recovered alone is "
          "0.6667, not 0.5\n")

    header = (f"{'condition':<26}{'trials':>7}{'pairs':>6}{'reps':>10}"
              f"{'count':>8}{'cond':>8}{'derived':>9}{'gap':>7}{'cut':>6}")
    print(header)
    print("-" * len(header))

    totals: dict[str, list[float]] = {a: [] for a in ARMS}
    totals["derived"] = []
    for path in paths:
        condition = xsl.read(path)
        index = CoOccurrence()
        for trial in condition.trials:
            index.observe(trial)
        truth = condition.classes()
        seen = sorted(set(condition.appearances().values()))
        reps = "-".join(str(v) for v in seen) if len(seen) < 4 else "varied"

        scores = {}
        for arm in ARMS:
            recovered = equivalence_classes(index, STATISTICS[arm], 1)
            scores[arm] = score_classes(recovered, truth)["f1"]
            totals[arm].append(scores[arm])
        recovered = equivalence_classes(index, STATISTICS["conditional"], None)
        derived = score_classes(recovered, truth)["f1"]
        totals["derived"].append(derived)

        gap, cut = _profile(index, STATISTICS["conditional"],
                            condition.surfaces())
        print(f"{condition.name:<26}{len(condition.trials):>7}"
              f"{condition.pairs:>6}{reps:>10}{scores['count']:>8.4f}"
              f"{scores['conditional']:>8.4f}{derived:>9.4f}{gap:>7.3f}"
              f"{cut:>6.1f}")

    print()
    for arm in ("count", "conditional", "derived"):
        values = totals[arm]
        print(f"  {arm:<12} mean {sum(values) / len(values):.4f}   "
              f"worst {min(values):.4f}   perfect "
              f"{sum(1 for v in values if v >= 0.9999)}/{len(values)}")

    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
