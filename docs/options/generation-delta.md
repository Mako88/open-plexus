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
