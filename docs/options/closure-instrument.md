# Option record — `closure.py`

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/tasks/closure.py` — an unmarked stream of stated and entailed facts, with **no
  question marker**.
- `tests/test_closure.py`.

---

## What was tried, and what came back

### The stated/entailed split IS the recall/reasoning split

    CONFIG  when    2026-07-29
            source  openplexus/tasks/closure.py, decision 95
            script  openplexus/tasks/closure.py
            task    n/a -- the instrument itself
            model   n/a
            knobs   none
            scale   n/a

Because nothing marks which facts were stated, a model cannot tell the two apart at read
time, so scoring them separately separates recall from inference **without the task telling
the model which is which**. That is the design property, and it is what the marker in the
earlier instruments was contaminating.

`095` measured the marker as most of the remaining gap, which is exactly what this removes.

### It passes G0 with headroom — `g14-01`

    CONFIG  when    2026-07-29
            source  g14-01
            script  experiments/g14_01_does_closure_pass_g0.py
            task    closure
            model   the standard arm against a frozen control
            knobs   learning on against off
            scale   unrecorded

**Entailed headroom 0.277 against a frozen 0.000.** A gate check needs a control that
cannot succeed and an arm that can, and this is the pair.

### And it is the layout under which ownership behaves — `note 072`

    CONFIG  when    2026-07-30
            source  note 072
            script  unrecorded
            task    CLUTRR, closure layout against kinship layout
            model   ownership as `previous_concept = tokens[t-1]`
            knobs   layout
            scale   7,132 traversal bindings

**0.0% of traversal bindings relation-owned** under the closure layout, against 100.0%
under kinship. Recorded here because the instrument's layout turned out to be a distributed
property as well as a task property, which nobody expected when either was chosen. Record:
[concept-partitioning.md](concept-partitioning.md).
