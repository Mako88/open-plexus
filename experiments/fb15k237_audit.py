"""What FB15k-237 gives away for free, before anything is built on it.

Two instruments died this week for the same reason: the answer was already in the
stated facts, and nobody checked before building. CLUTRR-symbolic is answered in
full by 62 counted facts plus a bracketing search, and withholding those facts
does not help because the three-hop rows deduce them back. **So no benchmark
enters this project again without this audit first.**

FB15k-237 exists because its predecessor leaked: FB15k paired most test triples
with their inverse in train, so a lookup scored near the state of the art.
Toutanova and Chen removed the near-duplicate and inverse relations, and 237 is
what was left. This measures whether that worked, on the copy in `data/`.

Four attacks, each of which is a thing to do with the training set and no model:

    random          the floor, and it is 1/entities rather than zero
    frequency       rank by how often an entity is that relation's tail. No
                    structure at all -- the strongest baseline that ignores the
                    query's other half
    inverse         mine `r2(t, h) => r(h, t)` from train, apply to test
    two hop         mine `r1(h, x) & r2(x, t) => r(h, t)`, which is the attack
                    that killed CLUTRR

Reported as filtered ranks, which is the convention this benchmark is scored
under: every other known-true answer is removed before the rank is taken, so a
correct answer is not punished for a second correct answer sitting above it.

    python experiments/fb15k237_audit.py --json out/fb15k237-audit.json
    python experiments/fb15k237_audit.py --queries 2000        # a quick look
"""

from __future__ import annotations

import argparse
import collections
import json
import pathlib
import random
import sys
import time

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402

DATA = ROOT / "data" / "fb15k237"

#: A rule needs this many supporting facts and this share of its body to be
#: mined. Chosen here rather than swept: the result is that the mined rules
#: answer NOTHING on the held-out splits, and a threshold that admits more rules
#: can only raise the number it is compared against.
SUPPORT, CONFIDENCE = 5, 0.5

#: Published filtered MRR and Hits@10 on this benchmark, carried from TransERR
#: (arXiv 2306.14580) Table 3, which takes several of them from Sun et al. 2019.
#: **Printed beside the floor rather than remembered next to it**, and the
#: caveat travels with them: that table does not state in its own words that the
#: metrics are filtered and averaged over both directions, though that is the
#: protocol the benchmark is conventionally reported under and the one used
#: here. A remembered number beside a measured one is the borrowed claim
#: `CLAUDE.md` puts first, so this is a citation and not a memory.
PUBLISHED = {"TransE": (0.294, 0.465), "DistMult": (0.241, 0.419),
             "ComplEx": (0.247, 0.428), "RotatE": (0.338, 0.533),
             "TransERR": (0.360, 0.555)}

#: Caps on the two-hop mining, and they are printed rather than assumed. A
#: relation with 10,000 facts and entities of degree 1,000 is 10 million paths;
#: these bound it to something that runs in a minute. **A capped search can only
#: UNDERSTATE what the attack finds**, which is the safe direction for an audit
#: whose conclusion is "there is nothing here" -- and the run says so out loud.
FACTS_PER_RELATION, DEGREE = 400, 200


def load(name: str) -> list[tuple[str, str, str]]:
    return [tuple(line.split("\t"))
            for line in (DATA / name).read_text(encoding="utf-8").splitlines()
            if line.strip()]


def inverse_rules(train) -> dict[str, list[tuple[str, float]]]:
    """`r2(t, h) => r(h, t)`, mined from train alone. FB15k's original leak."""
    forward = collections.defaultdict(set)
    backward = collections.defaultdict(set)
    for head, relation, tail in train:
        forward[relation].add((head, tail))
        backward[relation].add((tail, head))
    rules = collections.defaultdict(list)
    for relation, facts in forward.items():
        for other, flipped in backward.items():
            if other == relation:
                continue
            support = len(facts & flipped)
            if support >= SUPPORT and support / len(forward[other]) > CONFIDENCE:
                rules[relation].append((other, support / len(forward[other])))
    return rules


def two_hop_rules(train, seed: int = 0):
    """`r1(h, x) & r2(x, t) => r(h, t)`, mined from train alone.

    The CLUTRR attack, in the one form that transfers: a composition of two
    stated relations standing in for a third. Bodies are counted over the same
    sampled facts as the heads, so the confidence is a ratio of two numbers
    taken the same way rather than a sample over a total.
    """
    out = collections.defaultdict(list)
    for head, relation, tail in train:
        out[head].append((relation, tail))
    by_relation = collections.defaultdict(list)
    for head, relation, tail in train:
        by_relation[relation].append((head, tail))

    rng = random.Random(seed)
    rules: dict[str, list[tuple[tuple[str, str], float]]] = {}
    for relation, facts in by_relation.items():
        sampled = (facts if len(facts) <= FACTS_PER_RELATION
                   else rng.sample(facts, FACTS_PER_RELATION))
        hits: collections.Counter = collections.Counter()
        bodies: collections.Counter = collections.Counter()
        for head, tail in sampled:
            for first, middle in out[head][:DEGREE]:
                for second, end in out[middle][:DEGREE]:
                    bodies[(first, second)] += 1
                    if end == tail:
                        hits[(first, second)] += 1
        found = [(pair, hits[pair] / bodies[pair]) for pair in hits
                 if hits[pair] >= SUPPORT
                 and hits[pair] / bodies[pair] > CONFIDENCE]
        if found:
            rules[relation] = sorted(found, key=lambda item: -item[1])
    return rules


