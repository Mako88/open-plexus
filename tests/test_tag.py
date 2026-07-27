"""A capacity measured in bindings, and a mark that fades.

[g9-03](../experiments/sweeps/g9-03-is-the-cliff-reach-or-cost.txt) found the
reward gate's cliff is the diagonal: a window recovers about 0.2 wherever it
covers the delay and about -0.22 wherever it does not, and a node does not know
the delay. Widening it does not help, because reach is bought in steps — at 31
steps per binding a 64-step window holds two bindings and sixty-two steps of
filler, and recovers 0.09 at every delay.

[g9-04](../experiments/sweeps/g9-04-is-there-a-local-signal.txt) measured what a
mark could hang on. Retrieval strength separates a binding-write from a
filler-write at AUC 0.293 and 0.215 — **below 0.5, so inverted**: filler repeats,
so a filler key has been bound many times and retrieves strongly, while a
binding's cue is fresh and retrieves weakly. The rule is *admit the weak
retrievals*, which is the opposite of what competitive capture does.

**These assert what the gate KEEPS, not what the model then scores.** The claim
a gate makes is about which writes survive; accuracy is downstream of that and
cannot tell a tag holding bindings from a tag holding the first four writes after
every reward. `trace` reports the surviving set directly, so the tests read it.

Whether keeping those writes recovers anything is g9-05's business.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig, admit, fade, tag)

VOCAB, WIDTH = 24, 48
REWARD = VOCAB - 1
CUE, VALUE = 10, 11


def build(slots: int = 0, window: int = 0, decay_tag: float = 1.0,
          strongest: bool = False, relative: bool = False, newest: int = 0,
          reward: int = REWARD, decay: float = 0.99):
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5, decay=decay,
        reward_token=reward, reward_window=window, tag_slots=slots,
        tag_decay=decay_tag, tag_relative=relative, tag_strongest=strongest,
        tag_newest=newest, seed=7))
    # A decoder, so a prediction reads the memory rather than an untrained
    # readout -- the same choice g9-04's probe made, for the same reason.
    model.wo[:] = model.wv
    return model


def _filler(count: int, rng) -> list[int]:
    """Drawn from a small spare alphabet, so filler keys recur and retrieve
    strongly. That recurrence is what makes the signal exist at all."""
    return [int(x) for x in rng.integers(1, 9, count)]


def stream(before: int = 25, after: int = 20, intervals: int = 1,
           seed: int = 0) -> np.ndarray:
    """`intervals` copies of: filler, one fresh binding, filler, reward."""
    rng = np.random.default_rng(seed)
    tokens: list[int] = []
    for _ in range(intervals):
        tokens += _filler(before, rng) + [CUE, VALUE] + _filler(after, rng)
        tokens.append(REWARD)
    return np.array(tokens)


def capture_sizes(model, tokens: np.ndarray) -> list[tuple[int, int]]:
    """Per capture: (writes offered in that interval, writes kept).

    Raw trace numbers, no step mapping, because the invariant being checked is
    about counts and a mapping bug would be indistinguishable from a real one.
    """
    trace: list = []
    model.run(tokens, trace=trace)
    sizes, offered = [], 0
    for entry in trace:
        if entry["write_index"] >= 0:
            offered = entry["write_index"] + 1
        if entry["captured"]:
            sizes.append((offered, len(entry["captured"])))
            offered = 0
    return sizes


def captures(model, tokens: np.ndarray) -> list[list[int]]:
    """The steps each capture kept, one list per reward.

    Read off `trace`, which reports the pending indices a capture protected and
    where each step's own write landed in that list. Reconstructing it here
    rather than reaching into the model keeps the model's surface closed --
    and the trace is pinned as prediction-neutral by test_trace_observes.
    """
    trace: list = []
    model.run(tokens, trace=trace)
    kept, where = [], {}
    for entry in trace:
        if entry["write_index"] >= 0:
            where[entry["write_index"]] = entry["t"]
        if entry["captured"]:
            kept.append(sorted(where[i] for i in entry["captured"] if i in where))
            where = {}
    return kept


def _intervals(count: int, length: int = 80, seed: int = 4) -> np.ndarray:
    """`count` equal stretches of filler, each closed by a reward.

    Equal lengths on purpose: the quantity under test is WHERE in an interval a
    mark lands, so the intervals have to be comparable.
    """
    rng = np.random.default_rng(seed)
    tokens: list[int] = []
    for _ in range(count):
        tokens += _filler(length, rng) + [REWARD]
    return np.array(tokens)


def _mean_offset(steps: list[int], tokens: np.ndarray) -> float:
    """Mean position of these steps within their own interval, as a fraction."""
    rewards = [int(t) for t in np.flatnonzero(tokens == REWARD)]
    closing = min(r for r in rewards if r >= max(steps))
    opening = max([0] + [r for r in rewards if r < min(steps)])
    span = closing - opening
    return sum((step - opening) / span for step in steps) / len(steps)


class WhatEachPendingKeyRetrievesNow(unittest.TestCase):
    """`pending_now` exists because everything else at a capture step is a
    property of the STEP.

    Surprise, strength and the running mean are all identical for every
    candidate at the moment of capture, so none of them can rank candidates.
    Only two candidate-specific things are available: what was recorded at the
    write, and how long ago it was. `pending_now` is the third — a node holds
    `pending`, so it can ask its own store what each pending key retrieves now.

    Whether it says anything the AGE does not already say is g9-13's question.
    These tests only pin that the field means what its comment claims, because
    a probe built on a field that is secretly a step property would answer that
    question with an artefact.
    """

    def setUp(self):
        self.trace: list = []
        build(window=1).run(stream(intervals=3, before=2, after=2),
                            trace=self.trace)
        self.captures = [e for e in self.trace if e["pending_now"]]

    def test_it_is_empty_on_every_step_that_is_not_a_capture(self):
        for entry in self.trace:
            if not entry["captured"]:
                self.assertEqual(entry["pending_now"], ())

    def test_there_is_one_number_per_pending_write(self):
        """Indices into `pending_now` have to line up with `captured`'s, or a
        probe reading them together silently scores the wrong candidate."""
        self.assertTrue(self.captures)
        for entry in self.captures:
            self.assertTrue(max(entry["captured"]) < len(entry["pending_now"]))

    def test_this_steps_own_write_is_the_LAST_candidate(self):
        """The reward step writes too, and its write is the most recent."""
        self.assertTrue(self.captures)
        for entry in self.captures:
            self.assertEqual(entry["write_index"], len(entry["pending_now"]) - 1)

    def test_the_candidates_are_in_PENDING_ORDER(self):
        """Pins the position of each value, which the length cannot.

        `captured` and `pending_now` are indexed into together by anything
        reading them, so a reversed or rotated `pending_now` scores the wrong
        candidate while every length, count and magnitude stays exactly as it
        is. The first version of this test asserted `write_index ==
        len(pending_now) - 1`, which is a claim about SHAPE — the mutation
        harness reversed the list and the test passed.

        The handle is that `pending_now` is computed at ONE moment against ONE
        store, so two writes whose keys are the same token get exactly the same
        value. A stream whose key tokens are [A, A, B] must therefore give
        [x, x, y]; reversed it gives [y, x, x]. No assumption about which key
        retrieves more strongly — only that equal keys give equal values.
        """
        trace: list = []
        # Writes bind the previous token to the current one, so these tokens
        # give keys [3, 3, 5] and the reward closes the interval.
        build(window=1).run(np.array([3, 3, 5, REWARD]), trace=trace)
        now = [e["pending_now"] for e in trace if e["pending_now"]]
        self.assertEqual(len(now), 1)
        first, second, third = now[0]
        self.assertEqual(first, second)
        self.assertNotEqual(second, third)

    def test_the_candidates_do_not_all_carry_the_same_number(self):
        """The whole point. If every candidate got the same value it would be a
        step property wearing a per-candidate shape, and ranking on it would be
        ranking on nothing."""
        varied = [e for e in self.captures if len(set(e["pending_now"])) > 1]
        self.assertTrue(varied, "every capture gave its candidates one value")

    def test_it_is_measured_BEFORE_the_unprotected_writes_are_removed(self):
        """A number taken after the removal would describe the store the
        capture produced, not the one the decision was made from — and a gate
        cannot consult the result of its own decision.

        Checked by a magnitude argument that does not depend on the arithmetic:
        removing writes only shrinks the store, so a reading taken afterwards
        would be no larger than one taken before. A strictly positive value at
        an index the capture DROPPED can only come from the earlier reading.
        """
        dropped = [(e, i) for e in self.captures
                   for i in range(len(e["pending_now"]))
                   if i not in e["captured"]]
        self.assertTrue(dropped, "no capture dropped anything to check")
        self.assertTrue(any(e["pending_now"][i] > 0.0 for e, i in dropped))

    def test_collecting_it_does_not_change_what_the_model_predicts(self):
        tokens = stream(intervals=3, before=2, after=2)
        np.testing.assert_array_equal(
            build(window=1).run(tokens, trace=[]), build(window=1).run(tokens))


class OffByDefault(unittest.TestCase):

    def test_the_default_is_disabled(self):
        config = LocalMemoryConfig(vocab_size=VOCAB)
        self.assertEqual(config.tag_slots, 0)
        self.assertEqual(config.tag_decay, 1.0)
        self.assertFalse(config.tag_strongest)

    def test_a_negative_capacity_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, tag_slots=-1)

    def test_a_tag_without_a_reward_token_is_refused(self):
        """Nothing would ever capture it, so the marks would never be read."""
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, tag_slots=4)

    def test_a_tag_and_a_window_together_are_now_the_combined_gate(self):
        """This pair used to be REFUSED, and the refusal was right at the time.

        They were mutually exclusive while each was being measured apart, so
        that no arm could be a hybrid of the two. Note 023 is why that changed:
        weak retrieval says *this write is a binding* and recency says *this
        binding is the rewarded one*, each mechanism has one answer, and a gate
        needs both. Enabling both now protects the union.
        """
        config = LocalMemoryConfig(vocab_size=VOCAB, reward_token=REWARD,
                                   reward_window=4, tag_slots=4)
        self.assertEqual(config.reward_window, 4)
        self.assertEqual(config.tag_slots, 4)

    def test_a_fade_outside_the_unit_interval_is_refused(self):
        for value in (0.0, -0.5, 1.5):
            with self.subTest(value=value), self.assertRaises(ValueError):
                LocalMemoryConfig(vocab_size=VOCAB, reward_token=REWARD,
                                  tag_slots=4, tag_decay=value)

    def test_a_fade_without_a_tag_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, reward_token=REWARD,
                              tag_decay=0.9)

    def test_the_direction_flag_alone_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, reward_token=REWARD,
                              tag_strongest=True)


class TheTagChoosesWhatSurvives(unittest.TestCase):

    def test_a_tag_keeps_a_different_set_than_a_window(self):
        """The connection test. If these agree the ranking is not consulted and
        the tag is a window with bookkeeping."""
        tokens = stream()
        self.assertNotEqual(captures(build(slots=4, decay_tag=0.9), tokens),
                            captures(build(window=3), tokens))

    def test_capacity_bounds_what_survives(self):
        tokens = stream()
        for slots in (1, 2, 4, 8):
            with self.subTest(slots=slots):
                for kept in captures(build(slots=slots), tokens):
                    self.assertLessEqual(
                        len(kept), slots,
                        "more writes survived than the pool has room for")

    def test_capacity_is_consulted(self):
        tokens = stream()
        sets = {str(captures(build(slots=k, decay_tag=0.9), tokens))
                for k in (1, 4, 16)}
        self.assertGreater(len(sets), 1, "the pool is not binding")

    def test_the_direction_is_consulted(self):
        """`tag_strongest` is g9-05's control arm. If it cannot change what
        survives, the arm measures nothing and the sweep cannot separate 'the
        signal works' from 'any capacity works'."""
        tokens = stream()
        self.assertNotEqual(
            captures(build(slots=4, decay_tag=0.9), tokens),
            captures(build(slots=4, decay_tag=0.9, strongest=True), tokens))

    def test_a_tag_bigger_than_the_demand_keeps_everything(self):
        """The equivalence that says what the mechanism IS.

        With more slots than writes to hold, nothing is refused and nothing is
        displaced, so the gate must reduce **exactly** to not gating. Any
        difference means it does something besides choosing what to keep.
        """
        tokens = stream()
        np.testing.assert_array_equal(build(slots=10_000).run(tokens),
                                      build(reward=-1).run(tokens))

    def test_a_stream_with_no_rewards_is_never_pruned(self):
        """Marks are made and nothing captures them, so nothing is removed."""
        rng = np.random.default_rng(3)
        tokens = np.array(_filler(60, rng) + [CUE, VALUE] + _filler(20, rng))
        np.testing.assert_array_equal(build(slots=2).run(tokens),
                                      build(reward=-1).run(tokens))


