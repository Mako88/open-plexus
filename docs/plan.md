# Open Plexus

## The goal

Frontier-level on one GTX 1080 Ti. Frontier models are controlled by whoever can afford the
training run; a model of that worth on an old consumer card changes who gets to have one.

A frontier model does two things with one huge set of weights: it holds what it knows, and it
reasons by one chain of tokens with no backtracking, a huge evaluator doing a weak search.
The design here inverts both. Knowledge lives in a store on disk and is learnt by appending.
Reasoning is the trunk's own thought, trained by the system's own outcomes (since 2026-10-30;
before that a small trained evaluator and search in ordinary code, which read as a harness
around the trunk), with search in code and tools where they pay, the way AlphaGo beat
players no network without search could touch, and the way a small model with search over
its steps matched one fourteen times larger on maths (Snell et al., 2024; on the easy and
middling problems, not the hard ones, which is the ceiling to beat). Arithmetic and
counting go to a tool, as they do at the frontier. The frontier claim rests on both
inversions holding, and the plan reads each on its own.

## The bet

A frontier model holds three things in one set of weights: skill at language, knowledge of
the world, and memory of what it was told a minute ago. Only the first needs dense
computation. The other two are stored in the weights at about two bits per parameter, and only
after a fact has been seen on the order of a thousand times in varied forms (Allen-Zhu and Li,
*Physics of Language Models 3.3*). That is why the models are huge and why they cannot learn a
fact without a training run. Keep the three apart:

- **The trunk.** A small pretrained language model, taken off the shelf. It reads and
  writes English and does what thinking a small model can. Frozen through step 3's first
  reading; from then a learner too (decided with John 2026-10-30): an adapter on it trained
  from the search's own wins, since every head over a frozen state can only re-weight what the
  trunk already represents, and a system that improves its own judgement needs a trunk whose
  representation can sharpen.
- **The store.** A table on disk. Each row is something the system read or was told. Learning
  is appending a row. It is never retrained and never forgets.
- **The glue.** The reasoning core, and the part we design. Small heads over the trunk's
  hidden state, trained by gradient descent on the card and kept in training: a walker that
  says which rows to read next, and an evaluator that says whether a candidate step is good.
  The glue and the trunk's adapter are what we train.
- **The search.** Ordinary code. It keeps candidate steps, scores them with the evaluator,
  follows the best and backs up from the ones that fail. Its budget is counted in forward
  passes of the trunk, which is the unit chain of thought is also priced in.

The trunk is the language organ, the glue is the judgement and the search is the thinking,
so the system is as clever as the glue's judgement multiplied by the search, not as clever
as the trunk's one pass. What learns for life is the store's contents, the glue, and the
trunk's adapter. The bet
is in two halves. Knowledge: a small trunk reading a store approaches the same trunk with
the relevant text placed in front of it (the ceiling) on questions whose answer needs two
rows put together. Reasoning: a trained evaluator with search beats the same trunk's chain
of thought at the same number of forward passes, on problems the trunk cannot do in one
pass. The part that cannot be outsourced to code is the evaluator, and whether a small one
can be trained well from labels we can get is the question the whole design turns on.

The standard, set 2026-10-29: frontier-level answers from this card, by any mechanism that
runs on it. Whether the trunk "really reasons" or code does the composing is not the question;
a decomposition we write, a tool, a search shape we chose for the problem are all fair. The one
thing that is not fair is a crutch the deployed system will not have: labels, supporting facts
or an oracle's choice available in the reading but not in the world. Each reading says which
of its inputs the deployed system would also have.

