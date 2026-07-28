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


#: `python … | tee f` reports tee's status. The python failure is discarded.
TEE = re.compile(r"\|\s*tee\b")
#: `python -m tools.thing` or `python tools/thing.py`, in a workflow step.
TOOL_INVOCATION = re.compile(r"python\s+-m\s+(tools\.[\w.]+)"
                             r"|python\s+(tools/[\w/]+\.py)")
PIP_INSTALL = re.compile(r"pip\s+install\s+([^\n|&;]+)")
#: Import roots that resolve inside this repository rather than to a package.
LOCAL_PACKAGES = {"tools", "openplexus", "experiments", "tests"}


def run_blocks(text: str) -> list[tuple[int, str]]:
    """Every `run: |` body in a workflow, with the line its `run:` sits on."""
    lines = text.split("\n")
    blocks = []
    for index, line in enumerate(lines):
        match = re.match(r"^(\s*)run: \|\s*$", line)
        if not match:
            continue
        indent = len(match.group(1))
        body = []
        for following in lines[index + 1:]:
            if following.strip() and len(following) - len(following.lstrip()) <= indent:
                break
            body.append(following)
        blocks.append((index + 1, "\n".join(body)))
    return blocks


def job_blocks(text: str) -> dict[str, str]:
    """Each top-level job's YAML, keyed by job name.

    Split by indentation rather than parsed, because this tool must run in the
    aggregate job of every workflow it checks and PyYAML is not installed there
    — which is the exact class of failure it now checks for.
    """
    lines = text.split("\n")
    try:
        start = next(i for i, line in enumerate(lines) if line.rstrip() == "jobs:")
    except StopIteration:
        return {}
    starts = [(index, match.group(1))
              for index, line in enumerate(lines[start + 1:], start + 1)
              if (match := re.match(r"^  ([\w-]+):\s*$", line))]
    return {name: "\n".join(lines[index:(starts[position + 1][0]
                                         if position + 1 < len(starts)
                                         else len(lines))])
            for position, (index, name) in enumerate(starts)}


def third_party_imports(module: Path, seen: set[Path] | None = None) -> set[str]:
    """Packages a tool needs installed, following imports within the repo.

    Only column-zero imports count: those run at import time, which is when the
    failure this guards against happens. Imports of repo-local packages are
    followed, so a summariser that reaches numpy through a helper is still
    covered.
    """
    seen = set() if seen is None else seen
    if module in seen or not module.is_file():
        return set()
    seen.add(module)
    found: set[str] = set()
    for match in re.finditer(r"^(?:import|from)\s+([\w.]+)",
                             module.read_text(encoding="utf-8"), re.MULTILINE):
        dotted = match.group(1)
        root = dotted.split(".")[0]
        if root in sys.stdlib_module_names:
            continue
        if root in LOCAL_PACKAGES:
            found |= third_party_imports(
                ROOT / (dotted.replace(".", "/") + ".py"), seen)
            continue
        found.add(root)
    return found


#: `${{ matrix.thing }}` anywhere in a workflow.
MATRIX_USE = re.compile(r"matrix\.([\w-]+)")


def matrix_keys(job: str) -> set[str]:
    """Every key a job's matrix defines, including via `include:` entries.

    **Reading only the top-level keys is wrong and was the first version.**
    g11-06 declares `chars` as an axis and supplies `arm`, `width` and `slots`
    through `include:` -- a grid that ran twenty cells correctly -- and a check
    that missed those would have flagged a working workflow while the broken one
    was the point. So this takes every `name:` inside the matrix block, at any
    depth.

    Over-inclusive on purpose. A key that exists and is never used costs
    nothing; a key that is used and does not exist expands to the empty string.
    """
    lines = job.splitlines()
    try:
        start = next(i for i, line in enumerate(lines)
                     if line.strip() == "matrix:")
    except StopIteration:
        return set()
    indent = len(lines[start]) - len(lines[start].lstrip())
    keys = set()
    for line in lines[start + 1:]:
        if line.strip() and len(line) - len(line.lstrip()) <= indent:
            break
        keys.update(re.findall(r"([\w-]+)\s*:", line))
    return keys


