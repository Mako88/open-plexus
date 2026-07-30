# 103 — the beam earns the default, and the rendezvous period does not

2026-07-30. `g21-01`, kinship at hops 2, width 256, 8 seeds, 32 cells. `search_beam_width`
now defaults to **4**. `search_prune_every` stays at **1**.

## Why this needed its own sweep

I told John this morning that `run()` was leaving 0.6588-against-0.8877 on the table by
calling `search` instead of `beam`. **That number is not evidence about this wiring.** It
is CLUTRR chain recovery at chain lengths 2 to 10 — a different task, a different depth,
and a different thing scored. Note 064's own diagnosis is why depth is the axis that
matters: the relation decode is 0.974 at the root and about 0.91 mid-chain, `search`
hedges at the root and commits after, so **at hops 2 there is exactly one mid-chain decode
for the beam to fix.** Most of the room it exploits is absent by construction.

Quoting it across regimes would have been note 087's mistake again, so the arms were run.

## Measured

    arm         overall            out-degree 1       out-degree >= 2
    walk        0.596 +/-0.018     0.702 +/-0.025     0.446 +/-0.010
    search4     0.604 +/-0.013     0.649 +/-0.021     0.539 +/-0.024
    beam4       0.644 +/-0.016     0.692 +/-0.030     0.577 +/-0.016
    beam4-k2    0.629 +/-0.013     0.671 +/-0.026     0.569 +/-0.017

    GATE   beam4 - search4 >= 0.010    +0.041 +/-0.013   CONFIRMED, above 2 SE
    RAIL   beam4 - walk    >  0.050    +0.049 +/-0.012   refuted as written
    FALS   |beam4-k2 - beam4| < 0.020  -0.016 +/-0.006   CONFIRMED

**The GATE is confirmed decisively, so the default moves.** The mechanism reaches this
regime despite having only one mid-chain decode to work with.

## The out-degree split, which says something the overall column cannot

    walk      0.702 at out-degree 1     search4  0.649     beam4  0.692

`search` is **worse than not branching at all** where the subject holds one relation — it
loses 0.053, which is g13-03's `-0.054` reproduced exactly. Committing to a root candidate
and following it can only replace a correct greedy pick with a luckier endpoint.

`beam4` recovers almost all of that (0.692 against walk's 0.702) *and* gains 0.038 over
`search4` at out-degree >= 2, where `key(FACT, e)` holds a sum. So its `+0.043` at
out-degree 1 is not luck in the endpoint score, which is what I wrote the split to detect:
**it is the beam repairing damage `search` does by hedging in the wrong place.** Keeping
several partials alive means a bad root commitment is survivable.

## The RAIL, scored as written and refuted by 0.001

`+0.049` against a `> 0.050` bar. **Recorded as refuted because that is what it says**, and
the concern it encodes — "the branching might be inert in `run()`" — is plainly satisfied
at 4 SE from zero. The threshold was picked to be easy to clear and landed a thousandth
short, which is an argument for stating rails as directions rather than as round numbers.

## Why the period is NOT switched on

`prune_every=2` costs **-0.016 +/-0.006**. That is inside the 0.02 tolerance, so **note
102's finding transfers to a second task** — but it is 2.7 SE from zero, so it is a real
small loss and not free, which the CLUTRR arms could not resolve at their seed spread.

So the period is a knob a DEPLOYMENT turns up when latency binds: pay 0.016 to bring a
migrating walk inside `d_max`, and only when that is what is being bought. Defaulting it
to 2 would spend accuracy on a constraint that is not binding in a single process.

## Single-seed observations recorded as leads, not results

Ran while the sweep was in flight, `tools/clutrr_recovery.py`, seed 0 only:

    concept_nodes   route            search    beam
    0               either           0.7914    0.8770
    8               current          0.7845    0.9058
    8               first-concept    0.8141    0.9040

1. **Concept partitioning improving accuracy is not news** — I nearly wrote it up as a
   lead before finding it already in the tree. Note 081's companion measurement has 4
   concept nodes giving beam **0.9220 against 0.8877** monolithic, *"because a node
   carries interference only from what it owns."* The 8-node 0.9058 here is corroboration
   at a second node count, and the mechanism was already named. **Searching the tree
   before reporting a finding is rule 19 applied to results rather than to code**, and I
   found this by accident rather than by looking.
2. **`route` is accuracy-neutral for the beam** (0.9058 against 0.9040) while being
   strictly better for ownership (note 073: markers stop owning content, busiest peer
   26.6% → 11.8%, and the 20-node cap goes). At `concept_nodes=0` the two routes are
   **bit-identical**, which is not a measurement: `owner()` is only consulted when the
   store is partitioned, so an unpartitioned comparison of routes cannot say anything.

`route` cannot simply be defaulted, either: `concept_nodes` still refuses to combine with
`hops > 1`, `reward_token`, `memory_cap`, `tag_relative`, `carry_store` and
`consolidation`. **Those six refusals are the real blocker to partitioning by default**,
and they are the next thing to take apart.
