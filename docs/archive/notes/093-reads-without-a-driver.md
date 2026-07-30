093 — Reads without a driver, and what removing it costs
=======================================================

**Status:** built and measured, with a misroute control. **The driver's reduction is gone
from the read path** — the collective amended C1 forbids — and the removal has a stated
price rather than being free.

---

## IN PLAIN TERMS

Until now every distributed read went through a driver that asked **every** node and added
up the answers. That sum is the one piece of global synchronisation the project's first
constraint rules out, and containers measured what it costs: at sixteen nodes, letting the
driver run further ahead made things *slower*, because it still has to hear from everyone
before it can move.

**A read now goes to the single node that holds the fact and comes straight back.** Two
messages instead of two per node. At sixteen nodes that is sixteen times fewer; at
two hundred and fifty six, two hundred and fifty six times fewer.

**What it costs is that the node decides how the read is done**, not the asker. That is not
a detail — it moves a choice from one place to many.

---

## The measurement

    24 reads, 4 peers, no driver, routing by concept

    match the in-process read      24/24
    decode to the right token      24/24
    CONTROL, misrouted by one       0/24
    0.536 ms per read on loopback, 2 messages per read

**The control is the load-bearing part.** The first version of this measurement passed while
`RemoteConcepts.owner` was effectively the identity, because the test handed it an
already-computed owner instead of a concept — so the routing was never exercised and every
assertion still held. Misrouting by one concept now gives **0/24**, which is what makes the
24/24 mean something.

## Message cost, which is the whole argument

    nodes    broadcast (msgs / bytes)    point to point    ratio
        4              8  /   8,212        2  /  2,056        4x
       16             32  /  32,848        2  /  2,056       16x
      256            512  / 525,568        2  /  2,056      256x

**And the serialisation point goes with it.** Note 086 measured a window of 8 at sixteen
nodes performing *worse* than a window of 4 (2.01 ms against 1.82) because the driver must
collect every vote before advancing. Nothing in a point-to-point read waits on a node that
was not asked.

## The seam this costs, stated because it is a real loss

`ConceptStore.matrix` exists so a caller keeps its own retrieval strategy — `SuperposedRead`,
`SettlingRead` and `ExactCache` all take a matrix. **A remote store cannot return a matrix:**
at width 256 that is 512 KB per read against 2 KB for the answer.

So the owning node performs the retrieval and returns the vector. **The retrieval strategy
now lives on the node rather than with the asker**, which means a network can no longer be
asked to read one way rather than another from outside. Whether that matters depends on
whether the strategies were ever going to differ per query, which nothing has measured.

## What is NOT claimed

**Not wired into `search`.** `beam` calls `readable.matrix(concept)`; `RemoteConcepts`
answers `read(concept, previous, token)`. The transport exists and the traversal does not use
it, so **no traversal has yet run without a driver.** That is the next piece and it is
integration rather than a question.

**Not the write path.** Reads are point-to-point; writes still go wherever the model's own
loop puts them. `ConceptStore.write` fans out to `replicas` nodes, which is a small
collective of its own and untouched here.

**Not churn-tested.** A peer that vanishes mid-read gives the asker a broken socket rather
than a degraded answer, and `distributed.py`'s deadline machinery is not wired in.

**And `owner` is `concept % peers`, not the ring.** `ownership.Ring` is the consistent-hashing
mapping this should use — it is what keeps a departure moving 1/n of concepts rather than
nearly all — and the modulo here is a placeholder that reshuffles everything when the peer
count changes. Deliberate for a transport test, wrong for a network.
