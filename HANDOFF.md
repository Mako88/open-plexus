# Handoff — state of play, 2026-07-27 (evening)

**For the next session.** Read this, then `GOALS.md`, then `BACKLOG.md`. The
notes in `docs/notes/` are the reasoning; `DECISIONS.md` is the running log John
reads — entries 56–69 are from this session and are the current state. This file
is a snapshot and goes stale — trust the notes and DECISIONS over it.

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
