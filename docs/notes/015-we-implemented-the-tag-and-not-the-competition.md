# Note 015 — We implemented the tag and not the competition

[Note 010](010-tagging-and-capture.md) read Lehr et al. 2022 and took the shape of
synaptic tagging and capture: **tag now, cheaply; let a later signal decide what
survives.** That is what `consolidation` implements, and
[g8-01](../../experiments/sweeps/g8-01-a-gate-without-an-oracle.txt) measured it
recovering none of the oracle's advantage.

Searching the current literature for what has changed since turns up something
note 010 does not mention anywhere — the words *competition*, *limited*, *scarce*,
*pool* and *budget* do not appear in it:

> The distribution phase is **competitive in a winner-take-all fashion**, when
> synapses potentiated at induction compete with each other for plasticity-related
> proteins.

**The protein pool is finite.** Tagged synapses do not all get captured; they
compete for a scarce resource, and most lose. We implemented the tag and the
later signal, and left out the scarcity that does the actual selecting.

## Why this is not a detail — it explains g8-01's worst number

Retrieval quality goes as `sqrt(d / N)`: node width over the number of things
piled into the store. The oracle wins for one reason — **it holds `N` constant.**
Filler is never written, so the memory holds `2 * n_pairs` bindings whatever the
sequence length. That is why g7-02's rows were identical to three decimals.

Now look at what a *threshold* gate does. It fires whenever a step clears a bar,
so the number of promotions is a rate times a length:

    promotions  ≈  firing rate  x  sequence length

`N` grows with length, so `SNR` falls with length, so **recovery must fall with
length**. g8-01 pre-registered that prediction and measured it: 0.05 at seq 192,
−0.00 at 1536.

That result was read as "the mechanism fails hardest where it is needed most". It
is better read as: **a threshold cannot hold `N` constant, and holding `N`
constant is the entire mechanism being copied.** No amount of tuning the bar
fixes it, because the bar sets a *rate* and the oracle sets a *quantity*.

## What the missing piece would be

A **fixed-capacity lasting store with competitive admission.** Not "promote
anything above a bar" but "hold at most `k` things, and a new candidate must beat
an incumbent to get in".

Then `N_lasting = k` by construction — constant in sequence length, exactly the
property the oracle has and the property every mechanism tried so far lacks.

This also removes the base-rate problem [note 013](013-salience-and-the-missing-body.md)
identified, and removes it *structurally* rather than by improving the signal. A
threshold drowns when 92% of the data is uninformative because 92% of a large
number is still a large number. **A budget of `k` cannot drown**: it promotes `k`
things whatever the base rate. Note 013 measured the salience signal as
genuinely selective — queries fire at 7.6× the filler rate — and that enrichment
is more than enough to win a competition for `k` slots, while being nowhere near
enough to survive a threshold.

So the same signal that failed may succeed unchanged, purely by changing what it
is allowed to do with it.

## And this is where traditional computing walks in

A fixed-capacity store with an admission policy and an eviction policy **is a
cache**, and caches are one of the most thoroughly studied objects in computer
science. LRU, LFU, ARC, CLOCK, and the whole admission-policy literature are
decades of work on precisely the question this project has been failing to answer
from first principles: *given more candidates than room, which do you keep?*

John asked whether more traditional computing could replace biological mechanisms
and get the same result. This is the strongest candidate in the project. The
biology and the systems engineering converge on the same answer — **finite
capacity plus a replacement policy** — and the CS version comes with fifty years
of analysis attached.

### The cost, corrected — and the correction changed the design

**A first version of this note said `k` slots cost `2 * k * d` against a
superposed `d * d`, and called that cheaper. That arithmetic was network-wide,
and per node it inverts.**

A node owns rows `[lo, hi)` of the `(d, d)` memory, so its superposed slice is
`w * d` numbers, where `w` is its width. A slot holding a retrieval slice and a
key *vector* costs `w + d` — and the key vector is full width, because retrieval
sums over every dimension. So:

    d = 256, w = 1     superposed = 256 numbers     slots affordable = 0
    d = 256, w = 4     superposed = 1024            slots affordable = 3
    d = 256, w = 32    superposed = 8192            slots affordable = 28

**A width-1 node cannot afford a single slot.** For exactly the nodes this
project exists for, slots are strictly more expensive than superposition. That
kills the mechanism as first described.

What saves it is work already done. [Note 012](012-broadcast-the-token.md)
established that a node need not store the key table at all: with `derived_keys`,
row `t` is regenerated from `(seed, token)` on demand, which is why a four-byte
token is enough to broadcast. **So a slot stores the token id, not the key
vector** — cost `w + 1` instead of `w + d`:

    d = 256, w = 1     superposed = 256 numbers     slots affordable = 128
    d = 256, w = 4     superposed = 1024            slots affordable = 204
    d = 256, w = 32    superposed = 8192            slots affordable = 248

A width-1 node affords **128 slots** where it could not afford one. The mechanism
is cheap, and it is cheap *only because keys are derived*.

That is a hard dependency and it is worth naming: competitive capture is not
independently implementable. It rests on derived keys, and if that ever has to be
withdrawn this goes with it.

## What has to be true for this to be wrong

- **If `k` has to be large to work**, the win evaporates — the store stops being
  small and the tiny-node story is no better off.
- **If the ranking signal is too weak to order candidates** even within a
  competition, then 7.6× enrichment was never the problem and note 013's
  diagnosis is wrong in a way g8-02 will also fail to detect.
- **If exact slots break the locality argument.** Superposition is what lets a
  node hold a fraction of a memory; slots might want whole bindings.

**That last risk was checked before building, and it survives — but only just,
and not in the form it was first written.** A slot holds this node's own slice of
a retrieval, `w` numbers, plus a token id. It does not hold a whole binding and
it does not reference any other node's dimensions, so nothing is shared and
nothing is synchronised: C1 holds. The token id is already on the wire, so the
protocol does not change either.

The cost check is what nearly killed it, and is recorded above: the obvious
implementation is *more* expensive than superposition for precisely the nodes
this project cares about.

## Status

**Not implemented. Not measured. No prediction registered yet.** This note is the
argument, written down before any of that, so the order is checkable.
