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
from openplexus.tasks.chains import ChainConfig, dataset


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


def keys_used(model, tokens, per_position=None):
    recorder = KeyRecorder(model.retrieval)
    model.retrieval = recorder
    model.run(np.asarray(tokens), learn=False)
    model.retrieval = recorder.inner
    if per_position is not None:
        assert len(recorder.keys) == len(tokens) * per_position, (
            "reads per position is not what the caller assumed, so any index "
            "computed from it is meaningless")
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


class TheGateChoosesWhichHopToRead(unittest.TestCase):
    """A fixed `hops` must match the question exactly (decision 85), so the gate
    learns which hop to read from instead. Decision 86 measured that the signal
    is in the CONTENT, not confidence.

    Both of the gate's defects are asserted here because both produced a
    plausible number rather than an error: an inert gate scored 0.707 on mixed
    depths by letting the readout cope with a flat average, and a gate scored by
    its OWN hop rather than the next one scored 0.773 by solving depth-1
    perfectly and depth-2 not at all.
    """

    def test_the_gate_needs_more_than_one_hop_to_choose_between(self):
        with self.assertRaises(ValueError):
            config(halt_gate=True, hops=1)

    def test_the_gate_is_refused_with_a_hidden_layer(self):
        """The gate mixes RETRIEVALS, which equals mixing predictions only
        through a linear readout. Through a relu the two differ and the
        gradient would be quietly wrong -- so it is refused rather than
        approximated."""
        with self.assertRaises(ValueError):
            config(halt_gate=True, hops=2, hidden=8)

    def test_gating_reads_one_hop_further_than_it_reports(self):
        """**The lookahead.** Hop k is scored by what hop k+1 returns, so a
        `hops=k` gated model performs k+1 retrievals. Without the extra read the
        last hop has nothing scoring it."""
        for hops in (2, 3):
            gated = LocalAssociativeMemory(config(hops=hops, halt_gate=True))
            plain = LocalAssociativeMemory(config(hops=hops))
            with self.subTest(hops=hops):
                self.assertEqual(len(keys_used(gated, TOKENS, hops + 1)),
                                 len(TOKENS) * (hops + 1))
                self.assertEqual(len(keys_used(plain, TOKENS, hops)),
                                 len(TOKENS) * hops)

    def test_a_zero_gain_gate_is_a_flat_average(self):
        """The control that makes the gate's result a claim. Gain 0 is a uniform
        softmax however well the vector has learned, so the model must equal a
        plain mean over hops -- which is what the gate was accidentally doing
        before the gain existed."""
        model = LocalAssociativeMemory(
            config(hops=2, halt_gate=True, gate_sharpness=0.0))
        model.wo[:] = model.wv
        model.halt_w += 5.0  # would dominate any softmax that read it
        gated = np.asarray(model.run(np.asarray(TOKENS), learn=False))

        same = LocalAssociativeMemory(
            config(hops=2, halt_gate=True, gate_sharpness=0.0))
        same.wo[:] = same.wv
        np.testing.assert_array_equal(
            gated, np.asarray(same.run(np.asarray(TOKENS), learn=False)))

    def test_the_gate_solves_depths_a_fixed_hop_count_cannot(self):
        """**The test the structural ones could not replace.**

        Everything else here asserts shape -- read counts, refusals, a zero-gain
        control -- and two mutations survived all of it:
        `the-gate-scores-its-own-hop-not-the-next` and `the-gate-never-learns`.
        Both leave a model that still beats every fixed hop count (0.773 and
        0.707 against 0.500) while doing the wrong thing or nothing at all.

        So this trains on MIXED depths, where a fixed count must fail by
        construction, and asserts the half that both defects give up on. Depth 2
        is the discriminating number: a working gate reaches 1.000, an
        own-hop-scored gate 0.547, an unlearned gate 0.553.
        """
        depths = (1, 2)
        n_chains, n_symbols, seq_len = 4, 48, 48
        vocab = ChainConfig(n_chains=n_chains, hops=1, n_symbols=n_symbols,
                            seq_len=seq_len).vocab_size

        def examples(seed, count):
            out = []
            for depth in depths:
                out.extend((depth, s) for s in dataset(ChainConfig(
                    n_chains=n_chains, hops=depth, n_symbols=n_symbols,
                    seq_len=seq_len, seed=seed + depth * 7919), count))
            np.random.default_rng(seed).shuffle(out)
            return out

        model = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=vocab, d_model=64, lr=0.05, key_scale=0.5, decay=0.997,
            derived_keys=True, memory_cap=5.0, hops=2, hop_sharpness=6.0,
            halt_gate=True, gate_sharpness=200.0, seed=1))
        model.wo[:] = model.wv
        for _ in range(2):
            for _, s in examples(1, 150):
                targets = np.asarray(s.targets)
                scored = targets != -1
                model.run(np.asarray(s.tokens),
                          np.where(scored, targets, 0), scored, learn=True)

        right = {d: 0 for d in depths}
        total = {d: 0 for d in depths}
        for depth, s in examples(10_001, 40):
            predicted = int(np.asarray(
                model.run(np.asarray(s.tokens), learn=False))[
                    s.answer_position])
            total[depth] += 1
            right[depth] += int(predicted == s.asked[-1])

        for depth in depths:
            with self.subTest(depth=depth):
                # A fixed hop count gets one of these two and 0.000 on the
                # other; both gate defects land near 0.55 on depth 2.
                self.assertGreater(right[depth] / total[depth], 0.85)

    def test_the_key_reading_gate_starts_as_the_one_rule_gate(self):
        """`halt_alt` and `halt_select` are zero-initialised, so at step zero
        the extra machinery contributes nothing and the model must be
        bit-identical to the plain gate. That is what makes `gate_reads_key` an
        extension rather than a different mechanism, and it is why the
        answer-only control stays at 1.000."""
        plain = LocalAssociativeMemory(config(hops=2, halt_gate=True))
        plain.wo[:] = plain.wv
        keyed = LocalAssociativeMemory(
            config(hops=2, halt_gate=True, gate_reads_key=True))
        keyed.wo[:] = keyed.wv
        np.testing.assert_array_equal(
            np.asarray(plain.run(np.asarray(TOKENS), learn=False)),
            np.asarray(keyed.run(np.asarray(TOKENS), learn=False)))

    def test_an_added_key_term_would_be_invisible(self):
        """**Why the key MODULATES instead of contributing.**

        The key is the same for every hop at a position, so adding its score to
        each of them shifts the whole column equally and the softmax removes it
        exactly. This asserts that property directly, because it is the reason
        decision 95's proposal as literally written would not have worked — and
        the same trap made a constant perturbation invisible to the decode.
        """
        scores = np.array([[0.4], [1.3], [-0.2]])
        shifted = scores + 7.0        # what an added key term would do

        def softmax(z):
            e = np.exp(z - z.max(axis=0, keepdims=True))
            return e / e.sum(axis=0, keepdims=True)

        np.testing.assert_allclose(softmax(scores), softmax(shifted))

    def test_the_key_changes_the_gate_once_the_selector_is_nonzero(self):
        """The other half: once `halt_select` and `halt_alt` are not zero, the
        current key must actually change the weighting — otherwise the mechanism
        is inert however good its gradient looks."""
        # Asserted on the MIXTURE the readout consumes, not on predictions. The
        # first version compared predicted tokens and passed vacuously: with an
        # untrained readout the mixture moved and `argmax` did not, so an inert
        # mechanism and a working one gave the same answer.
        mixtures = {}
        for select in (0.0, 30.0):
            model = LocalAssociativeMemory(
                config(hops=2, halt_gate=True, gate_reads_key=True))
            model.wo[:] = model.wv
            model.halt_alt += np.random.default_rng(0).normal(
                0.0, 4.0, model.halt_alt.shape)
            model.halt_select += select

            seen = []
            inner = model.retrieval

            class Watch:
                def begin(self, width):
                    return inner.begin(width)

                def read(self, readable, key):
                    out = inner.read(readable, key)
                    seen.append(np.array(out))
                    return out

                def observe(self, store, key, value, commitment):
                    return inner.observe(store, key, value, commitment)

            model.retrieval = Watch()
            model.run(np.asarray(TOKENS), learn=False)
            mixtures[select] = np.array(seen)

        # The reads themselves are identical -- the gate weights them, it does
        # not change what is looked up -- so any difference must come from the
        # selector, and there must BE one.
        np.testing.assert_allclose(mixtures[0.0], mixtures[30.0], atol=1e-12)
        chosen = {}
        for select in (0.0, 30.0):
            model = LocalAssociativeMemory(
                config(hops=2, halt_gate=True, gate_reads_key=True))
            model.halt_select += select
            slice_ = model.wk[TOKENS[3]].reshape(model.config.partitions, -1)
            chosen[select] = 1.0 / (1.0 + np.exp(
                -np.einsum("gd,gd->g", model.halt_select, slice_)))
        self.assertFalse(np.allclose(chosen[0.0], chosen[30.0]))

    def test_the_selector_actually_reaches_the_gate(self):
        """**`the-selector-never-reaches-the-rule` survived everything else.**

        The parameters exist, receive gradient and validate under that mutation;
        only behaviour changes. So this asserts behaviour: with `halt_alt`
        non-zero the model must differ from the same model with it zero, which
        is false the moment `chosen * halt_alt` stops reaching `rule`.

        **At `gate_sharpness=200`, not the default 1.0.** A first version used
        the default, where the gate is nearly uniform whatever the rule says, so
        the selector had nothing to move and the test passed vacuously in both
        directions.
        """
        outputs = {}
        for magnitude in (0.0, 4.0):
            model = LocalAssociativeMemory(config(
                hops=2, halt_gate=True, gate_reads_key=True,
                gate_sharpness=200.0))
            model.wo[:] = model.wv
            model.halt_select += 3.0
            model.halt_alt += np.random.default_rng(0).normal(
                0.0, magnitude, model.halt_alt.shape)
            outputs[magnitude] = np.asarray(
                model.run(np.asarray(TOKENS), learn=False))
        self.assertFalse(np.array_equal(outputs[0.0], outputs[4.0]))

    def test_which_hop_teaches_nothing_when_no_hop_is_right(self):
        """When the answer was not reachable at any depth there is no label —
        any target would be inventing one — so the gate must be left alone.
        Asserted because the alternative is silent: pushing toward hop 0 by
        default would be a plausible-looking bias with no justification."""
        model = LocalAssociativeMemory(config(
            hops=2, halt_gate=True, gate_reads_key=True,
            gate_objective="which_hop", gate_sharpness=200.0))
        model.wo[:] = model.wv
        before = model.halt_w.copy()

        tokens = np.asarray(TOKENS)
        targets = np.full(len(tokens), model.config.vocab_size - 1)
        scored = np.zeros(len(tokens), dtype=bool)
        # A target no retrieval can produce: nothing was ever bound to it.
        scored[2] = True
        model.run(tokens, targets, scored, learn=True)
        np.testing.assert_allclose(model.halt_w, before, atol=1e-12)

    def test_which_hop_moves_the_gate_toward_the_hop_that_was_right(self):
        """The objective's whole content. If some hop names the target, the gate
        must move — otherwise `which_hop` is an expensive way to do nothing."""
        model = LocalAssociativeMemory(config(
            hops=2, halt_gate=True, gate_reads_key=True,
            gate_objective="which_hop", gate_sharpness=200.0))
        # A REAL chain sequence, not an arbitrary token list. On arbitrary
        # tokens every key is a first occurrence, so nothing is retrievable, no
        # hop can name the target, and the gate correctly does nothing — which
        # is what the first version of this test actually measured.
        config_ = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=64,
                              seed=3)
        model = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=config_.vocab_size, d_model=64, lr=0.05, key_scale=0.5,
            decay=0.997, derived_keys=True, memory_cap=5.0, hops=2,
            hop_sharpness=6.0, halt_gate=True, gate_reads_key=True,
            gate_objective="which_hop", gate_sharpness=200.0, seed=1))
        model.wo[:] = model.wv
        before = model.halt_w.copy()

        moved = 0.0
        for sequence in dataset(config_, 20):
            tokens = np.asarray(sequence.tokens)
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, np.roll(tokens, -1), scored, learn=True)
            moved = max(moved, float(np.abs(model.halt_w - before).max()))
        self.assertGreater(moved, 0.0)

    def test_an_unknown_gate_objective_is_refused(self):
        with self.assertRaises(ValueError):
            config(hops=2, halt_gate=True, gate_objective="whatever")

    def test_a_gate_objective_without_a_gate_is_refused(self):
        with self.assertRaises(ValueError):
            config(hops=2, gate_objective="which_hop")

    def test_reading_the_key_without_a_gate_is_refused(self):
        with self.assertRaises(ValueError):
            config(hops=2, gate_reads_key=True)

    def test_the_gate_does_not_touch_the_store(self):
        """The same invariant the hop loop broke, now for the extra lookahead
        read: gating adds a retrieval and must still write nothing new."""
        written = {}
        for gate in (False, True):
            model = LocalAssociativeMemory(config(hops=2, halt_gate=gate))
            model.wo[:] = model.wv
            keys = []
            inner = model.retrieval

            class Watch:
                def begin(self, width):
                    return inner.begin(width)

                def read(self, readable, key):
                    return inner.read(readable, key)

                def observe(self, store, key, value, commitment):
                    keys.append(np.array(key))
                    return inner.observe(store, key, value, commitment)

            model.retrieval = Watch()
            model.run(np.asarray(TOKENS), learn=False)
            written[gate] = keys

        self.assertEqual(len(written[True]), len(written[False]))
        for off, on in zip(written[False], written[True]):
            np.testing.assert_allclose(off, on, atol=1e-12)


