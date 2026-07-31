"""The join must actually join, and must actually break when the clocks disagree.

The failure this file is written against is the one CLAUDE.md names first: a
mechanism that runs, produces plausible output, and is doing nothing. A join that
quietly read the TRUE time instead of each node's own skewed clock would pass
every shape test here, produce a perfect score at every skew, and be measuring a
world that does not exist.

So the load-bearing tests are stated as properties of the mechanism's reason for
existing:

- two nodes with the same clock reach the same bucket **without exchanging
  anything**, which is the only reason a bucket is used at all;
- two nodes whose clocks disagree by more than a bucket **lose the pairing**, and
  overlapping windows are what buy it back.

Both need their companion, per rule 10: an assertion that something did not
change is satisfied by a mechanism that is disconnected.
"""

from __future__ import annotations

import unittest

from openplexus.buckets import BucketConfig, Join, Observation, observations
from openplexus.grounding import STATISTICS, CoOccurrence, equivalence_classes
from openplexus.tasks.occasions import OccasionConfig, generate


def _seen(join: Join, one: int, other: int) -> int:
    return join.index.together(one, other)


class TheJoinJoins(unittest.TestCase):
    """The positive control, and it is exact rather than approximate."""

    def setUp(self) -> None:
        self.occasions = OccasionConfig(concepts=16, surfaces=3, presence=0.8,
                                        noise=2, distractors=1, occasions=600)
        self.stream = generate(self.occasions)

    def _run(self, **kwargs) -> Join:
        config = BucketConfig(width=50, nodes=8, observers=3, seed=0, **kwargs)
        join = Join(config)
        join.run(observations(self.stream, config, tempo=100))
        return join

    def test_a_clean_join_reproduces_the_single_process_counts_exactly(self):
        """The ceiling. If this drifts, every impaired number is uninterpretable."""
        direct = CoOccurrence()
        for occasion in self.stream:
            direct.observe(occasion.surfaces)

        join = self._run()
        for surface in direct.surfaces():
            self.assertEqual(join.index.seen(surface), direct.seen(surface),
                             f"marginal differs for {surface}")
            for other in direct.partners(surface):
                self.assertEqual(_seen(join, surface, other),
                                 direct.together(surface, other),
                                 f"pair count differs for {surface},{other}")

    def test_and_it_recovers_the_same_classes(self):
        direct = CoOccurrence()
        for occasion in self.stream:
            direct.observe(occasion.surfaces)
        join = self._run()
        self.assertEqual(equivalence_classes(join.index, STATISTICS["ppmi"], 2),
                         equivalence_classes(direct, STATISTICS["ppmi"], 2))

    def test_nothing_is_late_or_lost_on_a_clean_link(self):
        join = self._run()
        self.assertEqual(join.lost_late, 0)
        self.assertEqual(join.lost_dropped, 0)
        self.assertAlmostEqual(join.messages_per_observation, 1.0)


