# 028 — The learning rate has been frozen for seven sweeps

**Status:** an audit, not a measurement. Found by applying the provenance habit
from CLAUDE.md's frozen-axis calibration to the whole g9 line, one cycle after
writing it.
**Affects:** the scale of every recovery number in g9-05 through g9-11. Not their
ordering within a cell.

---

## IN PLAIN TERMS

Every experiment in this line reports a score as a *fraction*: how much of a
cheating filter's advantage a real one recovers. A fraction has a denominator,
and the denominator here is how much better the cheating filter is than no filter
at all.

That denominator is not a constant of nature. It depends on how fast the model
learns — a setting we picked once, months ago, for a different configuration, and
have carried into every experiment since without looking at it again.

We already know it matters, because we measured it: on an older experiment, the
same cell had a denominator of 0.20 at one learning rate and 0.61 at another. A
factor of three, in the number every score is divided by.

Nobody has checked which value is right for the configurations we have been
running.

---

## The audit

`lr 0.05, FIXED on every arm` appears in the grid of **g9-05, g9-06, g9-07,
g9-08, g9-09, g9-10 and g9-11** — every sweep in the line. The scripts can sweep
it; `LEARNING_RATES = (0.02, 0.05, 0.1)` is their default. Every workflow passes
`--lr 0.05` and turns it off.

**Where the value came from:** g9-03's workflow, which ran at `d_model` 32 in one
process over delays 1 to 20. Since then the line has changed `d_model`, node
width via partitions, capacity, fade and reach — and carried 0.05 through all of
it.

## Why this is not a small point

