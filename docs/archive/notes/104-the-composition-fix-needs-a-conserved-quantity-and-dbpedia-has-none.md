# 104 — the composition fix needs a conserved quantity, and DBpedia has none

2026-07-30. `tools/invariant_dimension.py`. Scopes note 090's result, which is the
project's headline composition finding, and the scope is narrower than it reads.

## The question, made cheap

Note 090 closed CLUTRR's 52% ceiling by supplying a missing step's **displacement**
rather than its name, recovering twenty generation deltas exactly from loop constraints.
It named its own limit: *"whether an arbitrary relational domain has a conserved quantity
of this kind is unknown, and a domain without one gets nothing here."*

That is answerable **from data alone, with no model and no training.** Each closed loop
gives one homogeneous equation, and the deltas are the null space of the constraint
matrix, so its DIMENSION counts the domain's independent additive invariants. For a
general graph the loops are the fundamental cycles of a spanning forest, which are a
basis of the cycle space — every constraint, without enumerating exponentially many
cycles.

## Measured

    domain                    rels    loops   rank   dim   no-loop
    CLUTRR kinship (CONTROL)    20    9,074     19     1         0
    DBpedia English            169   82,167    167     0         2
    DBpedia German              96   89,885     96     0         0

**Both general knowledge graphs have no additive invariant.** The constraint matrices
have full rank over every relation that closes a cycle, so the only consistent assignment
of displacements is all-zero.

And not an approximate one either, which is the check that matters for real data:

    smallest singular values, relative to the largest
    CLUTRR            1.89e-01  1.47e-01  1.31e-01  1.29e-15
    DBpedia English   2.63e-03  2.54e-03  2.53e-03  2.30e-03
    DBpedia German    3.18e-03  3.14e-03  3.02e-03  2.97e-03

CLUTRR's null direction sits at machine zero with a **fourteen-order gap** to the next
value — an exact invariant, cleanly separated. DBpedia's smallest values cluster at about
`3e-3` with no gap at all, which is what a matrix with no such structure looks like. An
invariant holding with exceptions would show as a small tail. There is none.

## So the composition result is scoped, not overturned

Note 090's numbers stand exactly as measured. What changes is the claim they support:

    NOT   "composition is solved"
    BUT   "composition is solved wherever a conserved quantity exists, and general
           knowledge graphs do not have one"

Kinship has a group-like structure — generations compose additively — and so would
plausibly spatial displacement, time offset, taxonomic depth, and organisational rank.
A general KG mixes `birthPlace`, `capital` and `spouse`, which compose into nothing.

**The next problem is therefore not "make the mechanism more general".** It is finding
invariants per sub-domain: whether some SUBSET of a graph's relations closes consistently
even though the whole does not. That is a different computation — a search for a large
consistent subset rather than a null space over all of them — and it is unbuilt.

## The artifact this nearly reported instead

Read raw, DBpedia English came back at **dimension 2**, and I very nearly wrote up
"two conserved quantities in DBpedia" as the finding. Both came from **two relations
that appear in no cycle at all**: an all-zero column joins the null space for free.
Among the 167 relations that do close a loop, the dimension is 0.

"Not enough loops" is indistinguishable from "structure" unless the instrument separates
them, so `dimension` now excludes unconstrained columns and reports the count beside the
answer. **The artifact was more interesting than the result**, which is the shape of
error this project keeps recording, and the only reason it was caught is that dimension 2
contradicted a prediction registered before the run.

## What is not claimed

That every knowledge graph lacks an invariant — two DBpedia graphs are two data points,
and both come from the same source. That the fundamental-cycle basis captures what a
human would call a loop; it captures the CYCLE SPACE, which is the right object for a
linear constraint but is not the same as semantically meaningful paths. And that
dimension 0 closes the door: it closes the door on ONE global invariant, which is what
`generation_delta.py` looks for.

The control is what makes any of this readable. CLUTRR returns 1 through the same code
path, so a sign error or a broken cycle extractor would have shown there first — note
065's rule, which has now fired five times.
