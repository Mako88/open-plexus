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
- **AND THE ASKING IS A NUMBER RATHER THAN AN ARGUMENT AT LAST.** The share of repairable
  rounds nothing in the language separates is a third on the counting concept and under two
  in a hundred on every other world measured.
- **THE CONDITION WAS DECIDABLE AND COMPUTED EVERY ROUND SINCE THE BRANCH BEGAN**, and read
  by nothing — `Repair.Discriminator` coming back empty IS the rule the ladder is admitted
  by. `Tally.Wanting` counts it.
- **AND NEGATION IS DEMANDED AND IS NOT ENOUGH, WHICH IS THE ANSWER DISTINGUISHING THE
  RUNGS.** An absence clears the same bar on about a third of the rounds nothing separated,
  over eight seeds.
- **SO THE OTHER TWO THIRDS SEPARATE ON NEITHER A PRESENT CODE NOR AN ABSENT ONE**, which is
  the counting concept itself. Rung two would pay part of that gap and cannot close it.
- **AND THE ABSENCE SHARE ELSEWHERE IS A RATIO OVER A HANDFUL OF ROUNDS**, so it carries
  nothing either way. Only the world with hundreds of unseparated rounds can be read.
- **Nine — the wire exists and one of the two learners is now on it.** `Posted` moves
  envelopes, reports, finished thoughts, deaths, questions and settlements over real sockets.
- **The WALK still learns nowhere but at home, which is what is left of fork 1.** An occasion
  writes its edges into locally-held clusters, so nothing it learns reaches another machine.
- **AND THE COMMITMENT LEARNER IS ON A BUS AT LAST, WHICH IS FORK 52'S TRANSPORT HALF.**
  `Ask` and `Answer` carry counts and testimony between machines that share no object.
- **PUSHED AND NEVER PULLED, WHICH IS JOHN'S RULE AND ALSO THE REVIVAL TABLE'S.** An awaited
  request would decide a missing holder by the client's timeout, which is a deadline.
- **AND THE LEARNER IS MOUNTED, WHICH COST THE HARNESS REWRITE THIS DOC BUDGETED FOR.**
  `Cycle` is async over a council — one population or a fleet of them behind the same two
  calls, so the learning loop still exists exactly once.
- **A ROUND IS TWO ROUND TRIPS AND A SWEEP ROUND IS THREE.** A vote must come back before
  the round can be scored, a settlement is pushed, and abstraction is the only operator
  whose statistic is the whole population's.
- **AND WHAT A FLEET HOLDS IS NOT DISJOINT, WHICH IS FORK 29 SHARPER THAN WRITTEN.** Genesis
  is placed and repair is not, so two parents on two machines reach one child — held twice,
  and a sum counts it twice.
- **PLACING THE CHILD WOULD DELETE IT INSTEAD**, since repair is the only thing proposing a
  scope longer than one code and nobody else would mint it. Minting it elsewhere puts a
  commitment on the wire, which C1 refuses.
- **AND PLACING BY ONE CODE CANNOT REACH MORE MACHINES THAN A WORLD HAS ROOTS**, which is
  twenty-three at eleven bits. So a placement rule can be capped by the front end's
  vocabulary, and twenty phones is already the edge.
- **A FLEET RUN REPRODUCES ITSELF EXACTLY, so fork 12 holds across sockets.** Every merge is
  ordered before it is combined and every placement is a fact about a commitment rather than
  about who asked or who answered first.
- **WHICH IS WHY TWO REPLICAS NEED NO MESSAGES BETWEEN THEM.** Fed one stream they mint the
  same children independently and stay identical, so redundancy costs coordination nothing
  and divergence becomes a free check on C2.
- **AND A REPLICA IS DEDUPLICATED BY WHAT IT SAYS RATHER THAN BY WHO SAID IT.**
  `Advocacy.By` already names the best advocate, so a merge can drop a duplicate exactly —
  and only where an expectation is worth that advocate.
- **THE CURVE SURVIVES DISTRIBUTION AND THE POPULATION DOES NOT.** A fleet scores what one
  process scores and holds twice the rules to do it, because a holder's repair gate can only
  refuse what a commitment ON THAT MACHINE covers.
- **AND SIX HOLDERS COST NO MORE THAN THREE**, so what distribution costs is paid at the
  first split rather than per machine. That is the number that says whether twenty phones
  are worse than two.
- **AND AT ELEVEN BITS IT IS AHEAD ON TWO SEEDS OF THREE, WHICH INVERTS THE SIX-BIT
  READING.** Genesis is placed and repair is not, so a fleet mints one child per HOLDER per
  round where one machine mints one.
- **SO HOW HARD A FLEET SEARCHES IS A DEPLOYMENT CHOICE**, which is a world reaching into
  the brain one level out. It pays where the true rules are conjunctions and costs a little
  where they are one code.
- **AND THE REPAIR GATE HAD NOTHING TO DO WITH IT, WHICH A CONTROL SAID AND AN ARGUMENT DID
  NOT.** `Mending` ships ungated, so the clause reading a placement never runs and a
  simulated shard is bit-identical to no shard.
- **Distance costs the DEPTH of a thought, measured.** A round costs about four and a half
  times the per-hop delay, so a LAN is comfortable and the internet is a slower experiment.
- **AND A ROUND OF ASKS IS UNDER A MILLISECOND ON LOOPBACK**, nine holders costing two and a
  half times one — so the scatter is in flight at once rather than a queue.
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
- **Ten — the vote rule, which now GATES the rest rather than sitting beside it.** Deciding
  it was a score question and is not: replication, wire deduplication and fork 29 all need
  the scale-free one and nothing else does.
- **Eleven — slots, because they make a death free and answer the loop's open half.** One
  machine per slot answering is a completeness condition rather than a deadline.
- **Twelve — placement by the minimum code**, which is cheap, priced, and the only thing
  that stops two parents reaching one child twice. It costs balance past a dozen machines.