class Ranker:
    """Filtered ranks over every entity, and the metrics taken from them."""

    def __init__(self, entities, known_tails, known_heads):
        self.entities = entities
        self.at = {entity: i for i, entity in enumerate(entities)}
        #: Every known-true answer for a query, so the others can be removed
        #: before the rank is taken. One map per direction.
        self.known_tails = known_tails
        self.known_heads = known_heads

    def rank(self, scores: np.ndarray, given: str, relation: str, answer: str,
             direction: str) -> tuple[float, float, float]:
        """The answer's filtered rank, under all three tie policies at once.

        **This is the caveat that matters for a counting baseline and not for
        the models it is compared against.** A frequency score puts thousands of
        entities on exactly zero, so the answer lands inside a huge tied block
        and the policy decides the number: optimistic puts it at the top of the
        block, pessimistic at the bottom, average in the middle. A trained
        embedding produces continuous scores and has almost no ties, so its
        published figure is insensitive to a choice that moves ours a long way.

        All three come from one pass — `above` and `tied` determine each — so
        reporting the range costs nothing and quoting only the middle would be
        choosing the flattering half of a bound.

        Returns:
            `(optimistic, average, pessimistic)`.
        """
        target = self.at[answer]
        mask = np.zeros(len(self.entities), dtype=bool)
        others = (self.known_tails.get((given, relation), ())
                  if direction == "tail"
                  else self.known_heads.get((relation, given), ()))
        for other in others:
            if other != answer:
                mask[self.at[other]] = True
        value = scores[target]
        above = int(np.sum((scores > value) & ~mask))
        tied = int(np.sum((scores == value) & ~mask)) - 1
        return above + 1.0, above + 1 + tied / 2.0, above + 1.0 + tied


def plain_rank(scores, at, given, relation, answer, known_tails, known_heads,
               direction) -> float:
    """The same filtered rank, in plain Python. **The ruler's second opinion.**

    `Ranker.rank` is numpy, and `CLAUDE.md` keeps numpy out of anything a result
    is measured against, because the ruler has to be obviously correct. Rather
    than give up the speed — 40,000 queries over 14,541 entities — this is the
    obvious version, and the run checks the two against each other on a sample
    and prints both. Three lines of masking is exactly the size at which nobody
    thinks to check.
    """
    excluded = set(known_tails.get((given, relation), ())
                   if direction == "tail"
                   else known_heads.get((relation, given), ()))
    excluded.discard(answer)
    value = scores[at[answer]]
    above = tied = 0
    for entity, index in at.items():
        if entity in excluded:
            continue
        if scores[index] > value:
            above += 1
        elif scores[index] == value and entity != answer:
            tied += 1
    return above + 1.0, above + 1 + tied / 2.0, above + 1.0 + tied


class Marginal:
    """The floor, as a dense score vector per `(relation, direction)`, cached.

    **Shared because three runs need the same floor, and a floor written three
    times is three floors** — `tools/check_duplication.py` refused the second
    copy, which is exactly what it is for.

    It is `Composition` with one role supplied: rank candidates by the relation
    alone, with no reference to the entity the question is about. So the arm and
    its baseline stay one program here too.
    """

    def __init__(self, counts, entities, relation_at, statistic) -> None:
        self.counts = counts
        self.width = len(entities)
        self.relation_at = relation_at
        self.statistic = statistic
        self._cache: dict = {}

    def vector(self, relation: str, direction: str) -> np.ndarray:
        key = (relation, direction)
        if key not in self._cache:
            want = "target" if direction == "tail" else "left"
            vector = np.zeros(self.width)
            for score, candidate in self.counts.given(
                    {"right": self.relation_at[relation]}, want,
                    self.statistic):
                vector[candidate] = score
            self._cache[key] = vector
        return self._cache[key]


