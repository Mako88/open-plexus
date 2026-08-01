"""Two nodes, different data, one seed — do the codes mean the same thing?

That falsifier was specified before this file existed and never written, which is
how the front end came to be the one component nobody had tested the distributed
claim of. It is the first class here, and it is measured against `grouping.cluster`
in the same test rather than asserted alone: **an agreement test that only ever
runs the arm that passes cannot fail**, and the k-means arm is what makes the
number mean something. Measured on this data, LSH routes 100% of shared items to
the same code and k-means routes 0-7%.

The rest is the connection test the mechanism needs — perturb the input, watch the
code move — plus the two refusals a front end must make: a zero input has no
content to address, and a configuration that gives every input its own code has
stopped discretising.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.grouping import codes as kmeans
from openplexus.surfaces import (Hyperplanes, agreement, centred, purity,
                                 spectra, waveform)

WIDTH, CLASSES = 32, 10

#: Cluster spread. Chosen here so classes overlap enough that a code can be
#: impure -- at a smaller spread every code is pure, agreement is 1.0 for free,
#: and the test would pass on data that cannot distinguish anything.
SPREAD = 0.35


def world(seed: int) -> np.ndarray:
    """`CLASSES` centres. A node's data is drawn from these; the centres are not."""
    return np.random.default_rng(seed).normal(size=(CLASSES, WIDTH))


def draw(centres: np.ndarray, count: int,
         seed: int) -> tuple[np.ndarray, list[int]]:
    rng = np.random.default_rng(seed)
    labels = rng.integers(len(centres), size=count)
    rows = centres[labels] + rng.normal(scale=SPREAD, size=(count, WIDTH))
    return rows, [int(label) for label in labels]


class TwoNodesWithDifferentDataMustAgree(unittest.TestCase):
    """The falsifier. Same seed, disjoint samples, and the same input arriving.

    Both arms are run on the same rows so the comparison is of front ends and
    nothing else. The shared items are quantised BY EACH NODE ALONGSIDE ITS OWN
    DATA, because that is the only way k-means can be asked the question at all —
    it has no out-of-sample assignment, which is itself part of what is wrong
    with it as a front end.
    """

    def _samples(self, trial: int):
        centres = world(trial)
        shared, shared_labels = draw(centres, 60, seed=1000 + trial)
        mine, _ = draw(centres, 300, seed=2000 + trial)
        yours, _ = draw(centres, 300, seed=3000 + trial)
        return shared, shared_labels, mine, yours

    def test_the_same_input_gets_the_same_code_on_both_nodes(self):
        for trial in range(3):
            shared, _, mine, yours = self._samples(trial)
            here = Hyperplanes(WIDTH, bits=8, seed=7)
            there = Hyperplanes(WIDTH, bits=8, seed=7)
            # Each node has already quantised its own stream. For the hash that
            # cannot matter, and this asserts it rather than assuming it.
            here.codes(mine)
            there.codes(yours)
            self.assertEqual(here.codes(shared), there.codes(shared),
                             f"trial {trial}: two nodes disagreed about where "
                             f"an identical input belongs")

    def test_the_trained_quantiser_does_not(self):
        """The companion. Without it the test above is a tautology dressed up."""
        for trial in range(3):
            shared, _, mine, yours = self._samples(trial)
            codes = len(set(Hyperplanes(WIDTH, bits=8, seed=7).codes(mine)))
            here = kmeans(np.vstack([mine, shared]), codes, seed=7)[-len(shared):]
            there = kmeans(np.vstack([yours, shared]), codes, seed=7)[-len(shared):]
            same = sum(a == b for a, b in zip(here, there)) / len(shared)
            self.assertLess(same, 0.5,
                            f"trial {trial}: k-means agreed on {same:.2f} of "
                            f"the shared items, which would mean the two front "
                            f"ends are not distinguishable on this data and "
                            f"the test above proves nothing")

    def test_what_a_code_MEANS_survives_a_different_sample(self):
        """Routing is exact; meaning is statistical, and this is the weaker claim.

        A code holding two classes takes its majority from whichever the sample
        held more of, so this falls below 1.0 with nothing wrong with the hash —
        and **what sets it is items per code, not the front end.** Measured at 8
        bits across five worlds: 0.69-1.00 at 300 items (5 per code), 0.87-0.96
        at 800, 0.85-0.95 at 1,500. The first band is the ruler being read at a
        sample size that cannot resolve it, which is why 1,200 is drawn here.
        """
        for trial in range(3):
            centres = world(trial)
            mine, my_labels = draw(centres, 1200, seed=2000 + trial)
            yours, your_labels = draw(centres, 1200, seed=3000 + trial)
            hashed = Hyperplanes(WIDTH, bits=8, seed=7)
            _, here = purity(hashed.codes(mine), my_labels)
            _, there = purity(hashed.codes(yours), your_labels)
            share, shared = agreement(here, there)
            self.assertGreater(shared, 20, "too few codes in common to score")
            self.assertGreater(share, 0.8, f"trial {trial}: {share:.2f}")


