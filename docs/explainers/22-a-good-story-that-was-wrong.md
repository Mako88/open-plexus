# 22. A good story that was wrong

Last time we found something surprising and explained it. The explanation was
tidy, it fit everything we'd seen, and I liked it.

**This experiment killed it.**

The thing we *observed* is still true. The reason we gave for it is not — and it
was wrong in a way that's worth showing, because the story was genuinely
convincing right up until it made a prediction.

---

## The story

Our local rule stores every association by layering it into one grid of numbers.
[Explainer 21](21-the-price-of-locality.md) found it needs 4–6× more room than
the unrestricted version, and — the surprise — that it degrades *gradually*
where the unrestricted one was all-or-nothing.

The explanation practically wrote itself: **crowding.** Every association is
layered into the same place, so retrieving one gives the right answer plus a
smear of the others. More room, less smear. And unlike "did the model find the
trick or not", which is a yes/no event, crowding eases off *continuously* — so a
smooth curve is exactly what you'd expect.

Neat. Explains the gradualness, explains the width penalty, and it's the kind of
thing that sounds like understanding.

## The prediction it made

If crowding is the problem, then **having more things to remember must make it
worse.** Store four things and the smear is made of three; store eight and it's
made of seven.

We made that quantitative — the maths says the room needed should grow *in
proportion* to the number of things stored. With 4 items needing width ~56, that
predicts ~28 for 2 items and ~112 for 8.

Then we picked test sizes to bracket exactly those three points, and ran it.

## What happened

| room | 2 things | 4 things | 8 things |
|---|---|---|---|
| 24 | 0.004 | 0.027 | 0.067 |
| 32 | 0.098 | 0.225 | 0.298 |
| 48 | **0.922** | **0.910** | **0.890** |
| 64 | 0.995 | 0.991 | 0.975 |
| 96 | 1.000 | 0.999 | 0.997 |

Predicted crossover points: **28, 56, 112.** Every one of them actually sits
**between 48 and 64.** Quadrupling what has to be remembered moves the
requirement essentially not at all.

**And look at the top two rows.** At the smallest sizes, having *more* to
remember makes it **better** — 0.004 → 0.067, and 0.098 → 0.298. Roughly triple.

That's not a weak version of what we predicted. **It's the opposite sign.**

---

## What survives and what doesn't

Worth separating carefully, because it's easy to over-correct.

**The observation stands.** The gradual curve is real — it's right there in the
table, and the unrestricted version really was all-or-nothing across sixty runs.
Both of those are measurements and neither is affected.

**The explanation is dead.** "It's gradual because crowding is gradual" made a
prediction, and the prediction failed in sign.

That distinction is the whole discipline: a falsified claim gets *fixed*, not
softened, and what was falsified here is the **mechanism**, not the number. The
previous write-up has been corrected in place with a pointer to what killed it —
not quietly edited, because the fact that a plausible story survived a week is
itself worth leaving visible.

## A second, independent vote

The same run tested something else: does *deliberately forgetting* old
associations help? If crowding were really the problem, throwing away the oldest
entries should buy something somewhere.

| forgetting | score |
|---|---|
| none | 0.89 |
| very slight | 0.89 |
| moderate | 0.46 |
| strong | 0.22 |

**It buys nothing, anywhere.** Slight forgetting does nothing at all; more is
catastrophic; there's no sweet spot in between.

That was a *separate* prediction, made for a different reason, and it points the
same way. If crowding were the binding constraint, relieving it would help. It
doesn't help even slightly.

---

## Our current guess, labelled as a guess

Two things probably pull against each other:

- **More items = more crowding.** Real, but apparently weak here.
- **More items = more practice.** Each sequence contains one question per stored
  item, so eight items give four times the learning opportunities per sequence
  as two — at identical cost. When the model is starved, that dominates. Which is
  exactly where the sign flips.

And a guess about what the size requirement is *actually* set by: **the size of
the alphabet, not the number of things stored.** The keys are drawn from a fixed
set of 32 symbols that recur in every sequence. Crowding from a *fixed* set is
regular rather than random — and a *trained* reader can learn to cancel regular
interference. What it can't do is tell apart two symbols that aren't distinct
enough at that size. And the alphabet was held at 32 for this entire experiment.

If that's right, changing the alphabet size moves the requirement and changing
the number of stored items doesn't. **That's cheap to test, and it's next.**

Nothing above goes into the code as understood until it's been run. The previous
explanation was also plausible.

---

## The uncomfortable bit

I want to be straight about this one, because it's different from the earlier
mistakes.

Previous errors in this project were: believing something nobody had measured;
having a measurement and not using it; reasoning correctly to a sign-flipped
conclusion. All findable in hindsight by being more careful.

**This one wasn't careless.** The explanation was consistent with every number we
had. It was mechanistic rather than hand-wavy. It made the phenomenon feel
understood.

The only reason it didn't survive is that we made it **predict something
specific and then checked** — and the discipline of aiming the test at exactly
the predicted crossover points is what made the failure unmissable rather than
arguable. A vaguer experiment would have produced a shrug and a caveat.

That's the actual argument for working this way. Not that it catches sloppiness —
it catches the plausible, careful, satisfying explanation that happens to be
wrong. Those are the expensive ones, because nobody goes looking.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
