# Sylvatica

*Myosotis sylvatica* is the wood forget-me-not. Current models, depending on how you frame it,
either cannot forget or always forget. This branch is the one meant to do both on purpose.

It REPLACES `commitments` and `csharp`. Both stay in history under `docs/history/`, and their
refutation tables still hold for what they measured: a gradient-free learner counting under a
prediction lost to a rule that never looked at the house. Read them before repeating anything
they tried. This branch keeps their culture and their constraints and drops their substrate.

This is the ONE design doc. It is written to be implemented by an agent working alone for long
stretches, so it says what to build, in what order, what each phase must show before the next
begins, and what reading would refute each bet. It says how only where a how was decided.

---

## THE ORDER

The one list a session edits at both ends. Strike what got done; write the handoff from what is
left. Each phase has an exit that is a measurement, never a feeling.

- ~~**Phase 0 — Ground.**~~ Struck 2026-09-06. Exit met: `readings/phase0-cost-*.json` and
  `readings/phase0-restart-*.json`. One item of it is NOT done and is not Phase 0's — the old
  C# tree is still in the working copy; see the handoff.
- **Phase 1 — Exam.** Build the told-then-asked worlds and the cost meter. Take the FIRST
  reading: how fast the bare state forgets, against the full-context baseline.
- **Phase 2 — Store.** Every turn to SQLite, hybrid retrieval, automatic injection. Tier B exam.
- **Phase 3 — Consolidation.** Replay out of the store into a LoRA adapter, regression gate,
  rollback, merge. Tier C exam. This is the bet; everything before it is scaffolding.
- **Phase 4 — Idle.** The machine runs with no input. Reflection fragments, hot and cold tiers,
  the forgetting policy.
- **Phase 5 — Fleet.** Many nodes on one box, sharded store, deadlines, nodes vanishing. Then
  phones, and not before.
- **Phase 6 — Specialists and tools.** A second core on the same memory fabric; retrieval and
  actions as calls the model makes rather than the harness.

---

## THE COMPLAINTS

John's, 2026-09-06. The reasons a current LLM is the wrong shape. Each is a requirement here,
and every phase should be able to say which of these it serves.

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

---

## DECIDED

Settled on 2026-09-06 between John and the designing session. A decision here is not an arm.
Reopening one is a conversation with John, never a commit.

- **Gradient-based neural nets are the substrate.** John's, on recommendation. The novelty is in
  the memory, the learning regime and the distribution, never in replacing gradients. The two
  earlier branches tried that and lost to a blind rule.
- **The core is a pretrained recurrent language model, RWKV-7 class.** State rather than
  context. The state is fixed-size, so compression is forced and forgetting is structural rather
  than a policy bolted on. Nothing here trains a core from scratch; the core is a part that can
  be swapped for a bigger one, and the value built is everything around it.
- **Forgetting and continual learning are ONE problem.** Training is split from inference because
  updating weights on the new overwrites the old. The known answer is a fast episodic store plus
  slow consolidation with replay. Complaint 1's "forget to disk" is that answer seen from the
  other side, so one mechanism serves complaints 1, 3 and 4.
- **Knowledge and learning distribute; per-token compute does not.** A dense network needs
  all-to-all traffic at every layer, so internet latency multiplies by depth; Petals and hivemind
  measured seconds a token. Retrieval, replay and consolidation tolerate latency, so they are
  what goes on the phones. Complaint 7 is served by where the STORE lives, not by splitting a
  forward pass.
- **Python, one codebase, until Phase 5 shows the seam.** The model and the training loop have
  no serious alternative to PyTorch, and a two-language prototype is two prototypes. The node
  layer is written behind a protocol so that it can be rewritten in C# later, which is John's
  side, if the fleet phase wants it.
- **Persistence (`D:\Documents\Persistence`) is the layer-on-top version and stays as prior
  art.** It works and is expensive for the reason complaint 2 names. Its store model, typed
  fragments with importance, confidence, provenance and archive-over-erase, is what the store
  here inherits in spirit. Do not port its code.
- **The culture carries over.** One doc; findings in commits and tests; a red set of tests that
  fail until the owed work is done; a pushback file of standing objections; say what would refute
  an arm before running it; correct the record in one sentence and move on. See `CLAUDE.md`.

---

## THE SHAPE

Six parts. Each is a package under `src/sylvatica/`, each has a protocol at its top, and no part
reaches into another's internals. The names below are the names in the code.

### `core` — the model and its state