- **Thirteen — rung two, whose payoff is written down BEFORE it is built.** About a third of
  a fifth on the one world that demands it, and a settlement of latency on every negative
  scope forever.
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
- **`Surprise` gates genesis, and it is armed and load-bearing.** Ungated covering walks
  the whole `code -> outcome` space; gated, it leads on every seed on the multiplexer and
  is behind on every seed on `Arranged`.
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
- **A repair budget per parent**, so one commitment cannot fork forever — and a TOTAL is a
  lifetime, which C4 refuses. What it should count is open.
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
- **AND WHEN TWO RUNGS BOTH CLEAR, THE SHORTER DESCRIPTION CHOOSES.** That is MDL in the one
  place it cannot mint noise, because it only ranks candidates that already beat chance —
  which is what its revival row demands.
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
- **AND THAT FLOOR IS WHAT THE CHAIN COSTS, MEASURED.** Every rung re-earns it, the world's
  true rules are three or four codes deep, and only the last rung ever pays.
- **AND A PARENT'S TABLE PREDICTS ITS CHILD'S FIRST CHOICE THREE TIMES IN TEN TO ONE IN TWO.**
  A majority only where the world is skewed, so a one-pass step would misfire on the even
  worlds and might not on the tilted one.
- **THOUGH A MAJORITY IS NOT ENOUGH WHILE THE TWO OUTCOMES COST DIFFERENTLY.** A right second
  code saves a miss floor; a wrong one mints a child too narrow to be sound, and what THAT
  costs is unmeasured.
- **AND THE INDIRECT EVIDENCE SAYS EXPENSIVE.** The subsumption that prunes harder holds fewer
  residents and buys MORE carriers, so a child that does not pay costs the search rather than
  merely occupying it.
- **SO SHORTENING IT NEEDS CONDITIONED COUNTS, WHICH IS THE TABLE THAT BLOWS UP** — argued
  rather than measured. A child could inherit its parent's table filtered by the added code,
  and that wants pair counts where the table is already commitments times codes.
- **Quantisation boundary noise is the interface risk, and repair amplifies it.** Two
  identical worlds either side of a band emit unrelated codes, so specialising on the
  artifact MINTS it.
- **Counting degrades gracefully here and repairing does not.** `csharp` splits a
  boundary across two cells and averages; this fragments. Said out loud as the cost.
- **`Winnow` is the defence and it is mounted.** Overlapping winner sets mean a scope
  that is a SUBSET still fires, so the boundary stops being a cliff — measured against
  bands and inside two standard errors of them.
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
| `Surprise` | The gate on genesis. Armed, and it decided a world |
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
| Handing every seat back to a general rule that has not beaten it, read-only | It changed not one withheld answer, and the in-run version cost four points | A world where drawn evidence can tell a parent from its child |
| Refusing an untested commitment its vote | They do decide wrong rounds, and excluding them moves no metric -- the seat passes to another wrong rule | A world where the right rule is usually present and merely outvoted |
| Generalising a rule for never being wrong | Not missing is nearly free for a narrow rule, so it picks the least-tested and mints unsound ones with wider reach | A gate reading how TESTED a rule is, not whether it was wrong |
| Loosening the repair budget, or gating it on the parent | Six cells over eight seeds found no more of the world's rules while repairing many times over | A world where the candidates it refuses are good ones. None known |
| Rung five proposing a pair it has already named | Mints nothing, and spends a third of the rung's asks. Skipping them nearly tripled the stacking | A rung minting more than once an ask, so a spent chance costs less |
| Subsumption weighed against DISTINCT occasions rather than firings | Chance on the noisy multiplexer, no sound rule left. Its halving explanation is measured true on even worlds only | A bar tracking depth as that world's reach does, which is not one direction |
| `Minting.UntilRefused` — naming until the gate refuses | Every count rose and hard-round coverage fell 2.7 standard errors; the extra sound rules were the LONG ones | A gate charging a name by what it stands for — fork 71 |
| A kill condition pre-registered on counts | `Minting`'s said the arm dies unless `named` or `stacked` moves. Both moved and it died anyway | Never on a column skew can raise. Pre-register on `Census.Paying` |
| `Weighing.Summing` — an expectation worth its advocates added up | Led on none of ten worlds once the vote stopped steering the search, and a sum splits inexactly | A world where a crowd outweighs one always-right rule |
| `Weighing.Lifting` — divide the best advocate by its answer's base rate | Beat `Strongest` nowhere in ten worlds, and trails worst where skew gives the divisor something to do | A world where an unusual answer on thin evidence is right |
| `Sharpness` — accuracy raised to a power before the vote | A workaround for a sum's shape: it cannot move the argmax of a maximum, and the sum is gone | Never under a maximum; only a summed vote returns it |
| `Stepping.Pair` — winner and runner-up in one repair | Coverage falls 2 to 4 standard errors on three worlds, accuracy with it, and the carriers overshoot the minimum sound depth | A machine knowing when a scope is deep enough — fork 75 |

---

## TRAPS

Named so nobody reintroduces them. These are about MEASUREMENT, so they survive the
change of architecture entirely.

- **A check can be wired and unable to fire**, which reads as passing. Arm anything that has
  always read zero; `Surprise` and `Abstain` were both found that way.
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
- **READ THE REVIVAL ROWS BEFORE PROPOSING A MISSING ARM.** `Mending`'s fourth cell was
  called absent here while its own row said *ignoring children* — the same axis in the
  mechanism's words rather than the comparison's, which is how a search misses it.
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
- **A DOCUMENTED PROMISE IS NOT A CHECK.** `Posted` said a fan-out was posts in flight
  while both of its fan-outs awaited each post in turn — false from the day it was
  written, and directly under the sentence describing the fault.
- **AND THE CLOCK CAME FROM THE OTHER PATH.** The thinking side had never been timed across
  a socket, so the defect was found by measuring the learning side — a check on one
  mechanism reaching a fault in another.
- **A BUDGET CAN BE SATISFIED BY A COINCIDENCE.** `Machines.Holder` read as wired because a
  tuple field in another file was spelt `Holder`; renaming it put two unmounted types onto
  the dead-code list at once.
