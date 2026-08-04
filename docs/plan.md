# Where this is going

**The only doc, capped at 4,000 words by `DocsTests`. To add something, retire
something.** Its predecessors grew past being loadable and kept being cited
anyway, which is the failure this exists to prevent.

**What every piece does lives in the XML comments beside the code**, where the
compiler enforces that every reference resolves and nothing can go stale.

**This file is forward-facing and records no results.** Findings live in the
commit that produced them, in the comment beside the mechanism, and in the test
that asserts them. Deleted docs are in git.

---

## The goal

A system that **understands** rather than performs — one that can be asked *what
would the world look like if I did X*, which a sequence model cannot be. Learning
is a co-occurrence count; everything else is plumbing around it.

**Three constraints shape every decision.** C1: no node reads another's data.
C2: messages are late, jittered, out of order. C4: no episode boundary, so
nothing may depend on training then testing.

---

## What is standing

**Six worlds sharing no world logic, over one `Fabric`.** Snake, where a chain
can cause a move; senses, where sight and touch never co-occur so the answer can
only be composed; binding, built so this architecture provably could not answer
it and since lifted; composition, where the answer was never observed at all; and
**two nobody here designed** — `Babi` and `Clevr`.

**AN EXTERNAL WORLD CANNOT FLATTER THIS ARCHITECTURE BY CONSTRUCTION**, which is
why to read one: the other four were built by the same hands as the mechanisms
they measure. `corpora/fetch.sh` fetches; nothing is vendored.

**An occasion is a SET of co-occurring codes**, so *red ball beside blue box* and
*blue ball beside red box* were the same input. That ceiling was
**representational, not a matter of scale**, and `Occasion.Groups` lifts it.

**The graphs are tiny.** Nothing here has been run at a size where its claims
could break. Hold them loosely — and the size dial that exists has already found
one thing that was wrong.

### ONE WEIGHT DOING TWO JOBS IS THIS DESIGN'S RECURRING FAULT

**It has bitten twice.** An edge weight both RANKS a partner and PRICES the hop
to it, and each attempt to improve one silently wrecked the other.
`Pricing.Sender` moves the ranking while meaning to move the price. `Doubt` —
shrinkage, so a partner seen once cannot claim the strongest edge in the graph on
one accident — destroys the senses world applied to both, and repairs a real
defect for free applied to the score alone.

**So the general move is: find the number serving two masters and split it.**
`Accumulate` is the next candidate. **And the standing rule — John, 2026-08-03 — is
that a dial wanting different values in different worlds is the same fault**:
prefer splitting it, or fusing the arms, over sweeping it. `DialTests` records
which channel each dial may move and fails when one moves the other.

**A CONFLICT BETWEEN TWO DIALS MAY BE A BUDGET ARTIFACT.** `Clevr` looked like a
sharper version of the pricing split and was one. **The trap it walked into is in
TRAPS.**

---

## NEXT

**The binding world is the scoreboard. Do not build a new world to test a change
to an old one — run that one.**

### 1. Composition over bindings — built and answered

**Closed by `Accumulate.Agreement` and `Refer.Narrowed`, which document
themselves.** What is left is the world's own ambiguity, not the walk's.

### 1a. RANKING BELONGS TO THE QUESTION — moved, and the dial is gone

**It travels on `Question`**, beside the grouping that was already going there,
and `WalkSettings` has one dial fewer. What is left here is unbuilt:

- **Merging routes AT A NODE is the version `Narrowed` could not do.** Reading
  the index back costs a round trip and puts the referent in the machine's
  hands; a node combining concurrent routes of one broadcast would keep it in the
  graph. It needs a wait, which C2 makes expensive — but it is the honest form.

**`Pricing` IS THE ARM STILL HAND-SET, AND PROMOTING IT WAS NOT TAKEN.** The
trade belongs to graph density and available budget rather than to the question —
on a near-clique the receiver arm cannot be given the budget at all, and on a
sparse graph with rare indexes it wins once it can pay. Both quantities are
locally observable, so it wants a controller and not a default. **`Fleeting` has
nothing global to promote**: where it applies, the right value is already set for
a written reason.

