# Where this is going

- **The only doc. `DocsTests` holds a word TARGET, a hard CEILING and a PROSE
  cap.** To add something, retire something — and prefer cutting prose.
- **Let it drift while a session runs; compact in ONE pass at the end** — John,
  2026-08-04. Not one pass per piece of work: finishing a task is not the end of
  the session. **The ceiling is what stops drift becoming permanent.**
- **What every piece does lives in the XML comments**, where the compiler enforces
  that every reference resolves.
- **Forward-facing, no results.** Findings live in the commit, the comment beside
  the mechanism, and the test that asserts them.

---

## The goal

- **Understand rather than perform** — answer *what would the world look like if
  I did X*, which a sequence model cannot be.
- **Learning is a co-occurrence count.** Everything else is plumbing around it.
- **C1**: no node reads another's data. **C2**: messages are late, jittered, out
  of order. **C4**: no episode boundary, so nothing may depend on train-then-test.

---

## What is standing

- **Nine worlds sharing no world logic, over one `Fabric`** — including binding,
  built to be unanswerable and since lifted; composition, where the answer was
  never observed; and **two nobody here designed**, `Babi` and `Clevr`.
  `ScoreboardTests` runs them in one table.
- **AN EXTERNAL WORLD CANNOT FLATTER THIS ARCHITECTURE.** `corpora/fetch.sh`
  fetches; nothing is vendored.
- **THE ARM THAT BEATS RANDOM IS ONE WORLD OLD** — `Kind.Helped` is measured on
  `Homeostat` alone, and no other world has a body to try it on.
- **An occasion is a SET**, so *red ball beside blue box* and *blue ball beside red
  box* were one input — a ceiling that was **representational, not scale**.
  `Occasion.Groups` lifts it.
- **The graphs are tiny.** Nothing has run at a size where its claims could break;
  hold them loosely.

### ONE WEIGHT DOING TWO JOBS IS THIS DESIGN'S RECURRING FAULT

- **IT HAS BITTEN THREE TIMES.** A weight both RANKS a partner and PRICES the hop,
  and improving one wrecks the other. `Pricing.Sender` moves the ranking to move
  the price; `Doubt` destroys the senses world applied to both and repairs a real
  defect applied to the score alone; **negative evidence muted the walk until it
  too was kept off the price** — 2026-08-04, with `Doubt`'s own comment sitting
  above the line where it was got wrong again.
- **The general move: find the number serving two masters and split it.** The row
  entry still ranks AND prices with one number; that split is the outstanding one.
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

### 1. Composition over bindings — closed by `Accumulate.Agreement` and `Refer.Narrowed`

### 1a. RANKING BELONGS TO THE QUESTION — moved onto `Question`, and the dial is gone

- **Merging routes AT A NODE is the version `Narrowed` could not do** — reading
  the index back puts the referent in the machine's hands, where a node combining
  concurrent routes keeps it in the graph. Needs a wait: expensive under C2.
- **`Pricing` IS STILL HAND-SET AND A CONTROLLER FOR IT IS REFUTED.** **What is
  missing is not the controller but the SIGNAL** — fork 23's lesson a third time.

### 2. Predictive coding — only surprise propagates

- Rao & Ballard, Friston. An expected onset is silent. **Built.**
- **THE OBSERVATION IS SUPPRESSED, NOT THE PREDICTION** — the walk making the
  expectation still runs, so the saving is partial. **Prediction itself conditional
  on surprise is the deeper version.**

### 3. Chunking — MDL. **`Chunk` is built, and it is a trade**

- **It mints a NODE where fork 21 mints an edge**, so the alphabet GROWS where the
  quantiser fixed it forever. **The name is DERIVED from the sorted members**, so
  two machines agree with nothing to ask — the ring's trick, and the only minting
  C1 permits. **The threshold is description length**, not a constant.
- **IT BUYS THE TRAFFIC AND NOTHING ELSE.** Per completion it falls threefold on
  `Motif`, the number step 3 was set. **But the graph gets BIGGER and accuracy
  costs a little**: the sets are a tiny share of a row count the noise dominates,
  so MDL's storage half never shows.
- **A MINTED NODE IS A HUB BY CONSTRUCTION and `Pricing.Receiver` refuses hubs** —
  the likely reading of that cost, unverified. `Pricing.Sender` tests it.
