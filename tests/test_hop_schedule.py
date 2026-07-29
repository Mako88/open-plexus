"""A walk must be able to follow LINK then FACT, not the same relation twice.

Decision 162. `hop_relation` is one value per MODEL, so a two-hop walk follows
LINK-then-LINK or FACT-then-FACT and never LINK-then-FACT -- and LINK-then-FACT
is the path the linked-families task needs:

    key(FACT, entity)   empty -- the gate fires, correctly
    key(LINK, rep)      -> the linked family's representative   hop 1, LINK
    key(FACT, rep')     -> that family's value                  hop 2, FACT

162 named this rather than building it, because *which* relation at *which* depth
is a schedule, and a schedule the task does not supply is a fitted constant. That
objection is about what the mechanism may be used to CLAIM; it is not a reason to
leave the mechanism unable to do its one job. These tests are about the job.

## Why the sequence has a THIRD family in it

The first version of this file asserted that the wrong walks do NOT reach
`LINKED_VALUE`, and one of them reached it anyway -- `hop_relation=FACT` starts
at an empty address, so its answer is whatever noise decodes to, and on seed 0
that noise decoded to exactly the right token. **A test whose passing condition
is a noise draw is not a test**, and it would have passed or failed by seed.

So the layout gives every walk under test a *determinate* destination:
`key(LINK, OTHER)` is written too, which means LINK-then-LINK arrives at `THIRD`
rather than at nothing. The discriminating comparison is then positive on both
sides -- same first hop, different second hop, two different named tokens --
and it is stable across seeds 0, 1 and 2.

`hop_relation=FACT` is deliberately **not** asserted on. Its walk begins at an
address nothing was written to, which is the case decision 162 describes as the
gate firing correctly; its answer is noise and asserting on noise is what this
file was rewritten to stop.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

#: The two relations the walk has to alternate between. `LINK` points at another
#: family's representative; `FACT` reads a representative's value.
LINK, FACT = 0, 1
#: Three families, so that following LINK twice ARRIVES somewhere nameable
#: instead of running off an empty address -- see the module docstring.
REP, OTHER, THIRD = 2, 3, 4
LINKED_VALUE, THIRD_VALUE = 5, 6
#: A cue whose ordinary read lands ON `REP`, so both hops have to do the work.
#: Without it the first read answers and the schedule is inert -- the defect
#: decision 158's test caught and this one inherits.
START, POINTER = 7, 8
VOCAB = 10

#: `RELATION subject object`, which is `closure.py`'s ordering reason in
#: miniature: with `context_keys` the store binds the previous position's key to
#: this position's value, so `R S O` writes `key(R, S) -> O`.
TOKENS = np.array([START, POINTER, REP,
                   LINK, REP, OTHER,            # key(LINK, REP)   -> OTHER
                   LINK, OTHER, THIRD,          # key(LINK, OTHER) -> THIRD
                   FACT, OTHER, LINKED_VALUE,   # key(FACT, OTHER) -> LINKED_VALUE
                   FACT, THIRD, THIRD_VALUE,    # key(FACT, THIRD) -> THIRD_VALUE
                   START, POINTER])

#: `hops=3` forms two keys plus one lookahead, hence three entries. The third is
#: never the answer's source and is present because the halting gate scores hop k
#: by what hop k + 1 returns.
LINK_THEN_FACT = (LINK, FACT, FACT)
LINK_THEN_LINK = (LINK, LINK, LINK)


def model(**over) -> LocalAssociativeMemory:
    settings = dict(
        vocab_size=VOCAB, d_model=64, lr=0.05, key_scale=0.5, decay=1.0,
        context_keys=True, derived_keys=True, hops=3, seed=0)
    settings.update(over)
    built = LocalAssociativeMemory(LocalMemoryConfig(**settings))
    # The readout is the value matrix, so a retrieval decodes to the token it
    # stored rather than to whatever an untrained readout prefers. Every hop test
    # in this project does this -- see `tests/test_hops.py`.
    built.wo[:] = built.wv
    return built


def answer(**over) -> int:
    return int(model(**over).run(TOKENS)[-1])


class AScheduleFollowsADifferentRelationAtEachDepth(unittest.TestCase):
    """The one job: hop 1 follows LINK, hop 2 follows FACT."""

    def test_LINK_then_FACT_reaches_the_linked_family_value(self):
        # The ordinary read at the repeated cue lands on REP. Hop 1 typed LINK
        # reads key(LINK, REP) -> OTHER. Hop 2 typed FACT reads
        # key(FACT, OTHER) -> LINKED_VALUE. This is decision 162's path, and it
        # is the first time this project has walked two DIFFERENT relations.
        self.assertEqual(answer(hop_relations=LINK_THEN_FACT), LINKED_VALUE)

    def test_LINK_then_LINK_reaches_the_third_family_instead(self):
        # THE SAME SEQUENCE, THE SAME POSITION, THE SAME FIRST HOP, A DIFFERENT
        # ANSWER -- and a determinate one rather than noise, which is what the
        # third family in the layout is for.
        self.assertEqual(answer(hop_relations=LINK_THEN_LINK), THIRD)

    def test_the_two_schedules_disagree(self):
        # Stated separately, because a readout returning one token regardless of
        # the walk would make both tests above pass for the wrong reason and look
        # identical to them passing for the right one. Rule 10's companion.
        self.assertNotEqual(answer(hop_relations=LINK_THEN_FACT),
                            answer(hop_relations=LINK_THEN_LINK))

    def test_one_relation_at_every_depth_cannot_reach_it(self):
        # DECISION 162's CLAIM, AS A TEST RATHER THAN AN ARGUMENT. `hop_relation`
        # is the pre-162 mechanism at the best setting available to it for this
        # task: LINK gets the walk to the linked family's representative and
        # then cannot read its value, because reading it needs FACT.
        #
        # So this fails if the schedule ever stops being necessary, which is the
        # honest way to hold the claim -- not by asserting the old mechanism
        # produces nothing, but by naming exactly where it stops.
        self.assertEqual(answer(hop_relation=LINK), THIRD)
        self.assertNotEqual(answer(hop_relation=LINK), LINKED_VALUE)

    def test_a_uniform_schedule_reproduces_the_single_relation(self):
        # THE REPRODUCTION GATE, in miniature. A schedule of one relation
        # repeated must give byte-identical answers to the pre-162 field, or
        # every number measured before 2026-07-29 stops being comparable to
        # anything measured after it -- CLAUDE.md's rule about a load-bearing
        # component changing under a comparison set.
        np.testing.assert_array_equal(
            model(hop_relations=LINK_THEN_LINK).run(TOKENS),
            model(hop_relation=LINK).run(TOKENS))


class ItRefusesWhatCannotWork(unittest.TestCase):

    def test_off_by_default(self):
        self.assertEqual(LocalMemoryConfig(vocab_size=VOCAB).hop_relations, ())

    def test_both_fields_at_once_is_refused(self):
        # Two answers to "which relation does hop i follow". Silently preferring
        # one would leave the other looking connected while doing nothing, which
        # is the failure mode CLAUDE.md's standard is built against.
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, context_keys=True, hops=2,
                              hop_relation=LINK, hop_relations=(LINK, FACT))

    def test_a_schedule_without_pair_keys_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, hops=2,
                              hop_relations=(LINK, FACT))

    def test_a_schedule_shorter_than_the_walk_is_refused(self):
        # A fallback here would be invisible: reusing the last entry turns
        # LINK-then-FACT into LINK-then-FACT-then-FACT, and every rule for
        # extending a short schedule changes which relations get followed
        # without saying so.
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, context_keys=True, hops=3,
                              hop_relations=(LINK, FACT))

    def test_a_negative_entry_is_refused(self):
        # One untyped hop inside a typed walk queries the single-token key space
        # the store never writes to under context_keys -- cosine -0.069 -- and
        # returns noise while still producing a number.
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, context_keys=True, hops=2,
                              hop_relations=(LINK, -1))


class TheScheduleIsWhatTheWalkActuallyConsults(unittest.TestCase):
    """Rule 6: the connection test, at the seam rather than end to end."""

    def test_the_helper_returns_the_entry_for_that_depth(self):
        built = model(hop_relations=LINK_THEN_FACT)
        self.assertEqual(built._relation_at(0), LINK)
        self.assertEqual(built._relation_at(1), FACT)

    def test_the_helper_falls_through_to_the_single_relation(self):
        # The pre-162 path has to keep working through the same helper, or the
        # two cases drift apart -- rule 9's duplicated-logic hazard, where a fix
        # lands in one copy and the other keeps producing plausible numbers.
        built = model(hops=2, hop_relation=FACT)
        self.assertEqual(built._relation_at(0), FACT)
        self.assertEqual(built._relation_at(1), FACT)

    def test_an_untyped_model_reports_no_relation(self):
        built = model(hops=1, context_keys=False, derived_keys=False)
        self.assertEqual(built._relation_at(0), -1)


if __name__ == "__main__":
    unittest.main()
