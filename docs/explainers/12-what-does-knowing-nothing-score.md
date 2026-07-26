# 12. What does knowing nothing score?

The first real measurement. It refuted two of our own predictions, and the
refutation is worth more than the confirmations.

**Headline: we had the wrong idea of what "no better than chance" means on our
own benchmark. The bar is nearly three times higher than we thought.**

---

## Why measure nothing first

Before you can say a system learned something, you need to know what a system
that learned *nothing* scores.

If guessing gets 50% and your clever method gets 52%, that's not a result — but
without the first number it looks like one.

So we ran several strategies that involve no learning whatsoever, and one that
cheats outright.

---

## The lineup

- **Base rate** — always give the single most common answer. Never varies.
- **Random** — pick an answer at random.
- **Most recent** — answer with the last value you happened to see.
- **Positional** — answer with "the value from the pair in the same slot" —
  ignoring content entirely and just counting.
- **The cheat** — a solver simply *told* the answers. **Must score 100%.** If
  perfect knowledge can't answer the question, the question is broken.

The last one is the mechanical version of the bug from
[explainer 11](11-the-first-code.md). That one was caught by a human reading
output. Now it's a check that runs every time.

---

## What we predicted

Written down **before** running, so we couldn't quietly adjust them afterwards:

1. The cheat scores exactly 1.000 everywhere.
2. The base rate ≈ 1 ÷ (number of possible answers).
3. Random ≈ base rate.
4. **Most recent ≈ base rate.**
5. **Positional ≈ base rate.**
6. Only the answer-alphabet size moves the base rate.

We even noted which prediction we thought was shakiest.

---

## What happened

**1, 2, 3 and 6 held.** The cheat scored 1.000 in all thirteen configurations —
the task is answerable everywhere we ask it. The base rate landed within a
whisker of predicted every time. And it was completely unmoved by the
distracting material or the sequence length, exactly as it should be.

**4 and 5 were wrong, and badly.**

We predicted the two "stupid" strategies would score around the base rate of
**0.134**. They scored **0.349** and **0.346** — about **two and a half times**
higher.

And in one setting they scored **1.000**. Perfect. Every time.

---

## Why

Once seen, it's obvious — which is exactly why it needed measuring.

Our distracting filler is made of *key*-type symbols. So the only *answer*-type
symbols anywhere in the sequence are the handful of real answers.

Which means a strategy of **"just say any answer you've seen"** isn't guessing
from a big alphabet. It's guessing from a tiny one — and if there are only four
pairs, it has a one-in-four shot before you count lucky coincidences.

With one pair, there's only one answer in the whole sequence. Say it. **Always
right.**

It works out to:

> **floor = 1 ÷ (number of pairs) + a bit for lucky coincidences**

We checked that formula against both strategies across eight settings. Worst
disagreement: 0.016. **It fits.**

---

## Why this matters so much

Suppose we'd skipped this and built a model. It scores 0.30.

We'd have written: *"0.30 against a base rate of 0.134 — more than twice
chance. The approach shows real signal."*

**And we'd have been beaten by a one-line heuristic that understands nothing.**

That write-up would have been *honest*, *arithmetically correct*, and completely
wrong. It's the same shape as everything else this project keeps tripping over:
not a mistake anyone could spot afterwards, because the number really is more
than twice the base rate. The base rate was just the wrong thing to compare
against.

**The real bar at our reference setting is 0.344, not 0.134.**

---

## A bonus finding

The floor is set by **how many pairs we ask about**.

- 4 pairs → floor of **0.344**
- 16 pairs → floor of **0.180**

More questions, lower floor, more room for a real result to show.

[Explainer 10](10-the-test-we-nearly-built.md) already found — from other
people's research — that asking about *all* the pairs is what makes the test
discriminating. **Now we have our own measured, independent reason for the same
setting.** Two separate arguments pointing at one dial is a good sign.

We've also put the correct floor into the code itself, so anyone reading a score
sees the right bar next to it rather than having to remember this document.

---

## And the test-checker caught a fake test

Remember the tool that sabotages our code to check the tests notice? We pointed
it at the new baselines. **One sabotage got through.**

It replaced "use the most common answer" with "use the lowest-numbered answer."
Every test still passed.

Why: because all answers appear about equally often, *any* fixed answer scores
about the same. The score is decided by the answer distribution, not by which
answer we picked — so no score-based test could ever tell the difference. Our
tests were measuring something real, just not the thing they claimed.

The fix was to test the promise directly, on deliberately lopsided data where
"most common" and "lowest-numbered" genuinely differ.

**This is the second time a test has been caught asserting something that was
being held in place by an unrelated mechanism.** It's evidently a common way to
write a useless test, which is precisely why the sabotage tool exists — that
kind of test is invisible from the outside. It's green. It looks fine.

---

## Where we are

- The benchmark is answerable in every configuration. Verified mechanically.
- The real floor is known, has a formula, and is in the code.
- 28 tests, 11 sabotages, all caught.
- Two of our own predictions publicly refuted and written down as such.

**Still nothing that learns.** That's next, and it needs the first genuine
outside dependency — the maths library everything in this field runs on, which
isn't installed on this machine yet.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