- Open: **only a WHOLE moment is a candidate**, so a set inside a larger one is
  invisible — pair-merging (Sequitur, BPE) composes. And the **utility problem**
  (Minton, SOAR): utility belongs per chunk.

### 4. Homeostatic drives — Ashby. **The arm beats random**

- Bounded internal variables make behaviour goal-directed **with no reward
  function**, survival having proved gameable. **No episode boundary**, fitting C4.
- **`Homeostat` SETS THE BAR, AND THE BAR IS RANDOM.** Attending to whatever is
  lowest holds the body indefinitely, so the world is winnable only by looking at
  it. An arm needs a bootstrap, since an action enters the graph only by being
  taken — **and every point the plain arm scored came from that coin toss.** Quiet
  it with budget and the score falls to *idling's exactly*.
- **SO ASSOCIATION IS ANTI-CORRELATED, not merely uninformative.** A count of what
  was done converges on the POLICY that did it, and the states a body is in are the
  ones its own actions produced — so the walk reaches what was done last time here,
  which is exactly what failed to prevent here. **Contiguity is not contingency**
  (Rescorla–Wagner), and this waits anywhere the graph learns from itself.
- **THE CREDIT NEEDED ITS OWN CELL, AND THAT IS THE FIRST ARM TO BEAT THE BAR.**
  Three earlier arms wrote a HEAVIER number into the cell meaning *this was done
  here*, deepening the groove. `Kind.Helped` records *and things got better* as a
  SECOND statistic and `Question.Worthwhile()` walks that alone, so ranking is the
  share of times an act helped. **Nothing is punished, both counts only rise.**
- **THE CONTRAST IS WHAT DID IT, and `Marked` says so**: same cell, equally stale,
  written unconditionally, and it loses to the bar. **`Ranked` now pays too**,
  having bought nothing alone — necessary, not sufficient.
- **WHAT IS LEFT IS NOT INEXPERIENCE.** The arm is silent most of the time and
  short of the ceiling, and **quadrupling the run moves neither.** The state count
  keeps growing, so a credit cell keyed on the state it was earned in never covers
  them, and nothing carries what was learnt in one state to a similar one.
  **Step 8, from a third direction.**
- **THE FRONT END WAS A REAL CEILING AND IS NOT THE CAUSE.** *Attend to whichever
  is lowest* is relational and a band is absolute: two states with identical bands
  can have opposite answers, and the ceiling policy held the body so still it
  visited ONE state. `Ranked` lifts both, at 27x the messages.

### 5. WHAT A CO-OCCURRENCE COUNT STRUCTURALLY CANNOT DO — John, 2026-08-03

**Consequences of the design, not missing features. Ordered by cost.**

- **ABSENCE — BUILT AS A SIGNAL, AND WIRED.** `Surprise` returns both halves of
  the signed error, and `Overreach` tells a solved world from a predictor naming
  everything — the one failure step 2 can cause rather than measure. **A SIGNAL,
  not a node**: minting `not-X` would double an alphabet to represent unboundedly
  many absent things. `Rhythm` complains when it happens.
- **SUPERSESSION — THE CHANNEL IS BUILT, NOTHING READS IT.** `Tie.When` rides
  beside the count as an LWW-Register and **the two must never merge**; `Tie` holds
  why. **Do not decay.** **Both consumers are left**: recency ranking on
  `Question`, and eviction on "not touched since". Across machines it wants a
  Lamport clock.
- **MULTI-TOKEN OUTPUT.** *Simultaneous* actions are nearly free and **fork 11
  built the addressing**; what is left is a world that wants two. *Ordered*
  sequences need edge kinds.

### 6. EDGE KINDS — BUILT, and the row was widened once

- **A row entry is `(Code, Kind)` to `(count, when)`**, and supersession rode in
  beside it for the single price. `Kind` holds every argument in full.
- **The front end SAYS the order inside the occasion** — the `Groups` trick again,
  because a phase cannot survive C2. `Occasion.Sequence` is additive; splitting the
  window's carried edge moves measured counts, so that half is an arm.
- **`Question.Through` restricts a walk to one relation**: it answered fork 18 and
  is how step 4's credit cell is walked.
- **`Rhythm` cannot measure this**: nothing there is ever simultaneous, so every
  cell is already temporal and splitting them is an isomorphism.

### 7. Credit over time — eligibility traces, and `Window` is already one

