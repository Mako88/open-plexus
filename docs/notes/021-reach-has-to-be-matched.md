# 021 — Reach has to be matched, so the tag is a mechanism

**Status:** settled by [g9-03](../../experiments/sweeps/g9-03-is-the-cliff-reach-or-cost.txt).
**Decides:** the next build.

---

## The question this was asked to answer

[g9-02](../../experiments/sweeps/g9-02-a-gate-that-reads-its-own-input.txt) found
the first thing in this project that recovered any of the oracle's advantage: a
gate that watches for a reward token and keeps the last few steps when it sees
one. It recovered about a fifth at delays 1, 4 and 8, and went **negative** at
delay 20 — worse than storing everything, because it kept the wrong nine steps
and threw away the right one.

The obvious next move was tagging and capture: mark the binding when it happens,
let the late signal decide. [Note 010](010-tagging-and-capture.md) has been
pointing at it for months.

**But the obvious move skipped a question, and the question was cheaper than the
mechanism.** The gate's reach was frozen at 8. A window of 32 also reaches a
binding 20 steps back. If that simply works, then the cliff is not about reach at
all — it is about what reach *costs* — and a tag is an engineering optimisation
rather than a new capability. Those are different projects, and one changed
constant separates them.

## What came back

Recovery, by reach (rows) and delay (columns):

| window | 1 | 4 | 8 | 20 |
|---:|---:|---:|---:|---:|
| 4 | **0.24** | **0.25** | −0.22 | −0.22 |
| 8 | 0.23 | 0.23 | **0.23** | −0.24 |
| 16 | 0.20 | 0.21 | 0.19 | −0.23 |
| 32 | 0.14 | 0.16 | 0.16 | **0.17** |
| 64 | 0.09 | 0.10 | 0.09 | 0.09 |

Two facts, pointing the same way.

**The cliff is the diagonal.** Every cell where the reach covers the delay is
positive; every cell where it does not is about −0.22. Delay 20 is not hard. A
window of 8 aimed at a binding 20 steps back is hard, and g9-02 measured the
window rather than the delay.

**More reach is monotonically worse.** Read down any column: 0.24, 0.23, 0.20,
0.14, 0.09. The best window is always the *smallest one that covers the delay*,
and every doubling past that costs roughly a fifth of what remains. A window of
64 recovers 0.09 no matter the delay — it reaches everything and resolves
nothing, because sixty-three steps of filler arrive with the binding and the gate
is barely gating.

This was the prediction registered as **most likely wrong**, on the grounds that
the store might be tolerant enough that thirty steps of filler barely register.
It is not tolerant.

## Why that makes the tag a mechanism

A window is a **span**, and a span has to be the right length. Too short and it
misses the binding. Too long and it drowns it. The table says both edges are
real, and that the useful range is narrow — a factor of two either way costs
most of the recovery.

**Nothing in the world tells a node how long the lag will be.** That *is* the
problem. A gate parameterised by a guess at the lag is not a solution to it; it
is the guess, wearing a parameter.

A tag marks **one binding** rather than a span. It does not need to know the lag
because it does not need to cover it. That is the entire difference between a
saving and a capability, and this table is what makes it the latter.

The cost argument survives and now comes for free. The gate holds `(value, key)`
per pending step — `2d` numbers, so reach 32 at width 32 costs 2048 numbers,
**twice the fast store itself**, on a project whose stated priority is minimum
node size. With derived keys a tag is a **token id**: keys regenerate from
`(seed, token)` and values are `wv[:, token]`, so every pending contribution is
re-derivable from one integer. Reach 32 for 32 numbers instead of 2048.

## What this does not settle

- **The lower edge was never probed.** At delay 1 the best window is 4, the
  smallest in the grid. The optimum may be 1 or 2. The monotone decline stands;
  "best is roughly `window = delay`" is supported only down to 4.
- **Delay 1 is the weak column.** The reward arm moves 0.125 across windows there
  against a seed spread of 0.094. At delay 8 it moves 0.356 against 0.088, and
  that is where the fall-off is decisive.
- **0.25 is a quarter of the oracle.** The best cell in the table recovers a
  quarter of what a gate with foresight gets. **The tag has to beat 0.25, not
  0** — and if it does not, the honest conclusion is that marking one binding is
  no better than guessing the span, which would be a result about this whole line
  of work rather than about the tag.

## The control, and why it is written down

All four other arms — `none`, `oracle`, `on-use`, `salience` — moved **0.0000**
with the window, across every delay and seed. None of them reads it, so this is a
wiring check rather than a finding, and saying otherwise would inflate a four-out-
of-four scoreline that is really one substantive prediction.

It earns its line because every number above is a ratio measured against those
arms. A floor that drifted with the swept axis is exactly the confound that
[withdrew g8-01's seq-1536 row](../../experiments/sweeps/g8-01-a-gate-without-an-oracle.txt),
and it cost a result the last time nobody checked.

---

*Next: build the tag. Related: [010 — tagging and capture](010-tagging-and-capture.md),
[018 — the fast store has no brakes](018-the-fast-store-has-no-brakes.md).*
