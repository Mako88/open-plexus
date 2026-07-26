"""Sizing a machine nobody has seen.

Deployment code cannot be checked the way the rest of this project is checked.
There is no seed, no oracle and no reference implementation -- the machine is
whatever it is. So these tests pin two different kinds of claim:

**Arithmetic that must hold whatever is detected.** A plan has to fit in the
memory it was allowed, and its nodes have to partition its capacity exactly,
because `slices_for` rejects uneven splits rather than rounding them.

**A policy that is a citation, not a preference.** The allocation rule comes
from g7-03. Flipping it would be a different deployment and a wrong one, so the
tests name the measurement and the mutation harness restores the flip.

The container path is exercised by substituting the file reads rather than by
launching Docker: the question is whether cgroup values are PREFERRED over the
host's, and that is answered by what the function reads, not by where it runs.
It was also checked for real at `--memory=64m --cpus=1.5`, which reported 64MB
and 1.5 CPUs against a host with 8.
"""

from __future__ import annotations

import unittest
from unittest import mock

from openplexus import deployment
from openplexus.deployment import (
    Machine, bytes_per_dimension, detect, plan)

D_MODEL, VOCAB = 256, 41
HOST = Machine(cpus=8.0, memory_bytes=1 << 30, containerised=False, source="host")


#: A host that is much larger than any container limit below. It has to be
#: present, or "prefer the host" and "prefer the cgroup" agree by default: with
#: no /proc/meminfo to read, a host-first bug falls through to the cgroup value
#: and every test here passes on the broken order.
HOST_MEMINFO = "MemTotal: 65536000 kB\nMemAvailable: 32768000 kB\n"


def cgroups(cpu: str | None, memory: str | None):
    """Stand in for the cgroup and /proc files, and nothing else."""
    table = {"/sys/fs/cgroup/cpu.max": cpu,
             "/sys/fs/cgroup/memory.max": memory,
             "/proc/meminfo": HOST_MEMINFO}
    return mock.patch.object(deployment, "_read",
                             side_effect=lambda path: table.get(path))


class APlanFitsInTheMemoryItWasAllowed(unittest.TestCase):

    def test_capacity_costs_no_more_than_its_share(self):
        for megabytes in (8, 64, 512, 4096):
            machine = Machine(cpus=1.0, memory_bytes=megabytes << 20,
                              containerised=True, source="test")
            made = plan(D_MODEL, VOCAB, machine=machine)
            self.assertLessEqual(
                made.capacity * bytes_per_dimension(D_MODEL, VOCAB),
                machine.memory_bytes,
                f"a plan for {megabytes}MB claimed more memory than the "
                f"machine has, never mind its share of it")

    def test_a_tiny_machine_still_gets_a_workable_plan(self):
        """A node that plans for zero dimensions is not a node."""
        made = plan(D_MODEL, VOCAB, machine=Machine(
            cpus=0.1, memory_bytes=1 << 16, containerised=True, source="test"))
        self.assertGreaterEqual(made.capacity, 1)
        self.assertGreaterEqual(made.nodes, 1)

    def test_capacity_never_exceeds_the_whole_network(self):
        made = plan(D_MODEL, VOCAB, machine=Machine(
            cpus=64.0, memory_bytes=1 << 40, containerised=False, source="test"))
        self.assertLessEqual(made.capacity, D_MODEL)


class NodesPartitionCapacityExactly(unittest.TestCase):
    """`slices_for` refuses uneven slices, so a plan must not produce one."""

    def test_nodes_times_width_is_capacity(self):
        for megabytes in (8, 64, 512, 4096):
            for gated in (True, False):
                machine = Machine(cpus=2.0, memory_bytes=megabytes << 20,
                                  containerised=True, source="test")
                made = plan(D_MODEL, VOCAB, gated=gated, machine=machine)
                self.assertEqual(made.nodes * made.node_width, made.capacity,
                                 f"{made} does not partition its own capacity")

    def test_an_awkward_width_rounds_capacity_down_rather_than_splitting_unevenly(self):
        with mock.patch.dict("os.environ", {deployment.NODE_WIDTH_VAR: "7"}):
            made = plan(D_MODEL, VOCAB, machine=HOST)
        self.assertEqual(made.node_width, 7)
        self.assertEqual(made.capacity % 7, 0)