class TheCombinedGateKeepsTheUnion(unittest.TestCase):
    """A write survives if EITHER mechanism claimed it.

    Note 023: the two select different things. Weak retrieval finds a binding;
    recency finds the one the reward was about, because the token sits a fixed
    distance after the cue. g9-06 measured them reaching almost the same
    recovery by different routes -- the tag +0.16 flat across delay, the window
    +0.23 where matched and -0.24 where not.

    The union cannot capture LESS than either alone. What it can do is keep more
    and pay for it in interference, since retrieval goes as `sqrt(d / N)`. That
    is the measurement, not these tests.
    """

    #: One interval, so nothing has been captured yet when the marks are made
    #: and the three arms see an identical store. Over several intervals the
    #: arms diverge -- each has subtracted different writes at earlier captures
    #: -- and the union stops being a set operation on the other two.
    def _one_interval(self):
        rng = np.random.default_rng(5)
        return np.array(_filler(50, rng) + [CUE, VALUE] + _filler(30, rng)
                        + [REWARD])

    def test_it_keeps_exactly_the_union(self):
        tokens = self._one_interval()
        tag_only = captures(build(slots=4, decay_tag=0.9), tokens)[0]
        window_only = captures(build(window=3), tokens)[0]
        both = captures(build(slots=4, decay_tag=0.9, window=3), tokens)[0]
        self.assertEqual(both, sorted(set(tag_only) | set(window_only)))

    def test_the_two_arms_do_not_already_agree(self):
        """Guard: if the tag and the window kept the same writes, the union
        would be trivially equal to both and the test above would be vacuous."""
        tokens = self._one_interval()
        self.assertNotEqual(set(captures(build(slots=4, decay_tag=0.9), tokens)[0]),
                            set(captures(build(window=3), tokens)[0]))

    def test_it_keeps_at_least_as_much_as_either(self):
        tokens = self._one_interval()
        both = len(captures(build(slots=4, decay_tag=0.9, window=3), tokens)[0])
        self.assertGreaterEqual(
            both, len(captures(build(slots=4, decay_tag=0.9), tokens)[0]))
        self.assertGreaterEqual(both, len(captures(build(window=3), tokens)[0]))

    def test_a_window_of_zero_alongside_a_tag_means_tag_only(self):
        """An ambiguity, pinned rather than papered over.

        `reward_window` 0 is a real one-write window on its own -- it keeps the
        write at the reward step. But 0 is also its DEFAULT, so a tag arm
        configured without touching it would silently become a combined gate,
        and every g9-05, g9-06 and g9-07 cell was measured with the tag alone.

        So a tag with `reward_window` 0 is tag-only, and the combined gate needs
        `reward_window` at least 1. The cost is that "tag plus a one-write
        window" cannot be expressed; the benefit is that the published arms stay
        reproducible and no default silently changes an arm's identity.
        """
        tokens = self._one_interval()
        self.assertEqual(captures(build(slots=4, decay_tag=0.9, window=0), tokens),
                         captures(build(slots=4, decay_tag=0.9), tokens))

    def test_one_is_the_smallest_window_the_combined_gate_can_use(self):
        tokens = self._one_interval()
        tag_only = captures(build(slots=4, decay_tag=0.9), tokens)[0]
        both = captures(build(slots=4, decay_tag=0.9, window=1), tokens)[0]
        self.assertEqual(both, sorted(set(tag_only)
                                      | set(captures(build(window=1), tokens)[0])))