- **AND A TIE-BREAK BY DICTIONARY WALK IS STABLE UNTIL THERE ARE TWO TABLES.** `Shared`
  resolved equal candidates by whichever its table reached first — reproducible in one
  process, arbitrary across a merge. Fork 12 by a new door.
- **A POLITE SHUTDOWN WAITS ON CONNECTIONS THE OTHER SIDE IS KEEPING ALIVE.** A one-second
  run had a teardown of minutes, so a grid of them read as a deadlock and the profile said
  the wire was fine.
- **AND A CAST TO AN INTERFACE THE TYPE DOES NOT IMPLEMENT IS CLEANUP THAT NEVER RUNS.** It
  compiles, reads as tidy, and does nothing — the pass-shaped defect again, wearing a
  disposal rather than a check.
- **EVERY WORLD ON THE BENCH DREW ITS OUTCOMES EVENLY AND NOTHING MEASURED THAT.** Any
  mechanism keyed on how COMMON an answer is was untestable here for the life of the
  branch while reading as tested.
- **AND A GRID OF IDENTICAL ROWS IS A VERDICT ON THE WORLDS RATHER THAN ON THE ARM.**
  Dividing by a constant cannot move an argmax, so eight rows four decimals apart were
  the bench saying it had no question.
- **AN ESTIMATE IS NOISE BEFORE IT IS A STATISTIC, AND A CHAOTIC RUN KEEPS THE
  PERTURBATION.** A divisor 1.36 at fifty rounds and 1.01 at twenty thousand mints a
  different population, so a mid-run reading and an end-of-run one are different
  measurements.
- **SIX EXPLANATIONS IN ONE SESSION DIED TO CONTROLS AND EVERY ONE CHANGED SELECTION.**
  Where the measurement says no right rule was present, a rule about who WINS cannot reach
  it. Ask which half of generate-and-test a proposal touches before building it.
- **AND THE INSTRUMENT THAT KILLS A STORY IS USUALLY BUILT FOR SOMETHING ELSE.** Twice in one
  session, then three times in another — so ask which grid already holds the number before
  running a new one, and build the instrument before the seventh story.
- **A `readonly record struct` HOLDING AN `ImmutableArray` COMPARES BY THE ARRAY'S
  IDENTITY.** Two separately built keys with identical contents are never equal, so an
  equality asserted on one fails on a world it has no complaint about.
- **AN EXACT PARTITION OF WHAT REACHED A MECHANISM SAYS NOTHING ABOUT WHAT NEVER
  REACHED IT.** Five gate shares summed to the candidates exactly and read as complete;
  the lineage that mattered was absent from the denominator.
- **AND A GENERATE-SIDE OPERATOR TRIGGERED BY THE VOTE'S ERRORS INHERITS THE VOTE'S BLIND
  SPOTS.** What may be repaired is then decided by what is already answered correctly,
  which is not a fact about the thing being repaired.
- **A LIST THAT APPENDS A DUPLICATE IS A COUNT WEARING A SET'S SHAPE**, and every reader
  gets whichever it assumed. `_minted` was the repair budget and the child set at once.
- **AND A READOUT ARM IS A SEARCH ARM WHEREVER THE READOUT TRIGGERS THE SEARCH.** Every
  vote comparison in four sessions moved both, and the cell proving it is two weighings
  building one population once the trigger is cut.
- **A FIXTURE INHERITS EVERY DIAL IT DOES NOT PIN, so a default moving rewrites an
  experiment nobody edited.** `BudgetTests` crosses two settings and pins neither timing
  nor budget; it changed arms silently and is a sweep, so CI never looked.
- **AND A TEST CAN FAIL AT BOTH ENDS OF A DIAL FOR OPPOSITE REASONS.** One end left the
  whole population unable to name, the other let every shard name alone — so pinning to
  the old value fixes nothing while reading as a fix.
- **A WORKFLOW IS THE ONE ARTIFACT WITH NO LOCAL CHECK, and it is wrong until a push says
  otherwise.** Three faults in one file: an invalid expression, a concurrency group copied
  from where it fit, and a matrix taking a runner per skip.
- **A LOCAL BUILDER'S INVARIANT IS NOT THE RECEIVED FORM'S.** No pair built here holds one
  code twice; the wire form takes whatever arrived. Such a row wins the argmax by
  construction, and the name minted for it throws.
- **AND SKIPPING WORK IS NOT SKIPPING A JOB.** A matrix entry that exits immediately still
  took a runner slot, so single-sweep dispatches starved the suite behind them.
- **A GRID CAN RANK ARMS ON COLUMNS A SKEWED WORLD RAISES FOR FREE, AND TWO DID.** Accuracy
  has a floor of four in five; `found` and `sound` are reachable by rules firing where
  guessing already works.
- **AND A GRID ASKING FOR ITS RUNS PER COLUMN BUILDS EVERY POPULATION ONCE A COLUMN.** One
  measurement printed as four, and the column that would have ranked the arms looked too
  expensive to add.
- **A SEARCH STEP OF TWO CODES IS A GATE PAID ONCE.** Repair clears the separation bar per
  code added; a minted name standing for two enters on that single clearance.
- **AN EXPLANATION CAN BE ARITHMETICALLY TRUE AND STILL NOT BE WHAT MOVED THE NUMBER.** A sum
  scales with the advocate count and a maximum does not; that predicted one sign change of
  three and got two backwards.
- **AN ACCURACY COMPARISON BETWEEN ARMS HOLDING DIFFERENT NUMBERS OF RULES WAS BIASED WHILE
  THE VOTE WAS A SUM.** The population is identical under both votes, measured; only the
  ballot changed, and a sum scales with the count.
- **TWO ARMS THAT SCORE ALIKE NEED NOT BE THE SAME MECHANISM, AND A SCORE CANNOT SAY.** A cap
  that refuses nothing and a cap that refuses a lot read identically until something counts
  what was BUILT. Four grids in one session.
