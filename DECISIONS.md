# Decision log — entries 83 onward

**What this document is.** A chronological record of decisions taken, why, what
each one ruled out, and how to undo it if it was wrong. It is a *reference*, read
by looking an entry up.

**What this document is NOT.** It is not the current state of the project.
Entries are never rewritten when later work overturns them, so reading forward
from any point here will hand you claims that were true when written and are not
true now. Four mechanisms in this log were built, measured, and abandoned; two
headline numbers were retracted.

> **For what is true today, read [STATE.md](STATE.md).** It is the only document
> in this project that is kept current. If STATE.md and an entry here disagree,
> STATE.md wins — and if it does not mention the entry at all, the entry is
> probably closed.

Entries 1–82 are in [docs/archive/decisions-001-082.md](docs/archive/decisions-001-082.md).
They cover the MQAR era, the gating line, and the first corpus work. Entry 83 is
where the relational/chain line begins, which is the work that is live now.

**Standing authorisation.** Push and dispatch CI freely, update documentation
freely, take necessary decisions and log them here. The pending-decisions list is
a report to John, not a gate.

## Index

| # | what it settled |
|---|---|
| 83 | G0 for the chain task: one hop perfect, two hops answers the intermediate 100% of the time |
| 84 | the hop mechanism is built, and the instrument it was built for is contaminated |
| 85 | the hop mechanism composes — the bug was in the WRITE path all along |
| 86 | a halting signal exists, and it is not confidence |
| 87 | the gate learns which hop to read; mixed depths go to 1.000 |
| 88 | three depths at once, and the gain has an upper edge |
| 89 | the gate is a token detector, measured — and the sign was not predicted |
| 90 | composition survives churn; the per-hop cost compounds gently |
| 91 | perpetual learning does not heal churn, because churn costs capacity |
| 92 | the gate generalises to a depth it never trained on — zero-shot |
| 93 | there is no token-agnostic terminal signal — which points at frozen `Wv` |
| 94 | `value_lr` does not build a terminator class |
| 95 | the gate is not outvoted, it is CONFLICTED — a mechanism problem |
| 96 | letting the gate see WHERE it is triples all-position accuracy, and is not enough |
| 97 | density raises the level and does NOT remove the decay |
| 98 | giving the gate its own objective removes the decay |
| 99 | a typed-relation task, three defects found by reading it, floor 0.546 |
| 100 | the published rules exist, and their STRUCTURE fixes 99's leak |
| 101 | the hop mechanism REPLACES retrievals, it does not COMBINE them |
| 102 | the accumulator is built; traversal is the only blocker |
| 103 | the store cannot hold an entity that appears in two facts |
| 104 | pair keys largely fix it, and a scale register now exists |
| 105 | hops and pair keys do not compose, and produced numbers anyway |
| 106 | composition degrades under repeated entities, gracefully; 1.000 was degenerate |
| 107 | the traversal mechanism is not worth building; the blocker is per-step fidelity |
| 108 | the store is not losing information — the question is ambiguous, and resolving it is SEARCH |
| 109 | the store is not the saturation bottleneck — capacity scales with d² |
| 110 | the readout is not at capacity either — saturation is not a capacity limit |
| 111 | search does not pay: the verifier is built from the same noisy retrievals |
| 112 | retrieval fidelity is a WIDTH limit, and both binding counts were wrong |
| 113 | 112's saturation hypothesis was aimed at the wrong axis — WIDTH ALREADY HELPS |
| 114 | `value_lr` does not collapse at a sane rate — it works, and it does not help |
| 115 | saturation is CLOSED — note 035 had it, three alternatives eliminated |
| 116 | `carry_store` and `hidden` are superadditive |
| 117 | the prequential reproduction FAILED — 4.540 could not be reobtained |
| 118 | the unigram was never beaten — 4.540 is an offline backprop probe |
| 119 | the superposed store EARNS ITS PLACE — note 030's question, answered |
| 120 | four documents were doing each other's jobs; John asked for three |
| 121 | width does NOT fix fidelity on the task — and search's blocker has expired |
| 122 | step 2 reproduces at 0.971, the traversal ceiling is 1.000, build it |
| 123 | search is built and proved on its own; beam 4 costs 3.2x the traffic |
| 124 | the objective is the thesis; the driver has no failure detector |
| 125 | traversal is the win (+0.269); search helps only where ambiguity is |
| 126 | SWIM and CRDTs read; the detector ejected nodes permanently, now fixed |
| 127 | the SWIM paper was never unreadable; the retry interval is in the wrong unit |
| 128 | d_max is ~640 ms measured; the in-process figure was measuring Windows |
| 129 | ambiguity is detectable before searching; the expensive signal is below chance |
| 130 | the gate pays (+0.020 over search-everywhere); the search line closes |
| 131 | the persistence test tested a SATURATED store; lasting_cap is what binds |
| 132 | the slow store had no brake, and the write rate was 100x too large |
| 133 | persistence moves the LEVEL not the SLOPE — the wall is CAPACITY |
| 134 | pooled capacity is identical; concept partitioning's case is INDEPENDENCE |

---

## 83. G0 for the chain task: one hop is perfect, two hops answers the intermediate 100% of the time

**The cleanest instrument result this project has produced**, and the one that
makes the hop mechanism worth building.

     hops  chains   floor   linear  hidden128
        1       4   0.250    1.000      0.555
        1       8   0.125    1.000      0.510
        2       4   0.250    0.000      0.020
        2       8   0.125    0.000      0.030

**G0 passes.** One hop is 1.000 with a linear readout — the task is wired
correctly, the model solves it, and note 038's positive control holds. A zero at
two hops is therefore readable rather than ambiguous.

**Two hops scores BELOW chance, and that is the finding.** A model guessing
scores 0.250. Scoring 0.000 means it is confidently producing a specific wrong
answer, every time. Which one:

    100.0%   the INTERMEDIATE (b) -- one hop, then stopped

**Every single test sequence.** The store binds `a -> b`, retrieval with `a`
returns `b`, and the readout emits it. The model performs exactly one hop,
correctly, and has no mechanism to take the second.

That is not "the task is too hard" and not "the task is broken". It is a precise
statement of the architectural gap, and a random-looking failure would have left
the mechanism unmotivated. **Decode-and-re-encode now has a number to beat: any
2-hop accuracy above 0.000 is progress, and the ceiling is the 1.000 the model
already reaches at one hop.**

### The composed readout LOSES here, which is the fifth instance of one pattern

`hidden128` scores 0.51-0.56 at one hop where the linear readout scores 1.000.
On text it won 9 of 9 cells in g11-07 and was the largest single factor in the
grid. On exact retrieval it halves accuracy.

The reading: a hidden layer helps when the answer is a STATISTICAL function of a
superposed retrieval, and hurts when the retrieval already contains the exact
answer and only needs reading off. Composition buys generalisation and costs
fidelity.

This is decision 74's pattern for the fifth time — sparse keys, the cache, the
write gate, readout bias, and now the composed readout itself. **A mechanism's
effect is a property of the configuration, not of the mechanism**, and the
configuration now includes the task. Worth holding before any hop mechanism is
built on the assumption that hidden layers help.

### What this licenses

The hop axis is a valid instrument. One hop is a known-good positive control,
two hops is a known-zero with a diagnosed cause, and anything between them is
measurable. That is what note 038 said had to exist before the mechanism was
worth writing, and it now does.

## 84. The hop mechanism is built, and the instrument it was built for is contaminated

Decode-and-re-encode is implemented: a hop decodes its retrieval to a token
distribution and re-encodes it as a key, using `Wo`/`Wv` and `Wk`, which already
exist. **No new parameters.** `hops=1` is the default and every golden value is
bit-identical, so nothing earlier moved.

It does not work yet, and the reason is not in the mechanism.

### One real bug, found by measuring instead of reasoning

First attempt scored 0.000 at two hops, and `task=1, model=2` fell from **1.000
to 0.005** — the extra hop destroyed the case that already worked, which was
pre-registered as disqualifying. The diagnosis:

    frozen decoder  (wv @ r) finds the intermediate : 1.000
    learned readout (wo @ r) finds the intermediate : 1.000
    softmax entropy                                 : 3.912
    uniform would be                                : 3.912

**The decode was right and the re-encode threw it away.** argmax found the
intermediate every time, and the softmax over those logits was uniform to three
decimals because top-1 beat top-2 by 0.0388. `weights @ wk` on a flat weight
vector is the *mean of every key row* — one constant vector regardless of what
was decoded.

Fixed by standardising the logits before the softmax rather than by tuning a
temperature. The logit scale moves with `key_scale`, `d_model`, `decay` and
`memory_cap`, so a constant would have worked in this cell and failed silently
elsewhere — decision 74 again. `hop_sharpness=0` reproduces 0.000 exactly, which
is what makes the fix a claim rather than a coincidence.

It bought 0.000 → **0.035**. Real, and nowhere near the 0.250 floor.

### Two hypotheses of mine, both refuted, both by the same method

**"The readout is dragged off decoding by the answer gradient."** Refuted. A
`hop_decoder` axis between the learned `Wo` and the frozen `Wv` transpose is a
null: 0.030 vs 0.035. Worth recording that the first probe appeared to refute
this too but did not — it trained at `hops=1`, which is not the regime where the
drag could happen. A refutation from the wrong regime is not a refutation.

**"Sharpness needs tuning."** Refuted. 2.0 / 6.0 / 12.0 / 30.0 all sit at
0.01–0.04, and 30.0 is effectively argmax.

### Where it actually fails: a four-rung bisection

    A  real mechanism            0.035
    B  oracle KEY for hop 2      0.100
    C  oracle VALUE for hop 2    1.000
    D  one hop, want b           1.000

**C is 1.000.** Handed the correct value vector, the readout produces the answer
perfectly — so the readout, the training budget and everything downstream of
retrieval are fine. The failure is entirely in the **second lookup**, and an
oracle is an upper bound on every proposal that shares it.

### The cause, and it is the instrument

Retrieving with the exact `wk[b]`:

    rank 0 in 54.0% of sequences
    FIRST:   c (THE ANSWER) 54.0%   SEPARATOR 39.5%
    SECOND:  SEPARATOR      45.0%   c (THE ANSWER) 44.5%

**The separator competes with the answer for the same key.** Stating each link
as its own triple makes `b` appear twice — once as a target followed by the next
`sep`, once as a source followed by `c` — so `key(b)` carries two bindings and
the store returns their sum. `a` is never anyone's target, which is exactly why
one hop scores 1.000 and two hops collapse.

**This is decision 82's shape and the false-link bug's shape at once.** The
separator was introduced to fix the false-link defect and it created a second
one, and `test_no_false_chain_link_is_ever_stated` cannot see it: that test only
inspects pairs where *both* tokens are chain symbols, so the separator is exempt
by construction. The guard has a hole exactly where the new defect lives.

### What this licenses

The fix is forced, not chosen. A key with two bindings returns their sum — that
is what a superposed store *is*, not a defect in it. So `b` must appear once,
which means chains must be laid down contiguously (`sep a b c`) with separators
between *chains* rather than between links. That also restores the no-false-link
property, since chain-internal adjacencies are all real links.

It costs something and the cost should be stated: contiguity fixes the offset
from the query symbol to the answer at exactly `hops`. This model has no
positional access so it cannot exploit that, but the instrument would need
interleaving before it could be pointed at a positional model. Contiguity and
shuffling trade off directly — if `b` appears twice the bindings compete, and if
it appears once the offset is constant.

**No hop number from before this fix means anything**, including the 0.035. The
mechanism has not yet been measured on an uncontaminated instrument.

## 85. The hop mechanism composes — and the bug was in the WRITE path all along

**Two hops and three hops both score 1.000, from 0.000.** The model follows a
relational chain no single stated fact answers.

    task  model  sharp  accuracy   answered
       2      1      —     0.000   intermediate 100%
       2      2    0.0     0.015   other 96%
       2      2    2.0     1.000   answer 100%
       2      2   30.0     1.000   answer 100%
       3      1      —     0.000   intermediate 100%
       3      3    2.0     1.000   answer 100%

Every control holds. A **1-hop model still scores 0.000** and still answers the
intermediate 100% of the time, so the task genuinely requires composition and
nothing leaked when it started working. **Sharpness 0 still fails** at 0.015, so
the standardisation is still load-bearing. And 2 through 30 all give 1.000, so
it is not a tuned knob.

### The bug

`key` is the token's key, and it is carried out of the retrieval block into
`previous_key` — which is what the NEXT position writes its binding with. The
hop loop reassigned that same `key`. So with `hops > 1`, **every binding in the
store was written using a re-encoded hop key instead of the token's**.

The hop mechanism was corrupting the memory it was trying to read.

One line, `hop_key = weights @ self.wk`, and it is the same shadowing class the
code three blocks up already carries a warning about — `store` was renamed for
exactly this reason, after shadowing it turned `if wrote:` into an array test.

### Why it took four probes to find

Every measurement pointed at retrieval and the damage was in the write:

- the decode was **correct** (argmax 1.000) — so not the decoder
- the decoder axis was a **null** (0.030 vs 0.035) — so not the drag
- sharpness 2–30 all sat at **0.01–0.04** — so not the temperature
- an oracle KEY gave **0.135** and an oracle VALUE gave **1.000** — which
  correctly localised it to the second lookup, and the second lookup was
  reading a store the hops had corrupted

The tell was a contradiction I could not explain away: `argmax(wv @ r2)` was `c`
in 100% of sequences measured outside the run, and 12% measured inside it, with
prediction and decode agreeing 1.000. Two measurements of the same quantity
disagreeing is not noise — **it means the two runs are not the same run**, and
the only thing that differed was `hops`.

`test_the_store_is_identical_at_every_hop_count` is the invariant, stated so it
cannot come back: **hops change what is read, never what is written.**
`a-hop-key-escapes-into-the-write-path` is the mutation, verified caught.

### What this does NOT license

**`hops` is a fixed count and must match the question exactly.** Measured:

    task  model     acc   answered
       1      2   0.000   other 100%
       1      3   0.000   other 100%
       2      3   0.000   other 100%
       3      2   0.000   intermediate 100%

Overshoot is total, not graceful. A model with more hops than the question needs
walks past the answer into whatever the answer points at; one with fewer stops
early and answers the intermediate. A model that does not know in advance how
deep a question is **cannot use this**, and a mixed workload contains both
depths by definition.

So this is composition with the depth supplied from outside. The next problem is
a halting signal — deciding *when to stop hopping* from something the model can
compute locally — and it is well posed now in a way it was not before, because
both failure directions are measured and the ceiling at every depth is 1.000.

### What it does license

The separator finding in decision 84 stands on its own: it was measured at
`hops=1`, on an uncorrupted store, and took the lookup from 54% to 100%. Both
fixes were needed and neither would have been enough alone.

## 86. A halting signal exists, and it is not confidence

Overshoot is total, so the model must decide when to stop hopping. Before
designing a mechanism, the question is whether the information to do it is
present locally at all. Four candidates, each computable by one node from its
own slice with no barrier, measured on a depth-2 chain and split into hops still
ON the chain and the hop that has walked PAST the end:

     signal    on chain (k<d)    past end (k>d)   separated?
       peak    1.0000 ±0.000      0.9357 ±0.136    no  d=0.67
     spread    0.0123 ±0.003      0.0171 ±0.006    no  d=0.95
       norm    0.1323 ±0.027      0.1849 ±0.069  weak  d=1.01
        gap    3.1119 ±0.705      2.1462 ±1.369    no  d=0.89

**Confidence says nothing.** Every d′ is at or below 1.01, and the model is
0.94-confident *after* it has walked off the end.

That is not a quirk. Past the end, `key(c) → value(separator)` is a **real
binding** — the store has a genuine answer for that query, so the decode is
sharp and correct. The model is confidently answering a question nobody asked,
which is why overshoot scored a clean 0.000 rather than something noisy. **A
confident retrieval is not evidence that the retrieval was wanted.**

### What does separate is the CONTENT

    hop 1: asked[1] 100%
    hop 2: asked[2] 100%                        <- the answer
    hop 3: SEPARATOR 73%, QUERY 27%             <- past end
    hop 4: other chain symbol 55%, asked[0] 45%

The first hop past the end lands on a **structural marker 100% of the time**,
and an on-chain hop never does. The two classes are perfectly separable by what
is retrieved, while being inseparable by how strongly it is retrieved.

### What this licenses

A halting gate is worth building, and it is a **linear function of the
retrieval** — a per-group vector scoring "does this look terminal", which stays
inside a group and adds one vector per group rather than a matrix. The gate does
not need to be told which token is the separator; it needs to learn that some
retrievals mean *stop*, and the measurement says that class is linearly
available.

### What it does NOT license

**That this generalises is untested.** Structural markers exist here because the
task lays them down, and the honest general claim is narrower: *a chain ends at
something structurally different from its links*, which is true of prose
punctuation and of record delimiters but is not proven for either. A gate
trained here learns this task's terminal class, and the first real test is a
task whose terminator was never designed in.

Recorded before building, because the gate's own result will be much harder to
read once the mechanism can move the number.

## 87. The gate learns which hop to read, and mixed depths go to 1.000

Questions of depth 1 and depth 2 shuffled together, nothing marking which is
which. **A fixed hop count must fail half of them by construction**, and that is
what makes the gate's number readable:

    model                overall   depth 1   depth 2
    fixed hops=1           0.500     1.000     0.000
    fixed hops=2           0.507     0.013     1.000
    GATE gain=1            0.720     0.887     0.553
    GATE gain=10           0.987     1.000     0.973
    GATE gain=50           1.000     1.000     1.000
    GATE gain=200          1.000     1.000     1.000
    GATE gain=1000         1.000     1.000     1.000

**1.000 on both depths**, from a single learned vector per group, stable across
a 20× range of gain. The model answers questions whose depth it is not told,
which is the limitation decision 85 ended on.

### Two defects, each of which looked like a working mechanism

**The gate was inert.** The learned vector reached norm 0.089 against retrieval
slices of ~0.13, so the scores were ~0.01 and a two-way softmax over them is a
flat average. Measured directly: weight on hop 1 was **0.5020** for depth-1
questions and **0.5000** for depth-2 — the right direction, and 0.2% of the way
there. It still scored **0.707**, beating both fixed models, because the readout
learned to cope with a fixed blend. Same shape as the unsharpened hop decode:
a correct signal flattened into uniformity.

**The gate was scoring the wrong hop.** With gain it reached 0.773 — depth 1 at
1.000 and depth 2 at 0.547 — and that split is the diagnosis. Decision 86's
signal separates *past the end* from *on the chain*. For a depth-1 question hop
2 is the separator, so the gate can reject it. For a depth-2 question hop 1 is
`b` and hop 2 is `c`, **both on the chain, both chain symbols**, and the gate has
nothing to tell them apart by. It split them and averaged.

The rule the signal actually supports is *the last hop before the first marker*,
so **hop k is scored by what hop k+1 returns**. One extra lookahead retrieval,
same linear score, still inside a group. That is the change from 0.773 to 1.000.

### What the mutation harness caught that the tests did not

The first test pass asserted read counts, refusals, a zero-gain control and
store invariance — and **both mutations survived all of it**. Every structural
property held while the mechanism did the wrong thing.

They survived because each defect leaves a model that still beats the baseline:
0.707 and 0.773 against 0.500. **A mechanism that does nothing and still beats
the baseline is the hardest kind to notice**, and structural tests cannot see
it. `test_the_gate_solves_depths_a_fixed_hop_count_cannot` trains on mixed
depths and asserts the depth-2 half, which is where both defects give up
(0.553 and 0.547 against 1.000). Both are caught now.

### What this does NOT license

The gate is trained and tested on the **same terminator**. Decision 86 already
recorded that this task lays down its own structural markers, and the gate has
now learned this task's terminal class — not a general one. **The first real
test is a task whose terminator was never designed in.**

`hops` is still a ceiling: the gate chooses among hops 1..k and cannot choose a
depth beyond k. Nothing here tests depth 3 mixed with depth 1 — **decision 88
does** — and the lookahead means a `hops=k` gated model pays k+1 retrievals, the
cost of not knowing the depth, which is one extra hop over knowing it.

## 88. Three depths at once, and the gain has an upper edge

Decision 87 explicitly did not license three depths: the gate must pick one hop
of three, the softmax has more ways to split, and the lookahead has to reject a
marker two hops further out for the deepest questions.

    model                  overall     d1      d2      d3
    fixed hops=1             0.333   1.000   0.000   0.000
    fixed hops=2             0.339   0.017   1.000   0.000
    fixed hops=3             0.353   0.000   0.058   1.000
    GATE max=3 gain=50       0.997   1.000   1.000   0.992
    GATE max=3 gain=200      1.000   1.000   1.000   1.000
    GATE max=3 gain=1000     0.986   1.000   0.983   0.975

**It scales.** 1.000 on all three depths, against fixed counts pinned at 0.333
because each solves only its own third. Reported per depth on purpose: an
overall number would hide a gate that solved two depths and abandoned the third,
which is the exact shape the own-hop-scored gate failed in.

**The gain has an upper edge**, which two depths did not show. At 1000 the model
loses 0.986, and the loss is on the deeper questions (d2 0.983, d3 0.975) while
d1 stays perfect. A very large gain makes the hop softmax effectively an argmax,
so a single mis-scored hop is taken outright instead of being averaged against
its neighbours — and deeper questions have more hops to mis-score. So the gain
is a real dial with a middle, not a "larger is safer" knob, and 200 is where
both grids agree.

### What this licenses

The mechanism is not a two-hop special case. Depth is now a property of the
question rather than of the configuration, up to a ceiling the caller sets.

### What it still does not license

The terminator. Every result so far trains and tests on the **same** structural
marker, and with random value vectors there is nothing shared between two
different marker tokens for a linear gate to latch onto — so the honest
prediction is that it does **not** transfer, and the interesting question is
whether anything survives at all. Decision 86 measured retrieval `norm` as the
one signal with any separation (d′=1.01), and a norm is not tied to a token's
identity. That is the thread worth pulling: it is the difference between a
mechanism and a fit.

## 89. The gate is a token detector, measured — and the sign was not what I predicted

Decision 88 predicted the gate has learned this task's terminator rather than a
general notion of one. That is a claim about `halt_w`, so it is checkable by
looking at the vector instead of running a transfer experiment.

Cosine between the gate vector and each token's **value** vector:

    SEPARATOR      +0.563      +8.3 sd from the rest
    QUERY          +0.518      +7.7 sd from the rest
    every other    mean -0.068, sd 0.076, range [-0.290, +0.078]

**The gate has latched onto two specific tokens**, eight standard deviations
clear of the other forty-eight. This is no longer a suspicion about transfer: it
is a measurement of what the parameter contains. Two different marker tokens
have unrelated random value vectors, so a linear gate trained on one **cannot**
recognise the other. Transfer is impossible by construction, not merely
unlikely, and the experiment to confirm it would only restate the arithmetic.

### The sign was the opposite of what I predicted, and the mechanism is right

I expected strongly NEGATIVE — "reject anything that looks like a marker". It is
strongly positive, and positive is correct. The gate scores the **lookahead**, so
a high score on hop k means *take* hop k. For a depth-1 question hop 1 is the
answer and its lookahead is the separator, so the separator must score HIGH.

The rule the gate learned states cleanly: **take the hop whose next hop is a
marker** — the last hop before the end. That is the rule decision 87 designed
the lookahead for, arrived at from data rather than assumed, and the sign error
was in my prediction rather than in the mechanism.

### What this licenses

Nothing new about capability. It converts decision 88's caveat from a guess into
a fact, and it means **the next experiment is not a transfer test** — that
result is already determined. The open question is a different one: whether a
gate can be given a signal that is not token identity at all.

Decision 86 measured retrieval `norm` as the only candidate with any separation
(d′=1.01, past-end 0.185 against on-chain 0.132), and a norm is a property of
how a key was bound rather than of which token was stored. That is worth trying,
and it is a weak signal being asked to do a job a very strong one currently
does — so the honest expectation is that it degrades accuracy and the question
is by how much.

## 90. Composition survives churn, and the per-hop cost compounds gently

Every churn result before this was measured on **one-hop recall**. C3 is a
premise of the whole project, so the question is whether the new capability
survives it, and there was a specific reason to doubt: a depth-3 question needs
three lookups to survive where a depth-1 question needs one.

Width 64, gated, depths 1–3 mixed, dimensions zeroed **after** training — a
model that learned on a whole machine and then lost part of it, which is the
realistic order. Three seeds averaged.

    removed   depth 1   depth 2   depth 3
       0.0%     1.000     1.000     0.986
      12.5%     1.000     1.000     0.975
      25.0%     0.997     0.989     0.956
      37.5%     0.981     0.989     0.961
      50.0%     0.986     0.964     0.928
      62.5%     0.928     0.886     0.831
      75.0%     0.739     0.694     0.542

**Half the machine gone and depth-3 chains still answer at 0.928.**

The prediction was directionally right and wrong about the magnitude. Deeper
questions do degrade faster — the depth-1 to depth-3 gap widens from 0.014 at
full width to 0.197 at 75% removed — but the compounding is gentle until 62.5%
and there is no cliff where composition stops working while recall keeps going.
Relative to depth 1, depth 3 holds 0.986 at full width, 0.941 at half, and 0.733
at three-quarters removed.

### What this licenses

Composition is not a fair-weather capability that only exists on an intact
machine. C3 was measured on recall and now covers the hop mechanism too, at the
churn fractions decision 81 measured over real containers.

### What it does NOT license

**Three seeds, and no spread reported.** The ordering is consistent and the
trend is monotone in depth at every fraction, which is what the claim rests on;
individual cells at the noisy end are not worth quoting to three decimals.

Ablation is a **frozen** departure — dimensions zeroed once, after training.
Decision 81's containers measured real join and leave; this did not, and a model
that keeps learning while nodes come and go (C4 crossed with C3) is untested
for hops. **Decision 91 tests it.**

## 91. Perpetual learning does not heal churn, because churn costs capacity

Decision 90 measured survival of a frozen departure. C4 says the model never
stops learning, so the different question is whether continued learning **claws
back** what a departure cost. Half the nodes leave after 400 sequences and 800
more follow; every arm sees the same number of sequences, so a gain cannot be
"more training" rather than "better training".

    arm            depth 1   depth 2   depth 3
    intact           1.000     1.000     0.989
    frozen           0.983     0.969     0.942
    learning         0.992     0.978     0.950

    recovered        +0.008    +0.008    +0.008

**Continued learning recovers +0.008 against the ~0.047 lost at depth 3.** Close
to nothing.

The reading: a departure costs **capacity**, and capacity is not a thing
learning can rebuild. The readout was already near-optimal on the dimensions
that survived — the delta rule on `Wo` is the exact gradient for a linear
readout — so there was very little left for further training to fix. Nothing was
stale; there was simply less machine.

### Treat the +0.008 as a direction, not a number

It is **identical to three decimals at all three depths**, which is about 3
sequences out of 360 per depth. Three seeds, no spread reported. That pattern is
consistent with coincidence at a small effect size, and the claim here rests on
the effect being *small*, which does not depend on its exact value.

### What this licenses

**Do not expect C4 to pay for C3.** They are independent requirements and this
result separates them: churn tolerance has to come from capacity and redundancy,
and perpetual learning has to earn its keep somewhere else.

### What it does NOT license

Nothing about what C4 is actually for. This run holds the data distribution
**fixed**, so continued learning had nothing new to learn — it could only
re-fit what it already knew on fewer dimensions. The test that would show C4's
value is a distribution that *changes* after the departure, where a frozen model
must fall behind and a learning one need not. That is the experiment to run next,
and it is the one that speaks to "always learning as it goes" rather than to
repair. **Decision 92 runs it, and it does not come out as expected.**

## 92. The gate generalises to a depth it never trained on — zero-shot

The experiment was meant to show what C4 is for: train on depths 1 and 2, then
let depth-3 questions start arriving, and score **only** on depth 3 — the kind
the model did not have. A frozen model should fall behind and a learning one
should not.

    arm                  depth 3
    never sees it          0.992
    frozen at shift        0.992
    keeps learning         0.992
    always had it          0.992

**Every arm identical.** The experiment measures nothing about adaptation,
because a model trained only on depths 1 and 2 already answers depth-3 questions
at 0.992 without ever having seen one.

### The null is the result

The gate learned a **rule**, not a table. "Take the hop whose lookahead is a
marker" says nothing about how deep a question is, so once it is learned from
depths 1 and 2 it applies at depth 3 unchanged — and the readout is shared
across hops, so there are no depth-3-specific parameters to train. Nothing about
a depth-3 question is new to this model except the number of times it goes
round.

That is worth more than the result the experiment was designed to get: it is
direct evidence the mechanism is a mechanism rather than a fit, on the axis it
was built for.

### Read this against decision 89, because together they are precise

    over DEPTH        generalises zero-shot to a depth never trained on
    over TERMINATOR   does not generalise at all -- halt_w sits +8.3 sd on one
                      specific token's value vector

**Same gate, same vector, opposite answers.** The rule it applies is general;
the feature it applies that rule to is a memorised token. That is a sharp
description of what was built, and it says where the next work is: not in the
hop machinery, which composes and generalises, but in what makes a retrieval
recognisable as terminal.

### What it does NOT license

C4 is still untested. Two attempts have now failed to construct a case where
continued learning helps — decision 91 because a departure costs capacity rather
than currency, and this one because the mechanism already generalises. Neither
is evidence that perpetual learning is worthless; both are evidence that **this
task is too easy to need it**. A real test of C4 needs something the model
genuinely cannot already do, and finding that case is the open problem.

## 93. There is no token-agnostic terminal signal — and that points at frozen `Wv`

Decision 92 put the next work in "what makes a retrieval recognisable as
terminal". The cheap version of that question is whether any identity-free
feature carries the signal, measured **before** building a config flag, tests
and mutations for a gate that might not work.

Five features, every one a property of *how* a key was bound rather than *which*
token was stored, so any of them would transfer to an unseen terminator by
construction. Labels are "has this hop walked past the end", and the separator is
fitted **with** the labels — a ceiling, not a mechanism.

    norm      d = 0.60        BEST LINEAR SEPARATOR on all five:
    entropy   d = 0.62          accuracy 0.628
    peak      d = 0.54          against  0.500 for guessing
    gap       d = 0.63
    kurtosis  d = 0.46        the token-identity gate: 1.000

