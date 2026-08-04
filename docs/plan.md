# Where this is going

- **The only doc. `DocsTests` holds a word TARGET, a hard CEILING, and a separate
  PROSE cap.** To add something, retire something — and prefer cutting prose.
- **Let it drift while a session runs; compact in one pass at the end** — John,
  2026-08-04. Piecemeal trimming costs attention and cuts in ignorance of what the
  session will find. **The ceiling stops that becoming permanent drift.**
- **What every piece does lives in the XML comments**, where the compiler enforces
  that every reference resolves — including why prose goes first.
- **Forward-facing, no results.** Findings live in the commit, the comment beside
  the mechanism, and the test that asserts them. Deleted docs are in git.

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
  never observed; `Homeostat`, where a body must keep itself in bounds; and **two
  nobody here designed**, `Babi` and `Clevr`. `ScoreboardTests` runs them in one
  table.
- **AN EXTERNAL WORLD CANNOT FLATTER THIS ARCHITECTURE.** `corpora/fetch.sh`
  fetches; nothing is vendored.
- **An occasion is a SET**, so *red ball beside blue box* and *blue ball beside
  red box* were one input. That ceiling was **representational, not scale**;
  `Occasion.Groups` lifts it.
- **The graphs are tiny.** Nothing has run at a size where its claims could break;
  hold them loosely.

### ONE WEIGHT DOING TWO JOBS IS THIS DESIGN'S RECURRING FAULT

- **It has bitten twice.** A weight both RANKS a partner and PRICES the hop, and
  improving one wrecked the other. `Pricing.Sender` moves the ranking meaning to
  move the price; `Doubt` destroys the senses world applied to both and repairs a
  real defect applied to the score alone.
- **The general move: find the number serving two masters and split it.** The row
  entry still ranks AND prices with one number, and that split is the outstanding
  one.
- **A dial wanting different values in different worlds is the same fault** —
  John, 2026-08-03. Split it or fuse the arms rather than sweep. `DialTests`
  records which channel each dial may move.
- **A conflict between two dials may be a BUDGET ARTIFACT** — `Clevr` was one; the
  trap is in TRAPS.

---

## NEXT

**The binding world is the scoreboard. Do not build a new world to test a change
to an old one — run that one.**

### 1. Composition over bindings — closed by `Accumulate.Agreement` and `Refer.Narrowed`, which document themselves

### 1a. RANKING BELONGS TO THE QUESTION — moved, and the dial is gone

- **It travels on `Question`**, and `WalkSettings` has one dial fewer.
- **Merging routes AT A NODE is the version `Narrowed` could not do** — reading
  the index back puts the referent in the machine's hands, where a node combining
  concurrent routes keeps it in the graph. Needs a wait: expensive under C2, and
  the honest form.
- **`Pricing` IS STILL HAND-SET AND A CONTROLLER FOR IT IS REFUTED.** **What is
  missing is not the controller but the SIGNAL** — fork 23's lesson a third time,
  and the refuted row holds why.

### 2. Predictive coding — only surprise propagates

- Rao & Ballard, Friston. An expected onset is silent. **Built.**
- **THE OBSERVATION IS SUPPRESSED, NOT THE PREDICTION** — the walk making the
  expectation still runs, so the saving is partial. **Making prediction itself
  conditional on surprise is the deeper version**, and would drive a dial.

### 3. Chunking — MDL

- **`Chunk` mints a NODE where fork 21 mints an edge**, so the alphabet GROWS
  where the quantiser fixed it forever. **The name is DERIVED from the sorted
  members**, so two machines mint the same code with nothing to ask — the ring's
  trick, and the only minting C1 permits.
- **The threshold is description length and nothing was chosen**: naming wins at
  `n(S-1) > S`, so a set of four pays on its second arrival.
