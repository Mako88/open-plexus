# 022 — The signal was there, pointing backwards

**Status:** measured by [g9-04](../../experiments/sweeps/g9-04-is-there-a-local-signal.txt), 12 of 12 cells.
**Explains:** two of the six failed mechanisms, and g9-03's fall-off.

---

## What was asked

[g9-03](../../experiments/sweeps/g9-03-is-the-cliff-reach-or-cost.txt) settled
that a tag is a mechanism rather than a saving. Before building it, one question:
**is there anything local for a tag to hang on?**

Reading `reward_recall`'s generator answered half of it before anything ran.
Rewarded cues are chosen with `rng.sample(cues, n_rewarded)` — uniformly, out of
the same alphabet as the filler. **Nothing local can predict reward**, by
construction, and a tag claiming to would be `position_kinds()` in disguise.

That leaves the half the biology describes: a cheap marker on everything worth
marking, decaying, captured by a late signal. Not selective about value —
selective about *being a binding at all*. Which turns on a number nobody had
printed:

    body 744 steps / 24 bindings  ≈  31 steps per binding

A 64-step window holds **two bindings and sixty-two steps of filler**.

## What came back

AUC separating a binding-write from a filler-write. 0.5 is no information; below
0.5 means the signal separates them backwards, which is still usable.

| signal | width 32 | width 64 | |
|---|---:|---:|---|
| surprise | 0.499 | 0.492 | noise |
| **strength** | **0.293** | **0.215** | **inverted** |
| deviation from mean | 0.379 | 0.328 | inverted |
| hit | 0.492 | 0.490 | noise |
| position | 0.479 | 0.479 | noise |

Every rewarded-vs-unrewarded cell is noise. The control holds; there is no leak.

## Four things in that table

**1. There is a signal, and it is the one nothing was built on.** Retrieval
strength separates, inverted: **admit the weak retrievals**. Filler is drawn with
replacement from 40 spare cues over ~700 positions, so a filler key has been
bound many times and retrieves strongly. A binding's cue is fresh and retrieves
weakly.

[Competitive capture](015-we-implemented-the-tag-and-not-the-competition.md) ranks on exactly this
quantity and admits the **strongest** traces. It was pointed backwards. That is a
mechanistic account of a failure previously filed under base rates.

It also *improves* with width — 0.293 → 0.215 — which was the half named most
likely wrong. Seed spread is ±0.02, the tightest thing this project has produced.

**2. Predict-the-future-and-compare carries nothing.** `hit` is 0.49 everywhere.
John asked whether it deserved another look given everything fixed since it last
failed. It did, and this is the first time the raw signal has been scored against
the label instead of inferred from a mechanism that could have failed for six
other reasons. The mechanisms were not what was wrong about it.

**3. Surprise carries nothing either — but its deviation does, inverted.** So
binding-writes sit *closer* to the node's typical surprise, and filler occupies
both tails. The [salience gate](013-salience-and-the-missing-body.md) fires on both
tails. **It was firing preferentially on filler.** Second mechanism explained
rather than merely recorded.

**4. Recency carries nothing: 0.479.** A window ranks on recency and nothing
else, so its only virtue was ever *reaching* the binding, never *selecting* it.
That is g9-03's fall-off seen from the other side — past the point where reach
covers the delay, every additional step is admitted by a signal with no
information in it.

## What it does not settle

- **A separable signal is not a working mechanism.** 0.22 is a good AUC and it is
  not 0. A capacity-limited tag admitting on weak retrieval still has to beat the
  window's 0.25 recovery, and nothing here says it will.
- **One task, one filler density.** The separation exists *because* filler
  repeats and bindings do not. On a corpus where informative tokens are also
  frequent, the inversion reverses — and note 013's collapse-onto-modal-token
  finding says that case is real.
- **Untrained readout** (`wo = wv`), as everywhere here.

## The method note, which is the transferable part

The pilot ran at 8 sequences and put `position` vs rewarded at **0.617**, which
looks like a finding. At 32 sequences it was 0.541; at 32 across six seeds,
0.510. Nothing changed but the sample.

That number is left in the pre-registration on purpose. It is what an
under-powered cell looks like next to the same cell with more data, and **the
difference is larger than several results this project has taken seriously.**

---

*Next: build the tag, admitting on weak retrieval. Related:
[015 — competition for a finite pool](015-we-implemented-the-tag-and-not-the-competition.md),
[021 — reach has to be matched](021-reach-has-to-be-matched.md).*