class TwoNodesAgreeWithoutAsking(unittest.TestCase):
    """The property the whole design rests on, asserted as a property."""

    def test_two_observers_at_one_instant_reach_the_same_bucket(self):
        """Neither is told anything. They both do the same arithmetic.

        Stated over the OUTCOME rather than over the bucket arithmetic: two
        surfaces observed by different machines at the same instant end up
        counted as having met.
        """
        config = BucketConfig(width=50, nodes=8, observers=3, seed=0)
        join = Join(config)
        join.run([Observation(surface=0, when=10, observer=0),
                  Observation(surface=1, when=10, observer=1)])
        self.assertEqual(_seen(join, 0, 1), 1)

    def test_a_bucket_holding_one_thing_witnesses_no_PAIR_but_still_counts_it(self):
        """No coincidence, so no pairing — but the marginal is still counted.

        The marginal has to match the single-process path exactly, where a
        surface present alone still increments its own `seen`. Dropping it would
        make `count(y)` too small for anything usually seen alone, and every
        chance-corrected statistic divides by that — so a rare surface would look
        like a stronger partner than it is, in a direction nothing downstream
        could detect.

        **The first version of this file asserted the opposite** and the
        implementation obliged, because dropping lone buckets was how the
        original `spread` inflation was being avoided. Once counting moved to
        one designated bucket per observation, the reason evaporated and the
        assertion was left describing a bug.
        """
        config = BucketConfig(width=50, nodes=8, observers=3, seed=0)
        join = Join(config)
        join.run([Observation(surface=0, when=10, observer=0),
                  Observation(surface=1, when=5000, observer=1)])
        self.assertEqual(join.index.seen(0), 1)
        self.assertEqual(join.index.seen(1), 1)
        self.assertEqual(_seen(join, 0, 1), 0)

    def test_a_recurring_surface_keeps_its_marginal_when_windows_overlap(self):
        """The defect that made overlapping windows look like a design flaw.

        A bucket holds each surface once, and with `spread` on, several
        neighbouring moments write the same ever-present surface into the same
        bucket. If the last writer wins, the surviving reading centres on some
        OTHER bucket, so the marginal is counted at neither — and a surface
        present at every moment ends up with a marginal of one.

        Measured at `c(distractor) = 1` against 8,000 in `g33-01`, where it
        dropped f1 from 1.0000 to 0.6953 and read as *"overlapping windows
        manufacture spurious pairs"*. They do not. The reading that belongs to a
        bucket has to outrank the one that does not.

        Asserted as the property: a thing present at every moment has a marginal
        equal to the number of moments, whatever `spread` is.
        """
        moments = 40
        for spread in (0, 1, 2, 4):
            with self.subTest(spread=spread):
                config = BucketConfig(width=10, spread=spread, nodes=8,
                                      observers=3, seed=0)
                stream = []
                for step in range(moments):
                    stream.append(Observation(surface=99, when=step * 10,
                                              observer=0))
                    stream.append(Observation(surface=step % 7, when=step * 10,
                                              observer=1))
                join = Join(config)
                join.run(stream)
                self.assertEqual(join.index.seen(99), moments)
                self.assertEqual(join.index.occasions, moments)

    def test_a_surface_is_counted_ONCE_however_many_buckets_it_reaches(self):
        """The defect `spread` introduced, asserted so it cannot come back.

        With overlapping windows one observation is sent to many buckets. If
        each counted it, its marginal — and any pair — would be multiplied by
        how many buckets the two observations happened to SHARE, which is a
        function of how well their clocks agree rather than of what co-occurred.
        Caught at 5x.
        """
        config = BucketConfig(width=10, spread=4, nodes=8, observers=3, seed=0)
        join = Join(config)
        join.run([Observation(surface=0, when=100, observer=0),
                  Observation(surface=1, when=100, observer=0)])
        self.assertEqual(join.index.seen(0), 1)
        self.assertEqual(_seen(join, 0, 1), 1)
        self.assertEqual(join.index.occasions, 1)

    def test_two_instants_far_apart_are_not_joined(self):
        """The companion to the first test: the bucket is doing the separating."""
        config = BucketConfig(width=50, nodes=8, observers=3, seed=0)
        join = Join(config)
        join.run([Observation(surface=0, when=10, observer=0),
                  Observation(surface=1, when=10, observer=1),
                  Observation(surface=2, when=900, observer=2),
                  Observation(surface=3, when=900, observer=0)])
        self.assertEqual(_seen(join, 0, 1), 1)
        self.assertEqual(_seen(join, 2, 3), 1)
        self.assertEqual(_seen(join, 0, 2), 0)


class ClocksThatDisagree(unittest.TestCase):
    """Skew has to HURT, or the mechanism is not reading its own clock."""

    #: Seed chosen so observers 0 and 1 carry different offsets. Asserted below
    #: rather than assumed, because a seed where they happen to agree would make
    #: the whole class pass while testing nothing.
    SEED = 3

    def _pairing(self, skew: int, spread: int = 0) -> int:
        config = BucketConfig(width=10, skew=skew, spread=spread, nodes=8,
                              observers=3, seed=self.SEED)
        join = Join(config)
        join.run([Observation(surface=0, when=100, observer=0),
                  Observation(surface=1, when=100, observer=1)])
        return _seen(join, 0, 1)

    def test_the_chosen_seed_really_does_skew_the_two_observers_apart(self):
        """Guard on the fixture, so the tests below cannot pass vacuously."""
        config = BucketConfig(width=10, skew=40, nodes=8, observers=3,
                              seed=self.SEED)
        join = Join(config)
        offsets = join._offset                    # noqa: SLF001 - fixture guard
        self.assertNotEqual(offsets[0] // 10, offsets[1] // 10,
                            "this seed gives both observers the same bucket "
                            "offset, so the skew tests would test nothing")

    def test_clocks_that_agree_keep_the_pairing(self):
        self.assertEqual(self._pairing(skew=0), 1)

    def test_clocks_that_disagree_by_more_than_a_bucket_lose_it(self):
        """The companion above is what makes this meaningful rather than trivial."""
        self.assertEqual(self._pairing(skew=40), 0)

    def test_overlapping_windows_buy_it_back(self):
        """The option record's stated answer to boundaries, measured."""
        self.assertEqual(self._pairing(skew=40, spread=4), 1)

    def test_and_overlapping_windows_cost_messages(self):
        """Constant factor, and it is `2 * spread + 1`."""
        config = BucketConfig(width=10, spread=4, nodes=8, observers=3, seed=0)
        join = Join(config)
        join.run([Observation(surface=0, when=100, observer=0)])
        self.assertAlmostEqual(join.messages_per_observation, 9.0)


