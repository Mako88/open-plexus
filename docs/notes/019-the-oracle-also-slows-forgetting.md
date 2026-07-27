# Note 019 — The oracle does two things, and only one was ever named

Found while writing a test for the reward gate's bookkeeping, which failed for a
reason that turned out not to be about the reward gate.

## The line

In `LocalAssociativeMemory.run`, the fast store is written like this:

```python
if previous_key is not None and (store is None or store[t]):
    if self.config.decay < 1.0:
        memory *= self.config.decay
    memory += np.outer(value, previous_key)
```

**The decay is inside the `store[t]` guard.** So a position the oracle masks out
is not merely un-written — it is un-*decayed*. The store does not fade on that
step at all.

## Why that matters

Every gating result in this project compares an `oracle` arm against a `none`
arm, and the difference has always been described as *what gets stored*:

> The oracle gates the fast store, so `memory` holds `2 * n_pairs` bindings
> whatever the sequence length, and that is its entire advantage.
> — g8-03, and repeated in note 015's withdrawal

**That is not its entire advantage.** On MQAR with 4 pairs and 92% filler, the
oracle skips the decay on roughly 92% of steps too. Its effective half-life is
therefore something like an order of magnitude longer than the `none` arm's at
the same nominal `decay`. It stores less *and* forgets more slowly, and the two
have never been separated.

## What this does and does not change

**It does not change any measured number.** Every arm ran as described and the
comparisons are between the things they were said to be between.

**It does change what the oracle's advantage means**, and therefore what a
mechanism has to reproduce to match it. Six mechanisms have failed to recover it.
All six were aimed at *selectivity*. If a meaningful part of the gap is
*retention* — a slower effective fade, available to any node by simply lowering
`decay` — then part of what looked unreachable may be reachable by a dial nobody
turned.

**It is also a confound in the decay sweeps.** g7-04 asked when forgetting starts
to pay and swept half-life against the ungated arm. The oracle arm in the same
grids was, in effect, at a different half-life than the label on the axis.

## How to settle it

Cheap, and it should be run before any more gating mechanisms are built: an arm
that applies `store` to the write but **not** to the decay, so the decay
schedule is identical to the ungated arm's. The difference between that and the
current oracle is the part of its advantage that is retention rather than
selectivity.

If they are close, the finding stands as written. If they are far apart, then a
chunk of six negative results was measuring against a ceiling built partly from
something ordinary.

## Status

**Observed in the code and unmeasured.** The line is there and the logic is
plain, but no number has been taken. Written now because it is upstream of
everything currently queued, and because writing it down before measuring is the
only way the prediction above counts for anything.
