# Option record — note 050's linked-families task, as first designed

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `family_links` in `openplexus/tasks/families.py`, and the fairness rail that refuted the
  first design.

---

## What was tried, and what came back

### Refuted by its own rail, on the first run — `155`

    CONFIG  when    2026-07-29
            source  decision 155
            script  unrecorded
            task    linked families as note 050 first specified them
            model   the standard arm
            knobs   none
            scale   the rail was a p90 calibration

The rail was a **p90 calibration** — it flags what chance produces on this task — and the
first run's numbers were inside it. So the task could be passed without the mechanism it
was designed to test.

**Worth keeping as the example of a fairness check paying immediately.** A rail that fires
on the first run of the thing it was written for has cost nothing and saved a whole line of
work built on a number that meant nothing.

### The LINKED run is still not informative, and the reason is one constant — `162`

    CONFIG  when    2026-07-29
            source  decision 162
            script  unrecorded
            task    linked families
            model   a hop carrying one relation for the whole walk
            knobs   hop_relation
            scale   unrecorded

Not the rail this time. **A hop could not carry its own relation**, so the linked structure
was unreachable regardless of the task's design — and 162 split the question into *which*
relation and *whether a hop can carry one at all*, with the second blocking first.

`164` then built `hop_relations` and the LINK→FACT walk reaches the linked family's value.
Records: [hop-relation.md](hop-relation.md) and [hop-relations.md](hop-relations.md).

### And note 050's wider gap is still open — `note 050`

    CONFIG  when    2026-07-29
            source  note 050
            script  none -- an instrument gap
            task    none exists
            model   n/a
            knobs   none
            scale   n/a

**No task has both** an address that is never written and a composition over it, which is
what blocks `index_at_hops`. Record: [index-at-hops.md](index-at-hops.md).
