# Now

What is being worked on, and what has been agreed but not started.

**The invariant that makes this worth having:** every 🚧 in
[README.md](README.md) appears here, and nothing appears here that is not in the
README. An approved piece of work cannot go quiet, which is how the LSH front end
was agreed and then dropped for two sessions — it sat as ⬜ in the tree, identical
to options nobody had ever considered, and the next planning pass never saw it.

Delete a line when it is done. This file is disposable and nothing may cite it.

---

## The result, and it is the first one

**A typed ranked walk clears the floor by the same margin ComplEx does, with no
training and no embedding.** `sum over paths`, blend weight 0.01 chosen on
10,233 validation triples, read on all 20,466 test triples in both directions:

    floor (relation only)   0.2334
    the arm                 0.2470
    margin                 +0.0136  +/- 0.0005, so about 27 standard errors

    published, same floor:  ComplEx +0.0136   DistMult +0.0076

**And it is not the marginal being reinforced**, which was the live worry. Split
by how many training triples the answer entity has:

    rare (<10)         n= 1,703   0.0166 -> 0.0469   +0.0303
    middling (10-49)   n=17,330   0.0548 -> 0.0684   +0.0137
    common (50+)       n=21,899   0.3917 -> 0.4039   +0.0122

The gain is LARGEST where the answer is rare, nearly tripling the floor there,
and smallest where it is common. The 120-query preview said the opposite and had
seven queries in that band.

**What is still true and must travel with the number:** 7,375 queries improved
and 11,302 got worse. The mechanism wins a minority of queries by a lot and
loses the majority by a little, at every sample size from 120 to 40,932, so that
is a property of it rather than noise. Most of the 0.2470 is the marginal; the
structure contributes the 0.0136.

**Not yet reproduced.** A second seed is running (`out/fb15k237-typed-seed1.txt`).
The only seed-dependent step is which 10,233 validation triples choose the blend
weight — the test set is scored whole and the fan-out cap is deterministic — and
the alpha curve is broad enough that the choice should barely matter (0.01 gives
0.2470, 0.02 gives 0.2459, 0.05 still gives 0.2406). **If seed 1 disagrees, the
README line goes back to ⬜ and the disagreement is the finding.**

## In flight

- **`fb15k237_walk.py` at 4,000 queries was STOPPED before its last two cells**,
  and the hole is named rather than left to be discovered: `depth 3 beam 64` ran
  `walk only` (0.0065) and never ran `min` or `mean`. It was stopped because its
  conclusion was already established — every arm below the floor, and walk-only
  peaking at beam 16 then declining at 64 and 256 at every depth, which is the
  interior maximum that rules out the grid being too small — and because those
  two cells are about fifty minutes of a machine the decisive run needs.
  `out/fb15k237-walk.txt` has everything up to that point. **If the depth-3
  beam-64 combined cells ever matter, they have not been run.**
- **`fb15k237_typed.py` on the FULL test set, with the popularity bands**
  → `out/fb15k237-typed-full.txt`. **Read this first.** An earlier full run was
  stopped and relaunched rather than left to finish: it predated the
  stratification, and the bands are now the question that decides the result, so
  it would have had to be run again regardless. The 2,500-query run held the margin and sharpened it:
  `sum over paths`, alpha 0.02 chosen on validation, test **0.2409** against the
  floor's 0.2286, **margin +0.0124** over 5,000 scored queries. The alpha curve
  now has an interior maximum rather than an edge winner — 0.0:0.2286,
  0.01:0.2410, 0.02:0.2409, 0.05:0.2383, 0.1:0.2267, 1.0:0.1328.

  **First full-scale arm is in, and it holds.** `max over paths`, alpha 0.01
  chosen on 10,233 validation triples: test 0.2381 against the floor's 0.2334,
  **margin +0.0047 with a standard error of 0.0004** over 40,932 scored
  queries — about twelve standard errors, so the margin is real and it is
  small. **7,246 queries better against 11,114 worse**, so it remains a
  minority of large wins funding a majority of small losses. `sum over paths`,
  which was the stronger arm at 2,500 queries, and the popularity bands are
  still running.

  **Two things this full run fixes, and until it lands the margin is not
  quotable.** The 2,500-query floor is 0.2286 where the full-set floor is
  0.2334, so a margin measured against the small floor and compared with a
  published full-set number flatters us by about 0.005. And a difference of two
  means needs a paired error bar rather than an eyeball; the run now reports the
  per-query gain, its standard error, and how many queries got better against
  how many got worse.

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

- **A typed ranked walk blended with the marginal is the first thing this
  project's own mechanism has done above a floor on external data**, and the
  size of it is +0.0124 MRR at 2,500 queries. For scale, DistMult's margin over
  the full-set floor is +0.0076 and ComplEx's is +0.0136. **That is the whole
  claim** — not that the count graph is competitive, but that it clears a
  no-structure baseline by an amount in the same range as two published models
  clear it by, having been given no training and no embedding.
