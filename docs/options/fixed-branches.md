# Option record — bound the enumeration by a supplied `branches` count

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.index_branches`, an integer supplied by the caller.

---

## What was tried, and what came back

### The peak sits at `family_size − 1` and collapses either side — `167`

    CONFIG  when    2026-07-29
            source  decision 167
            script  unrecorded
            task    families
            model   gated collection over index-proposed neighbours
            knobs   fixed branches
            scale   every row of the family-size grid

    1.000 -> 0.500 -> 0.083

The peak is at `family_size − 1` in every row. **So the count is not a hyperparameter with a
good default; it is the answer's size, supplied.**

### It beats the derived bound wherever the grouping is imperfect — `note 056`

    CONFIG  when    2026-07-29
            source  note 056
            script  unrecorded
            task    families, index purity degraded
            model   fixed branches against the largest-gap rule
            knobs   purity 0.795, 0.951 and >= 0.99
            scale   unrecorded

    purity   fixed   gap rule
     0.795   0.417      0.167
     0.951   1.000      0.750
    >=0.99   level      level

Given the count, a noisy ranking can only hand you wrong *candidates*. Deriving the count,
it hands you wrong candidates **and** a wrong count. Two error sources against one, and they
draw level only at purity ≳ 0.99.

This is decision 74's shape again, which is what a kept pair is for: **which one is right is
a property of the grouping's quality, not of either mechanism.** Record for the other side:
[biggest-similarity-gap.md](biggest-similarity-gap.md).

### What the pair leaves open

    CONFIG  when    2026-07-29
            source  decision 167, note 056
            script  none -- a scope statement
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

The enumeration bound is **either supplied, or it needs a near-oracle grouping.** Neither is
answering from awareness, which is why the tree's row for component 6 is partial rather than
passing.
