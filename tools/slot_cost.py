"""What would per-key slots cost a node, and at what width do they stop paying?

[g10-05](../experiments/sweeps/g10-05-how-many-slots.txt) measured the ceiling of
a bounded set of DISTINCT successors per character: 8 slots recovers 83-97% of
what unbounded counting gains over one superposed average. It closed by naming
the question it did not answer — **whether that is affordable** — and this is
that question, asked the way `gate_cost.py` asks it, by arithmetic.

This is the same shape as note 015's correction, which is worth stating twice: a
first cost model made competitive capture look cheap, and the corrected version
showed the obvious implementation is MORE expensive than superposition for
exactly the tiny nodes this project is for. It survived only because keys are
derived. So the default expectation here is that the obvious implementation
loses, and the interesting question is whether the derived-key version does not.

## What is being compared

**SUPERPOSED** is what the model does now: one dense store, every association
added into it.

    store                      w x d

**SLOTS, VECTORS** keeps `slots` successors per vocabulary entry, each as a
vector.

    store                      vocab x slots x w

**SLOTS, TOKEN IDS** keeps `slots` successor TOKEN IDS per vocabulary entry, and
regenerates the vector from `(seed, token)` when it is read.

    store                      vocab x slots            (integers)

The third is only possible with `derived_keys`, the standing dependency of the
entire g9 line ([note 024](../docs/notes/024-what-the-gate-costs-a-tiny-node.md)).
It costs arithmetic at read time instead of memory at rest, and **this file does
not price that arithmetic** — it counts numbers held, which is what a device with
a fixed memory budget is limited by. A version that priced compute could reach
the opposite conclusion, and saying so is the point of saying it here.
"""

from __future__ import annotations

#: A node's slice of the network. `w` is what THIS node holds; `d` is the
#: network's width, which the key spans because retrieval sums over all of it.
WIDTHS = (1, 4, 16, 64, 256)
#: Character level, which is where the corpus line is. A word-level vocabulary
#: is 1000x this and the table would look very different, which is why the
#: vocabulary is a parameter rather than a constant.
VOCAB = 86


def superposed(w: int, d: int) -> int:
    """The dense store: one matrix, every association summed into it."""
    return w * d


def slots_as_vectors(w: int, vocab: int, slots: int) -> int:
    return vocab * slots * w


def slots_as_tokens(vocab: int, slots: int) -> int:
    """Integers, not floats. Counted the same here, which FLATTERS this option.

    A token id at a vocabulary of 86 fits in a byte where a weight is eight, so
    counting them alike understates the advantage by up to eightfold. Understated
    in the direction that makes the interesting option look worse is the safe
    way round.
    """
    return vocab * slots


def report(vocab: int = VOCAB, slots: int = 8, d: int = 256) -> list[dict]:
    rows = []
    for w in WIDTHS:
        dense = superposed(w, d)
        rows.append({
            "node_width": w,
            "superposed": dense,
            "slots_vectors": slots_as_vectors(w, vocab, slots),
            "slots_tokens": slots_as_tokens(vocab, slots),
            "vectors_ratio": slots_as_vectors(w, vocab, slots) / dense,
            "tokens_ratio": slots_as_tokens(vocab, slots) / dense,
        })
    return rows


def main() -> int:
    slots, d = 8, 256
    print(f"vocabulary {VOCAB}, {slots} slots per entry, network width {d}\n")
    print(f"{'node w':>8}{'superposed':>13}{'slots(vec)':>13}{'ratio':>9}"
          f"{'slots(ids)':>13}{'ratio':>9}")
    for row in report(slots=slots, d=d):
        print(f"{row['node_width']:>8}{row['superposed']:>13,}"
              f"{row['slots_vectors']:>13,}{row['vectors_ratio']:>8.1f}x"
              f"{row['slots_tokens']:>13,}{row['tokens_ratio']:>8.1f}x")

    print("\nThe vector version is a CONSTANT multiple of the store -- both")
    print("scale with w, so it never gets cheaper by shrinking the node. That")
    print("is note 015's finding arriving at a different mechanism.")
    print("\nThe token version does not scale with w at all, so it gets")
    print("RELATIVELY more expensive as the node shrinks, and cheaper as it")
    print("grows. The crossover is where vocab*slots = w*d:")
    crossover = VOCAB * slots / d
    print(f"    w = vocab*slots/d = {VOCAB}*{slots}/{d} = {crossover:.1f}")
    print(f"  so at network width {d} a node wider than {crossover:.1f} holds")
    print("  the slots more cheaply than it holds its own store")
    print("\nCOUNTED AS NUMBERS HELD, NOT COMPUTE. Regenerating a vector from")
    print("(seed, token) costs arithmetic at read time, which this does not")
    print("price. A compute-limited device could reach the opposite answer.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