def undeclared_matrix_keys() -> list[str]:
    """Every `matrix.X` a job uses that its own matrix does not declare.

    **An undeclared key expands to the EMPTY STRING**, silently. The job runs, a
    flag arrives with no value, and the failure surfaces as an argparse error in
    every cell at once — which reads like the script being broken rather than
    the workflow.

    > *Calibration.* g12-02 was written by copying g12-01 and editing it. The
    > edit to the run step did not apply — a `str.replace` that matched nothing
    > and returned the original — so the churn sweep kept g12-01's command,
    > referencing `matrix.window`, `matrix.link` and `matrix.repeat` against a
    > matrix declaring `nodes`, `lost` and `leave_at`. **All eighteen cells
    > failed with `--window: expected one argument`.** The step's own NAME
    > rendered as "window  on a  link, run " in the job list and nobody read it.
    >
    > This check reads every workflow in about a second and would have refused
    > the dispatch.

    Job-scoped rather than file-scoped: two jobs in one file legitimately have
    different matrices, and a key declared by one is not available to the other.
    """
    offenders = []
    for path in sorted(WORKFLOWS.glob("*.yml")):
        for job, body in job_blocks(path.read_text(encoding="utf-8")).items():
            declared = matrix_keys(body)
            for used in sorted(set(MATRIX_USE.findall(body))):
                if used not in declared:
                    offenders.append(
                        f"{path.name}: job `{job}` uses matrix.{used}, which "
                        f"its matrix does not declare -- it will expand to an "
                        f"empty string in every cell")
    return offenders


def module_for(reference: str) -> Path:
    """The file `-m tools.thing` or `tools/thing.py` resolves to."""
    return ROOT / (reference if reference.endswith(".py")
                   else reference.replace(".", "/") + ".py")


def silent_failures(name: str, text: str, needs) -> list[str]:
    """Steps that can fail while the job stays green, in both known shapes.

    **A summariser that dies prints nothing and the step passes.** Both halves
    are needed for that: an import the job never installed, and a pipe into
    `tee` that discards the exit status.

    > *Calibration.* g11-04, run `30295529865`. All twelve cells returned;
    > the aggregate job ran `python -m tools.summarise_g11_04 | tee -a
    > summary.txt` without `pip install numpy`, so the summariser died on
    > `ModuleNotFoundError` and `tee` exited 0. The step is marked **success**
    > and `summary.txt` holds one line: `cells returned: 12 of 12`. The
    > sweep's numbers were recovered by hand. `summarise_g11_04` is the only
    > summariser importing numpy, and its was the only aggregate job missing
    > the install — so a single check over the enumeration covers the class.

    The package name is compared against the import root, which is right for
    everything this repo installs and would need a mapping for a package whose
    import name differs from its pip name. `needs` maps a tool reference to the
    packages it needs installed, so the rule can be tested without the tree.
    """
    offenders = []
    for line_number, body in run_blocks(text):
        if TEE.search(body) and "pipefail" not in body:
            offenders.append(
                f"{name}:{line_number} pipes into `tee` without `set -o "
                f"pipefail`, so the command before it can die and the step "
                f"still passes")
    for job, body in job_blocks(text).items():
        installed = {token.lower() for group in PIP_INSTALL.findall(body)
                     for token in group.split() if not token.startswith("-")}
        for dotted, script in TOOL_INVOCATION.findall(body):
            reference = dotted or script
            # `needs` returns None for a reference that resolves to no file. A
            # missing module otherwise reads as "imports nothing", so a renamed
            # summariser would switch this check off in the same change that
            # broke the workflow -- which the rename to
            # summarise_scaling_exponent did, and two tests caught.
            required = needs(reference)
            if required is None:
                offenders.append(
                    f"{name}: job `{job}` runs {reference}, which does not "
                    f"exist -- the step will die and nothing here can check "
                    f"what it needed installed")
                continue
            for package in sorted(required):
                if package.lower() not in installed:
                    offenders.append(
                        f"{name}: job `{job}` runs {reference}, which imports "
                        f"{package}, and never installs it -- the step will "
                        f"die at import and print nothing")
    return offenders


def packages_needed(reference: str) -> set[str] | None:
    """What a tool needs installed, or None if the reference resolves nowhere."""
    module = module_for(reference)
    return third_party_imports(module) if module.is_file() else None


def silently_failing_steps() -> list[str]:
    """`silent_failures` over every workflow in the tree."""
    return [problem for path in sorted(WORKFLOWS.glob("*.yml"))
            for problem in silent_failures(
                path.name, path.read_text(encoding="utf-8"), packages_needed)]


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
    silent = silently_failing_steps() + undeclared_matrix_keys()
    for problem in silent:
        print(f"FAIL  {problem}")
    if silent:
        print(f"\n{len(silent)} step(s) that can fail while the job reports "
              "success. A green run with an empty summary is how a sweep gets "
              "read as having no result.")
        return 1
    print(f"ok - {checked} invocation(s) across "
          f"{len(list(WORKFLOWS.glob('*.yml')))} workflow(s), sweeps dispatch-only")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
