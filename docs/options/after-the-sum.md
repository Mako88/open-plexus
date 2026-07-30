# Option record — recovering per-item information AFTER the sum

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Four mechanisms, all built and all still in the tree as switches: `readout_bias`,
  competitive retrieval, `orthogonal_every`, and pair keys used for recovery rather than
  for role separation.
- The read they all sit behind: `r = M @ key`, which is a sum.

---

## What was tried, and what came back

### Everything moves the level; nothing moves the slope — `69`

    CONFIG  when    2026-07-27
            source  decision 69
            script  unrecorded
            task    corpus, character level, 4,000 to 250,000 characters
            model   superposed store, linear readout
            knobs   width, cache_slots, sparse keys, pair keys, trained Wv, carry_store
            scale   four corpus sizes, seed spread stated in the entry

    mechanism            effect on LEVEL      effect on SLOPE
    width, 4x                    +0.089                 none
    exact cache, 128 slots       +0.19 (g11-06)         none
    sparse keys, k=4             +0.15                  none
    pair keys                    -0.23                  none
    trained Wv                   -0.45                  none
    carry store (training)       -0.15                  none

**Six mechanisms, three of them helpful, and not one changes the convergence point.** The
model converges by about 16,000 characters and then stops; these move where it converges
TO. The backprop baseline over the same range moves 0.95 bits.

Sparse keys at 4,000 characters already beat dense keys at 250,000 — a per-character
efficiency win rather than a raised ceiling, and they saturate exactly as fast.

### Why one reason covers all four

    CONFIG  when    2026-07-27
            source  decision 69, and the g11 line
            script  none -- the arithmetic of the read
            task    n/a
            model   `r = M @ key`
            knobs   none
            scale   n/a

The read is a sum of every binding written to that key, weighted by key overlap. Nothing
applied to `r` can separate contributions that were added together before `r` existed. A
readout bias, a competitive selection among candidates, an orthogonalising update and a
finer key all act at or after that point, and all four failed for the same reason rather
than four different ones.

**The fix that this does point at is per-step fidelity** — fewer things summed at one
address — which is what pair keys do when used for role separation rather than recovery,
and what concept partitioning does at the node level.
