# Where this is going

A different bet from `csharp`, on purpose. That branch counts co-occurrences and
walks them. This one counts CONDITIONED ON A PREDICTION, so what is counted is
attached to something that can be wrong. `csharp` is not abandoned and nothing here
refutes it — read its refutation table before repeating anything.

- **The only doc, and it holds nothing finished.** What a built mechanism does lives
  in its XML comments, where the compiler enforces every reference.
- **Findings live in the commit** that produced them, and in the test that asserts
  them. Never here.
- **One line an item.** A cap per ITEM, not per doc.
- **Built and decided means GONE FROM HERE, and no arm either.** A winner becomes the
  code; losers are deleted, leaving a revival row.

---

## The goal

- **Understand rather than perform** — answer *what would the world look like if I
  did X*, which a sequence model cannot be.
- **A COUNT IS NEVER WRONG; A COMMITMENT IS.** A cell that mispredicts becomes a
  different number. A commitment that mispredicts is wrong about SOMETHING, and which
  something is the whole of what can be learnt.
- **The counting does not go away, it moves under the prediction.** Repair asks which
  code separates misses from hits, which is `together / seen` indexed by commitment
  rather than by node.
- **The representation is the residue of repaired failures**, not a thing designed up
  front. Distinctions get minted to tell two conflated cases apart.

## The constraints

Carried unchanged from `csharp`. They are about the machine, not the architecture.

- **C1** — no node reads another's data. A commitment records its OWN hits and
  misses, and TELLS anyone who needs them at the moment it speaks.
- **C2** — messages are late, jittered, out of order.
- **C3** — a cluster vanishing mid-thought is NORMAL, not an error.
- **C4** — no episode boundary, so nothing may depend on train-then-test.
- **Merge monotone.** Hits, misses and abstains are G-Counters; they converge with no
  coordinator, and they are the only thing another node is ever told.
- **Decide local.** Each node also holds a recency-weighted accuracy over what IT saw,
  which never merges — because a lifetime average cannot track a world with no
  episode boundary.
- **Repair ADDS a narrower commitment and never edits the old one**, so monotonicity
  is preserved rather than strained.
- **Subsumption is the one exception and it is deliberate.** Where a scope and a
  narrower version of it are equally accurate, the GENERAL one stays — the narrower
  says nothing extra and covers less.
- **Keeping the narrower one is how a population drifts to one rule per instance**,
  which is the memorising this design is otherwise careful about. XCS is this way
  round for the same reason.
- **Codes are identical on every machine forever.** A commitment's identity derives
  from its SCOPE, so every repair path that reaches a scope converges on one name.
  Parent-plus-condition gave one scope two.
- **A front end may say what it is looking at, never what to conclude** — *this is
  the same thing you saw six times*, never *this is a red ball*.
- **ONE BRAIN, AND A WORLD MAY NEVER REACH INTO IT.** Brain dials are built once and
  handed in; a world turns only its own. `csharp` had `Ranking` set one way on bAbI
  and another on CLEVR, so a WORLD decided how the brain thought.
- **Every score was then a comparison between two brains as much as two problems.**
  `SeparationTests` fails the build if anything in a world names a brain type, because
  the rule was broken within an hour of being agreed.
- **The translation is a third thing and belongs at the join.** Whether a reading is
  banded or winnowed is neither a fact about the problem nor a setting on the brain.
- **Adaptation lives above the codes and never inside them.** The feature basis is a
  constant of the design; a learned feature is a minted name over co-firing codes.
- **So the demand for resolution may change how finely the front end cuts, and never
  what it cuts along.**

## TO BUILD

### The order, which is not the order of the sections below

- **One — step one, on the multiplexer.** The learner alone, and the part most likely
  to work.
- **Two — rung five, before step one is polished.** If abstraction fails nothing
  downstream matters, and the multiplexer has an answer key for it.
- **Three — the scaling exponent.** How observations-to-target grows with the number
  of relevant bits is what predicts whether this ever reaches perception.
- **Four — a world where `Winnow` does real work**, with a frozen published encoder as
  an arm against raw. `Cifar` is that world and BOTH HALVES ARE NOW MEASURED.
- **The encoders are in `corpora/encoders` and `fetch.sh` gets them.** CLIP ViT-B/32
  and MobileNetV3-Small, both frozen, so the red-ball property survives them.
- **The cheap one needed its classifier cut off.** A vector of class scores is a
  conclusion, and a front end may never have one.
- **AND THE ANSWER CAME BACK AGAINST THE BET.** See the grid under OPEN DEFECTS: the
  learner is fine and the front end is the ceiling.
- **Five — `Arranged` is built, and it inverted step four's verdict.** Same parts in two
  arrangements, opposite answers, soundness by enumeration on photons.
- **Six — the repair gate, which everything pointed at until it did not.** Three rules,
  three worlds, three winners, and no combination best on two.
- **AND IT IS NOT THE LIVE PROBLEM, BECAUSE ALMOST NONE OF THE POPULATION IS READ.**
  Fifteen of five hundred decide every withheld answer, so a gate that changes what is
  HELD cannot reach what decides.
- **SO THE LIVE PROBLEM IS WHICH RULE GETS THE SEAT**, and two arms at it have failed —
  see the revival rows before trying a third.
- **Eight — the rung the failures demand, and never the rung that sounds next.** Choosing
  before a failure asks is hand-specified bias by a side door.
- **AND ONE HAS ASKED NOW, WHICH IS WHAT STEP SEVEN WAS BUILT FOR.** `Monk` settles its
  own ceiling by enumeration rather than inferring it from a score — fork 50 is the rung.
- **Nine — the wire exists and the learners are not on it yet.** `Posted` moves envelopes,
  reports, finished thoughts and deaths between processes over real sockets.
- **What crosses is THINKING and never LEARNING, which is fork 1.** An occasion writes its
  edges into locally-held clusters, so nothing learnt on one machine reaches another.
- **And the commitment learner has no bus at all, so C1 holds by convention** — everything
  that constraint exists for is untested and `Abstain` cannot fire. Fork 52.
- **Distance costs the DEPTH of a thought, measured.** A round costs about four and a half
  times the per-hop delay, so a LAN is comfortable and the internet is a slower experiment.
- **THE VOTE SPLITS EXACTLY, so fork 52's arithmetic half is closed.** `Speak` emits what a
  holder claims and `Decide` merges any number of those; a commitment never crosses.
- **And the transport half is untouched** — nothing is late and nothing dies in any of it,
  so a green split says nothing whatever about C2 or C3.
- **EXCEPT THAT A MERGED NAME HAS A DEATH THRESHOLD, AND IT SEPARATES TWO FAILURES.** Up to
  a quarter of holders gone it names rightly or says nothing; past that it proposes a name
  the whole population would not.
