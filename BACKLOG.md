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

### 2. Multiple timescales, of which we implemented one

Zenke & Gerstner (2017) is titled *Hebbian plasticity requires compensatory
processes on multiple timescales*. `lasting_cap` came from this paper and took
only the compensation half.

We have exactly two stores: one that decays at a fixed rate and one that never
decays. The paper's claim is that stability wants a **cascade** of them. That is
a concrete architectural change with a concrete prediction, and it has never been
tested.

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

## Blocked on a result

**A real gate, if g8-01 leaves one possible.** If recovery is near zero, every
result resting on the oracle has to be relabelled a ceiling in GOALS, and replay
(above) becomes the main remaining candidate rather than one of several.

---

## Built but not finished

**The Docker/tc-netem testbed.** The prerequisites are all done: netem verified
working in Docker Desktop and available on Actions runners, `Network(spawn=False)`
accepts nodes it did not start, and the slice handshake that this would have
silently corrupted is fixed. What does not exist yet: a node entrypoint, a
Dockerfile, a compose or run script, and an experiment. **It turns G2, G3 and G4
from modelled into measured**, which is the whole reason to want it.

**Per-job parallelism in sweeps.** Every sweep job trains its models one after
another on a runner with about four cores, so it uses roughly one of them. This
is not about containers — containers on one runner add isolation, not compute —
it is about the inner loop being serial. A `--workers` option would cut sweep
wall-clock by roughly the core count, for free, on every sweep from now on.
Costed nowhere yet; measure before believing the factor.

---

## Deployment

**A node entrypoint.** `openplexus/deployment.py` decides capacity and allocation
and nothing consumes it yet. The thing John actually asked for — a process that
starts, sizes itself to whatever machine it landed on, and joins — does not exist.

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
