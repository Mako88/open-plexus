# Option record — `ByConcept`, address the store at the concept

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/concepts.py` — `ByConcept`, mapping every surface to its concept's address.
- `openplexus/tasks/families.py`, the instrument this was measured on, and the only one
  where entities resemble each other.

---

## What was tried, and what came back

### It buys transfer — `143`

    CONFIG  when    2026-07-29
            source  decision 143, decision 144
            script  unrecorded
            task    families, homogeneous
            model   concept addressing over a discovered grouping
            knobs   grouping on against off
            scale   3 seeds

Discover a grouping, address a store by it, recall through it: it works, and on a
homogeneous family `direct` reaches **0.9967** and `transfer` **0.9983**. Transfer without
concepts is 0.0608, so the indirection is doing the thing it was built to do.

### It cannot hold an exception, and the first measurement of the price was wrong — `144`

    CONFIG  when    2026-07-29
            source  decision 144
            script  experiments/g19_01_can_grouping_answer_what_was_never_stated.py
            task    families with exceptions, g19-01's exception arm
            model   concept addressing
            knobs   2 facts stated per family, 1 contradicting
            scale   3 seeds

    arm           direct  transfer  exception   wrong answer = a sibling's
    ungrouped     0.7792    0.0608     0.7833        0.0084
    concept       0.4492    0.4708     0.3708        0.8657
    permuted      0.3417    0.0517     0.3167        0.0300
    nostore       0.0000    0.0000     0.0000        0.0000

An exception is an entity whose own stated fact contradicts its family's. For `ungrouped`
that is ordinary recall at 0.783; for `concept` it is **0.371**, worse than having no
concepts at all. And when `concept` answers an exception wrongly it says **a sibling's
value 86.6% of the time** against 0.8% for `ungrouped` — the superposition speaking, where
generic failure would scatter.

The entry's conclusion that *"one exception halves everything"* is **struck in place** by
145. Two facts with one contradicting is a literal 50/50, so the coin flip it measured was
the configuration and not the mechanism.

### The majority wins and the exception is erased — `145`

    CONFIG  when    2026-07-29
            source  decision 145
            script  unrecorded
            task    families, exception share varied
            model   concept addressing
            knobs   stated facts 2, 3 and 5 with one exception throughout
            scale   the sweep 144 did not run; four minutes

    stated  agree  exception share   direct  transfer  exception
         2      1            0.50   0.4650    0.4400     0.3725
         3      2            0.33   0.9200    0.9250     0.0300
         5      4            0.20   0.9825    0.9900     0.0000

**The default is robust** — the concept address holds the superposition and the majority
dominates it, exactly as a sum should. **And the exception is not merely wrong, it is
erased**: 0.030 at a third and **0.000** at a fifth. Not degraded, not noisy — gone.

The corrected statement is worse in one way and better in another. Better: the concept map
is a robust default store that tolerates dissent in proportion to how outnumbered it is.
Worse: **the system does not fail to answer about an exception, it confidently answers with
the category's default** — the most dangerous shape a wrong answer can have, and a
straightforward description of a stereotype.

### The load-bearing correction: the store never collided — `note 049`

    CONFIG  when    2026-07-29
            source  note 049, and decision 145 for the two-level figures
            script  none -- reasoning against the code
            task    families
            model   surface keys and concept keys are different addresses
            knobs   none
            scale   n/a

A fact at the surface key and a default at the concept key are *different addresses*, so
the store was never in conflict. `ByConcept` was a **READ POLICY**, which is why decision
148 ended up costing a sketch rather than a representation. Decision 145 strengthens the
same note from the other side: 0.99 transfer at a 20% exception rate means a two-level
scheme can hold the default at the concept and the override at the surface without the two
destroying each other, and the ceiling probe agrees — at least one of the two arms is right
on **0.853** of exceptions and **0.878** of directs.

### It explains why grouping hurt on text — `144`, `141`

    CONFIG  when    2026-07-29
            source  decision 144, decision 141
            script  unrecorded
            task    families, read across to corpus
            model   concept addressing
            knobs   grouping on against off
            scale   3 seeds

**Text is nothing but exceptions.** Every word has its own continuations, so every grouped
address holds a dozen competing values. The cost that was argued about for a day became a
number, measured where it can be isolated.
