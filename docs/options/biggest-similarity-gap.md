# Option record — bound the enumeration by the biggest similarity gap

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- The rule as measured in `167` and `note 056`: an argmax over consecutive similarity gaps,
  not a threshold — the same move decision 148 made when it replaced a tuned bar with a
  structurally-zero read.
- `LocalMemoryConfig.index_branches` and `index_sharpness` are the knobs it sits behind.

---

## What was tried, and what came back

### It matches the best fixed count without being told the size — `167`

    CONFIG  when    2026-07-29
            source  decision 167, and notes 056 and 058 for the cliff's width
            script  unrecorded
            task    families, index purity 1.000
            model   argmax over consecutive similarity gaps
            knobs   look ahead 4, 6 and 16; family sizes 3 to 6
            scale   cliff about 0.45 wide against within-family steps of ~0.01

Matches the best fixed `branches` at family sizes 3–6 **without being told the size**, where
no single fixed value works across all of them.

**`look` is a CEILING, not a target.** Flat from 6 to 16, but **0.500 at look=4 for a family
of 6** — so it must exceed the group, and setting it too low is the one way to break it.

The cliff it exploits is ~0.45 wide against within-family steps of about 0.01, which is what
makes an argmax over gaps well-posed here at all.

### Degrade the grouping and it falls FASTER than a fixed count — `note 056`

    CONFIG  when    2026-07-29
            source  note 056
            script  unrecorded
            task    families, index purity degraded
            model   gap rule against fixed branches
            knobs   purity 0.795, 0.951 and >= 0.99
            scale   unrecorded

    purity   gap rule   fixed
     0.795      0.167   0.417
     0.951      0.750   1.000
    >=0.99      level    level

**Why:** given the count, a noisy ranking can only hand you wrong *candidates*. Deriving the
count, it hands you wrong candidates **and** a wrong count — two error sources against one.
The tell is over-emission: size 2.58 against a true 2.00, precision 0.708.

So this is a measured **crossover**, not a loser, and which one is right is a property of the
grouping's quality rather than of either mechanism. Record for the other side:
[fixed-branches.md](fixed-branches.md).

### Real language provides a slope where the rule needs a cliff — `note 058`

    CONFIG  when    2026-07-29
            source  note 058
            script  unrecorded
            task    real word co-occurrence against the families task
            model   similarity profile over a content-word slice
            knobs   weighting off, content-word slice, centring confirmed, shuffled control
            scale   four confounds closed; shuffled control at 0.002

    largest gap, real co-occurrence   0.059
    largest gap, the task             0.424

**At no setting is the profile bimodal.** Language decays in steps of 0.02–0.03 where the
task falls 0.45 at once. So the crossover needs purity ≳0.99 **and** bimodality, and one
real dataset supplies neither.

**The shape is the finding, not the number** — a cliff rule needs a cliff.

### It resolves a hub-and-spoke tension a fixed count CANNOT express — `g33-04`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g33-04-does-a-derived-bound-fix-the-star.txt
            script  experiments/g33_04_does_a_derived_bound_fix_the_star.py
            task    occasions, 64 concepts, 8,000 occasions, pairings complete/chain/star
            model   grounding.cliff over conditional scores; look 16; no join
            knobs   bound in {2, 3, derived}, surfaces 3-5; 3 seeds
            scale   uniform frequency, 1 distractor

**A second, independent argument for this option from a different direction.**
`g33-02` found a single global bound cannot express a star: the hub needs a bound
at least its own degree while a spoke needs one, and no value is both.

Derived, on `star` at five surfaces: bridged **0.8429** and f1 **0.9202**, against
a fixed 2's **0.1667** and **0.4847**, and a fixed 3's collapse to a largest class
of **0.8962** of all surfaces. Largest for the derived arm never exceeds
**0.0208** in any of nine cells, so **the rule is self-limiting** where a fixed
bound large enough for the hub is not.

On `complete` it matches the best fixed bound at 3 and 4 surfaces and reaches
**1.0000** at 5 where the best fixed value in that grid reaches 0.9236 — the
decision-167 property reproducing on a different world and a different statistic.

