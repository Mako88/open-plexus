# 20. The knobs that do nothing

We built a test with difficulty knobs. We turned them all the way up against a
model that can actually do the test.

**Nothing happened. Fifty-six runs, every single one perfect.**

That's a useful thing to find out, and it changes the design of the next stage.

---

## What the knobs were for

Our memory test has two obvious dials:

- **How many pairs** to remember before being quizzed on all of them.
- **How long a stretch of junk** sits between seeing a pair and being asked about
  it.

Turning either up ought to make it harder. And it does — we measured that back in
[explainer 12](12-what-does-knowing-nothing-score.md). With more pairs, the
cheap-trick score drops from 0.56 to 0.18. The dials work.

**But we'd only ever measured them against things that fail anyway** — an
untrained network, and a one-line heuristic. Of course those get worse.

Nobody had checked what happens to something that *can* do it.

---

## Nothing happens

| pairs to remember | cheap-trick score | our model |
|---|---|---|
| 2 | 0.56 | **1.000** |
| 4 | 0.34 | **1.000** |
| 8 | 0.23 | **1.000** |
| 16 | 0.18 | **1.000** |

| length of junk | our model |
|---|---|
| 32 | **1.000** |
| 64 | **1.000** |
| 128 | **1.000** |

Eight separate training runs per row. **Fifty-six runs, and every one scored
exactly 1.000.**

Not "roughly the same." Not "slightly worse at the extremes." Identical to three
decimal places while one knob moved by a factor of eight.

---

## Why, and why it isn't a contradiction

The reason is the same thing [explainer 14](14-one-missing-ingredient.md)
found: our model works by **looking things up by content.**

Looking something up doesn't care how many things are in the drawer. Finding
`river` in a list is the same operation whether the list has four entries or
sixteen, and whether they're nearby or far apart. That's the difference between
*retrieval* and *storage* — and we'd already seen a hint of it, when the
hand-written lookup shrugged off a doubling of the pairs.

**So the knobs aren't broken.** They do exactly what we needed them for: making
weak approaches score badly, so that a strong approach stands out. That was gate
zero's whole purpose and it worked.

They just don't do the *other* thing — **grade a strong approach.** Both facts
are true at once, and keeping them apart matters.

---

## So we went looking for something that does

If task difficulty doesn't bite, what does?

We shrank the model instead. Same test, same everything — just fewer numbers for
the model to think with.

| model width | runs that solved it |
|---|---|
| 4 | **0 / 8** |
| 8 | **0 / 8** |
| 16 | **8 / 8** |
| 32 | **8 / 8** |
| 64 | **8 / 8** |

**That bites, and it bites like a cliff.** Somewhere between 8 and 16 the thing
goes from never working to always working. One step either side and it's
completely different — same all-or-nothing shape as
[explainer 19](19-all-or-nothing.md).

*(One honest footnote: at width 16 one run scored 0.969 rather than a clean
1.000 — the only in-between value we've seen in 120 runs. Right at the cliff
edge there's a hair's width of middle ground. Everywhere else, it's two
outcomes.)*

---

## What we now know difficulty is made of

Two things make this task hard, and **neither is in the task.**

- **How much room the model has to think** — measured here, a sharp cliff.
- **How much the model is told about where to look** — measured last time.
  Handing over the trick versus making it find the trick moved the failure rate
  from never to 40%.

Both are properties of *the learner*, not of the puzzle. Which, in hindsight, is
exactly right: the puzzle is easy once you know the trick. **The hard part is
acquiring the trick.**

---

## And this gives the next stage a much better shape

The plan was: build a learning method that obeys our restrictions, and see **how
close it gets** to a perfect score.

We now have something better.

> **Measure how much room a restricted learner needs, against how much the
> unrestricted one needs, on the same test.**

The unrestricted one crosses the cliff somewhere between 8 and 16.

- If the restricted one needs **64**, that ratio is *the price of our
  restrictions* — expressed in a unit that means something, rather than as a
  score we'd have to interpret.
- If it **never crosses at any width**, that's a completely different and far
  more serious answer.

And crucially, those two outcomes are **distinguishable**. "It scored 0.4" would
not have told them apart, and 0.4 is exactly the kind of number that averaging
over a two-outcome result produces.

That's the third time in a row that looking closely at *how* something failed
turned out to be worth more than the score itself.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
