# Now

What is being worked on, and what has been agreed but not started.

**The invariant:** every 🚧 in [README.md](README.md) appears here, and nothing
appears here that is not in the README. An approved piece of work cannot go
quiet, which is how the LSH front end was agreed and then dropped for two
sessions.

**A finding updates a line; it never appends one.** Settled results belong in the
README, which carries the claim; this file carries only what is unfinished.
Delete a line when it is done. Nothing may cite this file. Rewritten at the end
of every turn — see `.claude/skills/monitor`.

---

## Waiting on John

**Decay, eviction to disk, and reinstatement.** His design, 2026-08-01: edges
weaken over time; a node fired adds to its own lifespan; a node whose edges have
worn away is written to disk on its own machine and loaded back if it ever fires
again. It is C1-legal without trying, and it is the same primitive as the
flood's stamina. Two amendments proposed and not yet answered:

- **Archive the edges with the node, not just the node.** A node reinstated
  empty has been forgotten in the only sense that matters.
- **Reinstate with a boost, not the default weight**, or a node hovering at the
  threshold pages in and out forever.

Also unanswered: whether uniform decay is free. It should be — every statistic
here is a ratio, so a common factor cancels — but lazy decay-on-read is not
uniform unless each edge carries a stamp and is aged forward to now. Unmeasured.

Not in the README until he answers, because it is proposed rather than approved.

## The broadcast flood: BUILT, not measured

`openplexus/broadcast.py`. Many seeds, stamina in place of a floor, termination
by accounting, and the per-node work columns `pathways.flood` never produced.
15 tests, 4 mutations, all caught. Callerless for one step and recorded in
`tools/orphans_baseline.json` with the reason.

**The gate is `forward`, and the design said mutual.** Measured on the real
proportions — a word on 845 occasions, its codes on 60, a distractor on 3,845:

    seeded at a rare code   min  0.2298 vs 0.1231   correct
    seeded at the hub word  min  0.0766 vs 0.3592   INVERTED

Mutuality is not wrong everywhere. **It is wrong from the common end**, and a
flood stands on both ends during one walk — a route seeded at an image code
arrives at the word and expands from the word, and that hop is scored from the
hub's side. `forward` is the only combiner correct at both. A first version of
this claim said symmetrising is always wrong; a test refuted it.

**Mutuality survives elsewhere and the distinction is worth keeping:** as a
top-k membership gate in `equivalence_classes` it is load-bearing and has its
own mutation. It fails as a weight, not as a filter.

**Not measured: whether many seeds replace edge kinds.** The typed walk
discriminated by route kind. This has no kinds and its questions have none
either, so the claim is that hundreds of surfaces firing at once converge. That
is the first measurement and it needs the word channel repaired first.

## The word channel is a label, and the repair is to make it a modality

John's position is right — text is a legitimate input for a digital system, one
of several that co-occur. The code does not implement it. In
`surfaces_pipeline.py`:

- `shared.reserve("word", len(mnist.WORDS))` — **one node per class**, ten in
  total, where image has about a hundred codes per digit at 1024.
- `present = [("word", digit)]`, from `said = [u.digit for u in heard]` — the
  scoring label, never wrong, never absent. The correct word is on 100% of
  occasions; each of the `NOISE = 2` wrong words on about 20%.

**The repair follows from the position rather than retreating from it:** emit
the word as bytes and LSH them, so one word becomes many surfaces the system
must discover are one thing; let it be absent sometimes and wrong sometimes;
include several written forms. **It is a precondition for the flood's headline
measurement** — the many-seeds claim cannot be tested on a channel of
multiplicity 1.

## Prediction, agreed and not started

Counts only go up, so nothing here can ever be wrong; predicting the next input
supplies the missing error signal. **John's connection: prediction error is what
should drive the asking**, which currently runs on a fixed budget fraction. One
mechanism, two holes, no new knob.

Named risk, from active learning: uncertainty sampling chases irreducible noise.
A surface unpredictable because it is random attracts every question and teaches
nothing — structurally the ever-present distractor, one level up. Cheap proxy:
ask where error is high **and falling**, not high and flat.

## Decided

- **No tokenizer.** Its vocabulary is learned from a corpus we never saw.
- **Facts are dropped**, not islanded — a separate corpus sharing no referent.
- **No pre-commit hook.** Every red preflight so far was caught immediately.
- **Video after the flood and the word channel.** It hands over prediction
  targets for free, which is what the error signal needs, and continuity across
  frames is an unsupervised answer to the multiplicity problem.

## Known debts

- **From the README audit, 2026-08-01.** `experiments/fb15k237_flood.py:264`
  prints `+0.0136 margin, 0.35 arrived` as a **string literal**, not recomputed
  on the queries the run sampled, so the comparison spans two query sets and two
  floors. Two lines down, published full-test-set MRRs are compared against the
  subsample floor and labelled "the same kind of floor" — that is where
  `DistMult +0.0224` comes from against the README's correct `+0.0076`. Fix both
  before the flood numbers are cited again.
- **Nine files reference `openplexus/distributed.py`, `openplexus/peer.py` or
  `DECISIONS.md`**, none of which exist. A search for "is there a dimension
  split" finds prose saying yes.
- **`deployment.py` and `agreement.py` are dead** — imported by nothing but
  their own tests, and `deployment.py` budgets predecessor-era `w × d`
  associative memory. **`tasks/xsl.py` has no caller.**
- **DISTRIBUTED: entry point and in-process agreement done, container left.**
  `node_main.py` runs a node on TCP; a `Federation` across 4 owners agrees with
  a whole `CoOccurrence` on every read, still at 32 owners. Left: latency,
  departure, partition. `testbed/driver.py` measures a deleted network.
- **The link columns in `surfaces_pipeline.py` step in tenths.**
- **`experiments/` has nine scripts and no harness.**
- **§5's ⬜ "refuse when nothing was written — the machinery exists" is
  unverified.** Every refusal in the package is an ownership refusal or the
  asking experiment's detachability rate. Neither is that.

## Reading leads, none of them read

- **Predictive coding** — read first; prediction now has two jobs here.
- **AnyBURL / rule mining over paths** — a rule-over-paths system lands near
  0.31 where ours lands at 0.247, so our implementation is the limit: length-2
  only, one confidence per route shape, evidence summed rather than combined.
- **Interventional causal discovery under a budget.** The sharper question:
  when does structure say what you need not test?
