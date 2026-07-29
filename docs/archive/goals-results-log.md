# Results log — ARCHIVE, lifted out of GOALS.md

> **This is history. Do not read it for the current state of the project.**
> For what is true now, read [DECISIONS.md](../../DECISIONS.md) — the option tree.
> It replaced STATE.md and the append-only log on 2026-07-29.

**What this is.** For most of the project's life this narrative sat at the bottom
of `GOALS.md` under a heading reading `*Status:*`. It grew to 405 lines of running
commentary — gate verdicts, retractions, corrections of corrections — inside a
document whose opening line says *"nothing below is a measurement."*

It is moved here rather than deleted for three reasons. The retractions in it are
the useful part and several are cited elsewhere. It is the record of how G0–G5
were actually reached, which a table of ✅ marks cannot carry. And several
sections quote a refuted sentence deliberately, because that sentence gated how
everything under it was read.

**What replaced it.** `GOALS.md` now states intent and the constraints only, and
carries the gate ladder with a verdict per gate and nothing else. Measurements
live in the sweep records under `experiments/sweeps/`, the reasoning lives in
`docs/notes/`, and the decisions live in `DECISIONS.md`.

**The specific drift this document is evidence of.** It presented `T^0.67` as the
answer for minimum machine width while quoting `T^0.82` for the same quantity two
paragraphs later, with the consequences still computed from the older figure.
Nothing was wrong in either sweep; the document grew a second answer and kept the
first. `tests/test_archive_consistency.py` still guards the figures below.

**Everything here is measured under the pre-amendment C1** (no global state at
all) and against MQAR or `reward_recall`, not the relational tasks. Both changed.

---

