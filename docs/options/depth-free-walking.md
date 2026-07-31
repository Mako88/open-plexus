# Option record — letting the walk choose its own length

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/search.py`, `beam(any_length=True)` — `depth` becomes a maximum and walks of
  every length join one ranking. Off by default.
- `tests/test_search.py`, `TreatingDepthAsAMaximum`; the mutation
  `the-shorter-walks-never-join-the-ranking`.

---

## What was tried, and what came back

### The aid, priced: one extra hop costs 0.74 — `g42-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g42-01-the-walk-is-not-told-its-own-depth.txt
            script  experiments/g42_01_the_walk_is_not_told_its_own_depth.py
            task    CLUTRR gen_train23_test2to10, TEST split, 1,146 puzzles
            model   LocalAssociativeMemory + search.beam + the delta fold
            knobs   depth arms told / wrong-1 / free-10 / free-15
            scale   8 seeds, width 256, beam 8, per hop bucket

Every CLUTRR figure in this project hands the walk `len(chain)`, parsed from the puzzle.
The `wrong-1` arm is handed `len(chain) + 1` and exists only to price that.

At 10 hops, subset `all`: **`told` 0.9076 against `wrong-1` 0.1649**, which is near the
achievable floor of 0.0588. `wrong-1` falls below the random-fill bar at **every** depth,
not only at 6 and deeper as predicted.

**So the walk is not mildly assisted by knowing its depth; it is almost entirely dependent
on it.** This is decision 85's *"overshoot scores 0.000 in every direction"* reproduced on
the traversal rather than on the model's own `hops`, which is the different object the
record said could not be inherited.

### And selecting by endpoint score across lengths does not replace it — `g42-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g42-01-the-walk-is-not-told-its-own-depth.txt
            script  experiments/g42_01_the_walk_is_not_told_its_own_depth.py
            task    as above
            model   as above, `any_length=True`
            knobs   budget 10 against budget 15
            scale   as above

`free-10` reads **0.8666** at 10 hops and it is an artefact: the budget IS the answer
there, since the deepest chain in the data is 10. `free-15` gives **0.4254** in the same
bucket, and the two budgets differ by more than 0.03 in **9 of 9** buckets, by as much as
0.4412.

**The budget is the mechanism.** The residual aid the sweep record named before dispatch
turned out to be all of it.

The diagnostic says the same thing from the other side: `free-10` names the true chain
length **0.3508** of the time at 10 hops while answering **0.8666** correctly, so wrong
routes routinely fold to right answers — the additive invariant absorbing the error. End
task on a `free` arm therefore cannot be read as evidence the chain was found.

### Normalising the score is NOT the repair — `g42-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g42-01-the-walk-is-not-told-its-own-depth.txt
            script  a probe recorded in that sweep record
            task    CLUTRR, 40 puzzles per bucket
            model   as above
            knobs   raw `endpoint @ target` against cosine
            scale   3 buckets, seed 0

`beam` ranks by an unnormalised dot product, endpoint norms grow with walk length
(0.4086 at length 1, 0.6602 at length 10), and the beam picked length 10 in 28 of 40
puzzles whose true chain is 3. The hypothesis was that magnitude was deciding.

Ranking by cosine gives true-length accuracy **0.1500 / 0.0500 / 0.4250** against raw's
**0.1500 / 0.0500 / 0.4750** at 3, 6 and 10 hops. **The norm correlates with length
without deciding the choice.**

Kept because a refuted repair is worth more than an untried one: the next person to notice
the unnormalised score does not need to spend the two minutes again.

### What it costs: nothing — `g42-01`

    CONFIG  when    2026-07-31
            source  tests/test_search.py, TreatingDepthAsAMaximum
            script  tests/test_search.py
            task    a two-hop fixture
            model   n/a -- a property of the implementation
            knobs   any_length on against off
            scale   read count compared exactly

A length-k walk's endpoint is the value hop k+1 already fetches in order to follow, so the
shorter walks are scored out of a round trip that was happening anyway. The read count is
identical with the flag on and off, asserted by a counting reader rather than by prose —
which matters because kill-list #11 is measured in reads.
