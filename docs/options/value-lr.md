# Option record — `value_lr` / `value_centre`, unfreezing the values

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.value_lr`, `value_centre`, `value_from_readout`.
- `tests/test_learned_value_projection.py`, `tests/test_value_projection.py`.

---

## What was tried, and what came back

### It works, and it does not help — `114`

    CONFIG  when    2026-07-28
            source  decision 114
            script  unrecorded
            task    corpus, character level
            model   `Wv` trained rather than frozen random
            knobs   value_lr, value_centre
            scale   unrecorded

The values **move a long way and stay spread** — so the mechanism is connected and is doing
what it says — **and the plateau does not budge.** That combination is the useful shape: a
working mechanism that changes nothing rules out an explanation rather than leaving it
open.

It is one of the three competitors decision 115 eliminated by name. Record:
[saturation-closed.md](saturation-closed.md).

### It does not build a terminator class — `094`

    CONFIG  when    2026-07-28
            source  decision 94
            script  unrecorded
            task    chains with several terminator markers
            model   `Wv` trained
            knobs   value_lr; separators as targets
            scale   unrecorded

The gate learns whatever depth dominates training instead, and **making separators into
targets breaks the gate**. Record: [token-agnostic-terminal.md](token-agnostic-terminal.md).

### And it costs bits on text — `69`

    CONFIG  when    2026-07-27
            source  decision 69
            script  unrecorded
            task    corpus, character level, 4,000 to 250,000 characters
            model   as above
            knobs   trained Wv
            scale   four corpus sizes

**−0.45 on the level**, the largest negative in decision 69's table of six mechanisms, and
no effect on the slope.

**Do not re-propose as a fix for collapse.** **Revival:** a task where the value space
itself is the bottleneck, which the three measurements above jointly argue is not the case
on anything measured here.

### A learned value projection in its cheapest form is refuted — `64`

    CONFIG  when    2026-07-27
            source  decision 64
            script  unrecorded
            task    corpus
            model   learned value projection, cheapest form
            knobs   value_from_readout
            scale   unrecorded

The earliest entry in this line, and it points the same way as the three above.
