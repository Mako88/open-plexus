# Where this is going

A different bet from `csharp`, on purpose. That branch counts co-occurrences and
walks them. This one counts CONDITIONED ON A PREDICTION, so what is counted is
attached to something that can be wrong. `csharp` is not abandoned and nothing here
refutes it — read its refutation table before repeating anything.

- **The only doc, and it holds nothing finished.** What a built mechanism does lives
  in its XML comments, where the compiler enforces every reference.
- **Findings live in the commit** that produced them, and in the test that asserts
  them. Never here.
- **One line an item.** A cap per ITEM, and a cap on the whole.
- **John's test, and it is the one that decides**: if it is long enough that you would
  hesitate to load all of it, it is too long. This doc exists to be read whole at the
  start of every session. A doc read in pages is the pile of docs it replaced.
- **So the budget falls by default** and `DocsTests` fails the build the moment the doc
  grows. What will not fit belongs in a commit, a test, or an XML comment.
- **And it may be raised for something genuinely new** — John's, and the conditions are his:
  the existing items are reasonable, the new one duplicates nothing, and this is still a
  doc you would load whole. A cap that only ever falls decides what may be thought about.
- **Built and decided means GONE FROM HERE, and no arm either.** A winner becomes the
  code; losers are deleted, leaving a revival row.

---

## THE DESTINATION

What must be true when this is finished, and it does not change. **The route is where
everything moves; nothing here does.**

### The bet

- **Understand rather than perform** — answer *what would the world look like if I did X*,
  which a sequence model cannot be.
- **A count is never wrong; a commitment is.** A cell that mispredicts becomes a
  different number. A commitment that mispredicts is wrong about SOMETHING, and which
  something is the whole of what can be learnt.
- **The counting does not go away, it moves under the prediction.** Repair asks which code
  separates misses from hits, which is `together / seen` indexed by commitment and not by
  node.
- **The representation is the residue of repaired failures**, not a thing designed up
  front. Distinctions get minted to tell two conflated cases apart.

### THE ARCHITECTURE

John's, and the one section written to STAY. It says what the brain must DO and never how —
a mechanism written here is a decision wearing a requirement's clothes. No finding enters.

- **A BRAIN THAT UNDERSTANDS CONCEPTS.** It holds a model of the world and answers from it,
  never by matching the surface of a question to the surface of a text.
- **A concept is a thing in its own right** — distinct from every other and interrelated
  with every other. That pairing IS the understanding: what a thing is, and how it stands to
  everything else.
- **And every input is an attribute of a concept, never the concept.** The look of a thing,
  the sound of it, its temperature, its name: each is one way it shows through one sense. The
  thing is what they are all attributes of.
- **And relations are concepts too** — association, containment, ownership, movement. If the
  meta level is not representable then the model is a list rather than an understanding.
- **And a concept and its label are independent.** Either may arrive first: a slot with no
  word for it yet, or a word for a thing nothing else is known about. Both must be reachable.
  A label is welcome; what is required beside it is everything the thing stands to. John's.
- **And what is understood of a thing deepens and broadens without limit**, thousands of
  truths about one individual, each sharpening what it is.
- **And part of what is understood is which aspects are temporal** — which properties come
  and go, what those aspects are in themselves, and how they stand to everything else.
- **And knowledge is held at several grains at once** — *a person sleeps in a bedroom* and
  *this bedroom is Mary's*, both live, neither replacing the other.
- **And how hard a belief is to shift is its own record**, never a weight. Gravity is
  immovable because that belief has vast evidence and has never missed; Mary's room moves
  because that one misses constantly.
- **And it learns by being wrong and finding out.** It predicts, is scored, and refines.
  What supplies the scoring is left open — a question asked, an action taken, a consequence
  observed.
- **And it may be told and must never be architected.** A primer may teach it that a room is
  a space people enter. What is forbidden is an ontology built in by hand: the representation
  is what learning left behind.
- **And what it is told must be falsifiable.** Told and configured are
  indistinguishable from the inside, so a fact it cannot fail on was not taught to it — it
  was installed in it.
- **And from that understanding it produces original thought** — conclusions it was never
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
- **Keeping the narrower one drifts a population to one rule per instance**,
  which is the memorising this design is otherwise careful about. XCS is this way
  round for the same reason.
- **Codes are identical on every machine forever.** A commitment's identity derives
  from its SCOPE, so every repair path that reaches a scope converges on one name.
  Parent-plus-condition gave one scope two.
- **A front end may say what it is looking at**, never what to conclude — *this is
  the same thing you saw six times*, never *this is a red ball*.
- **One brain, and a world may never reach into it.** Brain dials are built once and
  handed in; a world turns only its own. `csharp` had `Ranking` set one way on bAbI
  and another on CLEVR, so a WORLD decided how the brain thought.
- **Every score was then a comparison between two brains**, as much as two problems.
  `SeparationTests` fails the build if anything in a world names a brain type, because
  the rule was broken within an hour of being agreed.
- **The translation is a third thing and belongs at the join.** Whether a reading is
  banded or winnowed is neither a fact about the problem nor a setting on the brain.
- **Adaptation lives above the codes and never inside them.** The feature basis is a
  constant of the design; a learned feature is a minted name over co-firing codes.
- **So the demand for resolution may change the front end's grain**, and never
  what it cuts along.

### The first north star

John's, and it is a stepping stone rather than the goal above. Written down because it
FORBIDS things, and several of them are already measured.

- **Twenty used phones on one wifi running the brain**, one stronger machine carrying the
  body — camera at a frame or two a second, audio, temperature, motion.
- **And the phones are last rather than first, which is John's ordering.** They will not be
  bought until this is worth showing somebody, so the hardware is a conclusion of the prototype
  rather than a precondition for it.
- **First is one process on one box**, the only sense English, and talking to it is the first
  interactive test of the system — no camera or wifi.
- **And the CONVERSATION is that world**, John's: a block told, a window where
  the machine may ask, then a fixed examination. It stays until it is exhausted.
- **`Roaming` is kept for the modalities rather than retired**, being the world where a sound,
  a look and a sentence can be one moment. A parked world rots, so that is its reason.
- **Then the phone as the body and the containers as the brain**, video and sensors in, if one
  machine can hold enough containers to be a fleet. That is what says whether twenty of anything
  is needed before twenty are bought.
- **And a text conversation as a second body**, so one brain is judged on symbols and on a
  stream without being two brains.
- **The constraints were all written for this**, so it is a demonstration rather than a
  benchmark: no shared memory, late messages, a cluster vanishing mid-thought.
- **So the fleet was never the risk**, and what text teaches this learner is answered where
  it should have been asked.