### 2. Predictive coding — only surprise propagates

Rao & Ballard, Friston. An expected onset is silent. **Built** — and
`Surprise.Rate` is the first quantity a controller could read from inside rather
than from outside.

**THE OBSERVATION BROADCAST IS SUPPRESSED, NOT THE PREDICTION** — the walk making
the expectation still runs and costs more, so the saving is partial. **Making
prediction itself conditional on surprise is the deeper version**, and what would
let this drive a dial.

### 3. Chunking — MDL

Fork 21 mints edges; it should mint **nodes**. When a set of codes recurs, create
a code standing for the set. Threshold is minimum description length, not a
constant. **This is what lets the alphabet GROW** — today the quantiser fixes it
forever. Fork 21's trade is the **utility problem** from explanation-based
learning (Minton, SOAR): utility belongs per chunk, not as one global `Weight`.

**`Motif` HAS ALREADY SAID WHAT THIS IS NOT ABOUT.** A familiar set completes
perfectly without any chunking, because its members co-occur and the counts are
right. **So step 3 is not being asked to fix an accuracy — it is being asked to
stop paying for one**, and the number to beat is the traffic per completion.

### 4. Homeostatic drives — Ashby

Keep internal variables in bounds and behaviour becomes goal-directed **with no
reward function** — which matters, because survival proved gameable by circling.
**Homeostasis has no episode boundary**, which fits C4.

**`Homeostat` HAS SET THE BAR, AND NOT WHERE ANYBODY WOULD GUESS.** Attending to
whatever is lowest holds the body indefinitely, so the world is winnable and only
by looking at it. **But choosing by association scores BELOW random**: with
nothing to say what an action is for, the walk repeats what it did last time in
that state, which is fork 20's mirror again. **So step 4 must beat random, not
idling** — and needs a bootstrap, since an action enters the graph only by being
taken.

### 5. WHAT A CO-OCCURRENCE COUNT STRUCTURALLY CANNOT DO — John, 2026-08-03

**Three limits that are not missing features but consequences of the design**, and
the approach for each. **Ordered here by cost.**

- **ABSENCE — BUILT AS A SIGNAL, AND NOTHING ACTS ON IT YET.** `Surprise` returns
  both halves of the signed error, and `Overreach` is what tells a solved world
  from a predictor naming everything — which `Rate` alone cannot, and which is the
  one failure step 2 can cause rather than measure. **Absence is a SIGNAL, not a
  node**; minting `not-X` would double an alphabet to represent unboundedly many
  things that are not there. **What is left is a consumer**: it is the second
  quantity a controller could read from inside, and a third-factor candidate for
  step 7.
- **SUPERSESSION.** Counts only increment, which is the G-Counter property that
  makes them converge with no coordination — and exactly what forbids
  most-recent-wins. **Do not decay**, which breaks convergence. **Add a second
  channel instead: an LWW-Register is also a CRDT.** `together` stays monotonic
  and a `when` rides beside it; the two must never merge, because LWW discards
  concurrent writes — right for state, ruinous for learning. `Occasion.At` exists,
  so this is buildable now; across machines it wants a Lamport clock, which needs
  no coordinator. Ranking by recency then belongs on `Question`, not on a dial.
- **MULTI-TOKEN OUTPUT**, and concurrently. **Splits in two.** *Simultaneous*
  actions are nearly free — `BestOf` already returns many, and many thoughts are
  already in flight, which is what `BroadcastId` is for. What is missing is
  several output machines, so **fork 11 is the enabler and not plumbing.**
  *Ordered* sequences need edge kinds.

### 6. EDGE KINDS — promoted, because three roads end here

