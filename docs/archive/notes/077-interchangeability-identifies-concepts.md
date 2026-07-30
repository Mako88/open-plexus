077 — Interchangeability identifies concepts, and the signal scales with evidence
===============================================================================

**Status:** measured on a standard benchmark, zero supervision, `tools/openea_alignment.py`
committed. **It answers what note 076 left open** and confirms note 076's central claim by
measurement rather than argument.

---

## IN PLAIN TERMS

`concepts.Merged` can express that two concepts are one thing without moving any address.
**What it lacked was a reason to.** The proposed reason: two surfaces are the same concept if
they relate the same way to the same things.

Note 076 could not test it — CLUTRR gives each entity one or two facts, so two surfaces of
one concept share nothing and score zero by arithmetic. It stated the requirement instead:
**surfaces that recur, in contexts that overlap.**

OpenEA meets it. On two DBpedia graphs with the entity names encoded so nothing can be
matched by string, and with **no examples given**, relating-the-same-way picks the correct
partner out of fifteen thousand candidates as its FIRST guess 3.9% of the time — **583 times
better than chance.**

**And the more often a thing is mentioned, the better it works, in every bucket** — from
0.2% at one mention to 15% at sixteen. Which is the requirement note 076 named, now with a
curve attached.

---

## The measurement

`EN_DE_15K_V2`, 15,000 gold links, **zero seed alignments** where the standard setting gives
3,000. Profile each entity as a bag of `(shared relation, direction)` counts; cosine
similarity; rank the true partner.

    hits@1   0.0389    583 of 15,000
    hits@10  0.1565
    MRR      0.0787
    chance   0.000067  --  a 583x lift

    by shared-vocabulary edges on the WEAKER side of each pair

    edges        n     hits@1   hits@10
        0      609     0.0000    0.0000
        1    2,485     0.0024    0.0093
      2-3    4,481     0.0152    0.0770
      4-7    5,268     0.0537    0.2371
     8-15    1,611     0.0894    0.3035
      16+      546     0.1502    0.4414

**Monotone in every bucket. 60x from one edge to sixteen.**

## Why this instrument, chosen on two measurements rather than preference

    relation vocabulary shared between the two graphs

    EN_DE_15K_V2    74.0%      both sides are DBpedia, different languages
    EN_FR_15K_V2    60.8%
    D_W_15K_V2       0.0%      DBpedia against Wikidata
    D_Y_15K_V2       0.0%      DBpedia against YAGO

A shared vocabulary is what puts both graphs' profiles in one feature space. **`D_W` and
`D_Y` share nothing, so cosine similarity there is meaningless** — the tool refuses those
datasets by name rather than returning a number about nothing. They are the harder setting
and need an alignment bootstrapped from something vocabulary-free first.

**And v2.0 encodes the URIs** (`E823797`, not a readable label) as the authors' name-bias
fix. So string matching cannot contribute and relational structure is the whole signal, which
is precisely the claim under test.

## It explains CLUTRR's failure exactly

Note 076 reported 0.163 against chance 0.141 on a split-CLUTRR fixture and called the
instrument at fault. **This says why, quantitatively.** CLUTRR entities carry one or two
edges, and at one edge this measures hits@1 = 0.0024, at 2–3 edges 0.0152.

**The fixture was not weak. It sat in the region where this signal does not exist.**

## What this does NOT establish

**Not a competitive alignment number.** Published methods on these datasets use 3,000 seed
alignments, attribute triples, and neighbour structure, and reach far higher. This uses none
of them on purpose: the question was whether first-order relational structure carries the
signal at all, and 583x says yes while 0.0389 says it is not sufficient alone.

**Not that the profile is the right one.** It is the crudest available — a bag of
`(relation, direction)`, no neighbour information, no iteration. Bootstrapping (align the
confident pairs, then use aligned neighbours as features) is the standard next step and is
untried here.

**And not that this is the acquisition mechanism.** It is evidence a merge could be decided
from. Deciding *when* the evidence is enough — a threshold, and what it costs to be wrong,
given that a wrong merge makes two things one forever — is untouched. `Merged` being
append-only means a wrong merge cannot be undone by forgetting it, which makes that threshold
the load-bearing question this note does not answer.
