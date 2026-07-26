"""Shared plumbing so a sweep can run one seed per machine and be recombined.

A sweep is a grid of conditions measured at many seeds, and the seeds are
independent. Locally that is a serial loop; on CI it is one job per seed running
at once. This is the seam that lets the same script do both without the
experiment itself knowing which is happening.

    python experiments/g1_05_local.py                    # every seed, serial
    python experiments/g1_05_local.py --seed 3 --json out/3.json
    python experiments/harness.py --aggregate out/*.json

The aggregation deliberately reports **solved / stuck counts, not means**.
g1-03 established outcomes on this task are bimodal, so a mean describes a
mixture of two populations and no run that actually happened.
"""

from __future__ import annotations

import argparse
import glob
import json
from collections import defaultdict
from pathlib import Path

#: A run at or above this counts as having solved the task; at or below STUCK,
#: as never having got near it. The band between is reported separately rather
#: than being split, because how often runs land in it is itself a finding.
SOLVED, STUCK = 0.9, 0.2


def parse_args(description: str) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=description)
    parser.add_argument("--seed", type=int, default=None,
                        help="run this seed only; omit to run all of them")
    parser.add_argument("--width", type=int, default=None,
                        help="run this d_model only; omit to run all of them")
    parser.add_argument("--pairs", type=int, default=None,
                        help="run this n_pairs only; omit to run all of them")
    parser.add_argument("--decay", type=float, default=None,
                        help="run this memory decay only")
    parser.add_argument("--jitter", type=int, default=None,
                        help="delivery jitter in steps")
    parser.add_argument("--max-delay", type=int, default=4,
                        help="receiver buffer depth in steps")
    parser.add_argument("--epochs", type=int, default=None,
                        help="training budget in epochs")
    parser.add_argument("--churn", type=float, default=None,
                        help="fraction of dimensions a departing machine takes")
    parser.add_argument("--drop", type=float, default=None,
                        help="fraction of events lost entirely")
    parser.add_argument("--sweep", default=None,
                        choices=("widths", "decay", "identity", "degrade", "drops"),
                        help="which sub-sweep to run when a script has more than one")
    parser.add_argument("--json", type=Path, default=None,
                        help="write results here as JSON instead of a table")
    parser.add_argument("--aggregate", nargs="+", metavar="FILE",
                        help="combine JSON files from a matrix run into a table")
    return parser.parse_args()


def emit(records: list[dict], path: Path | None) -> None:
    """Write results, or print them as a table."""
    if path is None:
        table(records)
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(records, indent=1), encoding="utf-8")
    print(f"wrote {len(records)} records to {path}")


def load(patterns: list[str]) -> list[dict]:
    records: list[dict] = []
    for pattern in patterns:
        for name in sorted(glob.glob(pattern)):
            records.extend(json.loads(Path(name).read_text(encoding="utf-8")))
    if not records:
        raise SystemExit(f"no records matched {patterns}")
    return records


def table(records: list[dict]) -> None:
    """Print solved/stuck counts per condition, and every individual accuracy.

    The per-run line is not decoration. An aggregate hides bimodality, and
    bimodality is the thing this project keeps needing to see — g1-03's headline
    only became visible when the individual seeds were printed.
    """
    by_condition: dict[str, list[dict]] = defaultdict(list)
    for record in records:
        by_condition[record["condition"]].append(record)

    header = (f"{'condition':<18}{'solved':>9}{'stuck':>9}{'between':>10}"
              f"{'worst':>8}{'best':>7}")
    print(header)
    print("-" * len(header))
    for condition, runs in by_condition.items():
        accs = sorted(r["accuracy"] for r in runs)
        n = len(accs)
        solved = sum(a >= SOLVED for a in accs)
        stuck = sum(a <= STUCK for a in accs)
        print(f"{condition:<18}{f'{solved}/{n}':>9}{f'{stuck}/{n}':>9}"
              f"{f'{n-solved-stuck}/{n}':>10}{accs[0]:>8.3f}{accs[-1]:>7.3f}")
        print(f"    {' '.join(f'{a:.2f}' for a in accs)}")


if __name__ == "__main__":
    args = parse_args(__doc__)
    if not args.aggregate:
        raise SystemExit("harness.py is only run directly to --aggregate")
    table(load(args.aggregate))
