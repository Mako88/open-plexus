# Handoff — state of play, 2026-07-28 (overnight)

**For the next session.** Read this, then `GOALS.md`, then `BACKLOG.md`. The
notes in `docs/notes/` are the reasoning; `DECISIONS.md` is the running log John
reads — **entries 84–88 are the newest and are the current state**. This file is
a snapshot and goes stale — trust the notes and DECISIONS over it.

---

## THE OVERNIGHT RESULT: the model composes, at a depth it is not told

This is new capability, not a tuning gain. It was 0.000 before.

    2-hop chain, fixed hops=2                 1.000   (was 0.000)
    3-hop chain, fixed hops=3                 1.000
    depths 1+2 mixed, gated                   1.000   on both halves
    depths 1+2+3 mixed, gated                 1.000   on all three
    1-hop model on a 2-hop chain              0.000   <- the control still fails
    depth 3, gated, HALF the machine gone     0.928   <- C3 holds for hops too

The last row is what makes the rest readable: a one-hop model still answers the
intermediate 100% of the time, so the task genuinely requires composition and
nothing leaked when it started working.

**Three defects had to be cleared, in this order.**

1. **The hop loop reassigned `key`** (decision 85). `key` is carried into
   `previous_key`, which is what the NEXT position writes its binding with — so
   with `hops > 1` every binding in the store was written with a re-encoded hop
   key. *The hop mechanism was corrupting the memory it was reading.* One line.
   Four probes and two refuted hypotheses went past it, because every one of
   them measured retrieval and the damage was in the write.

2. **The task stated each link as its own triple** (decision 84), so an
   intermediate symbol appeared twice and `key(b)` carried two bindings — the
   real one and one to the separator. Retrieval put the answer first 54% of the
   time. Chains are contiguous now: 100%.

3. **Two flattened signals** (decisions 84, 87). The hop decode and then the
   gate each computed the right thing and threw it away in a near-uniform
   softmax — decode entropy 3.912 against a uniform 3.912, and the gate putting
   0.5020 vs 0.5000 on the hop it should have chosen. Both needed a gain, and
   both have a gain-0 control proving the fix was the fix.

**The gate** (decisions 86, 87, 88) learns which hop to read from: one linear
score per group, softmaxed across hops, where **hop k is scored by what hop k+1
returns**. Scoring a hop by its own content is only half a mechanism — it solves
depth-1 perfectly and depth-2 at 0.547 — because the available signal separates
*past the end* from *on the chain*, not *the answer* from *an intermediate*.

### Be sceptical of

- **The gate is a token detector, and this is measured** (decision 89). Cosine
  between `halt_w` and the value vectors: SEPARATOR **+0.563 (+8.3 sd)**, QUERY
  **+0.518 (+7.7 sd)**, every other token mean −0.068. It has latched onto two
  specific tokens. Two markers have unrelated random value vectors, so transfer
  is **impossible by construction** — do not spend a session confirming it.
  The open question is whether a gate can run on a signal that is not token
  identity; retrieval `norm` is the only candidate with any separation
  (d′=1.01), and it is weak where the current signal is overwhelming.
- **`hops` is a ceiling** the caller sets; the gate chooses within it.
- **Contiguity fixed the offset** from query symbol to answer at exactly `hops`.
  Harmless for this model, which has no positional access — but the instrument
  needs filler interleaved before it can be pointed at one that does.
- **`gate_sharpness` has a middle.** 200 is where both grids agree; 1000 loses
  accuracy on the deeper questions.
- **C4 does not pay for C3** (decision 91). With half the nodes gone, continued
  learning recovers **+0.008** against ~0.047 lost — churn costs *capacity*, and
  capacity is not something learning rebuilds. The two are independent.

## What is actually known about SCALE and communication

Asked on 2026-07-28 and answered from existing evidence rather than new
measurement — but the first answer given was **wrong** and worth recording so it
is not re-derived.

