# Option record — an external persistent store

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing of the external store itself.
- Two things it would be built on do exist: `derived_keys`, which means
  `keys.pair(entity, relation)` is rebuilt from two token ids, and `ownership.Ring`, which
  is already consistent hashing.

---

## What was tried, and what came back

### Raised, and the addressing layer turns out to be already built

    CONFIG  when    2026-07-30
            source  John in conversation, and openplexus/ownership.py
            script  none -- nothing built
            task    none
            model   derived_keys on; ownership.Ring as the routing layer
            knobs   none
            scale   n/a

Eviction becomes **archival** rather than deletion. The key already exists: with
`derived_keys`, `(entity, relation) → value` is an ordinary key-value pair needing no
translation, and `ownership.Ring` **is** the DHT addressing layer.

### It cannot be in the traversal loop, by the latency arithmetic — `docs/SCALE.md`

    CONFIG  when    2026-07-30
            source  docs/SCALE.md, note 101
            script  tools/walk_rounds.py
            task    a depth-10 walk
            model   peer transport at PROTOCOL 3
            knobs   none
            scale   priced at an assumed 50 ms RTT against d_max 640 ms

Ten sequential hops at ~50 ms is ~500 ms against `d_max`'s 640 ms — about 20% headroom —
and a DHT lookup is several hops of its own. **So it is a PREFETCH source, not a read
path**, and `lasting` becomes a cache over it rather than the bottom of the stack.

### It cannot replace the vectors — `notes 070`, `077`, `078`

    CONFIG  when    2026-07-30
            source  notes 070, 077 and 078
            script  none -- reasoning from what those notes measured
            task    n/a
            model   similarity over the whole representation space
            knobs   none
            scale   n/a

Those notes need similarity over the whole space. A key-value store cannot answer *"which
entity relates most like this one"*, which is the question acquisition rests on.

### It moves the hard question rather than removing it

    CONFIG  when    2026-07-30
            source  note 083, and the reasoning above
            script  none -- nothing built
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

Note 083's *"what will be used"* becomes *"what to prefetch"*. **A better failure mode
though** — a wrong prefetch is a slow answer where a wrong eviction was a lost one.
