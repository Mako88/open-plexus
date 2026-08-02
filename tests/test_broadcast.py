"""The broadcast flood — many origins, stamina, and the accounting that ends it.

The load-bearing tests here are the connection tests and the accounting
invariant. Everything else this module reports — messages, work, chains — is
arithmetic over what the walk did, and would keep reporting confidently if the
walk were disconnected from the graph entirely.
"""

from __future__ import annotations

import unittest

from openplexus.broadcast import COSTS, flood
from openplexus.grounding import STATISTICS, SYMMETRIC, CoOccurrence

CONDITIONAL = STATISTICS["conditional"]


def _two_concepts() -> CoOccurrence:
    """Two concepts of several codes each, one hub word apiece, one distractor.

    The shape the senses graph has, at `_hub_with_distractor`'s proportions: a
    word is COMMON (845 occasions) where each of its codes is RARE (60, all of
    them with the word), and a distractor sits on all 3,845 occasions.

    **A first version of this fixture had the word at 120 occasions against a
    distractor at 2,200, and a companion assertion failed on it.** At those
    proportions `conditional(word, distractor)` is small, so even `min` ranks
    correctly and the inversion this file is about does not occur. The word has
    to be common enough for the distractor to look like a plausible partner.
    """
    index = CoOccurrence()
    for code in (10, 11, 12):
        for _ in range(60):
            index.observe({0, code, 99})
    for _ in range(665):
        index.observe({0, 2, 99})
    for code in (20, 21):
        for _ in range(60):
            index.observe({1, code, 99})
    for _ in range(725):
        index.observe({1, 3, 99})
    for _ in range(2155):
        index.observe({99, 50})
    return index


class BroadcastReachesAndReports(unittest.TestCase):

    def test_the_word_is_reached_from_its_own_codes(self):
        """The mechanism does the thing at all. Not a claim about ranking."""
        got = flood(_two_concepts(), CONDITIONAL, [10, 11, 12])
        self.assertIn(0, got.reached)
        self.assertGreater(got.reached[0].routes, 1,
                           "several origins should reach the word they share")

    def test_a_chain_never_repeats_a_node(self):
        """What makes an unbounded walk terminate on a cyclic graph."""
        got = flood(_two_concepts(), CONDITIONAL, [10, 11, 12])
        for endpoint, arrival in got.reached.items():
            self.assertEqual(len(set(arrival.chain)), len(arrival.chain),
                             f"chain to {endpoint} revisits a node")
            self.assertEqual(arrival.chain[-1], endpoint)

    def test_cost_is_reported_per_node_and_not_only_as_a_total(self):
        """`busiest` is the column the design exists to produce; a mean hides it."""
        got = flood(_two_concepts(), CONDITIONAL, [10, 11, 12])
        self.assertEqual(got.messages, sum(got.work.values()))
        self.assertGreaterEqual(got.busiest(), got.messages / len(got.work))


class TheAccountingEndsIt(unittest.TestCase):
    """Termination is bookkeeping, not a threshold, so the books must balance."""

    def test_a_completed_flood_ends_with_no_live_routes(self):
        got = flood(_two_concepts(), CONDITIONAL, [10, 11, 12])
        self.assertFalse(got.gave_up)
        self.assertEqual(got.live, 0)
        self.assertTrue(got.balanced(3),
                        f"splits {got.splits} deaths {got.deaths} "
                        f"live {got.live} do not account for 3 origins")

    def test_giving_up_does_not_look_like_finishing(self):
        """A run stopped by the safety must be distinguishable from one that ended.

        The companion to the test above: `live == 0` would be a vacuous
        assertion if nothing could ever leave it non-zero.
        """
        got = flood(_two_concepts(), CONDITIONAL, [10, 11, 12], ceiling=50)
        self.assertTrue(got.gave_up)
        self.assertNotEqual(got.live, 0)


