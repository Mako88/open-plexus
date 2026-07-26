# 1. What are we actually building?

## The one-sentence version

We are trying to find out whether an AI can be built out of ordinary people's
computers, phones and consoles — the ones already sitting in their homes doing
nothing — instead of out of a data centre.

## Why that's an interesting question

Right now, serious AI needs a data centre. Not because the maths is secret, but
because of *how* the machines have to talk to each other. We'll cover that in
[explainer 2](02-why-ai-needs-data-centres.md), but the short version is: every
part of the AI has to check in with every other part, constantly, at enormous
speed. That only works if the machines are in one building, wired together.

The consequence is that **how big an AI you can build is decided by how much
money you have.** Data centres cost billions. A handful of companies can afford
them; nobody else can.

Meanwhile, there are *billions* of computers sitting idle in people's homes right
now. Already bought. Already paid for. Already plugged in. Collectively they are
an enormous amount of computing power that nobody is using.

**The question is whether we can build an AI that runs on those instead.**

## Why nobody has just done this

Because home computers are terrible in three specific ways that data centres are
not:

- **They're slow to talk to each other.** A message between two data-centre
  machines takes a fraction of a millisecond. A message between your house and
  someone's house in Australia takes about a fifth of a second. That sounds
  small. It is roughly a thousand times worse.
- **They're all different.** Different speeds, different amounts of memory,
  different operating systems, some ten years old.
- **They keep disappearing.** People close their laptops. Wi-Fi drops. Someone
  starts a game and wants their computer back.

So you cannot take the existing approach and spread it out. It falls apart
immediately. **You have to build a different shape of thing** — one that never
needs all the machines to agree, never needs an answer *right now*, and treats a
machine vanishing mid-thought as a Tuesday rather than a crisis.

That different shape is what this project is trying to find.

## Does anything work this way already?

Yes — your brain.

Brain cells only ever react to what's touching them. No brain cell has a picture
of the whole brain. Signals take a surprisingly long time to travel. And you lose
brain cells continuously, without noticing, forever.

So we know a system with these three limitations *can* be intelligent, because
you are one. That is genuinely useful, and it's the main reason to look at
biology at all. It is **not** a reason to copy brains in detail — see
[explainer 3](03-the-three-rules.md) for where we draw that line.

## What we're hoping for

Two things, in order:

1. **The big one:** a real path toward genuinely intelligent AI, built on
   hardware that already exists.
2. **The practical one:** replacing some of what today's large AI models do,
   without needing data centres to do it.

If it works, the size of the thing is limited by **how many people want to join**
rather than by how much money one company can raise. That is a meaningfully
different world.

## And if it doesn't work?

Then the useful result is a clear, honest answer to *why not* — which specific
one of those three problems is the killer, measured rather than guessed.

That's a real contribution too. As far as we can tell, nobody has written it
down. See [explainer 4](04-how-well-know-if-were-wrong.md) for how we'd find
out, and how we plan to find out **cheaply** rather than after two years.

---

*Next: [Why can't today's AI run on ordinary computers?](02-why-ai-needs-data-centres.md)*