- A `Core` protocol: `feed(tokens, state) -> state`, `generate(prompt_tokens, state, budget)
  -> (tokens, state)`, `embed(tokens) -> vector`, plus `save_state(path)` / `load_state(path)`.
- One implementation: `RwkvCore`, wrapping an RWKV-7 checkpoint. The G1 series on Hugging Face
  under `BlinkDL/rwkv7-g1` is the current line; verify what is published before choosing, since
  releases move. Start at 0.4B for iteration speed and use 1.5B for readings that count.
- **The state is the fast memory.** It is a fixed-size tensor per layer, on the order of tens
  of megabytes for 1.5B. It is saved after every turn and reloaded on start. A `Thread` is a
  named state file plus its transcript pointer; the default thread is the one a restart resumes.
- Every reading records `tokens_in`, `tokens_out`, `seconds` and estimated `flops` (forward:
  2 x params x tokens; training: 6 x params x tokens). This feeds the cost meter.

### `store` — the slow memory

- A `Fragment`: `id` (content-addressed), `kind` (`episode` | `summary` | `fact` | `identity`),
  `text`, `embedding`, `importance`, `confidence`, `created_at`, `last_recalled_at`,
  `recall_count`, `consolidated_at`, `provenance` (who said it, which turn, which node), `node_id`.
  Immutable once written; corrections are new fragments that cite the old. Nothing is ever
  deleted; `archived_at` is the strongest thing that happens to a row. Persistence's rule.
- A `Store` protocol: `write(fragment)`, `search(query_text, k, deadline) -> [Hit]`,
  `mark_recalled(ids)`, `sample_for_replay(spec) -> [Fragment]`, `tiers()`.
- One implementation for Phases 2 to 4: `SqliteStore`. FTS5 for lexical search, embeddings as
  blobs with brute-force cosine in NumPy (fine to a million rows; a vector extension is a later
  swap). Hybrid rank: reciprocal-rank fusion of lexical and vector, then re-weighted by
  `importance` and recency. The exact weighting is an arm, not a decision.
- Embeddings: a small off-the-shelf sentence encoder (MiniLM-class, ~22M params, runs on CPU
  and on a phone). An arm for later: embed with the core's own hidden state and see if the
  external encoder is needed at all.
- **Every turn is written**, in and out, as an `episode` fragment, before anything else
  happens. The store is the lossless record. Forgetting is what falls out of the STATE and out
  of the store's HOT tier; it is never a deletion.

### `learn` — consolidation

- The slow path is a LoRA adapter over the core's time-mix and channel-mix weights. The base
  is frozen. The adapter is trained from replay sampled out of the store.
- `sample_for_replay` returns a mix, and the mix is a dial: recent episodes, a rehearsal draw
  from older fragments weighted by importance and low `recall_count`, and a slice of general
  text (a fixed local corpus of a few million tokens) so the adapter is anchored to language it
  did not learn from this house.
- **What is trained on is an arm with three shapes**, and the refutation for each is named
  below: (a) raw episode text; (b) declaratives the core extracts from an episode window
  (*"John's GPU is a 1080 Ti"*), each paraphrased several ways by template and by the core; (c)
  both. The literature says models learn facts from a handful of raw exposures badly and from
  paraphrased declaratives much better, so (b) is expected to win, and it is still an arm.
- **A regression gate runs after every consolidation cycle.** It is a fixed held-out
  perplexity set and a fixed small general-QA set, both unchanging for the life of the branch.
  A cycle that moves either past its threshold is rolled back and the rollback is a reading.
  Calibrate the thresholds on noise FIRST: run two identical cycles and read the spread before
  a threshold is chosen.
- The adapter accumulates across cycles. Every K cycles it is merged into the base weights and
  a fresh adapter starts; K is a dial and merging is the "sleep". A fragment whose content the
  regression-gated adapter now answers correctly with the store disabled has its
  `consolidated_at` set, and that is what moves it out of the hot tier.

### `loop` — the turn and the idle

- A turn: write the input as an episode; retrieve (Phase 2 onward) and prepend hits in a fixed
  compact format; feed the core; generate under a token budget; write the output as an episode;
  save the state. The budget is small on purpose, per complaint 5, and is a dial.
- The idle scheduler: when no input has arrived for T minutes, run one consolidation cycle, then
  one reflection pass (the core reads its last N episodes and writes `summary` fragments), then
  sleep. T is a dial. This is the thing a transformer cannot do and the reason a stateful core
  was chosen; it must exist as a process, not as a function called by a test.

