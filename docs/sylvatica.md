# Sylvatica

*Myosotis sylvatica* is the wood forget-me-not. Current models, depending on how you frame
it, either cannot forget or always forget. This branch is the one that is meant to do both
on purpose.

It REPLACES `commitments` and `csharp`. Both stay in history, and their refutation tables
still hold for what they measured: a gradient-free learner counting under a prediction lost to
a rule that never looked at the house. Read them before repeating anything they tried.

## The complaints (John's, 2026-09-06)

The reasons a current LLM is the wrong shape. Each is a requirement here.

1. **It cannot forget**, so everything in the context weighs the same forever.
2. **It is event-driven.** Every call re-sends the whole context. It should hold state and take
   what is new.
3. **Training then inference.** It should learn continually and change.
4. **Told once should stick**, across a restart, without being in the prompt.
5. **Time is cheap and words are not.** Reasoning already takes seconds; verbosity is the cost.
6. **It knows everything.** It should know how to talk and use tools and learn or fetch the rest.
   Specialists where depth is wanted.
7. **It is sequential and centralised.** The rest of computing scaled by going parallel and
   distributed over many weak machines; this should too.

## Decided

- **Gradient-based neural nets are the substrate.** John's, on recommendation. The novelty is
  in the memory, the learning regime and the distribution, never in replacing gradients.
- **Persistence is the layer-on-top version and stays as prior art.** It works and it is
  expensive for the reason complaint 2 names. Its store model, typed fragments with importance,
  confidence, provenance and archive-over-erase, is what the memory here should learn from.
- **Forgetting and continual learning are ONE problem.** Training is split from inference because
  updating weights on the new overwrites the old. The known answer is a fast episodic store plus
  slow consolidation with replay, and that is complaint 1's "forget to disk" seen from the other
  side.

## The shape (proposed, nothing built)

1. **A small recurrent core**, RWKV-7 class. State rather than context. The state is fixed-size,
   so compression is forced and forgetting is structural rather than a policy bolted on.
2. **An external memory**, sharded across nodes, read asynchronously as a tool call. What leaves
   the state lands here. SQLite per node to begin.
3. **Two speeds of learning.** Fast is the recurrent state itself; RWKV-7's state update is a
   delta rule, which is test-time learning already. Slow is an adapter trained locally from replay
   out of the store and consolidated into the core on a schedule. What consolidation has absorbed
   leaves the store's hot tier. That is the forgetting.
4. **Idle time is consolidation time.** A stateful model runs with no input. A transformer cannot.
5. **Specialists are separate cores on one memory fabric**, never one model that knows everything.
6. **Knowledge and learning distribute; per-token compute does not.** A dense network needs
   all-to-all traffic at every layer, so internet latency multiplies by depth. Petals and hivemind
   measured seconds a token. Retrieval, replay and consolidation tolerate latency, so they are
   what goes on the phones. Constraints C1 to C4 carry over unchanged.

## What would refute it

Named before anything runs, per the standing rule.

- A recurrent core with the store loses to a same-size stateless model given the whole
  conversation as context, on the same exam. Then state bought nothing.
- Adapter updates lose the core's baseline abilities faster than they add. Then replay does not
  solve forgetting at this scale.
- Consolidation costs more compute than re-sending the context would have. Then the workaround
  was cheaper than the fix.

## The first north star

Carried from before, with ONE line added. A machine that holds a basic conversation in English,
is told a block, and answers on it. **And is told something today and knows it tomorrow after a
restart, with nothing about it in the prompt.** Persistence meets that line only by re-sending.
The old first north star did not have it.

## Hardware

One GTX 1080 Ti, 11 GB, Pascal. fp32 is fine; fp16 is slow and there is no bf16. In reach:
running RWKV to about 3B in fp16 or 7B quantised; adapter-tuning to about 1.5B; pretraining
something in the 100M to 400M range from scratch, slowly. Check the current PyTorch wheels still
build for sm_61 before anything else, since Pascal support has been dropping out of recent CUDA
builds.

## Open forks

- **Which core.** Pure RWKV-7 against a hybrid that keeps a little full attention (Qwen3-Next,
  Nemotron-H shape). Hybrids recover the exact recall a pure recurrence loses, and the store may
  or may not make that moot.
- **Where the fast/slow line sits.** An adapter per node, or one shared and merged.
- **The store schema.** Carry Persistence's fragment model or start smaller and let it grow.
- **The old guard culture.** OutstandingTests as the red set, PushbackTests as standing
  objections, the doc cap. Recommend carrying all three.
