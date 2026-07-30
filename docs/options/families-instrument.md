# Option record — `families.py`

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/tasks/families.py`, dependency-free like the rest of the ruler.
- `tests/test_families.py`, `tests/test_family_set_queries.py`, `tests/test_grouping.py`.

---

## What was tried, and what came back

### It is the only instrument where things RESEMBLE each other — `note 048`

    CONFIG  when    2026-07-29
            source  note 048
            script  openplexus/tasks/families.py
            task    n/a -- the instrument itself
            model   n/a
            knobs   family_size, n_values, exception rate, attribute sharing
            scale   n/a

**Every other instrument's entities are arbitrary, so nothing resembles anything** — and a
concept is only meaningful where two things can be alike. That is why index purity, family
size and exception rate are variables here and do not exist elsewhere.

### Its first result — `143`

    CONFIG  when    2026-07-29
            source  decisions 143 and 144
            script  experiments/g19_01_can_grouping_answer_what_was_never_stated.py
            task    families, homogeneous
            model   grouping discovered, store addressed by it
            knobs   grouping on against off
            scale   3 seeds

Grouping answers what was never stated: transfer **0.9983** against 0.0608 ungrouped. The
first result for `concepts.py`.

### And it is where the set-valued question came from — `166`

    CONFIG  when    2026-07-29
            source  decision 166
            script  unrecorded
            task    families, a question a single token cannot answer
            model   n/a -- an instrument change
            knobs   none
            scale   n/a

The first question in the repository that a single token cannot answer, which is what made
component 6 measurable at all. Records: [set-of-tokens.md](set-of-tokens.md) and
[gated-collection.md](gated-collection.md).

### It also carries the exception and index-quality axes

    CONFIG  when    2026-07-29
            source  decisions 144, 145, 148, 149 and notes 056-057
            script  unrecorded
            task    families
            model   various
            knobs   exception share; index purity; family_size; n_values
            scale   3 seeds and up

Everything measured about `ByConcept`, the occupancy gate and the enumeration bound was
measured here, because they are the questions this instrument makes askable.
