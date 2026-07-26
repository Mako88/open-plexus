"""How much of itself should this machine give, and how should it be divided?

**This is the first module here that is about deployment rather than
measurement.** Everything else answers a question; this one has to make a
decision, on a machine nobody has seen, with no oracle and no seed.

Two questions, and the sweeps have already answered the second.

**How much?** Bounded by memory. A node owning `w` of the network's `d`
dimensions holds `w x d` of the associative memory plus `vocab x w` of the value
projection and the same again of the readout -- so a machine hosting `C`
dimensions in total needs about `8 * C * (d + 2 * vocab)` bytes however it
divides them. That is the whole sizing calculation, and it does not depend on the
split.

**How divided?** [g7-03](../experiments/sweeps/g7-03-how-to-spend-a-machine.txt)
measured this directly, and the answer is two answers:

- **Gated: it does not matter.** Across every capacity the largest gap between
  the best and worst way to spend it was **0.031**. Sixteen dimensions reach
  1.000 as one node of sixteen or as sixteen nodes of one.
- **Ungated: it matters enormously**, worth up to **0.425**, and the rule is *as
  few and as wide as possible* -- because node width has to clear the g5-04
  floor of roughly 24 at seq 384 before a node contributes at all.

So the policy below is not a guess. Gated, it takes width 1, because allocation
is free and the smallest node is the one that runs on the most devices. Ungated,
it spends everything on one node, because anything narrower may be under the
floor.

**The caveat that has to travel with this:** the gated arm of g7-03 saturates at
capacity 16, so "allocation barely matters" is measured over capacities 1 to 8.
And the gate it was measured under is an oracle. Both are recorded in
`Plan.basis` rather than left in a comment here, so a deployed system can report
what its own configuration rests on.

## Containers

`os.cpu_count()` reports the HOST's processors inside a container, and
`/proc/meminfo` reports the host's memory. A container told it may use one core
of forty will happily plan for forty. The cgroup files are the authority and are
read first; the fallbacks are for when they are absent, not for when they are
inconvenient.
"""

from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path

#: Read before anything is detected. Set these and detection is not consulted.
CAPACITY_VAR = "OPENPLEXUS_CAPACITY"
NODE_WIDTH_VAR = "OPENPLEXUS_NODE_WIDTH"
MEMORY_VAR = "OPENPLEXUS_MEMORY_BYTES"
CPUS_VAR = "OPENPLEXUS_CPUS"

#: Fraction of detected memory the plan is allowed to spend. A node is not the
#: only thing on the machine, and on a phone or a router it is very much not.
SHARE = 0.25

#: Used only when nothing can be detected at all.
ASSUMED_MEMORY = 256 * 1024 * 1024


@dataclass(frozen=True)
class Machine:
    """What this machine will admit to having."""

    cpus: float
    memory_bytes: int
    containerised: bool
    source: str                 # how the numbers were obtained, for reporting


@dataclass(frozen=True)
class Plan:
    """How many dimensions to host, and how to divide them."""

    capacity: int
    node_width: int
    nodes: int
    basis: str                  # what measurement this rests on, and its limits


def _read(path: str) -> str | None:
    try:
        return Path(path).read_text(encoding="utf-8").strip()
    except (OSError, ValueError):
        return None


def _cgroup_cpus() -> float | None:
    """CPU allowance from cgroup v2 then v1, or None if unlimited/absent."""
    value = _read("/sys/fs/cgroup/cpu.max")            # "max" or "quota period"
    if value:
        quota, _, period = value.partition(" ")
        if quota != "max" and period:
            return int(quota) / int(period)
    quota = _read("/sys/fs/cgroup/cpu/cpu.cfs_quota_us")
    period = _read("/sys/fs/cgroup/cpu/cpu.cfs_period_us")
    if quota and period and int(quota) > 0:
        return int(quota) / int(period)
    return None


