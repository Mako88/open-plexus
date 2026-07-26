# 14. One missing ingredient

[Explainer 13](13-the-untrained-network-cant-do-it.md) found the untrained
network scoring at chance on our memory test. This explainer finds out *why* —
and the answer is a single missing operation.

**Adding it takes the score from 0.180 to 1.000.**

That number is less impressive than it sounds, and the last section explains
exactly why. But what it *does* settle was a real open worry.

---

## The suspicion

From other people's research ([explainer 10](10-the-test-we-nearly-built.md)):
what separates architectures that can do this task from those that can't isn't
size. It's whether the model can **change how it combines information based on
what it's currently looking at.**

Our untrained network can't. It blends everything it has recently seen in a
fixed, predetermined way, no matter what arrives.

And [explainer 13](13-the-untrained-network-cant-do-it.md) supports that: doubling
the network's size bought **0.008**. Whatever is missing, more of the same isn't
it.

---

## The experiment

Take the same untrained network. Change nothing about it. Add **one** operation:

> **"Find the last time you saw this exact symbol. Report what came next."**

That's it. It's called a lookup, and which position it reads depends entirely on
what symbol you're currently looking at — which is precisely the
adapt-to-your-input property we suspected was missing.

Same network. Same reader. Same data. Same everything else.

## The result

| | score |
|---|---|
| untrained network alone | 0.180 |
| **untrained network + one lookup** | **1.000** |
| just the lookup, network removed entirely | 1.000 |

Side by side with the previous finding:

- **Doubling the network's size: +0.008**
- **Adding one lookup: +0.820**

**A hundred times the effect.** Capacity was never the constraint. It was
always this.

And it holds when we double the number of things to remember — where the
untrained network drops to 0.143, the lookup stays at 1.000. That's the
signature of *retrieval* rather than *storage*: a lookup doesn't care how much
there is to look through.

The third row is worth a pause: **remove the network entirely and the score is
unchanged.** Once the lookup is present, the untrained network is contributing
nothing whatsoever.

---

## Now the deflation

**A score of 1.000 here is not impressive, and it would be dishonest to present
it as though it were.**

Our test asks: *pairs go by, then which value went with this key?* And the
lookup does: *find where this key appeared, report what came next.*

**That's the answer.** By construction. The lookup isn't cleverly solving the
task — it's a restatement of the task.

It's very nearly a tautology, and if we reported "1.000!" without saying so, it
would be one of those results that's technically true and thoroughly
misleading.

## So what did it actually buy?

Three things, and they're real.

**1. The room is reachable.** [Explainer 13](13-the-untrained-network-cant-do-it.md)
ended on a genuine worry: we'd found 0.82 of space above the untrained network,
but had no evidence *anything* could use it. If nothing could, that space would
be a mirage, and every future failure would be uninterpretable — we'd never know
whether our idea failed or the test was impossible for everything.

**Something can reach the top. The door isn't locked.**

**2. The gap has one name.** Not "the network is too small." Not "the reader is
too simple." Not "the task is too hard." **One specific missing operation**, and
we can point at it.

**3. The remaining question got much smaller.** It was: *is this task learnable
at all?* It's now: *can this one lookup be learned?* That's a far sharper
question, and a much better use of the expensive experiment we still owe.

---

## The mistake worth admitting

We ran two versions of the lookup: one that reports the *symbol* that followed,
and one that reports the network's *internal state* at that moment. We predicted
the second would be clearly worse — the internal state is a blur of everything
happening at the time, so the answer would have to be dug out rather than read
off. We guessed 0.55–0.85.

**It scored 1.000. Identical.**

And here's the annoying part: **we already had the evidence that this would
happen, from the previous experiment, and didn't use it.**

Explainer 13 described a check where we asked the reader to identify the symbol
currently being presented, straight from the network's internal state. It scored
**1.000**. Which means the state at any moment is a *perfect* encoding of the
symbol arriving at that moment.

So "read the state there" and "read the symbol there" were never two different
difficulties. **The earlier result said so plainly, and we predicted as though it
didn't exist.**

That's a distinct kind of error from the ones we've hit before. Previous
mistakes were about believing things nobody had measured. This one was about
having the measurement, in our own repository, from a week's-worth of work
earlier, **and not carrying it forward.**

Worth recording, because the fix isn't "be more careful about unverified claims"
— we're already doing that. It's something else: *check what you already know
before predicting.*

---

## Still owed

Nothing here learned anything. **We wrote the lookup by hand.**

The honest scoreboard:

- The task is *answerable* — the cheat that's told the answers gets 100%.
- The task is *reachable* — one hand-built operation gets 100%.
- The task is **not yet shown to be learnable** — nothing has worked it out for
  itself.

That last one still needs a real model trained from scratch, and it's the last
thing standing between us and a finished gate zero.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