### `exam` — the instrument

- **Told-then-asked worlds.** A generator makes a fictional house: N facts about invented
  people, places and numbers that cannot be in the pretraining. The facts are delivered as
  natural sentences across a conversation of M turns, interleaved with filler dialogue, and each
  is asked at several delays. Scoring is contains-match on a short canonical answer. Negatives
  are included: facts never told, where the right answer is *I don't know*, and the rate of
  invented answers is scored separately.
- **Three tiers**, and every reading names its tier:
  - **A** — same thread, state saved and reloaded across a process restart. Tests the state.
  - **B** — fresh state, store enabled. Tests retrieval.
  - **C** — fresh state, store DISABLED, adapter enabled. Tests what was learned into weights.
    Tier C is complaint 4's bar and the first north star's new line.
- **Two baselines, always run beside an arm.** *Full-context*: the same core fed the whole
  transcript every turn, which is how a transformer would do it. *Blind*: answer the commonest
  answer for the question's kind. The second is what killed the earlier branches; it stays.
- **The cost meter** reports, per exam, total flops and wall seconds for the arm and for the
  full-context baseline, with consolidation amortised over the turns it served.
- Readings are JSON rows under `readings/`, one file per run, committed with the commit that
  produced them. The commit message says what the reading refuted or failed to refute.

### `node` — the fleet (Phase 5, protocol only before that)

- A `Node` holds a store shard, a consolidation worker and optionally a core. Nodes exchange
  fragments by gossip. `FanoutStore` implements `Store` over many nodes: `search` fans out with
  a deadline and returns what arrived; a missing node is missing memories rather than an error.
- Constraints C1 to C4 from the earlier branches, unchanged: no node reads another's data
  directly; messages are late, jittered, out of order; a cluster vanishing mid-thought is normal;
  no episode boundary, so nothing may depend on train-then-test. `recall_count` is a G-counter.
- Phase 5 is many processes on one machine first. Twenty phones are bought when, and only
  when, the one-box fleet has shown that a store shard on a weak node is worth having.

---

## THE PHASES IN FULL

### Phase 0 — Ground — STRUCK 2026-09-06

Done, so the plan for it is gone. What was built is in `src/sylvatica/core`, `loop` and
`scripts/`, and what it does is in those docstrings. The numbers are in `readings/`. One item
that was written here is still owed and it is in the handoff, not here: the old C# tree.

### Phase 1 — Exam

- Build the world generator, the three tiers, the two baselines and the cost meter. Tier A is
  the only tier runnable yet; B and C exist and fail until their phases land.
- **The first reading**, and take it before anything else is built: on a house of 50 facts
  over 300 turns at 1.5B, how does Tier A score at each delay against full-context and blind?
- **Would refute the whole core choice:** Tier A at delay 20 turns below blind. Then the state
  holds nothing usable and a recurrent core was the wrong part.
- **Exit:** the reading committed, and `readings/` has its first rows.

### Phase 2 — Store

- `SqliteStore`, hybrid retrieval, automatic injection of the top k hits in a fixed compact
  format ahead of the input. Write-every-turn from the first commit of the phase.
- **Readings:** Tier B against Tier A and both baselines on the same house. Retrieval precision
  at k: how often the fragment holding the answer is in the injected set.
- **Would refute:** Tier B with the store loses to full-context at equal flops. Then state plus
  retrieval bought nothing over re-sending, which is refutation 1 in the summary below.
- **Exit:** Tier B beats Tier A at every delay past 20 turns, and the retrieval precision is
  read.

### Phase 3 — Consolidation

- Calibrate the regression gate on noise first. Then LoRA, replay sampling with the three
  training-shape arms, the gate, rollback, merge every K cycles, `consolidated_at`.
- **Readings:** Tier C per arm. Gate readings per cycle. Cost of a cycle in flops and seconds
  against the turns it served.
- **Would refute:** (a) raw episodes: Tier C at blind after ten cycles. (b) declaratives: the
  extractor at 1.5B produces facts that are wrong more than a fifth of the time, read by hand on
  a sample of 50. (c) both: no better than the better of (a) and (b). Gate: any arm that trips
  the gate on more than a third of its cycles, since then replay is not protecting the base at
  this scale, which is refutation 2.
- **Exit:** one arm has Tier C above blind on a fresh state with the store off, and the gate has
  not tripped in its last five cycles.

### Phase 4 — Idle

