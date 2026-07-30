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

### It passes G0 with headroom — `g14-01`, ON ONE SEED

    CONFIG  when    2026-07-29
            source  g14-01
            script  experiments/g14_01_does_closure_pass_g0.py
            task    closure, 10 people, 24 stated edges, 6 entailed
            model   attention at width 128 / 16 epochs; local arms at width 256
            knobs   learning on against off
            scale   ONE SEED, run before dispatch. The 4x8 matrix had not
                    returned when this was written

**Entailed headroom 0.277 against a frozen 0.000.** A gate check needs a control that
cannot succeed and an arm that can, and this is the pair.

**The qualifier, added 2026-07-30, and the `scale` field is where it should have been from
the start.** It read `unrecorded`. The number is the single seed the sweep record prints
under *"THE SINGLE SEED, RUN BEFORE DISPATCH"*, and that record's own status line still
reads `ANSWER: pending`. `GOALS.md` §4 says no gate is passed on a single run, and
`CLAUDE.md` rule 3 says the same thing in general — so *"passes G0"* is currently a
prediction wearing a result's clothes, in two documents.

Found while choosing an instrument, not by a check: nothing verifies that a sweep record
whose status is `pending` is not being cited as settled.

### The matrix returned, and it reproduces — `g14-01`

    CONFIG  when    2026-07-30
            source  g14-01
            script  experiments/g14_01_does_closure_pass_g0.py
            task    closure, 10 people, 24 stated edges, 6 entailed
            model   attention width 128 / 16 epochs; local arms width 256
            knobs   four arms -- majority, frozen, local, attention
            scale   8 of 8 seeds, 32 cells, run 30573507385

    arm                      stated            entailed
      majority       0.100 +/-0.002      0.190 +/-0.009
      frozen         0.000 +/-0.000      0.000 +/-0.000
      local          0.098 +/-0.002      0.108 +/-0.005
      attention      0.097 +/-0.002      0.282 +/-0.011

The pre-dispatch seed reproduces — nothing moved by more than **0.011** — which is what
rule 3 asks for and is the one thing here that came out better than expected.

**P2 is REFUTED.** The strong reference beats the base rate by **+0.092**, against a
prediction registered in advance of **>0.15**.

**P3, the gate, is CONFIRMED at 0.282 — against a control scoring exactly 0.000.** A random
`Wo` emits a token that is essentially never the right relation, so it is a floor the way a
wall is a floor. The honest floor is `majority`, and against that the usable band is
**0.092 wide** with the reference's standard error at **0.011**.

Both readings are recorded rather than one chosen. GOALS §4 is satisfied by the letter and
the spirit is thinner than the verdict sounds, and the difference between them is what
decides whether this instrument can host a comparison between two training objectives.

### The local rule is BELOW the base rate on the reasoning half — `g14-01`

    CONFIG  when    2026-07-30
            source  g14-01
            script  experiments/g14_01_does_closure_pass_g0.py
            task    closure, entailed targets only
            model   our model under the delta rule, width 256
            knobs   learning on
            scale   8 seeds, spread +/-0.005

**0.108 against a majority floor of 0.190, on every one of eight seeds.**

P5 predicted `local` would land between `frozen` and `attention`, and it does. What no
prediction asked was which side of the BASE RATE it would land on. A model scoring under
*"always answer the commonest relation"* is not composing weakly — it is actively
mispredicting, and would do better having learned nothing.

Against the frozen control it takes 38% of the available headroom. Against the base rate it
takes **−0.082 of the 0.092 available.** The second ratio is the one that describes what
this project has achieved on the first task it built for itself.

Not a defect in the experiment — it is the measurement the experiment existed to take, and
it points at [wo-only-delta-rule.md](wo-only-delta-rule.md): `Wo` alone learns, `Wk` and
`Wv` are frozen random.

### The record was cited as settled while its status read `pending`

    CONFIG  when    2026-07-30
            source  g14-01
            script  none -- a process finding, nothing measured
            task    none
            model   n/a
            knobs   none
            scale   n/a

Before the matrix ran, `DECISIONS.md` and this record both carried *"passes G0 with entailed
headroom 0.277"* — the single pre-dispatch seed — while the sweep record's own status line
read `ANSWER: pending`. This record's `scale` field read `unrecorded`, which is the field
that would have caught it.

Found while choosing an instrument, not by any check. **Nothing verifies that a sweep record
whose status is `pending` is not being quoted as settled**, and that is a checkable property:
`check_provenance.py` already resolves every measurement to a cited source, so it could read
that source's status line at the same time.

The numbers held, so this cost nothing — which is the same shape as
[note 105](../archive/notes/105-the-partitioning-accuracy-figure-has-no-source.md), and the
reason it is worth writing down rather than quietly fixing.

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