- **And twenty is already the measured edge.** Placement by minimum code is capped by a world's
  distinct roots, so fleet size and the front end's vocabulary are one number.
- **So what the footprint reading is about changes with the rung.** Containers share one
  machine's memory, so the fleet's whole brain is what matters there and on phones it is one
  holder's share. Both come off the same reading.
- **And what prices a camera is the front end's vocabulary**, at residents times codes times a
  hundred bytes rather than anything about the population. Take it before a sensor is plumbed.
- **And one input machine feeding twenty holders is the identical case.** Every holder is told
  the same moment and settlement, so naming converges — what breaks it is many eyes rather than
  many brains, and a camera per phone is the arrangement to avoid.
- **A sensor is a world and a world is a stream**, so nothing here is a new kind of input —
  what is missing is the plumbing, not a mechanism.
- **And every ground-truth instrument goes dark.** Soundness, overshoot and hard-round coverage
  all need an enumerable world, so the generated worlds never leave.
- **So the probe is the one instrument that crosses.** Ask what the codes carry against what the
  raw reading carries, per sensor, BEFORE building on that sensor.
- **And a curriculum is allowed, and C4 is not about it.** *No episode boundary* constrains the
  LEARNER; what an experimenter feeds, and in what order, is outside the machine.
- **So a primer before a test is expected rather than a cheat** — the language before the
  play, the room before the question. What is forbidden is the learner being able to tell
  that a boundary happened.
- **John's curriculum proposal**: teach it English, then examine it — and mixing the two is
  worse than either alone, the function-word rules crowding out the population the questions
  needed.
- **And the first conversation demands unification rather than sequence**, which inverts what
  the ordering assumed. Binding the question's actor to a statement's actor is rung four, and
  no dose of recency reaches it.
- **And the exam is already chosen.** Twenty tasks each isolating one prerequisite, written
  elsewhere with published baselines, built so surface matching fails. A school comprehension
  test would read nothing until the components pass.

### What the field already knows

- **Borrow the problem, not the mechanism.** This is not a new idea and pretending otherwise
  wastes months.
- **DreamCoder** (Ellis et al., 2021) — grows its own library under MDL pressure and
  BOOTSTRAPS: learns `filter`, uses it to learn `max`, then `sort`. The existence proof for
  representation as residue.
- **Popper / Learning From Failures** (Cropper & Morel, 2021) — generate, test, **constrain**.
  This design's core loop, already formalised, and GENERATE is the half this plan kept
  forgetting.
- **XCS** (Wilson) — accuracy-based fitness, because strength-based systems delete low-reward
  rules still correct in their niche. Its covering, prediction array and subsumption are taken
  here; its recency-weighted accuracy is the one thing deliberately not.
- **The Monk's problems** (UCI) — the classic symbolic benchmark, external baselines, small.
  Monk-3 carries deliberate noise, testing the repair gate and nothing else; **Monk-2 is a
  counting concept a conjunctive scope CANNOT express**, a language-ceiling probe with a
  published number.
- **Why none of it scaled**: noise sensitivity, hand-specified language bias, and no way to
  learn from probabilistic or sensory background knowledge. **And the failure was at the
  interface with perception** rather than in the logic — the one place this project is unusually
  well placed, because its substrate manufactures symbols. That is the bet, said plainly.

---

## THE ROUTE

Everything that is built, unbuilt, refuted or broken, against the requirement it serves. A
branch is what must hold, an entry is one requirement, and a leaf is one line opening with
exactly one of **NOW**, **OPEN**, **DEAD**, **BLOCKED**, **BROKEN** or **SETTLED**. A fork
gets one home and a cross-reference by number from anywhere else it serves; numbers are never
renumbered. Forks 1 through 25 are `csharp`'s, and when that code is stripped, point
`DocsTests` at `csharp` for them rather than weakening it.

The order of the work is John's. A number is an IDENTITY other files cite, never renumbered,
so the list's order is where each one sits. Nothing here is taken out of it, and anything
before six that reads as tuning is out of order.

- **One, the seam** — a world becomes a set of inputs pushing moments, the brain answers with
  what it did, and `Trial` goes. It carries its own repair: a phase leaving the suite
  unreadable makes the next one blind, so one ends when the reds are the three that are named.
- **Two, a mechanism for every entry of THE ARCHITECTURE**, however bad, and a spine world
  exercising all of them. `DocsTests` holds the first half and `OutstandingTests` the
  second; adhesion and nesting fail it.
- **Seven, the conversation harness**, and it is built. `Conversing` is one moment a typed
  line and the machine asks for the settlements it wants; what is left of the path is the
  situation store's evidence rule and rung four.
- **Three, the intentional reds cleared**, which is the stable state to refine from.
- **Four, audit TRAPS and DO NOT RE-TRY** — a failure class that has earned a check belongs
  in the check, and a revival condition that has expired is a superstition.
- **Five, split `Population`** and any other class holding several mechanisms at once.
- **Six, refining** toward the first north star.

    Commitment := scope (codes that must all be present)
                → expects (a code that should follow)
                + hits, misses, abstains

