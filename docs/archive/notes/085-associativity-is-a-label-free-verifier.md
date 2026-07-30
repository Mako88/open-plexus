085 — Associativity cannot generate rules but verifies them almost perfectly
===========================================================================

**Status:** measured, 40 seeds. **It is the second label-free correctness signal in the
record**, after note 080's contradiction, and note 082 measured that such a signal is exactly
what the whole memory design reduces to.

---

## IN PLAIN TERMS

Composing relationships should be associative: *(father then sister) then brother* has to mean
the same as *father then (sister then brother)*. On the rules the data supplies, it does —
**93.3% of the time.**

**That structure cannot work out unknown rules.** Only 15% of the possible rule table is known,
so the triangles almost never close, and asking associativity to name a missing rule performs
at chance.

**But it can tell a good guess from a bad one, and it is brutal about it.** A guess consistent
with every triangle it touches is right **56%** of the time. A guess that contradicts them is
right **1.6%** of the time. Nothing else in this project separates right from wrong that
sharply without being told the answer.

---

## The measurements

**Associativity holds on the known table: 112/120 = 0.933.** The premise is sound, and the 7%
that fail are themselves worth knowing about — kinship is not a group, so some compositions
are genuinely ambiguous (`note 063` measured 0.42% irreducible ambiguity in the chain-to-target
mapping, which is a different and smaller quantity).

**It cannot generate.** Hold out a quarter and ask associativity alone to determine them:

    held-out rules with any constraint at all     0.580
    uniquely and CORRECTLY determined             0.059     chance 0.050

At chance. 62 known rules over 20×20 = 400 possible pairs is **15% density**, so a triangle
needing three known entries rarely closes and, when it does, candidates tie.

**It verifies, and this is the finding.** Let the readout propose (note 070's mechanism) and
bucket its predictions by whether they satisfy their own constraints:

    associativity of the prediction       n     accuracy
    satisfies ALL                        62       0.5645
    satisfies some                       10       0.9000
    satisfies NONE                      185       0.0162
    no constraint                       383       0.2219
    overall                             640       0.2062

**0.5645 against 0.0162 is a factor of 35**, and the rejection side is the more useful one:
185 predictions flagged as inconsistent, of which **98.4% are genuinely wrong.**

## Why this matters more than the accuracy number

Note 082 measured that consolidation's recall tracks its correctness signal **one-to-one** —
0.9 accuracy in gives 0.915 recall out. So the entire memory design reduces to the quality of
a label-free *"was that right"* signal, and the record now holds two:

    note 080   contradiction    derived vs retrieved disagree. Six sd separation.
                               General -- works on any address with two routes to it
    note 085   associativity    a composition inconsistent with its own algebra.
                               Specific to composition, and near-perfect at REJECTING

**They are complementary.** Contradiction needs two independent derivations of the same fact;
associativity needs only one prediction and the algebra. Where both apply they should be
combined, and where neither does there is no signal at all — which is 383 of 640 cases here,
and the honest limit.

## What is NOT claimed

**Not that filtering raises the usable rule count much.** Accepting only fully-consistent
predictions gives 0.5645 accuracy on **62 of 640** held-out rules — 9.7% recall. High
precision, low coverage. Whether that lifts note 066's 52% fold ceiling is arithmetic nobody
has done, and it is not obviously more than a point or two.

**Not that 0.933 means kinship is associative.** It means associativity holds where the table
lets it be checked, on 120 checkable triangles. The 8 failures are unexplained and could be
data noise, genuine ambiguity, or an artefact of the bootstrap that built the table.

**And the readout reproduces at 0.2062 rather than note 070's 0.223** — 40 seeds against 120,
with 16 held-out items making each worth 0.0625, so the gap is under one item. Same
measurement, coarser.