class ForwardIsTheOnlyCombinerCorrectFromBOTHEnds(unittest.TestCase):
    """Why this module gates on `forward` and not on the design's MUTUAL.

    **The claim had to be narrowed by a failing test and is stronger for it.**
    Seeded at a rare code, `min` ranks correctly (0.2298 against the
    distractor's 0.1231) — mutuality is not wrong everywhere. Seeded at the hub
    word it inverts (0.0766 against 0.3592), because `conditional(code, word)`
    is 0.071 where `conditional(distractor, word)` is 1.00.

    **A flood stands on both.** A route seeded at an image code arrives at the
    word and expands from it, and that hop is scored from the hub's side. So a
    combiner that is right only at the rare end funds the background halfway
    through every walk. `forward` is the only one right at both ends.
    """

    def test_forward_is_correct_seeded_at_a_rare_code(self):
        got = flood(_two_concepts(), CONDITIONAL, [10, 11, 12],
                    combine="forward")
        self.assertGreater(got.reached[0].score, got.reached[99].score)

    def test_forward_is_correct_seeded_at_the_hub(self):
        got = flood(_two_concepts(), CONDITIONAL, [0], combine="forward")
        self.assertGreater(got.reached[10].score, got.reached[99].score)

    def test_every_symmetric_combiner_inverts_at_the_hub(self):
        """The companion. Without it, `forward` being right would be untested
        against the alternatives actually being wrong."""
        for name in SYMMETRIC:
            got = flood(_two_concepts(), CONDITIONAL, [0], combine=name)
            self.assertLessEqual(
                got.reached[10].score, got.reached[99].score,
                msg=f"{name} ranks the code above the distractor from the hub, "
                    f"so the reason this module defaults to 'forward' is wrong")


class PerturbingTheInputMovesTheOutput(unittest.TestCase):
    """Connection tests. Each asserts something DID change, not that it did not."""

    def test_strengthening_a_link_raises_what_it_reaches(self):
        index = _two_concepts()
        before = flood(index, CONDITIONAL, [10, 11, 12]).reached[0].score
        for _ in range(200):
            index.observe({0, 10, 99})
        after = flood(index, CONDITIONAL, [10, 11, 12]).reached[0].score
        self.assertNotAlmostEqual(before, after)

    def test_the_origin_set_changes_what_is_reached(self):
        """The design's whole claim is that many origins do something one cannot.

        This asserts only that the origin set is connected to the outcome.
        Whether more origins DISCRIMINATE better is the open measurement and
        is not asserted here.
        """
        index = _two_concepts()
        one = flood(index, CONDITIONAL, [10])
        many = flood(index, CONDITIONAL, [10, 11, 12])
        self.assertGreater(many.reached[0].routes, one.reached[0].routes)
        self.assertNotAlmostEqual(one.reached[0].score, many.reached[0].score)

    def test_stamina_changes_how_far_a_route_gets(self):
        index = _two_concepts()
        lean = flood(index, CONDITIONAL, [10, 11, 12], cost="constant",
                     charge=0.5, stamina=0.05)
        rich = flood(index, CONDITIONAL, [10, 11, 12], cost="constant",
                     charge=0.5, stamina=5.0)
        self.assertLess(lean.messages, rich.messages)


class ArgumentsAreRefusedRatherThanDefaulted(unittest.TestCase):
    """An argument that silently does nothing is a sweep arm that is not one."""

    def test_charge_without_constant_cost_is_refused(self):
        with self.assertRaises(ValueError):
            flood(_two_concepts(), CONDITIONAL, [10], cost="local", charge=0.3)

    def test_constant_cost_without_a_charge_is_refused(self):
        with self.assertRaises(ValueError):
            flood(_two_concepts(), CONDITIONAL, [10], cost="constant")

    def test_an_empty_broadcast_is_refused(self):
        with self.assertRaises(ValueError):
            flood(_two_concepts(), CONDITIONAL, [])

    def test_every_cost_mode_runs(self):
        for mode in COSTS:
            extra = {"charge": 0.3} if mode == "constant" else {}
            got = flood(_two_concepts(), CONDITIONAL, [10, 11], cost=mode,
                        **extra)
            self.assertEqual(got.live, 0, f"{mode} did not terminate")


if __name__ == "__main__":
    unittest.main()
