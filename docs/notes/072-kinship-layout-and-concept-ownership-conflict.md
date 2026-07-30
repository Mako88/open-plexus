072 — Kinship layout and concept ownership conflict, and both were chosen alone
==============================================================================

**Status:** measured on the CLUTRR test split via the real token streams. **It is a
conflict between two options the tree lists as chosen**, each decided on its own merits,
and it is the kind of thing John asked for when he asked which existing decisions will not
scale.

---

## IN PLAIN TERMS

Decision 157 chose the `kinship` token ordering because it collides far less — 7.7% of test
rows against 35.9%. That was the right call on addressing.

Separately, when the network is split by concept, each stored fact is put on the node that
owns **the token the fact is filed under**.

**Put those two together and every fact lands on a node named after a RELATIONSHIP TYPE
rather than a person.** There are only twenty relationship types and there always will be,
so the network can never use more than twenty machines for the facts it traverses — and
`sister` alone would hold a fifth of them.

**That is worse than the sixteen-node ceiling concept splitting was chosen to fix.**

---

## The measurement

`local_memory` writes with `memory = concepts.matrix(previous_concept)`, and
`PairKeys.concept` returns `tokens[t]`, so a binding `key(t-1) → value(t)` is owned by
`tokens[t-1]`. Per four-token fact block:

    kinship  FACT s r o    key(FACT,s)->r owner s     key(s,r)->o owner r   <-- RELATION
    closure  FACT s o r    key(FACT,s)->o owner s     key(s,o)->r owner o

`walk_from` and `beam` read `key(entity, relation) → next entity`, which is kinship's
second binding.

    CLUTRR test, 7,132 traversal bindings

    layout     relation-owned   busiest owner        owners covering 90%
    closure             0.0%    entity, 18.7%                          7
    kinship           100.0%    `sister`, 20.2%                        8

**100.0%, not a tendency.** Every traversal binding under `kinship` is owned by a relation.

> **The owner counts are NOT the finding and reading them as one would be wrong.** CLUTRR
> renumbers entities per puzzle by first appearance with `max_entities=11`, so closure's
> "11 distinct owners" is an artifact of that renumbering, not a distribution. **The
> finding is the CATEGORY of the owner**: closure routes by entity, which grows with the
> corpus, and kinship routes by relation, which is capped at twenty forever.

## Why current-token routing was chosen, and why it is right for closure only

`PairKeys.concept`'s own docstring argues for the current token over the pair: *"Every
`key(FACT, X)` lands on X's node, so one node holds an entity and everything said about
it."* That reasoning is sound, and under `closure` it delivers — both bindings land on
entities.

**Under `kinship` the same rule delivers the opposite**, because the ordering puts the
relation where closure puts the object. Neither decision is wrong in isolation; the pair is.

## The options, none of which is free

    route by the PAIR         one line, and the docstring already says it is
                              unbuilt and awaiting a measurement that wants it.
                              THIS IS THAT MEASUREMENT. Cost: an entity's facts
                              scatter across every node, which is what decision
                              134's case was against

    route by the FIRST        key(s,r) would land on s. But key(FACT,s) would land
    pair element              on FACT -- one special token owning everything, so it
                              trades one imbalance for a worse one

    route by whichever        principled, and the rule is "own the side the network
    element is NUMEROUS       has many of". Needs the key source to know vocabulary
                              cardinalities, which it can. Untried

    use `closure` for         cannot have both: 157's 4.7x collision reduction and
    distribution              entity ownership are the same choice made two ways

## What this does NOT establish

**Not that concept partitioning is refused.** It is not enabled — `concept_nodes` is 0 —
so nothing measured to date runs through this path. What is established is that turning it
on under `kinship` would produce a twenty-node ceiling, and that the argument for concept
splitting rests on a per-node capability that GROWS.

**Not measured on a real corpus.** CLUTRR has twenty relations by construction. A domain
with thousands of relation types would not have this problem at all, which is worth saying
because it means the defect is in the *interaction* and not in either option.

**And the balance numbers are one split of one benchmark.** `Ring.balance` is the
instrument that would answer this properly and it has not been pointed at either layout.