**0.628 against a 0.500 baseline, with the labels handed to it.** No gate
learning from a downstream error can beat a classifier that was given the
answers, so this closes the approach rather than discouraging it. Note this also
supersedes decision 86's hopeful reading of `norm` at d′=1.01: measured over
three depths rather than one, it is 0.60 and it is not the outlier.

### Why, and it is not a property of the task

**`Wv` is frozen and random.** Two tokens' value vectors are independent draws,
so there is no shared structure for a "class of terminators" to live in. A gate
can memorise one vector — decision 89 measured exactly that, +8.3 sd — but there
is nothing for it to generalise *over*, because in this representation
`separator` and some other marker have no more in common than any two tokens.

That is not a limitation of gating. It is a limitation of frozen random
embeddings, and it would apply to any mechanism asked to recognise a *kind* of
token rather than a specific one.

### What this licenses

**A concrete reason to unfreeze the value projection.** `value_lr` already
exists in the model and "unfreezing the value projections" is already in
BACKLOG as one of the four approved un-constraints — this gives it a purpose
sharper than "more capacity": *tokens that play the same role can only become
similar if the representation is learned*, and role-similarity is the thing the
gate needs and cannot have.

That makes a falsifiable prediction worth testing next: with `value_lr` on and
training that includes **several different terminators**, the value vectors of
those markers should move closer together than chance — and only then can a gate
trained on some of them recognise a held-out one. If they do not converge, the
delta-rule-on-values mechanism is not doing representational work and that is
worth knowing on its own.

### What it does NOT license

Five features is not every feature. This rules out the retrieval *statistics*
that were available, not every identity-free signal that could exist — a feature
computed across hops rather than within one, for instance, was not tried.

## 94. `value_lr` does not build a terminator class — and the gate learns whatever depth dominates training

Decision 93 predicted that unfreezing `Wv` and training with several
terminators would make those markers' value vectors converge, giving a gate
something to generalise over. **The prediction is refuted, and the route to
testing it is blocked twice over.** `n_separators` and `use_separators` are
added to the task so the question can be asked at all; `n_separators=1` is
pinned byte-identical by a digest test.

### First blocker, from the code rather than a measurement

`value_lr` updates `self.wv[targets[t]]` at **scored positions only**, and the
chain task scores exactly one position whose target is always a chain symbol. A
separator is never a target, so its value vector **can never move**. Decision
93's experiment is not merely hard to run — as written it is a no-op.

### Second blocker: making separators targets breaks the gate

The fix is to score every position — next-token prediction, which is also how
the model would train on real text. It costs almost everything:

    separators        scored   depth-2 accuracy
             1   answer only              1.000
             1   every position           0.117
             4   answer only              0.992
             4   every position           0.683

**Four separators cost 0.008. All-position training costs 0.883.** The
diagnosis, checked rather than assumed — weight the gate puts on hop 1 at the
answer position of a depth-2 question, where a working gate puts it near zero:

    trained on answer only      0.0102
    trained on every position   0.3034

**At almost every position the next token is exactly one hop away.** The answer
position is a rare exception competing against a large majority, so the gate
learns the dominant depth and drags hop 1 up thirtyfold.

### And `value_lr` itself does not do what was hoped

    value_lr  accuracy   sep cos  base cos  sep-base
           0     0.683     0.064    -0.015    +0.080
       0.001     0.300     0.068     0.090    -0.023
        0.01     0.058     0.126     0.191    -0.065
        0.05     0.025     0.535     0.382    +0.153

The separator-minus-baseline contrast does not rise with `value_lr`. At the
largest rate **everything** converges — ordinary symbols reach 0.382 — which is
the representation collapsing globally, not terminators forming a class, and it
matches decision 65's trained projection collapsing the rank. Accuracy falls
monotonically to 0.025 alongside it.

### What this licenses — and it is the most important thing here

**The gate is trained by the same error as the readout, so it learns the depth
that dominates the training distribution rather than the depth a question
needs.** On the answer-only objective that distribution *is* the task. On
next-token over every position it is overwhelmingly one hop.

That is a serious obstacle on the path to real data, and it was invisible while
every experiment scored one position. Real text is trained at every position, so
a gate learning by this route would settle on "one hop" and **composition would
be built, correct, and never used**. Any future result on text has to show the
gate is actually gating, not just that accuracy moved.

### What it does NOT license

That all-position training is unusable — only that it is unusable *with the gate
learning from the same undifferentiated error*. A gate with its own objective, or
one trained only where depth is ambiguous, is untried. And 4 separators beating 1
under all-position training (0.683 against 0.117) is unexplained; it is a real
gap in the account, not a detail.

## 95. The gate is not outvoted, it is CONFLICTED — and that is a mechanism problem

Decision 94 left two explanations for why all-position training breaks the gate,
and they call for different work:

- **outvoted** — the rule is right everywhere and the answer position is rare, so
  reweight the training signal.
- **conflicted** — the rule is right at the query and wrong in the body, so no
  reweighting can help and the gate needs different inputs.

Take the gate trained **answer-only**, where it reaches 1.000, and ask what it
says at ordinary body positions, where the correct next token is one hop away:

    at the QUERY position   0.0171   want LOW  -- the answer is two hops out
    at BODY positions       0.4712   want HIGH -- the next token is one hop out

**It is conflicted.** In the body the gate is essentially uninformative — 0.47,
a coin flip, where serving the body requires close to 1.0. It is not doing the
body's job at all.

So under all-position training the body supplies the overwhelming majority of
the error, pulls the shared vector toward hop 1, and wrecks the query behaviour
that worked. That is exactly the 0.0102 → 0.3034 shift decision 94 measured, and
it is not a sampling problem.

### Why one gate cannot do both

The gate is a linear score on the **lookahead retrieval** and nothing else. At a
body position and at a query position that lookahead can look the same, while
the right answer differs — hop 1 in the body, hop 2 at the query. A function of
the lookahead alone cannot separate cases it cannot see apart.

**The missing input is where the model is, not what it retrieved.** The query
marker sits in the input at the query position, so the information exists; the
gate simply has no access to it.

### What this licenses

A specific, small mechanism change to try next: **give the gate the current
position's key alongside the lookahead retrieval**, so it can learn "at a query,
use the marker rule; otherwise take one hop". That is one more vector per group,
the same locality, and it is the smallest change that could resolve a conflict
this measurement says is real.

It is worth being clear that this is now a *design* claim and not yet a result.
The measurement establishes the conflict; it does not establish that the extra
input fixes it.

### What it does NOT license

The body number is *uninformative* (0.47), not *confidently wrong* (near 0). The
gate is failing to serve the body rather than actively fighting it, which is a
weaker statement than "the rule inverts" — and the distinction matters, because
a uninformative gate degrades gracefully while an inverted one would not.

## 96. Letting the gate see WHERE it is triples all-position accuracy — and is not enough

Decision 95's proposal, built as `gate_reads_key`. **The proposal as literally
written would not have worked**, and that is worth stating: "give the gate the
current key" as an added term is identical across hops at a position, so the
softmax removes it exactly — the same trap that made a constant perturbation
invisible to the decode. The key has to **modulate** the rule, not contribute to
the score. So it selects between two rules, blended by a scalar from that
group's own slice of the key. Two vectors per group, both zero-initialised, so
the model begins as exactly the one-rule gate.

    depth-2 accuracy      one rule   reads key
       answer only           1.000       1.000
       every position        0.117       0.400

**3.4× on all-position training, and the control holds** — answer-only stays at
1.000, so the extra machinery does no harm where the old gate already worked.

### But a single budget hides the real finding

Quoting only that row would have been misleading. Across training budgets:

    per depth  epochs   one rule   reads key      gap
          100       1      0.750       0.833   +0.083
          200       1      0.683       0.717   +0.033
          400       1      0.250       0.683   +0.433
          400       2      0.100       0.383   +0.283

**Accuracy falls as training proceeds, in both arms.** The one-rule gate goes
0.750 → 0.100 and the selector 0.833 → 0.383. Under all-position training the
model does not fail to learn composition — it **progressively unlearns it**, as
the body's error accumulates and drags the shared gate toward one hop.

So the selector **slows the decay rather than preventing it**, and the gap is
not stable either (+0.083, +0.033, +0.433, +0.283). The honest headline is the
decay, not the 3.4×.

### The gain is the intended mechanism, not the extra parameters

Two vectors per group is more capacity, so the gain could have come from
anywhere. The design says the key should make the gate behave *differently* at a
query than in the body. Weight on hop 1, after all-position training:

    gate        at query   in body   separation
    one rule      0.7491    0.5081      -0.2411
    reads key     0.3761    0.4945      +0.1184

**The one-rule gate separates them backwards** — more hop-1 weight at the query,
where the answer is two hops out, than in the body, where it is one. That is the
conflict of decision 95 shown from the other side. The selector **flips the sign**.

### What this does NOT license

**It is a delay, not a fix.** The body sits at 0.4945 where serving the body
wants ~1.0, and the query at 0.3761 where it wants ~0. The separation is correct
in sign and weak in magnitude, and accuracy still decays with training. Something
else is binding and this measurement does not say what.

Nor does it license the conclusion that the remaining gap is more of the same. A
gate strong enough to fix the sign but not the magnitude may be limited by the
selector being a single scalar per group, by the two rules being too few, or by
something outside the gate entirely — all untested.

**The decay is the thing to chase next**, and it is a sharper target than
"all-position is worse". A mechanism that is learned and then unlearned is
usually a mechanism whose gradient is being outvoted at a rate that grows with
exposure — so the next question is whether the gate's own error can be
decoupled from the readout's, rather than whether the gate needs more inputs.
**Decision 97 tests it. The answer is that the decay is real.**

## 97. Density raises the level and does NOT remove the decay — and the first run of this was leaking

Decision 96 measured composition being learned and then progressively unlearned
under next-token training — 0.750 falling to 0.100. The obvious suspect was
density: with one question per sequence, about 1 position in 50 needs
composition and every other needs a single hop, so ~98% of the error says
"always take one hop". **Real text is not that lopsided.** `n_queries` raises
the share of positions where the next token is genuinely several hops away.

### The first run of this leaked, and the guard was written after it

`n_queries` was added, the experiment run, and *then* the tests written. One of
them failed, and it was right to: **a query block writes `a` next to `c`, so it
STATES the link `a -> c`.** With one question that is harmless — the block is
last and the answer is read before the binding is written. With several, an
early block stated the answer to a chain a later block asked about, making that
question a **one-hop lookup of a link already in the store**.

The leak grew along exactly the axis being measured: more questions meant more
repeated chains meant more free answers. It produced a clean, plausible,
completely wrong curve — a decay collapsing from +0.517 to +0.029 — which was
reported in-session before the guard caught it. **Those numbers are discarded.**

Fixed by sampling asked chains **without replacement**, which caps `n_queries`
at `n_chains`. Guarded by `test_no_answer_is_stated_before_its_own_question`,
which checks the precise property — a chain's answer never appears before the
question that needs it — rather than the over-broad one the first version
checked, which flagged each block's own `(a, c)` and would have condemned the
task itself.

### The corrected measurement

Eight chains, so the floor is 0.125.

    queries     100x1     200x1     400x1     400x2     decay
          1     0.150     0.033     0.017     0.033    +0.117
          4     0.567     0.392     0.375     0.379    +0.188
          8     0.515     0.333     0.290     0.346    +0.169

**Density does not remove the decay.** +0.117, +0.188, +0.169 — no trend, and
the smallest value belongs to the row that has already collapsed to the floor
and has nothing left to lose.

**What density does is raise the level.** One question per sequence falls to
0.033, *below* the 0.125 floor, which means confidently wrong rather than
guessing. Four or eight stabilise around 0.35–0.38, well clear of it. That is a
real effect and a useful one — it is simply not the effect claimed.

### What this licenses

**Decision 96's proposed next step stands.** The decay is a property of the
mechanism and not of the instrument's uniformity, so decoupling the gate's error
from the readout's is back on the table as the thing to try.

And density is worth keeping regardless: it is the difference between a model
that is confidently wrong and one that is meaningfully above the floor under the
training regime real text requires.

### What it does NOT license

Levels are not comparable to any earlier chain result: `n_chains` is 8 here, so
the floor is 0.125 rather than 0.250, and every question in the sequence is
scored rather than one. **Only the within-row decay and the across-row level
ordering are measurements here.**

`n_queries=1` is pinned byte-identical by the same digest test as
`n_separators`, so every earlier chain number still reproduces.

## 98. Giving the gate its own objective removes the decay

Decisions 96 and 97 ruled out more inputs and more density. What was left was
the objective itself: the gate learns from the readout's error carried back
through the mixture, so **conflicting demands get averaged**. In the body the
error says "take hop 1", at a query it says "take a later one", and one shared
vector pulled by both drifts toward whichever supplies more gradient.

`which_hop` asks a question with the **same answer in both places** — *which hop
would have been right here?* At a scored position that label is locally
available: each hop's own readout either names the target or does not, decidable
from what the group already holds. The body then stops outvoting the query and
merely supplies more examples of one class, which a classifier handles.

    mixture                                which_hop
    queries  100x1 200x1 400x1 400x2 decay  100x1 200x1 400x1 400x2  decay
          1  0.150 0.033 0.017 0.033 +.117  0.233 0.500 0.383 0.600  -0.367
          4  0.567 0.392 0.375 0.379 +.188  0.571 0.475 0.550 0.517  +0.054
          8  0.515 0.333 0.290 0.346 +.169  0.404 0.406 0.412 0.404  +0.000

**The decay is gone**, and the objective is better on both axes — no decay *and*
a higher level at every density. At density 8 the trajectory is flat to three
decimals; at density 1 accuracy now *rises* with training where the mixture
objective collapsed it to 0.033, below the 0.125 floor.

### It also undoes decision 97's reading

With a working objective, **one question per sequence is the best row, not the
worst**. Density was compensating for a broken objective rather than fixing a
property of the task — which is worth stating plainly, because decision 97
recommended keeping the density and that recommendation is now weaker.

### What this does NOT license

**The claim rests on the flat row, not the dramatic one.** Density 1 scores 60
questions per evaluation and is visibly noisy — 0.233, 0.500, 0.383, 0.600 is
not monotone, and the −0.367 "improvement" is mostly that noise. Density 8
scores 480 and is flat. Quote the flat row.

Nor is ~0.40 good. It clears the 0.125 floor comfortably and no longer rots, but
answer-only training still reaches 1.000. **The gap between a marked question
and an unmarked stream is still most of the problem**, and this decision only
shows that the gap stops widening.

## 99. A typed-relation task, three defects found by reading it, and a floor of 0.546

