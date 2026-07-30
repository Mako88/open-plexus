# Option record — `hop_accumulate`: `concat` against `bind`

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.hop_accumulate` — `replace`, `concat`, `bind`.
- `docs/SCALE.md` carries the row for this choice, with its revisiting trigger.

---

## What was tried, and what came back

### `concat` wins, and the reason is a property of having FEW RULES — `SCALE.md`

    CONFIG  when    2026-07-29
            source  docs/SCALE.md
            script  unrecorded
            task    kinship
            model   16 rules, 10 relations, 128-wide
            knobs   hop_accumulate concat against bind
            scale   unrecorded

    concat   1.000
    bind     0.812

**But sixteen rules in a 128-wide space are linearly separable whatever the labels do.**
That is a property of the rule count, not evidence that concatenation is the right
operation, and nothing in the result says so — which is why the row is in `SCALE.md` with
a trigger rather than recorded as a settled win. `bind` is kept as the measured alternative
for exactly this reason.

### The trigger is MET — `note 063`

    CONFIG  when    2026-07-29
            source  note 063
            script  unrecorded
            task    CLUTRR gen_train23_test2to10
            model   readout over a whole chain against a fold over pairwise rules
            knobs   none -- a property of the data
            scale   1,393 distinct chains

`SCALE.md`'s stated trigger was *"a rule table in the hundreds"*. CLUTRR has **1,393
distinct chains**, and **99.8% of test chains are unseen** while only 6.6% of adjacent
PAIRS are. A readout over a whole chain must generalise to what it never saw; **a fold over
pairwise rules only asks what it was trained on**, median 144 times each.

### Note 066 corrects 063 in both directions — `note 066`

    CONFIG  when    2026-07-29
            source  note 066
            script  unrecorded
            task    CLUTRR, the fold over pairwise rules
            model   pairwise rule table built from the task's own labels
            knobs   none
            scale   4,076 two-hop answers, 62 unambiguous; 603 puzzles scored

**Intermediates are NOT unlabelled.** A two-hop answer IS a labelled pairwise rule — 4,076
of them, 62 unambiguous — and three-hop puzzles label `(derived, base)`, so the task
supplies its own curriculum.

**But 063's "6.6% unseen" counted the wrong thing.** It counted *stated* pairs, where the
fold needs `(accumulated, next)` with the accumulated side **derived**: **120 asked for, 97
derivable**, converged in two rounds.

### The fold is right where it can act, and completes about half the time — `note 066`

    CONFIG  when    2026-07-29
            source  note 066
            script  unrecorded
            task    CLUTRR, per hop bucket
            model   fold over pairwise rules
            knobs   none
            scale   603 puzzles

    right where it can act   596/603 = 98.8%
    completes                52.6%

**Tabulation's ceiling, not the fold's error.** The bottleneck moved twice: 063
route-finding → 065 naming → 066 the rules to name with. **Unexplained:** the 3-hop cell
(0.524) is below 4-hop (0.732).

### The 52% ceiling is 31 missing rules — `note 087`

    CONFIG  when    2026-07-30
            source  note 087
            script  unrecorded
            task    CLUTRR
            model   fold over pairwise rules, coverage supplied
            knobs   missing rules supplied against not
            scale   unrecorded

Supply every missing rule and puzzles complete **1.0000**. The gap was **31 rules**, all
spouse or in-law, never stated anywhere in the corpus. So the fold is perfect given
coverage, and the open question became where the missing rules come from — which is
[naming-the-missing-rule.md](naming-the-missing-rule.md) and
[generation-delta.md](generation-delta.md).
