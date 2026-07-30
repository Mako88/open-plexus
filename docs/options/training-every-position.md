# Option record — training on every position

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.gate_objective` and `gate_reads_key`, which are what decisions 96 and
  98 added.

---

## What was tried, and what came back

### It costs composition, 1.000 to 0.40 — `095`

    CONFIG  when    2026-07-28
            source  decision 95
            script  unrecorded
            task    composition over chains
            model   delta rule at scored positions against every position
            knobs   scored-only against all-position
            scale   unrecorded

**And the gate is not outvoted, it is CONFLICTED** — which is a mechanism problem rather
than a ratio problem, and it is what makes the next three entries a line of work rather
than a search for a better weighting.

### Letting the gate see WHERE it is triples all-position accuracy, and is still not enough — `096`

    CONFIG  when    2026-07-28
            source  decision 96
            script  unrecorded
            task    as above
            model   gate given positional information
            knobs   gate_reads_key
            scale   unrecorded

A **3×** improvement that does not close the gap. Recorded as insufficient in its own entry
rather than as a win, which is what stopped it being the fix.

### Density raises the level and does NOT remove the decay — `097`

    CONFIG  when    2026-07-28
            source  decision 97
            script  unrecorded
            task    as above
            model   as above
            knobs   scored-position density
            scale   unrecorded

The distinction that matters: a mechanism that moves the level and leaves the shape is not
addressing the cause. The same shape as decision 69's six mechanisms on text.

### Giving the gate its OWN objective is what removes it — `098`

    CONFIG  when    2026-07-28
            source  decision 98
            script  unrecorded
            task    as above
            model   gate trained against a separate objective
            knobs   gate_objective
            scale   unrecorded

**Do not re-propose all-position training without a separate gate objective.**

**Revival condition, and it is precise:** with `gate_objective` set, all-position training
is not refuted — 098 is the entry that says so. What is refused is the naive version, and
the four entries together are the reason a revival condition can be stated as a
configuration rather than as a hope.
