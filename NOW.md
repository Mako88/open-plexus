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

## In flight

- **`fb15k237_flood.py`, 150 queries, depth 2** → `out/fb15k237-flood.txt`. Two
  gates: `strength` weighs an edge `1/degree(neighbour)` and prunes by how well
  connected things are; `meaning` weighs every edge 1.0 so the only decay is the
  confidence of what a route composes into, which is what the design asks for
  and has no defence against a hub. Blend weight swept on both arms — and taken
  on TEST, which flatters the flood, because this run has no validation split.

  **Early reading is poor**: the strength gate reaches 0.208 of answers for
  ~36,000 expansions per query, against the capped enumeration's 0.35 for a
  fraction of that. If the meaning gate does not beat it, the honest conclusion
  is that flooding costs a great deal and buys less than flat enumeration.

## Next, in the order I would take them

1. **Structured logging.** Agreed with John. Experiments emit JSON rows already;
   what is not checked is that they ALL do, that prose never carries a number,
   and that a run's parameters travel with its rows. That is the thing that went
   wrong before the restructure, so it wants a check and not a convention.
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

- **Nothing runs as a node.** `bucket_peer`, `federated`, `deployment` are in
  `tools/orphans_baseline.json` because no entry point starts one. **The
  distributed half — the project's actual claim — is untested end to end**, and
  FB15k measures knowledge-graph completion instead, which the README names as
  the failure case.
- **`tasks/asking.py` has a falsifier registered as g44-01 and never run.**
- **`tasks/xsl.py` has no caller.** Use it or drop it.
- **The link columns in `surfaces_pipeline.py` step in tenths** — shares over ten
  words, so nothing smaller than 0.1 can be read.
- **`experiments/` has nine scripts and no harness.** They share `Ranker`,
  `Marginal` and `load`; argument parsing and JSON writing are still copied.

## Reading leads, none of them read

Each may replace work otherwise done by hand, and each must be read before it is
cited anywhere. A remembered number about someone else's work is the borrowed
claim `CLAUDE.md` puts first.

- **Rule mining over paths, e.g. AnyBURL.** Recalled, not checked: that learned
  path rules reach the RotatE range on FB15k-237 with no embeddings. If true our
  +0.0136 is far below what this family achieves and the ceiling is our
  implementation — length-2 only, one confidence per shape, evidence summed
  rather than combined as probabilities, no filtering. **Stated once in
  conversation already; check before repeating.**
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