- **WHAT IT MUST DO** — one entry a line of THE ARCHITECTURE, in that order, and a guard
  holds the two in step.
  - Understand concepts
    - **NOW** — a commitment fires when its scope is a subset of the moment, and is then right
      or wrong about something SPECIFIC. That is the whole difference from a count.
    - **NOW** — its identity is a `Code`, the same type a front end emits, so one can sit
      inside another's scope — which makes metacognition, chaining and abstraction
      expressible with no new machinery.
    - **NOW** — everything else is in the XML comments and the compiler enforces them: genesis
      and its gate, the vote and its weighting, settlement, blame and repair all live beside the
      code in `Commitments`.
    - **OPEN** — nothing here answers what a concept IS beyond a code that fires.
    - **NOW** — the exponent is polynomial ON SYMBOLS: 2,003, 3,468 and 7,920 rounds at 6,
      11 and 20 bits, so cost tracks a scope's DEPTH.
    - **OPEN** — the POPULATION is where it grows: 19, 797 and 1,824 sound rules.
    - **OPEN** — the scope language is the CEILING: whatever a scope cannot say cannot be
      learnt. ILP's language-bias problem, what killed the field, and the ladder is finite.
    - **OPEN** — six bits is refused on POWER and eleven names the wrong thing; whether any world
      the naming reaches holds a nameable concept. Fork **34**.
    - **OPEN** — genesis does not root on a code that never varied, and an always-present one is
      still an entry in every table forever. Fork **51**.
  - A concept a thing in its own right
    - **NOW** — `Code`. A commitment's identity is one, and adhesion over a window reaches one
      group a persistent SOURCE. Derived offline, so no run reaches it.
    - **OPEN** — nothing tracks a source through a CHANGE, so a thing that moves is a new one
      and adhesion never reaches an individual. Fork **106**.
    - **OPEN** — minting an INDIVIDUAL is unbuilt. Where a thing never moves a source and a
      thing are one set; where it moves they come apart.
    - **OPEN** — co-firing binds what is SIMULTANEOUS and never what persists, so a thing at
      two moments does not co-occur with itself and no amount of it reaches the same thing
      seen twice.
  - Every input an attribute of it
    - **NOW** — several front ends manufacture symbols from a signal, and each is priced.
    - **OPEN** — nothing makes them attributes of one THING. Rung five names what co-fires, as a
      seen ball and a heard *ball* do.
    - **SETTLED** — the binding world failed and the block is lifted. Fork **25**.
    - **OPEN** — text as an IMAGE keeps ground truth enumerable, so soundness survives where a
      camera kills it. `Senses` names cross two senses. Fork **107**.
    - **OPEN** — spreading a reading over its range costs most of the score at both front ends.
      Fork **38**.
    - **OPEN** — under ten dimensions there are too few distinct wirings for a projection to
      expand into. Fork **39**.
    - **OPEN** — the tiled patch is the arranged world's cell, so it is told where the parts
      are. Does the advantage survive a grid that does not divide the world's. Fork **44**.
    - **OPEN** — the interface costs most of the score and the front end's resolution is a hard
      floor: a fixed projection splits what is separable at some resolution and never invents a
      direction. How the projection is AIMED beat both, rung five uninvolved.
    - **OPEN** — quantisation boundary noise is the interface risk and repair AMPLIFIES it:
      two identical worlds either side of a band emit unrelated codes, so specialising on the
      artifact mints it. Counting degrades gracefully here and repairing does not.
    - **NOW** — `Winnow` is the defence and it is mounted: overlapping winner sets mean a
      scope that is a SUBSET still fires, at the price that its sparsity unbounds rung two's
      candidate set. What graded codes cost is SEARCH.
    - **OPEN** — the multiplexer does not test the bet, its inputs being symbols already, so it
      measures the learner and the front end not at all.
  - Relations are concepts too
    - **NOW** — a commitment IS a relation and is scored as one. Nesting is expressible on
      that rather than reached, and the build that reached it is in DO NOT RE-TRY.
    - **SETTLED** — unification costs its candidate set rather than a subset test's price,
      and what blocks rung four is admission rather than cost. Fork **33**.
    - **SETTLED** — roles are carried by ORDER rather than unification; rung three reaches
      `Handing`'s ceiling. TRANSFER still needs the argument on both sides and `Expects` is
      a constant. Fork **105**.
    - **OPEN** — anti-unification as rung four's admission, gated by a hole whose covered
      values never co-occur. Open on the build. Fork **102**, gated by fork **97**.
    - **SETTLED** — a second hop pays where a second fact is needed and is damage where one
      suffices, so the depth is the task's rather than the mechanism's. Fork **96**.
    - **OPEN** — what puts a commitment in a moment without widening every world's. Fork
      **116**, and the revival row is the shape of the answer.
  - Concept and label independent
    - **NOW** — rung five, and it goes UP: mint a code for a shared sub-scope and rewrite in
      terms of it, gated by two bars. Its trigger is REDUNDANCY, so no failure summons it, and
      the code is reusable inside a scope that abstracts again. `Abstracting` says why.
    - **OPEN** — the recursion is scarce on both benches and blocked differently. A named
      `Motif` scope is left too short to carry a name, the cue's length being the axis;
      `Latent`'s three-code scopes fire 0.2 times each, so depth is never tested. Fork **112**.
    - **NOW** — concept-before-label is measured, and alternation groups things with no word
      for them yet. Four groups on bAbI, all word classes.
    - **OPEN** — what rung five names is a SET, never a variable, so the two rungs are not
      independent: a code carrying position AND value together makes the shared thing
      unnameable.
    - **OPEN** — label-first is unbuilt: being told a word for a thing nothing else is known
      about.
    - **OPEN** — a word is one hash, so `walked` and `walking` are as unrelated as `walked`
      and `kitchen`. Sub-word codes BESIDE the atom, never instead; letters are background by
      construction. Priced by a corpus statistic first. Fork **108**.
    - **DEAD** — graded codes to make a POSITION nameable; the code reached the moment and
      no scope. Revives if naming ever looks inside a scope. Fork **36**.
    - **OPEN** — whether rung five buys anything a better-aimed projection does not, patch
      tokens having raised the floor while abstracting nothing. Fork **42**.
    - **OPEN** — two clean rules disagreeing about one code name the redundant one neither can
      see. Fork **80**.
    - **OPEN** — should the separation bar be charged by what a scope's codes STAND FOR rather
      than by how many. Fork **71**.
    - **OPEN** — a category is the set of codes that are ALTERNATIVES, from moments alone. Open
      on the individual, which substitutability never reaches. `csharp` refuted a SIMILARITY
      code as the coarse form, a hub at one end and an index at the other, so **83**, **84** and
      **85** want a told alphabet. Fork **97**.
    - **OPEN** — a name over ALTERNATIVES is derivable and not admitted, rung five being the
      wrong SHAPE: it names what CO-FIRES, and alternatives never do.
    - **OPEN** — so likeness read off the POPULATION rather than the moment: two codes are
      alike where the commitments naming them EXPECT the same things. Never asks whether they
      co-occurred, which is the one thing they never do. Fork **129**.
    - **OPEN** — and it is the wall both architectures hit. `csharp` refuted widening a walk in
      three shapes and its row asks for a likeness the GRAPH DID NOT COMPUTE; this branch
      refuted a similarity code. Four tries, two designs, one target.
  - Understanding deepens without limit
    - **NOW** — repair. Specialisation on failure, gated, adding a narrower rule and never
      editing the old. Rung one of the ladder, conjunction, and the only rung there is.
    - **NOW** — the gate is the whole difference from overfitting: twenty misses before a repair
      is allowed, and Z must clear a two-proportion SEPARATION bar between its rate in the misses
      and in the hits, corrected for candidates. Uncorrected, noise clears any bar.
    - **NOW** — and Z is what the HITS had; backwards it mints a child reliably wrong, and a
      code commoner in the MISSES is the condition for a NEGATED one. A random-Z arm runs
      beside it, and the bet is dead if discriminative-Z does not beat it.
    - **NOW** — the ladder's admission is decidable and computed: the language extends only when
      nothing in it separates failures from hits. A rung is admitted for ONE commitment, and
      where two clear, the SHORTER description chooses.
    - **OPEN** — it only ever NARROWS, rungs one to four making a scope smaller and nothing but
      rung five broadening. A specialise-only machine is arbitrarily accurate and conceptless,
      and the ladder's ORDER is a bias over when a construct is tried.
    - **OPEN** — a total is a lifetime, which C4 refuses, and the earned rate replacing it reads
      as free on `Arranged` rather than as a budget.
    - **OPEN** — the curve is `Curved`, that rate capped by the parent's hits, and it is wired
      rather than measured. Its grid is `Arranged`'s. Fork **110**.
    - **OPEN** — a fresh child starts BLIND and re-earns its statistics, a floor every rung of
      a chain pays while only the last pays off. The escape is rung five.
    - **OPEN** — rung two, negation, *X and NOT Z*. Unsound against a live moment, so it may
      only be read against a SETTLED occasion and fires one settlement behind; its candidates
      must be bounded to codes seen in this commitment's own hits. Fork **30**.
    - **OPEN** — emit *Z was absent* as its own code at settlement, so rung two needs no new
      matcher. Bounded to the commitment's own hits. Fork **64**.
    - **OPEN** — `Mending.Uncovered` is a gate plus every-round repair, and the gate alone is far
      worse than none. Fork **37**.
    - **OPEN** — a per-COMMITMENT signal separating *needs specialising* from *is being
      outvoted*. Fork **45**.
    - **OPEN** — a blinded repair gate costs on the multiplexer. Open on any other world.
      Fork **55**.
    - **OPEN** — the gate's sign flips with the timing, and what is ruinous is the gate AFTER
      a failure. Open on any other world. Fork **58**.
    - **OPEN** — genesis mints ONE scope over a scene and repeated scenes narrow it by
      overlap: specific-to-general, the DUAL of repair. Fork **63**.
    - **SETTLED** — `Budget` is a re-derivation limit rather than a search limit, its
      apparent interior optimum having been the ballot. Fork **66**.
    - **OPEN** — does a conjunction EARN its narrowing? Reach halves with depth on even worlds
      and RISES under skew. Fork **68**.
    - **OPEN** — repairs sit at the world's minimum sound scope. Open on walking a chain in
      fewer steps. Fork **73**.
    - **DEAD** — one repair adding two codes to spare a miss floor; coverage fell while the
      carriers overshot. The revival row is in DO NOT RE-TRY. Fork **74**.
    - **OPEN** — nothing stops a chain at a sound depth. What signal INSIDE the machine says
      stop here. Fork **75**.
    - **OPEN** — the world holding repair still does it with a one-code moment, which any
      brain-side code breaks. A world whose codes never VARY would hold it on principle.
    - **OPEN** — a budget buys re-derivations. Does quantity buy the uncovered rounds or only
      more population. Fork **76**.
  - Which aspects are temporal
    - **NOW** — a forward store beside the population, retracting where counters cannot.
    - **NOW** — rung three, sequence. A precedence is a CODE derived where the moment is FORMED,
      so matching, the tally, repair and the wire are untouched. No dial, inert where no order
      is reported.
    - **SETTLED** — rung three reads real English, and its gain tracks the front end's
      SELECTIVITY rather than the task: largest under `Chained`, nil under a bag. Fork **109**.
    - **OPEN** — rung three is blind on a word said TWICE, so a thing that MOVES is not tracked.
      Placing a repeat at its LATEST recovers a RECENCY bar needing no learning and never clears
      it, so a front end reaches the shortcut and not past it. John's. Fork **119**.
    - **NOW** — and a question NAMING which thing it is about clears it, by every placing alike.
      John's, and the first reading here that is not a recency proxy.
    - **OPEN** — a referent is a THIRD store: *Mary's bedroom* survives leaving the room and
      *Mary is in the bedroom* does not, so one store gets a lifetime wrong whichever it takes.
    - **OPEN** — nothing SCORES the update, so what retracts is the experimenter's rule. Fork
      **104**.
    - **OPEN** — does a band the learner MINTS differ from a handed one. Fork **92**.
    - **OPEN** — does overwriting dissolve the selection rather than help it, on
      `Distinguished`. Fork **94**.
    - **OPEN** — the key that moved last is not worth following: it leads where one walker
      makes it the only candidate and loses at four. Fork **95**.
  - Several grains at once
    - **NOW** — subsumption keeps the general rule where both are equally accurate, and reads a
      category's entailment.
    - **OPEN** — the gradient is fragile: the vote takes the narrowest every round and
      subsumption the general one every thousandth. A second store needs an evidence rule the
      first lacked or it collapses too.
    - **OPEN** — specificity as a gradient across the SITUATION stores too. Rules have one;
      situations have nowhere to keep it — repetition for a general rule, assertion for a
      particular, and the vote ranks repetition only.
    - **OPEN** — whether compression is self-regulating. No signal yet. Fork **23**.
    - **OPEN** — project each scope code to its COARSER form when counting pairs. Test the
      rewrite first: a name no scope can say is a word with no referent. Fork **83**.
    - **OPEN** — `IQuantizer` must answer *what is the coarser form of this one*, which is
      the first thing a world tells the brain about its alphabet. Fork **84**.
    - **OPEN** — a coarse name entering a scope as a new claim is redundant where the moment
      carries the category. Open where none does. Fork **85**.
    - **OPEN** — three stores rather than two, the missing operator minting an INDIVIDUAL that
      no rung covers. Fork **93**.
  - Malleability is the record
    - **NOW** — an accuracy-weighted vote plus a recency-weighted local estimate that never
      merges. This one works.
    - **SETTLED** — the local decaying estimate earns its keep: level with a lifetime average
      where the world holds still, ahead where the target moves. Fork **27**.
    - **NOW** — `Rhythm` is run: the one world whose ANSWER moves and where a scope cannot grow,
      so repair and every rung above it are held still.
    - **OPEN** — more unsound commitments resident than sound ones while the score holds: is
      the vote robust to them, or are they why it stops short. Fork **35**.
    - **OPEN** — the live problem is which rule gets the seat, and three arms at it have
      failed. A gate changing what is HELD cannot reach what decides, and one refusing a young
      rule its vote silences the machine rather than reseating it.
    - **NOW** — and the gap IS the seat, measured: every claiming arm holds a rule for every
      truth an enumerable lesson states while answering wildly different shares of it.
    - **OPEN** — so the axis is TIME, not the vote rule. More tellings close the gap on their
      own, so what is wanted is a correct young rule outranking a wrong old one sooner without
      a lucky young one winning. `Crediting` is one point on it and the vote gate was the
      opposite direction.
    - **NOW** — `Alternating` is live and its store add-only, so a front end fills its OWN
      vocabulary, reaching every group the experimenter's holds for no score.
    - **OPEN** — it costs 4.7x the rules. Nothing waits for a group to hold still, and the
      closure sightings are what a patience rule reads. Fork **130**.
    - **OPEN** — mutual exclusion is unbuilt, so a belief can be wrong and never CONTRADICTED.
      A miss says *I expected Y and got Z* and nothing says the two cannot both hold, which is
      the whole of what a conflict is. Fork **99**.
  - Learns by being wrong
    - **NOW** — commitment, settlement, blame capped at one hop, repair, and abstention so a
      round that could not settle costs nothing. Reading is an objective: a sentence a story,
      and withheld sentences the exam.
    - **OPEN** — what it converts is unread, and English's alphabet is far wider than anything
      here has run on. Fork **89**.
    - **OPEN** — a round is fold, fire, vote, and nothing puts what fired BACK in the moment,
      so a conclusion needing two statements is unreachable at any repetition. Fork **28**.
    - **OPEN** — and neither the loop nor a selecting front end reaches it: seven shapes are
      refuted. What the question does not NAME cannot trigger the link. Fork **125**.
    - **OPEN** — the horizon is K occasions, K=1. Fork **28**.
    - **OPEN** — entailment depth and the horizon are capped at one; both come off when blame
      diffusion has a number. Fork **32**.
    - **NOW** — a separating condition must also leave a child that can clear the floor itself,
      or it is a rule nothing could ever refute. The trigger fires at last. Fork **86**.
    - **OPEN** — and what that bar COSTS is a function of how young the population is, free at
      saturation and most of the examination before it, because it blocks repair while nothing
      can clear the floor yet. Unpriced, and it is why the bar does not ship. Fork **86**.
    - **NOW** — a SHRUG is an outcome, so an ask costs something and where to ask is LEARNT.
      The machine declines a statement far more often than a question and was told neither.
      Open is a guess entering the moment it is scored on. Fork **117**.
    - **SETTLED** — the front end intersecting the question with EACH statement answers task
      one where the bag sits near the marginal, and one hop is all it reaches. Fork **88**.
    - **OPEN** — whether reading real English is predictive enough to teach this learner. bAbI
      is disqualified as a primer, its held-out half being re-reading. Fork **100**.
    - **OPEN** — two English objectives read one corpus at wildly different rates, so no single
      capacity sizes both. Fork **101**.
    - **BLOCKED** — the exam tier above bAbI is unpriced until the components pass. Fork **90**.
    - **OPEN** — a subject told once and then an UNSOLVED problem, scored by partial progress so
      improvement has a scale rather than a pass mark. John's, and it is a measurement design
      rather than a task list. Blocked behind rung four. Fork **128**.
    - **OPEN** — it is a near-perfect READER and a hopeless SELECTOR, a front end handing it
      the right statement answering a whole task at a twentieth of the bag's population.
      Selecting IS reading a commitment backwards, so fork **115** carries it.
  - Told, never architected
    - **NOW** — a front end may say what it is looking at, never what to conclude, and
      `SeparationTests` fails the build.
    - **NOW** — a final `?` is all a world says, on the conversation, so questionhood is the
      learner's. The corpora still hand it separated. John's.
    - **OPEN** — how hard a fleet searches is a deployment choice, which is a world reaching
      into the brain one level out. Fork **60** carries it, under the machine.
  - What it is told must be settleable
    - **NOW** — `Roaming` asks what the statement it is telling DID, so a told statement carries
      a settlement and can be wrong.
    - **OPEN** — no arm reaches its marginal, so it prices rung four rather than ranking
      anything, and testing the MECHANISM wants an effect a conjunction can reach.
    - **OPEN** — the store's update rule is still the experimenter's, so what is falsifiable
      is *this statement changes what is known* and not *my store was right*. Fork **104**.
    - **NOW** — a world that ASKS, so the machine obtains its settlements rather than being
      handed them. A claim needs a rule and everything else is a question, which breaks the
      bootstrap: without a blind ask it never asks, settles or mints. John's.
    - **OPEN** — on a read corpus the OBJECTIVE is the wall and no gate reaches it: the
      informative words are the unpredictable ones and the predictable ones are `to` and
      `the`.
    - **NOW** — OSTENSION is the signal rather than a shortcut around unsupervised reading.
      Which word the question is about is information no co-occurrence contains, and it is the
      pointing-and-naming shape.
  - Original thought
    - **NOW** — `IActed` is the verb and what was done rides in the moment as a code, so a scope
      names it and expects the consequence.
    - **NOW** — the chooser is `Drives`, reading a population and preferring by felt bands, and
      it loses to both controls.
    - **OPEN** — ranking by its own expectation wins by making the world CONSTANT, so the
      preference wants a term a dead body fails. The goal is unbuilt. Fork **111**.
    - **OPEN** — a commitment read BACKWARDS is a plan: want a code, take the scopes that
      entail it, do the part of one you can. No new machinery, and what bounds it is the
      entailment cap of one. Fork **115**.
    - **OPEN** — a goal is a SET of codes wanted present, so a goal and a prediction are one
      type once what is expected is a set. What is missing is which set, never how to say it.
    - **NOW** — asking is a RATE and the signal is the POPULATION's, every chooser that read the
      vote having lost. What predicts whether a reply can settle is learnt from being wrong about
      it rather than chosen.
    - **NOW** — more than one doing a moment, so the machine can ask, be refused, and ask again.
      The world says whether it will take another and the chooser whether it has more, and where
      nothing refuses it changes no reading.
    - **DEAD** — a refusal as a code in the moment, the chooser having already refused to
      repeat itself. Revives where the exclusion must survive the moment. Fork **127**.
    - **OPEN** — a drive that cannot be sated is a fault in the design and not a risk to
      manage, so a term must have a point where it stops pulling. John's.
    - **NOW** — `Roaming` is acted in and declining leaves the walk the world drew, so the
      watched arm is a chooser and not a second world.
    - **NOW** — `do(x)` is distinguishable from `x`: `Intervened` derives a code beside each one
      the learner was handed, on rung three's seam.
    - **OPEN** — and no world here holds a common cause, so nothing says those claims are
      better.
    - **OPEN** — and nothing ranks a chooser there, a house having nothing to want. Twins are
      the tier still unbuilt and are rung four's; an isolating world is still built freely and
      goes when its question shuts, only a constructed world proving a ceiling.
    - **OPEN** — TextWorld's shape is built here and watched before acted in; Crafter puts two
      unbuilt subsystems in front of the measurement. Open on twins. `csharp` disqualified
      SURVIVAL and refuted absolute actions unrotated, so an acting world owes both. Fork
      **103**.
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
    - **NOW** — `Posted` and `Cycle`: asks, answers and settlements over real sockets, the
      learning loop still existing exactly once. THE WALK IS GONE and forks 1, 5, 6, 11, 18,
      20, 21, 22 and 24 went with it; `csharp` keeps their code and numbers.
    - **BROKEN** — worlds with no runner, their `*Run` files being the walk's. Each wants a
      `Bench` or a deletion, and `OutstandingTests` prints the debt.
    - **NOW** — the vote's arithmetic composes and a whole learner runs over it. Fork **52**.
    - **NOW** — a round is a barrier, so lateness costs the CLOCK and not one answer, and C2's
      out-of-order half is untestable here. Breaking it is unmeasured.
    - **NOW** — a fleet cannot be quiesced, because waiting for every holder waits forever on
      a silent one. A tally therefore reads machines still writing, and is atomic, not final.
    - **OPEN** — what the repair gate's query costs on a wire, priced on loopback. Open on a
      LAN. Fork **56**.
    - **OPEN** — the vote decides what repair may run on, so under skew blame lands on the
      majority lineages. Open on what breaking that costs at width. Fork **65**.
    - **OPEN** — rung five's evidence is the population, so splitting the population splits
      the agreement. Whether shipping name frequencies recovers it exactly. Fork **81**.
    - **OPEN** — every node predicting its own output while the real wave verifies behind it.
      It saves a hop, so where it pays is deployment. `csharp`'s adaptive version wrote most
      where it helped least, so WHAT to spend on is the open half. Fork **57**.
    - **NOW** — replicas DRIFT, because the completeness condition ends a round on one of
      them and the other may take the next moment before the last settlement. Order rather
      than content, so a failover replica is a similar population and not the same one.
    - **NOW** — only IDENTICAL evidence converges on a name. Machines sharing most of a
      stream agree as poorly as machines sharing none, so merging the counts is the only
      thing that works rather than an optimisation.
  - The world pushes and the brain receives
    - **NOW** — `Brain.Receive` takes a stamped moment, and one not advancing its source is
      refused rather than settled; `Tally.Refused` counts them. Overrun is that same door and
      is unbuilt, so the refusal is not yet a backpressure reading.
    - **OPEN** — a commitment is settled by the SUCCESSOR moment from the same source, so
      absence is established by arrival rather than by a clock. C2 forbids the deadline.
    - **OPEN** — and a settlement must CARRY the moment it settles, which `Holder` already
      does and `Alone` does not. Two sources clobber its one remembered firing.
    - **NOW** — `IInput` is one WORLD pushing stamped moments, `Watching` is the join,
      `Bench` is the loop. `Trial` is gone; `Alone` stays, being the substrate seam.
    - **OPEN** — `IWorld.Next` is still a pull behind `Watching`. A sense on its own schedule
      is a second `IInput`, not a second bench. Fork **113**.
    - **NOW** — statements as MOMENTS, on the conversation. A typed SENTENCE is one moment, so
      a pasted paragraph arrives one at a time. The corpora still arrive whole. John's.
    - **NOW** — a statement CLAIMS its rarest word, so being told is falsifiable, mints, and
      answers an exam never sat. Delete the statements and nothing is learnt. John's.
    - **NOW** — and REPETITION earns it, at the repair gate's floor rather than the lesson's.
    - **OPEN** — what a moment carries beside its own sentence. A question re-handing the story
      leaves every code always-present, so genesis roots on nothing. Fork **120**.
    - **NOW** — an assertion may mint its WHOLE scope, so a conjunction is stated rather than
      discovered by failing, and a fact costs fewer tellings. John's. `Rooting`.
    - **NOW** — a mint is CREDITED with the round that made it, being right about it by
      construction, and a correct rule is then believed a telling sooner. `Crediting`.
    - **NOW** — a statement claims EVERY word in turn, one moment each, so nothing picks one.
      Told once, it answers an exam never sat. John's. Forks **121** and **123**.
    - **NOW** — and a source owing moments is drained before a new line is read, so a scripted
      one cannot be advanced past a sentence still arriving.
    - **SETTLED** — claiming, width and crediting all reproduce on drawn lessons, so none of
      those readings was about the one hand-written text. Fork **124**.
    - **SETTLED** — claiming every word makes a rule wrong on its own sentence's other claims,
      and the churn that bought was children too small to judge. Fork **126**, by **86**.
    - **OPEN** — rarest is one split of a statement into scope and claim, and the brain does not
      choose it. What picks the claim with no experimenter. Fork **123**.
    - **NOW** — a contradicted belief is REPLACED by being outvoted rather than retracted, on
      ONE contradiction, and uncontradicted facts do not move. John's.
    - **OPEN** — one brain process, worlds attached over a stream: fork **113**'s shape as a
      HOST. Blocked on cost, a pipe making every grid a round-trip. John's.
    - **NOW** — one claim a MOMENT rather than a set, which is fork **114**'s second arm: a
      statement carrying several claims becomes several moments and genesis is unchanged.
    - **OPEN** — what is predicted is a SET and what is done is a set, so one motor moving and
      a sentence written out are one shape. Scoring becomes precision and recall, and every
      baseline here is re-taken rather than preserved.