`openplexus/tasks/kinship.py`, modelled on
[CLUTRR](https://arxiv.org/abs/1908.06177). **Not CLUTRR** — its rules, not its
dataset, and a number here is not a CLUTRR score.

### Why a second task at all

`chains.py` is **pure transitive chaining**: `a -> b -> c` means the answer is
`c`, and following an edge is the whole operation. Decision 92's zero-shot depth
generalisation is real but it is generalisation over *how many times to repeat
one operation*.

Kinship is not that. `mother` of `brother` is `mother`; `mother` of `mother` is
`grandmother`. **Composition is a lookup in a table the model must learn**, so
two paths of equal length compose to different relations. A model can be perfect
at "follow the arrow k times" and have no way to represent that.

### Three defects, all found by generating sequences and reading them

The habit that caught every chain-task defect, applied before any test existed.

1. **A distractor stated the asked pair directly in 7.0% of sequences.** One in
   three hundred handed over the answer; the rest **contradicted** it, making
   the task inconsistent rather than merely easier.
2. **Three-hop paths could not be generated.** Only 24 of 256 relation pairs
   compose, so rejection sampling raised "no 3-hop path composes" on an ordinary
   seed — the generator failing, not the depth being impossible. Paths are
   constructed by walking the table now.
3. **The floor was wrong.** 1/16 = 0.062 was assumed; the majority-class
   strategy actually scores 0.080, 0.108, 0.150 at one, two and three hops,
   because composition contracts the answer space (16 → 12 → 8 reachable
   relations).

### G0: the QUESTION ORDER decides whether the task is addressable at all

    hops 1 (floor 0.090)        hops 2 (floor 0.130)
      object last    0.020        object last    0.027
      subject last   0.700        subject last   0.407

**0.020 against 0.700 on the same task.** This store binds adjacent pairs, so
the retrieval key at the scored position is whichever person the question block
ends with. End it with the object and the model is keyed on the wrong token and
cannot address the task at all. That is a free choice of the task presenting
itself as a model failure, and measuring only one order would have recorded it
as one.

**G0 passes at one hop**: 0.700 against 0.090.

### But two hops demonstrates NO composition, and the reason is my rule table

    majority floor (no information)      0.130
    best guess from the FIRST relation   0.546
    a one-hop model actually scored      0.407
    distinct answers per first relation  2, 2, 2, 2, 2, 2

**Every first relation admits exactly two answers.** `mother` of anything in
this table is `grandmother` or `mother`. So the second relation barely matters,
the prefix nearly determines the answer, and 0.407 sits *below* what guessing
from the prefix is worth — the one-hop model's score is fully explained by a
shortcut and shows no composition whatever.

That is a property of `COMPOSE` being small and regular (16 relations, 24
rules); CLUTRR's larger inventory weakens the same shortcut.

### What this licenses

**The floor for any composition claim on this task is `shortcut_floor` —
0.546 — not `majority_floor`.** Raising the floor is the honest response to a
leak that cannot be cheaply removed, and the three floors are asserted in strict
order so the weak one cannot be quoted by accident. g8-01's seq-1536 row was
withdrawn for exactly that mistake.

### What it does NOT license

Any statement about whether this model can compose typed relations. **That has
not been measured** — 0.407 is below the floor that matters. The instrument
exists and is honest about its own ceiling; the experiment comes next.

Enriching the rule table would raise the ceiling and is the obvious improvement,
but inventing kinship rules risks encoding ones that are wrong, which is a worse
failure than a stated-and-bounded shortcut.

## 100. The published rules exist, and using their STRUCTURE fixes decision 99's leak

John asked whether a pre-published version of the benchmark should be used
instead of a hand-made one. It should, and the answer changes the task.

CLUTRR's rules are public (`rules_store.yaml`, facebookresearch/clutrr). Reading
them named my defect immediately: **CLUTRR's relations are gender-free** —
`child`, `SO`, `sibling`, `grand`, `un`, `in-law`, each with an inverse — and
gender is applied later, at language realisation.

Decision 99's table baked gender into the relation names. `mother` and `father`
compose **identically**, so sixteen gendered relations carried no more
compositional structure than eight, every prefix had exactly two reachable
answers, and guessing from the prefix was worth 0.546. Gender was multiplying
the inventory while contributing nothing to composition.

**Their table is also deliberately partial**, with a commented-out rule and the
reasoning attached: `grand` then `inv-child` is not `child`, because the person
reached could be an in-law. That is the same argument decision 99 made
independently, which is worth recording — a partial table is the considered
position and not an unfinished one.

### On licensing, because it is John's call and not mine

CLUTRR is **CC BY-NC 4.0 — non-commercial only**. Fine for research and a
problem if Open Plexus ever has a commercial dimension. So the rules were
**not** vendored: kinship composition facts are not copyrightable, the valuable
part is the structural insight, and the table here is written independently with
CLUTRR cited as the design source. Using their generator for
published-comparable numbers is a separate decision that would accept the NC
term.

### The second defect: sampling, not rules

Gender-free relations alone made things *worse* in aggregate — the majority
floor rose to 0.433 at three hops and a **suffix** shortcut appeared at 0.708 —
because walking the table takes whatever answer falls out and the reachable
answers concentrate hard.

Fixed by **sampling the answer uniformly** and then a path that reaches it,
which makes the majority floor `1/(reachable answers)` by construction.

    hops   reachable  majority  first   last    ends
       1          10     0.109  1.000  1.000   1.000
       2           9     0.116  0.465  0.559   1.000
       3           8     0.133  0.261  0.549   0.724
       4           8     0.133  0.223  0.550   0.629

### And a framing error of mine, which `ends` exposed

`ends` is 1.000 at two hops. That is not a leak — **at two hops the path IS its
two ends**, so the number is a tautology.

More importantly, **the path is not observable**. The model sees facts and two
people; learning any relation of the path requires searching the graph. So only
two of those columns are *floors*:

- `majority` needs nothing.
- `first` is reachable: retrieving the relation stated for the queried subject
  gives `path[0]` directly, which is exactly what a one-hop model does.
- `last` and `ends` require reaching the far end, **which is the work the task
  is asking for**.

Treating all four as floors — which the first version did — would have set an
impossible bar and made an honest result look like a failure. `ends` remains
reported as an information bound: 0.724 at three hops and 0.629 at four, so the
middle of the path carries real information and the depth axis is worth having.

### G0 on the rebuilt task

    hops 1 (majority floor 0.120)    object last 0.033   subject last 0.713
    hops 2 (floor to beat 0.465)     object last 0.060   subject last 0.227

**G0 passes** — one hop clears its floor 5.9×, so the architecture can address
this task's shape. Two hops sits at 0.227, *below* the 0.465 a non-composing
model could reach, against a ceiling of 1.000.

### What this licenses

A valid instrument with the bar stated **before** the experiment rather than
after: floor 0.465, ceiling 1.000, positive control 0.713. The hop mechanism and
gate have not been run on it — the G0 probe is a one-hop model and cannot
compose by construction. That run is the next thing.

### What it does NOT license

Decision 99's numbers. The 0.546 shortcut, the 0.407, and the three floors there
were all measured on the gendered table and are **superseded**, not refined.

## 101. The hop mechanism REPLACES retrievals, it does not COMBINE them

The bar was set in decision 100 before the run: floor 0.470, control 0.713,
ceiling 1.000. The result:

    task hops 2   floor to beat 0.470
      model hops 1                0.347
      model hops 2                0.027    <- not even a relation, 79%
      model hops 2 + gate         0.187

    task hops 3   floor to beat 0.282
      model hops 1                0.120
      model hops 3                0.047
      model hops 3 + gate         0.093

**Turning hops on makes it thirteen times worse**, and the gate recovers only
part of that. Nothing here clears the floor.

### Where the hops actually go

A fact is laid down as `[subject, relation, object]`, so from the subject:

    hop 1: RELATION (on the path) 48%
    hop 2: PERSON 90%
    hop 3: PERSON 61%

Hop 1 is right. **Hop 2 lands on a person** — it re-encodes the relation it just
decoded and retrieves what follows *that*, which is the object of the same fact.
The mechanism walks **along a fact**, not **across the graph** to the next one.

### The deeper reason, which the traversal bug was hiding

Fixing the traversal would not fix this. Composing `R1` with `R2` requires
**holding both and applying a binary function**. But each hop *replaces*
`retrieved`, and the readout maps **one** retrieval to an answer. There is
nowhere for `R1` to be while `R2` is fetched.

So the mechanism does **sequential retrieval**, not composition:

    replace   follow a pointer, keep only where you land   -- chains
    combine   hold two things and apply a rule to them     -- kinship

### What this does to decision 92

It narrows it rather than contradicting it. Zero-shot generalisation to unseen
depth is real, and it is generalisation over **how many times to repeat one
replace**. On chains, token adjacency *is* the relation graph, so replacing is
sufficient and the task reached 1.000. That result stands with its scope
corrected: **the hop mechanism composes pointers, not relations.**

Worth saying plainly that this is the outcome kinship was built to expose, and
it exposed it on the first run — which is what a second task is for. A model
perfect at "follow the arrow k times" with no way to represent "these two
relation types combine into a third" is exactly the gap decision 99 predicted.

### What this licenses

A named next mechanism: **carry state across hops instead of overwriting it.**
Something that accumulates — the retrievals so far, or a running composed value
— and a readout that consumes it. That is a bigger change than the gate and it
is the first thing on this project's path that requires holding two things at
once.

### What it does NOT license

Concluding that this architecture *cannot* compose typed relations. What is
measured is that **this hop mechanism** does not, and the reason is structural
and identified. An accumulator has not been tried.

Nor is the traversal problem separately settled: even a mechanism that combines
would still need to reach the second fact, and `key(relation)` is superposed
across every fact sharing that relation. Two problems, not one.

## 102. The accumulator is built, my reason for choosing it was wrong, and traversal is now the only blocker

Decision 101 named the missing mechanism: carry state across hops instead of
overwriting it. `hop_accumulate` does that, with `replace` the default so every
earlier number is unchanged and the golden values are bit-identical.

### I picked the wrong combiner, for a reason that does not hold

The argument was that concatenation cannot work — a linear readout over
`[r1, r2]` learns only `f(r1) + g(r2)`, and composition is not additive, since
`child` then `sibling` is `child` while `child` then `SO` is `in-law`. So an
elementwise product was chosen to carry the interaction.

Fitting a linear map from a combined pair to the answer, over the entire rule
table, with no model or store involved:

    product   0.812      concat   1.000      convolve   0.812

**Concatenation is perfect and the multiplicative bindings lose information.**
The argument confused a functional form with a classification problem: sixteen
rules in a 128-wide space are linearly separable whatever structure the labels
have, and a product of two random vectors does not keep its operands
recoverable. `bind` is kept as the measured alternative rather than deleted.

Whether concat still wins with far more than sixteen rules is a scale question
and is **not** settled by this.

### On the task

    task hops 2   floor 0.470        task hops 3   floor 0.282
      hops 1 replace     0.347         hops 1 replace     0.120
      hops 2 replace     0.027         hops 3 replace     0.047
      hops 2 bind        0.067         hops 3 bind        0.060
      hops 2 concat      0.347         hops 3 concat      0.180

**Concat exactly matches the one-hop model at two hops** — 0.347 to three
decimals — and that is not luck. Hop 2 retrieves a *person*, which carries no
information about the second relation, so the readout learns to ignore those
columns and the model reduces to its one-hop self. At three hops concat does
beat one hop (0.180 against 0.120), so there is some signal deeper in.

So the accumulator now does its job and no longer *harms* the way `replace` and
`bind` did. **What it holds is still the wrong second thing.**

### What this licenses

Traversal is the single remaining blocker and its cause is identified. To reach
the second fact the model needs `M`, the middle person — which lives in fact
`[S, R1, M]` — and then `key(M) -> R2`. The obstacle is that `key(R1)` is
superposed across every fact sharing that relation, so following it retrieves an
average of every such object.

`context_keys` already binds `(previous, token)` pairs, which would make
`key(S, R1) -> M` a distinct binding. Whether a hop can *construct* that pair
key is the next design question.

### What it does NOT license

Any claim that concat helps on tasks the traversal already serves. On chains it
would be extra parameters with nothing new to see, and it is refused alongside
the gate and the hidden layer rather than silently composed with them.

The near-miss worth recording: under `bind` the accumulator and the newest
retrieval differ, and the decode must read the **newest**. Decoding the
accumulator asks what token `R1`-and-`R2`-together names, which is nothing —
and because the two are the same vector under `replace`, every default result
and every structural test would have passed anyway.
`a-hop-decodes-from-the-accumulator` is the mutation, verified caught.

## 103. The store cannot hold an entity that appears in two facts

Traversal was supposed to be the last blocker. An oracle says otherwise, and
what it says is more fundamental than traversal.

### The oracle, and the number that gave it away

Hop 2 was handed the correct second relation and nothing else changed:

    accumulate    real hop 2   ORACLE hop 2
    replace            0.027          0.560
    concat             0.347          0.560

**Identical.** If concat were using hop 1, holding both `R1` and `R2` should
reach about 1.000 — that is what fitting a linear map over the whole rule table
scores. 0.560 is instead exactly the `last`-relation information bound (0.559
in decision 100). The readout is getting **nothing** from hop 1.

### Why, and it is not about hops at all

Hop 1 finds the queried subject's own relation, split by how many facts that
person appears in anywhere:

    appearances   sequences   hop 1 correct
              1         146          0.959
              2         145          0.366
              3          81          0.321
              4          23          0.348

**One appearance is near perfect. Two collapses it.**

`key(person)` accumulates one binding per appearance and a retrieval returns
their **sum**. A person who is the subject of one fact and the object of another
has both bindings on the same key, and the store hands back a superposition of
"the relation I am the subject of" and "whatever followed my other mention".

### This is not a defect in the task

It is what relational data *is*. Every knowledge graph has entities in many
relations; an entity in exactly one is a degenerate case. Decision 84 hit the
same wall on chains and the fix there was to make every symbol appear **once**,
by laying chains out contiguously — which worked only because a chain is a path.
**A graph cannot be laid out that way**, and there is nothing to redesign.

### What this does to decisions 101 and 102

It puts them downstream of something more basic. Composition needs two
retrievals held together (101) and reached correctly (102), and **both assume
the individual retrievals are right**. At two appearances they are right about a
third of the time. Fixing traversal on top of a store that cannot answer a
single-fact question would not have produced a working model, and would have
looked like the mechanism failing.

### What this licenses

**Pair keys are no longer an optimisation, they are the blocker.**
`context_keys` already binds `(previous, token)` rather than `token`, which
makes `key(S, R1)` distinct from `key(X, S)` and gives an entity one key per
role rather than one key total. That is the mechanism to measure next, and the
prediction is specific and falsifiable: **hop-1 accuracy at two-or-more
appearances should rise toward the 0.959 that one appearance already reaches.**

It also raises a question about every earlier result on chains, where the
contiguous layout guaranteed one appearance per symbol. Those numbers were
measured in the degenerate case, and how much of decision 92's 1.000 survives an
entity appearing twice is **not known**.

### What it does NOT license

Concluding the architecture cannot do relational work. What is measured is that
**single-token keys** cannot, and the reason is arithmetic rather than
mysterious. `context_keys` exists and is untried on this.

## 104. Pair keys largely fix it, and a scale register now exists

Decision 103's prediction, written before the run: pair keys should raise
hop-1 accuracy at two-or-more appearances toward the 0.959 that one appearance
already reaches.

`context_keys` binds `(previous, token)`, which is only usable if what precedes
a fact's subject is predictable — otherwise the question cannot reconstruct the
key. So the task now writes a **fact marker** before every fact, and the
question ends `FACT subject`. `key(FACT, S)` is then "S in **subject** role",
distinct from `key(R, S)`, which is "S in object role" — exactly the two
bindings that were colliding.

    appearances   sequences   single key   PAIR key
              1         146        0.884       0.918
              2         145        0.303       0.628
              3          81        0.198       0.568
              4          23        0.087       0.565
        overall                    0.480       0.710

**The collapse largely goes.** 2.1× at two appearances, **6.5× at four**, and
the curve flattens instead of falling off a cliff.

### Confirmed, but not to the predicted level, and the residual has a cause

The prediction said "toward 0.959". It reaches ~0.57–0.63, not ~0.92.

Pair keys separate an entity's **roles**. They do nothing for an entity that
appears twice in the **same** role — a person who is the subject of two facts
puts two bindings back on `key(FACT, S)`, and the store sums them again. That
part is genuine ambiguity in the question rather than a limitation of the store:
"what relation does S hold" has two answers, and only the path says which.

So the mechanism does what it was predicted to do, on the collision it was aimed
at, and a second collision remains that it was never going to address.

### Numbers here are not comparable to decision 103's

The task changed to make pair keys usable — a marker before every fact, and a
longer sequence. Single-key accuracy at one appearance reads 0.884 here against
0.959 there for that reason. **Only the within-run comparison is a measurement.**

### And a scale register, which John asked for

[`docs/SCALE.md`](docs/SCALE.md) records every choice known to depend on the
size it was measured at: what was chosen, at what scale, what would trigger
revisiting it, and what to try instead. Six rows to start — the readout's
pooling, dimensions per node, how a hop combines retrievals, single versus pair
keys, gate sharpness, and store capacity.

The rule in CLAUDE.md is that a row is added **when the choice is made**, and
that the trigger also lives in the config docstring, since that is where someone
reading the code will be. `hop_accumulate="concat"` is the motivating case: it
beat a true binding 1.000 to 0.812, but only because sixteen rules in a
128-wide space are linearly separable whatever the labels do — a property of
having few rules, and nothing in the result says so.

### What this does NOT license

An end-to-end number. This measures **hop 1 in isolation** — whether the store
can answer a single-fact question about a repeated entity. Composition on top of
it has not been re-run, and decisions 101 and 102 were both measured on the
single-key task where hop 1 was right under half the time.

## 105. Hops and pair keys do not compose, and the combination produced numbers anyway

Re-running decisions 101 and 102 on pair keys was supposed to say whether their
conclusions survive a reliable hop 1. It says something else first.

    task hops 2, floor 0.470      single key   PAIR key
      hops 1 replace                   0.280       0.413
      hops 2 replace                   0.080       0.040
      hops 2 concat                    0.327       0.413

    task hops 3, floor 0.282
      hops 1 replace                   0.147       0.100
      hops 3 concat                    0.180       0.120

Pair keys improve hop 1 as decision 104 said. But concat again **exactly**
matches the one-hop model (0.413), and the three-hop numbers get *worse* — which
is the tell.

### The two key spaces are orthogonal

A hop re-encodes its decoded token through `Wk`, a **single-token** table.
`context_keys` derives the store's keys from `(previous, token)` **pairs**.
Measured cosine between `context_key(5, 7)` and `wk[7]`:

    -0.069

So with both on, **every hop after the first queries a key space nothing was
ever written to.** It gets noise back, and the model still returns answers,
still trains, and still reports accuracies. Nothing errors.

**The multi-hop `PAIR key` column above is therefore meaningless**, and reading
those numbers as "worse" was wrong — they are not measurements. The hop-1 rows
stand.

### Refused rather than left available

`hops > 1` with `context_keys` now raises. A hop that constructs a **pair** key
is the mechanism this needs and it does not exist; until it does, the
combination is a configuration that produces plausible output without meaning,
which is the failure class this project exists to catch.
`hops-are-allowed-to-use-pair-keys` is the mutation, verified caught, and
`test_the_two_key_spaces_really_are_unrelated` records the measurement the guard
rests on so it can be relaxed if that ever changes.

### So decisions 101 and 102 stand, and the question they were re-run to answer is still open

Whether the accumulator works given a reliable hop 1 **cannot be answered
yet** — the only way to get a reliable hop 1 is pair keys, and hops cannot use
them. The two fixes are individually correct and mutually unusable.

### What this licenses

The next mechanism is now forced and narrow: **a hop must re-encode into the
store's own key space.** With pair keys that means constructing
`context_key(marker, decoded)` rather than `wk[decoded]` — the decoded token is
already in hand, and what it must be paired with is the question. Hardcoding the
task's fact marker would work and would be task knowledge in the model; learning
which context to pair with is the honest version and is a real design problem.

### What it does NOT license

Any claim about which of the two fixes matters more. They have never run
together, so their interaction is unmeasured — and the one number that looked
like an interaction (three hops getting worse) was noise from an unwritten key
space.

## 106. Composition degrades under repeated entities, gracefully, and 1.000 was the degenerate case

Decision 103 raised a doubt over every chain result: contiguous disjoint chains
give each symbol **exactly one appearance**, which is the one case the store
handles well. `linked_chains` joins chains end-to-start, so the shared symbol is
a target in one and a source in the next, while the answer stays determined —
stressing the store rather than the task.

    task  model  gate   linked 0   linked 2   linked 4
       1      1     -      1.000      0.950      0.975
       2      1     -      0.000      0.000      0.000
       2      2     -      0.995      0.815      0.630
       2      2   yes      0.970      0.790      0.610
       3      3   yes      0.955      0.775      0.645

**The doubt was justified.** Composition falls from 0.995 to **0.630** with four
of six chains linked — so decision 92's 1.000 is the number for a layout that
guaranteed away the store's hardest case, and it should not be quoted as the
model's composition ability without that condition attached.

**But it degrades rather than collapsing.** 0.630 is still 3.8× the 0.167 floor,
and the negative control holds at every link level: a one-hop model stays at
**0.000** on the two-hop task, so composition is still required and still
happening.

### Why chains survive what kinship does not

Single-hop retrieval barely moves — 1.000 to 0.975 — against kinship's cliff
from 0.884 to 0.303. Two reasons, and both are properties of the data rather
than of the model:

- On a chain a repeated symbol has one binding to its **successor** and one to
  the **separator**. A marker is easy to tell from a symbol. In kinship both
  bindings are meaningful tokens.
- A linked chain symbol appears **twice**. A kinship entity appears up to five
  times, and decision 103's curve is steepest over exactly that range.

So the two results agree: the store degrades with the number of bindings on a
key and with how confusable they are. Chains are the mild end of that and
kinship the harsh end.

### What this licenses

Quoting composition results **with the repetition rate attached**, the way
window results are quoted with the run length after decision 82. "1.000" is
true of disjoint chains; "0.630" is true at four joins in six; neither is *the*
number on its own.

It also weakens the case for treating the pair-key work as urgent for chains —
the mechanism there is not what is failing.

### What it does NOT license

Any claim about churn or depth generalisation under repetition. Decisions 90 to
92 were all measured on disjoint chains and **none has been re-run linked**.
This says composition survives; it says nothing about whether zero-shot depth
transfer or the 0.928-at-half-the-machine result do.

## 107. The traversal mechanism is not worth building, and the real blocker is per-step fidelity

Decision 105 called a pair-key hop "forced and narrow" and made it the next
mechanism. Measuring its ceiling first says **do not build it.**

The ideal traversal, done by hand outside the model — `key(FACT,S) → R1`, then
`key(S,R1) → M`, then `key(FACT,M) → R2`, then a linear readout on `[r1, r2]`:

    2-hop kinship, floor to beat 0.475
      current-style re-encode (single-token)   0.435
      IDEAL pair-key traversal                 0.485

**A perfect traversal buys 0.05** and lands barely above the floor. The
mechanism would have been largely wasted work, and the only reason that is known
is that the ceiling was measured before the build.

### Why: compounding, and the breakdown is the useful part

    step                      chained   given a perfect input
    1  key(FACT,S) -> R1        0.710                   0.710
    2  key(S,R1)   -> M         0.703                   0.960
    3  key(FACT,M) -> R2        0.497                   0.677

    product of the three isolated steps   0.462
    end-to-end ideal traversal            0.485

The product of the isolated steps **is** the end-to-end number. Compounding is
the whole explanation, and no routing fixes it.

**Step 2 — the pair-key traversal itself — is 0.960.** It works. The weak steps
are 1 and 3, and both are the same operation: `key(FACT, X) → X's relation`.

### So the blocker is the same-role collision, which I had just deprioritised

Decision 104 fixed the subject-versus-object collision and left the
subject-of-two-facts one, which I called a residual and put behind traversal.
That was wrong. **It is the thing capping steps 1 and 3**, and therefore the
thing capping everything.

The arithmetic makes the case: at the current 0.710 / 0.960 / 0.677 the product
is 0.462. Take the two entity-lookup steps to the 0.95 that step 2 already
reaches and the product is **0.87**.

### What this licenses

A reordering, and a sharper target than "make retrieval better". The operation
to fix is specifically *retrieve the relation an entity holds as a subject, when
it is the subject of several facts*. The disambiguator exists in the question —
it names the **object** — so a key over `(subject, object)` rather than
`(marker, subject)` would be unique. Whether the model can form that key when
the object is the far end of a multi-hop question is the design problem, and it
is a different one from traversal.

It also joins this line to the saturation question. Per-step fidelity falling as
bindings-per-key rises is a **capacity** limit, not a mechanism gap, and
decisions 103, 104 and 106 are all measurements of the same curve.

### What it does NOT license

Discarding pair keys. Step 2 at 0.960 is the pair-key mechanism working, and it
is the only step that does work — the finding is that the traversal HOP is not
worth building, not that pair keys were a mistake.

## 108. The store is not losing information. The question is ambiguous, and resolving it is SEARCH

Decision 107 made the same-role collision the blocker and proposed a better key.
Measuring first says **no key fixes this**, because nothing is being lost.

Step 1 — `key(FACT, S)` → the relation S holds — split by S's **out-degree**,
the number of facts in which S is the subject:

    out-degree   seqs   correct    1/k    ANY relation S holds
             1    316     0.915  1.000                   0.915
             2    149     0.349  0.500                   0.960
             3     31     0.355  0.333                   0.968

**The retrieval returns a relation S genuinely holds 96% of the time**, at every
out-degree. And "correct" tracks **1/k** — chance among the valid options.

So the store is answering *"what relation does S hold"* correctly. The question
needs *"which of S's relations leads to T"*, and the store was never asked that.

### Superposition and ambiguity are different, and only one was ever fixable

This reconciles the whole line rather than overturning it:

- **Single-token keys lost information.** A subject-role and an object-role
  binding on one key, summed: 0.303 at two appearances. Real, and pair keys
  recovered it — 0.915 at out-degree 1.
- **What remains is ambiguity.** Several facts, all with S as subject, all
  correct answers to the question the key encodes. Nothing is lost; the answer
  is underdetermined.

Decisions 103, 104 and 107 all measured "correct" and so conflated the two.
**The store was never as broken as those numbers made it look**, and the
`ANY relation S holds` column is what separates them.

### What this licenses, and it is the sharpest architectural statement of the line

**Multi-hop reasoning over a BRANCHING graph requires search, and an associative
store does retrieval.** Resolving which of S's relations to follow means trying
one, seeing where it lands, and backtracking — a fundamentally different
operation from a keyed lookup, and one nothing in this architecture performs.

That is not a defect to fix with a better key or a better hop. It is a missing
capability, and naming it is worth more than the four mechanisms that were built
while it was invisible.

The chain task hid it entirely: a chain has **out-degree 1 by construction**,
which is exactly the row that scores 0.915. Every composition result on chains
was measured where no search was needed.

### What it does NOT license

Concluding search must be added. **Branching may simply be out of scope**, and
"this architecture does retrieval, not search" is a legitimate and honest
boundary to state rather than a gap to close. Deciding that is a project
question, not a measurement.

Nor does it excuse the per-step fidelity at out-degree 1 — 0.915, not 1.000 —
which is still short and is still the capacity question decision 107 pointed at.

## 109. The store is not the saturation bottleneck — capacity scales with d²

Decision 63 is the oldest unresolved result here: more data does not help and
more width does not help. It undercuts the distributed premise directly, since
the argument for many nodes is that more nodes means more capability.

Measured at the substrate, with no learning and no task: write N random
`(key, value)` bindings into a width-`d` store, retrieve each key, decode by
nearest value.

    width      8      16      32      64     128     256
       16  0.500   0.276   0.174   0.057   0.029   0.012
       32  0.979   0.901   0.750   0.461   0.212   0.086
       64  1.000   1.000   0.997   0.987   0.873   0.568
      128  1.000   1.000   1.000   1.000   0.999   0.997
      256  1.000   1.000   1.000   1.000   1.000   1.000

    bindings held at 90% recovery
      width  32     16     0.50 per dimension
      width  64     96     1.50 per dimension
      width 128    384     3.00 per dimension

**Capacity scales roughly with d², not linearly** — quadrupling as width
doubles. (Width 256 reads 384 only because the sweep stopped there; it was at
1.000 throughout, so its capacity is higher and unmeasured.)

**At width 64 the store holds ~96 bindings and this project's tasks write about
10 to 30.** We are nowhere near the ceiling, so the store is not what saturates.

### The first version of this probe said the opposite, and the tell was an exact tie

It sampled key tokens **with replacement** from a 200-token vocabulary, so at
256 bindings most keys were written twice with different values —
**contradictory by construction**, unrecoverable for reasons unrelated to width.
It produced a clean plateau with widths 128 and 256 identical to three decimals
across four loads.

An exact tie across a doubling of width is a bug, not a finding. Chasing it
found that the probe had reproduced **decision 108's ambiguity by accident**, in
the experiment meant to measure something else — and the wrong conclusion would
have been "the store is the ceiling", which is the opposite of the truth and
would have redirected the project.

### What this licenses

Saturation is **not** a superposition-capacity limit at the sizes used, and the
search for it narrows to what remains:

- **The single linear readout.** `Wo` is the only thing that learns across
  sequences, and one linear map has a ceiling regardless of how wide the store
  beneath it is.
- **Frozen random representations.** Decision 93 showed these block learning any
  *class* of token, and decision 94 measured `value_lr` failing to fix it.

Both are testable separately and neither has been.

It also settles a cross-reference: kinship's 0.915 at out-degree 1 is **not**
capacity. Ten facts in a width-64 store is an order of magnitude below the
measured ceiling, so that residual is something else — most likely decode
confusability at this vocabulary size.

### What it does NOT license

Any claim about capacity under the model's own write path. This wrote outer
products directly; the model applies `decay` and a `memory_cap` on the store's
norm, and **neither was active here**. Capacity under those is a different
number and is the one that matters in practice.

## 110. The readout is not at capacity either — so saturation is not a capacity limit

Decision 109 eliminated the store. This measures the second candidate the same
way, and the prediction written before the run was **wrong**.

The prediction: a linear map separates about `d` points, so the readout should
cap near `d` while the store holds `d²` — saturation with a number and a place.

    items held at 90%          readout          store (decision 109)
      width  32          64   2.00 / dim      16   0.50 / dim
      width  64         128   2.00 / dim      96   1.50 / dim
      width 128         256   2.00 / dim     384   3.00 / dim

    hidden readout: 1.000 at every width and every load tested

**The readout holds 2.00 items per dimension**, not dramatically less than the
store — and at width 32 it is four times *larger*. The "much lower" half of the
prediction was simply wrong.

### What is right is the SCALING, and it matters later rather than now

The readout grows **linearly** — 2d, flat per dimension. The store grows
**quadratically** — 0.5, 1.5, 3.0 per dimension. They cross around width ~100,
and above that the readout is the binding constraint: doubling the width doubles
what the readout can hold while quadrupling what the store can.

So a linear readout **will** become the ceiling as this scales, and a hidden
layer removes that limit in this range entirely. That is the same mechanism
decision 83 found was the largest single factor on text (+0.63 bits, 9 of 9
cells), now with a reason attached.

### But it does not explain decision 63

At widths 64–128, where saturation was measured, **both capacities exceed what
the tasks demand** — the store by an order of magnitude, the readout by several
times. And both numbers are worst cases: these are *random* assignments, and a
structured task should need less, not more.

**So saturation is not a capacity limit in either component.** Both mechanical
candidates are now eliminated.

### What this licenses

The search narrows to what is left, and it is no longer mechanical:

- **Frozen random representations** — decision 93 showed they block learning any
  *class* of token and decision 94 measured `value_lr` failing to fix it. This
  is now the leading candidate by elimination rather than by evidence.
- **The objective or the task itself** — that more data does not help may be a
  statement about what next-character prediction offers a model of this shape,
  rather than about the shape.

It also puts a `hidden` readout on the scaling path for a reason beyond its text
result: it is what keeps the readout from becoming the ceiling above width ~100.

### What it does NOT license

Reading these as the model's real capacities. Both probes measure components in
isolation, with random data, no decay, no cap and no interference between them.
The composed system is what saturates and it has **not** been measured this way
— what is established is only that neither part is individually at its limit.

## 111. Search does not pay, because the verifier is built from the same noisy retrievals

John's answer to decision 108's open question was to add any capability that
gets closer to the goal. Search is that capability, so its ceiling was measured
before building it — the discipline that saved the traversal hop in decision 107.

The ideal search, by hand: branch over the top-k candidate relations at **both**
hops, follow each to an endpoint, and keep the branch whose endpoint is the
target the question named. **That target is already in the question and the
model currently ignores it**, so verification costs no new information, only
computation.

    2-hop kinship, floor to beat 0.475
    ideal traversal without search (decision 107)   0.485

      beam 1                                       0.485
      beam 2                                       0.510
      beam 3                                       0.510
      beam 4                                       0.495
      oracle first relation                        0.545

**Search plateaus at ~0.51**, barely above the floor, and beam 4 is worse than
beam 2. About **+0.03 for k² the retrievals.**

### Why, and it is not that search is the wrong idea

To verify that a branch reaches T you must **retrieve its endpoint** — and that
retrieval is exactly as unreliable as the one that produced the branch. Step 3
was measured at 0.677 in decision 107 and the verifier is made of the same
parts.

**You cannot search your way out of noisy primitives, because the verifier is
built from the primitives.** Generate-and-verify needs the verify half to be
better than the generate half, and here they are the same operation.

The oracle row is the confirmation: a **perfect** first relation still only
reaches 0.545, so even removing step 1's ambiguity entirely leaves most of the
gap. The ambiguity was never the binding constraint.

### What this licenses

**Retrieval fidelity is the prerequisite for everything else**, and this is the
fourth mechanism to fail against it — traversal (107), the accumulator (102),
pair keys beyond their own collision (105), and now search. Each was correct in
itself and each was capped by the same number.

The dependency is explicit and gives a threshold to aim at: at out-degree 1
retrieval is **0.915** and at higher out-degrees it is ~0.35. Search would work
if retrieval were reliable — so the order is fidelity first, search second, and
search is worth revisiting the moment fidelity moves.

### What it does NOT license

Concluding search is unnecessary. It remains the right answer to branching
ambiguity and the measurement says only that it cannot pay **yet**. This is a
sequencing result, not a rejection — and the implementation was deliberately not
built, so nothing has to be undone when it is.

## 112. Retrieval fidelity is a WIDTH limit, and both my binding counts were wrong

Four mechanisms failed against retrieval fidelity, so the question is why a
single unambiguous lookup is 0.915 rather than ~1.0. Ablated at out-degree 1,
where there is no ambiguity to confuse it:

    as configured                    0.915
    no decay (1.0)                   0.927
    no cap (1e9)                     0.915
    neither decay nor cap            0.927
    width 128                        1.000
    width 256                        1.000
    half the facts                   0.962

**Width, and nothing else.** Decay costs 0.012 and the cap costs 0.000 — both
were plausible suspects and both are cleared. Doubling the width takes fidelity
to a clean 1.000.

### Two wrong counts of mine, in opposite directions

Decision 109 concluded the store was not the bottleneck on the grounds that
"tasks write 10 to 30 bindings". That counted **facts**. The store binds every
adjacent pair, so the count is per token.

Correcting that, I then said a 160-token sequence writes ~160 bindings. That
counted `seq_len`, which is a **maximum** — kinship has no filler, so the
sequence is 45 tokens and writes **44 bindings**. Measured, not inferred.

### And 44 is UNDER capacity, so the width effect is not simply load

Decision 109 measured width 64 holding ~96 bindings, with 0.997 at a load of 32
and 0.987 at 64. At 44 it predicts about 0.99. Kinship gets **0.915**.

So kinship's bindings are **harder than random ones at the same load**, and why
is not measured. Its keys are hashed `(previous, token)` pairs rather than rows
of `Wk`, and its values repeat heavily — a handful of relations across 44
bindings — either of which could reduce the effective capacity. **Unmeasured is
the honest state of it.**

### The saturation hypothesis this suggests, which IS testable

Decision 63's text runs used sequences up to **1536 tokens**, so a load of
~1536 bindings against a width-128 capacity of ~384. **Four times over
capacity**, and width 64 is sixteen times over.

That would explain "more width does not help" exactly: doubling 64 to 128 moves
capacity from ~96 to ~384 while the load stays at 1536, so the model is far past
saturation either way and the doubling is invisible. It also explains "more data
does not help", since the store is per-sequence working memory and more corpus
does not raise what a single sequence can hold.

**The prediction that would test it:** at SHORT sequences, where load is below
capacity, width should help — and at long ones it should not until width is
large enough to matter. That is a clean two-axis sweep and it is the first
concrete, falsifiable account of decision 63 this project has had.

### What this does NOT license

Treating the hypothesis as established. It is arithmetic plus one ablation on a
different task, and decision 63 was measured on text with a different key
scheme. The sweep has not been run. **Decision 113 withdraws it.**

## 113. Decision 112's saturation hypothesis was aimed at the wrong axis — WIDTH ALREADY HELPS

Before costing the sweep decision 112 proposed, I read the record it was built
on. It does not say what I said it says.

**Width is not flat.** g11-04, on Tiny Shakespeare:

    arm         d=16     d=32     d=64    d=128     fitted b      R²
    single     5.730    5.624    5.505    5.494      -0.0213    0.92
    context    5.917    5.827    5.759    5.703      -0.0176    0.99
    backprop   4.197    4.150    4.157    4.175      -0.0021    0.13

Our arms **improve with width**. The *baseline* is the flat one over that range,
which is why g11-04 was ruled inadmissible as a comparison — not because our
model failed to scale.

**The flat axis is DATA.** Decision 63: the model stops improving at about
16,000 characters, and total movement from 4,000 to 125,000 is 0.039 bits
against a seed spread of 0.04. Noise.

So decision 112's proposed two-axis sweep would have tested a claim nobody
makes. **Withdrawn before dispatch**, which is the only reason it cost nothing.

### What decision 112 got right, and it is not nothing

The ablation stands on its own terms: retrieval fidelity on kinship is a width
limit, decay costs 0.012 and the store cap costs 0.000. Both suspects cleared,
and 44 bindings measured rather than guessed. That is a fact about **this task's
retrieval**, not about text scaling, and conflating the two was the error.

### And the data saturation is not mysterious — it is already explained

**The store is per-sequence working memory, rebuilt every chunk.** So `Wo` is
the only thing that persists across the corpus, and a single linear map
converges fast — decision 63 measured that as 16,000 characters.

More data cannot help a model whose only durable parameter has already
converged. That is not a capacity limit and not an architectural mystery; it is
arithmetic about what learns.

### What this licenses

**The target is persistent learnable capacity**, and it is the same target
decisions 93 and 94 already identified from the other direction: `Wv` and `Wk`
are frozen random, `Wo` is one linear map, and `value_lr` — the mechanism meant
to unfreeze the values — collapses the representation instead of organising it.

So the line from here is not a scaling sweep. It is: **make something other than
one linear map learn across sequences, without collapsing it.** Every other
thread now points there.

### The habit that caught this

Decision 63 itself records three sweeps whose grids did not contain the
phenomenon, and the rule it added was to probe cheaply before fitting an
exponent. This is the same failure one level up — **a hypothesis whose premise
was not checked against the record it cited.** Reading the source cost ten
minutes; the sweep would have cost twelve jobs and answered nothing.

## 114. `value_lr` does not collapse at a sane rate — it works, and it does not help

Decision 113 concluded the target was persistent learnable capacity, because
`Wo` is the only thing that persists and one linear map converges at 16,000
characters. `value_lr` makes `Wv` persist too, and decision 94 said it collapsed
the representation. So `value_centre` was built to remove the shared drift.

Both halves of that turn out to be wrong.

### There is no collapse to fix

After 64,000 characters, width 64:

    arm                  cosine     was     norm     was    |ΔWv|
    frozen               -0.003  -0.003    0.499   0.499     0.00
    value_lr              0.003  -0.003    0.718   0.499     6.34
    value_lr + centre     0.007  -0.003    0.725   0.499     6.37

**The values move a long way and stay spread out.** Decision 94 measured collapse
at `value_lr=0.05`; this is 0.002, twenty-five times smaller, and the cosine does
not move. So **decision 94's finding is a statement about a learning RATE, not
about the mechanism** — and `value_centre` is a fix for a problem that does not
exist in this range. It is kept, refused without `value_lr`, and unused.

### And the mechanism works without helping

    chars      frozen Wv   value_lr   value_lr+centre
     4,000         6.163      6.144            6.152
     8,000         6.100      6.027            6.033
    16,000         6.094      6.061            6.073
    32,000         6.058      6.059            6.085
    64,000         6.072      6.065            6.080

All three flatten together. Making `Wv` learnable — well-behaved, moving
substantially, not collapsing — **does not break the plateau at all**.

So decision 113's conclusion does not survive its own first test. Persistent
learnable capacity was added and the data axis did not move, which means the
plateau is not explained by "only one linear map learns".

### What this licenses

Not much, and saying so is the point. Three explanations for the data plateau
have now been eliminated: store capacity (109), readout capacity (110), and
persistent representation capacity (here). **The plateau is not a capacity limit
of any component measured so far.**

What remains unexamined is the *shape* of what the store can represent rather
than how much. Note 035's claim — that the store holds a bigram count table of
effective rank about 3 whatever the width — is a statement about shape, has
never been re-checked, and would explain a plateau that no amount of capacity
touches.

### What it does NOT license

Comparing these bits to decision 63's. This ran on the project's own notes and
reads 6.09 where decision 63 reads 5.53 on its corpus. **Only the within-run
comparison of the three arms is a measurement here**, and the plateau's shape —
flat after ~16,000 — is what reproduces, not the level.

Nor does it license removing `value_lr`. It is correct, it is cheap, and the
finding is that it does not pay *on this axis* — a task where representation
sharing matters is untested.

## 115. Saturation is CLOSED — note 035 had it, and three alternatives are now eliminated

Re-checked on current code, stable rank `‖S‖²_F/‖S‖²₂` — **naming the measure**,
because conflating it with participation rank refuted a correct hypothesis in
this project once:

     d     er(S)   minus mean   share of d
    32      2.75         2.96         8.6%
    64      3.25         3.09         5.1%
   128      2.96         3.21         2.3%
   256      3.05         3.34         1.2%

**Effective rank ~3 whatever the width**, matching note 035's 2.0–2.2. The share
of available dimensions falls from 8.6% to 1.2% as width grows.

### The account, which was already written

Note 033 measured the store faithfully holding a bigram count table (cosine
0.88+). Note 035's corrected reading follows: **a character bigram table over 66
symbols is intrinsically low-rank** — English is dominated by a few very
frequent characters — so *"the store is not failing to use its width. There is
nothing there to use."*

That explains both axes at once. Width buys little because rank does not grow
with `d`. **And more data buys nothing because bigram counts converge fast** —
decision 63's 16,000 characters is how long it takes to estimate a bigram table,
not a mysterious architectural wall.

### What this session added, which is elimination rather than discovery

The account was already there. What it lacked was the exclusion of competitors,
and three are now measured out:

    store capacity                 decision 109   ~96 bindings at width 64, d² scaling
    readout capacity               decision 110   2.00 items per dimension
    persistent representation      decision 114   Wv learnable, no collapse, no gain

So "the model is bigram-shaped at character level" is no longer the most
plausible story among several — **it is the one left standing**, and the others
failed on their own measurements rather than by argument.

Worth stating plainly: several hours went into re-deriving something the project
already knew. The value is that the alternatives are now closed, not that the
conclusion is new.

### What this licenses — and it is what the project already did

**Character-level bits is the wrong target**, which is exactly note 038's
argument for the relational task: a character bigram table cannot represent a
concept, so the task itself is part of the ceiling. Decision 83 acted on that
before any of this was measured.

**So saturation is not an open problem and should stop being treated as one.**
It is a property of the objective, the objective was already changed, and the
live work is on the relational side — where the blocker is retrieval fidelity
(decision 112) and the ceiling is a real one.

### What it does NOT license

Complacency about the level. This model scores **5.494** on Tiny Shakespeare
where a plain bigram scores **3.583**. Being bigram-*shaped* does not explain
being well short of a bigram, and that gap is unexplained by anything here.

## 116. `carry_store` and `hidden` are superadditive, and I was quoting a superseded configuration

Chasing the level, on the project's own notes corpus (uniform 6.508, unigram
4.721, bigram 3.695), train-then-test:

    chunk    linear   linear+carry   hidden 128   hidden+carry
       64     6.024          5.765        5.574          5.140
      256     5.914          5.755        5.393          5.137

**They compose better than either alone.** At chunk 64, `carry_store` is worth
0.26 and `hidden` 0.45 — and together they are worth **0.88**, not 0.71.

`carry_store` is off by default and its own docstring says it is correct when
consecutive calls are consecutive text, which is exactly the text case. It is
the cheapest unclaimed win measured here.

### The framing error, which is mine

I described the model as "worse than a bigram" and treated that as unexplained.
The 5.494 figure is the **linear** readout, and this project already established
the readout as the ceiling — note 037 and decision 83 record a two-layer readout
recovering 0.63 bits. Quoting the linear number as the model's level was
quoting a configuration the project had already superseded.

### And the remaining gap is not established, because the regimes differ

The best configuration here reads **5.137** against a unigram's 4.721, which
would still be short. But the project's recorded 4.540 is **prequential** — one
pass, predict then learn — on **Tiny Shakespeare**, and this is train-then-test
on the notes corpus, which has 91 symbols against 66.

Different regime, different corpus, different vocabulary. **The comparison is
not admissible** and neither number refutes the other. Saying "the model is
worse than a unigram" would repeat exactly the mistake g11-04 was ruled
inadmissible for.

### What this licenses

Turning `carry_store` on for text work, and re-measuring the level **in one
regime** before anyone concludes anything about it. The honest current statement
is that no single comparable measurement of this model's best configuration
against the n-gram baselines exists.

### What it does NOT license

Any claim that the gap to a unigram is real or that it is not. That is now a
known-unknown with a cheap resolution — run the best configuration
prequentially on Shakespeare, the way 4.540 was measured — rather than a finding
in either direction. **Decision 117 attempted that and could not reproduce it.**

