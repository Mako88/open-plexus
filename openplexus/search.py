"""Try a branch, see where it lands, keep the one that reaches the target.

## The capability this adds, and why it is not a defect being patched

Decision 108 stated it as sharply as this project has stated anything:
**multi-hop reasoning over a BRANCHING graph requires SEARCH -- try a branch, see
where it lands, backtrack -- and an associative store does RETRIEVAL. Nothing in
this architecture searches.** That is a missing capability, not a bug.

The chain task hid it completely, because a chain has out-degree 1 by
construction and every composition result on chains was measured where no search
was needed.

## Why now, when decision 111 refused it

111 measured search's ceiling before building it and declined: *"you cannot
search your way out of noisy primitives, because the verifier is built from the
primitives."* To check that a branch reaches the target you must RETRIEVE its
endpoint, and that retrieval was as unreliable as the one that made the branch.

**That refusal was conditional and the condition has expired.**

    g13-01   step 1 at out-degree 1   1.000 +/-0.000, 8 seeds
    g13-02   step 2 at a unique pair  1.000, and 0.971 overall
    g13-02   traversal-with-search ceiling   1.000 against the 0.87 that
                                             justified building it

The asymmetry is what makes it work. A `(subject, relation)` pair names one
person **94.9%** of the time; `(FACT, subject)` names one of several relations
about half the time. So the traversal's weak steps are its two ENDS and its
MIDDLE is sound -- and the verifier is built out of the middle.

## What the search is

The question names the object. That is the disambiguator, and nothing before this
used it: decision 108 found the store answering *"what relation does S hold"*
correctly while the question needs *"which of S's relations leads to T"*.

    1  read key(FACT, S)          -> a superposition of S's relations
    2  take the top `branches` of that decode as CANDIDATES
    3  for each candidate, walk the graph committing to it
    4  score each walk by how well its endpoint matches T's value vector
    5  return the walk that scores best

**A branch commits to a hard token.** Everywhere else in this model a decode is
softened and blended -- `hop_sharpness` exists for exactly that -- because a hard
decode gives the next step no gradient of confidence. Search is the one place the
opposite is right: the whole point is to assert a candidate, follow the
consequences, and find out. Hedging would reproduce the superposition search
exists to escape.

## Locality

**Every step is a token id and a dot product.** `PairKeys.pair` rebuilds a key
from two ids rather than looking one up, which is the property `derived_keys`
rests on -- a node is handed two integers and reconstructs the vector itself.

So a branch costs no new *kind* of communication. It costs MORE of the one the
readout already makes: the decode pools scores across groups, exactly as
`parts.sum(0)` does, and a search over `b` branches at depth `d` makes that
pooling happen `b * (2d - 1)` times instead of `d` times.

**That is bounded bytes per hop with no barrier, so amended C1 is satisfied** --
nothing here stalls when a participant is slow or gone. It is a traffic
multiplier and not a synchronisation one, and the multiplier is real: it is
`tools/search_cost.py`'s subject and it must be costed before this is called
affordable.

## NOT YET WIRED INTO THE MODEL

This module is tested on its own and `run()` does not call it. That is
deliberate: `run()` is 526 lines with 46 branch points and BACKLOG's
simplification item is about exactly that, so the mechanism is built and proved
here first and wired in as its own change. **Labelled as unfinished rather than
left to look finished** -- scaffolding that is not named as scaffolding becomes
load-bearing.
"""

from __future__ import annotations

from dataclasses import dataclass

import numpy as np


@dataclass(frozen=True)
class Walk:
    """One candidate branch, followed to its end.

    Attributes:
        relations: Token ids of the relations committed to, in order. Length is
            the requested depth.
        entities: Token ids of the entities passed through, after the start.
        retrieved: The retrieved VALUE vector at each relation step, which is
            what a readout consumes. Parallel to `relations`.
        endpoint: The value vector the walk finished on, before scoring.
        score: How well `endpoint` matches the target. Higher is better.
    """

    relations: tuple[int, ...]
    entities: tuple[int, ...]
    retrieved: tuple[np.ndarray, ...]
    endpoint: np.ndarray
    score: float


