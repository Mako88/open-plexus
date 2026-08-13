# Where this is going

A different bet from `csharp`, on purpose. That branch counts co-occurrences and
walks them. This one counts CONDITIONED ON A PREDICTION, so what is counted is
attached to something that can be wrong. `csharp` is not abandoned and nothing here
refutes it — read its refutation table before repeating anything.

- **The only doc, and it holds nothing finished.** What a built mechanism does lives
  in its XML comments, where the compiler enforces every reference.
- **Findings live in the commit** that produced them, and in the test that asserts
  them. Never here.
- **One line an item.** A cap per ITEM, and a cap on the WHOLE.
- **JOHN'S TEST, AND IT IS THE ONE THAT DECIDES: if it is long enough that you would
  hesitate to load all of it, it is too long.** This doc exists to be read whole at the
  start of every session. A doc read in pages is the pile of docs it replaced.
- **So the budget only ever goes DOWN**, and `DocsTests` fails the build the moment the
  doc grows. What will not fit belongs in a commit, a test, or an XML comment.
- **Built and decided means GONE FROM HERE, and no arm either.** A winner becomes the
  code; losers are deleted, leaving a revival row.

---

## THE DESTINATION

What must be true when this is finished, and it does not change. **The route is where
everything moves; nothing here does.**

### The bet

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

### THE ARCHITECTURE

John's, and the one section written to STAY. It says what the brain must DO and never how —
a mechanism written here is a decision wearing a requirement's clothes. No finding enters.

- **A BRAIN THAT UNDERSTANDS CONCEPTS.** It holds a model of the world and answers from it,
  never by matching the surface of a question to the surface of a text.
- **A CONCEPT IS A THING IN ITS OWN RIGHT** — distinct from every other and interrelated
  with every other. That pairing IS the understanding: what a thing is, and how it stands to
  everything else.
- **AND EVERY INPUT IS AN ATTRIBUTE OF A CONCEPT, NEVER THE CONCEPT.** The look of a thing,
  the sound of it, its temperature, its name: each is one way it shows through one sense. The
  thing is what they are all attributes OF.
- **AND RELATIONS ARE CONCEPTS TOO** — association, containment, ownership, movement. If the
  meta level is not representable then the model is a list rather than an understanding.
- **AND A CONCEPT AND ITS LABEL ARE INDEPENDENT.** Either may arrive first: a slot with no
  word for it yet, or a word for a thing nothing else is known about. Both must be reachable.
- **AND WHAT IS UNDERSTOOD OF A THING DEEPENS AND BROADENS WITHOUT LIMIT**, thousands of
  truths about one individual, each sharpening what it is.
- **AND PART OF WHAT IS UNDERSTOOD IS WHICH ASPECTS ARE TEMPORAL** — which properties come
  and go, what those aspects are in themselves, and how they stand to everything else.
- **AND KNOWLEDGE IS HELD AT SEVERAL GRAINS AT ONCE** — *a person sleeps in a bedroom* and
  *this bedroom is Mary's*, both live, neither replacing the other.
- **AND HOW HARD A BELIEF IS TO SHIFT IS ITS OWN RECORD, NEVER A WEIGHT.** Gravity is
  immovable because that belief has vast evidence and has never missed; Mary's room moves
  because that one misses constantly.
- **AND IT LEARNS BY BEING WRONG AND FINDING OUT.** It predicts, is scored, and refines.
  What supplies the scoring is left open — a question asked, an action taken, a consequence
  observed.
- **AND IT MAY BE TOLD AND MUST NEVER BE ARCHITECTED.** A primer may teach it that a room is
  a space people enter. What is forbidden is an ontology built in by hand: the representation
  is what learning left behind.
- **AND WHAT IT IS TOLD MUST BE SOMETHING IT CAN BE WRONG ABOUT.** Told and configured are
  indistinguishable from the inside, so a fact it cannot fail on was not taught to it — it
  was installed in it.
- **AND FROM THAT UNDERSTANDING IT PRODUCES ORIGINAL THOUGHT** — conclusions it was never
  told and could not have reached by matching.

### The constraints

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

### The first north star

John's, and it is a stepping stone rather than the goal above. Written down because it
FORBIDS things, and several of them are already measured.

- **Twenty used phones on one wifi running the brain**, one stronger machine carrying the
  body — camera at a frame or two a second, audio, temperature, motion.
- **AND THE PHONES ARE LAST RATHER THAN FIRST, WHICH IS JOHN'S ORDERING.** They do not exist
  yet and will not be bought until this is worth showing somebody, so the hardware is a
  conclusion of the prototype rather than a precondition for it.
- **FIRST IS DOCKER CONTAINERS ON ONE BOX, THE BODY A SEPARATE PROCESS, AND THE ONLY SENSE
  IS ENGLISH.** Talking to it is the first interactive test of the whole system, and it needs
  no camera, no phone and no wifi — the fleet is already the thing being exercised.
- **THEN THE PHONE AS THE BODY AND THE CONTAINERS AS THE BRAIN**, video and sensors in, if
  one machine can hold enough containers to be a fleet. That is the arrangement that says
  whether twenty of anything is needed before twenty of anything is bought.
- **And a text conversation as a second body**, so one brain is judged on symbols and on a
  stream of the world without being two brains.
- **THE CONSTRAINTS WERE ALL WRITTEN FOR THIS, so it is a demonstration rather than a
  benchmark.** No shared memory, late messages, a cluster vanishing mid-thought.
- **SO THE CONTAINERS ARE NOT THE THING THAT WAS UNPROVEN**, and the fleet was never the
  risk. What text teaches this learner is answered in one process on one box, which is where
  it should have been asked.
- **AND TWENTY IS ALREADY THE MEASURED EDGE.** Placement by minimum code is capped by a
  world's distinct roots, so fleet size and the front end's vocabulary are one number.
