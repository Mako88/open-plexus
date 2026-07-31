# HANDOFF — scratch context for a session swap

> **TEMPORARY and OVERWRITTEN, never appended to.** Not a record; nothing durable may
> depend on it. **Nothing else in the tree may cite this file.** Cite `DECISIONS.md` or a
> sweep record instead.
>
> **Where things live:** decisions → `DECISIONS.md`. An option's history →
> `docs/options/<name>.md`. A prediction, before a run → the sweep record. A finding about
> the METHOD → a `CLAUDE.md` calibration. The readable version → `docs/explainers/`. Goal
> and refutation conditions → `GOALS.md`. Notes are RETIRED in `docs/archive/notes/`.
>
> **NO CLAIM LIVES HERE.** Every number points at the file that owns it, and if the two
> disagree that file wins.

**Written:** 2026-07-31, after the session that changed what the project is aiming at.

---

## THE NEXT THING, and John has already chosen it

**Build the grounding mechanism and test it.** John, 2026-07-31: *"I think this is the
absolutely next thing to pursue — getting this all built and then a test."* His preference
is an existing test over a home-made one, and **one exists** (below).

Read these three, in this order, before anything else:

1. [`GOALS.md`](GOALS.md) — the grounding section. What understanding MEANS here, why
   multimodality is a requirement rather than a phase, and gates **G6** and **G7**.
2. [`identity-without-a-global-id.md`](docs/options/identity-without-a-global-id.md) — a
   concept gets no id; it is an equivalence class reached by walking.
3. [`time-bucket-join.md`](docs/options/time-bucket-join.md) — the rounded timestamp as the
   transient cross-node join. **Its first section says what a time bucket is NOT**, because
   the first write-up read as circular.

[Explainer 33](docs/explainers/33-how-two-machines-notice-the-same-moment.md) is the same
thing in plain language and is the fastest way in.

### The design in four lines

    owner(surface id)   everything ever learned about one percept    DURABLE, hashable
    owner(time bucket)  that two percepts occurred together          TRANSIENT, discarded
    a concept           never stored anywhere                        a shape in the links
    identity            learned by counting co-occurrence            not computed

Time is the **join**; the percept's owner is the **accumulator**. Cross-situational
learning becomes local counting at a fixed address, so nothing gathers and no barrier
appears.

---

## THE EXISTING TEST — found 2026-07-31, NOT yet verified beyond its README

