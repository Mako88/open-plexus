090 — One learned invariant closes the composition ceiling: 52% → 96.7%
======================================================================

**Status:** measured, with a control that collapses. **It closes the ceiling notes 066, 067,
070, 084, 087, 088 and 089 were all circling**, and it does so with one feature learned
exactly rather than a better readout.

---

## IN PLAIN TERMS

CLUTRR withholds 31 composition rules, and every attempt to guess them has failed — the
best learned readout scored *below* random guessing (note 088).

**Guessing the rule was the wrong goal.** What a chain needs is not the right *name* for a
missing step but the right *displacement*: how many generations up or down it moves. Fill a
gap with any relation that moves the right number of generations and the chain stays
arithmetically correct, so the steps that ARE known finish it properly.

**That takes the end task from 52% to 96.7%**, against 100% for an oracle handed the true
rules.

**And the displacement is learnable exactly.** A puzzle's chain plus its question forms a
loop, so the chain's displacements must sum to the answer's. That is one equation per
puzzle, 9,074 of them, in 20 unknowns — and it recovers all twenty exactly.

---

## The measurements

**Generation delta, learned from cycle constraints:** 9,074 usable train chains give 9,074
equations. The system's null space has dimension **exactly 1** — the expected global gauge
freedom — and fixing `brother = 0`, `father = +1` recovers **20/20** deltas exactly,
matching note 089's hand-coded values to three decimals.

**End task, symbolic fold over true chains:**

    arm                                  end-task   fills   final-step fills
    gap (no fill)                          0.5201     543                 17
    random relation                        0.6073    1152                189
    CONTROL: deliberately WRONG delta      0.5681    1214                215
    correct delta, arbitrary relation      0.9668     720                 28
    oracle, true rules (note 087)          1.0000       -                  -

**The control is what makes this readable.** Filling with a relation of the *wrong* delta
scores 0.5681 — below random. So the delta is the mechanism, not the filling.

**And the fill count falls, which is the mechanism visible from another angle:** 720 fills
against random's 1,152. A delta-preserving fill lands on relations the table already knows,
so it creates fewer subsequent gaps. The walk stays on tracks.

## The ablation, and it refutes my own hand-coding

    configuration                                    end-task
    learned features, WITH a hand-coded marry clause   0.7845
    learned features, no marry clause                  0.9092
    generation ONLY, ignoring gender and affinity      0.9668

**Every piece of domain knowledge I added made it worse.** The "descend then ascend lands on
a spouse" clause cost 0.125; gender and affinity together cost a further 0.058. Note 089
reported the hand-coded oracle at 0.7382 and treated its features as the target to learn —
**two of the three were noise, and the one that mattered was the one 089 measured as least
learnable.**

> Note 089 said generation was the blocker at 0.350 from extensional profiles, and it was
> right that profiles cannot see it. **Profiles are adjacency and generation is global**, so
> the answer was a different signal rather than a better regressor. That is the transferable
> lesson: when a feature will not learn, ask what KIND of structure it is before trying
> harder on the same representation.

## What is NOT claimed

**Not that composition is solved in general.** Kinship has an **additive invariant** —
generation composes by adding — and the whole result rests on that. Whether an arbitrary
relational domain has a conserved quantity of this kind is unknown and is the question this
raises. **A domain without one gets nothing from this.**

**Not fully learned.** The rule *"deltas add"* is a design choice rather than something read
from data. It is arithmetic rather than domain knowledge, and the ablation shows the actual
domain knowledge I supplied was harmful — but a system that discovered the additivity itself
would be a stronger claim than this makes.

**Not the model's own chains.** Symbolic fold over TRUE chains, as notes 066, 087, 088 and
089 were. Note 065 measured chain recovery at 0.8805 monolithic and 0.9220 partitioned, so
an end-to-end number is roughly the product and remains unmeasured.

**And 0.9668 is not 1.0000.** 28 of 720 fills land on the final step, where an
arbitrary-relation-with-the-right-delta is exposed: the answer needs the exact relation, not
merely the right displacement. Naming those correctly is what the last 3.3% is, and gender
would presumably help there — which is where the features the ablation discarded may earn a
narrower place.
