# State — open questions and work in flight

**This is the only document in this project that is kept current.** Everything in
it is either live work, an open question, or a standing agreement. When something
here is settled it leaves, and an entry goes in [DECISIONS.md](DECISIONS.md).

Three documents, three jobs:

| document | what it holds | when to read it |
|---|---|---|
| [GOALS.md](GOALS.md) | what the project is for, the constraints, what would refute it | before deciding whether a mechanism belongs here at all |
| [DECISIONS.md](DECISIONS.md) | a chronological log of what was chosen and why — **history, never rewritten** | when you need the reasoning behind a specific past choice, looked up by entry |
| **STATE.md** (this file) | what is true now, what is open, what is running | first, and every session |

If this file and DECISIONS.md disagree, **this file wins**. If this file does not
mention something in the log, that thing is closed.

---

## IN PLAIN TERMS

The project is trying to build a neural network that runs across ordinary
people's computers over the ordinary internet, instead of inside a data centre.

The current experiment is about **reasoning over facts** — being told "A is B's
parent" and "B is C's parent" and answering a question about A and C. The model
can now do this when the facts form a simple chain. It cannot do it reliably when
a person appears in more than one fact, which is what real facts look like.

**The reason has been found and it is boring in a good way:** the memory is too
narrow. Made wider, it stops making the mistake. Nobody has yet re-run the real
task at the wider setting, and that is the next thing to do.

---

## THE BLOCKER: retrieval fidelity, and it is a width limit

Every end-to-end relational result is capped by how often a single retrieval is
right. **Four mechanisms have failed against the same number**, each correct in
itself:

| mechanism | decision | reached |
|---|---|---|
| the accumulator (hold both retrievals) | 102 | matched the 1-hop model exactly |
| pair keys, beyond their own collision | 105 | unusable with hops at all |
| traversal (a hop that builds pair keys) | 107 | +0.05 over a broken one |
| search (generate and verify) | 111 | +0.03 for k² the compute |

The number: **0.915** when an entity appears in one fact as a subject, **~0.35**
when it appears in several. Three chained retrievals at 0.7 compound to 0.46,
which is every end-to-end kinship result.

**Do not build a fifth mechanism on top of this.** All four were measured before
being built, which is the only reason three were never written.

**Decision 112 said width fixes it outright:**

    as configured   0.915      no decay   0.927      no cap   0.915
    width 128       1.000      width 256  1.000

### ⚠ REFUTED ON THE TASK — g13-01 landed, decision 121

Full table in
[the sweep record](experiments/sweeps/g13-01-does-width-fix-fidelity.txt). What
is live: **a fourfold width increase buys 0.020 and saturates.** Out-degree 1 is
perfect at width 64 already, so there was never anything there for width to fix,
and **decision 112's 0.915 was never a bound on task performance** — it ablated
raw retrieval where this trains `Wo`, and a linear readout recovers the argmax
from a retrieval that is not itself clean.

**Everything left sits at out-degree ≥ 2, just above 1/k, and no width closes
it.** The blocker is decision 108's **ambiguity**, not capacity.

---

## Which instrument, and why — asked by John 2026-07-28

Six task modules exist and that is two too many to be honest about. The split:

