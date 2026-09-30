# Unfused

A frontier language model does three jobs in one set of weights: skill (grammar, reading,
composing an answer), knowledge (facts about the world), and memory (what it was told a minute
ago). Only the first needs dense computation. Knowledge is sparse and tolerates latency, and
memory is what a model is worst at keeping. This branch keeps the three apart:

- **Skill at language** is a small pretrained model, frozen, and it only translates. As the
  ears it turns a sentence into typed assertions and a question into a query. As the mouth it
  turns an answer into a sentence. It is called the faculty, and nothing here trains it.
- **Thinking** is built here and is not a neural network: storing what was asserted,
  following chains of it, knowing what changed and when, knowing what it was never told, and
  learning rules from what it hears. It learns from one hearing and keeps what it learnt
  across a restart.
- **Knowledge** is that memory at scale, spread across many devices. That is the distributed
  half of the project, and it comes after the thinking works on one box.

The measure of the bet is that the faculty can be too small to reason. If a system with a
mouth that cannot compose two facts answers questions that need composing, the composing is
the system's. A faculty that reads notes and answers from them makes the branch a
context-reduction scheme, which is worth building and is not the goal: its ceiling is the
faculty given the whole transcript.

It replaces `sylvatica`, `commitments` and `csharp`. Their plans and one review are under
`docs/history/` and their refutation tables still hold for what they measured. The two
gradient-free learners lost to a blind rule on a house world. The `architecture` spike showed
why: a learner is worth what its prior contains, and they had to learn to read as well as to
think. Here the faculty reads, and the learner thinks.

This is the one design doc. It says what to build, in what order, and what reading refutes
each bet. What a built thing does is in its code.

---

## THE ORDER

The one list a session edits at both ends. Each phase's exit is a measurement.

- ~~**Phase 0 — Ground.**~~ Struck 2026-09-29. `readings/exam-*-served-s0-*.json`.
- ~~**Phase 1 — Text memory.**~~ Struck 2026-09-29. `readings/exam-*-Qwen3.5-9B-s0-*.json`
  on the final house. Best text arm: `linked`.
- ~~**Phase 2 — Small mouth.**~~ Struck 2026-09-29. 2B on seeds 0 to 2, 0.8B on seed 0.
- ~~**Phase 3 — Ears.**~~ Struck 2026-09-29 under the strict scorer, which wants each gold
  filler in a slot of its own: Qwen3.5-2B at 0.96 recall and 1.00 precision.
- **Phase 4 — The system answers.** PAUSED 2026-09-29 for Phase 5, John's call. Built, with
  three faculty roles (ear, planner, judge) that can each be a different model, and kin. With
  the 0.8B as ear and the 9B as planner and judge, seeds 1 and 3 meet the exit and seed 2's
  count does not; on bAbI the same system is below the 0.8B reading each story (0.105 against
  0.175). Both failures are rules: belonging plus being somewhere is keeping, a move changes
  where a thing is, a carried thing goes with its carrier. The exit is taken again once
  Phase 5 has them.
- **Phase 5 — Learning rules, and operations over facts.** IN PROGRESS 2026-09-29, on the
  0.8B only; the 9B is off the card. Plans learnt from taught examples (arm `taught`) on
  facts held to their sentence (`cleans`), with order in time and learnt counts as step
  kinds, answer bAbI tasks 1, 2, 3 and 7 with no model planning, and on the house they are
  taught from practice houses and beat the system's own planning. Next, in order: why no
  taught plan holds for the house's `chain3`, whose cousin shape is taught fifty times in
  five practice houses; a trade kept out of a question's shape, so the person who repairs
  clocks and the person who binds books share a plan; correction as an input.
- **Phase 6 — Importance and forgetting.**
- **Phase 7 — Other senses.** A moment arrives across every modality, and what a sense
  perceives is kept beside what it asserts.
- **Phase 8 — Fleet.** The memory on many processes, merged without coordination, with nodes
  vanishing.

---

## DECIDED

With John, 2026-09-28 and 2026-09-29. Reopening one is a conversation with him, never a
commit.

- **The target shape is a child's core, taught for life.** John's, and the idea from the
  start: the faculty needs about a five-year-old's grasp of language and no more. Everything
  else is taught after deployment, by reading or by a person, the way a child is taught, and
  the system runs continuously rather than in sessions. A sense's encoder needs only basic
  cases for the same reason.
