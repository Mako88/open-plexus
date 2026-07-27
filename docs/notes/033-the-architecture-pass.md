# 033 — the architecture pass: components, flow, and what we assumed

**Status:** a design review with one measured claim and a great many unmeasured
ones, marked as such throughout.
**Asked for by John**: what components do we have, how are they connected, are
there better alternatives for any of them, and what assumptions are baked in that
might not be true?

---

## THE FINDING, BEFORE ANYTHING ELSE

**The model binds only adjacent tokens, so it computes bigram statistics, and its
architectural ceiling IS a bigram.**

    M += value(t) ⊗ key(t-1)
    r  = M @ key(t)
       = Σ over past positions  value(i) · ⟨key(i-1), key(t)⟩
       ≈ Σ over positions where token(i-1) = token(t)  of  value(i)

**The retrieval is the sum of values that followed this token before.** That is a
bigram count table, held in superposition. Nothing in the architecture can
represent a trigram, because no trigram is ever written down.

So the bar this project has been measuring itself against — beat a bigram, 3.583
bits per character — is **the model's ceiling, not a stretch goal.** It is at
5.256, which is the distance interference costs it.

I have been treating "does not beat a bigram" as a disappointing result for many
runs. It is not disappointing; it is arithmetic. **A structure that stores only
adjacent pairs cannot exceed the statistics of adjacent pairs.**

### Measured, because a derivation is an argument

The retrieval was compared against the bigram count vector it predicts — the sum
of the values of every token that has followed this one — as a cosine.

    writes    cosine against a bigram count table
        20    0.9455
        40    0.9036
        80    0.8817
       160    0.8795
       320    0.8784
       640    0.8866

**Not 1.0, and my own pre-registered threshold of 0.9 mean refused the claim as
first written.** So the question became whether the residual is extra SIGNAL —
which would refute the ceiling — or interference, which would not.

**It is interference.** At low load the retrieval is 0.9455 of the way to a pure
bigram table, and the gap grows as items superpose, then plateaus at the
steady-state signal-to-noise ratio. Extra structure would not depend on load that
way; noise from non-matching keys does, by construction, since those terms are
weighted by random-signed key overlaps.

**So the precise claim is:** the retrieval is a bigram count vector plus
interference, and the interference carries no information about what follows.
**The only signal available to the readout is bigram statistics**, and the
distance from 3.583 to the model's 5.256 is what interference costs.

The loose version — "the model IS a bigram" — was too strong, and the
measurement is what made it precise rather than merely plausible.

---

## The components, and what flows between them

    token ──► key(token)        frozen random, derived from (seed, token)
          └─► value(token)      frozen random

    STORE      M += value(t) ⊗ key(t-1)              Hebbian outer product
    RETRIEVE   r = M @ key(t)                        one linear read
    READ OUT   y = Wo @ r                            linear, the only cross-
                                                     sequence learning
    LEARN      Wo += lr · (target − y) ⊗ r           delta rule

    plus  decay          M *= 0.997 each write
          memory_cap     synaptic scaling on ‖M‖
          gates          window / tag decide which writes survive a reward
          partitions     each node owns a slice of the dimensions

**Every arrow is local**, which is the point and the constraint. Nothing here
needs a barrier, a population statistic, or a backward pass.

---

## The assumptions, ranked by how much they cost

### 1. Binding is adjacent-only — **the ceiling above**

Nothing writes a relation spanning more than one step. The obvious alternatives:

- **key = f(token(t-1), token(t-2))** — a trigram in vector form. Cheap, local,
  and derivable if `f` is a fixed hash. Raises the ceiling to a trigram (2.951).
- **bind to a running context vector** rather than the last token, which is what
  a recurrent state does. Raises the ceiling much further and costs the property
  that keys are regenerable from a token id.

**This is the highest-value unexplored change in the project**, and it was
invisible while the bar was mistaken for a target.

### 2. All cross-sequence learning is ONE linear map

`Wo` is the only thing that persists. The store is per-sequence working memory.
So everything the model knows about English lives in a `vocab × d` matrix read
off a superposed bigram.

Component capability tests (note 032) show the readout learns a mapping to >0.95
**given clean inputs** — so it is not underpowered. But it is being asked to be
the entire long-term memory, and one linear map is a small thing to hold a
language in.

### 3. Keys and values are frozen random projections

Deliberate and defensible: it is the strictest version of the question. The cost
is measured — no similarity structure (key separation 0.56, note 032), so no
generalisation between related tokens. John's "derive keys that are similar"
question is exactly this assumption, and the tension is that similarity **lowers**
capacity, which is the wall we are already against.

### 4. Retrieval is a single linear read

No settling, no iteration. Hopfield networks iterate to a fixed point and gain
capacity for it. **Iterating is local** — it re-reads the same store — so this
does not obviously violate C1, and it has never been tried here.

### 5. The memory resets between sequences

Working memory only. g10-04 measured the model capturing 24% of what more context
is worth, so persistence would hand it more of what it cannot use — **but that
was measured on the adjacent-binding architecture.** If assumption 1 changes,
this measurement does not transfer.

### 6. One vector per token, no composition

No roles and fillers, no way to represent "X in position 2 of a phrase".
Tensor-product representations and holographic reduced representations are the
classical answers. This is what bAbI task 2 would demand.

---

## What the literature already offers, stated honestly

**I am working from knowledge, not from a search of current papers**, and this
project's standard is that an unmeasured claim is an argument. Treat every line
here as a pointer to read, not a finding.

- **Fast weights** (Ba et al. 2016) is essentially this architecture with an
  inner loop of settling — assumption 4.
- **Dense associative memory / modern Hopfield** (Krotov & Hopfield) buys
  capacity through a sharpening nonlinearity, but the high-capacity forms keep
  patterns separately, which g10-07 already measured as a table beating us.
- **Linear attention** is mathematically what we compute, and note 006 already
  records that it FAILS MQAR unless its state is large. That is our capacity wall
  under another name, and it is prior art we should be citing against ourselves.
- **Tensor product representations** (Smolensky) address assumption 6 directly.
- **Complementary learning systems** (McClelland) is the two-timescale argument
  for assumption 2 — a fast store and a slow consolidated one, which biology
  separates into hippocampus and cortex and which this project currently does not
  separate at all.
- **Synaptic tagging and capture** (Frey & Morris; Redondo & Morris) is already
  the g9 line's basis, so the biological well is not dry — it produced the one
  mechanism here that works.

**On emergence, which was the original hope.** Nothing measured here has produced
a capability that was not built in. That is not evidence against the idea; it is
evidence that the current system is too small and too shallow for anything to
emerge from. Emergence in the literature shows up at scales and depths this has
not approached, and a two-layer linear system is not where anyone should expect
it. **The honest position is that the hypothesis is untested rather than
disfavoured.**

---

## What I would do, in order

1. **Fix the ceiling.** Bind over a two-token context via a fixed hash. Local,
   derivable, raises the bar from 3.583 to 2.951, and is the only change here
   that lifts a *proven* limit rather than a suspected one.
2. **Measure the similarity/capacity tradeoff** before touching keys, since it
   decides whether assumption 3 is worth attacking at all.
3. **Try iterated retrieval** — cheap, local, and never attempted.
4. **Leave assumptions 2, 5 and 6 alone** until 1 and 3 report, because every
   measurement about them was taken on the adjacent-binding architecture and may
   not survive it changing.

---

## What this note is not

It is one measured claim and five arguments. The project's failure mode this week
has been conclusions drawn from reasoning that runs then refuted — seven times.
**Only the ceiling derivation should be believed without a run**, and even that
should get one.
