"""Can each component do the job a working model requires of it?

Every other test here asks whether a mechanism behaves as SPECIFIED. These ask
something different and prior: **whether the component is capable of the job at
all**, with everything around it idealised.

The distinction matters because of how a wrong conclusion gets made. A weak
end-to-end number invites "that idea does not work", when the truth may be that
one component is broken and the idea was never tested. Note 012 records exactly
that shape — cap values from a reimplementation whose store never bound — and
g11-02 nearly repeated it: a mechanism that looked like a modest win on text was
destroying a second task, and only a deliberately negative prediction caught it.

**So each class below isolates one component, hands it perfect inputs, and asks
whether it can deliver what the model needs.** A failure localises the fault. A
pass says the component is not the reason for a weak result, which is the more
useful half.

## What a memory model of this shape needs, component by component

    keys        distinct tokens must be distinguishable, or nothing downstream
                can separate them
    store       a written binding must be retrievable -- the associative
                property itself
    readout     given CLEAN inputs it must learn an arbitrary mapping, or it is
                the bottleneck whatever the store does
    learning    the delta rule must actually reduce error
    end to end   a task it cannot fail must not be failed

Each is a floor, not a grade. Passing does not mean good.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 48, 64


def build(**overrides):
    config = dict(vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5,
                  decay=1.0, derived_keys=True, seed=11)
    config.update(overrides)
    model = LocalAssociativeMemory(LocalMemoryConfig(**config))
    model.wo[:] = model.wv
    return model


class KeysCanBeToldApart(unittest.TestCase):
    """If two tokens share a key, nothing downstream can separate them.

    This is the floor under everything: the store, the gate and the readout all
    assume a token's key identifies it.
    """

    def setUp(self):
        self.keys = np.array(build().wk)

    def test_no_two_tokens_share_a_key(self):
        overlap = self.keys @ self.keys.T
        np.fill_diagonal(overlap, 0.0)
        diagonal = float(np.mean(np.diag(self.keys @ self.keys.T)))
        self.assertLess(float(np.abs(overlap).max()), 0.8 * diagonal,
                        "two tokens have keys as similar as a key is to itself")

    def test_a_key_is_closest_to_ITSELF(self):
        """The property retrieval actually depends on, stated directly."""
        overlap = self.keys @ self.keys.T
        self.assertTrue(
            (overlap.argmax(axis=1) == np.arange(VOCAB)).all(),
            "some token's key is more like another token's than its own")


class TheStoreIsAssociative(unittest.TestCase):
    """Write a binding, query the cue, get the value. The core property.

    If this fails, no gating result means anything -- the thing being gated does
    not work.
    """

    def bind(self, pairs, corrective=False):
        model = build(corrective_writes=corrective)
        keys, values = np.array(model.wk), model.wv
        memory = np.zeros((WIDTH, WIDTH))
        for cue, item in pairs:
            if corrective:
                scale = float(keys[cue] @ keys[cue])
                memory += np.outer(values[item] - memory @ keys[cue],
                                   keys[cue]) / scale
            else:
                memory += np.outer(values[item], keys[cue])
        return model, memory, keys, values

    def test_one_binding_comes_back(self):
        model, memory, keys, values = self.bind([(3, 20)])
        self.assertEqual(int((values @ (memory @ keys[3])).argmax()), 20)

    def test_several_bindings_all_come_back(self):
        """Eight in a width-64 store is comfortably inside capacity, so a
        failure here is a bug rather than interference."""
        pairs = [(i, (i * 7 + 5) % VOCAB) for i in range(8)]
        model, memory, keys, values = self.bind(pairs)
        for cue, item in pairs:
            self.assertEqual(int((values @ (memory @ keys[cue])).argmax()), item,
                             f"cue {cue} did not return item {item}")

    def test_an_unwritten_cue_does_NOT_return_a_confident_answer(self):
        """The guard on the two above: a store that returned the same token for
        everything would pass them if that token happened to be right."""
        model, memory, keys, values = self.bind([(3, 20)])
        scores = values @ (memory @ keys[40])
        written = values @ (memory @ keys[3])
        self.assertLess(float(scores.max()), float(written.max()),
                        "an unwritten cue retrieves as strongly as a written "
                        "one, so the store is not content-addressed")


class TheReadoutCanLearnGivenCLEANInput(unittest.TestCase):
    """**The most diagnostic test here.**

    The readout is handed the exact value vectors rather than noisy retrievals,
    and asked to learn which token each one is. If it cannot do that, the
    readout is the bottleneck whatever the store does — and every conclusion
    that blamed the store would be wrong.

    g11-02 asked whether the readout was underpowered and answered only "not for
    want of a bias". This asks the prior question the right way round.
    """

    def train(self, epochs: int, lr: float = 0.05) -> float:
        model = build(lr=lr)
        values = np.array(model.wv)
        readout = np.zeros((VOCAB, WIDTH))
        rng = np.random.default_rng(2)
        order = np.arange(VOCAB)
        for _ in range(epochs):
            rng.shuffle(order)
            for token in order:
                clean = values[token]
                answer = readout @ clean
                target = np.zeros(VOCAB)
                target[token] = 1.0
                readout += lr * np.outer(target - answer, clean)
        right = sum(int((readout @ values[t]).argmax()) == t
                    for t in range(VOCAB))
        return right / VOCAB

    def test_it_learns_the_mapping_almost_perfectly(self):
        """Given clean inputs the delta rule should solve this outright: the
        value vectors are near-orthogonal and the task is linear."""
        self.assertGreater(self.train(epochs=200), 0.95)

    def test_it_is_UNTRAINED_at_zero_epochs(self):
        """The vacuity guard. A readout that scored well with no training would
        mean the test measures the value geometry, not the learning."""
        self.assertLess(self.train(epochs=0), 0.2)

    def test_more_training_helps(self):
        """Learning, not luck."""
        self.assertGreaterEqual(self.train(epochs=200), self.train(epochs=5))


class TheDeltaRuleReducesError(unittest.TestCase):
    """Learning must actually descend. A rule that does not is a rule that
    cannot be blamed OR credited for any result."""

    def test_error_falls_on_a_fixed_example(self):
        model = build()
        clean = np.array(model.wv[7])
        readout = np.zeros((VOCAB, WIDTH))
        target = np.zeros(VOCAB)
        target[7] = 1.0
        errors = []
        for _ in range(40):
            answer = readout @ clean
            errors.append(float(np.abs(target - answer).sum()))
            readout += 0.05 * np.outer(target - answer, clean)
        self.assertLess(errors[-1], errors[0])
        self.assertTrue(all(b <= a + 1e-9 for a, b in zip(errors, errors[1:])),
                        "error rose at some step, so the rule is not descending")


class TheWholeThingSolvesSomethingItCannotFail(unittest.TestCase):
    """A canary. The model must learn a sequence whose next token is fully
    determined by the current one, because that is precisely what binding the
    previous token to the current one stores.

    If this fails, the end-to-end path is broken and no task result means
    anything -- which is the situation John's question is about.
    """

    def test_a_deterministic_cycle_is_learned(self):
        cycle = np.tile(np.arange(6), 40).astype(np.int64)
        targets = np.concatenate([cycle[1:], cycle[-1:]])
        scored = np.ones(len(cycle), dtype=bool)
        scored[-1] = False
        model = build(lr=0.1, vocab_size=8)
        for _ in range(30):
            model.run(cycle, targets, scored, learn=True)
        predicted = model.run(cycle)
        # Skip position 0, which has no previous token to have bound anything.
        hits = sum(int(predicted[t] == targets[t]) for t in range(1, len(cycle) - 1))
        share = hits / (len(cycle) - 2)
        self.assertGreater(share, 0.9,
                           f"only {share:.0%} of a fully determined cycle was "
                           f"predicted; the end-to-end path is broken")


if __name__ == "__main__":
    unittest.main()
