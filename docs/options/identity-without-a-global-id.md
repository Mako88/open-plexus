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

### The walk was built and it recovers the classes — `g32-01`, `g32-02`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g32-01-can-counting-tell-the-distractor.txt
            script  experiments/g32_01_can_counting_tell_the_distractor.py
            task    occasions, 64 concepts, 3 surfaces, presence 0.7, noise 3
            model   none -- a mutual top-k graph over counts; no store, no vectors
            knobs   statistic, zipf, distractors, shuffled control; k 2; 3 seeds
            scale   8,000 occasions per stream

`equivalence_classes` is the walk this record describes: start anywhere, follow links,
and the connected component you arrive at is the class. It reaches **1.0000** f1 — every
concept recovered exactly — under three of four statistics with a distractor present, and
under all four with none.

**`k` was SUPPLIED**, so the walk was told how large a class is. That is generous, and it
makes the pass a weak confirmation while a failure would have been strong. Finding the size
instead is *bound the enumeration by the biggest similarity gap* in `DECISIONS.md` §6 and
is untried here.

**The cost this record predicted is real and now has a number.** A concept needs about
**16** occasions before the walk recovers it, and a concept the stream shows a handful of
times is not reachable by any statistic over counts —
[co-occurrence-statistic.md](co-occurrence-statistic.md) holds the curve.

**And the load this record warned about — a percept that co-occurs with everything —
arrives without anyone building one.** At zipf 2.0 the commonest concept is the subject of
most occasions, so its surfaces become the best raw-count partner of **60 of 60** surfaces
of the twenty rarest concepts. The busiest-peer pressure this record names is therefore not
hypothetical, and it is the same object as the distractor.
