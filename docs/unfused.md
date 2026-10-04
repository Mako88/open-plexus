# Unfused

A frontier language model does three jobs in one set of weights: skill (grammar, reading,
composing an answer), knowledge (facts about the world), and memory (what it was told a minute
ago). Only the first needs dense computation. Knowledge is sparse and tolerates latency, and
memory is what a model is worst at keeping. This branch keeps the three apart:

- **Skill at language** is a small pretrained model, frozen, and it only translates. As the
  ears it turns a sentence into its dependency parse. It is called the faculty, and nothing
  here trains it. The mouth is the system's own (see DECIDED).
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
  filler in a slot of its own: Qwen3.5-2B at 0.96 recall and 1.00 precision. The ear was
  deleted 2026-10-02, when the parse graph replaced it.
- **The stream, first.** John's, 2026-10-03. The target is the TinyStories stream (THE
  STREAM, below); the houses and bAbI are regression checks, and their items further down
  wait behind these. In order:
  1. What broke first, traced on 300 stories (each fault's reading is in its commit):
     - Focus. Every story is one world to the system, so most wrong answers named
       something from another story. Focus (`73808c1e`) is recency times slot fit; how
       far it reaches is a fixed number of turns for now. The mechanism it stands in for
       is event segmentation: a boundary where what is heard stops being predicted by
       what was, so an episode is found rather than set.
       OPEN, John's to decide: the stream tells stories back to back on one clock, so
       nothing but their words separates them. Real conversations are separated by
       time, and the turn index is the system's clock, so the stream could advance it
       between stories, as a night passing. That adds time the real world has rather
       than removing difficulty, and it is still a change to the world made because the
       machine did badly, which the rules forbid unless he says otherwise. Without it,
       the boundary has to be found from the words, where TinyStories' shared
       vocabulary (every story has a Lily and a park) makes overlap a weak signal.
     - Identity. Every Lily in every story is one node. An episode-scoped name
       ('lily' in this story) with a link to the shared word is what memory of many
       stories needs.
     - Still expected and not yet traced: adjectives folded into names ('the red ball'
       is one name, so a colour is never a node), and the edges `SKIP` drops.
     - The plans learn one shape per wording, and a cloze is a wording of its own, so
       plans rarely answer here and the slot fit carries the learning.
     - A wrong kind is most of what is left ('thanked what?' answered 'guitar'). It
       is not mostly new words, and no one of focus's factors sinks the answer: it is
       in focus for most late misses, often second or third (`scripts/autopsy.py`,
       `e3b3647d`).
     - No stream question meets a shape a plan was learnt for, and half have a path
       from their names to the answer (`e3b3647d`). Plans are kept by wording, so
       they never reach the stream. Next, after the comprehension form: a question
       read as its own pattern ('Lily ate what?' is an 'eat' event with Lily as its
       subject and its object free), matched against the episode, as `solve` matches
       a plan, so the question needs no lesson in its wording.
  2. Kinds as the fixed point of relations and things (John's, 2026-10-03). Two things
     are of a kind where they take part in alike relations with things of alike kinds,
     and two relations are alike where they join alike kinds. Each starts as its own
     class and both are refined in turn until nothing moves, as colour refinement
     (Weisfeiler-Lehman), SimRank and co-clustering do. A shape's kind is the class its
     answers fall in, so shapes need no grouping of their own. Built on the stream, where
     a word has hundreds of contexts, rather than the house's three to five. A recursive
     structure is wanted for scale as well (John's). Refuted if it separates the house's
     true kinds no better than labels as a set did (`kinds` at `55ebec4d`), or the
     stream's curve does not rise with it. On the stream, the graded fixed point (walks
     through names sharing verb slots, `6aaf8dcf`) tied the verb's fit, and a slot's
     kind by its fillers' case, part of speech or pronoun agreement lost (`e470b64f`,
     `d7ab6e68`).
  3. Retention and knowledge across stories: early questions asked again late, and
     questions only many stories answer ('What colour can a ball be?').
- **The models are a milestone check, not a baseline.** John's, 2026-10-02: the system is
  compared with its own last version on every change, and with `blind` (seconds, no GPU);
  the language models given everything are run with `scripts/baselines.sh` now and then,
  to say what size of model the system stands against. So phases 1, 2 and 4 of the red
  set read the latest readings of the models whatever house they were taken on, and say
  which house that was; the system and blind are read on today's house.
- **The bet's checkpoint.** Two halves. The first house: the system beats the 0.8B given
  the whole transcript on `twohop`, `chain3` and `count`, and blind, on seeds 1 to 3. The
  system is the graphed arm, which asks no faculty anything, so it is held to the smallest
  faculty's full context. The second house at no worse than half the first house's score:
  met by the graphed arm on seeds 1 to 3.
- ~~**Plans over the parse graph.**~~ Struck 2026-10-02: the graphed arm
  (`src/unfused/graph.py`) keeps the conversation as its dependency parse and learns plans
  as paths in it, taught in conversation. It led the taught arm on both houses and on bAbI
  (0.890 against 0.873), so the taught arm, the 0.8B ear and the instruments measuring the
  ear were deleted; the ear's speed, its known relations and the second house's lendings
  went with it. What would bring the ear back: a world whose sentences the parse cannot
  read, where an ear's reading answers more.
- **The graphed arm, next.** John's, 2026-10-01 and 2026-10-02: what a conversation will
  need anyway comes before tuning to either world, and the system goes generic as soon as
  it can, then iterates there. Each enters the exam as a form when it does. In order:
  1. Wordings never heard, first. The readings at `f15479ba` chose it over the mouth: a
     fact told in a shape no lesson's fact was told in reads 0.571 0.629 0.514 against
     `direct`'s 0.880 0.893 0.905, so the mouth would voice answers to tellings the
     system cannot read. Three things are missing, one diagnosis apiece.
     - A telling of a new shape. `kinded` (`4a9efac4`) reads one where nothing else
       answers. What `novel` still misses is a wrong kind from a borrowed or loose plan
       ('Where would I find X's jars?' answered '15'), which is item 4, and 'shares a
       grandmother with B; they're cousins', where the relation is a clause apart.
     - A synonym taught inside one conversation. Aliases are never carried from the
       practice houses (John's, 2026-10-03): carried, they were memory of the synonym
       table every house shares, read back as about 0.85 on oblique against 0.494 0.558
       0.523 without. Only plans and positions reach a test house.
     - Question frames no lesson used, trades asked by their verb, and moves told with a
       verb no lesson used, as before: two wordings are one relation where their
       solutions agree, on what the house holds rather than a lesson's answer. Moves are
       why counts fail: 'X's jars are in the attic now' is not known to replace 'X keeps
       the jars in the cellar'.
     Lessons are the price of all three: the first house reads 0.539 0.550 0.551 after
     one practice house, 0.677 0.717 0.618 after two and 0.850 0.850 0.863 after five,
     and `direct` is 0.62 at one because a plan is learnt per wording.
     - Learning rate is the measure for all three (John's, 2026-10-03). Needing many
       tellings is expected, as it is for a child; what the lessons buy is the question.
       The reading: how many tellings inside one conversation it takes to pick up a new
       word or a new shape, without practice on the same wordings. It starts with John's
       idea of a word's meaning built from its context with the word itself left out:
       over the house's oblique questions, how often the context alone leaves exactly
       one candidate, and how that grows when the word appears in tellings as well as
       in the question. A synonym said only in its question is one context, which is
       why the graph version of this, solving with the word's slot free, was refuted.
       The reading is `scripts/context.py`: a word's candidates are intersected over the
       questions it is heard in; the graph does the same (`heard_in`). Kinds are not the
       lever for it: even the true kind leaves three or four candidates after one
       hearing (`readings/context-oracle-*`). Kinds by link labels stay item 4's, where
       a wrong kind is the fault.
       - MiniLM ranks what the intersection allows (`voted`, John's, 2026-10-03). Oblique
         reads what the language already knows and made-up words read what is learnt, so
         a gain on oblique is never read as the learner's.
       - Next: item 4's kinds, before items 2 to 6. Most wrong oblique answers are now
         of a wrong kind ('What shade are the lamps?' answered 'coach house').
       Once item 4's kinds have landed, the curve is
       compared with the language models' in-context learning (John's, 2026-10-03): the
       house's synonyms swapped for made-up words in an instrument copy, so a model's
       prior cannot say what they mean, and each model asked what the word names after
       each hearing, read as right and as wrong-but-confident beside the system's curve.
       Refuted if the 0.8B is above the system's curve at every hearing. Not before
       then, since a loss expected while the mechanism is unfinished decides nothing.
  2. A mouth of the system's own, after item 1 (conversation before counts, John's,
     2026-10-02). An answer is one node of the graph or "I don't know."; it cannot say
     yes or no, list, or say what it is sure of. The mouth renders the answer's path
     from fragments of parses it has heard: each edge of the path is said in the words
     of a heard sentence that has that edge, its nodes replaced, and fragments join
     where they share a node, as data-oriented parsing builds new sentences from pieces
     of old trees. A reply is therefore never limited to sentences heard whole, and
     inflection comes from the parse's morphology. The system answers yes-or-no
     questions by finding the fact asked, or the fact that rules it out; finding neither
     is "I don't know.", since never told is not told it is not so. The form: yes-or-no
     questions, half true, some about what was never told. Refuted if yes-or-no accuracy
     is no better than half, or if no reply joins two fragments when the path has two
     edges. The bare node cannot be the bar, since the contains-match scorer gives a
     sentence holding it the same credit.
  3. Counts, which hold phase 4 of the red set: count is below blind on every seed. A
     count plan counts one walk's ends, so one wording of the place. Counting a
     relation's solutions instead was tried and dropped (see the commit that says so),
     for two reasons, each of which would bring it back once met. A move told in another
     wording does not replace the keeping, since `replaced` wants the same arguments, so
     the room left still counts the person: what is still so must be decided by
     relation, the later solution for the same person and thing, which item 1
     enables.
     And most rooms hold one person, so many relations count a lesson's 1 rightly and
     the one chosen is chosen on too few lessons.
  4. Chains of three, and joins. A clause about something is a join now (`solve`,
     `related`, `joined`); seed 2's chain3 is below the 0.8B's. Colour asked through a
     clause ('the stuff X keeps in the dairy') is the largest miss left, 35 over seeds 1
     to 3, answered with a room by a loose walk where the colour was told in a wording no
     plan holds. Joining before walking loosely was refuted (see the commit that says
     so): the join's free variable took a thing where a person was asked. Both faults
     are a wrong kind, so the next arm is a kind check on a join's free variable and a
     loose end. Two shapes of kind are refuted, each in its commit: the link the answer
     hangs by at the end of the shape's plans (too narrow: a right answer told another
     way hangs by another link), and every verb and link the graph holds the answer by
     (too fine: 'Dov sells apples' no longer answers a trade taught on 'keeps bees', a
     guard). A kind has to generalise across verbs as the loose walk does, so what is
     left is likeness of names by overlap, as DECIDED's meaning section has it: a
     candidate is of the answers' kind where it shares more of what is known about it
     with them than with the question's other names, by link labels so a new verb still
     fits. A name of the question that is itself of the answers' kind is not evidence
     against: 'Who is X cousins with?' asks for a person about a person, and the
     comparison MUST NOT reject the right one for being like X. The case it is for:
     'the stuff X keeps in the nursery' answered 'coach house', which is like the
     nursery and unlike any colour (`f827f42e` tried kinds on borrowed ends, before
     plans were relations). A third shape is refuted (`a9dfa7c2`): the kind taken from
     the question's shape when the answer is filtered, which is not the plan that found
     it for counts, joins and borrowed shapes. A fourth is refuted (`kinds` in the
     commit that says so): the labels a shape's lessons' answers were held by, as a
     set, on the shape's own plans, which is where most wrong kinds come from. Colours
     are told 'painted in X' as well, so a colour shares `prep:in` with every room.
     Next: kinds as the fixed point of relations and things, on the stream (the
     stream's item 2); `scripts/kinds.py` reads the ceiling on the house with the true
     kinds as classes.
  5. Pronouns across sentences, and size. A pronoun is bound to the entity in focus as
     Phase 5 has it, and the first house is read at 3,000 and 30,000 turns, a size the
     house already has, for seconds a question as well as score (`tests/pushback.py`).
     Both are needed before the primer fork.
  6. The edges `SKIP` drops (determiners, auxiliaries, conjunctions, `advmod`,
     particles) are kept and plans learn which matter, as negation, modals and 'before'
     were pulled out of it one at a time. Last because no miss traced on the
     many-worded house came from a dropped function word. A function word is an edge's
     label or a node's mark, never a node, so 'the' does not become a hub. Refuted if
     the many-worded house reads no higher with every edge kept than with `SKIP`.
- **Phase 5 — Learning rules, and operations over facts.** Plans learnt from lessons
  answer bAbI tasks 1, 2, 3 and 7. Order in time, the present against history, and a
  visit before or after another are learnt per plan. Rules induced from co-occurring
  facts ('lent X to Y' then Y has X) are next, kept while their predictions hold.
- **Phase 6 — Importance and forgetting.**
  - **A body of its own.** John's, 2026-10-02. Emotion is a feedback loop: signals about
    the self that the self cares about, sensed as input and turned by what happens. Here
    that is interoception plus neuromodulation: the signals the system already makes
    (surprise, how often it said 'I don't know', corrections received, plans dropped)
    arrive as a sense beside the conversation, and some of them set how fast it learns
    and what it keeps, as dopamine and noradrenaline set plasticity and attention. It
    MUST be measured as a learner: refuted if a system steering its own learning rate by
    them learns no faster from corrections than one with a fixed rate.
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
  - **Reopened on a condition** (John's, 2026-10-03): learnt meaning gets a fair shot, and
    MiniLM's word vectors come in as a vote beside the intersection if it stalls. The
    trigger: the 0.8B's in-context curve above the system's at every hearing (THE ORDER,
    item 1), or the system's curve no higher at five hearings than at three once kinds
    have landed. `scripts/unheard.py` picked the right name 33 times in 40 on seed 1 with
    MiniLM fitted to the rest of the question. Met (John's, 2026-10-03) by a ceiling read
    sooner: the true kind still leaves three or four names after one hearing
    (`readings/context-oracle-*`). MiniLM is a vote at a word's first hearing; the
    meaning learnt from hearings decides once there are enough of them.
- **The TinyStories stream is the target; the houses and bAbI are regression checks.**
  John's, 2026-10-03, replacing the house as target. The house has about fifty words and
  three or four frames a kind, so it runs out of things to learn, and work fitted to it
  bent the system to its templates. bAbI is small and templated the same way. A score on
  either is never the objective.
- **A continual learner is judged by how well it keeps learning.** John's, 2026-10-03.
  Three readings along one stream, weightiest first: whether it still gets more right as it
  hears more, whether it keeps what it had (early material asked late), and what an answer
  costs in seconds and memory as it grows. How much it learns from nothing on a first
  encounter, a house at a time with nothing carried, is the second measure.
- **Nothing memorised in one world is read back as a score in another.** John's,
  2026-10-03. Aliases are never carried from practice houses, since every house draws on one
  synonym table, and no reading is called "as deployed".
- **bAbI's milestone is transfer** (John's, 2026-10-03). A system that learns as it goes is
  judged after teaching, as a model is judged after training rather than at its first
  weights. The milestone reading is bAbI after a general curriculum (the primer fork) with no
  bAbI lessons at all. Until a curriculum exists, the bAbI reading taught on its own 20
  stories stays as the regression check.

- **The 1080 Ti is the hardware.** No rented compute. Anything that needs more than one
  11 GB Pascal card does not get built.
- **No language model in the system.** John's, 2026-10-02, replacing 2026-09-29's faculty
  as ears and mouth. The ear is a dependency parser and the mouth is the system's own,
  built from the sentences it has heard. A language model that renders an answer from the
  path that found it can compose that path, which puts thinking back in the faculty. An LLM
  mouth is a last resort, taken only once the system's own mouth has been exhausted, and
  the language models stay as the milestone check. Small faculties are still preferred
  wherever one is used, because the bet is only shown by a faculty too small to do the
  thinking itself.
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
- `graph` — `GraphArm`, the system: the conversation kept as its dependency parse, plans
  learnt as paths in it from a teacher's reactions, and `turn`, which sorts a turn itself.
- `exam` — the house generator, the runner, and `converse`, which teaches in conversation.

---

## THE STREAM

TinyStories (Eldan and Li, 2023): short stories in a three- or four-year-old's vocabulary,
about two million of them, in free sentence structure. The validation split's 22,000 are
read first (`data/tinystories/`, fetched; `src/unfused/exam/stories.py`). One system hears
them in a fixed order and is never restarted between them.

Each story is told sentence by sentence. One later sentence naming something the story told
before, by name or as 'the X' or 'his X', is held back and asked with that phrase blanked,
as the Children's Book Test asks ('Finally, the butterfly landed on what?'). The teacher
reacts as a lesson's teacher does, then the held-back sentence and the rest are told.

- **The curve** is the share right by stories heard, in buckets 0-10, 10-30, 30-100 and on
  by about threefold, each with seconds a question and the graph's size.
- **Baselines.** `blind` says the commonest answer of the questions before it and reads no
  story; `frequent` says the noun the story has named most so far. The second is the bar a
  memory has to clear.
- **Comprehension questions**, beside the cloze (John's, 2026-10-03). A cloze asks about a
  sentence before it is told, so it measures prediction. A comprehension question asks
  about one already told, a few sentences on, as a parent checks that a child followed:
  'Where did Lily put the ball?'. Its answer is in the graph, so it measures what the
  system kept and can find. Each story keeps its cloze and adds these, read as their own
  curve. Not built yet.
- **Refutes the target's bet:** the graphed arm's curve flat from the start, so nothing it
  learns on one story helps on the next.

## THE HOUSE (regression)

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

- The parser's model.
- `k` hits per query and the number of recall hops.
- The store's fusion weights: `rrf_k`, importance weight, recency weight and half-life.
- The linked arm's shortlist size, spreading width and decay.
- RAM against speed (John's, 2026-10-03): how much of the graph is kept in memory. Every
  name an input touches is loaded before it is walked, so a walk runs in memory wherever
  the dial allows. The intelligence is the same at every setting; only the hardware it
  fits and what an answer costs change.

---

## OPEN FORKS

- **A primer, and Simple English Wikipedia.** John's, 2026-10-02. The system learns from
  examples, never from explanations: a lesson on what a noun is gives it nothing, while a
  curriculum of many short tellings with questions about them, in every common wording,
  teaches it question shapes in general, which is a primer that would help. Wikipedia read
  alone gives knowledge and no skill, since nothing in it is asked; it could give a
  word's meaning as what is known about it, which is the symbol encoder's raw material.
  Needs first: pronouns bound across sentences, and a graph that walks at a million edges.
  A first reading: a few hundred Simple English articles on one subject, read whole, then
  asked questions in shapes the house taught.
- **Spark-234K as a far world.** John's, 2026-10-02 (`OpenDataArena/Spark-234K` on Hugging
  Face): 234K research-level science problems from recent papers, each self-contained,
  each answer a worked derivation of 2.5K to 44K characters. Nothing here can attempt one:
  it needs arithmetic and algebra as step kinds, knowledge the conversation never told,
  and a mouth that composes. A first rung that would say something sooner: read a
  problem's givens as tellings and ask only for a named quantity it states, scored by
  whether the system finds the givens the derivation uses.
- **Where the encoder learns.** Two places, wanted both. Rows: aliases and confirmed
  sameness kept per symbol, which is memory and does not carry to a new word. Weights:
  the encoder's own function, which carries what it learnt about one unknown word to the
  next. Rows first, because they are what the house can measure.
- **Choice questions** ("Is the gate red or blue?"). A taught chain must touch every filler
  the question names, and "red" sits in no fact about the gate, so no plan is learnt.
- **A thread's working notes as a fragment**: what surfaced for the last few turns is carried
  forward, the way attention holds a topic.
- **Hyperdimensional vectors for symbols and assertions**, each symbol a random bipolar vector
  seeded by a hash of its name so every node agrees without coordinating. They buy a
  fixed-size memory that merges by addition and forgets by interference; they have to earn
  that against the exact tables in Phase 8, or not be built.
- **Episode boundaries the system sets itself.** John's, 2026-10-03. A boundary is a turn
  with no words in it, so once the system has input of its own it can mark one, and
  manage its memory by where episodes end. Event segmentation theory sets them where
  prediction fails, so Phase 6's surprise signal is the likely trigger.
- **Idle-time inference**: the system composes and writes derived assertions when no input is
  arriving, so a chain is found once rather than per question.
- **A retryable buffer of saves.** John's, 2026-10-03, for stability at scale. Writes
  gather in memory and go to disk in batches that are retried until they land, sized by
  the RAM-against-speed dial, so a crash costs at most one buffer.
