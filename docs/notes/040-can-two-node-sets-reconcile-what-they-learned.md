# 040 — Can two node sets reconcile what they learned?

**IN PLAIN TERMS.** If two groups of machines each hold a copy of the model and
each learns from different conversations, they drift apart. To serve many people
at once we need to put them back together. The good news: the way this model
learns happens to be the kind of change that can be merged safely, because
addition does not care what order you do it in. The bad news: the obvious way to
merge — averaging the two copies — is specifically the operation the literature
says does **not** work, and it is what everyone in federated learning does.

---

## Why this note exists

John, 2026-07-28: *"the requirements per node need to be as minimal as possible,
and everything that is at all possible to scale by adding nodes should be the way
we scale rather than requiring heavier nodes."*

STATE.md's answer is that concurrency scales by giving each conversation its own
set of nodes, which keeps per-node memory constant. **The cost is that disjoint
sets drift**, because under C4 the readout never stops learning. Reconciling them
is the CRDT question GOALS §6.2 has carried as unread since the beginning.

Source: [Wikipedia on
CRDTs](https://en.wikipedia.org/wiki/Conflict-free_replicated_data_type). A
summary, not the papers — Shapiro et al. (2011) is **unread**. Rule 1 applies.

## The two shapes, and their exact requirements

**State-based (CvRDT)** — send the whole state, merge on arrival. The merge must
be **commutative, associative and idempotent**, forming a join-semilattice, and
updates must be **monotone** with respect to it. Idempotence is what makes it
survive duplicated and re-ordered delivery.

**Operation-based (CmRDT)** — send the operations. They must be **commutative and
associative**, but **need not be idempotent** — the price is that the transport
must deliver every operation **exactly once**.

## What our learning rule actually is

The delta rule updates the readout additively:

    Wo += lr * outer(error, retrieved)

**Addition is commutative and associative.** So the updates satisfy the
op-based requirements exactly, and **reconciliation is possible in principle**:
two node sets can exchange their deltas in any order and arrive at the same
matrix.

**They are not idempotent.** Applying the same delta twice doubles it. So this is
a CmRDT and it inherits the CmRDT obligation: **exactly-once delivery**, over the
unreliable internet, which is a real engineering requirement rather than a
detail. Duplicating a delta silently corrupts a weight; dropping one silently
loses learning. Neither announces itself.

## The trap, and it is the obvious approach

**Averaging is explicitly named as not safely mergeable**, along with
conditional updates, because it is not commutative in the required sense: the
mean of means is not the mean.

**That is precisely what federated averaging does.** FedAvg merges replicas by
averaging their weights, and it works there only because there is a **central
aggregator and synchronous rounds** — which note 003 already found to be a C1
violation twice over. So the standard answer from the field nearest our problem
is unavailable to us for the same reason it was unavailable before, and now there
is a second, independent argument against it.

**Do not reconcile node sets by averaging their readouts.**

## The idempotent version, and what it costs

G-Counter's trick makes an additive quantity properly state-based: **each node
owns a slot only it writes**, and merge takes the per-slot maximum. Applied here,
the state becomes *{node → that node's cumulative delta}* and the model is their
sum. Merge is then idempotent, commutative and associative — a genuine CvRDT,
safe under duplication and re-ordering, needing no exactly-once transport.

**The cost is `P` copies of the parameters.** At 62,500 nodes and ~6 MB each that
is ~375 GB of merge state for a ~6 MB model. Unusable as stated.

So the trade is real and now named:

| approach | converges? | transport requirement | state |
|---|---|---|---|
| exchange deltas (CmRDT) | yes | **exactly-once** | one model |
| per-node slots (CvRDT) | yes | none | **P models** |
| average the models | **no** | — | one model |

## What to do

1. **Neither extreme.** The interesting middle is per-*set* slots rather than
   per-node: reconcile at the granularity of the node set, of which there are as
   many as there are concurrent conversations, not as many as there are machines.
   That is a handful of copies rather than 62,500.
2. **Read Shapiro et al. before building.** Especially on delta-state CRDTs,
   which exist specifically to avoid shipping whole states and which this summary
   does not cover.
3. **Check the assumption underneath all of it**: that two sets learning from
   different conversations *should* converge. Under C4 a node that has seen
   different data legitimately knows different things, and forcing agreement may
   be discarding exactly the specialisation that makes a distributed system worth
   having. **That is a goals question, not a distributed-systems one**, and it is
   not answered here.

## What this does NOT say

That reconciliation is solved. It says the operation we happen to perform is
mergeable in principle, that the obvious merge is the wrong one, and that the
safe merge has a cost which has to be bought down. All three are from a summary,
and the papers remain unread.
