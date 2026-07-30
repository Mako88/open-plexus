086 — Containers work, and the transport cannot carry PAIR KEYS
==============================================================

**Status:** measured on real containers over a bridge network with `tc netem`. **The
headline is a defect nobody had noticed**, and it is the same shape as note 072: two
decisions each correct alone and incompatible together.

`distributed.Network(spawn=False)` was built for this and never used outside a test — its
docstring says a container on an emulated link *"is the only way G2, G3 and G4 stop being
modelled."* This is that, and it found something.

---

## IN PLAIN TERMS

The distributed machinery works. Nodes as separate containers, over a real network, with
realistic delay injected, produce **exactly** the answer one process produces — and killing
a container mid-run costs one step rather than the run.

**But only when addresses come from single tokens.** The driver broadcasts one token id,
four bytes, and a node rebuilds what it needs from that. **A pair key needs two tokens —
the current one and the one before — and the node is never told the second.** So it builds
a different address than the driver assumed and the answer stops matching.

**Every relational result in this project uses pair keys.** They are the chosen option for
relational work. So the transport that has been measured and the addressing that has been
chosen do not currently fit together, and nothing caught it because every distributed test
uses single-token keys and every relational result is in-process.

---

## The finding

    d=64, 4 containers, netem 5 ms, a fixture whose bar predicts 17 distinct
    tokens over 32 steps -- so the exactness check CAN fail

    context_keys=True   (PairKeys)        exact = FALSE
    context_keys=False  (TableKeys)       exact = TRUE

`distributed.py` states the assumption plainly and it is correct on its own terms:

> *"`Wk` is needed in full by every node... Being a frozen projection, row `t` can instead
> be drawn from `(seed, t)` on demand, **which is what makes a four-byte token
> sufficient**"*

**`PairKeys.pair(previous, token)` needs both.** Four bytes is sufficient for a key indexed
by one token and insufficient for a key indexed by two. **Revival is cheap and unbuilt:**
broadcast eight bytes instead of four, or have the node keep the previous token itself —
the node already tracks `step`, so carrying one integer is not a new mechanism. Neither is
tried, and until one is, **no relational result has ever crossed a wire.**

## What DOES work, and the numbers

    exactness       TRUE at 4, 8 and 16 nodes, d=64 and d=256, with single-token
                    keys and a check that can fail
    latency         lock-step tracks the ONE-WAY delay almost exactly:
                    25.76 ms/step at 25 ms, 5.76 at 5 ms, 0.44 on loopback
    the window      converts latency into throughput almost perfectly --
                    window 4 gives 3.97x at both 5 ms and 25 ms

    node count      lock-step ms/step, netem 5 ms, d=256
                     4 nodes   5.81      window 4: 1.48   window 8: 0.78
                     8 nodes   6.10      window 4: 1.93   window 8: 1.73
                    16 nodes   6.48      window 4: 1.82   window 8: 2.01

**Latency is nearly independent of node count** — +11% for four times the nodes, because
nodes answer in parallel. **But the window's benefit reverses at 16**, where window 8 (2.01)
is worse than window 4 (1.82). The driver has to collect every vote per step, so **the
driver's reduction is the scaling limit, not the network** — which is the global sum C1
forbids and the thing `note 081` says concept partitioning removes. **The containers reach
the same conclusion the arithmetic did, independently.**

## Churn: survivable, and not free

`docker kill` on one of four containers, mid-run, 1500 steps at 25 ms, `deadline=0.15`:

    the run COMPLETED               39.1 s, no crash, no hang
    steps settled short                   1
    exact against the all-nodes bar   FALSE

**One short step**, because the driver stops expecting a node once it has missed a deadline
— after which the remaining three proceed normally. So a departure costs a step's
completeness, not the run, which is what `deadline` was built for.

**And the answer degrades**, which is correct rather than a failure: a quarter of the width
is gone. That is dimension splitting's signature — *every* answer slightly worse — against
concept splitting's, where *some* concepts vanish entirely. The tree records that contrast
as reasoning; this is it happening.

## The trap I fell into, which was already written down

Every container run reported `exact=True` at first — **including one where a node served a
completely different model** (fingerprints `9186fc8db5b7` against `393a0b99a04d`). The bar
predicted **token 0 for all 32 steps**, so `array_equal` compared all-zeros to all-zeros.

`tests/test_connection_order.py` had hit this and said so:

> *"`wo` is learned by the delta rule and starts at zeros, so an untrained model scores
> every token 0 and predicts token 0 forever. Every node would then be interchangeable and
> a departure would change nothing — **which is exactly what the vacuity guard below caught
> on the first attempt.**"*

Seeding `wo` from `wv` is the fix. `cluster_driver.py` now **refuses to report** an
exactness number when the bar predicts fewer than three distinct tokens, because a check
that cannot fail is worse than no check.

> **Two harness bugs, and both were mine rather than the system's.** The other:
> `--abort-on-container-exit` killed the driver the moment the victim container died, so
> the churn test could not observe the thing it existed to observe. A flag that makes a
> normal run tidy makes this measurement impossible.

## What is NOT claimed

**Not concept partitioning.** `ConceptStore` has no socket transport, so everything here is
DIMENSION splitting — the arrangement `note 081` says caps at `width ÷ 16`. A number from
this harness is not a number about the arrangement the project chose.

**Not that the window helps a traversal.** It amortises INDEPENDENT positions. A walk's hops
are dependent — hop four needs hop three's answer — so `SCALE.md`'s ~500 ms for ten hops is
not reduced by this mechanism, and the 3.97x above must not be read as buying headroom
against `d_max`.

**And netem was applied to the nodes only**, so the driver's egress is undelayed and the
measured per-step cost is roughly one-way rather than round-trip. A symmetric emulation
would roughly double it.
