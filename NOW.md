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

- **The full `fb15k237_audit.py` re-run is going, in the background.** It adds
  the tie-policy bound and nothing else; the arms it has already printed match
  what is committed (floor 0.2334, and the bound is 0.2305 to 0.2597). When it
  lands, check nothing moved and refresh `out/`.
- **The mutation harness has not been run since `composition.py` was
  generalised.** Two mutations were re-pointed — `the-role-is-dropped-from-the-
  surface` and `a-candidate-needs-only-ONE-half-behind-it`, the second caught by
  `--verify` rather than by anyone remembering — and both need a run to confirm
  they still go red. **It could not run tonight because the audit was touching
  the tree**, which is `CLAUDE.md`'s rule and not an oversight.

## Next, in the order I would take them

1. **The walk on FB15k-237**, and it is now the obvious next thing rather than a
   preference. Tonight's counted arm scored **below** the marginal — margin
   −0.0480 — and the reason is measured: the two endpoints of a test triple are
   **0.0000 one hop apart in training and 0.7373 two hops apart**. A one-step
   mechanism cannot reach the answer whatever statistic it uses. `grounding.reach`
   is built, has a beam and a depth, multiplies path strength along the route,
   and **has never been run on external data**. Report the margin over the
   0.2334 floor, never the MRR.
2. **Keep the route, not just the endpoint.** `reach` returns each surface it
   reached and the best path strength to it, and throws the path away. If a
   concept is a traversal then the route is the object — and for a
   link-prediction answer the route is also the explanation. Small change,
   directly on the project's own claim.
3. **An error signal** (README §7). Nothing is currently ever wrong, because
   counts only go up. Predicting *relations* rather than tokens gives a signal
   that can be wrong without being next-token prediction.
4. **Compression** (README §7). One principle that supplies forgetting,
   hierarchy, and a reason to reorganise — three holes that are currently three
   separate open questions.

## What tonight established

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
- **`openplexus/tasks/asking.py` has no tests and no mutation**, so by this
  project's own rules it is not finished.
- **`experiments/` has six scripts and no harness.** They now share `Ranker` and
  `load` through `experiments/__init__.py`, which is the first step away from
  each script carrying its own copy, but argument parsing and JSON writing are
  still duplicated six ways.
- **The link columns in `surfaces_pipeline.py` step in tenths.** Shares over ten
  words, so nothing smaller than 0.1 can be read.
- **The front end is not wired to anything that runs as a node**, because there
  is no node entry point. It is used by the sweeps and nowhere else.
