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
