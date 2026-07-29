# 048 — A task where concepts can mean something

**Status:** a design, not a measurement. Nothing here has run.
**Answers:** g17-01's option 3, chosen in July and never built, and John's
2026-07-29 restatement of the goal.
**Blocks:** any further test of `concepts.Shared`, `grouping.cluster` or
`keys.ByConcept`, all of which are built, tested, and have nowhere to show
whether they mean anything.

---

## IN PLAIN TERMS

Every task this project has works on symbols that are interchangeable — entity 4
is no more like entity 5 than like entity 40. So there is nothing for a *concept*
to be. Grouping things that resemble each other cannot pay when nothing resembles
anything.

This is a task where some things genuinely are alike, and where being aware of
that is the only way to answer some of the questions.

---

## Why the existing instruments cannot answer this

`kinship.py` and `closure.py` are the right **shape** — relational, not
continuation — and John confirmed on 2026-07-29 that they capture the idea. But
g17-01 recorded the gap in July and it was never acted on:

> *"MQAR and the relational tasks are solved by this model, but their entities
> are arbitrary by construction, so there IS no similarity for an index to find.
> Running there would measure an index over noise."*

So the relational tasks give **relations between arbitrary symbols**. The goal
asks for **relations between things that also resemble each other** — one concept
with several surfaces, concepts that share attributes with their neighbours. A
model that groups well has nowhere to show it.

**g17-01's own conclusion was to build this**, and the project went to word-level
text instead. Decisions 135–142 are what that cost.

## The design

**Entities have families.** Each entity token belongs to one of `F` families.
Entities in a family share observable company: they appear in similar contexts, so
`ContentIndex` — which learns from co-occurrence and nothing else — can discover
the family without being told it. That is the *designed similarity*, and it is
designed to be discoverable by the machinery already built rather than handed
over.

**Facts are stated once and arbitrary.** `X R Y` appears in the sequence. The
pairing is redrawn every sequence, so no statistic over the corpus predicts it —
note 047's condition for the store to be able to pay at all.

**Two kinds of query, and the second is the point.**

    DIRECT       "X R ?"  where X R Y was stated in this sequence.
                 Tests the store. This is MQAR with dressing, and the model
                 already scores 0.995 on that.

    TRANSFER     "X R ?"  where X's R was NEVER stated -- but the R of other
                 members of X's family WAS.

**TRANSFER is the whole experiment.** A model that treats X as an arbitrary
symbol has been told nothing about it and can only guess. A model that has
grouped X with its family can answer from what it holds about the others. That is
"aware of the interrelations between concepts", made scoreable.

**The family→R mapping is redrawn per sequence**, exactly as MQAR redraws its
pairs. Otherwise a global prior learns "family 3 answers 7" and transfer becomes
counting again — the failure note 047 describes, one level up.

## The controls, and one of them is free

    SHUFFLED     the same grouping built over SHUFFLED attributes. Families
                 exist but mean nothing. TRANSFER must fall to chance.
    NOSTORE      nothing written. Both query kinds must fall to chance.
    UNGROUPED    the identity mapping -- today's model. DIRECT should hold,
                 TRANSFER should be at chance BY CONSTRUCTION, because nothing
                 was ever stated about X.

`UNGROUPED` is the one that makes this worth building: **it is at chance on
TRANSFER for a structural reason, not a tuning one.** Any gap above it is
grouping doing something no amount of learning rate can imitate.

## Predictions, to be registered before anything runs

  P1  THE GATE. `concept` beats `ungrouped` on TRANSFER by more than 0.20
      accuracy. Grouping lets the model answer about a thing it was never told
      about.

  P2  THE CONTROL. `shuffled` does not beat `ungrouped` on TRANSFER by more
      than 0.05. If it does, the gain is having fewer addresses rather than
      having the right ones — which is exactly what happened on text
      (decision 141), so it is the failure most likely to recur.

  P3  THE RAIL. `nostore` is at chance on BOTH query kinds. If it is not,
      something other than the store is answering and no comparison here is
      readable.

**What would refute the whole line:** P1 failing while DIRECT still scores high.
The store would work, the grouping would be discoverable, and joining them would
still buy nothing — which would say the indirection in `concepts.py` does not do
what it was built for, and that is a finding worth having for the cost of one
task.

## What this reuses

Everything. `grouping.cluster` builds the families from `ContentIndex` vectors.
`concepts.Shared` expresses them. `keys.ByConcept` addresses the store by them.
`mqar.py`'s generator is the template for stated-once bindings. Nothing new is
needed in the model — which is the point: **this measures what is already built,
against the goal as stated.**

## The honest risk

**The families must be discoverable but not trivial.** If entities of a family
are too alike, `ContentIndex` finds them instantly and the task measures nothing
about representation. If too unlike, no grouping is possible and the task measures
the clusterer. That is a dial, it needs a calibration pass before the real run,
and calibrating it is the first thing that should happen — decision 63's rule.

**And the task is self-designed**, like everything else here. It is built to make
a mechanism visible, so a positive result says the mechanism can work, never that
it works on anything real. CLUTRR remains the unrun external check.
