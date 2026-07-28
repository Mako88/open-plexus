"""Churn over a real network: did a departure reach backwards?

The assertion is NOT that a run with a departure agrees with the single-process
model. It should not — losing a quarter of the store's dimensions should change
later answers, and a test demanding otherwise would fail on correct behaviour.

The assertion is that it did not diverge EARLY:

    mismatches_before_departure == 0

A machine switching off may change what happens next. It must not change what
already happened. **That is the only field here that can fail**, and it is
checked before anything else is printed.
"""

from __future__ import annotations

from collections import defaultdict

from tools.recovery import mean_and_error
from tools.summarise_g12_01 import load_reports


def main() -> int:
    reports = load_reports()
    if not reports:
        print("no testbed reports matched")
        return 1

    departures = [r for r in reports if r.get("leave_at")]
    if not departures:
        print("no report has a departure; this sweep measures nothing without one")
        return 1

    broke = [r for r in departures if r.get("mismatches_before_departure")]
    if broke:
        print("== A DEPARTURE REACHED BACKWARDS ==")
        for report in broke:
            print(f"  nodes {report['nodes']}, absent {report['absent']}, "
                  f"leave_at {report['leave_at']}: "
                  f"{report['mismatches_before_departure']} mismatches BEFORE "
                  f"the departure step")
        print()
        print("  **C3 is refuted at the protocol level.** A machine switching")
        print("  off changed an answer that had already been given. Nothing")
        print("  about the model matters until this does not happen.")
        return 1

    print(f"{len(departures)} runs with a departure, "
          f"NONE diverging before it\n")

    cells: dict[tuple, list[dict]] = defaultdict(list)
    for report in departures:
        lost = len(report["absent"])
        cells[(report["nodes"], lost, report["leave_at"])].append(report)

    print("== mismatches AFTER the departure ==")
    print("   expected to be non-zero: losing dimensions should cost answers\n")
    print(f"{'nodes':>7}{'lost':>6}{'leave_at':>10}{'mismatches':>14}"
          f"{'completed':>11}")
    for key in sorted(cells):
        group = cells[key]
        mean, error = mean_and_error([r["mismatches"] for r in group])
        print(f"{key[0]:>7}{key[1]:>6}{key[2]:>10}"
              f"{mean:>9.1f} +/-{error:.1f}{len(group):>11}")

    silent = [key for key, group in cells.items()
              if all(r["mismatches"] == 0 for r in group)]
    if silent:
        print()
        print("== A DEPARTURE THAT COST NOTHING ==")
        for nodes, lost, leave_at in silent:
            print(f"  nodes {nodes}, lost {lost}, leave_at {leave_at}: "
                  f"zero mismatches after departure")
        print()
        print("  Losing a node changed no answer at all. Either the node held")
        print("  nothing the answer depended on, or **it never actually left**.")
        print("  The second is far more likely and is not a passing result.")

    print("\nA run with a departure is not required to agree. It is required")
    print("not to diverge early, and none of these did.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