- **IT BUYS THE TRAFFIC AND NOTHING ELSE.** Per completion it falls threefold on
  `Motif` — the number step 3 was set. **But the graph gets BIGGER and accuracy
  costs a little**: the sets are a tiny share of a row count the noise dominates,
  so MDL's storage half never shows.
- **A MINTED NODE IS A HUB BY CONSTRUCTION and `Pricing.Receiver` refuses hubs** —
  the likely reading of that cost, unverified; `Pricing.Sender` here tests it.
  **The candidate is the whole MOMENT and not the onsets**, or partial views earn
  their own names.
- Open: the **utility problem** (Minton, SOAR) — utility belongs per chunk, and a
  chunk that stops recurring earns nothing.

### 4. Homeostatic drives — Ashby

- Bounded internal variables make behaviour goal-directed **with no reward
  function**, survival having proved gameable by circling. **No episode
  boundary**, which fits C4.
- **`Homeostat` HAS SET THE BAR.** Attending to whatever is lowest holds the body
  indefinitely, so the world is winnable and only by looking at it. **The bar is
  random, not idling**, and an arm needs a bootstrap, since an action enters the
  graph only by being taken.
- **EVERY POINT THE ARM SCORES COMES FROM THAT BOOTSTRAP.** Spend the budget until
  the walk decides nearly every step and the score falls to *idling's exactly*,
  attending one variable almost always and the two FASTEST-DRAINING ones never.
  **Quieter is worse, monotonically, on both front ends. So association is
  ANTI-CORRELATED here, not merely uninformative** — the walk reaches what was
  done last time in this state, and in a body that is precisely what failed to
  prevent the state. Fork 20's mirror at its sharpest.
- **`Drives` IS BUILT AND THREE ARMS FAILED THE BAR** — credit, credit without the
  delay, and the delay alone. **All lost to no credit at all**, because all three
  wrote a HEAVIER number into the cell that already means *this was done here*.
  Reinforcing that deepens the groove it was meant to fix.
- **THE CREDIT NEEDED ITS OWN CELL, AND THAT IS THE FIRST ARM TO BEAT RANDOM.**
  `Kind.Helped` records *this pair, and things got better after* as a SECOND
  statistic; `Question.Worthwhile()` walks that one alone. Ranking is then the
  share of times an act helped rather than how often it was taken, and the
  contrast falls out of the anti-hub weighting already there — `seen(act)` is the
  denominator. **Nothing is punished and nothing decays**: an act that did not
  help gets no second write, and both counts stay monotonic.
- **THE CONTRAST IS WHAT DID IT, and a control says so.** `Marked` writes the same
  second cell, equally stale, on EVERY step regardless of outcome, and walks it
  the same way — it loses to the bar and to no credit at all. The condition is the
  whole of the gain.
- **AND `Ranked` NOW PAYS, having bought nothing alone.** The ordering was
  necessary and not sufficient: with no contrastive signal there was nothing for a
  better representation to attach to.
- **Silence cannot explain any of it.** The bootstrap acts at random, so coin
  tosses pull an arm TOWARDS the bar and can never carry it past.
- **WHAT IS LEFT ON STEP 4, AND IT IS NOT INEXPERIENCE.** The arm is silent most
  of the time and sits below the ceiling, and **quadrupling the run does not move
  it** — no learning curve at all across seeds, though one seed alone shows a
  clean one. **The state count keeps growing**, so a credit cell keyed on the
  state it was earned in can never densely cover them, and nothing carries what
  was learnt in one state to a similar one. **Step 8's argument from a third
  direction.**
- **THE FRONT END WAS A REAL CEILING AND IS NOT THE CAUSE.** *Attend to whichever
  is lowest* is relational, and a band is absolute: two states with identical
  bands can have opposite answers, and the ceiling policy held the body so still
  it visited ONE state. **`Ranked` lifts both and buys the arm nothing.** It does
  change what is attended to, in the ceiling's direction, at 27× the messages.
