062 — Traversal recovers CLUTRR's chains; the search on top of it does not pay
=============================================================================

**Status:** measured, one seed, and **the first external-benchmark result this project
has.** Path-finding only — the readout step is deliberately excluded, so this is not an
end-task number and must not be quoted as one.

---

## IN PLAIN TERMS

CLUTRR gives a chain of family facts and asks how the two people at the ends are
related. Working that out has two parts: **find the route between them**, then **name
what the route adds up to.**

This measures the first part only. The route is found correctly **1.000 of the time at
two and three steps, and 0.36 of the time at ten** — so the traversal works, and it
degrades with length in a way that says it is genuinely walking rather than guessing.

Two things it also settled, both by controls rather than by argument. **The clever part
turned out to be unnecessary:** the mechanism can consider several possible routes and
pick the best, and doing so is worth almost nothing here — one route, chosen by the
obvious first step, does just as well. And **the case where a person appears in several
facts is six times worse** than the case where they do not, which is the confound
predicted two notes ago, now measured.

---

## Chain recovery, kinship layout, one seed

    hops     n   recovered
       2    38       1.000
       3   105       1.000
       4   190       0.963
       5   174       0.764
       6   107       0.626
       7   144       0.465
       8   150       0.460
       9   119       0.420
      10   119       0.361
    overall 1146     0.659

**Chance is `20^-h`** — 0.0025 at two hops, about `1e-13` at ten — so this is far above
it. **It is NOT comparable to note 060's 0.0365 floor**, which is end-task accuracy
(naming the relation) rather than path recovery. Conflating the two would overstate
this by an order of magnitude, and the temptation to do so was real.

**The decay is monotone**, which was P2 and is the evidence that the walk follows the
store rather than something else producing the answer.

## Control 1: the endpoint scoring does select

    best-scored walk   0.659
    random walk        0.149

A 4.4x gap, so scoring is not inert. That was worth checking before believing anything
else.

## Control 2: and yet beam width buys nothing

    branches       1        2        4        8
    overall    0.650    0.660    0.659    0.659

**+0.009 for eight times the walks.** Committing to the top decode and walking once is
as good as searching.

**The reason is structural and it is `search.py`'s own argument, inverted.** That module
exists because *"a `(subject, relation)` pair names one person 94.9% of the time;
`(FACT, subject)` names one of several relations about half the time."* On `kinship.py`
an entity holds several relations, so the first read is ambiguous and searching pays.
**On CLUTRR the story IS a chain**, so each entity has one outgoing story edge — and
measured, the top decode of the first relation is **0.974.** There is nothing to
disambiguate.

> So decision 130 transfers intact: **search helps only where ambiguity is.** Its
> condition is largely absent here, and the honest reading is that **the traversal is
> what pays and the search on top of it does not.** That is a refutation of the
> expensive half, arrived at by measuring the cheap half against it.

## Control 3: the collision confound, quantified

    clean rows      745/1058   0.704
    collided rows     10/88    0.114

**Six times worse**, and this was P3. Note 059 predicted it from the addressing
measurements (103: 0.884 → 0.303 on a repeated entity) without having run anything.
A walk through a collided address reaches the wrong entity, and the endpoint check then
rejects a route that was structurally correct.

**88 rows is what the kinship layout left behind** after cutting collisions from 411.
So the layout choice was worth 323 rows, and the residue costs 0.59 of recovery on the
rows it did not fix.

## Where the failure actually is

The first relation is recovered at **0.974** and the whole chain at **0.659**, so the
loss is not in starting the walk — it is in the alternation that continues it:
`key(entity, relation) -> next entity`, then `key(FACT, next) -> its relation`. Each
step is nearly right and the errors compound with depth.

**Capacity is not the cause.** Decision 109 puts the store at ~d² bindings; at d=256
that is thousands against the ten facts a ten-hop puzzle writes. This is **drift, not
saturation**, which is a different problem with different fixes.

## What is NOT measured

**End-task accuracy.** Naming the composed relation needs the readout trained on walk
outputs, and that has not run. A recovered chain is a route, not an answer — six of
CLUTRR's target relations never appear as an edge, so naming is a real second step and
not a lookup.

**One seed**, so rule 3 applies: every figure here is a bound to reproduce. The 2- and
3-hop cells at 1.000 are 38 and 105 rows and note 060's correction already withdrew
that region as a gate.

**And nothing is wired into `run`.** This calls `search.py` against the store the model
ended a sequence with, which is how decision 123 proved it standalone. Wiring it in
remains its own change.
