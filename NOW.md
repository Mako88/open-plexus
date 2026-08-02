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

## The flood: void numbers, superseded by the broadcast design

`flood` does NOT beat flat enumeration (+0.0081 against +0.0136) — **but it ran
on published knowledge-graph triples, not on anything this system observed**, so
the numbers say nothing about the graph the architecture builds. Nor can it be
re-pointed at the senses graph: it takes `types: PathTypes`, route KINDS from
FB15k's typed relations, and co-occurrence edges have no kind. **The merged
graph IS walked** by `equivalence_classes` — I claimed otherwise; wrong.

## g44-01: CLOSED — see `experiments/g44_01_asking.py` and its commits

**Settled and out of this file**, which carries only unfinished work. One line:
`ask-set` beats watching by **+0.0085** (paired, 20 seeds, 16/20) — the only arm
that does, at 1.6% of the oracle's swing. The bound survived every attempt to
move either factor or the total, and each ruled-out branch records what revives
it. **The principle worth keeping: ask about a candidate relative to what IT
predicts, not relative to the query that made you notice it.**

## THE ASKING POLICY BUILDS A GRAPH AND NEVER WALKS IT

**John's catch**, still true: `grep -c "pathways|flood|reach|routed"` in
`g44_01_asking.py` returns **0**. Two structural attempts refuted — containment,
and containment with the background discounted — because a shadow's
neighbourhood is not distinctive. **The asymmetry is DIRECTIONAL**, which is why
mutual prediction works and overlap does not, and why the broadcast design
refuels on mutual strength.

## ONE GRAPH: BUILT, AND FOUR CHECKS GUARD IT

The senses share one declared graph. `stream()` was already a hand-rolled
namespace with the same layout, so `Namespace` gave byte-identical node numbers
and the results table was the regression check.

**Four checks, each catching what the others cannot, each mutation-caught:**
`graph=N`, `holding={...}`, `disjoint=True`, and `shared.linked(a, b)` —
co-resident but disconnected, which the other three pass. Each of the last three
exists because checking showed the previous one blind, and the fourth caught a
real bug in `SharedGraph` one commit after being written.

**CROSS-MODAL REACHES AGAIN**, the first measurement on the merged architecture:
at `--repeats 2` the `alternating` arm — senses sharing ZERO occasions — reaches
**cross 1.0000** where it was 0.0000. Under-resourced, not regressed; and the
repeat reuses recordings, so `g40-01`'s ~300 per digit is a price in EVIDENCE.

## THE DESIGN, AGREED WITH JOHN 2026-08-01. Not started

**A correction that makes the rest work.** There is not one node per concept per
modality — there are MANY. About a hundred image codes per digit at 1024 codes,
which is what `q_img 0.90` measures. **The multiplicity is the point:** the
system has to discover that those hundred codes are one thing, and does, by them
all reaching the same word. One node per digit would mean somebody decided which
images count as a three, which is the label the design exists to avoid.

**A concept is never stored** — it is what the walk recovers, so it is already
distributed: its image and audio nodes can live on different machines, because
they were never together.

### The broadcast flood

John's design, made concrete. Input is broadcast to every node. A node holding a
MUTUAL link to something in the broadcast re-broadcasts, appending itself, so a
route carries its whole chain of reasoning — the thing `flood` was built for.

- **Stamina replaces the floor.** A route carries a budget refuelled by the
  strength of what it walks, so strong reasoning funds itself and weak reasoning
  runs out. **One fewer tuned constant**, which is the objection to the floor.
- **Refuel on MUTUAL strength, never raw.** The ever-present background has the
  strongest raw edges to everything and destroyed four separate policies today.
  Raw-fuelled stamina would fund routes straight through it and starve the rest,
  and they would arrive looking like the best reasoning in the system.
- **Termination is accounting, not a threshold.** A route splitting into k
  children reports k; a dying route reports one death; the origin knows the live
  count and the thought is over at zero. No cutoff to tune, and the origin ends
  holding every complete chain that survived.
- **It produces the honest cost columns**: MESSAGES SENT and WORK PER NODE,
  which `expansions` never measured.

### Prediction, and it closes TWO gaps at once

**Counts only go up, so nothing in this system can ever be wrong** — there is no
error signal anywhere. Predicting the next input and learning from the miss
supplies one.

**And John's connection, which is the strongest idea in the session: prediction
error is what should DRIVE the asking.** Nothing currently decides when to ask
rather than watch — it is a fixed budget fraction, a knob. A surface that keeps
being predicted wrong is exactly a surface worth spending a question on. One
mechanism fills both holes, and it needs no new knob.

### Decided

- **No tokenizer.** A tokenizer's vocabulary is LEARNED from a corpus we never
  saw, which is the imported artefact this design exists to avoid. LSH the text
  bytes like everything else; cross-language then follows only where writing
  systems share bytes, and that is the honest version.
- **Facts are dropped**, not islanded. They are a separate corpus sharing no
  referent with anything sensory.
- **No pre-commit hook.** John's call: every red preflight so far was caught and
  fixed immediately, so the check has not been needed.

### The label that is still load-bearing

**The word surface is ground truth** — `said = [u.digit for u in heard]`. So
*audio reaches image through the word* is real routing through a SUPERVISED
anchor. The front end is genuinely untrained; the anchor is not.

## Known debts

- **DISTRIBUTED: entry point and in-process agreement DONE, container left.**
  `node_main.py` runs a node as a process on TCP. A `Federation` across 4 owners
  agrees with a whole `CoOccurrence` on every read, still at 32 owners where
  most nodes are empty, read through `federation.at(owner)` so it checks the
  routing rather than stepping past it. **Left: the container run** — latency,
  departure, partition. `testbed/driver.py` stays dead: it measures a network
  the restructure deleted, and only its question survives, in `agreement.py`.

- **`tasks/xsl.py` has no caller.** Use it or drop it.
- **The link columns in `surfaces_pipeline.py` step in tenths** — shares over ten
  words, so nothing smaller than 0.1 can be read.
- **`experiments/` has nine scripts and no harness.** They share `Ranker`,
  `Marginal` and `load`; argument parsing and JSON writing are still copied.

## Reading leads, none of them read

- **AnyBURL / rule mining over paths** — partly checked, and the check corrected
  me. What survives: **a rule-over-paths system lands near 0.31 where ours lands
  at 0.247**, so our implementation is the limit — length-2 only, one confidence
  per route shape, evidence summed rather than combined, no filtering.
- **Interventional causal discovery under a budget** — unsearched. The sharper
  question: **when does structure say what you need not test?**
- **Predictive coding** — new, and the one to read first: prediction now has two
  jobs here, the missing error signal and the trigger for asking.