- **THE GENERAL LESSON, AND IT IS BIGGER THAN THIS WORLD.** A count of what was
  done converges on the POLICY that did it — *P(act | state)* — and the states a
  body finds itself in are the ones its own actions produced. So the association
  actively encodes the wrong act for the state and is self-confirming.
  **Contiguity is not contingency** (Rescorla–Wagner, and this is `ΔP` in the
  shape the design allows). Anywhere the graph learns from its own behaviour,
  the same confound is waiting.

### 5. WHAT A CO-OCCURRENCE COUNT STRUCTURALLY CANNOT DO — John, 2026-08-03

**Consequences of the design, not missing features. Ordered by cost.**

- **ABSENCE — BUILT AS A SIGNAL, AND WIRED.** `Surprise` returns both halves of
  the signed error, and `Overreach` tells a solved world from a predictor naming
  everything — the one failure step 2 can cause rather than measure. **A SIGNAL,
  not a node**: minting `not-X` would double an alphabet to represent unboundedly
  many absent things. `Rhythm` complains when the predictor foresees nearly
  everything while nearly nothing it named happens.
- **SUPERSESSION — THE CHANNEL IS BUILT, NOTHING READS IT.** `Tie.When` rides
  beside the count as an LWW-Register and **the two must never merge**; `Tie`
  holds why. **Do not decay**, which breaks convergence. **What is left is both
  consumers**: recency ranking on `Question`, and eviction on "not touched
  since". Across machines it wants a Lamport clock.
- **MULTI-TOKEN OUTPUT, concurrently. Splits in two.** *Simultaneous* actions are
  nearly free and **fork 11 built the addressing**; what is left is a world that
  wants two. *Ordered* sequences need edge kinds.

### 6. EDGE KINDS — BUILT, and the row was widened once

- **A row entry is `(Code, Kind)` to `(count, when)`**, with `With` and `After`,
  and supersession rode in beside it for the single price.
- **The front end SAYS the order inside the occasion** — John's insight, the
  `Groups` trick again, because a phase cannot survive C2. `Occasion.Sequence` is
  additive; splitting the window's carried edge moves measured counts, so that
  half is an arm and OFF is every earlier number.
- **`Question.Through` restricts a walk to one relation**, and it is what answered
  fork 18. `Babi` is not a question about what follows, which is why kinds help
  there without rescuing the window.
- **The one-way rule was a workaround for the missing kind, and step 6 retired
  it** — with the bound that keeps `together <= seen`. `Kind.Before` holds both
  arguments in full.
- **`Rhythm` cannot measure this**: nothing there is ever simultaneous, so every
  cell is already temporal and splitting them is an isomorphism.

### 7. Credit over time — eligibility traces, and `Window` is already one

- **The gap: nothing learns that an act led somewhere good three steps later.** No
  reward function, and no backpropagation either.
- **Three-factor Hebbian learning needs neither** (Izhikevich 2007, the distal
  reward problem): keep a fading trace of what recently fired, and let a third
  signal consolidate what is still in it, most credit to the most recent.
- **`Window` IS that trace, ungated.** Drives supply the third factor and
  `Surprise` is a second candidate — and it stays **safe for the CRDT property**,
  the trace being transient state deciding how much to add while counts only rise.

### 8. Also likely necessary

- **VARIABLE BINDING.** *A is north of B* is a count between two codes, so it
  cannot apply to a new A and B. Without it every generalisation runs through
  similarity. **This un-parks vector-symbolic binding**; `Clutrr` and `gSCAN` are
  the worlds that would force it.
- **REPLAY.** Re-run experience when nothing is arriving: consolidates, learns from
  rare events, interleaves old with new against interference. **`WhenIdle()` is
  already the trigger**, and fork 21 is its cousin.
