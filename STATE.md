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

**And decision 112 says width fixes it outright:**

    as configured   0.915      no decay   0.927      no cap   0.915
    width 128       1.000      width 256  1.000

Decay costs 0.012, the cap costs 0.000, width closes the gap completely.

> **Why 112 has not already unblocked everything.** Kinship is 45 tokens and
> **44 bindings** — *under* the ~96 that decision 109 measured as width 64's
> capacity, which predicts ~0.99. So kinship's bindings are harder than random
> ones at the same load, and **why is unmeasured.** Its keys are hashed pairs and
> its values repeat heavily; either could reduce effective capacity.

**This is item 1 below and it is the whole of the critical path.**

---

## Open work, in order

### 1. Re-run the relational tasks at width 128 and 256

Decision 112 measured fidelity in isolation. Nothing has re-run `kinship.py` or
the multi-appearance chain layouts end-to-end at a width where fidelity is 1.000.
If the compounding argument is right, three of the four failed mechanisms above
become worth revisiting *in that order*, starting with the accumulator (102),
which matched the one-hop model exactly and was diagnosed as reading a hop that
carried no information.

Two things to settle in the same pass, both cheap and both currently guesses:

- **Why kinship's bindings are harder than random ones at the same load.** Hashed
  pair keys and repeated values are the two candidates.
- **What width costs on the wire.** Nodes ≈ width ÷ 16 (below ~16 dimensions a
  node stops having a standalone opinion), so width 256 is ~16 nodes, and hops
  multiply the reads. The bandwidth arithmetic below was measured at width ≤ 128
  with no hops.

### 2. `carry_store` + `hidden` — the cheapest unclaimed win

Decision 116, on the notes corpus:

    chunk    linear   linear+carry   hidden 128   hidden+carry
       64     6.024          5.765        5.574          5.140
      256     5.914          5.755        5.393          5.137

**Superadditive** — 0.26 and 0.45 alone, **0.88 together**. `carry_store` is off
by default, and its docstring says it is correct when consecutive calls carry
consecutive text, which is the text case. It has never been the default because
it was measured as harmful under the *shuffled* chunk order the corpus
experiments use. **The honest version needs sequential chunks**, and that is a
different experiment from the one that refuted it.

### 3. A relational self-supervised objective

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

### 6. The readout still violates C1 — and it may not, under the amendment

`answer = parts.sum(0)` sums across every partition. Known since note 009 §4,
still outstanding, and it is a current bug rather than a design question.

**But C1 was amended** (GOALS §3): the test is now whether progress stalls when a
participant is slow or gone, not whether a sum happens. This is 64 floats per
group per step. **Re-examining it costs a reading, not a run**, and either
retires the violation or states precisely which clause it fails. Cheapest item on
this page.

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

- **The Docker testbed is not in CI.** Built, validated bit-identical over 80 ms
  ± 20 ms losing 2%, and no workflow runs it. Add churn to it when wiring it up —
  killing a container mid-run is the one thing a single process cannot honestly
  simulate.
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
3. **`reward_recall`'s layout leak.** The nearest binding before a reward is
   always the rewarded one, 160/160. Measured as **inert** — binding-detection is
   too weak to exploit it past delay 1 — so this is correctness, not urgency. The
   one-line fix (randomise the gap) does not work; the fix that would work
   changes what the task *is* and invalidates nine sweeps' comparison set.

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
| Search / beam over branches | You cannot search your way out of noisy primitives, because the verifier is built from the primitives. +0.03 for k² the retrievals. **Right answer to branching ambiguity, wrong sequencing** — revisit the moment fidelity moves | 111 |
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
- **Six checks before every commit:** `mutate.py --verify`, `mutate.py --changed`,
  `unittest discover`, `check_workflows.py`, `check_rails.py`,
  `check_duplication.py`.
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
