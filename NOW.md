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

**Which chain to render.** The flood returns many complete chains and nothing
chooses among them. John named this as the piece he does not know how to do,
and there is no candidate mechanism. Three offered, none agreed:

- **Let the world choose.** Output the chain whose consequences best predict
  what arrives next. Needs the 🚧 prediction mechanism and no new knob, and it
  makes the choice testable against the world rather than against an internal
  score.
- **Let arrival choose.** In John's own addressing design the input names an
  output machine, so the candidates are exactly the chains that reach it.
- **Let brevity choose.** The chain explaining the most input in the fewest
  steps, which is §7's compression principle doing a second job.

**Whether ARC-AGI-3 is the target.** John's suggestion: feed frames in, wire
outputs to the buttons, watch. It is the right KIND of target — it needs action,
which §4 and §6 both name as missing, and it supplies an error signal for free.
The objection is that counting needs recurrence and ARC withholds it by design;
the interactive form weakens that but does not remove it. **A null there would
not be informative**, because the action channel does not exist yet and the
induction is hard, so a failure could not be attributed. Recommended
intermediate: any environment where an action changes what is observed.

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

## The word channel: repaired and measured. What is LEFT

Settled and moved to the README. The run is `out/word-channel-comparison.txt`
and the three JSON files beside it; `--words label` stays the default so every
earlier number is reproducible.

**Unfinished:**

- **The dials are chosen, not measured.** `silence 0.15 mistake 0.05
  corrupt 0.30`. `--silence/--mistake/--corrupt` exist so they can be swept and
  nothing has swept them.
- **Nobody has asked what the byte channel is worth on its own terms.** Every
  column here was built to score a channel of multiplicity 1. `link_img` counts
  surfaces per word NODE, so a channel with 72 nodes per digit is measured on a
  denominator that means something different — the comparison is sound in
  direction and unsound in units.
- **Order is discarded.** `features` is a bare byte histogram, so `three` and
  `there` are one surface. The obvious extension, and it changes the SPACE
  rather than the allocation, which is where §1 says such changes land.

**Dials are dials.** `silence 0.15 mistake 0.05 corrupt 0.30` were chosen, not
measured, and `--silence/--mistake/--corrupt` exist so a result that moves with
them is visible as a result about them.

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

- **FIXED, and the fix changed the answer.** `fb15k237_flood.py` printed
  `+0.0136 margin, 0.35 arrived` as a string literal from a full-test-set run,
  while its own margins came from whatever subsample it drew. The capped
  enumeration is now an arm in the same table, on the same queries, through the
  same scoring loop. Published margins are no longer computed at all — the
  subtraction across query sets is refused and both numbers are printed.
  **A 25-query smoke run has the flood ABOVE the flat arm** (+0.0188 against
  +0.0088) where the literal said it lost. 25 queries decides nothing; the real
  run has not been done and no flood number should be cited until it is.
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
