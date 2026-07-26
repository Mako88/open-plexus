# 2. Why can't today's AI run on ordinary computers?

The honest answer is: **because of how they learn, not because of how they
think.**

That distinction is the whole reason this project exists, so it's worth ten
minutes.

## What a neural network is, roughly

Imagine an enormous set of dials. Millions or billions of them. Input goes in one
end, passes through all the dials, and an answer comes out the other end.

"Training" the network means: show it an example, see how wrong the answer was,
then nudge every dial slightly in whatever direction would have made the answer
less wrong. Repeat a few trillion times.

The thinking part — using an already-trained network — is comparatively easy. The
**training** part is what needs the data centre.

## The problem: figuring out which dial to blame

When the answer comes out wrong, you have to work out how much each individual
dial contributed to the mistake. This is called **credit assignment**, and it is
the central problem in the whole field. Remember that phrase — it comes up
constantly.

Today's method is called **backpropagation**, and it works backwards. You start
at the answer, work out the error, and pass that error backwards through the
network — each layer telling the layer before it how much it was to blame.

It works extraordinarily well. It's why modern AI exists.

## Why that method forces a data centre

Two properties of working backwards:

**Everything has to wait.** The layer at the front can't be updated until the
error has travelled all the way back from the end. Nothing moves until everything
moves. It's a lockstep march, and it happens thousands of times per second.

**The messages are gigantic.** You're not sending a little "you were wrong"
signal. You're sending information proportional to the number of dials — which is
billions.

Put those together: **billions of numbers, moved between every part of the
system, thousands of times a second, and everyone waits for the slowest.**

That is only affordable when the machines are bolted into the same racks on
dedicated hardware. Spread those machines across the internet and the whole thing
grinds to a halt — every step now waits for the slowest, most distant, least
reliable participant.

**The data centre isn't a preference. It's a direct consequence of working
backwards.**

## So what's the alternative?

Make each part figure out its own mistake, using only what it can see locally.

No backwards pass. No lockstep. No giant messages. Each piece looks at its own
little corner and improves itself.

**Nobody knows how to do this well.** That's the honest position. There are
promising ideas — several decades of them — and none has matched
backpropagation. Whether one *can* is precisely the question this project is
asking.

## The idea we're most interested in

The most promising candidate is beautifully simple: **have each part predict what
it's about to see next, then compare its prediction to what actually arrives.**

Why that's appealing:

- **There's no message that can be late,** because there's no message. Each piece
  gets its error by comparing its own guess to its own next input. The internet's
  slowness stops being a problem to work around — it stops being a problem at
  all.
- **It's what large language models already do.** ChatGPT is trained by
  predicting the next word. "Predict what comes next" is a proven objective, not
  a speculative one.
- **It doesn't need anyone to label anything.** If your AI is running on
  strangers' laptops, you can't hand every machine a neatly labelled answer
  sheet. Predicting your own next input needs nobody's permission.

This is our leading candidate, not our decision. We haven't built it, and we
haven't yet checked whether the people who've tried it already found out
something we should know first.

---

*Next: [What are the three rules everything has to obey?](03-the-three-rules.md)*
