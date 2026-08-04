# Where this is going

- **The only doc. `DocsTests` holds a word TARGET, a hard CEILING and a PROSE
  cap.** To add something, retire something — and prefer cutting prose.
- **Let it drift while a session runs; compact in ONE pass at the end** — John,
  2026-08-04. Finishing a task is not the end of a session, and **the ceiling is
  what stops drift becoming permanent.**
- **What every piece does lives in the XML comments**, where the compiler enforces
  every reference. **Forward-facing, no results**: findings live in the commit, the
  comment beside the mechanism, and the test that asserts them.

---

## The goal

- **Understand rather than perform** — answer *what would the world look like if
  I did X*, which a sequence model cannot be.
- **Learning is a co-occurrence count.** Everything else is plumbing around it.
- **C1**: no node reads another's data. **C2**: messages are late, jittered, out
  of order. **C4**: no episode boundary, so nothing may depend on train-then-test.

---

## What is standing

- **Ten worlds sharing no world logic, over one `Fabric`** — including binding,
  built to be unanswerable and since lifted, and **two nobody here designed**,
  `Babi` and `Clevr`, which cannot flatter this architecture. `ScoreboardTests`
  runs them in one table; `corpora/fetch.sh` fetches rather than vendoring.
- **THE ARM THAT BEAT RANDOM IS NO LONGER ONE WORLD OLD, AND IT DID NOT
  TRANSFER.** `Tending` is a second body — plants dry, water reaches only what is
  underfoot and lands a step late — and `Kind.Helped` there is indistinguishable
  from acting at random. **Not because it ranks badly: because it is SILENT on
  nearly every step.** More states, and a credit cell keyed on the state that
  earned it covers almost none of them.
- **An occasion is a SET**, so *red ball beside blue box* and *blue ball beside red
  box* were one input — a ceiling that was **representational, not scale**.
  `Occasion.Groups` lifts it.
- **The graphs are tiny.** Nothing has run at a size where its claims could break;
  hold them loosely.

### THE BET THE WHOLE DESIGN RESTS ON — name it, do not re-argue it

- **COUNTS ONLY EVER RISE.** The G-Counter property, and it buys convergence with
  no coordinator — which is what buys C1 and C2.
- **The price is that nothing can be unlearned, only outvoted.** `Kind.Hindered`
  buys back *contradiction* and not *forgetting*: a PN-Counter's halves both rise.
- **Eviction and cold storage are the escape hatches, and BOTH ARE UNBUILT.** If
  forgetting is necessary rather than optional, this is the expensive thing to walk
  back. **Nothing so far says it is.**

### FOUR THINGS THE FRONT END IS HANDED, AND NOBODY WAS COUNTING THEM

- `Occasion.Groups` — which codes belong to which object. `Occasion.Sequence` —
  what came first. `Occasion.Fleeting` — this code will never recur.
  `HomeostatSettings.Ranked` — where each variable stands against the others.
- **Each is defended in its own file and the total is defended nowhere.** Every one
  tests whether the graph can USE a fact, never whether it can DISCOVER it.
- **`Ranked` is the one to watch**: *lowest of the four* is relational, and
  relational facts are what this design exists to learn.
- **A fifth needs an argument against the other four**, not just for itself.

### ONE WEIGHT DOING TWO JOBS IS THIS DESIGN'S RECURRING FAULT

- **IT HAS BITTEN FOUR TIMES.** A weight both RANKS a partner and PRICES the hop.
  `Pricing.Sender` moves the ranking to move the price; `Doubt` destroys the senses
  world applied to both and repairs a real defect applied to the score alone;
  negative evidence muted the walk until it too was kept off the price.
- **`Doubt` SPLIT THE ARITHMETIC AND LEFT THE STATISTIC** — believed and cost still
  read one number, so evidence set the budget. **`Toll` splits the statistic**:
  `Traffic` charges `1 + log₂(entries)`, what the hop costs in messages.
