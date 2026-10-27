# Open Plexus

## The goal

Something that approaches a frontier model and runs on one GTX 1080 Ti. Frontier models are
controlled by whoever can afford the training run; a model of that worth on an old consumer
card changes who gets to have one. Everyone is looking for this, and the bet below is a
specific place to look, with numbers behind it.

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
- **The glue.** A small trained piece between them. At each step it asks the store what is
  relevant and hands that to the trunk, and it decides what is worth writing. This is the only
  thing we train, and it is trained once.

What learns for life is the store's contents. The bet is that a small trunk reading a store
can reason as well as a big model reasoning over what it has memorised. The bet fails if a
trunk reading a store never approaches the same trunk with the relevant text placed in front
of it (the ceiling), on questions whose answer needs two rows put together.

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

0. **The trunk: the smallest model that composes in context.** `scripts/ceiling.py` gives a
   model the whole story and the question, thinking off, greedy, and scores tasks 1 to 3.
   Run Qwen3.5-0.8B, 2B and 4B (all in the Hugging Face cache). The trunk is the smallest
   that is near the ceiling of a much larger model on tasks 2 and 3, since a trunk that
   cannot put two facts together with both in front of it cannot do it from a store either.
   The earlier 0.8B reading (0.45 on task 1, 0.05 on task 2) was taken with another world's
   system prompt and an unverified thinking switch and is not trusted. Decides the trunk.

1. **The store, with no training.** Rows are a story's sentences as heard, each with a vector
   (a small embedding model, or the trunk's own pooled hidden state; try both) in an on-disk
   index. Brute-force numpy first; an ANN index only when it is the bottleneck. Hear N
   stories; after each, ask its questions with the top-k rows placed in the trunk's context.
   Also ask early stories' questions late, with thousands of rows in between. Save the store,
   restart the process, load it, and the answers must be identical. Decides whether a store
   that is appended to counts as memory, and what a row costs.

2. **Reading across rows.** Tasks 2 and 3 with the supporting facts as separate rows among
   thousands of others. First a loop with no training: retrieve, read, re-query with what was
   read, retrieve again. Then a trained query: LoRA on the trunk's query path or a small
   trained retriever, trained on bAbI's train split with checkpoint and resume. Decides whether
   composing over the store works, and whether training the glue buys anything over the loop.
   This is the step the bet lives or dies on.

3. **Writing.** What goes in a row: the raw sentence, or the trunk's own restatement with
   names resolved ("John is in the hallway", not "he went there"), one fact a row. Rows carry
   time and are never deleted; the reader prefers the latest. Decides the row format and
   whether the trunk should write its own rows.

4. **Continual state.** The conversation's working state saved to disk and reloaded, so a
   restart continues mid-conversation: the KV cache of the prefix first. If its growth bites,
   a trunk with a fixed-size state (a state-space model, or a Titans-style module) is the
   alternative. Decides how state persists.

5. **Scale and speed.** A real knowledge store (a Wikipedia subset), the trunk quantised to
   int8 or 4-bit, and the budget read honestly: tokens per second, VRAM, disk. Decides whether
   this reads as frontier knowledge on an 11 GB card, and what is left.

Later, in no order: routing in the trunk (mixture of experts or activation sparsity, with cold
weights in CPU RAM); training a trunk of our own; discrete coarse codes as store keys; sharding
the store across machines. None of these starts before step 2 has a reading.

## Setup

- Python 3.12 in `.venv` via `uv sync`. Torch comes from the cu126 index because cu128 and
  later dropped Pascal; `tests/test_toolchain.py` checks it.
- Models live in the Hugging Face cache (`~/.cache/huggingface/hub`). Qwen3.5-0.8B, 2B and 4B
  are there as safetensors for in-process use; llama.cpp is built at
  `D:\tools\llama.cpp-src\build\bin\Release` for serving quantised models later.
- Thinking is off for every reading: `enable_thinking=False` in the chat template in process,
  `--reasoning-budget 0` on llama-server. Check it in the reading, not in your head.
