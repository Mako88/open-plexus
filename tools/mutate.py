"""Verify that the tests can fail.

A passing test is not evidence. This harness breaks a named mechanism on
purpose, runs the suite, and requires it to go red. A mutation the suite does
*not* catch marks a vacuous region of the test set — the tests there are
asserting something other than what they claim (CLAUDE.md rule 10).

It also fails loudly when it goes stale. If a refactor moves a line a mutation
targets, the mutation is reported as SOURCE MOVED and the run fails, rather than
quietly passing because nothing was mutated.

    python tools/mutate.py

Safety: the original file is written to a sibling `.bak` before any edit, so an
interrupted run is recoverable from disk. `try`/`finally` does not run when the
process is killed, and a timeout will eventually leave an edit in the working
tree — so on startup any leftover `.bak` is restored before anything else.
"""

from __future__ import annotations

import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MQAR = ROOT / "openplexus" / "tasks" / "mqar.py"
BASELINES = ROOT / "openplexus" / "baselines.py"
INDUCTION = ROOT / "openplexus" / "models" / "induction.py"


@dataclass(frozen=True)
class Mutation:
    """One deliberate defect.

    Attributes:
        name: Short identifier shown in the report.
        breaks: What mechanism this disables, in the terms a reader would use.
        path: File to edit.
        old: Exact source text to replace. Must appear exactly once.
        new: What to replace it with.
    """

    name: str
    breaks: str
    path: Path
    old: str
    new: str


MUTATIONS = [
    Mutation(
        name="filler-collides-with-keys",
        breaks="filler may reuse a key this sequence queries, making the task ill-posed",
        path=MQAR,
        old="spare_keys = tuple(k for k in range(config.n_keys) if k not in pairs)",
        new="spare_keys = tuple(range(config.n_keys))",
    ),
    Mutation(
        name="query-only-the-first-pair",
        breaks="the multi-query property that makes the benchmark discriminating",
        path=MQAR,
        old="    slots = [True] * config.n_pairs + [False] * n_filler",
        new="    slots = [True] + [False] * (n_filler + config.n_pairs - 1)",
    ),
    Mutation(
        name="ignore-the-seed",
        breaks="seeding, so every sequence in a dataset becomes identical",
        path=MQAR,
        old="rng = random.Random(config.seed)",
        new="rng = random.Random(0)",
    ),
    Mutation(
        name="wrong-recall-target",
        breaks="the task definition itself: query targets stop being the paired value",
        path=MQAR,
        old="            targets.append(pairs[key])",
        new="            targets.append(_value_token(config, 0))",
    ),
    Mutation(
        name="filler-mode-disconnected",
        breaks="the filler dial, so 'random' and 'structured' become the same condition",
        path=MQAR,
        old='    if config.filler == "random":',
        new="    if False:",
    ),
    Mutation(
        name="value-alphabet-collapsed",
        breaks="n_values, so the base rate is wrong in every configuration",
        path=MQAR,
        old="pairs = {k: _value_token(config, rng.randrange(config.n_values)) for k in keys}",
        new="pairs = {k: _value_token(config, rng.randrange(1)) for k in keys}",
    ),
    Mutation(
        name="query-order-not-shuffled",
        breaks="the shuffle, so query order leaks pair order and `positional` solves the task",
        path=MQAR,
        old="    rng.shuffle(query_order)",
        new="    pass",
    ),
    Mutation(
        name="trivial-floor-is-the-base-rate",
        breaks="the floor, reverting it to the flattering 1/n_values a model must NOT be judged against",
        path=MQAR,
        old="        return 1 / self.n_pairs + (1 - 1 / self.n_pairs) / self.n_values",
        new="        return 1 / self.n_values",
    ),
    Mutation(
        name="oracle-off-by-one",
        breaks="the oracle, which is the only check that the task is answerable at all",
        path=BASELINES,
        old="    return sequence.pairs[sequence.tokens[position]]",
        new="    return sequence.pairs[sequence.tokens[position]] + 1",
    ),
    Mutation(
        name="constant-baseline-not-fitted",
        breaks="fitting, so the base rate stops tracking the data it is meant to describe",
        path=BASELINES,
        old="    most_common = counts.most_common(1)[0][0]",
        new="    most_common = min(counts)",
    ),
    Mutation(
        name="accuracy-scores-every-position",
        breaks="scoring, diluting every measurement with positions where no answer is required",
        path=BASELINES,
        old="        for position in sequence.query_positions:",
        new="        for position in range(len(sequence.tokens)):",
    ),
    Mutation(
        name="autoregressive-flag-inert",
        breaks="the autoregressive layout, so docs/notes/001 P2 is silently unsatisfied again",
        path=MQAR,
        old="    query_width = 2 if config.autoregressive else 1",
        new="    query_width = 1",
    ),
    Mutation(
        name="answer-positions-classified-as-filler",
        breaks="the answer's classification, excluding it from the task column of the probe",
        path=MQAR,
        old='            kinds[i] = "answer"',
        new='            kinds[i] = "filler"',
    ),
    Mutation(
        name="lookup-uses-first-occurrence",
        breaks="most-recent lookup, silently answering from stale evidence",
        path=INDUCTION,
        old="        last_seen[token] = position",
        new="        last_seen.setdefault(token, position)",
    ),
    Mutation(
        name="lookup-off-by-one",
        breaks="the lookup, returning what the token WAS rather than what followed it",
        path=INDUCTION,
        old="            one_hot[tokens[previous + 1]] = 1.0",
        new="            one_hot[tokens[previous]] = 1.0",
    ),
    Mutation(
        name="lookup-ignores-the-current-token",
        breaks="input-dependence, turning the lookup into a fixed filter",
        path=INDUCTION,
        old="        previous = last_seen.get(token)",
        new="        previous = position - 1 if position else None",
    ),
]