## 117. The prequential reproduction FAILED — 4.540 could not be reobtained, and that is the finding

Decision 116 queued one measurement: the best configuration, prequentially, on
Shakespeare, everything in one regime. Run at 250,000 characters, 62 symbols,
single pass, with the **n-grams scored prequentially too** — a bigram fitted on
the whole corpus is not a fair opponent for a model given one online pass.

    uniform                            5.954
    unigram, prequential               4.776
    bigram, prequential                3.554
    model, default                     5.742   (temperature 0.1)
    model, hidden 128                  5.665   (temperature 0.1)
    model, hidden 128 + carry_store    5.737   (temperature 0.1)

**1.1 bits away from the recorded 4.540**, with the configuration that record
names. The reproduction failed.

### The first attempt was broken, and the way it broke is worth keeping

Scored without a temperature it read **5.920** against a uniform 5.954 — the
model apparently learning nothing. **The delta rule targets a one-hot, so raw
scores sit in about [0, 1], and a softmax over that range is nearly uniform
whatever the model knows.** The number measured the SCALE of the scores, not the
information in them.

The temperature is fitted on the first fifth and applied to the rest, so nothing
is calibrated on text it is then scored against. That moved it to 5.665 — real,
and still nowhere near 4.540.

### What this establishes, which is narrow

**Not** that the model is worse than a unigram. It establishes that **the
recorded 4.540 does not reproduce from its stated description**, and that
whatever produced it differs from "hidden readout, prequential, Shakespeare" in
some way not written down — corpus slice, chunk size, learning rate, the exact
cache, or the temperature treatment.

Chasing hyperparameters until one matches would be fitting a number rather than
measuring one, so it was not attempted.

### What this licenses

**Treat 4.540 as unverified until its setup is identified.** It sits in HANDOFF
as the model's headline text result and is the basis for "unigram BEATEN"; that
claim now has a failed reproduction against it and should not be quoted without
one.

The cheap next step is finding the script that produced it and reading its
configuration, rather than any new experiment.

### What it does NOT license

Concluding the record is wrong. A failed reproduction is a discrepancy between
two measurements, and mine is the newer and less carefully built of the two —
250,000 characters, one seed, a temperature grid of eight points. **Either could
be the error.** **Decision 118 found which.**

## 118. The unigram was never beaten — 4.540 is an offline backprop probe, not the model

Decision 117 could not reproduce 4.540 and recommended archaeology over
hyperparameter search. The archaeology took ten minutes and the answer is that
**there was nothing to reproduce**: that number is not a measurement of this
model.

### Provenance

`4.540` appears **only in HANDOFF.md** — no sweep file, no experiment, no
DECISIONS entry. Its source is note 037's **4.525**, and note 037 states its own
conditions plainly:

> the readouts here are trained with **ordinary backpropagation, offline,
> deliberately** — the question is whether a composed readout would help AT ALL,
> and there is no point asking whether it can be trained locally before knowing
> that.

So the line `2-LAYER READOUT, prequential 4.540 ... unigram BEATEN` was wrong
twice: it is an **offline backprop probe on frozen features**, not the model
under its own learning rule, and it is **not prequential** — it is the opposite
of prequential.

### What the model actually scores, three ways

    g10-12, Tiny Shakespeare, split           5.466
    g11-07, best of EIGHTEEN compositions     5.172   dense/cache128/hidden128
    decision 117, prequential single pass     5.665
    unigram                                   4.829

**None reaches the unigram**, and g11-07 is the best result this project has
ever measured under local learning — 0.34 bits short.

That also explains decision 117 cleanly: I reproduced the model faithfully and
compared it to a number the model never produced. The reproduction did not fail;
the target was wrong.

### Note 037's finding is not diminished — it is the interesting half

**The retrieval carries enough information to beat a unigram, and a linear
readout cannot extract it.** That is a statement about the FEATURES, it is why
`hidden` exists, and it is the strongest positive result on this corpus.

What it does not say is that the model achieves it. Whether a **local** rule can
train such a readout is exactly the open question note 036 begins, and note 037
says so in the sentence that was skipped when the number was copied forward.

### What this licenses

HANDOFF corrected: the table now names the model's best real number (5.172),
marks the unigram **not beaten**, and keeps 4.525 in a clearly separated block
labelled as offline backprop.

**And a rule worth having:** a headline number in HANDOFF must cite a sweep file
or a DECISIONS entry. This one cited nothing for long enough to become the
project's summary of itself, and it survived because HANDOFF is the document
everyone reads and the least often re-derived.

### What it does NOT license

Treating 5.172 as a ceiling. It is the best of eighteen compositions at 60,000
characters on a split protocol, above the ~16,000 saturation point — a LEVEL,
not a slope, as g11-07 says of itself.

## 119. The superposed store EARNS ITS PLACE — note 030's question, answered

BACKLOG's highest-value open item: no benchmark discriminates a superposed store
from a cache, so neither justifies the store. It could not be answered because
**the cache only ever ADDED to the store** — every arm holding a cache also held
the store, so nothing separated them. `cache_only` drops the superposed half of
the read, which is the ablation that was missing.

### Predictions, registered before the run, and three were wrong

    P1  chains: cache-only MATCHES OR BEATS superposed at slots >= load
    P2  the store wins only where bindings EXCEED the slots
    P3  kinship: cache-only beats superposed, because superposition is what
        the 0.915 -> 0.35 collapse is made of
    P4  if P1 and P3 hold, the store has no measured task where it wins

### The measurement

    CHAINS, ~45 bindings written, floor 0.167
      slots    superposed          both    cache only
          8         0.995         0.845         0.120
         16         0.995         0.865         0.270
         32         0.995         1.000         1.000
        128         0.995         1.000         1.000

    KINSHIP, hop-1 fidelity      deg 1    deg 2+
      superposed                 0.911     0.350
      both, 64 slots             0.794     0.392
      cache only, 64 slots       0.444     0.301

**P2 CONFIRMED, decisively.** At 8 slots against ~45 bindings the store scores
0.995 and the cache 0.120 — a factor of eight. The store's structural advantage
is that it holds far more than its size in a degraded form, where a bounded
cache holds exactly its slot count and then fails.

**P3 REFUTED.** Cache-only is far *worse* on kinship at every out-degree
(0.444 against 0.911). Superposition causes the collapse *and* is still better
than the bounded alternative, because 44 bindings do not fit in the cache
either.

**P4 REFUTED, and it is the answer.** The store wins wherever load exceeds
slots, which is most of the regimes this project runs in.

**P1 only partially.** Cache-only ties at 1.000 once slots exceed load; it never
beats.

### This corrects the drift of decisions 103 to 112

Those measured superposition as the fidelity blocker and I had begun treating it
as a straight liability. It is a liability *and* the best option available: the
comparison that matters is not "superposed against exact" but "superposed
against exact-and-bounded", and bounded is what loses.

**The hybrid is not uniformly best either.** On kinship at out-degree 1 the
cache HURTS — 0.911 superposed against 0.794 with the cache added — so
`cache_slots` is a setting to measure per task, not a free improvement.

### What this licenses

Note 030's item can close. The store is justified by a measured structural
advantage rather than by assumption, and the shape of it is stated: **it wins
exactly when bindings exceed slots, ties when they do not.**

### What it does NOT license

One seed, two tasks, a local probe. The effect at 8 slots is a factor of eight
and far beyond noise, but the *crossover* — where slots stop mattering — is a
single-seed estimate and would need a grid to state precisely.

Nor does it justify the store on TEXT, where g11-07 measured `cache128` in the
best arm and this probe did not run.

---

## 120. The four top-level documents were doing each other's jobs, and John asked for three

**John's ask, 2026-07-28:** *"there's lots of churn in the model's current
behavior from past statements ... I'd like to have three separate things clearly
defined in different docs: the goals/constraints of the project, a log of past
decisions and rationale for reference, and then a current working doc of open
questions and in-flight work."*

**The diagnosis is his and it is right.** Four documents totalled **503,000
characters**, and not one of them had a single job:

    GOALS.md      56 KB   intent AND 405 lines of running results
    HANDOFF.md    46 KB   a snapshot that says of itself that it goes stale
    BACKLOG.md    78 KB   a todo list that says of itself that it became a record
    DECISIONS.md 318 KB   history, being read as current state

**Two concrete errors this produced**, both found by reading rather than by any
test, and both already propagated into planned work:

- `GOALS.md` carried `T^0.67` as the live answer for minimum machine width while
  quoting `T^0.82` for the same quantity two paragraphs later, with the
  consequences still computed from the older figure.
- `HANDOFF.md` carried *"prequential 4.540 ... unigram BEATEN"* as this project's
  headline text result for weeks. Decision 118 established it was note 037's
  **offline backprop probe on frozen features**, not the model under its own
  learning rule, and not prequential.

**The failure mode is not size. It is a stale claim wearing a current document's
authority.** A 318 KB log nobody mistakes for current state is harmless; a 46 KB
snapshot that reads as current is not.

### What was done

| document | now |
|---|---|
| `GOALS.md` | intent, constraints, gate ladder. **449 lines, no measurements.** The results narrative moved to `docs/archive/goals-results-log.md` |
| `DECISIONS.md` | entries 83+ with an index and a header saying it is history. **2,394 lines.** Entries 1–82 to `docs/archive/decisions-001-082.md` |
| `STATE.md` | **new.** What is true now, open work in order, in flight, John's calls, and a *do-not-re-propose* table |
| `HANDOFF.md`, `BACKLOG.md` | **retired.** Open items to `STATE.md`; the rest to `docs/archive/backlog-2026-07-28.md` |

Entry 83 is the split point because it is where the relational line begins, which
is the work that is live.

**Nothing was deleted.** Every archive carries a header saying what replaced it,
because the retractions in them are the useful part and several are cited from
notes and decisions.

### The guard, and why the old one was the weaker fix

`tests/test_goals_consistency.py` used to check individual numbers inside
`GOALS.md` — that `T^-0.45` appeared, that `0.82` carried its interval. That
guards one instance of the drift. **The structural version is that `GOALS.md`
carries no measurement at all**, enforced by refusing sweep identifiers
(`g\d+-\d{2}`) and links into `experiments/sweeps/`. There is then no second
answer for it to grow. Rule 18: prefer a rule that makes the mistake
structurally impossible.

The number-drift guards were not dropped — they moved to
`tests/test_archive_consistency.py` and now run against the archive, which is
still cited and still needs them.

Also added: `CLAUDE.md` rule **14b**, the three-documents standard, with the
503,000-character calibration attached; and two tests that the documents point at
each other and say which one wins a disagreement.

### What this cost and what it rules out

**A reader can no longer reconstruct the project's position by reading the newest
decisions.** That was the habit and it is exactly what produced the churn — the
newest entries usually ARE the current state, right up until they are not. The
replacement habit is `STATE.md` first, every session.

**The risk is `STATE.md` growing back into a HANDOFF.** The rule that prevents it
is written into the document itself and pinned by a test: *when something here is
settled it leaves*, and an entry goes in the log.

**To undo:** the archives are verbatim, so `git revert` restores all four
documents intact.

**Taken without asking on the structure**, under standing authorisation — John
specified three documents and their jobs; the split point, the naming, retiring
`HANDOFF.md` and `BACKLOG.md` rather than keeping them as stubs, and the
structural test were mine.

---

## 121. Width does NOT fix retrieval fidelity on the task, and decision 112 was never a bound on it

g13-01, 48 cells, 8 seeds, run 30389532519. **Three of five predictions refuted,
including the control**, and the refutation of the control is the finding.

    hop1-pair      d64 0.726 +/-0.012   d128 0.746 +/-0.010   d256 0.746 +/-0.009
      out-degree 1     1.000 +/-0.000        1.000 +/-0.000        1.000 +/-0.000
      out-degree 2     0.516 +/-0.018        0.548 +/-0.024        0.558 +/-0.014
      out-degree 3+    0.388 +/-0.030        0.435 +/-0.036        0.418 +/-0.032
                 1/k   1.000 / 0.500 / 0.333

**A fourfold width increase buys 0.020 overall and saturates between 128 and
256.** Out-degree 1 is perfect at width 64 already, at +/-0.000 across eight
seeds. There was never anything at that out-degree for width to fix.

### Decision 112's 0.915 does not reproduce end-to-end, and neither number is wrong

112 measured **raw retrieval**, ablated, with no readout learning. This trains
`Wo` over 400 x 4 sequences, and a linear readout recovers the argmax from a
retrieval that is not itself clean.

That is note 037's distinction arriving on the relational task: **the retrieval
CARRIES the information and the question is what can extract it.** A trained
readout extracts it perfectly at out-degree 1.

**The consequence is the important part. Decision 112's number was never a bound
on task performance**, and STATE.md made width the critical path on the strength
of it. That was my error, made in the same session, and it is now corrected.

### What it says about search — and P3's letter disagrees with its substance

P3 predicted out-degree >= 2 would stay within 0.10 of 1/k at every width. Gaps:
+0.016, +0.054, +0.048, **+0.102**, +0.058, +0.084. **One cell of six exceeds the
threshold, by 0.002.** Mechanically refuted, and recorded as refuted.

The 0.10 was mine, chosen before any data, and too tight for a quantity whose
seed SE runs 0.014 to 0.036. What P3 was reaching for does hold: accuracy sits
just above 1/k at every width and no width closes it — +0.058 at the best cell,
on a quantity that would need +0.442.

**So the primitives are reliable-but-ambiguous, which is the regime decision 111
named as the condition for search being worth building.** 111 refused search
because *"you cannot search your way out of noisy primitives, because the
verifier is built from the primitives."* At out-degree 1 the primitive is 1.000,
so a verifier built from it is trustworthy. **That refusal has expired.**

John approved the direction in advance: *"any functionality and/or adjustments
that get us closer to our goals ... as long as it doesn't contradict with
those."*

### Two things that are not explained

**P4 refuted, backwards.** `hop2-concat` gains MORE from width than `hop1-pair`
— +0.051 against +0.021 — from a far lower base, and still lands below its own
floor. Composition benefiting from width where the primitive does not is the
opposite of the compounding story and nothing here accounts for it.

**`hop2-concat` is below the floor that matters.** 0.327 at width 256 against a
first-relation floor of 0.466. It clears the majority floor (0.118) and loses to
"retrieve the queried subject's own relation and guess". Decision 102 recorded
concat *matching* the one-hop model; on this instrument it is worse than the
one-hop shortcut.

### The instrument, and why it did not exist before

**Decisions 99 through 119 — the entire live relational line — were measured by
inline probes that left no script behind.** `kinship` appeared only in its task
module and its unit test; no experiment script existed. That is why a churn probe
from the same line returned on 2026-07-28 with no condition string, no seed count
and no pre-registered prediction, and had to be set aside under rule 11b.

`experiments/g13_01_does_width_fix_fidelity.py` is the first committed instrument
on this line. It measures **through `model.run()` only** and splits by out-degree
using `sequence.facts`, deliberately building no retrieval probe: reimplementing
one is the mistake `run()`'s own docstring records, where the 150/300 cap values
came from a reimplementation whose store never bound.

### A vacuous confirmation in my own summariser, caught after the run

The first version took floors from `records[0]` regardless of arm. The arms run
at different depths, and at hops=1 `shortcut_floors["first"]` is **1.000 by
construction** — the path is a single relation, so guessing from the first
relation is the answer.

P5 was therefore scored as "does not clear 1.000", which nothing can clear, and
printed CONFIRMED. **A check that passes because it cannot fail** — the class R4
exists for, appearing in a summariser rather than a test. Fixed to per-arm
floors; the corrected comparison is 0.327 against 0.466.

### What this licenses

- **Building search.** Its blocking condition is measured gone.
- **Retiring width as the critical path**, and with it the reading of decision
  112 that STATE.md carried for one session.

### What it does NOT license

Treating 0.915 as refuted. It is a correct measurement of raw retrieval; it is
simply not a statement about what the model scores. And this is one task, one
depth, one key scheme — `chains.py` was not re-run here.

**Taken without asking**, under standing authorisation and John's advance
approval of the direction: building the instrument, dispatching at 8 seeds
rather than 3, and recording P1 as refuted rather than restating it once the
smoke seed contradicted it.

---

## 122. Step 2 reproduces at 0.971, the traversal ceiling is 1.000, and the build is justified

g13-02, 24 cells, 8 seeds, run 30391374763. **Five of five predictions
confirmed**, one run after g13-01 refuted three of five.

    step 2, key(S, R) -> O
      d64   overall 0.971 +/-0.003   unique 0.997 +/-0.001   shared 0.478 +/-0.043
      d128  overall 0.972 +/-0.003   unique 1.000 +/-0.000   shared 0.456 +/-0.063
      d256  overall 0.975 +/-0.003   unique 1.000 +/-0.000   shared 0.508 +/-0.047

**Decision 107's 0.960 reproduces**, off by 0.011 — and it is the first number on
the relational line ever reproduced from a committed script rather than from an
inline probe.

### The ceiling that decides the build

    step 1 at out-degree 1   1.000   (g13-01, 8 seeds, +/-0.000)
    step 2 at a unique pair  1.000   (here)
    step 3 at out-degree 1   1.000   (same operation as step 1)
                             -----
    traversal with search    1.000   against decision 107's hand-derived 0.87

**Build it.** 107 declined the traversal because a perfect one bought 0.05 over a
broken one; that was true when steps 1 and 3 were 0.710 and 0.677. Search's whole
job is to put those two steps into the out-degree-1 regime, and there they are
1.000.

### The asymmetry is the reason it works, and it is worth stating plainly

    step 2's ambiguity     81/1600 sequences    5.1%
    step 1's out-degree>=2  ~800/1600           50%

A `(subject, relation)` pair names one person almost always; `(FACT, subject)`
names one of several relations half the time. **So the traversal's weak steps are
its two ends and its middle is sound** — which is exactly the shape that makes a
verifier built out of step 2 trustworthy, and it is the condition decision 111
named as missing.

Where step 2 *is* ambiguous it sits at 0.481 against a 1/2 bound: decision 108's
ambiguity again, on a third mechanism, tracking 1/m every time.

### What the ceiling is NOT

**It is conditional on the mechanism working.** Steps 1 and 3 are held at their
out-degree-1 value because putting them there is what search is for. If search
does not reach that regime the ceiling is not reached either. This is an upper
bound given a working mechanism, not a prediction of what the mechanism scores.

Composition on top of clean retrievals is a **separate, unmeasured factor**.
Decision 102 put it at 1.000 over the whole rule table, but on a different
configuration, and it is an assumption until re-run.

### A near-miss in how the checks were being run, and the structural fix

The five pre-commit checks were being run as one compound shell command. **A
shell reports only the last statement's exit code**, so that line said nothing
about the first four. Run separately, `unittest` and `check_duplication` were
both FAILING while the combined command reported success.

Interleaved output made it worse: several checks print reassuring lines of their
own — `rails ok` appears twice, because a test shells out to the rails checker —
so the tail of a failing run looked exactly like a passing one.

The real defect it hid was small and legitimate: `load` copied between the two
new summarisers, where `tools/recovery.load` already existed. Both now import it.

`tools/check_all.py` runs every check as a separate subprocess, captures output
rather than interleaving it, prints the verdict **before** the failing output so
it cannot be buried, and exits non-zero if any check failed. Rule 18: prefer a
rule that makes the mistake structurally impossible over one that asks for more
care. Reading five exit codes correctly is care.

`--changed` is deliberately excluded from it — it edits source, and the mutation
harness takes the tree exclusively.

### And kinship has mutations for the first time

`openplexus/tasks/kinship.py` carried **no mutation at all** before this, the
same gap that left decisions 99–119 without a committed instrument. Two are added
and both are caught: `the-object-question-ends-on-the-wrong-pair` (querying a
pair nothing ever wrote, which would read as a weak store rather than a broken
question — the defect decision 100 measured at 0.020 against 0.713) and
`the-object-question-follows-a-distractor` (an easier question wearing the same
name, which would inflate the very ceiling this decision rests on).

### What this licenses

Building **traversal with search** as the next mechanism: enumerate candidate
relations for the queried subject, follow each through `key(S, R)`, and keep the
branch whose endpoint matches the object the question names.

### What it does NOT license

Assuming the ceiling is reached. And the wire cost is still unmeasured — a
search over `b` branches multiplies retrievals, and though that is bounded bytes
per hop with no barrier (so amended C1 is satisfied), the traffic multiplier is
real and should be costed before the mechanism is called affordable.

**Taken without asking**, under standing authorisation and John's advance
approval of the direction.

---

## 123. Search is built and proved on its own, and the wire cost says it is affordable

`openplexus/search.py`. The capability decision 108 named as missing — *"multi-hop
reasoning over a BRANCHING graph requires SEARCH ... nothing in this architecture
searches"* — now exists as a tested unit.

### What it does, and the one line that makes it search rather than retrieval

    1  read key(FACT, S)       -> a superposition of S's relations
    2  take the top `branches` of that decode as CANDIDATES
    3  for each candidate, walk the graph COMMITTING to it
    4  score each walk by how well its endpoint matches T's value vector
    5  return the walks, best first

**A branch commits to a hard token.** Everywhere else in this model a decode is
softened and blended — `hop_sharpness` exists for that, and decision 86 records
what a flattened decode cost. Search is the one place the opposite is right: the
point is to assert a candidate and find out. Hedging would reproduce the
superposition search exists to escape.

**And the score is the endpoint, not confidence.** Decision 93 measured every
identity-free confidence signal — norm, entropy, peak, gap, kurtosis — and the
best linear separator over all five, *fitted with the labels*, reached 0.628
against 0.500 for guessing. Matching the named target is a different kind of
signal: it asks whether the walk arrived, not whether it felt sure. That
distinction has its own mutation.

### The disambiguator was in the question the whole time

Decision 108: the store answers *"what relation does S hold"* correctly and the
question needs *"which of S's relations leads to T"*. **T is stated in the
question and nothing had ever used it.** That is the entire content of the fix.

The sharpest test says so directly: same store, same start, only the target
changed, and the other branch must win. If it does not, search is reading
something about the store rather than about the question.

### The wire cost, and it is the good news

`tools/search_cost.py`, arithmetic over the two constants in `distributed.py`
(5-byte broadcast, 8-byte reply):

    branches   decodes   bytes/position   x greedy   positions/s
           1         4           32,788       1.0x        39,062
           4        13          106,561       3.2x        12,019
          16        49          401,653      12.2x         3,189

    nodes 1024, depth 2, 10 Mbps uplink per node

**Beam 4 costs 3.2x the decode traffic and still supports ~12,000 answered
positions per second.** Depth is the harsher axis and only mildly — 3.2x at depth
2 rising to 3.7x at depth 5, because a walk costs `2d - 1` decodes.

So **bandwidth is not what binds search.** That was the open worry and it is
answered before the mechanism was wired in rather than after.

**What the cost tool does NOT say.** The pooled decode is a collective, and note
009 §4 has carried that as an outstanding C1 item since long before search. Search
does not create it — the readout already requires it — but it makes it
`b(2d-1)/d` times more frequent, which raises the stakes on the un-constraint
STATE.md lists as costing a reading rather than a run. And none of it is measured:
the model has never run a search on more than one machine.

### Why it is NOT wired into `run()` yet, and why that is said out loud

`run()` is 526 lines with 46 branch points and there is a standing item about
exactly that. The mechanism is built and proved on its own first, and wiring is
its own change.

**Labelled as unfinished rather than left to look finished.** CLAUDE.md: scaffolding
that is not named as scaffolding becomes load-bearing.

### The test that carries the claim

`TheAmbiguousCaseIsWhereSearchEarnsItsPlace` builds a store where the
superposition's own argmax is the WRONG relation — FRIEND written at twice the
weight of PARENT, and only PARENT reaching the target. So a greedy traversal is
not merely uncertain, it is **confidently wrong**, and there is a control asserting
exactly that so the rest cannot pass vacuously.

The store is written by hand with the model's own rule rather than trained,
because the point is control over the AMBIGUITY, which a trained store would
supply only by luck. `test_decay_when_masked` is the precedent and the reason is
the same: no black-box comparison can see the property under test.

### Mutations

`openplexus/search.py` gets two, both caught:
`search-scores-by-confidence-not-by-the-target` (which would make search greedy
wearing a beam) and `search-keeps-the-worst-branch` (the direction, which gets its
own mutation because decision 87 records this project shipping a gate whose sign
was wrong and beating the baseline anyway).

### What this licenses

Wiring it into `run()` and measuring it end-to-end on kinship against the 1.000
ceiling g13-02 established.

### What it does NOT license

Any claim that search works on the task. **It has been proved on a hand-built
store of four facts and has never seen a generated sequence.** The unit test says
the mechanism is correct; whether it survives a real store with distractors,
decay and a cap is exactly what the next measurement is for.

**Taken without asking**, under standing authorisation and John's advance approval
of the direction.

---

## 124. The objective is the thesis, the sum was never the C1 problem, and the driver has no failure detector

John raised five things on 2026-07-28. Three changed a document, one closed an
open decision, and one found the largest gap between this code and its goal.

### The objective is the thesis, and GOALS contradicted it

In his words: *"Most models currently just train on next-token prediction, and
therefore at the end of the day they're taught to predict text. My idea here is,
instead of focusing on predicting text, train the model to understand the
relationships between things: to associate a given thing in the context of all
other things."*

**GOALS §5 recorded the opposite.** Its credit-assignment candidate is
self-supervised *temporal prediction* — each unit predicts its own next input —
and one of the three stated advantages is *"it is the same objective family as an
LLM."* That was written as a recommendation and is now the objection.

Recorded as GOALS §1.2, with §5 marked as superseded in part rather than
rewritten. **The delivery argument survives** — no signal in transit means
latency costs memory rather than credit precision, which is note 002's real
contribution. What does not survive is next-input prediction as the thing being
learned.

This is the "old assumption still being acted on" failure John is worried about,
found in the founding document, and it was pointing the whole project at the axis
decisions 63 and 115 already measured as closed.

### Scale is now a stated goal-level concern

Also his: do not optimise for a benchmark at this scale unless the result
transfers to the target scale; when a decision IS scale-specific say so where it
is made, with the trigger to revisit; give those choices a seam.

Recorded as GOALS §1.3. **`docs/SCALE.md` already existed and already does this**
— one row per scale-dependent choice, with what was chosen, at what size, the
trigger, and what to try instead. It is now pointed at from the goals rather than
being a file nobody was sent to.

### `reward_recall` is retired, not fixed

John: *"if it's just a failure in a test (not the model itself), and the test is
no longer useful, definitely just abandon it."* The leak is real and measured
inert; the task is not fixed, not re-baselined, and retired as an instrument.
Decision 119 had already shown it does not discriminate what the g9 line measured
on it.

### The sum was never the C1 problem

STATE.md and note 009 §4 carried `answer = parts.sum(0)` as an outstanding C1
violation. **It is the numpy reference's convenience.** The deployed path sends
each node's argmax in 8 bytes, and `distributed.py` already says why that differs
in kind: *"Absence costs a voter, not a term of a sum, which is why this degrades
where summing amputates."*

Bounded bytes per hop, and a missing node degrades the vote. **Amended C1 is
satisfied by the wire format**, and the item is retired.

### ⚠ But the driver settles a step only on a FULL COUNT, and that is a barrier

    distributed.py:427
    while settled < sent and pending[settled][1] >= expected[settled]:

A **declared** departure works — `absent` and `leave_at` adjust `expected`, which
is what g12-02 measured across 18 cells with no hang. An **undeclared** one does
not: the step never reaches its count, the window fills, the driver stops
sending, and 30 seconds later `select` raises `TimeoutError`.

**That is exactly what amended C1 forbids** — a barrier that stalls when a
participant is slow or gone — and C3 says departure arrives without warning.

**The design exists and is not implemented.** Note 003 specified a separate
liveness channel, because on a sparse substrate silence is normal and absence of
data cannot signal absence of a machine, and it unified `d_max` as both the C2
bound and the C3 timeout. The driver needs to settle on a **quorum plus a
deadline** rather than on a full count.

**Every churn result in this project was measured with departures announced in
advance.** That is not wrong — g12-02 is a real measurement of what it measured —
but it is not the failure C3 describes, and the gap was hidden behind a claim
about summing that turned out to be about something else.

### And a correction of my own, which John caught

I wrote in STATE.md that *"the Docker testbed is not in CI, no workflow runs
it"*, carried from the archived backlog without checking. **Three sweeps run it
on Actions in real containers** — g12-01, g12-02 and g12-03 — plus
`testbed-identity.yml`. The model HAS run distributed across containers.

What has not is the **relational** work: kinship, hops and search are
single-process only, and `Node.step` cannot run a gated model at all. The precise
claim is much narrower than the one I made, and the imprecise version was the
kind of stale inheritance the 2026-07-28 restructure was supposed to stop.

### The self-imposed limits still standing

Audited against John's test — the only real constraints are that it runs across
devices over the internet and that the model is as capable as possible. Recorded
as STATE.md item 10. The one worth naming here: **`hop_accumulate="concat"` is
refused alongside `hidden`**, so the best readout (decision 116, 0.45 bits) and
the composition mechanism cannot be used together, on the grounds that "the two
have not been made to compose" — a to-do wearing a constraint's clothes.

### What this licenses

Building the liveness path, and treating the objective as the next major work
rather than as item 4.

### What it does NOT license

Assuming the driver fix is small. Settling on a quorum changes what a run means:
answers become dependent on who replied in time, so bit-identity — the property
G2 was passed on — cannot survive it unchanged. That needs its own plan and its
own predictions.

---

## 125. Traversal is the win. Search helps only where ambiguity is, and hurts where it is not

g13-03, 32 cells, 8 seeds, run 30394574459.

    arm       overall            out-degree 1       out-degree >= 2
    concat    0.327 +/-0.014     0.348 +/-0.018     0.297 +/-0.017
    walk      0.596 +/-0.018     0.702 +/-0.025     0.446 +/-0.010   CLEARS FLOOR
    search4   0.604 +/-0.013     0.649 +/-0.021     0.539 +/-0.024   CLEARS FLOOR
    search8   0.580 +/-0.014     0.619 +/-0.023     0.525 +/-0.024   CLEARS FLOOR

    walk - concat      +0.269 +/-0.024
    search4 - walk     +0.008 +/-0.018      <- inside 2 SE

### Decision 107's verdict does not survive the primitives moving

