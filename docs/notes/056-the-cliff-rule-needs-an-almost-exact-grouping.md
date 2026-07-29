# 056 — The cliff rule needs an almost-exact grouping

**Status:** measured, local probe, six seeds. **P2 refuted.**

**Why it exists:** the commit that added the similarity-cliff enumeration said
plainly that its 0.45-wide cliff *"is a property of a task calibrated to make
families recoverable"* and did **not** claim the rule survives a noisy index. This
asked. It does not.

---

## IN PLAIN TERMS

To answer "what values did this family state", the model has to decide **how many
neighbours to look at.** Two ways: be told the number, or work it out from where the
similarity ranking falls off a cliff.

Working it out looked strictly better — it matched being-told at every family size
without being told. **But that was measured on a grouping that was perfect.** When
the grouping gets noisy, working it out gets worse *faster* than being told, because
being told only has to survive bad candidates while working it out has to survive
bad candidates **and** a bad count.

So the honest version is narrower than the last one: the cliff rule removes a
supplied constant **only where the grouping is essentially exact.**

---

## The measurement

`attribute_mentions` and the number of background streams are `families.py`'s
discoverability dial. Fewer streams, less evidence for the grouping. Purity is: of
the `family_size − 1` nearest neighbours, how many are genuine siblings.

    family_size 4, 4 families, chance purity 0.200. n = 12 per cell, six seeds.

     streams  purity  gap rule  fixed=3  gap size  gap prec
           1   0.347     0.167    0.250      2.25     0.604
           2   0.583     0.250    0.250      2.25     0.667
           3   0.795     0.167    0.417      2.58     0.708
           5   0.951     0.750    1.000      2.08     0.944
          10   0.993     1.000    1.000      2.00     1.000
          40   1.000     1.000    1.000      2.00     1.000

**P1 CONFIRMED.** Purity moves, 0.347 → 1.000, so the instrument works and the rest
is readable.

**P2 REFUTED.** The gap rule degrades **faster** than the fixed count, not equally.
At purity 0.795 it scores 0.167 against the fixed rule's 0.417; at 0.951, 0.750
against 1.000. It only draws level at purity ≳ 0.99.

**P3 CONFIRMED.** Near chance purity both are at the floor (0.167 and 0.250), so the
gap rule is not reading something other than the family structure. There is no leak,
which also means the 1.000 at full purity is trustworthy as far as it goes.

## Why, and the mechanism is not subtle

**The fixed rule has one error source; the gap rule has two.**

Given the count, a noisy ranking can only hand you the wrong *candidates*. Deriving
the count, a noisy ranking hands you the wrong candidates **and** a wrong count,
because the biggest gap stops being at the family boundary.

`gap size` is the tell: 2.58 against a true 2.00 at purity 0.795, with precision
0.708. The rule cuts **too late**, admitting strangers — so the failure is
over-emission, which is the direction decision 165's falsifier is built to catch and
the reason precision is reported beside `exact`.

## What this changes

**The two options are now a measured crossover, not a winner and a loser.**

    purity >= 0.99      the gap rule matches, and asks for no constant
    purity 0.79-0.95    the FIXED count is better by 0.25 exact
    purity ~ chance     both at the floor

That is decision 74's shape again — sparse keys were worse on MQAR, then a readout
change reversed them cleanly — and it is exactly what 🔀 is for. Both stay, and which
is right is a property of the grouping's quality rather than of either mechanism.

**Row F3 stays PARTIAL, and the reason is now sharper than "the size is supplied":**
the enumeration bound is **either supplied, or it requires a near-oracle grouping.**
Neither is answering from awareness.

## What it does NOT say

**That the cliff rule is wrong.** At the purity `families.py` is calibrated for it is
exact and asks for nothing, and every result measured on it stands under that
condition — named, per the rule that a measurement is conditional on its
configuration.

**That purity 0.99 is unreachable.** It is what 10 background streams buy on this
task. Whether a real grouping over real concepts reaches it is a different question
and this probe says nothing about it.

## The habit that produced this

The probe cost minutes and ran **before** anything was built on the cliff rule. The
same day, three recommendations went out built on claims that had already been
superseded, because the record was consulted after proposing rather than before.
**This is the same correction applied one step earlier** — check the thing you just
built before building on it, not after.
