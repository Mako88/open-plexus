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
published knowledge-graph triples, not on anything this system observed**, so it
says nothing about traversing the graph the architecture is meant to build.
John's point, and it stands. **Unfinished:** `flood` has a floor and no beam,
and EXPANSIONS IS THE WRONG COST COLUMN where nodes expand in parallel — what
transfers is messages sent and work per node, neither measured.

## g44-01: asking separates a confound watching cannot

**Settled, and paused behind the architecture work.** `learned_threshold`
demotes only the low group of observed refusal rates, using nothing but rates
the arm paid for, and at 384 asks per pair reaches **+0.2256** — matching an
oracle that calls `is_shadow`, against watching's **−0.2967**.

**No arm reaches it, and the constraint is one number: pairs × asks-per-pair.** A
real part is detachable 62% of the time (0.3837 refused against a shadow's
0.2222), so the signal is a 0.16 gap needing ~48 asks per pair, and a
misclassified pair demotes a real part. Policy, budget, noise, sampler pricing,
metric strictness, coverage and self-poisoning were each measured and each was a
face of that number.

**P12 refuted at 100%:** with no confound present it demotes all 72 true
partners. Missing is an absolute anchor the arm can compute — the scale-free
ratio is useless (0.55 against 0.52).

## THE ASKING POLICY BUILDS A GRAPH AND NEVER WALKS IT

**John's catch.** `index.observe` turns every moment into edges, then
`grep -c "pathways|flood|reach|routed"` in `g44_01_asking.py` returns **0** —
every use is one direct edge. The policy nominates by direct association, which
is one-hop; a confound is a TWO-hop fact, two things tied together only through
a third. So it must TEST candidates structure might have ruled out for free, and
the product bound assumes exactly that. Reading the neighbourhood's shape would
break the bound rather than trade along it. Untested.

## ONE GRAPH: BUILT, AND FOUR CHECKS GUARD IT

`CoOccurrence` is the whole representation, and **no single graph had ever held
more than one KIND of thing** — images+audio+words in one, intervention moments
in another, knowledge-graph facts in a third.

**Four checks, each catching what the others cannot, each mutation-caught:**
`graph=N` (an accumulator split by accident), `holding={...}` (a declared kind
that never arrived), `disjoint=True` (two kinds sharing node numbers, which
`holding` is blind to), and `shared.linked(a, b)` (two kinds co-resident but
disconnected, which all three others pass).

**THE MERGE LANDED.** `stream()` was already a hand-rolled namespace with the
same layout, so `Namespace` gives byte-identical node numbers and the whole
results table was the regression check — every figure unchanged. The senses now
share one declared graph.

**The declaration caught a bug in `SharedGraph` on its first run:** `holds()`
read process-global `wiring.kinds()`, so one-graph-per-arm runs had every graph
reporting earlier arms' kinds. Every test passed with it — the tests build one
graph per test, exactly the case it did not break. Fixed, regression test added.

**Facts stay a separate island.** DEFAULT APPLIED, John to override: nothing in
the data bridges a fact to a picture, and inventing that corpus is a bigger
decision than a default should make.

**Next:** the 6,000-occasion cross-modal run, now that the architecture it would
be measured on is the intended one.

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

A remembered number about someone else's work is the borrowed claim `CLAUDE.md`
puts first.

- **AnyBURL / rule mining over paths** (Meilicke 2019). Partly checked, and the
  check corrected me: FB15k-237 is specifically hard for rule-based methods.
  What survives — **a rule-over-paths system lands near 0.31 where ours lands at
  0.247**, so our implementation is the limit: length-2 only, one confidence per
  route shape, evidence summed rather than combined, no filtering.
- **Interventional causal discovery under a budget** — not searched. The sharper
  question after today: **when does structure say what you need not test?**
- **SCAN, COGS, CFQ** — splits made by structure rather than sampling, which is
  the property CLUTRR lacked. Audit any with the table attack before adopting.
