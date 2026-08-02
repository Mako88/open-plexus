# Now

What is being worked on, and what has been agreed but not started.

**The invariant:** every 🚧 in [README.md](README.md) appears here, and nothing
appears here that is not in the README. An approved piece of work cannot go
quiet, which is how the LSH front end was agreed and then dropped for two
sessions.

**A finding updates a line; it never appends one.** And a finding SPLITS: the
claim goes to the README, the numbers stay with the run in `out/`. "Settled
results belong in the README" was the wording here and it is wrong — it invites
whole tables into prose, which is what `CLAUDE.md` refuses and what the record
check keeps catching. This file carries only what is unfinished. Delete a line
when it is done. Nothing may cite it. Rewritten at the end of every turn — see
`.claude/skills/monitor`.

---

**UNDERPOWERED**: senses runs used n=150 where detecting +0.03 needs 840. The
seed spreads reported all session, 0.038 to 0.330, are the signature. Nothing
has been re-run.

## THE POWERED RE-RUN — `out/powered-rerun.txt`

Six runs at 1,000 questions across both front ends and three seeds, because an
audit found **three refutations resting on 120-150-question runs** where +0.03
needs 840: the flood as a cross-modal walk, surprise/rarity refuelling, and
many-origins. All three are the broadcast line.

`lsh` seeds 0 and 1 are in and null, as the front-end finding predicts. **The
`kmeans` half is where the question is answered and has not run.** Fix
`flood-many`'s budget first — it gives up on 0.999 of questions.

## REPORT `busiest`, NOT `messages` — the design's own column

Nothing here runs in parallel: `broadcast.flood` is a single-threaded loop and
every node is a dict entry. So the seconds a run takes are TOTAL work, and the
number the design is about is `Flood.busiest()`. Measured in the run in flight:
199,062 expansions per question against a busiest node's 13,920 — **14.3× —
so 411s of total work is about 29s of distributed wall clock.** Every cost
reported this session led with the wrong column.

## AGREED 2026-08-02, NOT BUILT — start here

1. **BUILT — snake's energy bar.** See the README; `SELECTING_ENERGY = 200` is
   measured, not chosen. What is left is USING it: no experiment runs with
   energy on, so no policy has yet been selected against.

2. **Re-run the three broadcast refutations at 1,000 questions.** In flight;
   see below.

3. **Split the anchor as well as the codebooks.** The disagreement result had
   one clean word per digit shared by both codebooks by construction; a real
   federation has none. This decides whether k-means is genuinely legal.

4. **Many snake games into one graph.** John's idea. The win is not
   parallelism — N games is close to one game N times longer — it is EVIDENCE:
   N× the occasions per code, which is the axis that made the senses graph
   unwalkable. It also tests interleaved independent sources writing to one
   graph, which is what a federation is. **`Flood` has no broadcast id**, so
   two thoughts in flight would mix their routes and their death counts; that
   is the Dijkstra-Scholten bookkeeping needing a per-thought identifier and it
   is not there.

5. **Random centres from a shared seed**, as a legal middle between the hash
   and a fitted codebook. A seed fixes where k-means STARTS, not where it ends,
   so seeded fitting does not make two nodes agree — but centres drawn from a
   shared seed and never moved are data-free and agree exactly, partitioning by
   Voronoi cell rather than half-space. Untried. Honest caveat: random centres
   do not know where the data is either.

6. **THE FRONT END SHOULD BE AS WEAK AS POSSIBLE.** John's observation, and
   already the project's own argument in `surfaces.py`: *clustering by
   similarity is an identity assignment, and identity is the walk's job.* So
   "k-means walks better than the hash" may mean **the walk is not doing its
   job** — a good clusterer answers the question upstream where nothing audits
   it. The front end's only mandate is decision 1's ❌ for no-discretisation:
   make recurrence possible. **The ensemble is the right shape for this
   reason.** Reread "the front end is the bottleneck" against it.

   **John's counter, accepted:** with the same front end on BOTH arms, a walk
   beating random is attributable to the walk. **Relative claims are safe under
   any quantiser; absolute ones are not.**

## Waiting on John

- **Which chain to render** — agreed in principle (arrival narrows, prediction
  ranks, brevity breaks ties), nothing built. The prediction half now exists.
- **Whether ARC-AGI-3 is next** — the objection stands: counting needs
  recurrence and ARC withholds it by design.

**Preference**: three honest sources exist; reward is rejected and curiosity
loses to random, so homeostasis is the one left. Action 1 builds it.

**Ensemble front end**: built; budget pins at the TOP of its grid. Sweep higher,
then three seeds at whatever interior maximum appears.

**`--repeats` may do nothing**: it replays the same recordings, and repeated
identical evidence does not sharpen a ratio. Unmeasured.

**Columns**: neighbour-conditioned works. Left: conditioning on a SUMMARY of
the neighbours rather than the single one in the direction of travel.

**Snake**: no multimodality yet — vision, plus hearing in `snake_hearing.py`;
action and interoception designed, not built. Nothing beats random play.

## Prediction and forgetting: four holes

- **Prediction error does not drive the asking.** Still a fixed fraction.
- **Automatic dial tuning** is designed, not built: a node scoring candidates
  for its own dial against its own prediction error, local and never-ending.
  **Risk**: minimising surprise is won by never looking at anything
  surprising, so score error per observation MADE.
- **Nothing has swept the half-life**, now known to change answers.
- **Eviction has no policy** — nothing measures memory pressure.

## Forgetting: built, one half untested

`CoOccurrence(half_life=...)`, off by default.

- **Nothing has swept the half-life**, which is now known to change answers
  and not only memory, so it needs sweeping like any other dial.
- **Eviction has no policy.** `weakest` ranks by what is left; nothing decides
  when to call it, because nothing measures memory pressure.

## Decided

- **No tokenizer**; **facts are dropped**, not islanded; **no pre-commit hook**.
- **Mutations run in CI, sharded six ways.** Locally use
  `--only <the ones just added>`; `--changed` let a live mutation reach a commit.

## Known debts

- **`deployment.py`, `agreement.py`, `tasks/xsl.py` are dead** — tests only.
- **DISTRIBUTED: entry point and in-process agreement done, container left.**
  `node_main.py` runs a node on TCP. Left: latency, departure, partition.
- **`experiments/` has fifteen scripts and no harness**; the link columns in
  `surfaces_pipeline.py` step in tenths.
- **§5's "refuse when nothing was written — the machinery exists" is
  unverified.** Every refusal in the package is an ownership refusal instead.
- **The written channel's dials were chosen, not measured**, and nothing has
  swept `--silence/--mistake/--corrupt`.

## Reading leads, unread

**Predictive coding** (the dark room is its named risk, measured);
**interventional causal discovery under a budget**; **AnyBURL**, near 0.31
where ours lands at 0.247.
