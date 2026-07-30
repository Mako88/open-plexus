"""How often the beam has to meet itself, which is a distribution question.

Ranking all `width` partial walks against each other is a **rendezvous**. Note 101
measured that meeting as the second of a hop's two round trips — the one that keeps a
driver-free walk outside `d_max` past depth 7 — and named it as the obstacle to a walk
that migrates peer to peer instead of returning to the caller.

`prune_every` makes the period explicit:

    1    meet every hop. What every number before note 102 was taken under
    k    meet every k hops. Between them each walk keeps its own top `branches`, so the
         population grows by `branches` per unpruned hop
    0    never meet. `width` independent greedy walks

`tools/prune_period.py` is the measurement; this file pins the mechanics the measurement
would otherwise be free to misread — that the default is unchanged, that a period really
does grow the population, and that `0` really does stop pruning rather than quietly
pruning anyway.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.keys import PairKeys
from openplexus.retrieval import SuperposedRead
from openplexus.search import beam

WIDTH, FACT = 32, 0
#: A graph where every entity has several outgoing relations, so pruning has something
#: to discard. Out-degree 1 would make every period identical and the whole file vacuous.
ENTITIES, RELATIONS = 9, 4


def fixture(seed: int = 0):
    vocab = 1 + ENTITIES + RELATIONS
    rng = np.random.default_rng(seed)
    values = rng.normal(0.0, 1.0, (vocab, WIDTH))
    values /= np.linalg.norm(values, axis=1, keepdims=True)
    keys = PairKeys(seed=1, spread=1.0 / np.sqrt(WIDTH), width=WIDTH, start=vocab,
                    route="first-concept", markers=frozenset({FACT}))
    matrix = np.zeros((WIDTH, WIDTH))
    for entity in range(1, ENTITIES):
        for offset in range(RELATIONS):
            relation = 1 + ENTITIES + offset
            landed = 1 + (entity + offset) % (ENTITIES - 1)
            for previous, token, value in ((FACT, entity, relation),
                                           (entity, relation, landed)):
                matrix += np.outer(values[value], keys.pair(previous, token))
    allowed = np.arange(1 + ENTITIES, 1 + ENTITIES + RELATIONS)
    return matrix, keys, values, allowed


class Counting:
    """Counts reads and rounds. The two quantities note 100 is about, kept apart."""

    def __init__(self, matrix, keys):
        self.matrix, self.keys = matrix, keys
        self.retrieval = SuperposedRead()
        self.rounds = self.reads = self.widest = 0
        self.many = self._many

    def __call__(self, previous, token):
        return self._many([(previous, token)])[0]

    def _many(self, pairs):
        self.rounds += 1
        self.reads += len(pairs)
        self.widest = max(self.widest, len(pairs))
        return [self.retrieval.read(self.matrix, self.keys.pair(previous, token))
                for previous, token in pairs]


class ThePrunePeriodChangesWhatItSaysItChanges(unittest.TestCase):

    DEPTH, BEAM, BRANCHES = 5, 4, 4

    def setUp(self):
        self.matrix, self.keys, self.values, self.allowed = fixture()

    def _run(self, period, reader=None):
        return beam(self.matrix if reader is None else None, SuperposedRead(),
                    self.keys, self.values, FACT, 1, self.values[3], self.DEPTH,
                    width=self.BEAM, branches=self.BRANCHES, allowed=self.allowed,
                    prune_every=period, reader=reader)

    def _counted(self, period):
        counting = Counting(self.matrix, self.keys)
        self._run(period, reader=counting)
        return counting

    def test_period_one_is_the_DEFAULT_and_is_unchanged(self):
        """Every number in the tree was taken at the default, so it must not move."""
        explicit = self._run(1)
        implicit = beam(self.matrix, SuperposedRead(), self.keys, self.values, FACT,
                        1, self.values[3], self.DEPTH, width=self.BEAM,
                        branches=self.BRANCHES, allowed=self.allowed)
        self.assertEqual([w.relations for w in explicit],
                         [w.relations for w in implicit])

    def test_a_LONGER_period_reads_MORE(self):
        """The cost of not meeting, which is the column that stops this being free.

        Asserted as a direction rather than a ratio: the exact factor depends on how
        many candidates `_top` finds at each entity, and pinning it would make this a
        test about the fixture.
        """
        self.assertGreater(self._counted(2).reads, self._counted(1).reads)
        self.assertGreater(self._counted(3).reads, self._counted(2).reads)

    def test_a_longer_period_carries_a_WIDER_population(self):
        """Where the extra reads come from: an unpruned hop has more walks in it."""
        self.assertGreater(self._counted(3).widest, self._counted(1).widest)

    def test_NEVER_pruning_keeps_the_population_FLAT(self):
        """`0` is one child per parent, so it must not blow up like an unpruned beam.

        This is the assertion that catches `prune_every=0` being read as "skip the
        truncation" while still expanding by `branches` -- which is `branches**depth`
        walks and would hang rather than fail.
        """
        never = self._counted(0)
        self.assertLessEqual(never.widest, self.BEAM)
        self.assertEqual(never.reads, self._counted(1).reads,
                         "never pruning read a different amount than pruning every "
                         "hop, so the population is not one child per parent")

    def test_NEVER_pruning_gives_a_DIFFERENT_answer_than_meeting(self):
        """Otherwise the rendezvous does nothing here and the measurement is vacuous.

        `tools/prune_period.py` measures the size of the difference on CLUTRR -- 0.8770
        against 0.8037. This only asserts that a difference exists in the fixture, so
        the knob is not inert.
        """
        self.assertNotEqual([w.relations for w in self._run(0)],
                            [w.relations for w in self._run(1)])

    def test_the_ROUND_count_is_two_per_hop_at_every_period(self):
        """Periods trade reads for meetings, and change neither read's dependency.

        A hop is still follow-then-look-up whatever the period, so the round count this
        harness sees does not move -- the saving is in a MIGRATING walk, which is not
        built. Pinned so the arithmetic in note 102 is not read as already achieved.
        """
        for period in (0, 1, 2, 3):
            self.assertEqual(self._counted(period).rounds, 2 * self.DEPTH,
                             f"period {period} changed the round count, so the "
                             f"follow/look-up dependency is not what note 101 says")

    def test_a_NEGATIVE_period_is_refused(self):
        with self.assertRaises(ValueError) as raised:
            self._run(-1)
        self.assertIn("prune_every", str(raised.exception))


if __name__ == "__main__":
    unittest.main()
