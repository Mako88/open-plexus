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

import hashlib
import unittest

from openplexus.tasks.chains import (
    IGNORE,
    ChainConfig,
    ChainSequence,
    dataset,
    generate,
)


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
        laying chains end to end states a link at every boundary."""
        for hops in (1, 2, 3):
            config = ChainConfig(n_chains=4, hops=hops, n_symbols=40,
                                 seq_len=hops * 16 + 24, seed=3)
            for sequence in dataset(config, 25):
                symbols = {s for chain in sequence.chains for s in chain}
                false = {pair for pair in adjacent_pairs(sequence)
                         if pair[0] in symbols and pair[1] in symbols}
                false -= stated_links(sequence, hops)
                with self.subTest(hops=hops):
                    self.assertEqual(false, set())

    def test_a_hop_source_is_followed_by_exactly_one_token_ever(self):
        """**The hole in the test above, and the defect that hid in it.**

        That test exempts any pair involving a non-chain token, so a chain
        symbol binding to the SEPARATOR was invisible to it. Decision 84
        measured the consequence: with links stated one at a time, an
        intermediate symbol appeared twice — once as a target followed by the
        next separator, once as a source followed by its real successor — and a
        superposed store returned the sum. Retrieving with the exact key put the
        answer first only 54% of the time, against the separator at 39.5%.

        The property with no exemption: **a symbol that a hop must pass through
        is followed by exactly one distinct token in the entire presentation.**
        Not "one chain symbol" — one token, separators and filler included,
        because the store does not know which tokens are scaffolding.

        Chain-FINAL symbols are excluded: nothing hops out of them, so their
        binding to whatever follows competes with nothing.
        """
        for hops in (2, 3, 4):
            config = ChainConfig(n_chains=4, hops=hops, n_symbols=40,
                                 seq_len=hops * 16 + 24, seed=13)
            for sequence in dataset(config, 25):
                body = sequence.tokens[:sequence.answer_position - 1]
                successors: dict[int, set[int]] = {}
                for first, second in zip(body, body[1:]):
                    successors.setdefault(first, set()).add(second)
                for chain in sequence.chains:
                    for symbol in chain[:-1]:
                        with self.subTest(hops=hops, symbol=symbol):
                            self.assertEqual(len(successors.get(symbol, set())),
                                             1)

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


class SeveralTerminatorsAreAvailable(unittest.TestCase):
    """`n_separators > 1` exists to ask whether a model learns a CLASS of
    terminator or one specific token. Decision 89 measured the halting gate
    sitting +8.3 sd on a single value vector, which cannot transfer, and
    decision 93 traced that to `Wv` being frozen and random.
    """

    def test_one_separator_generates_exactly_what_it_always_did(self):
        """**A regression pin, not a description.**

        `rng.choice` consumes a draw even from a one-element sequence, so the
        obvious implementation would shift the random stream and silently change
        every sequence the single-separator task has ever produced — every
        number measured before this option existed would stop reproducing with
        nothing to show it had happened. This digest is that guarantee.
        """
        digest = hashlib.sha256()
        for hops in (1, 2, 3):
            for n_chains in (4, 6):
                config = ChainConfig(n_chains=n_chains, hops=hops,
                                     n_symbols=48, seq_len=96, seed=7)
                for sequence in dataset(config, 40):
                    digest.update(bytes(str(sequence.tokens), "utf8"))
                    digest.update(bytes(str(sequence.targets), "utf8"))
                    digest.update(bytes(str(sequence.asked), "utf8"))
        self.assertEqual(
            digest.hexdigest(),
            "a6d496e18fdd54798895d91673a19b3f522d137853186ec7962711be88aecc0a")

    def test_every_separator_in_the_pool_gets_used(self):
        config = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=64,
                             n_separators=4, seed=5)
        seen = set()
        for sequence in dataset(config, 40):
            seen |= set(sequence.tokens) & set(config.separator_tokens)
        self.assertEqual(seen, set(config.separator_tokens))

    def test_a_held_out_separator_never_appears(self):
        """The whole point of `use_separators`: train on some, test on one the
        model has never seen, with the vocabulary unchanged so the two models
        stay comparable."""
        trained = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=64,
                              n_separators=4, use_separators=(0, 1, 2), seed=5)
        held_out = trained.separator_tokens[0] + 3
        for sequence in dataset(trained, 40):
            self.assertNotIn(held_out, sequence.tokens)

    def test_holding_one_out_does_not_change_the_vocabulary(self):
        whole = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=64,
                            n_separators=4)
        part = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=64,
                           n_separators=4, use_separators=(0, 1, 2))
        self.assertEqual(whole.vocab_size, part.vocab_size)
        self.assertEqual(len(part.separator_tokens), 3)

    def test_separators_stay_outside_the_symbol_alphabet(self):
        config = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=64,
                             n_separators=5)
        for token in config.separator_tokens:
            self.assertGreaterEqual(token, config.n_symbols)
            self.assertNotEqual(token, config.query_token)
            self.assertLess(token, config.vocab_size)

    def test_no_false_link_with_several_separators(self):
        """The original defect, re-asserted on the new shape: more separators
        must not create an adjacency a chain does not contain."""
        config = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=64,
                             n_separators=4, seed=17)
        for sequence in dataset(config, 25):
            symbols = {s for chain in sequence.chains for s in chain}
            false = {pair for pair in adjacent_pairs(sequence)
                     if pair[0] in symbols and pair[1] in symbols}
            self.assertEqual(false - stated_links(sequence, 2), set())

    def test_every_question_is_scored(self):
        """With several questions the sequence must score all of them —
        scoring only the last would make `n_queries` raise the *difficulty*
        without raising the density of composition in the training signal,
        which is the entire point of the dial (decision 97)."""
        config = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=96,
                             n_queries=4, seed=21)
        for sequence in dataset(config, 15):
            scored = [t for t in sequence.targets if t != IGNORE]
            self.assertEqual(len(scored), 4)
            for position, chain in sequence.queries:
                self.assertEqual(sequence.targets[position], chain[-1])

    def test_the_last_question_is_still_the_reported_one(self):
        """`asked` and `answer_position` predate `queries` and must keep
        meaning what they meant, or every caller written for the
        single-question task silently reads the wrong block."""
        config = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=96,
                             n_queries=4, seed=22)
        sequence = generate(config)
        position, chain = sequence.queries[-1]
        self.assertEqual(sequence.answer_position, position)
        self.assertEqual(sequence.asked, chain)

    def test_no_answer_is_stated_before_its_own_question(self):
        """**The defect several questions introduced, and the reason they are
        drawn without replacement.**

        A block writes `a` next to `c`, so it STATES the link `a -> c`. With one
        question that is harmless — the block is last, and the answer is read
        before the binding is written. With several, an early block would state
        the answer to a chain a later block asks about, and the later question
        would be a ONE-HOP LOOKUP of a link already in the store. The whole hop
        axis would be measuring nothing, exactly as if `a -> c` had been written
        into the body.

        Each block's own `(a, c)` is not a leak and is not counted — that pair
        IS the task. What must never happen is the pair appearing *before* the
        question that needs it.
        """
        for n_queries in (2, 3, 4):
            config = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=96,
                                 n_queries=n_queries, seed=23)
            for sequence in dataset(config, 15):
                for position, chain in sequence.queries:
                    before = sequence.tokens[:position]
                    stated = {(before[i], before[i + 1])
                              for i in range(len(before) - 1)}
                    with self.subTest(n_queries=n_queries, chain=chain):
                        self.assertNotIn((chain[0], chain[-1]), stated)

    def test_no_chain_is_asked_twice(self):
        """What makes the guard above hold, asserted directly so a future change
        to the sampling cannot quietly reintroduce the leak."""
        config = ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=96,
                             n_queries=4, seed=24)
        for sequence in dataset(config, 20):
            asked = [chain for _, chain in sequence.queries]
            self.assertEqual(len(set(asked)), len(asked))

    def test_more_questions_than_chains_is_refused(self):
        with self.assertRaises(ValueError):
            ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=96,
                        n_queries=5)

    def test_no_questions_is_refused(self):
        with self.assertRaises(ValueError):
            ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=96,
                        n_queries=0)

    def test_an_empty_use_separators_is_refused(self):
        with self.assertRaises(ValueError):
            ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=64,
                        n_separators=3, use_separators=())

    def test_a_separator_index_outside_the_pool_is_refused(self):
        with self.assertRaises(ValueError):
            ChainConfig(n_chains=4, hops=2, n_symbols=40, seq_len=64,
                        n_separators=2, use_separators=(0, 5))


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
