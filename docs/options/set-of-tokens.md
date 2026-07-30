# Option record — an answer is a SET of tokens, scored by `exact` and F1

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/answers.py` — `exact`, F1, and `single_token_accuracy`. Dependency-free,
  like the rest of the ruler.
- `tests/test_answers.py`, `tests/test_answer_set.py`.

---

## What was tried, and what came back

### The ruler was built before anything produced a set — `165`

    CONFIG  when    2026-07-29
            source  decision 165
            script  openplexus/answers.py
            task    none -- the measurement convention itself
            model   n/a
            knobs   none
            scale   n/a

Built deliberately ahead of the mechanism, so the mechanism could not be graded on a scale
invented to fit it.

**It degenerates exactly.** On singletons `exact` IS the old accuracy, and
`single_token_accuracy` recovers the old number while raising on anything else — so every
previous result stays comparable rather than being re-baselined.

### Recall alone is never reported, and the trap fired within one commit — `165`, `167`

    CONFIG  when    2026-07-29
            source  decisions 165 and 167
            script  openplexus/answers.py
            task    families, set-valued question
            model   gated collection
            knobs   the gate removed
            scale   unrecorded

Emitting the whole alphabet scores recall **1.000** and F1 **0.400**. That is why recall is
never reported alone — and the trap fired immediately: removing the gate in `167` **raised**
recall while precision fell.

**A metric that can be gamed by emitting everything is not a metric**, and the F1 pairing is
what makes the set answer scoreable at all.
