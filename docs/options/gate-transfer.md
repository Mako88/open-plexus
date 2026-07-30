# Option record — transferring the halting gate to new terminator tokens

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing. The measurement that refuses it is `089`'s, and it refuses it by construction
  rather than by a score.

---

## What was tried, and what came back

### Impossible by construction — `089`

    CONFIG  when    2026-07-28
            source  decision 89
            script  unrecorded
            task    chains
            model   `halt_w` at +8.3 sd on one token's value vector
            knobs   none
            scale   n/a

The gate is a **token detector**: `halt_w` sits +8.3 sd on one specific token's value
vector. Two markers have **unrelated value vectors** — `Wv` is frozen random — so a
detector aligned to one carries no information about the other. There is nothing to
transfer, and no amount of training on the first marker changes that.

**Do not re-propose.** **Revival:** an unfrozen or structured `Wv` in which two terminators
share representational structure, which is what decision 93 says this points at. Record for
the attempt on that: [token-agnostic-terminal.md](token-agnostic-terminal.md).