**Cross-situational word learning is an established field and it has a model-comparison
dataset.** [Kachergis et al., *A large-scale comparison of cross-situational word learning
models*](https://www.kachergis.com/publication/bakeoff/) — **44 experimental conditions,
1,696 human participants**, with code and data at
[github.com/kachergis/word_learning_models](https://github.com/kachergis/word_learning_models).

Why it is a strong fit, and this is the assessment rather than a measurement:

- **The stimuli are already symbolic.** Per the repository README, a condition gives
  per-trial matrices of words and objects. **Each trial IS a time bucket** — a set of
  things that co-occurred, with the correct pairing unknown. That is our mechanism's input
  shape with no perception layer required.
- **It has HUMAN baselines**, so the opponent is people rather than a counting baseline we
  wrote. This project has never had that.
- **It has published models to compare against**, associative and hypothesis-testing.
- **Referential ambiguity is the whole point of it**, which is exactly G6's falsifier.

**What is NOT verified and must be before it is trusted** (rule 1 — this was read as a
summary and a README, not run):

- The data format has not been opened. `.RData` needs converting; the task layer takes no
  dependencies, so the conversion is a one-off, not an import.
- **There is no licence file.** Downloading and using locally is inside John's standing
  permission; committing the data into this repo is not obviously safe. Check before
  vendoring — `.gitignore` already covers `data/*/`.
- Whether the 44 conditions are all word-object, or include variants that do not fit.
- It is words-and-objects, **not literally image and audio**, so it tests the mechanism's
  shape rather than G7's cross-modality. G6 first, G7 later.

**If it does not pan out**, the fallback is registered in both option records: a symbol
stream with a **distractor present on every single occasion**. Does it ever get pruned, and
can the walk tell it from the target? Minutes to write, no perception layer.

---

## WHO ELSE IS DOING THIS — searched 2026-07-31, and the search was NOT wide

**Read as search summaries and abstracts, not papers.** Rule 19 says a negative result is
not a finding until the search was a wide one, and four web searches is not that. Treated
as leads.

**Three established fields, each doing one piece of it:**

- **Cross-situational word learning** — cognitive science, decades old, human baselines.
  Exactly our co-occurrence-to-identity mechanism, but single-machine, batch, and much of
  it explicitly works on simplified symbolic inputs rather than raw sensory ones.
- **Hyperdimensional computing / Vector Symbolic Architectures** — two ACM Computing
  Surveys, applied to edge devices and federated learning. **Our superposed store IS a
  VSA associative memory.** The SNR law and the capacity constant this project derived and
  checked against an analytic bound are results in that field. Worth knowing we are inside
  a literature rather than beside one.
- **Decentralised training on volunteer hardware** — Learning@home / Hivemind, Petals,
  Nous Psyche, Pluralis Agora. These already run large models across unreliable volunteer
  machines over the ordinary internet, at electricity costs reported far below cloud spot
  pricing.

**THE UNCOMFORTABLE PART, and it is a premise-level challenge rather than a detail.** Those
projects keep backpropagation and engineer AROUND the synchronisation — pipeline
parallelism, expert routing, scheduling. They work. So *"distributed AI on consumer
hardware"* is **not** this project's differentiator: it exists and it ships.

The bet that is unclaimed is the narrower one — **that a local rule removes the need for
the global step**, rather than tolerating it. Which means the payoff has to be something
the synchronised systems cannot do: learn continuously without stopping, survive arbitrary
churn, keep reorganising concepts as new things arrive.

**`GOALS.md` currently argues from cost and access. The stronger argument is capability
under conditions the alternatives cannot meet, and it is not written that way.** Raised as
an open question for John rather than edited in, because it is a change to the project's
stated premise and that is his call.

---

## WHAT CHANGED ABOUT THE PROJECT'S DIRECTION

**`GOALS.md` §2 already refused next-token prediction. It was not enough** — a
sequence-prediction benchmark was proposed anyway, for good reasons (it solved recurrence,
filler and perpetual learning at once), and nothing in the document objected. **A refusal
without a positive statement only catches bad-faith proposals.**

So `GOALS.md` now states the positive half, and the gate ladder has two new rungs:

    G6  composition   answer about a relation it was never given, that follows from
                      ones it was. Refuted if it can only return what it was told
    G7  grounding     a concept introduced through one modality, queried through another

**The ladder previously stopped at scale**, which meant every gate could be passed by a
system that had understood nothing.

**MQAR should stop being the scoreboard.** It has no concepts and no relations in it, its
memory is rebuilt per sequence so it structurally cannot test C4, and its shape already
forced a C1 violation once. Keep it as the capacity ruler — its scaling exponent is real
and useful — and stop judging progress by it.

---

## THE KILL LIST

     ✅  2  representations learned LOCALLY   18 graphs, beats counting, no invariant
     ✅  6  independent nodes agree           TRANSPORT half only; quantiser half ⬜
     ✅  7  decide what to say, and decline   exact, on the case the gate can see

     🔀 10  margin survives scale             REFUTATION WAS ON THE WRONG ARRANGEMENT

     ⏸  4  multi-hop walk over real internet MEASURED; accepted, tweak later

     ⬜  1  relational objective buys reasoning  blocked: no instrument with a wide band
     ⬜  3  conventional system already wins     first outside number run, and we lose
     ⬜  5  learn forever                        the cheap route is refuted
     ⬜  8  adjudicate contradictions            untouched
     ⬜  9  survive hostile participants         untouched
     ⬜ 11  training traffic fits broadband      G4 passes on ONE SEED
     ⬜ 12  survives a second modality           now G7, and it has a design

**#10 moved from ❌ to 🔀.** Everything recorded about it was measured on DIMENSION
partitioning. Concept partitioning solves the same cell on a quarter of the memory and a
quarter of the machines — figures in
[`g29-02`](experiments/sweeps/g29-02-concept-partitioning-at-EQUAL-state.txt). **Not a
checkmark: the concept arm is saturated at 1.0000 in every cell with zero spread**, so the
grid cannot rank anything or fit a slope.

**A local probe DID find concept's wall and it is a cliff.** Width 32, 2 machines: solid at
sequence 384, collapsed at 768, all three seeds. **The 1536 cell was killed for the session
swap and never ran.** Numbers are in the terminal history only — **re-run before quoting**,
they are not in any record.

**#3's first outside number**: raw store on FB15k-237's own metric loses to a counting
baseline by a wide margin —
[`g30-01`](experiments/sweeps/g30-01-link-prediction-on-their-task.txt). The learned arm is
better but still loses, and **width HURTS it**, which says it is optimisation-limited
rather than capacity-limited. It does not close #3: link prediction is offline, global and
non-local.

---

## IN FLIGHT AND UNFINISHED

**`g30-02` has no write-up.** The predictions are committed; the grid ran; the record still
says pending. P4 (best K interior) and P5 (widening helps) both need scoring, and **P5 is
refuted in the opposite direction to the prediction**. Convergence was probed and the arm
peaks around 8 epochs then turns down. **Everything is in terminal history only — the
numbers must be re-run, not recalled.**

**`g29-03` was never written.** The sequence-length grid that would fit concept
partitioning's exponent. The cheap probe located the cliff; the grid is the next step and
belongs in Actions.

**`concept_replicas` defaults to 1 in the model config and 3 in `ConceptStore`.** Every
measurement ever run had zero fault tolerance. **Replication is free in RAM** — verified,
identical state at 1, 2 and 3 replicas — so this is a default worth fixing before anything
distributed is measured. It costs superposition capacity, not memory.

**`g22-01` is built and dispatchable and has never been dispatched.**

---

## PROCESS, and what must not regress

- **CHECKPOINT ONTO A BRANCH** when a sweep is in flight. `checks.yml` cancels superseded
  runs per ref, so pushing to master repeatedly starves a queued matrix.
- **`gh run view --jq` with a literal `"/"` breaks under Git Bash** — MSYS expands it into
  a Windows path and the poll silently returns garbage rather than erroring. Build status
  strings without slashes.
- **Verify a run's identity FROM THE DATA.** It caught three wrong numbers this session,
  including one where the machine count was derived from the width by the script and the
  prediction assumed otherwise.
- **`check_provenance.py` earned its keep four times in one session**, every time the same
  shape: a figure written into a record under a citation that does not contain it.
- **Read a knob's own definition and its test's docstring before sweeping it.** `g29-01`
  was dispatched into a confound that `local_memory.py` and `tests/test_concept_routing.py`
  had both already written down.

---

## STATE

Clean tree, 207 mutations verified, 1,370 tests green, all seven checks passing. **Seven
commits unpushed at the time of writing** — push them and watch `checks.yml`. No background
processes, no `.mutate.lock`, no sweep in flight.
