"""A split graph answering like a whole one, and saying so when it does not.

`openplexus/agreement.py` asks C1's question of the count graph: does a
`CoOccurrence` split across owners answer reads identically to one held whole?
The old driver's reason is why this reports WHERE they differ rather than
whether they matched — a count off by one is a routing bug and a count off
everywhere is a split that never happened, and a boolean cannot tell them apart.
"""

from __future__ import annotations

import pathlib
import random
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from openplexus.agreement import disagreements, summary  # noqa: E402
from openplexus.federated import Federation  # noqa: E402
from openplexus.grounding import CoOccurrence  # noqa: E402

SURFACES = 12


def both(nodes: int = 4, occasions: int = 200, seed: int = 0):
    """The same stream into a whole graph and into a split one."""
    rng = random.Random(seed)
    whole = CoOccurrence()
    split = Federation(nodes=nodes, seed=seed)
    for _ in range(occasions):
        present = sorted(rng.sample(range(SURFACES), 3))
        whole.observe(present)
        for surface in present:
            split.note(surface)
        for i, one in enumerate(present):
            for other in present[i + 1:]:
                split.link(one, other)
    return whole, split


class ASplitGraphAnswersLikeAWholeOne(unittest.TestCase):

    def test_they_agree_everywhere(self):
        whole, split = both()
        found = disagreements(whole, split, range(SURFACES))
        self.assertEqual(found, [], summary(found, range(SURFACES)))

    def test_and_it_is_split_across_MORE_THAN_ONE_owner(self):
        """The companion. Agreement is trivial if everything lives on one node,
        and that is exactly what a broken split looks like."""
        _, split = both()
        owners = {split.owner(s) for s in range(SURFACES)}
        self.assertGreater(len(owners), 1)

    def test_the_agreement_FOLLOWS_the_data(self):
        """A comparator that always returned empty would pass the first test."""
        whole, split = both()
        whole.observe([0, 1])
        found = disagreements(whole, split, range(SURFACES))
        self.assertNotEqual(found, [])

    def test_it_says_WHERE_not_just_whether(self):
        whole, split = both()
        whole.observe([0, 1])
        found = disagreements(whole, split, range(SURFACES))
        what = {entry[1] for entry in found}
        self.assertIn("seen", what)
        self.assertIn("DISAGREES", summary(found, range(SURFACES)))

    def test_agreement_holds_at_more_owners_than_surfaces(self):
        """Empty owners are the boundary a routing bug hides at."""
        whole, split = both(nodes=32)
        found = disagreements(whole, split, range(SURFACES))
        self.assertEqual(found, [], summary(found, range(SURFACES)))


if __name__ == "__main__":
    unittest.main()
