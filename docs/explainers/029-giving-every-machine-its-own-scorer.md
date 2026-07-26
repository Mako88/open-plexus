# 029 — Giving every machine its own scorer

## The thing that was hiding in plain sight

Our model has been passing tests for months with a flaw at the end of it.

Picture a room of people each holding a few pieces of a puzzle. To answer a
question, all of them shout their pieces at one person in the middle, who
assembles them and says the answer.

That middle person is the problem. **The whole point of this project is that no
machine should ever have to wait for all the others.** But the middle person
can't speak until every single piece has arrived. One slow machine stalls
everybody.

It was in there because the test we use asks for one answer per question, so we
built one answerer. Perfectly reasonable — and then we passed four major
milestones on top of it without noticing that it broke our own first rule.

## What we did instead

Every machine gets its own scorer now. Each one looks only at its own pieces,
makes its own complete guess at the answer, and learns only from its own
mistakes. Nobody consults anybody.

So there are two ways to get an answer:

- **Pooled** — add up everyone's guess. Still a combining step, but a much
  smaller one: it happens only when a question is actually asked, it's the size
  of the vocabulary rather than the size of the model, and — crucially — it's
  **optional**, because every machine already has a complete answer.
- **Alone** — just ask one machine.

**If "alone" is good enough, the middle person is a nicety rather than a
requirement.** That's the measurement now running.

## Why this might work at all

Here's the part that isn't obvious. You'd think chopping the model into 8 pieces
gives you 8 tiny models, and tiny models are hopeless.

But that's not what happens. Each machine holds fewer *answers*, yet it still
searches the **full** address space — every machine needs the whole lookup key,
so each one is a narrow window onto a large memory rather than a small memory.

Early signs (one setting, one seed — not a result yet) are that this matters a
lot. A machine holding 16 of 32 slots scored **0.866**, where a genuinely small
model with 16 slots scores **0.559**. Same number of slots, very different
outcome, because one of them can still see the whole address space.

## Two things I got wrong, both caught by tests

**First:** I wrote a test asserting a machine's group is *identical* to a small
standalone model. It failed. It has to fail — the group's lookup key is 16 numbers
wide, the small model's is 4. My assertion was wrong; the code was right.

**Second, and it matters more:** I then wrote a test saying "if one machine leaves,
only that machine's answers are lost." That failed too. And it *should* have,
because **every machine needs the full key** — so a departing machine takes a
slice of something everybody uses.

So the honest version is: **a machine leaving degrades every surviving machine,
not just its own share.** Not by making anyone wait — nothing waits — but because
a piece of shared information has gone missing. We'd already measured that the
system recovers from this; what was missing was knowing *why* it hurt at all.

## And one test that was quietly worthless

We have a tool that deliberately breaks the code to check the tests notice. It
broke the part that decides which machine owns which slots — and **every test
still passed.**

The reason: my test had recomputed the model's arithmetic by hand instead of
running the model, so it never touched the code being broken. It looked
thorough and checked nothing.

That's the second time that exact trap has caught me here. A test that
reimplements what it's testing will agree with itself forever.

## Where this sits

The measurement is running now: 54 jobs, three model sizes, two sequence lengths,
four ways of splitting, three learning rates each — sweeping the learning rate on
*every* option rather than only the new one, because tuning one side of a
comparison and not the other is how we got four wrong answers in a row last time.

Four predictions are written down in advance, including which one I think is most
likely wrong, and a pre-agreed extra check to run if the result comes back in the
direction I'd prefer.
