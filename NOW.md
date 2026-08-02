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

## The broadcast flood: built, measured, and null

`openplexus/broadcast.py`, called by `experiments/senses_broadcast.py`. The gate,
the pricing and both refutations are settled and in the README. 15 tests, 4
mutations, all caught; preflight green.

**What is left, and it is the only live repair**: an origin's stamina scaled by
how much that origin predicts, so a specific surface funds a long thought and a
hub funds a short one. The gate governs which EDGES a route walks and says
nothing about where a route STARTS, and the origins that hurt were the word
hubs. Not built, and worth building only if the direction is being kept.

**A second possibility, untried**: the senses graph may simply be too dense —
169 nodes at mean degree 40.7, density 0.24, and the forward weights have a
median of 0.0119 against a max of 0.5. A flood needs somewhere not to go.

## The word channel: repaired and measured. What is LEFT

Settled and moved to the README. The run is `out/word-channel-comparison.txt`
and the three JSON files beside it; `--words label` stays the default so every
earlier number is reproducible.

**Unfinished:**

- **The dials are chosen, not measured.** `silence 0.15 mistake 0.05
  corrupt 0.30`, swept by `--silence/--mistake/--corrupt`, and nothing has.
- **Nobody has asked what the byte channel is worth on its own terms.** Every
  column here was built to score a channel of multiplicity 1. `link_img` counts
  surfaces per word NODE, so a channel with 72 nodes per digit is measured on a
  denominator that means something different — the comparison is sound in
  direction and unsound in units.
- **Order is discarded.** `features` is a bare byte histogram, so `three` and
  `there` are one surface. The obvious extension, and it changes the SPACE
  rather than the allocation, which is where §1 says such changes land.

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

- **FIXED, verdict survived, and it is in the README now.** Both depths run
  matched; the flood loses at both and deeper is worse.
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
