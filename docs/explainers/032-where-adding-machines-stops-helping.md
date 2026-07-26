# 032 — Where adding machines stops helping

## The question, and why the earlier answer didn't count

Last time we grew the network and everything got better. That looked like
evidence that scale helps. It wasn't — we'd grown the team while keeping the
puzzle the same size, and a bigger team beats a smaller one at a fixed puzzle
every time.

The question that matters: **can a bigger network take on a proportionally bigger
problem?** That's the whole premise. If it can't, this is a small-model curiosity
rather than a route anywhere.

So we grew both. Each machine fixed at 16 slots, networks of 1 to 16 machines,
against problems of four different sizes.

## The answer: yes, up to a point — and then no

At the three shorter problem sizes, it works. Machines compound. You need more of
them as problems get harder, somewhat faster than the ideal, but it holds
together.

**At the longest problem, it stops.** Here's what each doubling of the network
bought:

| network | gain from doubling |
|---|---|
| 1 → 2 machines | **+0.334** |
| 2 → 4 | +0.258 |
| 4 → 8 | +0.082 |
| 8 → 16 | **+0.021** |

Each doubling buys about a third of the last. Keep going and it adds up to
roughly 0.79 — and the bar is 0.90. **It levels off below the target instead of
climbing to it.** Doubling from 8 machines to 16 bought two percentage points.

The pattern from the three shorter sizes predicted about **6 machines** would be
enough here. Sixteen aren't.

## The comparison that says what's actually broken

This is the number that matters.

Sixteen machines of 16 slots each is **256 slots in total**. We know from earlier
work what one undivided model of 256 slots does on this problem: it solves it.

Split into sixteen pieces, those same 256 slots reach **0.769**.

**So it isn't that our approach can't handle long problems. It's that cutting it
into independent pieces stops paying off past a point — and that point depends on
how hard the problem is.** The wall belongs to how we divide the work, not to the
underlying idea.

That's a much more useful thing to know than a flat failure, because how we
divide the work is a design choice, and the underlying idea isn't.

## Checking before believing a bad result

We're deliberately harder on results we like than on results we don't. But bad
results deserve some checking too — we've been burned once by reading "not
trained enough" as "not capable enough."

1. **Was it just undertrained?** Every configuration was measured at two training
   budgets. Doubling the budget moved nothing by more than 0.014, including the
   exact case in question. The plateau is real.
2. **Does another experiment agree?** A separate run with different settings
   measured the same configuration at 0.741. This one got 0.748. It reproduces.
3. **Is it the splitting or the method?** Answered above: the same total capacity
   undivided solves it.

## My own tool told me it was fine

The script that analyses this printed: *"G5 passes, and the tax is quantified."*

It fitted a trend line to the problem sizes where the network *did* reach the bar,
and never asked whether that trend explained the one where it didn't. **A missing
result is the most informative thing in the whole run** — it means the requirement
ran off the end of what we tested — and the tool quietly dropped it.

Fixed: it now takes its own trend line, extrapolates to any size that failed,
and reports the mismatch. On the same data it now prints the failure.

There's a small embarrassment attached. I wrote a *second* check at the same time,
meant to catch exactly this by spotting a curve levelling off. It didn't fire —
its threshold was set by guesswork and the guess was slightly too strict. I've
left the threshold alone rather than tightening it until it fires on the case
I'm currently looking at, which would prove nothing.

## What's next

The likeliest way this result is wrong: **our machines might just be too small.**
Every one held 16 slots. Earlier work found the penalty for splitting shrinks
sharply as machines get wider, and we never tested 32-slot machines. That's the
next thing to run.
