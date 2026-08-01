# Now

What is being worked on, and what has been agreed but not started.

**The invariant that makes this worth having:** every 🚧 in
[README.md](README.md) appears here, and nothing appears here that is not in the
README. An approved piece of work cannot go quiet, which is how the LSH front end
was agreed and then dropped for two sessions — it sat as ⬜ in the tree, identical
to options nobody had ever considered, and the next planning pass never saw it.

Delete a line when it is done. This file is disposable and nothing may cite it.

---

## In flight

- **`fb15k237_walk.py` at 4,000 queries** → `out/fb15k237-walk.txt`. Partial
  rows so far agree with the smoke run (depth 2 beam 16 mean, −0.0418).
- **`fb15k237_typed.py` at 2,500 queries** → `out/fb15k237-typed.txt`. **This is
  the one to read first in the morning.** At 150 queries it produced the only
  positive margin of the night — `sum over paths`, blend weight 0.05 chosen on
  validation, test 0.2277 against the floor's 0.2170, **+0.0107**. That is 300
  scored queries and the margin is inside the noise at that size, and 0.05 was
  the smallest non-zero weight on the grid, so **it is not a result yet.** This
  run extends the grid down to 0.01 and raises n by 16x. If the margin holds at
  0.01-0.05 it is the first thing the project's own mechanism has done on
  external data; if it collapses, the night's answer is a clean four-way null.

## Next, in the order I would take them

1. **A walk that is told the relation types along the path.** Tonight's walk was
   deliberately untyped, and the ❌ it earned in README §4 names exactly this as
   its revival condition. The audit's rule miner already does typed two-hop
   paths — `r1(h, x) & r2(x, t) => r(h, t)` at 0.0460 — but it is a
   confidence-thresholded lookup rather than a ranked walk, so the combination
   of the two is untried and is the obvious next mechanism.
2. ~~Keep the route~~ — **done**. `grounding.routed` returns
   `(strength, route)` and `reach` is that with the routes dropped. Item 1 can
   now ask which edges a path used.
3. **An error signal** (README §7). Nothing is currently ever wrong, because
   counts only go up. Predicting *relations* rather than tokens gives a signal
   that can be wrong without being next-token prediction.
4. **Compression** (README §7). One principle that supplies forgetting,
   hierarchy, and a reason to reorganise — three holes that are currently three
   separate open questions.

## What tonight established

- **The structural signal is real and it is small.** Ranked on its own, with no
  marginal mixed in: untyped walk 0.0082, the audit's thresholded rule miner
  0.0460, typed ranked paths **0.1234**. Each mechanism roughly doubles the one
  before it, and all three sit below the 0.2334 a marginal reaches by ignoring
  the question entirely.
- **`sum` over paths beats `max` over paths** — 0.1234 against 0.0834 — which is
  the claim that separates a ranked walk from a thresholded lookup: many weak
  agreeing paths outrank one strong path. That is the first thing measured this
  week that came out the way the architecture says it should.
- **Fixed combiners were the wrong question.** `min` and `mean` both land below
  the floor, so they say more about the mix than the signal; the swept blend is
  what asks properly, and alpha 0 is the floor by construction.

- **The walk does not clear the marginal either, and it is not under-searched.**
  Best arm 0.2025 against a floor of 0.2290, over depths 1 to 3 and beams 4 to
  256. Walk-only peaks at **beam 16** and falls at 64 and 256 — an interior
  maximum, so a wider search finds more paths and ranks them worse rather than
  the grid stopping too early. Depth 1 walk-only returns 0.0001, reproducing the
  counted run's empty entity signal exactly, which is the check that the two
  runs measure the same quantity.
- **So being reachable is not being findable**, and that is the gap now open.
  0.7373 of answers are two steps away and the ranked walk puts them nowhere
  near the top, because two steps from an entity of average degree 37 is about
  1,300 candidates and nothing in an untyped walk says which of them the
  question was about.
- **Both re-pointed mutations go red.** `a-candidate-needs-only-ONE-half-behind-
  it` and `the-role-is-dropped-from-the-surface`, run once the audit had let go
  of the tree.

