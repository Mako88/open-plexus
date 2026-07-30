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

     ❌ 10  margin survives scale             G5 MET ITS REFUTATION CONDITION

     ⏸  4  multi-hop walk over real internet MEASURED; verdict needs a latency budget

     ⬜  1  relational objective buys reasoning  blocked: no instrument with a wide band
     ⬜  3  conventional system already wins     no real baseline yet
     ⬜  5  learn forever                        blocked: needs per-position attribution
     ⬜  8  adjudicate contradictions            untouched
     ⬜  9  survive hostile participants         untouched
     ⬜ 11  training traffic fits broadband      G4 passes on ONE SEED
     ⬜ 12  survives a second modality           no evidence either way

**#6's ✅ is the TRANSPORT half only.** The quantiser half — do two nodes turn the same
input into the same id — is untestable because no quantiser exists. That half is ⬜.

**#10 IS NOT UNTOUCHED AND I LABELLED IT WRONG.**
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
WRITE time, from local evidence, which is note 010's problem and is unsolved. Anyone picking
this up should read `g8-01` and `note 010` before writing code, not after.

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
