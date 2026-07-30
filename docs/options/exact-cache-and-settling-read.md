# Option record — `ExactCache` and `SettlingRead`

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/retrieval.py` — `ExactCache` and `SettlingRead`, beside `SuperposedRead`.
- `LocalMemoryConfig.cache_slots`, `cache_only`, `cache_sharpness`, `cache_weight`, and
  `retrieval_steps` for the settling read.
- `tests/test_exact_cache.py`, `tests/test_retrieval_steps.py`, and two mutations —
  `the-cache-admits-by-RECENCY-not-residual` and `the-cache-read-is-not-gated-by-the-MATCH`.

---

## What was tried, and what came back

### The cache was the first controlled improvement on the corpus — `69`, `g11-06`

    CONFIG  when    2026-07-27
            source  decision 69
            script  unrecorded
            task    corpus, character level
            model   superposed store plus a bounded exact cache
            knobs   cache_slots 128
            scale   unrecorded

    mechanism            effect on LEVEL      effect on SLOPE
    width, 4x                    +0.089                 none
    exact cache, 128 slots       +0.19 (g11-06)         none
    sparse keys, k=4             +0.15                  none
    pair keys                    -0.23                  none
    trained Wv                   -0.45                  none
    carry store (training)       -0.15                  none

**+0.19 bits at 128 slots**, the largest single mechanism in that table, and the project's
first controlled corpus gain. **And it moves the level, not the slope** — six mechanisms,
three of them helpful, and not one changes the fact that the model converges by about
16,000 characters and stops.

**The tree cited this to decision 60**, which is where the cache's tests were found vacuous,
not where the gain was measured. Corrected here during the migration.

### Its two defining claims had nothing asserting them — `60`

    CONFIG  when    2026-07-27
            source  decision 60
            script  tools/mutate.py
            task    none -- a test-coverage audit
            model   the cache as built
            knobs   none
            scale   two mutations, surviving at b480926 and at least one commit before

`the-cache-admits-by-RECENCY-not-residual` and `the-cache-read-is-not-gated-by-the-MATCH`
both survived. Admission by residual and the match gate are what the mechanism *is*, and
neither was under test. They were found only because an unrelated refactor made `--verify`
fail and someone went looking.

This is the calibration behind `--changed` existing at all, and behind the CI mutation
shards being treated as blocking rather than noted.

### It was mostly compensation for a weak readout — `76`

    CONFIG  when    2026-07-28
            source  decision 76
            script  unrecorded
            task    corpus, character level
            model   linear readout against a composed two-layer readout
            knobs   cache_slots, readout
            scale   unrecorded

Re-validated deliberately after the readout changed, and chosen first because decision 61's
argument for item-partitioning the distributed model rested on it. The pattern is decision
74's: a mechanism that reads as a representational improvement turning out to be
compensation for a readout that could not disentangle overlap.

### It loses to the superposed read past its slot count — `119`

    CONFIG  when    2026-07-28
            source  decision 119
            script  unrecorded
            task    unrecorded
            model   bounded exact cache against superposed
            knobs   bindings varied past the slot count
            scale   unrecorded

**8×** in the superposed read's favour once bindings exceed slots. A bounded exact
structure is exact until it is full, and the store is not.

### `SettlingRead` — kept, and thinly measured

    CONFIG  when    2026-07-27
            source  openplexus/retrieval.py, tests/test_retrieval_steps.py
            script  tests/test_retrieval_steps.py
            task    none -- unit tests only
            model   iterated retrieval
            knobs   retrieval_steps
            scale   unit tests, no sweep

Kept under the alternatives rule as a swappable read path with its own conformance tests.
No experiment has compared it against `SuperposedRead` on any instrument.