It declined the pair-key traversal because *"a perfect traversal buys 0.05"*,
computed when steps 1 and 3 were 0.710 and 0.677. **They are 1.000 at out-degree
1 now, and traversal is worth +0.269** — five times what it was costed at, and it
is the whole of the gain here.

**It also clears a floor nothing on this task had ever cleared.** `concat` sat at
0.327 against a first-relation floor of 0.466 — worse than guessing from the
queried subject's own relation. `walk` reaches 0.596 and `search4` 0.604.

This is the second time in three decisions that a refusal turned out to be
conditional on numbers that later moved, and both times the refusal was correct
when made. The pattern worth keeping: **a ceiling measured before building is
only valid while its inputs are.** Neither 107 nor 111 was wrong; both were
answers to a question whose terms changed.

### Search is a wash overall, and the out-degree split says exactly why

    search4 - walk at out-degree 1       -0.054
    search4 - walk at out-degree >= 2    +0.092

**Search does precisely what it was built to do and damages the case it was not
built for.** At out-degree 1 there is one relation and nothing to choose between,
so searching can only replace a correct greedy pick with a branch whose endpoint
scored higher by luck. At out-degree >= 2 it is worth +0.092, the largest single
gain anything has produced on the ambiguous case — which is the case decision 108
named as the blocker.

The test set is about half of each, so they cancel. **The tie is therefore not
evidence against search; it is evidence that search should not run
unconditionally.**

P2 was scored CONFIRMED by sign and is reported as a tie, because +0.008 +/-0.018
is inside 2 SE and calling that a win would be exactly the kind of thing this
project's own paired-scoring rule exists to prevent.

### More branches actively hurt, which was predicted the other way

P4 predicted `search8` within 0.02 of `search4`. It is **0.024 worse, at 6 SE** —
refuted, and in a direction worth understanding: every extra candidate is another
chance for a wrong branch whose endpoint happens to score well. The verifier is
good and not perfect, and widening the beam samples its errors more often.

That is a real constraint on the mechanism and it argues against "just search
wider" as a way to close the remaining gap.

### What to build next, and the warning attached to it

**A gate on search**: run the walk greedily, and only branch where the first
decode is ambiguous. The split above says that would keep +0.092 and give back
the −0.054.

The open question is whether ambiguity is detectable from the decode itself — the
gap between its top two candidates — as opposed to from the out-degree, which is
task structure a running system cannot read. **Decision 93 is the warning**: every
identity-free confidence signal it measured reached 0.628 against 0.500 for
guessing. The quantity here is different, but the precedent says measure it
before building on it, which is what saved three builds already.

### What this does NOT license

Quoting 0.604 as the approach's ceiling. Every arm runs the walk unconditionally
at a fixed depth of 2, on one task, at one width. And g13-02's 1.000 remains the
retrieval-chain ceiling; the gap to 0.604 is how often the walk fails to reach
the out-degree-1 regime, which nothing here has decomposed.

**Taken without asking**, under standing authorisation and John's advance
approval of the direction.

---

## 126. SWIM and the CRDT literature, read at last — and the detector I built ejects nodes permanently

GOALS §6.2 has listed gossip protocols, SWIM-style failure detectors and CRDTs as
**unread** since the project began, and note 003 named them the highest-value
gap. Read now — **after** building the detector, which is the wrong order and the
third time this project has done it (note 010, note 020, this).

**Both sources are summaries, not the papers.** Shapiro et al. (2011) and Das,
Gupta & Motivala (2002) remain unread; the SWIM PDF fetch returned unparseable
binary. Rule 1 applies to everything in notes 039 and 040.

### What SWIM says about what I built — note 039

| SWIM | decision 126's deadline |
|---|---|
| a dedicated probe channel | **liveness inferred from a missing vote**, which note 003 said cannot work |
| indirect probing via `k` peers | none; a slow node and a gone node are indistinguishable |
| suspect, then confirm, with recovery | **none — permanent ejection** |
| detection distributed | the driver is the sole detector, a coordinator by another name |

**The permanent ejection is a wrong behaviour, not a missing feature.** A machine
whose network blipped for a single send was removed for the rest of the sequence
with no path back, and its share of the store stayed dark. **Fixed**:
`unreachable` is now `suspect`, a node → step map, retried every
`RETRY_AFTER_STEPS`; a successful send clears the mark, because a reset peer
cannot accept one.

**The deadline is not refuted.** SWIM decides *who is alive*; a deadline decides
*when an answer is due*. They are complementary, and note 003's `d_max`
unification is better supported than before — SWIM separates probe period from
suspicion timeout, which is the same shape.

### What the CRDT literature says about reconciling node sets — note 040

The delta rule updates the readout **additively**, and addition is commutative
and associative, so the updates satisfy the operation-based (CmRDT) requirements
exactly. **Reconciliation is possible in principle.**

They are **not idempotent**, so this inherits the CmRDT obligation:
**exactly-once delivery** over an unreliable network. Duplicate a delta and a
weight is silently wrong; drop one and learning is silently lost.

**And the obvious approach is the one the literature names as broken.**
Averaging is explicitly not safely mergeable — and averaging model weights is
exactly what federated averaging does. It works there only because of a central
aggregator and synchronous rounds, which note 003 already found to be a C1
violation twice over. **Do not reconcile node sets by averaging their readouts.**

The idempotent state-based version exists — G-Counter's per-node slots — and
costs `P` copies of the parameters, ~375 GB for a ~6 MB model at 62,500 nodes.
The usable middle is per-**set** slots rather than per-node, of which there are
as many as there are concurrent conversations.

> **One assumption underneath all of it, and it is a goals question.** Under C4 a
> node set that has seen different conversations legitimately knows different
> things. Forcing convergence may discard exactly the specialisation that makes a
> distributed system worth having. Not answered here.

### A surviving mutation found a real duplication

`strict-mode-tolerates-a-dead-node` stopped biting after the suspicion change.
Rule 10 says strengthen the test rather than delete the mutation — and following
it found the cause was neither: **two guards implemented the same rule at two
sites**, so removing either left the other to raise a line later and no test
could tell them apart.

Collapsed to **one** guard, in `dispatch`, where the strict-versus-tolerant
decision belongs. The RESET loop now records the failure and decides nothing.
Both mutations bite again.

### What this licenses

Adding indirect probing and a real liveness channel, and designing set-level
reconciliation around additive deltas rather than averaging.

### What it does NOT license

Quoting either note as established. Both rest on encyclopaedia summaries, and
note 005 exists because a borrowed claim that gated a design decision turned out
to describe a variant this project cannot use.

---

## 127. The SWIM paper was never unreadable, and it describes our bug in its own words

Note 039 was published against a Wikipedia summary with a rule-1 caveat: *"the
paper is still unread — the PDF fetch returned unparseable binary."*

**It was not unparseable.** The PDF was fine and had been on disk the whole time.
The console here is cp1252, a single `fi` ligature aborted the extraction, and an
**encoding error looked like a bad download**. Ten pages of primary source were
behind a two-line fix: write to a UTF-8 file instead of printing.

`tools/pdf_text.py` exists so the next source is not written off the same way.
Its docstring carries the lesson rather than the incident: **a tool that dies on
the first character it cannot print will make a readable source look
unavailable.**

### The paper describes decision 126's bug in its own words

§4.2, on why the Suspicion subprotocol exists:

> a perfectly healthy process suffers a very heavy penalty, by being forced to
> drop out of the group at the very first instance that it is mistakenly
> detected as failed

That is exactly what `unreachable` did before decision 126 — arrived at
independently and for the same reason, one missed send treated as proof. The
paper's causes are ours: packet loss, a sleeping process, or a **slow** one.

### The parameters, which are measured rather than chosen

The part a summary cannot give, and the part that bears on our code:

    two parameters only        protocol period T', subgroup size k
    no synchronised clocks     properties hold if T' is the AVERAGE period
    indirect-probe timeout     an estimate of the ROUND-TRIP DISTRIBUTION --
                               the average, or the 99th percentile
    T' >= 3 x RTT              a concrete ratio; we have nothing like it
    packets <= 135 bytes       REGARDLESS OF GROUP SIZE

**No synchronised clocks matters here specifically**, because a network of
strangers' machines has no common clock and several designs quietly assume one.

**The packet bound is amended C1's requirement, achieved.** Bounded bytes per hop
independent of participant count — and SWIM gets it by separating detection from
dissemination, not by sending less often. That is an existence proof that the
property is reachable rather than a trade-off to be haggled over.

### So the next item on this line changes

`RETRY_AFTER_STEPS = 8` is **a guess in the wrong unit**. SWIM's periods are time
derived from measured round-trip times; ours counts steps, and a step has no
fixed duration.

**Measuring RTT on the testbed is now the concrete next item**, ahead of adding
indirect probing — and it is the same measurement note 003's `d_max` has always
needed and never had. A detector tuned in steps cannot state a bound, and C2 is
the constraint that requires one.

### The other half is still unread, and the asymmetry is the point

Shapiro et al. (2011) could not be fetched: HAL is behind Anubis, `lip6.fr`
refuses connections, Semantic Scholar returned an empty body. **Note 040 keeps
its rule-1 caveat**, so the pair is now one note on a paper and one note on an
encyclopaedia entry, and they should not be quoted with equal confidence.

Given what reading the SWIM paper changed, that gap is worth closing rather than
noting. Someone with the PDF can drop it anywhere on disk and
`tools/pdf_text.py` will do the rest.

### What this licenses

Nothing new to build. It sharpens what to measure and it upgrades note 039 from
summary to source.

### What it does NOT license

Treating note 040 as settled. And a caution that generalises past this entry: the
first version of note 039 was **wrong about why it was limited**, which is worse
than being limited, because a caveat that names the wrong obstacle stops anyone
trying the thing that would have worked.

---

## 128. d_max is ~640 ms, measured — and the number I published last cycle was measuring Windows

g12-04, 6 of 6 cells, run 30399620805. Every cell agreed with the single-process
model, so the timings are interpretable. **Three of six predictions refuted.**

    cell                              mean      p50      p99      max   3 x p99
    clean                             0.75     0.61     2.54     2.64       7.6
    loss 2%                           0.82     0.76     2.32     2.65       7.0
    delay 20ms                       20.55    20.45    21.10    21.15      63.3
    delay 80ms                       80.55    80.51    81.53    81.58     244.6
    delay 80ms jitter 20ms           82.74    84.40   100.38   100.42     301.1
    delay 80ms jitter 20ms loss 2%   95.25    87.22   211.88   265.20     635.6

### The number the project has never had

**`d_max` = ~640 ms**, on an 80 ms link with 20 ms jitter and 2% loss.
Simultaneously the C2 asynchrony bound and the C3 churn timeout — note 003's
"two constraints, one parameter" — and the first time either has been a number
instead of a count of steps.

**`RETRY_AFTER_STEPS = 8` means two things three orders of magnitude apart.** At
40 steps in 0.014 s on the clean link, eight steps is under 3 ms; on the worst
link the same eight steps span seconds. C2 requires a *stated* bound and a
constant in steps cannot be one.

### The correction, and it is mine from one cycle ago

Decision 127 quoted an in-process measurement — p50 0.38 ms, p99 24.19 ms — and
argued from it that "the tail runs 64x the median on the easiest link available".
It was the evidence for building percentile reporting at all.

**The clean container link's p99 is 2.54 ms. The in-process figure was ten times
larger, and it was measuring this development machine's process scheduler** —
four Python processes on Windows, 120 votes. The containers give 160 votes at a
fortieth of the tail.

So *"64x the median on the easiest link available"* is **withdrawn**. The easiest
link is 4x, and the 64x was Windows. The percentiles were still worth building,
for a different reason — see P4 and P5 — but the argument that justified them was
measuring the wrong machine.

### P1 refuted, and the right statistic is the gap not the ratio

p99 exceeds the mean by 3.4x on the clean link and by **1.01x** at delay 80 ms.
**Once a fixed delay dominates, mean and p99 converge**, because a constant added
to every sample moves both equally.

The ratio measures variance relative to total, and a timeout does not care about
that. What it must cover is the absolute gap `p99 - p50`: **1.0 ms** at delay 80,
**16.0 ms** with jitter, **124.7 ms** with loss on top. Quote the gap.

### Loss is multiplicative with delay, not additive

P5 predicted 2% loss would move p99 far more than p50. **Alone it is invisible**
— p99 went *down* 0.22 ms, which is noise. But adding the same 2% to the
delay-80-jitter-20 link takes p99 from 100.4 ms to **211.9 ms**.

**A retransmit costs a round trip, so the price of losing a packet is
proportional to how long a round trip takes.** On a fast link that is nothing; on
a slow one it doubles the tail. The prediction had the mechanism right and tested
it in the one place it could not show.

### Two defects in the sweep's own plumbing, neither touching the data

**`testbed/run.py` printed progress to stdout** while the workflow piped stdout
into a `.json` file, so all six artifacts had prose above the JSON and could not
be parsed. A program whose stdout is a data format has no business printing
anything else there. Progress now goes to stderr.

**`summarise_g12_04.py` shipped with a syntax error and every local check
passed**, because nothing imports a summariser at check time — the suite does not
touch them, the mutation harness does not target them, and the rails read them
only as text. It surfaced in CI *after* the matrix had run.

A summariser is the one piece of code that runs exactly once, at the end, when
the expensive thing has already happened. **R5 in `check_rails.py` now compiles
every file under `tools/` and `experiments/`**, verified to bite by breaking one
deliberately. Low bar on purpose: it does not check a summariser is right, only
that it can start, which is exactly what failed.

### What this licenses

Replacing `RETRY_AFTER_STEPS` with a duration, and stating C2's bound.

### What it does NOT license

Treating 640 ms as universal. It is a floor from these six links; intercontinental
paths, mobile networks and congested uplinks are all outside the grid, and a worse
link raises it.

---

## 129. Ambiguity IS detectable before searching — and the expensive signal is below chance

g13-04, 24 cells, 8 seeds, run 30401924214. Three of five confirmed.

    decode margin      d64  AUC 0.710    d128  0.841    d256  0.858
    endpoint margin    d64  AUC 0.480    d128  0.447    d256  0.448

### The cheap signal works, at width 128 and above

Decode margin AUC **0.803** overall, against a 0.75 bar and decision 93's
**0.628** — the best any identity-free confidence signal reached there when
*fitted with the labels*.

The distinction that predicted this holds up: 93's signals ask *"does this
retrieval feel reliable"*, and the margin asks something structural — one
relation bound to a key gives a peaked decode, several give a contested one. It
reads the superposition rather than guessing at it.

### The expensive signal is ANTI-CORRELATED, which was not predicted

P3 asked whether the endpoint margin — the gap between the best and second-best
walk — beats the decode margin by more than 0.05 AUC. It comes in **0.345 below
it, and below chance in absolute terms** at every width.

**Out-degree 2+ shows a WIDER endpoint margin than out-degree 1** (0.743 against
0.606 at d256), the opposite of the intuition that motivated recording it. With
one true relation the junk branches apparently reach endpoints scoring comparably
to the real one; with several, the real branches separate from each other.

So a gate must decide **before** walking rather than after — which is also the
cheap direction. Both arguments now point the same way, and the one that would
have cost the walks is the one that does not work.

### It is a WIDTH-DEPENDENT mechanism, and that is the caveat

P4 refuted: **d64 reaches only 0.710**, below the bar, while d128 and d256 reach
0.841 and 0.858. The signal strengthens monotonically with width, and the medians
say why — the out-degree-2+ median *falls* (0.235 → 0.147 → 0.118) while the
out-degree-1 median *rises* (0.538 → 0.650 → 0.769).

**A wider store holds a cleaner superposition**, so a peaked decode gets more
peaked and a contested one more contested. That is a real mechanism rather than
noise, and it means a gate built on this belongs in `docs/SCALE.md` as
width-dependent. At 256, where relational work now runs, it is sound; at 64 it
would be weak.

### P2 refuted on an outlier, and scored as written

Mean ratios are 2.3×, 4.4× and 6.5× — comfortably past the prediction. The
smallest ratio in any single cell of 24 is 1.5×, and the prediction was phrased
over cells rather than over means. Recorded as refuted because that is what it
says; the substance holds.

### What this licenses

Building the gate: walk greedily, branch only where the decode margin is narrow.
g13-03's split puts a perfect gate at roughly **+0.03 over search-everywhere**,
plus the walks saved where they cannot help.

### What it does NOT

**Say where the threshold goes.** AUC measures separability across all
thresholds; a gate needs one, and choosing it on the test set would be fitting a
number rather than measuring one. That is the first thing the gate experiment has
to handle honestly — a held-out split, or a threshold derived from the decode's
own scale rather than tuned.

And **the number to beat is search4's overall, not walk's.** A gate that merely
matches search-everywhere has bought compute savings and no accuracy, which is
worth having and is not what this was for.

**Taken without asking**, under standing authorisation and John's advance
approval of the direction.

---

## 130. The gate pays: +0.020 over search-everywhere, and the search line closes

g13-05, 40 cells, 8 seeds, run 30403497440. **Five of five predictions
confirmed**, including the rail.

    arm        overall            k=1                k>=2               fired
    walk       0.596 +/-0.018     0.702 +/-0.025     0.446 +/-0.010      --
    search4    0.604 +/-0.013     0.649 +/-0.021     0.539 +/-0.024      --
    gate-q50   0.624 +/-0.014     0.684 +/-0.024     0.539 +/-0.025     48%

    gate-q50 - search4   +0.020 +/-0.005      gate-q50 - walk   +0.028

### It does what the design argument said, which is new on this line

    out-degree 1     gate 0.684   walk 0.702   search4 0.649
    out-degree >= 2  gate 0.539   walk 0.446   search4 0.539

**At out-degree ≥ 2 the gate matches `search4` exactly** — all of search's gain
kept where ambiguity lives. **At out-degree 1 it recovers most of the way back to
`walk`**, giving back most of the damage search does where there is nothing to
choose between.

That is exactly the trade g13-03's split said was available, and +0.020 against a
perfect-gate ceiling of 0.03 is about **two thirds of it** — which is what AUC
0.803 rather than 1.000 buys.

### The line, end to end

    concat      0.327    what we had -- BELOW the 0.466 shortcut floor
    walk        0.596    pair-key traversal, which decision 107 declined
    search4     0.604    search everywhere, which decision 111 declined
    gate-q50    0.624    search where it helps

**Both refusals were correct arithmetic on the numbers of their day**, and both
conditions were measured away before either was rebuilt. Nothing had to be
undone, because 107 and 111 declined to *build* — which is precisely what
measuring a ceiling before building buys, and it has now paid three times on this
line.

### The threshold generalises; the number does not

`gate-q50` fires at a margin of 0.663 at width 256. **That constant is not the
mechanism** — it is the median training margin, and the margin distribution moves
with width and key scheme. What transfers is "the median of this model's own
training margins", computed with no labels and without touching the test set.

Registered as width-dependent: g13-04 measured the signal at AUC 0.710 at width
64, below the 0.75 usability bar. **This is a width-256 result.**

### What this does NOT license

Quoting 0.624 as the approach's ceiling. Fixed depth 2, one task, one width — and
g13-02's retrieval-chain ceiling is **1.000**. The gap between 0.624 and that is
unaccounted for, and nothing here decomposes it. Composition on top of clean
retrievals is still inherited from decision 102 rather than re-measured, which is
the most likely place for it to hide.

**Taken without asking**, under standing authorisation and John's advance
approval of the direction.

---

## 131. The persistence test did not test persistence — the store was full before it started

g15-01, 54 cells, 3 seeds, run 30408859908. **The headline is that P3's
"CONFIRMED" is an artefact and must not be quoted.**

    arm                4,000       8,000      16,000      32,000      62,500     125,000
    baseline          5.5989      5.5709      5.5353      5.5327      5.5255      5.5261
    consolidate       5.6349      5.6142      5.5694      5.5749      5.5647      5.5610
    persist           5.7733      5.7562      5.7838      5.7338      5.8448      5.7101
    decision 63       5.5700      5.5430      5.5270      5.5230      5.5310      5.5310

**`persist` is worse than `baseline` at every single data point**, by 0.18 to
0.32 bits.

### The diagnosis, and it is the only reason this sweep is worth anything

**The slow store's norm is 5.00 at every data size, including 4,000
characters.** `lasting_cap` is 5.0 and `scale_to` rescales the whole store to it.

So the persistent store **saturates before the smallest data point and stays
saturated**. It is a fixed-size bucket, full from the start, and every later
write rescales what is already in it. That also explains why it is *worse* than
no store: it adds a constant-norm blob of increasingly-overwritten material to
every read.

**This experiment tested a saturated store, not an accumulating one.** Note 042's
claim is that a map needs somewhere to accumulate. Nothing accumulated.

### What the registered rail bought

P4 was written to distinguish "persistence is wrong" from "the gate never
opened", because consolidation fires on `predictions[t-1] == token` and promotes
only what the model already got right.

**The gate opened**: 16,470 consolidations at 4,000 characters rising to 51,713
at 125,000. So the null cannot be blamed on an empty store — which eliminates the
risk that was registered in advance and points the next experiment at the cap
instead.

### Three of five predictions were badly specified, all the same way

- **P1 and P2** asked for total movement from 4,000 to 125,000 under 0.05 bits.
  That measures *across* the wall and counts the pre-wall improvement decision 63
  never disputed. Split at the wall, the control reproduces exactly: **0.064 bits
  before 16,000 and 0.009 after.** The control is sound; the test of it was not.
- **P3** asked whether `persist` improves from 62,500 to 125,000 by more than the
  seed spread. It does — because 62,500 was a high outlier in a row with no trend
  at all. **A one-pair difference cannot detect a trend**, which is what noise
  satisfies.

A statistic chosen for convenience rather than for what it would detect.
**Registering a bad test in advance is not the same as registering a good one**,
and pre-registration protects against moving the goalposts, not against picking
the wrong measure.

### What this settles

- **`lasting_cap` is the binding constraint on persistence.** Not the gate.
- **Note 042's item 1 is UNTESTED, not refuted**, and item 2 must not be built on
  it until it is.

### What comes next

Sweep `lasting_cap` with persistence on — 5, 50, 500, unbounded — reading the
norm beside the bits. The cap exists because a salience gate without one
diverges, so unbounded may be unusable; the question is whether any setting lets
the store grow with the corpus instead of saturating at the first data point.

**Taken without asking**, under standing authorisation. John approved items 1 and
2; this reports that item 1's test has to be re-run before item 2 starts.

---

## 133. Persistence moves the LEVEL, not the SLOPE — a decaying store is a cache, not a map

g15-01, third pass, 15 of 15 jobs, run 30409788113. **Note 042's item 1 is
refuted in its simple form, and what replaces it points straight at item 2.**

    arm                4,000    8,000   16,000   32,000   62,500  125,000
    baseline          5.5989   5.5709   5.5353   5.5327   5.5255   5.5261
    persist-slow      8.2720   9.3943   9.9479  11.1714   7.1027      nan
    persist-slow-decay 5.5250  5.4823   5.4551   5.4536   5.4393   5.4427

    slow-store norm
    persist-slow       27.2     40.1     58.5    139.3    131.7     52.2   diverges
    persist-slow-decay  0.4      0.4      0.4      0.4      0.4      0.4   equilibrium

### The good news first, because it is real

**`persist-slow-decay` beats the baseline at every single data point**, by 0.074
to 0.083 bits. That is a larger and more consistent gain than most mechanisms in
this project have produced on text, and it is the arm's own control that makes it
readable: `consolidate` (same consolidation, no persistence) is *worse* than
baseline everywhere, so **the gain is persistence and not consolidation.**

### And the refutation, which is the finding

**P3 REFUTED.** Movement past the wall is **+0.0124**, under the 0.04 seed
spread, and not monotone. With the store finally working — accumulating, not
saturated, gate firing 16,470 to 51,713 times — the wall does not move.

That is decision 69 arriving on a new mechanism: *everything found so far moves
the LEVEL, nothing moves the SLOPE.* Note 042 predicted persistence would be
different. It is not.

### Why, and this is the part worth keeping

**The store's norm is 0.4 at every corpus size.** Decay balances writes at a
fixed point, so the store reaches equilibrium almost immediately and stays there
whether it has seen 4,000 characters or 125,000.

**A decaying persistent store is a fixed-size cache holding a moving window, not
a map that grows.** And the alternative is worse: `persist-slow` without decay
grows to 139 and then diverges into NaN, scoring 8–11 bits throughout.

So the two options are a store that forgets at a fixed size, or one that
explodes. **Persistence alone adds no CAPACITY**, and a `d × d` matrix has fixed
capacity whatever its lifetime — decision 109 measured it at ~d².

### What this means for the architecture pass

Note 042 said the wall exists because there is nowhere to accumulate. **Wrong:
there is now somewhere, and the wall did not move.** The correct statement is
narrower and more useful:

> The wall is a CAPACITY limit, not a lifetime limit. Giving a fixed-size store
> a longer life does not give it more room.

**That is an argument for item 2 rather than against it.** Concept partitioning
is the only proposal on the page that adds capacity as the corpus grows — more
concepts live on more nodes — where persistence merely extends how long a fixed
amount of room is held.

It also re-reads decision 63 correctly: 16,000 characters is not where learning
stops, it is where **a d × d store plus a `vocab × d` readout runs out of
room.**

### What it does NOT license

Dropping persistence. It is worth 0.08 bits at every scale and it is the
prerequisite for anything that accumulates — a partition with no memory across
sequences is the same fixed-size problem spread across machines. `lasting_decay`
stays, and the settings that work are recorded at the config.

**Taken without asking**, under standing authorisation. John approved items 1 and
2; this reports that item 1 does what it can and item 2 is where the capacity
has to come from.

---

## 134. Pooled capacity is identical. Concept partitioning's case is INDEPENDENCE, not capacity

g16-01, 5 seeds, 50 cells, per-node memory held equal at ~4,096 numbers.

    arrangement nodes    pooled     ALONE   node sees
    concept     1           128       128   64 of 64 dims
    concept     16         2048      2048   64 of 64 dims
    dimension   1           128       128   64 of 64 dims
    dimension   2           256       141   45 of 91 dims
    dimension   4           512       128   32 of 128 dims
    dimension   8          1024       128   22 of 181 dims
    dimension   16         2048       128   16 of 256 dims

**Pooled capacity is the same at every node count.** 128, 256, 512, 1024, 2048
in both arrangements. **Lone-node capacity is not**: concept scales with the
network, dimension is flat at one node's worth from four nodes onward. At 16
nodes that is **2048 against 128, a factor of sixteen.**

### The finding, stated as the thing that is actually different

> Under **dimension** splitting, growing the network makes every node's view
> thinner while the total stays the same, so **a node can never answer alone
> however large the system gets.** Under **concept** splitting a node owns whole
> concepts, so its standalone capability grows with the network.

That is a capability difference rather than an engineering one, and it is the
only capability difference there is — capacity per unit of memory does not
distinguish them at all.

**And it is exactly what amended C1 cares about.** A read that requires every
node is the barrier the constraint forbids; "what can one node do" is the
question. g4-01 is what pointed here, having measured a lone node at 0.949 with
16 dimensions, 0.681 with 8 and 0.412 with 4.

### Two corrections to my own reasoning, both caught before the result

**Note 043's capacity argument was wrong.** It said concept partitioning is the
only proposal that adds capacity as the corpus grows. At equal per-node memory
both scale identically, which the arithmetic showed and this run confirms
exactly. Corrected in the note before the probe ran.

**And the first version of this probe measured the wrong quantity.** It reported
pooled capacity only — which is identical — so it would have concluded the two
arrangements are equivalent and there is nothing to build. The lone-node measure
was added after reading that output and noticing it could not tell them apart.

Both are the same failure in different places: **a plausible quantity chosen
before asking what would distinguish the hypotheses.** Decision 133 refuted note
042 for it, and note 043 and this probe each did it once more.

### Predictions

P1, P2, P4 confirmed. **P3 refuted and the truth is starker** — pooled capacity
never collapses, and lone-node capacity never *grows*: it is 128 at one node and
128 at sixteen. The floor is not a cliff at some node count; a lone node under
dimension splitting is simply stuck at one node's worth forever.

**P5 refuted**: concept capacity is exactly N times one store's with no
interference penalty, because the stores are independent. The prediction assumed
superposition across nodes; there is none, which is the point of the
arrangement.

### What this licenses

Building concept partitioning, on the independence argument rather than the
capacity one.

### What it does NOT

Any claim the model can LEARN through it. This measures what a data structure
holds — random keys, no decay, no cap, no task. **Decision 133 is the standing
reminder**: the last mechanism that looked obviously right moved the level and
not the slope.

And routing does not exist. Which node owns a key is consistent hashing, listed
unread in GOALS §6.2 since the project began, and note 043 records that the naive
`mod nodes` version reshuffles everything when the node count changes — which C3
makes a constant event.

**Taken without asking**, under standing authorisation and John's approval of
item 2.

## 135. The word unigram was never 9.323, and the temperature grid is too narrow at word level

Two defects in the word-level instrument, both found while building g18-01 and
both affecting numbers already written down. Neither changes a conclusion; one
makes the standing conclusion **larger** than it was recorded as.

### The bar was 1.26 bits easier than the number every claim was made against

g17-01's record and STATE both quote a word unigram at **9.323** and describe the
model as *"1.40 bits WORSE than counting how often each word appears"*.

`openplexus/ngram.py` — this project's own counter, the one every character-level
baseline goes through — scores the same corpus, the same 90,000 training words
and the same held-out positions at:

    word unigram (NGram order 0)    8.068
    word bigram  (NGram order 1)    7.848
    the model, floor                10.711    reproduced, against 10.721 recorded
    uniform                         10.759

**So the model is 2.65 bits worse than a unigram, not 1.40.** The reproduction of
the floor to 0.01 bits says the model side of the comparison was measured
correctly and only the bar was wrong.

Where 9.323 came from is not recoverable — the calibration was local and left no
script — which is itself the finding: **the bar was hand-rolled beside the
measurement instead of taken from the instrument that exists for it.** This is
the same shape as the `prequential 4.540 ... unigram BEATEN` line that stood for
weeks (decision 118): a number computed once, on the side, and then quoted.

`g18_01`'s `counting_bars` goes through `NGram` for exactly this reason, and it
scores the bars on `chunk[1:]` so they see the positions the model is scored on
rather than a set that differs by the first token of every chunk.

### The temperature grid pins at its own edge once the model has a prior