- **WHAT BREAKS IS EVERY STATISTIC THAT IS POPULATION-WIDE**, and each mechanism was local
  or not by accident rather than by decision.
- **Rung five loses the power to CERTIFY a redundancy and goes silent**; its evidence is
  the population and the population is what gets split.
- **`Mending.Uncovered` loses the evidence to AIM a repair**, which is the same cause
  failing in the opposite direction.
- **AND THE GATE AIMS RATHER THAN LIMITS, WHICH IS NOT WHAT IT LOOKED LIKE.** `Mend` mints
  once a round, so admitting covered commitments misdirects the attempt rather than adding
  attempts.
- **A COUNT MERGES AND A STRUCTURE DOES NOT, which is the line between the two fixes.**
  `Recurrence` ships frequencies and recovers rung five exactly; `Narrows` has nothing to
  add up and wants a round trip.
- **AND `Abstract` TAKES WHAT OTHERS COUNTED, so the seam is in the learner rather than
  beside it.** It is the only operator in `Population` asking for anything off the machine;
  everything else decides local.
- **AND MERGING THE COUNTS IS NOT AN OPTIMISATION, IT IS THE ONLY THING THAT WORKS.**
  Machines sharing three quarters of a stream agree on names as poorly as machines sharing
  none; only identical evidence converges.
- **SO THE LAN CASE IS NOT SAFE EITHER.** Twenty phones on one wifi share most of what they
  see and not all of it, and most is worth nothing here.
- **Repair's own choice of condition survives splitting untouched**, being per-commitment —
  so *decide local* is doing exactly what it was written to do.
- **JOHN'S SPECULATION PROPOSAL, AND IT IS EARLY RATHER THAN WRONG.** Every node predicts
  its own output at once while the real wave verifies behind it, which is lossless where a
  learned short-circuit is not.
- **Its depth to save is not in the vote** — that is one scatter-gather. The repair gate's
  query is where it fits, being a boolean whose answer is usually no.
- **AND WHAT SPECULATION COSTS WHEN WRONG IS WHAT DECIDES WHERE IT IS ALLOWED.** A mistaken
  repair is a rule subsumption may remove; a mistaken vote is an answer already given.
- **The two-learner head-to-head is a side quest and blocks nothing**, being cheap now
  that both are co-resident.
- **Resist polishing step one.** It is the least informative part when it passes.

### The primitive

    Commitment := scope (codes that must all be present)
                → expects (a code that should follow)
                + hits, misses, abstains

- **It fires when its scope is a subset of the moment**, and is then right or wrong
  about something specific. That is the entire difference from a count.
- **A commitment's identity is a `Code`** — the same type a front end emits, so a
  commitment can sit inside another commitment's scope.
- **Which makes metacognition, chaining and abstraction expressible with no new
  machinery.** A handle or an index special-cases all three later.
- **Genesis is promiscuous and the gates do the work.** On a surprise, mint
  `{c} → y` for each `c` that was live when `y` started.
- **One code, because a whole-moment scope never fires twice**, and a covering
  probability is a mode declaration wearing a hat.
- **And because a one-code seed makes the bet falsifiable in the first world** rather
  than after a dial hunt.
- **So subsumption and deletion are step one, not later.** Cheap genesis is only
  affordable if something clears it.
- **`Surprise` gates genesis and has never run.** It had one caller on `csharp`, so
  this machine's expectation was empty at every observation in every world.
- **A moment's prediction is a weighted vote, not a winner.** Every matching
  commitment votes for its expectation weighted by its own accuracy; highest total
  wins.
- **An outvoted commitment still accrues its own hits and misses**, which keeps C1 and
  stops the winner monopolising the learning.
- **And the weight is accuracy, never hit count** — the strength-versus-accuracy
  refutation, arriving somewhere nobody expects it.
- **A PLAIN SUM IS THE SAME FAULT AGAIN.** Three commitments right half the time
  outvote one that is always right, so the population's COUNT decides. Accuracy is
  raised to a power first, which is XCS's own answer.
- **The margin between first and second is a confidence, free.** A persistently thin
  margin is the two-conflated-cases signal, already instrumented.
- **A miss is decided by settlement and never by a deadline** — *the settlement closed
  and Y was not in it* — so lateness cannot manufacture one.
- **`csharp` already built the machinery**: an outstanding-branch count and a
  retirement that asks twice with no report in between. Mattern's shape, at one
  machine's scale.
- **The horizon is in events, not time.** A settlement closes after the next K
  occasions; K=1 to start, which is decidable and order-independent.
- **Abstain is the third outcome and C3 requires it.** A cluster dying mid-settlement
  leaves both counters untouched, because a monotone count can never retract a slur.
- **A prediction carries its provenance**, and each entailing commitment's reliability
  travels with it, copied when it fired. A node is TOLD, never reads.
- **Depth is capped at one until something measures it.** Blame diffusion is the
  historical failure and the cap is why it does not arrive on day one.
- **Failure blames the provenance, not the world.** Rank by the reliability that
  arrived — stale under lateness, never wrong by race.
- **Repair is SPECIALISATION, and it is gated.** *Whenever X, expect Y* becomes
  *whenever X and Z, expect Y*, where Z clears the bars below.
- **AND Z IS WHAT THE HITS HAD, WHICH IS THE OPPOSITE OF WHAT IS EASY TO SAY.** A
  conjunctive child keeps the firings Z was in, so Z must lead in the HITS.
  Backwards, it mints a child that is reliably wrong.
- **A code more present in the MISSES is the right condition for a NEGATED one**,
  which is rung two. Conflating the two is how one sentence describes both and fits
  neither.

### The repair gate, which is the whole difference from overfitting

- **N misses before a commitment may be repaired at all.** Twenty as a floor, because
  below it no test of a proportion has any power.
- **Z must clear a separation bar and not merely be the argmax** — a two-proportion
  test between its rate in the misses and in the hits.
- **Corrected for how many candidate Zs were considered.** Search four hundred codes,
  take the best, and noise clears any fixed bar; that is the machine that minted 715
  names on pure noise.
- **A repair budget per parent**, so one commitment cannot fork forever.
- **And a control arm where Z is drawn at random** from the codes present in the
  misses. If discriminative-Z does not beat it, repair does nothing and the bet is
  dead.

### Step one

- **`Commitment`** — scope, expects, hits, misses, abstains.
- **Genesis** — surprise mints one-code commitments.
- **The vote** — accuracy-weighted, margin reported.
- **Settlement** — hit, miss or abstain, by closure and never by a clock.
- **Blame** — rank the provenance by the reliability it carried.
- **Repair** — one added code, past every bar, against the random-Z arm.
- **Subsumption and deletion** — or genesis buries the machine.
- **The world is the multiplexer and it is not an analogy.** Address bits select which
  data bit carries the answer, and which bits are irrelevant changes per instance.