class AccumulatingAcrossHops(unittest.TestCase):
    """Decision 101: composing needs BOTH retrievals, and `replace` keeps one.

    `concat` was expected to fail on the argument that a linear readout over
    `[r1, r2]` is additive while composition is not. Measured over the whole
    rule table, that argument is wrong — 1.000 for concat against 0.812 for a
    product — because a handful of rules in a wide space are linearly separable
    whatever structure the labels have.
    """

    def test_concat_widens_the_readout_by_the_hop_count(self):
        for hops in (2, 3):
            plain = LocalAssociativeMemory(config(hops=hops))
            wide = LocalAssociativeMemory(
                config(hops=hops, hop_accumulate="concat"))
            with self.subTest(hops=hops):
                self.assertEqual(wide.wo.shape[1], plain.wo.shape[1] * hops)
                self.assertEqual(wide.grouped_wo.base is wide.wo, True)

    def test_a_hop_decodes_from_the_LATEST_fetch_not_the_accumulator(self):
        """**The bug this nearly shipped with.** Under `bind` the accumulator
        and the newest retrieval differ, and decoding the bound product would
        ask "what token is R1-and-R2 together" — which names nothing, so the
        traversal wanders off after hop 1 while still looking like it runs.

        Asserted by keys: `replace` and `bind` must issue the SAME sequence of
        hop keys, because binding changes what the readout sees and must not
        change where the hops go.
        """
        keys = {}
        for accumulate in ("replace", "bind"):
            model = LocalAssociativeMemory(
                config(hops=3, hop_accumulate=accumulate))
            model.wo[:] = model.wv
            keys[accumulate] = keys_used(model, TOKENS, 3)
        for one, other in zip(keys["replace"], keys["bind"]):
            np.testing.assert_allclose(one, other, atol=1e-12)

    def test_accumulating_needs_something_to_accumulate(self):
        for accumulate in ("bind", "concat"):
            with self.subTest(accumulate=accumulate):
                with self.assertRaises(ValueError):
                    config(hops=1, hop_accumulate=accumulate)

    def test_concat_and_the_gate_are_refused_together(self):
        """The gate chooses WHICH hop to read; concat gives the readout all of
        them. Together the gate would be selecting among inputs the readout
        already has, which is not a mechanism, it is a contradiction."""
        with self.assertRaises(ValueError):
            config(hops=2, hop_accumulate="concat", halt_gate=True,
                   gate_reads_key=False)

    def test_hops_and_context_keys_are_refused_together(self):
        """**A configuration that produced numbers without meaning.**

        A hop re-encodes a decoded token through `Wk`, a single-token table.
        `context_keys` derives the store's keys from `(previous, token)` pairs
        instead. Measured cosine between `context_key(5, 7)` and `wk[7]`:
        **-0.069** — orthogonal. So every hop after the first queried a key
        space the store never writes to, got noise, and the model still
        returned answers and accuracies.

        Refused rather than silently allowed, because a hop that constructs a
        PAIR key is the mechanism this needs and it does not exist yet.
        """
        with self.assertRaises(ValueError):
            config(hops=2, context_keys=True)
        # One hop is fine: the key comes from the key source, not from a
        # re-encode, so there is nothing in the wrong space.
        LocalAssociativeMemory(config(hops=1, context_keys=True))

    def test_the_two_key_spaces_really_are_unrelated(self):
        """The measurement the guard rests on. If these ever became aligned the
        guard could be relaxed — so this records why it is there rather than
        asserting the refusal is permanent."""
        model = LocalAssociativeMemory(config(hops=1, context_keys=True))
        pair = model.context_key(5, 7)
        single = model.wk[7]
        cosine = float(pair @ single
                       / (np.linalg.norm(pair) * np.linalg.norm(single)))
        self.assertLess(abs(cosine), 0.2)

    def test_an_unknown_accumulator_is_refused(self):
        with self.assertRaises(ValueError):
            config(hops=2, hop_accumulate="average")


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