class KeepingOnlyTheNewestMark(unittest.TestCase):
    """`tag_newest` narrows what a capture keeps to the most recent marks.

    Built to MEASURE a defect rather than to propose a mechanism: note 027 found
    the nearest binding before a reward is always the rewarded one, so "detect a
    binding, keep the most recent" would solve the task exactly. It turns out
    this project's binding-detection cannot reach it past delay 1 — but the dial
    is kept because that measurement is the reason the generator fix is not
    urgent, and it should be re-runnable.
    """

    def test_it_is_off_by_default(self):
        self.assertEqual(LocalMemoryConfig(vocab_size=VOCAB).tag_newest, 0)

    def test_it_is_refused_without_a_tag(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, reward_token=REWARD,
                              tag_newest=1)

    def test_it_is_refused_when_it_would_narrow_nothing(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, reward_token=REWARD,
                              tag_slots=4, tag_newest=8)

    def test_a_capture_keeps_at_most_that_many(self):
        tokens = stream(intervals=3)
        for newest in (1, 2):
            with self.subTest(newest=newest):
                sizes = capture_sizes(
                    build(slots=8, decay_tag=0.9, newest=newest), tokens)
                self.assertTrue(sizes)
                for _, kept in sizes:
                    self.assertLessEqual(kept, newest)

    def test_it_keeps_the_LATEST_marks_not_the_best(self):
        """The property, not the arithmetic: what survives is a suffix of what
        was marked, ordered by when it was written.

        **First capture only.** After one capture the two runs have protected
        different writes, so their stores differ, so their later marks differ --
        the same feedback the combined gate showed. Comparing a later interval
        across runs compares two different histories.

        The reward-step write is dropped from the expected set because the
        narrowed arm excludes it by design.
        """
        tokens = stream(intervals=2)
        reward_at = int(np.flatnonzero(tokens == REWARD)[0])
        all_marks = captures(build(slots=8, decay_tag=0.9), tokens)[0]
        kept = captures(build(slots=8, decay_tag=0.9, newest=2), tokens)[0]
        expected = [s for s in sorted(all_marks) if s != reward_at][-len(kept):]
        self.assertEqual(kept, expected,
                         "the surviving marks are not the latest of the marks, "
                         "so this is ranking rather than recency")

    def test_the_write_made_AT_the_reward_is_excluded(self):
        """A reward does not vouch for the write that carried it.

        The write at a capture step binds the previous token to the reward
        token. Note 027's rule is the most recent binding BEFORE the reward, and
        a write made at the reward is not before it. Without this the arm keeps
        that write every single time and measures nothing — which is what the
        first version did.
        """
        tokens = stream(intervals=3)
        rewards = [int(t) for t in np.flatnonzero(tokens == REWARD)]
        for kept, reward_at in zip(
                captures(build(slots=8, decay_tag=0.9, newest=1), tokens),
                rewards):
            self.assertNotIn(reward_at, kept,
                             "the capture kept the write made at the reward "
                             "step itself")


