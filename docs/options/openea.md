# Option record — OpenEA `EN_DE_15K_V2`

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `tools/fetch_openea.py`, which verifies size and sha256. GPL data, evaluation use, fetched
  with John's approval.
- `tools/invariant_dimension.py`, which uses the same graphs for note 104.

---

## What was tried, and what came back

### Chosen on measurement rather than convenience

    CONFIG  when    2026-07-30
            source  note 077, note 076, tools/fetch_openea.py
            script  tools/fetch_openea.py
            task    n/a -- instrument selection
            model   n/a
            knobs   none
            scale   two DBpedia graphs, 15,000 gold links

    relation vocabulary shared between the two graphs
    EN_DE_15K_V2    74.0%      both sides are DBpedia, different languages
    EN_FR_15K_V2    60.8%
    D_W_15K_V2       0.0%      DBpedia against Wikidata
    D_Y_15K_V2       0.0%      DBpedia against YAGO

**A shared vocabulary is what puts both graphs' profiles in one feature space**, and
`tools/fetch_openea.py` **refuses `D_W` and `D_Y` by name** rather than returning a number
about nothing.

The second measurement is degree. Every OpenEA entity has at least four edges; CLUTRR gives
each entity one or two — degree 1 at **28.3%** and degree 2 at **64.4%** (`note 076`), which
is 5.9% at four or more by the table in `tools/fetch_openea.py`. Two surfaces of one concept
holding two disjoint features have **cosine 0 by arithmetic**, which is why the acquisition
question was unaskable on CLUTRR.

URIs are **encoded**, so string matching cannot cheat.

### Zero supervision, and it works at 583x chance — `note 077`

    CONFIG  when    2026-07-30
            source  note 077
            script  unrecorded
            task    OpenEA EN_DE_15K_V2 entity alignment
            model   bag of (relation, direction), no supervision
            knobs   evidence per entity
            scale   15,000 gold links

    hits@1        0.0389   at 583x chance
    one edge      0.0024
    sixteen edges 0.1502

**Monotone in evidence**, which is why CLUTRR could not have seen it: at one or two edges
per entity the signal is at the bottom of that curve.

### Bootstrapping reaches 0.3098, and a confidence gate makes it WORSE — `note 078`

    CONFIG  when    2026-07-30
            source  note 078
            script  unrecorded
            task    as above
            model   mutual nearest neighbours, iterated
            knobs   confidence gate at >=0.9 and >=0.98
            scale   as above

    bootstrapped        0.3098   8x chance, not plateaued
    gate at >= 0.9      0.2334
    gate at >= 0.98     0.0855

**So mutuality is the merge gate and magnitude is not**, and the gate does not buy precision
either. Seed precision self-corrects **0.263 → 0.676** untuned.

### It is not the hard setting, and the hard one is untried — `note 078`

    CONFIG  when    2026-07-30
            source  note 078, note 077
            script  tools/fetch_openea.py
            task    OpenEA D_W and D_Y
            model   as above
            knobs   none
            scale   0.0% shared relation vocabulary

`D_W` and `D_Y` share **0.0%** of their relations, so round 0 has nothing to compare. **A
vocabulary-free seed is untried, and it is the case a real network faces** — two nodes that
have never agreed on anything.

### And the same graphs scope the composition result — `note 104`

    CONFIG  when    2026-07-30
            source  note 104
            script  tools/invariant_dimension.py
            task    DBpedia EN and DE
            model   null-space dimension of the loop-constraint matrix
            knobs   none
            scale   169 and 96 relations; 82,167 and 89,885 loops

Both graphs have **no additive invariant**, full rank, and not approximately so. Record:
[generation-delta.md](generation-delta.md).