- **It is *several cues arrive together and only some carry the outcome*, exactly** —
  the world `csharp`'s own plan lists as missing.
- **Generated, so no corpus can contain its own answer**, and XCS's canonical
  benchmark, so the external baseline is free.
- **And its ground truth is checkable by enumeration**, so a rule can be asked
  whether it is TRUE rather than whether it is the one expected. That is what catches
  right-for-the-wrong-reason.
- **A SINGLE ANSWER KEY WOULD MARK THE BASIS RATHER THAN THE LEARNER.** The world
  admits several correct rule sets, and the first run found one that was not the key's
  — scoring it against the key alone read as failure.

### What step one has to hit

- **The published accuracy, within ten thousand observations.** XCS is near-perfect in
  a few thousand; far below it is informative rather than fatal.
- **Discriminative-Z beating random-Z by a margin outside the seed spread**, ten
  seeds, counted in both directions.
- **A resident commitment count near the size of the true rule set.** High accuracy at
  ten thousand commitments is memorisation, and the count is what catches it.
- **How many resident commitments are SOUND** — true of the world, by enumeration
  rather than against a basis. The number step one is judged on, reported beside the
  unsound count because a count alone can be reached by minting everything.
- **Eleven bits reported with no bar**, as the scaling number.
- **The switching multiplexer** — flip the target mid-run, report steps to recover.
  The direct test of whether the local decaying estimate earns its keep.

### The ladder, which is how the scope language extends without a human

- **The rule is decidable and already computed.** The language extends when, and only
  when, no expression in the current language separates the failures from the hits.
- **Which is what repair discovers when nothing clears the bar.**
- **And the ladder has two directions, which is the correction that matters most
  here.** Rungs one to four only make a commitment narrower.
- **A specialise-only machine is arbitrarily accurate and conceptless** — it learns
  every multiplexer rule and holds no representation of *address bit*.
- **Rung one, conjunction** — *X and Z*. Step one, and the only rung built.
- **Rung two, negation** — *X and NOT Z*. What separates a failure from a hit is very
  often that something was ABSENT.
- **Negation is unsound against a live moment.** A live moment is incomplete under C2
  and C3, so a late Z makes *not Z* true and nothing retracts it.
- **So a negative condition may only be evaluated against a SETTLED occasion**, and a
  negative-scope commitment fires one settlement behind a positive one.
- **That latency is the price of absence**, and it is a constraint on the primitive
  rather than a detail.
- **And its candidate set must be bounded or the correction is hopeless.** Under
  sparse coding almost every code is absent.
- **Admissible: only a code that has appeared in this commitment's own hits** — *Z,
  which I have seen here before, is missing now*. It reuses the table repair keeps.
- **Rung three, sequence** — *X then Y* rather than *X and Y*. `csharp`'s `Kind.After`
  is the shape.
- **Rung four, roles** — a condition naming no argument is what buys transfer.
  `csharp`'s `Kind.Role` is the part of its edge vocabulary worth keeping.
- **And rung four is not a rung, it is a different matcher.** One to three keep
  matching a subset test; naming no argument requires UNIFICATION.
- **Which breaks the indexing that makes matching cheap**, and is the distance between
  a propositional learner and a relational one. Probe its cost before the escalation
  policy is designed.
- **Rung five, and it goes up.** When several commitments share a sub-scope, mint a
  code for the shared part and rewrite them in terms of it.
- **AND WHAT IT CAN NAME IS A SET, NEVER A VARIABLE.** The multiplexer's real
  redundancy is *these positions are the address, whatever they say*. Naming sets
  cannot reach that; it is rung four.
- **SO THE TWO RUNGS ARE NOT INDEPENDENT.** A code carrying position AND value
  together makes the shared thing unnameable. What is shared is the position, and
  nothing emits a code for one.
- **JOHN'S PROPOSAL, AND IT JOINS THEM.** Let `Winnow` emit SEVERAL codes per
  reading, so near readings overlap. The shared part of *bit three is zero* and *bit
  three is one* then IS a code, and naming can reach it.
- **Which would buy part of rung four without unification** — a variable becoming a
  shared sub-code rather than a binding. Cheapest test of the most expensive rung,
  and unmeasured.
- **What it costs is the soundness check.** A scope stops mapping to pinned bits, so
  the sharpest measurement here stops working as written. Say so before running it.
- **That code is then available inside any future scope**, including one that
  abstracts again — the recursion DreamCoder gets `sort` out of.
- **Its trigger is redundancy, not failure**, so it is the one rung a failure does not
  summon. Gated by `Paying`'s two bars.
- **And it is what makes the fixed front end survivable.** A minted name over
  co-firing codes is a learned feature that lives above the codes.
- **Load-bearing in four places**: hierarchy, transfer, learned features, and anything
  resembling one-shot learning.
- **Each rung is admitted for ONE commitment**, and only if the child clears the same
  bars.
- **The ladder's ORDER is still a bias** — over when a construct is tried, never over
  which are permitted. Weaker than mode declarations, and not nothing.
- **And when no rung clears, that is the signal the whole design exists for.** Two
  conflated cases with nothing to tell them apart is positing with a reason.
- **Which can be aimed at the front end.** A failure nothing explains is a localised
  demand for RESOLUTION: winnow these moments finer. It closes the loop to perception.

### Beyond the primitive, and not step one

- **A commitment ABOUT commitments** — metacognition, and where a self-model starts.
  It needs no mechanism at all if identity is a `Code`.
- **Action is EXPERIMENT** — act to test the commitment whose failure would be most
  informative. Interventional by construction; see `csharp`'s `Kind.Meddled`.
- **A goal is a commitment about a state that does not hold**, and planning is the
  attribution machinery run backwards.

### Known limits, carried as work rather than discovered later

- **The scope language is the ceiling.** Whatever a scope cannot say cannot be learnt
  — ILP's language-bias problem, and what killed the field. The ladder is finite.
- **The multiplexer does not test the bet.** Its inputs are already symbols, so step
  one measures the learner and measures the front end not at all.
- **THE INTERFACE COSTS MOST OF THE SCORE, AND THAT IS NOW MEASURED.** Same function
  and same learner, with readings spread rather than handed over as bits: both front
  ends fall from the mid nineties to near chance plus a tenth.
- **`Winnow` IS MOUNTED AT LAST**, on a world built for it — and that world cannot
  stress it. Crowding contracts every dimension toward one point, which leaves the
  projection's relative pattern untouched.
- **So what is needed is a world whose dimensions move INDEPENDENTLY.** The pairing,
  not population coding, is what the flat numbers are about. Say it before anyone
  reads them as a refutation.
- **The front end's resolution is a hard floor.** A fixed projection can split what is
  separable at some resolution and can never invent a direction.
