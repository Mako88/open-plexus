"""A module-level name is given its value once. Two assignments to one name in a module
mean the later silently replaces the earlier for every use, as `BATCH` did on 2026-10-07:
the parser's batch size and the commit batch, both 64, so nothing showed until one moved."""

import ast
from collections import Counter
from pathlib import Path

SRC = Path(__file__).resolve().parents[2] / "src" / "unfused"


def test_no_module_level_name_is_assigned_twice():
    twice = []
    for path in sorted(SRC.rglob("*.py")):
        tree = ast.parse(path.read_text(encoding="utf-8"))
        names = Counter()
        for node in tree.body:
            targets = (node.targets if isinstance(node, ast.Assign)
                       else [node.target] if isinstance(node, ast.AnnAssign) else [])
            for t in targets:
                if isinstance(t, ast.Name):
                    names[t.id] += 1
        twice += [f"{path.relative_to(SRC)}: {n}" for n, k in names.items() if k > 1]
    assert not twice, f"assigned more than once at module level: {twice}"
