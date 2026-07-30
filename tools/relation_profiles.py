"""Note 070's measurement: do extensional relation vectors make composition generalise?

## What this answers

`openplexus/keys.py` derives a relation's key by hashing its token id, so `father` and
`mother` are as unrelated as `father` and `7`. Note 067 measured the consequence — a
binding over random relation vectors names held-out compositions at the rate of guessing
(0.056 against chance 0.050) — and left one thing untried, calling it *"the whole
question"*: relation vectors that encode how relations resemble one another.

This is that experiment. Each relation is profiled by **how other relations attach to the
entities it links**, which is derivable from the store the model already builds:

    edge (a, r, b), any other edge sharing an entity
        (a, s, x) -> feature (s, "HH")     r's head is s's head
        (x, s, a) -> feature (s, "HT")     r's head is s's tail
        (b, s, x) -> feature (s, "TH")
        (x, s, b) -> feature (s, "TT")

**The attachment type is the whole claim.** Drop it and this is relation-relation
co-occurrence, which note 058 measured flat. Keep it and the profile separates roles.

## The controls, because four earlier versions of this script were wrong

    random arm       note 067's setup. `--bind hadamard` MUST give ~0.056, and that
                     gate is the only reason the rest is reportable
    collapsed        attachment type discarded, i.e. note 058's mechanism
    per-seed profile a 2-hop puzzle's labelled query edge is admitted ONLY when its
                     rule is in that seed's training split. Without this, a puzzle
                     writes its own rule into its target's profile -- which was the
                     ENTIRE effect the first time this was run

Two-hop rules only, no bootstrap: derived rules would let a 3-hop puzzle's target leak
the same way, and closing that path is worth the coarser 16-item holdout.

## What it does not settle

Held-out RULE prediction, not CLUTRR accuracy. And kinship has unusually strong positional
structure, so a positive number here is not evidence about a domain that does not.
"""

from __future__ import annotations

import argparse
import ast
import collections
import csv
import itertools
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from openplexus.tasks.clutrr import RELATIONS  # noqa: E402

ATTACHMENTS = ("HH", "HT", "TH", "TT")
#: Ridge, not least squares. 46 training rules against 136 concat features is
#: underdetermined, and the unregularised fit scored 0.917 on training data for
#: extensional, collapsed AND random vectors -- identical to three decimals, which is a
#: fit measuring its own capacity rather than the representations it was given.
ALPHA = 1.0


def hadamard(left: np.ndarray, right: np.ndarray) -> np.ndarray:
    """Elementwise binding. The arm that must reproduce note 067's 0.056."""
    return left * right


def convolve(left: np.ndarray, right: np.ndarray) -> np.ndarray:
    """Circular convolution, i.e. HRR binding."""
    return np.real(np.fft.ifft(np.fft.fft(left) * np.fft.fft(right)))


def both(left: np.ndarray, right: np.ndarray) -> np.ndarray:
    """Concat AND convolution.

    Concat carries the marginals — which relation sits in which slot, worth 0.242 on its
    own (note 069) — and convolution carries structure that survives losing slot
    identity. Note 070's headline uses this.
    """
    return np.concatenate([left, right, convolve(left, right)])


BINDINGS = {"hadamard": hadamard, "convolve": convolve, "both": both}


def rows(root: Path, config: str, split: str):
    """`(edges, types, query, target)` per puzzle, straight from the CSV.

    `openplexus.tasks.clutrr.load` discards the graph once it has tokenised, and this
    needs the graph, so it reads the file rather than reconstructing edges from tokens.
    """
    path = root / config / f"{split}.csv"
    if not path.exists():
        raise FileNotFoundError(
            f"{path} is not there. CLUTRR is fetched rather than committed: run "
            f"`python tools/fetch_clutrr.py`, which verifies size and sha256")
    with path.open(encoding="utf-8", newline="") as handle:
        for row in csv.DictReader(handle):
            yield (ast.literal_eval(row["story_edges"]),
                   ast.literal_eval(row["edge_types"]),
                   ast.literal_eval(row["query_edge"]),
                   row["target_text"])


def base_rules(puzzles) -> dict[tuple[str, str], str]:
    """`(r1, r2) -> t` from 2-hop chains, keeping only unambiguous pairs.

    Note 066 gets 62 of these and this must agree, which is the check that the
    extraction is sound before anything is measured on top of it.
    """
    seen: dict = collections.defaultdict(collections.Counter)
    for edges, types, _, target in puzzles:
        if len(edges) == 2 and edges[0][1] == edges[1][0]:
            seen[(types[0], types[1])][target] += 1
    return {pair: answers.most_common(1)[0][0]
            for pair, answers in seen.items() if len(answers) == 1}


