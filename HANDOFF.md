# HANDOFF — scratch context for a session swap

> **TEMPORARY and OVERWRITTEN, never appended to.** Not a record; nothing durable may
> depend on it. **Nothing else in the tree may cite this file.** Cite `DECISIONS.md` or a
> sweep record instead.
>
> **Where things live:** decisions → `DECISIONS.md`. An option's history →
> `docs/options/<name>.md`. A prediction, before a run → the sweep record. A finding about
> the METHOD → a `CLAUDE.md` calibration. The readable version → `docs/explainers/`. Goal
> and refutation conditions → `GOALS.md`. Notes are RETIRED in `docs/archive/notes/`.
>
> **NO CLAIM LIVES HERE.** Every number points at the file that owns it, and if the two
> disagree that file wins.

**Written:** 2026-07-30, after the session that got #2 its first yes and answered #4.

---

## THE ORDERING, WHICH IS THE MOST IMPORTANT THING ON THIS PAGE

**Order by what is most likely to DISPROVE the project.** Not by what is hard, not by what
is ready. **Tuning is deferred until the core is proven.** Three companions in
`DECISIONS.md` standing agreements: prefer the option that SETTLES a question even when
harder; every option offered to John carries a plain explanation, pros and cons, and a
recommendation; never offer an option already known to fail the goals.

     ✅  2  representations learned LOCALLY   18 graphs, beats counting, no invariant
     ✅  6  independent nodes agree           real bug found AND FIXED (transport half)
     ✅  7  decide what to say, and decline   exact, on the case the gate can see

     🔀 10  margin survives scale             REFUTATION WAS ON THE WRONG ARRANGEMENT

     ⏸  4  multi-hop walk over real internet MEASURED; verdict needs a latency budget

     ⬜  1  relational objective buys reasoning  blocked: no instrument with a wide band
     ⬜  3  conventional system already wins     FIRST REAL BASELINE RUN, and we lose
     ⬜  5  learn forever                        the cheap route is refuted; needs
                                                per-position attribution
     ⬜  8  adjudicate contradictions            untouched
     ⬜  9  survive hostile participants         untouched
     ⬜ 11  training traffic fits broadband      G4 passes on ONE SEED
     ⬜ 12  survives a second modality           no evidence either way

**#6's ✅ is the TRANSPORT half only.** The quantiser half — do two nodes turn the same
input into the same id — is untestable because no quantiser exists. That half is ⬜.

**#10 MOVED FROM ❌ TO 🔀 ON 2026-07-30, AND THE REASON IS THE ARRANGEMENT.**
Everything below this paragraph was measured on **dimension** partitioning — every machine
holds a slice of the width and every read is a fragment summed across machines.
[`g29-02`](experiments/sweeps/g29-02-concept-partitioning-at-EQUAL-state.txt) ran `g5-01`'s
own cell with **concept** partitioning instead, where a read is served whole by the one
machine holding the fact:

    dimension   16 machines, width 256, 147,520 numbers  ->  0.7549
    concept      4 machines, width  64,  37,440 numbers  ->  1.0000

**A quarter of the memory, a quarter of the machines, and it solves what the other stalls
on.** That cannot be bought with state, which is exactly what killed the first attempt —
[`g29-01`](experiments/sweeps/g29-01-does-concept-partitioning-escape-g5s-wall.txt) ran the
concept arm at 7.7× the state and is confounded beyond use.

**🔀 and not ✅, because the concept arm is SATURATED.** Thirty cells of thirty return
1.0000 with zero spread across learning rates, so the grid cannot rank anything or fit a
slope. What is established is that G5's wall belongs to dimension splitting. **Concept
partitioning's own wall has never been looked for**, and the numbers below stand as
statements about the arrangement the project does not intend to use.

The cost table is the standing caution: concept state grows as `machines × width²` against
dimension's `width²`, reaching **7.68×** at width 256. Whatever wall it has will be paid for
in memory per machine, which is the quantity #10 is about.

**#10's ORIGINAL REFUTATION, on dimension splitting, unchanged below.**
`docs/archive/goals-results-log.md` records G5 as **resolved and failing**: usable machine
count goes as `T^-0.45`, so a ten-times-longer problem needs machines **6.6x wider** while
the machine count you can split across falls to **a third**. In its own words — *"for a goal
whose whole premise is that machine COUNT is the elastic quantity and machine SIZE is fixed
by what people already own, the elastic quantity is the one that stops helping."* Pooling
(`g5-04`) postpones the wall and does not remove it, degrading at exponent 1.94 against 0.82.

**The one escape is an ORACLE.** `g7-02`: with an oracle gate deciding what to store,
sequence length stops being a difficulty dial at all — devices holding ONE NUMBER each score
identically at 96, 192, 288 and 384 steps. **The record says outright this is a ceiling, not
a result**, and note 010 argues MQAR cannot test a real replacement because the only event
separating a pair from filler is the query, which arrives too late and never recurs.

