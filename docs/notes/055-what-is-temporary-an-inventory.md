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

**The falsifier is already named and cheap** (042 §1): does a model with a
persistent slow store keep improving past decision 63's **16,000-character wall**?
One axis, one arm, and it can be probed locally before a matrix is spent.

**And this note must not become a third pass.** 042's closing caution is that an
architecture pass should end in a build, not another document. Two design documents
now describe this change and nothing has been built toward it.
