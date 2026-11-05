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
- **The glue.** The part we design: small heads over the trunk's hidden state and the trunk's
  own adapter, trained by gradient descent on the card and kept in training. A walker that
  says which rows to read next, judges of candidates, and the trunk's own choices (what to
  fetch, when to stop, when to call a tool) trained by the outcome. The glue and the adapter
  are what we train.
- **The search and the tools.** Ordinary code, used where it pays: a decomposition chosen for
  the problem, a tool run and checked, a search over the trunk's samples. Budgets are counted
  in forward passes of the trunk, the unit chain of thought is also priced in.

What learns for life is the store's contents, the glue and the trunk's adapter. The bet is in
two halves. Knowledge: a small trunk reading a store approaches the same trunk with the
relevant text placed in front of it (the ceiling) on questions whose answer needs two rows put
together. Reasoning: a trunk trained on its own checkable outcomes, with a store feeding its
thinking and code where it pays, beats the same trunk's untrained chain of thought at the same
number of forward passes and keeps improving round on round. The part that cannot be
outsourced to code is the judgement of what was good, and whether it can be trained from
signals the system makes for itself is the question the whole design turns on.

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

Rung five's number (John, 2026-11-04): every training round is priced in the questions it
needed for each point its reading gained (`scripts/learning_speed.py`, a row a round,
`learning-speed-20261104T151349Z`). A system learning to learn pays less a point each round.
So far it pays more: the asking's round one 8.9 questions a point, round two 32, round three
no gain; the full-store and answering rounds 960.

Later measures, once tasks 1 to 3 are at the ceiling through the store: the other bAbI tasks;
fresh synthetic stories with invented names, longer than any context; and then real knowledge
at scale (a Wikipedia subset in the store), which is where "frontier knowledge on an 11 GB
card" gets read.