- **AND IT FOUND THAT THE CONTROL DEGENERATES.** Where every weight is exactly
  one, `1 / weight` IS the refuted constant cost and growth on a clique is
  factorial. **The `StepCost` row's revival condition, met.**
- **A dial wanting different values in different worlds is the same fault.** Split
  it or fuse the arms rather than sweep; `DialTests` records which channel each
  dial may move.
- **A conflict between two dials may be a BUDGET ARTIFACT** — `Clevr` was one; the
  trap is in TRAPS.

---

## NEXT

**The binding world is the scoreboard. Do not build a new world to test a change
to an old one — run that one.** *(But check it can SEE the change: at chance or at
its ceiling it absorbs anything.)*

### 1. RANKING BELONGS TO THE QUESTION — moved onto `Question`, and the dial is gone

- **Merging routes AT A NODE is the version `Narrowed` could not do** — reading the
  index back puts the referent in the machine's hands. Needs a wait: dear under C2.
- **`Pricing` IS HAND-SET AND A CONTROLLER IS REFUTED. The missing thing is the
  SIGNAL** — fork 23 again.

### 2. Predictive coding — only surprise propagates

- Rao & Ballard, Friston. An expected onset is silent. **Built.**
- **THE OBSERVATION IS SUPPRESSED, NOT THE PREDICTION** — the walk making the
  expectation still runs. **Prediction conditional on surprise is the deeper
  version.**
- **AND THE WRITE PATH IS NOT GATED AT ALL.** The stated reason — *the expectation
  would decay* — **does not hold where nothing decays**. Rescorla and Wagner:
  learning tracks ERROR, not frequency, and `Occasion.Weight` is the channel.
  **The payoff is cost** — it silences the WRITE path as step 2 silenced thinking.

### 3. Chunking — MDL. **`Chunk` is built, and it is a trade**

- **It mints a NODE where fork 21 mints an edge**, so the alphabet GROWS where the
  quantiser fixed it forever. **The name is DERIVED from the sorted members**, so
  two machines agree with nothing to ask — the only minting C1 permits. **The
  threshold is description length**, not a constant.
- **IT BUYS THE TRAFFIC AND NOTHING ELSE.** The graph gets BIGGER and accuracy
  costs a little; the sets are a tiny share of a row count the noise dominates, so
  MDL's storage half never shows.
- **A MINTED NODE IS A HUB BY CONSTRUCTION and `Pricing.Receiver` refuses hubs** —
  the likely reading of that cost. **`Toll.Traffic` tests it**: a chunk should
  become dear to ENTER and still believed.
- Open: **only a WHOLE moment is a candidate**, so a set inside a larger one is
  invisible — pair-merging (Sequitur, BPE) composes. And the **utility problem**
  (Minton, SOAR): utility belongs per chunk.

### 4. Homeostatic drives — Ashby. **The arm beats random**

- Bounded internal variables make behaviour goal-directed **with no reward
  function**, survival having proved gameable. **No episode boundary**, fitting C4.
  **AND EVERY ACT PAYS OFF AT ONCE, so this world cannot see steps 7, 10 or 11.**
- **`Homeostat` SETS THE BAR, AND THE BAR IS RANDOM.** Attending to whatever is
  lowest holds the body, so the world is winnable only by looking at it.
- **SO ASSOCIATION IS ANTI-CORRELATED, not merely uninformative.** A count of what
  was done converges on the POLICY that did it, and the states a body is in are the
  ones its own actions produced. **This waits anywhere the graph learns from
  itself.**
- **THE CREDIT NEEDED ITS OWN CELL, AND THAT IS THE FIRST ARM TO BEAT THE BAR.**
  Three earlier arms wrote a HEAVIER number into the cell meaning *this was done
  here*. `Kind.Helped` is a SECOND statistic, walked alone; `Marked` is the
  control. **Nothing is punished, both counts only rise.**
- **ITS VIRTUE IS RARITY — four arms since say so.** Every second cell written
  DENSELY collapses back to the behaviour policy, whatever it claims to mean.
  **And rarity is also why it does not survive a second world**: too rare to cover
  the states.
