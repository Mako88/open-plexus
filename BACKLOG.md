# Backlog

Things worth doing that are not being done right now, and why. An item here has
been thought about at least once; an item that has not is not here.

Ordered by what would change the most if it turned out well.

---

## READ THIS FIRST — what is open, what is settled

This file grew a great deal in one session and now mixes a todo list with a
record of what was settled. Nothing below is deleted, because the reasoning in
the settled sections is why the open ones are shaped as they are. But a reader
looking for work should start here.

**OPEN, in order of what would change the most:**

1. **A decision only John can take: fix `reward_recall`, and how?**
   [Note 027](docs/notes/027-the-task-leaks-the-answer-through-its-layout.md).
   The nearest binding before a reward is always the rewarded one, 160/160.
   **Measured as inert** — our binding-detection cannot exploit it past delay 1 —
   so this is correctness, not urgency.

   **And the one-line fix does not work.** Randomising the gap was measured and
   leaves the leak intact at every jitter up to 0.9, because the discriminator is
   not the lattice: the reward sits a CONSTANT `delay - 1` after its own binding
   whatever the spacing does. The fix that would work is randomising the delay
   **per rewarded pair**, which stops `delay` being a swept axis at all — so it
   changes what the task is, not just its numbers. Bigger than a re-baseline.
2. **`g9-11` is running**: how far the union's window needs to reach. The last
   untested dial on the best mechanism here.
3. **Can anything identify WHICH binding without being told the delay?** The
   sharpest open question and it needs a probe, not a sweep. See *The sharpest
   open question BEFORE 027*.
4. **A decision only John can take: what belongs in GOALS.md?** Its gating
   section is corrected but is now the largest live-investigation block in a
   document that may be meant for settled results only. See *GOALS.md gating
   section — CORRECTED*.
5. **Everything under *Tasks beyond MQAR*** — the corpus benchmark is the first
   evidence for goal 2 and nothing on this page is closer to the actual aim.
6. **The testbed has never run a gated model.** Everything measured is one
   process. See *Built but not finished*.

**SETTLED THIS SESSION** — kept below for their reasoning, not as work:
the width question (*ANSWERED: eight dimensions*), why g9-08 asked it wrong
(*A small NODE, not a narrow NETWORK*), the combined gate being built
(*The gate that reads both signals*), the summariser port, the three meta-tests,
the masked-fade no-op, and both GOALS corrections.

---

## From the sources, not yet acted on