- **SO WHAT THE FOOTPRINT READING IS ABOUT CHANGES WITH THE RUNG.** Containers share one
  machine's memory, so the whole fleet's brain is the number that matters there; on phones
  it is one holder's share. Both come off the same reading.
- **AND WHAT PRICES A CAMERA IS THE FRONT END'S VOCABULARY**, at residents times codes times
  a hundred bytes rather than anything about the population. That is the number to take
  before a sensor is plumbed.
- **AND ONE INPUT MACHINE FEEDING TWENTY HOLDERS IS THE IDENTICAL CASE, NOT THE SHARED
  ONE.** Every holder is told the same moment and the same settlement, so naming converges —
  the divergence measured was between machines seeing DIFFERENT streams.
- **SO A CAMERA PER PHONE IS THE ARRANGEMENT TO AVOID**, and it is the arrangement nobody
  proposed. What breaks naming is many eyes, not many brains.
- **A SENSOR IS A WORLD AND A WORLD IS A STREAM**, so nothing here is a new kind of input —
  what is missing is the plumbing, not a mechanism.
- **AND EVERY GROUND-TRUTH INSTRUMENT GOES DARK.** Soundness, overshoot and hard-round
  coverage all need a world that can be enumerated, so the generated worlds never leave.
- **SO THE PROBE IS THE ONE INSTRUMENT THAT CROSSES.** Ask what the codes carry against what
  the raw reading carries, per sensor, BEFORE building anything on that sensor.
- **AND A CURRICULUM IS ALLOWED AND C4 IS NOT ABOUT IT.** *No episode boundary* constrains
  the LEARNER; what an experimenter feeds and in what order is outside the machine.
- **SO A PRIMER BEFORE A TEST IS EXPECTED RATHER THAN A CHEAT** — the language before the
  play, the room before the question. What is forbidden is the learner being able to tell
  that a boundary happened.
- **JOHN'S CURRICULUM PROPOSAL: TEACH IT ENGLISH, THEN SET IT A TEXT AND EXAMINE IT**, which
  is the shape the first conversation takes.
- **AND MIXING THE TWO IS WORSE THAN EITHER TEACHING ALONE**, so that curriculum costs rather
  than pays as built. The function-word rules crowd out the population the questions needed.
- **AND THE FIRST CONVERSATION DEMANDS UNIFICATION RATHER THAN SEQUENCE**, which inverts what
  the ordering assumed. Binding the question's actor to a statement's actor is rung four, and
  no dose of recency reaches it.
- **AND THE EXAM IS ALREADY CHOSEN.** Twenty tasks each isolating one prerequisite, written
  elsewhere with published baselines, and built so surface matching fails. A school
  comprehension test would read nothing until the components pass.

### What the field already knows

- **Borrow the problem, not the mechanism.** This is not a new idea and pretending otherwise
  would waste months.
- **DreamCoder** (Ellis et al., 2021) — grows its own library under MDL pressure and
  BOOTSTRAPS: learns `filter`, uses it to learn `max`, then `sort`. The existence proof for
  representation-as-residue.
- **Popper / Learning From Failures** (Cropper & Morel, 2021) — generate, test, **constrain**.
  This design's core loop, already formalised, and GENERATE is the half this plan kept
  forgetting.
- **XCS** (Wilson) — accuracy-based fitness, because strength-based systems delete low-reward
  rules still correct in their niche. Its covering, prediction array and subsumption are all
  taken here; its recency-weighted accuracy is the one thing deliberately not.
- **The Monk's problems** (UCI) — the classic symbolic benchmark, external baselines, small.
  Monk-3 carries deliberate noise, which tests the repair gate and nothing else; **Monk-2 is a
  counting concept a conjunctive scope CANNOT express**, a language-ceiling probe with a
  published number attached.
- **Why none of it scaled**: noise sensitivity, hand-specified language bias, and no way to
  learn from probabilistic or sensory background knowledge. **And the failure was at the
  interface with perception, not in the logic** — the one place this project is unusually
  well placed, because its substrate manufactures symbols. That is the bet, said plainly.

---

## THE ROUTE

Everything that is built, unbuilt, refuted or broken, against the requirement it serves. A
branch is what must hold, an entry is one requirement, and a leaf is one line opening with
exactly one of **NOW**, **OPEN**, **DEAD**, **BLOCKED**, **BROKEN** or **SETTLED**. A fork
gets one home and a cross-reference by number from anywhere else it serves; numbers are never
renumbered. Forks 1 through 25 are `csharp`'s, and when that code is stripped, point
`DocsTests` at `csharp` for them rather than weakening it.

    Commitment := scope (codes that must all be present)
                → expects (a code that should follow)
                + hits, misses, abstains