- **AND THE FLOOR IS WHERE THE SCORE GOES, MEASURED RATHER THAN FEARED.** Same learner,
  same world, raw pixels against a trained embedding — see `EncodedTests`.
- **BUT HOW THE PROJECTION IS AIMED BEAT BOTH, AND RUNG FIVE WAS NOT INVOLVED.** Reading
  one patch at a time loses nothing a linear probe can find; reading the whole picture
  loses a tenth. See `Tiling`.
- **Nothing can be learnt from one example.** The gate needs N misses by construction,
  and the escape is not a smaller N — that is the 715-names failure — it is rung five.
- **The table is what blows up, not the commitments.** Repair needs a table per code
  seen while firing: commitments times distinct codes, both large under population
  coding.
- **MATCHING AND SETTLING ARE NINE TENTHS OF THE CLOCK AND THE SWEEP IS NOT THE COST**,
  measured on a narrow world whose table never grows — so it says where the TIME goes and
  nothing about the memory a wide one showed.
- **A child fires only where its parent does, and matching ignores that**, going through
  the code index instead. Rete's own problem; the wrinkle is that culling orphans a
  child, and an orphan that stops firing reads as nothing.
- **The tally is built for commitments repair may never read.** Nothing under the floor
  of misses can be repaired, so gating the tally on that same floor costs one comparison
  and may drop most of the table.
- **And the table is a frequency count, which a sketch bounds** — at the price that
  collisions overestimate, so the separation bar has to absorb the error or the
  correction beside it goes back to being decorative.
- **So the TABLE spills** — to SQLite, on the owning node, when a commitment goes
  quiet or clears its gate, rehydrated only if it becomes a candidate again.
- **The commitment itself is four fields and stays resident**, so no index of the
  evicted is needed.
- **A spill that changes what fires is an undeclared dial**, and one not reproducible
  under a fixed seed reopens fork 12 a fourth time.
- **A fresh child starts blind.** It inherits no table, so it must re-earn its
  statistics — a floor on how deep specialisation goes per unit of observation.
- **Quantisation boundary noise is the interface risk, and repair amplifies it.** Two
  identical worlds either side of a band emit unrelated codes, so specialising on the
  artifact MINTS it.
- **Counting degrades gracefully here and repairing does not.** `csharp` splits a
  boundary across two cells and averages; this fragments. Said out loud as the cost.
- **`Winnow` is the defence and it is mounted nowhere.** Overlapping winner sets mean
  a scope that is a SUBSET still fires, so the boundary stops being a cliff.
- **And the defence is what makes absence expensive.** The sparsity that survives a
  boundary is what unbounds the negative candidate set.
- **What graded codes cost is SEARCH** — more possible scopes, and blame over more of
  them. Measure it rather than assuming the robustness is free.
- **A miss could be PARTIAL, weighted by overlap.** Either elegant or a way to make
  everything mushy. Unmeasured.
- **Blame diffuses when many commitments entail one prediction.** The historical
  failure, and the depth cap is why it is not today's problem.
- **Two nodes can repair one parent differently and mint SIBLINGS.** Survivable only
  because fitness is accuracy and the worse one is subsumed.

### What comes over from `csharp`, and what does not

| | |
|---|---|
| `Agreed`, `Seeds` | The hash and the seed discipline. Load-bearing for the red-ball property |
| `Code` | The identity type, and now a commitment's identity too |
| `Bus`, `Ring`, `Addresses` | The distributed half. Its PAYLOADS are rewritten, since `Envelope`, `Settled` and `Report` are made of things on the Leave list |
| `IQuantizer`, `Coded`, `Winnow`, `Grains`, `Banded`, `Passthrough` | Front ends, independent of what consumes them |
| `LiveSet`, `Window`, `Occasion` | Moments and the stream |
| `Surprise` | The gate on genesis. Arm it — it ran nowhere |
| `Paying` | The gate on rung five. Both bars, never MDL alone |
| `IRendezvous`, and the outstanding count inside `Thought` | The termination detection. The mechanism comes over though the type does not |
| `Measurement`, `Questioned`, `Sweep`, `Plumbing`, `Seeds.Apart` | The harness. Its question-and-answer shape does not fit per-step prediction; budget the rewrite |
| `DocsTests`, `DeadCodeTests`, `DuplicationTests`, `DialTests` | The budgets. Bring these FIRST |
| The traps list and the refutation-table discipline | The epistemics engine, and the most valuable thing in the repo |
| Every world as a PROBLEM | `Babi`, `Clevr`, `Clutrr` and the rest survive; every `*Run` is walk-shaped and does not |

- **Leave, all of it rung-one machinery built on counting co-occurrence:** `Node`,
  `Edge`, `Kind`, `Tie` · `Thought`, `Message`, `Arrival`, `Settled`, `Question`,
  `WalkSettings` and every dial on it · `Chunk`, `Macro`, `Stated`, `Posit` ·
  `Drives`, `Foresight`, `Consequence`, `Reflection`.
- **Bring the IDEAS out of the minters without the mechanisms.** `Message.Chain`'s
  provenance, `Stated`'s star-not-a-clique, `Macro`'s sorted-versus-ordered naming,
  `Kind.Role`'s argument that a cell naming no argument transfers.
- **Cite the idea, never the type** — a cite into deleted code is how a plan rots.
- **And never cite a TIME either** — *one session ago* is true when written and false
  forever after, which is the same rot from the other end.
- **BOTH LEARNERS ARE STILL CO-RESIDENT HERE, so the head-to-head needs no branch at
  all.** `Graph`, `Learning` and `Thinking` sit beside `Commitments` in this tree.
- **So it is one world, one front end, one held-out set and TWO READERS** — the cleanest
  comparison this repo could produce, and the rewrite this doc budgeted for is smaller
  than it says.
- **A `Question` broadcast from a moment's codes is a per-step prediction in the walk's
  OWN terms**, so neither learner has to be bent to fit the other's harness.

### What the field already knows

- **Borrow the problem, not the mechanism.** This is not a new idea and pretending
  otherwise would waste months.
- **DreamCoder** (Ellis et al., 2021) — grows its own library under MDL pressure and
  BOOTSTRAPS: learns `filter`, uses it to learn `max`, then `sort`. The existence
  proof for representation-as-residue.
- **Popper / Learning From Failures** (Cropper & Morel, 2021) — generate, test,
  **constrain**. This design's core loop, already formalised — and GENERATE is the
  half this plan kept forgetting.
- **XCS** (Wilson) — accuracy-based fitness, because strength-based systems delete
  low-reward rules still correct in their niche. Its covering, prediction array and
  subsumption are all taken here.