- **WHAT IS LEFT IS NOT INEXPERIENCE.** The arm is silent for most steps, and
  **quadrupling the run moves neither silence nor score.** The state count grows,
  so a cell keyed on the state that earned it never covers them.
- **AND THE SILENCE IS THE PRICE OF THE CONTRAST — step 9 settled that.** Anything
  wide enough to cure it re-introduces the policy. **Step 8, then step 10.**

### 5. WHAT A CO-OCCURRENCE COUNT STRUCTURALLY CANNOT DO — John, 2026-08-03

**Consequences of the design, not missing features. Ordered by cost.**

- **ABSENCE — BUILT AS A SIGNAL, AND WIRED.** `Surprise` returns both halves of
  the signed error, and `Overreach` tells a solved world from a predictor naming
  everything. **A SIGNAL, not a node**: minting `not-X` would double an alphabet
  to hold unboundedly many absent things.
- **SUPERSESSION — THE CHANNEL IS BUILT, NOTHING READS IT.** `Tie.When` rides
  beside the count as an LWW-Register and **the two must never merge**; `Tie` holds
  why. **Do not decay.** **Both consumers are left**: recency ranking on
  `Question`, and eviction on "not touched since". Across machines it wants a
  Lamport clock.
- **MULTI-TOKEN OUTPUT.** *Simultaneous* actions are nearly free and **fork 11
  built the addressing**; what is left is a world that wants two.

### 6. EDGE KINDS — BUILT, and the row was widened once

- **A row entry is `(Code, Kind)` to `(count, when)`**, and supersession rode in
  beside it for the single price. `Kind` holds every argument in full.
- **The front end SAYS the order inside the occasion** — the `Groups` trick again,
  because a phase cannot survive C2. `Occasion.Sequence` is additive; splitting the
  window's carried edge moves measured counts, so that half is an arm.
- **`Question.Through` restricts a walk to one relation**: it answered fork 18 and
  walks step 4's credit cell. **One kind was the limit — see step 9.**

### 7. Credit over time — eligibility traces, and `Window` is already one

- **The gap: nothing learns that an act led somewhere good three steps later**,
  with no reward function and no backpropagation. **Three-factor Hebbian learning
  needs neither** (Izhikevich 2007): a fading trace of what recently fired, and a
  third signal consolidating what is still in it, most credit to the most recent.
- **`Window` IS that trace, ungated**, and `Kind.Helped` is the third factor it
  would consolidate. **The conjunction is NOT built** — `Drives.Feel` prices the
  previous transition and nothing longer. **Safe for the CRDT property**: the trace
  decides how much to add, and counts only rise. **`Tending` is where it shows.**

### 8. Similarity — **and step 9 promoted it from an option to the only route**

- **VARIABLE BINDING.** *A is north of B* is a count between two codes, so it
  cannot apply to a new A and B. **This un-parks vector-symbolic binding**;
  `Clutrr` and `gSCAN` are the worlds that would force it.
- **IT WAS BUILT ONCE, ON `master`, AND DID NOT CROSS THE REWRITE.**
  **`surfaces.py`** put near inputs on near codes by random-hyperplane LSH
  (Charikar), bits being the granularity dial. **`grounding.equivalence_classes`**
  walked mutual-top-k co-occurrence to components — the distributional hypothesis
  (Harris), and **step 9 refutes that half here**: a likeness read off the rows is
  made of co-occurrence. `Code.Prefix` alone survives, read by fork 3.
- **A FRONT END FOR REAL PERCEPTION — measured once as THE binding constraint, and
  now the only route to generalisation as well.** Codes must be identical on every
  machine forever, so a fitted codebook is out — **and a uniform hash is agreed and
  unwalkable**, because the data is concentrated where uniform codes are not.
  **Spending codes where the data is WITHOUT fitting one is the unbuilt middle.**
- **REPLAY.** Re-run experience when nothing is arriving: consolidates, learns
  from rare events, interleaves old against new. **`WhenIdle()` is the trigger.**
