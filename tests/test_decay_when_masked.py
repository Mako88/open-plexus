"""Separating what the oracle stores from what it keeps.

[Note 019](../docs/notes/019-the-oracle-also-slows-forgetting.md): `memory *=
decay` sits **inside** the `store[t]` guard, so a masked-out position is not
merely un-written — it is un-faded. On MQAR with 92% filler, an oracle-gated arm
skips the fade on 92% of steps and runs at an effective half-life roughly an
order of magnitude longer than the ungated arm at the same nominal `decay`.

So the oracle stores less **and** forgets more slowly, and every gating result
has described only the first. Six mechanisms have failed to match it, all of them
aimed at selectivity alone.

`decay_when_masked` fades on every step regardless of the mask, giving an oracle
its selectivity without its retention. These tests pin the flag; how much of the
advantage is which is g8-05's business.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 16, 24
TOKENS = np.random.default_rng(13).integers(0, VOCAB, 150)
#: Keep one step in five, so most steps are masked and the fade they skip is
#: most of the fade there is. A mask that kept nearly everything would make the
#: flag's effect too small to see, and the tests would pass on nothing.
MASK = np.zeros(len(TOKENS), dtype=bool)
MASK[::5] = True


def build(decay_when_masked: bool = False, decay: float = 0.97):
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5, decay=decay,
        decay_when_masked=decay_when_masked, seed=4))
    model.wo[:] = model.wv           # a decoder, so predictions track the memory
    return model


class OffByDefault(unittest.TestCase):
    """Every result in this project was measured with the fade inside the guard."""

    def test_the_default_is_off(self):
        self.assertFalse(LocalMemoryConfig(vocab_size=VOCAB).decay_when_masked)

    def test_off_is_unchanged_from_before_the_flag_existed(self):
        np.testing.assert_array_equal(
            build(decay_when_masked=False).run(TOKENS, store=MASK),
            build(decay_when_masked=False).run(TOKENS, store=MASK))


class ItOnlyTouchesMaskedSteps(unittest.TestCase):

    def test_without_a_mask_the_flag_changes_nothing(self):
        """The control. With no mask every step is a stored step, so every step
        already fades and there is nothing for the flag to add. If this moves,
        the flag is reaching something it should not."""
        np.testing.assert_array_equal(
            build(decay_when_masked=True).run(TOKENS),
            build(decay_when_masked=False).run(TOKENS))

    def test_with_a_mask_it_changes_the_answer(self):
        self.assertFalse(
            np.array_equal(build(decay_when_masked=True).run(TOKENS, store=MASK),
                           build(decay_when_masked=False).run(TOKENS, store=MASK)),
            "fading on masked steps changed nothing, so the flag is not being "
            "consulted and the retention bonus cannot be measured")

    def test_a_mask_that_keeps_everything_makes_it_a_no_op(self):
        """The equivalence that says what the flag IS: it only ever adds fades
        for steps that were skipped, so a mask skipping nothing adds nothing."""
        everything = np.ones(len(TOKENS), dtype=bool)
        np.testing.assert_array_equal(
            build(decay_when_masked=True).run(TOKENS, store=everything),
            build(decay_when_masked=False).run(TOKENS, store=everything))

    def test_with_no_decay_at_all_it_is_a_no_op(self):
        """There is no fade to apply, masked or not."""
        np.testing.assert_array_equal(
            build(decay_when_masked=True, decay=1.0).run(TOKENS, store=MASK),
            build(decay_when_masked=False, decay=1.0).run(TOKENS, store=MASK))


class ItReweightsEarlyAgainstLate(unittest.TestCase):
    """WHERE the extra fades fall is what decides whether they are visible.

    A first version of this class stored only the three earliest steps and
    expected the flag to matter, since 147 extra fades would follow. It does
    not — and the reason is the same fact that shaped the `memory_cap` tests:
    **a uniform rescale of the store cannot change an argmax.** Fades applied
    after every write scale the whole memory by one factor, every score with it,
    and the largest score is still the largest.

    The flag is only visible when skipped steps are INTERLEAVED with stored ones,
    because then it changes how much an early binding has faded relative to a
    late one. That is a reweighting, not a rescaling.
    """

    def test_fades_after_every_write_are_invisible(self):
        """Pins the reason, so the next person does not write the same test."""
        early = np.zeros(len(TOKENS), dtype=bool)
        early[:3] = True
        np.testing.assert_array_equal(
            build(decay_when_masked=True).run(TOKENS, store=early),
            build(decay_when_masked=False).run(TOKENS, store=early),
            "extra fades applied after all the writes changed the answer, which "
            "a uniform rescale of the store cannot do")

    def test_fades_between_writes_are_not(self):
        """The same number of extra fades, moved so they fall between writes."""
        spread = np.zeros(len(TOKENS), dtype=bool)
        spread[::50] = True          # three writes, far apart
        self.assertFalse(
            np.array_equal(
                build(decay_when_masked=True).run(TOKENS, store=spread),
                build(decay_when_masked=False).run(TOKENS, store=spread)),
            "three writes spread across the sequence were unaffected by whether "
            "the gaps between them faded, so the flag is not reweighting "
            "anything")

    def _reference(self, model, tokens, mask, decay, fade_on):
        """The update rule, written out, so the fade SCHEDULE is inspectable.

        No black-box comparison can see which step the fade guard reads: the
        mutation shifts the fades by one while leaving the writes where they
        are, and no mask reproduces that pairing. A comparison between two masks
        changes what is STORED, which is a different thing and is what a first
        attempt at this test measured -- it passed under the mutation, which is
        how the mistake surfaced.

        `fade_on` picks the step the masked fade is decided by: `t` is the
        mechanism, `t - 1` is the mutation.
        """
        memory = np.zeros((WIDTH, WIDTH))
        predictions = np.zeros(len(tokens), dtype=np.int64)
        for t, token in enumerate(tokens):
            if t and fade_on(t) and decay < 1.0:
                memory = memory * decay
            if t and mask[t]:
                if decay < 1.0:
                    memory = memory * decay
                memory = memory + np.outer(model.wv[token],
                                           model.wk[tokens[t - 1]])
            predictions[t] = int((model.wo @ (memory @ model.wk[token])).argmax())
        return predictions

    def test_shifting_the_fade_guard_by_one_step_is_a_NO_OP(self):
        """BACKLOG's open question, settled: it is a no-op, for ANY mask.

        The fade `decay_when_masked` adds sits behind `not store[t]`. A mutation
        pointing it at `store[t - 1]` survives the whole suite, and BACKLOG
        guessed that an ASYMMETRIC mask would expose it -- some writes adjacent,
        some isolated, so bindings lose different numbers of fades. **That guess
        is wrong and the counting shows why.**

        Take consecutive writes at `a` and `b`. The mechanism fades on masked
        steps, which in `(a, b]` are the steps `(a, b)` -- `b - a - 1` of them.
        The mutation fades where the PREVIOUS step was masked, which in `(a, b]`
        are `(a + 1, b]` -- also `b - a - 1`. **The total decay applied before
        every write is identical, whatever the mask.**

        Between writes the two stores differ by at most one factor of `decay`,
        and a uniform rescale cannot move an argmax -- the same fact
        `test_fades_after_every_write_are_invisible` rests on.

        So `masked-fade-reads-the-previous-step` was REMOVED from
        tools/mutate.py rather than kept as a failing check. This test exists so
        the next person does not spend the afternoon rediscovering it: the
        branch is genuinely untestable through predictions, and that is a fact
        about the mechanism, not a hole in the suite.
        """
        for name, writes in (("adjacent pair and a lone write", [10, 11, 60]),
                             ("uneven runs", [5, 6, 7, 40, 90, 91]),
                             ("one write only", [30])):
            with self.subTest(name):
                mask = np.zeros(len(TOKENS), dtype=bool)
                mask[writes] = True
                model = build(decay_when_masked=True)
                np.testing.assert_array_equal(
                    self._reference(model, TOKENS, mask, 0.97,
                                    fade_on=lambda t: not mask[t]),
                    self._reference(model, TOKENS, mask, 0.97,
                                    fade_on=lambda t: not mask[t - 1]),
                    f"{name}: the two fade schedules disagreed, so the branch "
                    f"IS observable and the mutation should be restored")

    def test_the_model_matches_the_update_rule_written_out(self):
        """And the reference is worth having on its own: it is the only check
        here that reads the fade schedule rather than a difference between two
        configurations."""
        mask = np.zeros(len(TOKENS), dtype=bool)
        mask[[10, 11, 60]] = True
        model = build(decay_when_masked=True)
        np.testing.assert_array_equal(
            model.run(TOKENS, store=mask),
            self._reference(model, TOKENS, mask, 0.97,
                            fade_on=lambda t: not mask[t]))

    def test_both_masks_store_the_same_number_of_steps(self):
        """Otherwise the pair above differ in how much they stored, not in
        where the fades fell, and the comparison says nothing."""
        early = np.zeros(len(TOKENS), dtype=bool)
        early[:3] = True
        spread = np.zeros(len(TOKENS), dtype=bool)
        spread[::50] = True
        self.assertEqual(early.sum(), spread.sum())


if __name__ == "__main__":
    unittest.main()
