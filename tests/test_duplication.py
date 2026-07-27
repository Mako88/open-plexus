"""The copy-detector, and the honest limit on what it can detect.

`tools/check_duplication.py` was asked for by BACKLOG on the grounds that it
"would have found the five copied refusals before one of them lost its floor
check". It would not have, and the tool itself measures that: over the pre-port
tree it finds none of them, because those copies had already diverged and
divergence is what defeats a structural hash.

So these pin two things — that it catches an undrifted copy, and that it does not
catch a drifted one. The second matters more. A check whose stated purpose is
wider than its actual reach is a check that gets trusted for the wrong thing.
"""

from __future__ import annotations

import ast
import unittest

from tools import check_duplication as dup

SAME_A = """
def load(pattern):
    rows = []
    for path in sorted(pattern):
        rows.extend(read(path))
    if not rows:
        raise SystemExit("none")
    return rows
"""
#: Same shape, different names and literals. This is what a copy looks like.
SAME_B = """
def gather(glob):
    records = []
    for name in sorted(glob):
        records.extend(parse(name))
    if not records:
        raise SystemExit("empty")
    return records
"""
#: A copy that has DRIFTED -- one branch was added. This is what a copy looks
#: like by the time it is dangerous, and it is invisible to a structural hash.
DRIFTED = """
def gather(glob):
    records = []
    for name in sorted(glob):
        if name:
            records.extend(parse(name))
    if not records:
        raise SystemExit("empty")
    return records
"""
TINY = """
def floor(values):
    return sum(values) / len(values)
"""


def shape_of(source: str) -> str:
    """The tool's own hashing, applied to one function in a string."""
    node = ast.parse(source).body[0]
    body = list(node.body)
    if body and isinstance(body[0], ast.Expr) and isinstance(
            getattr(body[0], "value", None), ast.Constant):
        body = body[1:]
    if sum(dup._statements(s) for s in body) < dup.MIN_STATEMENTS:
        return "TOO SMALL"
    stripped = [dup.Shape().visit(ast.parse(ast.unparse(s))) for s in body]
    return "\n".join(ast.dump(s) for s in stripped)


class ItSeesThroughRenaming(unittest.TestCase):

    def test_two_copies_with_different_names_hash_the_same(self):
        self.assertEqual(shape_of(SAME_A), shape_of(SAME_B))

    def test_a_body_below_the_threshold_is_ignored(self):
        """A short function that returns a computation is not duplication, it is
        a language without a shorter way to say it."""
        self.assertEqual(shape_of(TINY), "TOO SMALL")


class ItIsBlindToTheDangerousCase(unittest.TestCase):
    """The limit, asserted so nobody has to rediscover it.

    A copy that has drifted is the one that produces plausible numbers through a
    path somebody already fixed elsewhere. That is rule 12's failure and this
    check cannot see it. `tools/mutate.py` can — a mutation in one path the tests
    do not notice — and nothing else here does.
    """

    def test_a_copy_that_drifted_by_one_branch_no_longer_matches(self):
        self.assertNotEqual(shape_of(SAME_A), shape_of(DRIFTED))

    def test_and_that_is_why_the_pre_port_summarisers_were_not_caught(self):
        """The claim in BACKLOG, tested rather than argued. Guarded so it cannot
        pass by finding nothing at all."""
        found = dup.current()["D1-same-function-twice"]
        self.assertTrue(found, "the analysis found nothing anywhere, so this "
                               "says nothing about what it can see")
        self.assertFalse([pair for pair in found if "summarise_" in pair],
                         "a summariser pair is duplicated; the port was "
                         "supposed to leave none")


class TheBaselineIsInSync(unittest.TestCase):

    def test_it_passes_today(self):
        """Fails for one of two reasons: something new is duplicated, or an
        exemption has outlived its reason. `--write-baseline` fixes the second
        and the diff is the record of what got better."""
        self.assertEqual(dup.main([]), 0)


if __name__ == "__main__":
    unittest.main()