**The wrong answer.** Reading `parts.sum(0)` in the numpy reference and
concluding each node ships a vocabulary-sized vector per token: ~205 MB per
token at 1024 nodes, apparently a showstopper. That is not what the distributed
implementation sends.

**What `openplexus/distributed.py` actually puts on the wire:**

    token broadcast to all nodes            5 bytes
    each node's reply, combine="vote"       8 bytes  (step + its argmax)
    per answered position, 1024 nodes      ~8 KB

**~25,000× less.** A node's readout spans the whole vocabulary from its own
slice, so its argmax is a *complete opinion*, not a fragment — which is why four
bytes is legitimate rather than lossy.

**g4-01 measured that the pooling is optional** (pooled / one group alone):

    seq_len 96, width 128     P=4  1.000 / 0.996     P=8  1.000 / 0.949

**And it gives the real scaling constraint, which is DIMENSIONS PER NODE, not
node count:**

    16 dims/node → lone node 0.949
     8 dims/node → lone node 0.681
     4 dims/node → lone node 0.412

Below ~16 dimensions a node stops having a standalone opinion, so **nodes ≈
width ÷ 16**. At width 8192 that is ~512 nodes and ~410M learned parameters —
GPT-2-large scale, *not* frontier scale. Frontier needs ~65k nodes at width 1M.

**Still open:** g4-01 was MQAR, width ≤128, no hops. Hops multiply the reads.
So "the reduction is affordable" holds for the regime tested and is
extrapolation outside it.

## READ THIS FIRST (decision 103)

**The store cannot hold an entity that appears in two facts.**

    hop 1 finds the queried subject's own relation:
      person appears in 1 fact    0.959
      person appears in 2 facts   0.366
      person appears in 3 facts   0.321

`key(person)` accumulates one binding per appearance and a retrieval returns
their **sum**. A person who is the subject of one fact and the object of another
gets a superposition of both.

**This is not a defect in the task — it is what relational data is.** Every
knowledge graph has entities in many relations. Decision 84 hit the same wall on
chains and the fix was to make each symbol appear once, by laying chains out
contiguously. That worked only because a chain is a path; **a graph cannot be
laid out that way.**

It puts decisions 101 and 102 downstream of something more basic: composition
assumes the individual retrievals are right, and at two appearances they are
right a third of the time. An oracle handing hop 2 the correct relation still
caps at 0.560 — exactly the `last`-relation bound — because hop 1 contributes
nothing.

### THE NEXT THING, and it is now the blocker rather than an optimisation

**`context_keys`** already binds `(previous, token)` rather than `token`, giving
an entity one key per ROLE instead of one key total. Falsifiable prediction:
**hop-1 accuracy at two-or-more appearances should rise toward the 0.959 that
one appearance already reaches.**

> **And it casts doubt backwards.** Every chain result was measured with a
> contiguous layout that guaranteed one appearance per symbol — the degenerate
> case. How much of decision 92's 1.000 survives an entity appearing twice is
> **not known**.

## The hop mechanism (decision 101)

**The hop mechanism REPLACES retrievals, it does not COMBINE them.**

    replace   follow a pointer, keep only where you land   -- chains, works
    combine   hold two things and apply a rule to them     -- kinship, absent

On `chains.py` token adjacency *is* the relation graph, so replacing is
sufficient and it reaches 1.000. On `kinship.py` — typed relations that compose
by rule — turning hops on makes it **thirteen times worse** (0.347 → 0.027), and
90% of hop 2's landings are a *person* rather than the next fact's relation.

Fixing the traversal would not fix it. Composing `R1` with `R2` needs both held
at once, and each hop overwrites `retrieved` while the readout consumes one
vector. **There is nowhere for `R1` to be while `R2` is fetched.**

This narrows decision 92 rather than contradicting it: zero-shot depth
generalisation is real and is generalisation over *repeating one replace*.

