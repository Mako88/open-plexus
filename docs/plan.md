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

Later measures, once tasks 1 to 3 are at the ceiling through the store: the other bAbI tasks;
fresh synthetic stories with invented names, longer than any context; and then real knowledge
at scale (a Wikipedia subset in the store), which is where "frontier knowledge on an 11 GB
card" gets read.

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
   store. HotpotQA (`data/hotpotqa`, 90k training and 7.4k validation questions, each answered
   from two Wikipedia paragraphs together, a short answer as the key; `openplexus.hotpot`, the
   benchmark's exact match and F1): first the distractor form, a question's ten paragraphs (two
   supporting, eight distractors) as the store's rows for that episode, bAbI's shape with real
   text; then the full form, the Wikipedia abstracts (5.2M, about 1.5 GB) as one store, the
   plan's scale reading, where the trunk is quantised and the budget read honestly. Every
   reading carries a frontier reference (John): a clean-context frontier agent on the same
   questions, as the target and as the check that a set is hard.
   - The frontier (`readings/frontier-hotpot-20261031T210036Z`, `-memory-20261101T003417Z`,
     `frontier-gsm8k-20261031T205453Z`): Opus 5.5 on the first 100 validation questions, exact
     match **0.79**, F1 0.886 with the ten paragraphs, and **0.79**, F1 0.882 from memory alone,
     so its score on this set is its weights and the passages add nothing: the store's claim is
     exactly that a 2B with a store stands where a frontier model stands on memory. The misses
     are wording (a fuller name, a date with the day), so F1 is the fairer line. On GSM8K the
     same agent scored 100 of 100 against our 0.82: GSM8K is solved at the frontier, HotpotQA
     is not.
   - The lines (`hotpot-20261031T232647Z`, the served Q8 trunk, no adapter, exact match on the
     first 200 validation questions, the arms stopped at 104 as clear of the floor): floor
     **0.15** (F1 0.206; bridge questions 0.06, comparison 0.59, mostly yes or no); gold, the
     two supporting paragraphs handed over, **0.635** (F1 0.745); episode, all ten, 0.538, the
     distractors costing a tenth as bAbI's whole story did; top-2 by MiniLM on the question,
     the store with no asking, **0.365** (F1 0.438). Top-2 found both supporting paragraphs for
     31 of 104 and the trunk then scored 0.71, above gold; one of the two for 65, scoring 0.22:
     the second hop is not in the question's words, it is in the first paragraph.
   - The asking (`scripts/ask.py`): TRL's GRPO with a store environment a rollout whose
     `search(query)` method is the trunk's one tool, the paragraph nearest the query by MiniLM,
     the key the only reward, nothing telling it when to ask (Search-R1, 2025). Read on the
     served trunk with the adapter as a GGUF LoRA (`hotpot_reading.py --arms ask`, the loop in
     `openplexus.asking`, four turns, no tool offered on the last so it always answers). The
     untrained asker (`hotpot-20261101T033048Z`): asks every time, 2.5 searches a question,
     finds both paragraphs for 140 of 200 where the question's words find 31 of 104, and scores
     **0.33** (F1 0.44), level with top-2, answering badly from what it found (0.41 with both in
     hand against gold's 0.635). **Asking round one** (`state/ask/round1`: 120 training
     questions, 60 steps, 2.5 hours on the shared card, a step 94 to 470 s since every tool
     turn re-reads the conversation): the trained asker (`hotpot-20261101T033732Z`) scores
     **0.465** exact, F1 0.568, **35 made right and 8 made wrong against the untrained asker**
     (4.1 standard errors), 23 and 9 against top-2, 7 and 21 against gold; it asks a little less
     (2.2 searches), finds both paragraphs about as often (122 of 200) and answers far better
     with them (0.61 against 0.41); comparison questions 0.735. The first reading of the core
     bet is positive: from the key alone the trunk learnt to ask the store and answer from what
     it found, and the store with a learnt asker beats the store without one. The regression
     check (`grpo-20261101T040321Z`): the asking adapter on GSM8K 0.734 against the plain
     trunk's 0.736, 17 and 18, so learning to ask cost the maths nothing. **Asking rounds two and three**
     (`state/ask/round2`, 240 more questions from round one's adapter, 120 steps, 51 minutes;
     `state/ask/round3`, 480 more from round two's, 240 steps, 3.9 hours on a card shared by day;
     both read on the first 200 validation questions, `hotpot-20261102T034219Z` and `-034225Z`):
     round two **0.54** exact, F1 0.628, 24 made right and 9 made wrong against round one (2.6
     standard errors), 54 and 12 against the untrained asker; round three **0.53**, F1 0.609,
     **13 and 15 against round two**, 18 and 5 against round one, 47 and 7 against the untrained
     asker, 26 and 5 against top-2. So the rounds stop here, as the loop's rule says: round three
     did not widen the gap. The asking is functional, 0.33 to 0.54 from the key alone, and the
     gap left is to gold (0.635) and the frontier (0.79). Where it sits: the asker searches
     twice a question and brings both supporting paragraphs for about half the questions (1.49
     of 2 on average, as rounds one and two did), and with both in hand it answers near gold
     (0.61 in round one); the second hop is what is still missed, not the answering. Two costs
     of the loop as run, for the next time a round is worth its card time: 61% of round three's
     groups were unanimous (`frac_reward_zero_std`), so 90 of 230 steps had no gradient (two
     questions a step, four tries each; larger groups or DAPO's resampling of unanimous
     questions buy signal at time per step); and a round beside a game makes no step at all, so
     it pauses for any game and resumes from its checkpoint (`--save-steps 20`). A tool call
     whose arguments are not JSON is refused by the server; the reading counts that question as
     unanswered rather than dying. The server as the actor was costed 2026-11-01 and does not
     pay on this card: the trainer's own path already runs the eight rollouts' tool turns
     batched, and the cost is each turn re-reading the grown conversation in fp32; the server
     would spare that with its cache but decodes at about 70 tokens a second across its slots
     against fp32 PyTorch's 176 batched, so a step would come out no faster, and the trainer's
     contract for a custom rollout treats the samples as on-policy anyway (`rollout_func`,
     `env_mask`, the sampler's logprobs unused without vLLM). The levers that remain are fewer
     and shorter turns; held unless a round's speed becomes the bottleneck.
   - Beside the asking, John's other form: automatic recall, as memory does, nothing asked
     for. Top-2 is its first version, keyed on the question; the second (`--arms recall:K`,
     `openplexus.hotpot.recall`, built 2026-11-01) is keyed on the state as the trunk reads,
     the paragraph nearest the question first and then the one nearest that paragraph's text,
     the first cueing the second. Read 2026-11-01 (`hotpot-20261101T092214Z`, `recall:2`): **0.365**
     exact, F1 0.425 on 200, the same as top-2 to the question (8 made right, 8 made wrong on
     the shared 104); it found both supporting paragraphs for 56 of 200 where top-2 found 31 of
     104, no better, since the paragraph's text cues its topical neighbours as the question's
     words do; against the round-two asker 6 and 25. So automatic recall by similarity, keyed
     on the question or on what was just read, does not find the second hop on this trunk's
     embedder, and the asking does: the deliberate form wins here. Recall keyed on the trunk's
     own state while it reads, rather than on the text, is the form left to try; the asking has
     plateaued (round three, 2026-11-01), so it is no longer held, and it comes after the full
     form below, since it needs the trunk in process and the full form is the scale reading the
     step is for. The key-less learner stays a live line, not a key-only loop; its confident
     form held the key's level on maths (step 3), and its next reading is on a set that is still
     climbing, which the asking on the distractor form is not.

   - The full form, read 2026-11-01 (`openplexus.wiki`, `scripts/wiki_build.py`): every abstract
     HotpotQA was built over, 5,233,329 of them (BeIR's copy of the 2017 dump, 976 MB parquet in
     `data/wiki-abstracts/`, since Stanford's host is unreachable from here), keyed by MiniLM as
     the ten paragraphs are, 4 GB of float16 keys built in 71 minutes on the card, shard by shard
     with a progress file so a build paused for a game resumes; searched on the card (3.8 GB) by
     a chunked float32 matmul, five queries in three seconds. The arms `wiki:top:K` and `wiki:ask`
     are the distractor form's over this store, gold counted by title, each paired with its twin
     on the same questions (`hotpot-20261102T052927Z`, `-053148Z`): top-2 over Wikipedia **0.20**
     (F1 0.251; 6 made right and 18 made wrong against top-2 on the ten, 0.365), both supporting
     articles found for 12 of 200 and one for 114; the untrained asker over Wikipedia 0.205 (2.7
     searches, both found for 29 of 112); **round two's asker over Wikipedia 0.321**, F1 0.406, 15
     made right and 2 made wrong against the untrained asker (3.2 standard errors), 18 and 10
     against top-2 over Wikipedia, **2 and 28 against itself on the ten-paragraph store** (0.554
     there), level with top-2 on the ten (15 and 19). With both articles in hand it scores 0.62
     (gold is 0.635), with one 0.38, with none 0.03: the loss at scale is in the finding, not the
     answering. So the learnt asking transfers to a store it never saw (its gain over the
     untrained asker holds), and the store's sense is what fails at scale: a 384-d sentence key
     over 5.2 million abstracts finds a supporting article on fewer than half the searches,
     where on the ten it found one on most. Floor 0.15, frontier 0.79; the gap to close is 0.47.
   - Next in the step: the first hop is a name in the question found among 5.2 million titles,
     where a sentence key is weak and a lexical index is strong, and the multi-hop retrieval line
     (MDR 2020, IRCoT 2023) retrieves iteratively from a lexical or trained-dense first hop, which
     is the asking with a better sense. Built and read 2026-11-01 (`openplexus.lexical`, BM25
     over title and text by tantivy, two minutes to build, 655 MB on disk, nothing on the card;
     `wiki-recall-20261102T054041Z`, 200 questions, every gold pair in the store, no trunk): the
     first hop from the question alone, a supporting article in the top two, MiniLM **0.63**,
     BM25 **0.755**, their rank fusion 0.775 (both articles there: 0.055, 0.14, 0.10); the
     second hop with one supporting abstract appended to the question, the other article in the
     top two, 0.235, 0.335, 0.307 (in the top ten: 0.385, 0.568, 0.578). So the lexical sense
     finds more at both hops on this store, and the asker's queries are names ("Scott Derrickson
     nationality"), the shape a title index answers where a sentence key returned "Bryan
     Smithson". The asker's search with each sense (`--sense`, read 2026-11-02,
     `hotpot-20261102T055343Z`, `-060233Z`, `-130522Z`, 200 questions): by BM25 the plain
     trunk with the two nearest abstracts 0.26 (0.20 by MiniLM), the untrained asker 0.255
     (0.205), and **round two's asker 0.39**, F1 0.471, 16 made right and 7 made wrong against
     itself by MiniLM (1.9 standard errors), 34 and 7 against the untrained asker by BM25, 34 and
     8 against top-2 by BM25, level with top-2 on the ten paragraphs (19 and 17), 4 and 34
     against itself on the ten (0.54). It brings both articles for 66 of 200 (dense: 24 of 112)
     and scores 0.65 with both, 0.36 with one, 0.09 with none, so the finding is still where
     the loss is: by BM25 the gold article is found on about half the searches. The untrained
     asker gains nothing from the better sense (0.255 against top-2's 0.26 by BM25) where the
     trained one gains 0.13: the asking is what turns a sense into a store. The rank fusion of
     the two senses, which won title recall at the first hop, loses as the asker's tool: 0.365,
     15 made right and 20 made wrong against BM25 alone, both articles for 60 of 200 against 66,
     so it is deleted (revive from `385e358d`, `fused` in `openplexus.lexical`) and BM25 is the
     default sense. The asking round on the full store (`ask.py --store wiki`, 480 questions
     from round two's adapter, 240 steps in 3.7 hours, half the groups uniform, training
     accuracy 0.33 in the first half and 0.39 in the second; read 2026-11-02,
     `hotpot-20261102T194748Z`) scores **0.395**, 17 made right and 16 made wrong against
     round two on the same store, both articles found for 64 of 200 against 66: training
     against the store it searches moves nothing, so rounds stop here too. Where the finding
     fails (181 missing supporting articles on 200 questions): for 38 the title's words were
     in a query and BM25 ranked another article first ("Ed Wood nationality" gave "Ed Wood
     filmography", "Androscoggin Bank Colisée seating capacity" gave "Seating capacity"); for
     143 no query named the title, the asker describing what it wanted ("science fantasy
     young adult series, first person, companion books") where a word index cannot follow.
     Both cheap fixes read level and are deleted (revive from `aa7ab186`): a title named in the
     query ranked first (`hotpot-20261102T200801Z`) 0.375, 14 made right and 17 made wrong
     against plain BM25, both articles found for 71 of 200 against 66; two rows a search
     (`-201652Z`) 0.385, 11 and 12, both found for 70. More finding did not become more
     answering, and the asker given both articles scores 0.66, so the full-store asker sits at
     0.39 against a reachable 0.64 with the finding at a third of questions. Next: recall keyed
     on the trunk's state for the described hop, and before it the reading end (the ceiling
     with thinking on), since that is the other half of the gap.

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
