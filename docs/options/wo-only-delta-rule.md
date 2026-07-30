# Option record — `Wo` only, delta rule at scored positions

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- The delta-rule update on `Wo` in `openplexus/models/local_memory.py`. `Wk` and `Wv` are
  frozen random; the store is rebuilt per sequence.

---

## What was tried, and what came back

### It is the EXACT gradient, not an approximation of backprop

    CONFIG  when    2026-07-28
            source  note 042 section 4
            script  none -- a property of the architecture
            task    n/a
            model   a single linear readout over the retrieval
            knobs   none
            scale   n/a

For a single linear readout the delta rule *is* the gradient. **There is nothing to
backpropagate through**, so the usual objection to local learning does not apply here — and
that is a statement about how little the architecture currently contains, not about the
rule being powerful.

### The rule is not the limitation; the absence of anything to write to is — `note 042 §4`

    CONFIG  when    2026-07-28
            source  note 042
            script  none -- an architecture pass
            task    n/a
            model   Wk and Wv frozen random, store rebuilt per sequence
            knobs   none
            scale   n/a

**Everything durable is one linear map.** `Wk` and `Wv` never move and the store does not
survive a sequence, so `Wo` is the entire persistent state of the learner.

That framing is what makes the two adjacent options meaningful rather than incremental:
unfreezing the values ([value-lr.md](value-lr.md)) and giving the readout depth
([hidden-readout.md](hidden-readout.md)) are both attempts to enlarge what there is to write
to, and only one of them helped.

### Training on every position costs composition — `095`–`098`

    CONFIG  when    2026-07-28
            source  decisions 95, 96, 97 and 98
            script  unrecorded
            task    composition over chains
            model   delta rule at scored positions against every position
            knobs   scored-only against all-position; gate objective
            scale   unrecorded

**1.000 → 0.40** when every position is scored. `095` the gate is not outvoted, it is
CONFLICTED, which is a mechanism problem rather than a ratio problem; `096` letting the gate
see WHERE it is **triples** all-position accuracy and is still not enough; `097` density
raises the level and does not remove the decay; `098` giving the gate its OWN objective is
what removes it.

**Do not re-propose all-position training without a separate gate objective.** Record:
[training-every-position.md](training-every-position.md).
