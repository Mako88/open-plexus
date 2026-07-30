060 — The CLUTRR floor is not chance, and it is 0.500 at two hops
=================================================================

**Status:** measured, local probe, one seed. **The G0 acceptance test applied to an
external benchmark, run before any composing mechanism, which is the whole point.**

---

## IN PLAIN TERMS

Before asking whether the model can reason over CLUTRR, ask what a model that
*cannot* reason already scores. Anything a non-reasoning model gets, a reasoning model
must beat before it has shown anything.

Guessing at random scores 0.05. **A model with composition switched off scores 0.09
overall — and 0.50 on the two-step puzzles.** So a later result of, say, 0.15 would
look like reasoning and would mostly be a trick: the puzzles get longer as they get
harder, the model can see how long the puzzle is, and the answers are distributed
differently at each length. Guessing well by length is not reasoning.

**This is the single most useful thing to know before spending a run**, and it is the
mistake that cost the predecessor project a year: it measured learning rules against a
ceiling that was never there.

---

## The measurement

Trained on all 9,074 training puzzles, scored on the 713 test puzzles with no repeated
entity (note 059's primary arm). `hops=1`, so the model **cannot compose** — every
answer is prior plus direct recall.

    chance (1 of 20 relations)          0.0500
    majority-class baseline              0.0421
    hops=1 model, overall                0.0898

    hops    n     hops=1 accuracy
       2   38            0.5000
       3  105            0.1429
       4  177            0.0113
       5  131            0.0458
       6   63            0.0794
       7   61            0.2131
       8   61            0.0000
       9   45            0.0667
      10   32            0.0312

**The majority-class baseline is BELOW chance** — 0.0421 against 0.0500 — because the
commonest training target is not the commonest test target. That is worth knowing on
its own: the two splits have different answer distributions, so anything that learns
the training marginal is mildly *mis*calibrated for the test set.

## Why a non-composing model reaches 0.500 at two hops

**Sequence length leaks the hop count.** A two-hop puzzle is 11 tokens; a ten-hop
puzzle is 43. The store's state at the query position therefore differs systematically
with depth, and the answer distribution differs with depth too — two-step compositions
are a small, skewed set. So the readout can condition on depth without being told it,
and answer the depth-conditioned marginal.

That is not a defect in the loader or in CLUTRR. **It is the ordinary reason a
benchmark needs a floor measured rather than assumed**, and it is invisible if the
comparison is against chance.

## What this decides

1. **Report against the `hops=1` floor PER HOP BUCKET, never against chance.** The
   floor is 0.500 at two hops and 0.011 at four. A single overall number compares a
   model to an average of two very different baselines.
2. **The two-hop cell is where a composing mechanism has to prove itself**, and it has
   to beat **0.500**, not 0.05. It is also the cell with the fewest rows (38), so it
   is the one most likely to produce a flattering accident.
3. **Four to ten hops is where the headroom is** — the floor there runs 0.011 to
   0.079, so there is real room, and it is the range CLUTRR was built to test.
4. **No matrix is needed yet.** Training on all 9,074 puzzles took **six seconds**.
   The rule about dispatching sweeps to Actions is about jobs that hold a machine;
   this one does not, and pretending otherwise would be ceremony. When an arm needs
   many seeds and configurations that changes, and the cost gets stated then.

## What it does not say

**Nothing about whether this project can do CLUTRR.** No composing configuration has
been run. `hops > 1` with `context_keys` needs a typed hop, and CLUTRR's relations vary
along the chain — which is decision 162's problem, and `hop_relations` supplies a
schedule the task does not, so a fixed schedule here would be a fitted constant. **The
mechanism question is open and this note deliberately does not touch it.**

**And one seed.** Rule 3 says reproduce before believing, so the floor is a bound to
re-measure rather than a constant. The 0.500 at n=38 in particular is one cell of
thirty-eight rows on one seed.