- **INHIBITION — EXPRESSIBLE, AND IT DOES NOT PAY YET.** "Punishment is
  unavailable" was **the wrong CRDT, not a law**: a **PN-Counter** is two
  G-Counters read as a difference. `Kind.Hindered` costs a KIND, not a wider row,
  and **must discount the SCORE and never the price**. **Still no better than the
  one-sided count** where most acts are wrong most of the time.

### 9. RELATIONAL PATHS — a walk of MIXED kinds, and the first attack on step 8

- **BUILT — `Question.Path` walks a relation per hop**, Path Ranking's move (Lao
  & Cohen). **The mechanism is right and the idea is refuted.**
- **A FIXED PATH TRADES COVERAGE AGAINST PRECISION WITH NO MIDDLE.** Through
  `With` it reaches almost everything and the credit dissolves; through `After` the
  composition is empty and the walk is silent at every window width. **And backoff
  settles it**: narrow FIRST, widening only on silence, overrides nothing and
  scores the same.
- **SO THE SILENCE IS THE PRICE OF THE CONTRAST, NOT A DEFECT.** Anything wide
  enough to be non-silent converges on what was DONE most — step 4's diagnosis,
  from a new direction.
- **WHICH MAKES STEP 8'S FRONT END THE ONLY ROUTE LEFT.** Every graph-internal
  likeness is made of co-occurrence and carries the policy with it; **a likeness
  the graph did not compute cannot be.**
- **`Question.Downstream` — the successor representation (Dayan) — remains
  unaskable**: it wants the REVERSE temporal edge, which a carried code does not
  write. **A real design question** — see `Kind.Before`.

### 10. A REASON TO SEEK — the drive this design has never had

- **`Drives` makes the system want to STAY ALIVE. Nothing makes it want to FIND
  OUT**, though `Surprise.Rate` is a quantity it reads about itself.
- **`Kind.Informed` is the shape**: a second write when an act produced something
  unpredicted — `Kind.Helped` with `Surprise` as the third factor, so no more a
  smuggled reward than Ashby's bounds are.
- **NOVELTY DECAYS WITH NOTHING DECAYING**: `informed / seen` falls on its own as
  an act becomes predictable, because the marginal keeps rising while the cell
  stops. **The anti-hub weighting already does it.**
- **ERROR IS NOT PROGRESS — the noisy television.** Seeking prediction error alone
  rewards whatever is least learnable; Schmidhuber's claim is about compression
  PROGRESS, and rarity is what a second cell needs.
- **IT REPLACES THE BOOTSTRAP.** The credit cell is empty until something has
  helped, so the arm is mostly its own coin toss — **and TRAPS calls a fallback a
  control arm nobody meant to run.**
- **BUILT, AND NOT MEASURABLE ON `Homeostat`: THE PREDICTION WALK RETURNS
  NOTHING THERE**, the same empty `after` that silenced step 9. So every moment
  reads as maximally surprising — the dense arm writes on nearly every step and
  collapses, the selective one can never fire. **Neither is evidence about
  curiosity.**
- **STEP 10 AND STEP 11 ARE BOTH BLOCKED ON A WORLD THAT CAN BE PREDICTED AT
  ALL.** That is the concrete case for the second body.

### 11. NOTHING HERE PLANS. Every action is a reflex

- **`Consequence` asks *what would the world look like if I did X*, ONE step, and
  stops.** No sequence held as a unit, no means-ends. **A world model that rolls
  forward once is an expensive reflex.**
- **The rollout needs nothing built.** `Foresight` already predicts the next frame
  from view-and-action; feed that prediction back as a synthetic occasion and ask
  again. Craik's small-scale model, Tolman's cognitive map, MuZero's search half.
- **The risk is compounding error**, which every learned simulator is worst at, so
  depth wants its own control.

### THE PATTERN UNDER ALL OF IT

- **None of this is a sufficiency argument** — all of it could land and still not
  be enough. The narrower claim: without structure, an internal error signal, a
  growing alphabet, a reason to act, a reason to seek, a way to plan, supersession,
  absence, concurrent output and a bounded row, scaling does not get there.

---

