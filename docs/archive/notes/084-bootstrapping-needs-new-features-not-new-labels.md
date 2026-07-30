084 — Bootstrapping needs new FEATURES, not new labels
=====================================================

**Status:** measured, 40 seeds, and it is a refutation of a transfer I expected to work.
It explains note 078's success and this failure with one principle.

---

## IN PLAIN TERMS

Note 078 got an eight-fold improvement by feeding a system's own confident answers back into
it. Note 070's weakest result — guessing a composition rule it was never shown, at 0.223 —
looked like the obvious place to try the same move.

**It does nothing. The accuracy freezes on the first round and stays frozen.**

The reason is the useful part. In note 078 each round's answers were turned into **new
features** — *which aligned group does this entity neighbour* — so every round handed the
system information it did not previously have. Here the answers become **new labels** for the
same features, so re-fitting just re-learns the function it already had.

**Feeding a model its own conclusions is only worth something if the conclusions change what
it can see.**

---

## The measurement

62 base rules, 16 held out, `concat + convolve` over extensional profiles, transductive —
accepted predictions come from the held-out set itself, chosen by readout **margin** and never
by a label.

    round     held-out accuracy
        0                0.2062     gate: note 070 reports 0.223
        1                0.1984
        2                0.1984
        3                0.1984
        4                0.1984

    marginal baseline (note 069)  0.242
    chance                        0.050

**Frozen at exactly 0.1984 from round 1**, which is the diagnostic: the accepted set stopped
changing after one pass, so nothing further was being learned from anything.

> **The gate is close but not exact — 0.2062 here against note 070's 0.223.** 40 seeds against
> 070's 120, and 16 held-out items make each one worth 0.0625, so a 0.017 gap is under one
> item's resolution. Recorded rather than called a match.

## The principle, which is worth more than the refutation

    note 078   each round produced ALIGNED PAIRS, which became new columns:
               "how often does this entity neighbour aligned group k".
               New information from the graph, every round -> 0.0389 to 0.3098

    here       each round produced PSEUDO-LABELS over the same feature space.
               A ridge fit on its own predictions recovers its own function ->
               no movement at all

**So self-training is not a general amplifier.** It compounds exactly when the loop routes
back through something the model cannot already compute, and note 078's loop did that by
construction while this one cannot.

## What composition would actually need

Not more labels over the same features. Something that injects information from outside the
readout — and there are two candidates, both untried:

    verify against the GRAPH   a predicted rule (r1,r2)->t is checkable on chains
                              the data contains. That is information the readout
                              does not hold
    ASSOCIATIVITY             if (r1,r2)->x and (x,r3)->y, and (r2,r3)->z and
                              (r1,z)->y', then y must equal y'. A label-free
                              consistency constraint over predictions, which is
                              the closest analogue to 078's mutuality

**The second is the more interesting one**, because note 078's finding was that *mutual
agreement* beat any confidence threshold, and associativity is mutual agreement for rules.

## What is NOT claimed

**Not that note 070's direction is refuted.** Extensional relation vectors still beat random
ones on this task — that is 070's measurement and it stands. What is refuted is that
self-training lifts it.

**Not that a different acceptance rule would fail too.** Margin was used because note 078
found confidence gates *worse* than mutuality, and there is no mutuality analogue for a
one-sided prediction. An associativity-gated version is a different experiment.

**And the fixture is small.** 16 held-out rules is coarse, and a freeze this exact is more
likely a property of the loop than of the numbers — but it is the loop that was under test.