- The scheduler as a process. Reflection writes `summary` fragments. Hot and cold tiers in the
  store, with `consolidated_at` and `last_recalled_at` deciding placement. Retrieval prefers
  hot; cold is searched when hot has no hit above a floor.
- **Readings:** Tier B and C after a day of idle cycles on a live thread, against the same house
  without idle. Store size in each tier over time. Whether reflection summaries are ever
  retrieved.
- **Would refute:** consolidation and reflection over a day cost more flops than re-sending the
  full transcript for that day's turns would have. Refutation 3.
- **Exit:** the machine has run unattended for 24 hours, learned something told at hour 1, and
  answered it at hour 24 on a fresh state with the store off.

### Phase 5 — Fleet

- `Node`, gossip, `FanoutStore`, deadlines. Several processes on one box, each a shard. Kill
  processes mid-exam. Measure Tier B under node loss and under injected latency and reordering.
- **Would refute:** Tier B with three of eight shards down falls to Tier A. Then the store did
  not survive its own constraints.
- **Exit:** a Tier B reading with a third of the fleet down that stays above Tier A, and a
  footprint row per node. That row is what says whether phones are worth buying.

### Phase 6 — Specialists and tools

- Retrieval as a call the core makes, measured against the harness doing it. A second core,
  possibly a different checkpoint, on the same store. A router. Deferred deliberately: small
  cores make structured calls unreliably, and Phase 3 is the bet, not this.

---

## WHAT WOULD REFUTE THE BRANCH

Named before anything runs, per the standing rule. Each phase above carries a local version.

1. **State plus store loses to a same-size stateless model given the whole conversation as
   context, at equal flops, on the same exam.** Then state bought nothing.
2. **Adapter updates lose the core's baseline abilities faster than they add.** Then replay does
   not solve forgetting at this scale, and the branch has rediscovered why training is split
   from inference.
3. **Consolidation costs more compute than re-sending the context would have.** Then the
   workaround was cheaper than the fix, and Persistence was right.

A refutation is a finding. It goes in the commit that found it and in `docs/history/` when the
branch closes, and it is the most valuable thing this branch can produce short of working.

---

## THE FIRST NORTH STAR

Carried from before, with ONE line added. A machine that holds a basic conversation in English,
is told a block, and answers on it. **And is told something today and knows it tomorrow after a
restart, with nothing about it in the prompt.** Persistence meets that line only by re-sending.
The old first north star did not have it. Phase 4's exit is that line, measured.

---

## DIALS

Every one is a number read on an exam, never a constant chosen once. Named here so a session
knows what is tunable and does not invent a second knob for the same thing.

- The turn format and the PREAMBLE, both in `loop/turn.py`. Added Phase 1: the framing fed
  once into a fresh state. Every exam reading records it verbatim, because a score taken
  under one framing is not comparable to a score taken under another.
- `k` hits injected; the injection format; the hybrid rank weights.
- The replay mix: recent, rehearsal, general.
- The generation budget per turn.
- `T` idle minutes before a cycle; `K` cycles before a merge.
- The regression gate thresholds, calibrated on noise before use.
- The hot-tier floor.

---

## OPEN FORKS

Ideas nobody has run. One line each. An open fork becomes a phase item only after the phase it
depends on has its exit.

- **A hybrid core** keeping a little full attention (Qwen3-Next, Nemotron-H shape), if Tier A
  shows the pure recurrence loses exact recall the store does not recover.
- **Core-derived embeddings** in place of the external encoder.
- **An adapter per node** merged by gossip, against one adapter on one node.
- **State as a fragment**: store a thread's state file and retrieve it, so a whole headspace
  can be recalled rather than reconstructed.
- **Brevity as a trained target**, per complaint 5, once Tier C works at all.
- **The core scoring its own confidence** so retrieval fires only when the state is unsure.

---

## FOR THE IMPLEMENTING SESSION

- This doc and `CLAUDE.md` are the whole brief. If they disagree, `CLAUDE.md` wins on how to
  work and this doc wins on what to build.
- One GPU, shared. Never run two GPU jobs at once. A training cycle and a REPL do not overlap.
- Do not download a checkpoint above 3B without a line in the commit saying why.
- Name the refutation in the commit message BEFORE the reading, then the reading, then the
  verdict. Three lines is enough.
- When a decision above turns out to be wrong, say so in one sentence in the commit and in the
  handoff. Do not act on it; that conversation is John's.
- John checks in with the designing session periodically for drift. The handoff should make
  that cheap: where the branch is against THE ORDER, what was refuted, what is open.
