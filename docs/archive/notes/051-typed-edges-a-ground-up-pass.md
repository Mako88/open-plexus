# 051 — Typed edges: a ground-up pass, and most of it is already built

**Status:** a design, nothing built. No code changed and none is proposed yet.

**Why it exists:** John asked whether the project is in a tweak-and-retry loop and
whether a first-principles pass would reveal better alternatives. Then, reading
the diagnosis, he proposed the mechanism this note is built around:

> *"a three phase step … here is the general concept, then what is the
> relationship I'm looking for, and then follow that relationship to the specific
> thing."*

**That is a typed edge, and this project has had half of one since kinship.** The
note exists to say what the other half is, what it costs, and what would refute
it.

---

## 1. The constraint set

A design pass with no measurements behind it is the predecessor's mistake, which
GOALS §4 puts first for that reason. These are the things a proposal has to
survive. Each is a measured number, and each is a way to be wrong.

    the store is necessary and sufficient for single-binding recall
      MQAR 0.995 with it, 0.000 without. Nothing else in the model does that

    on a next-token objective the store can only express an n-gram
      note 047. A counting table does that exactly and more cheaply, so the
      OBJECTIVE was the ceiling, not the memory

    one address cannot hold a default and an override
      grouping answers transfer 0.471 and destroys exceptions 0.371; plain
      addressing holds exceptions 0.783 and is at chance on transfer 0.061

    adding two retrievals sets an exchange rate, it does not choose
      transfer + exception pinned at ~0.93 across every weighting (146)

    a gate can choose, exactly, when the question is membership
      inherit 0.810 / 0.435 / 0.818, deferring on 1.0000 of transfer and
      0.0000 of direct and exception (148)

    but occupancy is a property of the ADDRESS, not of the knowledge
      it is informative exactly where an address is READ BEFORE IT IS WRITTEN
      (151, 153). On kinship, chains and MQAR it says nothing

    more distinct addresses cost capacity
      interference is O(N * rho) in mean key cosine (note 035). `PairKeys`
      already pays this: 469 distinct keys against 66 on the same text

    splitting the store by concept does not change pooled capacity
      identical pooled, 16x lone-node at 16 nodes (134)

## 2. The diagnosis

Four obstacles, and they are one obstacle.

    grouping erases exceptions              144
    summing can only average                146
    occupancy says WRITTEN, never WITH-WHAT 153
    a link overwrites the fact it composes  155

**The model has exactly one kind of relation** — a key→value binding in one
superposed store — **and it is being asked to carry several**: *has-value*,
*is-a*, *links-to*, *overrides*. They collapse into one address space, and every
mechanism built since is a way to disambiguate something that should not have
been collapsed.

Decision 155 is the cleanest instance because it is a hard failure rather than a
soft one. `LINK here there` and `FACT here value` both write `key(here)`. One
address, two kinds of edge, superposed. Every column fell to chance.

## 3. The design

**Put the relation in the address.**

    key(subject, relation) -> object

Then `key(here, LINK)` and `key(here, FACT)` are different addresses and **cannot
collide**. Decision 155's failure is not fixed by a better layout; it is
structurally impossible.

John's three phases are the read path:

    1  which concept     the content index, by similarity
    2  which relation    the type, bound into the key
    3  the exact read    key(concept, relation) -- and the gate asks if it is
                         empty

**And the gate gets sharper rather than blunter, which is the part that surprised
me.** Decision 151's bound was that occupancy tells you an address is empty, not
that you do not know something, and that the two coincide only where addresses
are per-fact. A typed address is much closer to per-fact: `key(entity,
has-value)` reading empty is the precise statement *"I do not know this entity's
value"*, where `key(entity)` reading empty only ever meant *"nothing at all has
been written about this entity"*. **Typing attacks the bound directly.**

## 4. What already exists, which is more than I expected

