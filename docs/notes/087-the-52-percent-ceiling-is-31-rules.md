087 — The 52% ceiling is 31 rules, and the fold is perfect without them
======================================================================

**Status:** measured, and it restates this project's central problem precisely for the
first time. **It supersedes the framing of notes 066, 067, 070 and 084** — all of which
treated composition as a mechanism to be fixed. It is not. It is 31 missing facts.

---

## IN PLAIN TERMS

Naming what a chain of relationships adds up to completes only about half of CLUTRR's
puzzles, and every attempt so far has treated that as a defect in the *method*.

**The method is not the problem. Give it the rules it lacks and it completes 100%.**

What it lacks is **31 pairs**, and eight of them cause two thirds of all failures. They are
one family: things like *my son's mother*, *my sister's husband*, *my daughter's father* —
the compositions that go DOWN the family tree and back UP, landing on a spouse or an
in-law. **CLUTRR never states any of them, in any split.** That is the benchmark working as
designed; it tests whether a system can reach a combination it was never shown.

**And the leverage is steep, because a long chain needs every step.** A ten-hop puzzle
composes nine times and one miss breaks it, so completion is roughly coverage to the ninth
power. Guessing those missing rules at 56% accuracy would take deep-chain performance from
0.345 to **0.640** — not because 56% is impressive, but because it compounds.

---

## The measurements

**Per-step coverage is 0.8755** (3,819 of 4,362 fold steps), and completion tracks
`coverage^(hops-1)` closely enough that the multiplicative model is the right one:

    hops     n    complete    coverage^(hops-1)
       8   150       0.380                0.394
       9   119       0.336                0.345
      10   119       0.218                0.302

**The fold is perfect given coverage.** With every missing pair supplied, puzzles complete
**1.0000** — so note 066's *"tabulation's ceiling, not the fold's error"* is exactly right,
and now has the stronger form: the fold has NO error of its own.

**31 distinct missing pairs, 543 failures, and the top 8 are 347 of them:**

    son        + mother         62        sister     + husband     38
    daughter   + father         60        brother    + wife        34
    son        + father         54        son-in-law + daughter    28
    daughter   + mother         47        father     + wife        24

**Every one is NEVER STATED, in any split** — checked against train, validation and test.
So they are not withheld, they are absent, and no amount of reading the data supplies them.

## What accuracy is worth, and this is the target

    guesser accuracy p    per-step coverage    completion at 9 hops
    today (nothing)                  0.8755                   0.345
    p = 0.223  (note 070)            0.9033                   0.443
    p = 0.400                        0.9253                   0.537
    p = 0.565  (note 085)            0.9458                   0.640
    p = 0.800                        0.9751                   0.817

**So the problem is now stated as a number:** raise held-out rule prediction from **0.223**
toward **0.56+**, *at full recall*, on 31 specific pairs.

> **The recall clause is where the current pieces fail, and it must not be glossed.** Note
> 085's 0.5645 is the accuracy of predictions that satisfy every associativity constraint —
> and only **9.7%** of predictions have a checkable constraint at all. Applying it as a
> filter fills 9.7% of the gap at 0.56 rather than 100% of it, which moves coverage from
> 0.8755 to about 0.882. Almost nothing. **High accuracy on a tenth of the gap is worth far
> less than moderate accuracy on all of it.**

## Why deduction cannot supply them, settled here

Associativity propagation was tried and is **inert**: from the 62 seeds it fills **zero
cells in zero rounds**, and propagating from the full true table adds zero and contradicts
zero — so the constraint is sound and simply never closes. At 15% table density, all three
cells of a triangle are simultaneously known about 0.3% of the time.

**So composition needs LEARNING, not deduction**, and note 085's associativity keeps its
role as a verifier rather than gaining one as a generator.

## What this makes of the earlier attempts

    note 066   "tabulation's ceiling" -- right, and understated: the fold has no
               error of its own at all
    note 067   `bind` over random relations, no generalisation. Still true, and
               now sized: it needed to reach 0.56 on 31 pairs
    note 070   extensional relations, 0.223. The best mechanism so far, and this
               says what it is worth: 0.443 at 9 hops, a 28% relative gain
    note 084   self-training does not lift 0.223. Still true
    note 085   associativity verifies at 0.5645 -- but on 9.7% recall, which this
               note shows is the clause that makes it nearly worthless as a filter

## What is NOT claimed

**Not that 31 rules is a small problem.** They are exactly the compositions the benchmark
withholds, so supplying them means generalising to unseen combinations — the thing the whole
line of notes has been unable to do. **The gap is small and the difficulty is not.**

**Not that the multiplicative model is exact.** It over-predicts at 10 hops (0.302 against
0.218) and under-predicts at 3 (0.767 against 0.571), so per-step failures are not
independent — longer chains presumably revisit the same missing pairs. The model is right
about the *shape* and should not be used to project a precise number.

**And not measured with the model in the loop.** This is the symbolic fold over true chains,
as note 066 was. Substituting recovered chains multiplies everything by note 065's recovery
rate.
