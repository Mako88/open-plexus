# Option record — a better similarity index

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/content.py` — `ContentIndex`, which proposes neighbours and addresses
  nothing.
- `openplexus/tasks/families.py`, the only instrument where entities resemble each other,
  and therefore the only one where index quality can be varied at all.

---

## What was tried, and what came back

### The index's quality bounds the answer — `note 056`

    CONFIG  when    2026-07-29
            source  note 056
            script  unrecorded
            task    families, set-valued question
            model   gated collection over index-proposed neighbours
            knobs   index purity degraded deliberately
            scale   unrecorded

    purity ~0.99   the enumeration works
    purity 0.951   0.750
    purity 0.795   0.167

**So the grouping's quality bounds the answer**, which is a far more tractable target than
re-keying the store, and it is measured rather than argued. This is what moved the index
from a nicety to something load-bearing.

### Purity looks like the sufficient statistic, and that is a hypothesis — `note 057`

    CONFIG  when    2026-07-29
            source  note 057
            script  unrecorded
            task    families, two different degradations
            model   as above
            knobs   starving the index of data, against families sharing attributes
            scale   one matched pair, n=12

Two very different routes to the same purity land answer quality in the same neighbourhood
— **0.417 and 0.333 at purity ~0.70**. Recorded in the note as *a hypothesis, not a result*:
one matched pair at n=12, unseparated from noise.

### Overlap does not break the index; it makes purity expensive — `note 057`

    CONFIG  when    2026-07-29
            source  note 057
            script  unrecorded
            task    families with shared attributes
            model   as above
            knobs   attributes shared 3 of 4; stream count 1 against 10
            scale   unrecorded

With full data **one private attribute suffices** — purity 0.997 while sharing three of
four. At ten streams, sharing three of four costs **0.28 purity and 0.50 exact**.

### Real word co-occurrence has no cliff — `note 058`

    CONFIG  when    2026-07-29
            source  note 058
            script  unrecorded
            task    real co-occurrence statistics against the families task
            model   similarity profile of a content-word slice
            knobs   weighting off, content-word slice, centring confirmed, shuffled control
            scale   four confounds closed; shuffled control at 0.002

Largest gap **0.059** against the task's **0.424**. **At no setting is the profile
bimodal** — language decays in steps of 0.02–0.03 where the task falls 0.45 at once. This
is a measurement about the *data*, not about the index, and it is what says a
cliff-detecting enumeration bound needs both purity ≳0.99 and bimodality, with one real
dataset supplying neither.

### Why the instrument had to exist first — `note 048`, `143`

    CONFIG  when    2026-07-29
            source  note 048, decision 143
            script  unrecorded
            task    families
            model   n/a
            knobs   none
            scale   n/a

Every other instrument's entities are arbitrary, so nothing resembles anything and index
quality is not a variable that exists. `families.py` is why the rows above are measurable
at all, and decision 143 is the first result taken through it.

**Still untried: what makes an index GOOD**, as opposed to what makes a grouping hard —
which is now measured on two axes.