- **A RATE WHOSE NUMERATOR COUNTS RULES SAYS NOTHING ABOUT HOW MUCH GOT COVERED.** The share
  of repairs that ever buy a hard round rose by half while hard-round coverage fell, because
  each deeper rule carries fewer rounds.
- **AND A SHARE WHOSE HALVES COUNT DIFFERENT EVENTS ANNOUNCES ITSELF BY EXCEEDING ONE.**
  Repairs that took two codes over children BORN read thirty-nine, because a lineage collides
  twenty to fifty times a birth. Arithmetic caught what a name did not.
- **DELETING THE LAST ARM DELETES THE CHECK THAT MADE THE DELETION LEGITIMATE.** *The vote
  builds one population* was asserted across three weighings; with one left it cannot be
  stated at all, so the property is an argument again.

---

## OPEN DEFECTS

- **MORE OF WHAT IT HOLDS IS UNSOUND THAN SOUND.** The vote scores well while
  carrying rules the soundness check refuses, at both widths — so what the vote
  tolerates is not what the world rewards.
- **AND TWENTY BITS PLATEAUS RATHER THAN BEING SLOW.** The same score at a hundred
  and fifty thousand rounds and at four hundred thousand, while sound rules keep
  rising — so it refines and stops improving.
- **THE REPAIR BUDGET'S INTERIOR OPTIMUM WAS THE BALLOT.** Re-taken under the vote that does
  not charge for size: hard-round coverage and sound rules rise monotonically to FREE at every
  width, and only accuracy still peaks at 256 — by a hair.
- **SO 256 IS A NUMBER ABOUT A DELETED VOTE AND NOT ABOUT THE SEARCH**, on the multiplexer.
- **AND `Arranged` DOES NOT CONFIRM IT: free is a point worse on the withheld set** at one
  standard error, holding 791 residents to 483. Where a world's rules are one code, repair is
  damage — so the budget's worth is the world's.
- **RUNG FIVE MINTS AT MOST ONE NAME AN ASK, AND THAT CEILING IS LOAD-BEARING.** Removing it
  was built and deleted — see the revival row. What it bounds is the search's STEP.
- **SO *NAMES PER ELIGIBLE SCOPE* IS A RATIO WITH A CAPPED NUMERATOR**, and it falls as the
  budget rises whatever abstraction does. The old row read that fall as the count's.
- **AND THE ONLY BAR THAT EVER REFUSES AT ELEVEN BITS IS THE CORRECTION**, which loosens
  rather than tightens as the population grows. `NamingYieldTests` partitions every ask.
- **THE GATE IS SCALE-RELATIVE IN PRINCIPLE AND NO RUN SHOWS IT.** With the learner removed,
  a fixed redundancy in a growing population walks to unnameable; on a run the extra scopes
  carry the redundancy with them.
- **AND NOT ONE NAME IN TWO HUNDRED AND FIFTY-EIGHT IS THE WORLD'S CONCEPT.** Zero group the
  address; they name data bits, or data mixed with address. The vocabulary compresses what
  repair happened to build, which is a fact about the population.
- **SIX BITS IS REFUSED ON POWER RATHER THAN ON HAVING NOTHING TO NAME**, which sharpens
  fork 34. Pairs repay and beat chance and cannot be certified.
- **A SOUND RULE IS NOT A GOOD RULE, AND NO READING SEPARATED THEM UNTIL A LENGTH WAS
  TAKEN.** Four codes is the shortest truth eleven bits has; the mean is 5.30 and a third are
  six or more.
- **AND EVERY ARM THAT RAISED THE SOUND COUNT RAISED THE LONG ONES FASTEST**, which is why
  that count cannot rank anything on its own. `Census.Paying` is the column that can.
- **THE LOCAL DECAYING ESTIMATE EARNS ITS KEEP ONLY WHERE THE TARGET MOVES.** Level with a
  lifetime average on a stationary world and ahead once the trailing window is entirely past
  a flip. Fork 27's prediction, held.
- **AND RELEARNING AFTER A FLIP IS SLOWER THAN LEARNING FROM NOTHING.** Five thousand rounds
  past one flip the run sits well under what it reaches from scratch in fewer.
- **AND THREE ACCOUNTS OF IT ARE DEAD.** Old rules squatting, genesis gated shut, subsumption
  eating the repairs — each had a mechanism, each got a control, none of them is it.
- **AND WHAT IT IS BOUND BY IS THE REPAIR BUDGET.** Free recovers where the capped arms do
  not, by five standard errors at six bits and ten at eleven, while a world that holds still
  stays level at both.
- **AND A PER-PARENT LIFETIME CAP IS WHAT C4 SAYS CANNOT BE ASSUMED.** A total spent over a
  parent's life is a bet that its life is one episode; the constraint says there is no
  episode boundary.
- **AND `Budgeting.Children` IS NOT THE FIX, IT IS FREE WEARING A CAP'S NAME.** Bit-identical
  to no budget on twelve cells of two widths. What should be counted so that it binds without
  being a lifetime is open.
- **AND A LIFETIME BUDGET DOES NOT DEGRADE WITH THE MOVES, IT FAILS AT THE FIRST ONE.** Its
  LEVEL stops mattering there too — sixty-four and two hundred and fifty-six sit a fifth of a
  standard error apart once the world moves at all.
- **AND `Budgeting.Earned` IS NEVER THE WORSE FIXED ARM AND NOT ALWAYS THE BETTER.** It leans
  free where free wins and capped where the cap wins, but leaves most of free's coverage gain
  unclaimed at eleven bits even.
- **AND THE SHORTFALL IS NOT VOLUME.** It buys seven repairs in ten of free's on both widths,
  and turns that into three quarters of the coverage gain on one and under a third on the
  other. WHICH repairs, not how many.
- **AND ON A STILL WORLD FREE IS NOT WORSE ANY MORE, WHICH IS THE LARGER SUSPICION.** A sum
  scales with the number of advocates and a maximum does not, so the budget's interior
  optimum may have been the deleted vote's crowd penalty.
- **AND FOUR SHIPPED DEFAULTS WERE CHOSEN PARTLY ON THAT COMPARISON**, each beating a
  larger-population rival on accuracy.
