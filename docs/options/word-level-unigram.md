# Option record — `9.323` as the word-level unigram

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/baselines.py`, dependency-free, which is where a baseline is computed rather
  than quoted.

---

## What was tried, and what came back

### It was never that — `135`

    CONFIG  when    2026-07-29
            source  decision 135
            script  unrecorded
            task    corpus, word level
            model   unigram baseline
            knobs   temperature grid
            scale   unrecorded

The figure was wrong, and the temperature grid was **too narrow at word level** — so the
baseline it was compared against was itself mis-calibrated.

**A wrong baseline is worse than a wrong model number**, because every arm is scored against
it and the error is invisible in the ordering. The arms stay internally consistent and the
whole comparison moves together.

This is the second instance of the same class in consecutive entries: `117` scored without a
temperature at all, `135` scored with a grid that did not contain the right one. Record:
[scoring-without-temperature.md](scoring-without-temperature.md).

**Revival:** none for the number. The practice it argues for is that a baseline is computed
by the dependency-free ruler in `openplexus/baselines.py` rather than carried forward.