def _decode(wv: np.ndarray, value: np.ndarray) -> np.ndarray:
    """Scores over tokens for a retrieved value vector.

    `wv` encodes token to value, so its transpose decodes value to token. Frozen,
    so nothing can drag it off that job -- the same argument `hop_decoder
    ="encoder"` rests on, and the reason it is preferred over reusing `Wo`, whose
    gradient pulls it toward emitting the FINAL token exactly where a hop needs
    the intermediate one.
    """
    return wv @ value


def _top(scores: np.ndarray, count: int,
         allowed: np.ndarray | None) -> list[int]:
    """The `count` highest-scoring token ids, best first.

    `allowed` restricts the candidates to a set the caller knows is legal -- the
    relation tokens, say. It is **not** required, and passing None searches the
    whole vocabulary. Whether restricting it is scaffolding or a real property of
    a deployed system depends on the task, so the choice is the caller's and is
    made visible rather than baked in here.
    """
    if allowed is not None:
        masked = np.full_like(scores, -np.inf)
        masked[allowed] = scores[allowed]
        scores = masked
    count = min(count, int(np.isfinite(scores).sum()))
    if count <= 0:
        return []
    return [int(i) for i in np.argsort(scores)[::-1][:count]]


def walk_from(readable: np.ndarray, retrieval, keys, wv: np.ndarray,
              fact_token: int, start: int, first_relation: int,
              depth: int) -> Walk:
    """Follow one committed branch `depth` relations deep.

    The walk alternates the two operations decision 107 measured separately:

        key(entity, relation) -> the next entity      step 2, 1.000 at a unique pair
        key(FACT, entity)     -> that entity's relation   steps 1 and 3

    It commits to `first_relation` rather than decoding it, which is what makes
    this a branch rather than another retrieval.

    Returns the walk with its endpoint. **Scoring is the caller's job** -- a walk
    does not know what it was looking for, and keeping the two apart is what lets
    the same walk be scored against different targets.
    """
    if depth < 1:
        raise ValueError("a walk of depth 0 has nothing to follow")

    relations = [first_relation]
    entities: list[int] = []
    retrieved: list[np.ndarray] = []
    current = start

    # The first relation was handed in, so its retrieval is the one that
    # produced the candidates. Re-read it here rather than threading it through,
    # so a Walk is reproducible from its own inputs.
    retrieved.append(retrieval.read(readable, keys.pair(fact_token, current)))

    for _ in range(depth - 1):
        # FOLLOW: commit to the relation and find who it reaches.
        nxt = retrieval.read(readable, keys.pair(current, relations[-1]))
        current = int(np.argmax(_decode(wv, nxt)))
        entities.append(current)
        # LOOK UP: that entity's own relation.
        value = retrieval.read(readable, keys.pair(fact_token, current))
        relations.append(int(np.argmax(_decode(wv, value))))
        retrieved.append(value)

    # The endpoint is where the LAST relation lands, and it is what the target
    # is compared against.
    endpoint = retrieval.read(readable, keys.pair(current, relations[-1]))
    return Walk(tuple(relations), tuple(entities), tuple(retrieved),
                endpoint, 0.0)


def candidates(readable: np.ndarray, retrieval, keys, wv: np.ndarray,
               fact_token: int, start: int, branches: int,
               allowed: np.ndarray | None = None) -> list[tuple[int, float]]:
    """The branches worth trying from `start`, with their decode scores.

    Split out of `search` so **the decision to search can be taken without
    searching.** g13-03 measured search gaining +0.092 where the queried subject
    holds several relations and losing 0.054 where it holds one, so the two
    nearly cancel — the mechanism is right and running it unconditionally is
    not. A gate needs this decode, and only this decode, before committing to
    the walks.

    Returned with scores rather than as bare tokens because **the margin between
    the top two is the quantity a gate would read**: a subject with one relation
    should show a wide gap, and one with several a narrow one. Whether that
    holds is measured, not assumed — decision 93 found every identity-free
    confidence signal reaching 0.628 against 0.500 for guessing, so a signal
    that merely sounds plausible has a poor record here.
    """
    first = retrieval.read(readable, keys.pair(fact_token, start))
    scores = _decode(wv, first)
    return [(token, float(scores[token]))
            for token in _top(scores, branches, allowed)]


