# Option record — sum the two retrievals

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing selecting this way. It survives as the baseline the selection rules are scored
  against.

---

## What was tried, and what came back

### It averages, so it cannot choose — `146`

    CONFIG  when    2026-07-29
            source  decision 146
            script  unrecorded
            task    families with exceptions
            model   read at the surface address and the concept address, add them
            knobs   summed against selected
            scale   unrecorded

Adding two retrievals produces a blend of the specific answer and the family default. On a
direct question the default is noise; on a transfer question the specific read is empty; on
an exception the two actively disagree. **The mechanism has no way to express "use this one
and not that one"**, which is what the row needs.

The summed baseline scores **0.688**, which both hand-made selection rules in decision 147
then failed to beat.

### And the refutation turned out to be about the QUESTION — `167`

    CONFIG  when    2026-07-29
            source  decision 167
            script  unrecorded
            task    families, set-valued question
            model   gated collection over index-proposed neighbours
            knobs   none
            scale   unrecorded

Decision 146 found that this mechanism can only average rather than select, and 147 refuted
the ways to choose. **Neither objection applies to a set answer, because nothing has to be
selected** — collecting is the right operation when the answer is a set. So 167 is decision
146's refuted mechanism, unchanged, in a place where the refutation does not bite. Record:
[gated-collection.md](gated-collection.md).