class ArrivingTooLate(unittest.TestCase):
    """A bucket is discarded, so a late observation has nothing to join."""

    def _late(self, lateness: int, grace: int) -> tuple[int, int]:
        config = BucketConfig(width=10, lateness=lateness, grace=grace,
                              nodes=8, observers=3, seed=1)
        join = Join(config)
        join.run([Observation(surface=s, when=100 + s, observer=s % 3)
                  for s in range(60)])
        return join.delivered, join.lost_late

    def test_a_generous_grace_loses_nothing(self):
        delivered, late = self._late(lateness=50, grace=500)
        self.assertEqual(late, 0)
        self.assertEqual(delivered, 60)

    def test_lateness_beyond_the_grace_loses_observations(self):
        """The companion above is what shows the loss is the DEADLINE, not the
        delay — the same delay with a longer grace costs nothing."""
        delivered, late = self._late(lateness=500, grace=0)
        self.assertGreater(late, 0)
        self.assertEqual(delivered + late, 60)

    def test_a_dropped_observation_is_counted_separately_from_a_late_one(self):
        """C3 loss and C2 lateness are different failures and must not merge."""
        config = BucketConfig(width=10, drop=0.5, nodes=8, observers=3, seed=2)
        join = Join(config)
        join.run([Observation(surface=s, when=s, observer=s % 3)
                  for s in range(200)])
        self.assertGreater(join.lost_dropped, 0)
        self.assertEqual(join.lost_dropped + join.lost_late + join.delivered,
                         200)


class Ownership(unittest.TestCase):
    """A bucket's owner is computed locally and agreed globally."""

    def test_the_same_bucket_always_has_the_same_owner(self):
        join = Join(BucketConfig(width=10, nodes=8, seed=0))
        self.assertEqual(join.bucket_owner(12345), join.bucket_owner(12345))

    def test_buckets_are_spread_over_the_ring(self):
        join = Join(BucketConfig(width=10, nodes=8, seed=0))
        owners = {join.bucket_owner(b) for b in range(500)}
        self.assertGreater(len(owners), 1)

    def test_busiest_share_is_near_even_over_many_buckets(self):
        config = BucketConfig(width=10, nodes=8, observers=3, seed=0)
        join = Join(config)
        join.run([Observation(surface=s % 20, when=s, observer=s % 3)
                  for s in range(4000)])
        # 0.125 is even across eight nodes and a real ring is lumpier than that.
        # The bound is 0.25 rather than something looser because the measured
        # value at this seed is 0.145, and a bound wide enough to admit a
        # badly-skewed ring would admit the broken case it exists to catch.
        self.assertLess(join.busiest_share(), 0.25,
                        "one node owns far more than its eighth of the buckets")


class Validation(unittest.TestCase):
    def test_a_zero_width_bucket_is_refused(self):
        with self.assertRaises(ValueError):
            BucketConfig(width=0)

    def test_negative_skew_is_refused(self):
        with self.assertRaises(ValueError):
            BucketConfig(skew=-1)

    def test_a_drop_of_one_is_refused(self):
        with self.assertRaises(ValueError):
            BucketConfig(drop=1.0)

    def test_occasions_must_be_at_least_one_tick_apart(self):
        config = BucketConfig(width=10)
        with self.assertRaises(ValueError):
            observations(generate(OccasionConfig(occasions=4)), config, tempo=0)


if __name__ == "__main__":
    unittest.main()
