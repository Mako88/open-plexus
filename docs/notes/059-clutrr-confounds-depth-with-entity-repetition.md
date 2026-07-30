059 — CLUTRR confounds depth with entity repetition, and we already know we are bad at one of them
==================================================================================================

**Status:** a measurement on the benchmark itself, before any model has been run
against it. Nothing here is a result about this project's model; it is a property of
the instrument, found by reading the data.

---

## IN PLAIN TERMS

CLUTRR's hard test is meant to ask one question: *can a model that learned on short
chains of reasoning handle long ones?* Chains of 2 and 3 steps to learn from, chains
of up to 10 to be tested on.

**It also quietly asks a second question, and this project is already known to be bad
at it.** In the training data, no person is ever mentioned in more than two facts. In
the test data, **38% of puzzles mention someone in three or more** — and it gets worse
the longer the chain.

So a poor score on long chains would have two possible causes, and they cannot be told
apart from the score: the reasoning got too deep, or the same person appearing many
times broke the memory's addressing. **This project has measured the second problem
separately and knows it is real.** Reporting one number would have credited the wrong
cause.

The fix costs nothing and has to be decided now: **report the score split by how often
a person repeats, as well as by chain length.**

---

## The measurement

    split         rows    with an entity in >2 edges
    train         9074      0    (0.0%)
    validation    2020      0    (0.0%)
    test          1146    433   (37.8%)

    test, max appearances of any one entity
      2 edges  713    3 edges   58    4 edges  350
      5 edges    5    6 edges   19    7 edges    1

    test, the 433 by chain length
      4 hops 13 · 5 hops 43 · 6 hops 44 · 7 hops 83
      8 hops 89 · 9 hops 74 · 10 hops 87

**Training and validation are entirely free of it.** Not rare — absent. Every entity
in every training puzzle appears in at most two edges, so a model trained here has
never once had to hold a person who participates in three facts.

**And the repetition rises with depth**, which is what makes it a confound rather than
a separable axis: 13 cases at four hops against 87 at ten.

## Why this project specifically must split on it

Repeated entities are not a generic difficulty here. They are a **named, measured
limitation of the addressing**:

    103   single-token keys: 0.884 at one appearance, 0.303 at two
    104   pair keys largely fix it: 0.918 / 0.628
    SCALE.md   "pair keys separate an entity's ROLES, but an entity that appears
               twice in the SAME role collides again. The residual at 2+
               appearances (~0.57-0.63) is that case"

So the mechanism most likely to fail on 38% of the test set is one whose failure was
measured two hundred decisions ago, and it is **correlated with the axis the benchmark
is advertising.**

> Run naively, this produces a falling curve against hop count and the obvious reading
> is *"composition degrades with depth."* That reading would be unfalsifiable from the
> number alone, and it would be **wrong about which component to fix.** It is decision
> 143's circularity again: two candidate causes that the measurement cannot separate.

## What this decides, before the run

1. **Report accuracy split by max-entity-appearances, not only by hop count.** A cell
   at (hops=8, max_appearances=2) is comparable to training conditions; (hops=8,
   max_appearances=4) is not, and averaging them describes neither.
2. **The loader must expose that count per puzzle.** A property the analysis needs and
   the data does not name has to be computed at load time or it will not be computed
   at all.
3. **`max_appearances = 2` is the honest primary arm.** It is the subset that asks the
   question CLUTRR advertises — depth alone — and it is 713 of 1,146 test rows, which
   is plenty.
4. **The repeated-entity subset is a second, separately reported result**, and a poor
   one there is evidence about *addressing*, not about composition.

## What it does not say

**That CLUTRR is flawed.** A benchmark including entities that recur is more realistic
than one that does not, and the 433 rows are the more interesting half for anyone whose
addressing can take it. The defect would be ours, for reporting a single average across
two populations we have separate evidence about — rule 8, measuring at the granularity
of the decision.

**And nothing here has run a model.** This is the instrument inspected before use,
which is the one habit that has paid every time it was applied today.
