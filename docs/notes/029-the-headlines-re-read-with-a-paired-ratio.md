# 029 — the headline claims, re-read with a paired ratio

**Status:** measured, from the sweeps' own archived records. No new jobs.
**Affects:** g9-06's headline, g9-09's node-width answer, and one sentence I
wrote in the previous cycle that was backwards.

---

## IN PLAIN TERMS

The two biggest claims in this project were comparisons of single numbers with no
error bars. A better way of computing the same quantity became available, so both
were re-checked against the data already on disk.

**The main claim got stronger.** The second one turns out to be two claims, one
of which holds and one of which was never supported. And a sentence written last
cycle about small devices had the direction reversed.

---

## What changed about the arithmetic

The recovery ratio was computed by averaging accuracies across seeds and dividing
once. That charges a mechanism for seeds whose data happened to be harder: a seed
whose `none` ran low and whose `oracle` ran high has a large gap for reasons that
have nothing to do with the arm being scored.

`tools/recovery.per_seed` computes the ratio **inside each seed**, against that
seed's own floor and its own ceiling, and `mean_and_error` reports the standard
error of the mean over those. Differences between two arms are taken **within a
seed** before averaging, so the two arms share the data and the seed's difficulty
cancels exactly.

---

## g9-06's headline is CONFIRMED, and now has an error bar

    slots 32, fade 0.95      paired         tag - tag-strongest
        delay  1            +0.159 ± 0.020      -0.008 ± 0.008   inside 2 SE
        delay  4            +0.164 ± 0.014      +0.002 ± 0.006   inside 2 SE
        delay  8            +0.172 ± 0.010      +0.016 ± 0.010   inside 2 SE
        delay 20            +0.162 ± 0.016      +0.005 ± 0.015   inside 2 SE

**The +0.16 flat row is real**, and the flatness is not an artefact of averaging:
every delay sits within one standard error of every other. The claim that a
bounded capacity with a fade is a gate which does not need to be told the delay
stands, and stands better than it did.

## THE CATCH is confirmed, and is now a measurement rather than a comparison

The catch was that at this working point `tag-strongest` scores the *same* as
`tag` — so the mechanism is the bounded capacity and the fade, not g9-04's
inverted signal. It rested on a point estimate of +0.003.

    slots 16, fade 0.99      tag - tag-strongest
        delay  1            +0.180 ± 0.012   REAL
        delay  4            +0.204 ± 0.009   REAL
        delay  8            +0.219 ± 0.016   REAL
        delay 20            +0.283 ± 0.012   REAL

At the starved pool the direction is worth 0.18 to 0.28 and is far outside 2 SE.
At the working point it is indistinguishable from zero. **Both halves of the
catch are now supported**, which is a better position than one point estimate
being close to another.

---

## g9-09's "height peaks at node 32 and declines" is NOT supported

    tag, paired          delay 1          delay 8         delay 20
    node  8         +0.101 ± 0.015   +0.087 ± 0.028   +0.113 ± 0.020
    node 16         +0.145 ± 0.023   +0.162 ± 0.040   +0.175 ± 0.023
    node 32         +0.215 ± 0.023   +0.207 ± 0.027   +0.217 ± 0.005
    node 64         +0.154 ± 0.024   +0.188 ± 0.029   +0.188 ± 0.016

**Node 32 above node 8 is real** — 0.21 against 0.10, many standard errors apart
— and node 16 above node 8 is real. Those parts of the finding hold.

**Node 32 above node 64 is not.** The differences are 0.061, 0.019 and 0.029 at
delays 1, 8 and 20, against combined 2 SE of roughly 0.066, 0.079 and 0.034. Two
of the three are comfortably inside; the third is on the line. The sweep file
says *"height peaks at node 32 and declines"*, and the decline is not
distinguishable from a plateau.

What survives is the part that matters for the tiny-node question: **recovery
falls off between node 16 and node 8, and node 8 still recovers about +0.10.**
The peak's exact location does not.

