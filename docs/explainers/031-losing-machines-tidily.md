# 031 — Losing machines tidily

## The test we'd been running was the wrong shape

Early on we checked what happens when machines drop out, by deleting a random
scattering of the model's parts. That was honest at the time: no part belonged to
anybody, so a random scatter was as good a guess as any.

Once each machine owned a specific block, the guess became testable. **A real
machine leaving takes its own contiguous block and nothing else.** A random
scatter leaves every machine slightly broken; a clean block leaves the survivors
perfectly intact, just fewer of them.

Those should not cost the same. So we measured both, removing exactly the same
amount either way.

## The answer: tidier is better, but only slightly

Machines leaving cleanly beat random damage in 7 of 8 settings — by about
**0.012**. So our earlier number was a touch pessimistic, and nothing we
concluded from it needs revisiting.

**The interesting part is where that small average hides something bigger.** At
the setting with the largest gap, here's each of the three runs:

| | run 1 | run 2 | run 3 |
|---|---|---|---|
| random damage | **0.842** | 0.917 | 0.938 |
| clean machine loss | **0.940** | 0.946 | 0.967 |

Clean loss isn't just better on average — it's far more *consistent*. Random
damage has a bad day; clean loss doesn't. **A real deployment cares about its
worst day, not its average one**, so reporting only the average would undersell
this.

## Why the difference is small: everyone loses a bit of the shared thing

We found earlier that every machine needs the full lookup key — so a departing
machine takes away a piece of something **everybody** was using, not just its own
share.

That damage is identical whether the loss was tidy or scattered. It sets a floor
on how much tidiness can possibly help, and that floor is why the gap is 0.012
rather than something dramatic.

So: **the dominant cost of losing a machine isn't which machine left. It's that
it took part of the shared thing with it.**

## A prediction I got wrong, and how

I predicted the advantage of tidy loss would *grow* with more machines. It
shrinks: +0.015 with four machines, +0.008 with eight.

My reasoning was that with more machines each one is narrower, so scattered
damage takes a bigger bite out of any single one. **That's true.** It just isn't
the only thing happening — with more machines the group answer is a bigger crowd,
which is more robust to *both* kinds of damage and squeezes the difference
between them; and the shared-key damage takes up more of the total.

So the reasoning wasn't wrong. It was **partial** — one correct mechanism
outvoted by mechanisms I hadn't listed. That's a harder mistake to catch than
being simply wrong, because there's nothing to spot when you re-read it.

## And a check on the last experiment that caught me out

Last time I reported that our tuning grid had gone wrong in four of six cases.
**It was six of six** — I misread my own output.

The fix wasn't to read more carefully. It was to write the check as code, so it
becomes an error rather than a line to skim. It found my mistake on its first run,
against the very experiment that prompted it.

Then it paid for itself immediately: this new experiment used the **identical**
grid and came out clean. The difference wasn't care — it was the situation. Which
is exactly why a habit wasn't good enough.
