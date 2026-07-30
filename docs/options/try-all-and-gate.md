# Option record — try every relation, keep the one whose address is not empty

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing of the mechanism. Both halves it would be assembled from exist and are measured:
  the occupancy gate (`inherit`) and `openplexus/search.py`'s candidate enumeration.

---

## What was tried, and what came back

### Proposed, and John called it the possible end solution — `163 §2`, `note 052 §2`

    CONFIG  when    2026-07-29
            source  decision 163, note 052
            script  none -- nothing built
            task    none
            model   the gate as decision 148 left it
            knobs   none
            scale   n/a

It costs `r` reads and needs **no new mechanism at all** — it is the gate doing selection
again, which is the one selection rule in this project that has ever worked. John:
*"I like your try-all-and-gate… as potentially the actual end solution"*, with the layout
version measured first.

### Its viability is a property of RELATION DENSITY, and the dense case is refuted — `108`

    CONFIG  when    2026-07-28
            source  decision 108, openplexus/search.py
            script  openplexus/search.py
            task    kinship and CLUTRR
            model   addresses keyed by `(subject, relation)` and `(FACT, subject)`
            knobs   none
            scale   unrecorded

The gate selects only where **exactly one** candidate address is occupied, and `search.py`
records the split:

    (subject, relation)   names one person 94.9% of the time
    (FACT, subject)       names one of several relations about half the time

So on ten relations it is undecided about half the time, which is why `search.py` exists.

**Where it works is where it is unnecessary** — few sparse relations, i.e. `families.py`,
where `hop_relations` already suffices — and where it is needed it is refuted.

### And note 090 reached the end by a different route — `note 090`

    CONFIG  when    2026-07-30
            source  note 090
            script  tools/generation_delta.py
            task    CLUTRR kinship
            model   generation delta learned from loop constraints
            knobs   none
            scale   9,074 puzzles, 20 relations

Supplying the DISPLACEMENT rather than choosing the relation closes the same ceiling, so
this option and the learned chooser are alternatives to a problem now solved another way.
Neither was ever measured, which is why they are not refutations. Record:
[generation-delta.md](generation-delta.md).
