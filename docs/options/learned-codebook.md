# Option record — a codebook learned by us, append-only

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing of the codebook itself.
- The parts it would be assembled from do exist and are measured: the occupancy gate
  (`inherit`), which answers *"was anything ever written here"* with a structural zero,
  and `concepts.Merged`, which expresses two concepts turning out to be one without moving
  any address.

---

## What was tried, and what came back

### Raised as an option, with the split that makes it tractable

    CONFIG  when    2026-07-29
            source  decision 163, and John in conversation
            script  none -- nothing built
            task    none
            model   the occupancy gate as decision 148 left it
            knobs   none
            scale   n/a

New distinctions get **new ids**; existing ids never move, so nothing is re-addressed. The
argument that it is reachable rather than speculative: the occupancy gate already answers
*"was anything ever written here"* with a structural zero, which **is** a novelty detector,
so minting a concept on novelty is reachable with parts that exist and are measured
(decision 148).

**The honest split John and note 052 both name:** the codebook — which concepts exist — is
ours; the FEATURE SPACE — what makes two images similar at all — is where off-the-shelf
earns its place. Decision 163 §1's ruling is that a quantiser sits at the edge, outside the
learning loop, so a stock encoder does not violate C1.

Nothing has been run. No quantiser exists for any modality, and the falsifier note 053
specifies — two nodes given identical input must emit identical ids, with the companion
that different input must differ — is unwritten.

### John wants the learned quantiser tried, on its own merits — 2026-07-31

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g36-04-a-picture-a-sound-and-a-word.txt
            script  experiments/g36_04_a_picture_a_sound_and_a_word.py
            task    MNIST images + FSDD spoken digits + 10 words
            model   conditional; bound derived
            knobs   codes 20/50/100; five arms; 3 seeds
            scale   link purity, chance 0.100

**The request is John's, 2026-07-31**, in conversation: *"I'd love to play around
with our system learning a quantiser itself. Just to see if it's worth it or
whatever."* The `source` field cites `g36-04` because that is where the numbers
below were measured; the request itself is not a measurement.

**Registered as a request, not as a plan**, and with the measurement that bears
on it stated so it is not re-derived: `g36-04` found the linking is **not**
limited by front-end quality over the range tested. The audio quantiser is 0.185
worse at its own job than the image one and produced the table's best link,
**0.9902**. `g36-01` reached the same conclusion from the other side.

So a learned quantiser is **not** currently on the critical path for accuracy —
which changes what it would be FOR. The live reasons are that a borrowed
quantiser is a component this project did not derive, and that kill-list #6's
quantiser half is the one part of "independent nodes agree" that has never been
tested at all. Two nodes running the same k-means on different data do not
obviously produce the same codes, and nothing here has checked.

**The falsifier to write first, before any learning:** do two nodes given
different samples of the same world produce codes that agree? If they do not, a
learned quantiser is not an improvement to try, it is a requirement.

### THE EDGE-QUANTISER ARCHITECTURE — John, 2026-07-31

    CONFIG  when    2026-07-31
            source  John, in conversation
            script  none -- a design sketch, nothing measured
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

His picture of the starting architecture, recorded verbatim in substance because
it constrains where the quantiser is allowed to live:

> A request routes through an **edge machine that holds the quantiser**. That
> machine converts whatever data is being input, sends the result to the actual
> network, and the response comes back to the same machine and back out.

**What this settles, and it is not nothing.** It makes the quantiser an EDGE
concern rather than a per-node one, so the "do two nodes agree" question above
becomes "do two EDGES agree" — a smaller, better-posed problem, and one where a
shared codebook is a legitimate answer rather than a violation.

**What it leaves open.** Whether an edge machine holding a codebook is compatible
with C1 depends on how the codebook got there. A codebook distributed once and
frozen is fine; a codebook that must stay in sync across edges as it learns is
the collective amended C1 forbids. **That is the question to ask of any learned
variant**, and it is the reason this is recorded beside the request above rather
than in a design file.

