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

## The flood: numbers void, and it cannot simply be re-pointed

Measured: meaning-gating beats strength-gating about two to one, and `flood`
does NOT beat flat enumeration (+0.0081 against +0.0136). **But it ran on
published knowledge-graph triples, not on anything this system observed**, so it
says nothing about traversing the graph the architecture builds — John's point,
and it stands.

**It cannot simply be re-run there, which I proposed before checking.** `flood`
takes `types: PathTypes` — route KINDS built for FB15k's typed relations — and
the senses graph has untyped co-occurrence edges. A design question, not a
re-run.

**And the merged graph IS walked**, by `equivalence_classes` at
`surfaces_pipeline.py:182`: the cross-modal `cross 1.0000` is already a walk on
a graph this system built. I said it had never been walked; wrong.

**Unfinished:** `flood` has a floor and no beam, and EXPANSIONS IS THE WRONG
COST COLUMN where nodes expand in parallel — what transfers is messages sent and
work per node, neither measured.

## g44-01: CLOSED. An arm beats watching, and the bound survived everything

**`ask-set` beats watching**, paired over 20 seeds: **+0.0085, sd of the mean
0.0025, 16/20**. The first and only arm to do it — **1.6% of the oracle's swing**
from −0.2967 to +0.2256. A mechanism identified, not a problem solved.

**WHY, and it explains six failures at once.** Of the scored pairs each arm
demotes, `ask-mutual` gets 0.2 confounds to 51 real parts; `ask-set` gets 4 to 8.
`ask-mutual` pays for the right suspects and files them under the wrong
questions — it asks a shadow against whichever query made it notice the shadow,
usually not that shadow's own concept's surface, so `separation` never reads the
pair. **The arm with the best coverage was the worst arm.**

**The principle: ask about a candidate relative to what IT predicts, not
relative to the query that made you notice it.** Nomination and interrogation
were one step in every arm. **Coverage of the METRIC is not coverage of the
CONFOUNDS.**

**The line is closed, not abandoned.** Ten ideas; the bound survived every
attempt to move either factor or the total, each refutation saying what revives
it: combining what worked (aim and concentration spend one budget — 77%
confounds at 7 candidates instead of 44); set-asking (no denominator, so no
rate); harvesting (enrichment separates wider, 0.2354 against 0.16, and resolves
**3–5× worse** on a 4× wider spread — revived by a harvested quantity whose
spread falls with asks like a Bernoulli's).

**The refusal rate is the best quantity measured, not merely the incumbent.**

**Three of my explanations died here**, each within a commit or two of being
written: "it changes the price of a fact" (refuted by `SET_SIZE=1`), a recorded
53 that had drifted to 46 unnoticed, and enrichment's wider gap meaning anything.

## THE ASKING POLICY BUILDS A GRAPH AND NEVER WALKS IT

**John's catch**, still true: `grep -c "pathways|flood|reach|routed"` in
`g44_01_asking.py` returns **0**. Two structural attempts refuted — containment,
and containment with the background discounted — both because a shadow's
neighbourhood is not distinctive: it meets the background and its concept's
surfaces exactly as a true partner does. **The asymmetry is DIRECTIONAL**, which
is why reading `P(query | candidate)` works and overlap does not. A directional
two-hop measure is unbuilt.

## ONE GRAPH: BUILT, AND FOUR CHECKS GUARD IT

`CoOccurrence` is the whole representation and no single graph had ever held
more than one KIND of thing. **The merge landed:** `stream()` was already a
hand-rolled namespace with the same layout, so `Namespace` gives byte-identical
node numbers and the results table was the regression check.

**Four checks, each catching what the others cannot, each mutation-caught:**
`graph=N`, `holding={...}`, `disjoint=True`, and `shared.linked(a, b)` —
co-resident but disconnected, which all three others pass. Each of the last
three exists because checking showed the previous one blind.

**The declaration caught a bug in `SharedGraph` on its first run:** `holds()`
read process-global state, so one-graph-per-arm runs reported earlier arms'
kinds. Every test passed with it — they build one graph per test.

**CROSS-MODAL REACHES AGAIN**, the first measurement on the merged architecture:
at `--repeats 2` the `alternating` arm — senses sharing ZERO occasions — reaches
**cross 1.0000** where it was 0.0000. Under-resourced, not regressed. The repeat
reuses recordings, so `g40-01`'s ~300 per digit is a price in EVIDENCE.

**Facts stay a separate island.** DEFAULT APPLIED, John to override.

## ONE UNGUARDED RULE, AND TWO ALREADY GUARDED

**I committed on a red preflight**, chaining `git commit` off a `grep` of its
output so `&&` read grep's status. Nothing stopped it; I found it re-reading my
own output. Candidate: a pre-commit hook running preflight, ~70s per commit.
John's call, since he bears it.

**Two others need no new check.** A corrupted `mutate.py` fails preflight's
import step (caught 4/4), and a mutation whose target moves fails `--verify`
(caught 2/2, both times because `ask` changed shape). The guards exist and work;
what they cost was my time, not a wrong result.

## Known debts

- **DISTRIBUTED: entry point done, in-process agreement done, container left.**
  `node_main.py` runs a node as a process on TCP. `agreement.disagreements`
  reports WHERE a split graph differs from a whole one, and a `Federation`
  across 4 owners agrees with a whole `CoOccurrence` on every read — still at 32
  owners, where most nodes are empty. Reads go through `federation.at(owner)`,
  the path `rank` uses, so it checks the routing rather than stepping past it.

  **Left: the container run** — latency, departure, partition — a different
  question and a later phase's. `testbed/driver.py` stays dead and is a
  REPLACEMENT not a rewrite: it measures a `LocalAssociativeMemory` network the
  restructure deleted. Only its reason survives, and `agreement.py` carries it.

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
