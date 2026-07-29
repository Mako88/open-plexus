# 055 — What is temporary: an inventory

**Status:** a status pass, not a new design. John asked for a scan of everything
that is a stopgap, so that work can shift to final versions rather than iterating
on things that will not survive.

**The first thing the scan found is that the design pass already exists.**
[Note 042](042-an-architecture-pass-before-more-component-work.md) is that
document, written 2026-07-28 to John's own criterion — rank by what invalidates the
most if changed later. **Its top three items have not moved, and every default
still ships them off.** So this note does not re-derive them; it records their
status and adds what 042 did not cover.

---

## IN PLAIN TERMS

Most of this system is real. Two things about it are pretending.

**It has almost nowhere to keep what it learns.** The part that holds relationships
is wiped clean at the end of every sequence, and the only thing that survives is a
single flat table of numbers. For a project whose goal is a map of how concepts
relate, **there is no map** — there is a scratchpad and a decoder.

**And it has no idea that two things can be similar.** Every concept gets a random
address, so `dog` and `wolf` are exactly as related as `dog` and `7`. The
similarity that does exist lives in a separate index bolted alongside, consulted by
hand.

Everything else on this list is either a number that will need re-measuring at a
bigger size, or a part that has not been built yet. Those are ordinary. The two
above are the ones where continuing to tune around them is wasted motion.

---

## A. Load-bearing temporary — replace before the goal is reachable

**A1–A3 are one change seen from three sides**, which is the single most important
line in this note. 042 says 1 and 2 are the same design from two directions;
content-derived keys are the third face. *A persistent, concept-partitioned store
addressed by content-derived keys **is** the concept map.* Building any one of them
alone means building it twice.

| # | what is temporary | status today | what it costs to defer |
|---|---|---|---|
| **A1** | **No persistent store.** `memory = np.zeros((d, d))` inside `run`; everything durable is the one linear map `Wo` | `carry_store` **False** by default. Decision 62 found it; 042 §1 ranked it first; note 052 §4 deferred it | Everything. It changes what the model *is*. Explains decision 63's 16k wall, 115's rank-3 store, and g14-01's 0.097 at once |
| **A2** | **Partition by dimension, not concept.** Every node computes a slice and inherits the *sum* | `concept_nodes` **0** by default. The seam exists (134, `partitioned.py`) and **refuses to combine with `consolidation` or `carry_store`** — both refusals labelled temporary in the source | Every distributed and concurrency result |
| **A3** | **Keys are random identity draws.** `TableKeys`, `PairKeys`, `ByConcept` all hash identity, so the store has no notion of similarity | `ContentIndex` exists but is a **side channel** that proposes neighbours; it does not address the store. `keys.py` mentions content-derived keys only hypothetically | Every task result, since addressing changes |
| **A4** | **The global readout sums across every dimension** — which is the globally synchronised step **C1 forbids.** The project's own first constraint | Live. Surfaced in a footnote to note 009 §4; `combine="vote"` mitigates the *bandwidth*, not the violation | Four gates were passed and five sweeps run on a model that violates constraint one |
| **A5** | **The answer's size is handed to the model.** `branches` must equal the group size or the set answer collapses | Measured this session, decision 167. F3 is PARTIAL for exactly this reason | The difference between answering and being told the shape of the answer |
| **A6** | **`hop_relations` is a fitted schedule.** Which relation at which depth is supplied, not chosen | Built this session (164) and **labelled an instrument, not the read path.** Try-all-and-gate is the intended final form (163 §2) | A composition number conditional on a constant |
| **A7** | **Every task is self-designed.** CLUTRR has been "next" for several cycles | Not run | Until it runs, this project is grading its own homework |

## B. Scale-limited — correct now, will need re-measuring

**`docs/SCALE.md` is the register and it is current**, which is the good news in
this scan: eleven rows, each with what was chosen, at what size, and the trigger to
revisit. Not repeated here. The three with the nearest triggers:

- **The linear readout crosses the store at width ~100.** Readout grows linearly,
  store quadratically. Above that the readout is the binding constraint
  (decision 110).
- **`hop_accumulate="concat"` wins because 16 rules in a 128-wide space are
  linearly separable whatever the labels do** — a property of having few rules,
  not of concatenation being right.
- **`RETRY_AFTER_SECONDS = 0.64` is a floor, not a constant.** Measured on a
  container bridge with simulated impairment; a real WAN raises it.

## C. Not temporary — absent

Distinguished from A on purpose: these are not stopgaps to replace, they are holes.

| what | status |
|---|---|
| **A quantiser** | Does not exist. Nothing non-text can enter. B4 and F2 both UNTESTED, and note 053 adds the codebook-agreement constraint |
| **A renderer** | Does not exist. A concept walk emits concepts, not language |
| **Declining to answer (C4)** | **Nothing anywhere lets the model say "I do not know"**, and no task scores abstention — while the architecture may be structurally free of confident confabulation. An untested claim about honesty |
| **A threat model for untrusted nodes** | None. Forks on the endgame: open-source-and-runs-everywhere means nodes that can lie |
| **`search.py` wired into `run`** | Built, tested, and **deliberately not called** by `run` — labelled *"scaffolding that is not named as scaffolding becomes load-bearing"* |
| **Slice negotiation** | Static, *"for now, by John's explicit choice"* — a node that negotiates its own slice is a coordination protocol and nothing needs one yet |
| **Self-modification** | No structure to modify: the store is `d × d`, fixed at construction. A1/A2 are its prerequisites, not alternatives |
| **The consumer-device runtime** | Undecided. numpy is approved for the model layer only |

