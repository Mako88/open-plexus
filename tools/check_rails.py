"""Repo-specific conventions, enforced as a ratchet rather than a rule.

Generic lint is a solved problem and adds nothing here. These three rails encode
failures that have already cost this project a result, which is the only reason
they are worth a check:

**R1 — a summariser reporting a recovery ratio imports `tools.recovery`.** Five
hand-copies of the two refusals had already drifted, and one had lost its floor
check entirely under a heading that named one
([g8-02](../experiments/sweeps/g8-02-when-the-statistics-are-real.txt)). Three
more were picking the learning rate that MAXIMISES `oracle - none`, which a
collapsed floor arm maximises. This rail is strict: every summariser passes today
and a new one has no excuse.

**R2 — a sweep file has PREDICTIONS, COST, and MEASURED ON sections.** A
prediction written after the run is a summary, not a commitment, and a sweep with
no costing is how "a quick control" became a ten-minute experiment on the local
machine.

**MEASURED ON is the newest and has the clearest provenance.** Seven times in one
week a conclusion rested on a number measured on one thing and applied to
another, and five of those were caught a cycle late. The section asks what each
number in the file was measured on, and what it is being compared against, at the
moment the writer is best placed to answer.

**R4 — a test method asserts something.** A test with no assertion runs the
code and reports success, which is the shape that passes while measuring
nothing. BACKLOG asked for this alongside the other two.

**Its reach is narrow and worth stating.** The failure that has actually cost
this project results is a test asserting a property the quantity does not have,
and no linter can tell a true property from a false one — four of them were
written in a single night here and every one was caught by running it, three by a
deliberate guard case and one by the mutation harness. R4 catches the cruder
thing only.

**R3 — an experiment goes through `experiments/harness.py`.** That is where
`refuse_if_mutating()` lives, and it is the one place the check cannot be
forgotten. An experiment that parses its own arguments can be run against a
deliberately mutated model and will produce plausible numbers with nothing in the
output saying otherwise.

## Why a ratchet

R2 and R3 are violated by legacy files: thirty-seven sweeps predate the COST
convention and eleven experiment scripts predate the harness. A rule that fails
on all of them is a rule that gets suppressed, and CLAUDE.md rule 18 prefers a
check that makes the mistake impossible over one that asks for more care.

So the known violations live in `tools/rails_baseline.json` and are exempt. A file
NOT in the baseline must comply. The baseline is also checked for staleness: an
entry naming a file that no longer exists, or a file that now complies, is an
error, because a baseline nobody prunes is a baseline that eventually exempts
everything.

Shrinking the baseline is the point. Growing it should be visible in review.

    python tools/check_rails.py
    python tools/check_rails.py --write-baseline    # after a deliberate change
"""

from __future__ import annotations

import ast
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
BASELINE = ROOT / "tools" / "rails_baseline.json"

#: A summariser that mentions either of these is computing a recovery ratio.
RATIO_HINTS = ("oracle", "recovery")
#: Sections a sweep file must carry, as line-initial headings.
#: A sweep file must say what it predicted, what it cost, and -- newest --
#: what its numbers were MEASURED ON.
#:
#: The third exists because the same error occurred seven times in one week and
#: was caught a cycle late five of those times: a number measured on one thing
#: and applied to another. A bigram's 210k-character context applied to a model
#: holding 64. A counter's available gain applied to a mechanism that captures a
#: quarter of it. A global write budget applied to a per-key problem. A cache
#: failure mode that a cache does not have. Two accuracies with chance floors
#: five hundred times apart.
#:
#: **Resolving to be careful failed five times. A section a rail can check for
#: does not depend on remembering.** It does not stop the error -- nothing here
#: can -- but it puts the question in front of whoever writes the file, at the
#: moment they are most able to answer it.
REQUIRED_SECTIONS = ("PREDICTIONS", "COST", "MEASURED ON")
#: Calls that count as asserting something. `assertRaises` appears as a context
#: manager rather than a bare call, and `np.testing.assert_*` is how the array
#: comparisons in this repo are written -- a check that only knew about
#: `self.assert...` would flag most of the model tests and be switched off.
ASSERTION_PREFIXES = ("assert", "fail")


def summarisers_missing_the_rail() -> list[str]:
    """R1. Reports a ratio, does not import the module holding the refusals."""
    offenders = []
    for path in sorted((ROOT / "tools").glob("summarise_*.py")):
        source = path.read_text(encoding="utf-8")
        reports_ratio = any(hint in source.lower() for hint in RATIO_HINTS)
        if reports_ratio and "tools.recovery" not in source:
            offenders.append(path.relative_to(ROOT).as_posix())
    return offenders


def sweeps_missing_a_section() -> dict[str, list[str]]:
    """R2. Which required sections each sweep file lacks."""
    offenders: dict[str, list[str]] = {}
    for path in sorted((ROOT / "experiments" / "sweeps").glob("*.txt")):
        text = path.read_text(encoding="utf-8")
        missing = [name for name in REQUIRED_SECTIONS
                   if not re.search(rf"^{name}\b", text, re.MULTILINE)]
        if missing:
            offenders[path.relative_to(ROOT).as_posix()] = missing
    return offenders