- **The gap: nothing learns that an act led somewhere good three steps later**,
  with no reward function and no backpropagation. **Three-factor Hebbian learning
  needs neither** (Izhikevich 2007): a fading trace of what recently fired, and a
  third signal consolidating what is still in it, most credit to the most recent.
- **`Window` IS that trace, ungated**, and `Kind.Helped` is now the third factor
  it would consolidate. **Safe for the CRDT property**: the trace is transient
  state deciding how much to add, and counts only rise.

### 8. Also likely necessary

- **VARIABLE BINDING.** *A is north of B* is a count between two codes, so it
  cannot apply to a new A and B. Without it every generalisation runs through
  similarity. **This un-parks vector-symbolic binding**; `Clutrr` and `gSCAN` are
  the worlds that would force it.
- **REPLAY.** Re-run experience when nothing is arriving: consolidates, learns
  from rare events, interleaves old with new against interference. **`WhenIdle()`
  is the trigger already**, and fork 21 is its cousin.
- **A FRONT END FOR REAL PERCEPTION — measured once as THE binding constraint**,
  and every world here is symbolic, so nothing has hit that wall since. Codes must
  be identical on every machine forever, so a fitted codebook is out — **and a
  uniform hash is agreed and unwalkable**, because the data is concentrated where
  uniform codes are not. **Spending codes where the data is WITHOUT fitting one is
  the unbuilt middle.**
- **INHIBITION — EXPRESSIBLE, AND IT DOES NOT PAY YET.** "Counts only increment,
  so punishment is unavailable" was **the wrong CRDT, not a law**: a **PN-Counter**
  is two G-Counters read as a difference, each monotonic. `Kind.Hindered` is the
  negative half and costs a KIND, not a wider row. **It must discount the SCORE
  and never the price.** **Still no better than the one-sided count** where most
  acts are wrong most of the time.

### THE PATTERN UNDER ALL OF IT

- **None of this is a sufficiency argument.** All of it could land and still not be
  enough. The narrower claim: without structure, an internal error signal, a
  growing alphabet, a reason to act, supersession, absence, concurrent output and a
  bounded row, no amount of scaling gets there.

---

## TO BUILD — a ticked box means the type exists, and a test checks

- [x] `Chunk` — the minted node of step 3. **A trade; see step 3**
- [x] `Drives` — step 4's third factor. **Wanted its own cell; see step 4**

---

## LATER — nothing is blocked on these

- **Fork 1 is smaller than it looks.** A count that only increments is a
  **G-Counter** and converges under reordering and loss with no coordination. The
  counts need no protocol; only the join does.
- **The absolute message cost is what step 2 attacks**, and nothing else should be
  optimised first.
- **Cold storage, once a row can be bounded.** **Paging a node out keeps the CRDT
  property** — the count does not decrease, it stops being resident. Decay does not.
- **The knob pass, last.** A dial swept before the structural work measures a
  system about to change under it.

### The scaling wall — measure it before cutting anything

- **STEP ZERO IS TO BUILD SOMETHING BIG ENOUGH TO BREAK.** The largest graph here
  is a few thousand nodes, so any optimisation now aims at a wall nobody has hit —
  though making the row a record already cost a third more width on `Babi`. Full
  `Clevr` and the ten-thousand-story `Babi` reach far larger; **measure, then
  cut.** In order of leverage:
- **BOUND THE ROW.** Cap a node at K partners: it turns *cost per thought grows
  with data forever* into *cost per thought is constant*, which is the trick
  approximate-nearest-neighbour indexes run at billions on. **Evict on "not
  touched since", never by eroding a count** — the `when` channel already provides
  it, so supersession and scaling share one mechanism. Top-K in bounded memory is
  the heavy-hitters problem; Space-Saving solves it with an error bound.
- **A SELF-SET BEAM, whose revival condition is now met.** The refuted row asks a
  width the system sets itself and reports; `Surprise.Rate` is one signal to set
  it from, a node's own row statistics a second.
- **Hierarchy, which is what step 3 is really for.** Do not walk a million nodes;
  walk a thousand chunks.

### The wire, when the remote half lands — John, 2026-08-03

