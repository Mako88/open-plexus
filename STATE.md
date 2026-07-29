# State — open questions and work in flight

**This is the only document in this project that is kept current.** Everything in
it is either live work, an open question, or a standing agreement. When something
here is settled it leaves, and an entry goes in [DECISIONS.md](DECISIONS.md).

| document | what it holds | when to read it |
|---|---|---|
| [GOALS.md](GOALS.md) | what the project is for, and what would refute it | before deciding whether a mechanism belongs here at all |
| [DECISIONS.md](DECISIONS.md) | what was chosen and why — **history, never rewritten** | for the reasoning behind a past choice |
| **STATE.md** (this file) | what is true now, what is open, what is running | first, and every session |

If this file and DECISIONS.md disagree, **this file wins.**

> **PRUNED 2026-07-29, from 1547 lines to this.** John: the drift that produced
> decisions 135–142 happened partly because this file carried *"competing
> information from different time periods"* — an instrument table saying the text
> line was closed, a START HERE section deep in word-level text, and an "In
> flight" section still describing decision 119. All three were true when
> written. Together they said nothing.
>
> **The rule at the top was always the fix and was never enforced.** The full
> pre-pruning file is `docs/archive/state-2026-07-29-before-pruning.md`; nothing
> was deleted, and `tools/check_state.py` now fails the build if this file grows
> past its budget or grows a second live question.

---

## IN PLAIN TERMS

The project is trying to build a neural network that runs across ordinary
people's computers over the ordinary internet, instead of inside a data centre.

Its central piece is a **memory that stores associations** — told "A is B's
parent", it can later answer about A and B.

**On the task that memory was built for, it does everything.** Recall test:
**0.995** with the memory, **0.000** without it. Nothing else does that work.

**On predicting the next word in ordinary text, it contributes nothing** — and
that is now understood rather than mysterious. The only relation it can express
on a next-token objective is "what followed this", which is an n-gram, and a
counting table does that exactly and cheaply. **The objective was the ceiling,
not the memory** ([note 047](docs/notes/047-what-the-store-can-hold-on-text-is-an-n-gram.md)).

---

## THE GOAL — understanding, not prediction

Full statement in [GOALS §1](GOALS.md); next-token prediction is an explicit
non-goal in §2. John, 2026-07-29:

> Store one concept and how it relates to other concepts. Be aware of the
> differences and interrelations between them. Process a query — text, picture,
> video, whatever — and respond from that awareness. **The goal is understanding,
> not prediction.**

**Multi-modal is part of the goal, not a later luxury**, which is why one concept
must be able to have many surfaces. **Text as input is fine; text-prediction as
the score is not.**

---

## ⇒ THE QUESTION RIGHT NOW

**What is an "answer"?**

ARCHITECTURE row **F3**, and decision 163 §3 is John choosing it as the next
work. **Nothing in this project has ever scored a multi-token answer.** Every
task emits one token, so *"form a response from awareness of the concepts in the
question"* — GOALS §1, the actual goal — has never been tested.

    (a) AUTOREGRESSIVE   emit a token, feed it back, repeat
    (b) TRAVERSAL        walk the concept graph and emit what is visited
    (c) SLOTS            fill a fixed frame

**(a) deserves care rather than reflex rejection.** GOALS §2 rules out next-token
prediction as the TRAINING OBJECTIVE, which is a different thing from
autoregression as an output MECHANISM. Conflating them would be a rule
misapplied. [Note 052 §3](docs/notes/052-decisions-that-cascade.md) has the
options and the blast radius.

**It reaches backwards, which is why it is worth doing before more mechanisms.**
Every task, every accuracy number and the whole scoring convention assume one
answer token. Whatever is chosen, the existing tasks stay valid as capability
probes and stop being measurements of the goal.