def _asserts(node: "ast.AST", helpers=None, seen=None) -> bool:
    """Does this function contain anything that can fail?

    Counts `self.assertX(...)`, `np.testing.assert_x(...)`, bare `assert`, and
    `self.skipTest(...)` -- a test that skips is declaring it measures nothing
    here, which is honest and is not the failure this rail is about.

    **Follows `self._helper(...)` calls into the same class**, because the two
    gradient tests in `test_attention.py` put their assertion in a shared
    `_fd_check` and are among the most carefully written tests in the
    repository. Flagging them would have been the false positive that gets a
    check switched off. `seen` guards mutual recursion.
    """
    helpers = helpers or {}
    seen = seen or set()
    for child in ast.walk(node):
        if isinstance(child, ast.Assert):
            return True
        if not isinstance(child, ast.Call):
            continue
        name = child.func.attr if isinstance(
            child.func, ast.Attribute) else getattr(child.func, "id", "")
        if name.startswith(ASSERTION_PREFIXES) or name == "skipTest":
            return True
        if name in helpers and name not in seen:
            seen.add(name)
            if _asserts(helpers[name], helpers, seen):
                return True
    return False


def tests_that_assert_nothing() -> list[str]:
    """R4. A test method whose body cannot fail."""
    offenders = []
    for path in sorted((ROOT / "tests").glob("*.py")):
        try:
            tree = ast.parse(path.read_text(encoding="utf-8"))
        except SyntaxError:
            continue
        for holder in ast.walk(tree):
            if not isinstance(holder, ast.ClassDef):
                continue
            helpers = {n.name: n for n in holder.body
                       if isinstance(n, (ast.FunctionDef, ast.AsyncFunctionDef))}
            for node in holder.body:
                if not isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
                    continue
                if not node.name.startswith("test"):
                    continue
                if not _asserts(node, helpers):
                    offenders.append(
                        f"{path.relative_to(ROOT).as_posix()}::{node.name}")
    return offenders


def experiments_bypassing_the_harness() -> list[str]:
    """R3. An experiment script that never imports the harness."""
    offenders = []
    for path in sorted((ROOT / "experiments").glob("*.py")):
        if path.name == "harness.py":
            continue
        source = path.read_text(encoding="utf-8")
        if "harness" not in source:
            offenders.append(path.relative_to(ROOT).as_posix())
    return offenders


def current() -> dict[str, list[str]]:
    return {
        "R1-summariser-imports-recovery": summarisers_missing_the_rail(),
        # Reported per (file, SECTION) rather than per file, so an exemption
        # covers only the section actually missing. Baselining whole files
        # would have exempted 26 sweeps from PREDICTIONS and COST as the price
        # of adding MEASURED ON -- a rail that quietly stops checking the two
        # things it was built for is worse than not adding the third.
        "R2-sweep-has-predictions-and-cost": sorted(
            f"{path} :: {section}"
            for path, sections in sweeps_missing_a_section().items()
            for section in sections),
        "R3-experiment-goes-through-harness":
            experiments_bypassing_the_harness(),
        "R4-test-asserts-something": tests_that_assert_nothing(),
    }


def read_baseline(path: Path, rails) -> dict[str, list[str]]:
    """The exemptions for these rails, or empty ones if the file is absent.

    Shared with `check_duplication.py`, which is where it started as a copy --
    caught by that tool, on the day it was written, against its own author. The
    rails are passed in rather than read from a module-level `current()` so the
    two callers can have different ones.
    """
    if not path.exists():
        return {rail: [] for rail in rails}
    stored = json.loads(path.read_text(encoding="utf-8"))
    return {rail: list(stored.get(rail, [])) for rail in rails}


def load_baseline() -> dict[str, list[str]]:
    return read_baseline(BASELINE, current())


def compare(found: dict[str, list[str]], baseline: dict[str, list[str]]
            ) -> tuple[dict[str, list[str]], dict[str, list[str]]]:
    """(new violations, stale exemptions), given what is found and what is excused.

    Pure, so the ratchet can be tested without a repository in a particular
    state -- which matters because the interesting cases are a file that starts
    violating a rail and a file that stops, and neither is convenient to arrange
    on disk.

    A stale exemption is an error rather than a warning. An exemption list nobody
    prunes is one that eventually covers whatever is added to that path later,
    and the whole point of the ratchet is that it can only tighten.
    """
    new, stale = {}, {}
    for rail, offenders in found.items():
        exempt = set(baseline.get(rail, ()))
        new[rail] = [name for name in offenders if name not in exempt]
        stale[rail] = [name for name in sorted(exempt)
                       if name not in set(offenders)]
    return new, stale


def main(argv: list[str] | None = None) -> int:
    argv = sys.argv[1:] if argv is None else argv
    found = current()

    if "--write-baseline" in argv:
        BASELINE.write_text(
            json.dumps(found, indent=2) + "\n", encoding="utf-8")
        total = sum(len(v) for v in found.values())
        print(f"wrote {BASELINE.relative_to(ROOT)} with {total} exemption(s)")
        return 0

    baseline = load_baseline()
    new, fixed = compare(found, baseline)

    problems = 0
    for rail, offenders in new.items():
        for name in offenders:
            print(f"FAIL {rail}: {name}")
            problems += 1
    for rail, stale in fixed.items():
        for name in stale:
            print(f"STALE BASELINE {rail}: {name} no longer violates it "
                  f"(or no longer exists) -- remove the exemption")
            problems += 1

    exemptions = sum(len(v) for v in baseline.values())
    if problems:
        print(f"\n{problems} problem(s). {exemptions} exemption(s) in "
              f"{BASELINE.relative_to(ROOT).as_posix()}.")
        print("A new file must comply. If a change deliberately alters what is")
        print("exempt, re-run with --write-baseline so the diff is reviewable.")
        return 1

    print(f"rails ok - {exemptions} legacy exemption(s), no new violations")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
