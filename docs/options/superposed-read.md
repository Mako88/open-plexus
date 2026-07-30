# Option record — `SuperposedRead`, one `d × d` matrix of summed outer products

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/retrieval.py` — `SuperposedRead`, beside `ExactCache` and `SettlingRead`.
- The store is a single `d × d` matrix, fixed at construction, written by summed outer
  products and read as `r = M @ key`.
- `tests/test_retrieval_conformance.py` and `tests/test_retrieval_seam.py` hold every read
  path to one contract, which is what makes them swappable.

---

## What was tried, and what came back

### It beats a bounded exact cache once bindings exceed slots — `119`

    CONFIG  when    2026-07-28
            source  decision 119
            script  unrecorded
            task    unrecorded
            model   superposed store against a bounded exact cache
            knobs   cache_slots, bindings varied past the slot count
            scale   unrecorded

**8× better** past the point where bindings exceed slots. This is the entry that answers
note 030's question and is the reason the superposed read earns its place rather than being
the thing an exact structure replaces.

### Capacity scales as `d²` — `109`

    CONFIG  when    2026-07-28
            source  decision 109
            script  unrecorded
            task    direct outer products -- NOT the model's write path
            model   no decay, no cap
            knobs   width 32, 64, 128
            scale   measured at 90% recovery

    width    bindings at 90% recovery
       32                          16
       64                          96
      128                         384

**The configuration is the caveat and it is a large one.** These are direct outer products
with no decay and no cap; the model's own write path reduces it. A number from this entry
is an upper bound on the store, not a prediction about a run.

### It is not the saturation bottleneck — `109`, `110`, `115`

    CONFIG  when    2026-07-28
            source  decisions 109, 110 and 115
            script  unrecorded
            task    corpus, character level
            model   widths 64 to 128
            knobs   width
            scale   unrecorded

At widths 64–128 store and readout **both exceed task demand**, so the 16k-character wall is
not a store limit. The full account is in [saturation-closed.md](saturation-closed.md);
this entry exists so that a reader of the store's record does not have to find it elsewhere.

### The crossover that is still live — `110`

    CONFIG  when    2026-07-28
            source  decision 110
            script  unrecorded
            task    unrecorded
            model   linear readout against the superposed store
            knobs   width
            scale   unrecorded

The linear readout holds **2.00 items per dimension** at every width, where the store scales
as `d²`. They cross near **width ~100**, above which the readout binds rather than the
store. That is what points at `hidden`, whose record is
[hidden-readout.md](hidden-readout.md).
