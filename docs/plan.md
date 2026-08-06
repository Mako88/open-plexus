# Where this is going

**A DIFFERENT BET FROM `csharp`, ON PURPOSE.** That branch counts co-occurrences and
walks them. This one holds COMMITMENTS and repairs them when they break. The
substrate is shared; the thing being counted is not. `csharp` is not abandoned and
nothing here refutes it — read its refutation table before repeating anything.

- **The only doc, and it holds nothing that is finished.** What a built mechanism
  does lives in its XML comments, where the compiler enforces every reference.
- **Findings live in the commit** that produced them, and in the test that asserts
  them. Never here.
- **One line an item.** A cap per ITEM, not per doc.
- **Built and decided means GONE FROM HERE, and it means no arm either.** A winner
  becomes the code; losers are deleted, leaving a revival row.

---

## The goal

- **Understand rather than perform** — answer *what would the world look like if I
  did X*, which a sequence model cannot be.
- **A COUNT IS NEVER WRONG; A COMMITMENT IS.** A co-occurrence cell that mispredicts
  becomes a slightly different number. A commitment that mispredicts is WRONG ABOUT
  SOMETHING, and which something is the whole of what can be learnt from it.
- **The representation is the residue of repaired failures**, not a thing designed
  up front. Distinctions get minted because something is needed to tell two
  conflated cases apart.

## The constraints

Carried unchanged from `csharp`. They are about the machine, not the architecture.

- **C1** — no node reads another's data. A commitment records its OWN hits and
  misses.
- **C2** — messages are late, jittered, out of order.
- **C3** — a cluster vanishing mid-thought is NORMAL, not an error.
- **C4** — no episode boundary, so nothing may depend on train-then-test.
- **Counts only ever rise.** Hits and misses are BOTH G-Counters, and reliability is
  their ratio — so convergence holds with no coordinator.
- **Repair ADDS a narrower commitment and never edits the old one.** Monotonicity is
  preserved rather than strained, and the eviction tension that blocked positing on
  `csharp` does not arise: a commitment specialised out of relevance stops firing.
- **Codes must be identical on every machine forever.** A specialised commitment's
  identity derives from its parent and the condition added.
- **A front end may say what it is looking at, never what to conclude.** And the
  rule is about SEMANTICS, not adaptation — *this is the same thing you saw six
  times*, never *this is a red ball*.

## TO BUILD

### The primitive

    Commitment := scope (codes that must all be present)
                → expects (a code that should follow)
                + hits, misses

- **It fires when its scope is satisfied, and is then right or wrong about
  something specific.** That is the entire difference from a count.
- **A prediction carries its provenance** — which commitments entailed it. `csharp`
  already built this and used it for cycle-checking; see its `Chain`.
- **Failure blames the provenance, not the world.** A commitment in many failures
  and few hits is the culprit — the same `together / seen` arithmetic, pointed at
  commitments instead of codes.
- **Repair is SPECIALISATION.** *Whenever X, expect Y* becomes *whenever X and Z,
  expect Y*, where Z is what most distinguishes the failures from the hits.
- **Action is EXPERIMENT** — act to test the commitment whose failure would be most
  informative. Interventional by construction; see `csharp`'s `Kind.Meddled`.
- **A goal is a commitment about a state that does not currently hold**, and
  planning is the attribution machinery run backwards.

---


### Step one, and nothing else until it runs

- **`Commitment`** — scope, expects, hits, misses. Fire when scope is a subset of
  the moment.
- **Blame** — rank the commitments that entailed a failed prediction by miss rate.
- **Repair** — mint a child with ONE added scope code, the one most present in
  misses and most absent in hits.
- **The world:** `csharp`'s plan already names it — *several cues arrive together
  and only some carry the outcome*. A broad commitment must specialise to survive.

### Look necessary and absent

- **NEGATION IN A SCOPE, AND IT MAY NOT BE OPTIONAL.** *Whenever X and NOT Z* — and
  the distinguishing feature between a failure and a hit is very often that
  something was ABSENT. Refused in step one; expect to need it.
- **Sequence in a scope** — *X then Y* rather than *X and Y*. `csharp`'s `Kind.After`
  is the shape.
- **Roles in a scope** — a condition naming no argument is what buys transfer. See
  `csharp`'s `Kind.Role`, which is the one part of its edge vocabulary worth keeping.
- **A commitment ABOUT commitments** — metacognition, and where a self-model would
  start.
