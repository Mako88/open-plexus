"""Chained retrieval: decode a retrieval to a token, re-encode it as a key.

## The defect this file was written after

The mechanism was built, ran, and scored 0.000 on the task it was built for --
and `task=1, model=2` fell from 1.000 to 0.005, so the extra hop destroyed the
case that already worked. The decode was not the problem. Measured:

    frozen decoder  (wv @ r) finds the intermediate : 1.000
    learned readout (wo @ r) finds the intermediate : 1.000
    softmax entropy                                 : 3.912
    uniform would be                                : 3.912

**The decode was right and the re-encode threw it away.** argmax found the
intermediate every single time, and the softmax over those logits was uniform to
three decimals because top-1 beat top-2 by 0.0388. `weights @ wk` with a flat
weight vector is the MEAN of every key row -- a constant, pointing nowhere near
the key it decoded.

So `test_a_flat_decode_produces_the_mean_key` and
`test_a_sharp_decode_approaches_the_argmax_key` assert on the re-encoded key
itself rather than on task accuracy. Accuracy was 0.000 either way while the
mechanism was silently correct in one half and broken in the other; only the key
distinguishes them.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory,
    LocalMemoryConfig,
)


def config(**overrides):
    base = dict(vocab_size=32, d_model=64, lr=0.05, key_scale=0.5,
                decay=0.997, derived_keys=True, memory_cap=5.0, seed=1)
    base.update(overrides)
    return LocalMemoryConfig(**base)


class KeyRecorder:
    """Wraps the retrieval seam and keeps the key of every read, in order."""

    def __init__(self, inner):
        self.inner = inner
        self.keys: list[np.ndarray] = []

    def begin(self, width):
        self.keys.clear()
        return self.inner.begin(width)

    def read(self, readable, key):
        self.keys.append(np.array(key))
        return self.inner.read(readable, key)

    def observe(self, store, key, value, commitment):
        return self.inner.observe(store, key, value, commitment)


def keys_used(model, tokens):
    recorder = KeyRecorder(model.retrieval)
    model.retrieval = recorder
    model.run(np.asarray(tokens), learn=False)
    model.retrieval = recorder.inner
    return recorder.keys


TOKENS = [3, 9, 4, 17, 8, 2, 11, 5, 20, 6, 14, 7]


def second_hop(position, hops=2):
    """Index of position `position`'s SECOND read.

    Reads run in position order, `hops` of them per position, so this is
    arithmetic rather than a guess -- and `test_each_extra_hop_costs_another
    _read` is what holds that arithmetic true.
    """
    return position * hops + 1


class OneHopIsExactlyTheOldPath(unittest.TestCase):

    def test_hops_one_reads_once_per_position(self):
        model = LocalAssociativeMemory(config(hops=1))
        self.assertEqual(len(keys_used(model, TOKENS)), len(TOKENS))

    def test_each_extra_hop_costs_another_read(self):
        """The cost of the mechanism, asserted rather than assumed. A hop is a
        second full retrieval, so `hops` multiplies retrieval work -- and it
        multiplies the across-group pooling with it."""
        for hops in (2, 3, 4):
            model = LocalAssociativeMemory(config(hops=hops))
            with self.subTest(hops=hops):
                self.assertEqual(len(keys_used(model, TOKENS)),
                                 len(TOKENS) * hops)

    def test_sharpness_is_not_read_when_there_is_one_hop(self):
        """A knob that quietly reaches a path it does not belong to is its own
        defect class. With `hops=1` no decode happens, so every sharpness must
        give bit-identical output."""
        baseline = None
        for sharpness in (0.0, 6.0, 1000.0):
            model = LocalAssociativeMemory(
                config(hops=1, hop_sharpness=sharpness))
            model.wo[:] = model.wv
            out = np.asarray(model.run(np.asarray(TOKENS), learn=False))
            if baseline is None:
                baseline = out
            else:
                np.testing.assert_array_equal(out, baseline)


class HopsChangeReadingAndNeverWriting(unittest.TestCase):
    """**The defect that cost the most to find.**

    `key` is the token's key, and it is carried out of the retrieval block into
    `previous_key`, which is what the NEXT position writes its binding with. The
    hop loop reassigned that same `key`, so with `hops > 1` every binding in the
    store was written using a re-encoded hop key instead of the token's.

    The hop mechanism was corrupting the memory it was trying to read, and the
    symptom was that turning hops on destroyed the 1-hop case that already
    worked. Four probes, two refuted hypotheses and a real instrument fix went
    past before this was found, because every one of them measured RETRIEVAL and
    the damage was in the WRITE.

    The invariant, stated so it cannot come back: **hops changes what is read,
    never what is written.**
    """

    def test_the_store_is_identical_at_every_hop_count(self):
        stores = {}
        for hops in (1, 2, 3):
            model = LocalAssociativeMemory(config(hops=hops))
            model.wo[:] = model.wv
            written = []

            inner = model.retrieval

            class Watch:
                def begin(self, width):
                    return inner.begin(width)

                def read(self, readable, key):
                    return inner.read(readable, key)

                def observe(self, store, key, value, commitment):
                    written.append(np.array(key))
                    return inner.observe(store, key, value, commitment)

            model.retrieval = Watch()
            model.run(np.asarray(TOKENS), learn=False)
            stores[hops] = written

        for hops in (2, 3):
            with self.subTest(hops=hops):
                self.assertEqual(len(stores[hops]), len(stores[1]))
                for one, many in zip(stores[1], stores[hops]):
                    np.testing.assert_allclose(one, many, atol=1e-12)


class TheDecodeIsActuallySharpened(unittest.TestCase):

    def test_a_flat_decode_produces_the_mean_key(self):
        """**The measured defect.** Sharpness 0 is a uniform softmax, so the
        re-encoded key is the mean of every row of `Wk` -- the same vector no
        matter what was decoded, which is what made every hop land in the same
        wrong place.

        Both decoders, because the defect is in the re-encode and neither
        choice of decoder protects against it."""
        for decoder in ("encoder", "readout"):
            model = LocalAssociativeMemory(
                config(hops=2, hop_sharpness=0.0, hop_decoder=decoder))
            model.wo[:] = model.wv
            keys = keys_used(model, TOKENS)
            mean_key = model.wk.mean(axis=0)
            for position in range(len(TOKENS)):
                with self.subTest(decoder=decoder, position=position):
                    np.testing.assert_allclose(keys[second_hop(position)],
                                               mean_key, atol=1e-9)

    def test_the_two_decoders_do_not_agree(self):
        """The axis has to be a real choice. `Wo` starts equal to `Wv` in these
        experiments, so if nothing ever moved it the two decoders would be the
        same matrix and the contrast would measure nothing -- a comparison
        between a thing and itself, which reads as a clean null result."""
        keys = {}
        for decoder in ("encoder", "readout"):
            # Same seed, so the two models differ ONLY in the decoder.
            model = LocalAssociativeMemory(
                config(hops=2, hop_decoder=decoder))
            model.wo[:] = model.wv
            # A RANDOM perturbation, not a constant one. The first version of
            # this test added 0.5 everywhere, which shifts every vocab logit by
            # the same amount -- and standardising removes constants, so the
            # two decoders agreed and the test failed. It was perturbing the
            # decoder in the one direction the mechanism is built to ignore.
            model.grouped_wo[:] += np.random.default_rng(0).normal(
                0.0, 0.5, model.grouped_wo.shape)
            keys[decoder] = keys_used(model, TOKENS)

        differ = any(not np.allclose(keys["encoder"][second_hop(p)],
                                     keys["readout"][second_hop(p)])
                     for p in range(1, len(TOKENS)))
        self.assertTrue(differ)

    def test_an_empty_store_decodes_to_no_particular_key(self):
        """At the FIRST position nothing has been written, so the retrieval is
        zero, every logit is equal, and the spread guard leaves the decode
        uniform. That is the mechanism declining to name a token it has no
        basis for, and it is why the sharpness test below starts later.

        Asserted rather than excluded: if this ever became sharp it would mean
        the hop had invented a confident answer out of an empty store."""
        model = LocalAssociativeMemory(config(hops=2, hop_sharpness=500.0))
        model.wo[:] = model.wv
        keys = keys_used(model, TOKENS)
        np.testing.assert_allclose(keys[second_hop(0)],
                                   model.wk.mean(axis=0), atol=1e-9)

    def test_a_sharp_decode_approaches_the_argmax_key(self):
        """The other end of the dial. High sharpness must put nearly all the
        weight on one token, so the re-encoded key is nearly a row of `Wk` --
        which is what makes the next retrieval a real lookup."""
        model = LocalAssociativeMemory(config(hops=2, hop_sharpness=500.0))
        model.wo[:] = model.wv
        keys = keys_used(model, TOKENS)
        rows = model.wk
        mean_distance = float(
            np.linalg.norm(rows - rows.mean(axis=0), axis=1).mean())
        # From position 1: position 0 reads an empty store, tested above.
        for position in range(1, len(TOKENS)):
            distances = np.linalg.norm(rows - keys[second_hop(position)],
                                       axis=1)
            with self.subTest(position=position):
                self.assertLess(float(distances.min()), 0.1 * mean_distance)

    def test_sharpening_is_scale_free(self):
        """Standardising the logits is the whole reason a constant was not used.
        Scaling the readout scales the logits and must NOT change the decode --
        otherwise the sharpness means something different in every cell and the
        mechanism breaks silently when an unrelated setting moves."""
        keys = []
        for scale in (1.0, 25.0):
            model = LocalAssociativeMemory(config(hops=2, hop_sharpness=6.0))
            model.wo[:] = model.wv
            model.grouped_wo *= scale
            keys.append(keys_used(model, TOKENS)[1])
        np.testing.assert_allclose(keys[0], keys[1], atol=1e-8)


class ImpossibleSettingsAreRefused(unittest.TestCase):

    def test_zero_hops_is_refused(self):
        with self.assertRaises(ValueError):
            config(hops=0)

    def test_negative_hops_is_refused(self):
        with self.assertRaises(ValueError):
            config(hops=-1)

    def test_negative_sharpness_is_refused(self):
        """A negative gain decodes to the LEAST likely token, which would be a
        working mechanism pointed backwards -- the kind of thing that produces
        a plausible curve and no error."""
        with self.assertRaises(ValueError):
            config(hops=2, hop_sharpness=-1.0)


if __name__ == "__main__":
    unittest.main()