**The ruler and the question are built; nothing emits a set yet — 165 and 166.**
`openplexus/answers.py` scores `exact` and F1 and refuses recall alone; **the
falsifier is the load-bearing part** — emit the whole alphabet and recall is 1.000
while F1 is 0.400. `families.py set_queries` asks what values a family stated,
which is its value AND its exceptions, and **no single token can answer it**: the
task has held that conjunction since 144 and could never ask for it.

**And the mechanism now exists — decision 167.** `answer_set` collects across
index-proposed siblings, gated on emptiness, and reaches `exact` **1.000** with
precision **1.000**. F3 is PARTIAL rather than PASSING for one reason: **the peak
sits at `branches = family_size - 1` in every row measured and collapses on both
sides** (table in 167). The answer's SIZE is handed to the model, and a mechanism
told how many things to find has not answered from awareness.

**The gate cannot supply it: it filters emptiness, not irrelevance** — the surplus
candidates are other families' entities and their addresses ARE written. The gate
does act (precision 0.733 → 1.000 at the matched bound) and cannot reach the other
failure.

**⇒ So the enumeration bound is the live sub-question.** `grouping.cluster` takes a
`k` of its own, converting a per-query constant into a global one — better in kind,
**not free**.

### Where the mechanisms stand

**Every one works in isolation with a unit test. Not one has a task number.**
That is the state, not a hedge — 148 the gate chooses exactly (1.0000/0.0000),
157 typed writes stop the collision, 158 a hop follows a named edge, 159 the
index proposes at a dead end for 1 extra read against an ungated 56, 161
`inherit` is read-gated and 148 still reproduces to four decimals, **164 a walk
follows LINK-then-FACT** where one relation stops at the representative.
**157's LINKED column at 0.1275 is the number to move, and 164 removed the last
thing blocking the run.** The relation is still fixed rather than chosen: a
schedule the task does not supply is a fitted constant (162), which is why 164 is
an instrument for reaching the measurement and not the final read path.

### The rail this project does not have

Decision 161: accuracy is measured everywhere, and **the read count that C1 and
G4 both turn on is measured nowhere.** Two cost claims were made from reasoning
and both were wrong (159, 160); both were caught only by writing the measurement
down. `tests/test_index_at_hops.py` counts reads in three places and nothing else
does. **Build it with measured budgets** — a rail with a guessed threshold is
worse than none, which decision 155's own p90 calibration proved by flagging what
chance produces.

### Answered and moved to DECISIONS