- **What to do when a commitment cannot be saved by any single added condition.**
  Two conflated cases with no distinguishing code present is the signal that a NEW
  code is needed — which is positing with a reason.
- **AND THAT SIGNAL CAN BE AIMED AT THE FRONT END.** A failure nothing present
  explains is a localised demand for RESOLUTION: winnow these moments finer. It
  closes the loop to perception, which every system in this family left open.

### Known limits, carried as work rather than discovered later

- **THE SCOPE LANGUAGE IS THE CEILING.** Whatever a scope cannot say, the system can
  never learn — this is ILP's language-bias problem and it is what killed the field.
- **QUANTISATION BOUNDARY NOISE IS THE INTERFACE RISK, AND REPAIR AMPLIFIES IT.**
  Two identical worlds either side of a band emit unrelated codes, so a coding
  artifact reads as a failure and specialising on it MINTS THE ARTIFACT.
- **Counting degrades gracefully here and repairing does not.** `csharp` splits a
  boundary across two cells and averages; this fragments. Said out loud as what the
  change COSTS.
- **`Winnow` IS THE DEFENCE AND IT IS MOUNTED NOWHERE.** Overlapping winner sets mean
  near-identical readings share most of their codes, so a scope that is a SUBSET
  still fires and the boundary stops being a cliff. Population coding.
- **What graded codes cost is SEARCH** — many more possible scopes, and blame over
  more of them. Measure it rather than assuming the robustness is free.
- **A miss could be PARTIAL, weighted by overlap.** Either elegant or a way to make
  everything mushy. Unmeasured.
- **Blame diffuses when many commitments entail one prediction.** The historical
  failure. Keep predictions shallowly entailed until it is measured.

---

### What comes over from `csharp`, and what does not

**Bring — the substrate, which is architecture-independent and proven.**

| | |
|---|---|
| `Agreed`, `Seeds` | The hash and the seed discipline. Load-bearing for the red-ball property |
| `Code` | The identity type |
| `Bus`, `Ring`, `Addresses` | The distributed half. Storage behind it is replaced |
| `IQuantizer`, `Coded`, `Winnow`, `Grains`, `Banded`, `Passthrough` | Front ends. Independent of what consumes them |
| `LiveSet`, `Window` | Moments and the stream |
| `Measurement`, `Questioned`, `Measured`, `Sweep`, `Plumbing`, `Seeds.Apart` | The measurement harness |
| **`DocsTests`, `DeadCodeTests`, `DuplicationTests`, `DialTests`** | **The budgets. Bring these FIRST** |
| The traps list and the refutation-table discipline | The epistemics engine, and the most valuable thing in the repo |
| Every world | A world is a PROBLEM and problems outlive architectures |

**Leave — all of it is rung-one machinery built on counting co-occurrence.**

`Node`, `Edge`, `Kind`, `Tie` · `Thought`, `Message`, `Arrival`, `Question`,
`WalkSettings` and every dial on it — `Accumulate`, `Pricing`, `Toll`, `Fanout`,
`Doubt`, `Row`, `Span` · `Chunk`, `Macro`, `Stated`, `Posit` · `Drives`,
`Foresight`, `Consequence`, `Reflection`.

**Bring the IDEAS out of the minters without the mechanisms.** `Paying`'s two bars —
description length AND beating chance, because MDL alone minted 715 names on pure
noise. `Stated`'s star-not-a-clique. `Macro`'s sorted-versus-ordered naming.
`Kind.Role`'s argument that a cell naming no argument is what transfers.

---

### What the field already knows

**Borrow the problem, not the mechanism.** This is not a new idea and pretending
otherwise would waste months.

- **DreamCoder** (Ellis et al., PLDI 2021) — grows its own library of abstractions
  under MDL pressure, and BOOTSTRAPS: learns `filter`, uses it to learn `max`, then
  `nth largest`, then `sort`. The existence proof for representation-as-residue.
- **Popper / Learning From Failures** (Cropper & Morel, 2021) — generate, test,
  **constrain**. A failed hypothesis yields constraints that prune the space. This
  design's core loop, already formalised.
- **XCS** (Wilson) — the innovation that made classifier systems work was separating
  credit assignment from selection and making fitness **accuracy-based rather than
  strength-based**, because strength-based systems delete low-reward rules that are
  still correct in their niche. Do not repeat that.
