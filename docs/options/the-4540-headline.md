# Option record — `4.540` bits/char, "unigram BEATEN"

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing. The record exists because the failure is reusable and the number is not.

---

## What was tried, and what came back

### The reproduction FAILED — `117`

    CONFIG  when    2026-07-28
            source  decision 117
            script  unrecorded
            task    corpus, character level, prequential
            model   the named configuration, run under its own rule
            knobs   as specified
            scale   unrecorded

    the named configuration   5.665 to 5.742
    prequential unigram       4.776

**1.1 bits away**, in the wrong direction. The claim was that the unigram had been beaten.

### The figure appears in no sweep and no entry — `118`

    CONFIG  when    2026-07-28
            source  decision 118, note 037
            script  none -- a provenance audit
            task    n/a
            model   n/a
            knobs   none
            scale   the whole repository searched

It appeared **only in a scratch session-swap document**, with no sweep and no decision entry
behind it, and traces to note 037's **4.525** — which that note says is *"trained with
ordinary backpropagation, offline"* on frozen features.

**Wrong twice: not the model under its own rule, and the opposite of prequential.**

### Why it is kept

    CONFIG  when    2026-07-29
            source  decision 118, CLAUDE.md rule 14b
            script  none
            task    n/a
            model   n/a
            knobs   none
            scale   carried as the headline text result for weeks

**An inherited headline with no provenance outranks every measurement downstream of it.**
Nothing that ran afterwards could have caught it, because everything was conditioned on it.

That is the same shape `note 105` found in a single day — a citation chain where each
document points at another and none holds the run — and it is why `script` is a required
field beside `source` in every option record.

**Revival:** none. The number is not a measurement of this model.
