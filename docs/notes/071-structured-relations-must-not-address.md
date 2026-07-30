071 — Structured relations must not be part of the address
=========================================================

**Status:** measured, five seeds, five loads. **It refutes note 067's reasoning** — not its
result — and it stops a build before it was started, which is what John asked for when he
said to nip unscalable decisions in the bud.

---

## IN PLAIN TERMS

Note 070 measured that describing a relation by what it connects, rather than hashing its
id, roughly doubles the ability to name a composition never seen. The obvious next move was
to put those descriptions into the addresses the store uses.

**That obvious next move is wrong, and the reason is measurable.**

Making relation keys similar does *not* stop the store reading facts back — that part works
fine. What it breaks is the store's ability to say **"I was never told that."** Ask about a
fact that was never written and the store hands back one of the *other* facts about the same
person, roughly 60% of the time, at every scale tested.

**So the structure has to live where composition happens, not where addressing happens** —
which is exactly where note 070 measured it, so nothing about that result is lost.

---

## The measurement

    key(e, r), width 512, 3 facts per entity, 5 seeds

    hashed      key = hash(seed, e, r)                 what keys.py does today
    structured  key = hash(e) (*) profile(r)           (*) circular convolution

    entities  facts   READ hashed  READ struct   FALSE HIT hashed  struct  chance
           8     24         1.000        0.992              0.400   0.775   0.375
          16     48         1.000        1.000              0.362   0.775   0.188
          32     96         1.000        0.996              0.169   0.750   0.094
          64    192         1.000        0.999              0.081   0.669   0.047
         128    384         1.000        0.998              0.036   0.592   0.023

A **false hit** is: read an address never written for this entity, and the nearest value is
one this entity *does* have.

**GATE passed** — hashed reads 1.000 at every load, so the harness is sound.

**RAIL passed, and it is the good news.** Structured keys read 0.992–1.000. **Interference
does not destroy the store**, which was the first-order worry and it is unfounded.

**FALSIFIER FIRED.** Hashed false hits track chance and **decay with scale** (0.036 against
0.023 at 128 entities). Structured false hits are **flat at ~0.6 and do not decay** —
0.55 to 0.59 above chance. That is not noise and it is not a load effect.

## Why, and it is a smaller population than note 067 reasoned about

`hash(e) ⊛ profile(r)` holds the entity factor constant, so **every address for one entity
lies in a shared subspace**, and within it a similar relation lands near a written one.

> Note 067 argued the interference concern does not transfer from entities to relations:
> *"twenty relations in a 512-wide space have room to be structured without meaningful
> interference."* **The count was the wrong count.** The interference is not global across
> twenty relations — it is local to **one entity's handful of facts**, where three
> addresses in a shared subspace is dense, not sparse. The reasoning was about the wrong
> population and this is the measurement that says so.

**Component 2's refusal of content-derived keys therefore holds for relations too**, by a
different mechanism than it holds for entities. The split note 067 proposed is real as a
statement about *representations* and wrong as a statement about *addresses*.

## What survives, and it is the whole of note 070

**Note 070 never used the store.** It fit a readout from `concat(v(a), v(b),
convolve(v(a), v(b)))` to the answer relation — the composition path, not the addressing
path. So the +0.099 stands untouched, and this note narrows where it may be applied rather
than reducing it.

    structure in the ADDRESS      refuted here
    structure in COMPOSITION      note 070, +0.099, t = 11.6
    structure in the VALUE        untried
    exact address + separate
      structured channel          untried, and it is what the gate already
                                  does -- AddressSketch is a mechanism apart
                                  from the store read

## What is NOT claimed

**Not that the gate was tested.** Decision 148's 1.0000/0.0000 comes from
`AddressSketch` with `index_prefer="inherit"`, and this measures the raw `memory @ key`
read, whose own docstring calls it *"the one under suspicion."* **The gate may well recover
what the raw read loses, and that is untested.** What is established is that the raw read
degrades in a way it does not with hashed keys, which is enough to refuse the wiring
without further work.

**Not a claim about three facts per entity being the right load.** Real use holds more, and
a denser subspace should make this worse rather than better — but that direction is
reasoned, not measured.

**And the false-hit number needed its control to mean anything.** Structured's 0.592 read
as alarming on its own and is only interpretable against hashed's 0.036 and chance's 0.023.
The first run of this script computed it for structured only, which would have produced a
confident claim from a number with no baseline — the third time in one session that a
missing control was the difference between a finding and a mistake.
