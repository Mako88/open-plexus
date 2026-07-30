# Option record — declining to answer

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- One surface for it: `openplexus/render.py` **declines** on an empty answer set rather than
  rendering a hole.
- Nothing that decides to decline, and no task that scores abstention.

---

## What was tried, and what came back

### Nothing lets the model say "I do not know"

    CONFIG  when    2026-07-29
            source  docs/archive/architecture-ledger-2026-07-29.md, row C4
            script  none -- nothing built
            task    no task scores abstention
            model   the gate is a fact about the store, not a learned probability
            knobs   none
            scale   n/a

The archived capability ledger's row C4. **No mechanism decides to abstain and no
instrument would notice if one did**, so any claim about honesty here is untested.

The nearest thing that exists is the occupancy gate, and it is a different kind of object:
it reports whether anything was ever written at an address, which is a fact about the store
rather than a confidence in an answer. That is what makes it trustworthy where a learned
probability would not be, and also what makes it unable to say "I do not know" about a
question whose addresses are all occupied.

### The renderer's half of it — `openplexus/render.py`

    CONFIG  when    2026-07-29
            source  openplexus/render.py
            script  tests/test_render.py
            task    none -- a rendering property, asserted by test
            model   template realiser
            knobs   none
            scale   unit tests

An empty set **declines** rather than rendering a hole. It is the surface row C4 would use
if anything ever earned it, written where it is trivially true so the rungs above have
something to fail against. Record: [template-realiser.md](template-realiser.md).

### The gate CAN decline, exactly, on the case it can see — `g26-01`

    CONFIG  when    2026-07-30
            source  g26-01
            script  experiments/g26_01_abstention.py
            task    kinship, 35 answerable and 35 unanswerable pairs per seed
            model   width 256, context+derived keys, track_occupancy
            knobs   AddressSketch at its default 16 bits
            scale   3 seeds, 105 unanswerable questions

    false abstention     0.0185 as measured, 0.000 corrected
    correct abstention   1.0000

**The first abstention measurement in this project.** An unanswerable question is a known
entity and a known relation whose PAIR was never written — so it fails at the address, not
at the vocabulary, which is what stops this measuring something easier.

**The 0.0185 was my artefact.** Exactly one position per sequence reads empty when it
should not: the LAST. A binding is written when the next step processes it, and the final
position has no next step. Corrected, false abstention is **0.000**.

**P4 was refuted and its refutation is weak, which is worth more than the result.** I
predicted abstention would not be exact, because `note 071` measured the sketch's false-hit
rate at 0.0044–0.0100 on 16 bits. None appeared — but at that rate, 105 questions expects
**about 0.5 to 1** false hits, so **this sample cannot distinguish "exact" from "0.0044"**.
Settling it needs thousands of questions, which is cheap because no training is involved.

**And note 071's trade is not reachable from config.** `local_memory.py` constructs
`AddressSketch(d, seed=...)` and takes the default 16; `LocalMemoryConfig` has no sketch
width. So more bits for fewer false hits cannot be chosen without editing the model.

**Still not a mechanism.** Nothing in `run()` consults occupancy to decline — this asked
the sketch from outside. Wiring it is a small, separate change. `P3`, whether the gate
costs anything on answerable questions, was NOT measured and is not claimed.
