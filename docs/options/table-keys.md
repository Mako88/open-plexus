# Option record — `TableKeys`, one key per token

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/keys.py` — `TableKeys`, one random key row per token.
- `tests/test_keys_conformance.py` asserts both key schemes satisfy the same contract, so
  either can be swapped in at the seam.

---

## What was tried, and what came back

### Where it is right: every entity appearing once — `103`

    CONFIG  when    2026-07-28
            source  decision 103
            script  unrecorded
            task    kinship, 14 people, 10 facts
            model   single-token keys
            knobs   none
            scale   146 sequences at one appearance

At one appearance hop 1 is **0.959** and the scheme is not the limitation. What collapses
it is a second appearance, where one key accumulates one binding per role and a retrieval
returns their sum — measured at **0.366**. The record for the alternative is
[pair-keys.md](pair-keys.md).

### The store carries MQAR completely under it — `142`

    CONFIG  when    2026-07-29
            source  decision 142
            script  unrecorded
            task    MQAR
            model   single-token keys, superposed store
            knobs   store on against off
            scale   unrecorded

**0.995 against 0.000** with the store removed, and the prior that wins on text costs
0.279 there. MQAR is the instrument where each key is queried once, which is exactly the
regime this scheme is built for.
