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
