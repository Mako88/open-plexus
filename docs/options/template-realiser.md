# Option record — the template realiser

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/render.py` — deterministic, dependency-free, and **structurally incapable of
  adding a fact**.
- `tests/test_render.py`, including the `content_words` bar and the empty-set decline.

---

## What was tried, and what came back

### Built as a floor, deliberately, so the rungs above have something to fail against

    CONFIG  when    2026-07-29
            source  openplexus/render.py, decision 163 section 3
            script  tests/test_render.py
            task    none -- a rendering property, asserted by test
            model   template realiser over a concept set
            knobs   none
            scale   unit tests, no experiment

*No measurement, and that is the point.* This is a floor rather than a mechanism with a
number. What it contributes is a **bar**:

    content_words(render(...)) - FRAME  must EQUAL the answer set

so **dropping a value fails as well as inventing one**, and `FRAME` is a fixed 25-word list
a reader can check in full. Written where it is trivially true, so anything that replaces it
is graded against a standard rather than on how well it reads.

John's ruling: templates first.

### An empty set DECLINES rather than rendering a hole

    CONFIG  when    2026-07-29
            source  openplexus/render.py
            script  tests/test_render.py
            task    none -- a rendering property
            model   as above
            knobs   none
            scale   unit tests

The surface for the archived ledger's row C4 if anything ever earns it. Record:
[declining-to-answer.md](declining-to-answer.md).

### Why the whole component is off the critical path — `note 052 §3`

    CONFIG  when    2026-07-29
            source  note 052
            script  none -- a scope statement
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

**Blast radius near zero.** A concept walk does not emit English, and the split — a
traversal that decides *what* to say, a realiser that decides *how* — is the field's own
two-stage generation shape. The hazard is specific and it is why the concept set stays the
scored artifact: **a fluent renderer can produce the right sentence from a wrong walk.**