class TheCodeFollowsTheINPUT(unittest.TestCase):
    """The connection test. Move the input, and the code has to move with it."""

    def test_far_apart_inputs_get_different_codes(self):
        centres = world(0)
        hashed = Hyperplanes(WIDTH, bits=12, seed=3)
        codes = [hashed.code(centre) for centre in centres]
        self.assertEqual(len(set(codes)), len(centres))

    def test_a_nudge_usually_leaves_the_code_alone(self):
        """Which is the whole point: near things must be able to collide."""
        centres = world(0)
        hashed = Hyperplanes(WIDTH, bits=6, seed=3)
        rng = np.random.default_rng(5)
        kept = sum(hashed.code(c) == hashed.code(c + rng.normal(scale=0.05,
                                                               size=WIDTH))
                   for c in centres)
        self.assertGreaterEqual(kept, len(centres) - 2)

    def test_scaling_an_input_does_not_change_its_code(self):
        # A sign is scale-free, so loudness and brightness cannot become
        # identity. If this ever fails, a quiet recording of `six` has become a
        # different concept from a loud one.
        hashed = Hyperplanes(WIDTH, bits=10, seed=3)
        vector = np.random.default_rng(11).normal(size=WIDTH)
        self.assertEqual(hashed.code(vector), hashed.code(vector * 17.0))
        self.assertEqual(hashed.code(vector), hashed.code(vector * 0.001))

    def test_a_different_seed_is_a_different_partition(self):
        rows, _ = draw(world(0), 200, seed=4)
        one = Hyperplanes(WIDTH, bits=8, seed=1).codes(rows)
        other = Hyperplanes(WIDTH, bits=8, seed=2).codes(rows)
        self.assertLess(sum(a == b for a, b in zip(one, other)) / len(rows),
                        0.2)

    def test_more_bits_is_a_finer_partition(self):
        rows, _ = draw(world(0), 400, seed=4)
        distinct = [len(set(Hyperplanes(WIDTH, bits=b, seed=3).codes(rows)))
                    for b in (2, 4, 6, 8)]
        self.assertEqual(distinct, sorted(distinct))
        self.assertLess(distinct[0], distinct[-1])

    def test_finer_codes_are_purer(self):
        """Granularity is the dial README §3 wants, so it has to move something."""
        rows, labels = draw(world(0), 400, seed=4)
        scores = [purity(Hyperplanes(WIDTH, bits=b, seed=3).codes(rows),
                         labels)[0] for b in (2, 5, 8)]
        self.assertEqual(scores, sorted(scores))


class TheBatchIsTheSameAsTheLoop(unittest.TestCase):
    """One matrix product and one call per row must not be two front ends."""

    def test_codes_matches_code_row_for_row(self):
        rows, _ = draw(world(0), 50, seed=4)
        hashed = Hyperplanes(WIDTH, bits=9, seed=3)
        self.assertEqual(hashed.codes(rows), [hashed.code(row) for row in rows])

    def test_including_the_empty_rows(self):
        rows, _ = draw(world(0), 10, seed=4)
        rows[3] = 0.0
        hashed = Hyperplanes(WIDTH, bits=9, seed=3)
        self.assertEqual(hashed.codes(rows)[3], -1)
        self.assertEqual(hashed.codes(rows), [hashed.code(row) for row in rows])


class ItRefusesWhatItCannotAddress(unittest.TestCase):

    def test_an_input_with_no_content_gets_no_code(self):
        # Every plane returns exactly 0, so the sign pattern is the tie-break
        # rather than the input. A code here would build one large surface out
        # of silence, which is what `grouping.cluster` refuses a zero row for.
        self.assertEqual(Hyperplanes(WIDTH).code(np.zeros(WIDTH)), -1)

    def test_a_wrong_width_is_refused_rather_than_broadcast(self):
        with self.assertRaises(ValueError):
            Hyperplanes(WIDTH).code(np.ones(WIDTH + 1))
        with self.assertRaises(ValueError):
            Hyperplanes(WIDTH).codes(np.ones((4, WIDTH + 1)))

    def test_zero_bits_is_rejected(self):
        with self.assertRaises(ValueError):
            Hyperplanes(WIDTH, bits=0)

    def test_a_bit_count_that_stops_discretising_is_rejected(self):
        with self.assertRaises(ValueError):
            Hyperplanes(WIDTH, bits=63)


