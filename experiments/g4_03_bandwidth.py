"""G4 — does the traffic fit a home connection?

Two directions, wildly asymmetric, and only one was ever accounted for:

    inbound   5 bytes per step, at any width and any vocabulary   note 012
    outbound  8 x vocab bytes per vote                            never counted

At a real vocabulary a vote is ~400 KB, so if every node must vote every step G4
fails by three orders of magnitude. But [g4-01](sweeps/g4-01-no-global-readout.txt)
found pooling OPTIONAL — one machine of eight scores 0.949 alone — so the
question is not how big a vote is. **It is how often a node must speak.**

A silent node still LISTENS: it keeps writing bindings into its own store, and
listening is the cheap direction. Only the answer is withheld.

    python experiments/g4_03_bandwidth.py
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.harness import parse_args  # noqa: E402
from openplexus.distributed import Network  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH, STEPS = 41, 32, 120
RATES = (1.0, 0.5, 0.25, 0.125, 0.0)      # 0.0 becomes one speaker, the floor
NODE_COUNTS = (2, 4, 8, 16)
#: A home uplink, generously. The gate is about consumer connections.
UPLINK_BYTES_PER_SECOND = 10_000_000 / 8      # 10 Mbit/s


def config(vocab: int = VOCAB) -> LocalMemoryConfig:
    return LocalMemoryConfig(vocab_size=vocab, d_model=WIDTH, lr=0.05,
                             key_scale=0.5, decay=0.9, derived_keys=True,
                             seed=5)


def measure(nodes: int, speak: float, tokens: np.ndarray,
            combine: str = "sum") -> tuple:
    """Agreement with the single-process answer, and what the protocol costs.

    The reference is always the whole network's exact answer. Under `sum` that
    is what a full-speaking network reproduces bit for bit; under `vote` it is a
    target rather than an identity, since a vote discards everything except each
    node's argmax.
    """
    model = LocalAssociativeMemory(config())
    model.wo[:] = model.wv          # or every node is interchangeable
    reference = model.run(tokens)
    with Network(config(), nodes, model.wv, model.wo, combine=combine) as net:
        answer = net.run(tokens, speak=speak)
        per_vote = net.bytes_per_vote
        per_listen = net.bytes_per_step_inbound
    agreement = float((answer == reference).mean())
    # Votes actually sent, by the same rule the driver uses.
    speakers = max(1, int(round(speak * nodes)))
    return agreement, speakers, per_vote, per_listen


def main() -> int:
    parse_args(__doc__.splitlines()[0])
    tokens = np.random.default_rng(9).integers(0, VOCAB, STEPS)

    print("AGREEMENT WITH THE WHOLE NETWORK, by speaking rate")
    print("(1.000 means the answer is unchanged by the silence)")
    print(f"{'nodes':>6}" + "".join(f"{r:>10}" for r in RATES))
    for nodes in NODE_COUNTS:
        row = [f"{nodes:>6}"]
        for rate in RATES:
            agreement, *_ = measure(nodes, rate, tokens)
            row.append(f"{agreement:>10.3f}")
        print("".join(row), flush=True)

    print("\nSAME, WITH combine='vote' -- each node sends its OWN answer,")
    print("not a distribution. Absence costs a voter, not a term of a sum.")
    print(f"{'nodes':>6}" + "".join(f"{r:>10}" for r in RATES))
    for nodes in NODE_COUNTS:
        row = [f"{nodes:>6}"]
        for rate in RATES:
            agreement, *_ = measure(nodes, rate, tokens, combine="vote")
            row.append(f"{agreement:>10.3f}")
        print("".join(row), flush=True)

    print("\nWHAT IT COSTS TO SPEAK, per node per step")
    _, _, per_vote, per_listen = measure(2, 1.0, tokens)
    _, _, token_vote, _ = measure(2, 1.0, tokens, combine="vote")
    print(f"  listening   {per_listen:>10,} bytes   (independent of vocabulary)")
    print(f"  one vote    {per_vote:>10,} bytes   at vocab {VOCAB}")
    for vocab in (1_000, 50_000):
        net_bytes = 4 + 4 + 8 * vocab
        print(f"  one vote    {net_bytes:>10,} bytes   at vocab {vocab:,}")
    print(f"  TOKEN vote  {token_vote:>10,} bytes   at ANY vocabulary")

    print("\nSTEPS PER SECOND A 10 Mbit/s UPLINK SUPPORTS, one speaking node")
    for vocab in (VOCAB, 1_000, 50_000):
        cost = 8 + 8 * vocab
        print(f"  vocab {vocab:>7,}   {UPLINK_BYTES_PER_SECOND / cost:>10,.0f} steps/s")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