def profile(puzzles, permitted: set, positional: bool) -> np.ndarray:
    """Row-normalised profiles over `RELATIONS`.

    `permitted` names the rules whose targets may be used. A 2-hop puzzle contributes its
    labelled query edge only if its own rule is in there; that is what keeps a held-out
    rule out of the representation.
    """
    counts: dict = collections.defaultdict(collections.Counter)
    for edges, types, query, target in puzzles:
        if len(edges) != 2:
            continue
        labelled = list(zip(edges, types))
        if (types[0], types[1]) in permitted:
            labelled = labelled + [(tuple(query), target)]
        for (head, tail), relation in labelled:
            for (other_head, other_tail), other in labelled:
                if (other_head, other_tail) == (head, tail):
                    continue
                for side, mine in (("H", head), ("T", tail)):
                    for position, entity in (("H", other_head), ("T", other_tail)):
                        if entity == mine:
                            counts[relation][
                                (other, side + position) if positional else other] += 1
    keys = sorted({key for row in counts.values() for key in row}, key=repr)
    matrix = np.zeros((len(RELATIONS), len(keys)))
    for i, name in enumerate(RELATIONS):
        for j, key in enumerate(keys):
            matrix[i, j] = counts[name][key]
    norms = np.linalg.norm(matrix, axis=1, keepdims=True)
    return matrix / np.where(norms == 0, 1.0, norms)


def score(vectors: np.ndarray, items: list, order: np.ndarray, bind) -> float:
    """Fit on 75% of the rules, return accuracy on the held-out quarter."""
    index = {name: i for i, name in enumerate(RELATIONS)}
    cut = int(len(items) * 0.75)
    train = [items[i] for i in order[:cut]]
    test = [items[i] for i in order[cut:]]

    def design(subset):
        x = np.array([bind(vectors[index[a]], vectors[index[b]])
                      for (a, b), _ in subset])
        y = np.zeros((len(subset), len(RELATIONS)))
        for row, (_, target) in enumerate(subset):
            y[row, index[target]] = 1.0
        return x, y

    xt, yt = design(train)
    weights = np.linalg.solve(xt.T @ xt + ALPHA * np.eye(xt.shape[1]), xt.T @ yt)
    x, _ = design(test)
    want = np.array([index[target] for _, target in test])
    return float(((x @ weights).argmax(axis=1) == want).mean())


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT / "data" / "clutrr")
    parser.add_argument("--config", default="gen_train23_test2to10")
    parser.add_argument("--bind", choices=sorted(BINDINGS), default="both")
    parser.add_argument("--seeds", type=int, default=120)
    parser.add_argument("--collapsed", action="store_true",
                        help="discard the attachment type, i.e. note 058's mechanism")
    args = parser.parse_args()

    puzzles = list(itertools.chain(
        rows(args.root, args.config, "train"),
        rows(args.root, args.config, "validation")))
    rules = base_rules(rows(args.root, args.config, "train"))
    items = sorted(rules.items())
    held = len(items) - int(len(items) * 0.75)
    bind = BINDINGS[args.bind]

    print(f"{len(items)} unambiguous 2-hop rules (note 066 gets 62), "
          f"{held} held out per seed, resolution {1 / held:.3f}")
    print(f"chance {1 / len(RELATIONS):.3f}, "
          f"majority {collections.Counter(rules.values()).most_common(1)[0][1] / len(items):.3f}, "
          f"marginal baseline 0.242 (note 069)")

    extensional, random_arm, unplaced = [], [], set()
    for seed in range(args.seeds):
        order = np.random.default_rng(seed).permutation(len(items))
        permitted = {items[i][0] for i in order[:int(len(items) * 0.75)]}
        vectors = profile(puzzles, permitted, not args.collapsed)
        unplaced.add(int((np.linalg.norm(vectors, axis=1) == 0).sum()))
        control = np.random.default_rng(seed).normal(0.0, 1.0, vectors.shape)
        control /= np.linalg.norm(control, axis=1, keepdims=True)
        extensional.append(score(vectors, items, order, bind))
        random_arm.append(score(control, items, order, bind))

    ours, theirs = np.array(extensional), np.array(random_arm)
    difference = ours - theirs
    stderr = difference.std(ddof=1) / np.sqrt(len(difference))
    print(f"\nbind={args.bind}"
          f"{', collapsed' if args.collapsed else ''}, {args.seeds} seeds, "
          f"unplaced relations per seed {sorted(unplaced)} of {len(RELATIONS)}")
    print(f"  extensional   {ours.mean():.3f}")
    print(f"  random        {theirs.mean():.3f}")
    print(f"  PAIRED DIFF  {difference.mean():+.3f}  se {stderr:.3f}  "
          f"t = {difference.mean() / stderr:.1f}")
    print(f"  wins {(difference > 0).mean():.0%}, ties {(difference == 0).mean():.0%}, "
          f"loses {(difference < 0).mean():.0%}")
    if args.bind == "hadamard" and not args.collapsed:
        print(f"\n  P0: the random arm is note 067's setup and must land near 0.056. "
              f"It is {theirs.mean():.3f}.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