**That mechanism is now built** (`hop_accumulate="concat"`, decision 102) and it
holds both. `replace` is still the default so every earlier number stands.

    task hops 2, floor 0.470      hops 1  0.347   hops 2 replace  0.027
                                  hops 2 concat   0.347
    task hops 3, floor 0.282      hops 1  0.120   hops 3 concat   0.180

Concat matches the one-hop model exactly at two hops, and that is diagnostic
rather than disappointing: hop 2 retrieves a PERSON, which says nothing about
the second relation, so the readout learns to ignore it and the model reduces to
its one-hop self.

### TRAVERSAL IS NOW THE ONLY BLOCKER, and its cause is known

To reach the second fact the model needs `M`, the middle person, which lives in
fact `[S, R1, M]`; then `key(M) -> R2`. The obstacle: **`key(R1)` is superposed
across every fact sharing that relation**, so following it retrieves an average
of every such object.

`context_keys` already binds `(previous, token)` pairs, which would make
`key(S, R1) -> M` a distinct binding. Whether a hop can *construct* that pair
key is the next design question.

> Also worth knowing: **concatenation was expected to fail and does not.** The
> argument — a linear readout over `[r1, r2]` is additive, composition is not —
> confuses a functional form with a classification problem. Measured over the
> whole rule table: concat **1.000**, product 0.812, convolve 0.812. Sixteen
> rules in a wide space are linearly separable whatever the labels do. Whether
> that holds with far more rules is unsettled.

## The sharpest thing to know, from decisions 89 and 92 together

    over DEPTH        generalises ZERO-SHOT to a depth never trained on (0.992)
    over TERMINATOR   does not generalise at all -- halt_w sits +8.3 sd on one
                      specific token's value vector

**Same gate, same vector, opposite answers.** The rule it applies is general —
"take the hop whose lookahead is a marker" says nothing about depth, so a model
trained on depths 1–2 answers depth-3 questions without ever seeing one. The
feature it applies that rule to is a memorised token.

So the next work is **not** in the hop machinery, which composes and
generalises. It is in what makes a retrieval recognisable as terminal.

**And decision 93 says that cannot be fixed with retrieval statistics.** The best
linear separator over five identity-free features (norm, entropy, peak, gap,
kurtosis), *fitted with the labels*, reaches **0.628** against 0.500 for
guessing. The token-identity gate reaches 1.000. No gate learning from a
downstream error beats a classifier that was given the answers.

**The reason points somewhere useful: `Wv` is frozen and random**, so two tokens'
value vectors are independent draws and there is no structure for a "class of
terminators" to live in. Nothing to generalise over. That is a limitation of
frozen random embeddings, not of gating.

### That experiment has now been run, and it failed twice over (decision 94)

`value_lr` updates `wv[target]` at **scored positions only**, and the chain task
scores one position whose target is always a chain symbol — so a separator's
value vector can never move. As written the experiment was a no-op.

Scoring every position fixes that and **costs almost everything**: depth-2
accuracy 1.000 → 0.117. Four separators cost 0.008; all-position training costs
0.883. And `value_lr` does not build a class either — at high rates *everything*
converges (ordinary symbols 0.382), which is global collapse, not role structure.

## READ THIS BEFORE TAKING ANY OF IT TO REAL TEXT

**The gate is trained by the same error as the readout, so it learns the depth
that dominates the training distribution — not the depth a question needs.**
Measured: gate weight on hop 1 at a depth-2 answer position is 0.0102 under
answer-only training and **0.3034** under all-position training, because at
almost every position the next token is exactly one hop away.

Real text is trained at every position. So a gate learning by this route settles
on "one hop", and **composition would be built, correct, and never used**. Any
future result on text must show the gate is actually gating, not merely that
accuracy moved.