class TheMarkFades(unittest.TestCase):
    """Without this the tag ranks the whole interval at once, and the weakest
    retrievals it will ever see are the ones made when the store was smallest.

    That is not a hypothetical. Measured on `reward_recall`, an un-faded tag of
    four keeps the same 8 bindings out of 32 captures at every capacity and every
    delay -- because slots two through four go to the first writes after each
    capture, when the store is nearly empty and everything retrieves weakly.
    """

    def test_the_fade_changes_what_survives(self):
        """The connection test for the fade, and the one that matters most.

        The first version of `fade` multiplied every rank by the factor. For a
        tag admitting weak retrievals the ranks are negative, so that moved them
        toward zero -- toward the winning end -- and ENTRENCHED the marks it was
        meant to release. It produced numbers identical to no fade at all, at
        every setting from 0.99 down to 0.7, which is exactly the failure mode
        CLAUDE.md is written against: a dial that is read, applied, and doing
        nothing.
        """
        tokens = stream()
        sets = {str(captures(build(slots=4, decay_tag=f), tokens))
                for f in (1.0, 0.95, 0.9, 0.8)}
        self.assertGreater(
            len(sets), 1,
            "the fade changed nothing at any setting, so marks are not ageing")

    def test_fading_releases_the_oldest_marks(self):
        """The property, stated without reference to how it is computed: a fade
        moves what survives LATER in the interval, because old marks lose."""
        tokens = stream()
        without = captures(build(slots=4, decay_tag=1.0), tokens)[0]
        with_fade = captures(build(slots=4, decay_tag=0.9), tokens)[0]
        self.assertGreater(
            sum(with_fade) / len(with_fade), sum(without) / len(without),
            "fading marks did not shift the surviving set later, so age is not "
            "costing an incumbent anything")

    def test_a_rank_falls_whichever_end_is_winning(self):
        """`admit` keeps the largest rank, so fading means the rank FALLS.

        Which arithmetic does that depends on the sign, and getting it wrong is
        silent: one end fades and the other becomes immortal.
        """
        self.assertLess(fade(-2.0, 0.9), -2.0)      # weak-preferring: negative
        self.assertLess(fade(2.0, 0.9), 2.0)        # strong-preferring: positive

    def test_a_fade_of_one_is_the_identity(self):
        for rank in (-3.0, -0.5, 0.0, 0.5, 3.0):
            with self.subTest(rank=rank):
                self.assertEqual(fade(rank, 1.0), rank)