- **WHAT IT MUST DO** — one entry a line of THE ARCHITECTURE, in that order, and a guard
  holds the two in step.
  - Understand concepts
    - **NOW** — a commitment fires when its scope is a subset of the moment, and is then
      right or wrong about something SPECIFIC. That is the entire difference from a count.
    - **NOW** — its identity is a `Code`, the same type a front end emits, so one can sit
      inside another's scope — which makes metacognition, chaining and abstraction
      expressible with no new machinery.
    - **NOW** — everything else about it is in the XML comments and the compiler enforces
      them: genesis and its gate, the vote and its weighting, settlement, blame and repair
      all live beside the code in `Commitments`.
    - **OPEN** — nothing here answers what a concept IS beyond a code that fires.
    - **OPEN** — the scaling exponent: how observations-to-target grows with the number of
      relevant bits is what predicts whether this ever reaches perception. UNMEASURED.
    - **OPEN** — the scope language is the CEILING: whatever a scope cannot say cannot be
      learnt. ILP's language-bias problem, what killed the field, and the ladder is finite.
    - **OPEN** — six bits is refused on POWER and eleven names the wrong thing; whether any
      world the naming reaches holds a nameable concept. Fork **34**.
    - **OPEN** — genesis no longer roots on a code that never varied, and an always-present
      one is still an entry in every table forever. Fork **51**.
  - A concept a thing in its own right
    - **NOW** — `Code`. A commitment's identity is one, and adhesion over a window reaches
      one group a persistent SOURCE.
    - **OPEN** — nothing tracks a source through a CHANGE, so a thing that moves is a new
      one, and adhesion reaches a source and never an individual. Fork **106**.
    - **OPEN** — minting an INDIVIDUAL is unbuilt. Where a thing never moves a source and a
      thing are one set; where it moves they come apart and holding the category costs.
    - **OPEN** — co-firing binds what is SIMULTANEOUS and never what persists, so a thing at
      two moments does not co-occur with itself and no amount of it reaches the same thing
      seen twice.
  - Every input an attribute of it
    - **NOW** — several front ends manufacture symbols from a signal, and each is priced.
    - **OPEN** — nothing makes them attributes of one THING. Rung five names what co-fires,
      which is what a seen ball and a heard *ball* do, and it has never run across two
      modalities.
    - **SETTLED** — the binding world was built to fail, failed as predicted, and has since
      lifted. Fork **25**.
    - **OPEN** — spreading a reading over its range costs most of the score at both front
      ends: fragmentation, or the search the extra codes buy. Fork **38**.
    - **OPEN** — a reading under about ten dimensions has too few distinct wirings for a
      projection to expand into, and population coding's floor is undocumented. Fork **39**.
    - **OPEN** — the tiled patch is the arranged world's cell, so it is told where the parts
      are. Does the advantage survive a grid that does not divide the world's. `csharp`
      refuted BANDED POSITION codes for both — row width bought with noise. Fork **44**,
      with **38**.
    - **OPEN** — THE INTERFACE COSTS MOST OF THE SCORE, and the front end's resolution is a
      hard floor: a fixed projection can split what is separable at some resolution and can
      never invent a direction. How the projection is AIMED beat both, rung five uninvolved.
    - **OPEN** — quantisation boundary noise is the interface risk and repair AMPLIFIES it:
      two identical worlds either side of a band emit unrelated codes, so specialising on the
      artifact mints it. Counting degrades gracefully here and repairing does not.
    - **NOW** — `Winnow` is the defence and it is mounted: overlapping winner sets mean a
      scope that is a SUBSET still fires, at the price that its sparsity unbounds rung two's
      candidate set. What graded codes cost is SEARCH.
    - **OPEN** — the multiplexer does not test the bet, its inputs being symbols already, so
      step one measures the learner and the front end not at all. The encoders are in
      `corpora/encoders` and `fetch.sh` gets them, frozen so the red-ball property survives.
  - Relations are concepts too
    - **NOW** — a commitment IS a relation and carries a code, so relations nest with no new
      machinery.
    - **SETTLED** — unification costs its candidate set rather than a subset test's price,
      and what blocks rung four is admission rather than cost. Fork **33**.
    - **SETTLED** — roles are carried by ORDER rather than unification; rung three reaches
      `Handing`'s ceiling. TRANSFER still needs the argument on both sides and `Expects` is
      a constant. Fork **105**.
    - **OPEN** — anti-unification as rung four's admission, gated by a hole whose covered
      values never co-occur. Open on the build. Fork **102**, gated by fork **97**.
    - **OPEN** — a second hop keyed on what the first reading supplied, banded by hop. Open
      at three facts. `csharp` refuted the cheap version: a second pass where a task merely
      LOOKS deep buys nothing, so the hop has to be real. Fork **96**.
    - **OPEN** — a commitment ABOUT commitments is expressible and not built, an identity
      being a code. Metacognition, and where a self-model starts.
  - Concept and label independent
    - **NOW** — rung five, and it goes UP: where several commitments share a sub-scope, mint
      a code for the shared part and rewrite them in terms of it. Gated by two bars, and its
      trigger is REDUNDANCY rather than failure, so it is the one rung a failure cannot summon.
    - **NOW** — and that code is available inside any future scope, including one that
      abstracts again — the recursion DreamCoder gets `sort` out of. Load-bearing for
      hierarchy, transfer, learned features and anything resembling one-shot learning.
    - **NOW** — concept-before-label is measured, and alternation groups things with no word
      for them yet.
    - **OPEN** — what rung five names is a SET, never a variable, so the two rungs are not
      independent: a code carrying position AND value together makes the shared thing
      unnameable.
    - **OPEN** — label-first is unbuilt: being told a word for a thing nothing else is known
      about.
    - **DEAD** — graded codes to make a POSITION nameable; the code reached the moment and
      no scope. Revives if naming ever looks inside a scope. Fork **36**.
    - **OPEN** — whether rung five buys anything a better-aimed projection does not, patch
      tokens having raised the floor while abstracting nothing. Fork **42**.
    - **OPEN** — two clean rules disagreeing about one code name the redundant one neither
      can see. Open on why it is damage where truths are one code. Fork **80**.
    - **OPEN** — should the separation bar be charged by what a scope's codes STAND FOR
      rather than by how many there are. Fork **71**.
    - **OPEN** — a category is the set of codes that are ALTERNATIVES, derived from moments
      alone. Open on the individual, which substitutability never reaches. `csharp` refuted
      a SIMILARITY code as the coarse form — a hub at one end, an index at the other, nothing
      between — so **83**, **84** and **85** want a told alphabet. Fork **97**.
    - **OPEN** — a name over ALTERNATIVES is derivable and not admitted, rung five being the
      wrong SHAPE: it names what CO-FIRES, and alternatives never do.
  - Understanding deepens without limit
    - **NOW** — repair. Specialisation on failure, gated, adding a narrower rule and never
      editing the old. Rung one of the ladder, conjunction, and the only rung there is.
    - **NOW** — the gate is the whole difference from overfitting: twenty misses before a
      repair is allowed, and Z must clear a two-proportion SEPARATION bar between its rate in
      the misses and in the hits, corrected for candidates considered. Uncorrected, noise
      clears any bar.
    - **NOW** — and Z is what the HITS had; backwards it mints a child reliably wrong, and a
      code commoner in the MISSES is the condition for a NEGATED one. A random-Z arm runs
      beside it: if discriminative-Z does not beat that, the bet is dead.
    - **NOW** — the ladder's admission is decidable and already computed: the language
      extends only when nothing in it separates failures from hits. A rung is admitted for
      ONE commitment, and where two clear, the SHORTER description chooses.
    - **OPEN** — it only ever NARROWS, rungs one to four making a scope smaller and nothing
      broadening but rung five. A specialise-only machine is arbitrarily accurate and
      conceptless, and the ladder's ORDER is a bias over when a construct is tried.
    - **OPEN** — a repair budget per parent, so one commitment cannot fork forever. A TOTAL
      is a lifetime, which C4 refuses, and what it should count is open.
    - **OPEN** — a fresh child starts BLIND and re-earns its statistics, a floor every rung
      of a chain pays while only the last pays off. Nothing is learnt from one example, and
      the escape is rung five rather than a smaller N — that is the 715-names failure.
    - **OPEN** — rung two, negation, *X and NOT Z*. Unsound against a live moment, so it may
      only be read against a SETTLED occasion and fires one settlement behind; its candidates
      must be bounded to codes seen in this commitment's own hits. Fork **30**.
    - **OPEN** — emit *Z was absent* as its own code at settlement, so rung two needs no new
      matcher. Bounded to the commitment's own hits. Fork **64**.
    - **OPEN** — `Mending.Uncovered` is a gate plus every-round repair, and the gate alone is
      far worse than no gate at all. Fork **37**.
    - **OPEN** — is there a per-COMMITMENT signal separating *needs specialising* from *is
      being outvoted*. Fork **45**.
    - **OPEN** — a blinded repair gate costs on the multiplexer. Open on any other world.
      Fork **55**.
    - **OPEN** — the gate's sign flips with the timing, and what is ruinous is the gate AFTER
      a failure. Open on any other world. Fork **58**.
    - **OPEN** — genesis mints ONE scope over a scene and repeated scenes narrow it by
      overlap: specific-to-general, the DUAL of repair. Fork **63**.
    - **SETTLED** — `Budget` is a re-derivation limit rather than a search limit, its
      apparent interior optimum having been the ballot. Fork **66**.
    - **OPEN** — does a conjunction EARN its narrowing? Reach halves with depth on even
      worlds and RISES under skew. Fork **68**.
    - **OPEN** — repairs sit at the world's minimum sound scope. Open on walking a chain in
      fewer steps without overshooting it. Fork **73**.
    - **DEAD** — one repair adding two codes to spare a miss floor; coverage fell while the
      carriers overshot. The revival row is in DO NOT RE-TRY. Fork **74**.
    - **OPEN** — nothing stops a chain at a sound depth. What signal INSIDE the machine says
      stop here. Fork **75**.
    - **OPEN** — a budget buys re-derivations. Does quantity buy the uncovered rounds, or
      only more population. Fork **76**.
  - Which aspects are temporal
    - **NOW** — a forward store beside the population, retracting where the counters cannot.
    - **NOW** — rung three, sequence. A precedence is a CODE derived where the moment is
      FORMED, so matching, the tally, repair and the wire are untouched. No dial, and inert where none is reported.
    - **OPEN** — a referent is a THIRD store rather than the same one: *Mary's bedroom*
      survives leaving the room and *Mary is in the bedroom* does not, so one store gets one
      lifetime wrong whichever it takes.
    - **OPEN** — nothing SCORES the update, so what retracts is the experimenter's rule. Fork
      **104** carries it, under *what it is told must be settleable*.
    - **OPEN** — banding a word by how many statements back it was buys about half of what
      narrowing the view buys. Does a band the learner MINTS differ. Fork **92**.
    - **OPEN** — does overwriting dissolve the selection rather than help it. Open on
      `Distinguished`. Fork **94**.
    - **OPEN** — recency over a forward store separates a verb from a name knowing nothing
      about the text, so the question is which key is worth FOLLOWING. Fork **95**.
  - Several grains at once
    - **NOW** — subsumption keeps the general rule where both are equally accurate, and it
      reads a category's entailment.
    - **OPEN** — the gradient collapsed once already: the vote takes the narrowest every
      round and subsumption the general one every thousandth. A second store needs an
      evidence rule the first lacked, or it collapses the same way.
    - **OPEN** — specificity as a gradient across the SITUATION stores too. Rules have that
      gradient; situations have nowhere to keep one — repetition for a general rule,
      assertion for a particular.
    - **OPEN** — whether compression is self-regulating. On no signal found yet. Fork **23**.
    - **OPEN** — project each scope code to its COARSER form when counting pairs. Test the
      rewrite first: a name no scope can be said in is a word with no referent. Fork **83**.
    - **OPEN** — `IQuantizer` must answer *what is the coarser form of this one*, which is
      the first thing a world tells the brain about its alphabet. Fork **84**.
    - **OPEN** — a coarse name entering a scope as a new claim is redundant where the moment
      carries the category. Open where no moment does. Fork **85**.
    - **OPEN** — three stores rather than two, and the missing operator mints an INDIVIDUAL,
      which no rung covers. Fork **93**.
  - Malleability is the record
    - **NOW** — an accuracy-weighted vote, plus a recency-weighted local estimate that never
      merges. This one works.
    - **SETTLED** — the local decaying estimate earns its keep: level with a lifetime average
      where the world holds still, ahead where the target moves. Fork **27**.
    - **OPEN** — more unsound commitments resident than sound ones while the score holds: is
      the vote robust to them, or are they why it stops short. Fork **35**.
    - **OPEN** — THE LIVE PROBLEM IS WHICH RULE GETS THE SEAT, and two arms at it have
      failed. Almost none of the population is read, so a gate changing what is HELD cannot
      reach what decides — read the revival rows before a third.
    - **OPEN** — `Alternating` sits on `DeadCodeTests`'s unwired list, the derivation being
      run offline. Wiring it needs one question answered: when does a front end RE-DERIVE.
    - **OPEN** — mutual exclusion is unbuilt, so a belief can be wrong and never
      CONTRADICTED. A miss says *I expected Y and got Z*; nothing says Y and Z cannot both
      hold, which is the whole of what a conflict is. Fork **99**.
  - Learns by being wrong
    - **NOW** — commitment, settlement, blame capped at one hop, repair, and abstention so a
      round that could not settle costs nothing. Reading is an objective at last: a sentence
      a story, and withheld sentences the exam.
    - **OPEN** — what it converts is unread, and English's alphabet is far wider than
      anything here has run on. Fork **89**.
    - **OPEN** — the horizon is K occasions, K=1. Fork **28**.
    - **OPEN** — entailment depth capped at one and the horizon at one; both come off when
      blame diffusion has a number. Fork **32**.
    - **OPEN** — the ladder's admission asks whether repair found a separating code, and on a
      wide alphabet memorising always does. What separates *nothing separates* from *nothing
      GENERAL separates*. Fork **86**.
    - **SETTLED** — the front end intersecting the question with EACH statement answers task
      one where the bag sits near the marginal, and one hop is all it reaches. Fork **88**.
    - **OPEN** — whether reading real English is predictive enough to teach this learner.
      bAbI is disqualified as a primer, its held-out half being all re-reading. Fork **100**.
    - **OPEN** — two English objectives read one corpus at wildly different rates, so no
      single capacity sizes both. Fork **101**.
    - **BLOCKED** — the exam tier above bAbI is unpriced, and blocked until the components
      pass. Fork **90**.
    - **OPEN** — it is a near-perfect READER and a hopeless SELECTOR, which the two ends say
      together: shown the right statement it is at the ceiling, shown the whole story it
      takes a fraction of what is present to be answered.
  - Told, never architected
    - **NOW** — a front end may say what it is looking at, never what to conclude.
      `SeparationTests` fails the build.
    - **OPEN** — how hard a fleet searches is a deployment choice, which is a world reaching
      into the brain one level out. Fork **60** carries it, under the machine.
  - What it is told must be settleable
    - **NOW** — nothing. Told and configured are indistinguishable from the inside, so a fact
      it cannot fail on was installed rather than taught.
    - **OPEN** — the store's update rule is the experimenter's, so nothing can be wrong about
      it. What must settle is *this statement changes what is known about that*. Fork **104**.
    - **OPEN** — a primer moves no counter, a round the world cannot settle taking no score,
      no genesis and no repair. A world that asks is one way to fix that and an action with
      a consequence is another; what is missing is any of them.
    - **OPEN** — on a read corpus the OBJECTIVE is the wall and no gate reaches it: the
      informative words are the unpredictable ones and the predictable ones are `to` and
      `the`.
    - **NOW** — OSTENSION is the signal rather than a shortcut around unsupervised reading.
      Being told which word the question is about is information no amount of co-occurrence
      contains, and it is the pointing-and-naming shape.
  - Original thought
    - **NOW** — nothing. Every world here is watched rather than acted in, and action, a
      consequence that can surprise, and a goal are all unbuilt.
    - **OPEN** — TextWorld's shape is built here and watched before acted in; Crafter puts
      two unbuilt subsystems in front of the measurement. Open on twins. `csharp` disqualified
      SURVIVAL as a score and refuted absolute actions under an unrotated view, so an acting
      world owes both. Fork **103**.
