082 — Consolidation buys SELECTIVITY, partitioning buys CAPACITY, and C4 needs both
==================================================================================

**Status:** measured, and it completes note 081. Together they are the first end-to-end
answer to C4 in this project, with every bound measured rather than assumed.

---

## IN PLAIN TERMS

Note 081 found that a single store cannot learn forever: keep everything and it all becomes
unreadable, fade it and only the last hundred facts survive.

**Consolidation fixes that completely for the things that get asked about.** Write everything
weakly into the fading store; when a fact is asked and answered correctly, copy it into a
store that does not fade. Recall of the facts that matter goes from **2% to 100%.**

**And it works exactly as well as the signal that drives it.** If the judgement of "that was
right" is 70% accurate, recall is 70%. The relationship is almost exactly one to one, so the
whole mechanism reduces to the quality of one signal — **and note 080 measured that signal at
six standard deviations of separation, with no label needed.**

**But it is a multiplier, not an escape.** The non-fading store has the same fixed capacity as
any other, so pushing the set of things-that-matter past it degrades in exactly the way note
081 described.

---

## The measurements

`d=128`, capacity `~0.023·d²` ≈ 377 bindings, 4,000 facts streaming, 200 of them ever asked
about, fast store at `decay=0.99`.

    condition                       useful recall    early useful    promoted
    no consolidation (GATE)                 0.020           0.000           0
    consolidation, signal 1.0               1.000           1.000         200
    consolidation, signal 0.9               0.915           0.937         183
    consolidation, signal 0.7               0.705           0.746         141
    consolidation, signal 0.5               0.540           0.556         108

**Recall tracks signal accuracy one-to-one.** 0.9 → 0.915, 0.7 → 0.705, 0.5 → 0.540.

And the bound, because 200 useful facts fitting inside 377 capacity could have been the whole
story:

    useful facts    load on slow store    recall
             200                  0.5x     1.000
             400                  1.1x     0.965
             800                  2.1x     0.714
            1600                  4.2x     0.419

**The slow store saturates too.** So consolidation buys a factor of `total ÷ useful` — 20x in
this fixture — and not infinity.

## The complete answer to C4, with each part's bound

    consolidation   a SELECTIVITY multiplier, `total / useful`. Needs a
                    correctness signal, and delivers exactly its accuracy
    partitioning    a CAPACITY multiplier, `node count`. Each node holds a
                    FULL-WIDTH store for its own concepts (`134`)
    together        they MULTIPLY, and neither alone is enough

**Neither is sufficient and the reasons differ.** Consolidation with one store still saturates
once the useful set outgrows `d²`. Partitioning without consolidation still fills every node
with filler, because nothing selects. **C4 says forever, and forever exceeds any fixed
multiple — so something must still shed, and what to shed remains untouched.**

## What this makes of the session's other results

    note 080   contradiction detects "was that wrong" at six sd, label-free.
               That is precisely the signal consolidation needs, and the
               one-to-one relationship above says the loop's quality is
               entirely determined by it
    note 079   blame localises which binding to fix, 0.900 against 0.050
    note 081   the single store fails, and the gate fails with it at load

**So the pieces compose into a coherent design:** a fading working store, a contradiction
signal deciding what was right, promotion into a non-fading store, blame locating what to
repair, and partitioning making the non-fading store grow with the network. **Every piece is
measured. Nothing is wired together.**

## What is NOT claimed

**Not that "useful" is well defined.** The fixture declares 200 facts useful by fiat and asks
about them at exactly the right moment — while they are still in the fading window. A real
system does not know which facts will matter, and **a fact never asked about during its window
is unrecoverable.** That is the mechanism's real cost and this fixture is built not to pay it.

**Not measured with the real `consolidation` path.** `LocalMemoryConfig.consolidation` exists,
with `capture_slots`, `salience` and `lasting_cap` around it, and this reimplements the idea
rather than exercising that code. The tree also records that `concept_nodes` currently
**refuses** consolidation, and that refusal is what has to be lifted for the combination above
to run at all.

**And not at any real scale.** `d=128` and 4,000 facts. The claim that the two multipliers
multiply is arithmetic, not a measurement.