class StrengthIsRelativeToTheStore(unittest.TestCase):
    """Rank on how weak a retrieval is FOR THIS STORE, not on how small it is.

    A retrieval's magnitude scales with how much is in the store, so right after
    a capture -- when the store holds only what survived -- everything retrieves
    weakly. An absolute tag therefore reads the first writes of every interval as
    the most binding-like ones it will ever see, and fills with them.
    `tag_decay` hides that by ageing them out rather than fixing it, which is why
    the fade ends up doing two jobs and can be tuned for only one.
    """

    def test_it_changes_what_survives(self):
        """The connection test. A flag that is read, divided by, and changes
        nothing is the failure mode this repository is written against."""
        tokens = stream(intervals=2)
        self.assertNotEqual(captures(build(slots=4), tokens),
                            captures(build(slots=4, relative=True), tokens))

    def test_it_is_off_by_default(self):
        self.assertFalse(LocalMemoryConfig(vocab_size=VOCAB).tag_relative)

    def test_it_is_refused_without_a_tag(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, reward_token=REWARD,
                              tag_relative=True)

    def test_it_still_keeps_exactly_what_it_had_room_for(self):
        """The invariant must survive the change of ranking quantity. Dividing
        by a store norm cannot make the pool fill differently -- only fill with
        different writes."""
        rng = np.random.default_rng(2)
        tokens = np.array(_filler(60, rng) + [REWARD] + _filler(9, rng)
                          + [REWARD] + _filler(40, rng) + [REWARD])
        for slots in (2, 4):
            with self.subTest(slots=slots):
                for offered, kept in capture_sizes(
                        build(slots=slots, relative=True), tokens):
                    self.assertEqual(kept, min(slots, offered))

    def test_an_absolute_tag_marks_the_first_writes_of_every_interval(self):
        """The failure, measured. This is what the normalisation is for.

        A first attempt at a meaning test here asserted that a relative ranking
        survives scaling every stored value by a constant while an absolute one
        does not. **It was vacuous**: a constant rescale multiplies every
        retrieval by the same factor, and `admit` compares ranks, so neither
        ranking moves. Its guard caught it, which is the only reason it is not
        still there passing.

        The property that actually separates them is temporal. The store's size
        varies WITHIN a run -- it is smallest just after a capture -- so an
        absolute tag reads the opening writes of every interval as the weakest
        retrievals it will ever see. Measured on a three-interval stream, it
        marks writes at offsets 0.00, 0.01, 0.03 and 0.05 of the final interval.
        Not approximately the start: the start.
        """
        tokens = _intervals(3)
        absolute = _mean_offset(captures(build(slots=4), tokens)[-1], tokens)
        self.assertLess(
            absolute, 0.10,
            "the absolute tag did not cluster at the start of the interval, so "
            "this stream does not pose the problem tag_relative solves")

    def test_a_relative_tag_does_not(self):
        tokens = _intervals(3)
        absolute = _mean_offset(captures(build(slots=4), tokens)[-1], tokens)
        relative = _mean_offset(
            captures(build(slots=4, relative=True), tokens)[-1], tokens)
        self.assertGreater(
            relative, 2 * absolute,
            f"relative marks sat at {relative:.2f} of the way through the "
            f"interval against the absolute tag's {absolute:.2f}; dividing by "
            f"the store did not free the ranking from when the store was small")