- **WHAT THE INSTRUMENTS MUST SAY** — an instrument that cannot fail says nothing, and every
  ground-truth one here needs a world that can be enumerated.
  - A run reproduces exactly
    - **SETTLED** — a fixed seed reproduces a run exactly, across sockets too; `Receive`
      folds arrivals in delivery order. Fork **12**.
  - The table fits and the clock allows
    - **OPEN** — the TABLE is what blows up rather than the commitments, needing an entry per
      code seen while firing. It spills to SQLite on the owning node and rehydrates as a
      candidate — and it is repair's candidate set, so a spill changes what fires. Fork **31**.
    - **OPEN** — matching and settling are nine tenths of the clock on a narrow world whose table
      never grows. Where they go on a WIDE one. Fork **49**.
    - **OPEN** — a child fires only where its parent does and matching IGNORES that, going
      through the code index. Rete's own problem, with the wrinkle that culling orphans a child
      and an orphan that stops firing reads as nothing.
    - **BROKEN** — four `EncodedTests` fail on a file never built: the graph is cut one `Gemm`
      early to drop a 1000-way classifier, needing `onnx`, which the runner lacks.
    - **BROKEN** — `BudgetTests` crosses two settings and pins neither timing nor budget, so it
      changed arms silently; being a sweep, CI never looked.
  - Withholding is real and the gap is readable
    - **SETTLED** — a generated world holds assignments back without the learner being able to
      tell, the draw rejecting rather than picking. Fork **48**.
    - **OPEN** — the held-out gap against RECURRENCE, the number saying how big a bag a world
      needs. Fork **41**.
    - **OPEN** — a withheld observation becomes a PAIR under settlement by successor, the moment
      and the one after it. Every generated world owes it.
  - A score says how often and never which
    - **NOW** — a transcript is an instrument and the cheapest one here. A population answering
      everything with the commonest word and one that has learnt the task read identically until
      the words are printed.
    - **NOW** — a held-out question can be word for word one already asked, the corpus being
      templated over a small cast. So an unseen score is read beside a count of its twins
      rather than on its own.
    - **OPEN** — a STANDARD story told once and a fixed question set, so an adjustment is read
      against one thing. John's. Hand-written and tiny beats a found text, which gives neither
      enumerable ground truth nor a computable RECENCY bar; the exam stays bAbI's.
  - What a rule learner is worth beside a probe
    - **OPEN** — given symbols worth having, how close a conjunctive rule learner comes to a
      linear probe on the same vectors. Fork **43**.

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
| Refusing an untested commitment its vote | Refuted twice: inert, then ruinous where its own row asked, the population holding every truth stated. It silences the right rule rather than reseating a wrong one | A floor read against a rule's own OPPORTUNITY, a fact stated a few times never clearing twenty |
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
| `Widening` — making a scope shorter, on all three arms | Both arms lose the withheld set on `Arranged`, whose one-code truths are where shortening should have helped; a drop usually makes a sound scope unsound, and the gate against that is inert under `Floor` | Judging a drop before it is resident: fork **64**'s absence code |
| `Joining.Situated` — displacement keyed on the commonest N words of the corpus | The motion verbs straddle the names, so no rank keeps names as keys and drops verbs | A corpus whose function words are separable by frequency. `Distinguished` takes its background from the story and needs no rank |
| Displacement as a way AROUND unification | At every width it is at its ceiling only where it keeps one statement, and a better key rule bought a better ceiling and no more score | Never as a substitute. A store must be read BY KEY, and reading by key is rung four |
| A forward store whose fold is transitive | It reaches every answer with nearly every room word still there, which is the bag by a longer road | Never uncapped. The depth's optimum is interior, so the reading that set the cap refuses a fold without one |
| A precedence's TRANSITIVE closure rather than the adjacent pairs | Identical ceiling on `Handing` for two and a half the population: a quadratic expansion to say what adjacency entails | A world whose relation spans an intervening position, where adjacency falls short |
| `Chunk`'s whole-moment rule ported to rung five | Two vocabularies: `Narrows` is syntactic, so a scope keeping the members and its children taking the name stand in no relation. Unsound rose on every seed that moved | A subsumption test read at the UNFOLDED grain |
| A front end putting a word's POSITION in the moment | Beside the code it is never absent, reaching every moment and no scope; FUSED in, it costs the identity — an input is an attribute, never the concept | Never. Order derives its own code: rung three |
| Marking the question-story coincidence — `Named`, `Anonymous`, `Either` | Rung four's cheap tests, and they answered: what blocks it is admission, not cost | A derived code over `Bind`'s groups that cannot say *this one is in two of them* |
| Carrying the DECIDER's identity into the next moment, so a scope roots on it | No score moved, the table grew by half, and on a cyclic world the identity SEPARATED — costing `Rhythm` the moment that holds repair still | Siting it as MEMORY rather than metacognition, and a control whose codes never vary |
| Curiosity read off the vote — `Unsure` on the margin, `Untested` on the weight | Both lose to a coin per ask, tenfold and fiftyfold over eight seeds: a conversation leaves the machine unsure exactly where nobody can answer | A signal saying whether a reply CAN settle, not whether the machine is sure |
| Keeping every mention of a repeated word, and marking the last | Worse than dropping the repeat where a thing moves, and a marker for *nothing follows this* moved it nothing. More precedences give repair more to grab, so `wanting` falls to nought while the score does | A rung that can say a NEGATIVE — fork 30 |
| Claiming only a sentence's LEAST-said words, so the two claiming rules become one comparison | Worse on both axes, needing ten times the telling to reach what claiming every word reached at once | The population cost of claiming every word binding, which is a corpus rather than a lesson |
| A refusal settling on a reserved outcome rather than the round abstaining | Nought over eight passes either way, and counting it taught the machine to stop asking | A CHOOSER that reads one. Recording a refusal buys nothing while nothing avoids what it refused |
| A question carrying the topic while statements stay bare, so a SELECTING front end has something to walk | Nought on the implied half under every front end, and worse on the stated half than carrying nothing at all | A relevance mechanism the front end does not have. No arrangement of what exists reaches a second fact |
| A second hop in three shapes: every conclusion made live, the winner's alone, and only rules that USED one voting | Nought on the implied half in all three, and the run's own accuracy fell each time | RELEVANCE, or a sub-question — a question not NAMING the intermediate cannot trigger the link to it |
| `Joining.Recent` — a word banded by how far back it was said | Half what narrowing the view buys, and the front end picks the bands | A band the learner mints. Fork 92 |
| Background admitted to a WIDE genesis scope, a code in every moment being unable to change when the rule fires | A third of what the varied filter reaches at three tellings and three quarters at ten | A world where the varied gate refuses a code a conjunction needed |
| A refusal as a CODE in the moment, so a negative fact enters positively | Level on both shapes for a larger population. The chooser already refuses to repeat itself, so the fact was acted on before the code arrived | The exclusion having to SURVIVE the moment, which is a store's job rather than a code's |