- **THREE ARE RE-TAKEN AND THE SIZE ACCOUNT EXPLAINS ONE.** `Surprising` and `Mending` moved
  the wrong way for a crowd penalty, so what the vote change does is not only charging for a
  population's size.
- **ROUNDS-TO-TARGET ACCELERATES IN THE RELEVANT BITS, WITH ERROR BARS AT LAST.** Six to
  eleven is under twofold; eleven to twenty is nearly sixfold. That is the number that
  predicts whether any of this reaches perception.
- **AND THE WIDEST WIDTH IS CENSORED, SO IT UNDERSTATES.** A quarter of seeds never hold the
  target inside the cap, and those are the slow ones. The true step is worse than the one
  printed.
- **`Abstain` FIRES AT LAST, AND WHAT ARMED IT WAS A SIGNATURE AND NOT A FLEET.** The loop
  took a non-nullable outcome, so no number of machines and no number of deaths could ever
  have produced one.
- **AND IT HAS ONLY EVER FIRED FROM A QUIET WORLD, NEVER FROM A DEATH.** A death silences
  the vote, measured — but a run losing a holder waits on its gathering forever, so this is
  behind fork 53 rather than behind the mount.
- **A QUIET WORLD COSTS AN OBSERVATION AND NOT A RULE**, which is the primitive's claim
  measured rather than argued. What it does to a world that is quiet MOST of the time is
  unmeasured.
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
- **AND THE SAME GATE STARVES GENESIS ON `Arranged`, HOLDING A TENTH OF THE SOUND ONE-CODE
  RULES.** Fork 40 asked whether that happens; it does, by a factor of ten.
- **AND THE ARM HOLDING TEN TIMES AS MANY SCORES NO BETTER** — which is the same finding as
  no carrier ever being a one-code rule. Sound singles are not what pays, on two worlds by
  two instruments.
- **THREE DIALS WERE THE WHOLE CASE THAT A WORLD REACHES INTO THE BRAIN, AND NOT ONE OF THEM
  DOES.** Two are deleted with revival rows and the third was two settings crossed. What is
  left of the case is below.
- **AND `Mending` DID NOT SHOW THAT PATTERN ONCE ITS TWO AXES WERE SEPARATED.** Every-round
  repair leads on both worlds measured — unseparated on one, near two standard errors on
  the other — so the row above may be about a conflation.
- **AND THE REASON IS NOT A LEVEL NOBODY TUNED.** *Which rule needs specialising* and
  *did the population get this wrong* are different questions, and whether they align
  is a fact about the world. No per-round switch serves both.
- **AND UNGATED EVERY ROUND MINTS THE FEWEST CHILDREN OF THE SIX, WHICH IS THE GATE AIMING
  RATHER THAN LIMITING.** Fewest residents, fewest unsound rules — and under a best-advocate
  vote it is level on score too, so it is cheaper for the same result.
- **SO THE AXIS IS NOT TIMING THEN GATE.** Ungated after a failure sits with the every-round
  group, so what is ruinous is the gate after a failure specifically — and the paid test
  adds nothing once repair waits for the vote.
- **AND THE THIRD LEG WAS TWO SETTINGS.** `Mending` is a gate and a timing now; every
  reading behind the row moved both at once, and the timing leads on both worlds.
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
- **GENESIS SATURATES ITS ONE-CODE SPACE IN THE OPENING HUNDRED ROUNDS** and never mints
  again. Its whole reach is the vocabulary times the outcomes, so every rule longer than
  one code has to come from repair.
- **AND TWO THIRDS TO ALL OF EVERY WRONG ANSWER IS A ROUND NO SOUND RULE ADVOCATED.** No
  vote rule can reach those, so every gate, weighing and subsumption arm was aimed at a
  third of the problem.
- **AND THOSE ROUNDS ARE NOT A CEILING: UNDER TWO IN A HUNDRED ARE UNREACHABLE.** On the rest
  something expecting the right answer DID fire — unsound, but present — so repair had the
  material to narrow and did not.
- **AND THE MISS FLOOR IS NOT THE BARRIER EITHER: a tenth at most are present but untouchable.**
  So on nine tenths repair was ALLOWED to narrow a parent expecting the right answer and did
  not. What is left is blame, subsumption, and QUANTITY.
- **AND QUANTITY IS THE ONE THE EVIDENCE CONFIRMS.** A child fires only where its added code
  is present, so covering what a parent is right about takes MANY children — and `uncovered`
  falls monotonically with the budget, 1354 at eight to 472 free.
- **THOUGH IT FLATTENS PAST 128 WHILE COVERAGE KEEPS CLIMBING**, so the top of the curve is
  buying something else: a hard round can gain a sound advocate without ever having been
  answered wrongly.
- **EVERY HARD ROUND THAT IS COVERED IS COVERED BY A REPAIRED RULE**, on all four worlds —
  genesis contributes none. Repair is the only source of coverage a base rate cannot give.
- **AND FOUR TO TWENTY-SIX PER CENT OF REPAIRS EVER BUY ONE**, falling with width and with
  skew. That is the search's hit rate, and it is lowest where the world is hardest.
- **AND THE ONES THAT PAY SIT AT THE WORLD'S MINIMUM SOUND DEPTH** — three codes at six bits,
  four at eleven, plus a fraction. Every step below it pays nothing by construction.
- **WHICH MAKES THE HIT RATE AND THE SCALING EXPONENT ONE PHENOMENON.** A wider world needs a
  longer chain and only a completed chain pays, so a relevant bit costs another step that must
  not go wrong.
- **AND THE NAMING LOOP WAS THAT AXIS FROM THE OTHER END.** A minted name let repair step two
  codes and it OVERSHOT the minimum depth; one code at a time undershoots it for three steps.
  Nothing in the machine knows the depth.
- **AND A STEP OF TWO OVERSHOOTS BY NINE TENTHS OF A CODE AT BOTH WIDTHS, MEASURED DIRECTLY.**
  Hard-round coverage falls two to four standard errors on all three worlds while the
  carriers' mean scope rises. Two mechanisms, one verdict.
