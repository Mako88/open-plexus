# Option record — a composition sweep on chains as evidence about composition

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/tasks/chains.py`, which remains valuable as a control.

---

## What was tried, and what came back

### A chain is out-degree 1 by construction — `108`

    CONFIG  when    2026-07-28
            source  decision 108, and note 103 for the out-degree split
            script  openplexus/tasks/chains.py
            task    chains
            model   any composition mechanism
            knobs   none
            scale   n/a

Every step of a chain has exactly one continuation, so **there is nothing to choose** and
any mechanism that chooses correctly scores the same as one that does not choose at all.

That is the definition of an arm whose predicted outcome is guaranteed by how the condition
is built, which CLAUDE.md rule 10 says is not evidence however it comes out — and it will
read as confirmation.

`note 103` later measured the point directly from the other side: `search` is **worse than
not branching** at out-degree 1 (0.649 against a walk's 0.702), and only gains where
out-degree is ≥ 2.

**Do not re-propose.** **Revival:** none for chains. The instrument for this question is one
with genuine out-degree, which is kinship and CLUTRR. Record:
[beam-search.md](beam-search.md).