- **Only the local half of `HybridBus` exists, so none of this is built.**
- **C2 IS STILL NOT TESTED, AND THE REASON IS THE HARNESS.** `Lateness` injects
  it and the composition world absorbs it completely — a held-back delivery is
  delayed inside the in-flight count, so `WhenIdle` does not fire, and **every
  reader here waits for quiet.** Lateness becomes waiting, never
  acting-without-it. **The hard half needs a reader on a DEADLINE**, not a bigger
  delay, or two machines. **Established meanwhile**: bus, accounting and walk are
  unharmed by arrivals far out of order.
- **Coalesce a settling wave into one send.** Hold remote envelopes until local
  traffic drains, then one datagram per destination; `WhenIdle()` is the trigger
  and is C1-legal. **Not a pure barrier** — flush on idle *or* size *or* time.
- **Bits, not JSON.** Modalities intern to small ints, a code is a varint, the
  `double`s are probably `float`s, and **a sixth of a packed message is the `Guid`
  broadcast id** — shorten it per connection.
- **`Chain` is what costs** — cycle check and explanation in one field, free
  locally and not on a wire. **Split them:** an approximate-membership filter for
  the hop, full chain rebuilt at the origin. A false positive is a route wrongly
  refusing a partner, the loss C2 admits.
- **Voting multiplies the wrong half.** `votes: n` pays n floods to insure against
  loss on the way back. **One thought, redundant reports**: the flood is the
  graph's, and what C2 loses is the return path.
- **UDP matches, and is not a compromise.** C2 assumes loss, and **TCP's
  head-of-line blocking would stall every thought behind one lost packet.** QUIC's
  unreliable datagram extension (RFC 9221) is the shape.

---

## DO NOT RE-TRY

**Three columns, one line each, enforced by a test. The third matters most: a
refutation is conditional on its configuration, so a row without a revival
condition is a superstition.**

| what | what refuted it | what would revive it |
|---|---|---|
| `StepCost.Best` / `Local` / `Constant` | Factorial message growth where inverse cost is polynomial | A bound not relying on positive cost at weight 1.0 |
| `Refuel` | Nothing is paid back, so it did nothing | Anything that returns budget to a route |
| Sender-*weighing*, `IMarginals` | A C1 violation, and behaviour was identical without it | Never. **But `Message.Seen`, the sender's OWN marginal, is legal and built** |
| Absolute actions, unrotated view | One move in four instantly fatal | A body with no heading |
| Survival as the score | Circling wins: it lives longest and eats least, and **`Steps` is the same row in a hat**. **Snake cannot discriminate policies at all** — nothing gives it a reason to seek food | Homeostatic drives, where standing still stops paying |
| A beam over partners | A constant nobody set, doing the cutting | A width the system sets itself and reports |
| Clusters by modality | Splits picture from sound — the one link this design exists to make | Never |
| Clusters by time of creation | Two machines compute different owners for one code | Placement agreement without a coordinator |
| `Adaptive` reflection on `Hunger` | Inverted: it wrote most where it helped least | A signal that discriminates; `Thwarted` does |
| A deeper walk for prediction | Monotonically worse — without edge kinds, deeper reaches more and ranks worse | **Edge kinds**, and that refutation reproduced |
| `ArrivalValue.Lift`, `Accumulate.Max` | Swept, inert, both explanations refuted, `Max` worse where its revival condition pointed. **Both DELETED** | Lift in the **cost** |
| Naming fewer predicted codes | Half true: coarse ranking informs, fine does not | **REVIVED at one code** — naming as many as the frame holds swamps the action |
| `Window` span | Null on snake, WORSE on `Babi` at an order of magnitude more traffic, and **the whole task on `Rhythm`**. **Its revival condition has been RUN and half-held**: kinds recover much of both, and carrying still loses to not carrying | **Something that makes a carried edge worth its row.** Kinds were the structural half and are not enough |
| `includeEmpty: true` | Ruinous under `Best` pricing | **Revived — inverse cost removed the reason; no clear winner since** |
| `Pricing.Balanced` — cosine's denominator | **Times out.** The geometric mean sits BETWEEN the marginals, so weights rise and the walk explodes. Built for a conflict that was a budget artifact | A bound not relying on the weight being one marginal's reciprocal |
| `Pricing.Driven` — the node picking the marginal per hop | **Two local rules, both worse than the better hand-set arm in BOTH worlds**, because a per-hop choice puts routes on different scales and the ranking stops meaning anything. **The premise was wrong — on `Babi` the better arm is the DEARER one** | A local quantity that predicts which arm wins, on a world where they differ: `Senses` scores them identically |
| `Accumulate.Fused` — rank fusion over the two orders | Half of agreement's lift and all its cost. **Two candidates whose orders invert tie identically under RRF for every damping constant**, so it ties exactly where it is needed | Many candidates, or a fusion separating by something other than position |
| Inhibition on `Homeostat` | Folding the negative cell into the COUNT mutes the walk — that number is the ranking AND the price. Discounting the SCORE alone recovers most of it and **still does not beat the one-sided count**, where most acts are wrong most of the time | A world where the wrong act is informative and RARE. **The PN-Counter itself is sound and stays** |
| `Ranked` as step 4's fix | **The lift was the bootstrap's coin toss** — silent three times as often, and quieter is worse on both front ends. A varying code thins every edge, so routes starve before reaching an action. **Kept: it lifts a real ceiling** | Anything making the walk prefer a partner other than the one it took last time |

