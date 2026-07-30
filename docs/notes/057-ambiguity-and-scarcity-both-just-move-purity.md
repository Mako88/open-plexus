# 057 — Ambiguity and scarcity both just move purity

**Status:** measured, local probe, six seeds. **P1 confirmed on the second attempt,
P2 REFUTED, P3 unreachable as designed.**

**Why it exists:** [note 056](056-the-cliff-rule-needs-an-almost-exact-grouping.md)
degraded index purity by starving the index of background streams. That conflates two
different situations — an index that has not seen enough, and a concept space where
the concepts genuinely overlap — and **only the second is what a real grouping looks
like.** A rule that survives ambiguity but not scarcity would be in much better shape
than 056's table alone can say.

---

## IN PLAIN TERMS

The model works out which things belong together by noticing what they are described
alongside. Note 056 made that harder by showing it less. This note makes it harder a
different way: by giving different kinds of thing **overlapping descriptions**, while
letting the model see as much as it likes.

The answer is that it does not matter which way you make it harder. **One number —
how cleanly the groups are recovered — predicts how good the answers are, and both
kinds of difficulty are just ways of moving that number.** Overlap does not break the
grouping; it makes a clean grouping much more expensive to learn.

---

## The axis, and its first version was inert

`shared_attributes` replaces that many of each family's own attribute tokens with
tokens from a pool **every family draws from**.

**The first version did not work and its own P1 caught it.** It had each family
borrow its *neighbour's* attributes, so family `f` was described by `{f0, g1, g2, g3}`
— a set no other family used, and therefore still uniquely identifying. **Purity
stayed at 1.000 while sharing three of four attributes.**

> **A condition guaranteed absent by the way it is built is not a condition**, and
> the only reason this was caught in minutes is that P1 asked whether the instrument
> moves before anything downstream was read. `tests/test_family_set_queries.py` now
> asserts the property that was missing — two families must actually share tokens —
> with the companion that they share nothing when the axis is off.

## What it measures, with data held constant

    n_attributes 4, family_size 4, 40 background streams. n = 12 per cell.

    shared   purity   gap rule   fixed=3
         0    1.000      1.000     1.000
         1    1.000      1.000     1.000
         2    1.000      1.000     1.000
         3    0.997      1.000     1.000

**With enough data, ONE private attribute is enough.** Co-occurrence is far more
robust to overlap than to scarcity, which is a genuinely reassuring thing to know
about the index.

**But reading only this table is how P2 got refuted.** At a saturating data budget
the axis cannot bite, so "ambiguity is harmless" was an artefact of where it was
measured — the same mistake in miniature as fitting a scaling exponent above
saturation.

## The interaction is where it bites

    purity / gap rule / fixed, by streams and sharing. n = 12 per cell.

     streams        sh=0                   sh=3
              purity   gap  fixed    purity   gap  fixed
           1   0.229 0.167  0.083     0.264 0.167  0.250
           2   0.507 0.000  0.333     0.375 0.083  0.167
           3   0.701 0.417  0.417     0.403 0.083  0.167
           5   0.885 0.667  0.917     0.493 0.250  0.333
          10   0.983 0.833  1.000     0.705 0.333  0.583

**P2 REFUTED.** Ambiguity is not gentler than scarcity. At ten streams, sharing three
of four attributes costs **0.28 purity and 0.50 exact** — the same data budget buys
far less separation.

**P3 was unreachable as designed**, and that is worth saying rather than scoring.
It asked for a regime where the grouping is unrecoverable and both rules collapse.
The configuration guard forbids `shared_attributes == n_attributes`, because a family
with no private attribute is unrecoverable *by construction* rather than merely hard
— so the falsifier could only have fired in a configuration the task refuses. **A
prediction that cannot fail is not a prediction**, and this one was written without
checking that.

## The finding: purity looks like the sufficient statistic

    matched purity ~0.70          gap rule
      sh=0, 3 streams  (0.701)      0.417
      sh=3, 10 streams (0.705)      0.333

Two very different routes to the same purity give answer quality in the same
neighbourhood. **Stated as a hypothesis rather than a result**, because it rests on
one matched pair at n=12 and the difference (0.417 against 0.333) is not separated
from noise at that size.

If it holds, it is useful out of proportion to its size: **there is no need to model
"hard groupings" and "small data" separately.** One instrument, purity, and everything
else is a way of setting it.

## What this does NOT change

**Note 056's conclusion stands and is strengthened.** The gap rule trails the fixed
count at essentially every sub-oracle purity here too — 0.333 against 0.583 at the
best sub-oracle cell. Both remain 🔀 with the crossover at purity ≳ 0.99, and the
reason F3 is PARTIAL is unchanged: the enumeration bound is either supplied, or it
needs a near-oracle grouping.

**And it says nothing about whether real groupings are near-oracle.** Every number
here is on a synthetic task built to make families recoverable. What a grouping over
real concepts looks like is the standing gap, and it is the same gap CLUTRR has been
"next" for several cycles to close.
