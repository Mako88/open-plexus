"""The relational-chain task, and the two ways it could be silently broken.

A task can fail in two directions and both produce numbers. It can be
**impossible**, in which case every arm sits at chance and chance looks like a
hard problem. Or it can be **already solved** by a shortcut, in which case every
arm scores well and nothing was measured. Note 006 records this project choosing
a benchmark that turned out to be already solved; the MQAR filler bug is the
other direction, and made the task impossible while looking merely difficult.

So the assertions here are about the DATA, generated and inspected, not about
the generator's intentions.

## The defect this file was written after

Links were first laid down as bare adjacent pairs — `a b c d` — and **this store
binds every adjacent pair**, so the sequence also stated `b -> c`, a link no
chain contains. With one boundary per link there were as many false links as
true ones. Found by generating one sequence and reading it, before any test
existed, which is exactly how the MQAR bug was found.

`test_no_false_chain_link_is_ever_stated` is the guard, and it fails on the
pre-separator design.
"""

from __future__ import annotations

import unittest

from openplexus.tasks.chains import ChainConfig, ChainSequence, dataset, generate


def stated_links(sequence: ChainSequence, hops: int) -> set[tuple[int, int]]:
    """The links the chains actually contain."""
    return {(chain[i], chain[i + 1])
            for chain in sequence.chains for i in range(hops)}


def adjacent_pairs(sequence: ChainSequence) -> set[tuple[int, int]]:
    """Every adjacent pair in the presentation, excluding the query block.

    The query block is excluded because the answer follows the question there BY
    CONSTRUCTION — that is the task, not a leak, and counting it would make
    every shortcut check fail.
    """
    body = sequence.tokens[:sequence.answer_position - 1]
    return {(body[i], body[i + 1]) for i in range(len(body) - 1)}


class TheTaskIsNotSecretlySolvable(unittest.TestCase):

    def test_the_answer_is_never_stated_beside_the_question(self):
        """The shortcut that would make a one-hop lookup win. If `a -> c` were
        ever adjacent, a model could score without composing and the whole hop
        axis would measure nothing."""
        for hops in (2, 3, 4):
            config = ChainConfig(n_chains=4, hops=hops, n_symbols=40,
                                 seq_len=hops * 16 + 16, seed=7)
            for sequence in dataset(config, 25):
                with self.subTest(hops=hops, seed=sequence.asked):
                    self.assertNotIn((sequence.asked[0], sequence.asked[-1]),
                                     adjacent_pairs(sequence))

    def test_no_false_chain_link_is_ever_stated(self):
        """**The defect this file exists for.** Every adjacent pair of two chain
        symbols must be a link some chain contains. Without the separator,
        laying links end to end states a link at every boundary."""
        for hops in (1, 2, 3):
            config = ChainConfig(n_chains=4, hops=hops, n_symbols=40,
                                 seq_len=hops * 16 + 16, seed=3)
            for sequence in dataset(config, 25):
                symbols = {s for chain in sequence.chains for s in chain}
                false = {pair for pair in adjacent_pairs(sequence)
                         if pair[0] in symbols and pair[1] in symbols}
                false -= stated_links(sequence, hops)
                with self.subTest(hops=hops):
                    self.assertEqual(false, set())

    def test_filler_never_uses_a_chain_symbol(self):
        """The MQAR filler bug exactly: filler drawn from the whole alphabet
        could state a link that does not exist, or answer a query it should
        not."""
        config = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=80,
                             seed=11)
        for sequence in dataset(config, 25):
            symbols = {s for chain in sequence.chains for s in chain}
            # +2, not +1: position `answer_position + 1` holds the ANSWER,
            # which is a chain symbol by definition. Filler starts after it.
            tail = sequence.tokens[sequence.answer_position + 2:]
            self.assertEqual(set(tail) & symbols, set())


class TheTaskIsPossible(unittest.TestCase):

    def test_every_link_needed_to_answer_is_present(self):
        """The other failure direction. If a link in the asked chain were
        missing, no model could answer and every cell would sit at chance --
        which is indistinguishable from the task being hard."""
        for hops in (1, 2, 3, 4):
            config = ChainConfig(n_chains=4, hops=hops, n_symbols=40,
                                 seq_len=hops * 16 + 16, seed=5)
            for sequence in dataset(config, 25):
                pairs = adjacent_pairs(sequence)
                for step in range(hops):
                    with self.subTest(hops=hops, step=step):
                        self.assertIn(
                            (sequence.asked[step], sequence.asked[step + 1]),
                            pairs)

    def test_a_symbol_never_has_two_successors(self):
        """Chains share no symbols, so following a link is unambiguous. If a
        symbol had two successors the answer would not be determined and the
        task would be unanswerable rather than hard."""
        config = ChainConfig(n_chains=6, hops=3, n_symbols=48, seq_len=96,
                             seed=2)
        for sequence in dataset(config, 20):
            successors: dict[int, set[int]] = {}
            for source, target in stated_links(sequence, 3):
                successors.setdefault(source, set()).add(target)
            self.assertTrue(all(len(v) == 1 for v in successors.values()))


class TheShapeIsWhatItClaims(unittest.TestCase):

    def test_one_hop_asks_for_a_directly_stated_link(self):
        """1 hop is the positive control: plain cued recall, which this model
        demonstrably does. If it fails there the implementation is broken rather
        than the model, and a zero at 2 hops is unreadable."""
        config = ChainConfig(n_chains=4, hops=1, n_symbols=40, seq_len=32,
                             seed=9)
        sequence = generate(config)
        self.assertIn((sequence.asked[0], sequence.asked[-1]),
                      adjacent_pairs(sequence))

    def test_exactly_one_position_is_scored(self):
        config = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=64,
                             seed=4)
        sequence = generate(config)
        scored = [t for t in sequence.targets if t != -1]
        self.assertEqual(len(scored), 1)
        self.assertEqual(scored[0], sequence.asked[-1])

    def test_the_floor_is_the_strongest_trivial_strategy(self):
        """Not 1/vocab. A model that learned only which symbols end chains beats
        uniform without composing, so the floor must be that strategy."""
        self.assertEqual(ChainConfig(n_chains=8).trivial_floor, 1 / 8)

    def test_markers_are_outside_the_symbol_alphabet(self):
        config = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=64)
        self.assertGreaterEqual(config.query_token, config.n_symbols)
        self.assertGreaterEqual(config.separator_token, config.n_symbols)
        self.assertNotEqual(config.query_token, config.separator_token)


class ImpossibleShapesAreRefused(unittest.TestCase):

    def test_too_few_symbols_for_the_chains(self):
        with self.assertRaises(ValueError):
            ChainConfig(n_chains=8, hops=4, n_symbols=10, seq_len=200)

    def test_a_single_chain_is_refused(self):
        """With one chain the answer is the only chain-ending symbol and
        guessing solves it."""
        with self.assertRaises(ValueError):
            ChainConfig(n_chains=1, hops=2, n_symbols=40, seq_len=64)

    def test_zero_hops_is_refused(self):
        with self.assertRaises(ValueError):
            ChainConfig(n_chains=4, hops=0, n_symbols=40, seq_len=64)

    def test_a_sequence_too_short_to_hold_the_links(self):
        with self.assertRaises(ValueError):
            ChainConfig(n_chains=8, hops=3, n_symbols=48, seq_len=20)


if __name__ == "__main__":
    unittest.main()
