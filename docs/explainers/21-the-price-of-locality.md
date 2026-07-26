# 21. The price of locality

This is the one the whole project was for.

**It works. And it costs about four to six times more room than the
unrestricted version.**

There's also a second finding nobody predicted, which may matter more than the
first.

---

## What was being tested

Every system that has solved our memory test so far did it in a way that could
never run on strangers' computers. Two reasons, both fatal:

- **Everything looks at everything.** To answer, the model compares the current
  position against every earlier one at once.
- **Information travels backwards.** Learning works by pushing an error signal
  back through the entire model, from the answer to the beginning.

Those are what require a data centre. So we built the alternative.

## What the alternative does

Three steps, and the point is what each one *needs to know*:

**Store.** When one thing follows another, bind them together. The binding is an
"outer product" — a grid where each cell holds the product of one signal from
each side. **Each cell changes based only on what its own two ends are doing.**
Nothing consults anything else. It's the most local update there is — a
connection reacting to its own two endpoints, which is roughly what a synapse
between two neurons can actually do.

**Retrieve.** To ask "what followed this?", present the thing and read out what
comes back. Each output adds up its own incoming connections. Nothing pooled,
nothing normalised across the system.

**Learn the readout.** Compare what you predicted with what arrived; nudge in
proportion to the difference. The error is the unit's own, about its own next
input.

**There is no backward pass anywhere in this code.** Not disabled — absent.

---

## It works

Eight independent training runs at each size. Sorted, so you can see the spread
rather than an average hiding it:

| room to think | runs that solved it | scores |
|---|---|---|
| 16 | 0 / 8 | 0.00 0.00 0.00 0.00 0.00 0.03 0.04 0.04 |
| 24 | 0 / 8 | 0.00 0.01 0.01 0.02 0.03 0.05 0.06 0.09 |
| 32 | 0 / 8 | 0.15 0.20 0.21 0.21 0.22 0.23 0.34 0.34 |
| 48 | 5 / 8 | 0.85 0.88 0.89 0.92 0.93 0.94 0.97 0.97 |
| **64** | **8 / 8** | 0.98 0.99 0.99 0.99 0.99 1.00 1.00 1.00 |
| 96 | 8 / 8 | 1.00 × 8 |

For reference: an untrained network gets **0.18**, and a one-line cheat gets
**0.34**.

**The restricted version solves it.** The constraint that the whole project is
built on — nothing may need the big picture — is not fatal. That's what this
stage existed to find out, and it's the first real evidence either way.

## The price

The unrestricted version crossed over somewhere between **8 and 16**. This one
crosses between **48 and 64**.

> **Locality costs roughly four to six times more room.**

That's a *number*. Not "it's free," not "it's impossible" — a cost you can
multiply by a machine count and argue about. Our earlier bandwidth and memory
sums can now be redone against a width that's been measured rather than guessed.

---

## The thing nobody predicted

Look at the middle rows again. **32 gives 0.22. 48 gives 0.91. 64 gives 0.99.**

It slopes.

Now compare [explainer 19](19-all-or-nothing.md), where the *unrestricted*
model was tested the same way: **sixty runs, every one either ~1.00 or ~0.04, not
one in between.** All or nothing.

**The restricted version has a smooth curve where the unrestricted one had a
cliff.**

### Why

The two fail for completely different reasons, and it's worth seeing.

The unrestricted model has to **discover a trick**. Its internal machinery either
falls into the right arrangement or it doesn't, and that's a yes/no event — hence
two outcomes and nothing between.

Ours never discovers anything. **It does the right operation from the very first
update.** What limits it is *crowding*: every association gets layered into the
same grid, so retrieving one returns the right answer plus a smear of all the
others. More room means less smearing — and crowding eases off **gradually**.

> Finding a trick is all-or-nothing. Getting crowded is a matter of degree.

### Why that's good news, and slightly awkward

**Good news:** the thing we're proposing to build is *better shaped for learning*
than the thing it's competing with. Learning works by being slightly wrong and
improving. That needs a slope. A cliff gives you nothing to walk up — and the
cliff belonged to the *conventional* method, not ours.

Nobody would have guessed that. It came out of a table.

**Slightly awkward:** two earlier explainers concluded, on solid evidence, that
averaging scores here is meaningless and we should count successes instead. That
was right *for the unrestricted model* and is **wrong for this one.** Where
behaviour is graded, an average means something, and "how close did it get" —
which [explainer 20](20-the-knobs-that-do-nothing.md) called a malformed
question — is the right question again for the mechanism we actually care about.

Both conclusions were correct about what they measured. Neither generalised. Which
is a lesson about scope rather than about either result.

---

## Kept honest

- **One task, one setting, one rule.** This is a claim about this mechanism on
  this test, not about locality in general.
- **This is not distributed.** It's a locality-*respecting* computation running
  on one machine. Whether it survives real delay and machines vanishing is the
  next two stages, and neither has been touched.
- **Only part of it learns.** Two of the projections are frozen at random. That
  was deliberate — the strictest version of the question — and if learning them
  turns out to be necessary, that's a further cost not yet counted.

---

## A note on how it was run

The first attempt at this ran on John's machine, was projected at over half an
hour, and had to be killed for eating the computer. The rerun took **about two
minutes** on GitHub's runners — 48 independent jobs at once.

The redesign also made it *cheaper and better at the same time*: the original
spent 74% of its budget on sizes far past where the answer turned out to be. The
version that costs a sixteenth as much also resolves the interesting region more
finely.

Worth remembering: an expensive experiment is often just a badly aimed one.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