class TheTagIsClearedByCapture(unittest.TestCase):
    """A mark is spent when the reward reads it.

    `tagged` holds indices into `pending`, and `pending` empties at every reward.
    A mark that outlived its capture would point into the NEXT interval's list
    and protect whichever writes landed at those positions -- a gate keeping the
    earliest writes after a reward, which nothing here argued for and which looks
    fine from outside.
    """

    def test_a_capture_keeps_as_many_writes_as_it_had_room_for(self):
        """The invariant a stale mark breaks, and the only one that catches it.

        The pool fills unconditionally while there is room, so after `n` writes
        it holds `min(n, slots)` -- every time, at every fade. A mark that
        outlived its capture holds a position from the PREVIOUS interval, and
        when the next interval is shorter that position does not exist, so the
        capture protects fewer writes than it had room for.

        **The obvious assertion misses this entirely.** Checking that a capture
        keeps steps inside its own interval passes while broken, because a stale
        index still lands inside the current interval whenever it is in range --
        it protects the WRONG write, not an out-of-range one. That version of
        this test was written first and `the-tag-outlives-its-capture` survived
        it in CI.

        **And the fade hides the bug**, which is why the long fade cases here are
        not sufficient on their own: a stale mark keeps ageing, so it is
        displaced within a few steps and the contamination flushes itself. With
        no fade the stale ranks are the near-zero ones from the cold start of the
        previous interval, which nothing displaces.
        """
        rng = np.random.default_rng(2)
        # Long, then SHORT: a short interval is what puts a stale position out
        # of range. Equal-length intervals hide it.
        tokens = np.array(_filler(60, rng) + [REWARD] + _filler(9, rng)
                          + [REWARD] + _filler(40, rng) + [REWARD])
        for slots in (2, 4):
            for decay_tag in (1.0, 0.99, 0.9):
                with self.subTest(slots=slots, fade=decay_tag):
                    sizes = capture_sizes(
                        build(slots=slots, decay_tag=decay_tag), tokens)
                    self.assertEqual(len(sizes), 3, "not every reward captured")
                    for offered, kept in sizes:
                        self.assertEqual(
                            kept, min(slots, offered),
                            f"an interval offering {offered} writes to a pool "
                            f"of {slots} kept {kept}; the pool fills while there "
                            f"is room, so anything else means it was holding "
                            f"positions from a previous interval")

    def test_every_capture_keeps_only_its_own_interval(self):
        tokens = stream(intervals=3)
        rewards = [int(t) for t in np.flatnonzero(tokens == REWARD)]
        kept = captures(build(slots=4, decay_tag=0.9), tokens)
        self.assertEqual(len(kept), len(rewards))
        start = 0
        for steps, reward_at in zip(kept, rewards):
            self.assertTrue(steps, "a capture kept nothing at all")
            self.assertTrue(
                all(start < step <= reward_at for step in steps),
                f"a capture at {reward_at} kept steps {steps}, which are not "
                f"all inside ({start}, {reward_at}] -- marks are surviving "
                f"their own capture")
            start = reward_at