| task | role now |
|---|---|
| **`closure.py`** | **THE PRIMARY INSTRUMENT.** Unmarked stream of facts, some implied by others. Matches the stated goal — relational, no question marker, self-supervised in form. Passes G0 (decision g14-01): entailed headroom 0.277 against a frozen 0.000 |
| `kinship.py` | **the mechanism testbed.** Marked questions, so it isolates a mechanism cleanly — the whole search line (g13-01…05) is measured on it and those numbers stay comparable. Kept for that, not as the goal |
| `chains.py` | solved at 1.000, out-degree 1 by construction. A control, not a target |
| `mqar.py` | G0–G5 were passed on it. History; not a live instrument |
| `corpus.py` | the text line, closed by decisions 115 and 118 |
| `reward_recall.py` | **retired** (John's call, decision 126) |

**The gap I should name rather than let sit:** *everything above is
self-designed.* **CLUTRR is the only external benchmark that would make a number
comparable to anyone else's, and it has been "next" for several cycles without
being run.** `kinship.py` borrows CLUTRR's design and says in its own docstring
that calling a number here a "CLUTRR score" would be wrong. Until that runs, every
result is this project grading its own homework.

## Open work, in order

> **PIVOTED TO ARCHITECTURE, 2026-07-28.** John: *"we're gonna have to redo all
> the tests anyway once some core pieces change, so let's get the core pieces
> right first."* Component work is paused, not abandoned — items 2 onward below
> are the queue it resumes into.
>
> [Note 042](docs/notes/042-an-architecture-pass-before-more-component-work.md)
> is the pass. Its finding: **the model has nowhere to keep a concept map.** The
> store is rebuilt every sequence and the only durable parameter is one
> `vocab × d` linear map (decision 62) — one fact that explains decision 63, 115
> and g14-01 at once.

### 0. THE ARCHITECTURE LINE — where the work actually is

**Approved by John: items 1 and 2 of note 042.** They are the same design seen
from two sides — a persistent store partitioned by concept — so building either
alone means building it twice.

| | change | status |
|---|---|---|
| **0a** | **persistent slow store** | built; **falsifier still not answered** |
| **0b** | **concept partitioning** | blocked on 0a, deliberately |
| 0c | content-derived keys | not started; every key is a random draw, so the store has **no notion of similarity at all** |

**0a's falsifier is decision 63's 16,000-character wall**, and two runs have
measured the *instrument* rather than the hypothesis:

- **g15-01 first pass (decision 131)** — the slow store's norm was pinned at its
  cap from the smallest data point. It tested a **saturated** store.
- **cap sweep (decision 132)** — every cap pinned exactly, because `lasting` has
  only `+=`: the fast store brakes with `memory *= decay` and the slow one had
  no equivalent. Note 018's defect, mirrored. Fixed with `lasting_decay`, and a
  brake alone was not enough — **the write rate was ~100× too large**, tuned for
  a store that gets rebuilt rather than one that persists.

`persist-slow` (consolidation 0.005) and `persist-slow-decay` are the first
settings where the store tracks the corpus instead of saturating. **That run is
the first time the question actually gets asked.**

> **If the wall does not move with the store genuinely accumulating and the gate
> firing tens of thousands of times, that is a real refutation** — note 042's
> account would be wrong and the proposal needs rethinking rather than retuning.

### 1. ✅ CLOSED — the search line landed. Decision 130

    concat      0.327    what we had -- BELOW the 0.466 shortcut floor
    walk        0.596    pair-key traversal, which decision 107 declined
    search4     0.604    search everywhere, which decision 111 declined
    gate-q50    0.624    search where it helps  (+0.020 +/-0.005 over search4)

**The gate keeps `search4`'s accuracy at out-degree ≥ 2 exactly (0.539) and
recovers most of `walk`'s at out-degree 1** — the trade g13-03 said was
available. Five of five predictions confirmed.

Both refusals — 107 and 111 — were correct arithmetic on the numbers of their
day, and both conditions were measured away before anything was rebuilt.
**Nothing had to be undone**, because both declined to *build*.

> **The threshold generalises; the number does not.** `gate-q50` fires at a
> margin of 0.663 at width 256, and that constant is not the mechanism — it is
> the median of the model's own training margins, computed without labels and
> without touching the test set. Width-dependent (`docs/SCALE.md`); this is a
> width-256 result.

**Still unaccounted for: 0.624 against g13-02's retrieval-chain ceiling of
1.000.** Nothing decomposes that gap. Composition on top of clean retrievals is
still inherited from decision 102 rather than re-measured, which is the most
likely place for it to hide.

<details>
<summary>How the line got here (superseded detail)</summary>

### BUILD SEARCH — its blocking condition is measured gone

Decision 111 refused search on one ground: *"you cannot search your way out of
noisy primitives, because the verifier is built from the primitives."*

**g13-01 measured the primitive at 1.000 (±0.000, 8 seeds) at out-degree 1.** A
verifier built from a retrieval that is right every time is trustworthy. The
refusal was conditional and **the condition has expired** — this is sequencing
catching up, exactly as decision 111 said it would ("revisit it the moment
retrieval fidelity moves").

What remains is ambiguity: at out-degree ≥ 2 the store returns *a* relation the
subject genuinely holds, and nothing in the question says which one leads to the
target. Search is the mechanism that resolves that — try a branch, retrieve its
endpoint, check it against the asked object.

**The ceiling is now measured, and it justifies the build** (g13-02, decision
122, 8 seeds, five of five predictions confirmed):

    step 1 at out-degree 1   1.000     search's job is to get here
    step 2 at a unique pair  1.000     0.971 overall; decision 107's 0.960 reproduces
    step 3 at out-degree 1   1.000     same operation as step 1
                             -----
    traversal with search    1.000     against the 0.87 that justified it

**The asymmetry is why it works.** Step 2's ambiguity is 5.1% of sequences where
step 1's is 50% — a `(subject, relation)` pair names one person almost always,
where `(FACT, subject)` names one of several relations half the time. The
traversal's weak steps are its two ends and its middle is sound, which is exactly
what makes a verifier built from step 2 trustworthy.

### 1a. BUILT, and not yet wired — decision 123

`openplexus/search.py` exists, with 10 tests and 2 mutations, both caught. It
takes the top `b` candidates from the first decode, **commits** to each, walks
the graph, and scores each walk by whether its endpoint matches the object the
question names — the disambiguator that was in the question all along and that
nothing had ever used.

**The wire cost is answered and it is affordable** (`tools/search_cost.py`):

    branches   decodes   x greedy   positions/s   (1024 nodes, depth 2, 10 Mbps)
           1         4       1.0x        39,062
           4        13       3.2x        12,019
          16        49      12.2x         3,189

Beam 4 costs **3.2×** the decode traffic and still supports ~12,000 answered
positions per second. Depth is harsher and only mildly: 3.2× at depth 2 to 3.7×
at depth 5. **Bandwidth is not what binds search.**

> The pooled decode is a collective, and note 009 §4 has carried that as an
> outstanding C1 item since long before search. Search does not create it — the
> readout already requires it — but it makes it `b(2d-1)/d` times more frequent,
> which raises the stakes on item 6 below.

**It has never seen a generated sequence.** The tests run on a hand-built store
of four facts. The unit test says the mechanism is correct; whether it survives
distractors, decay and a cap is the next measurement.

### 1b. MEASURED — g13-03, decision 125. Traversal is the win; search needs a gate

Full table in
[the sweep record](experiments/sweeps/g13-03-does-search-pay.txt). What is live:

- **Traversal is worth +0.269** and clears the 0.466 first-relation floor that
  nothing on this task had cleared. Decision 107 declined it at a costed "+0.05";
  that verdict did not survive the primitives moving.
- **Search overall is a tie** (+0.008 ±0.018) and the split says why:
  **−0.054 at out-degree 1, +0.092 at out-degree ≥ 2.** It does exactly what it
  was built for and damages the case it was not, and the test set is half of
  each.
- **`search8` is 0.024 WORSE than `search4`, at 6 SE.** "Search wider" is not the
  way to close the gap.

### THE NEXT MECHANISM: gate the search on ambiguity — signal MEASURED

**g13-04, decision 129: yes, at width ≥ 128.** The decode margin — the gap
between the first decode's top two candidates — separates ambiguous from
unambiguous at **AUC 0.803**, against decision 93's 0.628 for identity-free
confidence signals fitted *with* the labels.

    decode margin      d64 0.710    d128 0.841    d256 0.858
    endpoint margin    d64 0.480    d128 0.447    d256 0.448

Two things to carry into the build:

- **The expensive signal is below chance.** The endpoint margin — available only
  after paying for the walks — is *anti*-correlated, so a gate must decide
  **before** walking. That is also the cheap direction; both arguments agree.
- **It is width-dependent** and belongs in `docs/SCALE.md` as such. A wider store
  holds a cleaner superposition, so a peaked decode gets more peaked and a
  contested one more contested. Sound at 256, weak at 64.

**Build it: walk greedily, branch only where the margin is narrow.** A perfect
gate is worth roughly **+0.03 over search-everywhere** plus the walks saved.

> **The threshold is the honest problem.** AUC measures separability across all
> thresholds; a gate needs one, and picking it on the test set would be fitting a
> number rather than measuring one. Use a held-out split, or derive it from the
> decode's own scale. **And the number to beat is `search4`'s overall, not
> `walk`'s** — a gate that merely matches search-everywhere has bought compute
> savings and no accuracy.

Also still open: **re-measure composition** rather than inheriting decision 102's
1.000, which was taken on a different configuration.

### 1b. Two loose ends from g13-01, both cheap and both unexplained

- **`hop2-concat` gains MORE from width than the primitive does** (+0.051 against
  +0.021), from a far lower base. That is backwards from the compounding story
  and nothing accounts for it.
- **`hop2-concat` is below the floor that matters** — 0.327 against a
  first-relation floor of 0.466. Decision 102 recorded concat *matching* the
  one-hop model; on this instrument it loses to the one-hop shortcut.

</details>

### 2. `carry_store` — two measurements with OPPOSITE SIGNS, and nobody has reconciled them

Decision 116, notes corpus, train-then-test — `carry_store` **helps a lot**, and
superadditively with `hidden` (0.26 and 0.45 alone, **0.88 together**):

    chunk    linear   linear+carry   hidden 128   hidden+carry
       64     6.024          5.765        5.574          5.140
      256     5.914          5.755        5.393          5.137

Decision 117, Shakespeare, prequential, 250k chars — `carry_store` **hurts**:

    model, hidden 128                  5.665
    model, hidden 128 + carry_store    5.737

**An earlier version of this document called it "the cheapest unclaimed win",
citing 116 and not 117.** That is the same error the 2026-07-28 restructure was
about — quoting one measurement as current while another qualifies it.

The two differ in corpus, vocabulary, regime *and* chunk order, so neither
refutes the other and no one-line fix is available. The discriminating
measurement is a 2×2 — `{carry off, on} × {shuffled, sequential chunks}` — in
**one** regime, on Shakespeare, prequential. `carry_store`'s own docstring says
it is correct only when consecutive calls carry consecutive text, so chunk order
is the hypothesis and it has never been the swept axis.

Needs a committed instrument, same as kinship did.

### 3. A relational self-supervised objective — RAISED, on John's question

> *"Would it make sense to move this up higher, since it seems like it might
> shift a lot of things?"* — John, 2026-07-28. **Yes.** GOALS §1.2 now records
> the objective as the project's thesis rather than an implementation detail,
> and §5's recorded candidate (next-INPUT prediction) is marked as contradicting
> it. Everything below this point is measured under an objective the goals no
> longer endorse, which is exactly the "old assumption still being acted on"
> failure mode. It moves above the housekeeping and below only the two items
> that block it mechanically.

All-position (next-token) training was never required by the goal — it was
imported from how LLMs train, and it costs composition 1.000 → 0.40. Decision 98
stopped the *decay* by giving the gate its own objective (`which_hop`); it did
not close the level.

**Masked-link prediction** — state facts, hide one, predict it — is
self-supervised without marked questions, and relational rather than sequential.
That is much closer to what the task is about. Not built.

### 4. External benchmarks, so the numbers mean something to someone else

**CLUTRR** is the direct external check on our 0.992 zero-shot depth result
(train short chains, test longer). Then **bAbI task 2**, and knowledge-graph link
prediction. Keep bits/char as a diagnostic that the substrate works, not as the
score that matters.

### 5. A C4 test that the model cannot already pass

**C4 — perpetual learning — is still untested**, and two attempts to build a case
where continued learning helps both failed: decision 91 (a departure costs
capacity, and capacity is not something learning rebuilds) and decision 92 (the
mechanism already generalises). Neither says perpetual learning is worthless.
Both say **this task is too easy to need it**.

Related and unbuilt: **replay**. C4 forbids stopping, not revisiting (decision
78), and replay is one of the few known answers to the catastrophic forgetting
C4 makes first-class. A bounded buffer of past chunks, resampled. Cheap to try.

### 6. RESOLVED — and the real C1 gap is somewhere else entirely

**The sum is not the problem.** `answer = parts.sum(0)` is the numpy reference's
convenience. The deployed path sends each node's **argmax in 8 bytes**
(`combine="vote"`), and `distributed.py:419` says why that is different in kind:
*"Absence costs a voter, not a term of a sum, which is why this degrades where
summing amputates."* Bounded bytes per hop, and a missing node degrades the vote.
**Amended C1 is satisfied by the wire format.**

#### ⚠ But the DRIVER has no failure detector, and that IS a barrier

`distributed.py:427` settles a step only when it has a vote from every node it
expects:

    while settled < sent and pending[settled][1] >= expected[settled]:

A **declared** departure works — `absent` and `leave_at` adjust `expected`, which
is what g12-02 measured across 18 cells with no hang. An **undeclared** one does
not: the step never reaches its count, the window fills, the driver stops
sending, and 30 seconds later `select` raises `TimeoutError`.

**That is precisely what amended C1 forbids** — a barrier that stalls when a
participant is slow or gone. And C3 says departure is the normal case, arriving
without warning.

**BUILT (decision 126).** `run(deadline=...)` settles a step after a stated wait
with whatever votes arrived; off by default, because it costs bit-identity — the
property G2 was passed on. A node terminated without warning now leaves the run
running. Two related bugs fell out: a send to a reset peer propagated, and **a
reset was never treated as a hang-up at all**, so on any platform reporting a
dead peer as a reset the existing hang-up branch never fired.

**Still short of SWIM, and note 039 — now read from the paper — says how.**
Detection runs on the data path rather than a probe channel; there is no indirect
probing, so a slow node and a gone node are indistinguishable; and the driver is
the sole detector, a coordinator by another name. Suspicion-with-recovery is in.

**MEASURED — g12-04, decision 128. `d_max` ≈ 640 ms.**

    clean                            p50   0.61   p99   2.54   3xp99     7.6
    delay 80ms jitter 20ms loss 2%   p50  87.22   p99 211.88   3xp99   635.6

Full table in
[the sweep record](experiments/sweeps/g12-04-what-is-the-round-trip.txt). This is
simultaneously the C2 asynchrony bound and the C3 churn timeout — note 003's "two
constraints, one parameter" — and the first time either has been a number rather
than a count of steps. **A floor from six links, not a universal constant.**

**Next: replace `RETRY_AFTER_STEPS` with a duration.** Eight steps is under 3 ms
on the clean link and several seconds on the worst — one constant meaning two
things three orders of magnitude apart.

Two things worth carrying forward from that sweep:

- **Quote the p99−p50 gap, not the p99/mean ratio.** Once a fixed delay
  dominates, mean and p99 converge (1.01× at delay 80) because a constant moves
  both. The gap is what a timeout must cover: 1.0 → 16.0 → 124.7 ms as jitter
  then loss are added.
- **Loss is multiplicative with delay, not additive.** 2% loss alone is
  invisible; the same 2% on an 80 ms link doubles the p99, because a retransmit
  costs a round trip.

> SWIM also achieves **≤135 bytes per packet regardless of group size**, by
> separating detection from dissemination. That is amended C1's requirement met
> in a published system — an existence proof, not a trade-off to haggle over.

**Every churn result in the project was measured with departures announced in
advance.**

### 6b. CONCURRENCY COSTS d² PER CONVERSATION, and that inverts the usual picture

Raised by John on 2026-07-28: *"assuming ~65,000 nodes and a chat interface,
would we need another 65,000 nodes for each concurrent interaction?"*

**No — but the reason concurrency is expensive is worse than node count.** Read
from `openplexus/distributed.py`, a node holds three things:

    values    vocab x own.width     shared parameter, read-only
    readout   vocab x own.width     the learned parameter, shared
    memory    own.width x d_model   PER-SEQUENCE working state

The parameters are shared across conversations; only `memory` is per-conversation
— its docstring says so directly, *"per-sequence working state, not a
parameter"*. So a second conversation needs a second store, not a second network.

**The arithmetic is the problem.** Per node the store is `(d/P)·d`, so across the
network it is **d² per conversation**. At width 1M, and at the float64 the code
actually allocates, that is **~8 TB of aggregate store for one conversation** —
128 MB on each of 65,000 nodes — against a shared readout of ~6 MB per node at a
50k vocabulary.

**The per-conversation state is roughly twenty times the shared parameters.**
That is the inverse of a transformer, where weights dominate and the KV cache is
secondary, and it means **concurrency is bounded by node RAM rather than by node
count.**

#### ✅ AND IT CAN SCALE BY NODES AFTER ALL — John asked, and my first answer was too pessimistic

John's requirement, 2026-07-28: *"we can't control what nodes are actually going
to be running the code, so the requirements per node need to be as minimal as
possible, and everything that is at all possible to scale by adding nodes should
be the way we scale rather than requiring heavier nodes."*

**The store is d² in TOTAL but d²/P per node, so splitting further already
shrinks each node's share.** What stops that is the floor of ~16 dimensions per
node, below which a node has no standalone opinion (g4-01: 16 dims → 0.949,
8 → 0.681, 4 → 0.412). At width 1M that caps the split at ~62,500 nodes and
~128 MB per node per conversation.

**But concurrency does not have to reuse the same nodes.** Give conversation A to
one set of ~62,500 and conversation B to a different set. Then:

    per-node RAM        constant, one conversation's slice
    concurrency         linear in node count
    what is replicated  the LEARNED parameters, ~6 MB per node

The parameters are three orders of magnitude smaller than the store, so
replicating them across sets is cheap. **Concurrency scales by adding nodes, as
required.**

**The cost is real and it is a distributed-systems problem, not a modelling
one.** Under C4 the readout never stops learning, so disjoint node sets drift
apart — each set learns from its own conversations. Reconciling them is exactly
gossip, CRDTs and anti-entropy, which GOALS §6.2 has flagged as **unread** since
the beginning and which note 003 named as the highest-value gap.

Two things still follow, and neither is measured:

- It is the same d² that decision 109 measured capacity scaling by. A bounded
  cache is `slots × d`, not `d²` — so item 7 below is not only about churn
  tolerance, it is about how cheap a conversation is.
- **Nothing serves two conversations today.** `Node` holds exactly one `memory`
  with a `reset()`, so multi-session serving is unimplemented, unmeasured, and
  not costed. This entry is architecture read off the code, not a result.

### 7. Item-partitioning vs dimension-partitioning

`partitions` splits the store by DIMENSION, so every node computes the same
`M_slice @ key_slice` and **inherits the sum**. Partitioning by ITEM makes a read
a SELECTION across nodes. It is also partial-tolerant by construction: lose a node
holding dimensions and the retrieved vector has holes; lose a node holding items
and you take the best of whoever answered.

Decision 61 opened this and decision 119 bears on it — the superposed store beats
a bounded cache by a factor of eight when bindings exceed slots, so "just keep
items separately" is not free.

### 8. The distributed path cannot run a gated model

`distributed.Node.step` is a **reimplementation** of the model's inner loop, not a
call into it. A config carrying gate settings is accepted, ignored, and answered
anyway — measured, with two tests pinning it. **This scopes every "the split is
exact" claim in the project**: exactness was measured on the ungated inner loop.

The fix is a step-wise API on `LocalAssociativeMemory` that the node calls, not a
second gate implementation on `Node`. The second is what will be tempting. It is
a real refactor and wants its own cycle.

### 9. Housekeeping, none of it blocking

- ~~**The Docker testbed is not in CI.**~~ **WRONG, and it was carried into this
  document from the archived backlog without being checked.** Three sweeps run
  the testbed on Actions in real containers — `sweep-g12-01`, `sweep-g12-02`
  (churn, 18 of 18 cells, nodes vanishing mid-run) and `sweep-g12-03` — plus
  `testbed-identity.yml`. **The model has run distributed across containers, in
  CI and locally.** What has *not* run distributed is the relational work:
  kinship, hops and search are single-process only, and `Node.step` still cannot
  run a gated model at all (item 8).
- **`KeySource` needs the conformance suite retrieval has** — no shape check, no
  purity check, and nothing proving the suite bites. Before any combinatorial
  sweep over keys, because a broken implementation inside a grid does not
  announce itself.
- **`mutate.py --changed` should select by HUNK, not by file.** 60 of 134
  mutations for `local_memory.py` is twenty minutes, which is the long local run
  the rule exists to avoid — so it degenerates exactly where the work happens.
- **`orthogonal_every` cannot be re-checked without being reimplemented.**
  Decision 54 refuted it as "a cure for someone else's disease" because there was
  no per-layer structure to orthogonalise. With a `hidden` readout there is, so
  the refutation may not survive. Do not bundle this into another sweep —
  implementing a mechanism and re-checking a refutation together produces a
  number nobody can attribute.
- **Per-job parallelism in sweeps.** Every job trains serially on a ~4-core
  runner. A `--workers` option cuts wall-clock by roughly the core count on every
  sweep from now on. Costed nowhere; measure before believing the factor.
- **Uneven slices.** `slices_for` refuses any split that does not divide evenly.
  Real machines will not offer round numbers, and heterogeneous node sizes need
  this first.

### 10. Self-imposed limits found in the 2026-07-28 audit

John's standing test: **the only real constraints are that it runs across devices
over the internet, and that the model is as capable as possible.** Anything else
limiting the design is self-imposed and has to justify itself. Decision 78
audited four; these are what a fresh pass found still standing.

| limit | is it real? |
|---|---|
| `hop_accumulate="concat"` is **refused alongside `hidden`** | **Self-imposed, and it costs something measurable.** Decision 116 put `hidden` at 0.45 bits, and `concat` is what lets a readout see every hop — so the best readout and the composition mechanism cannot currently be used together. The refusal says only that "the two have not been made to compose", which is a to-do wearing a constraint's clothes |
| The store is **rebuilt every chunk** | Inherited from the recall tasks, where it is correct, and never re-examined for anything else. `carry_store` exists and its two measurements disagree (item 2) |
| `orthogonal_every` refused alongside `hidden` | **Correct** — it would orthogonalise a different matrix than the one it was measured on. But it blocks re-checking decision 54, which was refuted *because* there was no per-layer structure and now there is |
| Character level | Approved for removal by decision 78 and by John again on 2026-07-28. Still not done; needs its own plan because it invalidates the comparison set |
| `hops` + `context_keys` | **Was** self-imposed past its evidence. Lifted on 2026-07-28 exactly where search supplies the pair-key walk, and it still stands everywhere else |
| `slices_for` refusing uneven splits | Self-imposed. Real machines are not round numbers |

---

## In flight

**Nothing is dispatched.** No sweep matrix is running. The most recent runs are
the pre-commit checks for decision 119.

Newest sweep records, all landed: `g12-01`, `g12-02`, `g12-03` (the asynchrony
window on a real impaired link), `g11-06` through `g11-08`.

### ⚠ An unattributed churn probe landed, and it challenges decision 119

A background probe from a previous session returned while these documents were
being reorganised. Chains, 6 chains at 2 hops, floor 0.167, fraction of the
machine removed down the rows:

    CACHE SLOTS 8          superposed    both    cache only
      0% removed                0.995   0.770        0.082
     75% removed                0.690   0.340        0.045
     fall                         31%     56%          45%

    CACHE SLOTS 128        superposed    both    cache only
      0% removed                0.995   1.000        1.000
     75% removed                0.690   0.915        0.932
     fall                         31%      8%           7%

**Decision 119 says the store wins when bindings exceed slots and *ties* when
they do not. At 128 slots against ~44 bindings this is not a tie** — the cache
holds 0.932 where the store falls to 0.690, and falls 7% against the store's 31%.
Churn is the one axis where the store's degrade-gracefully story was supposed to
be structural, and this points the other way.

**Do not act on it yet, and do not quote it.** Rule 11b: verify a run's identity
from the data before reading a number off it. This output carries **no condition
string, no script name, no seed count, and no record of a pre-registered
prediction**, and it was not launched from this session. It is a number without a
provenance, which is the exact shape of the g9-11 near-miss.

**What it needs, in order:** find the script that produced it; confirm the arms
mean what the column headings say — in particular whether `superposed` is running
with the same width and cap as the other two; then re-run it with a condition
string and seeds. If it survives that, it belongs in the log as a decision and
item 7 below (item- vs dimension-partitioning) moves up the list.

---

## Waiting on John

Listed here because they are calls that are his rather than mine — but per the
standing agreement this is **a report, not a gate**. If he does not answer, I
decide, proceed, and say which calls were made without him.

> **ANSWERED 2026-07-28, and it closes two of the three below.** John, in his
> words: *"I'm good with any functionality and/or adjustments that get us closer
> to our goals. As long as it doesn't contradict with those (primarily being:
> runs on the internet, ideally results in AGI, but works as an LLM replacement
> as a secondary goal [but when they conflict, the AGI goal takes priority])."*
>
> So **search and moving off character level are both approved in advance**, and
> the test for any mechanism is the goals themselves rather than his sign-off:
> does it run over the internet (amended C1), does it serve AGI first. Item 2
> below is no longer a decision — it is a costed piece of work whose only
> remaining requirement is that the re-baselining is planned rather than
> discovered.

1. **Input and output.** He wants to talk this through rather than have it
   decided. His framing: if the AGI goal wins, inputs should look like a body — a
   loop with consequences, not a passive feed. Related work of his own:
   `Mako88/Persistence` (self-curated memory, a sensory block, scheduled
   wake-ups), and a robot project he would like to wire up. The output side is
   where C1 is already violated, so it is not purely speculative.
2. **Moving off character level.** A character bigram table is low-rank because
   English is, so part of the measured ceiling is the task — and concepts cannot
   be represented over characters, which puts it directly against the relational
   direction. **It invalidates every number in the comparison set**, so it should
   happen once, deliberately, with the re-validation costed in advance rather
   than discovered. This one needs its own plan.
3. ~~**`reward_recall`'s layout leak.**~~ **CLOSED 2026-07-28 — John: "if it's
   just a failure in a test (not the model itself), and the test is no longer
   useful, definitely just abandon it."** The leak is real (nearest binding
   before a reward is always the rewarded one, 160/160) and measured **inert**.
   The task is not fixed and not re-baselined. `reward_recall` is retired as an
   instrument: decision 119 showed it does not discriminate the mechanisms the
   g9 line measured on it, and the live work is relational. The three tests in
   `test_reward_recall.py` that pin the leak stay, now as documentation of a
   retired task rather than as a pending fix.