Decision 148 answered the one before those — *can anything tell which of two
retrievals to trust* — with **yes, once the question is asked exactly.** `inherit`
answers from the entity's own address when **anything** was written there and from
its neighbours' when nothing was: **0.8100 DIRECT / 0.4350 TRANSFER / 0.8183
EXCEPTION**, the first arm good at all three, where grouping bought transfer by
destroying exceptions (0.3708, saying a sibling's value 86.6%) and summing landed
between. The gate is exact — **1.0000** of TRANSFER, **0.0000** of DIRECT and
EXCEPTION, every seed. Full table in decision 148.

What made it work is that membership is *"is there anything here"* rather than
*"who has more"*, and with a hashed sketch an unwritten address reads exactly 0.0 —
so note 049's threshold is **structurally zero** and nothing is fitted. **167 is
now the limit of that**: emptiness is the only thing the sketch knows, so it cannot
bound an enumeration over addresses that are all occupied.

**The price is real:** without exceptions DIRECT costs 0.050 against summing
while TRANSFER gains 0.231. Summing lets agreeing neighbours corroborate;
`inherit` refuses that on principle, which is what keeps a contradicting fact
intact when there IS a conflict.

**And 149–153 measured its scope rather than assuming it.** Every entry is in
DECISIONS.md; the reason it is compressed here is that each carried a
forward-looking claim superseded within a day, which is the drift this file
exists to stop.

    149  not a fitted constant. Across n_values 4/8/16 and family_size 3/4/6 the
         ordering holds in EVERY cell, gate at 1.0000/0.0000. The one dip was
         BRANCHES=3 unable to reach a stated sibling; --branches 5 fixed it
    150  MQAR: inherit matches plain SEED FOR SEED (0.9950) and never defers,
         while summing the same extra reads costs 0.113. Also rules out sketch
         false negatives, which would have been invisible on families
    151  kinship: defers on 0.0000 at one hop AND at two, so the gate is blind
         where the address is occupied either way
    152  chains unaskable — index_branches and hops > 1 exclude each other
    153  half the gate goes there anyway: `track_occupancy` runs at hops=2.
         It then finds chain start/middle/end at 0.893/0.791/0.898 — nothing

> **Occupancy is informative exactly where an address is READ BEFORE IT IS
> WRITTEN within the sequence.** Families reads a transfer entity at its query
> and writes it only afterwards → 0.0. Chains, kinship and MQAR write every
> address before querying it → positive, and silent. That subsumes 151's bound
> and predicts where the sketch pays instead of hoping.

**And the neighbour half is not blocked by what the guard says it is —
decision 154.** Note 044 refuses `index_branches` above one hop because a hop key
*"names no concept"*. Measured on chains, at the sharpness the task is solved
with:

    sharpness 6.0        top cos   margin to 2nd
      ordinary read       1.0000          0.7173     <- the check, not a result
      HOP 1               0.9612          0.6408
      HOP 2               0.9734          0.6605

**A hop key sits at cosine 0.96 to a single token's row.** It names a concept,
and `argmax(wk @ hop_key)` is what the index could look up.

**The guard is not lifted**, because a real design question sits under it that a
cosine does not settle: `index_branches` runs once per POSITION, not once per
hop, so combining them means choosing whether the index proposes neighbours of
the position's concept or of the hop's landing concept.

**And that choice cannot be decided, because nothing measures it —
[note 050](docs/notes/050-the-missing-instrument-composition-over-things-never-stated.md).**

    families   addresses never written ✓   composition ✗
    kinship    addresses never written ✗   composition ✓
    chains     addresses never written ✗   composition ✓
    MQAR       addresses never written ✗   composition ✗

The gate pays where an address was never written; the hop pays where the answer
is at no single address. **No task has both**, and decision 153 says why that is
structural: composition tasks state their facts before querying them, so they
write every address they later read.

> Building the combined mechanism now would produce a number that means nothing —
> on chains the gate never fires, so the two design options would be
> indistinguishable. That is decision 143's circularity one level up.

**So the blocker is the instrument, not the mechanism**, which is where GOALS §4
says to look first. Note 050 designs the task — entity → family → linked family,
where step 1 is the gate and step 3 is the hop — with the calibration check that
decides whether it is fair before any arm is run, and four registered
predictions. **Not built.**

### How this line got here

**Decisions 143–147 are settled and live in DECISIONS.md**, and they leave here
under rule 14b rather than being summarised a third time: grouping can answer what
was never stated, it erases exceptions, and the two obvious ways to choose between
two retrievals are both refuted. The one correction worth carrying forward is note
049's — the store is one matrix addressed by keys, so a fact at the surface key and
a default at the concept key **never collided**; `ByConcept` mapping everything to
the concept did, which made it a read policy and is why 148 cost a sketch rather
than a new representation.

Nothing else in this file is a live question. Everything below is either the
evidence behind that one, a standing agreement, or a refusal.

---

## Where the model actually is

Full records in `experiments/sweeps/`.

**Relational — this is what works:**

    families, TRANSFER, grouped / not         0.998 / 0.087   decision 143
    families, DIRECT, grouped / not           0.997 / 0.658   decision 143
    MQAR, store on / off                      0.995 / 0.000   decision 142
    2-hop chain, fixed hops=2                 1.000
    3-hop chain, fixed hops=3                 1.000
    depths 1+2+3 mixed, gated                 1.000
    1-hop model on a 2-hop chain              0.000   <- the control still fails
    depth 3, gated, HALF the machine gone     0.928
    zero-shot transfer to an untrained depth  0.992
    chains linked end-to-start, 4 in 6        0.630   <- 1.000 was the disjoint case
    kinship, gated search                     0.624   decision 130

**Text, character level** (older split, `min_count` 20):

    uniform                        6.000 bits/char
    OUR MODEL, best ever measured  5.172   g11-07, eighteen compositions
    unigram                        4.829   <- NEVER beaten (decision 118)
    bigram                         3.583
    MLP-128 on frozen features     4.525   note 037, offline backprop, NOT the model

**Text, word level** (2026-07-29, corrected harness):

    uniform                       10.759 bits/word
    the model, tuned               9.185
    the SAME model, no store       9.187   <- the store contributes nothing
    unigram                        8.068
    bigram                         7.848

Note 037 remains the interesting one: the retrieval *carries* enough to beat a
unigram and a linear readout cannot extract it. Note 047 now says why that is not
the same as the store being useful for prediction.

**On scale and the wire:** 5 bytes broadcast per token, 8 bytes per node reply,
~8 KB per answered position at 1024 nodes. The binding constraint is **dimensions
per node, not node count** — below ~16 dimensions a node stops having a
standalone opinion, so nodes ≈ width ÷ 16. Measured on MQAR at width ≤ 128;
beyond that it is extrapolation.

---

## Which instrument, and why

| task | role now |
|---|---|
| **`closure.py`** | **THE PRIMARY INSTRUMENT.** Unmarked stream of facts, some implied. Relational, no question marker. Passes G0 (g14-01): entailed headroom 0.277 against a frozen 0.000 |
| `kinship.py` | **the mechanism testbed.** Marked questions isolate a mechanism cleanly; the search line is measured on it |
| `mqar.py` | **the store's control, not history.** The only instrument that isolates the store from a prior — decision 142 |
| `chains.py` | solved at 1.000, out-degree 1 by construction. A control |
| `corpus.py` | **PAUSED, not condemned.** Closed by 115/118, reopened by g17-01, and 135–142 measured on it without anyone re-deciding it was the instrument. A text task scored on what the model *holds* is untried |
| **`families.py`** | **NEW, 2026-07-29.** The only instrument where things RESEMBLE each other, so the only one where a concept can mean something. Calibrated by g19-00, first result decision 143 |
| `reward_recall.py` | **retired** (decision 126) |

**The gap note 048 named is now filled by `families.py`** — every other
instrument's entities are arbitrary by construction, so nothing resembles
anything and a concept has nowhere to mean something.

**And the standing gap:** everything above is self-designed. **CLUTRR is the only
external benchmark that would make a number comparable to anyone else's, and it
has been "next" for several cycles.** Until it runs, this project is grading its
own homework.

---

## Do not re-propose these

Each has a measurement pinning it. **Read the decision before proposing it
again** — several of these were proposed twice.

| proposal | why not | where |
|---|---|---|
| Anything recovering per-item information *after* the sum | `r = M @ key` is a SUM. Readout bias, competitive retrieval, orthogonal updates, pair keys all failed for this one reason | 69, the g11 line |
| Another mechanism on top of noisy retrieval | Four failed against the same 0.915/0.35 | 102, 105, 107, 111 |
| Transfer of the halting gate to new terminator tokens | `halt_w` sits +8.3 sd on one token's value vector; two markers have unrelated value vectors, so transfer is impossible by construction | 89 |
| A width × sequence-length sweep to explain "width doesn't help" | Nobody claims that. The flat axis is DATA | 112, 113 |
| More data on the text corpus | Converges at ~16,000 characters; `Wo` is the only durable parameter | 63, 115 |
| Store or readout capacity as the saturation cause | ~96 bindings at d=64 scaling as d²; 2.00 readout items per dimension | 109, 110 |
| `value_centre` / `value_lr` as a fix for collapse | The values move a long way, stay spread, and the plateau does not budge | 114 |
| A composition sweep on chains as evidence about composition | A chain is out-degree 1 by construction | 108 |
| **Concept addressing as a fix for text prediction** | 0.540 bits at bias 0, and a grouping built from SHUFFLED text does as well. The address count did the work, not the concepts | **141** |
| **Anything measured in bits per token as evidence about the store** | The objective is n-gram bounded, so it cannot show what the store adds | **142, note 047** |

---

## ⚠ Unverified — do not quote

**A churn probe with no provenance** returned during a previous session and
challenges decision 119's "the store ties a cache when bindings do not exceed
slots". At 128 slots the cache holds 0.932 where the store falls to 0.690.

It carries **no condition string, no script name, no seed count and no registered
prediction**, and was not launched from a known session. Rule 11b: verify a run's
identity from the data before reading a number off it. **What it needs:** find
the script, confirm the arms mean what the headings say, re-run with a condition
string and seeds.

---

## Working agreement with John

- **Blanket permission for architectural decisions.** The pending-decisions list
  is a REPORT, not a gate. If he does not answer, decide and proceed — document
  it in DECISIONS.md and say which calls were made without him.
- **List pending decisions at the end of every response.** He reads from a phone.
- He is not deeply versed in modern ML internals. **Explain plainly, keep the
  numbers, do not hide bad news.**
- **Goal ordering:** AGI is primary; being an LLM replacement that runs on
  distributed consumer machines is secondary and must not compete with it.
- **Biology gives policies, not representations.** Take mechanisms from computer
  science where the problem is well understood.
- **Scheduled wake-ups DO NOT FIRE in his setup.** Cron never fires and
  `ScheduleWakeup` was tried and did not either. **What works is a persistent
  `Monitor`** emitting a heartbeat line.
- **Input and output is his call, not mine** — his framing is that if the AGI
  goal wins, inputs should look like a body: a loop with consequences, not a
  passive feed.

---

## Standing operational rules

- Sweeps are GitHub Actions **dispatch-only** via `gh workflow run`, one matrix at
  a time, cost stated first and estimated **from the most expensive cell**.
  Nothing heavy runs locally. A 30-cell matrix is ~3 minutes of wall clock, so
  dispatching beats probing locally for anything above a couple of cells.
- **Never use bash heredocs.** **ALWAYS write the commit message to a file and
  use `git commit -F`. Never `-m`, for any message, ever** — strengthened
  2026-07-29 after breaking the conditional version twice in one session.
- **Run `python tools/check_all.py` before every commit.** Not as one compound
  shell command: a shell reports only the last exit code, and on 2026-07-28 that
  said success while two of five were failing.
- **Batch commits when a sweep is in flight** — every push queues check jobs
  ahead of the matrix.
- **Mutations run in CI, not locally.** `tools/mutate.py` **edits source in place
  and takes the tree exclusively**; nothing else may read the repository while it
  runs. To stop one: kill the `mutate.py` processes AND the `unittest` child by
  PID, `git checkout --` the mutated file, confirm with `mutate.py --verify`.
- **`harness.refuse_if_mutating()` is a STARTUP check, not a continuous one.** A
  run already in flight when a mutation begins keeps reading mutated source and
  says nothing. That happened on 2026-07-29 and voided a sweep.

---

## The standard this project holds itself to

Pre-register predictions before every sweep and score them honestly, including
the refuted ones. **Three, not five** — a gate, a rail, and a falsifier (John,
2026-07-29); the rest is ceremony.

A mechanism measured only on the task it was designed for is not measured. When a
mechanism adds state, compare against a model given the same amount of state.

**Probe the bottom of a scaling range locally before spending a matrix on it.**

**And reproduce a known number before trusting a harness.** Decision 138: a wrong
training target survived four sweeps and 142 cells because every arm was wrong
identically — internally consistent, both rails passing, a monotone ordering with
a tidy explanation. What caught it was a figure the project had already measured.
**Internal consistency is not evidence.**