---

## TRAPS

Grouped by FAILURE CLASS rather than by incident, because ninety separate lessons is a list
nobody finishes. Each names the sharpest instance; the rest are in the commits that found
them. **A class earning a check moves out of here into the check.**

### The harness lies, and nothing goes red when it does

- **A hand-typed filter runs the grids CI excludes.** Naming a class names its sweeps too, so
  two suites ran past forty minutes where `kind!=sweep&` takes them to seconds. The facts were
  tagged correctly and the COMMAND was not.
- **Pushing faster than the suite runs means nothing is ever tested.** The concurrency group
  cancels whatever is waiting, so a session committing every few minutes cancels its own queue.
  Only a `[checkpoint]` escapes.
- **A build during a test run can abort it**, every test passing while the assemblies are
  replaced underneath. The mirror of the `--no-build` staleness rule.
- **A cost measured on one platform can be nought on another.** A refused loopback connect is
  four seconds on Windows and immediate on Linux, so a shard went red for a repair working
  perfectly, and the same four seconds prices the local suite alone. Read a wire timing on CI.
- **A workflow is the one artifact with no local check**, and it is wrong until a push says
  otherwise. And skipping work is not skipping a job: a matrix entry that exits at once still
  took a runner slot.
- **A timed-out job reports as cancelled and not as failed**, and one makes the whole run read
  cancelled. Where cancellation is the NORMAL outcome an overrun is disguised as the concurrency
  group working, which is how a `[checkpoint]` can look cancelled by a push it is immune to.

