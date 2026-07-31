# Option record — naming the missing rule by a learned readout

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- The extensional relation representations of `note 070`, and the associativity filter of
  `note 085`. Both are built; neither supplies a rule.

---

## What was tried, and what came back

### The bar, set before the attempts — `note 088`

    CONFIG  when    2026-07-30
            source  note 088
            script  unrecorded
            task    CLUTRR, end task
            model   gaps filled at random
            knobs   random filling against majority
            scale   10 seeds

    random fill    mean 0.6081   sd 0.0055   min 0.5995   max 0.6178
    majority       0.5620

**Any composition mechanism must beat 0.6081 ± 0.0055.** And `majority` being *worse* than
random says systematic error costs more than noise — filling gaps beats filling them
correctly, which is the note's title and the reason the bar is what it is.

### The learned readout does not clear it — `note 088`

    CONFIG  when    2026-07-30
            source  note 088, and note 070 for the paired holdout figures
            script  unrecorded
            task    CLUTRR, end task, adversarially withheld family
            model   extensional relation representations from note 070
            knobs   learned naming against random filling
            scale   as above

**0.5995 end task, below random's 0.6081.** Note 070's holdout was a random quarter; the
rules that matter are an adversarially withheld family, and **this is the measurement that
separates them**. The same mechanism reaches 0.223 held out on 070's split, +0.099 paired
at t = 11.6 — a real effect on the wrong question.

**Revival: only if a mechanism beats 0.6081 end-task**, which is the bar note 090 clears.

### Self-training does not lift it — `note 084`

    CONFIG  when    2026-07-30
            source  note 084
            script  unrecorded
            task    CLUTRR
            model   pseudo-labelled self-training over the same feature space
            knobs   rounds
            scale   frozen from round 1

**Bootstrapping needs new FEATURES, not new labels.** Note 078's rounds added graph
columns; this adds only pseudo-labels over a space that does not change, and it is frozen
from the first round.

### Associativity verifies what it cannot generate — `note 085`

    CONFIG  when    2026-07-30
            source  note 085
            script  unrecorded
            task    CLUTRR rule table
            model   associativity as a constraint over known rules
            knobs   used as a determiner, then as a filter
            scale   15% rule density, so chance is 0.059

    holds on the known table          0.933
    determines held-out rules         0.059  -- chance
    as a FILTER, separates            0.5645 from 0.0162
    rejections that are genuinely wrong  98.4%

So it is an excellent verifier and supplies nothing. Propagating it iteratively fills
**zero cells in zero rounds** (`note 090`), which settles deduction as unable to supply the
rules.

### Random relation vectors are at chance — `note 067`

    CONFIG  when    2026-07-29
            source  note 067
            script  unrecorded
            task    CLUTRR, held-out relations
            model   `bind` over RANDOM relation vectors
            knobs   hop_accumulate bind
            scale   chance 0.050

**0.056 against chance 0.050.** Kept under the alternatives rule as the measured
comparison the structured version is a difference from.

### The structured address needs the gate — `note 071`

    CONFIG  when    2026-07-29
            source  note 071
            script  unrecorded
            task    CLUTRR
            model   structured relation vectors in the address
            knobs   AddressSketch at 24 bits and at the default 16
            scale   unrecorded

Raw reads return another of that entity's facts **0.592–0.775** of the time. At 24 bits the
sketch takes structured keys to 1.0000 recall against 0.0004–0.0007 false hits. Record:
[structured-relations.md](structured-relations.md).

### End to end at a swept width, and it is the only filler a domain WITHOUT an invariant gets — `g41-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g41-01-the-pipeline-on-the-published-protocol.txt
            script  experiments/g41_01_the_pipeline_on_the_published_protocol.py
            task    CLUTRR gen_train23_test2to10, TEST split, 1,146 puzzles
            model   LocalAssociativeMemory + search.beam + the contrastive fold
            knobs   d_model {32, 64, 128, 256, 512} x beam width {4, 8}
            scale   8 seeds, per hop bucket, achievable floor as an arm

At width 256, beam 8, 10 hops, subset `all`: **0.6061**, against an achievable
floor of **0.0588**, a random fill of **0.4632** and the hand-supplied invariant's
**0.9076**.

**This is the row that matters for scope.** `note 104` and `g23-03` measure
whether a graph has an additive invariant at all; a graph without one cannot have
the `delta` fold, and this is what it gets instead. Record:
[generation-delta.md](generation-delta.md).

**It does NOT transfer as a target to another graph, and saying so is the point.**
0.6061 is CLUTRR's number, where 97 rules are learned over a 20-relation domain.
A graph with 237 relations has a far sparser table, and the filler is only as good
as what the table already holds — so the figure to beat on a new instrument has to
be measured there, not carried from here. That is the mistake this record's own
`note 088` entry was caught by once already: a real effect on the wrong question.