- **WHAT THE MACHINE MUST SURVIVE** — C1 to C4 are under THE DESTINATION and do not move;
  these are the questions they leave open.
  - The constraints hold under lateness and loss
    - **NOW** — merge monotone, decide local. Hits, misses and abstains are the only thing
      another node is ever told.
  - Placement, so two machines reach one rule and not two
    - **NOW** — placement by the minimum code of a sorted scope, capped by a world's distinct
      roots, so fleet size and the front end's vocabulary are one number.
    - **OPEN** — uniform hash against prefix locality: a uniform ring separates a child from
      its parent, and prefix placement recovers much of it at unmeasured cost. Fork **3**.
    - **OPEN** — two parents on two machines reach the IDENTICAL child, so one rule is held
      twice and a sum counts its evidence per machine. Fork **29**.
    - **OPEN** — genesis is placed and repair is not, so a fleet mints one child per HOLDER a
      round. Place repair, divide the budget, or exploit it. Fork **60**.
    - **OPEN** — placement by the minimum code keeps a lineage together and costs balance
      past a dozen machines. Fork **61**.
  - Death is normal rather than an error
    - **NOW** — the signal is a REFUSED CONNECTION and never a death notice, so an impolite
      departure and a dropped message arrive by the same road. A machine never handed a
      question cannot answer it, which is exact rather than a guess.
    - **OPEN** — an ask watched failing to leave is written off exactly, so a fleet loses one
      and learns on. Open on the round a holder dies INSIDE. Fork **53**.
    - **OPEN** — slots of R identical holders let a round finish on either one. Open on what
      R buys and what it costs. Fork **62**.
  - A fleet learns what one machine learns
    - **NOW** — `Posted` and `Cycle`: asks, answers and settlements over real sockets, with
      the learning loop still existing exactly once. THE WALK IS GONE and forks 1, 5, 6, 11,
      18, 20, 21, 22 and 24 went with it; `csharp` keeps their code and their numbers.
    - **OPEN** — `Drives` is the one idea still owed off it: a third factor from the body's
      own variables.
    - **BROKEN** — eleven worlds have no runner, their `*Run` files being the walk's. Each
      wants a `Trial` OR A DELETION, a world going when its question closes, and nothing
      says which. `DeadCodeTests` counts the debt.
    - **NOW** — the vote's arithmetic composes and a whole learner runs over it. Fork **52**.
    - **NOW** — A ROUND IS A BARRIER, so lateness costs the CLOCK and changes not one answer.
      C2's out-of-order half is untestable here for the same reason, and the fleet is paced
      by its unluckiest holder twice a round. Breaking it is unmeasured.
    - **OPEN** — what the repair gate's query costs on a wire, priced on loopback. Open on a
      LAN and on building it. Fork **56**.
    - **OPEN** — the vote decides what repair may run on, so under skew blame lands on the
      majority lineages alone. Open on what breaking that costs at width. Fork **65**.
    - **OPEN** — rung five's evidence is the population, so splitting the population splits
      the agreement. Whether shipping name frequencies recovers it exactly. Fork **81**.
    - **OPEN** — every node predicting its own output while the real wave verifies behind it.
      What it saves is a hop, so where it pays is deployment. `csharp`'s adaptive version
      wrote most where it helped least, so WHAT to spend on is the open half. Fork **57**.
    - **NOW** — replicas DRIFT, because the completeness condition ends a round on one of
      them and the other may take the next moment before the last settlement. Order rather
      than content, so a failover replica is a similar population and not the same one.
    - **NOW** — only IDENTICAL evidence converges on a name. Machines sharing most of a
      stream agree as poorly as machines sharing none, so merging the counts is the only
      thing that works rather than an optimisation.
