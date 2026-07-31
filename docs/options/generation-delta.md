# Option record — GENERATION DELTA, learned from cycles

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `tools/generation_delta.py`, which reproduces both the symbolic and the end-to-end
  numbers.
- `tools/invariant_dimension.py`, the instrument that scopes it.

---

## What was tried, and what came back

### The deltas are recovered exactly, from loop constraints alone — `note 090`

    CONFIG  when    2026-07-30
            source  note 090
            script  tools/generation_delta.py
            task    CLUTRR kinship, symbolic fold over TRUE chains
            model   one homogeneous equation per puzzle, deltas as the null space
            knobs   none
            scale   9,074 puzzles, 20 unknowns

A chain plus its query is a loop, so the chain's deltas must sum to the answer's. Null
space **1** — the gauge — and **20/20 deltas recovered exactly**. Fill a gap with any
relation of the right delta and the chain stays arithmetically correct.

### End task 0.5201 to 0.9668, with a control that fires — `note 090`

    CONFIG  when    2026-07-30
            source  note 090, and notes 087-088 for the random-filling bar
            script  tools/generation_delta.py
            task    CLUTRR, end task, symbolic
            model   fold over pairwise rules with gaps filled by delta
            knobs   correct delta, deliberately WRONG delta, random filling
            scale   720 fills against random's 1,152

    delta filling        0.9668
    oracle (true rules)  1.0000
    random filling       0.6081
    WRONG delta          0.5681   -- below random
    no filling           0.5201

**The wrong-delta control is what makes this a mechanism rather than an artefact of filling
anything at all**: a deliberately wrong displacement scores *below* random. Fills also FALL
— 720 against random's 1,152 — because a delta-preserving fill lands where the table
already knows.

### End to end, with the model recovering its own chains — `note 091`

    CONFIG  when    2026-07-30
            source  note 091
            script  tools/generation_delta.py
            task    CLUTRR, end task, model in the loop
            model   the model's own chain recovery feeding the fold
            knobs   none
            scale   unrecorded

    end task         0.8578
    chain recovery   0.8770

Roughly the product, slightly better because a mis-recovered chain can still compose right.

### 0.8578 WAS TAKEN AT AN UNTUNED WIDTH AND THE BEST OF EIGHT SEEDS — `g41-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g41-01-the-pipeline-on-the-published-protocol.txt
            script  experiments/g41_01_the_pipeline_on_the_published_protocol.py
            task    CLUTRR gen_train23_test2to10, TEST split, 1,146 puzzles
            model   LocalAssociativeMemory + search.beam + the delta fold
            knobs   d_model {32, 64, 128, 256} x beam width {4, 8}
            scale   8 seeds, per hop bucket, both max_appearances subsets

The pooled figure is reported per hop bucket against the ACHIEVABLE floor, and
the carried constants are swept. **`d_model` was never varied before this** — it
arrives from `note 065`'s configuration — and it dominates every mechanism in the
comparison. At 10 hops, subset `all`, beam 8, the arm runs **0.1943 at width 32
against 0.9076 at width 256**, a spread of 0.71 where the sweep predicted under
0.05. Full table in the record; it is quoted nowhere else.

**Seed 0 is the best of eight at the carried width** — 0.7815 against a mean of
0.7185 and a worst of 0.6050 — and seed 0 is where `note 090`/`091` were taken.
At width 256 the same bucket reads **0.9076 mean against 0.8739 worst**, so the
variance was a symptom of running near a capacity cliff rather than a property of
the mechanism.

**Reporting it honestly made it BETTER, not worse.** The deepest bucket beats the
pooled 0.8578 once the width may move.

**The grid pinned at its top edge until 512, which closed it.** 512 raises the
10-hop mean to **0.9233** while its worst seed FALLS to **0.8655**, and it loses
to 256 at 5 hops (0.9468 against 0.9504) and at 6 (0.9322 against 0.9439) — so
the optimum is interior and these are measurements rather than lower bounds. The
reach at 10 hops is about 0.91-0.92 and **256 is the operating point**; 512 costs
four times as much for a difference the seeds cannot resolve.

**`branches` was NOT swept** and is carried from `note 065` like the other two.
`tools/check_constants.py` flagged it in `g41-01`'s own source, after the fact.

**AND THE RUN PRICES THE INVARIANT ITSELF**, which is what it bears on hardest.
At width 256, beam 8, 10 hops, subset `all`, the arms decompose:

    achievable floor (commonest TRAIN answer)          0.0588
    walk + learned rule table, gaps UNFILLED           0.3613
    + a random relation in the gap                     0.4632
    + a LEARNED relation vector (`g23-01`/`g23-02`)    0.6061
    + the hand-supplied additive invariant             0.9076

