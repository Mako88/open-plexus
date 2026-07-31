"""The occasion stream must be hard in the way it claims to be hard.

This generator exists to run one registered falsifier — *is a distractor present
on every occasion ever pruned* — and there is a specific way it could produce a
clean-looking answer while testing nothing. If a concept's surfaces were present
on **every** occasion it was the subject of, then the true partner and the
persistent distractor would have identical counts, and every arm's behaviour
would follow from arithmetic rather than from the data.

So the load-bearing assertion in this file is not that the stream is well-formed.
It is that `presence` genuinely takes effect, and that below 1.0 the distractor
is **strictly commoner** than the true partner — which is the condition that
makes raw counting fail for a reason rather than tie for a construction.
"""

from __future__ import annotations

import unittest
from collections import Counter

from openplexus.tasks.occasions import OccasionConfig, Occasion, generate, shuffled


class OccasionShape(unittest.TestCase):
    """What every stream must satisfy regardless of configuration."""

    def setUp(self) -> None:
        self.config = OccasionConfig(concepts=8, surfaces=3, presence=0.7,
                                     noise=2, distractors=1, occasions=500)
        self.stream = generate(self.config)

    def test_a_distractor_is_present_on_every_single_occasion(self):
        """The falsifier's premise. If this drifts, the test measures nothing."""
        distractor = self.config.concept_surfaces
        missing = [o.when for o in self.stream if distractor not in o.surfaces]
        self.assertEqual(missing, [], "the persistent distractor went missing")

    def test_every_occasion_shows_at_least_one_surface_of_its_subject(self):
        """An occasion with none of its subject in it dilutes every count."""
        for occasion in self.stream:
            own = {occasion.subject * self.config.surfaces + m
                   for m in range(self.config.surfaces)}
            self.assertTrue(own & set(occasion.surfaces),
                            f"occasion {occasion.when} shows no subject surface")

    def test_noise_never_comes_from_the_subject_concept(self):
        """Noise is the sofa. A sofa drawn from the dog would be free signal.

        Asserted through the rate rather than through the draw, because the
        first version of this test checked only that every surface belonged to
        *some* concept — true of every surface by construction, so it would have
        passed with the filter deleted.

        The property with teeth: a surface of the subject concept shows up at
        exactly `presence`, conditioned on the occasion being non-empty. If noise
        could draw from the subject as well, that rate would rise, and there is
        no other way for it to.
        """
        config = OccasionConfig(concepts=8, surfaces=3, presence=0.7, noise=2,
                                distractors=1, occasions=6000, seed=13)
        stream = generate(config)
        empty = (1.0 - config.presence) ** config.surfaces
        expected = config.presence / (1.0 - empty)

        for concept in range(config.concepts):
            subject_occasions = [o for o in stream if o.subject == concept]
            for member in range(concept * config.surfaces,
                                (concept + 1) * config.surfaces):
                shown = sum(1 for o in subject_occasions
                            if member in o.surfaces)
                rate = shown / len(subject_occasions)
                self.assertAlmostEqual(
                    rate, expected, delta=0.06,
                    msg=f"surface {member} appears at {rate:.3f} on its own "
                        f"subject's occasions, against {expected:.3f} — noise "
                        f"is leaking the subject back in")

    def test_timestamps_are_strictly_increasing_from_zero(self):
        """A bucket join rounds these, so a repeat would merge two moments."""
        self.assertEqual([o.when for o in self.stream],
                         list(range(len(self.stream))))

    def test_surfaces_are_a_sorted_set(self):
        """A repeated surface would count as its own partner."""
        for occasion in self.stream:
            self.assertEqual(list(occasion.surfaces),
                             sorted(set(occasion.surfaces)))

    def test_the_same_seed_gives_the_same_stream(self):
        self.assertEqual(generate(self.config), self.stream)

    def test_a_different_seed_gives_a_different_stream(self):
        """The companion. Without it, a generator ignoring its seed passes above."""
        other = OccasionConfig(concepts=8, surfaces=3, presence=0.7, noise=2,
                               distractors=1, occasions=500, seed=1)
        self.assertNotEqual(generate(other), self.stream)


