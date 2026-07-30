061 — How search answers CLUTRR, checked against the code rather than assumed
=============================================================================

**Status:** a design, verified against `openplexus/search.py` line by line and **not
built**. Nothing here is a measurement.

**Why it exists:** the live question was *where does the relation come from at each hop
on CLUTRR*, and the tree's candidates were all refused —
`hop_relation` is one per model, `hop_relations` supplies a schedule the task does not,
try-all-and-gate is refuted at CLUTRR's relation density, and a learned chooser is what
decision 147 says not to attempt yet. `search.py` was the remaining candidate and it
was **labelled scaffolding not yet wired into `run`**, deliberately, so it would not
become load-bearing before a task needed one. A task now needs one.

---

## IN PLAIN TERMS

CLUTRR gives a chain of family facts and asks how two people at the ends are related.
The model has to work out the route between them and then name what that route adds up
to.

The route-finding already exists and has been measured — it is the search built for the
kinship task. What was not obvious is that it fits here at all, because kinship's search
answers *"who?"* and CLUTRR asks *"what relationship?"*. Checking the code rather than
assuming settled it: the search hands back the route **and** the raw material a readout
needs to name it, so both halves are available.

Two details decide whether it is honest, and both check out. The model is not told how
long the route is — it can count the facts. And it is not told which relation to try at
each step — it proposes them from what it read.

---

## The assembly, and every part exists

    1  candidates(start=S, allowed=relation_tokens)   -> branches worth trying
    2  search(start=S, target=wv[T], depth=facts)     -> each branch walked and
                                                         scored by whether it
                                                         reaches T
    3  best walk's `retrieved` vectors                -> the readout names the
                                                         composed relation

`search.py`'s own argument is that **the question naming the object is the
disambiguator** kinship lacked — decision 108 found the store answering *"what relation
does S hold"* correctly while the question needs *"which of S's relations leads to T"*.
**CLUTRR names both endpoints**, so the disambiguator is handed over by the task.

## The two things that make it non-obvious, both verified

**1. `depth` is OBSERVABLE, not fitted.** `walk_from` needs a depth, and a supplied
depth would be exactly the fitted constant decision 162 refuses. But CLUTRR's story
*is* the chain, so the number of stated facts equals the path length — countable from
the token stream at read time. Including for the 433 walks that revisit a node, where
the path length is still the edge count. **Nothing is handed in that the input does not
contain.**

**2. The readout has something to consume.** Kinship's target was an entity, so the
walk's *endpoint* was the answer. CLUTRR's target is a relation, so the endpoint is
merely the check. `Walk.retrieved` is documented as *"the retrieved VALUE vector at each
relation step, which is what a readout consumes"* and is parallel to `Walk.relations` —
so the composed relation is predictable from the walk, using `hop_accumulate="concat"`
and the readout that already exists.

**And `allowed` is what keeps step 1 cheap.** `_top` takes an optional mask, and its
docstring leaves the choice to the caller because *"whether restricting it is
scaffolding or a real property of a deployed system depends on the task."* Here it is
real: relation tokens occupy a known contiguous range in `clutrr.py`, so restricting
candidates to relations is a property of the vocabulary rather than a hint.

## What this is measured against, and it is not chance

Note 060: the `hops=1` floor on the kinship layout is **0.0365**, below chance,
across three seeds. Report per hop bucket, and **not** the 2- or 3-hop cells — 060's
correction withdrew those as gates, since 38 rows swing 0.00 to 1.00. The headroom is
4–10 hops, where the floor runs 0.00–0.11.

## What is NOT settled

**Whether it works.** Search was measured on kinship at +0.269 for traversal and +0.020
for gating it (125, 130) — on a task this project designed, with a rule table it wrote.
CLUTRR's rules are crowd-authored and its table is deliberately partial.

**The repeated-entity 38%.** The kinship layout cuts colliding rows from 411 to 88, and
88 is not zero. A walk through a collided address is a walk to the wrong entity, and the
endpoint check would then reject a route that was correct.

**Gating.** `decode_margin` exists and g13-03 measured search gaining +0.092 where a
subject holds several relations and losing 0.054 where it holds one, so running it
unconditionally nearly cancels. Whether that gate transfers is its own question and
should not be assumed by bundling it into the first arm.

> **Deliberately stopping here.** The design is verified and the build is not started,
> because four of tonight's errors were caught by reading before asserting and the fifth
> would have been a mechanism built at the end of a long session. The expensive part —
> knowing the parts fit — is done and written down.
