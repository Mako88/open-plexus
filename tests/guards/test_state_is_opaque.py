"""Nothing outside `core` may index into a state. Enforced, not asserted in prose.

WHY THE RULE EXISTS. The whole justification for a `Core` protocol is that the
core can be swapped for a bigger one -- or a different architecture entirely, per
the open fork on hybrid cores -- without anything above it changing. A state is a
list of tensors today and is three entries per layer only because RWKV-7 says so.
The moment a store, a scheduler or an exam reaches in and takes `state[0]`, that
swap stops being free and nobody finds out until the day somebody tries it.

WHY IT NEEDED ENFORCING RATHER THAN WRITING DOWN. It was a docstring for a day,
and the open fork "state as a fragment" would have the STORE holding states --
which is exactly where the rule breaks first and where breaking it is least
visible, because a store that indexes a state still passes every test a store has.

WHAT THIS ALLOWS. Calling a core's own methods on a state (`copy_state`,
`save_state`, `state_bytes`) is the supported way to handle one and is not
indexing. Passing a state around, storing it in a variable, returning it: all
fine. The state is a value; what it is NOT is a container anybody may open.

Settles standing objection 7.
"""

from __future__ import annotations

import ast
from pathlib import Path

import pytest

SRC = Path(__file__).resolve().parents[2] / "src" / "sylvatica"

# `core` is the one package allowed to know what a state is made of. Every other
# package sees an opaque value.
OWNS_THE_STATE = "core"

# A name is treated as a state if it is called one. Deliberately broad: the
# point is to catch the shape of the mistake, not to be clever about aliasing.
STATE_NAMES = ("state", "states", "asking", "working", "st")


def state_like(node: ast.expr) -> str | None:
    """The name being subscripted, if it looks like a state."""
    if isinstance(node, ast.Name) and node.id.lower() in STATE_NAMES:
        return node.id
    if isinstance(node, ast.Attribute) and node.attr.lower() in STATE_NAMES:
        return node.attr
    return None


def offences(path: Path) -> list[str]:
    tree = ast.parse(path.read_text(encoding="utf-8"), filename=str(path))
    found = []
    for node in ast.walk(tree):
        if isinstance(node, ast.Subscript):
            name = state_like(node.value)
            if name:
                found.append(f"{path.name}:{node.lineno} indexes into `{name}`")
        # `for x in state:` is reaching in just as much as `state[0]` is.
        if isinstance(node, ast.For):
            name = state_like(node.iter)
            if name:
                found.append(f"{path.name}:{node.lineno} iterates `{name}`")
    return found


def non_core_modules() -> list[Path]:
    return sorted(
        p
        for p in SRC.rglob("*.py")
        if p.relative_to(SRC).parts[0] != OWNS_THE_STATE
    )


def test_there_is_something_to_check():
    """A guard that scans nothing passes forever. This is the check on the check."""
    modules = non_core_modules()
    assert len(modules) >= 5, f"only found {modules}; the scan is not reaching the source"


@pytest.mark.parametrize("path", non_core_modules(), ids=lambda p: p.name)
def test_no_package_outside_core_opens_a_state(path: Path):
    found = offences(path)
    assert not found, (
        "\n".join(found)
        + "\n\nA state is opaque outside `core`. Use the core's own methods "
        "(`copy_state`, `save_state`, `state_bytes`) or add one to the protocol. "
        "Indexing here makes swapping the core for a bigger one stop being free, "
        "and nothing would notice until somebody tried it."
    )


def test_the_guard_catches_what_it_is_for():
    """The scan itself, on code that breaks the rule. Otherwise a passing suite
    would be indistinguishable from a scan that cannot see anything."""
    import tempfile

    with tempfile.TemporaryDirectory() as d:
        bad = Path(d) / "bad.py"
        bad.write_text("def f(state):\n    return state[0] + 1\n", encoding="utf-8")
        assert offences(bad)

        looping = Path(d) / "loop.py"
        looping.write_text("def f(state):\n    for t in state:\n        print(t)\n", "utf-8")
        assert offences(looping)

        fine = Path(d) / "fine.py"
        fine.write_text(
            "def f(core, state):\n    return core.copy_state(state)\n", encoding="utf-8"
        )
        assert not offences(fine)
