080 — Contradiction is detectable without a label, and the credit loop closes
============================================================================

**Status:** measured, 40 seeds. **It supplies the signal note 079 named as its gap**, so
GOALS §5's step 2 now has all three pieces measured rather than two measured and one hoped
for.

---

## IN PLAIN TERMS

Note 079 found that a wrong belief can be tracked down by counting which bindings keep
appearing in wrong answers. It needed one thing it did not have: **something to tell it an
answer was wrong**, without a person supplying the label.

**Two reads and a dot product are enough.** Ask the store directly, and separately work the
answer out by following the chain. If they disagree, something is wrong — and the
disagreement is unmissable.

---

## The measurement

`closure.py`'s setup: state `a-r1->b` and `b-r2->c`, and also state the closure fact at
`key(a, r3)`. Two independent answers then exist for the same address — DERIVED by walking
the chain, RETRIEVED by reading the store.

    case             retrieved norm      cosine(derived, retrieved)
    absent            0.375 (sd 0.043)        -0.046 (sd 0.214)
    consistent        1.047 (sd 0.092)        +0.867 (sd 0.043)
    CONTRADICTION     1.060 (sd 0.089)        -0.025 (sd 0.151)

**The two signals do different jobs and neither substitutes for the other.**

**Norm answers "was anything written here"** — absent reads at a third of written, which is
decision 148's structurally-zero property doing its job. And the two written cases are
**indistinguishable by norm (1.047 against 1.060)**, which is correct rather than a
shortcoming: the store has no idea which of its facts are true.

**Cosine answers "does it agree"** — +0.867 against −0.025, a gap of 0.89 against standard
deviations of 0.043 and 0.151. Roughly six sd. Essentially perfect separation.

## The loop, now complete

    the SHORTLIST     the walk names every binding that produced an answer.
                      Structural, free, no propagation through a hidden graph
    "WRONG"           derived and retrieved disagree. Two reads, a dot product,
                      no label -- this note
    "WHERE"           blame accumulates on bindings that recur in wrong answers.
                      0.900 rank-#1 against chance 0.050 -- note 079

**All three are local, bounded and label-free**, which is what C1 and the primary goal both
require. §5 asked for the scheme to be argued against C1/C2/C3 before the substrate; the
substrate turned out to already serve it.

## And the two pieces compose in the right direction

A contradiction says *"one of these is wrong"* and never which. **That is exactly the input
blame wants:** blame does not need to know which side is right, only that this answer failed,
and it resolves the *which* over many questions instead of in one shot. The signals are
complementary rather than redundant, and neither is doing the other's job badly.

## What is NOT claimed

**Not on a branching graph.** The derived side is a 2-hop walk on a clean chain, so a
disagreement can only mean a wrong fact. Where the graph branches, the walk may be wrong
because it chose badly — a different error, with a different owner, and one this fixture
cannot produce.

**Not at scale.** Cosine 0.867 rather than 1.0 is interference from 17 background facts.
Whether the gap survives a store holding thousands is unmeasured, and it is the obvious way
this fails.

**Not that a contradiction means the STORE is wrong.** The derivation can be the wrong one.
Deciding which to change — and note that writing the derived value back is what note 068
warned makes a wrong derivation into a premise — is untouched here.

**And nothing is wired together.** Three measured pieces, no loop running. Building it is the
next step and it is a build rather than a question.