class PresenceIsWhatMakesItNonTrivial(unittest.TestCase):
    """The property the whole instrument rests on, asserted as a property."""

    def test_the_distractor_is_strictly_commoner_than_the_true_partner(self):
        """Below presence 1.0, raw counting must PREFER the distractor.

        Stated without reference to how the stream is built: for a surface, the
        number of occasions it shared with the always-present surface must
        exceed the number it shared with any surface of its own concept. If this
        stops holding, the falsifier has become a tie and its result is
        arithmetic.
        """
        config = OccasionConfig(concepts=6, surfaces=3, presence=0.6, noise=2,
                                distractors=1, occasions=4000, seed=3)
        stream = generate(config)
        distractor = config.concept_surfaces
        with_distractor: Counter[int] = Counter()
        with_partner: dict[int, Counter[int]] = {}

        for occasion in stream:
            present = set(occasion.surfaces)
            for surface in present:
                if surface == distractor:
                    continue
                with_distractor[surface] += 1
                partners = with_partner.setdefault(surface, Counter())
                for other in present:
                    if other != surface and other != distractor:
                        partners[other] += 1

        for surface, partners in with_partner.items():
            best = max(partners.values(), default=0)
            self.assertGreater(
                with_distractor[surface], best,
                f"surface {surface} meets a partner at least as often as the "
                f"distractor, so raw counting ties instead of failing")

    def test_presence_one_removes_that_property(self):
        """The companion, and it is the point.

        At presence 1.0 the distractor and the true partner tie exactly. This
        asserts the generator can be put into the degenerate state, so the
        assertion above is about the data and not about the counting code.
        """
        config = OccasionConfig(concepts=6, surfaces=3, presence=1.0, noise=0,
                                distractors=1, occasions=300, seed=3)
        stream = generate(config)
        distractor = config.concept_surfaces
        together: Counter[tuple[int, int]] = Counter()
        alone: Counter[int] = Counter()
        for occasion in stream:
            present = set(occasion.surfaces)
            for surface in present:
                alone[surface] += 1
                for other in present:
                    if other != surface:
                        together[(surface, other)] += 1
        surface = 0
        partner = 1
        self.assertEqual(together[(surface, partner)],
                         together[(surface, distractor)],
                         "at presence 1.0 the two must tie exactly")
        self.assertEqual(alone[surface], together[(surface, distractor)])

    def test_lowering_presence_lowers_how_often_a_surface_appears(self):
        """Connection test: the knob reaches the stream."""
        def rate(presence: float) -> float:
            config = OccasionConfig(concepts=6, surfaces=3, presence=presence,
                                    noise=0, distractors=0, occasions=2000,
                                    seed=5)
            stream = generate(config)
            shown = sum(len(o.surfaces) for o in stream)
            return shown / len(stream)

        self.assertGreater(rate(0.9), rate(0.4))


class Skew(unittest.TestCase):
    """`zipf` is the axis normalisation is expected to break on."""

    def test_zipf_makes_some_concepts_far_commoner(self):
        flat = Counter(o.subject for o in generate(
            OccasionConfig(concepts=16, zipf=0.0, occasions=3000, seed=7)))
        skewed = Counter(o.subject for o in generate(
            OccasionConfig(concepts=16, zipf=1.5, occasions=3000, seed=7)))
        self.assertLess(max(flat.values()) / min(flat.values()),
                        max(skewed.values()) / min(skewed.values()))

    def test_zipf_zero_is_close_to_uniform(self):
        """The companion: without it, a generator always skewed passes above."""
        counts = Counter(o.subject for o in generate(
            OccasionConfig(concepts=8, zipf=0.0, occasions=8000, seed=7)))
        self.assertLess(max(counts.values()) / min(counts.values()), 1.5)


class Pairings(unittest.TestCase):
    """Which modalities may share an occasion — G7's shape, and it must BITE."""

    def test_complete_is_byte_identical_to_a_stream_built_before_the_knob(self):
        """The knob must not invalidate g32-01, g32-02 or g33-01.

        Those ran before `pairings` existed, and a knob that shifted the random
        sequence — even with the same default — would make every one of them
        unreproducible. `groups()` returning a single group is what keeps the
        old code path untouched, and this is the assertion that says so.
        """
        plain = OccasionConfig(concepts=16, occasions=500, seed=0)
        named = OccasionConfig(concepts=16, occasions=500, seed=0,
                               pairings="complete")
        self.assertEqual(generate(plain), generate(named))

    def test_a_chain_never_shows_the_two_ends_together(self):
        config = OccasionConfig(concepts=16, surfaces=4, occasions=2000,
                                seed=0, pairings="chain")
        self.assertIn((0, 3), config.apart())
        for occasion in generate(config):
            own = [s for s in occasion.surfaces
                   if config.concept_of(s) == occasion.subject]
            modalities = {config.modality(s) for s in own}
            for one, other in config.apart():
                self.assertFalse(
                    {one, other} <= modalities,
                    f"modalities {one} and {other} shared occasion "
                    f"{occasion.when} and must never")

    def test_complete_leaves_NOTHING_for_a_walk_to_bridge(self):
        """The companion, and it is why every earlier run was the easy case."""
        self.assertEqual(OccasionConfig(surfaces=4).apart(), ())

    def test_a_star_isolates_every_spoke_from_every_other(self):
        config = OccasionConfig(surfaces=4, pairings="star")
        self.assertEqual(config.apart(), ((1, 2), (1, 3), (2, 3)))

    def test_an_unknown_pairing_is_refused(self):
        with self.assertRaises(ValueError):
            OccasionConfig(pairings="sometimes")


