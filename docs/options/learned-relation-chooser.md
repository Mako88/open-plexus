# Option record — a learned relation chooser

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing.

---

## What was tried, and what came back

### Deferred on an argument from what is strictly easier — `147`, `note 052 §2`

    CONFIG  when    2026-07-29
            source  decision 147, note 052
            script  none -- nothing built
            task    none
            model   the model as it stood when 147 ran
            knobs   none
            scale   n/a

Two **hand-made** selection rules were refuted before membership worked — norm at 0.247
against plain addressing's 0.783, and decode margin at 0.581 against a summed baseline of
0.688. A learned chooser is strictly harder than either, so note 052's recommendation was
to measure try-all-and-gate first and not attempt this yet.

Records for the two that were refuted: [select-by-norm.md](select-by-norm.md) and
[select-by-decode-margin.md](select-by-decode-margin.md).

### And the problem it addresses has since been solved another way — `note 090`

    CONFIG  when    2026-07-30
            source  note 090
            script  tools/generation_delta.py
            task    CLUTRR kinship
            model   generation delta learned from loop constraints
            knobs   none
            scale   9,074 puzzles, 20 relations

Supplying the missing step's DISPLACEMENT rather than its name closes the composition
ceiling without any chooser existing. Record: [generation-delta.md](generation-delta.md).
