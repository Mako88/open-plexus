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

Settled and in the commits: meaning-gating beats strength-gating about two to
one, and the flood does NOT beat the flat enumeration (+0.0081 against +0.0136
at 300 queries). A +0.0164 at sixty queries was withdrawn.

**Unfinished:** a route that composes confidently barely decays, so the floor
kills what means nothing and does not bound what means something. `flood` has a
floor and no beam, and they do different jobs.

**EXPANSIONS IS THE WRONG COST COLUMN.** Where every node expands its own edges
in parallel, wall clock is the longest path, not the sum of all work. What
transfers is MESSAGES SENT and WORK PER NODE, and neither is measured.

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
partners. The cases differ by the low group's absolute level (0.2105 against
0.3779); the scale-free ratio is useless. **Missing: an absolute anchor.**

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

## CROSS-MODAL: UNDER-RESOURCED, NOT REGRESSED

**John reported it working before the restructure and he is right.** `g40-01`
(readable at `f0a8a72^`) passed gate G7 and priced it: **a cross-modal link
costs ~300 occasions per digit**, against ~16 within-modal.

`surfaces_pipeline.py` runs 3,000 occasions over ten digits — exactly 300 each —
but `alternating` puts sound on odd occasions and pictures on even ones, so
**audio gets 150 per digit.** The arm that tests the claim is the one arm that
cannot afford it, and its `crossed` 0.0000 looks exactly like a broken mechanism.
The run now prints which arms can afford their own test.

**Unrun:** reuse recordings so `alternating` carries 6,000 occasions. No
`--occasions` flag and the audio set caps at 3,000, so the occasion builder must
repeat them. Predicted to make `crossed` non-zero.

## Known debts

- **THE DISTRIBUTED HALF IS ONE MISSING ENTRY POINT, not a hole.** The
  inventory says it precisely: `bucket_peer` (answers reads over a socket),
  `federated` (the count graph split across owners, remote reads counted) and
  `deployment` (how many slices a machine holds) all exist and are tested, and
  **all three have no caller because `node_main.py` was deliberately not carried
  over in the restructure.** `testbed/driver.py` then imports two modules the
  restructure deleted, so it cannot load; its docstring's container runs did
  happen, against code that is gone.

  Nothing caught it: `check_imports` skips `testbed/`, and `check_orphans`
  counts it as a CALLER, so the modules looked wired by a thing that cannot run.

  **This is C1 and John asked about it directly, 2026-08-01.** The discrete
  units exist; nothing launches one. The job is an entry point plus rewriting
  the driver against `bucket_peer`, and it is bounded.

- **`tasks/xsl.py` has no caller.** Use it or drop it.
- **The link columns in `surfaces_pipeline.py` step in tenths** — shares over ten
  words, so nothing smaller than 0.1 can be read.
- **`experiments/` has nine scripts and no harness.** They share `Ranker`,
  `Marginal` and `load`; argument parsing and JSON writing are still copied.

## Reading leads, none of them read

Each must be read before it is cited. A remembered number about someone else's
work is the borrowed claim `CLAUDE.md` puts first.

- **AnyBURL / rule mining over paths** (Meilicke 2019). Partly checked, and the
  check corrected the claim: FB15k-237 is specifically hard for rule-based
  methods and AnyBURL sits slightly BELOW ConvE there. What survives: **a
  rule-over-paths system lands near 0.31 where ours lands at 0.247**, so our
  implementation is the limit — length-2 only, one confidence per route shape,
  evidence summed rather than combined, no filtering of unreliable rules.
- **Interventional causal discovery under a budget** — not yet searched. The
  sharper question after today: not how to spend a budget of interventions, but
  **when structure says what you do not need to test**.
- **PROBE** (`arXiv 2606.08921`) — reweights the metric by inverse popularity.
  Fetched, not read; its smoothing constants were not in the summary, so the
  popularity stratification in `fb15k237_typed.py` takes the idea and not the
  metric.
- **SCAN, COGS, CFQ** — splits made by structure rather than sampling, which is
  the property CLUTRR lacked. Audit any with the table attack before adopting.
