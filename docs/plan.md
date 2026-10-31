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
   - The loop that runs now (`scripts/grpo.py`, `scripts/grpo_reading.py`), decided with John
     2026-10-30: TRL's GRPO on the trunk's non-thinking whole solutions, four a problem, the
     key as the reward plus 0.1 for an answer line, LoRA r16 on the attention and MLP
     projections, fp32, a checkpoint every twenty steps, the thinking mode behind a flag. The
     smoke: 20 to 25 s an optimiser step of two problems and eight solutions, 8.7 GiB peak, so
     a thousand problems is about three hours here. The control
     (`readings/grpo-20261030T111215Z`): the plain trunk's greedy whole solution under
     training's prompt, GSM8K test, **0.736 at 237 passes**, every question answered, between
     the step form's 0.364 at 102 and the thinking's 0.85 at 1,950. **Round one**
     (`readings/grpo-20261031T042520Z`, `state/grpo/round1`): a thousand training problems,
     500 optimiser steps in 2.8 hours of card time across two pauses for Ember, peak 10.1 GiB.
     The sampled training accuracy rose from 0.59 in the first fifty steps to 0.75 by step 200
     and held; solutions cut at the 400-token cap fell from 0.29 to 0.07 as it learnt to
     finish. Its adapter, greedy on the same 500 test problems: **0.774 at 246 passes**
     against 0.736 at 237, 50 problems made right and 31 made wrong, a paired gap of 0.038 at
     2.1 standard errors. So the trunk learns to reason from its own checkable outcomes on
     this card, modestly, in one round: the loop runs. Two lessons. The early stop against the
     unpaired reference fired at 232 problems reading 0.802, on a prefix the control also
     found easier (0.754), so an arm read against a control on the same questions is stopped
     by the paired count of flips, not the unpaired gap. And the gain is at the same cost, not
     cheaper: the loop made the trunk right more often, not briefer. **Round two**
     (`readings/grpo-20261031T093424Z`, `state/grpo/round2`), the same adapter trained on over
     the next thousand training problems (`--skip 1000 --init-adapter`), 4.7 hours, peak 10.4
     GiB: **0.804 at 227 passes**, against round one 49 made right and 34 made wrong (a gap of
     0.030 at 1.6 standard errors) and against the base 63 and 29 (**0.068 at 3.5**). So the gap
     widens round on round, and this round the solutions also got shorter (227 passes against
     246); the training accuracy reached 0.83 in its last blocks, and in six groups of ten all
     four samples scored alike and taught nothing, so round three wants harder problems or more
     samples a group. Rounds continue while each widens the gap; the recipe's ceiling at this
     size is the round that does not. The R1 paper found this loop on small models lost to distilling a large model's reasoning
     into them, so a frontier teacher at training time is fair by the standard, and the trunk
     should start as the best distilled reasoner that fits fp32 in process (Qwen3.5-2B is one;
     a comparison is an afternoon's reading).
   - Next, in order. (d) The store feeding the thinking (`openplexus.fetch`, the `:fetch` arm
     of `grpo_reading.py`, built, not run): the system's own solved problems (its right whole
     solutions on the training problems, `--split train`) indexed by MiniLM, first by the
     question (the nearest solved problem shown before the trunk solves, never a question's
     own), then John's form keyed on the thinking: at each step fetch the solved situations
     most like the steps so far and show what the next step was there; read against the same
     arm without it, since a trunk fed its own past can set in its ways. Then the asking as
     the trunk's learned action, credited by whether the fetch changed the outcome: the core
     bet. A published process reward model (Skywork-PRM 1.5B, Qwen2.5-Math-PRM 7B) as a judge
     arm, and agreement across samples as the first key-less signal, since it costs only
     passes. (c) A learning-progress monitor, budget to the steepest slope, once there are
     problem types. Problem supply: generators with keys, the trunk writing variants of solved
     problems with Python computing the key (Absolute Zero, 2025), test sets never trained on.
     A fixed held-out set of every earlier task re-read each round, for forgetting.
   - What the design is held to, from the talks with John 2026-10-30. Every decision runs on
     internal signals; external signals correct them, a signal no longer corrected drifts, and
     the absence of correction should read as uncertainty, not freedom. The system is several
     learners over a shared state, each credited for the difference its action made (the step
     by paths through it, the fetch by the solution with and without it, the stop by the
     answer when it stopped), the trunk trained on the final outcome and not made the sole
     decider, since every choice would then be capped by a 2B's text policy. The test is not
     how few parts there are but whether the joints learn: how many samples, when to stop,
     when to fetch, when to compute are where learning stops today, and each is to become a
     learner. A verifier without a key is a manufactured check: tests written, a second route,
     a tool, the world; two cheap judges and a third where they disagree; a value that does
     not count cost is a loop. Off the shelf first for key-less judging: process reward
     models, agreement, preference reward models (Skywork-Reward, ArmoRM), generative judges
     (Prometheus 2, CriticGPT), written principles; judges are gamed and have tastes (length,
     position, their own writing), so several trained differently, disagreement as
     uncertainty, refreshed every round. Ensembles of small models buy checking and
     calibration (Mixture-of-Agents, 2024), not reasoning capacity. Neighbours: STaR, ReST
     and Math-Shepherd are this loop small, the o-series and R1 large; Self-Rewarding Language
     Models (values as the key) drifted after a few rounds; Voyager is the store as skills,
     RETRO as knowledge. Not novel in any piece; possibly in the whole: one card, the store as
     the knowledge, the trunk a learner, the system supplying its own problems and checks.
     Order: learning before reasoning, since a reasoner that does not learn is a search over a
     fixed trunk, which is what the step form read as.
   - The reasoning fork, held until learning works: a latent loop, the trunk stepping its own
     state under a small trained adapter with the outcome as the reward, a thought without a
     token; natural on a trunk whose eighteen linear-attention layers carry a state, and a
     coin flip against written thought at this size (Coconut 2024, recurrent-depth
     transformers 2025). Its grown form, John's: a thinker that moves through an internal
     state and never writes, and a speaker that renders it when the thinker chooses, so
     thinking cost comes apart from token cost and speech is an action (Large Concept Model
     2024; Dreamer). The bottleneck is supervision: a model that never writes has only its own
     future states and rewards through the speaker to learn from, so the one-trunk latent loop
     comes first and the split if it reasons better a pass than words do; the speaker stays
     cheap, since a thinker that does not speak cannot be read. The three inefficiencies of
     word-stream reasoning and their answers: every thought through a token (the latent loop),
     no backing up (search in code, a backtrack as a rewarded action), no sense of how long to
     think (the stop and budget learnt from outcome, as R1's models learnt to think longer only
     where it paid).

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