---

## TRAPS

**Closed in code, named so nobody reintroduces them:** consecutive integer seeds
are not independent (`Seeds.Apart`); `Measured.Separation` returned zero where
repeated measurement found no spread (infinity now); `WhenQuiet()` was not a
finish signal (`WhenIdle()`); walks were read before they had finished (fork 22,
`Unsettled` on `Measurement`, so a new world cannot go without it).
**Voting survives that fix and buys nothing in one process** — kept, because a
real network loses reports.

**Live:**

- **A DIAL MEASURED AT ONE SETTING OF ANOTHER MAY BE MEASURING THAT ONE.** The
  stamina plateau reversed between short and long runs. **Sweep at two run
  lengths, and never compare dials with a third pinned.**
- **Short runs on the binding world score above chance for RECENCY ALONE.**
  **Nothing under a few hundred scenes measures binding there.**
- **A dial can be declared, documented, passed everywhere and connected to
  nothing.** `ThinkAsync`'s stamina was, and survived three measurements.
  **Every run reports `Complaints`; read them.**
- **A CHECK CAN BE WIRED AND UNABLE TO FIRE, which reads as passing.** Two worlds
  counted `unbalanced` off a variable nothing incremented. **Arming a check that
  has always read zero is the only way to tell the two apart.**
- **A FALLBACK IS A CONTROL ARM NOBODY MEANT TO RUN.** Where an arm acts at random
  when the walk says nothing, anything raising its silence drifts it toward the
  random bar for free, and that reads as the change working. **Report silence
  beside the score.** It cannot carry an arm PAST the bar, though — that much is
  arithmetic.
- **A small sample can look like a mechanism.** One seed with a collapsing echo
  read as a discovery and was three questions; one seed of step 4's credit arm
  showed a clean monotone learning curve that six seeds flattened to nothing.
- **A MEAN OVER A POPULATION THE PROBLEM CREATED CANNOT SEE IT.** Fan-out stayed
  flat where rows grew without bound, because the growth mints tiny nodes that
  hold the average down. **Read `Widest`.**
- **Copies drift where nothing fails**, and a difference between two copies moves
  a headline without failing a test — three worlds each grew their own settle
  loop. `DuplicationTests` is the budget now.
- **THE TEST SUITE IS SERIAL ON PURPOSE.** Parallel, the walk's agreement with
  itself measured perfect where alone it did not: load removes the ordering that
  produces the disagreement, so parallelism HID a real defect. `Parallelism.cs`.

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
| **11** | The output machine is addressed: a finished thought is PUBLISHED and the bus routes it by code, so N actuators act on one broadcast. **Closed — no world runs two yet** |
| **12** | A fixed seed reproduces a run exactly, `Halted` included. Closed by 22's fix |
| **18** | Score prediction **conditional on the next action**. **ANSWERED BY STEP 6** — action ordered first, prediction asking what follows, actuator what preceded; holds on every seed of twelve. **The survival cost was not one** |
| **20** | Split budgets — deep to act, shallow to predict. Closed |
| **21** | Compression. A trade: it pays where the budget cannot compose and costs where it can. Off |
| **22** | A transiently-zero live count untracked thoughts mid-flight and dropped every later report. `Retire` asks twice. Closed |
| **23** | Compression self-regulating? Not on this signal. `Thwarted` is the right shape and swings too little against the effect |
| **24** | Budget controller converges from both directions and **aims at a moving target**. Off by default |
| **25** | The binding world — built to fail, failed as predicted, and since lifted |