This is the reason the note is a design and not a rebuild.

    PairKeys              derives an address from the pair (t-1, t) by a fixed
                          hash. Kinship stores key(S, R) -> O with it. THAT IS
                          ALREADY A TYPED EDGE, and decision 100 measured the
                          cost of keying it wrong: 0.020 against 0.713
    hop_accumulate="bind" an elementwise binding operator, built and tested,
                          used only to accumulate hops
    the hop's decode      lands at cosine 0.96 on a single token's row (154), so
                          "which concept did I arrive at" is answerable
    ContentIndex          phase 1, fitted and measured (g19-00 purity 1.000)
    AddressSketch         the phase-3 emptiness test, exact (148)

**What is missing is one thing: the hop is untyped.** It decodes to a concept and
reads `key(concept)` — never `key(concept, relation)`. So the model can follow
*an* edge and cannot follow *the has-a* edge. Phase 2 exists in storage and has
no counterpart in retrieval.

That asymmetry is the whole gap, and it is smaller than "a new architecture".

## 5. What it costs, before anyone is enthusiastic

**Capacity, and this is the real one.** Typing multiplies distinct addresses by
the number of relation types. Note 035's interference is `O(N * rho)`, and
`PairKeys` already documents the trade — 469 distinct keys against 66 on the same
text. **A model that can express four relation types may hold a quarter as many
facts.** That is measurable and must be measured before anything is built on it.

**Locality (C1) is unaffected**, which is the one piece of good news. A typed key
is formed from two token ids the node already has. No population statistic, no
barrier, no second node. `derived_keys` rests on the same argument.

**Where the relation comes from at read time is unsolved.** In kinship the
question *states* the relation, so phase 2 is free. In an open query it is not,
and "which relation am I looking for" becomes a decision the model has to make —
possibly a learned one, which is the thing decision 147 warned costs more than it
looks.

**And it does not fix the objective.** Note 047's ceiling on text is a property
of next-token prediction, not of the store. Typed edges change what can be
represented, not what is being scored.

## 6. The case for keeping what exists

Stated properly, because a design pass that only argues for change is an
advertisement.

The store, the identity-addressing, the separate content index, and the gate are
**not** what is failing. The split between *identity addresses* (exact, no
interference) and a *separate similarity index* (which proposes which exact reads
to make) is note 045's design, and today's results support it rather than
undermine it: the gate works precisely because addresses are exact, and note 035
says content-derived addresses would spend the capacity that is already the wall.

**Typed edges are an addition to that design, not a replacement for it.** Nothing
in section 3 asks for the store to change shape.

## 7. Predictions, to register before building

  A1  THE COLLISION. With typed keys, decision 155's task runs without the
      collapse: DIRECT, TRANSFER and EXCEPTION stay within 0.05 of their
      link-free values where they fell to chance before. This is the cheapest
      test of the whole idea and it uses a task that already exists.

  A2  THE GATE GETS SHARPER. On kinship, `deferred_on_*` separates one-hop from
      two-hop queries by more than 0.5, where decision 151 measured 0.0000 at
      both. If typing does not move that, occupancy is not about addressing at
      all and section 3's main claim is wrong.

  A3  THE CAPACITY COST IS REAL AND BOUNDED. MQAR accuracy with `r` relation
      types falls by less than the `1/r` a naive reading of note 035 predicts,
      because the types are not equally used. **If it falls by `1/r` or worse,
      typing buys expressiveness at a price the C1 budget cannot pay**, and the
      honest response is to say so rather than to tune around it.

  A4  THE FALSIFIER. If A1 holds and A2 fails, typed edges fix collisions and
      nothing else — a bug fix wearing an architecture's clothes. That is still
      worth having, and it is not worth the name.

## 8. What to build first, and it is small

**A3 before A1.** The capacity question decides whether any of this is
affordable, it needs no new task, and it is a sweep over an existing MQAR script
with `context_keys` on and a varying number of synthetic relation types. If
typing costs what a naive reading of note 035 says, nothing else matters.

Then A1, which reuses decision 155's task and asks only whether the collision
disappears.

**Neither needs the hop to be typed.** The retrieval-side change — phase 2 in the
read path — comes after both, and only if they hold.

---

**Not built. Not started.** The organising idea is John's; the measurements it has
to survive are in section 1, and section 5 is the reason to expect it to cost
something.