- **A FRONT END FOR REAL PERCEPTION — measured once as THE binding constraint.**
  Every world here is symbolic, so nothing has hit that wall since. The red-ball
  property needs codes identical on every machine forever, so a fitted codebook is
  out. **But a uniform hash is agreed and unwalkable**: at a matched code count
  k-means cleared chance on every seed and LSH on none, because the data is
  concentrated where uniform codes are not. **Spending codes where the data is
  WITHOUT fitting one is the unbuilt middle** — the front end makes a thing
  STANDARD, and identifying it is the graph's job.
- **INHIBITION.** The graph is purely excitatory — nothing says *this rules that
  out*. Buys competition between candidates, and a second route to absence.

### THE PATTERN UNDER ALL OF IT

- **A row entry was one number doing several jobs** — the recurring fault at the
  level of the DATA STRUCTURE rather than of a dial. **The remedy was to make it a
  record**, widened ONCE, and the price is measurable: the row is a third wider on
  `Babi`, which is the scaling wall arriving sooner.
- **None of this is a sufficiency argument.** All of it could land and still not be
  enough. The narrower claim: without structure, an internal error signal, a
  growing alphabet, a reason to act, supersession, absence, concurrent output and a
  bounded row, no amount of scaling gets there.

---

## TO BUILD — a ticked box means the type exists, and a test checks

- [x] `Chunk` — the minted node of step 3. **Built, and it is a trade**
- [x] `Drives` — step 4's third factor. **Built, and it does not clear the bar**

---

## LATER — nothing is blocked on these

- **Fork 1 is smaller than it looks.** A count that only increments is a
  **G-Counter, a CRDT** — it converges under reordering and loss with no
  coordination. The counts need no protocol; only the join does.
- **The absolute message cost is what step 2 attacks.** Nothing else should be
  optimised until it has been tried.
- **Cold storage, once a row can be bounded.** **Paging a node out keeps the CRDT
  property** — the count does not decrease, it stops being resident. Decay does
  not.
- **The knob pass, last.** A dial swept before the structural work measures a
  system about to change underneath it.

### The scaling wall — measure it before cutting anything

- **STEP ZERO IS TO BUILD SOMETHING BIG ENOUGH TO BREAK.** The largest graph here
  is a few thousand nodes, so any optimisation now aims at a wall nobody has hit.
  Full `Clevr` and the ten-thousand-story `Babi` reach far larger; **measure, then
  cut.** In order of leverage:
- **BOUND THE ROW.** Cap a node at K partners. This is the one that matters: it
  turns *cost per thought grows with data forever* into *cost per thought is
  constant*. Approximate-nearest-neighbour indexes run at billions on this trick.
  **Evict on "not touched since", never by eroding a count** — which is what the
  `when` channel provides, so supersession and scaling share one mechanism.
- **A SELF-SET BEAM, whose revival condition is now met.** The refuted row asks for
  a width the system sets itself and reports; `Surprise.Rate` is one internal
  signal to set it from, and a node's own row statistics a second.
- **Hierarchy, which is what step 3 is really for.** Do not walk a million nodes;
  walk a thousand chunks.

### The wire, when the remote half lands — John, 2026-08-03

- **Only the local half of `HybridBus` exists, so none of this is built.**
- **C2 IS STILL NOT TESTED, AND NOW THE REASON IS KNOWN.** `Lateness` injects it
  and the composition world **absorbs it completely** — identical accuracy to four
  places, accounting closed. **The harness is why, not the design**: a held-back
  delivery is delayed inside the in-flight count, so `WhenIdle` does not fire
  while it waits, and **every reader here waits for quiet.** Lateness becomes
  waiting and never becomes acting-without-it.
- **SO THE HARD HALF NEEDS A READER ON A DEADLINE**, not a bigger delay — nothing
  under `Fabric`'s patience can escape a wait. That, or two machines. **What is
  established meanwhile**: the bus, the accounting and the walk are unharmed by
  deliveries arriving far out of order.
- **Coalesce a settling wave into one send.** Hold remote envelopes until local
  traffic drains, then one datagram per destination; `WhenIdle()` is the trigger
  and is C1-legal. **Not a pure barrier** — flush on idle *or* size *or* time, or a
  busy machine never sends.