- **WHAT THE INSTRUMENTS MUST SAY** — an instrument that cannot fail says nothing, and every
  ground-truth one here needs a world that can be enumerated.
  - A run reproduces exactly
    - **SETTLED** — a fixed seed reproduces a run exactly, across sockets too; `Receive`
      folds arrivals in delivery order. Fork **12**.
  - The table fits and the clock allows
    - **OPEN** — the TABLE is what blows up, not the commitments, needing an entry per code
      seen while firing. It spills to SQLite on the owning node, rehydrated if it becomes a
      candidate again — and a spill that changes what fires is an undeclared dial. Fork **31**.
    - **OPEN** — matching and settling are nine tenths of the clock on a narrow world whose
      table never grows. Where they go on a WIDE one, and what ends the run. Fork **49**.
    - **OPEN** — a child fires only where its parent does and matching IGNORES that, going
      through the code index instead. Rete's own problem, with the wrinkle that culling
      orphans a child and an orphan that stops firing reads as nothing.
    - **BROKEN** — four `EncodedTests` fail on a file never built: the graph is cut one `Gemm`
      early to drop a 1000-way classifier, which needs `onnx`, which the runner has not got.
    - **BROKEN** — `BudgetTests` crosses two settings and pins neither timing nor budget, so
      it changed arms silently; being a sweep, CI never looked.
  - Withholding is real and the gap is readable
    - **SETTLED** — a generated world holds assignments back without the learner being able
      to tell, the draw rejecting rather than picking. Fork **48**.
    - **OPEN** — the held-out gap as a function of RECURRENCE, which is the number saying how
      big a bag a world needs. Fork **41**.
  - A score says how often and never which
    - **NOW** — a transcript is an instrument and the cheapest one here. A population
      answering everything with the commonest word and one that has learnt the task read
      identically until the words are printed.
    - **NOW** — a held-out question can be word for word one already asked, the corpus being
      templated over a small cast. So an unseen score is read beside a count of its twins
      rather than on its own.
  - What a rule learner is worth beside a probe
    - **OPEN** — given symbols worth having, how close a conjunctive rule learner comes to a
      linear probe on the same vectors. The grid is a sweep. Fork **43**.