**Decision 95 pins down why, and it is a mechanism problem.** The gate trained
answer-only puts 0.0171 on hop 1 at the query (correct) and **0.4712 in the
body** — a coin flip where serving the body needs ~1.0. It is not outvoted, it
is **conflicted**: the body wants hop 1, the query wants hop 2, and the gate is a
linear score on the *lookahead retrieval alone*, which can look identical in both
cases. Reweighting the training signal cannot fix a function that cannot see the
cases apart.

### That is built (`gate_reads_key`, decision 96) and it is a delay, not a fix

The key **modulates** which rule the gate applies rather than adding to the
score — an added key term is identical across hops and the softmax removes it
exactly, so the proposal as first written would have done nothing.

It works in the intended direction: the one-rule gate separates query from body
**backwards** under all-position training (−0.241) and the selector flips the
sign (+0.118). All-position accuracy goes 0.117 → 0.400, answer-only stays
1.000.

**But the real finding is that accuracy DECAYS with training:**

    per depth  epochs   one rule   reads key
          100       1      0.750       0.833
          400       1      0.250       0.683
          400       2      0.100       0.383

The model does not fail to learn composition under all-position training — it
**progressively unlearns it**, as the body's error accumulates and drags the
shared gate toward one hop. The selector slows this; it does not stop it.

### The decay is REAL, not a task artifact (decision 97)

The obvious suspect was density — with one question per sequence, ~98% of the
next-token error says "take one hop". `n_queries` raises that share. It does
**not** remove the decay (+0.117, +0.188, +0.169 across densities, no trend).

What it does is raise the **level**: one question per sequence collapses to
0.033, *below* the 0.125 floor — confidently wrong — while four or eight
stabilise near 0.38. Keep the density; it is not the fix.

> **A first run of this leaked and its numbers were reported before the guard
> caught them.** A query block writes `a` beside `c`, so it STATES `a -> c`;
> with several blocks an early one answered a chain a later one asked about.
> The leak grew along the very axis being swept and produced a clean, plausible,
> wrong curve. Fixed by sampling asked chains without replacement. **Third time
> a task change has looked inert and been wrong — write the guard before the
> measurement, not after.**

### The decay is FIXED — the gate needed its own objective (decision 98)

More inputs (96) and more density (97) both failed. What worked was changing
what the gate learns from. The mixture objective **averages conflicting
demands**; `gate_objective="which_hop"` asks a question with the same answer
everywhere — *which hop would have been right here?* — from a label available
locally at any scored position.

    density 8, all-position    100x1  200x1  400x1  400x2   decay
    mixture                    0.515  0.333  0.290  0.346  +0.169
    which_hop                  0.404  0.406  0.412  0.404  +0.000

**Flat.** Better on both axes at every density, and it undoes 97's advice: with
a working objective, one question per sequence is the *best* row, so density was
compensating for a broken objective rather than fixing the task. (Quote the
density-8 row — density 1 is 60 samples and visibly noisy.)

**~0.40 is still not good.** Answer-only training reaches 1.000. The gap between
a marked question and an unmarked stream is still most of the problem; this only
stops it widening.

### THE NEXT THING — and it is a question about the OBJECTIVE, not the mechanism

All-position (next-token) training was never required by the goal. It was
imported from how LLMs train, and it costs 1.000 → 0.40. The goal is relational
reasoning, so the self-supervised signal should be relational too:

**Masked-link prediction** — state facts, hide one, predict it. Fully
self-supervised (no marked questions), but relational rather than sequential.
That is a different objective from "predict token t+1" and much closer to what
the task is about.

Benchmarks should follow the same logic: **CLUTRR** (train short chains, test
longer — the external check on our 0.992 zero-shot depth result), **bAbI**, and
knowledge-graph link prediction. Keep bits/char as a diagnostic that the
substrate works, not as the score that matters.

Also unexplained — 4 separators beat 1 under all-position training (0.683 against
0.117), a real gap in the account rather than a detail.

**C4 is still untested, and that is now the open problem.** Two attempts failed
to build a case where continued learning helps: decision 91 because a departure
costs capacity rather than currency, decision 92 because the mechanism already
generalises. Neither says perpetual learning is worthless — both say **this task
is too easy to need it**. A real C4 test needs something the model genuinely
cannot already do.

