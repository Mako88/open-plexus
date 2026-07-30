# Option record — `inherit`, the occupancy gate

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/sketch.py` — `AddressSketch`, the hashed occupancy sketch.
- `LocalMemoryConfig.index_prefer` (`inherit`) and `track_occupancy`.
- `tests/test_sketch.py`, `tests/test_gate_cost.py`, `tests/test_slot_cost.py`.

---

## What was tried, and what came back

### The first arm that is good at all three columns — `148`

    CONFIG  when    2026-07-29
            source  decision 148
            script  unrecorded
            task    families with exceptions
            model   answer from your own address if anything was written there, else
                    from your neighbours'
            knobs   index_prefer inherit
            scale   3 seeds

    DIRECT      0.8100
    TRANSFER    0.4350
    EXCEPTION   0.8183

**And the gate itself is exact**: it fires on 1.0000 of TRANSFER cases and 0.0000 of
DIRECT and EXCEPTION cases, every seed.

**Why it works, and this is the part worth keeping:** membership is *"is there anything
here"*, not *"who has more"*. With a hashed sketch an unwritten address reads **exactly
0.0**, so the threshold is structurally zero and nothing is tuned. That is what decisions
146 and 147 were unable to find, and it is why the answer cost a sketch rather than a
representation.

### It is not a fitted constant — `149`

    CONFIG  when    2026-07-29
            source  decision 149
            script  unrecorded
            task    families
            model   as above
            knobs   n_values 4, 8 and 16; family_size 3, 4 and 6
            scale   every cell of the grid

The ordering holds in **every cell**. This is note 049's P3, asked in July and answered:
no constant moved.

### It costs exactly nothing where it should do nothing — `150`

    CONFIG  when    2026-07-29
            source  decision 150
            script  unrecorded
            task    MQAR
            model   as above
            knobs   inherit against plain, and against summing the same extra reads
            scale   seed for seed

Matches plain **seed for seed at 0.9950** and never defers. Summing the same extra reads
instead costs **0.113**. The same run rules out sketch false negatives: if the sketch were
reporting occupancy wrongly, the gate would have deferred somewhere.

### Where it pays, stated as a property of the task — `153`

    CONFIG  when    2026-07-29
            source  decision 153
            script  unrecorded
            task    families, chains, kinship, MQAR
            model   as above
            knobs   none
            scale   unrecorded

Occupancy is informative exactly where **an address is READ BEFORE IT IS WRITTEN within
the sequence**. Families qualifies. Chains, kinship and MQAR write every address before
querying it, so there is nothing for the gate to know.

### It was never read-gated, and nobody had counted its reads — `161`

    CONFIG  when    2026-07-29
            source  decision 161
            script  unrecorded
            task    families
            model   as above
            knobs   none
            scale   unrecorded

An audit finding rather than a result: the mechanism's cost had not been measured, and the
gate was performing reads nobody had counted.

### Its limit, and it is what `167` ran into — `167`

    CONFIG  when    2026-07-29
            source  decision 167
            script  unrecorded
            task    families, set-valued question
            model   gated collection over index-proposed neighbours
            knobs   none
            scale   unrecorded

**The sketch knows emptiness, not relevance.** It cannot bound an enumeration over
addresses that are all occupied, which is exactly what a set answer needs. The enumeration
bound is a separate question with its own records —
[biggest-similarity-gap.md](biggest-similarity-gap.md) and
[fixed-branches.md](fixed-branches.md).

### And under load the structural zero degrades — `note 081`

    CONFIG  when    2026-07-30
            source  note 081
            script  unrecorded
            task    a stream at 10.6x the store's capacity
            model   single store
            knobs   load varied
            scale   unrecorded

The structurally-zero read is **1.26 at half capacity and 1.03 at 10.6×**. So gate health
tracks live load rather than total writes — the bar is structural in a store that is not
overloaded, and softens in one that is.
