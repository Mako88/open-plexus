"""Does the beam beat root-only search where `run()` actually runs?

`run()` has called `search` since decision 123 and has never called `beam`, which is
the mechanism note 064 built and note 065 measured as the largest single gain in the
project. Wiring it in is item one on the "wire everything up" list.

## Why this needs measuring rather than switching

The gap usually quoted for `beam` over `search` is **0.6588 against 0.8877 chain
recovery** — and that is CLUTRR, at chain lengths 2 to 10, scoring whether the true
sequence of relations was recovered. `run()`'s subject is neither of those things:

    the TASK is kinship, not CLUTRR
    the DEPTH is `hops = 2`, not 2-10
    the SCORE is the readout's answer, not the chain

Note 064's diagnosis says exactly why the depth matters: the relation decode is
**0.974 at the root and about 0.91 mid-chain**, and `search` hedges at the root while
committing everywhere after. At `hops = 2` there is exactly ONE mid-chain decode to
get wrong, so most of the room the beam exists to exploit is not there. Quoting the
CLUTRR number as if it transferred would be the note-087 mistake — a leverage table
that conflated two different measurements.

So this asks the narrow question the wiring actually turns on.

## The arms

    walk        search_branches=1                          the floor. No branching
    search4     search_branches=4                          what `run()` does today
    beam4       + search_beam_width=4                      branch at every step
    beam4-k2    + search_prune_every=2                     meet every OTHER hop

`beam4-k2` is here because the rendezvous is a **distribution** cost, not a search
parameter: note 102 measured the meeting as worth 0.089 chain recovery and its period
as worth nothing measurable, which is what lets a migrating walk meet `d_max`. If that
finding does not survive the end task, the distribution conclusion needs revisiting —
so it is measured here rather than assumed to transfer.

## PREDICTIONS (registered before running)

  GATE       `beam4` beats `search4` by at least 0.01. This is the whole claim, and
             the decision it gates is whether `search_beam_width` defaults to 4 or
             stays 0. **If refuted, the beam's advantage does not reach `run()`'s
             regime** and the honest conclusion is that the CLUTRR number was about
             depth, not about the mechanism — which is worth recording either way.

  RAIL       `beam4` beats `walk` by more than 0.05. If it does not, the branching is
             inert in `run()` and neither arm above is measuring what it claims. This
             is the check that the wiring is live at all.

  FALSIFIER  `beam4-k2` is within 0.02 of `beam4`. Note 102's period finding is a
             claim about search quality that the distribution argument leans on. A
             larger gap here refutes it on a second task, and `prune_every=2` stops
             being free.

COST: 4 arms x 8 seeds at one width = 32 cells. `--cost` times the most expensive arm
before dispatch.

MEASURED ON: `openplexus/tasks/kinship.py`, hops 2, the same task and sizes as
g13-03, so the `search4` column is comparable to a number already in the tree.
"""

from __future__ import annotations

import argparse
import json
import sys
import time
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.kinship import KinshipConfig  # noqa: E402

#: Matched to g13-03 so `search4` is comparable to a measurement already recorded,
#: rather than a fresh baseline nothing can be checked against.
WIDTH = 256
N_TRAIN, N_TEST, EPOCHS = 400, 200, 4
SEEDS = tuple(range(8))

_BASE = dict(hops=2, hop_accumulate="concat", derived_keys=True,
             context_keys=True)
ARMS = {
    "walk": dict(_BASE, search_branches=1),
    "search4": dict(_BASE, search_branches=4),
    "beam4": dict(_BASE, search_branches=4, search_beam_width=4),
    "beam4-k2": dict(_BASE, search_branches=4, search_beam_width=4,
                     search_prune_every=2),
}


def build(arm: str, task: KinshipConfig, seed: int) -> LocalAssociativeMemory:
    settings = dict(ARMS[arm])
    settings["search_fact_token"] = task.fact_token
    settings["search_query_token"] = task.query_token
    return LocalAssociativeMemory(LocalMemoryConfig(
        d_model=WIDTH, vocab_size=task.vocab_size, seed=seed, **settings))


if __name__ == "__main__":
    harness.kinship_sweep(
        __doc__, ARMS, build, width=WIDTH, n_train=N_TRAIN, n_test=N_TEST,
        epochs=EPOCHS, seeds=SEEDS, cost_arm="beam4-k2",
        cost_why=("skipping a rendezvous leaves the population uncapped between "
                  "meetings, so the beam grows by `branches` per unpruned hop -- "
                  "note 102 measured 2.29x the reads at period 2"))