### A check that cannot fire reads exactly like a check that passes

- **Arm anything that has always read zero.** `Surprise` and `Abstain` were both found wired
  and unable to fire, and *promiscuous on purpose* meant EXHAUSTIVE for the life of the repo
  because its gate was mounted nowhere.
- **A guard mounted on one caller is not mounted**, and a code path guarded by a cap is
  untested until something reaches the cap. Both sat unexercised for the life of the repo
  because no world was wide enough.
- **A documented promise is not a check.** `Posted` said a fan-out was posts in flight while
  both of its fan-outs awaited each post in turn, false from the day it was written and directly
  under the sentence describing the fault. A fix aimed at one measurement's callers leaves the
  rest.
- **A budget can be satisfied by a coincidence**, and a cast to an interface the type does not
  implement is cleanup that never runs. Both compile and read as tidy.
- **A prediction written into a wiring check fails two ways**, and reads the same. Assert that
  arms DIFFER, never which way.

### A comparison that moves two things at once

- **Measure one mechanism on from a known baseline**, never one off from all-on.
- **A setting can decide two independent things while being named for one**, so the cell that
  separates them may already exist and never have been read as a control.
- **A readout arm is a search arm** wherever the readout triggers the search, and every vote
  comparison in four sessions moved both.
- **A fixture inherits every dial it does not pin**, so a moving default rewrites an experiment
  nobody edited — and the grid deciding a default rewrites itself the moment it wins.
