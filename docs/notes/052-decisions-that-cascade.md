# 052 — The decisions that cascade, and which of them to make now

**Status:** a decision inventory. Nothing built, nothing chosen without John.

**Why it exists:** John asked, after the ground-up pass paid, whether there are
*other* decisions of the same kind — ones where going one way would force
rework across everything built afterwards, and which are therefore cheaper to
settle before more is built on the current answer.

**The test for belonging here** is not importance. It is **blast radius**: if
this is decided the other way in three months, how much has to be re-measured?

---

## 1. Discrete surfaces, or continuous addressing? — **the big one**

**John's question, made precise.** Multi-modality is in GOALS §1 and row B4 of
ARCHITECTURE is UNTESTED. A text token is a discrete id. An image patch or an
audio frame is a **vector**. So how does a non-text input reach a concept?

    (a) QUANTISE     a patch/frame is mapped to a discrete surface id, and
                     everything downstream is untouched. `concepts.Surfaces`
                     already exists for exactly this -- one concept, many
                     surfaces
    (b) ADDRESS BY   the store is keyed by a continuous vector directly
        VECTOR

**Blast radius of (b) is close to total**, and it is worth being concrete about
what it destroys rather than vague about it being "a big change":

- **exact addressing.** Note 035 measured interference as `O(N * rho)`. Two
  similar images give two similar keys, which is `rho` rising by construction --
  the thing identity addressing exists to avoid, and the reason note 045 keeps
  similarity in a separate index
- **the gate's structurally-zero bar.** Decision 148 works because an unwritten
  address reads *exactly* 0.0. With continuous keys, "near a written address"
  reads *nearly* zero, and the bar becomes a tuned constant -- which is note
  049's P3, the thing decisions 147 and 148 spent a day escaping
- **the sketch.** `AddressSketch` hashes by sign patterns and needs exact
  repeats to collide. Continuous inputs never repeat exactly

**Recommendation: (a), and it is not a close call.** Every strength this
architecture has measured — exact recall, exact membership, no interference
between distinct things — rests on discrete identity. The field does this
routinely (VQ-VAE and discrete audio codecs exist for the same reason), so it is
also the choice with the most precedent, which is GOALS' standing rule.

**What (a) still costs, honestly:** something must learn the quantiser, and a
bad one merges two things that should be distinct — which this architecture
cannot recover from, because it will address them identically. **That failure is
silent**, and it is the real risk of (a) rather than the change itself.

**Decide now?** Yes, and it is nearly free: choosing (a) is choosing to keep what
exists. What it changes is the *roadmap* — B4 becomes "fit `ContentIndex` across
paired streams", which is additive, rather than "re-key the store".

> **Outcome.** John accepted (a) — decision 163 §1. And the risk named there was
> half of one: [note 053](053-two-nodes-must-agree-on-what-a-picture-is.md) adds
> the direction that only exists distributed, where two NODES quantise the same
> input differently and no node can detect it locally.

## 2. Where does the relation come from at read time?

Decision 158 built `hop_relation` and named its limit: **the relation is fixed,
not chosen.** In kinship the question states the relation, so it is free. In an
open query it is not.

    (a) FROM THE LAYOUT    the query states it, as kinship does. Free, and
                           limits the system to structured queries
    (b) LEARNED CHOOSER    something predicts which relation to follow
    (c) TRY ALL, GATE      follow every relation type, keep the one whose
                           address is not empty

**(c) is worth noticing** because it costs what section 1 of this note's sibling
question costs — `r` reads — and needs no new mechanism at all. **It is the gate
doing selection again**, which is the one selection rule in this project that has
ever worked (148, after 147 refuted two others).

**Recommendation: (a) now, measure (c) next, and do not attempt (b) yet.**
Decision 147 is the argument: two hand-made selection rules were refuted before
membership worked, and a learned chooser is strictly harder than either.

> **Outcome.** John ruled (a) then (c) — decision 163 §2. Decision 162 then found
> that the prior question was not *which* relation but whether a hop can carry its
> own at all, and **164 built it** (`hop_relations`). (c) is still unbuilt.

**Blast radius: moderate.** It changes the read path but not the representation.
Deferring it costs a re-measure of the composition rows, not a rebuild.

## 3. What is an "answer"? — the one nobody has written down

ARCHITECTURE row F3: **nothing in this project has ever scored a multi-token
answer.** Every task emits one token. So "form a response from awareness of the
concepts in the question" has never been tested, and the shape of a response is
undecided.

    (a) AUTOREGRESSIVE      emit a token, feed it back, repeat
    (b) TRAVERSAL           walk the concept graph and emit what is visited
    (c) STRUCTURED SLOTS    fill a fixed frame

**(a) deserves care rather than reflex rejection.** GOALS §2 rules out next-token
prediction **as the training objective**, which is a different thing from using
autoregression as an output *mechanism*. Conflating those would be a rule
misapplied.

**Blast radius: large, and it reaches backwards.** Every task, every accuracy
number and the whole scoring convention assume one answer token. Whatever is
chosen, the existing tasks stay valid as *capability probes* — but they stop
being measurements of the goal.

**Recommendation: decide before scaling, not before E4.** It is the row where the
project's actual goal lives, and it is cheap to prototype on the tasks that
already exist.

> **Outcome.** John's ruling made this the live question (163 §3), and it has
> narrowed. (c) SLOTS is not a peer of (b) — a fixed frame is a traversal with a
> fixed relation schedule, which decision 162 already calls a fitted constant. So
> the choice is (a) against (b), and **what decides it is termination**: under (b)
> the walk stops where decision 148's gate reads structurally zero, and nothing is
> fitted; under (a) stopping is a learned end-token. Decision 165 builds the ruler.
>
> **And rendering is a separate row, not part of this one.** A concept walk does
> not emit English. John raised that, and the split — a traversal that decides
> *what* to say, a realiser that decides *how* — is the field's own two-stage
> generation shape. The hazard is specific: a fluent renderer can produce the right
> sentence from a wrong walk, so the concept set stays the scored artifact and the
> first realiser is templates, which cannot add a fact. Blast radius of rendering
> is near zero, which is why it does not belong in this note's table.

## 4. Does the store ever persist across sequences?

Today the concept map is durable and the store is per-sequence working memory.
`carry_store` exists and is off; C4 (perpetual learning) says the weights never
freeze, and GOALS §3 notes C4 **forbids stopping, not revisiting**.

**Blast radius: moderate, and it is a capacity question.** A persisting store
meets note 035's wall much sooner, and decision 134's concept partitioning is the
seam that would absorb it. **Not urgent**, because nothing currently needs
cross-sequence memory — but it becomes urgent the moment a task does, and it
should not be discovered then.

---

## What this note recommends deciding now

| # | decision | recommend | why now |
|---|---|---|---|
| 1 | discrete surfaces vs continuous addressing | **discrete** | free to choose, and it makes multi-modality additive instead of a rebuild |
| 3 | what an answer is | **prototype before scaling** | it reaches backwards through every task, and it is where the goal lives |
| 2 | how a relation is chosen | layout now, try-all-and-gate next | changes the read path only |
| 4 | store persistence | defer, deliberately | nothing needs it; note the trigger |

**The pattern worth keeping:** every one of these is a place where the cheap
choice and the general choice differ, and where the general one is only cheap
*before* something is built on the other. That is what made the ground-up pass
worth doing, and it is why this list exists rather than being rediscovered.
