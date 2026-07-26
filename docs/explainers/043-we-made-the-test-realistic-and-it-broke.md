# 43. We made the test more realistic and the model stopped working

The previous experiment was a bad result: no honest mechanism could replace the
cheat. This one was supposed to rescue it. It did not, and it found something
worse and more useful on the way.

## The excuse we were testing

Our devices have to decide what to remember. The best real candidate was **keep
what surprised you** — brains do something like this, flooding with chemicals
when something unexpected happens.

It failed. But we had an excuse ready, and it was a good one.

Our test sequences are **92% random padding**. Random padding is, by
construction, the most *surprising* thing in the sequence — nothing predicts a
random number. So a mechanism that keeps surprising things keeps the padding and
throws away the signal. It is not that the idea is wrong; it is that we built the
worst possible test for it.

**Real language is the opposite.** Common words are common everywhere and easy to
predict. Rare words are surprising *and* they are the ones carrying the meaning.
Surprise and importance point the same way.

So: make the padding realistic — a few very common tokens, a long tail of rare
ones — and see if the mechanism comes back to life.

We wrote down what we expected first, including a warning to ourselves that
scores would rise for *every* setup simply because predictable padding is easier
to predict, so we must measure the *ratio* and not the raw score.

## What happened

**Three of our four predictions were wrong, and one was wrong backwards.**

The mechanism did not improve. But the thing we got most wrong was the warning
itself. We predicted scores would go **up**. They went **down** — from 0.464 to
0.000.

Zero. Not "worse". Zero.

## Zero is a clue, not a result

A score of exactly zero, on every repeat, is not a hard task. Random guessing
should score *something*. So we looked at what the trained model was actually
saying.

**It had learned to say one word.**

At the realistic settings it output the same token at all 23,040 positions it was
asked about. Not a degraded answer — one answer, forever. And because the padding
alphabet and the answer alphabet don't overlap, a model that always says the
commonest padding token can never be right. Zero by construction.

| padding | score | different things it ever said |
|---|---|---|
| uniform | 0.700 | 40 |
| mildly skewed | 0.581 | 40 |
| realistic | **0.000** | **1** |

## Why this matters more than the experiment did

We were about to point this project at real text. That was the plan: synthetic
first, then a small book, measured against a simple baseline.

**Real text is skewed exactly like this.** Word frequencies sit right around the
setting where our model collapsed, and individual letters are more lopsided
still. So the plan had a wall in it that we could not see.

We found that wall in a **fifteen-job experiment that took ten minutes**, rather
than after building a text pipeline and spending a week wondering why nothing
learned.

The lesson isn't about padding. It's that **our learning rule has no defence
against a majority**. Give it something that happens most of the time and it will
learn to say that thing and stop. That has to be fixed before real language is
attempted, and it is now a known problem with a name rather than an ambush.

## The honest scorecard

- The excuse **is still standing, weakly**. *(Corrected after an audit.)* We
  could only measure two of the five settings — at the more realistic ones the
  model breaks down so badly that there is nothing left to compare against, and
  a comparison against a broken baseline measures nothing. So "our test was
  unfair" has not been ruled out; it has been left untested at exactly the
  settings that matter, because **the model cannot survive them yet.**
- We also learned our gate **doesn't control what we thought**. It decides what
  gets *stored*, but the model keeps *learning* from everything either way — which
  is why the cheating version was affected by a change that should not have
  reached it. Nobody had noticed that distinction until a number moved that
  shouldn't have.
- And we found a blocker on the road we were about to take, cheaply, before
  taking it.

Two of those three are things we did not set out to find. The experiment failed
at its stated job and paid for itself twice over anyway.