def decode_margin(scored: list[tuple[int, float]]) -> float:
    """Gap between the best and second-best candidate. Zero if fewer than two.

    Zero rather than infinity for a single candidate: a gate reading this
    decides "ambiguous" on a small margin, and a lone candidate is the case
    where searching is *least* useful. Returning a large number would gate it
    exactly backwards — which is the failure decision 87 records this project
    shipping once already, a gate whose sign was wrong that beat the baseline
    anyway.
    """
    if len(scored) < 2:
        return 0.0
    return scored[0][1] - scored[1][1]


def search(readable: np.ndarray, retrieval, keys, wv: np.ndarray,
           fact_token: int, start: int, target: np.ndarray, depth: int,
           branches: int = 4,
           allowed: np.ndarray | None = None) -> list[Walk]:
    """Every candidate branch from `start`, scored by whether it reaches `target`.

    Returns the walks sorted best first. The caller decides what to do with them
    -- taking the best is the obvious use, but the whole list is returned because
    the MARGIN between the top two is the only signal available about whether the
    search was decisive, and a mechanism that discards it cannot report its own
    confidence.

    Args:
        readable: The store to read, already including any lasting part.
        retrieval: The model's own `Retrieval`, so this reads through the same
            path everything else does rather than reimplementing it.
        keys: A `PairKeys`. Single-token keys cannot express `key(S, R)` at all,
            which is decision 105's finding and the reason this needs pairs.
        wv: The frozen value projection, used to decode.
        fact_token: The marker that precedes every fact, making `key(FACT, X)`
            mean "X in subject role".
        start: The entity the question asks about.
        target: The value vector of the entity the question names as the far end.
            **This is the disambiguator the store was never given** -- decision
            108 found it answering "what relation does S hold" when the question
            needs "which of S's relations leads to T".
        depth: How many relations to compose.
        branches: How many candidates to try. `1` is a plain greedy traversal and
            is the control that says whether searching bought anything.
        allowed: Optional restriction on candidate tokens.

    Raises:
        ValueError: If `keys` cannot build a pair key, which would otherwise
            silently search a key space nothing was ever written to -- the exact
            failure decision 105 recorded, where the combination "still returned
            answers and accuracies".
    """
    if not hasattr(keys, "pair"):
        raise ValueError(
            "search needs PairKeys: a walk keys on (entity, relation), and a "
            "single-token table has no such key. Decision 105 measured the "
            "cosine between the two key spaces at -0.069 and the model still "
            "returned numbers")
    if branches < 1:
        raise ValueError("branches must be at least 1")

    scored = candidates(readable, retrieval, keys, wv, fact_token, start,
                        branches, allowed)

    walks = []
    for candidate, _ in scored:
        walk = walk_from(readable, retrieval, keys, wv, fact_token, start,
                         candidate, depth)
        # SCORED BY THE ENDPOINT, not by confidence anywhere along the way.
        # Decision 93 measured every identity-free confidence signal -- norm,
        # entropy, peak, gap, kurtosis -- and the best linear separator over all
        # five, FITTED WITH THE LABELS, reached 0.628 against 0.500 for guessing.
        # Matching the named target is a different kind of signal entirely: it
        # asks whether the walk arrived, not whether it felt sure.
        score = float(walk.endpoint @ target)
        walks.append(Walk(walk.relations, walk.entities, walk.retrieved,
                          walk.endpoint, score))

    walks.sort(key=lambda w: w.score, reverse=True)
    return walks


def margin(walks: list[Walk]) -> float:
    """How decisively the best walk beat the runner-up.

    Zero when fewer than two walks were tried, which is the honest answer: a
    search with one branch has no margin, and reporting a large one would make a
    greedy traversal look confident.
    """
    if len(walks) < 2:
        return 0.0
    return walks[0].score - walks[1].score