- **And its recency-weighted accuracy is the one thing deliberately not taken.**
- **The Monk's problems** (UCI) — the classic symbolic benchmark, external baselines,
  small. The second world, after the multiplexer.
- **Monk-3 carries deliberate noise**, which tests the repair gate and nothing else.
- **Monk-2 is a counting concept a conjunctive scope CANNOT express** — a
  language-ceiling probe with a published number attached.
- **Why none of it scaled**: noise sensitivity, hand-specified language bias, and no
  way to learn from probabilistic or sensory background knowledge.
- **And the failure was at the interface with perception, not in the logic** — the one
  place this project is unusually well placed, because its substrate manufactures
  symbols. That is the bet, said plainly.

---

## DO NOT RE-TRY

A refutation is conditional on its configuration, so a row without a revival
condition is a superstition.

| what | what refuted it | what would revive it |
|---|---|---|
| Strength-based fitness for rules | XCS: it deletes low-reward rules that are still CORRECT in their niche | Never. Score a rule by how well it predicts, not by what it earns |
| MDL alone as a minting gate | On `csharp`'s `Motif` the pure-noise control minted 715 names | Never alone. Pair it with beating chance |
| Taking the argmax Z with no correction | The same finding read again: search enough candidates and noise clears any fixed bar | Never. Correct for the candidates considered, or the bar is decorative |
| A lifetime average as a rule's fitness | XCS uses a recency-weighted estimate because an average cannot track, and C4 says there is no boundary | Never as the DECIDING statistic. Monotone counters still merge and still convince |
| A miss decided by a deadline | C2 makes late indistinguishable from absent, and a monotone counter cannot retract | Never. Settlement decides; if settlement cannot, abstain |
| A minted name joining the occasion it completes | Its members are gone, so its only partner is its own last member. Broke two controls on `Rhythm` | A name reached by inference, never written as a partner |
| Hand-specified language bias | ILP's own post-mortem: mode declarations are where the human puts the answer in | A ladder the failures climb — and its ORDER is a bias this plan does not get to pretend away |
| A ladder that only discriminates | A specialise-only machine is arbitrarily accurate and conceptless | Never. Rung five, or the hierarchy claim is abandoned outright |
| Clusters by modality | Splits picture from sound, the one link this design exists to make | Never |
| A trained quantiser fitted per machine | Two machines fitted on different samples code the same input differently | A codebook reaching the same answer from any sample ORDER |
| Fixing a perception failure by changing the FEATURE BASIS | The fitted-quantiser refutation by a side door, and it gets proposed the first time a world will not code | Never. Resolution only; a new feature is a minted name above the codes |
| Reading C4 as a reason not to hold observations back | It constrains the LEARNER, not the experimenter, who is outside the machine and always was | Never. If the learner can tell it happened, that is a bug in the examination |
| A score on a trained front end with no probe beside it | Two unknowns multiplied, and a published bar was measured on another bench | Never. Same features, same held-out set, or the bar is decoration |
| Minting on every failure | It walks the whole `code -> outcome` space, which is a lookup table however it is scored | Never ungated. `Surprising.AnyFailure` survives as the ARM and not as a setting |
| Tuning a vote dial until both worlds pass | The peak of `Sharpness` moves between worlds, so it is the world's number and not the brain's | Never per world. A vote whose shape needs no number at all |
| Repair gated only on the vote being wrong | It hides the rules most needing specialisation — the ones wrong while the population is right | Never as the ONLY gate; `Mending` keeps it as an arm |
| `Mending.Earned` — repair on any earned failure, ignoring children | `Uncovered` dominates it everywhere measured, being the same rule with the redundant repairs removed | Never. Revive the child test, not the arm |
| The vote deferring to a general advocate unless the narrower earns it | Four points worse where it fires, and read-only it changes not one withheld answer | Never. The deciders earn their seats by a wide margin on drawn data |
| Genesis rooting on a code that has never varied | 7.4 standard errors behind where there is background, 0.2 apart without it. Background becomes a PARENT and its children inherit it | A world where an always-present code is informative. None is known |
| Subsumption weighed against DISTINCT occasions rather than firings | Chance on the noisy multiplexer, no sound rule left. Each condition halves where a rule can fire, so the bar caps DEPTH by world size | A bar whose expectation shrinks with depth too |

---

## TRAPS

Named so nobody reintroduces them. These are about MEASUREMENT, so they survive the
change of architecture entirely.

- **A check can be wired and unable to fire**, which reads as passing. Arm anything
  that has always read zero — `Surprise` is one, today.
- **A dial can be declared, documented, passed everywhere and connected to nothing.**
  Every run reports `Complaints`; read them.
- **A fallback is a control arm nobody meant to run** — silence drifts an arm toward
  the random bar for free. Report silence beside the score.
- **A ranking arm needs something to rank, AND ITS STATISTIC MUST DISAGREE WITH THE
  CONTROL'S.** Two comparable routes outsum one, so `Agreement` and `Sum` ordered
  alike everywhere and four sessions read a tautology as a bug.
- **Measure one mechanism ON from a known baseline, never one OFF from all-on.**
- **AND A SETTING CAN DECIDE TWO INDEPENDENT THINGS WHILE BEING NAMED FOR ONE**, so every
  comparison against it moves both axes. The cell that separates them may already exist
  and never have been read as a control.
- **A small sample can look like a mechanism, AND IT HIDES A REAL EFFECT TOO.** Count
  seeds in both directions.
- **A number in a commit message is a claim, not a record.**
- **A PERIODIC SWEEP INSIDE A CONDITIONAL RUNS AT THAT CONDITION'S RATE.** Subsumption
  and culling sat inside the failure branch, so at high accuracy they ran a handful of
  times in thirty thousand rounds and read as mechanisms that bought nothing.
- **A dial can be wired to ONE WORLD IN TEN**, and cashed in citing a finding as
  though it were general.
- **A CORPUS CAN CONTAIN ITS OWN ANSWER, and then a score measures the leak.** A
  generated world cannot, which is half of why the multiplexer is first.
- **AN ACCURACY CAN BE HIT BY MEMORISING.** Report the commitment count beside every
  score, and on a world with known ground truth report how much of it was found.
- **Two arms can peak at different budgets.** Compare PEAK TO PEAK.
- **AND A MECHANISM CAN BE RIGHT AND ITS OBVIOUS WIRING WRONG.** Minting a name is not
  the same decision as where the name goes; `csharp` broke two controls learning that.
- **A FILTER BEFORE A `Take` INVERTS AN EVICTION RULE WHEN THE POPULATION OVERSHOOTS.**
  The ask exceeded the eligible list, so the accuracy ordering chose nothing:
  everything experienced died and the young were immortal.
- **AND THE TELL WAS A DISTRIBUTION, NOT A SCORE.** Every commitment in the population
  topped out one short of the floor it was tested against. <b>A hard ceiling immediately
  below a threshold is never a coincidence</b> — read the spread, not the mean.
