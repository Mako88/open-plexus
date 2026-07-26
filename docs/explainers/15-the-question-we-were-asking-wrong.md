# 15. The question we were asking wrong

This was meant to be the big one — the single experiment that decides whether
our whole theory of learning is alive.

It came back negative. Then it turned out we'd been asking the question of a
test that couldn't answer it.

---

## The theory, and its one weak point

Our plan for how learning works
([explainer 6](06-who-is-to-blame.md)): **each part of the network guesses what
it's about to receive next, sees what actually arrives, and learns from the
difference.**

Elegant, and it dissolves the slow-internet problem entirely.

But it has an obvious dependency. **You can only learn from a guess if the thing
is guessable.** If what arrives next is pure surprise, every guess is wrong by
the same amount, and there's no signal in the error.

So: does a network's internal state actually contain any hint about what's
coming next? One small experiment, and it decides whether the plan lives.

---

## Standing on the predecessor's shoulders

The previous project built this exact probe, and its handover notes record
**three specific ways it went wrong.** We built all three guards in from the
start:

1. **Check you can see the present before asking about the future.** Their probe
   couldn't even identify what was happening *right now*, so its "the future is
   unpredictable" result was meaningless.
2. **Always report what a stupid constant guess scores.** Without it, "nothing
   is predictable" and "our probe is broken" look identical.
3. **Don't mix the boring stuff with the real stuff.** Theirs accidentally
   averaged a metronome-like signal in with the actual content and scored 0.797
   while predicting no content whatever.

Worth pausing on: **we didn't avoid these by being clever. We avoided them
because someone wrote down what went wrong.** That's the entire value of keeping
a record of failures.

---

## The result, and the headline we didn't write

Here's the number if you don't split things apart:

> **0.666, against a base rate of 0.030. Twenty-two times chance.**

That's a spectacular-looking result. *"An untrained network predicts its own
future input at twenty-two times chance, before any learning at all."*

**It's entirely meaningless.**

Split by what's actually being predicted:

| what we're predicting | score | stupid-guess baseline |
|---|---|---|
| the filler | **0.810** | 0.034 |
| the actual content | **0.058** | 0.051 |

All of it is the filler — the deliberately boring material we pad sequences
with, which runs on a repeating cycle. Predicting it is predicting that Tuesday
follows Monday.

The part that matters scores **0.058 against a baseline of 0.051**. Nothing.

**That's guard 3 catching exactly the failure it was written for.** Without the
split we'd have reported 0.666 and celebrated.

---

## So the gate is shut?

That's what it looked like. Content is unpredictable, so a prediction-based
learner has nothing to learn from, so our theory is in trouble.

Except we'd written down, *before running*, what a negative would and wouldn't
mean — specifically so a bad result couldn't be quietly reinterpreted into
something more comfortable afterwards.

And the top item on that list turned out to be exactly right, in a way that's
sharper than we'd anticipated.

---

## The actual finding: our test can't be predicted *by design*

Our memory test uses **randomly chosen** symbols. Which key comes next is drawn
from a hat.

**Nothing can predict a random draw.** Not our network, not a transformer, not
an oracle. A perfect predictor scores exactly the base rate there.

So the experiment didn't measure "our network is bad at predicting." It measured
**"we asked it to predict a coin flip."**

### And then the part that actually matters

We'd claimed — back when choosing this test — that it had a lovely property:
*the thing we want the network to learn (predict what comes next) and the thing
we score it on (answer the question) are the same thing.* One quantity, no
translation, so a failure is unambiguous.

**In our test, that isn't true.** And this probe is what exposed it.

When a question is asked, the right answer is a particular symbol — **but we
never put that symbol into the sequence.** It exists as a label off to the side.
The thing that actually comes next in the stream is more filler.

So "predict what comes next" and "answer the question" are **different
questions** in our test. The property we chose the test *for* isn't there.

The way this is normally done — and the way language models work — the question
is **followed by its answer**, right there in the sequence. Then predicting the
next thing *is* answering the question. Genuinely one quantity.

**We built the other version without noticing.**

---

## Where that leaves us

**The gate isn't shut. It was never properly opened.** We asked "can it predict
the important thing?" of a test that never says the important thing out loud.

The fix is small and obvious in hindsight: **emit the answer into the sequence
after each question.** Then this same probe measures exactly the thing our
learning theory needs, and a negative result would be a real negative.

That's the next thing to build.

---

## The pattern, now three for three

This is the third time a *check* has been worth more than the thing it was
checking:

- A check that the test was answerable at all found the test was impossible
  ([explainer 11](11-the-first-code.md)).
- A check that our measuring apparatus worked was the only reason we believed a
  result that confirmed our own prediction
  ([explainer 13](13-the-untrained-network-cant-do-it.md)).
- And now a probe aimed at our learning theory found a flaw in our *test design*
  instead.

None of these was the thing we set out to measure. All three were more valuable
than the thing we set out to measure.

There's a lesson in that which isn't "add more checks." It's that **the
assumptions that hurt you are the ones you never wrote down as assumptions** —
and controls are how those surface, because a control fails visibly when
something you never questioned turns out to be false.

---

## Housekeeping

We installed the standard maths library everything in this field uses. Pure
Python got us surprisingly far — an entire benchmark, a network, several
experiments — but training a model from scratch is where it stops being
reasonable.

The rule we've set: **the measuring instruments stay dependency-free.** The
ruler is the thing everything else is checked against, so it stays simple enough
to read line by line. Only the models get the library.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
