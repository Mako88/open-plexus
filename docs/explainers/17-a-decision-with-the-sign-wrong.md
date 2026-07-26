# 17. A decision with the sign wrong

Two corrections, one of them free money.

[Explainer 16](16-signal-that-teaches-nothing.md) found our learning method and
our test don't fit together. Working out what to do about it turned up a
mistake in our own reasoning — one where the fix costs a single word.

---

## First, correcting something *I* said

Last time, this was the explanation for the mismatch:

> Our test has a **step**, not a slope. You can't do the lookup at all, then you
> can, and there's nothing in between — so a learner has nothing to climb.

Neat, memorable, and **further than the evidence goes.**

What we measured is that an *untrained* network sits at chance. That's equally
consistent with a step *or* with a smooth slope whose bottom happens to be where
an untrained network starts.

In fact the slope version is probably right: something that retrieves correctly
30% of the time would presumably predict the answer about 30% of the time.
That's a slope.

The question I *meant* — whether a learner can find a path from "random" to
"retrieving" — is about how learning searches, and **we haven't trained anything,
so we can't say.** It was a nice-sounding story presented as a finding.

The symptom stands. The explanation was decoration, and it's been struck from
the record.

---

## Second, and better: we had a decision backwards

Now the useful part.

### The setup

Our test pads sequences with irrelevant filler. Two flavours:

- **Random** filler — unpredictable junk.
- **Patterned** filler — a repeating cycle, still irrelevant but predictable.

Way back, we reasoned: *our learner works by predicting things. Random junk is
unpredictable, so it gives the learner nothing. It would starve. Use the
patterned kind — still irrelevant, but at least there's something to learn from.*

That sounded obviously right. It's obviously wrong.

### The thing we missed

A learner doesn't improve by predicting things. It improves by **getting less
wrong than it currently is.** What drives it isn't how much error there is — it's
how much error it can *remove*.

And that changes everything about which filler is worse.

**Random filler can't be predicted by anything.** Not by us, not by a
transformer, not by a perfect oracle. So the error there is **stuck** — it's a
fixed cost that no amount of learning reduces. It just sits there. It doesn't
compete for the learner's attention, because there's no improvement available to
chase.

**Patterned filler can be predicted — easily.** And it's **83% of the sequence.**
So there's a huge, easy, immediately-available improvement sitting right
there — and it teaches the learner absolutely nothing about remembering things.

A learner turned loose on this would become an expert at continuing a counting
pattern. Because that's where all the winnable improvement is.

### An analogy

Imagine studying for an exam with two piles of practice questions.

- **Pile A** is written in a language you don't speak. You can't improve at it,
  ever. It's frustrating, and after a moment you leave it alone.
- **Pile B** is a thousand copies of "what is 2+2?" You can absolutely improve at
  it. You'll ace it. It has nothing to do with the exam.

We looked at these and said *"Pile A is useless, let's use Pile B."*

**Pile A is fine.** It's inert — you glance at it and move on. Pile B is the one
that eats your entire study week and teaches you nothing.

### So

> **Random filler is the right choice. Patterned filler is the harmful one.
> We had it exactly backwards.**

The fix is changing one word in a configuration. It's already built, already
tested, already measured. **We'd just been avoiding it on the strength of an
argument with the sign inverted.**

---

## Why this kind of error is worth dwelling on

Nothing was broken. No test failed. No code was wrong.

We reasoned carefully from a true premise — *random noise is unpredictable* —
to a conclusion that doesn't follow. And then we **built the test around it**,
and every experiment since ran in the configuration that reasoning chose.

The only reason it surfaced is that we went and measured how predictable each
kind of padding actually is, and then asked what that meant for a learner. The
numbers were sitting in front of us for two experiments before anyone put them
together.

That's the third distinct species of mistake this project has caught:

1. **Believing something nobody measured** — the literature that turned out to
   describe a different setup ([explainer 9](09-checking-our-homework.md)).
2. **Having a measurement and not using it** — predicting something our own
   previous experiment had already answered
   ([explainer 14](14-one-missing-ingredient.md)).
3. **Reasoning correctly to the wrong conclusion** — this one. Where the
   premise is true, the logic feels sound, and the sign is flipped.

The third is the hardest to catch, because there's nothing to check against.
No test can fail. It only surfaces when the reasoning gets forced up against a
number.

---

## Where things stand

Our learning method and our test still don't fit, and that's still open. Three
routes out are now written up properly — with what each costs, and a
recommendation — but the choice is a change of direction for the project, and
that's John's to make rather than something to slip into a commit.

The loop that's been running every five minutes is switched off while that's
decided. It's for doing work, not for waiting.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
