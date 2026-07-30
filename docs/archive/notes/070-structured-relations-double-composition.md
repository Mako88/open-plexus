070 — Extensional relation structure doubles held-out composition
================================================================

**Status:** measured, 120 seeds, no leakage path. **It answers note 067's open question**
— *"binding, STRUCTURED vectors: untried, and it is the whole question"* — in the
affirmative, and it took five harness iterations to earn a number worth reporting.

**It also supersedes note 069's "tie" and corrects two claims I made to John mid-session**,
in both directions. Both corrections are recorded below rather than smoothed over, because
the sequence is the useful part.

---

## IN PLAIN TERMS

Every relation in this system is currently a **random** pattern: `father` and `mother` are
as unrelated as `father` and `7` ([keys.py:200](../../openplexus/keys.py:200) hashes the
token id). Note 067 measured the consequence — combining two random patterns to name what a
chain adds up to generalises at the rate of guessing.

**Replace the random patterns with ones derived from what each relation actually connects,
and held-out composition roughly doubles.** A relation is described by how other relations
attach to the people it links: `father` and `grandfather` come out neighbours, and every
one of the fourteen nearest-neighbour pairs is same-gender, because gender propagates
structurally through a kinship graph.

**That is the first properly controlled evidence in this project that GOALS §1's
requirement — knowing how concepts relate to one another — buys something measurable.**

---

## The measurement

62 base rules from CLUTRR train 2-hop puzzles. Hold out 25% (16 rules), build the profile
from that seed's TRAINING portion only, fit a ridge readout from
`concat(v(a), v(b), circular_convolve(v(a), v(b)))` to the answer relation, score nearest.
120 seeds, paired.

    EXTENSIONAL profiles     0.223
    random vectors           0.124
    PAIRED DIFFERENCE       +0.099   se 0.009   t = 11.6
    wins 76% of seeds, ties 18%, loses 6%

    chance 0.050, majority class 0.082

**Nearly double, and it loses on 6% of seeds.** 0 to 3 of the 20 relations are unplaced on
a given seed depending on which rules land in train, so the result is not an artifact of a
crippled vocabulary.

## The profile, and why the attachment type is the whole claim

For each edge `(a, r, b)`, every other edge sharing an entity contributes a feature naming
the other relation AND how it attaches:

    (a, s, x) -> (s, "HH")     r's head is s's head
    (x, s, a) -> (s, "HT")     r's head is s's tail
    (b, s, x) -> (s, "TH")
    (x, s, b) -> (s, "TT")

**Without the attachment type this is relation-relation co-occurrence, which note 058
measured flat.** With it, the profile separates roles. The collapsed control confirms the
distinction matters: 0.210 against positional's 0.248 under the earlier protocol, and its
cosines run uniformly higher (0.926 vs 0.775) — the signature of a space where everything
resembles everything.

## Five harness iterations, and each failure was caught by a stated control

    1  lstsq readout        trained 0.917 for extensional, collapsed AND random,
                            identical to 3dp. A fit measuring its own capacity:
                            46 rules against 136 features. Fixed with ridge

    2  random gave 0.125    note 067 measured 0.056, so the harness failed the
                            reproduce-a-known-number gate. Cause: `concat` lets the
                            readout identify which relation is in which slot and
                            learn MARGINALS -- worth 0.242 on its own (note 069)

    3  bind, P0 PASSED      random + hadamard = 0.056 on the 97-rule bootstrapped
                            set, i.e. note 067's rule count AND its number to three
                            decimals. The harness became trustworthy here.
                            **On the final 62-rule protocol the same arm gives
                            0.050**, which is chance and consistent, but the exact
                            match belongs to the 97-rule configuration and quoting
                            it against the 62-rule run would be quoting the wrong
                            number at the right claim

    4  target leakage       profiles used the labelled query edge, so a 2-hop puzzle
                            wrote its OWN RULE into its target's profile. Caught by
                            a control, and it was the entire first effect

    5  the wrong control    story-edges-only flipped the sign to -0.057 -- but it
                            leaves all six target-only relations with ZERO profile,
                            so it measures a crippled vocabulary, not a clean one.
                            Per-seed profiles are the right fix: +0.099

**Iteration 3 is why any of this is reportable.** Reproducing 067's 0.056 exactly is what
separated "the representation helps" from "the harness is broken," and the project's rule
earned its keep for the second time.

## Two corrections to what I told John mid-session

**"First positive evidence that structured relations help" — I said it on the leaky
measurement, and withdrew it when the control fired.** The withdrawal was correct at the
time and the claim is now true again on a clean protocol. **Stating it before the leakage
control had run was the error**, not the claim itself.

**"It doesn't stand" was then too pessimistic.** I read `story-edges-only` as the clean
comparison when it was a crippled one. **A control that changes two things at once is not a
control** — it removed the leak and six relations' entire representation together.

## What is NOT claimed

**Not an end-task number.** This scores held-out RULE prediction, not CLUTRR accuracy. Note
066's fold gets 98.8% where rules exist and 52% coverage; this measures whether the missing
rules can be *guessed*, and 0.223 is far from filling the gap.

**Not a mechanism in this codebase.** [keys.py:200](../../openplexus/keys.py:200) still
hashes token ids and nothing here changes it. Wiring extensional profiles into the key
source is the build, and it faces the interference question note 067 split out: relations
are twenty and must be COMPARABLE, entities are thousands and must be EXACT.

**Not transferable off CLUTRR yet.** Kinship has unusually strong positional structure. A
domain where relations do not constrain each other's endpoints may profile flat, and note
058 already measured one that does.

**And the legibility cost is real.** `father then sister is aunt` is inspectable; "these
vectors are near each other" is not. That trade should be a recorded decision, since the
route-is-the-reason property is one of this architecture's genuine advantages.