**A row entry becomes `(Code, Kind)` and not `Code`**, with kinds at least
`With` and `After`. It is the revival condition on **two** refuted rows, and it is
what ordered output needs. **John's insight, and it is the `Groups` trick again:**
a phase cannot survive C2, so the front end SAYS the order inside the occasion,
where lateness cannot touch it — exactly as grouping did for binding. It also
explains the window: a carried edge is currently written into the same channel as
a simultaneous one, so the walk cannot tell *follows* from *accompanies*.

### 7. Credit over time — eligibility traces, and `Window` is already one

**The gap: nothing learns that an act led somewhere good three steps later.** No
reward function is available and backpropagation is not either. **Three-factor
Hebbian learning is the answer that needs neither** (Izhikevich 2007, the distal
reward problem): keep a fading trace of what recently fired, and let a third
signal consolidate whatever is still in it, most credit to the most recent.
**`Window` IS that trace, ungated.** Drives supply the third factor — out of
bounds is bad, returning is good — and `Surprise` is a second candidate. **Safe
for the CRDT property**, because the trace is transient state deciding how much to
add, and the counts stay monotonic.

### 8. Also likely necessary

- **VARIABLE BINDING.** *A is north of B* is a count between two codes, so it
  cannot apply to a new A and B. Without it every generalisation runs through
  similarity. **This is what un-parks vector-symbolic binding**, and `Clutrr` and
  `gSCAN` are the worlds that would force it.
- **REPLAY.** Re-run experience when nothing is arriving: consolidates, learns
  from rare events, interleaves old with new against interference. **`WhenIdle()`
  is already the trigger**, and fork 21 is its cousin.
- **INHIBITION.** The graph is purely excitatory — nothing says *this rules that
  out*. Buys competition between candidates, and a second route to absence.

### THE PATTERN UNDER ALL OF IT

**A row entry is one number doing several jobs**: it ranks, it prices, and it is
the only memory of the pair — no order, no recency, no kind. That is the
recurring fault at the level of the DATA STRUCTURE rather than of a dial, and the
general remedy is the same: **make it a record, `(count, when, kind)`.** That one
change carries kinds, supersession and eviction metadata together, and it has a
single price — memory per edge, which is the scaling wall. **So widen the row
once, not three times.**

**None of this is a sufficiency argument.** All of it could land and still not be
enough. The confident claim stays narrower: without structure, an internal error
signal, a growing alphabet, a reason to act, supersession, absence, concurrent
output and a bounded row, no amount of scaling gets there.

---

## TO BUILD — a ticked box means the type exists, and a test checks

**Standing, and each documents itself:** `Binding`, `Composed`, `Babi`, `Clevr`,
`Rhythm`, `Motif`, `Homeostat`, `Surprise`, over `Fabric`, `Seeds` and
`Measurement`.

- [ ] `Chunk` — the minted node of step 3
- [ ] `Drives` — the bounded internal variables of step 4

---

## LATER — nothing is blocked on these

- **Fork 1 is smaller than it looks.** A count that only increments is a
  **G-Counter, a CRDT** — it converges under reordering and loss with no
  coordination. The counts need no protocol; only the join does.
- **The absolute message cost is what step 2 attacks.** Nothing else should be
  optimised until it has been tried.
- **Cold storage, once a row can be bounded at all.** **Paging a node out to disk
  keeps the CRDT property** — the count does not decrease, it stops being
  resident. **Decay does not**, so eviction must key on "not touched since",
  never on eroding the count itself.
- **The knob pass, last.** A dial swept before the structural work measures a
  system about to change underneath it.

### The scaling wall — measure it before cutting anything

**STEP ZERO IS TO BUILD SOMETHING BIG ENOUGH TO BREAK.** The largest graph ever
run here is a few thousand nodes, so any optimisation now is aimed at a wall
nobody has hit. Full `Clevr` and the ten-thousand-story `Babi` both reach a far
larger graph; **measure, then cut.** Then, in order of leverage:

