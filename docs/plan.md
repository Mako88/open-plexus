# Open Plexus

## The goal

Frontier-level on one GTX 1080 Ti. Frontier models are controlled by whoever can afford the
training run; a model of that worth on an old consumer card changes who gets to have one.

A frontier model does two things with one huge set of weights: it holds what it knows, and it
reasons by one chain of tokens with no backtracking, a huge evaluator doing a weak search.
The design here inverts both. Knowledge lives in a store on disk and is learnt by appending.
Reasoning is a small trained evaluator and search in ordinary code, the way AlphaGo beat
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
  They are the only thing we train.
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
   step, is it good. Its first form, a linear head judging the walker's candidates with bAbI's
   supporting lines as labels, is read under step 2; then labels from step-labelled maths
   (PRM800K, 800k human labels on steps of MATH solutions) and bAbI's path finding (task 19),
   which is planning in miniature. The gradient-trained evaluator has to beat the linear one.
   Search in code: beam over hops first, then a tree with backtracking, budgeted in forward
   passes. Counting and arithmetic go to a tool: an aggregation over rows for the first, a Python
   call the trunk writes for the second, read on bAbI task 7 and on GSM8K. Decides whether a
   small evaluator with search beats the trunk's own chain of thought at matched forward passes.
   This is the step the frontier claim lives or dies on. It fails if search with the trained
   evaluator is no better than chain of thought at the same budget on a problem the 12B with
   thinking gets right; that would mean the evaluator, not the search, is the wall, and the next
   question is whether a bigger label set or a LoRA under the head moves it. Training here is in
   daytime bursts with checkpoints, as everything is.
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
    is right; whether that signal, applied k times a step, beats the trunk's own thought is
    the search reading next.
  - Forks held for after the search reading, agreed with John 2026-10-30, in the order to try
    them. (a) Label steps by outcome: after a search, every step is marked by whether the paths
    through it reached the right answer, and the evaluator is refitted on those labels, so the
    judge learns from the system's own experience and PRM800K falls away (Math-Shepherd's
    method). (b) The adapter on the trunk trained from the search's wins, the agreed fallback,
    which this card can train: frozen weights 8 GB, the adapter and its gradients a few hundred
    MB, checkpoints every few minutes. (c) A learning-progress monitor: the score a problem
    type over time, with search budget spent where the slope is steepest. (d) The store feeding
    the proposer: a solved similar problem or a fact fetched into the step prompt, the plexus
    mechanism itself, untested on the proposer. (e) A trust head beside the evaluator, over the
    same state, predicting whether the evaluator's verdict here will prove right, trained on the
    record of verdicts and outcomes; it chooses when to train, when to search harder and when to
    ask a tool. John's framing: every decision runs on internal signals, and external signals
    exist to correct the internal ones; a signal that stops being corrected drifts.
  - The loop, as talked through with John 2026-10-30. The forks above close one loop: the trunk
    proposes, the evaluator judges, the search finds wins the trunk alone would not, the wins
    retrain the evaluator and the trunk, and the next round searches from a better trunk. Its
    first reading is small: train one round on the maths loop and ask whether round two's
    search beats round one's on held-out problems. Nothing below is built before that number
    moves. Three pieces are missing from the loop and each resolves to the same move, the
    system making its own signal where none is handed to it: a supply of problems (generators
    with keys: DeepMind's mathematics dataset, procedural games, the trunk writing variants of
    a solved problem with Python computing the key; test sets never trained on; the world
    last); a verifier without a key (the evaluator is the values, and values are only as good
    as what last corrected them, so the mechanism is manufacturing a check: write the tests,
    re-derive by a second route, call a tool, ask the world; two cheap judges, and a third
    opinion only where they disagree; a value that does not count cost is a loop); and asking
    the store (finding a passage is solved engineering and fits this box, Wikipedia's text and
    a passage index of some 15 GB answered in milliseconds on the CPU; what is not solved is
    knowing when to ask and what for, which is the trunk learning to query, trained by whether
    the fetched thing changed the outcome). Neighbours in the literature, to be checked before
    any is leant on: STaR, ReST and Math-Shepherd are the small versions of this loop and the
    o-series and DeepSeek-R1 the large; Absolute Zero (2025) is the problem supply; Self-
    Rewarding Language Models (2024) is values-as-key, and drifted after a few rounds; Voyager
    is the store as a skill library. Two warnings: Snell et al. find the search ceiling of a
    small trunk below the frontier on hard problems, and the R1 paper reports that running
    this loop on small models did worse than distilling a large model's reasoning into them.
    So a frontier teacher at training time is on the table for the adapter, by John's
    standard a fair input since the deployed system does not carry it, with the loop then run
    to go past where the teacher left it. Not novel in any piece; possibly in the whole, a
    continuously running system on one card where the store is the knowledge, the trunk
    learns, and the system supplies its own problems and checks. John 2026-10-30: start from
    the best trunk that fits, a distilled open-weights reasoner if one beats Qwen3.5-2B on the
    chain-of-thought arm (one afternoon's reading; the in-process fp32 path caps the trunk
    near 2B on this card). The known failures of this loop and the plan's answer to each,
    with the two not yet answered marked: the self-judge drifts (labels anchored to outcomes,
    fork a; the judge refitted on fresh outcomes every round so the trunk cannot overfit a
    fixed judge; checks manufactured where there is no key); the trunk learns to please the
    judge (the same, plus the trust head, fork e); the search ceiling on hard problems (the
    trunk learns each round so the ceiling rises, fork b, and the store and tools inject
    steps the trunk would not write, fork d; whether that is enough is the bet, not
    answered); forgetting as the adapter trains on (replay from the store, step 7, adapters a
    domain routed by the trunk's state, John's idea, and a fixed held-out set of every
    earlier task re-read each round, the regression check; not yet built); running out of
    problems (generators with keys; not yet built).

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