def _cgroup_memory() -> int | None:
    for path in ("/sys/fs/cgroup/memory.max",
                 "/sys/fs/cgroup/memory/memory.limit_in_bytes"):
        value = _read(path)
        if value and value != "max" and value.isdigit():
            limit = int(value)
            # v1 reports a sentinel near 2**63 to mean "no limit". Anything
            # above a terabyte is that sentinel rather than a real allowance.
            if 0 < limit < 1 << 40:
                return limit
    return None


def _meminfo_available() -> int | None:
    value = _read("/proc/meminfo")
    if not value:
        return None
    for line in value.splitlines():
        if line.startswith("MemAvailable:"):
            return int(line.split()[1]) * 1024
    return None


def detect() -> Machine:
    """What this machine has, preferring the container's limits to the host's."""
    override_cpus = os.environ.get(CPUS_VAR)
    override_memory = os.environ.get(MEMORY_VAR)
    containerised = (Path("/.dockerenv").exists()
                     or _cgroup_cpus() is not None
                     or _cgroup_memory() is not None)

    if override_cpus or override_memory:
        source = "environment"
    elif containerised:
        source = "cgroup"
    else:
        source = "host"

    cpus = (float(override_cpus) if override_cpus
            else _cgroup_cpus() or float(os.cpu_count() or 1))
    memory = (int(override_memory) if override_memory
              else _cgroup_memory() or _meminfo_available() or ASSUMED_MEMORY)
    if not override_memory and _cgroup_memory() is None and _meminfo_available() is None:
        source += "+assumed-memory"
    return Machine(cpus=cpus, memory_bytes=memory,
                   containerised=containerised, source=source)


def bytes_per_dimension(d_model: int, vocab_size: int) -> int:
    """Memory one dimension of capacity costs, whatever the split.

    `d_model` of the associative memory, plus a value column and a readout
    column, at eight bytes each. Independent of how the dimensions are grouped
    into nodes, which is why sizing and allocation are separate decisions.
    """
    return 8 * (d_model + 2 * vocab_size)


def plan(d_model: int, vocab_size: int, gated: bool = True,
         machine: Machine | None = None) -> Plan:
    """Decide capacity and allocation for this machine.

    `gated` says whether selective storage is in use, and it changes the
    allocation rule completely -- see the module docstring. It defaults to True
    because that is the configuration every tiny-node result was measured in,
    and a caller who is not gating should have to say so.
    """
    machine = machine or detect()
    override_capacity = os.environ.get(CAPACITY_VAR)
    override_width = os.environ.get(NODE_WIDTH_VAR)

    if override_capacity:
        capacity = int(override_capacity)
        basis = f"{CAPACITY_VAR} was set"
    else:
        budget = int(machine.memory_bytes * SHARE)
        capacity = max(1, budget // bytes_per_dimension(d_model, vocab_size))
        capacity = min(capacity, d_model)      # never more than the whole network
        basis = (f"{SHARE:.0%} of {machine.memory_bytes} bytes reported by "
                 f"{machine.source}")

    if override_width:
        width = int(override_width)
    elif gated:
        # g7-03: allocation is worth at most 0.031 gated, so take the smallest
        # node, which is the one that runs on the most devices.
        width = 1
        basis += ("; width 1 because g7-03 measured allocation as worth at most "
                  "0.031 when gated -- MEASURED ONLY OVER CAPACITIES 1-8, WHERE "
                  "IT HAD NOT SATURATED, AND UNDER AN ORACLE GATE")
    else:
        # g7-03: worth up to 0.425 ungated, rule is as few and as wide as
        # possible, because narrow nodes sit under the g5-04 width floor.
        width = capacity
        basis += ("; one wide node because g7-03 measured allocation as worth "
                  "up to 0.425 ungated, with wider always better")

    width = max(1, min(width, capacity))
    if capacity % width:
        # Uneven slices are explicitly a later milestone in distributed.py, so
        # round the capacity down rather than hand it a split it will reject.
        capacity -= capacity % width
    return Plan(capacity=capacity, node_width=width,
                nodes=capacity // width, basis=basis)