- **AND THE CLAIM IS ALREADY IN TROUBLE, from its own diagnostics.** At 120
  queries the paired gain is +0.0068 with a standard error of 0.0062, and
  **40 queries improved against 69 that got worse** — so the positive mean is a
  few large wins paid for by many small losses, which an MRR difference hides
  completely. Worse, the gain is concentrated where the answer is ALREADY
  COMMON: −0.0081 on answers with fewer than ten training triples, +0.0096 on
  answers with fifty or more. The floor is a popularity ranking, so a gain that
  lives on popular answers may be the marginal being reinforced rather than
  structure being added. **The full run decides it, and if that pattern holds at
  scale the honest reading is that the margin is a popularity artefact.**
- **`sum` over paths beats `max` at every alpha**, which is the ranked walk
  earning its keep over a thresholded lookup.

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

- **PROBE** (`arXiv 2606.08921`, 2026) — *fetched, not read.* It reweights the
  metric by inverse popularity rather than comparing against a baseline:
  per-triple weights from entity degree and entity-conditioned relation
  frequency, with a `beta` setting how hard low-popularity triples are
  upweighted, and a separate `alpha` setting rank sharpness. **Its smoothing
  constants were not in what was fetched**, so it is deliberately NOT
  reimplemented — a metric named after a paper nobody opened is the borrowed
  claim `CLAUDE.md` puts first. The popularity stratification in
  `fb15k237_typed.py` takes the idea and needs no constants. Reading the paper
  properly would let the weighted version be reported as PROBE.
- **A Re-evaluation of Knowledge Graph Completion Methods**, Sun et al. ACL 2020
  (`aclanthology.org/2020.acl-main.489`, `arXiv 1911.03903`). Reportedly the tie
  problem and the average-rank fix, which is the policy `fb15k237_audit.py`
  chose independently. **Two fetches got the abstract only** — the anthology
  landing page and the arXiv abstract page — and the abstract says just
  *"inappropriate evaluation protocol"* and *"a simple evaluation protocol...
  robust to handle bias in the model"* without naming ties at all. **The PDF is
  what has to be read**, and WebFetch returns it as unparsed binary, so this
  needs a human or a different tool. Until then the tie policy stays our own
  documented choice rather than a cited convention — which is fine, because the
  tie bound shows the comparison's direction does not depend on it.
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
- **And the floor's tie policy does not decide the comparison, only its size.**
  Published models have continuous scores and almost no ties; ours puts
  thousands of entities on exactly zero, so the floor runs 0.2305 to 0.2597.
  Each published margin over it:

        model      pessimistic    average   optimistic
        DistMult      +0.0105     +0.0076      -0.0187
        ComplEx       +0.0165     +0.0136      -0.0127
        TransE        +0.0635     +0.0606      +0.0343
        RotatE        +0.1075     +0.1046      +0.0783

  The only sign change is DistMult and ComplEx going NEGATIVE under the reading
  most generous to the floor, which makes the finding stronger rather than
  weaker. The average is what is quoted because it is the neutral choice; the
  claim that it is also the published convention is unread and is not relied on.
- **And the two floors agree.** `fb15k237_audit.py` builds it as a raw count
  vector and `Composition.given` builds it as a relation-only ranking, written
  separately — both return **0.2334** over all 40,932 scored queries.
- **There is no node entry point.** `node_main.py` started the old store and was
  deliberately not carried over.
- ~~`asking.py`'s mutations and the route mutation are unrun~~ — **all three ran
  and all three were caught**, and `--verify` reports the source clean at 50/50.
  Run alongside the typed sweep after all: the rule's stated hazard is a commit
  landing mid-run, which is controllable, and the sweep holds its modules in
  memory and writes nothing to the tree. No commit was made until the harness
  had finished and verified.

  *(Superseded, kept one turn so the change of mind is visible:)* **the two new
  mutations have not been RUN.** `tests/test_asking.py`
  now covers it — the refusal being one draw, the budget being charged, a
  refusal not being a miss, and watching reproducing `occasions.generate`
  occasion for occasion — and `the-ask-retries-until-the-world-says-yes` and
  `an-ask-is-not-charged-for-what-it-drew` are registered. **Neither has been
  seen to go red**, because the FB15k runs were touching the tree all night and
  the harness may not run alongside them. **`the-route-reported-is-not-the-
  route-walked` is in the same state.** First thing to clear in the morning,
  once the sweeps have stopped:

      python tools/mutate.py --only the-ask-retries-until-the-world-says-yes,an-ask-is-not-charged-for-what-it-drew,the-route-reported-is-not-the-route-walked
- **`experiments/` has eight scripts and no harness.** They share `Ranker`,
  `Marginal` and `load` through `experiments/__init__.py`, but argument parsing
  and JSON writing are still duplicated eight ways. `tools/check_experiments.py`
  now at least starts each of them, in preflight and in CI.
- **The link columns in `surfaces_pipeline.py` step in tenths.** Shares over ten
  words, so nothing smaller than 0.1 can be read.
- **The front end is not wired to anything that runs as a node**, because there
  is no node entry point. It is used by the sweeps and nowhere else.