## DO NOT RE-TRY

| what | what refuted it | what would revive it |
|---|---|---|
| Proposing a category's claim as a new commitment | It costs population where reading the entailment costs none: a fresh record must fire before anything may judge it, and genesis already mints the coarse claim from the moment | A vocabulary the brain holds that no moment carries, so nothing else mints it |
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
| `Widening.Significant` — widen only where a clean record beats the base rate | Bit-identical on four cells: `Floor` demands twenty firings, and a perfect twenty clears every base rate under `n/(n+2.71)` | A world past that boundary. `WideningTests` fails the day one arrives |
| `Joining.Situated` — displacement keyed on the commonest N words of the corpus | The motion verbs straddle the names, so no rank keeps names as keys and drops verbs | A corpus whose function words are separable by frequency. `Distinguished` takes its background from the story and needs no rank |
| Displacement as a way AROUND unification | At every width it is at its ceiling only where it keeps one statement, and a better key rule bought a better ceiling and no more score | Never as a substitute. A store must be read BY KEY, and reading by key is rung four |
| A forward store whose fold is transitive | It reaches every answer with nearly every room word still there, which is the bag by a longer road | Never uncapped. The depth's optimum is interior, so the reading that set the cap refuses a fold without one |
| A precedence's TRANSITIVE closure rather than the adjacent pairs | Identical ceiling on `Handing` for two and a half the population: a quadratic expansion to say what adjacency entails | A world whose relation spans an intervening position, where adjacency falls short |
| `Chunk`'s whole-moment rule ported to rung five | Two vocabularies: `Narrows` is syntactic, so a scope keeping the members and its children taking the name stand in no relation. Unsound rose on every seed that moved | A subsumption test read at the UNFOLDED grain |
| A front end putting a word's POSITION in the moment | Beside the code it is never absent, reaching every moment and no scope; FUSED in, it costs the identity — an input is an attribute, never the concept | Never. Order derives its own code: rung three |