### THE QUANTISER IS DOING IDENTITY WORK IT IS NOT SUPPOSED TO DO — John, 2026-07-31

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g36-04-a-picture-a-sound-and-a-word.txt
            script  experiments/g36_04_a_picture_a_sound_and_a_word.py
            task    MNIST images + FSDD spoken digits + 10 words
            model   spherical k-means over borrowed features, `grouping.cluster`
            knobs   codes 20/50/100; 3 seeds
            scale   quantiser purity, link purity; chance 0.100

**John's discomfort, and it lands on a real inconsistency rather than a
preference.** `DECISIONS.md` §1 records the split: *the quantiser answers
ADDRESSING, not IDENTITY; identity is LEARNED.* But `grouping.cluster` is
**k-means**, and clustering by similarity IS an identity assignment — it decides
that these two pictures are the same thing, which is precisely the decision the
mechanism is supposed to reach by itself from co-occurrence.

So the current front end violates the project's own stated division of labour.
Nothing measured has caught it, because a good clusterer makes the downstream
look better rather than worse.

**Two things are being conflated and only one of them is required.**

  - **Discretisation** is required, and unavoidably. `CoOccurrence` counts
    recurrence of an id; if every input got a unique id every count would be 1
    and no statistic could form. Two recordings of *six* share almost no raw
    bytes, so SOMETHING must place perceptually near things near each other.
  - **A trained, global, semantic quantiser** is NOT required, and it is the
    part that carries the objection.

**RANDOM-HYPERPLANE LSH SEPARATES THEM, AND IT IS ALREADY IN THIS REPOSITORY.**
`openplexus/sketch.py` — `AddressSketch` — hashes by the sign pattern of `b`
random hyperplanes, so two inputs collide with probability set by the ANGLE
between them. It was built to record *that* an address was written, never what
went there, which is the same refusal being asked for here.

Its properties against k-means, for this use:

  - **No training and no data.** A shared seed is the only thing two nodes need,
    and a constant distributed once and frozen is C1-legal in a way a codebook
    that must stay in sync as it learns is not.
  - **It decides no identities.** A bucket is not a claim that two things are the
    same; it is a bin fine enough for counting to work. Identity stays with the
    walk, which is where this project says it belongs.
  - **It answers kill-list #6's untested half by construction.** Two nodes
    running k-means on different samples produce different centroids and
    therefore different code meanings. **That has never been tested and probably
    fails.** Two nodes running the same hyperplanes cannot disagree.

**The granularity argument, which is why a dumb hash may be sufficient.**
Over-segmentation is REPAIRABLE by the mechanism — the walk already merges
surfaces that co-occur with the same things, which is what `equivalence_classes`
does. Under-segmentation is NOT: if sixes and fives land in one bucket, nothing
downstream can separate them. So the front end does not need to be smart, it
needs to be **fine and stable**, which is a hash rather than a classifier.

**And John's specificity problem falls out of the same mechanism.** Fewer bits is
a coarser bucket and more bits is a finer one, so a multi-resolution hash gives
*dog* and *Labrador* from one device rather than needing a hierarchy bolted on.
Untried and not obviously correct, but it is the first proposal in this line that
addresses the granularity gap at all.

**WHAT THIS IS NOT.** It is not a fix for the shelf problems. `g36-04` measured
that the linking is not limited by front-end quality over the range tested — the
audio quantiser is 0.185 worse at its own job and produced the table's best link,
**0.9902** — and `g36-05` traced the eviction to the bound being a budget, which
is downstream of the quantiser entirely.

**THE EXPERIMENT, and what would refute it.** Swap `grouping.cluster` for an LSH
front end in `g36-04`'s pipeline, sweep the bit count, and add the agreement test
the current quantiser has never had: two nodes, different data samples, same
seed — do the codes mean the same thing? **Refuted if LSH is clearly worse than
k-means at a matched code count**, which would say the borrowed feature space was
doing real work and that John's *"no artificial identification"* costs accuracy.