- **BOUND THE ROW.** Cap a node at K partners. This is the one that matters: it
  turns *cost per thought grows with data forever* into *cost per thought is
  constant*, which is the difference between a system that gets permanently
  slower as it learns and one that does not. Approximate-nearest-neighbour indexes
  run at billions on this trick. **Evict on "not touched since", never by eroding
  a count** — and that is exactly what the `when` channel above provides, so
  supersession and scaling share one mechanism.
- **A SELF-SET BEAM, whose revival condition is now met.** The refuted row asks
  for a width the system sets itself and reports; until `Surprise.Rate` there was
  no internal signal to set one from, and a node's own row statistics are a
  second.
- **Hierarchy, which is what step 3 is really for.** Do not walk a million nodes;
  walk a thousand chunks.

### The wire, when the remote half lands — John, 2026-08-03

Only the local half of `HybridBus` exists, so none of this is built.

- **Coalesce a settling wave into one send.** Hold remote envelopes until local
  traffic drains, then one datagram per destination; `WhenIdle()` is the trigger
  and is C1-legal. **Not a pure barrier** — flush on idle *or* size *or* time, or
  a busy machine never sends.
- **Bits, not JSON.** Addresses and modalities intern to small ints, a code is a
  varint, the `double`s are almost certainly `float`s, and **a sixth of a packed
  message is the `Guid` broadcast id** — shorten it per connection.
- **`Chain` is what costs** — cycle check and explanation in one field, free
  locally and not on a wire. **Split them:** an approximate-membership filter for
  the hop, full chain rebuilt at the origin. A false positive is a route wrongly
  refusing a partner, which is the loss C2 admits.
- **Voting multiplies the wrong half — John, 2026-08-03.** `votes: n` pays n
  floods to insure against loss on the way back. **One thought, redundant
  reports**: the flood is the graph's, and what C2 loses is the return path.
- **UDP matches, and is not a compromise.** C2 assumes loss, and **TCP's
  head-of-line blocking would stall every thought behind one lost packet.**
  QUIC's unreliable datagram extension (RFC 9221) is the shape.

---

## DO NOT RE-TRY

**Three columns, one line each, enforced by a test.** The third matters most: a
refutation is conditional on its configuration, so a row without a revival
condition is a superstition. The commit named in git holds the numbers.

| what | what refuted it | what would revive it |
|---|---|---|
| `StepCost.Best` / `Local` / `Constant` | Factorial message growth where inverse cost is polynomial | A bound not relying on positive cost at weight 1.0 |
| `Refuel` | Nothing is paid back, so it did nothing | Anything that returns budget to a route |
| Sender-*weighing*, `IMarginals` | A C1 violation, and behaviour was identical without it | Never. **But `Message.Seen` — the sender's OWN marginal — is legal and is built** |
| Absolute actions, unrotated view | One move in four instantly fatal | A body with no heading |
| Survival as the score | Circling wins: it lives longest and eats least | Homeostatic drives, where standing still stops paying |
| A beam over partners | A constant nobody set, doing the cutting | A width the system sets itself and reports |
| Clusters by modality | Splits picture from sound — the one link this design exists to make | Never |
| Clusters by time of creation | Two machines compute different owners for one code | Placement agreement without a coordinator |
| `Adaptive` reflection on `Hunger` | Inverted: it wrote most where it helped least | A signal that discriminates; `Thwarted` does |
| A deeper walk for prediction | Monotonically worse — without edge kinds, deeper reaches more and ranks worse | **Edge kinds**, and that refutation reproduced |
| `ArrivalValue.Lift`, `Accumulate.Max` | Swept, inert, both explanations refuted, `Max` re-tried where its revival condition pointed and worse there too. **Both now DELETED** | Lift in the **cost**, which `Doubt` is the nearest thing to |
| Naming fewer predicted codes | Half true: coarse ranking informs, fine does not | **REVIVED at one code.** Naming as many codes as the frame holds swamps the action entirely — fork 18's gap is flat at every sight radius. Naming ONE, it opens wide |
| `Window` span | Null on snake and WORSE on `Babi`, at an order of magnitude more traffic — but **it is the whole task on `Rhythm`**, where at zero the graph forms no edges at all. So the arm is live and world-dependent, which is the recurring fault wearing a new hat | **Edge kinds** — a carried edge is ranked against a simultaneous one as if they meant the same, which is why it helps where everything is temporal and hurts where things overlap |
| `includeEmpty: true` | Ruinous under `Best` pricing | **Revived — inverse cost removed the reason; no clear winner since** |
| `Pricing.Balanced` — `together / sqrt(seen·seen)`, cosine's denominator | **Times out.** The geometric mean sits BETWEEN the marginals, so weights rise and hops go cheaper than under either arm — the walk explodes rather than compromising. Built for a conflict that was a budget artifact anyway | A bound on the walk that does not rely on the weight being the reciprocal of one marginal — the same condition `StepCost.Best` needs |
| `Accumulate.Fused` — rank fusion over the two orders | Half of agreement's lift on the conjunction and ALL of its cost on binding. **Two candidates whose orders invert score identically under RRF for every damping constant**, so it ties exactly where it is needed and the tiebreak answers | A question with many candidates, or a fusion that separates by something other than position |

