# 028 — Four corrections in a row, and the detour they caused

## The number we were chasing

Our approach and the conventional one both solve the same memory puzzle. The
conventional one needs a certain size to do it; ours needs a bigger one. **How
much bigger** is the price of doing things our way, and it is the number a
sceptic would ask for first.

We have now measured it five times. Here is every answer, in order:

| when | the price |
|---|---|
| first pass, neither side tuned | 4–6× |
| both sides tuned | 4.0× |
| measured properly across sequence lengths | 3.1×, flat |
| after finding the conventional side was undertrained | 2.7×–5.9× |
| **after feeding both sides four times as much training** | **5.6×–8.2×** |

**Every single correction moved against us.** Not most of them — all four.

## Why that keeps happening

It is not bad luck. It is a bias with a mechanism.

Each time, the flaw we found was in how carefully something had been measured.
And we are naturally more careful with the side we want to look good. So the
under-measured side was ours' opponent every time, and fixing it helped them
every time.

The last round was supposed to end this. We quadrupled the training on **both**
sides specifically so neither could be starved. Before running it, we wrote down —
in advance — that we expected the number to move against us again, because four
guesses in a row landing the same way is itself evidence.

It moved against us again. And **the quadrupled budget was still not enough**: the
conventional model was *still* improving when the training ran out, right at the
size where its threshold sits. So even 5.6×–8.2× is a floor, not a figure. It
could get worse.

## The clause we wrote against ourselves

Before running, that experiment contained this sentence:

> If a fourth confound turns up, the honest conclusion will not be another
> correction but that this comparison is harder to make fairly than it looks.

A fourth turned up. So we are invoking it rather than running a sixth round. The
standing form of the result is a **bound with a permanent caveat**, and we are
not chasing it further. Each round has cost a full sweep and moved the number the
same way; there is no reason the sixth would be the last.

## What that leaves — and it is the interesting part

The ratio was never the reason to build this. Here is what has **never been
revised, not once**:

- **No backward pass.** It learns going forwards only.
- **No looking over every past position.** One fixed-size scratchpad.
- **Scramble the network and it is bit-identical.** Not similar — identical.
- **Take away half the machines permanently and it recovers.**
- **It finishes learning in one pass** where the conventional model needs
  thousands.
- **Its memory does not grow with sequence length.** The conventional one's does,
  which is why our memory cost goes from *worse* than theirs on short sequences to
  about half theirs on long ones.

Those are properties, not ratios. The ratio is what a sceptic asks for; the
properties are what actually decide whether this can run on ordinary machines
scattered across the internet.

## The honest bit about ourselves

Five experiments went into that one number. In the same stretch, **the thing this
project actually proposes has still not been built.**

Our design says each unit should predict its own next input, with no central
scorer. What we have measured instead has a single shared scorer bolted on,
because that is what the benchmark wanted. That shared scorer is exactly the
everyone-waits-for-everyone step this whole project exists to avoid.

So the model we have been carefully sizing **breaks our own first rule**, and we
noticed only when working out the network costs.

That is the real answer to "are we focusing on the right things": we were being
rigorous about a measurement while the central claim sat untested. Rigour on the
wrong question is still the wrong question.

Next is building the version with no central scorer, and checking it still
learns.
