# 042 — An architecture pass, before more component work

**IN PLAIN TERMS.** The project has been improving one part at a time and getting
real gains. But every one of those gains is measured on a design that may be
about to change, and a change to the core would throw the measurements away. So:
stop, look at the whole thing, and decide what the shape should be before
polishing the parts.

The short version of what this found: **the model has almost nowhere to keep what
it learns.** Everything it knows long-term is one flat table of numbers, and the
part that holds relationships is wiped clean every few hundred words. For a
project whose goal is "a map of how concepts relate", that is the problem, and no
amount of component tuning reaches it.

---

## Why now

John, 2026-07-28: *"we're gonna have to redo all the tests anyway once some core
pieces change. So let's get all the core pieces right first."*

That is correct, and the record supports it. Decision 74 invalidated a comparison
set by changing one default; decision 12 records the same class. The search line
(g13-01…05) took kinship from 0.327 to 0.624 across five sweeps, and **not one of
those numbers survives a change to the store, the readout, or the objective.**

**The danger of an architecture pass is that it becomes unfalsifiable**, which is
precisely how the predecessor died: *"the architecture was built without a plan
first. Mechanisms accumulated and the design document was written to describe
them, so there was never a document that could reject a mechanism."* So every
proposal below carries the measurement that would decide it, and nothing is built
here.

## The finding that reframes the rest

**Decision 62, and it has never been acted on.** `memory = np.zeros((d, d))` is
inside `run`, so the associative store is rebuilt from scratch every sequence.
`Wk` and `Wv` are frozen random. Therefore:

    Wo    vocab x d, delta rule       THE ONLY THING THAT PERSISTS
    Wk    frozen random               never updated
    Wv    frozen random               never updated
    store d x d                       REBUILT EVERY SEQUENCE

**Everything this model knows across a corpus is one linear map.** The store —
the part that holds relations between things — is working memory and nothing
else. Confirmed empirically: with `learn=False`, predictions are byte-identical
whether or not another sequence ran first.

Set that against GOALS §1.2: *"once it has a good map of most all concepts, and
is able to be aware of how a given concept relates to some other concept."*
**There is no such map in this architecture.** There is a per-sequence scratchpad
and a linear readout.

This also explains three separate measured results at once, which is what makes
it an architectural finding rather than an observation:

- **Decision 63** — more data stops helping at ~16,000 characters. Of course: one
  linear map converges fast and nothing else durable exists.
- **Decision 115** — the store's effective rank is ~3 whatever the width. A
  per-sequence bigram scratchpad cannot be more.
- **g14-01** — `local` scores 0.097 on entailed against attention's 0.277. The
  local rule has one linear map to put composition into.

## Component pass

For each: what it is, what limits it *as measured*, and what would replace it.
**Novel where novel is better** — GOALS §2 as amended.

### 1. The store — where a concept map would have to live

**Now:** one `d × d` matrix of summed outer products, rebuilt per sequence.
**Measured limits:** capacity ~d² (109); effective rank ~3 (115); *"nothing
applied after a sum recovers what the sum destroyed"* (the through-line); d² per
conversation makes concurrency RAM-bound rather than node-bound.

**Proposal — two stores on different timescales.** A fast per-sequence store as
now, and a **slow persistent one that survives sequences**, written to only by
what the fast store found worth keeping. This is the hybrid the backlog has
carried, and it is also the only place a concept map can live.

It is not a new idea (complementary learning systems; Zenke & Gerstner's
multiple timescales, which `lasting_cap` already half-implements) and this
project has the machinery: `lasting`, `decay`, `memory_cap`, and a write gate
measured real in decision 79.

**What decides it:** does a model with a persistent slow store keep improving
past decision 63's 16,000-character wall? That is a direct, cheap test and it
falsifies the whole proposal.

### 2. Keys — the addressing scheme

**Now:** `TableKeys` (per token) and `PairKeys` (hashed `(previous, token)`).
**Measured:** pair keys were worth +0.269 on the relational line (decision 130's
`walk`); single-token keys lose information irrecoverably (108).

**The novel option, and I think it is the right one eventually:** keys derived
from *content* rather than identity, so similar concepts land on nearby keys.
Every key here is a random draw, which means **the store has no notion of
similarity at all** — `dog` and `wolf` are as unrelated as `dog` and `7`. A
concept map without similarity is a lookup table.

**What decides it:** g10-09 tried "is there similarity to generalise" and was
**retracted** — the cache is indexed by token id, so the question was never
asked. It is still open and it is now the more important half of the map idea.

### 3. The readout — the only thing that learns

**Now:** one `vocab × d` linear map, delta rule. `hidden` measured as the largest
single factor on text (83).
**Measured limit:** it is the *entire* persistent parameter set.

