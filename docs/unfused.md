# Unfused

A frontier language model does three jobs in one set of weights: skill (grammar, reading,
composing an answer), knowledge (facts about the world), and memory (what it was told a minute
ago). Only the first needs dense computation. Knowledge is sparse and tolerates latency, and
memory is what a model is worst at keeping. This branch keeps the three apart:

- **Skill** is a small pretrained language model, frozen. It reads and it speaks. It is called
  the faculty, and nothing here trains it.
- **Memory** is built here and is not a neural network. It learns from one hearing, keeps
  what it learnt across a restart, and brings it back unasked when it is relevant.
- **Knowledge** is memory at scale, spread across many devices. That is the distributed half of
  the project, and it comes after memory works on one box.

It replaces `sylvatica`, `commitments` and `csharp`. Their plans and one review are under
`docs/history/` and their refutation tables still hold for what they measured. The two
gradient-free learners lost to a blind rule on a house world. The `architecture` spike showed
why: a learner is worth what its prior contains. Here the prior is the faculty, and the
learner is the memory.

This is the one design doc. It says what to build, in what order, and what reading refutes
each bet. What a built thing does is in its code.

---

## THE ORDER

The one list a session edits at both ends. Each phase's exit is a measurement.

- **Phase 0 — Ground.** IN PROGRESS 2026-09-28. The exam, the faculty, the store, text recall,
  and the two baselines on the full house.
- **Phase 1 — Bound memory.** Facts read once into role-filler structure and recalled by
  spreading from what the input mentions.
- **Phase 2 — Importance and forgetting.**
- **Phase 3 — Intentional retrieval.** The faculty asks for a lookup as well as being handed
  what surfaced.
- **Phase 4 — Fleet.** The memory on many processes, merged without coordination, with nodes
  vanishing.
- **Phase 5 — Small faculty.** The same memory under the 1.7B faculty and smaller, to price
  the phone-sized version.

---

## DECIDED

With John, 2026-09-28. Reopening one is a conversation with him, never a commit.

- **The 1080 Ti is the hardware.** No rented compute. Anything that needs more than one
  11 GB Pascal card does not get built.
- **The faculty is a frozen pretrained language model.** It is the senses and the mouth.
  "No LLM" constrains the learner, not the senses. The faculty is a dial: Qwen3.5-9B at Q6
  through llama.cpp is the reference for readings, and Qwen3-1.7B in-process is the small one.
- **The learner is the memory, and it is not trained by gradient.** It learns by writing.
- **Recall is automatic.** Every input arrives at the faculty with what it called up from
  memory, found by words (BM25), by meaning (a sentence embedding) and weighted by importance.
  Nothing has to ask. Intentional retrieval, where the faculty asks for a lookup, is Phase 3
  and adds to this rather than replacing it.
- **Nothing is deleted.** Archiving is the strongest thing that happens, and a correction
  cites what it corrects. Persistence's rule.
- **Distributable by construction.** Ids are content-addressed, counters only rise, time is a
  turn index, and symbols map to vectors by a hash every node computes alike. A mechanism that
  needs coordination between nodes is a mechanism this branch does not use.
- **Python, one codebase.** PyTorch for the in-process faculty, SQLite for the store.

---

## THE SHAPE

- `faculty` — `Faculty` (in-process, has `surprise`, a sensor read off the logits) and
  `ServedFaculty` (llama-server). One method both share: `chat(system, user, budget)`.
- `store` — `SqliteStore`: every turn written, hybrid rank by reciprocal rank fusion of
  BM25 and MiniLM cosine, weighted by importance and recency on a turn clock.
- `arms` — what an exam compares. `Blind`, `FullContext`, `Recall`, and Phase 1's arms.
- `exam` — the house generator and the runner.

---

## THE EXAM

A generated house: invented people, rooms and objects, told once each as ordinary sentences
among filler, across 300 turns. Every question is asked at delays from 1 to 150 turns, in six
forms: `direct`, `oblique` (no shared content words), `reverse` (asked from the other end),
`twohop` (needs two facts told at different times), `update` (a fact that changed; naming the
old answer is scored as stale), and negatives about people never mentioned (the answer is a
refusal; anything else is scored as invented).