- **Meaning is learnt by the system, not borrowed as vectors.** 2026-09-29. Off-the-shelf
  vectors, MiniLM's and the 0.8B's own, do not carry relation meaning
  (`readings/lexicon-*`). A thing's meaning is the set of what is known about it, differences
  are the properties one has and the other lacks, analogy is the same relation on two pairs,
  and concepts are formed from shared properties (formal concept analysis). Identities match
  exactly and meanings match by overlap. What everyone knows comes from the faculty, asked
  once and kept; what only this conversation knows is learnt from it.
- **bAbI is the second world, and the house stays the target.** bAbI checks that the house is
  a fair test. It is small and templated enough to be won by building to it, so a score there
  is never the objective.

- **The 1080 Ti is the hardware.** No rented compute. Anything that needs more than one
  11 GB Pascal card does not get built.
- **The faculty is a frozen pretrained language model, and it only translates.** John's,
  2026-09-29: it is the ears and the mouth, and the thinking is the system's. "No LLM"
  constrains the thinking, not the senses. Small faculties are preferred, because the bet is
  only shown by a faculty too small to do the thinking itself, and because they iterate
  faster. Qwen3.5 at 0.8B, 2B and 9B through llama.cpp; Needle (Cactus Compute, tens of
  millions of parameters, schema-constrained) as an ear.
- **Typed output guarantees shape, never truth.** A schema-constrained ear always returns a
  well-formed assertion and can still return a wrong one, so every ear is scored on what it
  gets right against the house's ground truth, never on whether it parses.
- **The thinking is not trained by gradient.** It learns by writing assertions and by
  inducing rules from them.
- **Recall is automatic.** Every input arrives with what it called up from memory, found by
  words (BM25), by meaning (a sentence embedding) and weighted by importance. Nothing has to
  ask.
- **Nothing is deleted.** Archiving is the strongest thing that happens, and a correction
  cites what it corrects. Persistence's rule.
- **Distributable by construction.** Ids are content-addressed, counters only rise, time is a
  turn index, and tables grow by union. A mechanism that needs coordination between nodes is a
  mechanism this branch does not use.
- **Python, one codebase.** SQLite for the store, llama.cpp for the faculty.

---

## THE SHAPE

- `faculty` — `Faculty` (in-process, has `surprise`, a sensor read off the logits) and
  `ServedFaculty` (llama-server). One method both share: `chat(system, user, budget)`.
- `store` — `SqliteStore`: every turn written, hybrid rank by reciprocal rank fusion of
  BM25 and MiniLM cosine, weighted by importance and recency on a turn clock.
- `arms` and `linked` — what an exam compares. `Blind`, `FullContext`, `Recall`,
  `LinkedRecall`.
- `exam` — the house generator and the runner.

---

## THE EXAM

A generated house: invented people, rooms and objects, told once each as ordinary sentences
among filler, across 300 turns. Every question is asked at delays from 1 to 150 turns, in eight
forms: `direct`, `oblique` (no shared content words), `reverse` (asked from the other end),
`twohop` (needs two facts told at different times), `chain3` (needs three), `count` (how many
people keep things in a room once every move is told), `update` (a fact that changed; naming
the old answer is scored as stale), and negatives about people never mentioned (the answer is
a refusal; anything else is scored as invented). A number is matched whole.

The house has a size and no switches. Every house has all its forms in fixed proportions. A
form is added when real conversation has it and the exam does not; a form is never removed
because an arm does badly on it.

Every reading carries the two baselines. `Blind` answers the commonest answer for the kind of
question. `FullContext` gives the faculty the whole transcript. The arm is closed and reopened
every 50 turns, so what it knows must survive on disk.

---

## THE PHASES

### Phase 1 — Text memory

Every turn is written to the store, and every question arrives at the faculty with what it
called up, stamped with the turn each note was heard at. `recall` searches once, `recall2`
searches again from what it found, and `linked` spreads from the symbols a question refers
to, dividing activation among links the way ACT-R does. On seed 0 of the first house,
stamping took updates from 0.05 to 1.00.

