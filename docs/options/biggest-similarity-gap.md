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