The house has a size and no switches. Every house has all six forms in fixed proportions.
A form is added when real conversation has it and the exam does not; a form is never removed
because an arm does badly on it.

Every reading carries the two baselines. `Blind` answers the commonest answer for the kind of
question. `FullContext` gives the faculty the whole transcript, which is what the memory must
be worth against, at a fraction of its cost. The arm is closed and reopened every 50 turns, so
what it knows must survive on disk.

---

## THE PHASES

### Phase 0 — Ground

- **Reading:** all four arms (blind, full context, recall at one and two hops) on seed 0 with
  the reference faculty.
- **Refutes the recall design:** recall below blind on any form other than twohop.
- **Exit:** the reading committed, and the recall arm's failures read by form.

### Phase 1 — Bound memory

The faculty reads each turn once and writes what it states as frames: a relation and its role
fillers (`keeps: agent=Vessarine, thing=lanterns, place=scullery`). Symbols are canonicalised
on the way in: a new filler that embeds close to an existing symbol is that symbol, which is
where meaning-similarity enters the structure. Each symbol maps to a random bipolar vector
seeded by a hash of its canonical string, so every node gets the same vector with no
coordination. A frame is the bundle of its role-filler bindings.

Recall spreads. The symbols an input mentions activate the frames that contain them; the
fillers of those frames activate the next ring, at a decay. What surfaces is rendered back into
short sentences and joins the notes beside text recall. The faculty composes the answer; no
question is parsed into a query.

A frame that shares a relation and every filler but one with an older frame supersedes it, and
the older one is archived with the new one citing it. That is how an update is heard.

- **Arms:** text recall; bound recall; text and bound together. And one control: the same
  frames held as an exact table and spread over by exact symbol match, with no vectors.
- **Refutes bound memory:** it does not beat text recall on `reverse`, `twohop` or `update`,
  the three forms structure exists for.
- **Refutes the vectors:** the exact table matches bound recall everywhere. The vectors then
  have to earn their place in Phase 4, under merging and loss, or be deleted.
- **Exit:** bound recall (or the pair) above text recall on the three structural forms with no
  form falling, on three seeds.

### Phase 2 — Importance and forgetting

Importance is measured, never asked for. Two sensors: surprise at writing (the faculty's
per-token loss on the sentence, high for news and low for filler), and use at recall (a
fragment recalled into notes whose answer was right gains weight). Forgetting is what falls
below the recall floor, never a deletion.

- **Refutes:** surprise-weighted recall does no better than uniform importance on a house whose
  length is raised until uniform recall degrades.

### Phase 3 — Intentional retrieval

The faculty may answer with a lookup request instead of an answer, once per question.

- **Refutes:** asking adds nothing on any form over automatic recall alone.

### Phase 4 — Fleet

Several processes, each holding a shard of frames and fragments. Bundles merge by addition and
the store merges by id. Kill a third of the nodes mid-exam.

- **Refutes:** with a third of the nodes gone, recall falls below the faculty with no memory
  at all.

### Phase 5 — Small faculty

The Phase 1 winner under Qwen3-1.7B, and smaller if one exists that can read.

---

## WHAT WOULD REFUTE THE BRANCH

1. **Memory loses to the context window at the same faculty on a house that fits in it.** Then
   the memory costs accuracy the context would not, and it has to win on cost or on houses too
   long to fit.
2. **Structure buys nothing over a search index.** Phase 1's first refutation.
3. **The faculty is the whole score.** If every arm moves with the faculty and none with the
   memory, this branch is measuring someone else's model.

---

## DIALS

- The faculty; its system prompt; the answer budget.
- `k` hits per query and the number of recall hops.
- The store's fusion weights: `rrf_k`, importance weight, recency weight and half-life.
- Phase 1: vector width, the canonicalisation threshold, the spreading decay and floor.

---

## OPEN FORKS

- **A thread's working notes as a fragment**: what surfaced for the last few turns is carried
  forward, the way attention holds a topic.
- **Frames learnt without the faculty**, from co-occurrence in the store, once the faculty's
  frames exist to be compared against.
- **Confidence from the memory itself**: the cleanup similarity of a recalled filler as the
  sensor for "I don't know", in place of the faculty deciding.
- **Idle-time reflection**: frames about frames, written when no input is arriving.