*Status: **G0, G1 and G2 passed.** A rule with no backward pass and no softmax over
positions — every update a product of two signals at the synapse — solves MQAR
at 8/8 seeds, against 0.180 for a frozen substrate and 0.344 for a one-line
heuristic. **The price of locality is roughly 4–6× in width** (crossing at
48–64, against attention's 8–16). Unexpectedly, the local rule is **graded**
where attention was all-or-nothing, because superposition interference eases
continuously while circuit discovery does not — which makes it better shaped for
a learning rule than the thing it replaces. **G2 is passed too**: below a stated delay bound the learned weights are
**bit-identical** to a run with no network at all — 6/6 seeds, including every
event delayed by up to 64 steps on 96-step sequences. That exactness is bought by
emission-time indexing rather than by the learning rule, so it holds for any rule
behind it. Two costs measured: loss *compounds* (a binding needs its pair and its
query to both survive, so accuracy falls as a product not a fraction), and a
buffer deep enough for intercontinental lag is deeper than these sequences — the
system batches rather than streams, so "latency is free" holds for throughput and
not for time-to-first-response. **G3 is passed**: half the substrate removed permanently, mid-training, recovers
to 0.924 against a 0.992 baseline within a few epochs; a quarter costs 0.006.
Nothing persists but the readout, so a departing machine takes capacity rather
than memories.

**One number is retracted.** g3-02 found the width curve — and therefore the
"locality costs 4–6× in width" figure — was substantially measuring the frozen
projections' initialisation scale, a constant chosen once and never swept: a
native width-32 model scores 0.263 at scale 1.0 and 0.960 at 0.71, nothing else
changed. **g1-08 has now re-measured it with both arms tuned, and the honest price is
4.0× in width** — the local rule crosses at 32 and attention at 8, each at its
own best scale. Tuning both made the price *worse*: like-for-like at the old
untuned settings it was 3.0×, so the retracted figure was not conservative but
unfounded, and landing near the right answer was luck.
**G5 is resolved, and it fails — gently, and with a number.**
[g5-01](../../experiments/sweeps/g5-01-does-scale-help.txt) fixed each machine at 16
dimensions and grew the network. Machines compound at 48, 96 and 192 steps — a
fitted exponent of 0.69 against g1-10's width exponent of 0.37, so partitioning
taxes scale but does not break it. At **384 steps they stop compounding**:
doubling from 8 to 16 machines buys 0.021, and the curve saturates around 0.79
without reaching the bar. The fit predicts 5.6 machines there; 16 are not enough.

The decisive comparison is against the unpartitioned rule. g1-13 measured it
crossing at width 46.8 on 384-step sequences and reaching 0.994 at width 64.
Sixteen machines here total 256 dimensions. **256 dimensions in one piece solve
the task; the same 256 split sixteen ways reach 0.769.** So the wall belongs to
the partitioning, not to the local rule — which makes it a question about the
decomposition rather than about the mechanism, and the most likely way it is
wrong is that 16-wide machines are simply too narrow (untested; g4-01 found the
penalty shrinking sharply with width).

Checked before believing: every arm converged (no cell moved more than 0.014 when
the budget doubled), and an independent sweep agrees — g4-01 got 0.741 where this
got 0.748 on the same configuration.

**Then [g5-02](../../experiments/sweeps/g5-02-how-finely-can-it-split.txt) withdrew the
verdict.** Holding total width at 256 and varying only how finely it is cut, at
seq_len 384 **eight machines of 32 dimensions score 0.999 pooled and 0.911
alone** — both clear. g5-01 had pinned machine width at 16, which is below the
minimum at that length, so its wall was a property of the chosen machine size
rather than of partitioning. What survives is narrower and still true: *machines
cannot be arbitrarily small, and the floor rises with sequence length.*

How fast it rises is the open question, and g5-02 does not answer it: minimum
machine width comes out at `T^0.50` with a resolution of `±0.50`, a range that
contains g1-10's `T^0.37`. At the low end the usable machine count is constant
and partitioning scales freely; at the high end it falls as `T^-0.63` and the
approach is bounded. **Those are opposite conclusions and the grid separates them
not at all** — factor-of-two width steps cannot measure a quantity whose
interesting range is a factor of two wide.

**[g5-03](../../experiments/sweeps/g5-03-a-finer-ruler.txt) resolves it.** Moving the
total width from 256 to 240 — a number with divisors where a power of two has
none — and adding `seq_len 48` at the cheap end took the resolution from ±0.50 to
±0.14. Minimum machine width grows as `T^0.67`, range [0.53, 0.81], which excludes
0.37.

**[g5-04](../../experiments/sweeps/g5-04-how-far-does-pooling-stretch.txt) later refined
that to `T^0.82`, range [0.61, 1.03]**, fitting five located rows against g5-03's
four by adding `seq_len` 128 and 256. The intervals overlap, so this is a
refinement rather than a contradiction — but **0.82 is the current figure and 0.67
is superseded.** The numbers below are computed from 0.82; an earlier version of
this document quoted the consequences of 0.67 and was not updated when the
measurement moved, which is exactly the drift the record-keeping standard exists
to catch.

So the usable machine count goes as **`T^-0.45`**: to handle a problem ten times
longer you need machines about **6.6× wider** while total capacity grows only
2.3×, and the number of machines you can split across falls to roughly **a
third**.

That is G5's refutation condition met. It is not a cliff — doubling the problem
costs about a quarter of the machine count — but for a goal whose whole premise is
that machine *count* is the elastic quantity and machine *size* is fixed by what
people already own, the elastic quantity is the one that stops helping.

**[g5-04](../../experiments/sweeps/g5-04-how-far-does-pooling-stretch.txt) measured the
pooled criterion, and it is not the escape it looked like.** Pooling is the better
option at every length — its advantage runs 13×, 13×, 3.3×, 2×, 1.25× as sequences
lengthen — but it *degrades roughly twice as fast*: exponent **1.94 [1.36, 2.53]**
against **0.82 [0.61, 1.03]** for a lone machine. It postpones the wall rather
than removing it, and the earlier description of it as "the most promising
direction" is withdrawn.

**In the terms that matter — how small can a node be:** at `seq_len 128`, a machine
holding **one number** is enough; 240 of them pool to 0.978. Node size is not the
problem today. The growth rate is: by 384 steps the same arrangement needs 20–24.

**[g7-02](../../experiments/sweeps/g7-02-tiny-nodes-and-clusters.txt) then showed what
happens if that storage is selective: sequence length stops being a difficulty
dial at all.** With an oracle gate, devices holding *one number each* score
identically — to three decimals — at 96, 192, 288 and 384 steps, needing a cluster
of 8 (conservatively 32, where every seed clears the bar) rather than a network of
hundreds. Ungated, the same 240 devices reach 0.572 at 384 and fall with length.

The mechanism is the arithmetic below: with the gate on, memory holds `2·n_pairs`
bindings whatever the length, because the filler is never written. **The gate is
an oracle and this is a ceiling** — [note 010](../../docs/notes/010-tagging-and-capture.md)
works through the biological mechanism that would replace it and concludes MQAR
cannot test it, because the only event separating a pair from filler is the query,
which arrives too late and never recurs.

**And [g7-03](../../experiments/sweeps/g7-03-how-to-spend-a-machine.txt) found the gate
removes a second problem as well as the first.** A machine holding `C` dimensions
can run one node of width `C`, `C` nodes of width 1, or anything between — and
which it picks is the deployment decision. **Gated, it does not matter**: the
largest gap between the best and worst allocation at any capacity is **0.031**,
and sixteen dimensions suffice however divided. **Ungated it is worth 0.425**, and
the rule is as few and as wide as possible — one node of 64 scores 1.000 where
sixty-four nodes of 1 score 0.583.

So selective storage removes both the sequence-length scaling *and* the allocation
problem, which means heterogeneous hardware needs no policy. The caveat: the gated
arm saturates at capacity 16, so that comparison lives in a narrow band below it.

> ## THESE THREE ARE CEILINGS, NOT FINDINGS — measured, not suspected
>
> **[g8-01](../../experiments/sweeps/g8-01-a-gate-without-an-oracle.txt) asked how much
> of the oracle's advantage an implementable mechanism recovers. The answer is
> none.** Across 36 cells — four sequence lengths, three forgetting rates, three
> learning rates, three seeds — the largest recovery anywhere is **0.05**, and
> seven of twelve cells are **negative**, meaning the mechanism is worse than not
> having it. Both candidates fail: consolidate-on-confirmed-use, which is tagging
> and capture, and consolidate-on-surprise, which is the salience gate.
>
> And the advantage being forgone is large, though **smaller than this section
> used to say**. The oracle scores **0.998–1.000 in every cell**, at every
> length. The ungated arm is where the correction lands.
>
> *Corrected after re-summarising the archived run.* This read "the ungated arm
> falls to 0.46 at seq 768" and "the largest usable gap is 0.612". **Both
> numbers come from lr = 0.1**, the rate that most depresses the ungated arm —
> at seq 768, half-life 0.5, it scores 0.478/0.351/0.332, a mean of 0.387
> against a trivial floor of 0.344. The cell passes the floor check and the gap
> is real; it is also the cell where the baseline most nearly broke, and
> "largest usable gap" is a maximum taken over exactly the axis that rewards
> that.
>
> At **lr = 0.02** the same cell's ungated arm scores 0.831/0.824/0.752 — a mean
> of **0.80** — and the gap is **0.196**. Choosing the rate where the floor arm
> is highest, which is a baseline choice rather than a mechanism choice, the
> largest gap anywhere in the grid is **0.397** (seq 768, half-life 0.125).
>
> So the advantage forgone at seq 768 is **0.20 to 0.61 depending on the
> learning rate**, and the top of that range is not the honest headline. **The
> finding below is unchanged**: `on-use` and `salience` recover approximately
> nothing at every rate, so which rate is reported does not rescue either
> mechanism. What changes is how much gating is worth, and it was overstated by
> about a factor of three.
>
> **Recovery also falls as sequences get longer** — 0.05 at seq 192, −0.00 at
> 768 — so the failure is worst where the gate matters most. That was
> pre-registered as the outcome that would hurt the most, and it held.
>
> *Corrected after an audit:* the seq-1536 row is withdrawn. Every one of its
> nine cells has the ungated arm **below the trivial floor of 0.344**, so its
> denominator was the gap between a working ceiling and a broken floor — the
> same error g7-04 caught and recorded, repeated one sweep later. The headline
> and the direction are unchanged; the figures 0.000 and 0.996 are not.
>
> So the three findings below describe **what a device could do if something told
> it which of its inputs mattered.** They are not withdrawn — the arithmetic is
> real and the ceiling is worth knowing — but they are not claims about a system
> anyone can build today, and this section used to read as though they were.
>
> ## CORRECTED: something CAN tell it, and this section said otherwise
>
> This read *"Nothing tried can tell it"*. That was true when written and is now
> false. The sentence is quoted rather than quietly deleted because it gated the
> reading of everything below it.
>
> **What changed is the task, not a cleverer mechanism.** MQAR contains no event
> that separates a pair from filler in time to act on — the query arrives too
> late and never recurs, which [note 010](../../docs/notes/010-tagging-and-capture.md)
> identified as the reason tagging-and-capture could not be tested here at all.
> `reward_recall` supplies one: a reward token **in the stream**, after the
> binding it refers to, on the same broadcast every node already receives.
> `position_kinds()` is an oracle; a token in the input is not.
>
> **[g9-02](../../experiments/sweeps/g9-02-a-gate-that-reads-its-own-input.txt) — the
> first implementable gate to recover anything.** A reward gate on the FAST
> store recovers **0.23 / 0.23 / 0.24** at delays 1, 4 and 8, and **-0.13** at
> delay 20. Six mechanisms had recovered approximately zero before it.
>
> **[g9-03](../../experiments/sweeps/g9-03-is-the-cliff-reach-or-cost.txt) — and it has
> to be told the delay.** The cliff is exactly the diagonal: positive wherever
> the window covers the delay, about -0.22 wherever it does not, and every
> doubling past the smallest covering window costs about a fifth of what is left.
> A window of 64 recovers 0.09 at every delay. **A node does not know the delay**,
> so this is a saving rather than a mechanism.
>
> **[g9-06](../../experiments/sweeps/g9-06-is-the-tag-capacity-starved.txt) — a gate
> that does NOT have to be told.** A bounded capacity over WRITES rather than a
> span over steps, with marks that fade: at 32 slots and fade 0.95 it recovers
> **+0.16 at delays 1, 4, 8 and 20, spread 0.01**. The +0.16 at delay 20 is the
> **first positive result at that delay anywhere in this project**, where the
> window is -0.24. It does not beat a MATCHED window (0.16 against 0.23); it
> beats an unmatched one by 0.40, and nothing tells a node which case it is in.
>
> **The catch, which keeps this section's caveat alive.** At that same cell,
> admitting the STRONGEST retrievals scores identically (+0.003 apart) — so
> bounded capacity plus a fade is the mechanism, and the local signal
> [g9-04](../../experiments/sweeps/g9-04-is-there-a-local-signal.txt) found (retrieval
> strength, inverted, AUC 0.22) buys height only where the pool is starved:
> +0.222 at 16 slots.
> [g9-07](../../experiments/sweeps/g9-07-a-tag-that-knows-how-big-its-store-is.txt)
> found the same shape for normalising that signal — worth +0.09 at 8 slots and
> +0.01 at 32.
>
> **So the honest statement is 0.16 of the oracle's advantage, not all of it.**
> The ceiling remains a ceiling and the three findings below remain ceiling
> results. What is no longer true is that the gap is unreachable.
>
> **And none of it is yet about tiny nodes.** Every g9 cell is `d_model` 32 in
> one process.
> [g9-08](../../experiments/sweeps/g9-08-how-small-a-node-can-run-the-gate.txt) tried
> to fix that by sweeping `d_model` and asked the question on the wrong axis — a
> narrow NETWORK is not a small NODE, and nine of its fifteen cells refused
> because the task became impossible.
> [g9-09](../../experiments/sweeps/g9-09-a-small-node-in-a-wide-network.txt) asks it on
> g7-02's axis and is running.
>
> **What is still not settled:** whether any mechanism reaches the whole
> advantage. **Replay** — an offline phase that revisits stored traces later —
> remains the most interesting untried candidate in
> [BACKLOG.md](backlog-2026-07-28.md), and remains interesting for the same reason as
> before: every mechanism tried so far must decide at the one moment the least is
> known, and the tag only softens that rather than removing it. Nor is it settled
> that MQAR is innocent: note 013 blames a base rate only it has, and
> [g8-02](../../experiments/sweeps/g8-02-when-the-statistics-are-real.txt) tests that
> directly — with a usable range of only zipf 0.0 to 0.5, so largely untested
> rather than refuted.

> **What every gated result rests on, in one line.** The three findings above —
> length stops mattering, allocation stops mattering, forgetting stops — are all
> measured with an **oracle** that reads task structure no running system has.
> [Note 011](../../docs/notes/011-what-rests-on-the-oracle.md) gathers the dependency in
> one place, because each finding states it once and a reader assembling the
> position from here would reasonably conclude it was minor. It is not: the one
> implementable substitute, consolidate-on-use, works mechanically and is
> **harmful** in practice — now measured across a full grid in g8-01 and found to
> recover nothing at all. What *is* implementable and does help is plain decay —
> 0.672 against 0.526 at seq_len 768 — and the gap between that and the oracle is
> the honest size of what remains unsolved.
>
> **[g7-04](../../experiments/sweeps/g7-04-when-does-forgetting-pay.txt) put a number on
> the decay half.** Forgetting starts paying at `seq_len 768` — 0.761 against
> 0.725 — and the sign flips between 384 and 768, exactly where g1-06 predicted it
> would. Its largest margin, +0.249 at 1536, is **between two failures**: the
> trivial floor is 0.344 and neither arm clears it there, so a width-32 model is
> simply out of its depth. **The honest figure is +0.036.** All four of that
> sweep's predictions held, the first time in this project none was refuted.

**And every exponent here is an exponent in sequence length for one reason.** The
store binds every consecutive pair, so the number of things in memory *is* the
sequence length, and the measured `√(d/N)` retrieval law turns that into all the
interference there is. The task asks about four pairs; a 384-step sequence stores
383. **Over 98% of the interference comes from bindings no query will ever touch** —
which makes selective storage the most important untested idea in the project.

**Catastrophic forgetting is a function of width, and sparse keys buy some of it
back.** [g6-01](../../experiments/sweeps/g6-01-does-sparsity-protect-old-learning.txt)
trained on one body of data, then a disjoint one, and re-tested the first. Dense
keys retain 0.004 at width 48 and 0.996 at 128 — the transition is the whole
story. **Sparse keys at 4 active dimensions beat dense on retained accuracy at
widths 64, 80 and 96**, by 0.06–0.08, which is John's hypothesis confirmed: fewer
touched readout columns means new learning overwrites less of the old. It is worth
roughly a third of a step up the width grid — real, modest, and not a substitute
for capacity.

**And tiny devices do not forget, provided they are read as a cluster**
([g6-02](../../experiments/sweeps/g6-02-do-tiny-devices-forget.txt)). This resolves a
confound g6-01 could not: it varied total width and per-device width together.
With per-device width pinned at **one dimension**, a lone device keeps *nothing*
after a disjoint second task (0.000 of 0.114) while 240 of them pooling keep 0.537
of 0.827 — **forgetting is governed by total width, not by how small the devices
are.** Gated, the same cluster keeps everything. That removes the objection raised
when g7-02 landed, that tiny devices might hold a task and lose it the moment
another arrived.

Scrutiny, since all three of that sweep's refutations went the favourable way: the
gated headline sits at ceiling, so the open arm is where the claim has teeth
(there clustering reduces loss from total to 35%); the learning-rate grid pinned
at its bottom in both arms; and the decisive open-arm cell spans [0.442, 0.644]
across three seeds.

**And open participation is safe** ([g7-05](../../experiments/sweeps/g7-05-mixed-machines.txt)).
Real networks mix machines of wildly different power, and our machines combine by
adding their answers — so a tiny machine's mostly-noise vote could in principle
drag a good one down. Of forty cells testing exactly that, **one is negative, at
-0.006.** Admitting a weaker machine makes the pool better or leaves it unchanged,
everywhere else.

That matters more than its size suggests: the alternative would have required
someone to decide who counts, which is a coordinator by another name and the thing
C1 exists to forbid. **The network can accept whoever turns up.**

**And a machine that vanishes mid-sequence costs less than one that never joined.**
G3 removed machines between sequences; the realistic failure is a drop-out
partway through, which takes the departing node rows of the memory *including what
it stored earlier in that same sequence*. Measured at seq 192 with half the nodes
leaving: 0.592 if they go at step 0, 0.625 at step 64, 0.696 at step 180, against
0.704 if nobody leaves. **Leaving at step 0 is bit-identical to never having
joined**, so G3 measured the worst case and reality is strictly milder — which was
not obvious, since a network might reasonably have come to depend on a machine it
then loses.

> **G4 HAS AN ANSWER, ON ONE SEED, AND IT TURNS ON THE SIZE OF A MESSAGE.**
> [g4-03](../../experiments/sweeps/g4-03-what-does-it-cost-to-speak.txt) measured both
> directions for the first time. Inbound is **9 bytes** per node per step at any
> width and any vocabulary. Outbound was never counted, and a vote is one float64
> per token: **400,008 bytes** at a vocabulary of 50,000, which is *three steps
> per second* on a 10 Mbit/s uplink. As built, G4 failed by three orders of
> magnitude.
>
> The fix is a **token vote**. Each node carries its own complete readout —
> `partitions`, which g4-01 measured as costing nothing at adequate width — so
> its answer is a whole answer, and a whole answer is a token id. **12 bytes, at
> any vocabulary**, and at eight nodes it costs no accuracy at all: 0.658 against
> the single-process 0.658.
>
> It also changes what pooling *means*. Summing partial contributions needs
> everyone; combining whole answers is a vote, and a vote tolerates absence by
> construction — which is what C3 wants.
>
> **Three predictions died to get here**, all chasing "nodes could speak less
> often", all in the favourable direction. None of it mattered: at twelve bytes a
> node can speak every step and use a thousandth of a home uplink. The escape
> route was never needed.
>
> **One seed, no error bars, and visibly noisy** — 0.317 at quarter rate against
> 0.433 at eighth rate is not a monotone curve. This is a gate that *appears*
> passed rather than one that has been passed by this project's own standard,
> which is why it is marked as such and not simply ticked. Training traffic
> remains unmeasured and the ladder still has no gate for it.

G4's central assumption is no longer an assumption. [g4-01](../../experiments/sweeps/g4-01-no-global-readout.txt) removed the
global readout, which [note 009](../../docs/notes/009-splitting-the-memory.md) §4 had
identified as the largest untested claim in the project and as a standing C1
violation hiding inside a benchmark convenience. **At adequate width it costs
nothing: 1.000 against 1.000 with the width split eight ways.** And a single
machine's answer stands up alone (0.949 at eight-way, 0.996 at four-way), so the
pooling step is optional rather than required — which is the claim C1 actually
needs.

[g4-02](../../experiments/sweeps/g4-02-machine-shaped-churn.txt) then checked whether
G3 measured the right *shape* of failure — it removed dimensions at random, where
a departing machine takes a contiguous block. It did: machine-shaped churn is
easier by about **0.012** on average and more in the worst case, so G3's number
transfers and was mildly pessimistic. The dominant term in churn damage turns out
not to be *which* machine left but that it took part of the shared key with it.

The g4-01 penalty numbers away from ceiling are provisional: the learning-rate grid
pinned at an edge in **all six** rows — its interior value was never once chosen —
so every arm everywhere is under-tuned.

**The price, and a caveat that is now permanent.** Upward of **5.6×–8.2× the
width**, growing with sequence length; in *working memory*, worse than attention
below ~96 steps and about **2× better** at 384. Both are **bounds, not points** —
attention had still not converged at four times the budget already thought
sufficient, and **all four revisions of this comparison have moved against the
local rule** ([g1-13](../../experiments/sweeps/g1-13-both-arms-fed.txt)). The ratio is
not being chased further: it has a systematic bias toward whichever side was
measured less carefully, which has been ours every time.

*What is not in doubt are the properties rather than the ratios — no backward
pass, no softmax over positions, bit-identical under a scrambled network,
survives half its machines leaving, converges in one epoch where attention needs
thousands of steps, and working memory that does not grow with sequence length.
None of those has been revised once.*

**And one scaling law is now measured.** The width the local rule needs grows as
roughly the **cube root** of the stream length it must hold (exponent 0.37 across
an eightfold range) — where attention's state grows *linearly* in stream length
and its time quadratically. That took four attempts: `n_pairs` and `n_keys` are
both flat, and the load turned out to be `seq_len`, because the store binds every
consecutive pair rather than only the meaningful ones. It also means the 4.0×
price is a point on a curve rather than a constant, since the two architectures
must diverge in stream length — re-measuring it across `seq_len` is the natural
next step.*

*Previously: **G0 passed.** MQAR is answerable (oracle 1.000, checked mechanically
across a grid), reachable (one hand-written lookup, 1.000), and **learnable** (a
model trained from scratch, 1.000 on 5/5 seeds). Its trivial floor is measured
and has a closed form; a frozen substrate sits at 0.180, leaving **0.82 of
verified headroom**. G1 is next and is the actual bet: whether a **local** rule
can reach what an all-to-all one just did.*

*Nothing in this document
has been measured by this project.*
