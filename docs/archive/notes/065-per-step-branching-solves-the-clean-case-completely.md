065 — Per-step branching solves the clean case completely
=========================================================

**Status:** measured, three seeds, and it is the largest single mechanism gain in this
project's record. **It also corrects note 063.** Still chain recovery, not end-task
accuracy.

---

## IN PLAIN TERMS

Note 064 found that the route-following mechanism hedged only on its first step, which
was the step that needed it least. Making it hedge at every step was one change.

**On every puzzle where no person appears in more than two facts, it now finds the
route correctly every single time** — 713 of 713, on three separate runs. The
remaining mistakes are all in the puzzles where someone *does* appear repeatedly, and
that is a limitation this project measured two hundred decisions ago and knows the
shape of.

So the route-finding half of CLUTRR is, on the clean cases, done. What is left is
naming what the route adds up to — which was the *other* half all along, and which note
063 had ranked second.

---

## The measurement

    three seeds, chain recovery on all 1,146 test puzzles

    seed        search(b=4)     beam(w=4,b=4)      gain
       0             0.6588            0.8735    +0.2147
       1             0.6632            0.8831    +0.2199
       2             0.6623            0.8848    +0.2225
    mean             0.6614            0.8805    +0.2190
    spread           0.0044            0.0113

**+0.219 against a seed spread of 0.011.** Twenty times the noise, and the sign is the
same on every seed.

    by note 059's split, all three seeds

    plain rows (no entity in >2 edges)     713/713      1.000
    collided rows                       288-301/433    ~0.68

**Every clean chain, every seed.** That is the result: the mechanism is not merely
better, it is *exhausted* on the subset where the addressing is sound.

## What it confirms, and what it corrects

**Note 064's diagnosis is confirmed exactly.** It predicted the gain would be
concentrated at depth, because that is where the ambiguity was — and the per-hop
numbers do that: nothing at 2–3 hops (already 1.000), +0.21 at five, +0.34 at eight and
nine. It also named the refutation condition — *fails to beat 0.659 by more than seed
spread* — and that condition is not met by a wide margin.

**Note 063 is corrected.** It concluded *"the ceiling is set by route-finding, not
naming, so the next measurement belongs on drift rather than on the readout."* That was
right when route-finding was 0.659. **It is now wrong**: on the plain subset the route
is recovered 1.000, so naming is the only thing left there, and the readout is where
the next work belongs after all.

> The correction is not that 063 reasoned badly — its arithmetic was right on the
> numbers available. It is that **a bottleneck claim expires the moment the bottleneck
> moves**, and this one moved within the hour. Worth recording as an instance of why
> forward-looking claims in this project keep needing withdrawal.

## The residue is a known limitation, not a mystery

The 433 collided rows sit at ~0.68. Those are the puzzles where an entity appears in
three or more edges, so `key(FACT, e)` and `key(e, r)` hold superpositions. Decision
103 measured single-token keys at 0.884 for one appearance and **0.303** for two;
decision 104 measured pair keys at 0.628; `docs/SCALE.md` records that pair keys
separate an entity's *roles* but collide again on a repeat in the same role.

**0.68 on the collided subset is consistent with that line**, and the kinship layout
already cut the colliding population from 411 rows to 88 by the same mechanism
(decision 157). What remains is the residue that layout choice could not reach.

## The cost, stated plainly

`width * branches * depth` reads against `search`'s `branches * depth` — **four times
the reads** at width 4, branches 4. Unpruned it would be `branches ** depth`, a million
walks at ten hops, so the pruning is what makes it affordable rather than an
optimisation.

Whether four times the traffic is acceptable is a **G4 bandwidth question** and is not
answered here. Decision 123 recorded beam 4 costing 3.2x the traffic on kinship, so
this is the same order and the same open question.

## What is NOT claimed

**Not an end-task number.** A recovered route is not an answer. Naming the composed
relation needs the fold note 063 specifies, trained on unlabelled intermediate steps.

**Not wired into `run`.** `beam` is called against the store the model ended a sequence
with, exactly as `walk_from` was, and `search` is untouched — it is the measured
comparison and rule 14c keeps it.

**And the plain-subset 1.000 is on a task whose facts are stated and then queried.**
Nothing here says a route can be found when the store also holds unrelated material,
which every real use would.