- **A GUARD MOUNTED ON ONE CALLER IS NOT MOUNTED.** `Tending` refused a modality block
  that would not fit while `Banded` itself took anything; past 128 dimensions the byte
  wrapped and two different pictures became one observation, silently.
- **A CODE PATH GUARDED BY A CAP IS UNTESTED UNTIL SOMETHING REACHES THE CAP.** Both
  of the above sat unexercised for the life of the repo because no world was wide
  enough. `Graded` holds 371 commitments; `Cifar` holds ten thousand.
- **ONE SEED IS NOT A COMPARISON AND WILL HAPPILY INVERT.** Winnowing beat bands on
  seed one and lost to them over five. Error bars before ordering, every time.
- **AND ITS DUAL: A PERIODIC SWEEP AGAINST A PER-ROUND RATE THAT SCALES WITH THE FRONT
  END.** Culling on the calendar while genesis mints per live code holds a population at
  many times its capacity. `Graded` is too small to show it.
- **A GATE NAMED IN THE PLAN AND MOUNTED NOWHERE MAKES THE WORD IT GATES MEAN SOMETHING
  ELSE.** *Promiscuous on purpose* meant EXHAUSTIVE for the life of the repo.
- **A COST CAN BE IN MEMORY WHILE EVERY INSTRUMENT WATCHES TIME.** No report has ever
  carried a byte count, so what actually bounded the run was invisible to all of them.
- **AN ONLINE SCORE BELOW WHAT THE FINAL POPULATION GETS ON FRESH OBSERVATIONS IS A
  CHURN SIGNAL.** It means the population is being destroyed and rebuilt faster than the
  trailing window can read it, and the run understates its own machine.
- **A DEPENDENCY'S DEFAULTS CAN BREAK REPRODUCIBILITY SILENTLY.** Parallel inference
  reorders float reductions, and a code is a QUANTISED number — so a reading at a band
  boundary codes differently run to run. Fork 12, arriving from outside.
- **TAKING OUTPUT ZERO OF A GRAPH IS A SILENT WRONG ANSWER.** A pooled embedding and a
  per-token hidden state are both plausible tensors and only one is a reading. Name it.
- **A MEASUREMENT INSIDE A REPORT IS ASSERTED ON BY EVERY EQUALITY READING IT.** A wall
  clock in a record turns reproducibility red and makes every `NotEqual` beside it pass
  for free.
- **A CLAIM ABOUT CORRECTNESS WILL DO DUTY AS A CLAIM ABOUT THROUGHPUT UNLESS SOMEBODY
  MEASURES.** C2 says messages are late and never said how late; tolerating lateness by
  construction is not tolerating any amount of it.
- **AND A SIMULATED CONSTRAINT CAN BE HARSHER THAN THE REAL ONE.** `HybridBus` reorders on
  purpose and TCP does not, so a green distributed run is evidence about bytes and routing
  and says nothing about C2.
- **AN ANSWER KEY IN THE WRONG ALPHABET SCORES NOUGHT AND LOOKS LIKE A VERDICT.** A key
  expecting a code the population can never hold reports no rule true, which reads
  exactly like a learner holding none.
- **A MECHANISM IS LOCAL OR POPULATION-WIDE BY ACCIDENT UNTIL SOMETHING SPLITS IT**, and
  nothing in one process can tell the two apart. Both halves of the ladder read the whole
  population and neither said so anywhere.
- **A PREDICTION WRITTEN INTO A WIRING CHECK FAILS TWO WAYS AND READS THE SAME.** Genuinely
  unwired and wired-but-backwards are one message; assert that arms DIFFER, never which
  way.
- **A WINNER-TAKE-ALL ARGMAX IS CHAOTIC IN ITS EVIDENCE, and two ends of a sweep cannot
  show it.** Naming picks one pair at a time, so a small difference changes the winner and
  everything built on it.
- **A TYPE CAN DROP MOST OF ITSELF ON THE WIRE AND STILL WRITE A PLAUSIBLE NUMBER.** Private
  tables and tuple keys serialise to nothing; what arrives looks like a message and merges
  as one.
- **SO PIN A FORMAT FAILURE WITH A CHECK RATHER THAN A COMMENT**, and assert the round trip
  on the ANSWER — equal fields say the members survived, the same decision says the
  arithmetic did.
- **AND A DEFAULT CAN SHORT-CIRCUIT THE MECHANISM BEING MEASURED.** `Mending.Outvoted`
  ships and skips the narrows test, so a sweep on defaults returned three identical arms
  for a gate that was never running.
- **AND A TIE-BREAK BY DICTIONARY WALK IS STABLE UNTIL THERE ARE TWO TABLES.** `Shared`
  resolved equal candidates by whichever its table reached first — reproducible in one
  process, arbitrary across a merge. Fork 12 by a new door.

---

## OPEN DEFECTS

- **MORE OF WHAT IT HOLDS IS UNSOUND THAN SOUND.** The vote scores well while
  carrying rules the soundness check refuses, at both widths — so what the vote
  tolerates is not what the world rewards.
- **AND TWENTY BITS PLATEAUS RATHER THAN BEING SLOW.** The same score at a hundred
  and fifty thousand rounds and at four hundred thousand, while sound rules keep
  rising — so it refines and stops improving.
- **THE REPAIR BUDGET IS A LEVEL AND WAS WRITTEN DOWN AS A GUARD.** Loosening it
  crosses the target at twenty bits; removing it is worse at every width. An interior
  optimum that moves with the relevant bits, and nothing reads that.
- **ROUNDS-TO-TARGET GROWS FAST IN THE RELEVANT BITS.** Six, eleven and twenty differ
  by one relevant bit each; the cost between them does not grow by one. That is the
  number that predicts whether any of this reaches perception.
- **`Abstain` IS UNARMED IN ANY RUN.** Nothing in one process can die, so C3's third
  outcome is exercised only by unit tests. It reads zero for the same reason a check
  reads zero when it cannot fire.
- **AND SPLITTING THE VOTE DID NOT ARM IT.** Losing a holder changes an answer and never
  silences one, because something else always advocates — the third outcome needs a death
  taking the LAST advocate.
- **THE CENTRAL BET IS REFUTED ON THE ONLY WORLD BUILT TO TEST IT**, with a control in
  every cell of the grid. `EncodedTests` holds it and the commit records the numbers.
- **THE FRONT END IS THE CEILING AND THE LEARNER IS NOT — ON CIFAR.** Changing only the
  front end moves the same learner most of the way to the probe's score.
- **AND ON `Arranged` IT IS FLATLY THE REVERSE.** A probe on the tiled codes loses
  nothing a probe on pixels finds, a handful of one-code rules cover every withheld
  scene, and the machine still falls well short of both.