**And the escape route is ALSO measured, and it also failed.** `g8-01` — literally titled
*"can any real mechanism replace the oracle gate"* — ran 36/36 cells with `on-use` and
`salience` arms against the oracle ceiling and the ungated floor. Its verdict: **"No
mechanism tried recovers the oracle. The largest recovery anywhere in the grid is 0.05 and
most cells are at or below zero. Every result that rests on the gate is a CEILING."**

**So #10 is a COMPOUNDED negative and should be read that way:** scale fails, the one thing
that rescues it is an oracle, and nothing real has been able to build that oracle.

**No new candidate, and I checked rather than assuming one.** The occupancy gate (`148`,
exact per `g26-01`) is the obvious thing to reach for and it is the WRONG SHAPE: the oracle
decides *is this arriving position a real binding or filler*, occupancy answers *was this
address written before*, and a first-time binding is unwritten by definition — so it cannot
separate a new pair from new filler. Proposing it would be forcing a fit.

**What would actually move #10** is a mechanism that separates a binding from filler at
WRITE time, from local evidence, which is note 010's problem and is unsolved.

**`g28-01` put a NUMBER on how good it has to be, and screening is now seconds.** A gate
must fire on real content `target * filler / ((1-target) * real)` times more often than on
filler. **That is not a constant** — at MQAR's measured 98.92% filler share a merely
half-real stored set needs **91.9x**. Best structural signal screened: `token-novel` at
**25.2x**, `addr-novel` at **18.9x**. Both a factor of four short.

**Screen before building.** Four gates were built and measured after; this costs seconds
and killed two novel candidates plus a bug in the screener itself. `g28-01` also has no
working control yet — P1 compared `pair:filler` against note 013's `query:filler` number —
so the candidate figures are recorded rather than relied on, and reproducing 7.6x on the
right class is the first job for anyone continuing.

**And the filler share is most of the problem.** At 98.9% filler the bar is brutal
regardless of mechanism, which is an argument about the INSTRUMENT rather than about any
gate. Note 010 already says MQAR cannot separate pair from filler at write time.

**10 to 12 were MISSED from the original nine and John caught it**, and then **#10 was
labelled ⬜ when the archive already had it as failed** — the same too-narrow-a-search
failure twice on one item. The list was built from an outside review plus my reading and
never cross-checked against `GOALS.md`'s gate ladder or the archived results log.

## #2 — the live thread, and the session's result

A **local contrastive rule** over relation vectors. Positives from closing triangles,
negatives from every other relation, one update per puzzle, no population statistic and no
barrier. `tools/relation_contrastive.py`, evaluated through note 070's harness.

    kinship end task, true chains     0.7821 vs random 0.6642   +0.1179, 10/10 seeds
    kinship end task, END TO END      0.6943 vs random 0.6088   +0.0855,  3/3 seeds
    16 OpenEA graphs, vs COUNTING     +0.0974 at dim 0, +0.0507 at dim >= 1

Every one pre-registered. Records: `docs/options/structured-relations.md`, sweeps
`g23-01`, `g23-02`, `g23-03`.

**It does not need a conserved quantity**, which is what `note 104` scoped the whole
composition line on. It loses on 2 of 16 graphs and the informative loss has **48 rules** —
so "beats counting" carries a data requirement.

**Still ⬜: nothing in the model uses it.** A measurement is not an adoption.

## Three claims corrected, all of them mine

- **"The one-layer reference cannot compose"** — refuted, it reaches 0.714 at trained depth.
  **The two-layer reference is PARKED**; do not build it without a better reason.
- **"CLUTRR has ~4x closure's band"** — wrong twice, once from a borrowed number and once
  from a mismatched baseline.
- **"DBpedia has no additive invariant"** — narrowed to *one 15,000-entity extract has
  none*. Three of eight V1/V2 pairs disagree on dimension (`g23-03` P4). **`dim` is a
  property of the extract**, so note 104's scoping is partly a sampling artefact.

## #1 is blocked, and the block is the finding

**No instrument here can test the premise decisively.** Closure's usable band is 0.092
(`g14-01`, 8 seeds, and its `local` arm sits at 0.108 BELOW a 0.190 base rate). CLUTRR's is
~0.285 in one bucket with five answers and heavy label skew. `g22-01` is built, costed and
dispatchable and would likely return "below the resolution of this instrument".

**Outward-looking options exist and were not checked before I concluded otherwise:** SCAN,
COGS, CFQ for compositional generalisation; **FB15k-237 / WN18RR** for relation composition.
**FB15k-237 is FETCHED** (`tools/fetch_fb15k237.py`, pinned and checksummed) under John's
standing permission for benchmark data. **But it does not hand us a published number:** the
literature measures LINK PREDICTION on it and this project measures RULE PREDICTION, so
reaching a real baseline still needs one of them reformulating.

## #4 answered, and `d_max` was the wrong bar

