# Option record — a token-agnostic terminal signal

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.value_lr` and `value_centre`, the two knobs that unfreeze `Wv` and
  were the route tried.
- `tests/test_learned_value_projection.py`, `tests/test_value_projection.py`.

---

## What was tried, and what came back

### There is none, and that is what points at frozen `Wv` — `093`

    CONFIG  when    2026-07-28
            source  decision 93
            script  unrecorded
            task    chains with several terminator markers
            model   frozen random `Wv`
            knobs   none
            scale   unrecorded

No signal separates "this is a terminator" from "this is that particular token", because
with a frozen random `Wv` two markers have unrelated value vectors and there is no class
for a detector to find. The absence is a property of the representation rather than of the
gate.

### Unfreezing the values does not build the class — `094`

    CONFIG  when    2026-07-28
            source  decision 94
            script  unrecorded
            task    chains with several terminator markers
            model   `Wv` trained at a learning rate
            knobs   value_lr
            scale   unrecorded

`value_lr` **does not build a terminator class**, and the gate learns whatever depth
dominates training instead. Making separators into targets **breaks the gate** rather than
generalising it.

So the route from 093's diagnosis to a fix was tried and did not work. Record for the wider
result about unfreezing values: [value-lr.md](value-lr.md).