Every word-level number is calibrated over temperatures 0.05 to 20. At word level
the model's logits have a standard deviation of **0.006**, so the fit wants to
amplify them far harder than at character level:

    readout_bias off   stock grid  temp 0.0824  interior     10.711
                       wide grid   temp 0.0750  interior     10.711
    readout_bias on    stock grid  temp 0.0500  PINNED-LOW   10.252
                       wide grid   temp 0.0328  interior     10.195

**The arm with a bias chose the smallest temperature on the grid**, which means
the grid, not the data, decided it — and it understated that arm by 0.057 bits.
The floor happens not to pin, so the recorded floor stands, but a comparison
between a pinned arm and an unpinned one is not a comparison at all.

Fixed by widening the grid to 1e-4 and by recording `pinned` per cell, so a
future run reports the defect instead of absorbing it.

### And a third number, which is not a defect but is worth having

**`readout_bias` is worth 0.52 bits at word level** (10.711 → 10.195), against a
default of off. The model's own source says why: *"a unigram is exactly a bias
over tokens"*. It does not rescue anything — 10.195 is still 2.13 bits above the
unigram — so the account in g17-01 survives: the model's failure at word level is
not a missing prior.

It does mean the bias belongs in g18-01 as an **axis rather than a default**.
With it off the model cannot express a prior at all and cannot reach the bar
however good the addressing becomes; with it on the question "is word-level text
learnable at all" is a fair one.

**Taken without asking**, under standing authorisation. Nothing is retracted;
STATE and g17-01's record are corrected in place with the old figures shown.

## 136. At word level the store contributes nothing, and the floor is its own ablation

g18-00, three passes, 118 cells, runs 30425355572 / 30425842494 / 30426222929,
plus one local ablation. **The headline is not about concept addressing.**

    the model, tuned          9.185 bits/word
    the same model, NOTHING
      ever written to the
      store -- bias only      9.187
    word unigram              8.068
    uniform                  10.759

**Those first two numbers are the same model with its memory switched off, and
they agree to three decimals.**

### How this was reached, because the route matters

g17-01 reported the model at 10.721 and concluded it does not learn word-level
text at all. Two corrections landed on the way here:

- **The bar was wrong** (decision 135): the unigram is 8.068, not 9.323.
- **The rate was frozen** (note 046): `lr=0.05` came from character level.
  Sweeping it and the store's cap took the floor from 10.195 to **9.185**, which
  is 0.98 bits against the 0.038 the original finding rested on.

That second correction looked like good news for the model. **The ablation says
it is not news about the model at all.**

### The prediction, and it was refuted in the useful direction

Registered in the sweep record before the ablation returned: `nostore` would land
near the unigram at 8.068, meaning the store is a net *cost*.

**Measured 9.187.** The bias does not learn a unigram — it is 1.12 bits worse
than counting — so the "actively harmful" half is wrong at this rate. The other
half is right and larger: the store contributes **nothing**.

### What the learning rate was actually doing

    lr 0.05     floor 10.108    the store is HARMFUL, by 0.92 against nostore
    lr 5e-6     floor  9.185    the store is INERT, to three decimals

No rate between 5e-4 and 2e-6 makes it positive. **Lowering the learning rate
does not teach the model to use its memory; it turns the memory off.** The 0.98
bits "recovered" is the model shedding a component that was hurting it.

### And it explains the result that looked like a mechanism finding

Five addressing schemes at K=128, each at its own best rate:

    floor            9.185     no grouping
    stratified-128   9.185     only the rare tail grouped
    current-128      9.252     one coordinate of the pair grouped
    context-128      9.591     the other coordinate
    concept-128      9.985     both coordinates

That reads as "every step of address collapse costs accuracy". With the ablation
it reads more simply: **the store was not using any resolution, so grouping did
not spend any.** What the groupings did was make an inert component harmful
again — and the ordering tracks how much collapse each one applies.

`floor` and `stratified-128` agreeing to three decimals at four separate rates
was the clue, and it is what the ablation was written to chase. Two addressing
schemes cannot agree that precisely unless what distinguishes them has stopped
mattering.

### What this does NOT say

**Not that the store is inert generally.** At character level it has no bias term
to fall back on and it reaches 5.17 against a 6.00 uniform, so it is doing
something there. This is a word-level result at width 128, one seed, pair keys,
one epoch.

**Not that g17-01's address-sparsity account is wrong.** It may well be true. It
is no longer the *binding* constraint: at the tuned rate the store is not
superposing anything, reusable or not.

**Not that concept addressing is exonerated.** It is behind an inert baseline by
0.80 bits. Its own P1 gate asks for +0.10 the other way.

### What it licenses, and what it stops

**Stops:** reading any word-level table without `nostore` in it. It is now an arm
of g18-01 for exactly that reason.

**Opens, and this is the question that matters more than addressing:** is there
any rate, width or key scheme at which this store contributes a single positive
bit at word level? If the answer is no, the architecture line's next move is not
a better address — it is finding out what the store is for.

**Taken without asking**, under standing authorisation, at 06:0x on 2026-07-29
with John asleep. Nothing is retracted: g18-00's record carries every figure, and
the prediction it refuted is scored in place rather than rewritten.

## 137. Three axes, none of them it — and g18-01 is withdrawn before dispatch

g18-02, 24 of 24 cells, run 30430499110. **The gate is refuted and the
falsifier holds.** Each arm against its own matched ablation, at the rate chosen
on held-out training text:

    pair   d128    store 9.185 against nostore 9.187    +0.002
    pair   d512    store 9.184 against nostore 9.187    +0.002
    single d128    store 9.778 against nostore 9.187    -0.591
    single d512    store 9.869 against nostore 9.187    -0.682

**The learning rate is not it** — three rates over two orders of magnitude, best
gain +0.002. **The width is not it** — quadrupling the store moves the pair arm
0.001 bits, so "too small to hold anything useful" is refuted as flatly as the
rest. **And the key scheme is not it in the direction that mattered most:**
single keys make the store a bigram in vector form, a word bigram beats the
bias-only model by 1.34 bits, and addressed exactly that way the store is **0.68
bits worse than not existing.**

**The rail is what makes it readable.** `nostore` is identical to three decimals
across both widths and both key schemes — spread 0.000. It has no store, nothing
about the store should move it, and nothing does.

### So the problem is not the address

Note 042's account, note 045's index, and the whole concept-addressing line rest
on the store being addressed badly. Three axes say it is not addressed badly; it
is that whatever the store retrieves, the readout cannot turn into a better
prediction than the prior it already has — and mixing it in costs accuracy.

### g18-01 is WITHDRAWN before dispatch, and that is the point of the sequencing

It was written, checked, pre-registered and settled at lr 5e-6 / cap 5.0: 128
cells over the whole K axis, three seeds and both controls.

**It is not being run.** Its gate asks whether some grouping beats the floor by
0.10 bits. The floor is an inert store, the groupings were already measured at or
behind it at K=128 in five ways, and g18-02 says no width, rate or key scheme
makes the store contribute at all. Spending 128 cells would measure *how much
each grouping harms a component that does nothing*, which is not a finding
anybody needs.

This is decision 112's move — a sweep withdrawn after reading, before dispatch —
and g17-01's, which was calibrated and never sent. The scripts and the workflow
stay in the tree so the decision is reversible by anyone who thinks the K axis
still says something.

### The question that replaces it

**Has the store ever contributed anything on TEXT at all?**

At character level the model reaches 5.17 against a 6.00 uniform and that looks
like the store working. But **every character-level run had `readout_bias` off**,
so the store's contribution there was never measured against a model that could
express a prior — and `Wo` can carry a prior by itself.

If a character-level `nostore` with the bias on lands near 5.17, the store has
never contributed on text and the entire text line has been measuring a learned
prior. That is a larger claim than anything here, it is one run away, and it is
g18-03.

**Taken without asking**, under standing authorisation, with John asleep. The
withdrawal of g18-01 is the decision he would most likely want a say in, so it is
stated first in STATE and is trivially reversible.

> ## ⚠ 136 AND 137 ARE RETRACTED — see decision 138. The harness trained on the
> wrong target, and every model number in both entries is void. The entries are
> left standing, unedited, because a log that deletes its wrong conclusions
> cannot be checked.

## 138. RETRACTION: the g18 harness trained on the wrong target

    character floor, as g18 measured it       5.9965    uniform is 6.000
    character floor, target corrected         5.4227
    decision 63, the comparison set          ~5.53

One line, and it voids every model number this harness produced.

### The defect

The model's answer at step `t` is built from a retrieval keyed on token `t`, so
it is a prediction of token **`t+1`** — and `run` records it in the trace entry
for `t+1`, as `previous_scores`. **Scoring was right all along:** entry `t`
against `tokens[t]`.

Training was not. `model.run(piece, piece, ...)` teaches the answer at step `t`
to name token `t`, which is a mapping its input cannot carry. g15-01 has always
had it right:

    targets = np.concatenate([tokens[1:], tokens[-1:]])
    scored = np.ones(len(tokens), dtype=bool)
    scored[-1] = False

### How it hid, and this is the part worth remembering

**The readout still learns.** `|Wo|` reaches 0.88 with a mean of 0.043, and the
temperature calibration then flattens a signal-free score vector to uniform. So
the failure presents as *"the store contributes nothing"* rather than as a bug: a
component asked for something it cannot supply, and a readout dutifully fitting
noise.

Every arm was mistrained equally, so the results behaved. The table was
internally consistent. The rails passed. `nostore` sat exactly where a bias-only
model should. The ordering across five addressing schemes was monotone and
interpretable, and it had a tidy explanation. **A wrong measurement that behaves
itself is the expensive kind.**

What caught it was not a rail but a *reproduction*: the character floor came back
at 5.986 where decision 63 says 5.53, and that number had no innocent reading.

### Void

Everything measured through this harness: g18-00's three passes (the 9.185 floor,
the learning-rate sweep, "0.98 bits was the rate", the arm ordering, the cap
being worth 0.20); g18-02 entire; g18-03's first pass at both units; **decisions
136 and 137 in full**; and the 5.188 character ablation that looked like the
largest result of the night.

### Survives, because it never went through the model

- **Decision 135.** The unigram at 8.068 and the bigram at 7.848 are counts from
  `NGram`. No model, no training, no target.
- **The address-space measurements**: 36,299 surface addresses against 3,438 at
  K=128, recurrence 2.48 against 26.18. Computed from the stream.
- The `unstable` and `diverged` rails, `nostore` as an arm, per-unit
  configuration, and the `--keys` / `--width` / `--key-scale` / `--units` seams.
- **Note 046's point, strengthened rather than weakened.** Its rule was applied
  to the learning rate and then a *training convention* was inherited from the
  same script without being checked. The note says every inherited constant
  crosses a boundary with the measurement; a convention is one of them.

### And it puts g17-01 in question

`model.run(piece, piece, ...)` is g17-01's line and this harness inherited it. So
*"the model does not learn word-level text at all"* — the finding that turned the
architecture line toward addressing — was measured on a mistrained model and has
to be re-established before anything is built on it.

**Taken without asking**, under standing authorisation. The corrected sweeps are
dispatched, and the sweep records keep their tables with the defect named at the
top rather than deleted.

## 139. The store's contribution on text is exactly substitutable by a learned prior

g18-03, 20 of 20 cells, run 30432893640, **on the corrected harness and with its
reproduction gate passing.** This is the first valid measurement of the question
and it replaces every void number in decisions 136 and 137.

    arm                          0.05      0.02     0.005
    characters bias0 floor      5.423     5.385     5.395
    characters bias0 nostore    6.000     6.000     6.000
    characters bias1 floor      5.395     5.377     5.203
    characters bias1 nostore    5.421     5.280     5.195

    words bias0   floor 10.700   nostore 10.759     (lr 5e-6)
    words bias1   floor  9.186   nostore  9.187

    characters   bigram 3.884   unigram 4.852   uniform  6.000
    words        bigram 7.848   unigram 8.068   uniform 10.759

### P0 passes, which is what makes the rest readable

The character `bias 0` floor lands at **5.385**, against decision 63's ~5.53 and
a 6.000 uniform. Within the 0.15 tolerance and 0.6 below uniform. **The
instrument reproduces the character model**, which the previous three passes did
not.

### The finding, stated precisely

    with NO prior available    the store is worth +0.615 bits   (characters)
    with a prior available     the store is worth -0.008 bits   (characters)
                               and +0.002 bits                  (words)

**Both halves matter and the second does not cancel the first.**

Every character-level number this project holds was measured at `bias 0`, where
the store is the only thing that can learn, and there it genuinely carries 0.615
bits. That work was real.

**But a readout bias does the same job slightly better, and the two do not add.**
Give the model a prior and the store's contribution goes to zero — at both units,
across four learning rates.

So the honest claim is not "the store has never contributed". It is: **what the
store contributes on text is worth no more than a unigram-shaped bias, and it is
entirely substitutable by one.** It is not doing anything a prior cannot.

### What this does and does not license

**Does not touch the relational line.** MQAR, kinship and the chain tasks are
solved through this store and there is no prior that solves them — the bindings
are the answer. This is a statement about *text*, where a prior is most of what
there is to know.

**Does explain decision 118**, which has stood unexplained for weeks: the unigram
has never been beaten by this model on text. If the store's whole contribution is
prior-shaped, a unigram is roughly the ceiling and the model has been sitting
just above it. `bias 1` characters reach 5.195 against a 4.852 unigram — still
worse than counting, and now for a reason rather than as a mystery.

**And it re-poses g17-01's premise rather than restoring it.** *"The model does
not learn word-level text at all"* was measured on a mistrained readout. What is
true is narrower: the model learns word-level text about as well as a prior does,
which is 1.12 bits short of counting.

### The next question, and it is not addressing

Note 042 turned this line toward *where facts are stored*. Three sweeps say the
store contributes nothing on text beyond a prior, so **the open question is what
the store is for on this task at all** — or whether text is simply the wrong
instrument for measuring it, and the relational tasks were the right one from the
start.

**Taken without asking**, under standing authorisation, with John asleep. This is
the entry to read first: 136 and 137 are void, and this replaces them.

## 140. g17-01's premise survives its own correction — the pivot was not an artefact

g18-04, 4 of 4 cells, run 30433717766. g17-01's **exact** configuration — width
256, two epochs, lr 0.05, no cap, key_scale 1.0, pair keys, 90,000 words — with
the decision-138 target correction and nothing else changed.

    bias0 floor     10.750      g17-01 recorded 10.721, off by 0.029
    bias0 nostore   10.759      uniform, exactly
    bias1 floor      9.932
    bias1 nostore    9.364

    word unigram 8.068     word bigram 7.848     uniform 10.759

All three predictions confirmed. **The corrected model learns 0.009 bits over
uniform where g17-01 reported 0.038** — if anything less.

### What this settles, and it matters more than the number

Decision 138 put g17-01's premise in question: *"the model does not learn
word-level text at all"* was measured on a mistrained readout, and note 042's
architecture pass — plus the entire week of addressing work — rests on it.

**It reproduces.** So the pivot of 2026-07-28 was made on a real finding. **What
was void was my measurement of it, not it.** That distinction is the whole point
of running this rather than assuming either way.

### And the half that is not a reproduction

    bias1   floor 9.932 against nostore 9.364     the store is worth -0.568

At g17-01's own configuration, **the store is 0.568 bits worse than not
existing** once a prior is available. That is decision 139's claim in its strong
form, stated where the project actually lived rather than at a tuned corner: not
*"contributes nothing"* but *"actively costs"*, at `lr 0.05` — the rate every
text sweep this project ever ran used.

Tuned (139: lr 5e-6, cap 5.0) the same comparison is +0.002.

> **So the learning rate decides whether the store is harmless or harmful, and
> never whether it helps.** Across every configuration measured on a correct
> harness — two units, two key schemes, two widths, seven rates — the store's
> best contribution on text is +0.002 bits.

### Where the line stands now

| | |
|---|---|
| g17-01's premise | **stands** (140) |
| the store's text contribution | **prior-shaped, ≤ +0.002 bits** (139) |
| concept addressing as the fix | **not measured on a correct harness**, and its premise is intact |

The third row is the honest gap. Decisions 136 and 137 refuted concept
addressing on void numbers; g18-01's K sweep was withdrawn on those same numbers.
**Neither the refutation nor the withdrawal is currently supported** — but the
motivation for addressing is weaker than it was, because the thing it was meant
to improve turns out to contribute ~nothing either way.

**Taken without asking**, under standing authorisation. John's call, when he
reads this: re-dispatch g18-01's K sweep on the corrected harness, or leave
concept addressing unmeasured and ask what the store is for on text instead.
**Decided and run — see 141.**

## 141. Address density is worth 0.540 bits, and it does not need to mean anything

g18-01, the pre-registered sweep, on the corrected harness. 32 jobs, 128 cells,
**three seeds**, run 30434436216. This replaces the refutation decisions 136 and
137 made on void numbers.

    bias 0 -- no prior, which is every character-level number's configuration
      floor         10.700        concept-64    10.159      +0.540
      shuffled-64   10.143        permuted-64   10.329
      uniform       10.759

    bias 1 -- a prior available
      floor          9.186        concept-1024   9.279      -0.093
      unigram        8.068

### Three parts, and the middle one is the finding

**1. Address density is worth real bits where the store is the only learner.**
The bias-0 floor is 0.06 below uniform — a model that has learned almost nothing
— and concept-64 reaches 10.159. **0.540 bits, the largest effect any addressing
change has produced in this project.**

**2. It does not need to MEAN anything.** `shuffled-64` — grouping built from a
content index fitted on a *scrambled* corpus — reaches **10.143**, slightly
better than the learned grouping. The semantic content is worth nothing; the
collapse of the address space is worth everything.

The two controls disagree with each other, which is why there are two.
`shuffled` clusters a structureless space and lands on a slightly better size
distribution; `permuted` forces the real distribution onto random members and
does worse than both. **Even the shape of the grouping matters more than its
meaning.**

**3. A prior subsumes all of it.** At bias 1 no grouped arm beats the floor. The
0.540 bits grouping buys without a prior is less than the 1.514 a prior buys
alone, and they do not add — decision 139, from the other direction.

### The sign was backwards, and the reason is worth keeping

Decisions 136/137 said grouping *hurts* at every K, monotonically in how much
resolution it destroys, and had a tidy story for it. **On a mistrained readout
the retrievals are noise, so denser addresses make the noise more consistent and
the readout fits it harder — worse. Corrected, denser addresses carry more usable
signal — better.**

That is the second time tonight the void harness produced not just a wrong number
but a wrong number with a satisfying explanation attached.

### What stands

**Note 045's proposal is not vindicated.** Its claim is that addresses derived
from *meaning* pay; the control says meaning contributes nothing here and the
address count is doing the work. A cheaper mechanism — any grouping, however
chosen — captures the whole gain.

**And it is not refuted either.** 0.540 bits is real, reproducible across three
seeds, and larger than anything else this line has moved. It is simply not
*about* concepts.

**P5 holds across all 128 cells**: the best is 9.186, still 1.12 bits short of a
unigram.

**Taken without asking.** The lean recorded an hour earlier — leave it unmeasured
— was wrong, and reversing it is why this exists: 0.002 bits is what the *current*
addressing contributes, not a ceiling on what a different one could. Assuming an
axis is flat because a different axis is flat is decision 112's error.

## 142. The store carries MQAR completely, and the prior that wins on text costs 0.279 here

g18-05, 12 cells, three seeds, run 30436450902. **The control the whole text
line rests on, and it had never been run.**

    bias0 floor     0.9950        bias0 nostore   0.0000
    bias1 floor     0.7158        bias1 nostore   0.0000
    trivial floor   0.3438        what a SMART guesser scores

### The inference is now a measurement

Decisions 139, 140 and 141 each protect themselves with the same sentence: *"this
does not touch the relational line — MQAR, kinship and the chains are solved
through this store and no prior solves them."* It is a good inference. **It was
still an inference, and the same `nostore` ablation that overturned four sweeps
of text results had never been pointed at the results the project rests on.**

Pointed at it: the store carries the task completely. **`nostore` scores zero** —
not chance, zero, because a model with nothing to retrieve does not guess, it
emits a constant. So the text results are about *text* being the wrong instrument
for this component.

### And P3 failed in the direction nobody predicted

    bias 0    floor 0.9950
    bias 1    floor 0.7158        the prior COSTS 0.279

The prediction expected the bias to be inert on MQAR, and reasoned that if it
*paid* the generator must have a base rate. It does not pay. **It costs**, and
that is the more interesting failure: a prior with nothing to predict does not
sit idle — it competes with the retrieval for the same readout, and on a task
with no exploitable marginals it is pure interference.

> **The exact mirror of the text result.**
>
>     on TEXT    the prior wins and the store adds nothing   (139, 141)
>     on MQAR    the store wins and the prior costs 0.279    (here)
>
> Two pathways into one readout, and which of them pays is decided by the task
> rather than by the architecture.

**That reframes the whole night's text finding.** "The store contributes nothing
on text" is not a statement about the store being weak. It is a statement about
text having marginals a linear prior can exploit and the store having no
advantage over that — while on a task with no marginals, the store is everything
and the prior is a liability.

### Three instrument failures, and what caught each

    decay 1.0 inherited from word level      caught by the trivial floor
    autoregressive left at its default       caught by the trivial floor
    bits convention applied to accuracy      caught after the run, in review

The first two are note 046's mistake — a constant crossing a boundary — for the
fourth and fifth time in one night. **The trivial floor caught both before
dispatch**, which is what a rail is for and is the first time tonight one did the
job a reproduction had to do.

The third is mine after the fact: the summariser printed P3's two explanations
the wrong way round because every other sweep in this line scores bits, where
lower is better, and this one scores accuracy. Corrected in place.

**Taken without asking**, under standing authorisation. This is the entry that
says what the text results do and do not mean, and it is the one to read beside
139 and 141 rather than after them.

## 143. Grouping answers what was never stated — the first result for concepts.py

g19-01 on the `families.py` task, three seeds. **The first measurement in which
the surface-to-concept indirection pays for itself**, and it is in the frame John
restated the goal in: understanding rather than prediction.

    arm           direct  transfer   family recovery
    ungrouped     0.6583    0.0867         --
    concept       0.9967    0.9983      1.000
    permuted      0.2725    0.0658      1.000
    nostore       0.0000    0.0000         --
    chance                  0.1250

All three of note 048's predictions confirmed. P1 asked for +0.20 on TRANSFER and
got **+0.9116**.

### What TRANSFER is

An entity whose own fact was **never stated**, whose siblings' were. `ungrouped`
can only guess and does — 0.087, structurally rather than for want of tuning.
`concept` shares the store's address across the family, so the sibling's write is
what a read at this entity returns.

### The control that matters, and it held

`permuted` has the same number of groups and the same group sizes; only the
membership is wrong. It scores **below** `ungrouped`. On text (decision 141) the
whole measured gain came from having fewer addresses *however chosen*, and only a
size-matched control told that apart. **Here it cannot carry the result: the
similarity has to be the real one.**

And **a wrong concept map is worse than none** — permuted DIRECT 0.273 against
ungrouped 0.658. Accidental siblings share an address and overwrite each other.

### The tautology, stated before this is quoted anywhere

**The task is built so a family shares one value**, so "group by family" and
"know the answer is shared" are nearly the same statement. A sceptic is right
that it is partly circular, and the sweep record says so at length.

What is not circular: the families are **never labelled** (discovered from
co-occurrence in a separate stream), the value is **redrawn every sequence** so
no prior can help, the model must **store a sibling's fact and retrieve it**, and
`permuted` **fails at matched address count**.

So the claim is about the **composition** — discover a grouping, address a store
by it, recall through it — and all three parts working together is what had never
been measured.

**It does not show** that concepts solve anything harder than *same kind, same
answer*. That is the simplest generalisation there is. A task where the answer
depends on the family **and** on something entity-specific is the next real test,
and this says nothing about it.

### What it settles about the last two days

Decisions 139–142 found the store contributing nothing on text, and note 047
explained why: on a next-token objective it can only express an n-gram. **This is
the same store, the same grouping machinery, and the same `nostore` ablation, on
a task where the answer is stated rather than distributed — and it goes from
+0.002 bits to +0.91 accuracy.**

The objective was the ceiling. That was an argument on 2026-07-29 morning; it is
a measurement now.

**Taken without asking**, under standing authorisation and John's *"whatever next
experiments make sense to continue down that line"*.

## 144. Concept addressing cannot hold an exception, and that is the whole price

g19-01's exception arm, three seeds, run after decision 143 so that its numbers
are a test of that result rather than part of it. **E1 was registered as a
prediction I expected to lose, and it lost.**

    arm           direct  transfer  exception   wrong answer = a sibling's
    ungrouped     0.7792    0.0608     0.7833        0.0084
    concept       0.4492    0.4708     0.3708        0.8657
    permuted      0.3417    0.0517     0.3167        0.0300
    nostore       0.0000    0.0000     0.0000        0.0000

An EXCEPTION is an entity whose **own stated fact contradicts its family's**. For
`ungrouped` that is ordinary recall and it scores 0.783. For `concept` it is
0.371 — **0.41 worse than having no concepts at all.**

### The mechanism, named by E3 rather than assumed

When `concept` answers an exception wrongly, **86.6% of the time it says a
sibling's value**, against 0.8% for `ungrouped`. A wrong answer that is
specifically the family's is the superposition speaking; generic failure would
scatter.

### The larger finding, which was not predicted

**One exception per family does not just break the exception. It halves
everything.**

    concept, no exceptions      direct 0.9967   transfer 0.9983
    concept, one exception      direct 0.4492   transfer 0.4708

The arithmetic is mechanical: 2 facts stated per family, 1 contradicting, so the
family's single address holds two competing values and a read is a coin flip.
0.47 is that coin flip.

> **Concept addressing cannot represent within-family variation at all.** The
> address holds one thing, and a second thing written to it destroys the first.

> ### ⚠ THE SENTENCE ABOVE IS WRONG — CORRECTED BY DECISION 145
>
> "One exception halves everything" is an artefact of the configuration this arm
> happened to use: 2 facts stated with 1 contradicting is a literal 50/50, so of
> course a read is a coin flip. **Give the majority any weight and the default
> survives perfectly** — 0.99 transfer at a 20% exception rate. The address holds
> the SUM and the majority wins. What is annihilated is the exception, not the
> default. See 145.

### It explains decision 141 from the other side

Grouping words hurt on text. **This is why: text is nothing but exceptions.**
Every word has its own continuations, so every grouped address holds a dozen
competing values. The resolution cost that was argued about for a day is now a
number, measured where it can be isolated.

### What this does to decision 143

**Not a retraction.** 143 measured that the composition works — discover a
grouping, address a store by it, recall through it — and it does, on a homogeneous
family. This measures what that costs, and the cost is total: the mechanism holds
one value per concept and nothing else.

**Together they say the indirection is real and the representation is too poor to
use as it stands.** A concept that cannot differ from its siblings in any respect
is not a concept, it is a bucket.

### What it opens

The next mechanism is not a better grouping. It is **a store that can hold a
family default and a per-entity override at once** — which is a representation
question, not an addressing one, and it is the first time this line has arrived
at one.

**Taken without asking**, under standing authorisation. The exception arm was
added as a falsifier for 143 rather than as a new direction, and its predictions
were registered before it ran.

## 145. The majority wins and the exception is erased — correcting 144

Decision 144 concluded that *"the address holds one thing, and a second thing
written to it destroys the first"*, from a single configuration: 2 facts stated
per family with 1 contradicting. **That is a literal 50/50, so the coin flip it
measured was the configuration and not the mechanism.**

Varying how many siblings agree, at one exception throughout:

    stated  agree  exception share   direct  transfer  exception
         2      1            0.50   0.4650    0.4400     0.3725
         3      2            0.33   0.9200    0.9250     0.0300
         5      4            0.20   0.9825    0.9900     0.0000

**The default is robust.** At a third and a fifth, `direct` and `transfer` return
to 0.92 and 0.99 — the concept address holds the superposition and the majority
dominates it, exactly as a sum should.

**And the exception is not merely wrong. It is erased**: 0.030 at a third, and
**0.000** at a fifth. Not degraded, not noisy — gone.

### What that changes

**144's mechanism was right and its magnitude was wrong.** Grouping does spend
specificity, and E3's 86.6% "answers a sibling's value" still holds. What is not
true is that one dissenting fact breaks the whole family; it takes a dissenter
worth half the evidence to do that.

**The corrected statement is worse in one way and better in another.** Better:
the concept map is a robust default store, and tolerates dissent in proportion to
how outnumbered it is. Worse: **the system does not merely fail to answer about
an exception, it confidently answers with the category's default** — which is the
most dangerous shape a wrong answer can have, and a straightforward description
of a stereotype.

### And it strengthens note 049 rather than weakening it

Note 049 proposes writing at both the surface and concept addresses and reading
the surface first. 144's numbers left that looking doubtful, because the
exception's write appeared to poison the concept address.

**At realistic ratios it does not.** 0.99 transfer with a 20% exception rate means
the concept address survives the extra write comfortably, so a two-level scheme
can hold the default at the concept and the override at the surface without the
two destroying each other. **The ceiling probe agrees**: at least one of the two
arms is right on 0.853 of exceptions and 0.878 of directs.

So the mechanism is licensed, and the remaining question is the selection rule
rather than whether there is anything to select.

### What was learned about the method, again

**A finding from one configuration is a finding about that configuration.** 144
swept nothing — it took the task's default shape, measured a dramatic number, and
generalised from it. The sweep that corrects it cost four minutes.

This is the same failure as the single-seed claim on 2026-07-29 morning, in a
different dimension: there the axis was seeds, here it was the ratio the effect
depends on most.

**Taken without asking**, under standing authorisation. 144 is corrected in place
with the wrong sentence struck and the reason beside it, not rewritten.

## 146. Option B is the right addressing and cannot choose on its own

John picked option B independently on 2026-07-29 — never share an address, use
the content index to read neighbours instead — and it is note 045's design from
July. **`index_branches` already implements it**, so this cost a configuration
rather than a mechanism.

Three seeds, against the two extremes:

    with exceptions present     direct  transfer  exception
      ungrouped                 0.7792    0.0608     0.7833
      concept (grouped)         0.4492    0.4708     0.3708
      indexed (option B)        0.7158    0.2650     0.6875

    no exceptions               direct  transfer
      ungrouped                 0.6583    0.0867
      concept                   0.9967    0.9983
      indexed                   0.9733    0.7517

