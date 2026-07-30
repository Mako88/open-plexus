# Option record — the 16k-character wall is a property of the OBJECTIVE

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/tasks/corpus.py`, the instrument every measurement here was taken on.
- Nothing was built to fix the wall, because decision 115 established there is nothing at
  that address to fix.

---

## What was tried, and what came back

### A character bigram table is intrinsically low-rank — `115`

    CONFIG  when    2026-07-28
            source  decision 115
            script  unrecorded
            task    corpus, character level, 66 symbols
            model   widths 32, 64, 128 and 256
            knobs   width
            scale   effective rank measured at every width

**Effective rank ~3 at every width.** In the entry's words, *"the store is not failing to
use its width. There is nothing there to use."* Sixteen thousand characters is how long it
takes to estimate a bigram table.

### The competitors were eliminated by name — `115`, `109`, `110`, `114`

    CONFIG  when    2026-07-28
            source  decisions 115, 109, 110 and 114
            script  unrecorded
            task    corpus, character level
            model   store, readout and value projection each probed directly
            knobs   width; value_lr
            scale   unrecorded

    store capacity              109   capacity scales as d^2, above task demand
    readout capacity            110   2.00 items per dimension, above task demand
    persistent representation   114   value_lr works and does not move the plateau

Rule 2's shape: each component probed directly rather than inferred from the end-to-end
number.

### Width is NOT flat, so a width sweep tests a claim nobody makes — `113`

    CONFIG  when    2026-07-28
            source  decision 113
            script  unrecorded
            task    corpus, character level
            model   our arms across widths 16 to 128
            knobs   width
            scale   R^2 0.92

    d=16    5.730
    d=128   5.494

The arms improve with width. Decision 112's saturation hypothesis was aimed at the wrong
axis, and 113 is where that was established.

### The pattern this entry is really about — `170`

    CONFIG  when    2026-07-29
            source  decision 170
            script  none -- an audit of the log
            task    n/a
            model   n/a
            knobs   none
            scale   three wrong recommendations in one day

115's closure lived in one place and nothing pointed at it. Note 042 then built an
architecture case on the same wall; decision 133 ran its falsifier, was refuted, and
**relabelled the wall a "capacity limit"**; decision 134 superseded 133's follow-on one
entry later. **A ratchet on proposals does not catch a re-label after the fact** — which is
the argument the option tree exists on.