---

## Where the model actually is

Kept short deliberately. Full records are in `experiments/sweeps/`.

**On text** — and the headline here was wrong for a long time:

    uniform                        6.000 bits/char
    OUR MODEL, best ever measured  5.172   g11-07, best of eighteen compositions
    unigram (letter frequency)     4.829   <- NOT beaten, ever
    backprop attention, width 16   4.197   our own baseline, ~10k params
    bigram                         3.583
    char-LSTM (published)          ~1.45

    NOT THE MODEL, and a real result: MLP-128 on frozen features   4.525
    (note 037 — ordinary backpropagation, OFFLINE, deliberately)

**The unigram has never been beaten by this model** (decision 118). A line
claiming `prequential 4.540 ... unigram BEATEN` stood in the handoff for weeks and
was wrong twice over: 4.540 is note 037's offline backprop probe on frozen
features, not the model under its own learning rule, and it is not prequential.
Three independent measurements of the model agree — 5.466, 5.172, 5.665 — and
none reaches 4.829.

**What note 037 does establish is worth more than the mislabelled claim:** the
retrieval *carries* enough information to beat a unigram and a linear readout
cannot extract it. That is a statement about the features, and it is why `hidden`
exists. Whether a LOCAL rule can train such a readout is where note 036 starts.

**On relational tasks:**

    2-hop chain, fixed hops=2                 1.000   (was 0.000)
    3-hop chain, fixed hops=3                 1.000
    depths 1+2+3 mixed, gated                 1.000   on all three
    1-hop model on a 2-hop chain              0.000   <- the control still fails
    depth 3, gated, HALF the machine gone     0.928
    zero-shot transfer to an untrained depth  0.992
    chains linked end-to-start, 4 joins in 6  0.630   <- 1.000 was the disjoint case

