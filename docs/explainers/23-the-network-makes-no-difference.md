# 23. The network makes no difference at all

The project's central promise about the internet, finally measured instead of
argued.

**Below a stated delay, the network changes nothing. Not "changes little" —
nothing. The learned numbers come out bit-for-bit identical.**

---

## The promise

Way back when we chose how learning would work, we made a claim that sounded
almost too good: **a slow network should cost us memory, not accuracy.**

The reasoning: nothing is *racing*. Nobody is waiting for an urgent message that
might arrive too late to be useful. Each part makes a guess about what's coming,
then holds onto that guess until the thing actually shows up. If it shows up a
fifth of a second later because it crossed the Atlantic, fine — the comparison
is the same comparison.

The cost is a bigger notepad. That's it.

That was an *argument*. A good one, and it has been carrying a lot of weight, and
nobody had run it.

## The test

Deliver every message late, by a random amount, arriving thoroughly out of order.
Give the receiver a buffer of a stated depth. Train the system. Compare the
learned numbers against a run with a perfect instant network.

And critically: compare them for **exact equality**, not closeness. Our own rules
demand that. "It degrades gracefully" is the *weaker* property, and a test that
allowed "close enough" would pass something that only degrades gracefully.

## The result

| buffer depth | delay applied | learned numbers identical? | score |
|---|---|---|---|
| 4 | up to 4 | **yes — 6 of 6 runs** | 0.989 |
| 16 | up to 16 | **yes — 6 of 6 runs** | 0.989 |
| 64 | up to 64 | **yes — 6 of 6 runs** | 0.989 |

Perfect network, for comparison: **0.989**.

The bottom row is the one to look at. **Every message delayed by up to 64 steps,
in sequences only 96 steps long, arriving in a thoroughly scrambled order — and
the system learns exactly the same numbers as one that saw a perfect stream.**
Not similar. The same.

## Why it works, and why that's the good kind of reason

Worth being precise, because the reason is better than the result.

**The exactness has nothing to do with our learning method.** Every message
carries the time it was *sent*, not the time it arrived. The receiver waits its
buffer depth, then processes things in the order they were sent — which, if
everything arrived in time, is the true order. The system downstream sees the
identical sequence and *cannot* behave differently.

So this property is bought by the **addressing scheme**, and it would hold for
*any* learning rule you put behind it. We didn't design the model to tolerate
delay. We removed the need to.

That's a much better kind of guarantee than one that depends on the model
happening to be robust.

---

## When it breaks, it breaks hard

Push the delay past the buffer and messages start missing their slot:

| kept | score |
|---|---|
| 100% | 0.989 |
| 56% | 0.379 |
| 29% | 0.018 |

We predicted the score would roughly track the fraction that survived. **Wrong.**
Lose 44% and the score doesn't drop 44% — it drops by more than half. Lose 71%
and it's essentially zero.

But we'd written down *why* this might happen before running it, and the reason
was right: **our test needs both halves of a fact to survive.** To answer "what
went with `river`?" the system must have seen `river→7` *and* the later question.
Lose either and that question is unanswerable. So the chance of an answerable
question is a *product*, not a fraction — and products of numbers below one fall
fast.

**That's a real design consequence.** Tolerance to loss is not the friendly
linear thing that "we can afford 20% packet loss" suggests. It's an argument for
keeping the two halves of a fact **on the same machine** — which is the third
independent line of reasoning pointing at that same conclusion, after bandwidth
and after failure domains.

## Lost and late are the same thing

We also tested messages vanishing entirely, rather than arriving late:

| | survives | score |
|---|---|---|
| 40% lost outright | 60% | 0.391 |
| delayed past the buffer | 56% | 0.379 |

**Indistinguishable.** The receiver can't tell them apart and neither can the
learning. That's a simplification worth having — one failure mode instead of two.

And loss is more survivable than expected. **10% of messages lost costs about
0.03.** For consumer hardware, where dropped packets are routine, that's
encouraging.

---

## The cost nobody had stated

This is the part we forced ourselves to write down *before* running, precisely
so it couldn't be skipped when the scores came back clean.

A buffer of depth 64 works perfectly. But a buffer of depth 64 means **nothing
can be acted on until 64 steps after it was sent.**

For a genuinely intercontinental system, the buffer needs to be around 150
steps — **deeper than these whole sequences are long.** The accuracy is
untouched. But at that point the system isn't *streaming*, it's **batching**.

> **"Latency is free" is true for throughput and false for
> time-to-first-response.**

For what this project is aiming at, that's probably an acceptable trade —
training a model is a throughput problem, not a conversation. But it's a real
limit, and it belongs in the design rather than being discovered by someone
waiting for an answer.

---

## Where that leaves us

**Three gates passed.**

- The benchmark is sound — answerable, reachable, learnable, with a measured
  floor.
- A rule that never looks at everything and never sends anything backwards
  solves it, at about 4–6× the size.
- **And a slow, scrambled, lossy network makes no difference to it whatsoever,
  below a bound we can state and have now checked.**

What remains: machines *leaving* rather than messages dropping, the bandwidth
budget, and whether any of this still holds as the thing gets bigger.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