- **On raw pixels the population and the probe are TIED**, so the information is not
  there for either to find. Nothing done to the learner could have bought it.
- **AND THE COMMITMENT MACHINERY IS COMPETITIVE GIVEN SYMBOLS WORTH HAVING**, recovering
  most of a linear probe on the identical vectors. That was not the expected result.
- **SO WHAT FAILS IS *A FIXED PROJECTION MANUFACTURES THE SYMBOLS*, and not the rest.**
- **RUNG FIVE WAS CALLED THE ONLY ESCAPE AND PATCH TOKENS GOT THERE FIRST.** A front end
  that reads parts rather than pictures raised the floor to perfect on the one world
  where perfect is knowable, and it abstracts nothing.
- **The seed spread on that grid is unmeasured**, which is not the same as small.
- **AND GOOD SYMBOLS ARE NOT FREE: the encoded arm costs an order more codes a moment**,
  so the price of the ceiling coming off is search.
- **AND THAT PRICE IS PAID ON THE WRONG MACHINES.** Encoding is cheap and runs on the
  input machines; the codes it makes multiply the search, which runs on the nodes.
- **A POOLED EMBEDDING HAS NO PARTS AND CANNOT CARRY AN ARRANGEMENT.** The CLIP export
  emits ONE vector for the whole picture — the holistic blob this doc warned about,
  arriving dressed as the solution.
- **So it answers *what is this a picture of* and can never answer *what is where*.**
- **PATCH TOKENS ARE THE FIX AND THEY ARE A `fetch.sh` CHANGE, not an architecture
  one.** A grid of regional readings is parts, and a scope can name a part.
- **Untestable until a world needs parts**, which is why step five comes first.
- **On CIFAR at equal code count, bands are AHEAD of winnowing over five seeds** —
  inside two standard errors, so not a refutation, and nowhere near the claim.
- **WINNOW'S ONLY DEMONSTRATED ADVANTAGE IS STRUCTURAL, AND IT IS REAL.** A modality
  is one byte and `Banded` spends a block per dimension, so it tops out at a thumbnail
  a few pixels across. Winnow has no such ceiling.
- **MORE RESOLUTION IS WORSE, AND THAT IS OUTSIDE THE SEED SPREAD.** Four times the
  information at four times the code count scores LOWER — the search cost this doc
  predicted, on the first world that could show it.
- **A WORLD THAT DRAWS WITH REPLACEMENT PAYS FOR MEMORISING, AND THIS ONE DID.** An
  unbounded population's score tracked how often an image RECURRED rather than what it
  had learnt; only a bounded one held up as recurrence went away.
- **AND IT IS MEASURED NOW RATHER THAN MITIGATED.** `IWithholds` keeps images the world
  never draws and `Trial.Examine` asks about them without teaching. `WithheldTests`.
- **A BOUNDED POPULATION SHOWS NO GAP**, so the earlier memorising was a fact about an
  UNBOUNDED one and not about drawing with replacement.
- **`Surprise` IS MOUNTED AT LAST AND IT WAS LOAD-BEARING.** Ungated genesis walked the
  whole `code -> outcome` space, cost most of the wall clock, and scored lower.
- **THE RUN WAS MEMORY-BOUND AND NO INSTRUMENT WATCHED MEMORY.** `Separations` is
  commitments times distinct codes, exactly as this doc predicted.
- **Gating genesis attacks that at the root**, and the spill is still the answer when it
  returns at scale.
- **AND THE GATE HAS ERROR BARS NOW: it leads on EVERY seed, counted both ways.** The
  accuracy ordering is no longer the single run this doc warns about.
- **AND THE SAME GATE IS BEHIND ON EVERY SEED ON `Arranged`, ON BOTH FRONT ENDS.**
  Self-limiting genesis stops early and never mints most of the rules that solve the
  world. Fork 40 asked whether that happens; it does.
- **THREE DIALS IN A ROW HAVE A BEST VALUE THAT MOVES WITH THE WORLD.** `Sharpness`,
  `Weighing` and `Mending` each win one world and ruin another, and no combination wins
  two. That is a world reaching into the brain.
- **AND THE REASON IS NOT A LEVEL NOBODY TUNED.** *Which rule needs specialising* and
  *did the population get this wrong* are different questions, and whether they align
  is a fact about the world. No per-round switch serves both.
- **THE VOTE THREW AWAY PART OF A WORLD IT HAD ALREADY SOLVED**, with every sound rule
  resident and believed well above the false ones — and under a scale-free vote it is not
  a crowd doing it, it is a handful of deciders.
- **THE CHILDREN THAT SINK `Arranged` ARE NOT MEMORISED, AND THAT WAS THE STANDING
  EXPLANATION.** A rule deleting children that stand on one repeated moment removed the
  same share as the rules blind to repetition, and reached the identical withheld score
  on every seed.
- **AND THE READOUT DID NOT MOVE WHILE THE POPULATION DID, BECAUSE ALMOST NONE OF IT IS
  READ.** Of five hundred residents, fifteen to twenty-two decide every withheld answer,
  and the arm that is perfect there decides with four.
- **SO A POPULATION-LEVEL ARM CANNOT REACH WHAT IT DOES NOT DISPLACE**, which is every
  gate, weighing and subsumption rule tried across four sessions — on ONE world, because
  the instrument needs a withheld set and a generated world has none.
- **THE VOTE PREFERS THE NARROWER RULE EVERY ROUND WHILE SUBSUMPTION PREFERS THE GENERAL
  ONE EVERY THOUSANDTH.** Same statistic, opposite directions, and the one acting
  constantly wins — a child displaces its parent as decider, then answers what it has
  never seen.
- **AND MAKING THE VOTE APPLY SUBSUMPTION'S BAR IS WORSE RATHER THAN BETTER**, minting
  three hundred more commitments where it fires — because the vote steers repair as much
  as it reports it, so no change to it is only a readout.
- **THE DECIDERS EARNED THEIR SEATS AND ARE STILL WRONG.** Handing every seat back to a
  general rule that has not beaten it changes not one withheld answer, so the readout is
  not the gap and drawn evidence cannot tell those rules apart.
- **AND IT IS NOT COVERAGE EITHER.** Doubling how much of the world is drawn leaves the
  gap flat while the drawn score stays perfect, and one-code rules hold the withheld set
  with half the world held back.
- **SO ON A WORLD WHOSE TRUE RULES ARE ONE CODE, ANY REPAIR IS DAMAGE**, and nothing
  computed from inside can tell that this is such a world.
- **MORE UNSOUND RULES WITH FEWER SOUND ONES SCORED HIGHER THERE**, so what the withheld
  score tracks is not how much of the world the population has got right.

---

## FORK NUMBERS THE CODE CITES

