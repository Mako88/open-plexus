"""How much of a stream is worth writing, measured WITHOUT oracle labels.

**THE DEFINITION BELOW IS REFUTED. This file is the refutation, kept because re-running
it is how anyone checks the claim.** `g31-01`'s P1 was the gate: the label-free count
had to reproduce the oracle's bar on MQAR. The oracle control reproduces exactly --
**92.0x against `g28-01`'s 91.9x** -- and the label-free count of the same stream reads
**0.0x to 0.1x** at every granularity. Three orders of magnitude, so the two are not the
same quantity and no cross-source row in the output may be quoted.

**Why it fails, and it is the useful part.** MQAR's filler is drawn from a small key
range, so a filler address recurs constantly. *"This address is seen again"* is true of
almost every position in almost any stream; *"this address is later QUERIED"* is what
the oracle knows and what a write gate needs. **Recurrence is not demand.** Counting
cannot separate them, because the thing that makes a write worth making is a fact about
the future demand on it, not about the symbols.

**So the conclusion points at intervention rather than at a better count**: to learn
which writes mattered you have to remove them and see what breaks, which is the
per-position attribution route for kill-list 5 rather than any refinement of this.


**What this does not duplicate.** Searched `openplexus/`, `tools/`, `experiments/` and
`tests/` for enrichment, filler share and write gating before writing:

    experiments/g28_01_gate_screen.py   screens candidate SIGNALS against the bar, using
                                        `position_kinds()`. Its `bar_for` is IMPORTED
                                        here, not restated -- the bar's arithmetic must
                                        not fork, and `g28-01`'s docstring is the reason
                                        it is computed rather than hardcoded
    experiments/g8_02_control.py        enrichment on MQAR, also oracle-labelled
    openplexus/tasks/mqar.py            `position_kinds()`, the oracle itself

**So what is new is a definition that survives leaving MQAR.** Every measurement above
needs a label saying which positions matter, and that label exists only because this
project generated the data. Real streams do not come with one.

## The definition

    A write is WORTH MAKING if its ADDRESS is queried again later in the same stream.

Counting, no labels, identical on every source. A write nothing ever reads is waste
whatever it contained, which is what a write gate is for.

It reduces to one line: a position is worthy unless it is the LAST occurrence of its
address, so `worthy = 1 - distinct / total`.

## Granularity is a dial, and that is the point

Character bigrams recur constantly; four-character addresses mostly do not. MQAR's
filler is drawn from a wide key range, so its addresses almost never recur -- **which is
a choice made when the task was written, not a property of being synthetic.** Reporting
one number per source would compare sources at different settings and call the
difference "real versus synthetic", so the dial is swept and sources are compared only
at matched settings.

Predictions are in `experiments/sweeps/g31-01-is-the-filler-share-ours-or-the-worlds.txt`,
committed before this file existed.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from experiments import harness  # noqa: E402
from experiments.g28_01_gate_screen import BASE, bar_for  # noqa: E402
from openplexus.tasks.mqar import dataset  # noqa: E402

#: How many symbols the address includes. 1 is the coarsest addressing a stream
#: admits; 4 is fine enough that most addresses in most sources occur once.
WIDTHS = (1, 2, 3, 4)


def worthiness(addresses) -> tuple[float, int, int]:
    """`(worthy share, positions, distinct addresses)` for one stream of addresses.

    A position is worthy unless it is the LAST occurrence of its address, and the
    number of last occurrences is exactly the number of distinct addresses. So this
    is a set size, not a search -- which is why it runs on a million-character stream
    without a window parameter that would have to be chosen.
    """
    total = len(addresses)
    distinct = len(set(addresses))
    return ((total - distinct) / total if total else 0.0, total, distinct)


def ngrams(symbols, width: int):
    """Addresses over a token or character stream: the last `width` symbols."""
    return [tuple(symbols[max(0, t - width + 1):t + 1]) for t in range(len(symbols))]


def triple_addresses(triples, width: int):
    """Addresses over a graph, coarse to fine: relation, then head, then tail.

    **Ordered so `width` means the same thing it means for a token stream** -- more
    symbols in the address, fewer recurrences. `(relation,)` is the coarsest useful
    address a graph has; the whole triple is the finest and recurs only on duplicates.
    """
    order = [lambda h, r, t: r, lambda h, r, t: h, lambda h, r, t: t]
    return [tuple(f(h, r, t) for f in order[:width]) for h, r, t in triples]


def mqar_stream():
    """`(tokens, kinds, oracle_mask)` for MQAR at `g28-01`'s BASE config.

    The CONTROL, recomputed rather than quoted. `masks` is `harness.oracle_mask` --
    the SAME class `g28-01` scores against, which is the whole point of it being here.
    """
    sequences = dataset(BASE, 40)
    tokens: list[int] = []
    kinds: list[str] = []
    masks: list[bool] = []
    for sequence in sequences:
        tokens.extend(int(t) for t in sequence.tokens)
        kinds.extend(sequence.position_kinds())
        masks.extend(bool(m) for m in harness.oracle_mask(sequence.position_kinds()))
    return tokens, kinds, masks


def graph_triples(path: Path):
    rows = []
    for line in path.read_text(encoding="utf-8").splitlines():
        parts = line.split("\t")
        if len(parts) == 3:
            rows.append(tuple(parts))
    return rows


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--chars", type=int, default=400_000,
                        help="cap on the character corpus, so a source's SIZE does not "
                             "silently become the variable")
    args = parser.parse_args()

    tokens, kinds, masks = mqar_stream()
    shakespeare = (ROOT / "data" / "tinyshakespeare.txt").read_text(
        encoding="utf-8")[:args.chars]
    fb = graph_triples(ROOT / "data" / "fb15k237" / "train.txt")
    openea = sorted((ROOT / "data" / "openea").glob("**/rel_triples_1"))

    sources: list[tuple[str, object]] = [
        ("MQAR tokens", lambda w: ngrams(tokens, w)),
        ("shakespeare", lambda w: ngrams(list(shakespeare), w)),
        ("FB15k-237", lambda w: triple_addresses(fb, min(w, 3))),
    ]
    if openea:
        graph = graph_triples(openea[0])
        sources.append((f"openea {openea[0].parent.name[:12]}",
                        lambda w, g=graph: triple_addresses(g, min(w, 3))))

    print(f"\nWorthy share, and the enrichment bar it implies. Bar = (1-w)/w.\n"
          f"MQAR {len(tokens):,} positions, shakespeare {len(shakespeare):,} chars, "
          f"FB15k-237 {len(fb):,} triples.\n")
    print(f"{'source':<22}" + "".join(f"{'w=' + str(w):>18}" for w in WIDTHS))
    for name, build in sources:
        cells = []
        for width in WIDTHS:
            share, _, _ = worthiness(build(width))
            bar = bar_for(share, 1.0 - share) if share else float("inf")
            cells.append(f"{share:.4f} / {bar:>7.1f}x")
        print(f"{name:<22}" + "".join(f"{c:>18}" for c in cells))
    print("\n  each cell is  worthy share / bar for a HALF-real stored set")
    print("  a graph has no 4th symbol, so its w=4 column repeats w=3 by construction")

    # THE CONTROL. `g28-01` measured 91.9x on this exact config using the ORACLE.
    #
    # **The first version of this control read 23.0x, and it was mis-specified in
    # exactly the way `g28-01`'s own P1 was.** It counted every non-filler position as
    # should-store, where `g28-01` counts only what `oracle_mask` marks -- a position
    # whose PREDECESSOR was a pair, which is the binding a write gate must keep -- and
    # scores it against filler positions alone, excluding queries from both sides.
    # Two different classes, one name. The comparison set is now the same one.
    keep = sum(1 for m in masks if m)
    filler = sum(1 for k in kinds if k == "filler")
    oracle_bar = bar_for(keep / (keep + filler), filler / (keep + filler))
    print(f"\n  CONTROL, MQAR by ORACLE label: {keep:,} should-store against "
          f"{filler:,} filler -> bar {oracle_bar:.1f}x")
    print("  g28-01 measured 91.9x on this config. The label-free MQAR row above is")
    print("  the same stream counted without labels; P1 asks them to agree within 15%.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