### Operational lessons, now rules in CLAUDE.md

All learned the expensive way in one night:

- The mutation harness **takes the tree exclusively** — a concurrent test run
  reported 7 phantom failures in a file nobody had touched, then 3, a different
  set. The tell was that the failures moved.
- **Stopping the background task does not stop the harness.** It kills the shell
  wrapper and leaves the Python process editing source. Two full check runs
  passed against a tree that was still being mutated; passing under a live
  harness is luck, not evidence.
- Killing it mid-swap **leaves a mutation live on disk**. `--verify` caught it.
- **Renaming a variable can leave a mutation stale**, which reports as `65/66`
  rather than as a failure. A mutation that cannot be applied is not passing.
- **Structural tests could not see either gate defect.** Read counts, refusals,
  a zero-gain control and store invariance all held while the mechanism did the
  wrong thing — because each defect still beat the baseline (0.707 and 0.773
  against 0.500). It took a behavioural test on the half that fails.

---

## The headline result, and the correction that demoted it

**g11-05: our model does not learn from more text.** Sixteen times the training
data, on the standard benchmark, with a control that fired.

**Read decision 63 before quoting this.** The finding is TRUE and the sweep is
not evidence for it: every point sat above the model's saturation point, so a
flat exponent was guaranteed by the grid. The honest version — cheaper and
stronger — is the saturation curve two sections down.

    arm           n=62,500     n=125,000     n=250,000     n=500,000   n=1,000,000
    backprop   4.306+/-0.021 4.283+/-0.036 4.157+/-0.013 4.091+/-0.015 4.049+/-0.028
    context    5.775+/-0.014 5.770+/-0.009 5.759+/-0.011 5.764+/-0.011 5.763+/-0.009
    single     5.529+/-0.020 5.530+/-0.004 5.505+/-0.001 5.513+/-0.001 5.518+/-0.010

      backprop   b = -0.0243   R2 = 0.96    the control, and it FIRED
      context    b = -0.0008   R2 = 0.60    FLAT
      single     b = -0.0010   R2 = 0.33    FLAT

**This is not the Filipovich shape.** Their local rule lost the exponent but kept
one (DFA -0.040 against backprop -0.071). Ours is zero.

**It does not condemn local learning** — the delta rule on `Wo` is the exact
gradient for a single linear readout. **It says the architecture is saturated on
every axis tried**, which removes "we are just small" as an explanation for the
gap to the baselines. That was the last one available.

## Where the model is

    uniform                        6.000 bits/char
    OUR MODEL, width 128           5.494
    OUR MODEL + exact cache        5.311   (width 128, 128 slots)
    unigram (letter frequency)     4.829   <- BEATEN, see below
    2-LAYER READOUT, prequential   4.540   single pass, no split, no temperature
    backprop attention, width 16   4.197   (our own baseline, ~10k params)
    bigram                         3.583
    trigram                        2.951
    char-LSTM (published)          ~1.45

**Every component passes its capability test in isolation and the whole fails.**
The failure is the composition — superposition destroys the per-item information
downstream needs.

## READ THIS BEFORE INTERPRETING ANY RESULT ON THIS CORPUS

**The model is frozen random features into a linear probe. That is not an
analogy, it is the architecture.**

`r = M @ key` depends only on `Wv` and the keys, both drawn once and never
updated. So the retrieval is completely independent of `Wo`, and `Wo` — a single
`vocab x d` linear map — is the only thing that learns across a corpus. The
store itself is `np.zeros((d, d))` inside `run`, rebuilt every 128-character
chunk (a deliberate, guarded property: `local-memory-persists-across-sequences`,
correct for the recall tasks, inherited unexamined by the corpus experiments).