- **The count graph does not clear the marginal on FB15k-237, and the reason is
  structural rather than statistical.** Best combined arm 0.1707 against a floor
  of 0.2186 by the same mechanism with a half switched off. The entity half
  scores 0.0001 alone, because a link-prediction query asks for an edge that is
  not in training, so the answer never co-occurred with the question. Adding a
  near-empty signal to a working one makes it worse, which is what the negative
  margin is.
- **That is README §4's revival condition, met and measured.** *Walk further
  than one step* was ❌ with *"revives if a question needs two hops by
  construction"*. It does, here, by construction. The line is now 🚧.
- **The floor and the mechanism are one implementation.** `Composition.given`
  ranks any role from whichever roles the query supplies, so *relation only* is
  the marginal, *both* is the arm, and neither can drift from the other.

## Reading leads, none of them read

Found by search on 2026-08-01, recorded as leads and **not as findings**. Each
may replace work otherwise done by hand, and each has to be read before it is
cited anywhere.

- **Generalized Rank-based Evaluation for KGC** (`arXiv 2606.08921`, 2026).
  Reportedly proposes *popularity-bias robustness* as an evaluation axis, which
  is what the 0.2334 marginal floor measures. **Read this first** — if the metric
  exists, use theirs rather than inventing margin-over-marginal.
- **A Re-evaluation of Knowledge Graph Completion Methods**, Sun et al. ACL 2020
  (`aclanthology.org/2020.acl-main.489`). Reportedly the tie problem and the
  average-rank fix, which is the policy `fb15k237_audit.py` chose independently.
- **Akrami et al., realistic re-evaluation of KGC.** Reportedly finds redundancy
  and test leakage inflating accuracy by 19-175% across standard benchmarks —
  this week's CLUTRR result at family scale, possibly naming which datasets leak.
- **SCAN, COGS, CFQ** for kill-list #1's instrument problem: splits made by
  STRUCTURE rather than by sampling. Audit any of them with the table attack
  before adopting one; the filter is whether the ceiling is computable.

## Known debts

- **Kill-list #1 has an instrument with a floor rather than a ceiling.** CLUTRR
  is dead twice over (`clutrr_ceiling.py`, `clutrr_headroom.py`). FB15k-237
  passed its audit: the inverse leak is gone — 0.45 applied to train against
  0.0001 applied to test — but relation-tail frequency alone scores MRR 0.2334,
  against published DistMult 0.241 and ComplEx 0.247. **Report the margin.**
- **The protocol question is closed.** RotatE (ICLR 2019) evaluates filtered
  against train, validation and test, corrupting subjects *or* objects — which
  is what the audit does. Its Table 5 gives DistMult Hits@1 0.155 and ComplEx
  0.158 against our floor's 0.1700, so the marginal beats both there and loses
  at Hits@10.
- **There is no node entry point.** `node_main.py` started the old store and was
  deliberately not carried over.
- **`asking.py`'s two new mutations have not been RUN.** `tests/test_asking.py`
  now covers it — the refusal being one draw, the budget being charged, a
  refusal not being a miss, and watching reproducing `occasions.generate`
  occasion for occasion — and `the-ask-retries-until-the-world-says-yes` and
  `an-ask-is-not-charged-for-what-it-drew` are registered. **Neither has been
  seen to go red**, because the FB15k runs were touching the tree all night and
  the harness may not run alongside them. **`the-route-reported-is-not-the-
  route-walked` is in the same state.** First thing to clear in the morning,
  once the sweeps have stopped:

      python tools/mutate.py --only the-ask-retries-until-the-world-says-yes,an-ask-is-not-charged-for-what-it-drew,the-route-reported-is-not-the-route-walked
- **`experiments/` has six scripts and no harness.** They now share `Ranker` and
  `load` through `experiments/__init__.py`, which is the first step away from
  each script carrying its own copy, but argument parsing and JSON writing are
  still duplicated six ways.
- **The link columns in `surfaces_pipeline.py` step in tenths.** Shares over ten
  words, so nothing smaller than 0.1 can be read.
- **The front end is not wired to anything that runs as a node**, because there
  is no node entry point. It is used by the sweeps and nowhere else.
