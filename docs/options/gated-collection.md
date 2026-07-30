# Option record — emit by gated collection over index-proposed neighbours

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- The mechanism assembled in `167`: `ContentIndex` proposes neighbours, the occupancy gate
  admits, `answers.py` scores the resulting set.
- `tests/test_family_set_queries.py`.

---

## What was tried, and what came back

### The first set answer this project has ever produced — `166`, `167`

    CONFIG  when    2026-07-29
            source  decisions 166 and 167
            script  unrecorded
            task    families, a question a single token cannot answer
            model   index proposes, gate admits, set scored by exact and F1
            knobs   branches
            scale   unrecorded

`166` gave `families.py` a set-valued question — the first in the repository. `167` is the
mechanism that answers it.

### And it is decision 146's refuted mechanism, unchanged — `146`, `147`, `167`

    CONFIG  when    2026-07-29
            source  decisions 146, 147 and 167
            script  unrecorded
            task    families, single-answer then set-valued
            model   the same read-both-addresses mechanism in both
            knobs   none
            scale   unrecorded

`146` found it can only average rather than select, and `147` refuted the two ways to
choose. **Neither objection applies to a set answer, because nothing has to be selected.**

**The refutation was about the question, not the mechanism** — which is the reusable part,
and the reason a refuted option stays in the tree behind a switch rather than being
deleted.

### What it rests on, and it is a constant — `167`

    CONFIG  when    2026-07-29
            source  decision 167
            script  unrecorded
            task    families, family sizes 3 to 6
            model   as above
            knobs   fixed branches
            scale   unrecorded

The enumeration is bounded by a supplied `branches` count, and the peak sits at
`family_size − 1` in every row, collapsing either side — 1.000 → 0.500 → 0.083. **So the
answer's quality rests on being told how many things to emit**, which is the gap the two
bound-choosing options address:
[biggest-similarity-gap.md](biggest-similarity-gap.md) and
[fixed-branches.md](fixed-branches.md).

### The gate cannot supply the bound — `167`

    CONFIG  when    2026-07-29
            source  decision 167
            script  unrecorded
            task    families
            model   occupancy gate
            knobs   none
            scale   unrecorded

**The sketch knows emptiness, not relevance.** It cannot bound an enumeration over
addresses that are all occupied, which is exactly the case here. Record:
[inherit-gate.md](inherit-gate.md).