This shape has been tried twice before. Memory networks (Sukhbaatar et al., 2015) solved
most of bAbI with a tiny gradient-trained controller doing three hops over sentence rows,
and died because their language side could not scale; a pretrained trunk is that side
bought off the shelf. unfused (this repo's predecessor, 2026) walked a parse graph with a
core that learnt by rule induction and no gradient, over vectors that carried no relation
meaning, and lost to a blind rule. The two things different here are the two things its
own record blames: the core learns by gradient, and the reader is a trunk's contextual
hidden state, in which "Mary picked up the milk" is already bound.

Why it is plausible, with numbers from elsewhere:

- Decoding on one GPU is bound by memory bandwidth, not compute: every token reads every
  weight. Knowledge on disk costs nothing per token until it is looked up.
- RETRO (DeepMind, 2022) at 7B matched models 25x its size on language modelling with a text
  store. kNN-LM (Khandelwal et al., 2020) improved a trained model by interpolating with a
  nearest-neighbour store over its own hidden states, with no training.
- Memory layers at scale (Meta, 2024): a sparse key-value table beside a small trunk, each
  token touching a handful of rows, matched dense models at two to four times the compute.
- Titans (Google, 2025): a memory module that updates during the forward pass by surprise.
  The nearest existing thing to learning at test time without retraining the base.
- Mixture of experts and activation sparsity (DeepSeek-V3, PowerInfer): per-token compute
  touches a few percent of the weights. That is how the trunk gets cheap later; it is not how
  learning gets cheap, which is the store's job.
- Next-Concept Modeling (Stanford, 2026-10): text as a hierarchy of discrete codes, coarse
  levels first. Not useful here for generation speed on this card; possibly useful later as a
  cheap discrete key for store rows.

What this is not: a context-reduction scheme, where the trunk is handed the right paragraph
and the ceiling is the trunk given the whole transcript. That is worth having and is the floor
of what we are after, not the aim. The aim is composing over rows the trunk was never trained
on.

## The card

| GTX 1080 Ti | |
|---|---|
| VRAM | 11 GB |
| Memory bandwidth | 484 GB/s |
| fp32 | about 11 TFLOPS |
| fp16 | 1/64 rate, do not compute in it |
| int8 (dp4a) | usable |
| Tensor cores | none |
| Full training fits | about 500M parameters |
| Adapter training on a frozen 2B trunk | fits, in fp32 |

Inference of a 4-bit 13B fits. Training the trunk does not, and does not need to.

## The measure

bAbI (Weston et al., 2015), `data/babi/*.jsonl`, 20 tasks of 1,000 test questions each, every
row carrying the story up to its question. Tasks 1, 2 and 3 are one, two and three supporting
facts and are the spine of every reading; the rest widen it later. Stories are synthetic with
a closed vocabulary, so a pretrained trunk cannot know the answers, which is what makes them a
clean test of reading a store. Score is exact match on the answer word.

Every reading has three lines: the floor (trunk given no story), the ceiling (trunk given the
whole story in context), and the arm. An arm is read against both. Every reading also carries
its cost: tokens per second, VRAM, store bytes, lookup milliseconds.

Later measures, once tasks 1 to 3 are at the ceiling through the store: the other bAbI tasks;
fresh synthetic stories with invented names, longer than any context; and then real knowledge
at scale (a Wikipedia subset in the store), which is where "frontier knowledge on an 11 GB
card" gets read.

## The plan of attack

Each step says what it decides. A step is struck when it is done.

0. ~~**The trunk: the smallest model that composes in context.**~~ Struck 2026-10-27,
   `readings/ceiling-*`. No small model composes in context at a cost a store could pay.
   With the whole story in front of it and thinking off, tasks 1/2/3 read 0.49/0.04/0.07
   at 0.8B, 0.76/0.35/0.22 at 2B, 0.92/0.32/0.28 at 4B, and 0.995/0.43/0.26 at Gemma 4
   12B. One worked example per task buys little. Thinking on, the 2B closes its reasoning
   within 2,048 tokens on 16 of 120 questions, restating the story and looping, at about
   1,900 tokens a one-word answer. So: the one-fact task scales with size and is solved at
   4B; the two- and three-fact tasks are solved at no size we can run, with or without
   thinking. (Step 1 then showed the same trunk composes them given only the supporting
   rows: the wall is finding the facts, which is the glue's job by hops in step 2.) **The
   trunk is Qwen3.5-2B**, the largest that trains adapters in fp32 on the card; the 4B
   (0.92 against 0.76 on task 1,
   serving only, since fp16 training is not an option on Pascal) is the step up if the
   2B's one-fact ceiling bounds step 1. Open fork: a small model with a reasoning mode
   that terminates (Phi-4-mini-reasoning, SmolLM3) as a second trunk family, if hops
   need the trunk to reason within a hop. The scorer is checked by `tests/test_scorer.py`
   and the floors are near 0, so the wiring is not in doubt.

1. ~~**The store, with no training.**~~ Struck 2026-10-28, `readings/store-minilm-trunk-*`.
   10,702 rows from 300 stories appended as heard, brute-force cosine in numpy: 0.1 ms a
   lookup, 10 KB a row (8 KB of it the trunk's 2048-float key), and identical retrieval after
   a restart and after thousands of rows heard since. Within the episode, MiniLM top-4 puts
   task 1 at the ceiling (0.76, recall 0.98 of the supporting row); tasks 2 and 3 fall to
   0.13 and 0.03, below four random rows of the story (0.15, 0.14), because similarity to
   "Where is the milk?" returns the rows about the milk (86% of what is shown, 13% at random)
   and misses the row saying where its carrier went. The trunk's own mean-pooled state is a
   worse key on every task (recall 0.76/0.41/0.17). Over the whole store with no episode,
   retrieval reads the floor (0.06): four names across 300 stories, so a store with no
   episode or time is not memory, and the reader must carry both (step 4). The finding that
   matters most: **the oracle arm**, the trunk given exactly the supporting rows and nothing
   else, reads **1.00 / 0.77 / 0.93** against the whole-story ceiling of 0.76 / 0.37 / 0.23.
   The 2B composes two and three facts in one pass when handed only them; what it cannot do
   is find them in the story. Step 0's wall was distraction, not composition.

2. ~~**Reading across rows, by hops.**~~ Struck 2026-10-29 at 0.75 against the oracle's 0.78 on
   task 2, `readings/store-minilm-trunk-*`. Tasks 2 and 3 with the supporting facts as separate
   rows among thousands of others. Step 1 showed the trunk answers a two- or three-fact
   question once the supporting rows, and only they, stand in front of it, so the glue's job
   is to assemble that set: retrieve, read the row, re-query with what was read, retrieve
   again, and hand the set to the trunk in one pass. Read so far (2026-10-28,
   `readings/store-*-2026102802*`, `readings/walker-fit-*`), tasks 2/3, oracle 0.78/0.92:
   - Untrained hops, the re-query being the read row's own MiniLM key, top-4 a hop, three
     hops: 0.39/0.14, recall of the supporting set 0.89/0.46. Twelve rows with no hop:
     0.30/0.15, recall 0.85/0.61. Following a random row instead: 0.30/0.18. So the
     untrained hop is worth something on task 2 and nothing on task 3.
   - The trained walker: a linear map from the trunk's last-token state at layer 16, after
     reading "Question: ... Known: <rows so far> Next fact:", to the MiniLM key of the next
     supporting row, ridge-fitted in closed form on the train split in seconds.
     Teacher-forced, it ranks the next right row in its top four 96-98% of the time on
     task 2's hops and task 3's first two, and 62% on task 3's third; in first place only
     34-57%. End to end with its own choices, one hop, top-4 shown: **0.42/0.29**, recall
     0.93/0.57; two hops 0.41/0.27, recall 0.94/0.63. Training buys +0.04 recall on task 2
     and +0.16 on task 3 over the untrained hop, and task 2 is above the whole-story
     ceiling (0.37).
   - The gap to the oracle is not recall. With the whole supporting pair among the six or
     seven rows shown, the trunk scores 0.46 on task 2; given the pair alone, 0.78. Shown
     only the rows the walker followed, one a hop, it drops to 0.24/0.03, because the
     walker's first choice is right only half the time. The set is found; what is missing
     is choosing it from among what was found.
   - The evaluator, step 3's first form (`readings/evaluator-fit-*`,
     `readings/store-*-20261028T054705Z`): the same recipe over "Question ... Known ... Next
     fact: <candidate> Is the next fact right:", judging the walker's top four. Its stop rule
     is right at 99.6-100% of states with nothing left to find and goes on at 97-100% of the
     rest, so the number of hops is the glue's call, not a parameter. Its first choice is
     right 77% of the time at task 2's first hop (the walker's: 49%) and no better than the
     walker's at the other hops (0.55, 0.46, 0.55, 0.39). End to end, the walker proposing,
     the evaluator choosing and stopping, the trunk shown only the chain: 2.0 rows on task 2
     and 2.8 on task 3, **0.41/0.27**. The chain is exactly the supporting set 40%/7% of the
     time, and when it is the trunk scores 0.80/0.97, the oracle.
   - The gradient head, and time (`readings/evaluator-fit-2026102815*`,
     `readings/evaluator-train-*`): the judge state of every candidate cached once (12,600
     states, 52k judgements, three hours on the shared card), every head then fitted from the
     cache in seconds. An MLP over the judge state alone reads as the linear head does
     (0.77/0.52 and 0.49/0.55/0.45): capacity was not the wall. What the prompt lacks is when
     each row was heard, so four when-heard numbers (the row's place in the story, its
     distance from the last row read, before or after it) go in beside the state. Linear,
     that gives 0.84/0.80 and 0.84/0.94/0.88 at first place; the MLP, **0.96/0.93 and
     0.94/0.99/0.97**, and it stops right at 100% of done states. The control, when-heard
     alone and no judge state: 0.71/0.49 and 0.93/0.83/0.86, stopping right at only 45-77%.
     So task 3's hops are mostly time (the walker's four candidates hold the right rows and
     time orders them); task 2's second hop and every stop decision need the trunk's
     judgement; together they carry each hop. These are among states whose target is in the
     walker's top four, which is 96% on task 2 and 72%/98%/62% on task 3's hops, so the
     walker's recall is now what bounds task 3. Time belongs in the row (step 4); the
     evaluator reading it as numbers beside the state is that step's first form, arrived
     early.
   - The ranking walker (`readings/walker-train-*`): the walker trained by gradient, scoring
     every row heard so far from the hop state, the row's MiniLM key and when it was heard,
     listwise over the pool, from the same cache, ten minutes on the CPU. Teacher-forced, it
     puts the next right row first 98% of the time and in its top four 100%, at every hop of
     every task (the linear walker: 45% and 87%). Controls: without time it reads as the
     linear walker does (0.49 first); without the hop state, 0.47. The three together carry
     it, as with the evaluator.
   - End to end (`readings/store-*-20261028T154900Z`, `-161358Z`), the glue proposing,
     judging and stopping, the trunk shown only the chain: with the linear walker and the
     gradient evaluator **0.97/0.67/0.38**, recall of the supporting set 0.97/0.88/0.62;
     with the ranking walker **0.99/0.69/0.58**, recall 0.99/0.90/0.80, 2.0 and 2.8 rows
     shown, 0.8 s a lookup at fp32. On task 2 the chain was exactly the supporting pair 84%
     of the time, 14% held a wrong row, 2% stopped early; on task 3, 60% exact, 19% short,
     20% a wrong row. That evaluator had been fitted on the linear walker's candidates.
   - The evaluator refitted on the ranking walker's candidates (`readings/evaluator-*-20261028T1847*`,
     `-1848*`; `store-*-184937Z`, `-191355Z`). Teacher-forced it now stops on every question
     and goes on at every hop; its choice among the four is right 93-99% of the time, the
     walker's own first 96-100%. End to end with the evaluator choosing **0.99/0.74/0.84**,
     recall 0.99/0.94/0.94, no short chains; with the walker choosing and the evaluator only
     stopping (`select:4:3:w`) **1.00/0.75/0.86**, recall 1.00/0.97/0.96. The chain is exactly
     the supporting set 95% of the time on task 2 and 94% on task 3, and on those the trunk
     scores the oracle (0.78, 0.92). Every miss left is one wrong row chosen, in one chain in
     twenty. The stop is solved, and the evaluator's choice earns nothing over the walker's.
   So the shape is settled: propose, judge, stop, hand the trunk the chain, with time in
   front of both heads, the evaluator's job the stop. Step 2 stands at 0.75 against the
   oracle's 0.78 on task 2 and 0.86 against 0.92 on task 3; the gap is the wrong row in one
   chain in twenty and is left where it is. A walker covers composition over facts and
   nothing else: arithmetic, counting and planning (bAbI tasks 7 and 19 first) are covered
   by nothing yet.