`g24-01` pointed `tc netem` at the peer path at last — the gap notes 094 and 101 each named.
**161 ms per round on an 80 ms link against note 101's assumed 50**, so the walk exceeds
`d_max` at **depth 2**, not depth 8. Rounds are `2 * depth` in every row, so the structure
was right and only the constant was wrong.

**New term nothing here models: with 2% loss, cost is SUPERLINEAR in depth** — 164, 271,
302 ms/round — because a retransmit costs a timeout. Depth is dearest when the network is
worst.

**And `d_max` is a CHURN TIMEOUT, not a latency budget** (my call, recorded). It was derived
as 3x a p99 for declaring a node dead. No depth is called a failure until John states a real
budget.

## #5 — the question was wrong

`g25-01`'s first run was VOID: its control failed because it modelled eviction with no
promotion gate in front of it.

**Reading `local_memory.py` is what settled it.** Consolidation fires on
`predictions[t-1] == token` — *"it promotes what the model ALREADY GOT RIGHT, so a
persistent store cannot bootstrap a model that predicts badly"*. **The durable store
receives what is already predicted, not what is used.** No record mentioned this before
today; it is now on `docs/options/use-based-eviction.md`.

**Before building here, read `g8-03` and `g8-04`:** `capture_slots` is already measured, and
*"bounding the lasting store cannot reproduce a mechanism that gates the FAST one"* — every
pool recovered approximately zero.

**And the cheap route to unblocking it is REFUTED, 2026-07-30.**
[`g31-01`](experiments/sweeps/g31-01-is-the-filler-share-ours-or-the-worlds.txt) proposed a
label-free stand-in for *"is this write worth making"* — count whether the address recurs
later — so the 92× bar could finally be measured on data this project did not generate. Its
gate failed: the oracle control reproduces at **92.0×** against `g28-01`'s 91.9×, and the
same stream counted label-free reads **0.0×–0.1×** at every granularity.

**Recurrence is not demand.** MQAR's filler is drawn from a small key range, so it recurs
constantly; the proxy calls 99.9% of the stream worth writing where the oracle says 1.1%.
No granularity closes it, so the conclusion is structural: worth-writing is a fact about
FUTURE DEMAND, no count of the symbols can reach it, and the only routes left are a task
that declares its queries — which is MQAR, where we started — or an intervention that
removes the write and sees what breaks. **That is the per-position attribution route, and
this strengthens the case for it rather than replacing it.**

One thing survives independently, and it is structure rather than measurement: **only
`openplexus/tasks/mqar.py` and `openplexus/tasks/reward_recall.py` have a `filler` position
kind, and this project wrote both.** That does not show the bar is unrepresentative — a
stream with no explicit junk can still be mostly not-worth-storing — only that it has never
been measured off a task we authored.

## #3 — the first real outside baseline, and we lose to counting

[`g30-01`](experiments/sweeps/g30-01-link-prediction-on-their-task.txt) ran the raw store on
FB15k-237's own metric: filtered tail-side link prediction, 20,438 test triples.

    store, width 256    MRR 0.0122        frequency  MRR 0.3378
    store, width 512    MRR 0.0232        chance     MRR 0.000069

**177× chance and one twenty-eighth of counting.** The opponent ranks entities by how often
each is a tail of that relation — no learning, no capacity — and it is computed here on the
same test set rather than quoted from a paper. All four predictions held, including the one
whose refutation would have been the good news.

**It does not close #3.** Link prediction is offline, global and non-local; none of this
project's constraints are exercised by it. It bounds one reading of the store on one outside
task, and it did not include the learning rule at all —
[`g30-02`](experiments/sweeps/g30-02-the-local-rule-on-their-task.txt) is that run and is in
progress.

**The informative line was the width one.** Doubling width moved the raw store 1.90×,
against 1.41× from the superposition law and 4× from capacity, so at 181× over capacity
neither model describes the read. The LEARNED arm goes the other way — width **hurts**,
0.1633 at 64 against 0.1385 at 256 — which says it is optimisation-limited rather than
capacity-limited, and that any width or K comparison before convergence is measuring
learning speed instead of capability.

## Process, and one thing that must not regress

- **CHECKPOINT ONTO A BRANCH.** `checks.yml` uses a PER-REF concurrency group, so a branch
  push cannot cancel master's run. **Eight consecutive runs were cancelled on 2026-07-30**,
  so the six mutation shards — CI-only — did not execute once. Cancel superseded checkpoint
  branches so they do not compete for runners.
- **`check_rails.py` R6**: every module in `openplexus/` and `tools/` says what it does not
  duplicate. 65 of 66 baselined; it caught its author's own new files first.
- **`check_provenance.py` was case-blind** and therefore weaker locally than in CI.
- Four near-misses this session were caught by machinery, not by care, and a fifth — that
  `tc netem` had in fact been run many times, just never on the peer path — was caught by
  John. **Search wide before concluding a negative.**
