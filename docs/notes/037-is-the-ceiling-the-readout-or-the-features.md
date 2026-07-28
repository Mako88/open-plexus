# Note 037 — Is the ceiling the readout, or the features?

**Written while the probe is running, so the prediction below is registered
rather than retrofitted.** The result goes in the OUTCOME section and nothing
above it is edited afterwards.

## IN PLAIN TERMS

Our model has two parts. The first turns each piece of text into a bunch of
numbers — and that part never learns anything; it was set randomly at the start
and frozen. The second part looks at those numbers and guesses the next
character, and it is the only part that learns.

We now know the model stops improving after about sixteen thousand characters,
however much more text it is given. **This asks which of the two parts is the
reason.** If the guessing part is too simple, making it cleverer should help. If
the numbers it is given have already thrown away what matters, no amount of
cleverness downstream will recover it.

A yes means the fix is a better readout. A no means the fix has to be further
upstream, in how text is turned into numbers at all — and that is a much bigger
change.

## Why the question is well posed here, which is unusual

`r = M @ key` depends only on `Wv` and the keys. **Both are drawn once in
`__init__` and never updated.** So the retrieval is entirely independent of
`Wo`, and the architecture is exactly:

    frozen random features  ->  linear probe

That is not an analogy. It means the features can be extracted ONCE and any
readout trained on them offline, with no model surgery and no confound from the
two parts interacting. Almost nothing in this project separates that cleanly.

The measurement: extract `(retrieval, next token)` pairs at four data sizes,
train a linear readout and two-layer MLPs of two widths on each, and score all
of them on features taken from the corpus's own held-out test split.

## PREDICTION, registered before the numbers

**I predict the features are the ceiling: the MLP buys a LEVEL improvement and
does not produce a SLOPE.**

The reasoning, and it is the same quantity that has explained everything else
today. Under note 035's stable-rank measure the retrievals sit at about **4**.
A readout — linear or not — cannot use structure that is not present in its
input, and four dimensions is very little to be nonlinear about. Decision 69
listed six mechanisms that each moved the level and none the slope; this would
be the seventh, and the first one that says WHERE the limit lives rather than
merely failing to move it.

Concretely:

  P1  the MLP beats the linear readout at every data size (a level gain)
  P2  the gain does NOT grow with data — no slope, same as everything else
  P3  MLP-512 is not much better than MLP-128, because the input is the
      constraint rather than the capacity applied to it

**What would refute this, and it is the outcome I would rather have:** the MLP's
advantage widening with data, or either MLP showing a clearly negative exponent
where the linear readout is flat. That would say composition is the missing
ingredient and would make "one trained stage feeding another" the direction,
which is the one thing decision 69 identified as untested.

**What a confirmation costs.** If P1–P3 hold, no readout fixes this, and the
work has to move upstream to the keys and the store — the parts that decide what
the features ARE. Sparse keys (decision 67) are the only intervention so far
that touched that layer, and they bought 0.15 bits, which is consistent with it
being the layer that matters and inconsistent with it being solved.

## What this note is NOT

Not a test of whether local learning can train a composed function. The readouts
here are trained with ordinary backpropagation, offline, deliberately — the
question is whether a composed readout would help AT ALL, and there is no point
asking whether it can be trained locally before knowing that. If it does help,
the local-training question becomes the next one and note 036 is where it starts.

## OUTCOME

**P2 REFUTED, and it is the refutation I said I would rather have.** The features
are not the ceiling. The linear readout is.

    chars  samples   linear   MLP-128   MLP-512
     4,000    3,937    5.579     5.388     6.320
    16,000   15,875    5.436     4.865     5.214
    62,500   61,976    5.351     4.757     5.097
   250,000  248,031    5.320     4.525     4.659

    fitted exponent, bits ~ chars^b
      linear     b = -0.0115   R2 0.93
      MLP-128    b = -0.0397   R2 0.92
      MLP-512    b = -0.0681   R2 0.89

    for reference, same corpus and axis
      our model in situ (g11-05)          b = -0.0010   FLAT
      backprop attention, width 64        b = -0.0243
      Filipovich, published: DFA -0.040, backprop -0.071

**On identical frozen features, a two-layer readout recovers a data exponent
between -0.04 and -0.07, where a linear readout on the same features sits at
-0.012 and the deployed model sits at -0.001.** MLP-512's exponent is
indistinguishable from published backprop. The gap over the linear arm WIDENS
monotonically — 0.19, 0.57, 0.59, 0.80 bits — which is precisely the shape P2
predicted would not appear.

**MLP-128 reaches 4.525 bits, past the unigram at 4.829** — a bar this project
has never cleared — on features that never learn anything.

P1 held (the MLP wins at every size). P3 held only at small data and for the
ordinary reason: MLP-512 is worse than MLP-128 at 4,000 characters because
65,536 parameters on 3,937 samples overfits, scoring 6.320 against a uniform
6.000. By 250,000 it has nearly caught up and has the steepest slope of the
three, so capacity was never the constraint — sample count was.

### What this says, stated narrowly

The retrieval carries far more information about the next character than a
linear map can extract. Every flat exponent this project has measured is
consistent with one cause: **the readout, not the store, the keys, the values,
or the sum.**

That reverses the working diagnosis. Decision 59 blamed the sum; decision 62
blamed persistent capacity; decision 65 blamed rank and decision 67 refuted that.
None of them was looking at the readout, because the readout is the one part
that is *exactly* the right thing — the delta rule on `Wo` IS the exact gradient
for a single linear readout, so it was assumed to be doing its job perfectly.
**It was. That was the problem.** A perfect linear readout is still linear.

### What it does NOT say, and this is pre-registered above

**Nothing about local learning.** These readouts were trained by ordinary
backpropagation, offline, with Adam and many epochs over a fixed dataset. The
deployed model learns online, in one pass, by a local rule. This shows the
information is THERE and that a composed function can extract it. It does not
show that a composed function can be trained the way this project needs.

Part of the offline gain is optimisation rather than architecture: the offline
LINEAR arm reaches 5.320 where the in-model linear readout reaches 5.505. About
0.19 bits is better optimisation. **The remaining 0.80 is composition**, and that
comparison is clean — same features, same optimiser, same epochs, same test set.

### The question this makes central

Note 036 asked whether backpropagation can be distributed and was filed as
background reading. It is now the main line.

**And there is a specific reason to think a composed readout is C1-compatible.**
`partitions` already splits the readout by dimension, so each node holds its own
`vocab x d/groups` slice and computes its own `parts[g]`. If a node's slice
became two layers instead of one, backpropagation THROUGH THOSE TWO LAYERS uses
only that node's own activity and its own error — no other node's state enters.
A composed readout inside a node is not a violation of locality; it is the same
locality applied twice.

That is the next measurement: a per-node two-layer readout, trained by local
backprop within the node, online, and scored on the data axis with the bottom of
the range probed first (decision 63).