class GroundTruth(unittest.TestCase):
    """`classes()` is the answer everything is scored against."""

    def test_a_concepts_surfaces_are_one_class(self):
        config = OccasionConfig(concepts=4, surfaces=3, distractors=2)
        truth = config.classes()
        self.assertEqual(truth[0], frozenset({0, 1, 2}))
        self.assertEqual(truth[5], frozenset({3, 4, 5}))

    def test_a_distractor_belongs_to_nothing(self):
        """Otherwise `captured` could never be zero and the falsifier is unwinnable."""
        config = OccasionConfig(concepts=4, surfaces=3, distractors=2)
        truth = config.classes()
        self.assertEqual(truth[12], frozenset({12}))
        self.assertEqual(truth[13], frozenset({13}))

    def test_every_surface_in_the_stream_has_a_class(self):
        config = OccasionConfig(concepts=5, surfaces=3, distractors=1,
                                occasions=200)
        truth = config.classes()
        for occasion in generate(config):
            for surface in occasion.surfaces:
                self.assertIn(surface, truth)


class ShuffledControl(unittest.TestCase):
    """The control has to keep frequency and destroy structure. Both halves."""

    def setUp(self) -> None:
        self.config = OccasionConfig(concepts=8, surfaces=3, presence=0.8,
                                     noise=2, distractors=1, occasions=3000)
        self.stream = generate(self.config)
        self.control = shuffled(self.stream, seed=11)

    def test_it_destroys_co_occurrence(self):
        """The point of the control, and the quantity is LIFT, not presence.

        The first version of this asserted that a same-concept partner is
        present less often in the control, and it FAILED — the control scored
        higher. The reason is a property of the instrument worth keeping: with
        few concepts the world is small, so an occasion drawing five or six
        surfaces out of twenty-odd lands on a same-concept surface by accident
        roughly two times in five. **Presence is not evidence of structure at
        small world size**, which is a caution for any sweep cell that shrinks
        `concepts`.

        What separates the two streams at any world size is how much likelier
        than its own base rate a concept-mate is. That is 1.0 in the control by
        construction, because members are drawn independently of one another.
        """
        def lift(stream: list[Occasion]) -> float:
            appearances: Counter[int] = Counter()
            mate_with: Counter[int] = Counter()
            for occasion in stream:
                present = set(occasion.surfaces)
                for surface in present:
                    appearances[surface] += 1
                    own = self.config.concept_of(surface)
                    if own is None:
                        continue
                    mates = {other for other in present
                             if other != surface
                             and self.config.concept_of(other) == own}
                    mate_with[surface] += len(mates)

            total = len(stream)
            ratios = []
            for surface, seen in appearances.items():
                own = self.config.concept_of(surface)
                if own is None:
                    continue
                mates = [m for m in range(own * self.config.surfaces,
                                          (own + 1) * self.config.surfaces)
                         if m != surface]
                expected = sum(appearances[m] for m in mates) / total
                if expected > 0:
                    ratios.append((mate_with[surface] / seen) / expected)
            return sum(ratios) / len(ratios)

        self.assertGreater(lift(self.stream), 2.0,
                           "the real stream must over-represent concept-mates")
        self.assertLess(lift(self.control), 1.2,
                        "the control must sit at chance, and it is the floor "
                        "every recovered score is read against")

    def test_it_keeps_the_common_things_common(self):
        """The companion. A control that also flattened frequency would make a
        frequency-only mechanism look like it had learned structure."""
        real = Counter(s for o in self.stream for s in o.surfaces)
        fake = Counter(s for o in self.control for s in o.surfaces)
        distractor = self.config.concept_surfaces
        self.assertEqual(max(real, key=real.get), distractor)
        self.assertEqual(max(fake, key=fake.get), distractor)

    def test_it_keeps_the_occasion_sizes(self):
        self.assertEqual([len(o.surfaces) for o in self.control],
                         [len(o.surfaces) for o in self.stream])


class Validation(unittest.TestCase):
    """Bad configurations fail at construction, not in the middle of a sweep."""

    def test_one_concept_is_refused(self):
        with self.assertRaises(ValueError):
            OccasionConfig(concepts=1)

    def test_one_surface_per_concept_is_refused(self):
        with self.assertRaises(ValueError):
            OccasionConfig(surfaces=1)

    def test_presence_above_one_is_refused(self):
        with self.assertRaises(ValueError):
            OccasionConfig(presence=1.5)

    def test_presence_of_zero_is_refused(self):
        with self.assertRaises(ValueError):
            OccasionConfig(presence=0.0)

    def test_negative_zipf_is_refused(self):
        with self.assertRaises(ValueError):
            OccasionConfig(zipf=-1.0)

    def test_more_noise_than_the_world_holds_is_refused(self):
        with self.assertRaises(ValueError):
            OccasionConfig(concepts=3, surfaces=2, noise=5)


if __name__ == "__main__":
    unittest.main()
