"""One graph, several kinds, and a route that crosses between them.

`openplexus/shared.py` is the first thing in this project to hold more than one
kind in a single `CoOccurrence`. What is worth fixing is that it is genuinely
ONE graph — a picture and a word observed together are connected, and can be
walked between — and that the guarantees around it hold: kinds land on disjoint
nodes, and a kind is reported present only once data for it has ARRIVED.
"""

from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from openplexus import wiring  # noqa: E402
from openplexus.shared import SharedGraph  # noqa: E402


def loaded() -> SharedGraph:
    """A graph where every picture co-occurs with its word, and nothing else."""
    shared = SharedGraph()
    shared.reserve("image", 4)
    shared.reserve("word", 4)
    shared.reserve("fact", 4)
    for _ in range(20):
        for digit in range(4):
            shared.observe([("image", digit), ("word", digit)])
    return shared


class ItIsGenuinelyONEGraph(unittest.TestCase):

    def setUp(self):
        wiring.reset()

    def test_a_picture_and_its_word_are_connected(self):
        shared = loaded()
        picture = shared.space.node("image", 2)
        word = shared.space.node("word", 2)
        self.assertIn(word, shared.index.partners(picture))

    def test_and_NOT_connected_to_another_word(self):
        """The companion. A graph connecting everything to everything would
        pass the test above and mean nothing."""
        shared = loaded()
        picture = shared.space.node("image", 2)
        other = shared.space.node("word", 3)
        self.assertNotIn(other, shared.index.partners(picture))

    def test_a_third_kind_joins_the_SAME_graph(self):
        """The point of the whole exercise: facts land beside pictures."""
        shared = loaded()
        before = shared.index.occasions
        shared.observe([("word", 1), ("fact", 0)])
        self.assertEqual(shared.index.occasions, before + 1)
        word = shared.space.node("word", 1)
        self.assertIn(shared.space.node("fact", 0),
                      shared.index.partners(word))

    def test_a_route_crosses_from_a_picture_to_a_fact(self):
        """Two hops through a word, between kinds that never co-occurred. This
        is the capability three separate graphs made impossible."""
        shared = loaded()
        for _ in range(20):
            shared.observe([("word", 1), ("fact", 0)])
        picture = shared.space.node("image", 1)
        fact = shared.space.node("fact", 0)
        self.assertNotIn(fact, shared.index.partners(picture))
        through = {p for step in shared.index.partners(picture)
                   for p in shared.index.partners(step)}
        self.assertIn(fact, through)


class TheGuaranteesHold(unittest.TestCase):

    def setUp(self):
        wiring.reset()

    def test_kinds_land_on_disjoint_nodes(self):
        with wiring.expect(holding={"image", "word"}, disjoint=True, graph=1):
            shared = SharedGraph()
            shared.reserve("image", 4)
            shared.reserve("word", 4)
            shared.observe([("image", 0), ("word", 0)])

    def test_RESERVED_BUT_NEVER_FED_is_not_held(self):
        """The check that would be defeated by declaring at reserve time."""
        shared = SharedGraph()
        shared.reserve("image", 4)
        shared.reserve("audio", 4)
        shared.observe([("image", 0)])
        self.assertEqual(shared.holds(), {"image"})

    def test_and_a_run_declaring_it_FAILS(self):
        with self.assertRaises(wiring.WiringError):
            with wiring.expect(holding={"image", "audio"}):
                shared = SharedGraph()
                shared.reserve("image", 4)
                shared.reserve("audio", 4)
                shared.observe([("image", 0)])

    def test_an_unreserved_kind_is_refused(self):
        shared = SharedGraph()
        with self.assertRaises(KeyError):
            shared.observe([("fact", 0)])


if __name__ == "__main__":
    unittest.main()