class AgreementSaysWhichKINDOfZeroItIs(unittest.TestCase):
    """0.0 from disagreement and 0.0 from nothing shared are different answers."""

    def test_nothing_in_common_reports_no_shared_codes(self):
        self.assertEqual(agreement({1: 0}, {2: 0}), (0.0, 0))

    def test_disagreement_reports_the_codes_it_scored(self):
        self.assertEqual(agreement({1: 0, 2: 1}, {1: 5, 2: 6}), (0.0, 2))

    def test_weighting_by_traffic_changes_the_answer(self):
        # One code holds a thousand items and agrees; one holds one and does
        # not. Unweighted that is 0.5, and weighted it is what a router would
        # actually misdirect.
        one, other = {1: 0, 2: 0}, {1: 0, 2: 9}
        self.assertEqual(agreement(one, other)[0], 0.5)
        self.assertGreater(agreement(one, other, {1: 1000, 2: 1})[0], 0.99)


class CentringIsPerITEMOrItIsNotAllowED(unittest.TestCase):
    """The property that makes it legal: a row is centred by itself alone."""

    def test_a_rows_result_does_not_depend_on_its_neighbours(self):
        # A mean taken over the batch would be a statistic of this node's
        # sample, two nodes would hold different ones, and the front end would
        # be back to disagreeing -- which is the whole thing being removed.
        rows = np.abs(np.random.default_rng(0).normal(size=(20, WIDTH))) + 5.0
        alone = centred(rows[7:8])
        with_others = centred(rows)[7]
        self.assertTrue(np.allclose(alone[0], with_others))

    def test_it_opens_up_all_positive_data_the_hash_cannot_cut(self):
        """The measured failure it repairs, in miniature and with a companion."""
        rng = np.random.default_rng(3)
        # An offset far larger than the variation, which is what a log-energy
        # spectrum is: every row points almost the same way.
        rows = rng.normal(size=(400, WIDTH)) + 60.0
        hashed = Hyperplanes(WIDTH, bits=10, seed=1)
        raw = len(set(hashed.codes(rows)))
        opened = len(set(hashed.codes(centred(rows))))
        self.assertLess(raw, 8)
        self.assertGreater(opened, 100)

    def test_it_leaves_data_that_is_already_spread_out_alone(self):
        # Otherwise it is not a repair for an offset, it is a second front end
        # with its own effects, and the axis would mean something different on
        # every modality.
        rows, labels = draw(world(0), 400, seed=4)
        hashed = Hyperplanes(WIDTH, bits=8, seed=1)
        raw = purity(hashed.codes(rows), labels)[0]
        opened = purity(hashed.codes(centred(rows)), labels)[0]
        self.assertLess(abs(raw - opened), 0.15)


class TheAudioFeaturesAreShapedAsPromised(unittest.TestCase):

    class Recording:
        def __init__(self, samples):
            self.samples = samples

    def test_one_row_per_recording_of_segments_by_bands(self):
        rng = np.random.default_rng(0)
        heard = [self.Recording(list(rng.integers(-2000, 2000, size=n)))
                 for n in (4000, 6000)]
        self.assertEqual(spectra(heard, segments=4, bands=8).shape, (2, 32))

    def test_a_recording_shorter_than_the_segmentation_is_padded(self):
        # Rather than raising or returning a short row: FSDD has recordings of
        # a few hundred samples and a front end that dropped them would quietly
        # change which digits are in the stream.
        self.assertEqual(spectra([self.Recording([1, -1, 1])],
                                 segments=8, bands=4).shape, (1, 32))

    def test_the_waveform_is_one_fixed_width_whatever_the_length(self):
        rng = np.random.default_rng(0)
        heard = [self.Recording(list(rng.integers(-2000, 2000, size=n)))
                 for n in (500, 9000)]
        self.assertEqual(waveform(heard, width=256).shape, (2, 256))

    def test_the_waveform_keeps_the_shape_it_stretched(self):
        # A connection test for the stretch: the same sound at two speeds must
        # come back as nearly the same row, or the fixed width is destroying
        # what it is supposed to preserve and the comparison against the
        # spectrum would be measuring the resampler.
        slow = np.sin(np.arange(4000) * 0.05) * 1000
        fast = np.sin(np.arange(2000) * 0.10) * 1000
        rows = waveform([self.Recording(list(slow.astype(int))),
                         self.Recording(list(fast.astype(int)))], width=512)
        rows = rows / np.linalg.norm(rows, axis=1, keepdims=True)
        self.assertGreater(rows[0] @ rows[1], 0.9)

    def test_two_recordings_of_the_same_shape_of_sound_land_near_each_other(self):
        """Otherwise the hash has nothing to collide and the front end is noise."""
        rng = np.random.default_rng(1)
        tone = np.sin(np.arange(4000) * 0.3) * 1000
        near = [self.Recording(list((tone + rng.normal(scale=50, size=4000))
                                    .astype(int))) for _ in range(2)]
        far = self.Recording(list(rng.integers(-1000, 1000, size=4000)))
        rows = spectra(near + [far])
        rows = rows / np.linalg.norm(rows, axis=1, keepdims=True)
        self.assertGreater(rows[0] @ rows[1], rows[0] @ rows[2])


if __name__ == "__main__":
    unittest.main()
