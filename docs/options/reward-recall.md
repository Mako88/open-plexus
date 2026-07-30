# Option record — `reward_recall.py`

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `tests/test_reward_recall.py` and `tests/test_reward_gate.py` survive the instrument
  itself.
- `LocalMemoryConfig.reward_token` and `reward_window`, which it was built to exercise.

---

## What was tried, and what came back

### Retired — `126`

    CONFIG  when    2026-07-28
            source  decision 126
            script  unrecorded
            task    reward_recall
            model   n/a
            knobs   reward_token, reward_window
            scale   n/a

### It was the literature search that was skipped, not the build — `note 017`

    CONFIG  when    2026-07-26
            source  note 017, CLAUDE.md's search-before-you-build rule
            script  none -- an audit finding
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

The instrument was built from note 017's five-point requirements list. **That list turns out
to describe bsuite's Memory Length test** — a T-maze parameterised by length, testing how
many steps an agent can hold one bit.

**The list was a search query and was not used as one.** That is the calibration behind the
rule that prior art is searched when the requirements are written rather than when the code
is: the version in the literature is better specified than the one derived at a desk, and
finding it afterwards costs the build twice.
