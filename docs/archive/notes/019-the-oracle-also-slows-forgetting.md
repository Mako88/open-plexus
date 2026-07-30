# Note 019 — The oracle does two things, and only one was ever named

> ## MEASURED, AND THE CONCERN IS REFUTED — which is the good outcome
>
> `decay_when_masked` was built and run at seq_len 768, three half-lives, three
> seeds. Accuracy at each cell's best learning rate:
>
> | half-life | none | oracle | oracle-decayed | retention share |
> |---|---|---|---|---|
> | 0.5 | 0.831 | 0.996 | 0.996 | **0.00** |
> | 0.25 | 0.782 | 0.996 | 0.996 | **0.00** |
> | 0.125 | 0.633 | 0.998 | 0.996 | **0.01** |
>
> **Prediction 1 refuted.** It guessed the decay-matched oracle would fall to
> 0.80–0.95. It falls to 0.996 — indistinguishable from the oracle that skips the
> fade. Essentially **none** of the oracle's advantage is retention.
>
> The observation in this note is still true: the fade genuinely does sit inside
> the `store[t]` guard, and a masked step genuinely is un-faded. It simply does
> not matter. The likely reason is that the oracle stores so little — eight
> bindings against a sequence of 768 — that its store is nowhere near crowded
> enough for the fade rate to bite. `SNR = sqrt(d / N)` is dominated by `N` being
> small, and how hard the few survivors have faded barely enters.
>
> **So g8-03's statement stands as written**: the oracle gates the fast store and
> that is its entire advantage. The six mechanisms that failed to match it were
> compared against the right ceiling, and none of their results move.
>
> Prediction 4 held trivially — the ungated arm is untouched. Prediction 3, that
> the gap grows with length, is **untested**: only seq 768 was run, so a
> length-dependence has not been ruled out at 1536 where the compounding would be
> largest. Recorded as a gap rather than a pass.


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

## Predictions, written before the arm exists

`decay_when_masked` will apply the fade on every step regardless of the store
mask, so an oracle arm using it is **selectivity without the retention bonus**.
Everything below is guessed before any code is written.

1. **The decay-matched oracle scores materially below the current one.** At
   `seq_len` 768 and half-life 0.25 the current oracle sits at 0.998. Skipping
   the decay on 92% of steps stretches its effective half-life by roughly an
   order of magnitude, so removing that should hurt. Guess: **0.80–0.95**, and
   anything above 0.98 means the retention bonus was never doing much and this
   note is a curiosity.

2. **Recovery figures roughly double, and stay small.** Recovery is
   `(arm - none) / (oracle - none)`, so a lower ceiling raises every ratio
   measured against it. If the oracle drops from 0.998 to about 0.9 while `none`
   stays near 0.64 at that cell, the denominator shrinks by about a third and
   recoveries near 0.05 become near 0.08. **Six failed mechanisms do not become
   successes**; they become slightly less bad, measured against an honest
   ceiling.

3. **The gap between the two oracles GROWS with sequence length.** The retention
   bonus is a compounding factor — skipped decays accumulate — so it should be
   worth little at seq 192 and a lot at 1536. If it is flat in length, the effect
   is not the one described here.

4. **`none` is unaffected**, since it has no mask and therefore decays on every
   step already. A control: if the ungated arm moves at all, the flag is reaching
   something it should not.

Most likely wrong: (2). It assumes the ceiling moves and the floor does not, and
the arms in between may not sit where the arithmetic suggests.

## Status

**Observed in the code and unmeasured.** The line is there and the logic is
plain, but no number has been taken. Written now because it is upstream of
everything currently queued, and because writing it down before measuring is the
only way the prediction above counts for anything.