- **Bits, not JSON.** Addresses and modalities intern to small ints, a code is a
  varint, the `double`s are almost certainly `float`s, and **a sixth of a packed
  message is the `Guid` broadcast id** — shorten it per connection.
- **`Chain` is what costs** — cycle check and explanation in one field, free
  locally and not on a wire. **Split them:** an approximate-membership filter for
  the hop, full chain rebuilt at the origin. A false positive is a route wrongly
  refusing a partner, which is the loss C2 admits.
- **Voting multiplies the wrong half — John, 2026-08-03.** `votes: n` pays n floods
  to insure against loss on the way back. **One thought, redundant reports**: the
  flood is the graph's, and what C2 loses is the return path.
- **UDP matches, and is not a compromise.** C2 assumes loss, and **TCP's
  head-of-line blocking would stall every thought behind one lost packet.** QUIC's
  unreliable datagram extension (RFC 9221) is the shape.

---

## DO NOT RE-TRY

**Three columns, one line each, enforced by a test. The third matters most: a
refutation is conditional on its configuration, so a row without a revival
condition is a superstition. The commit named in git holds the numbers.**

| what | what refuted it | what would revive it |
|---|---|---|
| `StepCost.Best` / `Local` / `Constant` | Factorial message growth where inverse cost is polynomial | A bound not relying on positive cost at weight 1.0 |
| `Refuel` | Nothing is paid back, so it did nothing | Anything that returns budget to a route |
| Sender-*weighing*, `IMarginals` | A C1 violation, and behaviour was identical without it | Never. **But `Message.Seen` — the sender's OWN marginal — is legal and is built** |
| Absolute actions, unrotated view | One move in four instantly fatal | A body with no heading |
| Survival as the score | Circling wins: it lives longest and eats least. **`Steps` is the same row in a hat — John, 2026-08-04**: ending at exactly the starting energy is a body that ate NOTHING. **Snake cannot discriminate policies at all** — a handful of apples across two dozen runs, nothing giving it a reason to seek food | Homeostatic drives, where standing still stops paying |
| A beam over partners | A constant nobody set, doing the cutting | A width the system sets itself and reports |
| Clusters by modality | Splits picture from sound — the one link this design exists to make | Never |
| Clusters by time of creation | Two machines compute different owners for one code | Placement agreement without a coordinator |
| `Adaptive` reflection on `Hunger` | Inverted: it wrote most where it helped least | A signal that discriminates; `Thwarted` does |
| A deeper walk for prediction | Monotonically worse — without edge kinds, deeper reaches more and ranks worse | **Edge kinds**, and that refutation reproduced |
| `ArrivalValue.Lift`, `Accumulate.Max` | Swept, inert, both explanations refuted, `Max` worse where its revival condition pointed. **Both DELETED** | Lift in the **cost**, which `Doubt` is nearest to |
| Naming fewer predicted codes | Half true: coarse ranking informs, fine does not | **REVIVED at one code.** Naming as many as the frame holds swamps the action; naming ONE, the gap opens wide |
| `Window` span | Null on snake, WORSE on `Babi` at an order of magnitude more traffic, and **the whole task on `Rhythm`**, where at zero the graph forms no edges. **Its revival condition has been RUN and half-held**: kinds recover much of both and carrying still loses to not carrying | **Something that makes a carried edge worth its row.** Kinds were the structural half and are not enough alone |
| `includeEmpty: true` | Ruinous under `Best` pricing | **Revived — inverse cost removed the reason; no clear winner since** |
| `Pricing.Balanced` — cosine's denominator | **Times out.** The geometric mean sits BETWEEN the marginals, so weights rise and the walk explodes rather than compromising. Built for a conflict that was a budget artifact | A bound not relying on the weight being one marginal's reciprocal — `StepCost.Best`'s condition |
| `Pricing.Driven` — the node picking the marginal per hop | **Two local rules, both worse than the better hand-set arm in BOTH worlds**, because a per-hop choice puts routes on different scales and the ranking stops meaning anything. **The premise was wrong — on `Babi` the better arm is the DEARER one** | A local quantity that predicts which arm wins, on a world where they differ: `Senses` scores them identically |
| `Accumulate.Fused` — rank fusion over the two orders | Half of agreement's lift and all its cost. **Two candidates whose orders invert tie identically under RRF for every damping constant**, so it ties exactly where it is needed | Many candidates, or a fusion separating by something other than position |
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
- **Short runs on the binding world score above chance for RECENCY ALONE**, and it
  decays with data. **Nothing under a few hundred scenes measures binding there.**
