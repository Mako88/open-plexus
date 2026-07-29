"""The index may propose neighbours where a HOP dead-ends, and only there.

ARCHITECTURE row E4, and John's option B. Note 044 refused `index_branches`
above one hop because a hop key "names no concept"; decision 154 measured that
false at cosine 0.96, so `argmax(weights)` names where the hop arrived and the
index can look it up.

**Two properties, and the second is the one John asked for.** Reaching through a
dead end is the point. Not branching when there is no dead end is what keeps the
cost off `b ** depth` -- three branches at three hops is 27 reads, which is the
wrong shape for C1 and would have made this mechanism unaffordable rather than
merely expensive.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.content import ContentIndex
from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

REL = 0
START, POINTER = 1, 2
#: `LONELY` is never written under `REL` -- the dead end. `SIBLING` is, and the
#: index is fitted so that the two are neighbours.
LONELY, SIBLING, ANSWER = 3, 4, 5
#: A context both of them sit beside. **Two tokens that only ever see each other
#: do NOT become similar** -- they become each other's CONTEXT, and their vectors
#: end up near-orthogonal. `families.py` documents the same trap: entities are
#: similar because they share ATTRIBUTES, and an entity sits BETWEEN two of its
#: own. The first version of this test paired LONELY and SIBLING directly and
#: `nearest` returned similarity 0.0.
SHARED = 6
VOCAB = 8


def model(index_at_hops: bool, content: ContentIndex | None
          ) -> LocalAssociativeMemory:
    built = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=64, lr=0.05, key_scale=0.5, decay=1.0,
        context_keys=True, derived_keys=True, hops=2, hop_relation=REL,
        track_occupancy=True, index_branches=2 if index_at_hops else 0,
        index_at_hops=index_at_hops, seed=0))
    built.wo[:] = built.wv
    built.content = content
    return built


def fitted() -> ContentIndex:
    """An index in which LONELY and SIBLING sit beside each other.

    Fitted by putting both BETWEEN the same context token, which is exactly
    `families.background`'s layout and for the same reason -- nothing here hands
    the index the grouping, it has to come out of co-occurrence.
    """
    index = ContentIndex(VOCAB, width=64, seed=0, power=0.0, window=1)
    for _ in range(40):
        index.observe(np.array([SHARED, LONELY, SHARED,
                                SHARED, SIBLING, SHARED]))
    return index


class ItReachesThroughADeadEnd(unittest.TestCase):

    #: `REL SIBLING ANSWER` writes `key(REL, SIBLING) -> ANSWER`. Nothing is
    #: ever written at `key(REL, LONELY)`. The cue lands the ordinary read on
    #: LONELY, so the hop from there dead-ends and the index is the only route
    #: to ANSWER.
    #: `REL ANSWER ANSWER` PINS THE ENDPOINT, and it is not a trick to make the
    #: test pass -- it isolates the row being measured. With `hops = 2` the
    #: chain takes a second hop after it arrives, and where it goes then is
    #: E3's question (knowing when to stop, which `halt_gate` learns) rather
    #: than E4's (fanning out at a dead end). Without the pin this test would
    #: fail for the halting reason and read as the fan-out not working.
    TOKENS = np.array([START, POINTER, LONELY,
                       REL, SIBLING, ANSWER,
                       REL, ANSWER, ANSWER,
                       START, POINTER])

    def test_without_the_index_the_hop_dead_ends(self):
        # THE CONTROL, and it has to come first: if the answer were reachable
        # without fanning out, the test below would prove nothing.
        predictions = model(False, None).run(self.TOKENS)
        self.assertNotEqual(int(predictions[-1]), ANSWER)

    def test_with_the_index_the_hop_reaches_the_sibling(self):
        predictions = model(True, fitted()).run(self.TOKENS)
        self.assertEqual(int(predictions[-1]), ANSWER)


class ItDoesNotBranchWhereThereIsNoDeadEnd(unittest.TestCase):
    """John's question: does this explode? Not if it only fires at dead ends."""

    #: Here the hop's own address IS written -- `REL LONELY ANSWER` -- so the
    #: fan-out must not fire at all.
    TOKENS = np.array([START, POINTER, LONELY,
                       REL, LONELY, ANSWER,
                       REL, ANSWER, ANSWER,
                       REL, SIBLING, LONELY,
                       START, POINTER])

    def reads(self, index_at_hops: bool) -> int:
        built = model(index_at_hops, fitted() if index_at_hops else None)
        counted = {"n": 0}
        inner = built.retrieval

        class Counting:
            def __getattr__(self, name):
                return getattr(inner, name)

            def read(self, memory, key):
                counted["n"] += 1
                return inner.read(memory, key)

        built.retrieval = Counting()
        built.run(self.TOKENS)
        return counted["n"]

    def test_the_fan_out_costs_dead_ends_and_not_depth(self):
        # THE COST CLAIM, MEASURED RATHER THAN ASSERTED.
        #
        # The first version asserted EXACTLY zero overhead and measured 29
        # against 28. The extra read is real and correct: at the start of a
        # sequence the store is empty, so the earliest position genuinely IS a
        # dead end. Asserting zero was the wrong claim, not a failing mechanism.
        #
        # The claim that matters is that cost tracks DEAD ENDS rather than
        # DEPTH. An ungated fan-out proposes `branches` candidates at every hop
        # of every position -- 2 * 2 * 14 = 56 extra reads here. The gated one
        # spends 1.
        gated, plain = self.reads(True), self.reads(False)
        ungated_bound = plain + 2 * 2 * len(self.TOKENS)
        self.assertLess(gated - plain, 2 * 2)
        self.assertLess(gated, ungated_bound / 2)


class ItRefusesWhatCannotWork(unittest.TestCase):

    def test_the_fan_out_needs_the_sketch(self):
        # "Holds nothing" is the sketch's question. Answering it by retrieval
        # norm is what decision 147 refuted, and an ungated fan-out is the
        # b ** depth cost this mechanism exists to avoid.
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, index_at_hops=True)

    def test_off_by_default(self):
        self.assertFalse(LocalMemoryConfig(vocab_size=VOCAB).index_at_hops)

    def test_the_old_refusal_still_stands_without_it(self):
        # Note 044's guard is relaxed ONLY for this mechanism. Everywhere else
        # an untyped hop with a fitted index is still refused.
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, index_branches=2, hops=2)


if __name__ == "__main__":
    unittest.main()