---

## TRAPS

Grouped by FAILURE CLASS rather than by incident, because ninety separate lessons is a list
nobody finishes. Each names the sharpest instance; the rest are in the commits that found
them. **A class earning a check moves out of here into the check.**

### The harness lies, and nothing goes red when it does

- **A HAND-TYPED FILTER RUNS THE GRIDS CI EXCLUDES.** Naming a class names its sweeps too, so
  two suites ran past forty minutes where `kind!=sweep&` in front takes them to seconds. The
  facts were tagged correctly and the COMMAND was not.
- **PUSHING FASTER THAN THE SUITE RUNS MEANS NOTHING IS EVER TESTED.** The concurrency group
  cancels whatever is waiting, so a session committing every few minutes cancels its own queue
  all day. Only a `[checkpoint]` escapes.
- **A BUILD DURING A TEST RUN CAN ABORT IT WITH EVERY TEST PASSING**, the assemblies being
  replaced underneath. The mirror of the `--no-build` staleness rule.
- **A COST MEASURED ON ONE PLATFORM CAN BE NOUGHT ON ANOTHER.** A refused loopback connect is
  four seconds on Windows and immediate on Linux, so a shard went red for a repair working
  perfectly — and that same four seconds prices the local suite and not CI. Read a wire
  timing on CI.
- **A WORKFLOW IS THE ONE ARTIFACT WITH NO LOCAL CHECK**, and it is wrong until a push says
  otherwise. And SKIPPING WORK IS NOT SKIPPING A JOB: a matrix entry that exits immediately
  still took a runner slot.
- **A TIMED-OUT JOB REPORTS AS CANCELLED AND NOT AS FAILED**, and one such job makes the
  whole run read cancelled. On a branch where cancellation is the NORMAL outcome, an overrun
  is perfectly disguised as the concurrency group working — which is how a `[checkpoint]`
  can appear to have been cancelled by a later push it is immune to.

### A check that cannot fire reads exactly like a check that passes

- **ARM ANYTHING THAT HAS ALWAYS READ ZERO.** `Surprise` and `Abstain` were both found wired
  and unable to fire, and *promiscuous on purpose* meant EXHAUSTIVE for the life of the repo
  because its gate was mounted nowhere.
- **A GUARD MOUNTED ON ONE CALLER IS NOT MOUNTED**, and a CODE PATH GUARDED BY A CAP IS
  UNTESTED UNTIL SOMETHING REACHES THE CAP. Both sat unexercised for the life of the repo
  because no world was wide enough.
- **A DOCUMENTED PROMISE IS NOT A CHECK.** `Posted` said a fan-out was posts in flight while
  both of its fan-outs awaited each post in turn — false from the day it was written, directly
  under the sentence describing the fault. A fix aimed at the callers one measurement touched
  leaves the rest.
- **A BUDGET CAN BE SATISFIED BY A COINCIDENCE**, and a CAST TO AN INTERFACE THE TYPE DOES NOT
  IMPLEMENT IS CLEANUP THAT NEVER RUNS. Both compile and read as tidy.
- **A PREDICTION WRITTEN INTO A WIRING CHECK FAILS TWO WAYS AND READS THE SAME.** Assert that
  arms DIFFER, never which way.

### A comparison that moves two things at once

- **MEASURE ONE MECHANISM ON FROM A KNOWN BASELINE, NEVER ONE OFF FROM ALL-ON.**
- **A SETTING CAN DECIDE TWO INDEPENDENT THINGS WHILE BEING NAMED FOR ONE**, so the cell that
  separates them may already exist and never have been read as a control.
- **A READOUT ARM IS A SEARCH ARM WHEREVER THE READOUT TRIGGERS THE SEARCH.** Every vote
  comparison in four sessions moved both.
- **A FIXTURE INHERITS EVERY DIAL IT DOES NOT PIN**, so a default moving rewrites an
  experiment nobody edited — and THE GRID THAT DECIDES A DEFAULT REWRITES ITSELF THE MOMENT
  IT WINS.
- **A DEFAULT CAN SHORT-CIRCUIT THE MECHANISM BEING MEASURED**, so a sweep on defaults
  returned three identical arms for a gate that was never running.
- **A TEST CAN FAIL AT BOTH ENDS OF A DIAL FOR OPPOSITE REASONS**, so pinning to the old value
  fixes nothing while reading as a fix. **Do not attribute a red test to your own change
  without a baseline.**

### A score reached the wrong way

- **AN ACCURACY CAN BE HIT BY MEMORISING.** Report the commitment count beside every score,
  and on a world with known ground truth report how much of it was found.
- **A CORPUS CAN CONTAIN ITS OWN ANSWER, and then a score measures the leak.** A generated
  world cannot, which is half of why the multiplexer is first.
- **A GRID CAN RANK ARMS ON COLUMNS A SKEWED WORLD RAISES FOR FREE**, and a GRID OF IDENTICAL
  ROWS IS A VERDICT ON THE WORLDS RATHER THAN ON THE ARM.
- **A PRECISION TAKEN AT THE ANSWER'S OWN SIZE IS THE EXPERIMENTER HOLDING THE KNIFE.**
  Nothing inside the machine knows a category has four members. Report the size-free cut.
- **A FRONT-END ARM HAS A CEILING COMPUTABLE WITH NO LEARNING, AND IT COSTS MILLISECONDS
  AGAINST A RUNNER'S HOUR.** Take it FIRST — a grid cannot tell a rule that dropped the wrong
  sentence from a learner that failed to use the right one.
