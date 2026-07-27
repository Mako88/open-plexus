# Backlog

Things worth doing that are not being done right now, and why. An item here has
been thought about at least once; an item that has not is not here.

Ordered by what would change the most if it turned out well.

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

Next: a cap on the fast store, same shape as `lasting_cap` — scale the whole
store, never an entry — default off, with the four predictions in note 018
registered first. The loudest of them is that **it must NOT improve the gating
result**; stability is not selectivity, and a fix that quietly lifts the headline
number is the most dangerous kind.

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

**Decide it before adding the mutation back:** if asymmetric masks make it
visible, write that test and restore the mutation. If nothing makes it visible,
it is a no-op and does not belong in the harness — record why, because the next
person will try the same mutation.

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

- **Duplication across summarisers and experiments.** AST-normalise function
  bodies (strip names, literals, comments), hash, flag near-identical bodies in
  different files. This would have found the five copied refusals *before* one of
  them lost its floor check. Restrict it to `tools/` and `experiments/`, where
  copy-paste is the actual working style.
- **Repo-specific rails**, which are where the value is, because generic lint is
  a solved problem and these encode the failures that have already cost results:
  every summariser computing a recovery ratio imports `tools.recovery`; every
  sweep file has a PREDICTIONS section and a COST section; every experiment goes
  through `experiments/harness.py` so `refuse_if_mutating()` cannot be skipped;
  every workflow under `sweep-*.yml` is `workflow_dispatch` only (this one exists
  as `tools/check_workflows.py` and is the model for the rest).
- **A test-quality check with teeth**: flag test methods with no assertion at
  all, and ones whose only assertion is `assertIsNotNone` or `assertTrue` on a
  call result. Both are shapes that pass while measuring nothing.

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

Still carrying their own copies: `summarise_g8_01.py`, `summarise_g8_03.py`,
`summarise_g9_02.py`, `summarise_g9_03.py`. They agree with the shared version
today; the point of porting them is that nothing keeps them agreeing tomorrow.

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
