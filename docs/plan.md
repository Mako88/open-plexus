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
   thinking. The composing is therefore the glue's job, by hops (step 2), and the trunk is
   judged on reading one fact from rows in front of it. **The trunk is Qwen3.5-2B**, the
   largest that trains adapters in fp32 on the card; the 4B (0.92 against 0.76 on task 1,
   serving only, since fp16 training is not an option on Pascal) is the step up if the
   2B's one-fact ceiling bounds step 1. Open fork: a small model with a reasoning mode
   that terminates (Phi-4-mini-reasoning, SmolLM3) as a second trunk family, if hops
   need the trunk to reason within a hop. The scorer is checked by `tests/test_scorer.py`
   and the floors are near 0, so the wiring is not in doubt.

1. **The store, with no training.** Rows are a story's sentences as heard, each with a vector
   (a small embedding model, or the trunk's own pooled hidden state; try both) in an on-disk
   index. Brute-force numpy first; an ANN index only when it is the bottleneck. Hear N
   stories; after each, ask its questions with the top-k rows placed in the trunk's context.
   Also ask early stories' questions late, with thousands of rows in between. Save the store,
   restart the process, load it, and the answers must be identical. Decides whether a store
   that is appended to counts as memory, and what a row costs.

2. **Reading across rows, by hops.** Tasks 2 and 3 with the supporting facts as separate
   rows among thousands of others. Step 0 showed the trunk cannot do a two-fact question in
   one pass at any size we can run, so the glue turns it into one-fact questions: retrieve,
   read one fact, re-query with what was read, retrieve again. First as a loop with no
   training, the re-query being the trunk's own hidden state after reading. Then a trained
   query: LoRA on the trunk's query path or a small trained retriever, trained on the train
   split's supporting line numbers (every row carries them), with checkpoint and resume.
   The first trained query is a linear head over the hidden state fitted in closed form by
   least squares, which takes seconds and needs no training loop; gradient training comes
   in only when that head is beaten by a nonlinear one or by a LoRA on the trunk.
   Decides whether composing over the store works when each hop is a one-fact read, and
   whether training the glue buys anything over the loop. This is the step the bet lives or
   dies on, and the ceiling it is read against is the trunk's one-fact score, not its
   two-fact one. If the trained walker does not reach that ceiling on task 2, the wall is
   the trunk's representation of what it read, not its reasoning, and the core has to be
   rethought at that level. A walker covers composition over facts and nothing else:
   arithmetic, counting and planning (bAbI tasks 7 and 19 first) are covered by nothing
   yet, and are where a different reasoning mechanism would be needed.

3. **The evaluator and search.** A second head over the trunk's hidden state: given a
   candidate step, is it good. Labels first from bAbI, where a hop that lands on a
   supporting line is good and any other is not, so the walker's own training data
   serves; then from step-labelled maths (PRM800K, 800k human labels on steps of MATH
   solutions) and bAbI's path finding (task 19), which is planning in miniature. As in
   step 2, the first evaluator is linear and fitted in closed form, and the gradient-trained
   one has to beat it. Search in
   code: beam over hops first, then a tree with backtracking, budgeted in forward passes.
   Counting and arithmetic go to a tool: an aggregation over rows for the first, a Python
   call the trunk writes for the second, read on bAbI task 7 and on GSM8K. Decides whether
   a small evaluator with search beats the trunk's own chain of thought at matched forward
   passes. This is the step the frontier claim lives or dies on. It fails if search with
   the trained evaluator is no better than chain of thought at the same budget on a problem
   the 12B with thinking gets right; that would mean the evaluator, not the search, is the
   wall, and the next question is whether a bigger label set or a LoRA under the head moves
   it. Training here is in daytime bursts with checkpoints, as everything is.

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
  Inference-only readings go through llama-server (`local:` in `ceiling.py`), which is
  several times faster on this card than fp32 PyTorch, whose Qwen3.5 linear-attention
  layers run on fallback kernels here. PyTorch is for anything that needs hidden states or
  gradients.
- Decoding on this card is bandwidth-bound, so serve with eight parallel streams and batch
  the questions; four streams and a 2,048-token thinking budget was 300 questions in two and
  a half hours.
- Thinking is off for every reading: `enable_thinking=False` in the chat template in process,
  `--reasoning-budget 0` on llama-server. Check it in the reading, not in your head.