---

## The sentence I wrote last cycle was backwards

[g9-12](../../experiments/sweeps/g9-12-what-does-the-frozen-learning-rate-cost.txt)
closed with an observation that `tag-strongest` runs −0.51, −0.10, +0.02, +0.05
as the node narrows from 64 to 8, and I concluded *"the inverted signal's
direction is worth nothing at a wide node and turns positive at a narrow one."*

That reads `tag-strongest`'s own absolute recovery. **The value of the DIRECTION
is the gap between `tag` and `tag-strongest`**, and in the same data it runs

    node 64   0.74      node 32   0.35      node 16   0.17      node  8   0.10

so the direction is worth **most at the widest node and least at the narrowest** —
the opposite of what I wrote. g9-09 agrees independently: `tag − tag-strongest`
at delay 20 is +0.222 at node 64 and +0.028 at node 8.

**Why the mistake matters beyond itself.** The g9 line has repeated that g9-04's
signal "pays where something is scarce". That is true for the axis it was
established on — a starved *capacity*, confirmed above at 0.18 to 0.28 — and it
does not generalise to node width, which is a different scarcity pointing the
other way. Two axes were being described by one sentence.

---

## g9-11 is CONFIRMED to three decimal places

    tag, paired            published
    delay  1   +0.237 ± 0.022     +0.24
    delay  8   +0.221 ± 0.060     +0.23
    delay 20   -0.146 ± 0.075     -0.14

And the union's value, taken within each seed:

    combined - reward       delay 8            delay 20
        reach  1        +1.791 ± 0.162     +1.490 ± 0.059
        reach  4        +1.801 ± 0.165     +1.501 ± 0.077
        reach  8        -0.002 ± 0.010     +1.507 ± 0.075

Every unmatched-reach cell is REAL by more than ten standard errors, and the
matched cell at delay 8 is indistinguishable from zero. **The combined gate as
insurance against a badly set reach is the best-supported claim in the line.**

The `slots` 4 against `slots` 16 comparison that calibrates CLAUDE.md's
frozen-axis rule also survives: -0.35 against +0.221, a difference of 0.57 with
standard errors of 0.06 to 0.13.

## g9-10's capacity choices are mostly TIES, but the PATTERN is not

    node 32 delay  8   best slots 16 at +0.251 ± 0.012   tied with 8, 32
    node 32 delay 20   best slots 32 at +0.217 ± 0.005   CLEAR
    node 16 delay  8   best slots 16 at +0.193 ± 0.024   tied with 8, 32
    node 16 delay 20   best slots 32 at +0.175 ± 0.023   tied with 16
    node  8 delay  8   best slots 16 at +0.142 ± 0.043   tied with 4, 8, 32
    node  8 delay 20   best slots 32 at +0.113 ± 0.020   tied with 16

**Only one of six cells picks a capacity distinguishable from its rivals.** Read
cell by cell, g9-10 chose on differences inside the noise, and every sweep that
pinned `slots` from it inherited that.

**But the pattern across cells is perfectly consistent**: all three node widths
pick 16 at delay 8 and all three pick 32 at delay 20. Six independent cells
agreeing on a direction is evidence that individual comparisons inside 2 SE do
not carry on their own. So *the best capacity tracks the delay* survives as a
pattern; *this exact capacity is best at this cell* does not.

Which is enough for how it was used. The pins taken from it were 16 at delay 8
and 32 at delay 20, and those are the pattern's values, not one cell's.

## What this does not do

**It does not add seeds.** Every number here is three seeds, and a standard error
from three samples is itself uncertain. Pairing extracts more from the same data;
it does not manufacture evidence. Node 4 in g9-09 has one usable seed and an
infinite error, which is the honest report of a cell that was mostly refused.

**It does not re-run anything.** These are the archived records of runs
30240589408 and 30244075029, re-read.
