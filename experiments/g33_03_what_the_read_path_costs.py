"""g33-03: how many peer messages does one query cost?

`openplexus/federated.py` splits the count table by `owner(surface)` and counts
every crossing. This is the reproduction of what that counter says, because the
figures were first taken at a terminal while the module was being written and
**a number that lives only in a terminal is a number nobody can check** — which
is the failure `tools/check_provenance.py` refused to let past.

**Not a prediction, and it must not be read as one.** `g32-01`, `g32-02` and
`g33-01` all registered predictions before running. These are measurements of a
mechanism taken while building it, which is weaker evidence, and the record says
so beside the numbers.

What is being counted, per WALK — one query, from one surface to the equivalence
class it reaches:

    remote reads   `count(y)` lookups a node had to ask a peer for
    hops           node-to-node steps the walk took
    fan-out        distinct partners a surface has ever been seen beside

The question the numbers answer is whether the read scales with `k` — how many
partners are kept — or with fan-out — how many were ever seen. It is fan-out,
and that is the finding.
"""

from __future__ import annotations

import sys
import time
from itertools import combinations
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from experiments import harness  # noqa: E402
from openplexus.federated import Federation  # noqa: E402
from openplexus.grounding import STATISTICS  # noqa: E402
from openplexus.tasks.occasions import OccasionConfig, generate  # noqa: E402

#: Carried from g33-01 at its own shape. `concepts` is the axis here, because
#: the whole question is what the cost scales WITH.
SURFACES = 3
PRESENCE = 0.7
NOISE = 3
DISTRACTORS = 1
OCCASIONS = 4000
K = SURFACES - 1
NODES = 8
CONCEPTS = (16, 32, 64)
ARMS = ("conditional", "local")


def _fill(concepts: int, seed: int) -> tuple[OccasionConfig, Federation]:
    config = OccasionConfig(concepts=concepts, surfaces=SURFACES,
                            presence=PRESENCE, noise=NOISE,
                            distractors=DISTRACTORS, occasions=OCCASIONS,
                            seed=seed)
    federation = Federation(nodes=NODES, seed=seed)
    for occasion in generate(config):
        for surface in occasion.surfaces:
            federation.note(surface, sender=federation.owner(surface))
        for one, other in combinations(sorted(occasion.surfaces), 2):
            federation.link(one, other)
    return config, federation


def main() -> None:
    harness.parse_args(__doc__)
    started = time.time()

    print(f"g33-03  surfaces {SURFACES}  presence {PRESENCE}  noise {NOISE}  "
          f"distractors {DISTRACTORS}  occasions {OCCASIONS}  k {K}  "
          f"nodes {NODES}")
    print("        one WALK is one query: from a surface to the class it "
          "reaches\n")

    header = (f"{'arm':<13}{'concepts':>9}{'surfaces':>10}{'fan-out':>9}"
              f"{'reads/walk':>12}{'hops/walk':>11}{'reads/fan-out':>15}")
    print(header)
    print("-" * len(header))

    for arm in ARMS:
        statistic = STATISTICS[arm]
        for concepts in CONCEPTS:
            config, federation = _fill(concepts, seed=0)
            surfaces = config.concept_surfaces
            fan = sum(len(federation.partners_of(s)) for s in range(surfaces))
            fan /= surfaces

            before_reads, before_hops = federation.remote_reads, federation.hops
            for surface in range(surfaces):
                federation.walk(surface, statistic, K)
            reads = (federation.remote_reads - before_reads) / surfaces
            hops = (federation.hops - before_hops) / surfaces
            ratio = reads / fan if fan else 0.0
            print(f"{arm:<13}{concepts:>9}{surfaces:>10}{fan:>9.1f}"
                  f"{reads:>12.1f}{hops:>11.1f}{ratio:>15.1f}")
        print()

    print("WRITE PATH, for comparison -- one link is two messages, one per owner")
    config, federation = _fill(64, seed=0)
    print(f"    {federation.writes} row updates over {OCCASIONS} occasions "
          f"= {federation.writes / OCCASIONS:.1f} per occasion")

    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
