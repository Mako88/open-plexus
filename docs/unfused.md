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
- **The foundation, first.** John's, 2026-10-04: what is known to be wrong in principle
  is fixed before it becomes the blocker, one change at a time and each measured, since
  a change right in principle can still lose (`4cdbe641`). A drop on the way is
  expected, since crutches come out, and is recorded rather than chased until the
  whole foundation is in (John's, 2026-10-04); a drop that lasts past the last item is
  the finding. The red set (`test_foundations.py`) is the list. In order:
  1. ~~Individuals~~ (DECIDED, three layers). Struck 2026-10-04: `cecba76b` (identity,
     labels as before), `6e5b8391` (adjectives and compounds out of labels, said of the
     individual; the house's rooms stay apart). An individual is an index (John's): an
     id, its label and what was said of it are links pointing at it. The joining rule
     is in `in_mind`, and across episodes nothing joins until joining is learnt.
  2. ~~Nothing the parse gives dropped~~. Struck 2026-10-04 (`203e5829`). Plans learning
     which of the kept links matter is still to come. Refuted if neither the stream
     nor the many-worded house reads higher with everything kept than with `SKIP`.
  3. ~~Concepts~~. Struck 2026-10-04 (`51ad1e33`). Kinds by the fixed point of
     relations were deleted 2026-10-07 (John's): they entered only focus's slot fit,
     and they cost a fifth of a run's CPU for nothing. Kinds come back from item 7e's
     vectors, or rebuilt where a reading shows a wrong kind is the fault.
  4. No English by hand (the red set's last test: 41 words and 4 readings of capitals).
     The parser marks some of what is needed in any language: pronouns' `Gender`,
     `Number`, `Person` and `Case`, proper names' part of speech. It marks nothing for
     wh-words or refusals: `en_core_web_trf` gives 'who' and 'where' an empty
     morphology and 'no' no `Polarity`, only the English tags. Either those are learnt
     from teaching, or the parser becomes one trained on Universal Dependencies
     treebanks (Stanza marks `PronType=Int` and `Polarity=Neg`). The parser is a dial;
     which way is John's to say. Number words have no feature in either and are learnt.
- **After the foundation.** John's, 2026-10-04, in order:
  1. ~~Every missed check read and sorted by cause~~. Struck 2026-10-04 (`a69e59ad`,
     `384c1d53`): of 14 at 300 stories, 7 the system's (fixed but two), 5 ambiguous,
     2 malformed.
  2. Size: 3,000 stories parsed ahead (`scripts/preparse.py`), one run profiled, and its
     hotspots fixed in the order the profile ranks them, until a 3,000-story reading
     takes about ten minutes. Answering is meant to cost what is in mind plus a bounded
     number of index lookups, as recall by cue does, never a scan of a label's history.
     Hearing and teaching are done (`bc5a61ca`: 34% less CPU at 1,000 stories), and
     answering's hotspots (2026-10-04: 870 s at 1,000 stories to about 220). 3,000
     stories are parsed. A 3,000-story reading takes 48 minutes: seconds a question
     still grow with history (fivefold from 300-1,000 to 1,000-3,000), through hubs
     and the taught shapes several ways of answering still scan. What is left rides
     on item 5's walk, whose decay bounds what a question touches (John's suggestion
     taken, 2026-10-04); re-read the 3,000 reading once it lands. Held (John's,
     2026-10-05): a change is read at 1,000 stories (about six minutes, 618 clozes
     focus decides, flat past 100 stories), so 3,000 waits until a question needs
     what only it shows. Profiled 2026-10-06 (`readings/profile-stories-*`), the
     order to take them: plans (`follow`, `paths`, `replaced`, in answering and in
     teaching alike), then focus, then recall by cue, then kinds. 10,000 stories are
     parsed ahead. Taken 2026-10-06: `replaced` a lookup (`b005e196`) and kinds'
     merge (`79dd3ab8`), 1,000 stories 428 to 325 cpu s, every answer the same. Next
     (John's, 2026-10-06: fast iteration first): `paths`, then teaching; the
     refuted factors stripped from `scripts/weights.py`; independent arms run in
     parallel processes. Profile at 100-300 stories (proportions are all a profile
     needs), check every answer the same once at 1,000, and read growth from the
     1,000 reading's bands; 3,000 only where those bands cannot show the change
     (John's, 2026-10-07). Done by agents in their own worktrees (John's, 2026-10-06), one PR a
     fix, each held to every answer the same, while the core work goes on: `paths`,
     the gitignored caches found by a setting rather than linked into worktrees,
     teaching; then parallelism and a fast weight sweep; then measurement that does
     not slow the run (John's: the arm traces the decisions it makes anyway,
     instruments sample questions, a sampling profiler from outside); then rounds of
     review (the first struck 2026-10-08, #34 to #40: duplication into shared
     helpers, vacuous tests, dead code from refuted arms, names); and once a piece
     stops changing, its hot kernel moved to native code (the graph store, the
     walks).
  3. ~~A check that more than one telling answers accepts every answer they support~~.
     Struck 2026-10-04 (`_answers` in `exam/stories.py`).
  4. Harder checks, one at a time and each measured alone (John's, 2026-10-07:
     the main curve and slope; see DECIDED, the cloze is knowledge). Done: further
     from their sentence (`far`, not harder: an episode is held whole) and early
     stories asked late (`late`, after a parent's reminder, 0.484 against near 0.965;
     0.686 with no reminder, since a retold sentence opens new individuals). Late's
     reminder as a person gives one is in (`cued`, John's). Two told facts put
     together is in (`joined`: the doer named by another thing told of them), 0.899
     with a relative clause read as a question about its noun and answered by the
     walk (`resolved`; 0.523 before). What changed is in (`changed`: where a thing is
     now, after the story moved it), 1 of 6 at 1,000 stories: rare, since the stories
     move things by pronoun ('She put it in the box') and the exam reads no
     coreference. It stays as it is: where a thing is now is read on standard tests
     instead (John's, 2026-10-08), bAbI tasks 2 and 3 and ProPara, so no question
     is written by a coreference model. Then why something
     happened. The information is in the story, so these read understanding apart
     from knowledge; scale should not be what they need.
     A standard test a rung above the stream, beside our own checks (John's,
     2026-10-07): FairytaleQA (Xu et al. 2022; `scripts/fairytale.py`), teachers'
     questions on Gutenberg fairy tales, labelled by what they ask (action, setting,
     causal, feeling, prediction) and explicit or implicit, scored by ROUGE-L as
     published. The stream stays what the system learns from; the tales test whether
     what it learnt carries to prose it was not built around. Read against MCTest's
     sliding window and the 2B and 9B handed the tale (the 9B owed, John's: later).
     What it points at first (`2f4b8f4c` and the commit after it): answers that are
     an event or a property ('What did the fisherman do?'), then binding pronouns,
     which the world-side control prices at a quarter more right answers. NEXT
     (John's, 2026-10-08): the mouth (the graphed arm, item 2), read on FairytaleQA
     and on the stream's checks. In: the answer said as its phrase where it was
     heard (`mouth.py`), and a question whose frame's lessons wanted an event
     answered by one, said as its predicate (`recounter.py`, taught on FairytaleQA's
     train split). Next, by size on the tales: 'why', a reason rather than an event
     (278 of 1,007, near 0.05), whose wh-word `blank` does not read as a frame since
     it is no pronoun; then properties and feelings ('How did the king feel?').
     Binding pronouns is the thinking's job, never the ear's (John's, 2026-10-08): no
     coreference model in the system. NEXT: PreCo (Chen et al. 2018, coreference in
     preschool English) wired in as a standard test of the system's own binding,
     each pronoun's referent scored against the gold chains, with the world-side
     control (a quarter more right answers on the tales) as the ceiling to aim at.
     Wired 2026-10-08 (`scripts/preco.py`, 100 dev documents). The control that prices
     it: agreement known per label (seeded from the gold chains) is the lever, worth
     about twelve points once the binder looks in every slot and prefers a candidate
     known to agree; learnt as it is, both those rule changes lose. NEXT: where
     agreement comes from. Clause-bound possessives and reflexives teach it (Bergsma and
     Lin 2006), and 1,000 stories of them barely carry to PreCo's labels; owed, the
     primed run under the changed rule, then agreement that generalises past the label
     (the noun's own `Number` from the parse first, then what fills the verb's slot).
     ProPara is wired (`scripts/propara.py`); only `window` is read so far.
     Beside them, knowledge asked fairly (John's, 2026-10-07): the cloze banded by how
     many earlier stories told its answer in its slot (`scripts/heard.py`), and a form
     asking a fact seen across five or more stories in a wording no story used ('What
     does someone pet?'), every answer seen that often right.
  5. The walk (John's): every node a question names fires at once, and what
     their activations meet at is the answer, as spreading activation does
     (Quillian; ACT-R), each node passing on activation divided by its fan so a hub
     passes almost none. `match` is retired for its meet (`met`, `7bc77a2e`). Left:
     the walk retiring joins and borrowing, each against a control. Not focus (John's,
     2026-10-05): a cloze's sentence is not yet told, so answering it is prediction,
     and the walk and plans recall what was told. The walk's activation alone ranked
     focus's names at 0.23 (`f16c57f8`) and the walk as the whole stack read 0.244.
     The cloze's lever is a better predictor. Refuted so far: weighting focus's
     factors (`e7d53f7c`), narrative schemas, fit by the verb's own slot (Resnik),
     analogy, Erk's preference by MiniLM. A reader gets 0.925 where the system gets
     0.517 (`readings/reader-cloze-*`), so the cloze has room. From its misses, in
     order, each measured alone:
     a. ~~The question's own thing given back~~. Struck 2026-10-06: dropped by the
        individual its name finds (`readings/stories-graphed-*-20261006T014853Z`).
     b. The kind the question asks for (John's: these are one fault, not knowing
        categories). 'who' wants a person and 'at what' a place whatever the verb,
        which is why fit by the verb's slot lost. Learn, from each lesson's answer,
        which kind each question frame (wh-word with its preposition) brings, by the
        kinds already learnt, as counts; rank by it in place of the wh-word's mark.
        Built and refuted 2026-10-06 (the commit that deletes it): the frame adds
        nothing over the wh-word, and the kinds cost twelve points, because at 1,000
        stories they hold every person in one kind and most common nouns alone. WordNet's
        true categories in their place lose as well (0.433, `021256Z`): the stream asks
        'who' of a proper name and 'what' of anything else, so the mark reads the exam's
        own rule. John's (2026-10-06): the cloze now asks 'who' of a person however
        written (`stories-7`, baseline 0.486). With that, WordNet's categories read
        0.494 (`040416Z`), 77 fixed and 69 lost: perfect kinds are worth about a point
        here, so the cloze's room is not in the kind a question asks for. Kinds that
        group things are still owed for the loop (OPEN FORKS, meta layers), judged
        elsewhere than this factor.
     c. What the story is about (its setting and thread: the circus, the camp trip),
        a layer over the episode's individuals, as a reader holds a story's gist
        (John's episode layer, item 6). After a and b say how much is left. Its
        control refuted it as salience (`462288d7`), and vectors as the slot fit are
        refuted on GloVe and our counts too (`195f20d6`): the room is world
        knowledge. More stories do not bring it as the system is: the cloze reads
        0.496 at 300-1,000 and 0.483 at 1,000-3,000 (`061744Z`). What hearing more
        should improve is the predictor itself: item 7.
     Also refuted 2026-10-06: fit by the whole event (verb, slot and co-arguments)
     and John's veto, dropping what cannot fit (`41183049`: a point at most, and it
     dropped the right answer in one cloze in eight, since counts this sparse say
     only what was never heard). Owed: the veto with WordNet's categories beside
     ours, and each focus decision traced by stage (the funnel), stopped for item 7's
     control; its patch was not kept.
  6. An episode recalled by its gist (John's, 2026-10-05; hippocampal indexing, Teyler
     and DiScenna). A boundary changes what is in mind and erases nothing; `remind`
     runs it backwards, and the oracle arms (`recalled`, `asked`) say what is left is
     which episode the cue picks, never when or how it is used. Left, as later
     improvements (John's, 2026-10-05: the cloze comes first): the cue's misses (late
     0.855 against the oracle's 0.969), first by reading them; recalling the likeliest
     few where the cue is ambiguous (worth at most about three points); and whether
     episodes recalled together become a category (OPEN FORKS, meta layers, John's
     'dreaming').
  7. A slow memory that generalises (John's, 2026-10-06; complementary learning
     systems, McClelland, McNaughton and O'Reilly). The graph is the fast memory, one
     hearing and every episode kept apart, which is why the checks read 0.97. What is
     missing is the slow one, whose representations overlap so what is learnt of one
     thing reaches the things like it, fed by replay. The control (`e4c2351d`): a
     small transformer from nothing reads the cloze at 0.271 from the same 500
     stories where the system reads 0.501, and at 0.655 from 21,000, where the system
     is flat from 300 stories to 3,000. In order, each judged by whether the cloze
     rises with stories heard (1,000-3,000 against 300-1,000):
     a. Situations (`c1988b38`, John's): each word's meaning is the contexts it was
        heard in, folded over the stream at several rates and at the story's end, as
        event sourcing projects a stream; vectors of the system's own, fixed by name
        and spelling, bound to their links, read centred. At 3,000 (`situations-
        buckets-*`) they are the first part whose worth grows with stories (the slow
        folds alone 0.094, 0.151, 0.191 by bucket) but beside recency, fit and mark
        they add nothing yet (0.482 without, at most 0.486 with): they still mostly
        know what fit knows, and alone stay under it (0.23 against 0.37). Kept as the
        substrate; nothing reads them. If one day they earn, the situational match
        becomes focus's main score, a joint fit of everything at once, rather than
        one more factor.
     b. Scales set by their worth (John's): folds added, removed and moved at a
        story's end by what each adds to prediction.
     c. Consolidation at each story's end, predict then learn from the error (John's,
        2026-10-04; predictive processing, Rao and Ballard): replay the episode,
        predict each telling's arguments from the meanings, and correct them where
        the prediction missed, since meanings shaped by prediction beat counted ones
        (Baroni et al. 2014); a local rule, no gradient. Its earlier form, learning
        focus's factor weights from the guesses, was refuted (the commit that deletes
        them). The error is also Phase 6's surprise and where event segmentation
        puts a boundary. Built over the situations as Rescorla and Wagner's rule
        (`2a9b18a5`) and refuted at 3,000 (the commit that deletes it): it learns the
        folds sooner (the slowest 0.170 at 300-1,000 against counting's 0.151) but no
        further (0.193 against 0.191 at 1,000-3,000), and all scales together end
        lower. Prediction as the teacher stays the idea; this rule over these vectors
        only front-loads what counting reaches anyway.
     d. Schemas with roles, minted where a pattern recurs and pays (OPEN FORKS, meta
        layers): the units the next layer builds on.
     e. ~~How much a vector holds~~ (John's, 2026-10-07). Struck 2026-10-07
        (`scripts/capacity.py`): bound vectors hold a word's experience exactly up to
        tens of facts at 1,024 wide, summed never, learnt only a hub's; and no likeness
        of words, the exact counts' own included, predicts a pairing not yet heard
        better than the role's commonest fillers. The exact store stays the store.
     f. ~~A predictor from the situation~~. This shape lost 2026-10-07 (`c479e826`,
        deleted in the commit after it): learnt role-gated vectors over the event and
        the story, replayed, read the cloze at 0.310 alone and 0.387 beside focus,
        against 0.486, and do not grow from 300 stories to 1,000. Not the idea: it never
        read the wh-word's mark, and was trained apart from focus rather than as its
        score. Next for knowledge is d, schemas, judged by slope.
     Refuted if, with all of it, the cloze still reads no higher at 1,000-3,000 than
     at 300-1,000.
  Then John's two nearest goals (2026-10-04): a conversation with the system (the
  mouth, the graphed arm's item 2), and worlds it acts in, its output heard back as
  input. The first world is TextWorld (Microsoft's text adventures), and saying
  commands is its prerequisite, which a conversation brings. Beside both, idle
  inference that feeds itself (OPEN FORKS), its conclusions marked as derived and kept
  only while later tellings bear them out, so a loop that hears itself does not harden
  its own mistakes. A conversation comes before reading Wikipedia (John's,
  2026-10-06), so what it read can be asked. Where nothing is asked, what to say is
  chosen as a contribution: relevant to what is in mind (the situations) and not yet
  shared (common ground, Clark: the graph knows who was told what), in the move that
  fits the turn before (an answer to a question, a reaction to news), learnt from the
  dialogue the stories hold as schemas, and taught by the other's reactions. It
  starts one from what consolidation turns up (a failed prediction, a new schema, a
  contradiction, a gap), the likeliest to teach it first (curiosity as expected
  information gain, Oudeyer; Gottlieb). Measured first by yes-or-no questions and the
  mouth's round trip, then by exchanges with John scored as agreed with him.
- **The stream.** John's, 2026-10-03. The target is the TinyStories stream (THE
  STREAM, below); the houses and bAbI are regression checks, and their items further down
  wait behind these. In order:
  1. What broke first, traced on 300 stories (each fault's reading is in its commit):
     - Focus. Every story is one world to the system, so most wrong answers named
       something from another story. Focus (`73808c1e`) is recency times slot fit; how
       far it reaches is a fixed number of turns for now. The mechanism it stands in for
       is event segmentation: a boundary where what is heard stops being predicted by
       what was, so an episode is found rather than set.
       Decided (DECIDED, episode boundaries): the break before each story stays.
     - Identity: every Lily in every story is one node. Moved to the foundation's item
       1; adjectives taken out of names merged the house's rooms into 'room'
       (`4cdbe641`), which is why it comes first.
     - Adjectives folded into names and the edges `SKIP` drops: the foundation's item 2.
     - The plans learn one shape per wording, and a cloze is a wording of its own, so
       plans rarely answer here and the slot fit carries the learning.
     - A wrong kind is most of what is left ('thanked what?' answered 'guitar'). It
       is not mostly new words, and no one of focus's factors sinks the answer: it is
       in focus for most late misses, often second or third (`scripts/autopsy.py`,
       `e3b3647d`).
     - No stream question meets a shape a plan was learnt for, and half have a path
       from their names to the answer (`e3b3647d`). Plans are kept by wording, so
       they never reach the stream. A question is now also read as its own pattern
       (`matched`), but only on the told verb and links exactly, so a cloze, whose
       sentence was never told, is mostly left to focus. Loosening the match by
       which verbs and links are alike is item 2's.
  2. Kinds as the fixed point of relations and things (John's, 2026-10-03), built as the
     foundation's item 3. Two things
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
     `d7ab6e68`), and latent classes over (name, slot) tied it too (`bb732491`). Built
     on individuals and a graph that drops nothing (`51ad1e33`), it ties in the slot fit
     as well, with kinds that read right.
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
     sentence holding it the same credit. How a thought becomes words (John's,
     2026-10-06): a thought is a piece of graph, an event with its arguments by link,
     which is a dependency tree without its order, so saying it is surface
     realisation (the SR'18 and SR'19 shared tasks): ordered by counts of which side
     of its head each link falls, learnt from every parse heard, and inflected from
     the parse's morphology. A sentence is said only if the system's own ear parses
     it back to the graph it meant, so the ear checks the mouth, and the share that
     comes back whole is the mouth's measure. If that is not fluent enough, a small
     realiser (a word-level model trained here on heard text, as `scripts/small_lm.py`
     trains one) may choose only order and function words with the content locked,
     checked the same way. That relaxes DECIDED's no language model in the system, so
     it is John's to open.
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
  6. The edges `SKIP` drops: moved to the foundation's item 2, which gives function
     words a kind of node of their own that walks skip, so 'the' is kept and is still
     never a hub a walk passes through.
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
- **Vectors hold a moment; the graph holds a lifetime.** John's, 2026-10-07, from item 7e's
  reading. A vector holds tens of facts exactly and blurs past that, so the graph stays the
  store: every telling, the individuals, the counts, one hearing and exact. Vectors carry
  what they are good at: a word's likeness (its position, learnt slowly from its record by
  replay against prediction's error), and the situation of the moment (a few things bound
  together, built fresh), which is the cue a predictor reads. The predictor's guess is
  cleaned up against the graph, which names the word or individual. The system's own
  vectors, learnt, so the meaning bullet above holds.
- **The TinyStories stream is the target; the houses and bAbI are regression checks.**
  John's, 2026-10-03, replacing the house as target. The house has about fifty words and
  three or four frames a kind, so it runs out of things to learn, and work fitted to it
  bent the system to its templates. bAbI is small and templated the same way. A score on
  either is never the objective.
- **On the stream, comprehension is the objective and the cloze is prediction.** John's,
  2026-10-04. The checks ask about what was told, which is what the bet says the system
  does exactly, so they are the curve the target is read by; a symbolic memory that holds
  a fact should answer it every time, so each miss has a cause to find. The cloze stays
  as a second curve, read as prediction (what a story brings back), never removed. The
  checks are made harder (THE ORDER) so they measure past 0.94.
  - **The cloze is knowledge, not understanding** (John's, 2026-10-07). What fills a
    held-back sentence comes from experience across many stories (how often a butterfly
    lands on a flower), not from the story asked about, so it measures how much has been
    heard. It drops in priority: the harder checks are the main curve, and a mechanism
    for knowledge earns its place by the cloze's slope (rising from 300-1,000 to
    1,000-3,000), never by its level at one size. Nothing is scaled up until something's
    slope rises.
- **A continual learner is judged by how well it keeps learning.** John's, 2026-10-03.
  Three readings along one stream, weightiest first: whether it still gets more right as it
  hears more, whether it keeps what it had (early material asked late), and what an answer
  costs in seconds and memory as it grows. How much it learns from nothing on a first
  encounter, a house at a time with nothing carried, is the second measure.
- **Nothing memorised in one world is read back as a score in another.** John's,
  2026-10-03. Aliases are never carried from practice houses, since every house draws on one
  synonym table, and no reading is called "as deployed".
- **Stories bring their own episode boundaries** (John's, 2026-10-03). A story's end is
  the world's, as a chapter break or a conversation ending is, so the stream's break
  before each story stays and no clock is advanced between them. Boundaries the system
  sets itself are still wanted (OPEN FORKS); when they are built, a control with the
  breaks removed says how much the system leaned on the given ones.
- **bAbI's milestone is transfer** (John's, 2026-10-03). A system that learns as it goes is
  judged after teaching, as a model is judged after training rather than at its first
  weights. The milestone reading is bAbI after a general curriculum (the primer fork) with no
  bAbI lessons at all. Until a curriculum exists, the bAbI reading taught on its own 20
  stories stays as the regression check.

- **Three layers: labels, concepts, individuals.** John's, 2026-10-04. A label is a
  word, and belongs to a language. A concept is what labels point at, learnt from how
  they connect (kinds), so 'red' is a colour by how it attaches to things, and naming the
  concept later is one more label. An individual is a thing: an identity of its own (a
  GUID, John's; derived from where it was first heard, so a run reproduces it) with
  labels and concepts attached and free to change. A ball painted blue is the same ball;
  two red balls are two. A description finds an individual and is not it. Individuals
  are long-lived (a person met once is the same person next week), and what is in mind
  now is what is active, which focus already is. Nothing of one language is held by
  hand: what a word does is read from what the parser marks in any language, or learnt.
  The red set holds each part (`tests/outstanding/test_foundations.py`).
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
  curve. It is asked in a parent's words, the verb under 'did' ('Where did Roxy put the
  leaves?'), so it is never the told sentence with a hole in it.
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

As designed. Where a phase gives the faculty a job (ear, planner, judge), DECIDED's "no
language model in the system" overrides it: the parse reads, and the graph plans.

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

Pronouns are bound by the system: the parse gives 'she' or 'there' as heard, and the system
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
what falls below the recall floor, never a deletion. Recency and use are one mechanism,
not two (John's, 2026-10-04): ACT-R's base-level activation, the sum over every use of
the time since it to the power of minus the decay, which focus already reads. A name's
steps are then returned by activation rather than by recency alone, once answers credit
the events they rested on.

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
  fits and what an answer costs change. Its first setting (John's, 2026-10-05): what
  is in mind (the episode and those recalled) held as structures in memory, loaded at
  a boundary or a recall, and read without SQL. SQLite's page cache already keeps hot
  pages in RAM, so what it would save is the per-query cost of thousands of small
  lookups, not disk reads; a profile says whether that is worth it. Down the road.

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
  asked questions in shapes the house taught. Then all of Simple English Wikipedia, and
  English Wikipedia after a conversation exists (John's, 2026-10-06): about 200 million
  sentences, so a week needs hearing at about 330 a second against about 100 today.
  What buys it: a faster parser, what is in mind held in RAM (DIALS), and shards read
  by processes in parallel and merged, the graph by union and the situations by
  addition, which no order changes.
- **Other languages.** John's, 2026-10-06. The situations hang on labels, so a new
  language's words start unrelated to the known ones, and meet only through the
  situations both are heard in, as a bilingual child's do. A shortcut: a word given as
  a direct translation starts from the known word's meaning and then moves with its
  own hearings, since one-to-one translations are rarer than they look ('know' is
  savoir and connaître).
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
  that against the exact tables in Phase 8, or not be built. Taken for meaning as the
  situations (THE ORDER, item 7a), beside the exact tables, not in their place.
- **Episode boundaries the system sets itself.** John's, 2026-10-03. A boundary is a turn
  with no words in it, so once the system has input of its own it can mark one, and
  manage its memory by where episodes end. Event segmentation theory sets them where
  prediction fails, so Phase 6's surprise signal is the likely trigger.
- **Idle-time inference**: the system composes and writes derived assertions when no input is
  arriving, so a chain is found once rather than per question. What it thinks about is
  chosen by a signal of its own (John's, 2026-10-06): what it expects to learn most
  from, read off consolidation's errors (Phase 6's body); it is what a conversation's
  first move comes from.
- **Meta layers, minted rather than built.** John's, 2026-10-04. A pattern that recurs and
  pays (a schema, 'lose, search, find') becomes a node of its own, and patterns are learnt
  over those nodes in turn, with no fixed number of layers: chunking (Soar; ACT-R's
  production compilation). `commitments`' rung five was the same idea (`docs/history/`);
  its lesson holds, that the vocabulary grows under the learner, never a controller
  tuning it from above. Schemas are the first thing to mint (THE ORDER, item 7d).
  John's, 2026-10-05: the goal is one loop that runs at every layer, which is learning
  to learn. Item 5b was built as its second turn (a kind is a class of things by their
  contexts, and the kind a frame asks for a class of frames by contexts made of kinds,
  the same refinement over its own output) and refuted as built (`73e84186`); the next
  turn is item 7's, over the situations rather than one kind per word. A node is
  minted where it lowers prediction's error (item 7c), so the loop has a reason to
  stop. Refuted if the same code run a turn higher pays nothing the turn below did not.
  Agreed the same night: categories reshape as evidence comes, splitting as a toddler's
  'doggie' does and merging where two predict alike, so a category is a view read from
  counts and never a stored thing, and reshaping it loses nothing (Anderson's rational
  model; Kemp and Tenenbaum's structural form). Layers send guesses down as well as up,
  so a higher one can regroup a lower. First test with a known answer: a word heard for
  two kinds of thing, which the loop has to split on its own. Higher turns need more
  text: TinyStories at 10,000 and 100,000 stories first, then the Children's Book Test
  (Hill et al., a cloze on real children's books) and the BabyLM corpus.
- **Pieces behind interfaces, and versioned representations.** John's, 2026-10-04. The
  parser, the store and the encoder each swappable alone, and every derived row stamped
  with the version that made it, read through a converter that does nothing for its own
  version (event sourcing's upcaster). What was heard stays the source of truth, so a
  representation that cannot be converted exactly (one parser's relations to another's)
  is read again from it; a vector space can be, by a map fitted on texts both versions
  encoded (orthogonal Procrustes), trusted once its error on held-out texts is read.
  ~~First: `graph.py` split by single responsibility~~. Struck 2026-10-08 (#19 to
  #33): fifteen parts `GraphArm` holds by composition, every answer the same.
- **Concurrent walks, and sessions.** John's, 2026-10-04. Every walk has an id and
  runs on its own: a question's answers now, and a prediction's (THE ORDER, item 7)
  runs beside it, as a brain predicts while it perceives, never in the answer's path.
  An answer reads the learnt state as of a version (MVCC), and an exam's stream waits
  at a checkpoint (a story's end) until that story's predictions land, so a reading
  reproduces; a live session does not wait. Each session holds its own working memory
  (what is in mind, its episode) and shares the long-term store, so every
  conversation teaches the whole. What follows for learning: concurrent updates must
  merge without coordination, so what is learnt is kept as counts (DECIDED, counters
  only rise) and weights are read from them. After item 7 shows predicting pays.
- **A retryable buffer of saves.** John's, 2026-10-03, for stability at scale. Writes
  gather in memory and go to disk in batches that are retried until they land, sized by
  the RAM-against-speed dial, so a crash costs at most one buffer.