- **Exit:** the linked arm's verdict on seed 0 of the corrected house, and the best text arm
  named as the baseline for Phase 4.

### Phase 2 — Small mouth

The best text arm and full context under Qwen3.5-2B and 0.8B, seed 0.

- **Exit:** the readings committed. There is no refutation; this is the price list.

### Phase 3 — Ears

A generic schema, not the house's: an assertion is a subject, a relation, an object, and
optional place, quantity and time, each filler a phrase from the sentence. The relation is
mapped to a known relation where it means the same, which is lexical knowledge and the ear's
job. A question is read into the same shape with one filler unknown, or a chain of them.

- **Arms:** Qwen3.5-2B and 0.8B under a llama.cpp JSON grammar; Needle.
- **Reading:** assertion precision and recall per ear against the house's facts, which the
  generator knows exactly, and query accuracy on the questions.
- **Refutes this phase's design:** no ear reaches 0.8 on both. Then reading is the wall, and a
  bigger ear is a price to state rather than a fix.

### Phase 4 — The system answers

Assertions go into an exact table keyed by subject, relation and object, each with the turn it
was heard at. A query is matched against it, and a chain is matched link by link. Where a
subject, relation and object are asserted again with a new place or quantity, the latest turn
wins, which is how an update is heard. Nothing matching is an answer: "I don't know". The
mouth renders the found filler.

The faculty has three jobs here, and each can be a different model. The ear reads every
sentence, so it is called most and is the one that has to be small. The planner turns a
question into steps; a plan is kept by the question's shape once it finds an answer that is
not a name the question gave and uses every filler the question named, and plans carry from
one house to the next because they hold no facts. The judge says whether two wordings mean
the same, once a pair, kept in a table. The planner and the judge are called per shape and
per pair rather than per sentence, so a larger model there costs a few calls a house.

Search replaces the planner's steps. The matcher already follows a chain of any length; what
limits a chain is the planner writing every step up front. Given only the question's anchors
(the fillers it names) and the relation it asks for, the system finds the shortest chain of
stored assertions from an anchor to one with that relation, backward from the goal as a
Datalog engine does. The planner then says what is asked and never how to find it.

- **Refutes search:** it scores below `planned` with the same planner on the same seeds.
- **Arms:** the system under each ear; the Phase 1 baseline and full context under the same
  small faculty; full context under the 9B.
- **Refutes the branch's bet:** the system with a small faculty does not beat that small
  faculty given the whole transcript on `twohop`, `chain3` and `count`.
- **Exit:** it beats it and blind on all three, on three seeds, and a second house with
  relations the system was never built around is answered at no worse than half the first
  house's score.

### Meaning, concepts and correction (Phase 5)

Each symbol and each relation wording has a set of properties: the slots it fills, the kinds
of thing it links, the relations it takes part in, and the verdicts the faculty gave about it.
Similarity is overlap of properties, so it is exact, inspectable and learnt from one hearing.
Concepts are the lattice of things that share properties, formed as facts arrive; a verdict
about one member of a concept is shared with the others until a member contradicts it. A
correction ("no, that is not what I meant", "yes, that's right") is an input like any other
and changes the properties it names: a learner that is never told it is wrong cannot fix a
meaning, which is the signal the exam has lacked.

- **Refutes:** judge calls do not fall and the score does not rise on the house seeds, and
  bAbI task 2 does not rise, with property overlap in place of per-pair judging.

### Phase 5 — Learning rules, and operations over facts

