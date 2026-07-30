# Option record — scoring without a temperature

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- The temperature calibration in the corpus scoring path, added after this.

---

## What was tried, and what came back

### It reads as the model learning nothing — `117`

    CONFIG  when    2026-07-28
            source  decision 117
            script  unrecorded
            task    corpus, character level
            model   raw readout scores through a softmax, no temperature
            knobs   temperature absent
            scale   unrecorded

    the model, uncalibrated   5.920
    a uniform distribution    5.954

**Barely distinguishable from uniform.** The delta rule targets a one-hot, so raw scores
sit in about `[0, 1]`, and a softmax over that range is nearly uniform whatever the model
knows.

**A calibration artefact that looks exactly like a null result** — which is the dangerous
property, because a null is a publishable finding and this one would have been published.

**Revival:** none. A scoring path without a temperature is not measuring the model, and the
fix is not a knob to be swept but a defect to be absent.

### And the grid was too narrow at word level — `135`

    CONFIG  when    2026-07-29
            source  decision 135
            script  unrecorded
            task    corpus, word level
            model   as above, with a temperature grid
            knobs   temperature
            scale   unrecorded

Having added the temperature, the *grid* over it was too narrow at word level — so the
correction introduced a second version of the same class of error one level down. Record:
[word-level-unigram.md](word-level-unigram.md).
