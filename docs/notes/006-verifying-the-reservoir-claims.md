# Note 006 — Verifying note 001's reservoir claims

Note 001's model of what a random substrate can and cannot do was steering the
entire choice of benchmark, and was unread. This is the check.

**The argument survives and is now quantified. The specific recommendation was
wrong in a way that would have wasted the first experiment.** One worry that had
not been articulated is resolved, and one new architectural requirement falls
out.

---

## IN PLAIN TERMS

We picked a memory test based on our own reasoning about what an untrained
network is bad at. We hadn't checked whether anyone had measured it.

They had — extensively, and recently, on exactly this. The good news: the
reasoning holds, and there are hard numbers where we only had arguments.

The bad news, and it matters: **the simple version of the test we proposed is
already solved by weak models.** We would have built it, watched everything pass
easily, and learned nothing — the same trap the previous project fell into,
reached by a different road. There is a harder variant that does separate them,
and the difference is small but decisive.

Also resolved: a worry that the test might require exactly the kind of
everyone-talks-to-everyone operation we've banned. It doesn't. Someone proved
that.

---

## 1. What was checked

| note 001 claim | verdict |
|---|---|
| A random substrate's memory is finite and spent indiscriminately | **Confirmed, and quantified** |
| Nonlinearity and memory trade against each other | **Confirmed** — this was not in note 001 and should have been |
| Associative recall separates a weak substrate from a strong reference | **Confirmed, with a large measured gap** |
| "Associative recall" as specified | **Wrong variant — corrected in §3** |
| Query distance / count works as a difficulty dial | **Confirmed** |

Primary source for §3–5: Arora et al., *Zoology: Measuring and Improving Recall
in Efficient Language Models* (arXiv 2312.04927), text extracted and read.
Capacity results in §2 are from search summaries of Dambre et al. (2012) and the
reservoir-computing literature and are **not** primary-source verified.

## 2. Capacity is bounded, and nonlinearity costs memory

Two established results, both stronger than note 001's informal version:

**Memory capacity is bounded by the number of linearly independent state
variables**, with equality under a fading-memory condition. Note 001 said a
reservoir's capacity is "finite" and spent uniformly; the literature gives the
bound explicitly. A substrate cannot remember more than it has state for, no
matter how it is wired.

**There is a memory–nonlinearity trade-off** (Dambre et al. 2012): the nonlinear
dynamics that make a reservoir useful for computation actively degrade the
memory stored in it. **Note 001 missed this entirely, and it strengthens its
argument** — a substrate cannot buy long memory by becoming more expressive,
because the two draw on the same budget.

## 3. The correction: single-query associative recall is already solved

**This is the finding that would have cost the first experiment.**

Note 001 recommended "associative recall — pairs appear, then one is queried."
The source is explicit that this version is not discriminating:

> "This gap in associative recall perplexity is very surprising because **prior
> work shows gated-convolutions can perfectly solve a formalized version of the
> task**… In this synthetic task, the input contains a sequence of bigrams
> representing key-value pairs from a random dictionary **followed by a single
> query token.**"

A single query is solvable by models much weaker than attention. Building it
would have produced a benchmark everything passes — **exactly the G0 failure
note 001 was written to prevent, reached by a different route.**

**The discriminating variant is multi-query associative recall (MQAR):** K
key-value pairs embedded in a sequence of length T, with the model required to
retrieve **all K** values. The source developed it precisely because the
single-query version failed to reflect the real gap.

> **Corrected recommendation: multi-query associative recall, not associative
> recall.** The number of queries `K` is not a difficulty dial bolted on
> afterwards — it is the thing that makes the task discriminating at all.

Note 001's §5 table listed "number of pairs" as one dial among four. It is the
load-bearing one, and the note did not know that.

## 4. The gap is large, and measured

> "a 70M parameter attention model outperforms a **1.4 billion parameter**
> gated-convolution model on associative recall"

A 20× parameter difference in favour of the weaker-in-principle architecture,
on real language rather than a synthetic. That is the G0 headroom note 001
asked for, already demonstrated by someone else.

The separation also responds to the dial as predicted: a recurrent model's
"performance degrades sharply as we increase the number of queries in an example
while attention performs consistently well."

**Note 001's predictions 1, 2 and 3 are therefore supported before we run
anything** — which does not excuse us from running it on *our* substrate, but
does mean the instrument is known to work.

## 5. The sharper statement, and why it is better news than "reservoirs fail"

