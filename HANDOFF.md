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

    1  relational objective buys reasoning   BLOCKED, and that is a finding
    2  representations learned LOCALLY       YES, and it does not need an invariant
    3  conventional system already wins      PARTLY -- counting gets 0.25 of 0.37
    4  multi-hop walk over real internet     NO at depth 2, and d_max was the wrong bar
    5  learn forever without wrecking it     the QUESTION was wrong; see below
    6  independent nodes agree what a thing IS
    7  decide what to say, and decline
    8  adjudicate contradictions
    9  survive hostile participants

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
COGS, CFQ for compositional generalisation; **FB15k-237 / WN18RR** for relation composition,
which would answer **#3 at the same time** because they ship published baselines. **A fetch
needs John's approval and he has not given it.**

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
