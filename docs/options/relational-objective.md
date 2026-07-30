# Option record — a relational objective

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Every instrument in `openplexus/tasks/` except `corpus.py`: kinship, families, chains,
  closure, MQAR, and the CLUTRR loader.
- `GOALS.md §1` states it; `§2` makes next-token prediction an explicit non-goal.

---

## What was tried, and what came back

### The objective was the ceiling, not the memory — `047`

    CONFIG  when    2026-07-27
            source  decision 47
            script  unrecorded
            task    corpus, character level
            model   superposed store on a next-token objective
            knobs   none
            scale   unrecorded

**The only relation the store can express on a next-token objective is *"what followed
this"*, which is an n-gram** — and a counting table does that exactly and cheaply. So the
store had nothing to contribute that a simpler structure did not already do better, and
every measurement of it on that objective was measuring the objective.

### The store carries MQAR completely, and the text prior costs there — `142`

    CONFIG  when    2026-07-29
            source  decision 142
            script  unrecorded
            task    MQAR
            model   superposed store
            knobs   store on against off
            scale   unrecorded

    with the store      0.995
    without the store   0.000
    the prior that wins on text, applied here   costs 0.279

**MQAR is the only instrument that isolates the store from a prior**, which is what makes
it the store's control rather than another benchmark.

### At word level the store contributes nothing — `136`, `139`

    CONFIG  when    2026-07-29
            source  decisions 136 and 139
            script  unrecorded
            task    corpus, word level
            model   superposed store plus a learned prior
            knobs   store on against off
            scale   unrecorded

    with the store      9.185
    without             9.187

And `139`: the store's contribution on text is **exactly substitutable** by a learned prior.
Two ways of saying the same thing, and together they close the question of whether the text
objective could ever have shown what the store adds.
