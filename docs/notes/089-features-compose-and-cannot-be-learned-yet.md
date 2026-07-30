089 — A feature representation composes and beats the baseline; its features are not learnable yet
=================================================================================================

**Status:** measured. **It is the first mechanism in this project to beat note 088's
end-task falsifier**, and the same note locates precisely why it is not yet a mechanism.

---

## IN PLAIN TERMS

Note 088 left a bar: any composition mechanism must beat **0.6081** end-task, which is what
guessing at random achieves. Note 070's learned readout scored 0.5995 — below it.

**Representing a relation by a few properties instead of as an atom clears the bar by a
wide margin: 0.7382.** Generation (how many levels up or down), gender, and whether the tie
is blood or marriage. Compose the properties, look up which relation has the result.

**But the properties were written by hand, and that is the whole problem.** Asked whether
those same properties can be *learned* from how relations are used, the answer is: gender
yes, the rest no. **And generation — the property that does most of the work, because it
simply adds — is the one that learns worst.**

So the position is unusually clear for this project: **we know what representation composes.
We cannot yet build it from data.**

---

## The measurement that clears the bar

    arm                          end-task CORRECT
    gap (no fill)                          0.5201
    random, 10 seeds                       0.6081   (sd 0.0055)
    note 070 readout                       0.5995
    FEATURE model (hand-coded)             0.7382
    oracle, true rules (note 087)          1.0000

**+0.130 over random, about 24 standard deviations.** It closes **33%** of the distance
between random and perfect.

**And it does that while getting the known rules WRONG a third of the time.** The gate — the
feature model must explain the 97 derivable rules — came out at **0.670**, so 0.7382 is a
floor for what a correct feature model would reach, not a ceiling.

## Where the hand-coded model is wrong, which is a fact about kinship

    brother     + father    truth father    model uncle
    daughter    + brother   truth son       model nephew
    grandfather + son       truth uncle     model father

**CLUTRR's siblings share parents.** So a sibling step *collapses* before an ascent
(`brother∘father = father`) and *creates* laterality after a descent
(`grandfather∘son = uncle`). An additive laterality flag is wrong in both directions:
kinship needs the **normalised path**, not a sum of features. Refining the hand-coding
further would be hand-solving kinship algebra, which is why the gate was left failing and
the end-task number taken anyway.

## The learnability measurement, which is the blocker

Leave-one-relation-out, predicting a held-out relation's feature from its extensional
profile (note 070's representation):

    feature       accuracy    chance / majority
    gender           0.850                0.500     learnable
    generation       0.350                0.200     barely
    affinity         0.800                0.700     +2 relations of 20

**Only gender is convincingly recoverable** (17 of 20 against 10). Generation is 7 of 20
against 4 — above chance and useless. Affinity's 0.800 is two relations better than always
guessing "blood", on twenty samples, which is nothing.

> **The features the oracle depends on most are the ones that learn worst.** Generation is
> what makes composition additive — `father∘father = +2 = grandfather` — and an extensional
> profile is a bag of *adjacent* relations, which does not encode depth. That is not a
> tuning failure; it is the wrong kind of feature for the job.

## What this makes of the earlier notes

    note 070   extensional profiles, +0.099 on held-out rules. Stands, and note 089
               says what they contain: gender, strongly. Not generation
    note 087   the ceiling is 31 rules, fold perfect given coverage. Stands
    note 088   the readout loses to random end-task, and set the 0.6081 bar.
               Stands, and the bar is now cleared -- by an oracle

## What is NOT claimed

**Not a mechanism.** The features are hand-written. This is an oracle bound in the sense
`g7-01`'s oracle gate was: it establishes that the *shape* works before an implementable
version exists, and `g7-01`'s lesson was that the implementable version is the hard part.

**Not that 0.7382 is the ceiling for features.** The gate failed at 0.670, so a model that
got kinship right would score higher. How much higher is unmeasured.

**Not that generation is unlearnable in general** — only that it is not recoverable from a
bag of adjacent relations. **Depth is a path property**, and a profile built from paths
rather than neighbours is the obvious untried thing. That is the next experiment and it is
named rather than attempted here.

**And not measured with the model in the loop.** Symbolic fold over true chains, as notes
066, 087 and 088 were.