The theoretical result is not "fixed-state models cannot do it." It is a
**scaling separation**:

> "the model dimension for BaseConv (and thus the aforementioned architectures)
> to solve Mqar **grows with the input sequence length** (Theorem 4.4) while
> attention can solve Mqar with model dimension **independent of sequence
> length** (Proposition 4.3)"

**This is exactly what note 001's P3 asked for and did not expect to find
pre-established.** P3 warned that a gap no local method could ever close is as
useless as no gap — and a scaling separation is a closeable gap with a known
price. The task is not impossible for a weak substrate; it is *expensive*, at a
rate the literature has characterised.

That gives the difficulty dial a principled shape rather than an empirical one.

## 6. Resolved: the task does not require an all-to-all operation

An unarticulated worry, which should have been written down when note 001 was
drafted: if content-based retrieval fundamentally requires comparing every item
with every other item, then MQAR requires a C1 violation, and no local system
could ever pass — making it a useless benchmark for us.

The source addresses this directly:

> "It is natural to wonder then if all pairwise comparisons among tokens are
> necessary to solve Mqar… we observe that we can parallelize this algorithm
> using dyadic intervals and achieve a depth of Õ(1)… This allows us to prove
> new upper bounds for BaseConv models applied to Mqar, **which improves upon
> the quadratic time complexity of attention**."

**Pairwise comparison is not necessary.** A sub-quadratic, bounded-depth
construction exists. MQAR is therefore a fair test for a locality-constrained
system rather than one rigged against it.

## 7. New requirement: the mixing must be input-dependent

The source identifies what actually separates the architectures, and it is not
attention as such:

> "**input-dependent sequence mixing is important to solve Mqar efficiently**…
> The model needs to adapt the sequence mixing weights based on the
> token-interaction distances required for each new example." (Theorem 4.5)

A model whose mixing is fixed in advance needs dimension growing with sequence
length. A model that adapts its mixing to the current input does not.

**This is a concrete architectural requirement arriving from the literature
rather than from our own reasoning, and this project did not have it before.**

**Is it compatible with C1?** Provisionally yes. A unit modulating how it
combines its inputs based on what it is currently receiving is a local decision
— it consults nothing global and requires no agreement. This is materially
different from a global sort or a pooled statistic.

**Is it compatible with note 004?** Probably, and the distinction matters.
Note 004 requires *placement* to be clustered and static so that `D` stays in
single digits. Input-dependent **connectivity** — deciding at runtime *which
machine* to talk to — would break that budget. Input-dependent **gain** on fixed
connections would not: same destinations, same packet count, different weights.

> **The likely resolution is fixed connectivity with data-dependent gains.**
> Recorded as the current best guess, not as a decision, and it needs checking
> against whether that is enough to satisfy Theorem 4.5's requirement — which it
> may not be, since the theorem is about mixing *weights* across token
> distances, and whether a gain on a fixed topology can express that is an open
> question this note cannot settle.

## 8. What changes

- **Note 001's recommendation is amended** from associative recall to
  **multi-query** associative recall, with `K` promoted from one dial among four
  to the parameter that makes the task work at all.
- **The memory–nonlinearity trade-off is added** to note 001 §3's list of what a
  random substrate cannot do.
- **Input-dependent mixing becomes a design requirement** for anything that
  hopes to pass G0 efficiently, and feeds directly into the plan.
- **G0's own protocol is partly pre-validated.** The instrument is known to
  separate architectures; what remains unknown is where *our* substrate sits on
  it.

## 9. Still unverified

- **The capacity results in §2** are from search summaries. Dambre et al. (2012)
  has not been read.
- ~~**Reservoir computing specifically.**~~ **CLOSED — measured, and the
  inference was right.** The Zoology results concern gated convolutions, linear
  attention and state-space models; a random frozen reservoir with a trained
  linear readout was not among them, so its position here was reasoned by
  analogy. It has now been measured on our own generator:
  [g0-02](../../experiments/sweeps/g0-02-frozen-reservoir.txt) puts it at
  **0.180 against a base rate of 0.125** — at chance, and *below* the 0.344
  one-line-heuristic floor. Doubling the substrate from 64 to 128 units buys
  0.008, which is §5's scaling argument showing up directly: width does not
  supply content-addressed retrieval. A connection control decoding the current
  token from the same states scores **1.000**, so this is the substrate and not
  the pipeline.
- **SORN, Forward-Forward, SWIM, gossip protocols, CRDTs** remain unread.