class TheRankingPrimitiveIsShared(unittest.TestCase):
    """`tag` is `admit` with the sign flipped, and that is the finding.

    Competitive capture admits the strongest trace; g9-04 measured the signal as
    inverted. Rather than a second pool with the comparison the other way round
    -- two implementations of one behaviour, which drift -- the rank handed to
    `admit` is negated.
    """

    def test_the_weakest_candidates_win_a_full_tag(self):
        tagged: list = []
        for index, strength in enumerate([5.0, 4.0, 3.0, 2.0, 1.0]):
            tag(tagged, strength, index, capacity=2, strongest=False)
        self.assertEqual(sorted(index for _, index in tagged), [3, 4])

    def test_the_strongest_win_when_asked(self):
        tagged: list = []
        for index, strength in enumerate([5.0, 4.0, 3.0, 2.0, 1.0]):
            tag(tagged, strength, index, capacity=2, strongest=True)
        self.assertEqual(sorted(index for _, index in tagged), [0, 1])

    def test_a_losing_candidate_is_refused_rather_than_queued(self):
        tagged: list = []
        for index, strength in enumerate([1.0, 2.0, 3.0, 4.0]):
            tag(tagged, strength, index, capacity=2, strongest=False)
        self.assertEqual(sorted(index for _, index in tagged), [0, 1],
                         "a stronger retrieval displaced a weaker incumbent, so "
                         "the pool holds the most recent k, not the best k")

    def test_ties_go_to_the_incumbent(self):
        """Same rule as `admit`: a strictly-better test is what makes a run of
        equal candidates settle instead of churning."""
        tagged: list = []
        for index in range(4):
            tag(tagged, 2.0, index, capacity=2, strongest=False)
        self.assertEqual(sorted(index for _, index in tagged), [0, 1])

    def test_it_is_admit_underneath(self):
        """If these disagree there are two pools, and one will be fixed while
        the other keeps producing plausible numbers."""
        self.assertIsNone(admit([-1.0, -2.0], -3.0, 2))
        tagged = [(-1.0, 0), (-2.0, 1)]
        tag(tagged, 3.0, 9, capacity=2, strongest=False)
        self.assertNotIn(9, [index for _, index in tagged])


if __name__ == "__main__":
    unittest.main()
