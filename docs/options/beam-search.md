# Option record — `search.beam`, branch at every step

> **RECORD ONLY. This file carries no status.** Whether this option is chosen, refused,
> untried or live-both lives in `DECISIONS.md` and nowhere else.
>
> **Only events are recorded here, and events do not un-happen.** Every entry says what
> was tried, what the model looked like when it was tried, and what came back — which is
> why this file cannot go stale. **Absence means untried**; there is no "gaps" section,
> because that is status and it rots.
>
> **The model state is recorded per entry** because a number taken at one task, depth and
> width is not evidence about another. This option is the reason that rule exists: its
> headline figure was quoted across regimes twice.

---

## What exists

- `openplexus/search.py` — `walk_from`, `candidates`, `search`, `beam`, `margin`.
  `beam` takes `width`, `branches`, `prune_every`, and an injectable `reader=`.
- `LocalMemoryConfig.search_beam_width` and `search_prune_every`; `run()` calls `beam`
  when the width is ≥ 1 and `search` otherwise.
- `tools/clutrr_recovery.py` (chain recovery), `tools/prune_period.py` (rendezvous
  period), `tools/walk_rounds.py` (round trips), `experiments/g21_01_*` (the end task).

---

## What was tried, and what came back

### Search was refused twice, on the arithmetic of its day — `111`, `107`

**Model state:** steps 1 and 3 of the traversal at 0.710 and 0.677.

`111`: *"you cannot search your way out of noisy primitives, because the verifier is built
from the primitives."* `107` had already declined the pair-key traversal beneath it — *"a
perfect traversal buys 0.05"* — because compounding those step accuracies left nothing.

### The condition expired — `g13-01`, `g13-02`

**Model state:** pair keys built, `derived_keys` and `context_keys` on.

    step 1 at out-degree 1      1.000 +/-0.000, 8 seeds
    step 2 at a unique pair     1.000, and 0.971 overall
    the two together, ceiling   1.000 against the 0.87 that would justify building it

### Root-only branching was measured at the wrong place — `note 064`

**Model state:** `search` only; `walk_from` commits to `first_relation` and takes argmax
after. CLUTRR.

    entity hop        0.9889   flat in position and in chain length
    relation decode   0.9348   0.974 at the root, ~0.91 mid-chain

and **15%** of relation-decode reads land on an entity with two or more outgoing edges,
where `key(FACT, e)` holds a **sum**. So `search` hedges where the decode is already 0.974
and commits blindly where it is 0.906 — which is why beam width 1→8 moved chain recovery
only 0.650 → 0.659. Measured at the wrong place by its own construction.

### Per-step branching, and the figure that has been quoted across regimes — `note 065`

**Model state:** CLUTRR `gen_train23_test2to10`, kinship layout, width 64.

Reported **+0.2190** chain recovery over `search`, and 713/713 on the plain subset.

### That gain does not reproduce — `note 074`, `note 075`

**Model state:** as above, re-run from a committed script.

No committed script produced 065's numbers, so the configuration behind them is
unrecovered. On re-measurement `beam` lands within **0.007** of 065's mean while `search`
is high by **0.12**, so the gain comes out at **+0.107**. Not width, not the `allowed`
mask, not `branches` — all tested. Differences are to be taken against
`tools/clutrr_recovery.py`'s own baseline.

### 713/713 is reached under partitioning — `note 081` companion

**Model state:** 4 concept nodes, CLUTRR.

    4 concept nodes   0.9220
    monolithic        0.8877

### The rendezvous, and its period — `note 102`

**Model state:** CLUTRR, width 64, beam 4, branches 4, 3 seeds, 1,146 puzzles.

    prune_every   recovery      sd     reads vs k=1
              1     0.8877  0.0305            1.00x
              2     0.8860  0.0312            2.29x
              3     0.8831  0.0336            5.74x
              5     0.8773  0.0279           38.25x
          never     0.7987  0.0206            1.00x

The seed spread of 0.0305 exceeds every period effect except `never`. So the meeting is
worth about **0.089** and its period nothing measurable; skipping meetings leaves the
population uncapped between them, which is where the reads go.

### Round trips, batched — `note 100`, `note 101`

**Model state:** `peer.py` `PROTOCOL` 3, loopback, priced at a 50 ms RTT.

A hop is **two dependent rounds** — follow, then look up what the follow decoded to — so a
depth-10 beam is 20 rounds. Batching a hop's independent reads took depth 10 from
`77 × RTT` = 3,850 ms to `20 × RTT` = 1,000 ms. `d_max` is 640 ms. `owner()` routes a hop's
look-up and the next hop's follow to the same concept, and 12 of 19 consecutive rounds
asked a peer the round before had already used.

### On `run()`'s own task — `note 103`, `g21-01`

**Model state:** kinship, `hops=2`, width 256, 8 seeds, 32 cells. **Not CLUTRR, and not
chain recovery** — the readout's answer.

    arm         overall            out-degree 1       out-degree >= 2
    walk        0.596 +/-0.018     0.702 +/-0.025     0.446 +/-0.010
    search4     0.604 +/-0.013     0.649 +/-0.021     0.539 +/-0.024
    beam4       0.644 +/-0.016     0.692 +/-0.030     0.577 +/-0.016
    beam4-k2    0.629 +/-0.013     0.671 +/-0.026     0.569 +/-0.017

`beam4 − search4` is **+0.041 ±0.013**, above 2 SE. `beam4 − walk` is **+0.049 ±0.012**
against a `> 0.050` prediction, recorded as refuted as written.

`search` is **worse than not branching** at out-degree 1 (0.649 against walk's 0.702),
reproducing g13-03's −0.054; `beam` recovers 0.692 there and gains +0.038 at out-degree
≥ 2. Period 2 costs **−0.016 ±0.006** on this task — inside the 0.02 predicted, and 2.7 SE
from zero.

### Cost

`width × branches × depth` reads, roughly 4× `search`. Unpruned it is `branches^depth` —
a million walks at ten hops — so pruning is what makes the option exist at all. `123`
measured beam 4 at 3.2× on kinship. G4 unanswered.
