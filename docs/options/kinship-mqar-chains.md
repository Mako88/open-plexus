# Option record — `kinship.py`, `mqar.py`, `chains.py`

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/tasks/kinship.py`, `mqar.py`, `chains.py`. All dependency-free.
- `tests/test_kinship.py`, `test_mqar.py`, `test_recurrent_mqar.py`, `test_chains.py`,
  `test_zipf_filler.py`.

---

## What was tried, and what came back

### `kinship.py` is the mechanism testbed

    CONFIG  when    2026-07-28
            source  decisions 101-107, 121-123, 158, 164, and g21-01
            script  openplexus/tasks/kinship.py
            task    n/a -- the instrument itself
            model   n/a
            knobs   hops, out-degree, entity repetition
            scale   n/a

Nearly every composition mechanism in the tree was measured here first: the hop
accumulator, pair keys, the traversal, search, the beam, `hop_relation` and
`hop_relations`. It is also `run()`'s own task, which is why `note 103`'s numbers are the
ones that decide a default and CLUTRR's are not.

### `mqar.py` is the store's control, and the only instrument isolating it from a prior — `142`

    CONFIG  when    2026-07-29
            source  decision 142
            script  openplexus/tasks/mqar.py
            task    MQAR
            model   superposed store
            knobs   store on against off
            scale   unrecorded

**0.995 with the store against 0.000 without**, and the prior that wins on text costs 0.279
here. That gap is the whole reason the instrument exists: on every other task a prior can
substitute for the store, so a good score does not attribute.

### A defect found in the FIRST sequence ever generated

    CONFIG  when    2026-07-25
            source  CLAUDE.md rule 6 calibration
            script  tests/test_mqar.py
            task    MQAR generation
            model   n/a -- the generator
            knobs   filler drawn from the whole key range
            scale   found by printing one sequence and reading it

The filler drew from the whole key range, so a filler token could be **byte-identical to a
query token while requiring a different output**. The mechanism appeared to be creating
difficulty and was actually creating **impossibility**.

Found in minutes, because someone looked. Had it not been, every G0 number would have been
a measurement of an impossible task and the flatness would have looked like a result.
`test_a_used_key_never_appears_as_filler` guards it and the `filler-collides-with-keys`
mutation confirms the guard bites.

### `chains.py` is solved at 1.000 and is a CONTROL — `083`, `108`

    CONFIG  when    2026-07-28
            source  decisions 83 and 108
            script  openplexus/tasks/chains.py
            task    chains
            model   one hop, then two
            knobs   none
            scale   unrecorded

`083` one hop is perfect and two hops answer the intermediate 100% of the time. **Out-degree
1 by construction**, which is what makes it a control rather than a benchmark — and what
makes a composition sweep on it uninformative. Record:
[composition-sweep-on-chains.md](composition-sweep-on-chains.md).