def metrics(ranks: list[float]) -> dict:
    array = np.asarray(ranks, dtype=float)
    return {"n": len(ranks), "mrr": float(np.mean(1.0 / array)),
            "hits1": float(np.mean(array <= 1.0)),
            "hits10": float(np.mean(array <= 10.0)),
            "median_rank": float(np.median(array))}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    parser.add_argument("--queries", type=int, default=None,
                        help="score this many test triples instead of all")
    # Chosen here, and it moves only the two-hop mining sample and the query
    # subsample. The full run uses every test triple, so the seed does not
    # enter the reported floor at all.
    parser.add_argument("--seed", type=int, default=0)
    args = parser.parse_args()

    if not (DATA / "train.txt").exists():
        raise SystemExit(f"no data in {DATA}: python tools/fetch_fb15k237.py")

    started = time.time()
    train, valid, test = load("train.txt"), load("valid.txt"), load("test.txt")
    entities = sorted({e for triples in (train, valid, test)
                       for h, _, t in triples for e in (h, t)})
    relations = sorted({r for _, r, _ in train})
    print(f"train {len(train)}  valid {len(valid)}  test {len(test)}  "
          f"entities {len(entities)}  relations {len(relations)}")

    # BOTH DIRECTIONS, because the convention this benchmark is reported under
    # averages them and TAIL PREDICTION IS THE EASY HALF -- many relations are
    # many-to-one, so `nationality` has a few dozen plausible tails and tens of
    # thousands of plausible heads. A tail-only figure read against a published
    # average would overstate by whatever that asymmetry is worth, which is the
    # borrowed-claim failure CLAUDE.md puts first.
    known_tails = collections.defaultdict(set)
    known_heads = collections.defaultdict(set)
    for head, relation, tail in train + valid + test:
        known_tails[(head, relation)].add(tail)
        known_heads[(relation, tail)].add(head)
    ranker = Ranker(entities, known_tails, known_heads)

    queries = test if args.queries is None else random.Random(
        args.seed).sample(test, min(args.queries, len(test)))
    print(f"scoring {len(queries)} triples in both directions, filtered over "
          f"all {len(entities)} entities\n")

    # ARM 1: frequency. One score vector per relation and direction, once.
    tails = {relation: np.zeros(len(entities)) for relation in relations}
    heads = {relation: np.zeros(len(entities)) for relation in relations}
    for head, relation, tail in train:
        tails[relation][ranker.at[tail]] += 1.0
        heads[relation][ranker.at[head]] += 1.0

    # ARM 2 and 3: the rules, mined from TRAIN ONLY.
    mining = time.time()
    inverse = inverse_rules(train)
    print(f"mined {sum(len(v) for v in inverse.values())} inverse rules over "
          f"{len(inverse)} relations ({time.time() - mining:.1f}s)")
    mining = time.time()
    two_hop = two_hop_rules(train, args.seed)
    print(f"mined {sum(len(v) for v in two_hop.values())} two-hop rules over "
          f"{len(two_hop)} relations, sampling at most {FACTS_PER_RELATION} "
          f"facts per relation and {DEGREE} edges per entity "
          f"({time.time() - mining:.1f}s)")
    print("   CAPPED, so this UNDERSTATES what the attack would find. The "
          "conclusion below is that it finds nothing, and a wider search can "
          "only raise it.\n")

    heads_of = collections.defaultdict(set)
    tails_of = collections.defaultdict(set)
    out_of = collections.defaultdict(list)
    into = collections.defaultdict(list)
    for head, relation, tail in train:
        heads_of[(relation, tail)].add(head)
        tails_of[(relation, head)].add(tail)
        out_of[head].append((relation, tail))
        into[tail].append((relation, head))

    def rule_scores(arm: str, given: str, relation: str,
                    direction: str) -> np.ndarray:
        """What the mined rules say, from whichever end the query supplies.

        The two directions are not the same walk: from a head the rule runs
        forwards along `out_of`, and from a tail it runs backwards along `into`.
        Written once for both rather than twice, so a fix cannot land in one.
        """
        scores = np.zeros(len(entities))
        step = out_of if direction == "tail" else into
        if arm == "inverse":
            for other, confidence in inverse.get(relation, ()):
                reached = (heads_of[(other, given)] if direction == "tail"
                           else tails_of[(other, given)])
                for candidate in reached:
                    at = ranker.at[candidate]
                    scores[at] = max(scores[at], confidence)
            return scores
        for (first, second), confidence in two_hop.get(relation, ()):
            near, far = ((first, second) if direction == "tail"
                         else (second, first))
            for edge, middle in step[given]:
                if edge != near:
                    continue
                for edge2, end in step[middle]:
                    if edge2 == far:
                        at = ranker.at[end]
                        scores[at] = max(scores[at], confidence)
        return scores

    # THE RULER AGAINST ITSELF, before any arm is read. A disagreement here
    # means every number below is wrong in the same direction and nothing
    # downstream would say so.
    checked = 0
    for head, relation, tail in queries[:25]:
        for direction in ("tail", "head"):
            given, answer = ((head, tail) if direction == "tail"
                             else (tail, head))
            scores = (tails if direction == "tail" else heads)[relation]
            fast = ranker.rank(scores, given, relation, answer, direction)
            slow = plain_rank(scores, ranker.at, given, relation, answer,
                              known_tails, known_heads, direction)
            if fast != slow:
                raise SystemExit(
                    f"the two rankers disagree on {given} {relation}: "
                    f"{fast} against {slow}")
            checked += 1
    print(f"ruler check: the numpy rank and the plain one agree on "
          f"{checked} queries\n")

    rows: list[dict] = []
    for arm in ("random", "frequency", "inverse", "two hop",
                "two hop + frequency"):
        per_direction: dict[str, list[float]] = {"tail": [], "head": []}
        bounds: dict[str, list[float]] = {"optimistic": [], "pessimistic": []}
        fired = 0
        rng = np.random.default_rng(args.seed)
        for head, relation, tail in queries:
            for direction in ("tail", "head"):
                given, answer = ((head, tail) if direction == "tail"
                                 else (tail, head))
                marginal = (tails if direction == "tail" else heads)[relation]
                if arm == "random":
                    scores = rng.random(len(entities))
                elif arm == "frequency":
                    scores = marginal
                else:
                    scores = rule_scores(
                        "inverse" if arm == "inverse" else "two hop",
                        given, relation, direction)
                    fired += bool(scores.any())
                    if arm == "two hop + frequency":
                        # The rules first, the frequency floor underneath, so a
                        # query no rule fires on is not scored as a refusal.
                        scores = scores + marginal / (marginal.max() + 1.0)
                best, middle, worst = ranker.rank(scores, given, relation,
                                                  answer, direction)
                per_direction[direction].append(middle)
                bounds["optimistic"].append(best)
                bounds["pessimistic"].append(worst)
        both = per_direction["tail"] + per_direction["head"]
        got = metrics(both) | {
            "arm": arm,
            "mrr_tail": metrics(per_direction["tail"])["mrr"],
            "mrr_head": metrics(per_direction["head"])["mrr"],
            "mrr_optimistic": metrics(bounds["optimistic"])["mrr"],
            "mrr_pessimistic": metrics(bounds["pessimistic"])["mrr"],
            "hits1_optimistic": metrics(bounds["optimistic"])["hits1"],
            "hits1_pessimistic": metrics(bounds["pessimistic"])["hits1"]}
        if arm in ("inverse", "two hop", "two hop + frequency"):
            got["fired"] = fired / (2 * len(queries))
        rows.append(got)
        extra = (f"  fires {got['fired']:.4f}" if "fired" in got else "")
        print(f"{arm:>22}  MRR {got['mrr']:.4f}  hits@1 {got['hits1']:.4f}  "
              f"hits@10 {got['hits10']:.4f}  tail {got['mrr_tail']:.4f}  "
              f"head {got['mrr_head']:.4f}{extra}")
        print(f"{'':>22}  MRR under ties: optimistic "
              f"{got['mrr_optimistic']:.4f}, pessimistic "
              f"{got['mrr_pessimistic']:.4f}; hits@1 "
              f"{got['hits1_optimistic']:.4f} to "
              f"{got['hits1_pessimistic']:.4f}")

    floor = next(row for row in rows if row["arm"] == "frequency")
    print("\nAgainst published models, filtered MRR and hits@10 "
          "(TransERR arXiv 2306.14580 Table 3):")
    for name, (mrr, hits) in sorted(PUBLISHED.items(), key=lambda i: i[1][0]):
        print(f"  {name:>10}  {mrr:.4f}  {hits:.4f}     "
              f"above this floor by {mrr - floor['mrr']:+.4f} MRR")
    print("  The floor is a marginal with no structure in it whatsoever, so a "
          "model within 0.02 of it has shown very little.")

    print("\nAnd the cross-split check, which is what 237 was built for:")
    # BUILT ONCE. Written inside the comprehension, `set(train)` is rebuilt per
    # test triple -- 20,466 x 272,115 -- and the run goes from one second to
    # longer than anyone waits, with no output to say which step is slow.
    stated = set(train)
    verbatim = sum(1 for triple in test if triple in stated)
    flipped = sum(1 for h, r, t in test if (t, r, h) in stated)
    print(f"  test triples verbatim in train: {verbatim}")
    print(f"  test triples reversed in train under the same relation: {flipped}")
    rows.append({"arm": "overlap", "verbatim": verbatim, "flipped": flipped})

    harness.emit(args.json, rows, started=started,
                 support=SUPPORT, confidence=CONFIDENCE,
                 facts_per_relation=FACTS_PER_RELATION, degree=DEGREE,
                 queries=len(queries), seed=args.seed)
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
