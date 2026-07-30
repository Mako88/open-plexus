# Option record — `render.speak`, the retrieval realiser

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/render.py` — `speak`, and `spoken_faithfully`.
- `tests/test_render.py`, including the connection test that counts must MOVE the choice.

---

## What was tried, and what came back

### The words come from the CONCEPT MAP, not the caller

    CONFIG  when    2026-07-29
            source  openplexus/render.py
            script  tests/test_render.py
            task    none -- a rendering property, asserted by test
            model   surfaces supplied by `Shared.surfaces`
            knobs   surface-choice policy
            scale   unit tests, no experiment

*No measurement*, same reason as the template realiser. The model supplies its own
vocabulary and `render` arranges it: **no new model and no next-token prediction.**

`Shared.surfaces` had already stated the design problem — *"which surface to use is a choice
the concept itself does not contain"* — and this is where that choice gets made rather than
dodged.

### The default policy is arbitrary and says so

    CONFIG  when    2026-07-29
            source  openplexus/render.py
            script  tests/test_render.py
            task    none
            model   as above
            knobs   lowest token id; most frequent when counts are given
            scale   unit tests

Lowest token id: deterministic, and agrees across nodes, which is the property that matters
in a distributed setting. Most frequent wins when counts are supplied, **with a connection
test asserting that counts MOVE the choice** — rule 6's shape, because a policy that reads a
parameter and ignores it looks identical to one that works.

**Neither is the eventual answer.** With surfaces in several modalities the choice belongs to
the QUERY, and nothing is multimodal yet.

### `spoken_faithfully` is the same bar one level down

    CONFIG  when    2026-07-29
            source  openplexus/render.py
            script  tests/test_render.py
            task    none
            model   as above
            knobs   none
            scale   unit tests, checked in both directions

Whether a CONCEPT was invented rather than a word — and **checked in both directions**,
because a realiser that dropped a concept passes any invents-nothing test trivially. That
paired assertion is the same rule the C1 locality test needed.