def restore_any_leftovers() -> None:
    """Recover from a previous run that was killed mid-mutation."""
    for bak in ROOT.rglob("*.py.bak"):
        target = bak.with_suffix("")
        print(f"!! recovering {target.relative_to(ROOT)} from an interrupted run")
        target.write_text(bak.read_text(encoding="utf-8"), encoding="utf-8")
        bak.unlink()


def suite_passes() -> bool:
    result = subprocess.run(
        [sys.executable, "-m", "unittest", "discover", "-s", "tests", "-t", ".", "-q"],
        cwd=ROOT, capture_output=True, text=True,
    )
    return result.returncode == 0


def apply(mutation: Mutation) -> str | None:
    """Apply a mutation. Returns an error string if the source has moved."""
    source = mutation.path.read_text(encoding="utf-8")
    occurrences = source.count(mutation.old)
    if occurrences != 1:
        return f"expected 1 occurrence of the target text, found {occurrences}"
    bak = mutation.path.with_suffix(".py.bak")
    bak.write_text(source, encoding="utf-8")
    mutation.path.write_text(source.replace(mutation.old, mutation.new), encoding="utf-8")
    return None


def revert(mutation: Mutation) -> None:
    bak = mutation.path.with_suffix(".py.bak")
    if bak.exists():
        mutation.path.write_text(bak.read_text(encoding="utf-8"), encoding="utf-8")
        bak.unlink()


def main() -> int:
    restore_any_leftovers()

    if not suite_passes():
        print("The suite is red before any mutation. Fix that first.")
        return 2

    survived, stale = [], []
    for mutation in MUTATIONS:
        problem = apply(mutation)
        if problem:
            stale.append(mutation)
            print(f"SOURCE MOVED  {mutation.name}: {problem}")
            continue
        try:
            caught = not suite_passes()
        finally:
            revert(mutation)
        if caught:
            print(f"caught        {mutation.name}")
        else:
            survived.append(mutation)
            print(f"SURVIVED      {mutation.name} — breaks {mutation.breaks}")

    print(f"\n{len(MUTATIONS) - len(survived) - len(stale)}/{len(MUTATIONS)} caught")
    if stale:
        print(f"{len(stale)} mutation(s) could not be applied — the harness is stale "
              "and is not checking what it claims to.")
    if survived:
        print("A surviving mutation means the tests covering that mechanism are "
              "vacuous. Strengthen them (rule 10), do not delete the mutation.")
    return 1 if (survived or stale) else 0


if __name__ == "__main__":
    raise SystemExit(main())
