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

## The flood, and why its numbers are now suspect

Measured: meaning-gating beats strength-gating about two to one, and `flood`
does NOT beat flat enumeration (+0.0081 against +0.0136). **But it was tested on
knowledge-graph triples — published facts, not anything this system observed —
so it says nothing about traversing the graph the architecture is meant to
build.** John's point, and it stands.

**Unfinished:** a confidently-composing route barely decays, so the floor kills
what means nothing and does not bound what means something. `flood` has a floor
and no beam. And EXPANSIONS IS THE WRONG COST COLUMN where nodes expand in
parallel — what transfers is messages sent and work per node, neither measured.

## g44-01: asking separates a confound watching cannot

**Settled and paused behind the architecture work.** `learned_threshold` demotes
only the low group of observed refusal rates, using nothing but rates the arm
paid for, and at 384 asks per pair reaches **+0.2256** — matching an oracle that
calls `is_shadow`, against watching's **−0.2967**. Tested, mutations caught.

**No arm reaches it, and the constraint is one number: pairs × asks-per-pair.** A
real part is detachable 62% of the time (refused 0.3837 against a shadow's
0.2222), so the signal is a 0.16 gap needing ~48 asks per pair, and a
misclassified pair demotes a real part rather than merely failing to help.
Policy, budget, noise, sampler pricing, metric strictness, coverage and
self-poisoning were each measured and each turned out to be a face of that.

**P12 refuted at 100%:** with no confound present the rule demotes all 72 true
partners. Missing is an absolute anchor the arm can compute; the scale-free
ratio is useless (0.55 against 0.52).

## THE ASKING POLICY BUILDS A GRAPH AND NEVER WALKS IT

**John's catch, 2026-08-01.** `index.observe` turns every moment into edges, and
then `grep -c "pathways|flood|reach|routed"` in `g44_01_asking.py` returns **0** —
every use is `statistic(index, a, b)`, one direct edge.

**Why it bears on the wall.** The policy nominates by direct association, which
is one-hop. A confound is a TWO-hop fact: two things tied together only through
a third. A one-hop policy cannot express that, so it must TEST candidates that
structure might have ruled out for free — and the product bound assumes exactly
that every candidate needs testing. Reading the neighbourhood's shape would
break the bound rather than trade along it. Untested.

## ONE GRAPH: THE PROBLEM IS KINDS, NOT INSTANCES

`CoOccurrence` is the whole representation, and **no single graph has ever held
more than one KIND of thing** — images+audio+words in one, intervention moments
in another, knowledge-graph facts in a third, none ever meeting.

**Counting instances does not find it.** One arm builds one graph and
`expect(graph=1)` passes; a sweep builds many and that is correct, since arms
are independent experiments. So `wiring.expect(holding={...})` counts the KINDS
entering a graph and fails when a declared one never arrives. Built, tested,
mutation caught.

**THE MERGE NEEDS A NAMESPACE, AND THE KIND CHECK IS BLIND TO IT.** All three
sources number from zero: image codes `[0, codes)` then audio then words;
concept surfaces then distractors then shadows; `{entity: i for i, entity in
enumerate(entities)}`. Merged naively, image code 0 and concept surface 0 and
entity 0 are ONE integer accumulating into one row — no error, just wrong
counts. And `expect(holding=...)` passes it: all four kinds arrive, lying on
top of each other.

**Next, and it goes in before any merging:** a companion asserting ids from
different kinds are DISJOINT. Then the merge itself.

**Also:** traversal (beam walk, `flood`, route-typing) MOVES through a graph;
intervention EDITS it. Different operations on one structure, discussed as
alternatives.

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

  **`node_main.py` is now built**, smoke-tested end to end: a separate process
  listening on TCP, answering a real socket request and refusing an unknown
  message. C1's discrete unit runs again.

  **The driver is NOT a rewrite, it is a replacement, and checking that first
  is what stopped a wasted port.** `testbed/driver.py` measures whether a
  distributed `Network` of `LocalAssociativeMemory` agrees with a single-process
  one — weights `wv`/`wo`, `d_model`, a vocabulary. **That architecture was
  deleted in the restructure**, so there is nothing in it to point at
  `bucket_peer`; only its SHAPE survives, and that shape is the right one.

  What it did was compare a distributed result against a single-process
  reference and report where they diverge — *"the only measurement that
  distinguishes a network which is slow from one which is wrong"*. The
  replacement asks the same question of the count graph: does a `CoOccurrence`
  split across `federated` owners answer reads identically to one held whole?

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