Never renumbered — `DocsTests` asserts each resolves.

- **1 through 25 are `csharp`'s and are not renumbered.** Most concern the walk and go
  when it goes.
- **When that code is stripped, point `DocsTests` at `csharp` for them** rather than
  weakening it. Decide that with the strip, not after.

| | |
|---|---|
| **1** | The distributed rendezvous. Open, and now the thing in the way: `Posted` carries the THINKING path between machines, and `LocalRendezvous` writes edges straight into locally-held clusters, so a machine can think across a wire and cannot learn across one |
| **3** | Cluster placement: uniform hash against prefix locality. Open, with a measured case — a uniform ring separates a child from its parent, which is the pair `Mending.Uncovered` compares. Prefix placement recovers much of it at unmeasured cost in load |
| **5** | A death writes off routes into the dead cluster. Closed |
| **6** | Broadcast the origin, route the hops. Closed |
| **11** | A finished thought is published and routed by code, so N actuators act on one broadcast. Closed |
| **12** | A fixed seed reproduces a run exactly. Closed — reopened and reclosed; `Receive` folded arrivals in delivery order |
| **18** | Prediction conditional on the next action. Answered by edge kinds |
| **20** | Split budgets — deep to act, shallow to predict. Closed |
| **21** | Compression as an edge. A trade; off |
| **22** | A transiently-zero live count dropped later reports. Closed |
| **23** | Compression self-regulating? Not on any signal found yet |
| **24** | Budget controller aims at a moving target. Deleted |
| **25** | The binding world — built to fail, failed as predicted, since lifted |
| **26** | Genesis mints per live code per surprise: does deletion clear them faster than surprise makes them? The question was wrong — nothing gated it, so genesis simply walked a finite space. Closed |
| **27** | Monotone counters merge, a local decaying estimate decides. Does the local one earn its keep? Open — predicted NO on a stationary world |
| **28** | The horizon is K occasions, K=1. Open |
| **29** | Divergent local repair mints siblings. Predicted survivable by subsumption. Open |
| **30** | A negative condition needs a settled occasion, so it fires one settlement late. Open |
| **31** | The table spills without changing what fires, and reproducibly. Open |
| **32** | Entailment depth capped at 1, horizon at K=1. Both come off when blame diffusion has a number — a cap with no trigger is a permanent decision nobody made. Open |
| **33** | Unification's per-match cost against a subset test. Probed before the ladder's escalation policy, not after. Open |
| **34** | Rung five names nothing at six bits and names and STACKS at eleven. The plan said this world had an answer key for it; it does not, because its structure is positional. Open |
| **35** | More unsound commitments resident than sound ones, while the score holds. Is the vote robust to them, or are they why it stops short? Open |
| **36** | Graded codes: does `Winnow` emitting several codes per reading make a position nameable, and what does it cost the search and the soundness check? Open |
| **37** | The repair budget has an interior optimum moving with the relevant bits. And `Mending.Uncovered` is two mechanisms — a gate plus every-round repair — where the gate alone is far worse than no gate at all. Open |
| **38** | Spreading a reading over its range costs most of the score at both front ends. Is that fragmentation, or the search the extra codes buy? Open |
| **39** | A reading under about ten dimensions has too few distinct wirings for a projection to expand into. Population coding has a floor, and it is not documented anywhere. Open |
| **40** | Does `Surprising.Unaccounted` starve genesis? YES, and at a WORLD rather than a width. On `Arranged` it stops early and holds a fraction of the sound one-code rules the ungated arm does, behind on every seed. Closed |
| **41** | The held-out gap as a function of RECURRENCE. A bounded population shows none at four draws an image; the question is where it opens, and that is the number that says how big a bag a world needs. Open |
| **42** | Rung five was called the only mechanism that raises a front-end floor. Patch tokens raised it to perfect on `Arranged` while abstracting nothing, so the question is now whether rung five buys anything a better-aimed projection does not. Open |
| **43** | Given symbols worth having, a conjunctive rule learner reaches 86% of a linear probe on the same vectors. What is the remaining 14%, and is it the scope language or the vote? Open |
| **44** | The tiled front end's patch is the arranged world's cell, so it is told where the parts are. Does the advantage survive a patch grid that does not divide the world's? Open |
| **45** | Three repair gates, three worlds, three winners, and conjoining two keeps one and loses the other. Is there a per-COMMITMENT signal separating *needs specialising* from *is being outvoted*? Open |
| **46** | Of five hundred residents, fifteen to twenty-two decide every withheld answer on `Arranged`, and the arm perfect there decides with four. A population-level arm cannot reach what it does not displace. Closed |
| **47** | Making the vote defer unless a child is significantly better costs four points where it can fire, and is inert under a permissive subsumption by construction. The vote steers repair, so it cannot be read as a readout change. Closed |
| **48** | Can a generated world hold assignments back without the learner being able to tell? Closed — it can, and the draw rejects rather than picks, so a run withholding nothing keeps every number the world ever produced |
| **49** | Matching and settling are nine tenths of the clock on a narrow world whose table never grows. Where do they go on a WIDE one, and is it the memory or the search that ends the run? Open |
| **50** | `Monk`'s second puzzle admits no sound conjunction saying YES short of a whole instance. Which rung does that demand — a count, a negation, or a disjunction — and does the failure distinguish them? Open |
| **51** | Genesis no longer roots on a code that has never varied. What is left is the TALLY: an always-present code is still an entry in every commitment's table forever, and background still roughly doubles it |
| **52** | The commitment learner runs in one process, so C1 is kept by convention. Half closed: the vote's arithmetic composes across holders, exactly under `Strongest`. The transport half is open — nothing has been late or died |
| **54** | Where between identical and disjoint streams naming stops converging. Answered, and there is no between: three quarters shared agrees as badly as nothing shared, and only identical evidence converges. One seed a row, so the ends carry it |
| **55** | Whether a blinded repair gate costs anything. Answered on the multiplexer: children minted separates decisively over twelve seeds, and accuracy, residents and sound rules all move the same way at two standard errors. Open on any other world |
| **56** | What the repair gate's query costs on a wire. Priced: about nine asks a round, all askable at once, so one round trip. The structural cache removes a sixth. Open on building it |
| **57** | Every node predicts its own output while the real wave verifies behind it, lossless where a learned short-circuit is not. The repair gate's query is where it first fits: a boolean usually answered no, and being wrong costs a rule. Open |
| **58** | `Mending`'s two-by-two has three cells; every-round-with-no-gate is not a setting. Completing it is a decision about the machine rather than a measurement, and without it the interaction is a reading and not a result. Open |
| **53** | A body pushes and `IWorld` pulls, and nothing in the loop has a clock. What does a learner whose rounds arrive on the world's schedule rather than its own do differently? Open |
