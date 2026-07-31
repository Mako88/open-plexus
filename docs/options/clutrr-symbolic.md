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
