"""What does a late-signal gate cost a node, and below what width is it absurd?

Every gating result in the g9 line -- the window, the tag -- is a RECOVERY
number. None of them has been costed per node, and note 015 is the standing
warning about exactly that: its first cost model made competitive capture look
cheap, and the corrected version showed the obvious implementation is MORE
expensive than superposition for precisely the tiny nodes this project exists
for. It survived only because keys are derived.

The same question has never been asked of the mechanism the whole g9 line rests
on. This asks it.

## The thing that makes a late signal expensive

The signal arrives AFTER the binding, so the node writes everything and undoes
what nothing vouched for. Undoing requires remembering what was written. That is
`pending` in `local_memory.py`, and it holds one entry per write since the last
reward -- **a cost that has nothing to do with how wide the node is.**

The store it gates does scale with width. So the two cross.

## Two implementations, and the cheap one flips with width

**SUBTRACT** is what the model does: keep every write since the last reward, and
at capture subtract the ones no mark protected.

    per entry, keys derived    1 weight + 1 token id            = 2
    per entry, keys stored     1 weight + w value + d key       = 1 + w + d
    total                      interval length x per entry

With `derived_keys` the value is `wv[token]` and the key is `wk[token]`, both
regenerable from the token id -- so an entry is a weight and a token, whatever
the width. Without it, a node must keep the full key because retrieval sums over
every dimension, which is note 012's argument arriving here.

**REBUILD** keeps a second store for writes since the last reward, and at capture
throws it away and re-adds only what was marked, regenerating each from its token.

    scratch store              w x d
    marks                      2 per slot
    total                      w*d + 2*slots

Its cost does not grow with the interval; SUBTRACT's does not grow with width.

## What this is not

An argument that either is right. It is the arithmetic, in code, because the last
time it was done by hand in a note it was wrong in the direction that flattered
the mechanism.

    python tools/gate_cost.py
"""

from __future__ import annotations

#: reward_recall as every g9 sweep runs it: 768 steps, 4 rewards, so roughly
#: this many writes between one capture and the next. Named rather than inlined
#: because it is a property of ONE task and a tool that hard-codes one
#: experiment's property will be wrong about the next.
INTERVAL = 186
#: g9-06's flat-and-positive cell.
SLOTS = 32


def superposed(width: int, d_model: int) -> int:
    """What this node's slice of the memory costs: its own rows, all columns."""
    return width * d_model


def subtract_cost(width: int, d_model: int, interval: int = INTERVAL,
                  derived_keys: bool = True) -> int:
    """Remember every write since the last reward, and undo the unvouched ones."""
    per_entry = 2 if derived_keys else 1 + width + d_model
    return interval * per_entry


def rebuild_cost(width: int, d_model: int, slots: int = SLOTS) -> int:
    """Hold a scratch store, and at capture rebuild from the marks alone."""
    return width * d_model + 2 * slots


def crossover(d_model: int, interval: int = INTERVAL) -> float:
    """The width at which SUBTRACT stops costing more than the memory it gates.

    `width * d_model = interval * 2`, so below this a node spends more
    remembering what it might undo than it spends on the memory itself.
    """
    return interval * 2 / d_model


def main() -> int:
    print(__doc__.split("## The thing")[0].strip())
    print(f"\ninterval {INTERVAL} writes between captures, tag of {SLOTS} slots")
    print("numbers per node, against its own slice of the superposed store\n")
    for d_model in (256, 1024, 4096):
        print(f"d_model {d_model}   (gate costs more than the memory below "
              f"width {crossover(d_model):.2f})")
        print(f"  {'width':>7}{'store':>10}{'SUBTRACT':>10}{'REBUILD':>10}"
              f"{'SUBTRACT/store':>16}{'cheaper':>10}")
        width = 1
        while width <= d_model:
            store = superposed(width, d_model)
            sub = subtract_cost(width, d_model)
            reb = rebuild_cost(width, d_model)
            cheaper = "REBUILD" if reb < sub else "subtract"
            print(f"  {width:>7}{store:>10}{sub:>10}{reb:>10}"
                  f"{sub / store:>15.2f}x{cheaper:>10}")
            width *= 8
        print()

    print("WITHOUT derived keys, at d_model 256:")
    print(f"  {'width':>7}{'store':>10}{'SUBTRACT':>12}{'ratio':>10}")
    for width in (1, 8, 64):
        store = superposed(width, 256)
        sub = subtract_cost(width, 256, derived_keys=False)
        print(f"  {width:>7}{store:>10}{sub:>12}{sub / store:>9.1f}x")
    print("  -- which is note 012's argument arriving at a second mechanism:")
    print("     an entry has to carry the FULL key, because retrieval sums over")
    print("     every dimension, so the cost stops depending on the node's own")
    print("     width and starts depending on the whole network's.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