- **Why none of it scaled**: noise sensitivity, hand-specified language bias, and no
  way to learn from probabilistic or sensory background knowledge. Neurosymbolic ILP
  is the live attempt.
- **AND THE FAILURE WAS AT THE INTERFACE WITH PERCEPTION, NOT IN THE LOGIC** — which
  is the one place this project is unusually well placed, because its substrate
  manufactures symbols. That is the bet, said plainly.

---

## DO NOT RE-TRY

**A refutation is conditional on its configuration, so a row without a revival
condition is a superstition.** These carry from `csharp` and from the literature;
its own table holds thirty more that are about the walk and do not apply here.

| what | what refuted it | what would revive it |
|---|---|---|
| Strength-based fitness for rules | XCS: it deletes low-reward rules that are still CORRECT in their niche. Accuracy-based fitness is the fix | Never. Score a rule by how well it predicts, not by what it earns |
| MDL alone as a minting gate | On `csharp`'s `Motif` the pure-noise control minted 715 names against structured 245 | Never alone. Pair it with beating chance |
| A minted name joining the occasion it completes | Its members are gone, so its only partner is its own last member. Broke two controls on `Rhythm` | A name reached by inference, never written as a partner |
| Hand-specified language bias | ILP's own post-mortem: mode declarations are where the human puts the answer in | A scope language the failures themselves extend |
| Clusters by modality | Splits picture from sound, the one link this design exists to make | Never |
| A trained quantiser fitted per machine | Two machines fitted on different samples code the same input differently | A codebook reaching the same answer from any sample ORDER |

---

## TRAPS

**Named so nobody reintroduces them.** These are about MEASUREMENT, so they survive
the change of architecture entirely. `csharp`'s list holds a dozen more.

- **A check can be wired and unable to fire**, which reads as passing. Arm anything
  that has always read zero.
- **A dial can be declared, documented, passed everywhere and connected to
  nothing.** Every run reports `Complaints`; read them.
- **A fallback is a control arm nobody meant to run** — silence drifts an arm toward
  the random bar for free. Report silence beside the score.
- **A ranking arm needs something to rank, AND ITS STATISTIC MUST DISAGREE WITH THE
  CONTROL'S.** Two comparable routes outsum one, so `Agreement` and `Sum` ordered
  alike everywhere and four sessions read a tautology as a bug.
- **Measure one mechanism ON from a known baseline, never one OFF from all-on.**
- **A small sample can look like a mechanism, AND IT HIDES A REAL EFFECT TOO.** Count
  seeds in both directions.
- **A number in a commit message is a claim, not a record.**
- **A dial can be wired to ONE WORLD IN TEN**, and cashed in citing a finding as
  though it were general.
- **A CORPUS CAN CONTAIN ITS OWN ANSWER, and then a score measures the leak.**
- **Two arms can peak at different budgets.** Compare PEAK TO PEAK.
- **AND A MECHANISM CAN BE RIGHT AND ITS OBVIOUS WIRING WRONG.** Minting a name is
  not the same decision as where the name goes; `csharp` broke two controls learning
  that.

---

## OPEN DEFECTS

- *(none — nothing is built yet. The first entry here should be a thing measured and
  not understood, never a thing not yet attempted.)*

---

## FORK NUMBERS THE CODE CITES

**Never renumbered** — `DocsTests` asserts each resolves.

**Inherited whole from `csharp` and NOT renumbered.** Most concern the walk and go
with it; they stay listed because that code is still on this branch until it is
stripped, and a number that stops resolving is how a citation rots.

| | |
|---|---|
| **1** | The distributed rendezvous. Open, and inherited unchanged |
| **3** | Cluster placement: uniform hash against prefix locality. Open |
| **5** | A death writes off routes into the dead cluster. Closed |
| **6** | Broadcast the origin, route the hops. Closed |
| **11** | A finished thought is published and routed by code, so N actuators act on one broadcast. Closed |
| **12** | A fixed seed reproduces a run exactly. Closed — REOPENED and reclosed 2026-08-05; `Receive` folded arrivals in delivery order |
| **18** | Prediction conditional on the next action. Answered by edge kinds |
| **20** | Split budgets — deep to act, shallow to predict. Closed |
| **21** | Compression as an edge. A trade; off |
| **22** | A transiently-zero live count dropped later reports. Closed |
| **23** | Compression self-regulating? Not on any signal found yet |
| **24** | Budget controller aims at a moving target. Deleted |
| **25** | The binding world — built to fail, failed as predicted, since lifted |