- **A default can short-circuit the mechanism being measured**, so a sweep on defaults
  returned three identical arms for a gate that was never running.
- **A forced control is not a control.** A chooser made to ask wherever nothing fired spent a
  quarter of its budget by the harness rather than by the arm.
- **A test can fail at both ends of a dial**, for opposite reasons, so pinning to the old value
  fixes nothing while reading as a fix. **Never attribute a red test to your own change without
  a baseline.**

### A score reached the wrong way

- **An accuracy can be hit by memorising.** Report the commitment count beside every score,
  and on a world with known ground truth report how much of it was found.
- **A corpus can contain its own answer**, and then a score measures the leak. A generated
  world cannot, which is half of why the multiplexer is first.
- **A skewed world raises columns that rank arms for free**, and a grid of identical
  rows is a verdict on the worlds rather than on the arm.
- **A precision taken at the answer's own size is the experimenter's knife.**
  Nothing inside the machine knows a category has four members. Report the size-free cut.
- **A front-end arm has a ceiling computable with no learning**, and it costs milliseconds
  against a runner's hour. Take it FIRST — a grid cannot tell a rule that dropped the wrong
  sentence from a learner that failed to use the right one.
- **Two arms that score alike need not be the same mechanism**, and a score cannot say. A cap
  that refuses nothing and a cap that refuses a lot read identically until something counts
  what was BUILT.