---

## TRAPS

**Closed in code — named so nobody reintroduces them.** Consecutive integer seeds
are not independent (`Seeds.Apart`). `Measured.Separation` returned zero where
repeated measurement found no spread at all (infinity now). `WhenQuiet()` was not
a finish signal (`WhenIdle()`). Questions were read before their walk had
finished, which made every number incomparable with any other (fork 22;
`Unsettled` is asserted). **Voting survives that fix and buys nothing in one
process** — kept, because a real network loses reports.

**Live:**

- **A DIAL MEASURED AT ONE SETTING OF ANOTHER MAY BE MEASURING THAT ONE.** The
  stamina plateau reversed between short and long runs; two pricings compared at
  one stamina read as a conflict that vanished at a higher one. **Sweep at two
  run lengths, and never compare dials with a third pinned.**
- **Short runs on the binding world score above chance for RECENCY ALONE**, and
  it decays with data. **Nothing under a few hundred scenes measures binding
  there.**
- **A dial can be declared, documented, passed everywhere and connected to
  nothing.** `ThinkAsync`'s stamina was, and survived the whole suite and three
  measurements. **Every run reports `Complaints`; read them.**
- **A small sample can look like a mechanism.** One seed with a collapsing echo
  read as a discovery and was three questions.
- **A MEAN OVER A POPULATION THE PROBLEM CREATED CANNOT SEE IT.** Fan-out stayed
  flat where rows grew without bound: the growth mints tiny nodes that hold the
  average down. **Read `Widest`.**
- **Copies drift where nothing fails.** Three worlds each grew their own settle
  loop, complaint list and vote tally, and a difference between them would move a
  headline without failing a test. `DuplicationTests` is the budget now.

---

## OPEN DEFECTS

**Fork 11 — the output machine is not addressed.** `Message.ReturnTo` is the
input machine; the harness hands the finished thought over by direct call. Needed
before a second machine exists.

**Nothing else outstanding.**

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
| **11** | The output machine is not addressed. **Open**, above |
| **12** | A fixed seed reproduces a run exactly, `Halted` included. Closed by 22's fix |
| **18** | Score prediction **conditional on the next action**. `Consequence` says the system does not model its own effect. **Blocked on temporal edges** |
| **20** | Split budgets — deep to act, shallow to predict. Closed |
| **21** | Compression. A trade: it pays where the budget cannot compose and costs where it can. Off by default |
| **22** | A transiently-zero live count untracked thoughts mid-flight and dropped every later report. `InputMachine.Retire` asks twice. Closed |
| **23** | Compression self-regulating? Not on this signal. `Thwarted` is the right shape and swings too little against the effect |
| **24** | Budget controller converges from both directions and **aims at a moving target**. Off by default |
| **25** | The binding world — built to fail, failed as predicted, and since lifted |