[g8-01's re-summarisation](../../BACKLOG.md) measured the learning rate moving
the FLOOR arm, which is the denominator, by a factor of three:

    seq 768, half-life 0.5
      lr 0.10   ungated arm 0.387   oracle gap 0.612
      lr 0.02   ungated arm 0.80    oracle gap 0.196

That is the same quantity every recovery ratio in this project divides by. GOALS
was corrected for exactly this, and the correction said the advantage was
*overstated by about a factor of three* because the rate that most depressed the
baseline was the one being quoted.

**So the g9 line has been dividing by a denominator whose scale nobody has
checked at any of its configurations.**

## What it does and does not invalidate

**It does not change any comparison within a cell.** Every arm in a cell shares
the same learning rate, so `tag` against `window` against `combined` is a fair
comparison at 0.05 whatever 0.05 turns out to be. Every ordinal finding in the
line survives:

- the tag is flat across delay at `slots` 32 and cliffed at `slots` 8
- the best capacity tracks the delay, not the node
- the tag's marks are worth a great deal against an unmatched window and nothing
  against a matched one
- recovery declines with node width, and the task stops working below node 8

**It does change what the numbers mean.** "+0.16 of the oracle's advantage" is a
fraction of a quantity that may itself be two or three times larger or smaller at
a better-chosen rate. Statements of the form *the tag recovers a fifth of the
oracle* are the ones at risk; statements of the form *A beats B here and not
there* are not.

## What would settle it

Drop `--lr 0.05` from one workflow. The scripts already sweep
`(0.02, 0.05, 0.1)` by default, and `tools/recovery.py`'s refusals plus
`best_by` already choose among rates on an arm no prediction is about. It costs
three times the jobs of whichever grid it is added to.

**The cheapest useful version** is not a new grid at all: re-run g9-09's shape —
node width against the arms — with the rate swept, because node width is the
axis where the floor arm moves most (0.648 down to 0.171) and therefore where a
mis-chosen rate does the most damage to the denominator.

## AND THE ONES THAT NEVER APPEAR IN A GRID AT ALL

The audit above looked at GRID sections. Every g9-05 sweep also imports this line
from `g9_02_reward_gate.py`:

    D_MODEL, N_TRAIN, N_TEST, EPOCHS, KEY_SCALE, DECAY = 32, 200, 80, 6, 0.5, 0.997

**None of those six appears in any grid, in any sweep file, ever.** `lr` at least
was written down as FIXED and could be audited by reading. These cannot: they
arrive by import and are invisible to anyone reading the experiment's own
description of itself.

Two of them are not innocuous.

**`KEY_SCALE = 0.5` has already caused a headline error in this project, and
CLAUDE.md carries the calibration for it.** g3-02 measured a width-32 model at
**0.263** with unit-norm keys and **0.960** with the same keys multiplied by
0.71 — nothing else changed. The rule's own calibration reads: *the projection
scale was pinned at a value one step from where the mechanism diverges, and
silently produced the width curve the project's headline came from.* It is
pinned again, at 0.5, in every sweep of the g9 line, and it is not in a single
grid.

**`DECAY = 0.997` is the fast store's half-life — about 231 steps — and it is the
other multiplicative time constant on the store the tag's `fade` acts on.** g9-05
through g9-11 swept `fade` extensively and never moved `decay`.

> **MEASURED, and this warning was overstated.** Rewarded-binding recall at
> `slots` 16, counting only, no training:
>
> | delay | fade | decay 0.99 | decay 0.997 | decay 0.999 |
> |---|---|---:|---:|---:|
> | 8 | 0.99 | 59% | 66% | 66% |
> | 8 | 0.95 | 100% | 100% | 100% |
> | 20 | 0.99 | 56% | 56% | 56% |
> | 20 | 0.95 | 53% | 62% | 66% |
>
> `decay` moves recall by **at most 13 points**, and only in the corner where
> recall is not already saturated. At delay 8 with `fade` 0.95 it does nothing
> whatever. So freezing it shifted some numbers and **did not invalidate a
> conclusion** — *the fade is a reach dial* survives, with its calibration mildly
> conditional on 0.997.
>
> The original wording here implied the conclusion might be wrong. It said so
> without measuring, which is the rule this project puts first, and it is
> corrected rather than softened.

`EPOCHS = 6` and `N_TRAIN = 200` set the training budget, which decides whether
any arm has converged. Nothing in the line has checked that either.

## A correction to a tool this session leaned on

Reading the table above showed something about the recall x precision product
used to predict g9-06, g9-10 and g9-11. **A capacity-bound tag keeps a constant
number of writes** — `min(slots, writes)` per capture — so `kept` does not vary
with anything except the capacity. In the table above it is 496 and 512 at every
decay and fade.

Which means precision is recall divided by a constant, and the "product" is

    recall x precision  =  recall^2 x TOTAL / kept  ~  recall^2 / slots

**So it was never two independent quantities.** It is recall squared, penalised
by capacity. That is still a reasonable ranking function — it is why it ordered
g9-10's peaks correctly — but the reasoning attached to it, that a mechanism
trades recall against precision, does not describe a capacity-bound tag. The two
cannot move independently there.

It also explains the one case where the product failed: comparing `combined`
against `reward` in g9-11, `kept` differs BETWEEN the arms, so the constant does
not cancel and the product stops being a monotone function of recall.

## This is being measured

[g9-12](../../experiments/sweeps/g9-12-what-does-the-frozen-learning-rate-cost.txt)
sweeps the rate at node widths 64, 32, 16 and 8, at delay 8, and asks the three
questions this note raises separately: whether the best rate moves with node
width, whether the RATIO moves (the only one that matters, since the ratio
divides by the gap and can be stable while both ends move), and whether the arm
ORDERING moves — which is the reassurance this note offered as an argument
rather than a measurement.

It costs four jobs. `g9_05_the_tag.py` sweeps `LEARNING_RATES` *inside* a job
when `--lr` is omitted, so the rate axis triples the compute per job and adds no
jobs at all. The assumption that sweeping an axis multiplies the matrix is most
of why this went unmeasured for seven sweeps.

## What this is an instance of

CLAUDE.md's frozen-axis rule now carries a calibration about constants **carried
from another configuration**, written after g9-09 named the risk and g9-11
committed it anyway. That calibration ends with a habit: *when a sweep pins a
value taken from an earlier sweep, write down which cell it came from, next to
the pin.*

Applying it to one sweep surfaced that `fade` was also carried. Applying it to
the whole line surfaced this. **The habit found in two cycles what seven sweeps
of naming the risk did not**, which is the argument for writing provenance rather
than warnings.

---

*Related: [024 — what the gate costs a tiny node](024-what-the-gate-costs-a-tiny-node.md),
GOALS.md's corrected gating section, and `tools/recovery.py`'s third refusal.*
