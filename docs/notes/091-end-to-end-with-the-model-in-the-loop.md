091 — End to end with the model in the loop: 0.53 → 0.86
========================================================

**Status:** measured, gate passed, `tools/generation_delta.py` committed. **It closes the
caveat every composition note since 066 has carried** — *"symbolic fold over true chains,
not the model's own"* — and it is the first number in this project where the model does the
whole task.

---

## IN PLAIN TERMS

Every composition result so far handed the fold a chain taken from the data. **This one makes
the model find the chain itself**, from its own store, and then folds what it found.

**0.86**, against 0.53 with the gaps left unfilled.

---

## The measurement

    END TO END, width 64, seed 0, 1,146 CLUTRR test puzzles

    chain recovery                    0.8770   <- GATE: clutrr_recovery.py says 0.8770
    gap (no fill)                     0.5279
    random relation                   0.6003
    CONTROL: wrong delta              0.5672
    delta-filled                      0.8578

**The gate is exact** and the control still collapses — wrong delta scores below random,
just as it does symbolically, so the mechanism is the displacement rather than the filling.

**And the composition is roughly multiplicative:** 0.8770 recovery × 0.9651 symbolic fold =
0.846, against 0.8578 observed. Slightly better than the product, because a mis-recovered
chain can still compose to the right answer — the same error-correction note 088 measured.

## A methodological discrepancy, found by writing the tool

The committed tool folds the chain that `clutrr_recovery.true_chain` returns — a **path walk**
from the query's subject to its object. Notes 087, 088 and 090 folded `edge_types` directly,
which is **story order**. Note 075 flagged that these differ.

    arm            path order (tool)    story order (notes 087-090)
    gap                       0.5960                         0.5201
    random                    0.6640                         0.6073
    wrong-delta               0.6379                         0.5681
    delta                     0.9651                         0.9668

**Path order is the correct chain**, so those notes' baselines were pessimistic by 0.06–0.08.
**The delta arm is unchanged (0.9651 against 0.9668)**, which is the reassuring part: the
mechanism does not depend on the ordering convention, while the baselines do. The relative
claim — delta beats every control by a wide margin — holds under both.

> Recorded rather than silently corrected, because a baseline that moves when the harness
> changes is exactly the thing that makes two numbers incomparable later. The tool is now the
> reference and its ordering is the one to quote.

## Where this leaves the composition line

    note 066   fold right 98.8% where it can act, 52.6% coverage. Stands
    note 087   the ceiling is 31 rules; the fold is perfect given coverage. Stands
    note 088   the learned readout loses to random; bar set at 0.6081. Stands, and
               the bar is beaten by 0.9651 symbolically
    note 090   generation delta learned exactly, 20/20, closes the ceiling. Stands
    note 091   and the model can do it itself: 0.8578

## What is NOT claimed

**Not comparable to published CLUTRR numbers.** This is the **graph layer**, never the prose
— *"CLUTRR-symbolic"*, as `openplexus/tasks/clutrr.py` insists. Published results read
sentences; the hard part of their task is absent here.

**Not one seed.** Width 64, seed 0. Note 065 measured chain recovery varying 0.8735–0.8848
across three seeds, so the end-to-end number presumably moves similarly and has not been
swept.

**Not the distributed path.** In-process, monolithic store. Note 086's transport now carries
pair keys, so this *could* be run across containers and has not been.

**And the invariant is still kinship's.** Generation composes additively here. Whether an
arbitrary relational domain has a conserved quantity of this kind is the open question note
090 raised and this does nothing to settle.
