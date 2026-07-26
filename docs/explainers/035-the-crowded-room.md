# 035 — The crowded room

*The plainest account of the central limit. Everything else in this project is
detail hanging off it.*

## The machine isn't doing more work

That is the thing to get straight first, because the phrase "bigger machines"
makes it sound like they need to be faster.

**Each machine does exactly the same amount of arithmetic per step, no matter how
hard the problem is.** The same handful of multiplications, whether the sequence
is 48 steps or 384. Nothing needs to run quicker.

What needs to be bigger is how much a machine can **hear at once**.

## The room

Picture the memory as one room. Every fact the system learns gets shouted into it
and *stays there* — nobody stops talking. After 48 steps there are 48 voices.
After 384 steps there are 384 voices, all still going.

To answer a question, a machine listens for one particular voice.

**That is the thing that scales.** Not the listening effort — the amount of babble
the wanted voice is buried in.

## Width is microphones

Each machine has a set of microphones pointed into the room. "Machine width" is
just how many microphones it has.

One microphone in a quiet room picks out the voice fine. One microphone in a room
of 384 shouting people gives mush. But twenty microphones can be compared against
each other: the one voice you want lines up across all of them, and the babble
does not. It cancels.

So more voices in the room means more microphones needed to hear through them.
Each microphone does the same trivial job either way — you simply need more.

Measured, and it is very regular: **double the voices and the clarity of any
single microphone halves.**

## Which is why sharing helps so much

If every machine pools what it heard, the room has *all* the microphones working
together — hundreds of them. That is why the pooled answer stays fine with tiny
machines. On a problem where a lone machine needs **20** microphones, the pooled
answer manages with **6**.

## The problem, in one line

We want lots of small machines. The smaller each one is, the fewer microphones it
has — and harder problems put more babble in the room. So difficulty pushes toward
**fewer, better-equipped listeners**, which is the opposite of what "runs on
ordinary computers" needs.

Unless the machines share what they heard. Whether that escape hatch holds up is
being measured now.

## One honest wrinkle

The room picture predicts you would need microphones in direct proportion to the
voices. The measurement says it is gentler than that — about two thirds as fast.

So the machines are doing something slightly cleverer than dumb listening: they
**learn which babble to ignore**. How much of it they learn to ignore is not
something this analogy captures, and it is not something I can currently account
for.
