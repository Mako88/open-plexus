095 — Consistent hashing for peer routing, and a control that weakened when it landed
=====================================================================================

**Status:** built and measured. It fixes the placeholder note 093 and 094 both flagged, and
the fix **quietly broke a control**, which is the part worth reading.

---

## IN PLAIN TERMS

Peer routing used `concept % peers`, which was fine for a transport test and wrong for a
network: **change the number of peers and nearly every fact is suddenly at an address nobody
asks about.** Consistent hashing moves about one in `n` instead.

    peers  ->  peers    modulo moves    RING moves    ideal 1/n
        4          5           80.0%         19.1%        20.0%
        8          9           88.9%         10.2%        11.1%
       16         17           93.8%          6.0%         5.9%
       64         65           98.4%          1.4%         1.5%

**At 64 peers the modulo relocates 98.4% of concepts and the ring relocates 1.4%** — a
seventyfold difference, and the ring lands on the theoretical ideal to within a tenth of a
point. Under C3's churn, where peers arrive and leave continuously, the modulo would leave the
store permanently mis-addressed.

---

## The failure the swap caused, which is the useful part

Four tests broke immediately. **They were writing through `concept % NODES` while reads had
started routing through the ring** — writer and reader disagreeing about ownership, which is
precisely the fault the ring exists to prevent, arriving as a test failure rather than as
silently wrong numbers.

The fixture now builds `Ring(NODES, seed=RING_SEED)` and `RemoteConcepts` is handed the same
seed. **Writer and reader agree because they compute the same thing, not because anything
coordinates** — and there is now a test asserting that agreement directly, since everything
else rests on it.

## And then the control weakened, silently

`test_MISROUTING_breaks_it` asks the same read of the wrong owner and requires it not to
match. It expressed "wrong owner" as `concept + 1`.

**Under the modulo that always changed peer. Under the ring it usually does not** —
consistent hashing puts adjacent concepts on the same peer — so three of twenty-four
"misrouted" reads went to the right peer and matched, and the control failed.

> **The control was right to fail and wrong in how it was written.** It expressed the thing
> it was controlling — the peer — through an arithmetic on a different quantity, and that
> arithmetic stopped implying a peer change. A control has to be expressed in the quantity it
> controls. It now searches for a concept the ring genuinely sends elsewhere.

**This is the second time this control has been strengthened.** The first was because the test
handed `owner` an already-computed owner, so the routing was never exercised at all (note
093). A control that has needed fixing twice is not a weak control; it is the only assertion
in the file that has ever caught anything.

## What is NOT claimed

**Not that ownership is dynamic.** The ring is built once from a fixed peer count. Nothing
here adds or removes a peer at runtime, so the 1.4% figure is arithmetic about what *would*
move, not a measurement of a live rebalance.

**Not the replica set.** `Ring.holders(concept, replicas)` gives the next distinct peers
clockwise and `ConceptStore` uses it; `RemoteConcepts.owner` asks only the first. So a peer
that vanishes still costs its concepts, and note 093's churn gap is untouched.

**And the seed is a shared constant.** Two peers with different ring seeds disagree about
everything and nothing detects it. That is the same class as note 086's config fingerprint,
and no fingerprint covers the ring.
