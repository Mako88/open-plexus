"""The caches git does not hold, `state/` and `data/`, are reached through `home()` and
nothing else, so a worktree finds them instead of building them again (2026-10-07: a
record file written under the worktree's own root, and no cache found there)."""

import ast
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CACHES = {"state", "data"}


def _chain(node):
    """The parts of `a / b / c`, leftmost first."""
    if isinstance(node, ast.BinOp) and isinstance(node.op, ast.Div):
        return _chain(node.left) + [node.right]
    return [node]


def _from_home(base) -> bool:
    return (isinstance(base, ast.Call) and isinstance(base.func, ast.Name)
            and base.func.id == "home")


def test_state_and_data_are_built_only_from_home():
    wrong = []
    for path in sorted([*(ROOT / "src").rglob("*.py"), *(ROOT / "scripts").glob("*.py")]):
        if path.name == "home.py":
            continue
        tree = ast.parse(path.read_text(encoding="utf-8"))
        inner = {id(n.left) for n in ast.walk(tree)
                 if isinstance(n, ast.BinOp) and isinstance(n.op, ast.Div)}
        for node in ast.walk(tree):
            if not (isinstance(node, ast.BinOp) and isinstance(node.op, ast.Div)) \
                    or id(node) in inner:
                continue
            parts = _chain(node)
            named = [p.value for p in parts[1:] if isinstance(p, ast.Constant)
                     and p.value in CACHES]
            if named and not _from_home(parts[0]):
                wrong.append(f"{path.relative_to(ROOT)}:{node.lineno} {named[0]}")
    assert not wrong, f"state/ or data/ built from something but home(): {wrong}"


def test_the_check_sees_what_it_is_for():
    """A path built from the code's own root is found, and one from `home()` is not."""
    bad = ast.parse('ROOT / "state" / "x.json"').body[0].value
    good = ast.parse('home() / "state" / "x.json"').body[0].value
    assert not _from_home(_chain(bad)[0]) and _from_home(_chain(good)[0])