**The model converges at about 16,000 characters and then stops.** Three seeds:

    chars     4,000   8,000  16,000  32,000  62,500  125,000
    bits      5.570   5.543   5.527   5.523   5.531    5.531   (spread ~0.04)

A linear probe on fixed features converges once it has enough samples to
estimate its coefficients, and no amount of further data changes the features.
**g11-05 swept 62,500 upward — entirely above saturation — so its flat exponent
was guaranteed by the grid** (decision 63). The rule now in CLAUDE.md: probe the
BOTTOM of a scaling range locally before spending a matrix on it.

## The synthesis that should drive what comes next (decision 69)

    mechanism            effect on LEVEL      effect on SLOPE
    width, 4x                    +0.089                 none
    exact cache, 128 slots       +0.19                  none
    sparse keys, k=4             +0.15                  none
    pair keys                    -0.23                  none
    trained Wv                   -0.45                  none
    carry store (training)       -0.15                  none

**Six mechanisms, three helpful, and not one changes the shape.** Stacking every
positive result reaches roughly 5.1 bits — still worse than a unigram at 4.829,
and still flat. The backprop baseline moves 0.95 bits over the same range and is
still moving at 1,000,000.

So the question is no longer *what raises the score*. It is **what would make the
loss keep falling with data at all** — and the one arm that does keep falling
differs in exactly one respect nothing here has varied: its parameters are
trained through a COMPOSED function. Ours are not.

**Two rank measures are in play and confusing them cost a wrong conclusion.**
Note 035 uses stable rank `‖S‖_F²/‖S‖₂²`; decision 65 used participation rank
`exp(H(σ))`. Under note 035's measure the retrievals sit at 4.06 — the "rank ~3"
reading is right. **Name the measure next to the number** (decision 66). And rank
does NOT predict bits in general: sparse keys have the lowest rank and beat the
baseline, pair keys have near the highest and are the worst (decision 67).

## The through-line, which now has two measurements behind it

**`r = M @ key` is a SUM, and nothing applied after a sum recovers what the sum
destroyed.** Readout bias, competitive retrieval, orthogonal updates and pair
keys all failed for this one reason — each has a test pinning it, **do not
re-propose them without reading it.**

g11-05 is the second, independent measurement: the store holds bigram statistics
(note 033, cosine 0.9455), a real bigram scores 3.583 where we score 5.5, and
more text sharpens counts whose information is destroyed before the readout sees
them. **A bottleneck downstream of the statistics cannot be widened by improving
the statistics.**

## In flight