A taught example is a question with its answer. The system searches its facts for the
chains that link the question's anchors to the answer, generalises each by putting slots
where the question's fillers were, and keeps the result as a plan for the question's shape
and as a rule over the relations it passed through ('the place of whoever last got the
thing'). A plan or rule is kept while its predictions on later taught examples hold, and
dropped when they fail more often than they hold. bAbI's training split and practice
questions on a house are the teaching; the test split and the house's exam stay unseen. This
is explanation-based learning, and it takes planning from the faculty, which is the planner
role's exit.

Pronouns are bound by the system: the ear writes 'she' or 'there' as heard, and the system
binds it to the entity most recently in focus that is consistent with what is known of it,
as centering theory has it.

The system meets relations it was not told the properties of: that cousin runs both ways,
that a parent's parent is a grandparent, that moving a thing changes where it is. It induces
such rules from co-occurring assertions, keeps a rule only while its predictions hold, and
answers from rules as well as facts. This is where the `commitments` rule learner comes back,
working on assertions a model has read rather than on raw codes.

- **Refutes:** induced rules add nothing over the Phase 4 system on a house whose questions
  need them.

Operations over a set of facts sit beside the rules: counting exists; comparison (more,
fewer, the largest), order in time (before, after, since) and absence (nothing heard says
so, which differs from being told it is not so) do not. Each is a step kind a plan can name,
and each enters the exam as a form when real conversation has it and the house does not.

- **Refutes:** a step kind the planner never chooses when the question needs it.

### Phase 6 — Importance and forgetting

Importance is measured, never asked for. Two sensors: surprise at writing, read as the
faculty's per-token loss on the sentence given what memory recalls for it, so that news is
surprising and a thing heard before is not (read alone, the 1.7B scores filler and facts
alike); and use at recall, where an assertion used in an answer gains weight. Forgetting is
what falls below the recall floor, never a deletion.

- **Refutes:** surprise-weighted recall does no better than uniform importance on a house long
  enough that uniform recall degrades.

### Phase 7 — Other senses

A world emits one moment across every modality, and a source is a world, never a sense. Any
sense that can say what it perceived as a subject, relation and object is an ear, so an image
read by a vision-language model writes the same assertions a sentence does. What that shape
cannot hold (a face, a tune, where things sit relative to one another, a tone of voice) is
lost at the ear, so every assertion keeps a pointer to the percept it came from, stored as an
embedding in a space the senses share. A symbol is then its assertions and its exemplars:
"kettle" resolves to a kettle seen as well as to the word, through the same embedding
shortlist `resolve` already uses, and recall can reach a percept by likeness as well as by
relation.

- **Refutes:** questions whose answer was only ever perceived, never said, fall to blind.

### Phase 8 — Fleet

Several processes, each holding a shard of assertions and fragments. Tables merge by union and
the store merges by id. Kill a third of the nodes mid-exam.

- **Refutes:** with a third of the nodes gone, the system falls below the faculty with no
  memory at all.

---

## WHAT WOULD REFUTE THE BRANCH

1. **The system with a small faculty never beats that faculty given the whole transcript** on
   the forms that need composing. Then the thinking is still the model's.
2. **The system only works on the house it was built around.** A second house with different
   relations falls to blind. Then it is a hand-written reasoner, which the earlier branches
   named as a trap: the human put the answer in.
3. **Reading is the wall.** No ear small enough to matter reads well enough to be worth
   reasoning over.

---

## DIALS

- The faculty for ears and for mouth, separately; the schema; the prompts.
- `k` hits per query and the number of recall hops.
- The store's fusion weights: `rrf_k`, importance weight, recency weight and half-life.
- The linked arm's shortlist size, spreading width and decay.

---

## OPEN FORKS

- **Shapes by likeness.** A shape matches only word for word, so an oblique question never
  meets its plain twin's plan. Matching shapes by embedding would carry a plan across
  wordings, at the risk of carrying it to a question that only looks alike.
- **A thread's working notes as a fragment**: what surfaced for the last few turns is carried
  forward, the way attention holds a topic.
- **Hyperdimensional vectors for symbols and assertions**, each symbol a random bipolar vector
  seeded by a hash of its name so every node agrees without coordinating. They buy a
  fixed-size memory that merges by addition and forgets by interference; they have to earn
  that against the exact tables in Phase 8, or not be built.
- **A reader and a typist**: Qwen reads a sentence into plain restatements and a
  schema-constrained extractor (Needle) types them. John's, 2026-09-29. Worth running only if
  a grammar-constrained Qwen reads measurably worse than the same Qwen reading freely, since
  otherwise the pipeline ends on the weaker model.
- **Needle fine-tuned on public relation data**, not on house sentences, so it learns to read
  relations without being taught the answers.
- **Calibrated ears**: an ear that returns a confidence with each assertion, so the system can
  weigh what it heard. Jev (TypeSafe AI) does this and is closed and paid; the fork is an open
  ear that does the same.
- **Idle-time inference**: the system composes and writes derived assertions when no input is
  arriving, so a chain is found once rather than per question.