## TO BUILD — a ticked box means the type exists, and a test checks

- [x] `Chunk` — the minted node of step 3. **A trade; see step 3**
- [x] `Drives` — step 4's third factor. **Wanted its own cell; see step 4**
- [x] `Toll` — the weight split. **Measured against its control on spend, not
  on stamina; see the trap about one dial measured at another's setting**
- [x] `Question.Path` — step 9's mixed-kind walk. **Built, and the idea it was
  built for is refuted; see step 9**
- [ ] `Kind.Informed` — step 10's curiosity
- [x] `Tending` — the **second body**, and it does not confirm step 4
- [ ] The rollout of step 11
- [ ] Surprise on the write path — step 2's second half

---

## LATER — nothing is blocked on these

- **Fork 1 is smaller than it looks** — see the bet above. The counts need no
  protocol; only the join does.
- **The absolute message cost is what step 2 attacks**, and nothing else should be
  optimised first.
- **Cold storage, once a row can be bounded.** **Paging a node out keeps the CRDT
  property**: the count does not decrease, it stops being resident. Decay does not.
- **The knob pass, last.** A dial swept before the structural work measures a
  system about to change under it.

### The scaling wall — measure it before cutting anything

- **STEP ZERO IS TO BUILD SOMETHING BIG ENOUGH TO BREAK.** The largest graph here
  is a few thousand nodes, so any optimisation now aims at a wall nobody has hit.
  Full `Clevr` and the ten-thousand-story `Babi` reach far larger; **measure, then
  cut.** In order of leverage:
- **BOUND THE ROW.** Cap a node at K partners: *cost grows with data forever*
  becomes *cost is constant*, the trick approximate-nearest-neighbour indexes run
  at billions on. **Evict on "not touched since", never by eroding a count** — the
  `when` channel provides it, so supersession and scaling share one mechanism.
  Top-K in bounded memory is heavy-hitters; Space-Saving bounds the error.
- **A SELF-SET BEAM, whose revival condition is now met.** The refuted row asks a
  width the system sets itself; `Surprise.Rate` is one signal, a node's own row
  statistics a second.
- **Hierarchy, which is what step 3 is really for.** Do not walk a million nodes;
  walk a thousand chunks.

### The wire, when the remote half lands — John, 2026-08-03

- **Only the local half of `HybridBus` exists, so none of this is built.**
- **C2 IS STILL NOT TESTED, AND THE REASON IS THE HARNESS.** A held-back delivery
  is delayed inside the in-flight count, so `WhenIdle` does not fire, and **every
  reader here waits for quiet** — lateness becomes waiting, never
  acting-without-it. **The hard half needs a reader on a DEADLINE**, an anytime
  reader that acts on what has arrived, or two machines. **Established meanwhile**:
  bus, accounting and walk are unharmed by arrivals far out of order.
- **Coalesce a settling wave into one send.** Hold remote envelopes until local
  traffic drains, then one datagram per destination; `WhenIdle()` is the trigger.
  **Not a pure barrier** — flush on idle *or* size *or* time.
- **Bits, not JSON.** Modalities intern to small ints, a code is a varint, and
  **a sixth of a packed message is the `Guid` broadcast id** — shorten it per
  connection.
- **`Chain` is what costs** — cycle check and explanation in one field, free
  locally and not on a wire. **Split them:** an approximate-membership filter for
  the hop, full chain rebuilt at the origin.
- **Voting multiplies the wrong half.** **One thought, redundant reports**: the
  flood is the graph's, and what C2 loses is the return path.
- **UDP matches.** C2 assumes loss, and **TCP's head-of-line blocking would stall
  every thought behind one lost packet.** QUIC's unreliable datagram extension
  (RFC 9221) is the shape.

---

## DO NOT RE-TRY

**Three columns, one line each, enforced by a test. The third matters most: a
refutation is conditional on its configuration, so a row without a revival
condition is a superstition.**

