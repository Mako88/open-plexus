# 031 — a design pass: where better options may exist

**Status:** a review, not a measurement. Every claim here is either a citation of
something already measured, or a hypothesis marked as one.
**Asked for by John**, after the cache results, to check whether the mechanisms
we are using are the right ones for the goals rather than the ones we started
with.

---

## IN PLAIN TERMS

Three separate walls have been measured in the last day. This asks whether they
are three problems or one, and it turns out to be closer to one.

Almost everything the memory cannot do traces back to a single early decision
about how it labels things — and that decision was made for a very good reason
which is still valid. The question is whether the reason can be kept while the
cost is paid down.

---

## The three measured walls

    capacity is about `d` items      g10-10, g10-11: a 64x64 store holds ~64
                                     bindings, then collapses
    no generalisation between items  g10-09: key overlaps are accidental, mean
                                     +0.0005 against a diagonal of 0.2522
    no overwriting                   g10-11 + correction: rebinding accumulates,
                                     and decay 0.95 fixes it at 18x the
                                     forgetting rate

## Two of the three are the same decision

**`derived_keys` draws an independent random vector per token**, and retrieval is
**linear**: `r = memory @ key`.

- Independent keys means no two tokens resemble each other, so there is nothing
  for the store to generalise *over*. That is the second wall, by construction.
- Linear superposition of near-orthogonal keys gives capacity proportional to
  `d`. That is the first wall, and note 020's `sqrt(d/N)` law is its statement.

**The reason for that decision is still good.** Keys regenerable from
`(seed, token)` are why a node can broadcast a token id instead of a vector, and
note 024 measured the alternative at 187x the storage for a width-1 node. Three
separate results now rest on it. **This is not a mistake to undo; it is a cost to
see clearly.**

---

## Where better options may exist

Ordered by how much they would change, with the tradeoff each entails.

### 1. Keys that carry similarity — the biggest lever, and the hardest

**What it buys:** generalisation between related items, and compression, which
is what makes an associative memory worth having over a table.

**What it costs:** a key that encodes similarity cannot be a fresh draw from
`(seed, token)`. It is either learned, or derived from a structured code. If it
is learned, updates must reach every node, and **that is a global synchronisation
which C1 forbids** — unless the updates are rare enough to be a bounded-asynchrony
problem rather than a barrier.

**The cheap version worth testing first:** structured keys that are *not* learned
— derived from a token's context statistics, or a random projection of a
co-occurrence sketch. Each node can compute them from local information, so C1
survives, and they carry similarity. **Whether that similarity is enough to buy
capacity is a measurement nobody here has attempted.**

### 2. A nonlinearity at retrieval — the well-known fix for the capacity wall

Modern dense associative memories reach capacity far above `d` by applying a
sharpening nonlinearity at retrieval rather than reading a linear sum.

**The catch, and it is the same finding as the cache work:** the high-capacity
formulations keep the stored patterns *separately* and apply the nonlinearity
across them. **That is a table with extra steps**, and g10-07 already measured
what a table does to this task. A nonlinearity applied to a *superposed* store
sharpens what is there; it cannot recover what interference destroyed.

So this is worth knowing about and probably **not** worth building: it converges
on the structure we have already measured as better, without the properties that
made the store attractive.

### 3. The readout is the least examined component

`Wo` is a linear map trained by a delta rule, and it is the only part of the
system that learns anything across sequences. Everything measured about "the
model" on text is really about that linear map reading a superposed store.

**No experiment has ever varied it.** A different readout is the cheapest
untested axis in the project, and unlike keys it has no locality problem — the
delta rule is already local. The obvious first question is whether it is
underpowered or whether the store starves it, and g10-03's finding that the model
BEATS within-chunk counting at chunk 64 suggests the readout is doing more than
it is given credit for.

### 4. Overwriting, now that it is priced rather than absent

Decay 0.95 overwrites and forgets in 13 steps; 0.997 remembers for 231 and never
overwrites. **A write gate would decouple them** — overwrite on surprise, retain
otherwise — and the project already has surprise as a local signal (g9-04) and
already has gating machinery. This is the cheapest concrete improvement on the
list and it uses parts that exist.

### 5. Distribution: dimension-slicing versus a DHT

g10-08 measured key-sharding degrading to a better place under node loss. But
dimension-slicing is what makes C1 free: every node updates its own slice with no
routing and no coordination. **A DHT needs a routing layer, and routing under C3
churn is a hard distributed-systems problem the project has not costed.**

The honest position: the comparison so far has been on a task where the table
wins outright, so it has not been a fair test of the *distribution* property.
That remains untested and is the one place the architecture might still earn its
keep.

---

## What I would do next, if it were mine to choose

**(4) then (3) then (1).** The write gate is cheap and uses existing parts. The
readout is the least examined thing in the project and has no locality cost.
Structured keys are the biggest prize and the biggest risk, and should follow
evidence from the first two rather than precede it.

**(2) I would not build**, and saying so is the point of listing it.

---

## What this note is not

**It is not a measurement**, and none of the five items above has a number
attached that is not a citation. The project's failure mode this week has been
conclusions drawn from arguments rather than runs, five times over. **This is an
argument, and it should be treated as one until each item is tested.**
