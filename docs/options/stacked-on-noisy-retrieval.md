# Option record — another mechanism stacked on noisy retrieval

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Four attempts, spread over decisions 102, 105, 107 and 111. The mechanisms that survived
  from them are recorded under their own options; what this record holds is the pattern.

---

## What was tried, and what came back

### Hops and pair keys do not compose, and the combination produced numbers anyway — `105`

    CONFIG  when    2026-07-28
            source  decision 105
            script  unrecorded
            task    kinship
            model   multi-hop retrieval with pair keys
            knobs   hops > 1 together with context_keys
            scale   unrecorded

The two mechanisms were combined, the run completed, and it reported figures. **The
combination was not doing what it appeared to be doing** — which is the failure mode this
repository's standards are written against, and the reason it is recorded as an option
rather than a footnote.

### Composition degrades under repeated entities, gracefully — `106`

    CONFIG  when    2026-07-28
            source  decision 106
            script  unrecorded
            task    kinship with entities repeated across facts
            model   multi-hop retrieval
            knobs   repetition rate
            scale   unrecorded

And **the 1.000 that preceded it was the degenerate case** — every entity appearing once.
Degradation is gentle rather than a cliff, which is the useful half.

### The traversal mechanism is not worth building — `107`

    CONFIG  when    2026-07-28
            source  decision 107, and decisions 121-122 for the expiry
            script  unrecorded
            task    kinship
            model   steps 1 and 3 at 0.710 and 0.677
            knobs   none -- an arithmetic argument
            scale   n/a

*"A perfect traversal buys 0.05."* Compounding those step accuracies leaves nothing for a
traversal to recover. **Condition expired at `121`/`122`**, where pair keys took step 1 to
1.000 at out-degree 1 and step 2 to 0.971 overall — and the traversal became worth building.
Record: [beam-search.md](beam-search.md).

### You cannot search your way out of noisy primitives — `111`

    CONFIG  when    2026-07-28
            source  decision 111
            script  unrecorded
            task    kinship
            model   as decision 107
            knobs   search_branches
            scale   n/a

*"The verifier is built from the primitives."* The same condition, and it expired the same
way.

### What the four have in common

    CONFIG  when    2026-07-28
            source  decisions 102, 105, 107 and 111
            script  none -- the synthesis
            task    n/a
            model   n/a
            knobs   none
            scale   four attempts against one ceiling

All four failed against the same **0.915 / 0.35** ceiling, for one reason rather than four:
**the fix is per-step fidelity, not another layer.** Two of the four conditions have since
expired, which is what a stated revival condition is for — the mechanisms became right when
their inputs moved, and neither could have been re-measured if the option had been deleted.