class TheAllocationPolicyIsG7_03(unittest.TestCase):
    """Not a preference. Flipping it would be a measurably worse deployment."""

    def test_gated_takes_the_smallest_node(self):
        """Allocation measured worth at most 0.031 gated, so take width 1 --
        the node that fits on the most devices, which is the whole priority."""
        self.assertEqual(plan(D_MODEL, VOCAB, gated=True, machine=HOST).node_width, 1)

    def test_ungated_spends_everything_on_one_node(self):
        """Worth up to 0.425 ungated, wider always better, because narrow nodes
        sit below the g5-04 width floor and contribute nothing."""
        made = plan(D_MODEL, VOCAB, gated=False, machine=HOST)
        self.assertEqual(made.nodes, 1)
        self.assertEqual(made.node_width, made.capacity)

    def test_the_plan_carries_what_it_rests_on(self):
        """A caveat that lives in a comment does not reach the operator."""
        basis = plan(D_MODEL, VOCAB, gated=True, machine=HOST).basis
        self.assertIn("0.031", basis)
        self.assertIn("ORACLE", basis.upper())


class AContainerSeesItsOwnLimits(unittest.TestCase):
    """The bug this module exists to avoid.

    `os.cpu_count()` reports the HOST's processors inside a container and
    `/proc/meminfo` reports the host's memory, so a container allowed one core
    of forty plans for forty.
    """

    def test_cgroup_cpu_beats_the_host_count(self):
        with cgroups("150000 100000", "67108864"):
            with mock.patch("os.cpu_count", return_value=64):
                machine = detect()
        self.assertAlmostEqual(machine.cpus, 1.5)
        self.assertTrue(machine.containerised)
        self.assertEqual(machine.source, "cgroup")

    def test_cgroup_memory_beats_the_hosts(self):
        with cgroups("max", "67108864"):
            machine = detect()
        self.assertEqual(machine.memory_bytes, 67108864)

    def test_an_unlimited_cgroup_is_not_a_limit(self):
        with cgroups("max", "max"):
            with mock.patch("os.cpu_count", return_value=4):
                machine = detect()
        self.assertEqual(machine.cpus, 4.0)

    def test_the_v1_sentinel_is_not_read_as_an_allowance(self):
        """cgroup v1 reports a number near 2**63 to mean "no limit"."""
        table = {"/sys/fs/cgroup/memory/memory.limit_in_bytes": str(2 ** 63 - 1)}
        with mock.patch.object(deployment, "_read",
                               side_effect=lambda p: table.get(p)):
            self.assertIsNone(deployment._cgroup_memory())


class TheEnvironmentWins(unittest.TestCase):
    """John asked for a static override before the dynamic path is trusted."""

    def test_capacity_override(self):
        with mock.patch.dict("os.environ", {deployment.CAPACITY_VAR: "12"}):
            made = plan(D_MODEL, VOCAB, machine=HOST)
        self.assertEqual(made.capacity, 12)
        self.assertIn(deployment.CAPACITY_VAR, made.basis)

    def test_width_override_survives_the_policy(self):
        with mock.patch.dict("os.environ", {deployment.CAPACITY_VAR: "12",
                                            deployment.NODE_WIDTH_VAR: "4"}):
            made = plan(D_MODEL, VOCAB, gated=True, machine=HOST)
        self.assertEqual((made.capacity, made.node_width, made.nodes), (12, 4, 3))


if __name__ == "__main__":
    unittest.main()