- **AND IT BUYS MORE CARRIERS AT A HIGHER HIT RATE WHILE COVERING LESS.** A deeper rule fires
  less often, so the search's own quality number rose by half on the skewed world while the
  world got covered worse.
- **AND THE VOTE IS NOT THE CULPRIT, BECAUSE `Census.Paying` IS NOT A VOTE STATISTIC.**
  Accuracy and coverage fell together, so what shrank is what the population holds rather than
  which rule got the seat.
- **SO NOTHING STOPS THE CHAIN, WHICH IS THE FINDING RATHER THAN THE STEP SIZE.** Six bits
  reaches its minimum depth of three in ONE pair step and its carriers still average 4.41, so
  repair keeps narrowing well past a sound scope.
- **AND A STEP OF TWO FROM A ONE-CODE SEED MAKES EVERY EVEN DEPTH UNREACHABLE**, which
  eleven bits needs. Nine tenths of the steps took two, so the scopes go one, three, five —
  and the world's shortest truth is four.
- **AND IT LOSES WORST WHERE THE DEPTH IS REACHABLE, so parity is not all of it.** Six bits
  needs three and reaches it in one step, and falls furthest — a wrong second code mints an
  unsound child that can then only reach five.
- **AND SUBSUMPTION IS NOT CUTTING THE CHAIN: KEEPING MORE RUNGS BUYS FEWER CARRIERS.** The
  stricter rule holds more residents and lowers the hit rate on all three worlds. Pruning is
  part of the search rather than a tax on it.
- **THE OVER-SPECIALISATION STORY IS REFUTED BY ITS OWN NUMBER.** A child beats a sound
  parent on a fortieth of wrong rounds, so the vote preferring the narrower rule costs
  nothing measurable.
- **AND `Weighing` MOVES THE UNCOVERED BUCKET RATHER THAN THE OUTVOTED ONE**, which is a
  better SEARCH wearing a readout's name. Genesis mints the identical count under all
  three; only what repair does with them differs.
- **IT HOLDS TRUE RULES THAT FIRE ONLY WHERE GUESSING ALREADY WORKS.** On the skewed world
  no hard round has a sound rule advocating correctly, while sound rules sit resident at
  perfect accuracy.
- **AND `Found` MARKS THE BASIS RATHER THAN THE LEARNER, WHICH THIS DOC ALREADY WARNED.**
  Skew makes an all-agree rule cheap, sound and useless. `Census.Paying` is the reading
  that cannot be gamed.
- **AND ACCURACY SEPARATES TRUTH FROM FALSEHOOD BEST ON THE WORLD IT LEARNS LEAST**, so
  the fitness signal is not what fails there.
- **AND THE VOTE RULE'S SIGN FLIPS ON THAT READING, WHICH NO SCORE SHOWED.** A sum carries
  most hard rounds where outcomes are even and none where they are skewed; dividing by the
  base rate inverts both, and it is the only thing that has.
- **REPAIR'S CHOICE OF CONDITION PASSES ITS OWN KILL CONDITION** and is the most
  load-bearing thing measured. Drawing at random holds no sound rule on any world while
  repairing many times more.
- **AND EVERY LINK IS SOUND WHILE THE CHAIN MAKES NOTHING.** Minority seeds exist,
  choosing works, repair runs, true rules come out — and none of them fires where the base
  rate fails.
- **AND THE LINEAGE IS MEASURED NOW, AND IT IS NOT REPAIRED AND THEN LOST — IT IS NEVER
  BLAMED.** Four hundred to one against the majority at six bits, while the gates it never
  reaches refuse it LESS often than they refuse the majority.
- **BECAUSE REPAIR RUNS ONLY ON A ROUND THE VOTE GOT WRONG, AND UNDER SKEW THOSE ARE THE
  ROUNDS THE MINORITY RULES ARE RIGHT ON.** What expected what arrived is no culprit, so
  the only builder of hard-round rules is barred from them.
- **AND THE ARMS THAT PAY ARE EXACTLY THE ARMS THAT REDIRECT BLAME**, over three arms and
  two widths — a share of blame near nought pays near nought, and two unrelated ways of
  breaking the coupling both take it to four fifths and pay.
- **AND BREAKING THE COUPLING SURVIVED THE WORLD IT WAS PREDICTED TO RUIN.** `EveryRound`
  leads on `Arranged` at two standard errors over eight seeds, and is inside one on both
  even multiplexers. The kill condition was written down first and did not fire.
- **SO `Repairing.EveryRound` SHIPS AND `AfterFailure` IS THE CONTROL.** Sixteen standard
  errors of hard-round coverage at eleven bits skewed, four of accuracy, and no world
  measured is worse on score.
- **AND ITS NAMING AND SOUND-RULE COSTS WERE THE BUDGET'S, WHICH THE CURVE SETTLED.**
  Both were read at a budget that starves the search; loosened, neither cost survives, so the
  trade this row used to carry does not exist.
- **A SOCKET TEST INHERITED A REPAIR DIAL AND WENT RED WITH NOTHING WRONG ON THE WIRE.**
  Its precondition needs a population with something left to name, which every search dial
  moves. Both naming files pin the timing and the budget now.
- **AND A CHILD IS SUBSUMED ABOUT FOUR TIMES IN FIVE, WHERE THE CODE SAID IT NEVER
  HAPPENS.** A child that specialised on the wrong code is exactly as accurate as its
  parent, so the clause called unreachable is the population's main exit.
- **AND REPAIR SPENDS MOST OF ITS BUDGET RE-DERIVING WHAT IT HOLDS.** Collisions run twenty
  to fifty times the births at every majority rung, and a collision is charged to the
  parent's budget exactly as a child is.
- **AND NOTHING EVER STOPPED A PARENT PROPOSING THE SAME CHILD, WHICH IS WHY.** The argmax
  is stable for thousands of rounds and a commitment's table skips only its OWN scope, so the
  re-derivation was deterministic rather than incidental.