**On scale and the wire:**

    token broadcast to all nodes            5 bytes
    each node's reply, combine="vote"       8 bytes
    per answered position, 1024 nodes      ~8 KB

A node's readout spans the whole vocabulary from its own slice, so its argmax is a
*complete opinion*, not a fragment. The binding constraint is **dimensions per
node, not node count**: below ~16 dimensions a node stops having a standalone
opinion, so nodes ≈ width ÷ 16. At width 8192 that is ~512 nodes and ~410M learned
parameters — GPT-2-large scale, not frontier scale. Measured on MQAR at width ≤
128 with no hops; outside that it is extrapolation.

---

## Do not re-propose these

Each has a measurement pinning it. **Read the decision before proposing it
again** — this list exists because several of these were proposed twice.

| proposal | why not | where |
|---|---|---|
| Anything that recovers per-item information *after* the sum | `r = M @ key` is a SUM. Readout bias, competitive retrieval, orthogonal updates and pair keys all failed for this one reason | 69, and the g11 line |
| Another mechanism on top of noisy retrieval | Four have failed against the same 0.915/0.35. Fidelity first | 102, 105, 107, 111 |
| ~~Search / beam over branches~~ | **NO LONGER ON THIS LIST (decision 121).** 111 refused it because the verifier was built from noisy primitives; g13-01 measured the primitive at 1.000 at out-degree 1. The condition expired and search is item 1 | 111, 121 |
| Transfer of the halting gate to new terminator tokens | `halt_w` sits +8.3 sd on one token's value vector. Two markers have unrelated random value vectors, so transfer is **impossible by construction** | 89 |
| A width × sequence-length sweep to explain "width doesn't help" | Nobody claims that. Our arms *do* scale with width; the flat axis is DATA. Withdrawn before dispatch after ten minutes of reading source | 112, 113 |
| More data on the text corpus | The model converges at ~16,000 characters. The store is per-sequence working memory, so `Wo` is the only durable parameter and one linear map converges fast | 63, 115 |
| Store or readout capacity as the saturation cause | ~96 bindings at d=64 scaling as d²; 2.00 readout items per dimension. Both exceed what the tasks demand | 109, 110 |
| `value_centre`, or `value_lr` as a fix for collapse | `value_lr` does not collapse at a sane rate. The values move a long way, stay spread out, and the plateau does not budge | 114 |
| Replacing the superposed store with a cache | The store wins by a factor of eight when bindings exceed slots, and ties when they do not — **but see the churn probe below, which challenges the "ties" half** | 119 |
| A composition sweep on chains as evidence about composition | A chain has **out-degree 1 by construction** — the row that already scores 0.915. Every composition result on chains was measured where no search was needed | 108 |

