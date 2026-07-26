# Note 018 — The fast store has no brakes, and we knew the paper said it should

[g8-02](../../experiments/sweeps/g8-02-when-the-statistics-are-real.txt) found
that skewed input collapses the model onto a single token, and recorded it as the
blocker on the real-text benchmark. Run with warnings as errors, it turns out to
be worse than a collapse: **the readout diverges to NaN**, reproducibly, in the
delta-rule update.

The cause is arithmetic, and it has nothing to do with Zipf.

## The runaway

The fast store is `memory = decay * memory + outer(value, key)`. Repeat one
binding and that is a geometric series, approaching `1 / (1 - decay)`. At the
half-life these sweeps use, `decay = 0.5 ** (1/192) = 0.9964`, so the ceiling is
about **277 times a single binding**.

Then: retrieval is linear in the memory, the readout error is linear in the
retrieval, and the delta-rule update is `lr * error * retrieval` — **quadratic in
the memory norm**. Enough repetition and it runs away.

Measured, without any training, so the effect is isolated from learning:

    zipf_s   top token share   final |memory|   max |retrieved|
       0.0              4.8%            114.1            136.5
       1.0             24.5%            254.1            838.2
       2.0             57.4%            967.4           3452.1

**A 25× growth in retrieval magnitude**, driven entirely by how often the
commonest token recurs. The readout update squares that.

So this is not a property of Zipfian data. **Zipf merely supplies repetition**,
and any recurring input does the same thing — including real language, including
a sensor that reports the same reading twice, including a node that sees a quiet
period. It is a general instability of the fast store.

## The part that is embarrassing

`lasting_cap` exists. It bounds the **consolidated** store, it was added because
salience-driven consolidation reached NaN, and it is justified in the code by
citing Zenke & Gerstner (2017) — whose title is *Hebbian plasticity requires
compensatory processes on multiple timescales.*

**Multiple.** We read that paper, took the compensatory process, applied it to
one store, and left the other unbounded. [BACKLOG.md](../../BACKLOG.md) has been
carrying "multiple timescales, of which we implemented one" as a speculative
architectural idea for a while. It is not speculative. The unimplemented half is
the direct cause of a reproducible divergence that has already contaminated a
sweep.

This is the same shape as [note 015](015-we-implemented-the-tag-and-not-the-competition.md),
which found that tagging and capture had been implemented without the competition
that does the selecting. **Twice now, a borrowed mechanism has been implemented
with the stabilising half left out, and both times the omission produced a
measured failure that looked like a finding about the mechanism.**

## What to build

A cap on the fast store, the same shape as the one on the lasting store: when the
norm exceeds a bound, **scale the whole store**, never an individual entry.
Scaling is local, preserves the relative content, and is the operation a
synaptic-scaling account actually describes; clipping entries is a different
mechanism and a non-local one, which is already pinned by a mutation.

Default off, so nothing previously measured moves.

## What has to be predicted before it runs

1. **It stops the NaN.** Weak — it is a bound, and this is close to arithmetic.
2. **It stops or delays the collapse onto one token.** The collapse and the
   divergence may be the same event or two events, and this separates them.
3. **It changes little at uniform statistics**, where the memory norm is 114 and
   a cap set well above that never binds. If uniform results move, the cap is
   binding where nothing was wrong and the value is mis-set.
4. **It does not rescue the gating result.** Stability is not selectivity. If
   recovery improves, something is being confounded and this note is wrong about
   what the cap does.

Prediction 4 is the one worth stating loudest, because a fix that quietly
improves the headline number is the most dangerous kind.

## Status

**Argument and diagnosis only. The cap is not implemented and nothing above is
measured except the runaway table**, which was produced without training and
without the model's learning path, so it demonstrates the memory growth and not
the divergence itself.
