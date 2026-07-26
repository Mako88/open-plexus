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

It also costs the thing that matters. `k` slots of `(key, value)` is `2 * k * d`
numbers per node, against a superposed store's `d * d`. For small `k` that is
*cheaper* than what we have, which matters because the figure of merit is
minimum viable node size.

## What has to be true for this to be wrong

- **If `k` has to be large to work**, the win evaporates — the store stops being
  small and the tiny-node story is no better off.
- **If the ranking signal is too weak to order candidates** even within a
  competition, then 7.6× enrichment was never the problem and note 013's
  diagnosis is wrong in a way g8-02 will also fail to detect.
- **If exact slots break the locality argument.** Superposition is what lets a
  node hold a fraction of a memory; slots may want whole bindings, which is a
  different distribution story and needs checking against C1 before anything is
  measured on top of it.

That last one is the real risk and it is a design question, not a tuning
question. It gets answered before the mechanism is built, not after.

## Status

**Not implemented. Not measured. No prediction registered yet.** This note is the
argument, written down before any of that, so the order is checkable.
