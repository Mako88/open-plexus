075 — Note 065's +0.219 does not reproduce, and its beam number nearly does
=========================================================================

**Status:** measured, three seeds, with `tools/clutrr_recovery.py` committed alongside so
this is checkable. **It does not refute note 065** — it says the configuration behind the
project's largest claimed mechanism gain is unrecorded and the gain is much smaller in the
one harness that exists.

---

## IN PLAIN TERMS

Note 065 reports that hedging at every step of a traversal instead of only the first is
worth **+0.219** — the biggest single improvement in this project's record. Note 074 found
that nothing in the repository actually runs that measurement, so the settings behind it
were lost.

Rebuilding it gets close on one number and not on the other. **The improved method scores
about what 065 said. The method it was compared against scores much better than 065 said** —
so the gap between them, which is the whole finding, comes out at less than half.

**Nothing here says 065 was wrong.** It says the number cannot currently be confirmed, and
that a result built on top of it would inherit the doubt.

---

## The measurement

`tools/clutrr_recovery.py`, kinship layout, `context_keys` with `derived_keys`, decay 1.0,
`branches=4`, `beam(width=4)`, CLUTRR test split, all 1,146 puzzles, chain recovery.

    width 64, three seeds

    seed      search      beam       065 search   065 beam    plain
       0      0.7914    0.8770           0.6588     0.8735    701/713
       1      0.8063    0.9293           0.6632     0.8831    710/713
       2      0.7452    0.8569           0.6623     0.8848    685/713
    mean      0.7810    0.8877           0.6614     0.8805
    gain             +0.1067                     +0.2190

**`beam` reproduces to within 0.007 of the mean. `search` is high by 0.12.** So the gain is
**+0.107 against 065's +0.219** — less than half, and the discrepancy is entirely in the
baseline arm.

## It is not a width effect, which is what makes it a configuration difference

    width     search      beam     plain
       32     0.4887    0.6047   504/713
       64     0.7914    0.8770   701/713
      128     0.8080    0.9363   712/713

**No width matches all three targets.** `search` only approaches 0.659 where `beam`
collapses to 0.60, and at width 128 `beam` reaches **0.9363 — above 065's best seed** while
`plain` reaches 712/713. A harness that is *better* than the one being reproduced is not a
broken harness; it is a different configuration.

## What is most likely, and it is stated as a hypothesis

The discrepancy sits entirely in `search`, so the candidates are about how `search` was
invoked rather than about the store: a different `branches`, a different depth convention,
or a `search` arm that was not given the `allowed` mask the beam arm had. **Untested** —
each is one run, and they are the obvious next step.

## The gate, and my own first version of it failed the rule it enforces

The script prints 065's numbers beside its own and says whether they match. **The first
version checked `beam` alone and printed "the configuration is recovered"** while `search`
was off by 0.13 and the plain subset by twelve rows.

> **Fifth instance in one session of a subset reported as the whole**, and this one is the
> most pointed: it happened inside the instrument built to prevent exactly that. The gate now
> requires every reported number, and the comment in the source says why.

## What follows for the partitioning work

**The concept-partitioning measurement should be taken against THIS harness, reported as
its own baseline, and not against 065's numbers.** Two harnesses differ for two reasons at
once, and note 074's argument applies to 065 as much as to anything: a difference is only
meaningful within one instrument.

So the partitioned run is unblocked — it just cannot be described as a comparison to
0.8805.

## What is NOT claimed

**Not that 065's mechanism claim is wrong.** Per-step branching beats single-step branching
in both harnesses, on every seed, which is 065's qualitative finding and it stands. What
does not reproduce is the *size*.

**Not that the recovered configuration is right either.** It reproduces one of three
numbers. It is the best instrument available and it is not confirmed, which is why the gate
prints a failure rather than a pass.

**And not that 065's plain-subset 713/713 is unreachable** — width 128 reaches 712/713, so
that figure looks like a width away rather than a mechanism away.