- **AND REFUSING IT ITS SPENT CODES SOLVES SIX BITS AND FLOODS ELEVEN.** Perfect on every seed
  there while holding no more than `Forking.Repeated`; at eleven it holds six times the
  residents for under two standard errors of coverage.
- **SO THE RE-DERIVATION WAS AN ACCIDENTAL POPULATION BRAKE**, and removing it needs a real
  cap to replace it. `Budget` has never been one, being far above the vocabulary that bounds a
  parent's distinct children.
- **SO `Budget` HAS NEVER LIMITED CHILDREN AND CANNOT.** A child adds one code, so distinct
  children are capped by the vocabulary — twenty-two at eleven bits against a budget of
  sixty-four. Every number ever taken under it is a re-derivation limit.
- **AND THAT IS WHY ITS APPARENT OPTIMUM MOVED WITH THE RELEVANT BITS**, which this doc
  carried as a puzzle for the life of the branch. Re-taken on the ungameable columns there is
  no interior optimum at all.
- **FREEING IT PAYS ONLY ONCE BLAME REACHES THE LINEAGES, WHICH IS WHY IT READ AS INERT.**
  Free under `AfterFailure` carries no hard round at all; free under `EveryRound` carries
  most of them and more than doubles the sound rules.
- **AND IT BUYS HARD ROUNDS AND SELLS EASY ONES ON AN EVEN WORLD**, which is the first arm
  measured here whose two scores point opposite ways. Coverage rises while trailing
  accuracy falls, and no single number can rank it.
- **AND IT IS A DOSE RESPONSE RATHER THAN A STEP.** Over five tilts the minority's blame
  share and the hard rounds carried fall together and monotonically, while the arm that
  never consults the vote stays flat on coverage.
- **AND AT THE STEEPEST TILT `AfterFailure`'S ACCURACY RISES WHILE ITS COVERAGE REACHES
  NOUGHT**, which is the base-rate trap drawn as a curve: the score goes up as the learning
  goes away.
- **UNDER `EveryRound` ALL THREE WEIGHINGS BUILD ONE POPULATION, EQUAL PER SEED** on sound
  rules, unsound rules, residents and rules found. Asserted now rather than observed, and it
  goes red under the old timing.
- **AND EVERY VOTE ARM MEASURED BEFORE THAT TIMING SHIPPED WAS A SEARCH ARM TOO**, because
  repair ran only where the winner was wrong. Every reason to keep `Summing` was read under
  it, and `Strongest` costs no true rules now.

---

## FORK NUMBERS THE CODE CITES

Never renumbered — `DocsTests` asserts each resolves.

- **1 through 25 are `csharp`'s and are not renumbered.** Most concern the walk and go
  when it goes.
- **When that code is stripped, point `DocsTests` at `csharp` for them** rather than
  weakening it. Decide that with the strip, not after.