**g11-06, run `30309304474`** — the measurement that turns the inference above
into a finding. Same data axis, four arms, with the exact cache as the one
component that does not sum, against a **state-matched** superposed arm (width
143, 20,449 numbers, against the cache arm's 20,480). Predictions are registered
in `experiments/sweeps/g11-06-*.txt` before dispatch.

**A structural fact recorded before the run:** the cache is reset on every `run`
call, and `run` is called once per 128-token chunk. It is a within-sequence
working memory and cannot accumulate across the corpus; only `Wo` persists. So
the prediction is that **the cache arm is also flat** — and if it is, the next
mechanism is specific: **make the cache persist across chunks.**

## What changed structurally this session

- **A second seam.** `openplexus/retrieval.py` puts the sum, the exact cache and
  the settling loop behind `begin/read/observe`, composed rather than branched.
  Four config fields and two branches became three objects. Verified
  behaviour-preserving against golden values captured across nine configurations
  BEFORE the refactor. `run()` went 584 → 526 lines.
- **`tools/mutate.py --changed`**, now in the pre-commit list. `--verify` only
  checks that a mutation's original text is present; whether the suite would
  CATCH a break is the full harness, which is CI-only. Two cache mutations had
  been surviving for at least two commits because of that gap.
- **Sweeps can no longer fail silently.** 40 workflows piped a summariser into
  `tee` with no `pipefail`, so any crash produced a green run and an empty
  summary. `check_workflows.py` now refuses that and refuses a job running a tool
  it never installed.

## Two open architectural questions, both live

**1. Item-partitioning vs dimension-partitioning (decision 61).** `partitions`
currently splits the store by DIMENSION, so every node computes the same
`M_slice @ key_slice` and **inherits the sum**. Partitioning by ITEM instead
makes a read a SELECTION across nodes — which is what the exact cache already is
at one machine's scale. It is also partial-tolerant by construction: lose a node
holding dimensions and the retrieved vector has holes; lose a node holding items
and you take the best of whoever answered. g11-06 bears directly on this.

**Node SIZE is not what is binding** — width 16→128 in a single process is flat
and data 16x is flat, so making a node bigger cannot help when making the whole
model bigger does not.

**2. The readout still violates C1.** `answer = parts.sum(0)` sums across every
partition — the globally synchronised step the first constraint forbids. Known
since note 009 §4, still outstanding. Not a future design question; a current bug.

## Constraints — note the amendment

C1/C2/C3 are in GOALS.md. **C1 was amended 2026-07-27** at John's direction: the
real constraint is *"does it work over the internet"* — bounded bytes per hop, no
barrier that stalls when a participant is slow or gone. A global all-reduce is
still out even at twelve bytes. Everything measured before that date was measured
under the stricter rule.

**Goal ordering, restated by John this session:** AGI is primary; being an LLM
replacement that runs on distributed consumer machines is secondary and must not
compete with it.

## Working agreement with John

- **Blanket permission for architectural decisions**, and he extended it: the
  "pending decisions" list is a REPORT, not a gate. If he does not answer,
  decide and proceed. Document it in DECISIONS.md and say which calls were made
  without him.
- **List pending decisions at the end of every response** — he reads from his
  phone.
- He is not deeply versed in modern ML internals. Explain plainly, keep the
  numbers, do not hide bad news.
- **Scheduled wake-ups DO NOT FIRE in his setup.** He phones into a desktop
  session, which keeps it non-idle; cron never fires, and `ScheduleWakeup` was
  tried and also did not. **What works is a persistent `Monitor`** emitting a
  heartbeat line — that path delivers. Do not end a turn relying on anything
  else.
- Standing operational rules: sweeps are GitHub Actions DISPATCH-ONLY via
  `gh workflow run`, one matrix at a time, cost stated first and estimated **from
  the most expensive cell**. Nothing heavy runs locally. **Never use bash
  heredocs.** **Never `git commit -m` with backticks** — write the message to a
  file and use `git commit -F`. Six checks before every commit: `mutate.py
  --verify`, `mutate.py --changed`, `unittest discover`, `check_workflows.py`,
  `check_rails.py`, `check_duplication.py`.
- **Batch commits when a sweep is in flight** — every push queues seven check
  jobs ahead of the matrix, and a second push cancels the first run.

## The standard this project holds itself to

Pre-register predictions before every sweep and score them honestly, including
the refuted ones. A mechanism measured only on the task it was designed for is
not measured. When a mechanism adds state, compare against a model given the same
amount of state — g10-09 was retracted for missing exactly that, and it is why
g11-06 has a `matched` arm.

## Queued, in the order John set

1. **The composition sweep.** He approved it as its own sweep. Nothing currently
   measures composition and bits-per-character cannot. Proposed task: bind A→B
   and B→C separately, then ask for A→C — cheap, unambiguous, and it probes
   superposition directly, since the sum is exactly what would destroy the
   intermediate. **Not yet built.**
2. **Unfreezing the value projections** — after the cache line.
3. **Input and output.** John wants to talk this through rather than have it
   decided. His framing: if the AGI goal wins, inputs should look like a body —
   a loop with consequences, not a passive feed. Related work of his own:
   `Mako88/Persistence` (self-curated memory, a sensory block, scheduled
   wake-ups) and a robot project he would like to wire up. The output side is
   where C1 is already violated, so it is not purely speculative.