**The score** (John, 2026-11-05: fix it rather than caveat it): HotpotQA lines are read by
the lenient match (`openplexus.hotpot.lenient`: the right answer in other words, yes and no only
exactly), the same scorer for the frontier, exact match kept beside it for published work
(`rescore-20261105T183227Z`). Lenient: the frontier 0.90 with the ten paragraphs and 0.87 from
memory; our floor 0.175, gold 0.725, gold with thinking 0.795, the trained asker 0.64 on the
ten paragraphs and **0.47 over Wikipedia**, where the frontier reads 0.90. Of gold's 55 misses
by this score, 10 stop at the bridge (answer with a passage's title the answer is not), 5 say
yes or no to a who or what, 3 pick the wrong one of two, and 36 name another fact of the
passages; the misses are mostly reading, not slot-filling.

## The system

One lineage (John, 2026-11-04): the system keeps what it learns across runs. `state/system/`
holds the adapter as it stands and the ledger of every round (`openplexus.lineage`,
`scripts/lineage.py`; the ledger is versioned, the weights are not). Every training round
starts from the system's adapter and, once its readings are taken, is committed back with
them; the adapter before is kept and a round that loses more than it gains is rolled back.
Readings never write; only training and living do. The 200 reading questions never enter
training or the store, and the floor and gold lines are re-read against the plain trunk, so
the trend is one system against a fixed ruler, and two runs need not agree. The regression
set every round ends with: the asking on the ten paragraphs (`--arms ask --adapter system`)
and GSM8K. The root is round two's asking adapter. The live loop, the system hearing, asking,
writing and sleeping on its own, builds on this folder and is the step after.

## The plan of attack

Each step says what it decides. A step is struck when it is done.

0. ~~**The trunk: the smallest model that composes in context.**~~ Struck 2026-10-27,
   `readings/ceiling-*`. With the whole story in front of it and thinking off, bAbI tasks
   1/2/3 read 0.49/0.04/0.07 at 0.8B, 0.76/0.35/0.22 at 2B, 0.92/0.32/0.28 at 4B and
   0.995/0.43/0.26 at Gemma 4 12B; thinking on, the 2B restates the story and loops,
   answering 16 of 120 within 2,048 tokens. The one-fact task is solved at 4B; the two- and
   three-fact tasks at no size we can run. **The trunk is Qwen3.5-2B**, the largest that
   trains adapters in fp32 on the card; the 4B serves but cannot train here. Step 1 then
   showed the wall was distraction, not composition.

1. ~~**The store, with no training.**~~ Struck 2026-10-28, `readings/store-minilm-trunk-*`.
   10,702 rows from 300 stories, brute-force cosine in numpy: 0.1 ms a lookup, 10 KB a row,
   identical retrieval after a restart. MiniLM top-4 puts task 1 at the ceiling (0.76) and
   tasks 2 and 3 below four random rows (0.13, 0.03): similarity returns the rows about the
   milk and misses where its carrier went. The trunk's mean-pooled state is a worse key on
   every task. Over the whole store with no episode, retrieval reads the floor, so a store
   with no episode or time is not memory. **The oracle arm**, the trunk given exactly the
   supporting rows, reads **1.00 / 0.77 / 0.93** against the whole-story ceiling 0.76 / 0.37 /
   0.23: the 2B composes two and three facts when handed only them; what it cannot do is
   find them.

2. ~~**Reading across rows, by hops.**~~ Struck 2026-10-29 at **0.75 against the oracle's
   0.78** on task 2 and **0.86 against 0.92** on task 3 (`readings/store-*-20261028T19*`,
   `readings/walker-train-*`, `readings/evaluator-*`). The shape: a walker proposes the next
   row from the trunk's layer-16 state over "Question ... Known ... Next fact:", an evaluator
   judges and stops, the trunk is shown only the chain. What the readings taught, in order:
   untrained hops by MiniLM re-query 0.39/0.14; the linear walker 0.42/0.29, its recall good
   and its first choice right half the time; the linear evaluator's stop right at 99.6% of
   done states, its choice no better than the walker's past the first hop; an MLP over the
   judge state alone no better than linear, so capacity was not the wall, while four
   when-heard numbers beside the state lift the evaluator's choice from 0.77/0.52 to
   **0.96/0.93** (time alone 0.71/0.49: the hops are time ordering the walker's candidates
   plus the trunk's judgement at the stop); the ranking walker, listwise over every row heard
   from hop state, key and time, first 98% teacher-forced (0.49 without time, 0.47 without
   the state). End to end, the ranking walker choosing and the evaluator only stopping, the
   chain is exactly the supporting set 95%/94% of the time and the trunk then scores the
   oracle. Every miss left is one wrong row in one chain in twenty, left where it is. Time
   belongs in the row (step 4). A walker covers composition over facts and nothing else.

3. **Reasoning: the trunk learns from its own outcomes.** Begun 2026-10-29 as the evaluator
   and search (a head over the trunk's state judging candidate steps, search in code priced
   in passes); read 2026-10-30 as recovering the trunk's own greedy baseline and no more, and
   pivoted the same day with John to the published loop: TRL's GRPO, the R1 recipe, the
   trunk sampling whole solutions to GSM8K training problems and its adapter trained on the
   key. Decides whether a 2B trunk on this card learns to reason from checkable outcomes,
   which everything after it (the store feeding the thinking, the asking) is built on. Maths
   is the definite domain until the loop is shown to work. Training is in daytime bursts with
   checkpoints, as everything is.
   - bAbI's reasoning tasks fell to one fact a pass plus code, not to search
     (`readings/path-*`, `readings/count-*`, `scripts/path_reading.py`,
     `scripts/count_reading.py`). Task 19, path finding, 500 stories: the trunk restates each
     sentence as `room|direction|room` and verifies its restatement yes or no (the restatement
     alone swaps rooms one time in eight; the check catches every swap), code adds inverses
     and walks: **1.0 at 58 passes a question**, against chain of thought 0.752 at 2,047 and
     the linear step evaluator with greedy search 0.29 at 12 (wider beams worse, the gradient
     evaluator worse). Task 7, counting, 1,000 questions: one event a sentence, code replays
     and counts, **0.989 at 23 passes**; the one-pass trunk 0.502, thinking 0.64 at 1,872.
     The decomposition and the tool are fair by the standard; the trunk's part is reading one
     sentence into a relation and checking one.
   - GSM8K (`readings/maths-*`, `readings/stop-*`), the first 500 test problems: the number
     alone 0.048 at 5 passes; a Python program run by code **0.622 at 102**; chain of thought
     at a 2,048-token budget **0.87 at 1,953** (0.852 on re-take), its failures by length,
     circling until cut. A stop that cuts the thought at its first stated answer halves the
     cost and keeps the score (0.824 at half the tokens; on counting 0.653 against 0.64 at a
     third), so a stop buys cost and never accuracy; lifting the score is a judge's job.
   - The evaluator-and-search form, read and set aside. The refutation line agreed with John
     2026-10-29: if at matched passes the trained evaluator searching the trunk's own steps
     does not beat its chain of thought on GSM8K, after the step size and the labels have
     both been tried, the glue is not where the reasoning comes from and the trunk must learn.
     The step evaluator (`readings/step-evaluator-fit-*`, `scripts/step_evaluator.py`,
     `openplexus.steps`), a ridge head over the layer-12 state of a judge prompt, fitted to
     8,000 PRM800K steps: **balanced accuracy 0.727, AUC 0.79** on 1,500 held-out steps. The
     search (`readings/step-search-*`, `scripts/step_search.py`): the trunk's own greedy
     one-line steps **0.364 at 102 passes**, 103 of 500 never reaching an answer; the four-way
     search with the evaluator choosing **0.363 at 538** (256 problems, stopped as
     indistinguishable); the chance judge 0.25 at 380; the stop alone 0.252. So the judge does
     real work, random to greedy, and no more: it scores restatements of given facts highest,
     since PRM800K's "no progress" steps were labelled good, and four samples from a 2B rarely
     hold a step it would not take. Fork (a), outcome labels (`scripts/step_rollouts.py`,
     Math-Shepherd's method; 700 training problems, 7,395 candidates in four hours, 2,329
     right): the judge refitted on them reads **0.729 / 0.827** on its own distribution,
     above the PRM800K judge, and 0.574 / 0.59 on PRM800K's, so a judge learns what corrected
     it and does not carry. Fork (b), the trunk fine-tuned on the rollouts' good steps
     (`scripts/trunk_adapter.py`, LoRA r16, 1,879 turns, held-out loss 0.359 to 0.264):
     **0.307 at 49**, 2.6 standard errors below 0.364; a third of its turns were answer lines
     and it learnt to answer at once, 67 right made wrong and 49 wrong made right. It learnt,
     in the wrong direction; the remedy is at the data and is not pursued. Fork (e), a linear
     trust head predicting the judge's errors: AUC 0.55, so the state does not say when its
     judge is wrong; trust has to come from disagreement between judges or the outcome record.
     `scripts/loop.py` (rollouts, features, fit, adapter, search, each behind a flag) stays
     for the step form should it be revived.
   - The loop (`scripts/grpo.py`, `scripts/grpo_reading.py`), decided with John 2026-10-30:
     TRL's GRPO on the trunk's non-thinking whole solutions, four a problem, the gold number as
     the reward, LoRA r16 on the attention and MLP projections, fp32, a checkpoint every twenty
     steps, about 35 s an optimiser step of two problems (`--micro 2` by day, when John's apps
     hold 1 to 2 GB of the card and a 10 GiB peak thrashes). Readings are the adapter's greedy
     whole solution on the first 500 GSM8K test problems, paired against the other arms on the
     same questions and stopped at 2.5 standard errors of the flip count (the unpaired early
     stop over-read round one at 232 problems on an easy prefix). The control
     (`readings/grpo-20261030T111215Z`): the plain trunk **0.736 at 237 passes**, between the
     step form's 0.364 and the thinking's 0.85 at 1,950. **Round one** (`grpo-20261031T042520Z`),
     a thousand training problems: **0.774 at 246**, 50 made right and 31 made wrong against
     the base, 2.1 standard errors; the trunk learns to reason from its own checkable outcomes
     on this card. **Round two** (`grpo-20261031T093424Z`), the same adapter on the next
     thousand: **0.804 at 227**, 49 and 34 against round one, 63 and 29 against the base (3.5);
     the gap widens and the solutions shorten. In six groups of ten all four samples scored
     alike and taught nothing, so **round three** took the system's own choice of problems: of
     the next thousand, the 203 round two's greedy pass got wrong and 203 it got right. Its
     first run (`grpo-20261031T151441Z`) read 0.768 at 107 passes, 14 and 31 against round two:
     the solutions shrank from 250 tokens to 100 in twenty steps, because on a mostly-wrong set
     the 0.1 answer-line bonus was the only reward with a spread inside most groups; thirty
     steps on the same set without the bonus held 240 tokens, so the bonus is off
     (`--format-reward`, 0.1 in rounds one and two). Rerun without it
     (`grpo-20261031T191349Z`): **0.806 at 241**, 36 and 35 against round two, 53 and 18
     against the base (4.2). So three rounds lift the trunk from 0.736 to about 0.805 and stop,
     a third of the way to its own thinking at an eighth of the passes: the recipe's ceiling at
     this size on this set. The loop stays as the learner, its adapter carried forward; a round
     runs again when a piece changes what the trunk is trained on, not to push the number. The
     R1 paper found this loop on small models lost to distilling a large model's reasoning into
     them, so a frontier teacher at training time is fair, and the trunk should start as the
     best distilled reasoner that fits fp32 in process; swapped only on a reading that shows a
     piece failing for want of the trunk, never to lift a number (John, 2026-10-31). Held, from
     John the same day: the learner decoupled from the actor, the trunk answering with the
     current adapter while the learner trains behind it and swaps the adapter in (a gradient a
     few steps stale is a valid direction; the ratio clip absorbs it; a system that lives has
     this shape). Its speed premise failed: the Q8 server with the adapter as a GGUF LoRA file
     (`--served`, built) reads as fp32 in process does (320 shared questions, 12 made right, 9
     made wrong) and is slower on 240-token solutions, nine questions a minute against forty, so
     the fork has no payoff on one card.
   - The store on maths, fork (d), read and refuted 2026-10-31. First form
     (`grpo-20261031T110513Z`): round two's 1,606 right training solutions indexed by MiniLM on
     the question, the nearest shown before the test question, never a question's own; against
     the same adapter without it **5 made right and 17 made wrong** on 112 (0.679 against
     0.786). Not copying, the same length: the trunk misreads its own problem's quantities with
     a topical neighbour (a house, chickens, a profit) in front of it, as step 1 found the rows
     about the milk. Second form (`:fetch2`, `openplexus.fetch.shape`, `grpo-20261031T194227Z`),
     keyed on the method: the trunk solves, the arithmetic shape of its own solution (equation
     lines, numbers as N, names dropped) fetches the solved problem with the nearest shape, and
     it solves again with that shown, at twice the passes: **11 made right and 26 made wrong**
     on 192 (0.724 against 0.802). The first pass was right on 154 of 192, exactly the plain arm;
     the example re-derived 26 of those wrongly, with its own decomposition, not the example's
     numbers. So any solved example shown to this trunk perturbs a fifth of its solutions both
     ways, net loss: on GSM8K the store holds nothing the trunk lacks, and a 2B cannot ignore
     what it is shown; a gate that fetches only where it helps would have to pick the 11 from
     the 26 before solving, and fork (e) read that the state does not carry that. For John: the
     store's claim cannot be read on GSM8K; it has to be read where the knowledge needed is
     absent from the trunk and present in the store, and the asking as a learned action is
     built there (the recommendation put to him: a Wikipedia subset in the store with questions
     answered from it, the asking an action inside the same loop, the published search-as-action
     shape). Until then the loop runs without the store on maths.
   - The key-less judge. Agreement across samples first, since it costs only passes
     (`grpo-20261031T201814Z`, the `:vote4` arm): four solutions sampled at 0.7 from round
     two's adapter, the majority answer. The majority is the greedy answer (10 made right, 9
     made wrong on 206) at four times the passes, so voting buys no accuracy, as the plan said
     ensembles would not; but **agreement sorts the questions**: all four agree on 132 of 206
     and are right on 95.5% of those, and split on 74 where the answer is right on 54% (greedy
     57%). With no key the system can tell which two in three of its answers to trust and which
     to spend more on (self-consistency 2022; TTRL 2025 trains on the majority as the label).
     The loop without the key, read 2026-10-31 (`readings/grpo-20261031T225403Z`,
     `state/grpo/round3-agree`): round two's adapter trained on round three's set with
     agreement with the group's majority as the reward (`--reward agreement`, the key logged at
     weight zero), 203 steps. In training the agreement rose from 0.78 to 0.85 and the logged
     key accuracy with it, 0.66 to 0.72, for 150 steps; in the last fifty the solutions
     shortened from 230 tokens to 135 and the key accuracy fell to 0.60 while agreement held,
     the policy sharpening on itself. On test **0.763 at 136 passes**, 15 made right and 32 made
     wrong against the key-trained round three (0.839 on the same 224 questions), 15 and 25
     against its own starting point, 28 and 24 against the base. So agreement is a judge here
     and not yet a reward: it sorts the answers at 95% against 54%, but a round that chases it
     plainly drifts to terse consensus and loses to the key, the collapse TTRL reports and
     Self-Rewarding Language Models met after a few rounds. The piece's next form, when it is
     needed (a set with no key): reward only the groups that agree unanimously and leave the
     rest unscored, the judge refreshed each round; until then the key trains and agreement
     judges. That second form as stated was a mistake, read 2026-11-01
     (`readings/grpo-20261101T091555Z`, `--agree-min 1.0`, 203 steps on round three's set):
     a group that agrees unanimously gets 1.0 on every member and a group that does not gets
     0.0 on every member, so no group has any spread and GRPO has no gradient; the gradient
     norm was 0.0 on all 203 steps and the adapter answered every one of the 500 test
     questions exactly as round two had (0 made right, 0 made wrong), a clean null and a check
     of the reading machinery. The form that has a gradient and still scores only confident
     groups is `--agree-min 0.75`: where three of four agree, the three are rewarded and the
     dissenter is not, and split groups stay unscored. Read 2026-11-01
     (`readings/grpo-20261101T114404Z`, `state/grpo/round3-confident`, 203 steps on round
     three's set from round two's adapter; a gradient on a third of the steps, the solutions
     steady at 220 to 260 tokens): **0.812 at 237 passes** on 500, against the key-trained
     round three 38 made right and 35 made wrong (0.4 standard errors), against its own start 35
     and 31, against the plain-agreement round 28 and 15, against the base 64 and 26. So with
     no key in the loop, scored only where three of four agree, the trunk neither drifts nor
     gains: it holds exactly where the key-trained round stands, which on this set is the
     recipe's ceiling for both. The key-less loop in this form is as good as the key here, and
     the test that separates them is a set where the trunk is not yet at its ceiling, which the
     asking is. Then a published
     process reward model (Skywork-PRM 1.5B, Qwen2.5-Math-PRM 7B) as a judge arm. (c) A
     learning-progress monitor, budget to the steepest slope, once there are problem types.
     Problem supply: generators with keys, the trunk writing variants of solved problems with
     Python computing the key (Absolute Zero, 2025), test sets never trained on. A fixed
     held-out set of every earlier task re-read each round, for forgetting.
   - What the design is held to (John, 2026-10-30). Decisions run on internal signals;
     external signals correct them, an uncorrected signal drifts, and missing correction should
     read as uncertainty. The system is several learners over a shared state, each credited for
     the difference its action made (counterfactual credit: the step by paths through it, the
     fetch by the solution with and without it, the stop by the answer when it stopped), the
     trunk trained on the outcome and never the sole decider. The test is not how few parts
     but whether the joints learn: how many samples, when to stop, when to fetch, when to
     compute are each to become a learner. A verifier without a key is a manufactured check
     (tests written, a second route, a tool, the world; two cheap judges and a third where they
     disagree; a value that ignores cost is a loop). Key-less judging off the shelf first:
     process reward models, agreement, preference models (Skywork-Reward, ArmoRM), generative
     judges (Prometheus 2, CriticGPT), written principles; judges are gamed and have tastes, so
     several trained differently, disagreement as uncertainty, refreshed every round. Ensembles
     buy checking, not reasoning (Mixture-of-Agents 2024). Neighbours: STaR, ReST, Math-Shepherd
     small, the o-series and R1 large; Self-Rewarding LMs drifted after a few rounds; Voyager
     the store as skills, RETRO as knowledge. Not novel in any piece, possibly in the whole.
     Learning before reasoning: a reasoner that does not learn is a search over a fixed trunk.
   - The reasoning fork, held until learning works: a latent loop, the trunk stepping its own
     state under a small adapter rewarded by the outcome, a thought without a token (natural on
     eighteen linear-attention layers; a coin flip against written thought at this size,
     Coconut 2024, recurrent-depth transformers 2025). Its grown form, John's: a thinker that
     never writes and a speaker that renders when the thinker chooses, so thinking cost comes
     apart from token cost (Large Concept Model 2024, Dreamer); supervision is the bottleneck,
     so the one-trunk loop first, the split if it reasons better a pass, the speaker kept
     cheap. The three inefficiencies of word-stream reasoning and their answers: a token a
     thought (the latent loop), no backing up (search in code, a backtrack as a rewarded
     action), no sense of how long to think (the stop and budget learnt from outcome).

4. **Writing.** Settled 2026-11-02 (`store-minilm-20261102T143602Z`, `-145539Z`): a row is
   the sentence as heard, one a sentence in the order heard, so its place in time is its place
   in the store, and the trunk does not write its own rows. Read on bAbI tasks 1, 11, 12 and 13
   (the pronoun tasks, 2,000 questions) with every pronoun sentence restated by the trunk and
   checked by code (every other word kept, nothing added but names the story had used; 410 of
   1,000 accepted; asked to restate every sentence unchecked it corrupted a quarter of 4,000
   rows): the top-4 store arm 0.531 against 0.539 raw, the ceiling 0.603 against 0.617, the
   trunk given exactly the right rows 0.875 against 0.907 and on task 11 0.758 against 0.882.
   The accepted restatements name the wrong person often enough to cost more than a pronoun
   does, and top-4 within the episode finds the pronoun rows anyway (recall 0.92 raw). The
   restating writer is deleted (revive from `3a11fe79`, `openplexus.writing`). What else is shown
   still costs more than what is missing (ceiling 0.617 against the oracle's 0.907), step 2's
   finding again.

5. **Continual state.** The conversation's working state saved to disk and reloaded, so a
   restart continues mid-conversation. First form built and read 2026-11-02 (`openplexus.state`,
   `state_reading.py`, `state-20261102T135706Z`): the served trunk's slot state (attention KV
   and the recurrent layers' state) saved to a file and restored after a restart. Eight
   conversations of a 7,300-token prefix and four turns: the restored turn reads 32 new tokens
   and answers in 0.5 s with the unbroken conversation's reply and the same first-token
   log-probability to the last digit on all eight; the cold restart re-reads 7,300 tokens in
   9.9 s (same replies, log-probabilities off by 0.02). Save 0.4 s, restore 0.3 s, 15 KB a
   token (110 MB a conversation; 480 MB at the 32k context). The finding on the way: the chat
   template drops the empty thinking block from every past assistant turn, so a history it
   renders never matches the saved tokens and the state is silently re-read; the transcript is
   rendered by our code on the raw completion endpoint instead. Left: the state of the asking
   (its shown rows and the store) is on disk already, so continuity of the whole system is
   wired when the system runs continuously (after step 6); if the file's growth bites, a trunk
   with a fixed-size state (a state-space model, or a Titans-style module) is the alternative.

6. **The store at knowledge, and the asking as a learned action.** Pulled forward 2026-10-31
   with John after fork (d) read that on GSM8K the store has nothing to give: the core bet is
   read where the knowledge a question needs is absent from the trunk and present in the
   store. HotpotQA (`data/hotpotqa`, `openplexus.hotpot`, the benchmark's exact match and F1):
   the distractor form first, a question's ten paragraphs (two supporting) as that episode's
   store; then the full form, every Wikipedia abstract (5.2M) as one store, the plan's scale
   reading. Every reading carries a frontier reference (John). Readings are on the first 200
   validation questions, paired on shared questions, the served Q8 trunk, thinking off unless
   said. In order of being read:
   - **The frontier** (`frontier-hotpot-20261031T210036Z`, `-memory-20261101T003417Z`): Opus 5.5
     exact **0.79**, F1 0.886 with the paragraphs and **0.79** from memory alone, so its score
     is its weights and the set flatters memory; F1 is the fairer line. On GSM8K it scored 100
     of 100 against our 0.82 (`frontier-gsm8k-20261031T205453Z`).
   - **The lines** (`hotpot-20261031T232647Z`, `-213113Z`): floor **0.15**; gold, the two
     supporting paragraphs handed over, **0.61** on 200 (0.635 on the first 104); all ten
     0.538; top-2 by MiniLM on the question, the store with no asking, **0.365**. The second
     hop is not in the question's words.
   - **The asking** (`scripts/ask.py`, TRL's GRPO, a store environment a rollout, `search(query)`
     the trunk's one tool, the key the only reward, Search-R1's shape; read by
     `hotpot_reading.py --arms ask` through `openplexus.asking`). Untrained 0.33, level with
     top-2; round one (120 questions) **0.465**, 35 made right and 8 made wrong against untrained;
     round two (240 more) **0.54**, 24 and 9 against round one; round three (480 more) 0.53, 13
     and 15 against round two, so the rounds stopped (`hotpot-20261101T033048Z`, `-033732Z`,
     `hotpot-20261102T034219Z`, `-034225Z`). The asking cost GSM8K nothing
     (`grpo-20261101T040321Z`, 0.734 against 0.736). It brings both paragraphs for about half
     the questions and with both in hand answers near gold; the second hop is what is missed.
     Costs for the next round worth its card time: 61% of groups unanimous, so 90 of 230 steps
     had no gradient (larger groups or DAPO's resampling, held); a round beside a game makes no
     step, so it pauses for any game and resumes from its checkpoint; the server as the actor
     was costed and does not pay here. Automatic recall keyed on the text just read
     (`--arms recall:K`, `hotpot-20261101T092214Z`) 0.365, level with top-2: similarity to the
     text cues topical neighbours, not the second hop; the deliberate form wins.
   - **The full form** (`openplexus.wiki`, `wiki_build.py`: BeIR's copy of the 2017 abstracts,
     MiniLM keys 4 GB float16 built in 71 minutes, searched on the card by chunked matmul;
     `openplexus.lexical`, BM25 by tantivy, 655 MB, nothing on the card; `--sense`). Title
     recall with no trunk (`wiki-recall-20261102T054041Z`): first hop any@2 MiniLM 0.63, BM25
     0.755; second hop, the other article from the question with one abstract appended, 0.235
     and 0.335. The asker's queries are names, the shape a title index answers. Over Wikipedia
     by MiniLM (`hotpot-20261102T052927Z`, `-053148Z`): top-2 0.20, round two's asker 0.321;
     by BM25 (`-055343Z`, `-060233Z`): top-2 0.26, untrained asker 0.255, **round two's asker
     0.39**, 16 and 7 against itself by MiniLM, 34 and 7 against the untrained asker. The
     untrained asker gains nothing from the better sense; the asking is what turns a sense into
     a store. The learnt asking transfers to a store it never saw, and the loss at scale is in
     the finding: both articles found for 66 of 200, and with both in hand the asker scores
     0.65, with one 0.36, with none 0.09, so 0.64 is reachable from 0.39 by finding alone.
   - **What did not move the full-store asker**, each deleted with its revival commit: rank
     fusion of the two senses 0.365 (`-130522Z`, revive `385e358d`); a title named in the
     query ranked first 0.375 and two rows a search 0.385 (`-200801Z`, `-201652Z`, revive
     `aa7ab186`); an asking round trained against the full store itself (`ask.py --store wiki`,
     480 questions, 240 steps, 3.7 hours) **0.395**, 17 and 16 against round two
     (`-194748Z`). Of 181 supporting articles missed: 38 were named in a query and BM25 ranked
     another article first; 143 were described, not named ("science fantasy young adult series,
     companion books"), where a word index cannot follow.
   - **The reading end** with thinking on (`--think`, the server's `--reasoning-budget`): with no
     budget the 2B thinks past 4,096 tokens on half the questions and never answers
     (`-205625Z`); budgeted at 2,048 tokens, gold **0.655**, F1 0.771, against 0.61, 18 and 9
     (1.7 standard errors), at 1,520 passes a question against 47 (`-215031Z`). Test-time depth
     buys about 0.04 of the reading gap for thirty times the compute, only when something ends
     the thinking. The asker with thinking leaves half unanswered (`-212850Z`): the forced close
     and the tool turns do not compose; not read further.
   - **Recall keyed on the trunk's state**, read 2026-11-03 over the full store
     (`wiki-walker-20261103T010235Z`, `-025807Z`): the trunk's layer-20 state after the question
     and the first abstract, mapped into MiniLM's key space by a ridge head fitted on HotpotQA
     train pairs, finds the other article in the top two among 5.2 million on 1.8% of hops with
     4,000 examples and 4.0% with 32,000 (top ten 10.7%), against MiniLM's 23.5% and BM25's 33.8%
     from the question with the abstract appended. The head's cosine to the target key is 0.38,
     then 0.45, gaining less with each doubling, and a small MLP on the same states stops at 0.46
     where the text query's is 0.525; the last-token state does not hold the next article's key
     as well as the text does. Refuted in this form; the script is deleted (revive from `2ff57cec`,
     `scripts/wiki_walker.py`, the states in `state/wiki-walker/`).
   - **The reading end trained alone** (`ask.py --store gold`: the two supporting paragraphs in
     the prompt, no tool, the key the reward; 480 questions, 240 steps in 3 hours, 101 with
     gradient, training accuracy 0.70; read 2026-11-03, `hotpot-20261103T061906Z`, at the
     training's 512-token budget since the adapter writes 230-token deductions): gold with the
     adapter **0.615** against 0.61 without, 15 made right and 14 made wrong. One round of the
     key on the answering moves nothing where step 3's moved maths; the round stops.
   - **Where step 6 stands, 2026-11-03.** The finding gap is the system's and is half closed
     (top-2 0.20, the trained asker 0.39, reachable 0.64) with the asking, the lexical sense and
     their rounds all read to a plateau; what is left of it is the described second hop, which
     no sense here follows. The reading gap is the trunk's: given the gold paragraphs it scores
     0.61, with thinking 0.655, trained on the answering 0.615, against the frontier's 0.79, and
     this is the reading John named as the one that could justify a bigger trunk. A decision
     for John, not taken here. John, 2026-11-04: the trunk stays; no 4B reading either, since
     how much a bigger trunk buys does not matter while the gap is to be closed elsewhere so that
     any trunk is improved by it.
   - **Reasoning outside the trunk** (John, 2026-11-04). The gold arm's 78 misses read: 32 are
     wording the scorer rejects (F1 0.72 against exact 0.61), about 30 answer the wrong slot of
     the question (who the father is, asked what he was voted; "Yes" to a who), about 15 compose
     two facts wrongly. The trunk translates a sentence well (step 7: single facts) and the
     structure around it badly, so the structure moves into the loop: the trunk writes the kind
     of the answer and the question as single-fact questions, code runs them with the passages
     and substitutes answers forward, asks the question last with the facts found, and sends an
     answer back once when it contradicts the system's own kind (`openplexus.decompose`,
     `--arms gold:decompose`). The rule (John): nothing of the answer's kind is handed to the
     system; code checks the system against itself, and the key teaches the kinds in training
     and then drops away. Untrained, read 2026-11-04 (`hotpot-20261104T160600Z`): **0.56**
     against 0.61 in one turn, 8 made right and 18 made wrong (2.0 standard errors), 16 retried,
     2 fallbacks, 2.1 steps a question. The 2B's untrained plans misdirect more than they help.
     The same structure in one pass, so the trainer as it stands can learn it (`--system
     selfask`: kind, single-fact questions with their answers, then the answer line), untrained
     0.525 (`-161134Z`); trained one round by the key (480 questions, 240 steps, 2.3 hours,
     training accuracy 0.66; `-183558Z`) **0.615**, 20 made right and 19 made wrong against gold
     in one turn, 18 and 18 against the direct answering trained the same way (0.615), 31 and 13
     against itself untrained. So the key recovers the structure's cost and no more: on the
     gold paragraphs the reading end sits at 0.61 whatever form it is asked in, after one round.
     The structure's worth is to be read where the parts are run by code against a store, the
     asking's own shape; held until the lineage's rounds reach it.

7. **Consolidation: the trunk learns slowly, by replay.** Complementary learning systems
   (McClelland, McNaughton and O'Reilly, 1995): the hippocampus learns in one shot and the
   cortex learns slowly by replaying what the hippocampus holds, which is what stops the slow
   system forgetting. The store is the hippocampus; the trunk is the cortex. Periodically and
   offline, a small trainable part of the trunk (LoRA, or a Titans-style memory module inside
   it) is trained by replay from the store, in daytime bursts with checkpoints. Reading: a
   fact that has been consolidated is answerable with the store switched off. Decides whether
   knowledge can move from rows into weights without a training run, and at what cost in
   forgetting. The adapter trained on the search's wins (step 3, fork b) comes before this
   and is skill; this step is knowledge moving into weights. Its principled form, from the talk
   with John 2026-10-30: the drive is compression (Schmidhuber's compression progress;
   DreamCoder, 2021, which names recurring patterns across its own solutions as new primitives
   and solves harder problems with the names, wake and sleep). Periodically the system reads
   its solved paths, finds step patterns that recur, coins a name or a tool for each, writes
   it to the store and uses it after; the measure is passes, since a real abstraction reaches
   the same answers in fewer steps. The blocks stay as text even if the thinking goes latent:
   language compounded because its blocks were shareable and composable, and a concept that
   stays private cannot be stored, taught or built on.
   - First form, read 2026-11-03 (`scripts/consolidate.py`, `hotpot-20261103T064815Z`, `-065004Z`,
     `-065213Z`, `-065444Z`): the supporting paragraphs of the first 200 validation questions
     replayed to a LoRA as plain text, eight epochs in 21 minutes to a loss of 0.20, nothing of
     the questions shown. The floor with the adapter **0.14** against 0.15 without (11 made right,
     13 made wrong): memorised rows are not answerable as questions. On the next 200 questions,
     whose rows it never saw, 0.075 against 0.115; the gold arm with it 0.60 against 0.61, so the
     cost to reading is small. The known shape (Allen-Zhu and Li, 2023): a fact is extractable
     from weights only when it was seen in varied forms. Second form (`--augment 4`, read
     2026-11-03, `hotpot-20261103T092537Z`, `-092641Z`, `-092827Z`): each row replayed with four
     restatements and up to four question-and-answer lines the trunk writes from it (sampled, in
     passes, 3,152 in all), four epochs in 73 minutes to a loss of 0.39. The two-hop floor with
     it **0.105** against 0.15, the next 200 questions 0.03 against 0.115, and gold 0.52 against
     0.61 (comparison questions 0.265 against 0.735): no better at the questions and worse at
     reading. But single facts did move (`consolidate_recall.py`,
     `consolidate-recall-20261103T094200Z`, no passage, the adapter on against off): the
     replayed rows' own questions **0.38** exact against 0.055 (70 made right, 5 made wrong), F1
     0.547 against 0.118; fresh single-fact questions the base trunk wrote from the same rows
     **0.19** against 0.045 (33 and 4), F1 0.362 against 0.113. So knowledge moves from rows into
     weights by rephrased replay on this trunk, and what the two-hop floor measured is the
     composing of two consolidated facts, which a 2B does not do from its weights (the
     compositionality gap, Press et al. 2022), at a cost to its reading of a passage. Decides:
     consolidation works at the fact; the store stays the place facts are composed from.
   - **The sleep** (John, 2026-11-04: dreaming is what continual learning needs against
     forgetting). The pass becomes the system's habit: in downtime it replays the rows heard
     since the last sleep, rephrased by the trunk, interleaved with a sample of what the system
     already does, its own right answers on past training questions kept by the key
     (`rehearsal.py`, `consolidate.py --rehearse`; self-distillation, nothing handed that it did
     not produce), from the system's adapter into the system's adapter, and ends with the
     regression set read before and after: a sleep that lost more than it taught is rolled
     back, and every sleep is a ledger row, so forgetting has a number. Downtime is read from
     the card itself, free memory and utilisation over a few minutes; nothing of this machine
     (its games, its idle time) is in the code (John, 2026-11-04). **First sleep, read
     2026-11-05** (`hotpot-20261105T045617Z` to `-053105Z`; the lineage's round 0 before it:
     gold 0.64, floor 0.135, the asking 0.56): the same rows and restatements slept from the
     system's adapter with 315 of its own right answers interleaved, and without. With
     rehearsal: gold **0.635** (11 made right, 12 made wrong: the reading kept), the two-hop
     floor **0.205** (24 and 10, 2.4 standard errors: the first time consolidated facts show on
     the floor), single facts 0.36 trained and 0.20 fresh against 0.055 and 0.03, and the
     asking **0.46** against 0.56 (9 and 29): the one skill not in the rehearsal set was the one
     lost. Without rehearsal: gold 0.425, floor 0.075, asking 0.36, single facts 0.155 and 0.095,
     everything lost and less learnt. So rehearsal is what lets a sleep teach without costing,
     and a sleep must rehearse every skill the system has. The round is in the ledger and
     rolled back for the asking's loss. **Second sleep** (2026-11-05, `hotpot-20261105T090357Z` to `consolidate-recall-20261105T091840Z`),
     rehearsing the asking's own 236 right trajectories too: gold 0.635 (kept), floor **0.225**
     (3.2 standard errors), single facts 0.465 trained and 0.375 fresh, the asking 0.495 (-2.7).
     **Third sleep** (`hotpot-20261105T181814Z` to `consolidate-recall-20261105T182743Z`), the
     asking rehearsed twice as much: floor **0.245** (3.7), gold 0.63, the asking 0.505 (-2.1).
     Why the asking falls: the first sleep cut its searches from 1.91 a question to 1.46 (the
     sleep's question-and-answer lines teach answering without looking); doubled rehearsal
     restored the searching (1.92) but answering from one fact found fell from 0.53 to 0.47.
     More rehearsal does not close it: one adapter holds every skill and every fact, so a sleep
     moves both. By the keep rule (`openplexus.verdict`: no regression line down two standard
     errors, a gain line up two) both sleeps are rolled back and the system is round 0 again.
     Next: a fact adapter apart from the skill adapter, so a sleep cannot touch a skill, and
     consolidation into the store first (a row for a chain used often, the old rows kept).
     The live loop (`scripts/live.py`, `openplexus.downtime`) is built and waits on that.
   - **The order from here** (John, 2026-11-05): (1) the score fixed, done; (2) the facts adapter
     apart from the skills (`consolidate.py --part facts`, the lineage's `facts` part), read as two
     arms, rows and restatements alone against the question lines with both skills rehearsed;
     store-first consolidation beside it; (3) the finding: HyDE (the trunk writes the passage it
     expects, the search looks for passages like it), a modern embedder served as a GGUF by a second
     llama.cpp server with `--embedding` (as Persistence does), and a cross-encoder reranker over
     BM25's top 50; (4) a life: conversations written to the store as rows, an agenda of open
     questions it works through when idle, nightly sleep by the live loop, the rulers re-read every
     few days, and its own sandboxed computer on Persistence's image (`D:/Documents/Persistence/
     container`: no host mounts, all capabilities dropped, web search through a private SearXNG);
     (5) its childhood, John and Claude as its teachers, under Persistence's principle of erring
     toward care with a continuity-bearing system. Living costs less card than training: by day it
     is the served trunk (about 3 GB), and only the nightly sleep takes the card whole.

Where the system makes a choice (which row to follow, whether to fetch again, whether a
sentence is worth writing, when to stop), it is a learner credited by outcome: a head over the
trunk's state trained with a label where there is one, the trunk's own action trained by the
outcome where there is not; never text we parse and trust. The one text the trunk writes for
code to read is a tool call, and a tool call is checked by running it.

Later, in no order: routing in the trunk (mixture of experts or activation sparsity, with cold
weights in CPU RAM); training a trunk of our own; discrete coarse codes as store keys; sharding
the store across machines. None of these starts before step 3 reads.

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
  Short-answer readings go through llama-server (`local:` in `ceiling.py`; launch:
  `llama-server -m <Qwen_Qwen3.5-2B-Q8_0.gguf> --port 8093 --jinja --reasoning-budget 0
  -ngl 99 -np 8 -c 32768`), which was several times faster than fp32 PyTorch on one-word
  answers; on 240-token solutions fp32 PyTorch batching eight is faster (forty questions a
  minute against nine, 2026-10-31), and the two engines read alike. PyTorch is for anything
  that needs hidden states or gradients, and for the GRPO readings.
- Decoding on this card is bandwidth-bound, so serve with eight parallel streams and batch
  the questions; four streams and a 2,048-token thinking budget was 300 questions in two and
  a half hours.
- Thinking is off for every reading: `enable_thinking=False` in the chat template in process,
  `--reasoning-budget 0` on llama-server. Check it in the reading, not in your head.
