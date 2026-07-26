# Note 009 — How the memory splits across machines, and what it costs

[Note 004](004-the-bandwidth-budget.md) did G4's arithmetic for an architecture
this project no longer has. It assumed units with a fan-out and asked what
fraction of connections cross the network. The local rule has no fan-out — it has
one `d × d` matrix — so the question has to be asked again, and this time against
a **measured** width rather than an assumed one.

**This note is analysis, not measurement.** Nothing here has been built or run.
It is arithmetic on top of one architectural argument, and the argument is the
part most likely to be wrong.

---

## IN PLAIN TERMS

Our rule keeps one grid of numbers. To spread it over many machines you have to
cut the grid up, and there are two obvious ways to cut it.

One of them forces every machine to add its answer to everyone else's before
anything can proceed — which is the everyone-waits-for-everyone pattern this
whole project exists to avoid.

The other doesn't. Each machine produces its own finished answers and never has
to wait. The price is that every machine needs a copy of one small piece of
information each step.

That copy is the whole story, and the interesting part is that **sending it
naively is impossible** while sending it the obvious sensible way is cheap — and
the delay that way introduces is precisely the delay we already proved costs us
nothing.

---

## 1. Two ways to cut the grid, and only one survives C1

Machine `m` holds part of the memory `M`. Retrieval is `r = M k`.

**By columns.** Machine `m` holds columns `C_m`. Then

    r[i] = Σ_j M[i,j] k[j]

sums over *all* `j`, so each machine can only compute a **partial** `r` for every
`i`, and the partials must be summed across machines. That is an all-reduce: a
barrier, moving data proportional to `d`, every step.

**This is a C1 violation** and it is the obvious way to do it, which is worth
noting — the natural implementation is the wrong one.

**By rows.** Machine `m` holds rows `R_m`. Then

    r[i] = Σ_j M[i,j] k[j]   for i ∈ R_m

is computed **completely** on machine `m`. No partial results, no reduction,
nothing to wait for. Storing is the same shape: `M[i,j] += v[i] · k_prev[j]`
needs `v[i]` for its own rows, which is local.

What it needs is the **full `k` vector**, every step, at every machine.

> **Row-split trades a reduction for a broadcast.** That is the central
> architectural fact, and everything below is about whether the broadcast is
> affordable.

## 2. The naive broadcast is impossible, and not marginally

If the machine holding the input sends `k` to all the others directly, that one
machine sends `(M-1) · d · 4` bytes per step:

| `d` | machines | bytes per step from one machine |
|---|---|---|
| 1024 | 100 | 0.4 MB |
| 1024 | 1000 | 4.1 MB |
| 10000 | 1000 | 40 MB |

At any usable step rate that is impossible on a home connection, and it gets
worse with scale in exactly the way that matters. **A design that broadcasts from
an origin has a single machine whose cost grows with the size of the network,
which is the shape of a coordinator even if nobody calls it one.**

## 3. A tree makes it cheap, and the latency is already paid for

Forward `k` as a tree with fan-out `F`: each machine sends to `F` others, and no
machine is special. Per-machine outbound becomes `F · d · 4` bytes per step,
independent of how many machines there are.

Per-machine outbound, MB/s, at `F = 8`:

| `d` | 1 kHz | 100 Hz | 10 Hz | 1 Hz |
|---|---|---|---|---|
| 32 | 1.02 | 0.10 | 0.01 | 0.00 |
| 128 | 4.10 | 0.41 | 0.04 | 0.00 |
| 1024 | 32.77 | 3.28 | 0.33 | 0.03 |
| 10000 | 320.00 | 32.00 | 3.20 | 0.32 |
| 100000 | 3200.00 | 320.00 | 32.00 | 3.20 |

A 10 Mbps home upload is **1.25 MB/s**. So the affordable region is roughly
`d · rate ≤ 40,000` at this fan-out — a width of 1024 at 10 Hz, or 10,000 at
1 Hz, or 32 at 1 kHz.

`F = 8` is chosen to sit inside note 004's measured limit of **`D ≤ 15`
destination machines** on a home link, which was derived independently and for a
different architecture. The two constraints agree, which is mild evidence both
are about the network rather than about either design.

**The tree costs depth: `log_F(M)` hops.** 2 hops at 64 machines, 4 at 4096. And
this is the part worth noticing:

> The latency a broadcast tree introduces is **exactly** the latency
> [g2-01](../../experiments/sweeps/g2-01-latency.txt) measured as costing
> nothing. Below the buffer bound, delay changes the learned weights not at all —
> bit-identically, 6/6 seeds. The tree's depth is absorbed by a mechanism that
> already exists and was built for a different reason.

That is the second time C2's delay tolerance has paid for something it was not
designed for; the first was making packet batching affordable in note 004.

## 4. What this does NOT cover, and it is significant

**The readout is a reduction, and the current model has one.** `y = W_o r` sums
over every dimension of `r`, so with `r` split across machines the readout needs
an all-reduce — precisely what row-splitting avoided everywhere else.

That is real, and the honest position is that it is an artefact of the benchmark
rather than of the design. MQAR asks for **one answer per query**, so the model
has one global classifier. [Note 002](002-which-credit-assignment-scheme.md)'s
actual proposal is that **each unit predicts its own next input** — under which
there is no global readout at all, and each machine's units score themselves
against inputs they already receive.

So the reduction is a property of the thing built to measure the mechanism, not
of the mechanism. **But it has never been built without one**, and until it has,
that claim is an argument. It is the largest untested assumption in this note.

**Also not covered:** how `W_k` and `W_v` (vocab × d) are distributed; the
bandwidth of the heartbeat channel that [note 003](003-the-churn-model.md)
requires; and any measurement whatsoever. Note 004's heartbeat arithmetic came
out negligible and probably still does, but "probably" is doing work there.

## 5. What would settle it

1. **Build the row-split and check it is bit-identical to the single-process
   version.** The same test g2-01 used for delivery: not "close", identical.
   If it is not, the decomposition is wrong somewhere.
2. **Build a version with no global readout** — per-unit prediction — and check
   it still learns. That is the claim §4 rests on and it is currently unsupported.
3. **Measure the step rate the task actually needs**, since the whole affordable
   region in §3 is a `d · rate` product and nothing here has measured `rate`.

Until at least (1), this note is a design argument with arithmetic attached. It
belongs in the record because the row/column distinction is genuinely load-
bearing and was not obvious — the natural implementation is the one that breaks
C1 — but it should not be cited as a result.
