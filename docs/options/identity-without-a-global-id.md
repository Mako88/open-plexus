# Option record — a concept has NO global id; it is an equivalence class reached by walking

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing of the mechanism. This is a design decision recorded before it is built.
- Every piece it composes from is real: `openplexus/ownership.py` gives deterministic
  ownership of a surface id, `openplexus/partitioned.py` gives the per-owner store,
  `openplexus/search.py` gives the walk, and `concepts.Merged` already treats a concept as
  a read-side gather over aliases rather than as one object.

---

## What was tried, and what came back

### The conflict it dissolves — John, 2026-07-30

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- design pass
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

Two commitments were made deliberately and appeared to contradict:

- **Concept partitioning needs a DETERMINISTIC owner**, hashable, computable without asking
  anyone. That is what makes a read one hop rather than a broadcast, and it is the reason
  the cross-machine sum stops existing rather than merely shrinking.
- **Identity is LEARNED from co-occurrence**, negotiated, revisable, and different between
  nodes with different experience — so it is not stable and cannot be hashed.

Both are recorded in [concept-partitioning.md](concept-partitioning.md) and
[discrete-surface-ids.md](discrete-surface-ids.md) as a conflict with two candidate exits:
split routing from meaning, or converge by gossip.

### The exit taken: the concept was never the address — John, 2026-07-30

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- design pass
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

> **A concept does not get an id. It is the equivalence class that falls out of the
> co-occurrence links, and it is reached by starting at any member and walking.**

What the system needs is a deterministic way to find *where evidence about a thing lives*,
and somewhere for that evidence to accumulate. **Neither requires the concept to be named.**
The things that can carry stable ids already do:

    owner(surface id)   everything ever learned about one percept -- an image code, a word
    owner(time bucket)  that two percepts occurred together, transiently

Nothing addresses "dog". A reader arrives at the word, follows accumulated links, and
reaches the image code — which is the walk `openplexus/search.py` already performs.

**So the quantiser ruling is untouched and identity stays learned.** The negotiated thing
was never the address, and the apparent conflict came from assuming a concept had to be a
single addressable object. `concepts.Merged` is the same idea already in the tree at a
smaller scale: a concept as a read-side gather, where a late merge is a MISS and never a
corruption.

### What it costs, stated rather than discovered

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- design pass, none of this is measured
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

**Reaching a concept is a WALK, not a lookup.** Every cross-modal question costs at least
one extra hop over a single-percept question. The peer-path round-trip this project has
measured is the unit that cost is paid in, and no measurement exists of how many hops a
grounded question needs.

**An equivalence class has no canonical member**, so two readers starting from different
percepts may accumulate different neighbourhoods. That is intended — meaning should be
revisable — but it means *"do two nodes agree about dog"* stops having a yes/no answer and
becomes a question about overlap. The gate ladder's agreement question is stated in terms
of ids and would need restating.

**It moves load onto the surface ids.** A percept that co-occurs with everything — a very
common word — accumulates an enormous neighbour list at one owner. Busiest-peer share is
a quantity this project has already had to fix once
([concept-partitioning.md](concept-partitioning.md) holds the figures); this is a new
pressure on the same quantity and nothing has measured it.

### What would refute it

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- not yet written
            task    proposed: symbol stream with designed co-occurrence structure
            model   n/a
            knobs   none
            scale   n/a

**A concept introduced through one modality, queried through another, with a distractor
present on every occasion.** If the walk reaches the distractor as readily as the target,
the equivalence class is not a concept — it is everything that was ever nearby, and
co-occurrence plus local counting is insufficient without intervention.

This is the same falsifier [time-bucket-join.md](time-bucket-join.md) registers, from the
other end: that record asks whether the distractor is ever *pruned*, this one asks whether
the walk can *tell them apart*. Both are answerable on a symbol stream with no perception
layer, which is what makes them the cheap first test.
