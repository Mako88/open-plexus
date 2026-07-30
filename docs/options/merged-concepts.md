# Option record — `concepts.Merged`, the merge direction without moving an address

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/concepts.py` — `Merged`, wrapping any `Surfaces`. `of` is **unchanged**;
  `aliases(concept)` returns the class smallest first.
- `tests/test_merged_concepts.py`, 19 tests.
- Two mutations in `tools/mutate.py`, including
  `a-merge-remaps-the-surface-and-strands-its-bindings`.

---

## What was tried, and what came back

### The obvious design strands every binding it means to preserve

    CONFIG  when    2026-07-30
            source  openplexus/concepts.py, the `Merged` docstring
            script  tests/test_merged_concepts.py
            task    none -- a design property, asserted by test
            model   `ByConcept` builds the key from the concept id
            knobs   none
            scale   19 tests, 2 mutations

Remapping the loser's surfaces to the winner's concept is the design that first suggests
itself, and it **strands every binding it means to preserve**, because `ByConcept` builds
the key from the concept id: everything already written under the old id becomes
unreachable at the moment of the merge.

So writes always land on a surface's own concept, and the merge is a **read-side gather**
over `aliases()`. The cost is `k` reads at `k` addresses for a class of size `k`. A later
lazy consolidation can shrink that without breaking a read, which re-keying cannot promise.

The mutation `a-merge-remaps-the-surface-and-strands-its-bindings` is the guard, and its
own text says it *"breaks the entire reason `Merged` is shaped the way it is"*.

### Union by MINIMUM id, not by rank

    CONFIG  when    2026-07-30
            source  openplexus/concepts.py, the `Merged` docstring
            script  tests/test_merged_concepts.py
            task    none -- a design property, asserted by test
            model   `Surfaces.of` promises the same answer on every node forever
            knobs   none
            scale   included in the 19 tests

Rank makes the representative depend on **arrival order**, so two nodes learning the same
merges out of order disagree about the class. Minimum id makes the answer a property of the
merge SET rather than of its history, which is what lets **propagation be lazy and need no
coordinator**. A mutation covers exactly this, and its text names the consequence: nodes
*"send a"* different answer for the same question.

### A late merge is a MISS, never a corruption

    CONFIG  when    2026-07-30
            source  openplexus/concepts.py, the `Merged` docstring
            script  tests/test_merged_concepts.py
            task    none -- a design property, asserted by test
            model   `of` never moves
            knobs   none
            scale   included in the 19 tests

Because `of` never moved, a node that has not yet learned a merge returns less rather than
returning something wrong. Un-merging is free for the same reason: dropping an alias
strands nothing.

### What drives a merge is mutual agreement, not a confidence threshold — `note 077`, `note 078`

    CONFIG  when    2026-07-30
            source  notes 077-078
            script  unrecorded
            task    OpenEA EN_DE_15K_V2, entity alignment
            model   bag of (relation, direction), zero supervision, then bootstrapping
            knobs   confidence gate at >=0.9 and >=0.98 against mutual nearest neighbour
            scale   15,000 gold links

Bootstrapping on mutual nearest neighbours reaches **0.3098** hits@1, 8× chance. **A
confidence gate makes it WORSE** — 0.2334 at ≥0.9 and 0.0855 at ≥0.98 — and does not buy
precision. So mutuality is the merge gate and magnitude is not. Seed precision
self-corrects from 0.263 to 0.676 untuned.

### The direction this does NOT address — `note 053`

    CONFIG  when    2026-07-29
            source  note 053
            script  none -- a constraint register entry
            task    none
            model   n/a
            knobs   none
            scale   n/a

`Merged` is the MERGE direction — two concepts turning out to be one. Note 053's SPLIT,
one thing acquiring two ids on two nodes, exists only distributed and has no local
detector. Record: [per-node-codebooks.md](per-node-codebooks.md).
