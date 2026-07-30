064 — The search branches at the one step that does not need it
==============================================================

**Status:** measured, one seed, and it explains a null this project already recorded.
Nothing built. **This is the actionable end of the CLUTRR line: one mechanism change
with a measured justification.**

---

## IN PLAIN TERMS

Following a chain of facts has two moves at every step: *who does this person point
to*, and *what is the relationship*. Measured separately, the first is almost never
wrong (0.989) and the second is wrong about one time in fifteen (0.935).

The mechanism can hedge — try several possibilities and keep whichever ends up in the
right place. **But it only hedges on the very first step**, and the first step is the
one that is already nearly always right. Every step after it takes its best guess and
commits.

So the hedging is spent where it is not needed and missing where it is. That is why
widening it from one guess to eight was worth **+0.009** — it was widening the wrong
thing.

---

## The decomposition

Each half measured from the TRUE previous entity, so an earlier mistake cannot
propagate into the number.

    step   entity hop   relation decode
       0        0.988             0.974
       1        0.982             0.942
       2        0.993             0.935
       3        0.988             0.937
       4        0.995             0.923
       5        0.994             0.906
       6        0.989             0.908
       7        0.982             0.910
       8        0.996             0.920
       9        0.983             0.941
    overall     0.9889            0.9348

**The entity hop is flat and near-perfect.** Not falling with position, not falling
with chain length (0.982–1.000 across lengths 2–10). So **the store is not degrading as
it fills** — which was the competing hypothesis and is now excluded. Decision 109 said
capacity was not the constraint at these sizes; this confirms it on the walk.

**The relation decode carries roughly six times the error rate**, and it is the only
part that varies with position: 0.974 at the root, ~0.91 in the middle.

## Why: `key(FACT, e)` is a superposition wherever an entity has two edges

    subject out-degree   reads
                     1    6048
                     2    1008
                     3      72
                     4       4

**15% of the relation-decode reads are on an entity with more than one outgoing edge**,
where `key(FACT, e)` holds a sum of relations rather than one. That is `search.py`'s own
documented asymmetry, and it is why that module exists:

> *"A `(subject, relation)` pair names one person 94.9% of the time; `(FACT, subject)`
> names one of several relations about half the time."*

## And the mechanism hedges in the wrong place

`walk_from` commits to `first_relation` — *"which is what makes this a branch rather
than another retrieval"* — and then, for every subsequent step:

    relations.append(int(np.argmax(_decode(wv, value))))

**Greedy argmax. The branching is root-only.** So `branches` explores alternatives at
step 0, whose decode is 0.974, and takes the single best guess at steps 1 to 9, whose
decodes are 0.906–0.942.

> **That is the complete explanation of note 062's null.** Beam width 1 → 8 moved chain
> recovery 0.650 → 0.659, and it could not have moved more: it was widening the search
> at the one step that did not need widening. The mechanism is not refuted. **It was
> measured at the wrong place, by its own construction.**

This also revises note 062's reading. It concluded *"the traversal pays and the search
on top of it does not"*, on the evidence that beam width bought nothing. The narrower
and correct statement is **root-only branching buys nothing**, which is a claim about
the implementation rather than about searching.

## What follows, and it is one change

**Branch at every step, not only the root** — proper beam search over the chain, each
partial walk scored by the endpoint it eventually reaches. Independent compounding of
the measured per-step rates gives `0.9244^h`: 0.730 at four hops, 0.456 at ten. Observed
is 0.963 and 0.361, so **errors are not independent** — short chains do better than
compounding predicts because the endpoint check rescues them, and long chains do worse
because a drift does not self-correct and the check then has more wrong walks to choose
between. Branching per step attacks exactly that.

**What would refute it:** if per-step branching does not beat 0.659 by more than seed
spread, then the ambiguity is not where this note says it is, and the relation decode's
error is something other than superposition — which would point back at `112`'s width
limit instead.

## What this does not say

**That it will work.** No beam over the whole chain has been run, and the cost is
`branches^h` walks unless pruned, which at ten hops is not affordable naively. **A
pruned beam is a different mechanism from what is implemented**, and it needs its own
predictions.

**And one seed** for every figure above.
