# Option record — declining to answer

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- One surface for it: `openplexus/render.py` **declines** on an empty answer set rather than
  rendering a hole.
- Nothing that decides to decline, and no task that scores abstention.

---

## What was tried, and what came back

### Nothing lets the model say "I do not know"

    CONFIG  when    2026-07-29
            source  docs/archive/architecture-ledger-2026-07-29.md, row C4
            script  none -- nothing built
            task    no task scores abstention
            model   the gate is a fact about the store, not a learned probability
            knobs   none
            scale   n/a

The archived capability ledger's row C4. **No mechanism decides to abstain and no
instrument would notice if one did**, so any claim about honesty here is untested.

The nearest thing that exists is the occupancy gate, and it is a different kind of object:
it reports whether anything was ever written at an address, which is a fact about the store
rather than a confidence in an answer. That is what makes it trustworthy where a learned
probability would not be, and also what makes it unable to say "I do not know" about a
question whose addresses are all occupied.

### The renderer's half of it — `openplexus/render.py`

    CONFIG  when    2026-07-29
            source  openplexus/render.py
            script  tests/test_render.py
            task    none -- a rendering property, asserted by test
            model   template realiser
            knobs   none
            scale   unit tests

An empty set **declines** rather than rendering a hole. It is the surface row C4 would use
if anything ever earned it, written where it is trivially true so the rungs above have
something to fail against. Record: [template-realiser.md](template-realiser.md).