---

## Working agreement with John

- **Blanket permission for architectural decisions.** The pending-decisions list
  is a REPORT, not a gate. If he does not answer, decide and proceed — document it
  in DECISIONS.md and say which calls were made without him.
- **List pending decisions at the end of every response.** He reads from a phone.
- He is not deeply versed in modern ML internals. **Explain plainly, keep the
  numbers, do not hide bad news.**
- **Goal ordering:** AGI is primary; being an LLM replacement that runs on
  distributed consumer machines is secondary and must not compete with it.
- **Biology gives policies, not representations.** Biology has been a good source
  of control policies here (tagging and capture) and a poor source of
  representations (superposition, Hebbian outer products, frozen random
  projections). Take mechanisms from computer science where the problem is
  well understood.
- **Scheduled wake-ups DO NOT FIRE in his setup.** He phones into a desktop
  session, which keeps it non-idle; cron never fires, and `ScheduleWakeup` was
  tried and also did not. **What works is a persistent `Monitor`** emitting a
  heartbeat line. Do not end a turn relying on anything else.

## Standing operational rules

- Sweeps are GitHub Actions **dispatch-only** via `gh workflow run`, one matrix at
  a time, cost stated first and estimated **from the most expensive cell**.
  Nothing heavy runs locally.
- **Never use bash heredocs.** **Never `git commit -m` with backticks** — write
  the message to a file and use `git commit -F`.
- **Run `python tools/check_all.py` before every commit**, then `mutate.py
  --changed` separately. **Do not run the checks as one compound shell command** —
  a shell reports only the last statement's exit code, and on 2026-07-28 that
  reported success while two of the five were failing. `check_all.py` runs each
  as its own subprocess and fails if any fails.
- **Batch commits when a sweep is in flight** — every push queues seven check jobs
  ahead of the matrix, and a second push cancels the first run.
- **The mutation harness takes the tree exclusively.** Stopping the background
  task does not stop it: that kills the shell wrapper and leaves the Python
  process editing source. Two full check runs once passed against a tree that was
  still being mutated.

## The standard this project holds itself to

Pre-register predictions before every sweep and score them honestly, including the
refuted ones. A mechanism measured only on the task it was designed for is not
measured. When a mechanism adds state, compare against a model given the same
amount of state — g10-09 was retracted for missing exactly that.

**Probe the bottom of a scaling range locally before spending a matrix on it.**
g11-05 swept 62,500 characters upward, entirely above the model's saturation
point, so its flat exponent was guaranteed by the grid.
