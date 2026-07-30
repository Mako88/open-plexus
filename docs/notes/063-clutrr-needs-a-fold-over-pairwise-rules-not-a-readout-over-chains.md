063 — CLUTRR needs a fold over pairwise rules, not a readout over chains
=======================================================================

**Status:** a structural measurement of the benchmark, and it decides the mechanism.
No model was run. **It also trips a trigger `docs/SCALE.md` recorded before CLUTRR
existed in this project**, which is the register doing the job it was written for.

---

## IN PLAIN TERMS

CLUTRR cannot be passed by memorising. **99.8% of the chains in the test set never
appear in training** — 1,010 of 1,012 — so any approach that learns whole chains and
looks them up scores nothing.

But it can be passed by composing. **The two-step combinations are almost all
covered**: only 6.6% of the adjacent relation pairs in test are unseen in training, and
each one that is seen appears a median of 144 times. Two hundred-odd small rules, each
well attested, combine into a thousand chains nobody has seen.

**That is a statement about how to build it, not just about the data.** Learn what
"father then sister" gives you and apply it repeatedly, and depth stops mattering.
Learn what whole chains give you and nothing transfers.

---

## The measurement

    whole chains
      distinct in the corpus                      1393
      chains with more than one target               8   (121 rows, 1.2%)
      ceiling from the chain alone              0.9958
      TEST CHAINS UNSEEN IN TRAIN            1010/1012   (99.8%)

    adjacent relation pairs
      distinct in train                             98
      distinct in test                              91
      test pairs unseen in train                     6   (6.6%)
      test ROWS containing an unseen pair       47/1146   (4.1%)
      training examples per test-used pair    min 30, median 144, max 335

    the two-hop rule table, learnable directly from train
      distinct pairs                                62
      of those AMBIGUOUS                             0
      test two-hop pairs present in train        38/38

**The eight ambiguous chains are genuine, not noise.**
`husband-daughter-grandfather` maps to *father-in-law* 10 times and *father* 6 — the
route through a marriage or by blood. CLUTRR's rule table is deliberately partial for
exactly this reason, and 0.9958 is therefore close to an irreducible ceiling rather than
a measurement artefact.

## What it decides

**The composition must be a FOLD over pairwise rules, not a readout over a concatenated
chain.**

    a readout over the chain    must generalise to unseen WHOLE chains -> 99.8% novel
    a fold over pairs           needs pairwise coverage -> 93.4% seen, median 144 each

The second only ever asks a question it has been trained on. The first asks one it has
essentially never seen. **That is not a tuning difference; it is the difference between
a learnable problem and an unlearnable one**, and it is visible in the data before any
model runs.

It also explains why depth is where note 062's recovery decays. A fold's difficulty does
not grow with depth — the same rule applies at every step — whereas anything holding the
whole chain at once must represent something that gets longer.

## And SCALE.md predicted this, before CLUTRR was here

The register's row on `hop_accumulate`:

> *Chosen:* `concat` — the readout sees every hop side by side. **Measured at 16
> composition rules.** *Why it may not travel:* concat wins because 16 rules in a
> 128-wide space are linearly separable **whatever** structure the labels have. That is
> a property of having few rules, not of concatenation being right.
> **Trigger to revisit: a rule table in the hundreds.**

**CLUTRR has 1,393 distinct chains and 98 pairs.** The trigger is met on the first
reading, by the register's own criterion, and the alternative it names — `bind`, kept
*"as the measured alternative, not as a fallback"* — is the one a fold would use.

> This is the first time the scale register has fired on a task it was not written
> against. It was added on the argument that *"a default chosen at width 64 carries no
> warning label when it is read at width 8192"*, and the same logic held for rule count.

## What this does NOT say

**That the fold works.** Nothing has been built or measured. Learning 98 pairwise rules
from 9,074 puzzles where only the *final* answer is supervised is credit assignment
across a chain — and this project's whole premise is that credit assignment is the hard
part. **The intermediate compositions are not labelled**, so a fold has to discover them
from the endpoint, which is a materially harder problem than the coverage numbers above
suggest.

**And it does not license concat's removal.** Rule 14c: it stays, it is the measured
comparison, and its refutation here would be a *prediction* until something is run.

## What follows

    end-task <= chain recovery x composition accuracy
             <= 0.659 x (<=0.9958)  ~  0.65 at best, on note 062's one seed

So the ceiling is set by chain recovery, not by naming — which puts the next measurement
on the **drift** note 062 identified rather than on the readout. Fixing naming cannot
lift a number bounded by the route.
