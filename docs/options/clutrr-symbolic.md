# Option record — CLUTRR-symbolic

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/tasks/clutrr.py`, the loader; `tools/clutrr_recovery.py`, the reproducible
  chain-recovery harness; `tests/test_clutrr.py`.
- Split `gen_train23_test2to10`, layout **`kinship`**.

---

## What was tried, and what came back

### The graph layer, never the prose

    CONFIG  when    2026-07-29
            source  openplexus/tasks/clutrr.py
            script  openplexus/tasks/clutrr.py
            task    CLUTRR gen_train23_test2to10
            model   n/a -- the instrument
            knobs   layout kinship
            scale   1,146 puzzles in the test split

Results are *"CLUTRR-symbolic"* and **published text numbers are not comparable**. The
layout was chosen on measurement — collisions 35.9% → 7.7%, which is `157`'s mechanism
applied to someone else's data.

### Report per hop bucket and split on ENTITY REPETITION — `note 059`

    CONFIG  when    2026-07-29
            source  note 059
            script  tools/clutrr_recovery.py
            task    CLUTRR, train and test splits compared
            model   n/a -- a property of the data
            knobs   none
            scale   train and test entity-repetition rates

**Test is 37.8% repeated where train is 0%.** So a falling accuracy curve reads as depth and
is really decision 103's addressing problem. Any per-depth number that does not split on
this is confounded.

### The `hops=1` floor is not chance — `note 060`

    CONFIG  when    2026-07-29
            source  note 060
            script  tools/clutrr_recovery.py
            task    CLUTRR, hops=1
            model   n/a
            knobs   none
            scale   unrecorded

**0.0856**, not chance, because sequence length leaks the hop count. A baseline that is not
chance has to be measured rather than assumed, and this is the entry that measured it.

### 065's numbers have no committed script — `note 074`, `note 075`

    CONFIG  when    2026-07-30
            source  notes 074-075
            script  tools/clutrr_recovery.py
            task    CLUTRR chain recovery
            model   width 64
            knobs   width, allowed mask, branches -- all tested
            scale   3 seeds

No committed script reproduces note 065's figures, so its configuration is unrecovered.
`tools/clutrr_recovery.py` prints 065's numbers beside its own and **gates on the mismatch**,
which is why it says *"this is a harness, not a finding"* in its own output.

**Differences are taken against that harness's own baseline** — three-seed means search
0.7810 and beam 0.8877.

### What it CANNOT test: concept acquisition — `note 076`

    CONFIG  when    2026-07-30
            source  note 076
            script  unrecorded
            task    CLUTRR
            model   n/a -- a property of the data
            knobs   none
            scale   entities carry 1-2 edges

Entities carry one or two edges, so **two surfaces of one concept share nothing by
arithmetic**. That gap is what OpenEA was fetched for. Record: [openea.md](openea.md).

### THE GRAPH-ONLY NUMBERS ARE PUBLISHED, AND THE BAND IS WIDE — 2026-07-31

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g37-01-the-band-on-clutrr.txt
            script  experiments/g37_01_the_band_on_clutrr.py
            task    CLUTRR gen_train23_test2to10, test split, 1,146 puzzles
            model   n/a -- a property of the data plus published references
            knobs   layout kinship, per-hop buckets, max_appearances split
            scale   majority-class floor per hop bucket; chance 1/20 = 0.0500

**This entry exists because a true sentence in this record was read as a
different, false one, and it blocked kill-list item #1 for weeks.**

The record above says *"published text numbers are not comparable"*. That is
**correct** — the NLU/text baselines read prose this project does not generate.
What it was taken to mean is *"no published CLUTRR number is comparable"*, and
that is false: the standard evaluation in the literature is the **noiseless
graph-based** version, trained on k=2,3 and tested on k in [2,10] — which is
`gen_train23_test2to10`, **the exact split already fetched here.**

THE FLOOR, computed from the data on disk rather than assumed:

    hops    puzzles   majority   rep<=2   majority | rep<=2
       2         38     0.5000   1.0000              0.5000
       3        105     0.4286   1.0000              0.4286
       4        190     0.1895   0.9316              0.2034
       5        174     0.1609   0.7529              0.2061
       6        107     0.2336   0.5888              0.3810
       7        144     0.1806   0.4236              0.2951
       8        150     0.2667   0.4067              0.3115
       9        119     0.1681   0.3782              0.3111
      10        119     0.1849   0.2689              0.2812
     all      1,146     0.1370                    chance 0.0500

18 of the 20 relations are used as answers.

**The band against the weakest published graph model.** GCN at 10 hops is the
worst reference in either table consulted, at **0.39**, against a floor of
0.1849 — a band of roughly **0.20**. Against the strongest (R5, **0.97**) the
band is about **0.79**. The borrowed table and its provenance live in the sweep
record, in one place, so nothing re-establishes them.

`closure`'s usable band against its honest floor is **0.092** (g14-01). So even
the most pessimistic reading here is **more than twice** as wide, and the
realistic one is **eight times**.

**HOW MUCH OF THIS IS VERIFIED, stated precisely because it gates a decision.**
The floor table is computed here from the fetched data and is a measurement. The
reference numbers are **read from ar5iv HTML renderings of two papers via a
summarising fetch, NOT from the PDFs directly** — `CLAUDE.md` rule 1 calls that
a summarised claim, and it must be marked. Two independent sources were consulted
and agree on ordering and magnitude.

**The conclusion is robust to the uncertainty in them**, which is why it is
allowed to carry a decision: it survives even if every borrowed figure is wrong
by the full distance between the two sources.

THREE CAVEATS THAT ARE NOT DECORATION:

- **The 2-hop test bucket is 38 puzzles with a majority of 0.5000.** No number
  from it is quotable. The 3-hop bucket is 105 at 0.4286 and is nearly as bad.
  The usable range is 4 hops and up.
- **The floor MOVES with `max_appearances`, and in the unhelpful direction.** The
  clean arm (`rep<=2`) has a HIGHER majority everywhere — 0.2812 against 0.1849
  at 10 hops — so the primary arm is easier by base rate, and a column read
  across hops on that arm is several scales printed as one. This is `g35-02`'s
  floor confound waiting to happen on a new instrument.
- **The clean arm shrinks to 0.2689 of the 10-hop bucket**, which is 32 puzzles.
  Depth and entity repetition are confounded in the data itself (note 059) and no
  choice of arm removes both.

### G0 IS NOT ANSWERED: we have no reference that can compose — `g37-02`, `g37-03`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g37-03-can-the-reference-compose.txt
            script  experiments/g37_03_can_the_reference_compose.py
            task    CLUTRR gen_train23_test2to10, train hops 2-3, test 2-10
            model   ShiftedAttention, Adam lr 3e-3
            knobs   width 128/256 x epochs 16/48; 2,000 training puzzles; seed 0
            scale   accuracy on the TRAINING set, base rate 0.1087

`g37-02` ran the four G0 arms and every learning arm landed below the floor past
3 hops. **That is not a verdict on CLUTRR**, and the reason was established by a
probe registered *before* the failing run, in `g37-02`'s own P2.

**Train accuracy: 0.4185, 0.4205, 0.4185, 0.4215.** Twice the width and three
times the epochs move it by **0.0030**. A model that cannot fit 2,000 examples
seen forty-eight times is not undertrained — `ShiftedAttention` is single-layer
and single-pass, and composing a relation chain needs a second pass over an
intermediate result.

**So the blocker on kill-list #1 has moved again, and this time it is something
to BUILD.** The instrument exists (`g37-01`), the band is real, and nothing in
this repository can currently measure it.

**And the floor reported in `g37-01` was an ORACLE floor.** The commonest
training answer is `brother`; it appears **0 of 38** times in the 2-hop test
bucket and **0 of 105** at 3 hops, because those buckets are `grandson` and
`father` and the deep ones are `niece` — one of the six relations that never
appear as a stated edge. A constant fitted on train scores **0.0000** in the
shallow buckets and never exceeds its own **0.1087** base rate, against an oracle
floor running to **0.5000**. That record is corrected; the band is WIDER as a
result, and the oracle version is the *stricter* bar — a result read against it
would be judged against something no model can reach.

**The `local` arm's settings were not swept and its numbers are provisional in
the same way the reference's were.** Sweeping one arm and not the other is the
failure `CLAUDE.md` names explicitly, and correcting one while leaving the other
would repeat it.

### WHAT THE NEXT SESSION NEEDS: a reference that can compose — 2026-07-31

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g37-03-can-the-reference-compose.txt
            script  experiments/g37_03_can_the_reference_compose.py
            task    CLUTRR gen_train23_test2to10, train hops 2-3
            model   ShiftedAttention, Adam lr 3e-3
            knobs   width 128/256 x epochs 16/48; 2,000 training puzzles
            scale   accuracy on the TRAINING set, base rate 0.1087

**Written as a handoff entry, because John is taking kill-list #1 into a new
session.** Everything needed is measured and in place; what is missing is one
model.

**THE STATE, in three facts.** The instrument exists and its data is fetched
(`data/clutrr/gen_train23_test2to10`). The band is real — the achievable floor is
a constant fitted on train, which scores **0.0000** in the shallow test buckets
and never exceeds its own **0.1087** base rate, against published graph-only
references whose table and provenance live in `g37-01`'s record, one
place. And **nothing in this repository can measure it**:
`ShiftedAttention` fits its own training data to **0.4185** at d128x16 and
**0.4215** at d256x48, so twice the width and three times the epochs buy 0.003.

**WHY IT FAILS, and it is not a tuning problem.** The model is single-layer and
single-pass. CLUTRR asks you to work out `A is B's X`, then USE that answer to
work out the next link. One pass cannot do two steps. Every published system that
does well here — R5, CTP, GAT — is multi-hop.

**THE FOUR THINGS THAT WOULD SAVE TIME**, each of them a trap this project has
already stepped in:

- **Sweep the LOCAL arm too.** Its d256 and 4 epochs are carried from `g14-01`
  exactly as the reference's were, and `g37-03` swept only the reference.
  `CLAUDE.md` names sweeping one arm and not the other by name.
- **Use `majority`, not the bucket majority, as the floor.** The commonest
  TRAINING answer is `brother` and it appears **0 of 38** times in the 2-hop
  test bucket. The per-bucket majority is an ORACLE floor no model can reach.
- **Report per hop bucket and never pool.** Hops 2-3 are the only depths in
  train, so they are recall and 4-10 are generalisation. The 2-hop test bucket
  is 38 puzzles.
- **Split on `max_appearances`, and remember its floor MOVES** — it is markedly
  HIGHER on the clean arm at every depth past 4, because removing repeated
  entities removes the harder
  puzzles.

**The reference numbers are BORROWED and only partly verified** — read from ar5iv
renderings via a summarising fetch, not from the PDFs. The table and its
provenance are in `g37-01`'s record, in one place.
