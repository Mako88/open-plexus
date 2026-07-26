# 16. Abundant signal that teaches nothing

[Explainer 15](15-the-question-we-were-asking-wrong.md) found we'd been asking
our most important question of a test that couldn't answer it. We fixed the
test and asked again.

The answer was still no — **but for a reason that turns out to be much more
interesting, and it overturns a fix we'd been rather pleased with.**

---

## The fix

Our memory test never actually *said* the answer out loud. The right answer
existed as a note off to the side, while the sequence itself carried on with
filler. So "predict what comes next" was never the same as "answer the
question," even though we'd chosen the test specifically because we thought it
was.

The repair: **emit the answer into the sequence, right after the question.**
Now predicting the next thing genuinely *is* answering.

That's how language models work, and it's how this test is normally posed. We'd
built the other version without noticing.

---

## Asked properly, the answer is still no

At the moment a question is asked, can the network anticipate the answer that's
about to arrive?

**0.135, against a stupid-guess baseline of 0.140.** Nothing. Slightly below
nothing.

## But notice what the question turned into

Fixing the test had a consequence we didn't see coming.

At a question position, the network is holding the key. Predicting what comes
next means producing the value. **That is the memory task.** Exactly the thing
[explainer 13](13-the-untrained-network-cant-do-it.md) already measured the
untrained network failing at.

So this experiment didn't tell us anything new about the network. It re-measured
the same thing from a different angle and got a consistent answer — which is
reassuring, and not informative.

**Making the probe correct made it circular.** At the answer position, *"is
there signal to learn from"* and *"have you already solved the task"* became the
same question. You can't use the second to bootstrap the first.

That's worth knowing and wasn't obvious in advance.

---

## The real finding: a fix that made things worse

Now put two numbers side by side.

| what the network is asked to predict | score | stupid-guess baseline |
|---|---|---|
| the filler | **0.824** | 0.042 |
| the actual answer | **0.135** | 0.140 |

There *is* loads of predictable structure in this test. **It's all in the
filler, and it's worth nothing.**

### Why that overturns something

Back in [explainer 6](06-who-is-to-blame.md) we hit a genuine conflict between
two of our own documents:

- One wanted **lots of irrelevant padding**, because an untrained network can't
  tell what to discard — that's what makes the test hard.
- The other objected that **random padding is unpredictable**, and our whole
  learning method runs on prediction. Random noise gives it nothing to learn
  from. It would starve.

We were pleased with the fix: **make the padding predictable but still
irrelevant.** A repeating pattern. Still needs discarding, so the test stays
hard — but now it's predictable, so the learner has something to work with. Two
tangled properties, cleanly separated.

**Measured, it doesn't work.** It doesn't cure the starvation. It replaces it
with a different disease:

- **Random padding** → the learner gets *no* signal. It starves.
- **Patterned padding** → the learner gets *enormous* signal, and **every bit of
  it is about the padding.**

A learner trained here would spend almost all its effort getting very good at
continuing a counting pattern. Which teaches it nothing about remembering
things.

We separated *hard-to-remember* from *impossible-to-predict* exactly as
intended, and created a third problem we never named: **easy-and-useless.**

---

## Why this matters more than it sounds

There's a shape here worth seeing.

Our learning method works by being *slightly wrong* and improving. That needs a
**slope** — get a bit better, predict a bit better, get a bit better again.

Our test doesn't have a slope. It has a **step**:

- Can't do the lookup? You predict the answer at exactly chance level.
- Can do the lookup? You predict it perfectly.

**Nothing in between.** There's no gradual improvement to climb, so there's
nothing for a prediction-based learner to follow. Meanwhile the only thing with
a nice smooth slope — the padding pattern — leads nowhere.

That's not a flaw in our learning idea. It's a **mismatch between our learning
idea and our test.**

---

## What we're not doing

We're not fixing it in this explainer, and that's deliberate. Three directions
exist and each needs arguing properly before anything gets built:

- Use a task with **gradually predictable** content — something language-like,
  where partial understanding buys partial prediction — rather than random
  symbols.
- **Keep this test for measuring capability** and use a different one for
  *training*. That breaks the neat one-quantity property on purpose, and
  honestly, rather than believing in it while it's false.
- Accept that a prediction-based learner needs a starting foothold this test
  can't give, and **revisit which learning method we picked.**

The written plan has been corrected where it recommended the padding fix. It now
says the fix was measured and doesn't work, rather than quietly dropping it.

---

## The scoreboard

This is a genuinely uncomfortable result, so plainly:

- Our learning theory is **not refuted**. Nothing here tested it.
- Our test is **not broken**. It measures capability well.
- **The pairing of the two is the problem.** The test can measure whether
  something can do the task, and cannot train the kind of learner we chose.

That's a real setback, found for the cost of two small experiments, before
building anything that learns. The alternative was finding it after months of a
learner that mysteriously refused to improve — and concluding, wrongly, that the
learning idea was dead.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
