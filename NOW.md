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

## g44-01 is answered: asking separates a confound watching cannot

**A legitimate rule matches the oracle exactly.** `learned_threshold` splits the
observed refusal rates by two means and demotes only the low group, reading
nothing but rates the arm paid for. At 384 asks per pair it reaches **+0.2256**
where an oracle calling `is_shadow` reaches **+0.2256**, against watching's
**−0.2967**. Tested, and its mutation is caught.

**Why a threshold and not a gradient.** Per-pair rates at 192 asks: true
partners 0.292–0.474, shadows 0.135–0.292, and zero of 216 true partners below
the highest shadow. Every earlier rule multiplied a score by a rate, which is
the wrong shape for a clean split.

**Scored:** P5, P7, P9, P11 held. P1–P3, P6, P8, P10, P12 refuted. P10 failed on
**allocation** — at budget 0.10 the arm spends ~400 asks over 53 pairs, about 7
each, and the rule needs 48 before it beats doing nothing.

**Next here, and it is scheduling rather than mechanism:** every arm nominates a
fresh pair each draw, which is the worst spend for a rule needing a resolved
rate per pair. An arm that revisits fewer pairs is the change.

**P12 stands refuted at 100%, and it is a real defect.** Two means always return
two groups, so where the shadow genuinely is a part the rule demotes true
partners on every query. It has no way to report *nothing here*. P11 held, so it
does not manufacture an advantage — but a confound detector that cannot find
nothing is half a detector.

**One correction on the record.** "The shortfall is structural and not sampling"
was concluded from sweeping the raw and comparative rules. It does not
generalise: a multiplier takes a noisy rate as an unbiased factor, a classifier
asks which side of a boundary a value is on and noise flips it. The sweep has to
be re-run per rule.

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