| what | what refuted it | what would revive it |
|---|---|---|
| `StepCost.Best` / `Local` / `Constant` | Factorial message growth where inverse cost is polynomial | **MET by `Toll.Traffic`** — a bound not relying on positive cost at weight 1.0 |
| `Refuel` | Nothing is paid back, so it did nothing | Anything that returns budget to a route |
| Sender-*weighing*, `IMarginals` | A C1 violation, and behaviour was identical without it | Never. **But `Message.Seen`, the sender's OWN marginal, is legal and built** |
| Absolute actions, unrotated view | One move in four instantly fatal | A body with no heading |
| Survival as the score | Circling wins: it lives longest and eats least. **Snake cannot discriminate policies at all** | Homeostatic drives, where standing still stops paying |
| A beam over partners | A constant nobody set, doing the cutting | A width the system sets itself and reports |
| Clusters by modality | Splits picture from sound — the one link this design exists to make | Never |
| Clusters by time of creation | Two machines compute different owners for one code | Placement agreement without a coordinator |
| `Adaptive` reflection on `Hunger` | Inverted: it wrote most where it helped least | A signal that discriminates; `Thwarted` does |
| A deeper walk for prediction | Monotonically worse — without edge kinds, deeper reaches more and ranks worse | **Edge kinds**, and that refutation reproduced |
| `ArrivalValue.Lift`, `Accumulate.Max` | Swept, inert, both explanations refuted, `Max` worse where its revival condition pointed. **Both DELETED** | Lift in the **cost** |
| Naming fewer predicted codes | Half true: coarse ranking informs, fine does not | **REVIVED at one code** — naming as many as the frame holds swamps the action |
| `Window` span | Null on snake, WORSE on `Babi` at far more traffic, and **the whole task on `Rhythm`**. **Revival RUN and half-held**: kinds recover much of both, and carrying still loses | **Something that makes a carried edge worth its row.** Kinds were the structural half and are not enough |
| `includeEmpty: true` | Ruinous under `Best` pricing | **Revived — no clear winner since** |
| `Pricing.Balanced` — cosine's denominator | **Times out.** The geometric mean sits BETWEEN the marginals, so weights rise and the walk explodes | A bound not relying on the weight being one marginal's reciprocal |
| `Pricing.Driven` — the node picking the marginal per hop | **Two local rules, both worse than the hand-set arm in BOTH worlds**: a per-hop choice puts routes on different scales and the ranking stops meaning anything. **The premise was wrong — on `Babi` the better arm is the DEARER one** | A local quantity predicting which arm wins, on a world where they differ: `Senses` scores them alike |
| `Accumulate.Fused` — rank fusion over the two orders | Half of agreement's lift and all its cost. **Two candidates whose orders invert tie identically under RRF for every damping constant** | Many candidates, or a fusion separating by something other than position |
| Inhibition on `Homeostat` | Folding the negative cell into the COUNT mutes the walk — that number is the ranking AND the price. Discounting the SCORE alone recovers most of it and **still loses to the one-sided count**, where most acts are wrong most of the time | A world where the wrong act is informative and RARE. **The PN-Counter itself is sound and stays** |
| `Ranked` as step 4's fix | **The lift was the bootstrap's coin toss** — silent far more often, and a varying code thins every edge so routes starve before reaching an action. **Kept: it lifts a real ceiling, and buys nothing alone** | Anything making the walk prefer a partner other than the one it took last time |
| Widening the walk to cure step 4's silence — `Kindred`, `Foreseeing`, `Backing` | **All three, and backoff is what settles it.** Through `With`: louder and below the bar. Through `After`: silent on every step at every window width. Asking narrow FIRST and widening only on silence overrides nothing and scores the same — **so the widening costs even where it overrides nothing.** A walk wide enough to be non-silent converges on what was DONE most | **A likeness the GRAPH DID NOT COMPUTE.** Every graph-internal one is made of co-occurrence and carries the behaviour policy with it, so this is step 8's front end and nothing short of it |
| A trained quantiser — k-means on `master` | Two machines fitted on different samples give the same input different codes, and nothing downstream detects it | Never fitted. **A hash that spends its bits where the data is, without being fitted, is step 8's unbuilt middle** |

