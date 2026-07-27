# 032 — can each component do the job at all?

**Status:** measured. Ten tests, all passing, in
`tests/test_component_capability.py`.
**Asked for by John**, whose concern was precise: *"I would hate to end up
scrapping an idea based on 'it's just not working', but then find out later there
was a bug in that implementation and the idea itself was valid."*

---

## IN PLAIN TERMS

Until now the tests here have asked whether each part behaves as it was
specified. That is not the same as asking whether it is *capable* of the job the
whole thing needs from it.

The difference matters when a result is weak. A weak number invites "this
approach does not work", when the truth might be that one part is broken and the
approach was never really tried.

So each part is now handed perfect inputs and asked whether it can do its job at
all. All five can. **That means the weak language results are not hiding a
broken component**, which is worth more than it sounds: it makes the negative
findings trustworthy rather than suspect.

---

## What a model of this shape needs, and whether it has it

    keys        distinct tokens distinguishable          PASS
    store       a written binding comes back             PASS
    readout     learns a mapping from CLEAN input        PASS, >0.95
    learning    the delta rule descends                  PASS, monotone
    end to end  a fully determined cycle is learned      PASS, >0.90

Each is a **floor, not a grade.** Passing says the component is not the reason
for a weak result. It does not say it is good.

## The one that answers a live question

**The readout, given clean inputs, learns the mapping to better than 95%.**

It is handed the exact value vectors instead of retrievals from the store, and
asked which token each one is. It solves that outright — as it should, since the
values are near-orthogonal and the task is linear.

**So the readout is not underpowered and not broken.** The poor text numbers are
not a readout failure; the retrievals reaching it are not clean. That moves the
fault upstream to interference in the store, which is where g10-10 and g10-11
already put the capacity wall.

[Note 031](031-a-design-pass-against-the-goals.md) listed the readout as item (3)
on the grounds that no experiment had ever varied it. Two things have now varied
it — g11-02's bias, and this — and **both say it is not the constraint.**

## What this does not cover

**The gate is not here.** The tag, the window and the capture pool are mechanisms
with their own tests, and "can it do its job at all" is harder to state for them
because their job is a tradeoff rather than a capability.

**Nor is the distributed path**, whose `Node.step` is a reimplementation of the
inner loop and carries no mutation
([the audit](../../tools/mutation_coverage.py) reports 4 of 14 functions there).
A capability test for it would be worth having and is on BACKLOG.

**And passing is not evidence of quality.** Eight bindings in a width-64 store is
comfortably inside capacity by design, so `test_several_bindings_all_come_back`
proves the store binds, not that it binds well. g10-10 measured how well.

## Why the guards matter more than the assertions

Three of the ten are vacuity guards rather than capability checks:

- the readout must be **bad at zero epochs**, or the test measures the geometry
  of the value vectors rather than any learning
- an **unwritten cue must not retrieve confidently**, or a store returning one
  token for everything would pass the binding tests whenever that token was right
- **more training must beat less**, or the result is luck

Each exists because the corresponding test would otherwise pass on a broken
model, which is the failure this whole file is written against.
