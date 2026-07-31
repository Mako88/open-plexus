"""Relation vectors learned by a LOCAL contrastive rule, scored by note 070's harness.

## What this does not duplicate

Searched by capability across `openplexus/`, `tools/`, `tests/`, `experiments/`,
`testbed/` and `docs/archive/` before writing:

    tools/relation_profiles.py  note 070's extensional vectors. Builds a relation's
                                vector by COUNTING how other relations attach to the
                                entities it links. No objective, no negatives, no
                                gradient. Its `score`, `rows`, `base_rules` and
                                `BINDINGS` are IMPORTED here rather than rewritten --
                                the evaluation is the part that must not fork
    openplexus/content.py       distributional token vectors, and its docstring says
                                outright "no objective, no negative sampling and no
                                gradient". Same family as the above, different objects
    openplexus/keys.py          hashes a relation id, so `father` and `mother` are as
                                unrelated as `father` and `7`. The thing being replaced
    tools/generation_delta.py   solves for a conserved quantity from loop equations.
                                Exact linear algebra over cycles, not a learned
                                representation, and scoped by note 104 to graphs that
                                HAVE an invariant

**So what is new here is the objective**, and only that: an explicit local
positive/negative signal with a gradient. Everything about how the result is judged
comes from `relation_profiles`.

## The mechanism, and why it is local

A 2-hop CLUTRR puzzle is a closing triangle: `a -r1-> b -r2-> c` with `a -r3-> c`.

    POSITIVE   (r1, r2) composed should land ON r3
    NEGATIVE   (r1, r2) composed should land far from every OTHER relation

Both are constructible from **one puzzle**, by one node, with no global view: the
negatives are the other relations in the alphabet, which every node already knows.
Nothing here needs a population statistic, a barrier, or a second machine.

## What would make this vacuous, guarded rather than hoped for

**The held-out rules must not reach the representation.** Note 070's number was
`0.223` on a RANDOM quarter, and note 088 showed the same mechanism falls below random
filling once the holdout is adversarial -- so a rule leaking into the vectors is the
difference between a result and an artefact. `permitted` is threaded through exactly as
`relation_profiles.profile` threads it, and `tests/test_relation_contrastive.py`
asserts a held-out rule's triangle never contributes an update.

**And the random arm is the gate.** Untrained vectors must score near `0.056`
(note 067's measurement, chance `0.050`). If the random arm scores well, the harness is
measuring the ridge fit rather than the representation, and nothing else in the run is
reportable.

## What it measured, 2026-07-30, and how much to trust it

10 seeds, width 32, 8 epochs, `--bind hadamard`, 62 rules with a 16-rule holdout:

    untrained (random arm)              0.0312 +/-0.0133
    counted extensional, same binding   0.0690          (tools/relation_profiles.py)
    contrastive, learned                0.2437 +/-0.0419

    held-out rules EXCLUDED             0.2437
    held-out rules INCLUDED, a leak     0.4188
    so the guard is worth              +0.1750

**Read the binding caveat before quoting the middle row.** The counted vectors score
`0.069` under `hadamard` and `0.223` under `both` (note 070's headline). So matched-binding
is `0.244` against `0.069`, and best-against-best is `0.244` against `0.223`. Both are
true and neither alone is.

**THIS IS WEAKER EVIDENCE THAN A SWEEP AND THE REASON IS PROCESS.** Rule 4 requires a local
probe's prediction to be committed BEFORE the run, so git ordering is the evidence. That
was not done here -- the numbers were produced first and written down afterwards, which
makes them an observation rather than a test of a prediction. Recorded as such rather than
presented as if the discipline had been followed.

**And it is the EASY holdout.** A random quarter, which is note 070's split. Note 088
measured the counted mechanism below random filling once the holdout was an adversarially
withheld family. Nothing here has faced that, and it is the test that decides whether this
is a result.

## And on graphs with NO conserved quantity, 2026-07-30

`--graph`, 5 seeds, 75/25 rule split, scored by nearest relation. Dimensions from
`tools/invariant_dimension.py --graph` on the SAME file:

    graph                     dim  contrastive  counted  majority  untrained
    EN_DE_15K_V2/rel_triples_1  0   0.3680       0.2505   0.1000    0.0359
    D_W_15K_V1/rel_triples_1    2   0.3398       0.2602   0.0797    0.0441

    determinism 0.778 and 0.754, against kinship's near-1.0

**`counted` is the arm that matters and it is strong.** No learning, no vectors, no
gradient -- just "the commonest r3 among training rules sharing r1". It closes most of
the distance from `majority` on its own, and the objective adds ~0.12 on top of it
(`g23-02`). Quoting the contrastive score against `majority` quotes the weak opponent.

**Earlier figures here were NONDETERMINISTIC and are superseded.** They read 0.3602 /
0.3559 with majorities 0.0942 / 0.0559. `graph_rules` iterated a set of relation
strings and Python randomises string hashing per process, so tie breaks -- and
therefore the rules, and therefore the split -- moved between runs of the same seed.
**The counted arm exposed it by varying when it has no randomness of its own.** Now
deterministic across three consecutive runs.

**`dim 0` is where `tools/generation_delta.py` gets nothing at all** -- not weakly, but
structurally, which is the scope note 104 imposed on the whole composition line. **`dim 2`
is the case that tool explicitly REFUSES.** The contrastive representation scores the same
in both and cleared the end-task bar on kinship's `dim 1`, so it does not depend on a
conserved quantity existing.

**A wrong claim caught one step short.** The first run used `D_W_15K_V1` and was about to
be reported as a no-invariant graph. Measuring that exact file returned **2, not 0** --
note 104's dim-0 finding is about `EN_DE`. Both rows name their dimension because of it.

**These were NOT pre-registered**, unlike `g23-01`, and are therefore observations rather
than tested predictions. Rule 4's distinction, stated rather than blurred.

## Sixteen graphs, and the margin does not track the invariant -- `g23-03`

    dim 0    n=13   mean margin over counting  +0.0974
    dim >=1  n=3    mean margin over counting  +0.0507
    difference 0.0467, against a bar of 0.05 registered before the run

**The sign is the reassuring part.** A mechanism exploiting conserved quantities would
score HIGHER where they exist; this scores higher where they do not. Fragile at `dim >= 1`
-- three graphs, one of them a near-tie -- so it supports *"the margin does not increase
with an invariant"*, not *"the margin is independent of it"*.

**It loses on two of sixteen**, and the informative one is `D_Y_V1_s2` at **-0.0834 with
48 rules** -- the smallest graph here, twelve rules held out. Counting wins where there is
almost nothing to learn from, so *"beats counting"* carries a data requirement somewhere
between 48 and 175 rules.

**And invariant dimension is a property of the EXTRACT, not the domain.** Three of eight
V1/V2 pairs disagree, all on the DBpedia side, all `dim >= 1` in V1 and `dim 0` in V2. So
`note 104`'s *"DBpedia EN and DE have no additive invariant"* is a statement about one
15,000-entity sample.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from openplexus.tasks.clutrr import RELATIONS  # noqa: E402
from tools.relation_profiles import (  # noqa: E402
    BINDINGS, base_rules, rows, score)

INDEX = {name: i for i, name in enumerate(RELATIONS)}


def triangles(puzzles, permitted: set) -> list[tuple[int, int, int]]:
    """Closing 2-hop triangles as `(r1, r2, r3)` indices.

    A puzzle contributes ONLY if its own rule is permitted. That is the guard note
    088 made necessary: a held-out rule whose triangle trains the representation has
    written its own answer into the thing being tested.
    """
    found = []
    for edges, types, _query, target in puzzles:
        if len(edges) != 2:
            continue
        if (types[0], types[1]) not in permitted:
            continue
        if target not in INDEX or any(t not in INDEX for t in types):
            continue
        found.append((INDEX[types[0]], INDEX[types[1]], INDEX[target]))
    return found


def learn(tris, width: int, seed: int, epochs: int, lr: float,
          temperature: float, n_relations: int | None = None) -> np.ndarray:
    """Relation vectors under a local contrastive rule. Returns `(len(RELATIONS), width)`.

    One update per observed triangle. The composition is elementwise product, which is
    `BINDINGS["hadamard"]` -- the same binding the scorer may be asked for, so the
    representation is not learned under one composition and judged under another.

    The loss is softmax cross-entropy of `compose(r1, r2) . r_k` over every relation
    `k`, with the true `r3` as the target. That IS the contrast: the numerator is the
    positive and the denominator is every negative, so no negative sampling schedule
    has to be chosen and none can be got wrong.

    **This is the single-alphabet case of `learn_pairs`** -- one table, every negative.
    Kept as the name every existing caller uses, and delegating rather than repeating
    the update, so there is one implementation of the rule.
    """
    # `n_relations` defaults to CLUTRR's alphabet so every existing caller is
    # unchanged, and is a parameter so the SAME learner can run on a graph with
    # hundreds of relations. Forking it for the second domain would put the one
    # mechanism under test in two files, which is rule 9's whole argument.
    count = len(RELATIONS) if n_relations is None else n_relations
    return learn_pairs(tris, width, seed, epochs, lr, temperature,
                       n_left=count, n_right=None)[0]


def learn_pairs(items, width: int, seed: int, epochs: int, lr: float,
                temperature: float, n_left: int, n_right: int | None = None,
                negatives: int | None = None):
    """The contrastive rule over `(a, b, target)`, with `a`/`target` in one table.

    Returns `(left, right)`. **`n_right=None` means ONE SHARED TABLE** -- `right` is the
    same array as `left`, which is the CLUTRR and graph case where relations compose
    with relations. Give `n_right` when `b` is drawn from a different alphabet, as
    entities and relations are on a knowledge graph.

    `negatives=None` contrasts against EVERY row of `left`, which is the original rule
    and the default, so no existing result moves. **An integer samples that many
    negatives instead, and it is a real weakening rather than an optimisation:**

      - a sampled contrast is a schedule, `K` is a knob, and it can be chosen wrongly,
        which the full contrast could not be
      - the row normalisation becomes PARTIAL. Renormalising all `n_left` rows per
        update is the cost the sampling exists to avoid, so only touched rows are
        renormalised, and an untouched row keeps whatever norm it had

    Both are stated here rather than in a caller because this is where someone reading
    the update will be. `g30-02` is where the `K` grid and its results live.
    """
    rng = np.random.default_rng(seed)
    left = rng.normal(0.0, 1.0 / np.sqrt(width), (n_left, width))
    right = left if n_right is None else rng.normal(
        0.0, 1.0 / np.sqrt(width), (n_right, width))
    if len(items) == 0:
        return left, right

    order = np.arange(len(items))
    for _ in range(epochs):
        rng.shuffle(order)
        for i in order:
            a, b, target = items[i]
            composed = left[a] * right[b]

            if negatives is None:
                rows = None
                logits = left @ composed / temperature
                hit = target
            else:
                # The target sits at index 0 so `hit` needs no search, and duplicate
                # draws are harmless because the scatter below accumulates.
                rows = np.concatenate(
                    ([target], rng.integers(0, n_left, negatives)))
                logits = left[rows] @ composed / temperature
                hit = 0

            logits -= logits.max()
            probs = np.exp(logits)
            probs /= probs.sum()
            error = probs
            error[hit] -= 1.0

            # EVERY gradient is computed before ANY table update. With one shared
            # table `right[b]` and `left[a]` are rows of the array being written, so
            # applying the bulk step first would feed updated values into `grad_a`
            # and `grad_b` -- a different rule from the one CLUTRR's numbers were
            # measured under, and silent.
            source = left if rows is None else left[rows]
            d_composed = (source.T @ error) / temperature
            grad_a = d_composed * right[b]
            grad_b = d_composed * left[a]
            # Parenthesised to divide BEFORE multiplying by `lr`, matching the
            # pre-refactor `grad = outer / T` then `-= lr * grad`. Without it the
            # rounding differs in the last bits and `learn` stops being identical
            # to the code every recorded CLUTRR and graph number came from.
            bulk = lr * (np.outer(error, composed) / temperature)

            if rows is None:
                left -= bulk
            else:
                # `np.add.at` and not `left[rows] -=`, because fancy-index assignment
                # keeps only the LAST write when an index repeats, and a repeat is
                # exactly what sampling with replacement produces.
                np.add.at(left, rows, -bulk)
            left[a] -= lr * grad_a
            right[b] -= lr * grad_b

            if rows is None:
                _renormalise(left)
                if right is not left:
                    _renormalise(right)
            else:
                _renormalise(left, np.append(rows, a))
                _renormalise(right if right is not left else left, np.array([b]))
    return left, right


def _renormalise(table: np.ndarray, rows: np.ndarray | None = None) -> None:
    """Unit-normalise every row, or only `rows`. In place."""
    view = table if rows is None else table[rows]
    norms = np.linalg.norm(view, axis=-1, keepdims=True)
    scaled = view / np.where(norms == 0, 1.0, norms)
    if rows is None:
        table[:] = scaled
    else:
        table[rows] = scaled


def graph_rules(path: Path):
    """`(items, n_relations)` for a knowledge graph of `(s, r, o)` TSV triples.

    A rule is a closing triangle `a -r1-> b -r2-> c` with `a -r3-> c` present. Real
    graphs are not functional -- one `(r1, r2)` reaches several `r3` -- so the rule
    is the COMMONEST `r3`, and the determinism it reports is how often that choice
    is right. Kinship is near 1.0; these are near 0.78, which is the ceiling this
    evaluation is measured against and it is well below 1.

    **Reads the same TSV shape `tools/invariant_dimension.py` reads**, deliberately,
    so the invariant dimension and this can be quoted about the same file without
    anyone having to check they parsed it the same way.
    """
    edges = [tuple(line.split("\t"))
             for line in path.read_text(encoding="utf-8").splitlines()
             if len(line.split("\t")) == 3]
    relations = sorted({r for _, r, _ in edges})
    index = {r: i for i, r in enumerate(relations)}

    outgoing: dict = {}
    direct: dict = {}
    for subject, relation, obj in edges:
        outgoing.setdefault(subject, []).append((relation, obj))
        direct.setdefault((subject, obj), set()).add(relation)

    counts: dict = {}
    for start, first in outgoing.items():
        for r1, middle in first:
            for r2, end in outgoing.get(middle, ()):
                # `sorted`, because `direct`'s values are SETS of strings and
                # Python randomises string hashing per process -- so unsorted
                # iteration made the accumulation order, and therefore the tie
                # breaks below, differ between runs of the same seed.
                for r3 in sorted(direct.get((start, end), ())):
                    pair = (index[r1], index[r2])
                    counts.setdefault(pair, {})
                    counts[pair][index[r3]] = counts[pair].get(index[r3], 0) + 1

    # Ties broken by LOWEST relation index, not by insertion order. Three runs at
    # identical seeds gave 0.3641, 0.3680 and 0.3583 before this -- the split
    # itself was moving, which the counted arm exposed because it has no
    # randomness of its own and varied anyway.
    items = sorted((pair, max(answers.items(), key=lambda kv: (kv[1], -kv[0]))[0])
                   for pair, answers in counts.items())
    determinism = float(np.mean([max(a.values()) / sum(a.values())
                                 for a in counts.values()])) if counts else 0.0
    return items, len(relations), determinism


def counted_predictor(train):
    """The cheap opponent: no learning, no vectors, no gradient. Counting.

    For a held-out `(r1, r2)`, answer in this order — **specified in `g23-02`
    before the arm was built**, because a counting baseline has many reasonable
    definitions and choosing the one that loses is the easiest unconscious cheat
    available:

        1. commonest r3 among training rules whose FIRST element is r1
        2. failing that, commonest among those whose SECOND is r2
        3. failing that, the global commonest

    It sees exactly the training rules the contrastive arm sees, so a difference
    between them is the objective rather than the split.
    """
    by_first: dict = {}
    by_second: dict = {}
    overall: dict = {}
    for (r1, r2), r3 in train:
        for table, key in ((by_first, r1), (by_second, r2), (overall, None)):
            table.setdefault(key, {})
            table[key][r3] = table[key].get(r3, 0) + 1

    def commonest(counts):
        return max(counts.items(), key=lambda kv: kv[1])[0] if counts else None

    fallback = commonest(overall.get(None, {}))

    def predict(pair):
        r1, r2 = pair
        if r1 in by_first:
            return commonest(by_first[r1])
        if r2 in by_second:
            return commonest(by_second[r2])
        return fallback

    return predict


def score_by_nearest(vectors, test) -> float:
    """Accuracy of `argmax(vectors @ (v_r1 * v_r2))` over held-out rules.

    **A different scorer from the CLUTRR path above, and the difference is stated
    rather than hidden.** That one fits a ridge readout over composed vectors
    (note 070's harness, reused so the comparison to the counted representation is
    like for like). This one takes the nearest relation directly, which is exactly
    what `generation_delta.make_fold`'s contrastive mode does -- so a graph number
    here and a fold number there are the same decision rule.
    """
    hits = 0
    for (r1, r2), r3 in test:
        composed = vectors[r1] * vectors[r2]
        hits += int(int(np.argmax(vectors @ composed)) == r3)
    return hits / len(test) if test else 0.0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--graph", type=Path, default=None,
                        help="a TSV of (s, r, o) triples instead of CLUTRR. Pair "
                             "with tools/invariant_dimension.py --graph on the "
                             "SAME file, so the invariant dimension and this are "
                             "quoted about one graph")
    parser.add_argument("--root", type=Path, default=ROOT / "data" / "clutrr")
    parser.add_argument("--config", default="gen_train23_test2to10")
    parser.add_argument("--bind", choices=sorted(BINDINGS), default="hadamard")
    parser.add_argument("--seeds", type=int, default=20)
    parser.add_argument("--width", type=int, default=32)
    parser.add_argument("--epochs", type=int, default=8)
    parser.add_argument("--lr", type=float, default=0.05)
    parser.add_argument("--temperature", type=float, default=0.1)
    parser.add_argument("--random-arm", action="store_true",
                        help="score UNTRAINED vectors. The gate: must land near 0.056")
    args = parser.parse_args()

    if args.graph is not None:
        items, n_relations, determinism = graph_rules(args.graph)
        print(f"{args.graph.name}: {n_relations} relations, {len(items)} rules, "
              f"determinism {determinism:.3f}")
        scores, majorities, counted = [], [], []
        for seed in range(args.seeds):
            rng = np.random.default_rng(seed)
            order = rng.permutation(len(items))
            cut = int(len(items) * 0.75)
            train = [items[i] for i in order[:cut]]
            test = [items[i] for i in order[cut:]]
            tris = [] if args.random_arm else [(a, b, r) for (a, b), r in train]
            vectors = learn(tris, args.width, seed,
                            0 if args.random_arm else args.epochs,
                            args.lr, args.temperature, n_relations)
            scores.append(score_by_nearest(vectors, test))
            # The MAJORITY baseline is recomputed per graph, never carried. Two
            # baselines were got wrong on 2026-07-30 by reusing one measured
            # somewhere else, and a wrong baseline moves every arm together.
            counts: dict = {}
            for _, r3 in train:
                counts[r3] = counts.get(r3, 0) + 1
            common = max(counts.items(), key=lambda kv: kv[1])[0]
            majorities.append(float(np.mean([r3 == common for _, r3 in test])))
            # The STRONG cheap opponent, on the identical split. g23-02.
            predict = counted_predictor(train)
            counted.append(float(np.mean([predict(pair) == r3
                                          for pair, r3 in test])))
        arm = "RANDOM (untrained)" if args.random_arm else "contrastive"
        print(f"  {arm:<20} {np.mean(scores):.4f} "
              f"+/-{np.std(scores) / np.sqrt(len(scores)):.4f}")
        print(f"  {'counted (no learning)':<20} {np.mean(counted):.4f} "
              f"+/-{np.std(counted) / np.sqrt(len(counted)):.4f}")
        print(f"  {'majority class':<20} {np.mean(majorities):.4f} "
              f"+/-{np.std(majorities) / np.sqrt(len(majorities)):.4f}")
        print(f"  {'chance':<20} {1 / n_relations:.4f}")
        return 0

    puzzles = list(rows(args.root, args.config, "train"))
    rules = base_rules(puzzles)
    items = sorted(rules.items())
    bind = BINDINGS[args.bind]

    scores = []
    for seed in range(args.seeds):
        rng = np.random.default_rng(seed)
        order = rng.permutation(len(items))
        cut = int(len(items) * 0.75)
        permitted = {items[i][0] for i in order[:cut]}
        tris = triangles(puzzles, permitted)
        vectors = (learn([], args.width, seed, 0, 0.0, 1.0) if args.random_arm
                   else learn(tris, args.width, seed, args.epochs, args.lr,
                              args.temperature))
        scores.append(score(vectors, items, order, bind))

    mean = float(np.mean(scores))
    error = float(np.std(scores) / np.sqrt(len(scores)))
    arm = "RANDOM (untrained)" if args.random_arm else f"contrastive w{args.width}"
    print(f"{arm}: {mean:.4f} +/-{error:.4f} over {args.seeds} seeds, "
          f"{len(items)} rules, holdout {len(items) - int(len(items) * 0.75)}")
    if args.random_arm and mean > 0.15:
        print("THE GATE FAILED. Untrained vectors should score near 0.056 "
              "(note 067, chance 0.050). Above that the harness is measuring the "
              "ridge fit rather than the representation, and no other arm in this "
              "run is reportable.")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
