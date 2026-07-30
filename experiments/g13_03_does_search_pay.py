"""Does search beat traversal, and does traversal beat the mechanism we had?

Decision 107 declined to build a pair-key traversal: *"a perfect traversal buys
0.05"*, because steps 1 and 3 were 0.710 and 0.677 and compounding did the rest.
Decision 111 then declined search: *"you cannot search your way out of noisy
primitives, because the verifier is built from the primitives."*

**Both refusals were correct arithmetic on the numbers available then, and both
conditions have since been measured away.**

    g13-01   step 1 at out-degree 1   1.000 +/-0.000
    g13-02   step 2 at a unique pair  1.000, 0.971 overall
    g13-02   ceiling for the two together              1.000, against the 0.87
                                                       that would have justified it

So this measures the mechanism decision 123 built, end to end, against the one it
replaces.

## The arms, and why the control is not the obvious one

    concat    hops=2, no pair keys, search off      what we had. 0.327 in g13-01
    walk      search_branches=1, pair keys          TRAVERSAL ONLY, no search
    search4   search_branches=4
    search8   search_branches=8

**`walk` is the control that matters.** It is decision 107's mechanism -- commit
to the single best candidate and follow pair keys -- with no search on top. Without
it, any gain would be attributable to search when traversal might have supplied
all of it. This is why `search_branches=0` means off and `1` means a greedy walk:
the control had to be expressible.

Width is fixed at 256. g13-01 and g13-02 both measured width moving these numbers
by less than 0.03, so spending cells on it would buy nothing; seeds are the axis
that pays.

## PREDICTIONS (registered before running)

  P1  `walk` beats `concat` by more than 0.10. Decision 107's "traversal buys
      0.05" was computed with steps 1 and 3 at 0.710 and 0.677; they are 1.000 at
      out-degree 1 now, so the compounding that made traversal pointless no
      longer holds.
  P2  `search4` beats `walk`. This is the whole claim -- that CHOOSING among
      branches, by checking which reaches the named target, is worth more than
      committing to the loudest one. If refuted, decision 107's traversal was the
      entire gain and search is decoration.
  P3  `search4` clears the shortcut floor (`first`, about 0.466). No mechanism on
      this task has ever done so -- g13-01 measured `concat` at 0.327 against it.
  P4  `search8` is within 0.02 of `search4`. Only two or three relations are
      plausible candidates for any subject, so branches beyond that are spent on
      tokens the first decode already ranked far down.
  P5  Every arm falls short of 1.000. The g13-02 ceiling holds steps 1 and 3 at
      their out-degree-1 value, and real sequences are not all out-degree 1 --
      search has to FIND that regime and will not always.

P2 is the decision-relevant one. P1 and P5 are the sanity rails: if P1 fails the
wiring is inert, and if P5 fails the ceiling was not a ceiling.

COST: 4 arms x 8 seeds at one width = 32 cells. Estimated from the MOST EXPENSIVE
cell (`search8`, where eight branches each walk two relations and every step is a
retrieval), printed by `--cost` before dispatch.

MEASURED ON: `openplexus/tasks/kinship.py`, hops 2, 12 people, 10 facts.
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

WIDTH = 256
N_TRAIN, N_TEST, EPOCHS = 400, 200, 4
SEEDS = tuple(range(8))

ARMS = {
    # What we had. No pair keys, so the hop re-encodes through Wk.
    "concat": dict(hops=2, hop_accumulate="concat", derived_keys=True),
    # Decision 107's traversal, with no search on top. THE CONTROL.
    "walk": dict(hops=2, hop_accumulate="concat", derived_keys=True,
                 context_keys=True, search_branches=1),
    "search4": dict(hops=2, hop_accumulate="concat", derived_keys=True,
                    context_keys=True, search_branches=4),
    "search8": dict(hops=2, hop_accumulate="concat", derived_keys=True,
                    context_keys=True, search_branches=8),
}


def build(arm: str, task: KinshipConfig, seed: int) -> LocalAssociativeMemory:
    settings = dict(ARMS[arm])
    if settings.get("search_branches"):
        settings["search_fact_token"] = task.fact_token
        settings["search_query_token"] = task.query_token
    return LocalAssociativeMemory(LocalMemoryConfig(
        d_model=WIDTH, vocab_size=task.vocab_size, seed=seed, **settings))


if __name__ == "__main__":
    harness.kinship_sweep(
        __doc__, ARMS, build, width=WIDTH, n_train=N_TRAIN, n_test=N_TEST,
        epochs=EPOCHS, seeds=SEEDS, cost_arm="search8",
        cost_why=("eight branches each walk two relations and every step of "
                  "every walk is a retrieval"))
