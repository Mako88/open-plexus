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


def main() -> int:
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
    print(f"ok - {checked} invocation(s) across "
          f"{len(list(WORKFLOWS.glob('*.yml')))} workflow(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