## D. Deliberately permanent — do not churn these

Listed so effort does not go here looking for stopgaps.

- **The dependency-free ruler.** `openplexus/tasks/`, `baselines.py`, `answers.py`
  take no dependencies by design — they are what everything else is asserted
  against, and pure Python is auditable line by line.
- **Reference implementations**, with any faster path asserted against them rather
  than replacing them.
- **Refuted alternatives behind switches** (rule 14c) — `TableKeys` beside
  `PairKeys`, `SuperposedRead`/`ExactCache`/`SettlingRead`, `hop_accumulate`
  `bind` beside `concat`. **Refutations expire**, and 107 and 111 both became
  right later when their inputs moved.
- **The verification apparatus** — the mutation harness, the rails, the workflow
  and architecture checks, the three-document structure.

---

## What this implies for sequencing

**The "final version" John is asking to shift to is a single thing:** a persistent,
concept-partitioned store addressed by content-derived keys, with the per-sequence
store demoted to a working buffer in front of it. That is 042's conclusion
unchanged, and A4 belongs with it — a concept-partitioned read is a *selection*
across nodes rather than a sum, which is what removes the C1 violation instead of
mitigating its bandwidth.

**It invalidates essentially every task number**, which is the argument for doing it
now rather than after more iteration. That is John's own reasoning and the record
supports it: decision 74 invalidated a comparison set by changing one default.

**And this note must not become a third pass.** 042's closing caution is that an
architecture pass should end in a build, not another document.

---

## CORRECTION, same day, before anything was built on this

**Two claims above were wrong, and both were wrong in the flattering direction —
they made the next step look unmeasured and available.** Rule 5: the record gets
fixed, not softened.

**1. The falsifier has been run. It came back NO.** This note said 042 §1's
falsifier was "cheap and already named" and could "be probed locally before a matrix
is spent", implying it was open. `experiments/g15_01_does_persistence_break_the_wall.py`
exists, has a workflow, and **ran three times** — recorded in
`experiments/sweeps/g15-01-does-persistence-break-the-wall.txt` and read in
**decision 133**:

    arm                4,000    8,000   16,000   32,000   62,500  125,000
    baseline          5.5989   5.5709   5.5353   5.5327   5.5255   5.5261
    persist-slow-decay 5.5250  5.4823   5.4551   5.4536   5.4393   5.4427

    slow-store norm, persist-slow-decay:  0.4 at EVERY corpus size

**P3 REFUTED.** Movement past the wall is +0.0124, under the 0.04 seed spread and
not monotone, with the gate firing 16,470–51,713 times so the null is about
persistence and not a shut gate.

> **A decaying persistent store is a fixed-size cache holding a moving window, not
> a map that grows.** The wall is a **CAPACITY** limit, not a lifetime limit.

The good half is real and also sitting unused: `persist-slow-decay` beats baseline
by **0.074–0.083 bits at every data point**, its own control (`consolidate`, same
consolidation without persistence) is *worse* than baseline everywhere, and it is
**off by default.**

**2. "Nothing has been built toward it" is false.** Item 1 has an experiment run
three times and a decision. Item 2 has a built seam — `partitioned.py`,
`ConceptStore`, `concept_nodes` — and decision 134. What is true is narrower:
**neither is on by default and the combined change does not exist.** The flattened
version erased two real pieces of work.

### And the sequencing this note recommended is not supported

It pointed at A1 first. Decision 133 pointed at item 2 instead, on the grounds that
*"concept partitioning is the only proposal on the page that adds capacity as the
corpus grows."* **Decision 134, the very next entry, measured that claim and it does
not hold:**

    pooled capacity is IDENTICAL between arrangements -- 128 / 256 / 512 /
    1024 / 2048 in both, at 1 / 2 / 4 / 8 / 16 nodes

Pooled capacity grows with **total memory**, in either arrangement. What concept
partitioning uniquely buys is **lone-node capacity and independence** — 2048 against
128 at sixteen nodes — which is churn resilience and C1, not the wall.

**So 133's forward-looking sentence was superseded one entry later, and this note
inherited it.** That is precisely the drift `STATE.md`'s header exists to stop,
reproduced inside a note written to survey the drift.

**What is actually open**, stated without a recommendation attached, because the
next step should be chosen against the corrected record rather than this one:

- the wall is a capacity limit, and capacity is `~d²` (decision 109) — a property
  of **width and total memory**, not of lifetime and not of arrangement
- persistence is worth 0.08 bits, is a prerequisite for anything that accumulates,
  and adds no capacity
- concept partitioning buys independence and lone-node capacity, measured, and its
  case is **not** the wall
- **no measurement here says what breaks the wall.** That is the open question, and
  it was hidden by two stale forward-looking claims stacked on each other