- **A dial can be declared, documented, passed everywhere and connected to
  nothing.** `ThinkAsync`'s stamina was, and survived the whole suite and three
  measurements. **Every run reports `Complaints`; read them.**
- **A CHECK CAN BE WIRED AND UNABLE TO FIRE, which reads as passing.** Two worlds
  counted `unbalanced` off a variable nothing incremented, and snake never asked
  whether a walk had finished. **Arming a check that has always read zero is the
  only way to tell the two apart.**
- **A FALLBACK IS A CONTROL ARM NOBODY MEANT TO RUN.** Where an arm acts at random
  when the walk says nothing, anything raising its silence drifts it toward the
  random bar for free — and that reads as the change working. **Report silence
  beside the score, and spend budget until the two arms match on it.**
- **A small sample can look like a mechanism.** One seed with a collapsing echo
  read as a discovery and was three questions.
- **A MEAN OVER A POPULATION THE PROBLEM CREATED CANNOT SEE IT.** Fan-out stayed
  flat where rows grew without bound: the growth mints tiny nodes that hold the
  average down. **Read `Widest`.**
- **Copies drift where nothing fails**, and a difference between two copies moves
  a headline without failing a test — three worlds each grew their own settle
  loop. `DuplicationTests` is the budget now.
- **THE TEST SUITE IS SERIAL ON PURPOSE.** Parallel, the walk's agreement with
  itself measured perfect where alone it did not — load removes the ordering that
  produces the disagreement, so parallelism HID a real defect. See
  `Parallelism.cs`; the cost figure recorded there predates the suite doubling.

---

## OPEN DEFECTS

**Nothing outstanding.**

---

## FORK NUMBERS THE CODE CITES

**Never renumbered** — `DocsTests` asserts each still resolves. Closed forks stay
listed because the code still points at them.

| | |
|---|---|
| **1** | The distributed rendezvous. **Open** — not needed until a second machine; see the CRDT note |
| **3** | Cluster placement: uniform hash against prefix locality. **Open** |
| **5** | A death writes off exactly the routes heading into the dead cluster. Closed |
| **6** | Broadcast the origin, route the hops. Closed |
| **11** | The output machine is addressed: a finished thought is PUBLISHED and the bus routes it by code, so N actuators act on one broadcast without holding it. **Closed — no world runs two yet** |
| **12** | A fixed seed reproduces a run exactly, `Halted` included. Closed by 22's fix |
| **18** | Score prediction **conditional on the next action**. **ANSWERED BY STEP 6** — action ordered first, prediction asking what follows, actuator asking what preceded, and it holds on every seed of twelve. **The survival cost was not one** — see the refuted row |
| **20** | Split budgets — deep to act, shallow to predict. Closed |
| **21** | Compression. A trade: it pays where the budget cannot compose and costs where it can. Off by default |
| **22** | A transiently-zero live count untracked thoughts mid-flight and dropped every later report. `InputMachine.Retire` asks twice. Closed |
| **23** | Compression self-regulating? Not on this signal. `Thwarted` is the right shape and swings too little against the effect |
| **24** | Budget controller converges from both directions and **aims at a moving target**. Off by default |
| **25** | The binding world — built to fail, failed as predicted, and since lifted |