**The largest single term is the one supplied by hand.** `note 090` states in its
own text that *"deltas add"* is a design choice rather than read from data; this
is what that choice is worth on the deepest bucket, against nothing and against
the best learned alternative.

**This is the scope question with a number on it.** A domain with no conserved
quantity does not get the `delta` row — it gets `contrastive`, which is 0.6061
here against a floor of 0.0588. Real, and not the headline. `note 104` and
`g23-03` are where the presence of an invariant is measured per graph.

**Both aids remain**: the walk is handed `len(chain)`, and *"deltas add"* is still
supplied. Nothing here bears on the first. Record:
[beam-search.md](beam-search.md) for the search half.

### The hand-coded features were mostly noise, and the one that mattered was the least learnable — `note 089`

    CONFIG  when    2026-07-30
            source  note 089, and note 090 which is where the ablation costs are written
            script  unrecorded
            task    CLUTRR, feature ablation
            model   hand-coded relational features
            knobs   marry, gender, affinity, generation
            scale   oracle 0.7382

The *"married a spouse"* clause cost **0.125**; gender and affinity together cost a further
**0.058**. **The one measured as LEAST learnable —
generation, 0.350 from profiles — is the only one that mattered.** Profiles are ADJACENCY
and generation is GLOBAL, so it needed a different *kind* of signal rather than a better
regressor. That is the observation note 090 acts on.

### And 0.9668 is not 1.0000 — `note 090`

    CONFIG  when    2026-07-30
            source  note 090
            script  tools/generation_delta.py
            task    CLUTRR, end task
            model   as above
            knobs   none
            scale   28 of 720 fills

28 of 720 fills land on the **final** step, where an arbitrary relation with the right
delta is exposed: the answer needs the exact relation, not merely the right displacement.
Naming those is what the last 3.3% is, and it is where the discarded features may earn a
narrower place.

### SCOPED — the limit of the result, measured — `note 104`

    CONFIG  when    2026-07-30
            source  note 104
            script  tools/invariant_dimension.py
            task    CLUTRR kinship as CONTROL, DBpedia EN and DE
            model   null-space dimension of the constraint matrix, no model, no training
            knobs   none
            scale   9,074 / 82,167 / 89,885 loops over 20 / 169 / 96 relations

    domain                    rels    loops   rank   dim   no-loop
    CLUTRR kinship (CONTROL)    20    9,074     19     1         0
    DBpedia English            169   82,167    167     0         2
    DBpedia German              96   89,885     96     0         0

**Both general knowledge graphs have no additive invariant**, and not an approximate one
either: CLUTRR's null direction sits at 1.29e-15 with a **fourteen-order gap**, where
DBpedia's smallest singular values cluster at about 3e-3 with no gap at all. An invariant
holding with exceptions would show as a small tail. There is none.

So the result is *"solved wherever a conserved quantity exists"*, and kinship's is additive
while nothing else has been tried. The replacement question — does some *subset* of a
graph's relations close consistently — is a largest-consistent-subset search rather than a
null space over all relations, and it is unbuilt.

### The rule "deltas add" is a design choice — `note 090`

    CONFIG  when    2026-07-30
            source  note 090
            script  none -- a scope statement
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

Not read from data. It is arithmetic rather than domain knowledge, and the ablation shows
the actual domain knowledge supplied was harmful — but **a system that discovered the
additivity itself would be a stronger claim than this makes**, and the note says so in its
own text.

### `dim` is a property of the EXTRACT, not the domain — `g23-03`

    CONFIG  when    2026-07-30
            source  g23-03
            script  tools/invariant_dimension.py --graph <path>, over all 16
                    OpenEA rel_triples files
            task    none -- linear algebra over cycle constraints
            model   n/a
            knobs   graph
            scale   16 graphs, 8 V1/V2 pairs

    pair            side 1          side 2
    D_W       V1 = 2, V2 = 0    V1 = 0, V2 = 0
    D_Y       V1 = 0, V2 = 0    V1 = 0, V2 = 0
    EN_DE     V1 = 1, V2 = 0    V1 = 0, V2 = 0
    EN_FR     V1 = 1, V2 = 0    V1 = 0, V2 = 0

**Three of eight pairs disagree**, all on the DBpedia side, all `dim >= 1` in V1 and
`dim 0` in V2.

`note 104` measured `EN_DE` **V2** and concluded *"DBpedia EN and DE have no additive
invariant, and not approximately"* — which scoped this whole option to *"solved wherever a
conserved quantity exists"* and put *"invariants per sub-domain"* at the top of the
handoff. **The V1 extract of that same source graph has dimension 1.**

