079 — Credit follows the walk, and blame localises where confidence cannot
=========================================================================

**Status:** measured, and it is one refutation plus one validation. **Recorded as
validation and stopped**, per John's instruction not to refine before moving on.

This is GOALS §5's step 2 — *"the credit-assignment scheme, chosen against C1/C2/C3 before
any substrate exists, with the locality and latency argument written out."* §5's own
candidate was next-input prediction, which §1.2 superseded; what survived was only the
delivery argument. **This is a candidate that is not prediction.**

---

## IN PLAIN TERMS

When the system answers wrongly, something it believes is wrong — and in a network with no
central coordinator, the hard part is working out *which* thing.

**This architecture gets the shortlist for free.** A traversal returns the route it took, so
the bindings that produced an answer are already named. Nothing has to be propagated
backwards through a structure nobody can see.

**Picking the culprit out of the route needs the right signal, and confidence is not it.** A
wrong fact is stored exactly as firmly as a right one, so the store reads it back just as
surely — asking "which step was least sure" performs at chance.

**What works is counting.** A binding that keeps turning up in wrong answers is the wrong
one, and over enough questions it stands out: **the true culprit ranks first 90% of the
time, against 5% for guessing.**

---

## The two measurements

**Confidence does NOT localise.** Decode margin (top1 − top2) per step, one corrupted
binding, 60 seeds × 6 corruption positions:

    weakest-margin step is the corrupted one    0.169     chance 0.167
    GATE: clean chain recovered                 60/60

**Exactly chance.** `memory += outer(wrong_value, key)` produces a confident read of a wrong
thing, so confidence measures how cleanly something was *stored*, never whether it is *true*.
This is decision 93's result — every identity-free confidence signal reaching 0.628 against
0.500 — arriving again in the credit-assignment case.

**Blame DOES localise.** Per binding, the fraction of walks it appeared in that ended wrong.
30 seeds, the corruption rotated through 20 entities, 60 walks per seed:

    culprit ranked #1 by blame rate      0.900     chance 0.050
    culprit ranked top-3                 0.900
    mean rank                             2.37     of 20
    culprit's mean blame rate            0.717

**18x chance.**

## Why it satisfies the constraints §5 requires arguing first

    C1  locality, bounded bytes    one counter per binding, held by the node that
                                   owns the binding. The walk that carries the
                                   blame is ~10 (entity, relation) pairs, ~80
                                   bytes. No collective, nothing to stall on
    C2  bounded asynchrony         a blame update arriving late adjusts a binding
                                   that has since moved, which costs PRECISION and
                                   not correctness -- the one argument §5's
                                   superseded candidate had, and it transfers intact
    C4  perpetual learning         a counter accumulates for as long as the system
                                   runs, which is the regime rather than a problem
    no labels                      needs only "that answer was wrong", not what the
                                   right answer was

## The gap, named rather than hidden

**It needs to know an answer was wrong.** That is a supervision signal, and a network on
strangers' machines cannot assume one.

**The candidate source is already in the record:** note 068's contradiction — closure derives
a fact the store holds differently, and decision 148's gate is exact enough to tell a
contradiction from a blur. So contradiction would supply *"wrong"* and blame would supply
*"where"*, and neither needs a label or a broadcast. **Untested, and it is the next thing.**

## The bug that nearly buried this, recorded because the shape recurs

The first blame run reported **0.000 at rank 1 and mean rank 15 of 20** — below chance. That
was a defect, not a finding: the failure test compared `Walk.entities` against a
depth-long expectation, and `Walk.entities` is **depth − 1** long by construction. So the
comparison was vacuously true, every walk counted as failed, every binding scored blame rate
1.0, and the ranking became arbitrary.

> **A below-chance result on a mechanism that should be at-or-above chance is a bug signal.**
> Treating 0.000 as a refutation would have discarded the working scheme — rule 12's most
> expensive error, avoided only by not believing the first number.

## What is NOT claimed

**Not that the correction works, only that the culprit is found.** Locating a wrong binding
is not repairing it, and what to write instead is untouched.

**Not measured on a branching graph.** The fixture is a ring with out-degree 1, so every walk
is deterministic and a wrong step is unambiguous. Real graphs branch, and a wrong turn there
may be a wrong *choice* rather than a wrong *fact* — a different error with a different
owner.

**And not at scale.** 20 entities, 60 walks per seed. Whether blame separates a cause from
its downstream consequences when the store is large is exactly the question this fixture is
too small to ask.
