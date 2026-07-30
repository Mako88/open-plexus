# Option record — fill a fixed frame of structured slots

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing.

---

## What was tried, and what came back

### It is not a peer of the other two — `note 052 §3`, `162`

    CONFIG  when    2026-07-29
            source  note 052, decision 162
            script  none -- a scope statement
            task    none
            model   n/a
            knobs   none
            scale   n/a

Note 052 listed three candidates for what an answer is: autoregressive, traversal, and
structured slots. **A fixed frame is a traversal with a fixed relation schedule**, which
decision 162 already calls a fitted constant — so it is not an independent option, it is
the traversal option with its schedule frozen.

That reduces the live choice to autoregression against a gated traversal, and what decides
between those is termination. Records:
[autoregressive-output.md](autoregressive-output.md) and
[gated-collection.md](gated-collection.md).

**Revival:** a domain where the frame genuinely is fixed and known, in which case the
"fitted constant" objection does not apply because the task supplies the schedule.
