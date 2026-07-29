# 050 — The missing instrument: composition over something never stated

**Status:** a task design, nothing built. Written because decisions 153 and 154
together move the blocker off the mechanism and onto the instrument, and GOALS §4
puts the instrument first for exactly this reason.

**Answers:** the question STATE now carries — *should the index propose
neighbours of the position's concept or of the hop's landing concept?* — by
observing that **no existing task can tell the two apart.**

---

## Why this note exists

Decision 152 said the read gate and the hop mechanism exclude each other.
Decision 154 measured the guard's premise and found it false: a hop key sits at
cosine 0.96 to a single token's row, so `argmax(wk @ hop_key)` names a concept
the index could look up. The blocker became a design choice.

**But the design choice cannot be decided, because nothing measures it.** The
gate pays where an address was never written. The hop pays where the answer is
not at any single address. Every task in this project has one or the other:

    families   addresses never written ✓   composition ✗
    kinship    addresses never written ✗   composition ✓
    chains     addresses never written ✗   composition ✓
    MQAR       addresses never written ✗   composition ✗
    closure    every address unwritten at scoring time — uselessly, decision 151

Decision 153's principle says why this is structural rather than an oversight:

> **Occupancy is informative exactly where an address is READ BEFORE IT IS
> WRITTEN within the sequence.**

The composition tasks all state their facts before querying them, because that is
what makes them answerable at all. So they write every address they later read,
and the gate has nothing to say — not because it fails, but because there is
nothing there to detect.

**Building the combined mechanism now would produce a number that means nothing.**
On chains the gate never fires (153), so gate-plus-hops would score exactly what
hops score, and the two design options would be indistinguishable. That is the
shape of decision 143's circularity complaint, one level up.

## The task

Families gives an entity's value by its family. Compose that:

    FACT   entity     value          as today
    LINK   familyA    familyB        a relation BETWEEN families
    QUERY  entity     ?              answer: the value of entity's family's
                                     LINKED family

An entity whose own fact was never stated must (1) notice its address is empty,
(2) reach its family, (3) follow the family link, (4) read the linked family's
value. **Step 1 is the gate. Step 3 is the hop.** Neither alone answers it.

And it separates the two design options directly, which is the whole point:

    position's concept      the index proposes siblings of the ENTITY, and the
                            hop then has to travel from there
    hop's landing concept   the index proposes neighbours of whatever the hop
                            landed on, so the link can be followed at the point
                            it is needed

**These give different answers on this task and identical answers on every
existing one.** That is the definition of an instrument that can decide a
question, and the reason to build it before building either option.

## What must be true for this to be a fair instrument, not a rigged one

Decision 143's lesson, applied in advance rather than in retrospect: `families`
gave every member of a family one shared value, so *"group by family"* and
*"know the answer is shared"* were nearly the same statement, and the result was
partly circular.

**The equivalent trap here** is making the family link recoverable from
co-occurrence alone, so the content index could answer without any hop. The link
must therefore be **stated as a fact in the sequence** and drawn independently of
the co-occurrence structure the index is fitted on. If `ContentIndex` can rank
the linked family above chance from background streams, the task is measuring the
index and not the composition.

**That check comes first**, as g19-00 came before g19-01 — a task calibration
that asks whether the structure is discoverable, before any arm is run.

## The arms this needs

    plain          no index, no gate. Composition by hops alone
    indexed        neighbours summed, position's concept
    inherit        gate on, position's concept          <- design option A
    inherit-hop    gate on, hop's landing concept       <- design option B

## Predictions, to register before building

  T1  THE INSTRUMENT. `plain` scores near chance on queries about entities whose
      own fact was never stated. If it does not, the task is answerable without
      the gate and measures something else.

  T2  THE SEPARATION. `inherit` and `inherit-hop` differ by more than 0.05 on
      those queries. **This is the prediction the note exists for** — if they do
      not differ, the design choice does not matter and decision 154's careful
      refusal to pick one was wasted caution.

  T3  THE RAIL. Both reach at least `indexed` on queries whose fact WAS stated,
      as `inherit` did on every task in decisions 148–151.

  T4  THE FALSIFIER. If the gate fires on fewer than 0.9 of never-stated queries,
      the layout writes the address before the query reads it — decision 153's
      condition — and the task is a composition task with no gate signal, which
      is what the existing five already are.

## The cost, honestly

This is a **new task file plus a calibration experiment plus a four-arm sweep**,
which is the largest thing this line has proposed. What justifies it is that the
alternative is building a mechanism whose two variants no existing measurement
can tell apart.

It also touches `families.py`, which decisions 148 and 149 are measured on. The
link must therefore be **off by default** and the existing generator byte
identical without it, or every number in 148–151 stops reproducing — decision
74's failure, and the reason `--n-values` and `--branches` default to `None`.

**Not built. Not started.** The calibration check above is the part that decides
whether the rest is worth writing.