---

## TRAPS

**Closed in code, named so nobody reintroduces them:** consecutive integer seeds
are not independent (`Seeds.Apart`); `Measured.Separation` returned zero where
repeated measurement found no spread (infinity now); `WhenQuiet()` was not a
finish signal (`WhenIdle()`); walks were read before they had finished (fork 22,
`Unsettled` on `Measurement`). **Voting survives that fix and buys nothing in one
process** — kept, because a real network loses reports.

**Live:**

- **A DIAL MEASURED AT ONE SETTING OF ANOTHER MAY BE MEASURING THAT ONE.** The
  stamina plateau reversed between short and long runs. **Sweep at two run lengths,
  never with a third pinned.** **`Toll` is the sharpest case**: it changes what a
  unit of `Stamina` BUYS, so its arms compare on spend and nothing else.
- **Short runs on the binding world score above chance for RECENCY ALONE.**
- **A dial can be declared, documented, passed everywhere and connected to
  nothing.** `ThinkAsync`'s stamina was, and survived three measurements.
  **Every run reports `Complaints`; read them.**
- **A CHECK CAN BE WIRED AND UNABLE TO FIRE, which reads as passing.** Two worlds
  counted `unbalanced` off a variable nothing incremented; step 10's selective arm
  reproduced its control down to the edge count. **Arming a check that has always
  read zero is the only way to tell the two apart.**
- **A FALLBACK IS A CONTROL ARM NOBODY MEANT TO RUN.** Where an arm acts at random
  when the walk says nothing, anything raising its silence drifts it toward the
  random bar for free, and that reads as the change working. **Report silence
  beside the score** — step 9 is louder AND worse, which neither number says
  alone. It cannot carry an arm PAST the bar, though; that much is arithmetic.
- **A small sample can look like a mechanism.** One seed of step 4's credit arm
  showed a clean monotone learning curve that six seeds flattened to nothing.
- **A MEAN OVER A POPULATION THE PROBLEM CREATED CANNOT SEE IT.** Fan-out stayed
  flat where rows grew without bound, because the growth mints tiny nodes that
  hold the average down. **Read `Widest`.**
- **Copies drift where nothing fails**, and a difference between two moves a
  headline without failing a test. `DuplicationTests` is the budget now.
- **THE TEST SUITE IS SERIAL ON PURPOSE.** Parallel, the walk's agreement with
  itself measured perfect where alone it did not — load removes the ordering that
  produces the disagreement, so parallelism HID a defect. `Parallelism.cs`.

---

## OPEN DEFECTS

**Nothing outstanding.**

---

## FORK NUMBERS THE CODE CITES

**Never renumbered** — `DocsTests` asserts each resolves. Closed forks stay listed
because the code still points at them.

| | |
|---|---|
| **1** | The distributed rendezvous. **Open** — not needed until a second machine; see the CRDT note |
| **3** | Cluster placement: uniform hash against prefix locality. **Open** |
| **5** | A death writes off exactly the routes heading into the dead cluster. Closed |
| **6** | Broadcast the origin, route the hops. Closed |
| **11** | A finished thought is PUBLISHED and the bus routes it by code, so N actuators act on one broadcast. **Closed — no world runs two yet** |
| **12** | A fixed seed reproduces a run exactly, `Halted` included. Closed by 22's fix |
| **18** | Score prediction **conditional on the next action**. **ANSWERED BY STEP 6** — action ordered first, prediction asking what follows, actuator what preceded |
| **20** | Split budgets — deep to act, shallow to predict. Closed |
| **21** | Compression. A trade: it pays where the budget cannot compose and costs where it can. Off |
| **22** | A transiently-zero live count untracked thoughts mid-flight and dropped every later report. `Retire` asks twice. Closed |
| **23** | Compression self-regulating? Not on this signal. `Thwarted` is the right shape and swings too little |
| **24** | Budget controller converges from both directions and **aims at a moving target**. Off by default |
| **25** | The binding world — built to fail, failed as predicted, and since lifted |
