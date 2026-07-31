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

### On the literature's own task it loses to counting — `g30-01`

    CONFIG  when    2026-07-30
            source  experiments/sweeps/g30-01-link-prediction-on-their-task.txt
            script  experiments/g30_01_link_prediction.py
            task    FB15k-237 tail-side link prediction, filtered, 20,438 test triples
            model   raw summed outer products, random unit value vectors, key = ent*rel
            knobs   width 256 and 512, seed 0
            scale   272,115 train triples against 1,507 bindings of capacity at 256

    arm                width 256   width 512
    store MRR             0.0122      0.0232
    frequency MRR         0.3378      0.3378
    chance MRR          0.000069    0.000069

**177× chance and 1/28th of a counting baseline.** `frequency` ranks entities by how often
they are a tail of that relation — no learning, no capacity — and beats the store by a
factor of 28 at both widths.

**The width result is the informative one.** Doubling the width moved MRR by **1.90×**,
against `sqrt(2)` = 1.41× from the SNR law and 4× from capacity. So quality rises roughly
linearly in width, faster than superposition predicts and far slower than capacity — at
181× over capacity, neither model describes the read.

This bounds ONE reading of the store on ONE outside task; it is offline, global and
non-local, so it says nothing about the project's constraints. It also does not touch the
contrastive relation vectors, which are a different mechanism measured on a different
question ([relational-objective.md](relational-objective.md)).

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
