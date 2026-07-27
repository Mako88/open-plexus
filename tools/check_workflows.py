"""Verify every flag a workflow passes is one the script actually accepts.

A workflow that passes an unrecognised flag kills every job in its matrix on the
first line. The failure surfaces as "all twenty seeds produced nothing", which
looks like a catastrophic experimental result rather than a typo, and it arrives
only after the matrix has been spent.

    python tools/check_workflows.py

The predecessor project hit this three separate times and built exactly this
check in response. Inheriting it costs about a second per run, and it is the
cheapest guard in the repo: it turns a twenty-minute wasted matrix into an error
before anything is dispatched.

The check is deliberately crude — it reads every `python experiments/*.py`
invocation out of the workflow YAML, resolves the shell variables it can, and
compares the flags against the script's own `--help`. It does not attempt to
validate values.
"""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
WORKFLOWS = ROOT / ".github" / "workflows"

#: `python experiments/thing.py --flag ...` anywhere in a workflow file.
INVOCATION = re.compile(r"python\s+(experiments/[\w/]+\.py)((?:\s+[^\n|&;]*)?)")
FLAG = re.compile(r"(--[\w-]+)")


def accepted_flags(script: Path) -> set[str]:
    """Flags the script's own --help reports."""
    result = subprocess.run([sys.executable, str(script), "--help"],
                            cwd=ROOT, capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(f"{script.name} --help failed:\n{result.stderr[-500:]}")
    return set(FLAG.findall(result.stdout))


def push_triggered_sweeps() -> list[str]:
    """Sweeps that fire on push, which they must not.

    Every sweep shares one `concurrency` group, so at most one matrix runs at a
    time. Ten of them watched `openplexus/` or `tools/` on push, which meant
    **editing the model re-ran sweeps that finished weeks ago** -- and since a
    concurrency group holds only one pending run, a freshly dispatched matrix
    could be cancelled before starting by a re-run of old work.

    That is what happened to g5-04. A commit touching the shared experiment
    script re-triggered g5-03, and g5-04 was cancelled without running a single
    job. What it left behind was a workflow marked `completed` with zero jobs and
    no artifact, which reads as success until someone looks.

    A finished sweep is a record. Re-running it because an unrelated file moved
    is waste at best and interference at worst.
    """
    offenders = []
    for path in sorted(WORKFLOWS.glob("sweep-*.yml")):
        block = re.search(r"^on:\n(?:[ \t].*\n|\n)*",
                          path.read_text(encoding="utf-8"), re.MULTILINE)
        if block and "push:" in block.group(0):
            offenders.append(path.name)
    return offenders


def refuse_if_mutating() -> None:
    """Stop if tools/mutate.py currently has a file edited.

    This tool reads and parses the source, so a mutated tree makes it report
    problems that are not there. It did: "33 problem(s)" during a harness run,
    "ok" once the run finished.

    Duplicated from experiments/harness.py rather than imported, because a
    checking tool that depends on the package it checks fails in the one
    situation it exists for.
    """
    leftovers = sorted(ROOT.glob("**/*.py.bak"))
    if leftovers:
        raise SystemExit(
            "REFUSING TO RUN: tools/mutate.py has the source edited.\n"
            + "\n".join(f"  {p.relative_to(ROOT)}" for p in leftovers))


def main() -> int:
    refuse_if_mutating()
    if not WORKFLOWS.is_dir():
        print("no workflows to check")
        return 0

    problems: list[str] = []
    checked = 0
    cache: dict[Path, set[str]] = {}

    for workflow in sorted(WORKFLOWS.glob("*.yml")):
        text = workflow.read_text(encoding="utf-8")
        for relative, arguments in INVOCATION.findall(text):
            script = ROOT / relative
            if not script.is_file():
                problems.append(f"{workflow.name}: no such script {relative}")
                continue
            if script not in cache:
                try:
                    cache[script] = accepted_flags(script)
                except RuntimeError as error:
                    problems.append(f"{workflow.name}: {error}")
                    continue
            checked += 1
            for flag in FLAG.findall(arguments):
                if flag not in cache[script]:
                    problems.append(
                        f"{workflow.name}: {relative} is passed {flag}, which "
                        f"it does not accept. Accepted: "
                        f"{', '.join(sorted(cache[script]))}")

    for problem in problems:
        print(f"FAIL  {problem}")
    if problems:
        print(f"\n{len(problems)} problem(s). Every job in an affected matrix "
              "would die on its first line.")
        return 1
    offenders = push_triggered_sweeps()
    if offenders:
        print("these sweeps fire on push and can cancel a dispatched matrix "
              "before it starts: " + ", ".join(offenders))
        return 1
    print(f"ok - {checked} invocation(s) across "
          f"{len(list(WORKFLOWS.glob('*.yml')))} workflow(s), sweeps dispatch-only")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