**It loses on chains and the cause is stated:** a mid-chain surface's two true
neighbours score asymmetrically, because the rarer one scores higher, so the
biggest gap falls between them rather than after them. Bridged degrades
**0.9792, 0.7344, 0.6215** with length against a fixed 2's flat 1.0000 — while f1
is HIGHER at every length, because the fixed bound over-links a leaf. A trade,
not a loss.

### The rule's answer on an even slope is decided by FLOATING POINT — `test_grounding`

    CONFIG  when    2026-07-31
            source  tests/test_grounding.py, TheCliff
            script  none -- a property of the arithmetic, asserted in a test
            task    n/a
            model   grounding.cliff
            knobs   none
            scale   n/a

`[0.5, 0.4, 0.3, 0.2, 0.1]` returns 2 and `[5.0, 4.0, 3.0, 2.0, 1.0]` returns 1 —
the same ranking with the same gaps, because `0.5 - 0.4` and `0.4 - 0.3` differ in
binary and an argmax over gaps has nothing else to separate them.

Note 058 measured real co-occurrence as exactly that shape. **So on slope-shaped
data this rule does not merely degrade: its output is determined by representation
noise**, and a result taken from it there would be unreproducible for a reason no
seed controls. Asserted rather than commented so the caution cannot be read as
theoretical.

### The ranking IS bimodal on stimuli we did not design — `g34-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g34-01-external-word-learning-trials.txt
            script  experiments/g34_01_external_word_learning_trials.py
            task    xsl.py -- 29 published cross-situational word-learning conditions
            model   grounding.cliff over conditional scores; look 16
            knobs   bound in {1, derived}
            scale   12-18 pairs, 18-81 trials per condition

**The question `g33-04` left open.** Mean largest gap across the 26 solvable
conditions is about **0.5** — the same order as the designed families task's
0.424, and an order of magnitude above note 058's **0.059** for real language.

So the cliff this rule needs exists outside a world this project built, and the
derived bound beats a fixed one there: **0.9569** mean f1 against **0.9007**, and
26 of 29 conditions exact against 14.

**The `filt3E`–`filt9E` family is where it earns that**, and the mechanism is the
one `g33-04` named on chains. Those conditions cut at **2.0** rather than the
true 1, and score **1.0000** where a fixed bound of 1 scores **0.8333** — the
second candidate enters the ranking and is then removed by mutuality, while a
true partner whose score is asymmetric gets in where a cut of 1 excluded it.

**And the caveat that survives.** These are laboratory experiments designed to be
learnable from co-occurrence. Note 058's slope remains the only measurement on
natural language, and nothing here bridges the two.

### It is what makes MORE MODALITIES safe rather than harmful — `g36-02`

    CONFIG  when    2026-08-01
            source  experiments/sweeps/g36-02-do-more-modalities-learn-faster.txt
            script  experiments/g36_02_do_more_modalities_learn_faster.py
            task    occasions, 64 concepts, presence 0.7, noise 3, 1 distractor
            model   conditional; bound fixed 2 or derived; no join
            knobs   surfaces 2/3/5/8, occasions per concept 2..64; 3 seeds
            scale   floor-free `connected`, bar 0.95

**A third independent argument for this option**, after `g33-02`'s hub and
`g33-04`'s grid.

Under a FIXED bound of 2, adding modalities **caps** what can ever be learned: 5
surfaces plateaus at **0.7531** and 8 at **0.5781**, and neither improves from 16
occasions per concept to 64 — thirty-two times the data buys nothing. 2 and 3
surfaces reach **1.0000** on the same axis.

Under the derived bound every surface count reaches 1.0000, and more modalities
is better at every stream length: **0.3542, 0.4201, 0.4969, 0.5677** at the
sparsest column.

**So the failure is a ceiling rather than a delay**, and experience does not
repair it. The derived bound is what makes multimodality — which `GOALS.md`
requires — compatible with the mechanism at all.

**And the two bounds do NOT cross on this axis**: derived is equal or better in
all 24 cells. `g33-04`'s measured crossover is on the PAIRING STRUCTURE axis
(chains), so the reason both arms are kept is that one and not this one.
