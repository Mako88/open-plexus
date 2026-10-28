# Open Plexus

## The goal

Frontier-level on one GTX 1080 Ti. Frontier models are controlled by whoever can afford the
training run; a model of that worth on an old consumer card changes who gets to have one.

A frontier model does two things with one huge set of weights: it holds what it knows, and it
reasons by one chain of tokens with no backtracking, a huge evaluator doing a weak search.
The design here inverts both. Knowledge lives in a store on disk and is learnt by appending.
Reasoning is a small trained evaluator and search in ordinary code, the way AlphaGo beat
players no network without search could touch, and the way a small model with search over
its steps matched one fourteen times larger on maths (Snell et al., 2024). Arithmetic and
counting go to a tool, as they do at the frontier. The frontier claim rests on both
inversions holding, and the plan reads each on its own.

## The bet

A frontier model holds three things in one set of weights: skill at language, knowledge of
the world, and memory of what it was told a minute ago. Only the first needs dense
computation. The other two are stored in the weights at about two bits per parameter, and only
after a fact has been seen on the order of a thousand times in varied forms (Allen-Zhu and Li,
*Physics of Language Models 3.3*). That is why the models are huge and why they cannot learn a
fact without a training run. Keep the three apart:

- **The trunk.** A small pretrained language model, frozen. It reads and writes English and
  does what thinking a small model can. Taken off the shelf; nothing here trains it.
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
as the trunk's one pass. What learns for life is the store's contents and the glue. The bet
is in two halves. Knowledge: a small trunk reading a store approaches the same trunk with
the relevant text placed in front of it (the ceiling) on questions whose answer needs two
rows put together. Reasoning: a trained evaluator with search beats the same trunk's chain
of thought at the same number of forward passes, on problems the trunk cannot do in one
pass. The part that cannot be outsourced to code is the evaluator, and whether a small one
can be trained well from labels we can get is the question the whole design turns on.

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

2. **Reading across rows, by hops.** Tasks 2 and 3 with the supporting facts as separate
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
   So the shape is settled: propose, judge, stop, hand the trunk the chain. What is left is
   the choice at each hop, which both linear heads get right about half the time. Next is
   the fork the plan named: a gradient-trained head over the same hidden states and labels
   (a small MLP first, a LoRA on the trunk's query path if that is not enough), with
   checkpoint and resume, which has to beat 0.77/0.55 on task 2's hops and 0.46/0.55/0.39
   on task 3's at first place. Step 2 is struck when the chain reaches the oracle on task 2.
   A walker covers composition over facts and nothing else: arithmetic, counting and
   planning (bAbI tasks 7 and 19 first) are covered by nothing yet.

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
   forgetting. The trunk stays frozen until this step.

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