3. **The evaluator and search.** A second head over the trunk's hidden state: given a candidate
   step, is it good; search in ordinary code, budgeted in forward passes; counting and
   arithmetic to a tool. Decides whether a small evaluator with search beats the trunk's own
   chain of thought at matched forward passes. This is the step the frontier claim lives or
   dies on; its refutation line is in the decision bullet below. Training here is in daytime
   bursts with checkpoints, as everything is.
   - Path finding (`readings/path-*`, `scripts/path_reading.py`), bAbI task 19, five rooms placed
     relative to each other, the answer two moves in order, chance one in sixteen, 500 test
     stories. The step evaluator reads the trunk's state over the story, the question, the moves
     so far and one candidate move, labels from the gold path; the search is over the four moves
     with a stop, three steps at most, four prefill passes a step a partial path. Floor 0.016,
     ceiling 0.104 (the trunk says "w,s" to half the stories whatever they say), chain of thought
     0.262 at a 512-token budget, 517 passes a question; left to itself the 2B loops in its
     thinking and has not answered by 2048 tokens, so the server cuts the thinking at the budget
     and the model then answers. Greedy search with the linear step evaluator **0.29 at 12
     passes**: the first move right 46%, the second given a right first 74%, the stop 85%.
     Wider beams lose at every stage (0.266 at 20 passes, 0.254 at 36); the gradient evaluator
     is behind at every width (0.23-0.25) with no better stop. So the trunk's judgement of a
     move, read by a linear head and searched in code, beats its own chain of thought at a
     fortieth of the passes, and the evaluator's score is a decision, not a probability to sum.
     But chain of thought at a 2048-token budget scores **0.752** (2047 passes), and handed
     exactly the two supporting sentences the one-pass trunk scores 0.146: the trunk can do the
     task, thinking, and cannot compose two directional facts into an ordered path in one pass.
     So no store and no number of search passes over a one-pass judgement of a whole move reach
     it; the wall is one-pass composition, and the step prompt asked the trunk to compose.
   - The edge form (`readings/path-20261029T14*`, `edges:trunk`): the trunk never composes.
     It restates each sentence as `room|direction|room`, then verifies its restatement against
     the sentence, yes or no; code adds the inverse of every edge and walks the shortest path.
     **1.0 on all 500 stories at 58 passes a question**, every one of 2,500 edges right, against
     chain of thought's 0.752 at 2,047. The restatement alone swaps the two rooms one time in
     eight (0.726); asked which room lies in the direction of the other, the trunk swaps more
     (0.486); asked to verify, it catches every swap. The deployed system has every input:
     the story and the question. What is ours: the edge format, the verification question and
     the walk, chosen for the problem; what is the trunk's: reading one sentence into a relation
     and checking one. So on the first reasoning task, one fact a pass plus code beats the
     trunk's own thinking at a thirty-fifth of the passes, and the evaluator-and-search form
     is the loser here; it stays for maths, where the steps are not a map.
   - Counting (`readings/count-*`, `scripts/count_reading.py`), bAbI task 7, how many objects
     someone is carrying, 1,000 questions over 200 stories. The same shape: the trunk restates
     each sentence as one event, `person|took|object`, `person|dropped|object`,
     `giver|gave|object|receiver` or `none`, one pass a sentence, and code replays the events
     to the question and counts. **0.989 at 23 passes a question**; the one-pass trunk given
     the story 0.502 (`readings/ceiling-*-20261029T151659Z`), given nothing 0.468, which is the
     share of "none" answers; chain of thought at a 2048-token budget **0.64** on 300 questions
    at 1,872 passes (`readings/ceiling-*think-20261029T155653Z`), the thinking circling on
    whether a handed-over object is still carried. 2,458 of 2,470 events read right; the twelve wrong all swap giver
     and receiver in "handed the apple to Sandra". Not chased: a verification pass would double
     the cost for a point. The scorer now reads number words as digits; the first reading of
     task 7 scored 0.0 on "one" against "1".
  - Arithmetic (`readings/maths-*`, `scripts/maths_reading.py`), GSM8K test, the first 500 of
    1,319 grade-school word problems, the deployed system holding every input: the question.
    Asked for the number alone the trunk scores 0.048 at 5 passes. Asked for a Python program
    whose last line prints the answer, run by code in a fresh interpreter with a timeout,
    **0.622 at 102 passes**; 19 programs crash (a name typed two ways) and 170 run to the
    wrong number, mostly a quantity misread ("increased the value by 150%" of the purchase,
    not the total). Chain of thought at a 2048-token budget **0.87 at 1,953 passes**, 430 of
    the 461 that end in an answer right. The first thinking reading left 64 tokens for the
    answer after the cut thought, enough for a bAbI word, not for the worked solution the trunk
    writes before its number: 301 of 500 were cut mid-sentence and it read 0.40, but 196 of the
    199 whose thinking finished inside the budget were right. So on arithmetic the trunk's own
    thinking is the strong mechanism and the tool is the cheap one, and the thinking fails by
    length, not by reasoning: it circles and re-checks until the budget cuts it. That is the
    stop, the one job the step-2 evaluator did well.
  - The stop, read on recorded thoughts before any head is trained (`readings/stop-*`,
    `scripts/stop_reading.py`): a regex for "the answer is N" stands in for the stop, and the
    thought is cut at its k-th stated conclusion. On task 7's 300 thoughts, cutting at the
    first conclusion scores 0.653 against the trunk's own end 0.64, at 0.37 of the thought;
    the second and third conclusions are no better (0.62, 0.63). So on counting the stop buys
    the cost, two and a half times fewer tokens, and not the accuracy: the trunk's first
    conclusion is as often wrong as its last, and a head that only stops cannot lift 0.64
    toward the tool's 0.989. A head that lifts the score has to judge which conclusion is
    right, which is the evaluator's job, not the stop's. The same on GSM8K's 500 recorded
    thoughts (`readings/maths-20261029T205854Z`, the re-take scoring 0.852, within run-to-run
    noise of 0.87): cut at the first stated answer 0.824 at 0.51 of the thought, at the second
    0.836, at the third 0.838. The same shape on both tasks: a stop alone halves the cost and
    keeps or slightly lowers the score. The pruning form is not built.
  - Decided with John 2026-10-29: step 3 goes on as written, the evaluator over steps of maths
    solutions with search in code and the stop inside it, not a stop head on its own. The
    refutation line, in words: if at matched passes the trained evaluator searching the trunk's
    own steps does not beat the trunk's chain of thought on GSM8K, after the step size the
    trunk handles in one pass and the labels we can get have both been tried, then the glue is
    not where the reasoning comes from and this is a harness around a small model, which no
    trunk that fits the card makes frontier. The fallback agreed then is to unfreeze the trunk:
    an adapter on it trained from the search's own wins, so the trunk is a learning piece of
    the system rather than a fixed organ. The frozen trunk only makes sense if the system
    around a small one reaches frontier-level reasoning on the card; that is the bet, and this
    is the step that reads it.
  - The step evaluator (`readings/step-evaluator-fit-*`, `scripts/step_evaluator.py`,
    `openplexus.steps`): a ridge head over the trunk's last-token state at layer 12 of a judge
    prompt (the problem, the last six steps so far, the candidate, "Is the next step right:"),
    fitted to 8,000 PRM800K steps of MATH solutions, as many rated wrong as right. On 1,500
    test candidates from solutions it never saw: **balanced accuracy 0.727, AUC 0.79**, chance
    0.5; 0.70 of right steps pass and 0.755 of wrong steps fail. Layers 12 to 20 read alike.
    So the frozen trunk's state carries a real but middling signal about whether a maths step
    is right.
  - The search (`readings/step-search-*`, `scripts/step_search.py`), GSM8K test, 2026-10-30,
    the trunk in process proposing one-line steps: the control, the trunk's own greedy steps
    with no judge, **0.364 at 102 passes**, 103 of 500 never reaching an answer line (it loops
    on a step it cannot get past); the stop alone, one greedy step against the forced answer
    line with the evaluator choosing, 0.252 at 105, every path finished; the four-way search,
    four sampled steps and the answer line a depth, the evaluator keeping the best, **0.363 at
    538 passes** over 256 problems, ended early as indistinguishable from the control; the
    chance judge, the same search with random numbers for the evaluator, 0.25 at 380 passes,
    stopped at 104 problems as 2.7 standard errors below the search. So the evaluator's
    choices do real work, lifting a random pick from 0.25 to the trunk's own greedy 0.36, and
    no further: the PRM800K-trained evaluator choosing among four of the trunk's steps buys
    nothing over the trunk's first choice at five times the passes, and the stop folded into
    it fires too early. The rows say why: it scores a bare restatement of a given fact highest
    ("Seattle has 20 sheep", 1.21) and accepts an answer line right after one, because
    PRM800K's "reasonable, no progress" steps were labelled good; and a 2B trunk's four
    samples at one step rarely include a step it would not have taken anyway. Against the
    chain of thought's 0.85 at 1,950 passes the search form as built is far short. By the
    refutation line the labels are the first thing not yet tried (fork a, rollouts running 2026-10-30) and
    the trunk learning the second (fork b); the evaluator-and-search form is not struck, since
    the evaluator read here is the one trained on another distribution's labels, but the
    loop's first round starts from the trunk's own steps as the proposer, not from this search.
  - Forks after the search reading, agreed with John 2026-10-30, in order; the round that runs
    them is `scripts/loop.py` (rollouts, features, fit, adapter, search, each a script behind
    a flag, a stage already done skipped, the server up only for the rollouts). (a) Steps
    labelled by outcome (`scripts/step_rollouts.py`, Math-Shepherd's method): the served trunk
    walks training problems, proposes k steps and an answer line at each, and every candidate
    is labelled by m rollouts to an answer, no human label and no test problem; the evaluator
    is refitted on them and read on the PRM800K test steps too. Read 2026-10-30
    (`readings/step-evaluator-fit-20261030T085449Z`): 700 training problems walked, 7,395
    candidates labelled in four hours, 2,329 right; the forced answer lines 2,566 of them and
    584 right, so "not yet" is in the labels. The judge fitted on them, layer 16, on 772
    candidates from problems it never saw: **balanced accuracy 0.729, AUC 0.827**, above the
    PRM800K judge's 0.727 and 0.79 from fewer labels; on the PRM800K test steps 0.574 and
    0.59, so a judge learns the distribution it is corrected on and does not carry to
    competition maths written by humans. (b) The trunk learns (`scripts/trunk_adapter.py`): a LoRA adapter on its
    attention and MLP projections, trained on the step turns the rollouts labelled right,
    merged at load (`LocalTrunk(adapter=)`), the control the same arm without it. Read
    2026-10-30 (`readings/step-search-20261030T104210Z`): 1,879 turns from 436 problems, the
    held-out loss 0.359 to 0.264 at its best (step 400 of 700, noisy either side; a first run
    at the full rate from step one taught the trunk to answer at once, so the rate warms up and
    the best checkpoint is kept). The trunk with that adapter, its own greedy steps on GSM8K
    test: **0.307 at 49 passes**, 2.6 standard errors below the plain trunk's 0.364 at 102,
    stopped at 456 problems. It learnt to be terse: 84 paths answer at the first step (7
    right) against the plain trunk's 57, since a third of its training turns were answer lines;
    and it changed a quarter of the outcomes both ways (67 right made wrong, 49 wrong made
    right), so it learnt, in the wrong direction. The remedy would be at the data, fewer
    answer-line turns, but the filtered-steps form is not pursued: the pivot below replaces it
    with the published loop. (c) A learning-progress monitor, the score a problem type over time and search budget
    to the steepest slope; waits for a problem set with types. (d) The store feeding the
    proposer (`openplexus.fetch`, a `:fetch` arm): the system's own solved problems indexed by
    the question, the nearest shown before the trunk proposes, never a question's own solution;
    the plexus mechanism read on the proposer for the first time. Built, not run. Its second
    form, John 2026-10-30: key on the thinking, not the question; at every step fetch the solved
    situations most like the steps so far (by an embedding of the recent steps, or the trunk's
    state after reading them) and show what the next step was there, every fetched step one
    that outcome marked good; the rollout rows are already those pairs. Read against the same
    search without it, since a trunk fed its own past steps can set in its ways. (e) A trust
    signal for the judge: as a linear head over the trunk's state it reads AUC 0.55 at
    predicting the evaluator's errors (the trusted half right 0.76 against 0.69), so the state
    does not say when its judge is wrong; the signal has to come from disagreement between
    judges fitted on different rounds, or from the outcome record. Not built.
  - The loop, as talked through 2026-10-30: propose, judge, search, retrain the judge and the
    trunk on the wins, search again from a better trunk. Its first reading is small and comes
    before anything below is built: train one round and ask whether round two's search beats
    round one's on held-out problems. John's framing: every decision runs on internal signals;
    external signals exist to correct them, a signal that stops being corrected drifts, and
    the system should feel the absence of correction as uncertainty, not freedom. Three pieces
    are missing, and each resolves to the system making its own signal where none is handed to
    it: a supply of problems (generators with keys, the trunk writing variants of solved
    problems with Python computing the key, the world last; test sets never trained on); a
    verifier without a key (the evaluator is the values, and values are only as good as what
    last corrected them, so the mechanism is manufacturing a check: write the tests, re-derive
    by a second route, call a tool, ask the world; two cheap judges and a third opinion only
    where they disagree; a value that does not count cost is a loop); and asking the store
    (finding a passage is solved and fits this box, Wikipedia's text and a passage index of
    some 15 GB answered on the CPU; knowing when to ask and what for is the trunk learning to
    query, trained by whether the fetch changed the outcome). The known failures of this loop
    and the answer to each: the self-judge drifts (outcome labels, the judge refitted every
    round, manufactured checks); the trunk learns to please the judge (the same, plus the
    trust signal); the search ceiling of a small trunk sits below the frontier on hard
    problems (Snell et al., 2024; the trunk learns each round and the store and tools inject
    steps it would not write; whether that is enough is the bet, unanswered); forgetting as
    the adapter trains on (replay from the store, step 7; adapters a domain routed by the
    trunk's state; a fixed held-out set of every earlier task re-read each round; not built);
    running out of problems (generators; not built). Neighbours, to be checked before any is
    leant on: STaR, ReST and Math-Shepherd are this loop small and the o-series and DeepSeek-R1
    large; Absolute Zero (2025) is the problem supply; Self-Rewarding Language Models (2024)
    is values-as-key and drifted after a few rounds; Voyager is the store as a skill library;
    RETRO is the store as knowledge. The R1 paper found this loop on small models lost to
    distilling a large model's reasoning into them, so by John's standard a frontier teacher at
    training time is fair, and the trunk should start as the best distilled reasoner that fits
    (fp32 in process caps it near 2B; Qwen3.5-2B is one; a comparison is an afternoon's
    reading). Not novel in any piece; possibly in the whole: one card, the store as the
    knowledge, the trunk a learner, the system supplying its own problems and checks.
  - Decided with John 2026-10-30, after the search reading: the quickest bare-bones prototype
    from off-the-shelf parts, maths kept as the definite domain until the loop is shown to
    work, and the core bet (the store feeding the proposer, the trunk learning when and what
    to ask) read as soon as the loop runs. Four parts in five of the mechanisms were already
    borrowed; two we wrote have better published forms. The judge: a published process reward
    model of 1.5B would beat the 0.73 linear head on maths steps, and is fair by the standard
    since the deployed system carries it; the head stays as the reading of what the frozen
    state holds. The loop: TRL's GRPO, the R1 recipe, the trunk sampling whole solutions and
    trained on the key directly, no step judge and no search, since the step judge recovered
    the baseline and no more and the R1 paper found step judges added little
    (`scripts/grpo.py`). First its smoke measures seconds a step and memory on this card in
    fp32, which decides whether the recipe is days or weeks here; the trunk's non-thinking
    solution first, since it is a tenth the tokens of its thinking, the thinking mode behind a
    flag. Round one's adapter arm (fork b, filtered steps, the ReST-EM shape) is read on the
    trunk alone against 0.364 and the step-judge search arms are skipped. What stays ours: the
    store as the primary knowledge and the asking, the continuous life on one card, the system
    supplying its own problems and checks, and the readings. The GRPO smoke 2026-10-30: 20 to
    25 seconds an optimiser step of two problems and eight solutions, 8.7 GiB peak, so an epoch
    of a thousand problems is about three hours here, days not weeks. Its control
    (`readings/grpo-20261030T111215Z`, `scripts/grpo_reading.py`): the plain trunk's whole
    greedy solution under training's prompt, GSM8K test, **0.736 at 237 passes a question**,
    every question answered; between the step form's 0.364 at 102 and the thinking's 0.85 at
    1,950. Round one, a thousand training problems, four solutions each, is running; its
    reading is the same arm with the trained adapter against 0.736, the early stop on.
  - Two principles from the talk with John 2026-10-30, for after the loop runs. The system is
    several learners over a shared state, each credited for the difference its action made to
    the outcome (counterfactual credit: the step by paths through it, the fetch by the solution
    with and without it, the stop by the answer when it stopped), the trunk itself trained on
    the final outcome; the trunk is not made the sole decider, since every decision would then
    be capped by a 2B's text policy, and a judge with its own representation (a published 1.5B
    process reward model, or a second small model) is on the table. And the reasoning fork,
    held until learning works: a clock for the trunk, a loop over its own state driven by a
    small trained adapter with the outcome as the reward, so it can take a step of thought
    without writing a token; natural on this trunk, whose eighteen linear-attention layers
    already carry a state across tokens, and a coin flip against written thought at this size
    by the published results (Coconut 2024, recurrent-depth transformers 2025).
  - From the same talk, the tests the design is held to. A brain is also many organs; what
    makes it one learner is that every part learns from shared signals and they co-adapt, so
    the test of this design is not how few parts it has but whether its joints learn: the
    decisions between the parts (how many samples, when to stop, when to fetch, when to
    compute) are the places learning stops today, and each is to become a learner credited by
    outcome. An LLM is a cortex slice, not a brain; the store, the judge, the tools and the
    loop are the missing organs, not a scaffold around a brain. Ensembles of small models buy
    checking and calibration (agreement, debate, critique, routing: Mixture-of-Agents 2024),
    not reasoning capacity, which has not been shown to ensemble; the 1991 Mixture of Experts
    was separate learners under a gate, the modern one slices of one layer. The key-less judge,
    off the shelf first: a published process reward model (Math-Shepherd 7B, Qwen2.5-Math-PRM
    7B, Skywork-PRM 1.5B) as a comparison arm for our judge on maths; agreement across samples
    as the first key-less signal, since it costs only passes; a small preference reward model
    (Skywork-Reward, ArmoRM) when there is an open-ended task to read it on; generative judges
    (Prometheus 2, self-taught evaluators, CriticGPT) and written principles (Constitutional
    AI) after; nothing of our own built for key-less judging until those are read. Known: judges
    are gamed (reward over-optimisation, 2022) and have tastes (length, position, their own
    writing, 2024); the fixes are several judges trained differently with disagreement as
    uncertainty, and the judge refreshed every round. The three inefficiencies of word-stream
    reasoning and their answers here: every thought squeezed through a token (the latent loop
    above; this trunk's linear-attention layers already keep a state), no backing up (search in
    code, a backtrack as a rewarded action), no sense of how long to think (the stop and the
    budget learnt from outcome, as R1's models learnt to think longer only where it paid).
    Order: learning before reasoning, since a reasoner that does not learn is a search over a
    fixed trunk, which is what the step form read as.

4. **Writing.** What goes in a row: the raw sentence, or the trunk's own restatement with
   names resolved ("John is in the hallway", not "he went there"), one fact a row. Rows carry
   time and are never deleted; the reader prefers the latest. Decides the row format and
   whether the trunk should write its own rows.

5. **Continual state.** The conversation's working state saved to disk and reloaded, so a
   restart continues mid-conversation: the KV cache of the prefix first. If its growth bites,
   a trunk with a fixed-size state (a state-space model, or a Titans-style module) is the
   alternative. Decides how state persists.

6. **Scale and speed.** A real knowledge store (a Wikipedia subset), the trunk quantised to
   int8 or 4-bit, and the budget read honestly: tokens per second, VRAM, disk. Decides whether
   this reads as frontier knowledge on an 11 GB card, and what is left.

7. **Consolidation: the trunk learns slowly, by replay.** Complementary learning systems
   (McClelland, McNaughton and O'Reilly, 1995): the hippocampus learns in one shot and the
   cortex learns slowly by replaying what the hippocampus holds, which is what stops the slow
   system forgetting. The store is the hippocampus; the trunk is the cortex. Periodically and
   offline, a small trainable part of the trunk (LoRA, or a Titans-style memory module inside
   it) is trained by replay from the store, in daytime bursts with checkpoints. Reading: a
   fact that has been consolidated is answerable with the store switched off. Decides whether
   knowledge can move from rows into weights without a training run, and at what cost in
   forgetting. The adapter trained on the search's wins (step 3, fork b) comes before this
   and is skill; this step is knowledge moving into weights.

Where the glue makes a choice (which row to follow, whether to retrieve again, whether a
sentence is worth writing, whether a row supersedes an older one), it is a typed decision from
a small classifier head over the trunk's hidden state, trained with a label, never text the
trunk generates and we parse. The one text the trunk writes for code to read is a tool call,
and a tool call is checked by running it.

Later, in no order: routing in the trunk (mixture of experts or activation sparsity, with cold
weights in CPU RAM); training a trunk of our own; discrete coarse codes as store keys; sharding
the store across machines. None of these starts before step 2 has a reading.

A side bench, apart from the steps: alternatives to backpropagation, priced honestly. A
character-level model of about ten million parameters on TinyStories, AdamW with backprop as
the baseline, and every alternative read at matched FLOPs and matched wall-clock on this
card: zeroth-order (MeZO), forward gradients, forward-forward, local losses. The prior is
that all of them trail backprop and the gap widens with scale (Bartunov et al., 2018), and
the bench exists to measure that gap for the glue, which is small enough that a cheaper
credit signal might not cost it much. It runs in minutes and never touches the trunk.

## Setup

- Python 3.12 in `.venv` via `uv sync`. Torch comes from the cu126 index because cu128 and
  later dropped Pascal; `tests/test_toolchain.py` checks it.
- Models live in the Hugging Face cache (`~/.cache/huggingface/hub`). Qwen3.5-0.8B, 2B and 4B
  are there as safetensors for in-process use and as Q8 GGUFs; Gemma 4 12B at 4-bit is in
  `D:\Downloads`. llama.cpp is built at `D:\tools\llama.cpp-src\build\bin\Release`.
  Inference-only readings go through llama-server (`local:` in `ceiling.py`; launch:
  `llama-server -m <Qwen_Qwen3.5-2B-Q8_0.gguf> --port 8093 --jinja --reasoning-budget 0
  -ngl 99 -np 8 -c 32768`), which is
  several times faster on this card than fp32 PyTorch, whose Qwen3.5 linear-attention
  layers run on fallback kernels here. PyTorch is for anything that needs hidden states or
  gradients.
- Decoding on this card is bandwidth-bound, so serve with eight parallel streams and batch
  the questions; four streams and a 2,048-token thinking budget was 300 questions in two and
  a half hours.
- Thinking is off for every reading: `enable_thinking=False` in the chat template in process,
  `--reasoning-budget 0` on llama-server. Check it in the reading, not in your head.
