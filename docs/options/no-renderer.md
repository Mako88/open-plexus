# Option record — no renderer, for programmatic use

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- The answer set itself, as `openplexus/answers.py` scores it. Rendering is a separate step
  that a caller may skip entirely.

---

## What was tried, and what came back

### A scope position rather than a result

    CONFIG  when    2026-07-29
            source  note 052 section 3
            script  none -- nothing to build
            task    none
            model   n/a
            knobs   none
            scale   n/a

*No measurement.* For a query API or an agent tool, a set of typed bindings is **better**
than a sentence — it is machine-readable, it cannot be embellished, and it carries the walk
that produced it.

So rendering is optional for a whole class of uses rather than merely deferrable, and that
is what makes component 7 non-blocking for everything above it. Recorded as an option
because "we have not built the renderer yet" and "a renderer is not required here" are
different states, and only one of them is a gap.
