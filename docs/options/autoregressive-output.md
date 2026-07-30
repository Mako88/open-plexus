# Option record — autoregressive output

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing. No output loop feeds an emitted token back in.

---

## What was tried, and what came back

### It is NOT ruled out by GOALS §2, and conflating the two would be a rule misapplied — `note 052 §3`

    CONFIG  when    2026-07-29
            source  note 052, GOALS.md section 2
            script  none -- a scope statement
            task    none
            model   n/a
            knobs   none
            scale   n/a

GOALS §2 forbids next-token prediction **as the TRAINING OBJECTIVE**. That is a different
thing from autoregression as an output *mechanism*, and note 052 says so explicitly rather
than letting the rule be applied by reflex.

### What actually argues against it is TERMINATION — `note 052 §3`, `148`

    CONFIG  when    2026-07-29
            source  note 052, decision 148
            script  none -- a design comparison
            task    none
            model   gated walk against an emitted-token loop
            knobs   none
            scale   n/a

Under a gated walk, the walk stops where the occupancy gate reads **structurally zero** and
nothing is fitted. Under autoregression, stopping is a **learned end-token** — one more
thing to train, on an objective that does not otherwise exist here.

That is the whole comparison: the two candidates differ less in what they emit than in how
they know to stop.

### Its blast radius reaches BACKWARDS — `note 052 §3`, `163 §3`

    CONFIG  when    2026-07-29
            source  note 052, decision 163
            script  none -- a scope statement
            task    every task in the repository
            model   n/a
            knobs   none
            scale   n/a

Every task, every accuracy number and the whole scoring convention assumed one answer token.
Whatever is chosen, the existing tasks stay valid as *capability probes* but stop being
measurements of the goal. Decision 165's ruler is what makes that survivable — it degenerates
exactly on singletons, so old numbers remain comparable. Record:
[set-of-tokens.md](set-of-tokens.md).
