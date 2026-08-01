# Now

What is being worked on, and what has been agreed but not started.

**The invariant:** every 🚧 in [README.md](README.md) appears here, and nothing
appears here that is not in the README. An approved piece of work cannot go
quiet, which is how the LSH front end was agreed and then dropped for two
sessions.

**A finding updates a line; it never appends one.** Settled results belong in the
README, which carries the claim; this file carries only what is unfinished.
Delete a line when it is done. Nothing may cite this file.

---

## The flood: what is left of it

Settled and in the commit record: meaning-gating beats strength-gating by about
two to one, and the flood does NOT beat the flat enumeration (+0.0081 against
+0.0136 at 300 queries). A +0.0164 reported at sixty queries was withdrawn.

**What is unfinished.** A route that composes confidently barely decays, so the
floor kills what means nothing and does not bound what means something. The
floor and a beam do different jobs and `flood` has only the floor.

**And EXPANSIONS IS THE WRONG COST COLUMN here.** John's point: where every node
expands its own edges in parallel, wall clock is the longest path, not the sum
of all work. The costs that transfer are MESSAGES SENT and WORK PER NODE and
neither is measured.

## g44-01 ran: the mechanism works, both policies were starved

**Intervention separates a confound, in both directions.** True partners are
refused 0.3837 of the time against the shadow's 0.2222, so the confound is
demoted harder; and the control inverts it, refusing the shadow 0.7326 against
0.3917 when the shadow genuinely cannot be had alone. That check exists because
separation is a DIFFERENCE and the demotion MULTIPLIES, so uniform shrinkage
would have moved the number for free — it nearly made "recovers 83% of the gap"
a statement about arithmetic.

**P1–P3 stay refuted, and the reason is coverage.** Neither policy asked the
questions the metric reads: `ask-random` buys 504 pairs to land 18 of 108 on
target, `ask-targeted` is pinned at 1 at every budget, and the ceiling with
108/108 reaches −0.0500 against watching's −0.2967.

**Why targeting fails is the interesting part.** `occasions.py` holds the noise
and distractor surfaces present in EVERY occasion, so `conditional(background |
anything)` is 1.0, the largest the statistic can take, and a policy that asks
about its highest-scoring partner is pulled to the background on every draw. It
is the confound failure happening to the confound detector.

**A policy CAN find them, and the metric still gets worse.** `ask-mutual`
nominates by `min(P(c|q), P(q|c))`, which the background cannot fake because it
predicts nothing in reverse. It lands 53 of 108 against targeting's 1, 47.8% of
its asks are shadow pairs — P5 and P7 held — and separation falls to −0.5130.

**Splitting the ceiling by what it may demote says why, and it is good news:**

    shadows only        +0.2042      beats the confound outright
    true partners only  −0.5509
    both                −0.0500      watching −0.2967

**`adjusted` is the bug.** It multiplies by the raw refusal rate, but a true
surface at `presence` 0.7 is genuinely detachable — refused 0.3837 against a
shadow's 0.2222. Being detachable is not being no part of it; only the
comparison between candidates carries signal. Control holds: at
`shadow_alone` 0.0 the same shadows-only demotion gives −0.0135, not +0.2042.

**Next here:** demote by a candidate's refusal rate against the *other
candidates for the same query* rather than against 1.0. Nothing tells the arm
which surfaces are concepts, so it stays legitimate.

## Next, in the order I would take them

1. **A flood with a beam as well as a floor.** The floor removes routes that
   mean nothing; nothing currently bounds how many meaningful ones survive, and
   that is what makes depth 3 unaffordable. `reach`'s beam is the missing half.
2. **Contradiction, which is nearly free.** `flood` returns every route to an
   endpoint and throws all but the strongest away. Two routes composing to
   incompatible kinds is README §5's ⬜ *a contradiction the map contains* — an
   output that was never an input, computable from what is already being
   discarded.
3. **Three steps, and it needs the flood to work first.** 0.2597 of answers lie
   further than two. `PathTypes.best` reduces a pair to one kind so the table
   stays pair-sized at any depth; nothing has been run there.
4. **An error signal** (README §7). Counts only go up, so nothing is ever wrong.
5. **Compression** (README §7). One principle for forgetting, hierarchy and a
   reason to reorganise.

## Known debts

- **THE DISTRIBUTED TESTBED DOES NOT RUN.** `testbed/driver.py` imports
  `openplexus.distributed` and `openplexus.models.local_memory`, both deleted in
  the restructure, so it parses and cannot load. `run.py` also documents a
  `--mode bucket` the driver has no code for. Its docstring says the container
  runs were verified on Docker Desktop and on CI, and two workflows exist, so
  those runs did happen — **against code that is gone.**

  Nothing caught it: `check_imports` skips `testbed/` because it "expects a
  container runtime", and `check_orphans` counts `testbed/` as a CALLER, so
  `bucket_peer` and `federated` look wired by a thing that cannot start. That is
  the `experiments/` hole one directory over, and the same fix applies.

  **This is the project's actual claim and it is unrunnable**, which is worse
  than the "untested" this file said before.
- **`tasks/xsl.py` has no caller.** Use it or drop it.
- **The link columns in `surfaces_pipeline.py` step in tenths** — shares over ten
  words, so nothing smaller than 0.1 can be read.
- **`experiments/` has nine scripts and no harness.** They share `Ranker`,
  `Marginal` and `load`; argument parsing and JSON writing are still copied.

## Reading leads, none of them read

Each may replace work otherwise done by hand, and each must be read before it is
cited anywhere. A remembered number about someone else's work is the borrowed
claim `CLAUDE.md` puts first.

- **Rule mining over paths, e.g. AnyBURL** (Meilicke et al. 2019). **Partly
  checked, and the check corrected the claim.** A search summary supports the
  direction — rule learning over paths is competitive with embedding models on
  this benchmark family — but says FB15k-237 is *specifically hard* for
  rule-based methods and puts AnyBURL slightly BELOW ConvE there, where I had
  said "reaches the RotatE range". No table has been read: the paper's PDF and
  SAFRAN's OpenReview page both returned unparseable content.

  What survives is enough to matter: **a rule-over-paths system lands near 0.31
  where ours lands at 0.247**, so the ceiling on this family is well above us and
  our implementation is the limit — length-2 only, one confidence per route
  shape, evidence summed rather than combined as probabilities, no filtering of
  unreliable rules. Those are the levers, in that order.
- **PROBE** (`arXiv 2606.08921`) — reweights the metric by inverse popularity.
  Fetched, not read; its smoothing constants were not in the summary, so the
  popularity stratification in `fb15k237_typed.py` takes the idea and not the
  metric.
- **Sun et al., ACL 2020** (`arXiv 1911.03903`) — reportedly the tie problem and
  an average-rank fix, which is the policy `fb15k237_audit.py` chose
  independently. **Two fetches returned the abstract only**; the substance is in
  the PDF and WebFetch returns it as binary.
- **Akrami et al.** — reportedly finds redundancy and leakage inflating accuracy
  19–175% across standard benchmarks. May name which datasets leak.
- **SCAN, COGS, CFQ** — splits made by structure rather than sampling, which is
  the property CLUTRR lacked. Audit any with the table attack before adopting.
