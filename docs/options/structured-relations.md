# Option record — structured representations for RELATIONS

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/keys.py` addresses by `(entity, relation)`, so the seam is present.
- `openplexus/content.py` — `ContentIndex`, similarity kept out of the address.
- `openplexus/sketch.py` — `AddressSketch`, which is what makes a structured relation
  usable in an address without the read returning a neighbour's fact.

---

## What was tried, and what came back

### Random relation vectors do not generalise — `note 067`

    CONFIG  when    2026-07-29
            source  note 067
            script  unrecorded
            task    CLUTRR composition, adversarially held-out relations
            model   `bind` over RANDOM relation vectors
            knobs   hop_accumulate bind
            scale   held-out quarter, chance 0.050

**0.056 against chance 0.050.** Generalising composition is impossible without structure in
the relation representation, and this is the measurement that says so rather than an
argument. Note 067 is also where the entity refusal was split from the relation
requirement: entities must be exactly separated, relations must be **comparable**, and the
store addresses by `(entity, relation)` so the entity supplies the exactness and `O(N·ρ)`
does not bite.

### Structured relations double composition — `note 070`

    CONFIG  when    2026-07-29
            source  note 070
            script  unrecorded
            task    CLUTRR, extensional relation representations
            model   extensional relation vectors, learned
            knobs   structured against random
            scale   a random held-out quarter, paired t = 11.6

Reaches **0.223** held out, **+0.099 paired**. The holdout is the caveat and it is a large
one: a random quarter, where the rules that matter are an adversarially withheld family.
Note 088 is the measurement that separates them and it lands the same mechanism **below**
random filling on the end task — that account is in
[naming-the-missing-rule.md](naming-the-missing-rule.md).

### A structured vector in the ADDRESS needs the gate — `note 071`

    CONFIG  when    2026-07-29
            source  note 071
            script  unrecorded
            task    CLUTRR, reads at structured addresses
            model   structured relation vectors placed in the address
            knobs   AddressSketch at 24 bits, on against off
            scale   unrecorded

Raw reads return **another of that entity's facts** 0.592–0.775 of the time. With
`AddressSketch` at 24 bits, structured keys are **1.0000 recall against a false-hit rate of
0.0004–0.0007**, where hashed keys reach 1.0000 / 0.0000. At the default 16 bits the
structured false-hit rate is 0.0044–0.0100.

The gate is what makes a similarity-bearing address safe; without it the read is a
neighbour's answer wearing the right shape.

**A correction that this record is where it lands.** `DECISIONS.md` carried this as
*"1.0000/0.0005 at 24 bits"*. `0.0005` appears nowhere in note 071 — it is a midpoint of
the measured range, written as though it were a reading. Found by
`tools/check_provenance.py` during the migration.

### What the goal asks for, and why this row stays open

    CONFIG  when    2026-07-30
            source  GOALS.md section 1, note 067
            script  none -- scope statement
            task    none
            model   n/a
            knobs   none
            scale   n/a

GOALS §1 asks for exactly this — *"be aware of the differences and interrelations between
them"* — and note 067 measured that it cannot be had from random vectors. What has not been
built is a relation representation that is learned, comparable, and safe in an address at
the same time. Note 088's refutation is of one route to it (naming the missing rule by a
learned readout), not of the requirement.

### A LOCAL CONTRASTIVE rule reaches 0.2437 on held-out rules — the easy holdout

    CONFIG  when    2026-07-30
            source  tools/relation_contrastive.py
            script  tools/relation_contrastive.py --seeds 10
                    and --random-arm for the gate
            task    CLUTRR gen_train23_test2to10, held-out RULE prediction
            model   relation vectors, width 32, hadamard composition, softmax
                    cross-entropy over all relations, 8 epochs, lr 0.05
            knobs   bind hadamard; random arm on against off
            scale   10 seeds, 62 rules, 16-rule holdout

    untrained (random arm)              0.0312 +/-0.0133
    counted extensional, same binding   0.0690
    contrastive, learned                0.2437 +/-0.0419

**The first mechanism in this project with an ACTUAL OBJECTIVE on relation
representations.** `relation_profiles.py` counts co-occurrence and `content.py` says in its
own docstring *"no objective, no negative sampling and no gradient"*. This has positives,
negatives and a gradient, and every one of them is built from a single 2-hop puzzle on a
single node — no population statistic, no barrier, no second machine.

**The guard was measured rather than asserted.** Letting held-out rules train the
representation scores **0.4188** against **0.2437** with them excluded, so the guard is
worth **0.1750** and without it the number would nearly double and look like a
breakthrough. `the-held-out-rule-trains-the-representation` in `tools/mutate.py` is that
control as a mutation, and it is caught.

**Binding caveat, so neither framing is quoted alone.** The counted vectors score 0.069
under `hadamard` and 0.223 under `both`. Matched-binding is 0.244 against 0.069;
best-against-best is 0.244 against 0.223.

**Weaker than a sweep, on process grounds.** Rule 4 requires a local probe's prediction to
be committed before the run so git ordering is the evidence. That was not done: the numbers
came first and were written down after. An observation, not a tested prediction.

**And this is note 070's RANDOM-QUARTER holdout, which is the one note 088 killed.** The
counted mechanism scored 0.223 here and then fell below random filling on an adversarially
withheld family. Nothing here has faced that holdout, and until it does this row stays
untried as a component rather than becoming a choice.

### And it CLEARS the end-task bar the counted version failed — `g23-01`

    CONFIG  when    2026-07-30
            source  g23-01
            script  tools/generation_delta.py, contrastive fill mode
            task    CLUTRR-symbolic gen_train23_test2to10, END TASK, symbolic
                    fold over true chains, 1,146 test puzzles
            model   relation vectors width 32, hadamard composition, trained on
                    the rule table's rules only
            knobs   fill mode: gap, random, wrong-delta, delta, contrastive
            scale   10 seeds

                   arm      mean      sem
                   gap    0.5960   0.0000
                random    0.6642   0.0018
           wrong-delta    0.6329   0.0018
                 delta    0.9651   0.0000
           contrastive    0.7821   0.0077

    paired contrastive - random   +0.1179 +/-0.0087, wins 10 of 10 seeds

**The first mechanism other than generation delta to beat random filling**, and the
mechanism `note 088` refuted scored 0.5995 BELOW random on this same question. It closes
**39%** of the distance between random filling and the exact symbolic solution.

**All four predictions were registered and committed at `57f81e7` before the fill mode
existed**, which is the discipline the earlier rule-prediction entry above did not follow.

**Scope, and it is the same scope `note 090` carries.** Kinship has an additive invariant;
`note 104` measured that DBpedia EN and DE have none, not even approximately. A
representation that learns kinship's structure is not evidence about a domain without one.
The fold also runs over TRUE chains — `note 091` measured that recovering chains costs
about 0.11 for the delta arm, and nothing here has been run end to end.

**An unexplained discrepancy, recorded rather than smoothed.** P1 was registered against
`note 088`'s random-filling figure of 0.6081. This harness measures the same arm at
**0.6642 ±0.0018** and the two do not overlap. The verdict rests on the within-harness
paired comparison, which is unaffected, but a figure this project cites as a revival
condition disagreeing with a re-measurement is note 105's shape.

### AND IT DOES NOT NEED AN INVARIANT — three domains, three structures

    CONFIG  when    2026-07-30
            source  tools/relation_contrastive.py
            script  tools/relation_contrastive.py --graph <path> --seeds 5
                    and --random-arm; dimensions from
                    tools/invariant_dimension.py --graph <same path>
            task    held-out RULE prediction on knowledge-graph triangles,
                    scored by nearest relation to compose(r1, r2)
            model   relation vectors width 32, hadamard, 8 epochs, lr 0.05
            knobs   graph; random arm on against off
            scale   5 seeds, 75/25 rule split

    graph                        dim   contrastive   majority   untrained
    EN_DE_15K_V2/rel_triples_1     0   0.3602        0.0942     0.0350
    D_W_15K_V1/rel_triples_1       2   0.3559        0.0559     0.0492

**`dim 0` means `generation_delta.py` gets NOTHING in that domain** — the displacement
mechanism is not weak there, it is structurally impossible, and `note 104` is the
measurement that scoped the whole composition line to domains that have a conserved
quantity. **`dim 2` is the case `generation_delta.py` explicitly REFUSES** rather than
handles.

The contrastive representation scores the same in both, and on kinship (`dim 1`) it
cleared the end-task bar. **So it is indifferent to whether a conserved quantity exists**,
which is the constraint the handoff carries as open problem #1.

**Determinism is 0.778 on these graphs against kinship's near-1.0** — one `(r1, r2)`
genuinely reaches several `r3`, so the ceiling is well below 1.0 and where it sits is
unmeasured.

**A wrong claim caught one step before it was published.** The first run used `D_W_15K_V1`
and was about to be reported as *"works on a graph with no invariant"*. Running
`invariant_dimension.py` on that exact file returned **2, not 0** — note 104's dim-0 result
is about `EN_DE`, a different dataset. The graph with no invariant had to be measured, not
assumed, and that is why both rows above name their dimension.

**Baselines are recomputed per graph, never carried.** They differ — 0.0942 and 0.0559 —
and two baselines were got wrong earlier the same day by reusing one measured elsewhere.

**NOT PRE-REGISTERED, and that is the weakness.** Rule 4 wants the prediction committed
before the run; `g23-01` did that and this did not. These are observations. A pre-registered
confirmation is what would make them carry the weight `g23-01`'s numbers carry.

**Held-out RULE prediction, not an end task.** There is no end task defined for these graphs
in this repository. On kinship the same mechanism cleared the end-task bar, which is the
only reason to treat this proxy as informative at all.