- **TWO ARMS THAT SCORE ALIKE NEED NOT BE THE SAME MECHANISM, AND A SCORE CANNOT SAY.** A cap
  that refuses nothing and a cap that refuses a lot read identically until something counts
  what was BUILT.

### A statistic whose halves count different things

- **A RATE WHOSE NUMERATOR COUNTS RULES SAYS NOTHING ABOUT HOW MUCH GOT COVERED**, and A SHARE
  WHOSE HALVES COUNT DIFFERENT EVENTS ANNOUNCES ITSELF BY EXCEEDING ONE.
- **AN EXACT PARTITION OF WHAT REACHED A MECHANISM SAYS NOTHING ABOUT WHAT NEVER REACHED IT.**
  The lineage that mattered was absent from the denominator.
- **A LIST THAT APPENDS A DUPLICATE IS A COUNT WEARING A SET'S SHAPE**, and every reader gets
  whichever it assumed.
- **A PERIODIC SWEEP INSIDE A CONDITIONAL RUNS AT THAT CONDITION'S RATE**, so subsumption and
  culling read as mechanisms that bought nothing. Its dual: a periodic sweep against a
  per-round rate that scales with the front end holds a population far above its capacity.
- **AN EXPLANATION CAN BE ARITHMETICALLY TRUE AND STILL NOT BE WHAT MOVED THE NUMBER.**

### Too few seeds, or too much trust in the spread

- **ONE SEED IS NOT A COMPARISON AND WILL HAPPILY INVERT.** Error bars before ordering, every
  time, and count seeds in BOTH directions — a small sample hides a real effect as readily as
  it invents one.
- **A SEED SPREAD IS NOT ALWAYS A YARDSTICK**, so a kill line resting on one can be vacuous:
  identical scores in every cell admit any gain at all.
- **AN ESTIMATE IS NOISE BEFORE IT IS A STATISTIC, AND A CHAOTIC RUN KEEPS THE PERTURBATION**,
  so a mid-run reading and an end-of-run one are different measurements.
- **A WINNER-TAKE-ALL ARGMAX IS CHAOTIC IN ITS EVIDENCE**, and two ends of a sweep cannot
  show it.

### Reproducibility broken from outside the code

- **A DEPENDENCY'S DEFAULTS CAN BREAK IT SILENTLY.** Parallel inference reorders float
  reductions and a code is a QUANTISED number, so a reading at a band boundary codes
  differently run to run.
- **A TIE-BREAK BY DICTIONARY WALK IS STABLE UNTIL THERE ARE TWO TABLES** — reproducible in
  one process, arbitrary across a merge.
- **A MEASUREMENT INSIDE A REPORT IS ASSERTED ON BY EVERY EQUALITY READING IT.** A wall clock
  in a record turns reproducibility red and makes every `NotEqual` beside it pass for free.
- **A `readonly record struct` HOLDING AN `ImmutableArray` COMPARES BY THE ARRAY'S IDENTITY**,
  so two separately built keys with identical contents are never equal.
- **A TYPE CAN DROP MOST OF ITSELF ON THE WIRE AND STILL WRITE A PLAUSIBLE NUMBER.** Private
  tables and tuple keys serialise to nothing. Pin a format failure with a check on the
  ANSWER, never a comment.
- **A LOCAL BUILDER'S INVARIANT IS NOT THE RECEIVED FORM'S.** No pair built here holds one
  code twice; the wire form takes whatever arrived.

### Reading the machine wrong

- **A DOC CAN NAME THE WRONG BLOCKER AND BE BELIEVED FOR A WHOLE BRANCH.** Read the code
  before costing the fix.
- **READ THE REVIVAL ROWS BEFORE PROPOSING A MISSING ARM** — a row may name the same axis in
  the mechanism's words rather than the comparison's, which is how a search misses it.
- **THE TELL IS OFTEN A DISTRIBUTION, NOT A SCORE.** A hard ceiling immediately below a
  threshold is never a coincidence; read the spread, not the mean.
- **AN ONLINE SCORE BELOW WHAT THE FINAL POPULATION GETS ON FRESH OBSERVATIONS IS A CHURN
  SIGNAL** — the population is being rebuilt faster than the trailing window can read it.
- **A MECHANISM IS LOCAL OR POPULATION-WIDE BY ACCIDENT UNTIL SOMETHING SPLITS IT**, and
  nothing in one process can tell the two apart.
- **A COST CAN BE IN MEMORY WHILE EVERY INSTRUMENT WATCHES TIME**, and A CLAIM ABOUT
  CORRECTNESS WILL DO DUTY AS A CLAIM ABOUT THROUGHPUT UNLESS SOMEBODY MEASURES.
- **A SIMULATED CONSTRAINT CAN BE HARSHER THAN THE REAL ONE.** `HybridBus` reorders on purpose
  and TCP does not, so a green distributed run says nothing about C2.
- **AN ANSWER KEY IN THE WRONG ALPHABET SCORES NOUGHT AND LOOKS LIKE A VERDICT.**
- **A FALLBACK IS A CONTROL ARM NOBODY MEANT TO RUN** — silence drifts an arm toward the
  random bar for free. Report silence beside the score.
- **DELETING THE LAST ARM DELETES THE CHECK THAT MADE THE DELETION LEGITIMATE**, so a property
  asserted across three arms becomes an argument again with one left.
- **THE INSTRUMENT THAT KILLS A STORY IS USUALLY BUILT FOR SOMETHING ELSE**, so ask which grid
  already holds the number before running a new one. And ask which half of generate-and-test a
  proposal touches: where no right rule was present, a rule about who WINS cannot reach it.

---

