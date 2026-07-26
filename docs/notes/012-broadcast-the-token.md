# Note 012 — Broadcast the token, not the key

[Note 009](009-splitting-the-memory.md) worked out what a row-split costs in
bandwidth and concluded that the affordable region is `d · rate ≤ 40,000` on a
home upload. **That bound assumed the key vector is what travels.** It is not the
only option, and the alternative removes `d` from the equation entirely.

This note was written while starting the distributed harness, because the first
thing that harness has to decide is what a node receives.

## What a node actually needs

A node owning rows `R_g` computes `r[R_g] = M[R_g,:] k`, which needs the **full**
key vector — that is the broadcast note 009 says the row-split pays for. Two ways
to obtain it:

| | on the wire, per step, at fan-out 8 | what the node must hold |
|---|---|---|
| **broadcast the key** | `8 · d · 4` bytes — 7,680 at `d=240`, 131,072 at `d=4096` | nothing extra |
| **broadcast the token** | **32 bytes, at any width** | all of `Wk`, or a way to recompute it |

At `d = 4096` that is a factor of **four thousand**.

## The catch, and why it is not a catch

Holding `Wk` is `vocab × d`, which is 39 KB at this project's scale and **205 MB**
at a realistic vocabulary of 50,000 with `d = 1024`. On a tiny device the second
number is disqualifying, which is presumably why note 009 did not consider it.

But `Wk` is a **frozen random projection**. It never learns. So it does not have
to be stored at all — it can be *derived* from the token, if each row is drawn
from its own seed rather than from one draw over the whole matrix.

Measured, at `d = 240`, `vocab = 41`:

    projection      row norm    mean |overlap|    max |overlap|
    whole-table       0.9912           0.04987          0.1966
    per-token         0.9925           0.04854          0.2155

Statistically the same object. **A node can regenerate any row it needs from the
token id and a shared seed, holding nothing.**

It costs **35 µs** per regeneration against a measured **16 µs** for a whole node
step — roughly tripling the node's compute. That would matter if compute were the
constraint. [`tools/step_rate.py`](../../tools/step_rate.py) measured that it is
not: the network binds by 21× to 380×, so a node running at 51 µs instead of 16
still sits about 120× above what its upload allows.

  > **Pay three times the compute, which is free, to remove the width term from
  > the bandwidth cost, which is binding.**

## What this changes

**Note 009's `d · rate ≤ 40,000` becomes `rate ≤ 39,000 Hz`, independent of
width.** Bandwidth stops constraining how wide the network can be, which was the
one place where G4 and G5 pushed against each other — G5 says minimum machine
width grows with problem difficulty, and note 009 said wider costs more to feed.
It no longer does.

**And it makes the node footprint concrete.** With keys derived rather than
stored, a node holds its own rows of the memory and its own slice of the readout,
and nothing else:

    vocab      d    w    total
       41    240    1    1.1 KB
       41    240   16   18.0 KB
     1000   1024    1    8.1 KB
    50000   1024    1  204.1 KB

**A width-1 node at this project's scale is about a kilobyte.** Even at a
realistic vocabulary it is a fifth of a megabyte, and that is dominated by the
readout — `vocab × w` — rather than by anything to do with the network's size.

## What is not claimed

Nothing here has been run across two processes, let alone two machines. This is
arithmetic plus one statistical check, and it is exactly the kind of analysis note
009 turned out to have got wrong in a detail. **The specific risk is the readout
column:** each node's answer is `vocab`-sized, and pooling still has to move those
somewhere. Broadcasting tokens makes the *input* side free; it says nothing about
the output side, which g4-01 established is optional but not absent.

Nor has the per-token projection been substituted into the model and re-measured.
It is statistically equivalent, which is not the same as verified — and this
project has been caught before by "statistically equivalent" changes that moved a
result.