**I1 CONFIRMED** — B beats plain addressing on transfer by +0.665 without
exceptions and +0.204 with.

**I2 REFUTED** — I predicted B would hold exceptions within 0.05 of plain
addressing. It loses 0.096.

**I3 FIRED**, which is the finding. B reaches neither extreme; it lands between
them on both kinds. It is **averaging rather than choosing**.

### And the knobs prove it is a pure exchange rate

`index_weight` sets how much the neighbours count. Sweeping it:

    weight   direct  transfer  exception
      0.25   0.7700    0.1125     0.7763
      0.50   0.7612    0.1738     0.7538
      1.00   0.7012    0.2700     0.6787

**Monotone in both directions at once**, and `transfer + exception` is flat at
~0.93 across every setting. `index_sharpness` moves nothing. There is no corner
that holds both.

> **The additive rule cannot choose. It can only set the exchange rate.**

### What that settles

**B is the right addressing.** Nothing is ever overwritten, so the specific fact
survives *in the store* — which grouping cannot say, and which is why the
exception column is 0.688 rather than 0.371.

**But surviving in the store is not surviving to the answer.** Reading the
entity's own address and the neighbours' and *summing* them means the neighbours
always contaminate, in proportion to their weight.

**So it is B plus A's rule, not B or A** — and the combination is *simpler* than
note 049 proposed, because B already writes only at the surface:

    read the entity's own address
    if it holds a real binding, answer from it and stop
    otherwise read the neighbours the index proposes

No double writes. One conditional. The ceiling probe says both answers are
recoverable — at least one arm is right on 0.853 of exceptions and 0.878 of
directs — so the only remaining question is the threshold for "a real binding",
which is note 049's and has decision 130's precedent.

### The cost, unchanged and now worth quoting

A true sibling is the **nearest** neighbour 100% of the time on this task and all
three are inside the top 3. So the extra traffic is one read when the entity's
own address answers, and up to three when it does not — against grouping's one.
That is the C1 number, measured rather than estimated.

**Taken without asking** for the measurement; the model change it points at is
still John's call and has not been started.

## 147. The model has both answers and cannot tell which — note 049 refuted by its own falsifier

Built on John's greenlight, measured, and it does not work. Three seeds, and both
selection rules lose to simply adding the two retrievals together:

    with exceptions          direct  transfer  exception
      ungrouped              0.7792    0.0608     0.7833
      concept (grouped)      0.4492    0.4708     0.3708
      indexed (B, summed)    0.7158    0.2650     0.6875
      preferred (by norm)    0.2842    0.3442     0.2467
      margin (by decode)     0.5833    0.1917     0.5808

    no exceptions            direct  transfer
      concept                0.9967    0.9983
      indexed (B, summed)    0.9733    0.7517
      preferred (by norm)    0.8458    0.8933
      margin (by decode)     0.8942    0.7850

**R1 REFUTED** — neither rule holds EXCEPTION within 0.05 of `ungrouped`; they
lose 0.537 and 0.203. **R2 REFUTED** — both fall below `indexed` on the
no-exception task. **Note 049's P1 fails with both reads available**, which is the
refutation condition that note wrote down before any of this was built:

> *"The information would be present at two addresses, the model would have both,
> and it still could not choose — which would say the problem is selection rather
> than storage, and that is a different and harder question."*

### Two signals tried, both wrong

**Magnitude (`"norm"`).** How much was written at an address says nothing about
whether it is the right address. It collapsed to 0.247 on exceptions where plain
addressing holds 0.783 — making a hard choice on a signal that does not separate
the cases discards the correct answer about as often as the wrong one, and that is
worse than never choosing at all.

**Decode margin (`"margin"`), which is decision 130's actual signal.** Better —
0.581 against the norm rule's 0.247 — and still below the summed baseline's 0.688.
Confidence in *an* answer is not evidence about *which retrieval* produced it.

### A correction to what I claimed while building it

I said the threshold had decision 130's precedent and then implemented a **norm**
comparison. 130 fires on the margin of the decode. The two are not the same signal
and I substituted one for the other because it was the quantity to hand. The
margin version was written afterwards specifically to test the claim I had already
made. It does better, and still loses.

### What is now known, and it is worth more than the mechanism was

The ceiling probe says at least one of the two addresses holds the right answer on
0.853 of exceptions and 0.878 of directs. The model holds both reads in the same
step.

> **Storage was never the problem. Selection is.** Nothing hand-made — neither how
> much was retrieved nor how confidently it decodes — carries the information
> needed to choose between two retrievals.

### What that leaves

Summing remains the best combination measured, and decision 146 already showed it
is an exchange rate rather than an answer. The next thing that could work is a
**learned** gate — trained to predict which read to trust, rather than a rule
inferred from a quantity that happened to be lying around. That is a larger change
than anything this line has proposed and it should not be started on the strength
of two refuted rules; it needs its own predictions first.

`index_prefer` keeps both refuted settings rather than deleting them, because a
measured negative is cheaper to read than to rediscover.

**Built and measured under John's explicit greenlight**, including the freedom to
try novel solutions. This one failed, and the failure is the useful part.

## 148. "Is there anything here" — the first arm that is good at both

Three seeds. `inherit` answers from the entity's own address when **anything** has
been written there and from its neighbours' when nothing has:

    with exceptions    direct  transfer  exception   wrong answer = a sibling's
      ungrouped        0.7792    0.0608     0.7833        0.0084
      concept          0.4492    0.4708     0.3708        0.8657
      indexed (sum)    0.7158    0.2650     0.6875        0.3441
      inherit          0.8100    0.4350     0.8183        0.0247

    no exceptions      direct  transfer
      ungrouped        0.6583    0.0867
      concept          0.9967    0.9983
      indexed (sum)    0.9733    0.7517
      inherit          0.9233    0.9825

**N1 CONFIRMED, exactly.** The gate defers on 1.0000 of TRANSFER queries and
0.0000 of DIRECT and EXCEPTION ones, every seed. Not approximately — the sketch
is exact, so the decision is too.

**N2 CONFIRMED**, and it is the thing this whole line was for. EXCEPTION 0.8183
is within 0.05 of `ungrouped`'s 0.7833 (above it), and TRANSFER 0.4350 is well
past `indexed`'s 0.2650. **No previous arm was good at both.** Grouping bought
transfer by destroying exceptions; plain addressing held exceptions and was at
chance on transfer; summing landed between them on both.

**N3 REFUTED, by 0.050.** On the task with no exceptions, DIRECT falls from
`indexed`'s 0.9733 to 0.9233 while TRANSFER rises from 0.7517 to 0.9825. **That
is the mechanism's price and it is the same fact as its win:** summing lets
agreeing neighbours corroborate a fact the entity already has, and `inherit`
refuses that corroboration on principle. Refusing it is exactly what keeps a
contradicting fact intact when there IS a conflict. The trade is 0.050 of direct
recall for 0.231 of transfer, and it is a trade rather than a free win.

**And it is not answering in the family's voice.** When `concept` gets an
exception wrong it says a sibling's value 86.6% of the time. `inherit`: 2.5%,
below plain addressing's own error profile.

### Three wrong answers before the right one, and each named the next

**`norm`** (147) compared retrieval magnitudes. `||W k||` conflates *was this key
ever written* with *how large the value there is*.

**`margin`** (147) compared decode confidence. Confidence in AN answer is not
evidence about WHICH read produced it.

**`occupancy`** asked the right question in the wrong space — summing written
keys in the store's own `d` dimensions. Its falsifier O4 fired on the first run:
deferral 0.723 on DIRECT against 0.815 on TRANSFER, a separation of 0.09. The
reason was computable and had been written into O4 in advance. A sum of `N`
normalised near-orthogonal keys carries cross-talk of standard deviation
`sqrt(N / d)`, which at `d = 64` and `N ~= 100` is **1.25 against a signal of
1.0**. Widening the store separates the two faults cleanly:

    d      defer on TRANSFER   defer on DIRECT
      64             0.815              0.723
     256             0.887              0.745
    1024             0.963              0.603

TRANSFER climbs toward the 1.0 it should always have been -- that is the floor
receding, exactly as `sqrt(N/d)` says it should. DIRECT should have fallen to 0.0
and does not. **Width fixed the floor and did nothing for the rule**, and 16x the
store to get most of the way to one of two halves is the argument for not fixing
it with width.

**`sketch`** replaced the space. `AddressSketch` hashes a key by the sign pattern
of 16 random hyperplanes — Charikar (2002), whose collision probability follows
the angle between two vectors — so collisions fall as `2 ** -bits`, free of `d`.
S1's transfer half came back at **1.000** immediately. Its direct half was 0.613
against a predicted 0.1, and that was the rule, not the sketch: `sketch` still
asked *who has MORE written*, and `decay` makes a sibling's later-stated fact
outrank an entity's own.

**`inherit`** stopped comparing. Membership is not "who has more", it is "is
there anything here" — which is what note 049 wrote in the first place: *read the
entity's own address first; if it holds a real binding, answer and stop*.

### The part that matters beyond this task

Note 049's P3 asked how "a real binding" could be decided without a fitted
constant, and decision 147 could not answer it. **The bar is zero.** An address
never written misses the hash table and reads exactly 0.0; one written once reads
at worst `decay ** steps`, which is positive. Nothing is tuned, and nothing has to
generalise across configurations, because the separation is structural.

### What it costs, stated rather than buried

The sketch is a **second memory and it is not superposed**, so it does not
inherit the store's failure modes. That is the point and it is also the
objection. What justifies it is the asymmetry Bloom filters exist for --
membership is one bit, a value is `d` floats -- and the invariant that the sketch
records only THAT an address was written. The moment it carries a value it has
become a second store and the comparison proves nothing. `tests/test_sketch.py`
holds that line, including a test that asserts `SumSketch` FAILS at `d = 64`, so
the floor above is measured rather than argued.

Decision 147 said storage was never the problem and selection was. **That was
right, and the missing piece was a way to ask the membership question exactly.**

**Built under John's greenlight to look up research or invent, staying inside
concept-based learning.** Nothing here predicts a token; the gate decides which
concept's address answers a query.

## 149. Note 049's P3, asked in July and answered: no constant moved

P3 was the falsifier that mattered most and the one decision 147 could not reach,
because there was nothing working to sweep:

> *"The threshold generalises across `n_values` and `family_size` without being
> re-tuned. If it has to move per configuration, it is a fitted constant wearing
> a mechanism's clothes."*

