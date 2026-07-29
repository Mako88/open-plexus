# 043 — What concept partitioning would actually be

**IN PLAIN TERMS.** The machines currently split the model by *slicing every
memory into pieces* — each machine holds a thin slice of everything. The
alternative is to split it by *subject*: each machine owns some concepts and
everything known about them.

That sounds like a tidiness question and it is not. Under the current split,
adding machines makes each one's slice thinner and the total amount the system
can remember stays the same. Under the other, adding machines adds room.

---

## Why this is the live work

Decision 133 refuted the simple version of note 042's item 1. A persistent store
is worth **0.08 bits at every scale** and does **not** move decision 63's wall,
because its norm reaches equilibrium at 0.4 whatever the corpus size:

> A decaying persistent store is a fixed-size cache, not a map. Persistence adds
> **lifetime**, not **capacity**.

So the wall is a **capacity** limit. A `d × d` store holds ~d² bindings
(decision 109) however long it lives, and 16,000 characters is where that plus a
`vocab × d` readout runs out of room.

**Concept partitioning is the only proposal on the page that adds capacity as the
corpus grows.**

## What each thing is

**Now — partition by DIMENSION.** The store is one `d × d` matrix cut into rows.
Node `k` holds rows `[kw, (k+1)w)` and computes `M_slice @ key`, which needs the
**whole key**. Every node participates in every read, and the results are
concatenated or voted.

    total capacity     ~d^2, fixed
    adding a node      each slice gets THINNER; the total does not grow
    a read             every node, every time
    losing a node      every concept degrades a little

**Proposed — partition by CONCEPT.** Node `k` owns a set of entities, and holds
the bindings whose key is one of them.

    total capacity     nodes x per-node capacity
    adding a node      more concepts fit
    a read             the node that owns that key, not all of them
    losing a node      the concepts it owned go entirely; others untouched

### ⚠ The capacity argument does NOT survive the arithmetic, and I wrote it anyway

The first version of this note said concept partitioning is *"the only proposal
that adds capacity as the corpus grows"*. **Worked through at fixed per-node
memory, that is not true.**

Decision 109 measured capacity growing roughly as `d²`. So:

    concept        N nodes, each a w x w store
                   per-node memory  w^2          total capacity  N x cap(w)

    dimension      N nodes, each (d/N) x d, with d chosen so per-node
                   memory also equals w^2  =>  d = w x sqrt(N)
                   total capacity  cap(w x sqrt(N))  ~  N x cap(w)

**The same.** Capacity per unit of memory does not distinguish them, and the
claim as written was an assumption dressed as an inference.

**What survives is narrower and still decisive.** Holding per-node memory fixed,
dimension partitioning forces `d = w√N`, so each node holds `w/√N` **dimensions**
— which *shrinks* as nodes are added. g4-01 measured the floor: below ~16
dimensions a node has no standalone opinion (16 → 0.949, 8 → 0.681, 4 → 0.412).

> **Dimension partitioning trades per-node WIDTH for node count. Concept
> partitioning trades per-node CONCEPT COUNT and keeps width whole.** Width is
> the thing with a hard floor; concept count is not.

So the real argument is about **where the floor bites**, not about total
capacity — plus the three properties below, which were never in doubt: read cost,
concurrency, and churn shape.

**Recorded rather than quietly fixed**, because the overstated version is what I
would have built on. Decision 133 refuted note 042's item 1 for the same reason:
a plausible account of a measurement, asserted before it was checked.

## The design decision that makes it work, and it is not obvious

**Superposition WITHIN a node; selection ACROSS nodes.**

The tempting version is a distributed key-value store: each concept gets its own
slot, reads are exact lookups. **Decision 119 rules that out** — it measured the
superposed store beating a bounded cache **by a factor of eight** when bindings
exceed slots, because the store holds far more than its size, degraded, where a
cache holds its slot count and then fails.

So a node keeps a small superposed store over the concepts it owns. It degrades
gracefully when overloaded, exactly as today, and **capacity scales with node
count** because there are more stores rather than thinner ones.

That is the synthesis decision 119 and decision 133 jointly point at, and neither
alone would have found it.

## What it does to the constraints

**C1 gets BETTER, which is worth stating plainly.** Today a read is a collective:
every node computes and the answer is pooled — note 009 §4's outstanding item,
and what decision 128's deadline works around. Under concept partitioning a read
goes to **the one node that owns the key**, so there is no collective to bound.
The pooled decode does not shrink; it disappears.

**C3 gets WORSE per concept, and this is the real cost.** Losing a node under
dimension partitioning degrades everything slightly; under concept partitioning
it removes some concepts entirely. That is a sharper failure and it needs
replication — each concept on `r` nodes — which is a solved problem in a
literature GOALS §6.2 has listed as **unread since the project began**:
consistent hashing, DHTs, and the replication factor that comes with them.

**Concurrency is solved rather than mitigated.** STATE.md's d²-per-conversation
problem exists because every conversation needs a full `d × d` working store. A
conversation that touches a subset of concepts touches a subset of nodes, and the
per-node cost is proportional to the concepts in play rather than to the model's
width.

## Routing, which is the piece that does not exist yet

To read `key(c)` a node must know who owns `c`. With `derived_keys` the key is
rebuilt from the token id, so ownership can be `hash(token) mod nodes` — no
directory, no coordinator, and a joining node can compute its own share.

**That is consistent hashing**, and the reason to name it is that the naive
version has a known defect: `mod nodes` reshuffles *everything* when the node
count changes, which C3 makes a constant event. The literature's fix is a hash
ring. **Read it before building it** — note 005 exists because a borrowed claim
that gated a design decision described a variant this project cannot use.

## What would decide it, before anything is built

**Decision 109's capacity probe, re-run against node count — and pointed at the
corrected question.** It measured bindings held at 90% recovery: width 32 → 16,
width 64 → 96, width 128 → 384. **That probe was another inline one and left no
script**, so it has to be rebuilt anyway.

The question is not "does capacity grow with nodes" — the arithmetic above says
both arrangements scale the same per unit of memory. It is:

> **At fixed per-node MEMORY, which arrangement holds more bindings at 90%
> recovery as node count rises — and where does each one fall off?**

The prediction that follows from g4-01's floor: **dimension partitioning
collapses once `w/√N` drops under ~16 dimensions, and concept partitioning does
not**, because its nodes keep full width and simply own fewer concepts.

If both curves fall off together, the floor argument is wrong too and concept
partitioning is left with only the read-cost, concurrency and churn arguments —
which are real, but are engineering rather than capability.

**That measurement comes first.** Two of the last three architecture claims were
refuted by their own falsifiers, and both times the refutation arrived cheaply
because the falsifier ran before the build.

## What this note does not settle

Whether the model can *learn* through a concept-partitioned store — only whether
the store can hold more. Capacity is necessary and not sufficient, and decision
133 is the reminder: the last mechanism that looked obviously right moved the
level and not the slope.