### A statistic whose halves count different things

- **A rate whose numerator counts rules says nothing about coverage**, and a share
  whose halves count different events announces itself by exceeding one.
- **An exact partition of what arrived says nothing about what never did.**
  The lineage that mattered was absent from the denominator.
- **A list that appends duplicates is a count wearing a set's shape**, and every reader gets
  whichever it assumed.
- **A periodic sweep inside a conditional runs at that condition's rate**, so subsumption and
  culling read as mechanisms that bought nothing. Its dual: a periodic sweep against a
  per-round rate that scales with the front end holds a population far above its capacity.
- **An explanation can be arithmetically true and still not be the cause.**

### Too few seeds, or too much trust in the spread

- **One seed is not a comparison and will happily invert.** Error bars before ordering, every
  time, and count seeds in BOTH directions — a small sample hides a real effect as readily as
  it invents one.
- **A seed spread is not always a yardstick**, so a kill line resting on one can be vacuous:
  identical scores in every cell admit any gain at all.
- **An estimate is noise before it is a statistic**, and a chaotic run keeps the perturbation,
  so a mid-run reading and an end-of-run one are different measurements.
- **A winner-take-all argmax is chaotic in its evidence**, and two ends of a sweep cannot
  show it.

### Reproducibility broken from outside the code

- **A dependency's defaults can break it silently.** Parallel inference reorders float
  reductions and a code is a QUANTISED number, so a reading at a band boundary codes
  differently run to run.
- **A tie-break by dictionary walk is stable until there are two tables** — reproducible in
  one process, arbitrary across a merge.
- **Every equality reading a report asserts on the measurements inside it.** A wall clock
  in a record turns reproducibility red and makes every `NotEqual` beside it pass for free.
- **A `readonly record struct` holding an `ImmutableArray` compares by the array's identity**,
  so two separately built keys with identical contents are never equal.
- **A type can drop most of itself on the wire**, and still write a plausible number. Private
  tables and tuple keys serialise to nothing. Pin a format failure with a check on the
  ANSWER, never a comment.
- **A local builder's invariant is not the received form's.** No pair built here holds one
  code twice; the wire form takes whatever arrived.

### Reading the machine wrong

- **A doc can name the wrong blocker**, and be believed for a whole branch. Read the code
  before costing the fix.
- **Read the revival rows before proposing a missing arm** — a row may name the same axis in
  the mechanism's words rather than the comparison's, which is how a search misses it.
- **The tell is often a distribution, not a score.** A hard ceiling immediately below a
  threshold is never a coincidence; read the spread, not the mean.
- **An online score below the final population's is a churn signal** — that population is
  scored on fresh observations, and it is being rebuilt faster than the trailing window can
  read it.
- **A mechanism is local or population-wide by accident until something splits it**, and
  nothing in one process can tell the two apart.
- **A cost can be in memory while every instrument watches time**, and a claim about
  correctness will do duty as a claim about throughput unless somebody measures.
- **A simulated constraint can be harsher than the real one.** `HybridBus` reorders on purpose
  and TCP does not, so a green distributed run says nothing about C2.
- **An answer key in the wrong alphabet scores nought**, and looks like a verdict.
- **A fallback is a control arm nobody meant to run** — silence drifts an arm toward the
  random bar for free. Report silence beside the score.
- **Deleting the last arm deletes the check that made the deletion legitimate**, so a property
  asserted across three arms becomes an argument again with one left.
- **The instrument that kills a story is usually built for something else**, so ask which grid
  already holds the number before running a new one. And ask which half of generate-and-test a
  proposal touches: where no right rule was present, a rule about who WINS cannot reach it.

---