Three seeds per cell, exceptions on, everything else at decision 148's settings:

                      TRANSFER              EXCEPTION            gate
                  inherit  indexed    inherit  ungrouped    defer trn / dir
    n_values=4     0.4817   0.3025     0.8692     0.8500      1.0000 / 0.0000
    n_values=16    0.4158   0.2708     0.7600     0.7442      1.0000 / 0.0000
    family_size=3  0.4075   0.3350     0.8142     0.7942      1.0000 / 0.0000
    family_size=6  0.2875   0.2000     0.7775     0.7583      0.9025 / 0.0000
    (148's cell)   0.4350   0.2650     0.8183     0.7833      1.0000 / 0.0000

**G2 CONFIRMED at every setting.** `inherit` beats `indexed` on TRANSFER and
holds EXCEPTION within 0.05 of plain addressing everywhere -- in fact **above**
it in all five cells, and above it on DIRECT in all five too. The margins move
with the task, which they must: a larger answer alphabet lowers chance and lowers
everything. **The ordering does not move.** G3 did not fire.

**G1 held in four cells and dipped in the fifth**, and the dip is not the gate.
At `family_size=6` the gate deferred on 0.9025 of TRANSFER queries rather than
1.0000. `BRANCHES` is 3, set when decision 146 measured that all three siblings
of a 4-member family sit inside the top 3. A 6-member family has 5 siblings and
only 2 have stated facts, so on ~10% of transfers **no proposed neighbour holds
anything** -- and the gate then correctly refuses to defer, because deferring to
an empty address is decision 69's two-weak-reads failure, which `inherit` was
built to avoid on both sides.

Re-run at `--branches 5`:

    family_size=6   defer on TRANSFER   TRANSFER
      branches 3               0.9025     0.2875
      branches 5               1.0000     0.3317

**Exactly as predicted, and it names the limit as the index's reach rather than
the gate's rule.** The knob that had to move was `BRANCHES`, which is how many
neighbours the content index proposes -- a property of the index that was never
claimed to be family-size-independent. **No threshold moved, because there is no
threshold: the bar is zero.**

So P3 is answered rather than dodged. The `--n-values`, `--family-size` and
`--branches` flags default to `None` so an unswept run reproduces decision 148
byte for byte, and `n_values` and `exceptions_per_family` are now in the
condition string -- without them two sweep cells differing only in the answer
alphabet wrote the same label, which reads as a reproduction rather than a new
measurement.

**Still open, and unchanged by this:** every number is still the families task.
The gate has not met MQAR, kinship, closure or chains.

## 150. The gate costs exactly nothing where it should do nothing

The first measurement of `inherit` outside the task it was designed for. MQAR:
every queried key was written a few tokens earlier, so the correct deferral rate
is 0.0000 **by construction** rather than by argument. Three seeds:

    plain      accuracy 0.9950   deferred      -
    indexed    accuracy 0.8817   deferred      -
    inherit    accuracy 0.9950   deferred 0.0000

**M1 CONFIRMED, and not approximately.** `inherit` matches `plain` seed for seed
— 0.9950/0.9950, 0.9975/0.9975, 0.9925/0.9925 — because the gate never fires and
a gate that never fires cannot change an answer.

**M2 CONFIRMED at exactly 0.0000.** Not "low". Every queried key was written, and
not one of them read as unwritten.

**M4 CONFIRMED, and it is what makes M1 a result rather than a tautology.**
`indexed` — the same extra reads, summed instead of gated — lands **0.113 below
plain**. Consulting neighbours is not free; here they are arbitrary, because the
content index was fitted on MQAR where there is no family structure to find, and
summing arbitrary evidence into a read that was already right can only damage it.
**`inherit` pays the same reads and loses nothing.** The difference between them
is the rule and nothing else.

### M3 did not fire, and it reached backwards

`inherit` treats a sketch count of 0.0 as *nothing was ever written here*. A
**false negative** — an address that was written reading as empty — would be
invisible on the families task: an entity with its own stated fact would silently
inherit its family's answer, and the EXCEPTION column would be quietly wrong in a
way no amount of re-running it would show.

MQAR makes that failure loud, and it did not happen. So decisions 148 and 149 are
not measuring a gate that sometimes discards a fact the model has.

### What this does and does not establish

**Does:** the gate is safe to leave on. It is inert where there is nothing to
inherit, it is exactly inert rather than nearly, and the summing arm it replaces
is not.

**Does not:** MQAR has no structure for the gate to exploit, so this says nothing
about whether `inherit` helps anywhere other than families. Kinship, closure and
chains are still unmet, and that is now the only claim left standing between "the
read gate works on the task built to ask about it" and "the read gate works".

## 151. The gate knows which addresses it wrote, not what it knows

Kinship, three seeds, and it is a bound rather than a result:

    hops 1   plain 0.7767   indexed 0.7067   inherit 0.7767   deferred 0.0000
    hops 2   plain 0.4433   indexed 0.4067   inherit 0.4433   deferred 0.0000

At one hop the answer IS a stated fact. At two it is a composition of two. **The
gate cannot tell the difference** — 0.0000 either way, to four decimals, which is
K1 confirmed at the limit rather than approximately.

### Why, and it was predicted from the layout rather than discovered

Kinship's question ends `... QUERY target FACT subject`, so the scored position's
pair key is `(FACT, subject)` — the key a fact wrote for that person **as a
subject**. The asked subject is the start of the path, so it is the subject of at
least one stated fact at every hop count. The address is occupied whether the
answer needs recall or composition.

> **Occupancy is a property of the ADDRESS, not of the knowledge.** Those
> coincide only when addresses are per-fact, which is what the families task
> does and what kinship does not.

### What this bounds

Decision 148 is entitled to say **"the gate knows which addresses it has
written"**. It is NOT entitled to say "the gate knows what it knows", which is
the same claim only on tasks that address by fact. That distinction was going to
be made eventually and is cheaper made now than after it had been built on.

**K2 confirmed** — `inherit` matches `plain` to four decimals at both hop counts,
so decision 150's rail holds under `context_keys` pair keys as well as single
ones, which is a different key structure and was worth checking separately.
**K3 confirmed** — `indexed` loses 0.070 and 0.037, so the extra reads cost here
too and the gate is again what avoids the cost.

### And closure cannot ask this question at all

Worth recording so it is not built twice. Closure looked like the ideal test --
its docstring says an entailed fact's `key(S, O)` was never written -- but the
same file says the layout scores at the OBJECT position and *"a stated fact is
not recallable within its own sequence, which is correct and is why the stated
half is a floor rather than a second measurement."* **Every address is unwritten
at scoring time**, stated and entailed alike, so the gate has nothing to separate
and would defer everywhere. Checked before building, not after.

**So the gate has now met three tasks.** It fires selectively on families, never
on MQAR, never on kinship, and cannot be asked on closure. Every one of those is
consistent with a single sentence: it reports whether an address was written.

## 152. Chains cannot be asked either, and the reason is the interesting part

The gate's coverage sweep ends here, and not with a measurement. Building the
chains arm produced an immediate refusal from a guard that predates all of this:

    index_branches cannot be combined with hops > 1: the hop key is a softmax
    mixture of every token's row, so it names no concept and the index has
    nothing to look up. Note 044

**Chains at `hops=1` is not chains** — the task exists to require two. So the
experiment was deleted rather than weakened, and the finding is the
incompatibility itself.

### Why this matters more than the number would have

`inherit` needs `index_branches`, because "answer from the neighbours instead"
requires neighbours, and the content index is what proposes them. The index needs
a key that **names a concept**. A hop key is a softmax mixture over many tokens'
rows and names none.

So the read gate and the hop mechanism are **currently mutually exclusive**, and
they are the two mechanisms this project has for the two halves of the problem:

    the gate    knows when an address holds nothing, and looks elsewhere
    the hops    follow an address that holds a STEP toward the answer

Chains is exactly the case where the address is occupied by the first hop rather
than by the answer — the sharpest statement of decision 151's bound — and it is
the one case the two mechanisms cannot both be present for.

### What the coverage claim actually is now

    MQAR       gate never fires, costs nothing            decision 150
    families   gate fires selectively, and it works       decisions 148, 149
    kinship    gate never fires, costs nothing, is blind  decision 151
    closure    cannot be asked: no address is written at scoring time
    chains     cannot be asked: the gate and hops exclude each other

**Three tasks measured, two structurally unaskable, and both unaskable cases are
composition tasks.** That is not a coincidence and it should not be read as one:
composition is where the answer is not at any single address, and the gate's
whole vocabulary is about single addresses.

### What this puts on the table

Decision 151 said occupancy is a property of the address rather than of the
knowledge. This says the project's one mechanism for *reaching* knowledge that is
not at an address — the hop — cannot currently be combined with the one mechanism
that knows an address is empty.

**Making those two compose is now the concrete form of the next real problem**,
and it is a better-specified problem than "find something that reports
knowledge": it is *give the hop machinery a key that names a concept, or give the
index something else to look up.* Note 044 is where that argument already lives.

Nothing was measured here and nothing is claimed. The experiment file was
deleted rather than committed, because a file that cannot run is worse than no
file.

## 153. Half the gate CAN go where the index cannot — and it has nothing to say there

Decision 152 said the read gate and the hop mechanism exclude each other. That is
half true, and the half that is false is worth having.

**The gate needs two things and only one of them is blocked.** It needs a test
for *is this address empty*, and a source of neighbours to read instead. The
second needs a key that NAMES A CONCEPT, which is exactly what note 044's guard
refuses above one hop. **The first needs only a vector to hash**, and a hop key
is a vector. `AddressSketch` never required a concept name.

So `track_occupancy` is now separate from `index_prefer` and exposes the sketch
as `model.occupied`. It runs at `hops=2`, which `index_branches` cannot, and
`tests/test_hops.py` and `tests/test_sketch.py` both still pass. **That is a real
structural gain and it cost a flag.**

### And then it has nothing to say on chains

Measured before building an experiment on it, which is the only reason this is
three paragraphs instead of a sweep. On 30 chain sequences, occupancy at the
symbol keys:

    chain START    0.893    zero on 0.0%
    chain MIDDLE   0.791    zero on 0.0%
    chain END      0.898    zero on 0.0%

**No separation at all.** The hoped-for signal was that a hop landing past the
end of a chain would find an empty address, which would give the halt gate an
exact feature for free instead of a learned one. It does not, and the reason is
structural rather than incidental: **writes happen at every position**, so
`key(c)` is written the moment `c` is followed by anything — the next separator,
in this case. Occupancy says "written". It cannot say "written with something
that continues the chain", because it is blind to the value **by construction**,
and that blindness is the whole reason it worked on families.

### The principle, which is what to keep

> **Occupancy is informative exactly where an address is READ BEFORE IT IS
> WRITTEN within the sequence.** On families a transfer entity is read at its
> query and written only afterwards, so it reads 0.0. On chains, kinship and
> MQAR every queried address was written earlier, so it reads positive and says
> nothing.

That is a sharper statement than decision 151's and it subsumes it. It also
predicts, rather than hopes, where the sketch will be useful next: **a task where
the model is asked about something before anything about it has been stored.**

### A probe of my own that was not sound, said plainly

The same script measured occupancy at symbols absent from the sequence and got
2.227 — higher than the written ones. That would be a false positive and would
contradict decision 150's M3. It is not reported as a result because the probe
is wrong: with `derived_keys=True`, `key_as` does not substitute a token the way
that row assumed. The three rows above use real positions and real keys and
stand. The fourth was deleted rather than explained away.

**No experiment file was committed**, because the measurement that decided it
took eight lines and the finding is negative.

## 154. The guard that blocks the other half rests on a premise that is measurably false

Note 044's guard, quoted in full because the whole of this entry is about one
clause of it:

    index_branches cannot be combined with hops > 1: the hop key is a softmax
    mixture of every token's row, so it NAMES NO CONCEPT and the index has
    nothing to look up.

The mechanism is real — `hop_key = weights @ self.wk`, where `weights` is a
softmax over the vocabulary. **The inference from it is not.** How nearly that
mixture names one token is an empirical question, and the code beside it already
half-answers it: *"`hop_sharpness` is the dial between the two, and high enough
approaches argmax."* Nobody had measured where the dial actually sits.

Chains, 12 sequences, cosine of every read key against every normalised `wk` row:

    sharpness 6.0 (the chains line's own)   top cos   margin to 2nd
      ordinary read                          1.0000          0.7173
      HOP 1                                  0.9612          0.6408
      HOP 2                                  0.9734          0.6605

    sharpness 25.0
      ordinary read                          1.0000          0.7173
      HOP 1                                  0.9855          0.6924
      HOP 2                                  0.9892          0.6982

**A hop key sits at cosine 0.96 to a single token's row, with 0.64 of clear air
to the runner-up, at the sharpness this task is actually solved with.** That is
not a mixture that names no concept. It names one, and the nearest-row decode is
well separated rather than marginal.

**The ordinary read at exactly 1.0000 is the check that makes the rest
readable.** The reads arrive three per position and the slot assignment was
inferred rather than known; a first slot that did not come back at exactly 1.0 —
it is literally a token's own key — would have meant the hop rows were measuring
something else. It did. That is the reproduce-a-known-number rule, and the first
version of this probe skipped it and reported a number inflated by the ordinary
reads it had failed to exclude.

### What this unblocks, and what it does not

**Unblocks:** the index has something to look up after all — `argmax(wk @
hop_key)`, the token the hop most nearly names. Decision 152 called the gate and
the hop mechanism mutually exclusive and named the fix as *"give the hop
machinery a key that names a concept, or give the index something else to look
up."* It turns out the first was already true and unmeasured.

**Does not:** the guard is not lifted here, because there is a real design
question underneath it that a measurement does not settle. The `index_branches`
block runs **once per position**, not once per hop, so combining them requires
deciding whether the index proposes neighbours of the *position's* concept or of
the *hop's landing* concept. Those are different mechanisms with different costs,
and picking one on the strength of a cosine would be the kind of move decisions
144 and 147 were both retractions of.

So this entry moves the blocker from *"impossible, by construction"* to *"a
design choice with a measurement behind it"*, and stops there deliberately.

**No code changed.** The premise was checked before anything was built on it,
which is the only reason this cost twenty lines instead of a sweep.

## 155. Note 050's task is refuted by its own rail, on the first run

`--links` was registered with three predictions and T5 was the dullest of them:
DIRECT, TRANSFER and EXCEPTION stay within 0.05 of their link-free values, because
otherwise every comparison across `--links` is confounded. It fired immediately:

    inherit, exceptions on    direct  transfer  exception
      without links           0.8475    0.4600     0.8625
      with links              0.1125    0.0375     0.1475

**Everything collapses to chance**, and `answers_a_stated_value` falls to 0.1044,
so the model is not even naming values from the sequence any more.

### The cause is the task design, and it is mine

A link is stated as `LINK here there`, where `here` and `there` are
**representative entities** of the two families. The store binds the previous
position's key to the current value, so that writes **`key(here) -> there`**.

`here` is an ordinary entity. Its address is exactly where its own stated fact
lives. **The link overwrites the fact it was meant to be composed with**, for one
entity per family, and `corrective_writes` being off means the two superpose
rather than one replacing the other — so the damage spreads through every read of
that address.

I chose entity tokens deliberately and wrote the reason into the code: *"there is
no family token, and adding one would hand the model the grouping the task exists
to make it discover."* **That reason is wrong.** A family token used ONLY as a
link endpoint says nothing about which entities belong to which family — the
model still has to discover that from co-occurrence, exactly as before. I talked
myself out of the right design with an argument that does not survive being
stated plainly.

### What survives

Everything except the layout of the link fact:

- **The byte-identity rail holds.** 360 configurations, background streams and
  vocabulary identical to the pre-change generator. Decisions 143–151 are
  untouched, which is the property this was most at risk of quietly breaking.
- **The calibration holds.** The drawn links are statistically invisible to
  `ContentIndex` (smallest permutation p 0.414), and that is a property of where
  links are stated rather than of what the endpoints are.
- **T4 and the arm plumbing hold.** The fourth query kind, its deferral
  accounting and the `is_linked` flag all work; they were reading a store that
  had been corrupted upstream.

### What the redesign has to satisfy, stated so the next attempt is cheaper

A link endpoint must be a token **whose address is not also a fact's address**.
Three candidates, none built:

    family tokens         clean, and the objection above does not hold. Costs
                          `n_families` ids and a second indirection: reaching
                          the linked family's VALUE from its family token
    attribute tokens      already exist, already sit beside entities in the
                          index, and are never query subjects -- so the index
                          would propose them for free. Same second-indirection
                          problem
    a reserved endpoint   per family, written by nothing else

**The second indirection is the real design problem**, not the collision: whatever
names the linked family, the model still has to get from that name to a value,
and only entities have values. That is a third hop, and note 050 predicted the
task would need two.

**CORRECTION, same day, before anything was built on it.** The third hop is an
artefact of the entity-endpoint layout rather than of the task. Put the link on
ATTRIBUTE tokens and state each family's value at its attribute as well:

    QUERY entity   the gate fires -- the entity's own address is empty
                   the index proposes attrA, because entities already sit beside
                   their attributes in the background streams
    hop 1          attrA -> attrB
    hop 2          attrB -> the linked family's value

**Two hops after the index proposal, which is exactly what `hops = 2` supports.**
Attribute tokens are never query subjects and never carry an entity's fact, so
the collision that refuted the first layout cannot occur.

It also makes the instrument cleaner than the one designed. TRANSFER becomes
answerable through the attribute in ONE hop while LINKED needs two, so **the arms
separate by hop depth** rather than by whether a mechanism happens to be present
— which is a sharper axis than note 050 asked for.

**Still not built, and deliberately.** Stating a family's value at its attribute
changes what TRANSFER measures, and every TRANSFER number from decision 143
onward assumes it is answerable only through a sibling. That is a change to the
meaning of an existing column, which is exactly the class of thing this project
does not do without saying so first.

`family_links` stays in the tree, off by default and now documented as refuted,
because deleting it would also delete the byte-identity rail and the calibration
that both still hold.

## 156. Typing an address costs nothing, and at low load it pays

Note 051's A3 was the prediction that decided whether typed edges are affordable
at all, and it was run before the mechanism for that reason. Three seeds:

    axis 1 -- the same facts spread over more relation types, N FIXED
      load     r=1      r=2      r=4      r=8
        16   0.8333   0.8333   0.9375   0.9792
        32   0.6562   0.6771   0.7083   0.7604
        64   0.4896   0.4844   0.4791   0.4844
        96   0.3264   0.3507   0.3542   0.3507

**A3a CONFIRMED, in the opposite direction to the fear.** Typing never costs, and
at low load it PAYS: +0.146 at load 16, +0.104 at load 32, going from one
relation type to eight.

**The naive `1/r` worry had the mechanism backwards.** Note 035 measured
interference as `O(N * rho)` — `N` **writes** at mean key cosine `rho`. Typing
multiplies the address SPACE and adds no writes, so `N` is untouched. What it
does do is spread keys over more distinct pair-hashes, which LOWERS `rho`. The
formula that was the reason to fear typing is the reason it helps.

At loads 64 and 96 the effect washes out: capacity is saturated and every column
degrades together. So the honest summary is **free at the wall, better below it.**

### Axis 2, and it is not what its own docstring claimed

I wrote that axis 2 grows `N` with `r` — each subject stated under every relation.
**The code does not do that.** `subjects = load // relations` holds the total at
`load`, so `N` is constant across `r` there too. A3b is therefore **untested**,
and the docstring is corrected to describe what runs rather than what was meant.

What axis 2 does measure is worth having and is flat: re-using one subject across
many relation types costs nothing against spreading the same facts over many
subjects. **That is the D2 collision case**, and it says the collision decision
155 hit was never a capacity problem — it was purely an addressing one.

### A3d missed, and the reason matters more than the miss

`r = 1` at load 16 came back at 0.8333 against a registered rail of 0.90.

**The rail was a guess rather than a reproduction**, which is the actual defect.
This harness has never reproduced a known number, so **its absolute values must
not be quoted** — not here, not in ARCHITECTURE.md, not anywhere. Everything A3a
rests on is a comparison across `r` at the same seed, load and harness, which is
internally controlled and unaffected by the absolute level.

The standing lesson is that a harness earns trust by reproducing a number that is
already known, and this one was built without a candidate to reproduce. That is a
gap in the experiment, recorded rather than argued away.

### What it unblocks

Note 051's build order put A3 first because a `1/r` cost would have ended the
line. It does not. **A1 — does the collision disappear on decision 155's task —
is now the next thing**, and ARCHITECTURE.md rows D2, D3 and E4 are what it would
move.

## 157. Typed addresses fix the collision and do not follow the link — exactly as split

Note 051's A1, three seeds, and it separates two failures that looked like one:

    inherit, exceptions on   direct  transfer  exception   linked  defer_linked
      links, UNTYPED (155)   0.1325    0.0342     0.1175   0.0142        0.7642
      links, TYPED   (A1)    0.8333    0.4383     0.8150   0.1275        0.9933
      no links       (148)   0.8100    0.4350     0.8183       --            --

**A1 CONFIRMED.** Every column is within 0.05 of its link-free value —
+0.023, +0.003, −0.003. Decision 155's collapse to chance is gone, and stating
links now costs the existing columns nothing. **ARCHITECTURE row D2 moves FAILING
→ PASSING.**

The mechanism is the whole of it: with pair keys, `key(entity, FACT)` and
`key(entity, LINK)` are different addresses. Decision 156 had already ruled out
capacity as the cause, so addressing was the only candidate left, and it was.

### The layout change this needed, which was not in note 051

Turning on `context_keys` breaks the QUERY as well as fixing the link. The
question was `QUERY entity`, keying on `(QUERY, entity)`, while the fact wrote
`(FACT, entity)`. **Measured before building: cosine 0.0701 between them** — the
query would have read an address nothing ever wrote, and the arm would have
scored at chance for a reason with nothing to do with typing.

Kinship hit this first and its layout is the fix, so the question now ends
`... QUERY FACT entity` and the pair matches: **cosine 1.0000**. Decision 100
measured the wrong version of this at 0.020 against 0.713, which is the size of
the mistake avoided. **Only under `family_links`**, and the byte-identity rail
still holds across 280 configurations.

### And the LINKED column is the finding

**0.1275 against a chance of 0.125.** Exactly chance.

The gate is not the problem — it defers on **0.9933** of linked queries, so the
model correctly notices it holds nothing at that address. It then cannot follow
the link, because **the hop is untyped**: it reads `key(concept)`, never
`key(concept, relation)`.

> Typing the WRITE fixed the collision. Following a NAMED relation needs typing
> the READ, and that is a different change.

That is precisely the D2/D3 split note 051 proposed, arriving as two different
numbers in one run rather than as an argument. **D3 stays FAILING, now with
direct evidence rather than by inspection.**

### What it also settles

**Note 050's T1 is CONFIRMED**, and the instrument is real: `inherit` scores at
chance on LINKED, so the task genuinely requires something the model does not
have. **T4 CONFIRMED** at 0.9933 against a predicted 0.9. **T5**, which decision
155 refuted, now holds — with typed keys the existing columns do not move.

The next thing is D3: give the hop a relation. `argmax(wk @ hop_key)` names the
concept it landed on (154, cosine 0.96), so the missing half is which relation to
bind with it.

## 158. The hop can follow a named edge, and the guard that blocked it named the fix

ARCHITECTURE row D3. `hop_relation` binds a relation token into the hop's key, so
a hop reads `key(relation, concept)` rather than `key(concept)`. The concept comes
from decoding the hop's own softmax, which decision 154 measured landing at cosine
0.96 on a single row.

**`tests/test_typed_hop.py` is the whole result.** One sequence states two edges
about one subject — `IS_A SUBJECT THROUGH_IS_A` and `HAS_A SUBJECT
THROUGH_HAS_A` — and a cue whose ordinary read lands on the subject, so the HOP
is what has to do the work. The same sequence at the same position returns
`THROUGH_IS_A` or `THROUGH_HAS_A` **according to which relation the hop carries**.

That test could not have been written before typing. Untyped, both edges live at
`key(SUBJECT)` and a retrieval is their sum, so no setting of anything returns one
rather than the other.

### The guard predicted its own fix

`hops > 1` with `context_keys` was refused, and the refusal is worth quoting:

> *"hops re-encode a decoded token through Wk, a SINGLE-TOKEN key table, and
> context_keys makes the store's keys derive from (previous, token) pairs
> instead — measured cosine between the two is −0.069 … **A hop that constructs
> a PAIR key is the mechanism this needs.**"*

A typed hop constructs exactly that. So the guard is relaxed **only** when
`hop_relation` is set, and the reason cites the guard's own sentence rather than
overriding it. The `search_branches` route it points at (decision 123) remains the
other way to satisfy it.

### Two mistakes the tests caught, both mine

**`hops=1` takes no hop**, so the first version of the test had the ordinary read
answering and `hop_relation` inert — both settings returned the same token. The
`test_the_two_settings_disagree` case is what exposed it, and it exists precisely
because two tests passing for the wrong reason looks identical to two passing for
the right one.

**The addressing tests built a model they did not need**, tripping the guard.
They use only the key source.

### What this does NOT do, stated plainly

**The relation is fixed, not chosen.** `hop_relation` is a configuration, so the
mechanism follows a named edge and nothing decides which name. Note 051 §5 flags
choosing as unsolved for open queries, and decision 147 — where two hand-made
selection rules were both refuted — is the argument for not attempting a learned
chooser before the fixed one is shown to pay on a task.

**And 157's LINKED column is unmoved at 0.1275**, because the families task is
not wired to this yet. D3 moves FAILING → PARTIAL on the mechanism, not on a task
result, and the ledger says so.

## 159. The index proposes at the hop's landing concept, and only at dead ends

ARCHITECTURE row E4 -- the last FAILING row -- and John's option B, which he
chose. `index_at_hops` lets the content index propose neighbours where a HOP
arrived rather than only at the position it started from. Note 044 refused this
because a hop key "names no concept"; decision 154 measured that false at cosine
0.96, so argmax(weights) names where the hop landed and the index can look it up.

JOHN'S CONCERN WAS THE DESIGN CONSTRAINT, NOT AN AFTERTHOUGHT. He asked how this
is stopped from exploding -- proposing b neighbours at every hop is b ** depth,
27 reads at three hops with three branches. The answer is the gate we already
have: FAN OUT ONLY WHERE THE ADDRESS HOLDS NOTHING. A chain that is finding what
it needs never branches; branching happens at dead ends, which is exactly where
it is worth paying for. So it is built into the mechanism rather than bolted on,
and `track_occupancy` is required because "holds nothing" is the sketch's
question -- answering it by norm is what decision 147 refuted.

tests/test_index_at_hops.py measures both properties:
  - a chain reaches an answer THROUGH a dead end that it provably cannot reach
    without the fan-out (the control runs first, so the positive means something)
  - the fan-out costs 1 extra read on a chain with no real dead end, where an
    ungated version would cost 56

A AND B ARE ALTERNATIVES, NOT ADDITIVE, and the cost test is what found that. It
measured 56 reads against 28 -- exactly double -- because turning on
index_at_hops was also running the position-level fan-out. Option B means
similarity is applied where the chain HAS GOT TO rather than to what it was
asked about, so the position-level block is now skipped. The wrong version would
have doubled the wire cost and made the claim above false.

THREE THINGS THE TESTS CAUGHT, ALL MINE:
  - with hops=2 the chain takes a SECOND hop after arriving and wanders off the
    answer. That is E3's question (when to stop) rather than E4's, so the test
    pins the endpoint to isolate the row being measured
  - the index returned similarity 0.0 between the two tokens it was supposed to
    relate. TWO TOKENS THAT ONLY EVER SEE EACH OTHER DO NOT BECOME SIMILAR --
    they become each other's CONTEXT. families.py documents the same trap and
    its layout is the fix: both must sit beside a SHARED third token
  - the cost test asserted EXACTLY zero overhead and measured 29 against 28. The
    extra read is real and correct -- at the start of a sequence the store is
    empty, so the earliest position genuinely is a dead end. The assertion was
    the wrong claim, not a failing mechanism, and it now asserts what matters:
    cost tracks DEAD ENDS rather than DEPTH

E4 moves FAILING -> PARTIAL. Mechanism only: it has never run on the linked task,
so 157's LINKED column at 0.1275 is unmoved. NO FAILING ROWS REMAIN -- 8 passing,
7 partial, 0 failing, 4 untested, 4 claimed -- and what the partials share is
that each is a mechanism shown to work in isolation whose value on a task is
still unmeasured.

## 160. "Alternatives, not additive" was too strong, and it blocks the run that matters

Found while wiring the LINKED run — the sweep that would tell us whether today's
mechanisms are individually correct and collectively useless. **They cannot
currently be combined**, and the reason is a decision I made an hour earlier.

The LINKED path needs both gates, at different levels:

    position   `key(FACT, entity)` is empty -> ask the entity's SIBLINGS.
               That is `index_prefer="inherit"`, decision 148
    hop        the chain dead-ends -> ask neighbours of WHERE IT LANDED.
               That is `index_at_hops`, decision 159

Decision 159 made `index_at_hops` skip the position-level block entirely, so
`inherit` has no neighbours to defer to and never fires. **The two mechanisms
this line spent the day building cannot run in the same model.**

### Why I made that call, and where the reasoning went wrong

The cost test measured 56 reads against 28 and I concluded A and B were
alternatives. **That was the right fix for the wrong reason.** The doubling came
from the position-level block running as decision 146's UNGATED summing — it
reads `index_branches` neighbours at every position regardless of whether
anything is needed.

`inherit` is not that. It is gated: it defers only where the token's own address
is empty AND a neighbour's is not. **Two gated mechanisms compose without
doubling anything**, because each fires only at a dead end and a position is
rarely both kinds of dead end at once.

So the honest statement is narrower than the one in decision 159's config
comment: **ungated summing and hop-level fan-out are alternatives. Two gated
mechanisms are not.**

### What this does not change

The cost claim in decision 159 stands — `tests/test_index_at_hops.py` measures 1
extra read against an ungated 56, and that test does not involve `inherit`. What
changes is which configurations are reachable, not what the fan-out costs.

### Not fixed here, deliberately

The fix is to make the skip conditional on the position-level mechanism being the
ungated one rather than on `index_at_hops` being set. That is a read-path change
and it wants its own test — specifically one that measures the read count with
BOTH gates on, since the whole argument above is a cost argument and I have just
been wrong about a cost argument once today.

**Recorded rather than patched**, because a hasty fix to the read path is how
decision 74 happened, and because the finding — that the day's two mechanisms
have never been in the same model — is worth more than the patch.

## 161. `inherit` was never read-gated, and nobody had counted its reads

Decision 160 said two gated mechanisms compose without doubling. **A test
measuring it came back at exactly double, and the reason is that the premise was
false.**

`inherit` is gated in its DECISION and not in its READS. The position-level block
reads every neighbour the index proposes and *then* decides whether to defer. So
it paid the full fan-out at **every position**, including every position whose
own address was occupied and which therefore could not possibly defer.

**Decision 148 never counted reads.** It measured accuracy, and the mechanism has
carried an unmeasured C1 cost since the day it was built.

### The fix is the same gate, applied earlier

If the token's own address holds anything, skip the fan-out entirely. This is
**behaviour-preserving rather than an approximation**: `defer` requires
`here <= 0.0`, so a position whose address is occupied never defers whatever the
neighbours hold. The reads were pure cost.

**Reproduced rather than assumed:** decision 148's cells come back at 0.8100 /
0.4350 / 0.8183 — identical to four decimals across three seeds. That check is
the reason this is a decision entry and not a revert.

### Twice in one day, on the same kind of claim

Decision 159 concluded A and B were alternatives from a read count and was wrong
about why. Decision 160 concluded gated mechanisms compose and was wrong about
whether `inherit` was one. **Both were cost arguments made from reasoning rather
than measurement, and both were caught by writing the measurement down.**

The standing lesson is narrow and worth keeping: **this project has never had a
read-count rail.** Accuracy is measured everywhere, and the wire cost that C1 and
G4 both turn on is measured nowhere. `tests/test_index_at_hops.py` now counts
reads in three places, which is three more than existed this morning.

### What is now unblocked

`index_prefer="inherit"` and `index_at_hops` can be set on the same model, which
is what the LINKED run needs. That run is the one that would tell us whether the
day's mechanisms are individually correct and collectively useless, and it is no
longer blocked.

## 162. The LINKED run is still not informative, and the reason is one relation per model

I recommended the LINKED run as the next thing twice, and checking the path
before running it says it would come back at chance for a reason that has nothing
to do with the mechanisms under test.

**The path the task needs**, with typed keys:

    key(FACT, entity)   empty -- the gate fires, correctly
    key(LINK, rep)      -> rep of the linked family        <- hop 1, relation LINK
    key(FACT, rep')     -> the linked family's value       <- hop 2, relation FACT

**Two hops under two DIFFERENT relations.** `hop_relation` is a single
configuration value used by every hop, so the chain can follow LINK-then-LINK or
FACT-then-FACT and never LINK-then-FACT.

Decision 158 named the limit as *"the relation is fixed, not chosen"*. That was
true and it was not the whole of it. The sharper statement is **one relation per
MODEL, not one per hop** — and it is the second that blocks this task. Even a
correct chooser would not help until a hop can carry its own relation.

### Why this is recorded instead of built

The fix is small in code — `hop_relation` becomes a sequence indexed by hop
depth. It is **not** small in what it implies, because "which relation at which
depth" is a schedule, and a fixed schedule is a fitted constant wearing a
mechanism's clothes unless the task supplies it. Note 052 §2 already lists
choosing-the-relation as a cascading decision with three options, and this says
the decision is more urgent than that note implies: **it is not an optimisation,
it is what the composition task requires.**

### The correction, plainly

I recommended running LINKED as a cheap validation of the day's work, twice, and
that recommendation was wrong. Everything the run needs exists **except** the
ability to vary the relation across hops, and I did not check that before
proposing it. The check cost ten minutes and the run would have cost an hour and
produced a chance-level number that looked like the mechanisms failing.

**What the day's mechanisms have actually been shown to do stands unchanged** —
each works in isolation, with a unit test, and none has a task number. That was
true before this entry and it is still true. What has changed is that the reason
is now named: **not one of them is wrong, and the task needs a fourth thing
nobody has built.**

## 163. John's rulings on the four cascading decisions

Note 052 laid out four decisions whose blast radius makes them cheaper to settle
before more is built on the current answer. **John ruled on all four**, and they
are recorded here rather than left in conversation so the next session inherits
them as settled.

### 1. Discrete surfaces — ACCEPTED

> *"I agree with you on the exact addressing using a quantizer."*

**Target modalities: video, audio, text and images.** PDFs come in as text.

**Off-the-shelf where it works, our own where it does not** — and John named both
directions himself: the distributed setting may rule out a stock solution, and
there may equally be something better for this case than what exists.

Three things that follow, from note 052 §1 and worth keeping with the ruling:

- **the C1 constraint does not bite here.** A quantiser is preprocessing, run
  once per input at the edge, before anything reaches the store. It is not in the
  learning loop, so stock encoders are genuinely available
- **candidates exist for all four.** Residual-VQ audio codecs, the VQ-VAE/VQGAN
  family for images, frame tokenisers plus temporal handling for video. Text is
  already discrete
- **the cost to decide deliberately** is that a stock tokeniser is a large
  pretrained model in the pipeline. It touches nothing the learner does, but
  *"our system plus a pretrained encoder"* is a different claim from *"our
  system"*, and it is better named now than in a write-up

**The risk that matters is the silent one:** a bad quantiser merges two things
that should stay distinct, and this architecture cannot recover from that,
because it will then address them identically.

### 2. Where the relation comes from — LAYOUT FIRST, TRY-ALL-AND-GATE NEXT

John: *"I like your try-all-and-gate… as potentially the actual end solution"*,
and agreed to measure the layout version first.

**Decision 162 has since made this more urgent than note 052 said.** It is not an
optimisation to schedule — a hop cannot currently carry its own relation, so the
composition task is unreachable without it. And 162 split the question in two:
*which* relation, and *whether a hop can carry its own*. **The second blocks
before the first matters.**

### 3. What an "answer" is — EXPERIMENT, AFTER THE OTHERS

Accepted as a real gap. This is ARCHITECTURE row F3 and it is the one that
reaches backwards: every task and every accuracy number assumes a single answer
token.

### 4. Store persistence across sequences — DEFERRED

Deliberately, with the trigger named: nothing currently needs cross-sequence
memory, and it becomes urgent the moment a task does.

### And the ordering principle behind all four

> *"I would definitely like to shift over to doing these things because they have
> such a large blast radius first… so that the tweaks that start happening are
> gonna stick around rather than be completely wasted time because of the
> architecture changing underneath it."*

**With one correction, which is on the record because it changes what the
principle implies here.** Discrete surfaces is the option under which the
architecture does *not* change — that was the argument for it. So multimodality
is additive rather than sweeping, and the decision on this list with real blast
radius over existing measurements is **§3, not §1.**

## 164. A hop can now carry its own relation, and one seed nearly hid it

Decision 162 named the blocker and declined to build it: `hop_relation` is one
value per MODEL, so a walk follows LINK-then-LINK or FACT-then-FACT and never
LINK-then-FACT, which is the path the linked-families task needs. John's ruling
in decision 163 §2 was layout first, try-all-and-gate next; this is the layout
version, and it is what the composition task requires rather than an
optimisation.

**What was built.** `hop_relations` is a tuple indexed by hop depth, mutually
exclusive with `hop_relation` and off by default, so no earlier number moves. Both
use sites — the hop key and the dead-end neighbour under `index_at_hops` — go
through one helper, `_relation_at(depth)`, rather than reading the config twice.

    LINK then FACT      -> LINKED_VALUE   the linked family's value
    LINK then LINK      -> THIRD          arrives, cannot read the value
    hop_relation=LINK   -> THIRD          the pre-162 mechanism, its best setting

Stable across seeds 0, 1 and 2. The third row is the claim held as a test rather
than an argument: the old mechanism reaches the linked family's representative and
**stops there**, because reading its value needs a different relation.

### The part worth recording, which is how close this came to being an artefact

The first version of the test asserted that the wrong walks do NOT reach
`LINKED_VALUE`. `hop_relation=FACT` starts at `key(FACT, entity)`, which 162
describes as empty and the gate firing correctly — so its answer is whatever noise
decodes to, and **on seed 0 that noise decoded to exactly the right token.** The
test failed, which is the only reason it was looked at; across seeds 0, 1 and 2 it
returns OTHER, REP and LINK, so it would have passed or failed by seed.

The fix was not a wider bound. The layout grew a **third family** so that
`key(LINK, OTHER)` is written too, which gives the LINK-then-LINK walk a
determinate destination. The discriminating comparison is now positive on both
sides — same first hop, different second hop, two different named tokens — and
`hop_relation=FACT` is deliberately **not** asserted on, because asserting on
noise is what this rewrite removed.

> This is CLAUDE.md rule 10's "bounds so wide they admit the broken case" arriving
> from the other direction: an assertion narrow enough to be right and resting on
> a draw. The tell was that the *predicted* outcome and the *guaranteed* outcome
> were the same sentence.

### What is deliberately NOT claimed

**The relation is still fixed, not chosen.** A schedule is a fitted constant
wearing a mechanism's clothes unless the task supplies it — 162's own words, and
they apply to this entry. `hop_relations` is the instrument that makes the
composition measurement *reachable*; it is not a candidate for the final read
path. Note 052 §2's try-all-and-gate is, and decision 163 §2 has the ordering.

**No task number.** The LINKED run is now unblocked and has not been run.

### How to undo it

Remove the field, the helper's first branch, and `tests/test_hop_schedule.py`.
`hop_relation` is untouched and every pre-2026-07-29 number is measured with both
off. `tools/mutate.py` carries
`every-hop-follows-the-FIRST-scheduled-relation`, which reverts the depth index
and is caught.

### And a documented claim that measurement contradicts

CLAUDE.md said the mutation harness is "85 mutations at roughly fifteen seconds
each. Sharded it is about two minutes a job instead of twenty in one." The full
run on `57d8112` — the first to complete, because nothing superseded it — was
**169 mutations across six shards taking 18 to 35 minutes each**, so serial time
is about two and a half hours rather than twenty minutes. All 169 were caught.
Rule 5: the document is corrected rather than softened. It matters practically,
because the old figure is what would make a local full run look affordable.

## 165. The ruler for a set-valued answer, built before anything produces one

ARCHITECTURE row F3 and the live question in STATE: **nothing in this project has
ever scored a multi-token answer.** `openplexus/answers.py` is the measurement
convention for one, and it exists before any mechanism that emits such an answer
because that ordering is what GOALS §4 asks for and what note 050 established —
the blocker is the instrument, not the mechanism.

### The trap this is built against

**A model that emits EVERYTHING scores perfect recall.** Recall on a set answer is
monotone in the wrong direction: it improves as the answer gets less useful, and
it improves fastest for the laziest possible mechanism. Precision is what decision
148's emptiness gate is supposed to buy, so precision is exactly what a set score
must not be allowed to hide.

`SetScore` therefore carries `exact`, precision, recall and F1 together, and the
reportable numbers are `exact` and `f1`. `SetScoreSummary` carries `mean_size`
beside `mean_truth_size`, which is the over-emission tell: a mechanism buying F1
by guessing more is invisible in a headline and obvious there.

**`EmittingEverythingMustNotScoreWell` is a standing falsifier rather than a unit
test.** Against a 2-of-8 answer it records recall **1.000** and F1 **0.400**. If a
change ever makes that arm look good, the change is wrong however good the
headline reads.

### The reproduction gate

Every accuracy in this repository is `predicted == truth` over query positions.
`exact` on singleton sets is that same comparison, and `single_token_accuracy`
recovers it and **raises** on anything else — because averaging a set score into a
column labelled "accuracy" is how a number stops meaning what its heading says.
On singletons `exact`, precision, recall and F1 are all the same quantity, so a
task rewritten to this convention cannot report a different figure depending on
which column it is read from. Decision 138 is why this is a function rather than a
promise.

### Refusals, chosen over defaults

An empty TRUE set raises: scoring it 1.0 for an empty prediction would let
questions with no answer raise the mean. An empty PREDICTION scores zero rather
than raising, because declining to answer is a real behaviour (row C4). An empty
summary raises, because a zero there is indistinguishable from a mechanism that
scored zero — rule 8's accumulator reporting its own initial value.

### Dependency-free, and the test for it was wrong first

This is the ruler, so it takes no dependencies (note 007). The test asserting that
searched the source for the string `import numpy` and **failed on the module's own
docstring**, which contains the sentence "the ruler does not import numpy". It now
parses the file with `ast` and inspects import nodes, with a companion asserting
`dataclasses` IS found — because "this set does not contain numpy" passes
trivially when the parse found nothing.

> Two tests in two commits now, both written to check a claim and both initially
> answering a nearby question instead. 164's asserted on a noise draw; this one
> asserted on prose. The pattern is the same and worth naming: **the cheap version
> of a check tends to match the claim's WORDS rather than its content.**

### What is deliberately NOT claimed

**Row F3 stays UNTESTED.** A ruler is not a measurement. Nothing in this project
emits a set answer yet, and the next step is wiring `families.py` to a set-valued
query — John's ruling, families first and closure after.

### How to undo it

Delete the module and `tests/test_answers.py`. Nothing imports it yet, so no
result moves. `tools/mutate.py` carries
`the-set-score-reports-RECALL-as-F1`, which replaces the F1 formula with recall
and is caught by the falsifier above.

## 166. A question a single token cannot answer

`families.py` gains `set_queries`, off by default. The question is **"what values
were stated about this entity's family"**, and the answer is every distinct one:
the family's own value and its exceptions.

**This is not only a scoring change, and that is the reason to build it here rather
than anywhere else.** `families.py`'s docstring already states why EXCEPTION
exists:

    a system that cannot hold "birds fly, but not this one" does not
    understand birds

A one-token answer can report *birds fly*, or *not this one*, and never that both
are true. **The task has contained that conjunction since decision 144 and has
never been able to ask for it.** So the first set-valued question is not a new
capability bolted on; it is the existing task finally able to pose the question it
was designed around.

### The layout, and what it deliberately does not do

`ASK_ALL entity`, two tokens, and **no answer token follows.** A set has no single
next token, so the truth lives in `Sequence.answer_sets` and
`set_query_positions` is kept **separate from `query_positions`** — a script
scoring these through `roll(tokens, -1)` would compare the model against whichever
token happened to come next, and for the last set query against nothing at all.
Separate lists make that impossible rather than discouraged.

**It is therefore a read-only probe.** Training still learns from the stated facts
and the single-token questions exactly as before, so this changes what can be
*asked* without changing what is *learned* — which is the cheapest possible way to
reach row F3 and keeps every earlier number comparable.

`ASK_ALL`'s id is **conditional**, like `LINK`'s, so `config.ask_all` is the only
correct way to read it and reading it while `set_queries` is off raises. A
module-level constant for a conditionally-reserved marker is a marker in one
configuration and a real entity in another.

### The refusal that matters

**`set_queries` requires `exceptions_per_family >= 1`.** Without an exception every
member of a family states the same value, every answer set is a singleton, and the
measurement is the single-token one under a new column heading — **and it would
score well**, because a mechanism emitting one token is then exactly right. That is
a result rising for a reason having nothing to do with the mechanism under test,
which is the failure this project's standards exist to catch.

`tools/mutate.py` carries `the-set-answer-is-a-singleton-after-all`, which
reintroduces it past the guard by collapsing the set to its minimum. It moves
`exact` **up**, which is the direction that invites no checking. Caught.

### Measured, at the ruler rather than at the model

Nothing has run a model on this. What is checked is that the task and decision
165's ruler fit, including the falsifier:

    the true set                    exact 1.000
    the family value alone          precision 1.000, recall < 1, exact false
    every value in the alphabet     recall 1.000, exact 0.000, mean F1 < 0.5

The middle row is the single-token mechanism's *best possible* behaviour scored
under the set convention: it names the rule and misses the exception. The bottom
row is the standing falsifier carried out of the ruler's own tests and into the
task's.

### What is deliberately NOT claimed

**Row F3 stays UNTESTED.** A task that can ask the question and a ruler that can
score it are not a measurement. **Nothing in this project emits a set yet** — that
is the mechanism, and it is next: a gated walk over index-proposed siblings, where
decision 148's gate supplies precision and `ContentIndex` supplies the candidates.

**One caveat on the termination story, corrected before it is built on.** The gate
gives the answer's *size* without fitting anything — a sibling whose address was
never written reads exactly 0.0 and is not emitted. But the *candidate list* is
bounded by `index_branches`, which is a fitted constant. So "the gate terminates
the walk for free" is true of precision and not of enumeration, and the honest
version is that emitting is unfitted while proposing is not.

### How to undo it

Set the flag off; the stream is byte identical and
`tests/test_family_set_queries.py` asserts that against decisions 143-151's
layout rather than intending it.

## 167. The first set answer, and the constant it rests on

`LocalAssociativeMemory.answer_set` reads the entity's own address and its
content-index neighbours', **skips every address the occupancy sketch says was
never written**, and returns the decoded values as a set. Row F3 has a mechanism.

**It COLLECTS where decisions 146 and 147 tried to CHOOSE, and that is why it
works at all.** 146 found that reading neighbours through the index can only
average rather than select; 147 refuted both obvious rules for picking a winner
among them. **Neither objection applies to a set answer, because nothing has to be
selected** — so the mechanism that was refuted for a one-token answer is the right
shape for this question. That is worth stating plainly: this is not a new
mechanism, it is a refuted one whose refutation turned out to be about the
question rather than about it.

### The result, and the headline is the second row not the first

    exact, by family_size against `branches`.  n = 12 per cell, six seeds

                   1       2       3       4       5       6       7       8
    size 3     0.250   1.000   0.250   0.250   0.167   0.167   0.000   0.000
    size 4     0.083   0.833   1.000   0.500   0.083   0.000   0.000   0.000
    size 5     0.667   0.833   0.917   1.000   0.250   0.083   0.000   0.000
    size 6     0.500   0.500   0.500   0.667   0.917   0.333   0.083   0.000

**The peak sits at `family_size - 1` in every row, and falls off sharply on both
sides.** So the reportable finding is not 1.000. It is that **the enumeration bound
must equal the group's size, and the model is not told the group's size and cannot
currently discover it.** A single number quoted from the diagonal would be a
measurement of a constant supplied from outside the model.

This is decision 166's caveat, measured rather than argued, and it arrived one
commit after being written down — which is the only reason the 1.000 was not
reported as the result.

### Why the gate cannot fix it, stated precisely

The gate filters **emptiness, not irrelevance.** Neighbours beyond the family are
other families' entities, and their addresses *are* written — they have stated
facts of their own. So the gate has nothing to object to, and over-enumeration
costs precision directly:

    gated,   branches 3     exact 1.000   precision 1.000   size 2.00
    gated,   branches 8     exact 0.000   precision 0.530   size 3.90
    UNGATED, branches 3     exact 0.200   precision 0.733   size 2.80
    UNGATED, branches 8     exact 0.000   precision 0.453   size 4.70

**The gate does act** — 0.733 to 1.000 at the matched bound, and it removes 0.8
spurious values per answer. It is simply the wrong instrument for the other
failure. `the-set-answer-emits-every-candidate` is the mutation, and note that
removing the gate raises RECALL, which is precisely why decision 165 refuses to
report recall alone.

### What is deliberately NOT claimed

**Index purity was 1.000 in every cell**, so the grouping was effectively an
oracle. This measures the collecting read, not the composition under an imperfect
grouping — and `families.py` was calibrated to make the grouping recoverable, so
that is the intended condition rather than a flaw. It does mean the number says
nothing about what happens when the index is wrong.

**Row F3 moves from UNTESTED to PARTIAL and no further.** A mechanism that needs
the answer's size handed to it has not answered from awareness.

### The next problem, and no available option makes it free

The enumeration bound is now the blocker. `grouping.cluster` is the obvious
alternative — a cluster's membership is determined by the data rather than by a
per-query `k` — but **it takes a `k` of its own**, the number of clusters. So it
converts a per-query constant into a global one, which is a real improvement in
kind and **is not the same as unfitted**. Saying otherwise would be the third time
this line has claimed something was free before checking.

### How to undo it

Delete the method and `tests/test_answer_set.py`. `self._final` is assigned on
every run and read by nothing else, so removing it changes no result;  it is
deliberately not `carry_store`, which feeds a store into the next sequence and
would change what the model learns from.
