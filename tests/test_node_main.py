"""The launchable node: does a process started from nothing but env vars join?

`serve` and `Network` are already tested. What is new here is the *entrypoint* --
the part that reads an environment, decides what it can afford, rebuilds the model
from a shared seed and joins. The failure modes it introduces are configuration
failures, and they are silent: a node with the wrong seed still runs, still votes,
and still produces a number.

So the load-bearing test is that a network assembled from entrypoints agrees with
the single-process model exactly. That is the only check that notices a node
which is working perfectly on the wrong arrays.
"""

from __future__ import annotations

import threading
import time
import unittest
from unittest import mock

import numpy as np

from openplexus import node_main
from openplexus.distributed import Network
from openplexus.models.local_memory import LocalAssociativeMemory

VOCAB, WIDTH, NODES = 24, 16, 4
TOKENS = np.random.default_rng(3).integers(0, VOCAB, 40)


def environment(port: int, index: int, **extra) -> dict:
    return {
        node_main.DRIVER_HOST_VAR: "127.0.0.1",
        node_main.DRIVER_PORT_VAR: str(port),
        node_main.NODE_INDEX_VAR: str(index),
        node_main.NODES_VAR: str(NODES),
        node_main.D_MODEL_VAR: str(WIDTH),
        node_main.VOCAB_VAR: str(VOCAB),
        node_main.SEED_VAR: "5",
        node_main.DECODER_VAR: "1",
        **extra,
    }


class AnEntrypointNetworkMatchesOneProcess(unittest.TestCase):
    """The check that notices a node working perfectly on the wrong arrays."""

    def test_four_launched_nodes_reproduce_the_single_process_answer(self):
        with mock.patch.dict("os.environ", environment(0, 0), clear=False):
            config = node_main.config_from_env()
        reference = LocalAssociativeMemory(config)
        reference.wo[:] = reference.wv
        expected = reference.run(TOKENS)

        net = Network(config, NODES, reference.wv, reference.wo, spawn=False)
        box: dict = {}

        def driver():
            try:
                with net:
                    box["result"] = net.run(TOKENS)
            except BaseException as error:
                box["error"] = error

        thread = threading.Thread(target=driver, daemon=True)
        thread.start()
        while net.port == 0 and "error" not in box:
            time.sleep(0.01)

        workers = []
        for index in range(NODES):
            def launch(i=index):
                with mock.patch.dict("os.environ",
                                     environment(net.port, i), clear=False):
                    node_main.main()
            worker = threading.Thread(target=launch, daemon=True)
            worker.start()
            workers.append(worker)

        thread.join(timeout=30)
        for worker in workers:
            worker.join(timeout=5)
        if "error" in box:
            raise box["error"]
        np.testing.assert_array_equal(
            box["result"], expected,
            "a network of launched nodes disagreed with the single-process "
            "model, so at least one node is computing on different arrays")


class TheEnvironmentReachesTheConfig(unittest.TestCase):
    """Asserted against literals, deliberately.

    The agreement test above cannot catch a field that ignores its environment
    variable, because it builds its own reference with the same function -- so a
    hardcoded value lands on both sides and they agree on the wrong number. The
    mutation harness found exactly that by hardcoding the vocabulary, and the
    answer is a direct assertion rather than a cleverer end-to-end one.

    A node with the wrong vocabulary still runs, still votes and still produces a
    number, which is why this is worth pinning field by field.
    """

    def test_vocabulary(self):
        with mock.patch.dict("os.environ",
                             environment(1, 0, **{node_main.VOCAB_VAR: "31"}),
                             clear=False):
            self.assertEqual(node_main.config_from_env().vocab_size, 31)

    def test_width(self):
        with mock.patch.dict("os.environ",
                             environment(1, 0, **{node_main.D_MODEL_VAR: "12"}),
                             clear=False):
            self.assertEqual(node_main.config_from_env().d_model, 12)

    def test_seed(self):
        with mock.patch.dict("os.environ",
                             environment(1, 0, **{node_main.SEED_VAR: "9"}),
                             clear=False):
            self.assertEqual(node_main.config_from_env().seed, 9)

    def test_the_defaults_are_not_what_the_tests_set(self):
        """Otherwise the three tests above could pass on the defaults."""
        with mock.patch.dict("os.environ", {}, clear=True):
            config = node_main.config_from_env()
        self.assertNotEqual(config.vocab_size, 31)
        self.assertNotEqual(config.d_model, 12)
        self.assertNotEqual(config.seed, 9)


class TheDecoderSwitchIsLoadBearing(unittest.TestCase):
    """Without it every node is interchangeable and the testbed measures nothing.

    `wo` is learned and starts at zeros, so an untrained model scores every token
    zero and predicts token 0 forever. A latency experiment run against that
    would produce clean, meaningless curves.
    """

    def test_without_the_decoder_the_model_says_one_thing_forever(self):
        with mock.patch.dict("os.environ",
                             environment(1, 0, **{node_main.DECODER_VAR: "0"}),
                             clear=False):
            config = node_main.config_from_env()
        model = LocalAssociativeMemory(config)
        self.assertEqual(len(set(model.run(TOKENS).tolist())), 1)

    def test_with_the_decoder_it_does_not(self):
        with mock.patch.dict("os.environ", environment(1, 0), clear=False):
            config = node_main.config_from_env()
        model = LocalAssociativeMemory(config)
        model.wo[:] = model.wv
        self.assertGreater(len(set(model.run(TOKENS).tolist())), 1)


class ItRefusesToStartWithNowhereToGo(unittest.TestCase):

    def test_no_driver_port_is_an_error_not_a_default(self):
        with mock.patch.dict("os.environ",
                             {node_main.DRIVER_PORT_VAR: "0"}, clear=True):
            self.assertEqual(node_main.main(), 2)


class ItRebuildsTheSameArraysFromTheSameSeed(unittest.TestCase):
    """Why a container can be handed a seed instead of a matrix."""

    def test_two_builds_of_one_config_agree(self):
        with mock.patch.dict("os.environ", environment(1, 0), clear=False):
            config = node_main.config_from_env()
        one, two = LocalAssociativeMemory(config), LocalAssociativeMemory(config)
        np.testing.assert_array_equal(one.wv, two.wv)
        np.testing.assert_array_equal(one.wo, two.wo)

    def test_a_different_seed_gives_different_arrays(self):
        """Otherwise the test above passes on a constant."""
        with mock.patch.dict("os.environ", environment(1, 0), clear=False):
            one = LocalAssociativeMemory(node_main.config_from_env())
        with mock.patch.dict("os.environ",
                             environment(1, 0, **{node_main.SEED_VAR: "6"}),
                             clear=False):
            two = LocalAssociativeMemory(node_main.config_from_env())
        self.assertFalse(np.array_equal(one.wv, two.wv))


if __name__ == "__main__":
    unittest.main()