Four ideas mined from [John's source list](https://sites.google.com/view/sources-memory/).

**The list is a rich source of MECHANISMS and a poor source of TASKS.** Forty-five
sources, and the experiments in them are neuroimaging and stimulation protocols —
7T fMRI of taste quality, odour exposure in mice, intracranial recordings during
speech. Those measure brain activity in a subject. None of them ports to a
benchmark for a computational memory, and no behavioural task in that list can be
adapted into one.

> **Read the paper before acting on any of these.** Everything below comes from
> the source page's own summaries. [Note 010](docs/notes/010-tagging-and-capture.md)
> exists because Lehr et al. was *read* rather than guessed, and reading it
> changed what was built.

### 1. Replay, or an offline phase — the most relevant thing on the page

Filipchuk et al. (2022), *Awake perception is associated with dedicated neuronal
assemblies*, compares assemblies in wakefulness against anaesthesia. The
transferable idea is not their task; it is that **an offline phase exists at
all**.

Why it matters here more than anywhere else: the gating problem is precisely
that *the storage decision has to be made at the moment the least is known*.
[g8-01](experiments/sweeps/g8-01-a-gate-without-an-oracle.txt) measures both
implementable gates recovering approximately none of the oracle's advantage.
Replay is the natural completion of tagging and capture — **if you cannot tell at
storage time what mattered, revisit the traces later, when you can.**

We have no offline phase, no replay, and no mechanism that revisits anything.

### 2. Multiple timescales — NO LONGER SPECULATIVE, it is a live bug

Zenke & Gerstner (2017) is titled *Hebbian plasticity requires compensatory
processes on multiple timescales*. `lasting_cap` came from this paper, applied
the compensation to **one** store, and left the other unbounded.

[Note 018](docs/notes/018-the-fast-store-has-no-brakes.md) is the consequence.
The fast store is a geometric series in `decay`, so a recurring token drives its
entry toward `1 / (1 - decay)` — about **277×** a single binding at the half-life
these sweeps use. Retrieval is linear in that, and the delta-rule update is
**quadratic**. Measured without training:

    zipf_s 0.0   |memory| 114   max |retrieved|  137
    zipf_s 2.0   |memory| 967   max |retrieved| 3452

The readout then diverges to NaN, reproducibly, and it already contaminated
g8-02's bottom rows.

**This is not about Zipf.** Zipf supplies repetition; so does real language, so
does a sensor reporting the same reading twice, so does a quiet period on a node.

**Built, and answered without the grid.** `memory_cap` is in, with the four
predictions from note 018 registered first. Predictions 1 and 3 held and
**prediction 2 is refuted in the direction that matters**: the cap does not
increase the number of usable cells, it decreases them, which is why the full
matrix was never dispatched — see
[g8-04](experiments/sweeps/g8-04-brakes-on-the-fast-store.txt). The loudest
prediction was that it must NOT improve the gating result. It did not.

The cascade the paper actually argues for — more than two timescales — remains
untested and is a separate item.

### 3. Sequential neuromodulation — our gate may be too crude a copy

Ang et al. (2021), *The functional role of sequentially neuromodulated synaptic
plasticity in behavioural learning*. Plasticity gated by a **sequence** of
neuromodulators rather than by one signal crossing one bar.

Our salience gate is a single threshold on a single scalar, and it loses.
[Note 013](docs/notes/013-salience-and-the-missing-body.md) established the
signal is real — queries fire at 7.6x the filler rate — and drowns anyway. A
cruder-than-biology implementation is a candidate explanation that has not been
eliminated.

### 4. Heterogeneous node sizes

Barbas, Zikopoulos & John (2022), *The inevitable inequality of cortical
columns*, and Herculano-Houzel et al. (2008), *The basic nonuniformity of the
cerebral cortex*. Both argue directly that columns are **not** uniform.

Every node in every sweep so far is the same width as every other.
[g7-03](experiments/sweeps/g7-03-how-to-spend-a-machine.txt) closes by naming
this as the obvious next step and not touching it. `slices_for` currently
*refuses* uneven splits, so this needs the model changed before it can be asked.

---

## Nothing notices if the masked fade reads the wrong step

Fixing the stale `storage-mask-off-by-one` mutation exposed a second branch with
no mutation at all: the fade `decay_when_masked` adds. A mutation pointing it at
`store[t - 1]` **survives the whole suite**.

It is not obviously a real gap. Under the masks
[the tests use](tests/test_decay_when_masked.py) — periodic, `MASK[::5]` — every
write loses exactly one fade, and a uniform rescale of the store provably cannot
change an argmax. That is the same fact
[the tests already pin](tests/test_decay_when_masked.py) in
`test_fades_after_every_write_are_invisible`. So the mutation may be a genuine
no-op on symmetric masks rather than a hole in the tests.

The distinguishing case is an **asymmetric** mask — some writes adjacent, some
isolated — where bindings lose *different* numbers of fades and the effect is a
reweighting rather than a rescale. Sketched by hand: with writes at
`{10, 11, 60}`, the early pair lose two fades each and the lone write loses one.

**DECIDED: it is a no-op, and the asymmetric-mask guess was wrong.**

Take consecutive writes at `a` and `b`. The mechanism fades on masked steps,
which in `(a, b]` are `(a, b)` — `b - a - 1` of them. The mutation fades where
the PREVIOUS step was masked, which in `(a, b]` are `(a + 1, b]` — also
`b - a - 1`. **The total decay applied before every write is identical, whatever
the mask.** Between writes the two stores differ by at most one factor of
`decay`, and a uniform rescale cannot move an argmax.

So the branch is genuinely unobservable through predictions and the mutation does
not belong in the harness. `test_shifting_the_fade_guard_by_one_step_is_a_NO_OP`
records it over three mask shapes, so the next person does not rediscover it.

The route to that answer is worth keeping too: the first test written for this
compared two different MASKS, which changes what is stored rather than which step
the guard reads. It passed under the mutation. No black-box comparison can see
this branch — the mutation shifts the fades while leaving the writes in place,
and no mask reproduces that pairing — so it took a reference implementation of
the update rule, which is now in the test file and is the only check here that
reads the fade schedule directly.

---

## Meta-tests: what is worth checking about the tests themselves

John asked whether the suite could police its own quality — duplication, methods
too long or over-parameterised, tests that do not validate what they claim.
Split by whether it would catch anything real here.

**Already built, and it is the strongest one.** `tools/mutate.py` is exactly
"tests that do not validate what they claim": 95 mutations, each a plausible
wrong version of a mechanism, each required to make the suite fail. It caught two
real things in a single afternoon — a stale target the source had moved out from
under, and a test that checked refused cells were excluded from selection while
missing that selecting on the gap is wrong among cells that *pass*. Nothing
generic would have found either.

**Worth building.**

- **Duplication across summarisers and experiments — BUILT, and the
  justification above was wrong.** `tools/check_duplication.py` AST-normalises
  function bodies, hashes them, and ratchets against a baseline. Run over the
  pre-port tree at `9457c16` it finds **zero** of the five copied refusals: they
  had already diverged, and divergence is what defeats a structural hash. So it
  catches copies that have NOT drifted — the harmless ones — and is blind to the
  ones that have. Prevention, not detection.

  It earns its place anyway: within minutes of being written it caught
  `load_baseline` copied between it and `check_rails.py`, by the author of a tool
  for finding copies. Seven legacy pairs in `experiments/` are exempt and are
  real — four `score` functions sharing one shape, two `main`s, two `epoch`s.
  Worth collapsing when one of them next needs editing, not before.
- **Repo-specific rails**, which are where the value is, because generic lint is
  a solved problem and these encode the failures that have already cost results:
  every summariser computing a recovery ratio imports `tools.recovery`; every
  sweep file has a PREDICTIONS section and a COST section; every experiment goes
  through `experiments/harness.py` so `refuse_if_mutating()` cannot be skipped;
  every workflow under `sweep-*.yml` is `workflow_dispatch` only (this one exists
  as `tools/check_workflows.py` and is the model for the rest).
- **A test-quality check with teeth — BUILT as R4 in `tools/check_rails.py`,
  and it found a real one immediately.** Flags test methods containing nothing
  that can fail, following `self._helper(...)` into the same class so the
  gradient tests in `test_attention.py` are not false-positived for putting
  their assertion in a shared `_fd_check`.

  Two real hits, both FIXED rather than exempted, so R4 keeps zero exemptions:
  `test_the_first_position_can_never_consolidate` built a model, ran it and
  asserted nothing under a docstring naming a real property; and
  `test_releasing_a_lock_that_is_gone_is_not_an_error` passed by not raising.

  The second clause — flagging tests whose ONLY assertion is `assertIsNotNone`
  or `assertTrue` on a call result — is **not built**. No instance exists in the
  repository today, so it would be a rail with nothing to hold and no way to
  know it works. Worth adding the first time one appears.

**All three meta-tests are now built**, and the honest scorecard is mixed: the
duplication check refuted its own justification, R4 found one real defect, and
the repo-specific rails found a stale mutation on their first run. The caveat
below survives all three.

**Not worth building as written.** Method length and parameter count are style
rules, and a fixed threshold turns into noise that gets suppressed. If they go in
at all they should be a **ratchet** — fail only when a number gets worse than
today's value, recorded in a checked-in baseline. `run()` currently takes eight
parameters and every one of them earns its place; a rule that failed on it would
be wrong, and a rule tuned to permit it would permit nine.

**The honest caveat:** none of these can check the thing that has actually gone
wrong most often here, which is a test that asserts a property the quantity does
not have. The scale-invariance meaning test asserted something false about
softmax and failed on first run; no linter can tell a true property from a false
one. Mutation testing is the closest available substitute and it is already in
place.

---

## Port the last four summarisers onto the shared rail

`tools/recovery.py` now holds the two refusals, `tools/summarise_g8_02.py` is
ported, and the drift that prompted it is fixed — it had **no floor check at
all** under a heading that named one, and selected cells by maximising
`oracle - none`, which prefers exactly the cells whose floor arm collapsed.

**Done — all four are ported.** `summarise_g8_01.py`, `summarise_g8_03.py`,
`summarise_g9_02.py` and `summarise_g9_03.py` now use `load`/`by_cell`/`assess`,
and their workflow lines run as `python -m tools.X` so the import resolves.
`by_cell` gained a named `metric` parameter, because g9-02 reports first-asks and
all-asks and averaging them hides the number that matters.

**It was not only deduplication.** Three of the four picked their learning rate
by maximising `oracle - none` — the third rule in `tools/recovery.py`, the one
that actively seeks out cells whose floor arm collapsed. All three skipped
collapsed floors first, so none was the worst version of it, but among surviving
cells the bias is still there. They now pick on an arm no prediction is about:
`capture-0` for g8-03, and for g8-01, which has no such arm, the rate where the
FLOOR arm scores highest — the exact opposite bias. g8-01 also used to SKIP
refused rows entirely, so a cell whose denominator was noise vanished rather than
printing `undefined`.

**Re-summarised, and exactly one headline moved.** The archived JSON was pulled
from Actions and run through both versions.

- **g8-01 — the size of the prize was overstated by about 3x, and GOALS said so.**
  Its "largest usable gap is 0.612" and "ungated arm falls to 0.46 at seq 768"
  both come from lr = 0.1, the rate that most depresses the ungated arm: at seq
  768, half-life 0.5 it means 0.387 against a trivial floor of 0.344. At lr = 0.02
  the same cell means **0.80** and the gap is **0.196**. GOALS is corrected. The
  finding is unchanged — recovery is approximately zero at every rate.
- **g8-03 — numbers shift slightly, conclusion unchanged.** capture-0 at 768 moves
  from -0.00 to 0.02 and capture-16 from -0.01 to 0.02. Every curve still falls;
  bounded pools still do not flatten relative to the unbounded one.
- **g9-02 — essentially unchanged.** Recovery 0.21/0.20/0.23/-0.13 becomes
  0.23/0.23/0.24/-0.13. The floor arm was being depressed by the rate choice
  (0.167 to 0.238 at delay 1) and the RATIO was robust to it anyway. Everything
  g9-03, g9-04 and the tag rest on stands.

The sweep files themselves are still not edited: they record what was reported at
the time, and the corrections live here and in GOALS.

**Open, and noticed while doing this: g8-03 picks a different learning rate per
sequence length**, so its `slope` column compares cells trained at different
rates. That was true before the port and is still true. Either the slope should
be computed within a single rate, or it should be named as a cross-configuration
comparison. It does not change the current conclusion, since every rate falls.

Each port is: swap the loader for `load()`/`by_cell()`, swap the hand-rolled
means-and-spread block for `assess()`, pass the right floor (**0.34375 for MQAR,
0.125 for `reward_recall`** — do not let one of them become the default), and
change the workflow line to `python -m tools.summarise_X` so the import resolves.
Thirteen summarisers also repeat the `glob`/`json` loader and can take `load()`
without touching their logic at all.

---

## Standard tests that already exist at this size

John asked whether anyone else has needed these test shapes. They have, and the
answer changes how the next tasks should be chosen.

### We are already using one, and did not say so

**MQAR is not ours.** It comes from the Zoology line of work on recall in
efficient language models, and was designed as exactly what this project needed
it to be: *a small synthetic whose behaviour predicts large-scale recall.* The
"is a tiny synthetic legitimate" worry was answered before we arrived.

### bsuite is our gate ladder, built by someone else

DeepMind's [Behaviour Suite for RL](https://arxiv.org/pdf/1908.03568v1) is a set
of **targeted unit tests, each isolating one capability**, with **smooth variation
in problem complexity rather than fixed-size challenges**. That is the gate ladder
and the difficulty dials, arrived at independently.

Two of its tests overlap what has been built here from scratch:

- **Memory Length** — how many sequential steps an agent can hold one bit, via a
  T-maze **parameterised by length**. That is `reward_recall`'s delay dial. It was
  derived here from note 017's requirements list; it already existed.
- **Credit assignment** — the paper's own illustration is *"an algorithm might
  completely fail at credit assignment beyond n = 20 steps."*
  [g9-02](experiments/sweeps/g9-02-a-gate-that-reads-its-own-input.txt)'s cliff is
  at delay 20. Coincidence in the number, but the shape of the finding is the
  shape their instrument was built to produce.

**The fit caveat is real:** bsuite assumes an agent with actions and a policy, and
this project has neither. The *task shapes* transfer; the suite does not. Running
it would measure the absence of a policy, which is already known.

**Worth taking:** the parameterised-length T-maze framing, so the delay results
are comparable to a literature rather than only to themselves.

### Toy models of superposition

Elhage et al.'s toy-models work is the closest existing treatment of
`SNR = sqrt(d/N)` and of the false-positive question note 020 could not test —
and it is **real-valued rather than binarised**, which is precisely the caveat
that made Theorem 20 only partly transferable to this store.

> **Read the paper first.** All of the above is from search results and one
> abstract. This entry exists to be acted on by reading, not by citing.

### The pattern, which is the actual finding

Three times now the sequence has been: derive a requirement from first
principles, build the thing, then discover the requirement describes something
that already exists.

- note 010 — tagging and capture, read properly *after* the mechanism was
  half-built
- note 020 — the capacity equation, derived empirically and checked a year of
  sweeps later
- this entry — a task built from a requirements list that turned out to describe
  bsuite's Memory Length

Each time the borrowed version was better specified than ours. **The cheap move
is to search for prior art at the point the requirements list is written, not
after the code is.** That is now a rule in CLAUDE.md rather than an observation
here.

---

## Cross-checks against work that already exists

John asked whether any fully modelled digital-neuron project could be compared
against. The most useful answer turned out not to be a neuron simulator.

### The capacity of a superposed store has an analytic theory, and we derived ours by hand

`SNR = sqrt(d / N)` is the single most load-bearing equation in this project. It
is why tiny nodes need selective storage, why the oracle works, and why note 015
predicts a bounded pool should flatten the recovery curve. **It was obtained
empirically here** — measured within 5% across a 16x range — and never checked
against anything.

It has a literature. Hyperdimensional computing / vector symbolic architectures
studies exactly this object: bundle many bound pairs into one vector, then ask
how many can be recovered. The degradation has a name there — the **superposition
catastrophe** — and there is formal work on capacity, e.g.
[Capacity Analysis of Vector Symbolic Architectures](https://arxiv.org/abs/2301.10352).

**Worth doing because it can only be informative.** If the analytic result agrees
with `sqrt(d / N)`, the most important equation here stops being a local
observation and starts being an instance of a known law, with its assumptions and
its failure conditions already worked out. If it disagrees, one of the two is
wrong about our regime and finding out which is worth more than another sweep.

> **READ THE PAPER.** The above is from search results and abstracts. Rule 1: a
> summary tells you what a result is *called*, not what was run. Note 005 exists
> because a borrowed claim that gated a design decision turned out to describe a
> variant this project cannot use.

### Digital neuron models, for the record

- **Spaun** (Eliasmith, on Nengo) is the closest thing to what was asked for: a
  large spiking model with an actual *behavioural battery* — digit recognition,
  serial working memory, and an inductive-reasoning task. The only large neuron
  model with published per-task behaviour rather than biophysics alone.
- **OpenWorm** — the complete *C. elegans* connectome with locomotion as
  measurable behaviour. Complete, and too far from anything here to compare.
- **Blue Brain**-style cortical column simulations — highly detailed, no
  behavioural battery.

None of these is a benchmark we could adopt. Their value would be as a sanity
check on capacity and scaling claims, and the VSA literature above does that job
more directly.

---

## Can a better signal buy a smaller pool? — the live question

[g9-06](experiments/sweeps/g9-06-is-the-tag-capacity-starved.txt) found the tag
works on its own: **slots 32, fade 0.95 recovers +0.16 at every delay, spread
0.01**, and +0.16 at delay 20 is the first positive result at that delay anywhere
here. It does not beat a MATCHED window (0.16 against 0.23) and it beats an
unmatched one by 0.40, which is the case a node cannot distinguish.

**But the signal is not what makes it work.** `tag` minus `tag-strongest` is
+0.003 at that cell and +0.222 at `slots 16, fade 0.99`. The direction g9-04
measured buys height only where the pool is too small.

That turns the question into John's: **a pool of 32 is not a tiny node.** If a
better signal reaches +0.16 at slots 8 or 16, the mechanism scales down to the
devices this project exists for. If it does not, bounded capacity plus a fade is
the whole mechanism and the signal was a detour.
[g9-07](experiments/sweeps/g9-07-a-tag-that-knows-how-big-its-store-is.txt) asks
exactly that, with `tag_relative`.

## A SMALL-SLOTS COMBINED SWEEP WAS NOT DISPATCHED, and the control is why

> **Naming note.** This section is about a sweep that was proposed and dropped.
> The name `g9-11` was later used for a DIFFERENT sweep — the union's reach —
> which IS dispatched. Nothing below refers to that one.

g9-10's best cell is `combined` at `slots` 4, +0.26 -- the highest tag-family
recovery anywhere here -- and `slots` 4 is the BOTTOM EDGE of that grid. By the
rule that caught g9-05, that is a pinned axis and wants a follow-up.

A counting control says the follow-up would find flatness. Recall x precision,
window reach 8:

    delay 8    window alone 0.112   combined 1/2/4/8: 0.109 0.108 0.106 0.096
    delay 20   window alone 0.000   tag 8 alone 0.008   combined 8: 0.003

**At delay 8 the combined gate IS the window.** Recall is pinned at 100% by the
window -- a union cannot subtract -- and precision approaches window-alone from
below, so adding tag marks only dilutes. **At delay 20 it is WORSE than the tag
alone**, because the union adds the window's nine useless writes per capture
without adding recall.

So a matrix over `slots` 1-8 would measure a flat row. Not dispatched. The
control cost four minutes and the rule it serves -- controls before dispatch --
is exactly what it is for.

**One thing the control does NOT explain and it is worth chasing.** It ranks
window-alone above combined at delay 8 (0.112 against 0.106), but g9-10 measured
combined at +0.26 against the window's +0.23. So recall x precision is a good
ordinal predictor across capacities and NOT across mechanisms -- it got g9-10's
peaks right and this comparison wrong. Whatever the union adds at delay 8 is not
visible in what it keeps, which means it is in WHICH writes rather than how many.

### The axis nobody has swept

`REWARD_WINDOW` is frozen at 8 in every combined cell. A combined gate with reach
1 adds two writes per capture instead of nine, so it should keep the union's
delay-8 advantage while paying far less at delay 20. That is one dial, it is the
last untested one on this mechanism, and the control above cannot settle it
because the effect it would test is the one the control cannot see.

## THE TASK LEAKS THE ANSWER, and this outranks everything below it

[Note 027](docs/notes/027-the-task-leaks-the-answer-through-its-layout.md).
`reward_recall` lays bindings on a lattice — `generate` uses a CONSTANT gap, 31
at every sweep's settings — and places each reward `delay` steps after its cue,
where `delay` is at most 20. **A distance of 20 cannot reach past a spacing of
31**, so the nearest binding before any reward is ALWAYS the rewarded one.
Measured: 160 of 160, at delays 1, 8 and 20.

So "detect a binding, keep the most recent one before each reward" solves the
task exactly, from local signals only. No mechanism here uses that rule.

**No measurement is invalidated. The believed DIFFICULTY is.** g9-03's diagonal
cliff is a real fact about a window counting in STEPS while the answer lives at a
fixed number of BINDINGS. Note 026's 16.7% precision ceiling bounds gates that
rank on binding-ness alone, and a gate that also takes the most recent reaches
100% precision with one write per capture.

### What to do, in order

1. **Measure what the leak is worth.** A `nearest-binding` arm — detect bindings
   with the existing signal, keep the most recent before each reward. If it
   approaches the oracle, the leak is the whole story. If binding-detection is
   too weak to exploit it, the fix is merely correct rather than urgent. Cheap,
   and it is the honest next measurement.
2. **Then decide about the generator.** The fix is one line — randomise the gap —
   and it would invalidate the comparison set for nine sweeps. Rule 12 says a
   known-better setting can be worth deliberately NOT adopting until there is
   time to re-baseline. **That is John's call and the note does not make it.**

Three tests in `test_reward_recall.py` pin the leak as PRESENT, with a docstring
saying they are meant to fail once the generator is fixed.

## The sharpest open question BEFORE 027: can anything find WHICH binding?

[Note 026](docs/notes/026-the-tags-precision-comes-from-its-fade.md) puts a
ceiling on this line. One binding in six is rewarded and nothing local separates
them, so a PERFECT binding-detector tops out at **16.7% precision** — a property
of the task, not of any mechanism. The tag at its best capacity is at **70% of
that ceiling** and a matched window is at **76%**.

So binding-detection is close to exhausted, and a better signal competes for at
most another 30% of 16.7%. **The remaining room is entirely in identifying WHICH
of the six**, and only two things do that today: a window, by being told the
delay, and the tag's fade, by guessing a time constant.

**Nothing does it from the data.** If nothing can, then `reward_recall`'s ceiling
for any delay-agnostic gate is about 20% of the oracle's advantage — which is
approximately what the tag scores — and that is a result about the TASK rather
than about any mechanism, which would close this line honestly.

**It needs a probe, not a sweep**, and the shape is g9-04's: score candidate
local signals by AUC against the label "is the rewarded binding", among BINDINGS
only rather than against filler. g9-04 asked binding-vs-filler and found
retrieval strength; nobody has asked rewarded-binding-vs-unrewarded-binding with
the reward's arrival available as an anchor. Cheap, and it either finds the
signal or bounds the line.

## ANSWERED: eight dimensions, and the tag is not better on small nodes

[g9-09](experiments/sweeps/g9-09-a-small-node-in-a-wide-network.txt) ran, 15 of
15, node width chosen INTERIOR so the numbers are values rather than bounds.

    node        d1      d8     d20   spread     mean
      64     +0.16   +0.19   +0.19     0.04    +0.18
      32     +0.21   +0.21   +0.22     0.01    +0.21
      16     +0.14   +0.16   +0.17     0.03    +0.16
       8     +0.10   +0.10   +0.11     0.01    +0.11
       4   refused refused refused       --       --

**The flatness holds at every usable node size** -- spreads 0.04, 0.01, 0.03,
0.01 against the window's 1.85, 0.82, 0.45, 0.27. That is the property the tag
exists for and it now holds across an eightfold range rather than at one setting.

**Height peaks at node 32 and declines.** The hoped-for "better on small nodes"
does not happen. **The smallest node that can run the task at all is 8**, where
the gate still recovers +0.11.

**And the window's catastrophe is a WIDE-node phenomenon**: -0.10, -0.23, -0.56,
-1.62 at nodes 8, 16, 32, 64. Monotone, and backwards from the prediction.

### What is still open on this line, in order

1. **The working point was frozen at values chosen for `d_model` 32 in one
   process.** Named as the standing risk before dispatch and still untested: the
   decline from node 16 to 8 is exactly where a mistuned capacity shows first.
   A `slots` x `node` sweep at fixed delay would settle it, and is cheap.
2. **The combined gate has never been measured.** Built, tested, mutated, and its
   pre-dispatch control says it only differs from the tag at SMALL capacity --
   which is now known to be the regime that matters least for node size and most
   for the signal. Fold a `combined` arm into (1) rather than giving it a matrix.
3. **Nothing here has run on a real network.** One process with a split readout.
   The testbed exists, works over an impaired link, and has never run a gated
   model.

## A small NODE, not a narrow NETWORK -- g9-08 asked it wrong

[g9-08](experiments/sweeps/g9-08-how-small-a-node-can-run-the-gate.txt) ran and
**nine of its fifteen cells are refused**: below `d_model` 32 the ungated model
scores 0.036 to 0.062 against a trivial floor of 0.125, so the task is impossible
there and no ratio means anything.

The error is in the sweep. `--width` sets `d_model`, the width of the WHOLE
network; a `d_model` of 4 is not a tiny device in a network, it is a four-
dimensional network facing a vocabulary of 73. [g7-02](experiments/sweeps/g7-02-tiny-nodes-and-clusters.txt)
did not do that: it held the network wide and split it with `partitions`, each
group carrying its own readout over its own dimensions, then asked one machine to
answer with `run(partition=...)`. Note 024 keeps the two quantities apart
correctly -- its crossover is `w * d` -- and the sweep collapsed them.

**g9-09 is the replacement and it needs a small build first.** Hold `d_model` at
64 or 128, sweep `partitions` over 1, 2, 4, 8, 16 so a node's slice falls from 64
to 4, and read ONE machine. `experiments/g9_05_the_tag.py` does not pass
`partition` to `run()` yet; that is the build. `--partitions` already exists in
the shared arg parser.

Two things from g9-08 survive and are worth carrying into it:

- **The window collapses to -1.62 at `d_model` 64, delay 20** -- one and a half
  times the oracle's whole advantage, spent making things worse, against the
  tag's +0.19 in the same cell. One cell, three seeds, so an observation.
- **`tag-strongest` is -0.03 there against the tag's +0.19.** The signal's
  direction pays again once something else is scarce, which is now the third
  setting showing that shape.

## Nobody has run a gate at a width this project cares about

[Note 024](docs/notes/024-what-the-gate-costs-a-tiny-node.md) costs the gate and
finds it affordable — a width-1 node at `d_model` 256 pays about as much again
for the gate as for its memory, and much less at any larger size. It also finds
the whole g9 line resting on `derived_keys`, which was adopted for bandwidth and
turns out to be load-bearing for storage too: without it a tiny node pays **187x**.

**But every g9 cell is `d_model` 32 in one process.** The recovery figures are
not measurements about tiny nodes at all, and the cost table says nothing about
whether +0.16 survives at width 1. g7-02 and g7-03 did this for the ORACLE gate
and found selective storage removes the sequence-length scaling and the
allocation problem; nothing equivalent exists for an implementable gate.

That is the sweep John's priority actually wants, and it is cheap: the tag at its
working point, swept over `partitions` or `d_model`, against the same oracle
ceiling. It should wait for g9-07 only because one matrix runs at a time.

## The gate that reads both signals — BUILT, not measured

`combined` protects the union of what the tag marks and what the window keeps.
The `tag_slots`/`reward_window` exclusion is lifted; a tag with `reward_window`
0 stays tag-only so the published g9-05 to g9-07 cells remain reproducible, and
the combined gate needs `reward_window` at least 1. Tests and two mutations are
in.

**The pre-dispatch control has been run, and it changed what this is.**

**At the tag's working point the union degenerates into the tag.** At `slots` 32,
`fade` 0.95 the tag already captures 32 of 32 rewarded bindings at delays 1, 8
AND 20, and keeps 929-965 writes; adding a window of 8 changes neither number. So
a combined-gate sweep at the working point would have measured the tag, twice, at
15 jobs. **The control cost seconds and saved that.**

**And "at least as good as both" is false.** At `slots` 8, `fade` 0.95, delay 20
the union captured 6 of 32 where the tag alone managed 8. Within an interval the
survivors are the set union; across intervals protecting more writes leaves a
larger store, which returns stronger retrievals, which changes what the tag marks
next. It is a feedback loop, not a set operation — corrected in the config
docstring and the arm comment, both of which claimed otherwise.

**Where it is still worth running: small capacity.** At `slots` 4 and 8 with
`fade` 0.99 the union beats the better arm by +3 of 32 at delay 20, which is the
only regime where the two mechanisms hold different writes. That is also the
tiny-node regime, so it belongs with the width question rather than as its own
sweep — fold a `combined` arm into a g9-08 follow-up rather than spending a
matrix on it now.

[g9-05](experiments/sweeps/g9-05-a-tag-that-fades.txt) ran, 32 of 32 cells, no
refusals. **The tag's rows that are flat are flat at zero, and its rows that are
positive have the window's cliff.** No cell is both, which was the entire claim.

But the tie is the thing to build on. At delays 1 and 4 the tag matches the
window — 0.24/0.23 against 0.23/0.23 — while keeping about a quarter as much.
Two mechanisms, the same recovery, **different signals**:

    weak retrieval  ->  this write is a binding           (AUC 0.22)
    recency         ->  this binding is the rewarded one  (the delay, exactly)

[Note 023](docs/notes/023-two-signals-and-only-one-of-them-is-about-value.md) is
the argument. A gate that reads both is the obvious next mechanism. The shape is
already implied: rank admission on weak retrieval **within** the window's reach,
or equivalently let the window's recency break ties among what the tag marked.

**Blocked on [g9-06](experiments/sweeps/g9-06-is-the-tag-capacity-starved.txt)**,
which is running. g9-05's capacity axis pinned — every delay chose `slots 8`, the
top of the grid — so its tag numbers are lower bounds. If a bigger pool produces
a row that is both flat and positive, the tag stands on its own and the combined
gate is not the right next thing.

**And it needs the validation lifted.** `tag_slots` and `reward_window` are
currently mutually exclusive by construction (decision 1 in DECISIONS.md), which
was right for measuring them apart and is exactly what a combined gate has to
change. Three lines.

## GOALS.md gating section — CORRECTED

Its closing claim, *"Nothing tried can tell it"*, was true when written and
g9-02 made it false. The section now carries g9-02, g9-03, g9-06's flat +0.16,
the tag-strongest catch that keeps its caveat alive, and the fact that none of it
is yet about tiny nodes. The refuted sentence is quoted and marked rather than
deleted, because it gated how everything below it was read. Three guards in
`tests/test_goals_consistency.py`.

**What was NOT taken on, and is still John's call:** the rest of GOALS. This
corrected one false claim in one section. Whether a live investigation belongs in
that document at all — or whether GOALS should record only settled results and
point at the sweeps — is a structural decision, and the corrected section is now
the largest live-investigation block in it.

## GOALS.md stops at g8-01 on the gating line

Its gating section ends with "nothing tried can tell it", which was true when
written. Since then g9-02 measured a reward gate recovering 0.21/0.20/0.23 at
delays 1, 4 and 8, g9-03 found the cliff is reach and has to be matched, and
g9-04 found the local signal and its direction. **None of that is in GOALS**, so
the largest document in the project currently reads as though the g8-01 null were
the end of the line.

This is a record correction rather than new work, and it wants John's call on how
much of a live investigation belongs in GOALS at all — the alternative is that
GOALS records only settled results and points at the sweeps for the rest, which
is arguably what it should have been doing since g8.

## Blocked on a result

**A real gate, if g8-01 leaves one possible.** If recovery is near zero, every
result resting on the oracle has to be relabelled a ceiling in GOALS, and replay
(above) becomes the main remaining candidate rather than one of several.

---

## Built but not finished

**The Docker/tc-netem testbed — BUILT, and it has produced two results.** See
[note 014](docs/notes/014-the-first-real-packets.md). Correctness survives 80ms
delay with 20ms jitter and 2% loss, bit-identical; and lock-step costs a round
trip per token, which a window of 8 recovers at 7.3x. **Window 1 is the global
synchronisation C1 forbids, so obeying C1 is worth 7.3x on that link** rather
than being only a constraint to satisfy.

What it still owes:

- **Repeats.** Those are SINGLE RUNS. No seeds, no error bars, and timing is the
  noisiest quantity measured here. The 7.3x is an observation, not a measured
  effect, which is why it is in a note and not in GOALS. A repeated sweep on
  Actions is the fix and has not been costed.
- **Churn over an impaired link**, which is the measurement the slice handshake
  was fixed to make meaningful and the obvious next one. `testbed/driver.py` does
  not expose `absent` or `leave_at` yet.
- **Scale.** Width 16, four nodes, 40 steps. Nothing here reaches the widths the
  tiny-node results are about.
- **A real topology.** A Docker bridge has no NAT, no competing congestion, and
  netem is applied to each node's egress only, so the round trip is impaired in
  one direction.

**Per-job parallelism in sweeps.** Every sweep job trains its models one after
another on a runner with about four cores, so it uses roughly one of them. This
is not about containers — containers on one runner add isolation, not compute —
it is about the inner loop being serial. A `--workers` option would cut sweep
wall-clock by roughly the core count, for free, on every sweep from now on.
Costed nowhere yet; measure before believing the factor.

---

## Deployment

**A node entrypoint — BUILT.** `openplexus/node_main.py` starts from nothing but
environment variables, sizes itself with the cgroup-aware planner, rebuilds its
arrays from the shared seed rather than being handed a matrix, and joins. A
network assembled from entrypoints agrees with the single-process model exactly,
which is the check that notices a node working perfectly on the wrong arrays.

Still static by design: slice assignment comes from `OPENPLEXUS_NODE_INDEX`
rather than being negotiated, which is John's explicit choice. A node that
negotiates its own slice is a coordination protocol, and no measurement needs one
yet.

**Uneven slices.** `slices_for` refuses any split that does not divide evenly.
Real machines will not offer round numbers, and heterogeneity (above) needs this
too. Flagged in `distributed.py` as a later milestone.

---

## Tasks beyond MQAR

In the order they would be built. See
[g8-02](experiments/sweeps/g8-02-when-the-statistics-are-real.txt) for the first.

1. **Zipfian filler.** Cheapest possible attack on the base-rate diagnosis, and
   it keeps every existing floor and control valid because only the filler
   distribution changes.
2. **A small real corpus**, character level, measured as perplexity against a
   bigram baseline. The first evidence for goal 2, and the first time anything
   here sees language. Vocabulary scale is untested and is the known risk.
3. **bAbI task 2.** Task 1 is MQAR wearing a hat and should pass for free, which
   makes it a calibration. Task 2 needs chained retrieval — querying the memory
   with the result of a previous retrieval — and is the first item on this page
   that requires a new mechanism rather than a new measurement.

**ARC-AGI is the honest long-range target and is not near.** It tests
composition and inference over memory; we have built a memory. Recording it here
so the gap is written down rather than implied.
