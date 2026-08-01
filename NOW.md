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

## g44-01: asking separates a confound watching cannot

**Settled.** `learned_threshold` splits observed refusal rates and demotes only
the low group, using nothing but rates the arm paid for. At 384 asks per pair it
reaches **+0.2256**, matching an oracle that calls `is_shadow`, against
watching's **−0.2967**. Tested; both mutations caught.

**No arm reaches it, and the constraint is one number: pairs × asks-per-pair,
bounded by the budget.** A real part is detachable 62% of the time (refused
0.3837 against a shadow's 0.2222), so the signal is a 0.16 gap needing ~48 asks
per pair to resolve, and a misclassified pair demotes a real part rather than
merely failing to help. Reaching +0.19 took 108 × 96 = 10,368 asks against a
4,000 stream. Policy, budget, noise, sampler pricing, metric strictness,
coverage and self-poisoning have each turned out to be a face of that.

**P12 refuted at 100%:** where there is no confound the rule demotes all 72 true
partners. The two cases differ by the low group's absolute level (0.2105 against
0.3779); the scale-free ratio is useless (0.55 against 0.52). **Missing: an
absolute anchor the arm can compute.**

**Scored:** P5, P7, P9, P11, P15, P19 held. P1–P3, P6, P8, P10, P12, P13, P18
refuted. P14 unmeasurable, P16 withdrawn as an artefact.

## THE ASKING POLICY BUILDS A GRAPH AND NEVER WALKS IT

**John's observation, 2026-08-01**, and it is correct. He expected the graph to
be built from the moments. It is: `index.observe(occasion.surfaces)` turns every
moment into edges. What he spotted is that nothing then uses it as a graph —
`grep -c "pathways|flood|reach|routed"` in `g44_01_asking.py` returns **0**, and
every use is `statistic(index, a, b)`, a single direct edge.

**I had described these as two separate worlds. That was wrong** and it hid the
gap rather than naming it.

**Why it matters for the wall.** The policy nominates by DIRECT association —
`conditional`, then mutual predictability — both one-hop. A confound is a
two-hop fact: two things tied together only through a third. The policy cannot
express that, so it must TEST candidates it might have ruled out structurally.

Today's conclusion was that the constraint is a product, pairs × asks-per-pair,
bounded by budget. That holds as measured, and it assumes every candidate has to
be tested. **A policy that reads the neighbourhood's shape might not need to
test most of them** — which would break the bound rather than trade along it.
Untested, and it is the first idea here that is not another face of that number.

## CROSS-MODAL DOES NOT CURRENTLY REACH, and John remembers it working

**John's report, 2026-08-01:** before the restructure, audio reached a word with
no direct connection. The experiment survived — `surfaces_pipeline.py`'s
`alternating` arm is exactly that test, an image code and an audio code sharing
ZERO occasions so the only route is through the word. Run today:

    front     arm          link_img  link_aud   cross  crossed
    kmeans    together       0.0000    0.0000  0.7339   1.7563
    kmeans    alternating    0.9000    0.0000  0.0000   0.0000

**`crossed` is 0.0000: no image code reaches any audio code at all**, and that
column exists precisely so a collapse and an empty reach are not read alike. In
that arm `link_aud` is also 0.0000 — the audio codes do not link to their own
word, so there is nothing for a route to pass through. `together` works, but
that is the arm where both senses share a moment, which is not the claim.

**Not yet established: regression or never-held-here.** Saying which needs the
pre-restructure run, not a guess. **This outranks further g44-01 work** — a
headline result that stopped reproducing is worth more than another decimal on
one that did not.

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