V1 and V2 are different samples of one source at different densities, and the dimension is
a property of the cycle structure that density changes. So the honest statement is that a
particular 15,000-entity sample has no invariant, not that DBpedia has none — and the
with/without framing this option's scope rests on is partly an artefact of sampling.

**Predicted before the run and expected to fail** (`g23-03` P4), with this consequence
written out in advance rather than after.

### FB15k-237 HAS NO INVARIANT, and not approximately one — 2026-07-31

    CONFIG  when    2026-07-31
            source  docs/options/generation-delta.md -- this entry holds the run
            script  tools/invariant_dimension.py --graph data/fb15k237/train.txt
            task    none -- linear algebra over cycle constraints
            model   n/a. No model, no training, no walk
            knobs   none
            scale   272,115 edges, 267,089 loops, 237 relations

    domain                    rels    loops   rank   dim
    CLUTRR kinship (CONTROL)    20     9,074     19     1
    FB15k-237 train            237   267,089    234     0

Three relations appear in no loop and are excluded; an all-zero column would join
the null space for free.

**And the zero is HARD, which is the claim `note 104` established the standard
for.** The singular spectra:

    domain            largest   smallest   smallest/largest   gap at the bottom
    CLUTRR             70.6216    9.09e-14           1.29e-15          1.02e+14x
    FB15k-237         446.69        9.936             0.02224             1.006x

CLUTRR's null direction sits fourteen orders below its largest and the two
smallest values are separated by a factor of 10^14. **FB15k-237's bottom six
cluster between 9.94 and 10.39 with no separation at all** — 1.006x between the
two smallest. An invariant holding with exceptions would show as a tail. There is
none.

**So the displacement mechanism gets NOTHING on FB15k-237**, and that mechanism is
worth the difference between 0.6061 and 0.9076 on CLUTRR's deepest bucket.

**The CLUTRR control reproduces `note 104`'s 1.29e-15 exactly**, which is what
makes the FB15k-237 column readable rather than a number from an unvalidated
probe.

**Scope, carried from `g23-03` because it still applies:** `dim` is a property of
the EXTRACT. FB15k-237 is itself a curated selection of Freebase with inverse
relations removed, so this is a statement about that extract. What it is NOT is a
statement that no subset of those 237 relations closes — a largest-consistent-
subset search is a different computation and `g23-03` named it as unbuilt.

**No prediction was registered before this run and none is claimed.** It is a
deterministic property of a fixed file with no arm, no tuning and nothing to
choose after the fact, so there was no result to steer; the rail exists for runs
where there is.

### NO SUB-DOMAIN OF FB15k-237 CLOSES EITHER — `g43-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g43-01-does-any-sub-domain-close.txt
            script  experiments/g43_01_does_any_sub_domain_close.py
            task    none -- linear algebra over cycle constraints
            model   n/a. No model, no training, no walk
            knobs   30 name-derived sub-domains; a ladder of best-evidenced
                    subset sizes; a shuffled control on both
            scale   272,115 edges, 267,089 loops, 237 relations, 51.1s

`g23-03` named this computation and left it unbuilt. **Nothing closes anywhere.**
All 30 by-domain subsets, all eight rungs of the size ladder, and the shuffled
control: **dim 0** in every cell.

    |S| 234  loops 267,089  ratio 1141.4  dim 0
    |S|  16  loops  79,841  ratio 4990.1  dim 0
    |S|   2  loops   2,574  ratio 1287.0  dim 0

**Two relations with 2,574 loops between them do not compose additively.** The
best-evidenced domains fail too — `award` at 947.6 loops per relation, `people` at
814.1.

**So the additive invariant is a property of kinship.** It is worth the difference
between 0.6061 and 0.9076 on CLUTRR's deepest bucket and it gets nothing on
FB15k-237, at any granularity tried.

**A GATE, not decoration:** the run refuses to report unless the same restriction
recovers CLUTRR's own dim 1 first. A search that cannot find a known invariant
would produce this exact null.

**What it does NOT settle, stated because a complete-looking null is the dangerous
kind.** The partition is by relation NAME — honest, because the data supplies it,
but one partition among very many, and a search that is not name-derived is still
unbuilt. And every cell here asks whether displacements SUM to zero; a
multiplicative, modular or vector-valued conserved quantity would read as dim 0
throughout. `note 090` already records that *"deltas add"* is a design choice, and
this establishes that the choice does not transfer — not that no choice would.

**The first version of this run reported four closing domains and it was an
artefact**, corroborated rather than caught by its own shuffled control. The
account is in the sweep record and the method lesson is a `CLAUDE.md` calibration.
