# 037 — When length stops mattering

## The pattern that had held all along

Every difficulty in this project has grown with the length of the problem. Longer
problems needed wider machines, bigger clusters, more of everything. Every number
we measured was some power of problem length.

This is the first time that stopped.

## The measurement

Devices holding **one number each** — the smallest a device can be. 240 of them.
Accuracy by how many pool their answers together:

**With selective storage:**

| problem length | 1 device | 4 | 8 | 32 | all 240 |
|---|---|---|---|---|---|
| 96 | 0.238 | 0.776 | 0.919 | **1.000** | 1.000 |
| 192 | 0.238 | 0.774 | 0.920 | **1.000** | 1.000 |
| 288 | 0.238 | 0.779 | 0.917 | **1.000** | 1.000 |
| 384 | 0.238 | 0.776 | 0.918 | **1.000** | 1.000 |

Look at the columns. **The four rows are the same numbers to three decimal
places.** Not similar — identical. A problem four times longer is not harder at
all.

**Without selective storage**, the same devices:

| problem length | all 240 devices |
|---|---|
| 96 | 1.000 |
| 192 | 0.795 |
| 288 | 0.627 |
| 384 | 0.572 |

Falling steadily, and never recovering however many devices you add.

## Why the rows are identical

This is the arithmetic we'd been staring at for days.

Our system normally writes down every consecutive pair — 383 facts in a 384-step
problem, when only four matter. Turn that off and keep only the four, and **the
memory holds the same eight things no matter how long the problem was.** The
filler was never written, so it never mattered.

A query then retrieves from an identical situation whether it waded through 90
steps of noise or 380. Length has left the problem.

**Every scaling law we've measured came from storing the rubbish.**

## The honest qualifications, and there are three

**One device is still not enough.** Even with perfect storage, a single device
holding one number gets 0.238. You need a cluster — tens of devices, not hundreds.
Selective storage doesn't remove the need to pool; it makes pooling cheap.

**"8 devices" is really "8 devices on two runs out of three."** We ran each
setting three times, and at 8 devices one run consistently came in at 0.833 —
below the 0.9 bar — in every row. The check that caught this was written *before*
the experiment, precisely because a constant requirement was the answer I wanted.
The conservative number is **32**, where every run reaches a perfect score.

**And the gate is an oracle.** It's told which facts matter. No real system knows
that. This measures what perfect judgement would be worth, not what any
implementable rule achieves.

## So what would a real system do?

I read the biology this time rather than guessing. The mechanism is called
**synaptic tagging and capture**: mark a change immediately and cheaply, and let a
signal arriving *later* decide whether it survives.

That's exactly the right shape — it converts an impossible question ("is this
worth keeping?") into an answerable one ("was that worth having kept?").

But it doesn't work on *our* test, for a specific reason: the only thing that ever
reveals which facts mattered is the question at the end, and by then it's too late,
and the same facts are never asked about again.

**So the next step isn't a cleverer gate. It's a fairer test** — one where the same
things get asked about more than once, so that noticing what proved useful can
actually pay off. That's a small change to how we generate problems, and it keeps
everything measured so far comparable.
