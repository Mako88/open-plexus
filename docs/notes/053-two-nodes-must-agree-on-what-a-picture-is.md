# 053 — Two nodes must agree on what a picture is

**Status:** a constraint register entry. Nothing built, nothing measured, and
nothing here needs a run — it is a consequence of decisions already made.

**Why it exists:** John asked how the input and output converters interact with
"every node can be its own input and output". The answer is that they fit cleanly,
and working it through surfaced a failure mode that decision 163 §1 named only half
of.

---

## IN PLAIN TERMS

The plan is that a picture, a sound or a video gets turned into **concept ids**
before it reaches the network — decision 163 §1. Each machine does that conversion
for its own input, which is what lets every machine be both an input and an output
for the model.

The problem: **if two machines turn the same picture into different ids, the
network stores it in two unrelated places and never connects them.** Ask about the
picture and you get half of what it knows, or nothing. Nothing about this
announces itself — both machines are working correctly by their own lights, and
every number the system reports still looks reasonable.

A yes to "the codebook is shared" makes this impossible by construction. A no
means the network can quietly fragment in a way no single machine can detect.

---

## What 163 §1 named, and what it did not

Decision 163 §1 accepted quantisation and named the risk it carries:

> **The risk that matters is the silent one:** a bad quantiser merges two things
> that should stay distinct, and this architecture cannot recover from that,
> because it will then address them identically.

That is the **MERGE** direction, and it is a property of one quantiser being bad.
It exists on a single machine and is not a distributed problem.

Distributed, there is a second direction and it is the one that has no analogue in
a single process:

    MERGE   two distinct things -> one id          163 §1 named this
    SPLIT   one thing -> two ids on two nodes      this note

**SPLIT is worse than MERGE in one specific way: no node can detect it locally.**
A merge is at least visible to the machine that made it — two inputs that should
differ produce the same address, and a probe on that machine finds it. A split is
invisible everywhere: node A wrote to address `x`, node B wrote to address `y`,
each is internally consistent, and the disagreement exists only in the relation
between two machines that never compare notes.

**Why it is likely rather than hypothetical.** A codebook is *fitted from data*,
and the whole premise of this project is that nodes are heterogeneous, unreliable
and constantly arriving and leaving (see `GOALS.md`). Any codebook that adapts
per node diverges by construction. And even a frozen one splits across *versions*:
a node that joins later, with a newer encoder, disagrees with every node already
running.

## The options

    (a) FROZEN GLOBAL CODEBOOK, versioned, part of the network's identity. A node
        whose version does not match is refused rather than allowed to write
    (b) QUANTISE ONCE AT INGEST. The node that owns the input converts it, and
        only concept ids travel. No other node ever quantises that input
    (c) PER-NODE CODEBOOKS PLUS A TRANSLATION LAYER between them

**(c) is refused, and it is worth saying why rather than just declining it.**
Aligning two independently-learned discrete spaces with no paired data is a real
research problem — it is the same problem as the unsupervised-translation work, and
it is strictly harder than anything this project is trying to do. Solving it as a
*precondition* for the actual goal is the wrong order by a wide margin.

**(a) and (b) are not alternatives — they compose, and both are needed.** (b) is
what makes the architecture cheap: one conversion per input, at the edge, outside
the learning loop, so C1 does not bite and stock encoders stay available (163 §1's
argument). But (b) alone does not prevent SPLIT, because two nodes can each ingest
*the same content* — the same image arriving twice by different routes — and (b)
says nothing about them agreeing. (a) is what prevents that, and it is the part
with a cost.

**The cost of (a), stated plainly.** A codebook that is part of the network's
identity **cannot improve without re-addressing everything already stored.** That
sits against C4, which says the weights never freeze. The resolution is probably
that C4 governs the *learner* and not the *sensor* — a frozen sensor with a
perpetually-learning network behind it is coherent, and is roughly what biology
does — but that is an argument and not a measurement, and it should be made
explicitly rather than assumed.

## Recommendation

**(b) as the mechanism, (a) as the constraint that makes it safe**, and the
codebook version recorded as part of network identity from the first multimodal
commit rather than added when it breaks.

## The falsifier, which is cheap and should exist before any of this is built

**Two nodes given byte-identical input must emit byte-identical concept ids.**
That is an ordinary connection test — rule 6's shape, at the seam between the
converter and the wire — and it costs nothing to write. It is also the *only*
check that catches SPLIT, because by construction no single node's own behaviour
is wrong.

A companion is required, per the paired-assertion rule: two nodes given
*different* input must emit *different* ids. Without it the test passes whenever
the quantiser returns a constant.

## Blast radius

**Zero today.** Nothing in this project is multimodal, no quantiser exists, and no
result depends on this. It becomes load-bearing the moment the first non-text
modality arrives, and the reason it is written now is that the decision is nearly
free before then and expensive afterwards — the same argument note 052 is built on.

**What it changes about the roadmap:** row B4 was already "fit `ContentIndex`
across paired streams". It gains a second requirement — the codebook is shared
state with a version — which is additive rather than a rebuild.
