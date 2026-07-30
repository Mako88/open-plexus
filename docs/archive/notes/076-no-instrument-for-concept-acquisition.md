076 — There is no instrument for concept acquisition, and CLUTRR cannot become one
=================================================================================

**Status:** measured, and it is a finding about the INSTRUMENT rather than about the idea.
`concepts.Merged` is built and unaffected; what this examines is the evidence that would
drive a merge, and it establishes that nothing available can test it.

---

## IN PLAIN TERMS

`Merged` lets two concepts become one without moving any address. **What it does not have is
a reason to.** The proposed reason was interchangeability: two surfaces are the same concept
if they relate the same way to the same things — note 070's mechanism pointed at entities
instead of relations.

Testing it needs examples of one concept wearing two surfaces. **CLUTRR contains none**, and
the obvious way to manufacture them turns out to build precisely the case that cannot work.

---

## First: CLUTRR has zero natural merge candidates

`_entity_ids` assigns one slot per distinct graph node, so within a puzzle the mapping is
injective — no two ids ever name one person. Across puzzles ids are reused for *different*
people, so id identity carries no meaning either.

**This was named as the falsifier before the measurement and it fired**, which is the
cheapest way this could have gone.

## Second: the derived instrument builds the pathological case

Split a degree-≥4 entity into two aliases holding half its edges each. Ground truth known:
the aliases are one concept. 453 such entities exist, in 375 puzzles — a workable
population.

    375 puzzles with a splittable entity

    alias found as nearest neighbour     0.163      chance 0.141
    mean cosine(alias, its own half)     0.184
    pairs scoring EXACTLY zero           249/375 = 0.664

**Two-thirds share no feature at all, and that is a property of the construction.**
Splitting edges between two aliases guarantees their profiles are disjoint: every feature
one alias has, the other lacks, because the edge went to one of them.

> **So this measures a case built to fail.** Real synonyms appear in *overlapping* contexts
> — that is why their distributions converge — and the split forbids overlap by
> construction. Reporting 0.163-against-0.141 as evidence against interchangeability would
> be reporting an artifact of the fixture as a property of the mechanism.

## What it does establish, and it is the useful part

**Co-reference evidence needs a DISTRIBUTION per surface, which needs each surface observed
many times.** Two surfaces of one concept appear in different contexts — that is what makes
them two surfaces — and their profiles only converge once there are enough observations for
the *distributions* to be comparable rather than the individual features.

    CLUTRR gives each entity 1 or 2 edges         degree 1: 28.3%, degree 2: 64.4%
    a split alias holds 2                          profiles of ~2 features
    two profiles of 2 disjoint features            cosine 0 by arithmetic

**So the requirement on an instrument is: surfaces that recur, in contexts that overlap.**
Nothing in this project's task set has that. Every task hands each entity a couple of facts
and moves on.

## What this leaves

    the MECHANISM      `concepts.Merged`, built, 19 tests, 2 mutations. Unaffected
                       by anything here -- it expresses a merge and never decides one

    the EVIDENCE       untested, and untestable with what exists. Interchangeability
                       is neither supported nor refuted

    the INSTRUMENT     the blocking gap, and it is a task rather than a mechanism:
                       repeated surfaces in overlapping contexts, with known
                       ground truth about which are the same concept

**The instrument is now the work.** Candidates, unequal: a synthetic graph where entities
recur under two aliases across many facts (cheap, and its realism is exactly what would be
in question); a coreference dataset (real, and the surfaces are pronouns, which may be a
different problem); or a synonym-annotated corpus (real, and the contexts are text, which
`g17-01` found unlearnable at word level).

## What is NOT claimed

**Not that interchangeability is wrong.** It is the standard answer in the literature this
project's steer points at, and note 070 measured it working for relations, which do recur.
It is untested for entities here because entities do not recur.

**Not that `Merged` needs the evidence to be useful.** It fixes the addressing problem a
merge creates, and that problem exists however the decision is reached — including from a
decision handed in by a person, which is what `Shared` already expresses.

**And not that a synthetic instrument would settle it.** A graph built so that aliases share
context would demonstrate the arithmetic works and say nothing about whether real
co-reference looks like that, which is the same objection this note raises against its own
split fixture.
