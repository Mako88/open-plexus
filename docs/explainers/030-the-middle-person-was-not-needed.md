# 030 — The middle person wasn't needed

## What we asked

Last time: our model had one machine in the middle that everyone else had to
report to before an answer could come out. That's the everybody-waits-for-
everybody step this whole project exists to avoid, and it had been sitting there
through four major milestones.

We replaced it. Every machine now has its own scorer, learns only from its own
mistakes, and produces a complete answer by itself. Then we measured what that
cost.

## The answer: at a decent size, nothing at all

With the model split **eight ways**, accuracy was **1.000** — exactly the same as
the version with the middle person.

Not "close enough". The same.

And the stronger result: **one machine, on its own, ignoring all the others,
scored 0.949.** Split four ways instead of eight, one machine alone gets 0.996.

That's the number that matters. It means the combining step is now a *nicety*
rather than a *requirement*. A machine that can't afford to talk to the others
still answers, and answers well.

## Why splitting doesn't wreck it

This was the prediction I most wanted to check, and it held clearly.

Each machine holds fewer answers — but it still searches the **whole** address
space, because every machine needs the full lookup key. So it's a narrow window
onto a big memory, not a small memory. Those are very different things.

Hold each machine's size fixed at 16 slots and grow the network:

| network | one machine scores |
|---|---|
| 2 machines | 0.861 |
| 4 machines | 0.918 |
| 8 machines | 0.949 |

A genuinely small 16-slot model scores **0.559**.

So the same machine, same size, gets *better* the more company it has — because
the shared address space it's searching got bigger. **It's the addressing that
was doing the work, not the storage.**

## What I got wrong, in the harmless direction

I predicted splitting eight ways would cost about 0.10 in accuracy. It cost
**0.010**. Right direction, wrong by a factor of ten — I was too pessimistic.

Worth noting because it's the first time in a while an estimate here has been
wrong in the flattering direction rather than the other one.

## The catch, and it's a real one

We tune a setting called the learning rate, trying three values. The tool prints
which one each option picked, precisely so we can spot a rigged comparison.

**In four of the six rows, every option picked a value at the very edge of the
three we tried.** That means the best value is *outside* our range and everything
in those rows is under-tuned.

So the exact penalty figures away from the top of the scale are provisional.
What survives regardless:

- **The 1.000-vs-1.000 result**, because you cannot tune your way above a perfect
  score.
- **The direction**, which was consistent in all six rows.
- **The narrow-window finding**, which was compared against separately-tuned
  numbers.

We're **not** re-running it. The claims that matter can't move, and this project
just spent five experiments chasing one number that got worse every time. The
lesson we wrote down from that was: publish the bound, name the caveat, move on.

## Where this leaves things

The biggest untested claim in the project is no longer untested. The middle
person was scaffolding we'd built for the test we were using, and it turns out
the model runs fine without it.

Running now: whether it matters *which* machines drop out — a random scattering
of broken parts, versus whole machines cleanly leaving.
