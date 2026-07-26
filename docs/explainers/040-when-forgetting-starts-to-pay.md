# 040 — When forgetting starts to pay

## The idea

Our system writes down everything it sees. On a long problem that means hundreds
of notes, only a handful of which anyone will ever ask about — and the rest are
noise that makes the useful ones harder to read.

The obvious fix is to let old notes fade. We tried that early on and it didn't
help, so it was shelved with a measurement saying not to bother. But the note
recording that result added a caveat: *it may still matter for problems long
enough that running out of room is the actual problem — which this one isn't.*

That was written months ago. This is the experiment where the condition finally
applies.

## The answer: yes, from about 768 steps

| problem length | never forget | best forgetting | difference |
|---|---|---|---|
| 192 | **0.978** | 0.957 | −0.021 |
| 384 | **0.961** | 0.937 | −0.024 |
| 768 | 0.725 | **0.761** | **+0.036** |
| 1536 | 0.089 | **0.337** | +0.248 |

The sign flips between 384 and 768. **The old note was right, and it named the
condition correctly before anyone could test it.**

## But the biggest number is a mirage, and it matters

That last row looks like a triumph. It isn't.

There's a floor to this task: **0.344** is what you'd score by a one-line guess
with no memory at all. At 1536 steps, the best forgetting arm gets **0.337** — it
is *at* the floor. And never-forgetting gets 0.089, which is *below* it.

**So the +0.248 is the difference between failing badly and failing at chance.**
Neither is working. The model is simply too small for a problem that long, and
that row measures a model out of its depth rather than a mechanism paying off.

The honest number is **+0.036 at 768 steps**, where both approaches are
comfortably above the floor and the comparison means something.

Real, and modest. Which is what it is.

## A first for this project

All four predictions written before the run turned out right — the crossover
existed, it was where we guessed, the advantage grew past it, and harsher
forgetting suited longer problems.

That's never happened here before; usually at least one gets refuted. The likely
reason isn't better guessing: **three separate checks run before the experiment
had already taught us how the mechanism behaved.** By the time predictions were
written, they weren't really guesses.

## And the clever mechanism still doesn't win

We also built a system that notices when a memory turned out to be useful and
copies it somewhere permanent. It works — it helps in three settings, by 0.03 to
0.06.

But every one of those is in the *harshest* forgetting setting, and none produces
the best result at its problem length. It rescues over-aggressive forgetting;
sensible forgetting beats the rescue.

And it doesn't help at all at 1536 — the one length where forgetting matters most.
That's the failure we predicted: **the mechanism needs enough already working to
have something worth confirming.** Where accuracy is at chance, "this memory
proved useful" is itself a coin flip, and copying it just copies noise.