| | |
|---|---|
| **1** | The distributed rendezvous. HALF ANSWERED: the commitment learner learns across sockets now, holders settling, covering and repairing what is placed on them. `LocalRendezvous` still writes the WALK's edges into locally-held clusters, so the graph learner is where this fork stays open |
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
| **27** | Monotone counters merge, a local decaying estimate decides. Does the local one earn its keep? ANSWERED and the prediction held: level with a lifetime average where the world holds still, ahead where the target moves |
| **28** | The horizon is K occasions, K=1. Open |
| **29** | Divergent local repair mints siblings. MEASURED, AND IT IS NOT SIBLINGS: two different parents on two machines reach the IDENTICAL child, so one rule is held twice and a sum counts its evidence per machine. Open on what that costs |
| **30** | A negative condition needs a settled occasion, so it fires one settlement late. Open |
| **31** | The table spills without changing what fires, and reproducibly. Open |
| **32** | Entailment depth capped at 1, horizon at K=1. Both come off when blame diffusion has a number — a cap with no trigger is a permanent decision nobody made. Open |
| **33** | Unification's per-match cost against a subset test. Probed before the ladder's escalation policy, not after. Open |
| **34** | Six bits is refused on POWER, not on having nothing to name; eleven names the wrong thing, with zero names in 258 grouping the address. Open on whether any world it reaches has a nameable concept |
| **35** | More unsound commitments resident than sound ones, while the score holds. Is the vote robust to them, or are they why it stops short? Open |
| **36** | Graded codes: does `Winnow` emitting several codes per reading make a position nameable, and what does it cost the search and the soundness check? Open |
| **37** | The repair budget's interior optimum was the ballot and does not survive a best-advocate vote. And `Mending.Uncovered` is two mechanisms — a gate plus every-round repair — where the gate alone is far worse than no gate at all. Open |
| **38** | Spreading a reading over its range costs most of the score at both front ends. Is that fragmentation, or the search the extra codes buy? Open |
| **39** | A reading under about ten dimensions has too few distinct wirings for a projection to expand into. Population coding has a floor, and it is not documented anywhere. Open |
| **40** | Does `Surprising.Unaccounted` starve genesis? YES, by a factor of ten in sound one-code rules on `Arranged` -- and it costs nothing on the withheld set, because a one-code rule never carries a hard round. Closed |
| **41** | The held-out gap as a function of RECURRENCE. A bounded population shows none at four draws an image; the question is where it opens, and that is the number that says how big a bag a world needs. Open |
| **42** | Rung five was called the only mechanism that raises a front-end floor. Patch tokens raised it to perfect on `Arranged` while abstracting nothing, so the question is now whether rung five buys anything a better-aimed projection does not. Open |
| **43** | Given symbols worth having, a conjunctive rule learner reaches 86% of a linear probe on the same vectors. What is the remaining 14%, and is it the scope language or the vote? Open |
| **44** | The tiled front end's patch is the arranged world's cell, so it is told where the parts are. Does the advantage survive a patch grid that does not divide the world's? Open |
| **45** | Three repair gates, three worlds, three winners, and conjoining two keeps one and loses the other. Is there a per-COMMITMENT signal separating *needs specialising* from *is being outvoted*? Open |
| **46** | Of five hundred residents, fifteen to twenty-two decide every withheld answer on `Arranged`, and the arm perfect there decides with four. A population-level arm cannot reach what it does not displace. Closed |
| **47** | Making the vote defer costs four points where it can fire, and reading a trained population back that way changes not one withheld answer. Closed, and the arm is deleted with a revival row |
| **48** | Can a generated world hold assignments back without the learner being able to tell? Closed — it can, and the draw rejects rather than picks, so a run withholding nothing keeps every number the world ever produced |
| **49** | Matching and settling are nine tenths of the clock on a narrow world whose table never grows. Where do they go on a WIDE one, and is it the memory or the search that ends the run? Open |
| **50** | `Monk`'s second puzzle admits no sound conjunction short of a whole instance. ANSWERED over eight seeds: a fifth of its repairable rounds separate on nothing against a two-hundredth elsewhere, and an absence rescues about a third of those |
| **51** | Genesis no longer roots on a code that has never varied. What is left is the TALLY: an always-present code is still an entry in every commitment's table forever, and background still roughly doubles it |
| **52** | The vote's arithmetic composes, its transport is real, and a whole learner now runs over it — placed genesis, local repair, merged counts on the sweep. Open on C2 alone, which TCP cannot show |
| **54** | Where between identical and disjoint streams naming stops converging. Answered, and there is no between: three quarters shared agrees as badly as nothing shared, and only identical evidence converges. One seed a row, so the ends carry it |
| **55** | Whether a blinded repair gate costs anything. Answered on the multiplexer: children minted separates decisively over twelve seeds, and accuracy, residents and sound rules all move the same way at two standard errors. Open on any other world |
| **56** | What the repair gate's query costs on a wire. Priced at one round trip and measured on loopback: 0.36ms at one holder, 0.93 at nine, so the fan-out is in flight at once. Open on a LAN and on building it |
| **57** | Every node predicts its own output while the real wave verifies behind it. What it saves is a LAN millisecond and an internet hundred, so where it pays is deployment. What a holder predicts from is a commitment about one |
| **58** | The gate's sign flips with the timing, and the whole two-by-two has been run rather than read off four rows and a revival note. What is ruinous is the gate AFTER a failure specifically. Open on any other world |
| **59** | Dissolved rather than decided. The setting was a gate and a timing crossed, so the cell that isolated the gate axis is not a cell — it is a corner of a grid, and both axes are now settings. Closed |
| **53** | The loop is asynchronous and holds no clock, so a fleet losing one message waits forever. ANSWERED IN PRINCIPLE by fork 62: act when one machine in every slot has answered, which is a completeness condition rather than a deadline. Unbuilt |
| **60** | Genesis is placed and repair is not, so a fleet repairs once per HOLDER a round where one machine repairs once. How hard it searches is a deployment choice. Place repair, divide the budget, or exploit it? Open |
| **61** | John's: place a commitment by the MINIMUM code of its sorted scope, so identical children land together and a lineage stays together. PRICED: level at three holders, three times the average at twelve, capped by a world's distinct roots |
| **62** | John's: partition into slots and give each R machines holding identical populations. A death costs nothing while a slot survives, replicas need no messages between them, and it answers fork 53. R scales with load |
| **63** | John's: genesis mints ONE scope over the whole scene and repeated scenes narrow it by overlap, the rest fading out. Specific-to-general, the DUAL of repair — an intersection needs two examples where the gate needs twenty misses. Open |
| **65** | The vote decides what repair may run on, so under skew blame lands on the majority lineages alone. `Repairing.EveryRound` breaks that and pays, at four to six times the repair. Open on what it costs at width |
| **66** | Is `Budget` a search limit or a re-derivation limit? ANSWERED: a re-derivation limit. Its level looked interior and stable across width, and that peak was the ballot -- on the ungameable columns there is none. Closed |
| **64** | John's: emit *Z was absent* as its own code at settlement, so rung two needs no negation in the scope language and no new matcher. Bounded to the commitment's own hits, or the candidate set is the vocabulary. Open |
| **67** | John's: a parent spends ENERGY to repair rather than a fixed count. Closed on stationary worlds -- REOPENED by fork 72, since a fixed total is what a moved target exhausts and fuel that returns is not |
| **69** | Naming until refused finds far more true rules and keeps a fleet agreeing. CLOSED AGAINST: it finds LONGER ones, and hard-round coverage falls while every count rises. Deleted with a revival row |
| **70** | Proposals naming a pair already named mint nothing. ANSWERED: spending the ask on the best unnamed pair nearly triples the stacking on both eleven-bit worlds and sells nothing. Shipped, and the arm is deleted. Closed |
| **68** | John's: does a conjunction EARN its narrowing? Its premise is half wrong -- reach halves with depth on even worlds and RISES under skew, where correlated codes let a long scope fire as often as a short one. Open |
| **74** | Could one repair add two codes, sparing a miss floor? CLOSED AGAINST, built and measured: coverage falls two to four standard errors on three worlds while the carriers overshoot the minimum sound depth. Deleted with a revival row |
| **76** | Repair proposes the same child until its table drifts, so a budget buys re-derivations. Refusing a parent its spent codes multiplies its distinct children eightfold at one seed. Does quantity buy the uncovered rounds, or only more population? Open |
| **75** | Nothing stops a chain at a sound depth. One code a step undershoots for three rungs; two overshoots by nine tenths of a code at both widths, by two independent mechanisms. What signal inside the machine could say STOP HERE? Open |
| **73** | What distinguishes a repair that buys hard-round coverage from one that does not? ANSWERED: depth. They sit at the world's minimum sound scope and every shorter step pays nothing. Open on walking a chain in fewer steps without overshooting |
| **72** | Relearning after the target moves is slower than learning the world from nothing. ANSWERED: the repair budget, a per-parent LIFETIME cap the lineages that must relearn have already spent. Free recovers, the capped arms do not |
| **71** | A minted name is a code, so repair adds it for the price of one. Should the separation bar be charged by what a scope's codes STAND FOR rather than by how many there are? Open |