**Proposal:** stop treating it as the model's memory. If the slow store carries
the map, the readout's job shrinks to decoding, and its size stops being the
bottleneck. **This is a consequence of (1), not an independent change** — which
is exactly why (1) goes first.

### 4. The learning rule

**Now:** delta rule on `Wo` only, at scored positions.
**Measured:** it is the exact gradient for a single linear readout, so it is not
an approximation of backprop — there is nothing to backpropagate *through*.

**The honest statement:** the rule is not the limitation; **the absence of
anything for it to write to is.** Improving the rule before (1) is polishing.

### 5. Retrieval, hops, search

**Now:** `SuperposedRead` / `ExactCache` / `SettlingRead`; `search.py` with a
decode-margin gate (130).
**Measured:** the gate line works and is the project's cleanest result.
**Verdict: leave alone.** This is the one part that is not architecture-limited,
and it will need re-measuring after (1) regardless.

### 6. Partitioning — and this is the one that serves the goal directly

**Now:** split by **dimension**. Every node computes `M_slice @ key_slice` and
**inherits the sum**.
**Measured:** dimensions-per-node ≥ ~16 or a node has no standalone opinion
(g4-01); concurrency costs d² per conversation.

**Proposal — partition by CONCEPT, not by dimension.** Each node owns some
entities and the relations they participate in.

    reading            a SELECTION across nodes, not a sum
    churn              lose a node, lose some concepts -- degrades, not amputates
    concurrency        a conversation touches a subset of concepts, so a subset
                       of nodes; the d^2-per-conversation problem does not arise
    the map            IS the partition -- each node holds part of the concept map

**This is the single change that most serves the stated goal**, and it composes
with (1): a persistent per-node store *is* concept-partitioned by construction.

**What decides it:** decision 119 measured the superposed store beating a bounded
cache by 8× when bindings exceed slots, so "just keep items separately" is not
free. The test is whether a concept-partitioned store keeps that advantage.

### 7. Transport and the driver

**Now:** vote-based, 8 bytes per node per decode; deadline and suspicion added
(126, 128); `d_max` measured at ~640 ms.
**Verdict: sound, and ahead of the rest.** The remaining gaps (probe channel,
indirect probing, the driver being a single detector) are known and note 039
lists them. Not architecture-blocking.

### 8. The objective

**Now:** next-token prediction with marked questions; `closure.py` removes the
marker.
**Measured:** all-position training costs composition 1.000 → 0.40 (95–98).

**This is architectural in effect** and GOALS §1.2 already records the intent.
Grouped with (1) because a persistent store changes what an objective can even
ask for.

## The whole: what I would change, and in what order

Ranked by **what invalidates the most if changed later** — John's criterion, and
the right one.

| | change | invalidates if deferred | decided by |
|---|---|---|---|
| **1** | **Persistent slow store** | everything; it changes what the model *is* | does it break decision 63's 16k wall? |
| **2** | **Concept partitioning** | every distributed and concurrency result | does it keep decision 119's 8× advantage? |
| **3** | **Content-derived keys** | every task result, since addressing changes | does similarity generalise (g10-09, retracted) |
| 4 | objective / instrument | task results only | already in flight |
| 5 | readout, retrieval, search | component results | re-measure after 1–3 |

**1 and 2 are the same change seen from two sides.** A persistent store that is
partitioned by concept is one design, and building either alone would mean
building it twice.

**So the architecture proposal is one thing, not five:** *a persistent,
concept-partitioned associative store, with the per-sequence store demoted to a
working buffer in front of it.*

## On self-modification — John asked, and my answer is split

He is right that adding it later would invalidate things, by the same argument as
above. But there is a distinction worth holding:

**Reserve the seam now. Build the mechanism when it is measurable.**

A self-modifying structure needs somewhere to modify. Right now the structure is
`d × d` and fixed at construction, so there is nothing to change — which is
another way of saying **(1) and (2) are prerequisites for self-modification, not
alternatives to it.** A concept-partitioned persistent store is a graph, and a
graph is the thing that can grow an edge, drop one, or split a node.

What I would *not* do is build it before there is a task where structure that
adapts beats structure that does not. This project has no such measurement, and
a mechanism with no falsifier is the predecessor's exact failure mode. Note the
precedent: **C4 (perpetual learning) is still untested after two attempts,
because both times the task was too easy to need it** (91, 92).

So: design (1) and (2) so the store's shape is *data* rather than a constructor
argument, and self-modification becomes reachable rather than requiring a
rewrite. Then build it against a task that can tell whether it helped.

## What this note does NOT do

Build anything, or claim any of it works. Every row in the table names the
measurement that would decide it, because the failure this project exists to
avoid is a design document that cannot reject a mechanism.

**And one caution about the pivot itself.** Component work has been producing
measured gains and architecture work produces designs. The rule that keeps that
honest is rule 17: *after a block of verification, the next block builds
something.* An architecture pass should end in a build, not a second pass.
