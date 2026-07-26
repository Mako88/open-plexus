# 19. All or nothing

We took away the help we'd been giving the model. It mostly coped.

But the *shape* of how it failed is the real finding, and it changes what we
should be measuring from here on.

---

## The help we'd been giving

[Explainer 18](18-the-one-word-that-decided-it.md) had a model teach itself our
memory task perfectly, just by predicting what came next. Genuinely encouraging.

But we'd quietly stacked the deck. The model's wiring was built so that *"look
back to where you last saw this symbol, and report what came **next**"* was the
natural thing for it to do. **We handed it the shape of the answer.**

So: take that away. Instead of being wired to look one step ahead, give the model
several candidate places to look and make it work out which one is useful.

- Two candidates: *the symbol itself*, or *the one after it*.
- Three: also *the one before*.
- Up to five.

Same everything else.

---

## It coped

| candidates it must choose from | runs that solved it |
|---|---|
| just the right one *(the old hint)* | **20 / 20** |
| two | **12 / 20** |
| three | **19 / 20** |

**The earlier result wasn't the hint.** The learning method finds the task
without being told where to look. And in every successful run it settled on the
correct place to look — so it really is doing what we think, not sneaking to the
answer some other way.

---

## Now the part that matters

Look at the individual runs rather than the averages. Here's every result for
the two-candidate case, sorted:

```
0.03  0.03  0.04  0.04  0.04  0.04  0.04  0.05
1.00  1.00  1.00  1.00  1.00  1.00  1.00  1.00  1.00  1.00  1.00  1.00
```

**There is nothing in the middle.**

Across all sixty runs we did — every condition, every random starting point —
**not one landed between.** Every single run either solved the task completely
or never got near it.

That's not how learning usually looks. Normally you see a spread: some runs good,
some mediocre, some poor. Here there are **two outcomes and no gradient between
them.**

### What that means

There's a *knack* to this task, and either training stumbles into it or it
doesn't. There's no such thing as being halfway to figuring it out. You don't
get partial credit for nearly having the idea.

Once a run finds it, it goes all the way to perfect. Until it does, it may as
well not have started.

---

## And something genuinely strange

Look at the table again:

- Two candidates to choose from → **fails 8 times out of 20**
- Three candidates → **fails once out of 20**

**More options made it easier.**

That's backwards from every intuition. A bigger search should be harder, not
eight times more reliable. And the third option we added was *useless on its
own* — "look at the symbol *before* the one you're attending to," which predicts
nothing.

We have a guess: the two-candidate case has a tempting wrong answer sitting right
next to the right one — *"just report the symbol you looked at"* — which is
plausible enough to get stuck on, and with only two options a run that drifts
toward it can't get out.

**But that's a story, not a measurement**, and it's written down as a story. This
project has a rule about that: something that works for unknown reasons can't be
improved on purpose, can't be predicted to survive a change, and shouldn't have
an explanation written into the code as though it were understood.

---

## Vindication with an asterisk

Two days ago I wrote that this task "has a step, not a slope" — you either can do
it or you can't, nothing in between.

Then I struck it out, because it was a claim about *how learning searches*, and
we hadn't trained anything. It sounded good and outran the evidence.

**Now we've trained sixty of them, and it's true.** Two outcomes, nothing
between.

Worth being precise about what changed: the claim wasn't right *then*. It was
unsupported, and striking it was correct. The experiment that could settle it is
the one we've just run — which is exactly what the correction said would be
needed. The claim is right and it was still wrong to assert it.

---

## What we'll do differently

This changes what we measure next, and it's a concrete change.

The plan was: build a learning rule that obeys our constraints, and see **how
close** it gets to a perfect score.

**That question is now malformed.** With two-outcome results, an average is a
fiction — the "0.615" in our table describes *no run that actually happened*. It's
the midpoint of a coin flip.

So from here: **count how often a method finds the answer, not how close it gets.**

A rule that solves it 3 times in 20 and a rule that scores 0.15 every single time
would look identical as averages. They're completely different results — the
first has found something real and needs better luck; the second hasn't found
anything.

**And a sobering note for expectations.** The model here is the *easy* case: it
learns by the standard method, with full access to everything. It still failed
40% of the time in one configuration. Whatever we build under our own
restrictions will have strictly less to work with.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
