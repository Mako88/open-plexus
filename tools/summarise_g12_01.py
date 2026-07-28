"""What the asynchrony window buys, with the repeats note 014 said it needed.

Note 014 measured the window's speedup once and marked the number unreliable:
*"timing is the noisiest thing this project has ever measured"*, no repeats, no
error bars. This reports per-repeat values and the spread, because a speedup
inside its own spread is not a speedup.

**Agreement is checked first and refuses everything downstream.** Under an
impaired link a network can be slow or it can be WRONG, and a run that merely
finished does not distinguish them. A timing number from a run that disagreed
with the single-process model is a measurement of nothing.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict

from tools.recovery import mean_and_error


def load_reports(pattern: str | None = None) -> list[dict]:
    """Every testbed report matching, tolerating the driver's leading noise.

    `run.py` prints progress before the JSON, so each file is sliced from its
    first brace rather than parsed whole.
    """
    if pattern is None:
        pattern = sys.argv[1] if len(sys.argv) > 1 else "out/*.json"
    reports = []
    for path in sorted(glob.glob(pattern)):
        text = open(path, encoding="utf-8").read()
        if "{" in text:
            reports.append(json.loads(text[text.index("{"):]))
    return reports


def link_of(report: dict) -> str:
    """A readable name for the impairment, from the report's own fields."""
    if not report.get("delay") and not report.get("loss"):
        return "clean"
    return (f"{report.get('delay') or '0ms'}"
            f"/{report.get('jitter') or '0ms'}"
            f"/{report.get('loss') or '0%'}")


def main() -> int:
    reports = load_reports()
    if not reports:
        print("no testbed reports matched")
        return 1

    cells: dict[tuple, list[dict]] = defaultdict(list)
    for report in reports:
        cells[(link_of(report), report["window"])].append(report)

    disagreed = [(key, r) for key, group in cells.items() for r in group
                 if not r.get("agrees_with_one_process") or r.get("mismatches")]
    if disagreed:
        print("== THE NETWORK DISAGREED WITH THE SINGLE-PROCESS MODEL ==")
        for (link, window), report in disagreed:
            print(f"  link {link}, window {window}: "
                  f"{report.get('mismatches')} mismatches")
        print()
        print("  **Every timing number below is void.** Under an impaired link a")
        print("  network can be slow or it can be WRONG, and only agreement")
        print("  distinguishes them. Fix correctness before reading speed.")
        return 1

    print(f"{len(reports)} runs, all agreeing with the single-process model\n")
    links = sorted({key[0] for key in cells})
    windows = sorted({key[1] for key in cells})

    print("== seconds per step ==")
    print(f"{'link':>22}" + "".join(f"{'w=' + str(w):>18}" for w in windows))
    best: dict[str, dict[int, float]] = {}
    for link in links:
        line, row = f"{link:>22}", {}
        for window in windows:
            group = cells.get((link, window))
            if not group:
                line += f"{'missing':>18}"
                continue
            values = [r["seconds_per_step"] for r in group]
            mean, error = mean_and_error(values)
            row[window] = mean
            line += f"{mean:>11.5f} +/-{error:.5f}"
        best[link] = row
        print(line)

    print("\n== what the window buys, against lock-step ==")
    print("  speedup, and whether it clears the spread across repeats\n")
    for link in links:
        row = best.get(link, {})
        if 1 not in row:
            print(f"  {link:>22}: no window-1 cell, so no reference")
            continue
        for window in windows:
            if window == 1 or window not in row:
                continue
            group1, groupw = cells[(link, 1)], cells[(link, window)]
            spread = max(
                max(r["seconds_per_step"] for r in group1)
                - min(r["seconds_per_step"] for r in group1),
                max(r["seconds_per_step"] for r in groupw)
                - min(r["seconds_per_step"] for r in groupw))
            gap = row[1] - row[window]
            verdict = "real" if gap > spread else "INSIDE THE SPREAD"
            print(f"  {link:>22}  w1 -> w{window}: "
                  f"{row[1] / row[window]:>6.2f}x   gap {gap:.5f} "
                  f"vs spread {spread:.5f}   {verdict}")

    print("\nNote 014 saw 7.3x once, with no repeats, and said so. A speedup")
    print("inside its own spread is not a speedup.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
