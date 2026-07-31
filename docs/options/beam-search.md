# Option record — `search.beam`, branch at every step

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

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

### Search was refused twice, on the arithmetic of its day — `107`, `111`

    CONFIG  when    2026-07-28
            source  decisions 107 and 111
            script  unrecorded
            task    kinship traversal
            model   single-token keys, steps 1 and 3 at 0.710 and 0.677
            knobs   search_branches 0
            scale   unrecorded

`111`: *"you cannot search your way out of noisy primitives, because the verifier is built
from the primitives."* `107` had already declined the pair-key traversal beneath it — *"a
perfect traversal buys 0.05"* — because compounding those step accuracies left nothing.

### The condition expired — `g13-01`, `g13-02`

    CONFIG  when    2026-07-28
            source  decision 122, g13-01, g13-02
            script  experiments/g13_02_what_the_traversal_step_costs.py
            task    kinship
            model   pair keys built
            knobs   derived_keys on, context_keys on
            scale   8 seeds

    step 1 at out-degree 1      1.000 +/-0.000
    step 2 at a unique pair     1.000, and 0.971 overall
    the two together, ceiling   1.000 against the 0.87 that would justify building it

### Root-only branching was measured at the wrong place — `note 064`

    CONFIG  when    2026-07-29
            source  note 064
            script  tools/clutrr_recovery.py
            task    CLUTRR gen_train23_test2to10, kinship layout
            model   `search` only; `walk_from` commits to `first_relation`, argmax after
            knobs   search_branches 1 to 8
            scale   unrecorded

    entity hop        0.9889   flat in position and in chain length
    relation decode   0.9348   0.974 at the root, ~0.91 mid-chain

and **15%** of relation-decode reads land on an entity with two or more outgoing edges,
where `key(FACT, e)` holds a **sum**. So `search` hedges where the decode is already 0.974
and commits blindly where it is 0.906 — which is why beam width 1→8 moved chain recovery
only 0.650 → 0.659. Measured at the wrong place by its own construction.

### Per-step branching, and the figure that has been quoted across regimes — `note 065`

    CONFIG  when    2026-07-29
            source  note 065
            script  unrecorded -- note 074 established no committed script reproduces it
            task    CLUTRR gen_train23_test2to10, kinship layout, chain recovery
            model   width 64
            knobs   unrecorded
            scale   unrecorded

Reported **+0.2190** chain recovery over `search`, and 713/713 on the plain subset.

### That gain does not reproduce — `note 074`, `note 075`

    CONFIG  when    2026-07-30
            source  notes 074-075
            script  tools/clutrr_recovery.py
            task    CLUTRR gen_train23_test2to10, kinship layout, chain recovery
            model   width 64, re-run from a committed script
            knobs   width, `allowed` mask and `branches` each varied in turn
            scale   3 seeds

No committed script produced 065's numbers, so the configuration behind them is
unrecovered. On re-measurement `beam` lands within **0.007** of 065's mean while `search`
is high by **0.12**, so the gain comes out at **+0.107**. Not width, not the `allowed`
mask, not `branches` — all tested. Differences are to be taken against
`tools/clutrr_recovery.py`'s own baseline, whose three-seed means are search **0.7810**
and beam **0.8877**.

### 713/713 is reached under partitioning, and the figure now has a run behind it — `note 105`

    CONFIG  when    2026-07-30
            source  note 105, and note 075 for the monolithic baseline
            script  tools/clutrr_recovery.py --concept-nodes 4 --seeds 0 1 2
            task    CLUTRR gen_train23_test2to10, kinship layout, chain recovery
            model   width 64, decay 1.0, route current
            knobs   concept_nodes 4 against 0, beam width 4, branches 4
            scale   1146 puzzles, 3 seeds

    4 concept nodes   0.9220   713/713 at two seeds, 712/713 at the third
    monolithic        0.8877   note 075's three-seed mean, same script

`0.9220` was carried in the tree for a day citing `note 081`, **which contains no
partitioning measurement at all** — the run existed and had never been written down. Re-run
here it reproduces to four decimal places. Search gains too: **0.8089** against note 075's
monolithic **0.7810**.

### The rendezvous, and its period — `note 102`

    CONFIG  when    2026-07-30
            source  note 102
            script  tools/prune_period.py
            task    CLUTRR, chain recovery
            model   width 64
            knobs   search_beam_width 4, branches 4, search_prune_every 1 to 5 and never
            scale   1,146 puzzles, 3 seeds

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

    CONFIG  when    2026-07-30
            source  notes 100-101
            script  tools/walk_rounds.py
            task    a depth-10 beam walk
            model   openplexus/peer.py at PROTOCOL 3, loopback
            knobs   read_many batching on against off
            scale   priced at an assumed 50 ms RTT

A hop is **two dependent rounds** — follow, then look up what the follow decoded to — so a
depth-10 beam is 20 rounds. Batching a hop's independent reads took depth 10 from
`77 × RTT` = 3,850 ms to `20 × RTT` = 1,000 ms. `d_max` is 640 ms. `owner()` routes a hop's
look-up and the next hop's follow to the same concept, and 12 of 19 consecutive rounds
asked a peer the round before had already used.

### On `run()`'s own task — `note 103`, `g21-01`

    CONFIG  when    2026-07-30
            source  note 103, g21-01
            script  experiments/g21_01_does_the_beam_pay_in_run.py
            task    kinship, hops 2 -- NOT CLUTRR and NOT chain recovery
            model   width 256, the readout's answer
            knobs   search_beam_width 0 and 4, search_branches 4, search_prune_every 1 and 2
            scale   32 cells, 8 seeds

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

### What it costs

    CONFIG  when    2026-07-28
            source  decision 123
            script  unrecorded
            task    kinship
            model   width unrecorded
            knobs   search_beam_width 4
            scale   unrecorded

`width × branches × depth` reads, roughly 4× `search`. Unpruned it is `branches^depth` —
a million walks at ten hops — so pruning is what makes the option exist at all. `123`
measured beam 4 at 3.2× on kinship. G4 unanswered.

### BEAM WIDTH 4 IS UNDERTUNED AT DEPTH, and it is a fifth of what `d_model` was worth — `g41-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g41-01-the-pipeline-on-the-published-protocol.txt
            script  experiments/g41_01_the_pipeline_on_the_published_protocol.py
            task    CLUTRR gen_train23_test2to10, TEST split, 1,146 puzzles
            model   LocalAssociativeMemory + search.beam + the delta fold
            knobs   beam width {4, 8} x d_model {32, 64, 128, 256}
            scale   8 seeds, per hop bucket, both max_appearances subsets

`search_beam_width=4` arrives from `note 065` and had never been varied. At 10 hops,
subset `all`, the end task reads **0.8015 at beam 4 against 0.8676 at beam 8**, width
128 — so the carried value costs real accuracy at depth and nothing at all in the
shallow buckets.

**It is the smaller of the two carried constants, by about fivefold**, and that is the
transferable part: `d_model` was swept in the same run and moved the same cell far
further. Sweeping the knob that looks most likely would have found a real effect and
missed the one that mattered. Record: [generation-delta.md](generation-delta.md).
