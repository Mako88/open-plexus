069 — The baseline was wrong: marginal structure alone gets 0.24
===============================================================

**Status:** measured, 20 seeds. **It corrects note 067's baseline**, which I quoted to
John an hour before this as the number to beat. It also reports a first look at
extensional relation profiles, which is **neither supported nor refuted** — the test that
would decide it has not been run.

---

## IN PLAIN TERMS

Note 067 measured that composing relations built from random patterns generalises at
0.056, against guessing at 0.050, and concluded there is no generalisation. **That
conclusion is right for the setup it used and the comparison was against the wrong
number.**

Given the two relations in a chain, some of the answer is predictable without composing
anything at all: if the second relation is female, the answer is usually female. That is
not composition, it is a **marginal** — and measured here it is worth **0.24**, five times
the guessing rate.

**So anything claiming to compose must beat 0.24.** Note 067's protocol destroyed the
marginals along with everything else, so its 0.056 was not evidence of a floor; it was a
measurement taken with the useful signal already removed.

---

## The measurement

97 rules from CLUTRR train by note 066's bootstrap (62 from 2-hop directly, then round 2
— **the same 62 and 97, so the extraction is confirmed against a known number**). Hold out
25%, 25 items, resolution 0.040. Ridge readout from `concat(v(r1), v(r2))` to the answer
relation. 20 seeds.

    chance                 0.050
    majority class         0.082
    MARGINAL BASELINE      0.242   <- random vectors, concat readout

    extensional positional 0.248
    extensional collapsed  0.210

**Random gets 0.242 and extensional gets 0.248. That is a tie**, and the tie is the
finding: `concat` of near-orthogonal random vectors lets a linear readout identify which
relation occupies which slot, after which it learns marginal effects. **No property of the
vectors is used, so no representation can distinguish itself.**

## Two harness defects found first, and one of them is the reason to write this down

**Defect 1 — the readout memorised.** 46 training rules against 136 concat features is
underdetermined, so `lstsq` fit everything: trained accuracy was **0.917 for extensional,
collapsed AND random, identical to three decimals.** Three unrelated representations
scoring the same on training data is the signature of a fit measuring its own capacity.
Fixed with ridge (`ALPHA = 1.0`), after which trained separates: 0.818 / 0.694 / 0.817.

**Defect 2 — the harness did not reproduce a known number.** The random condition is note
067's setup, and it scored **0.125 where 067 measured 0.056.** That gate is what stopped
this from being reported as a result. The cause turned out to be the `concat`, and chasing
it is what produced the finding above.

> Both defects were caught by the same rule, and it earned its place again: **reproduce a
> known number before trusting a harness.** Had 067's baseline not existed to fail against,
> the first run's 0.163-vs-0.125 would have been reported as extensional profiles beating
> random by 30%.

## What extensional profiles DID recover, separately and more cleanly

Profile each relation by how other relations attach to the entities it connects — 20
relations x 4 attachment types, built from train and validation story edges plus the
labelled query edge.

    nearest neighbour among the 14 edge relations

    father        -> grandfather  0.775      brother -> son      0.806
    mother        -> grandmother  0.778      sister  -> daughter 0.810
    uncle         -> brother      0.732      grandson -> son     0.666
    husband       -> wife         0.853      (the only mutual gender-pair)

**All 14 nearest neighbours are same-gender, and the axis recovered is generational
adjacency.** `father` is `grandfather`'s nearest neighbour — which is note 067's own
statement of the requirement (*"`grandfather` should be near `father∘father`"*), so the
structure present is the structure asked for.

**My stated gate was 7/7 mutual nearest neighbours on the gender-pairs and it scored 1/7.
The gate was mis-specified, not failed.** It assumed `father` and `mother` occupy identical
relational positions; they do not, because gender propagates through a kinship graph and
the extension sees it. Recorded as a mis-specification rather than a near-miss, because a
prediction that has to be reinterpreted after the fact is worth nothing as a prediction.

**The falsifier partly fired.** The six target-only relations get extension only from query
edges, and four place sensibly — `father-in-law`→`grandfather` (0.548),
`mother-in-law`→`grandmother` (0.543), `nephew`→`brother` and `niece`→`sister` (0.517
each, symmetric). **`daughter-in-law` and `son-in-law`, at ~160 support, land on `grandson`
at 0.16 — effectively unplaced.** Low support is the weak point, as predicted.

## What is NOT claimed

**That extensional profiles help composition.** 0.248 against 0.242 is a tie and the
protocol cannot tell them apart. **The deciding test has not been run**: it needs a
composition that forbids marginals — 067's `bind`, which destroys slot identity — with
extensional operands rather than random ones. That is one substitution away and it is the
next measurement.

**And the geometry above is not evidence for the geometry mattering.** Recovering gender
and generation is a property of the profiles; whether a composition operator can use it is
the separate question this note failed to answer.
