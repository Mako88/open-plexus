# 25. Measuring it properly made it worse

[Explainer 24](24-the-number-was-a-setting.md) ended with our headline figure
retracted: "running on ordinary machines costs four to six times more room" had
turned out to be measuring a setting we picked and never checked.

So we measured it again, properly. **The answer is 4.0×** — and getting there
honestly made it *worse*, not better.

---

## The temptation, stated plainly

The obvious fix was: tune our setting, re-measure, publish a better number.

**That would have been the same mistake wearing a friendlier face.**

The thing we compare against — the conventional model — has the *identical*
problem. Its own initialisation setting was fixed at one value through every
experiment and never varied either. Tune ours and not theirs, and you get a
flattering figure by exactly the mechanism that produced the wrong one.

And nobody could catch it from outside. The result would look carefully measured.
It would *be* carefully measured. It would just be carefully measured on one side.

So we swept both, took each at its own best, and compared those.

We also wrote down, in advance: **if the comparison model improves more than
ours, the price is bigger than we said, and that gets reported exactly as
loudly.**

---

## What happened

| | our rule | conventional | ratio |
|---|---|---|---|
| at the old untouched settings | needs 48 | needs 16 | **3.0×** |
| each at its own best setting | needs 32 | needs 8 | **4.0×** |

Our side got better — 48 down to 32. **The conventional model got better by
more** — 16 down to 8.

**So the honest price is 4.0×, and measuring it fairly made it worse.**

That's the prediction we flagged as the risk, and it's the one that landed. It's
being reported here with the same prominence a good result would have got,
because that promise is the only thing that makes the good ones worth anything.

## And the old number was luck

Worth being precise about how wrong the retracted figure was.

We'd said "four to six times". Measured *like for like* at the settings it was
actually taken at, it was **3.0×**.

So the old number wasn't conservative. It was **unfounded** — and it landed near
the right answer by coincidence, because two untuned settings happened to roughly
cancel. That's the worst way to be approximately right, because it feels like
evidence.

---

## What makes the new table trustworthy

Before believing any of it, the check that matters: **does it reproduce what we
already measured?**

- Our rule at the old setting, width 48 → **0.902**. The earlier experiment said
  **0.910**.
- The conventional model at its old setting → **0.935** at width 16 and **0.023**
  at width 8. The earlier experiment put its threshold exactly between those.

Two experiments, built separately, landing on the same numbers where they
overlap. That's what earns the rest of the table a reading.

---

## Two things worth noticing in passing

**The setting was never fixable by a better constant.** The best value moves
steadily with size — 0.25 at width 16, 0.5 at 24 and 32, 0.71 at 48. Smaller
models want smaller numbers. So there was no single right answer we'd merely
guessed wrong; pinning it to *any* fixed value mismeasures every size but one.

**We were one step from a cliff.** Push the setting to 1.41 and our rule scores
**0.000** at every size tested. Zero. The learning step scales as the *cube* of
that number, so a 1.4× setting is a 2.8× step and the whole thing diverges.

Our default sat at 1.0 — the last value before the mechanism breaks entirely.
That was luck, not judgement, and it's worth writing down as luck.

---

## The rule this bought

This has gone into our standards, and it earned its place by being *tested*:

> **A setting tuned on one side of a comparison must be tuned on all of them.**

The evidence for it is that following it changed the answer in the direction we
didn't want. Had we tuned only our own side, this experiment would have reported
the price *falling* from "4–6×" to about **2×** — a lovely result, apparently
rigorous, and roughly half the truth.

That's the kind of error that survives, because nothing downstream contradicts
it. Every later experiment would have been built on top of a number that was
carefully measured and wrong.

---

## So, honestly, where does that leave the comparison?

**Our rule needs four times the room** to do the same job as a conventional one
on this task.

In exchange, it never looks at everything at once, never sends anything
backwards, tolerates a scrambled and delayed network with *identical* results,
and survives half its machines vanishing.

Whether four times is a good trade depends on what you're buying with it — and
that's a judgement about the goal, not a measurement. But it is now a real
number, taken fairly, with the losing side of the argument reported as clearly as
the winning one.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
