# Now

What is being worked on, and what has been agreed but not started.

**The invariant:** every 🚧 in [README.md](README.md) appears here, and nothing
appears here that is not in the README. An approved piece of work cannot go
quiet, which is how the LSH front end was agreed and then dropped for two
sessions.

**A finding updates a line; it never appends one.** And a finding SPLITS: the
claim goes to the README, the numbers stay with the run in `out/`. "Settled
results belong in the README" was the wording here and it is wrong — it invites
whole tables into prose, which is what `CLAUDE.md` refuses and what the record
check keeps catching. This file carries only what is unfinished. Delete a line
when it is done. Nothing may cite it. Rewritten at the end of every turn — see
`.claude/skills/monitor`.

---

## THE SENSES RUNS WERE UNDERPOWERED — n=150 where +0.03 needs 840

John's question: too many things that should have helped have not. The flaw is
arithmetic. The seed spreads reported all session, 0.038 to 0.330, are its
signature, and every senses measurement is null where every snake measurement
works. **Nothing has been re-run.**

## THREE REFUTATIONS TO RE-RUN, and they are all the broadcast

John's question: what did we abandon on a failed test that may now be
hindering us? Audited against the README's 33 ruled-out entries. Seven rest on
senses-graph numbers; three of those are small effects measured at 120-150
questions where +0.03 needs 840, under the hash, before the rank-blind metric
was fixed:

- **the broadcast flood as a cross-modal walk**
- **refuelling on surprise, and valuing arrivals by rarity**
- **many origins in place of edge kinds** — and the ensemble front end now
  supplies far more origins than the four that were tested

**All three are John's design, and none has had a properly powered test.**
Re-run at ~1,000 questions before anything is built on their being dead.

The other four survive for reasons that are not about power: the k-means ❌ is
a C1 argument rather than a score, `MEAN` pricing was unbounded rather than
weak, and the stamina-scale and `cross 1.0000` entries are facts about spread.

## Waiting on John

**Which chain to render.** Agreed in principle — arrival narrows, prediction
ranks, brevity breaks ties — and nothing is built. The prediction half exists
now, so the ranking step has a mechanism for the first time.

**Whether ARC-AGI-3 is next.** The objection stands: counting needs
recurrence and ARC withholds it by design.

## Nothing in this design wants anything

John's finding: there is no reason to pursue the fruit, so `food` measures an
accident. A score is external and an energy is a sensation, so only the first
is foreign — but **energy alone supplies no preference** either. Three honest
sources exist and this design has none: reward is rejected, homeostasis needs
running out to END the stream and death here resets, and curiosity loses to
random in every form tried.

**Recommended, not built**: energy as an INPUT-ONLY channel. Not a score.

## THE ENSEMBLE FRONT END: built, budget not swept

`experiments/ensemble_front.py`. Several coarse hashes per item instead of one
fine one — the first legal repair for the hash's deficit, since it fits nothing.

**Not a result yet.** The budget pins at the TOP of its grid: families=4 at
stamina 0.2 gives 0.2200 against a chance of 0.100, seeds 0.038/0.292/0.330. A
sweep at its edge has not swept and a spread containing chance is not a finding.
**Next: a grid that goes higher**, then three seeds at whatever interior maximum
it finds.

**`--repeats` may do nothing**, unmeasured: it replays the same recordings, and
repeated identical evidence does not sharpen a ratio. It has been used
throughout as though it added evidence.

## Columns: neighbour-conditioned works, settled in the README

Left: conditioning on a SUMMARY of the neighbours rather than one, which is
what would let a column read more than the direction it is heading.

## Snake: built, two gaps

- **No multimodality yet.** Vision, plus hearing in `snake_hearing.py`. Action
  and interoception are designed as their own kinds in one `SharedGraph` and
  are not built.
- **Nothing beats random play.** Five policies tried.

## Prediction: built, two holes open

`openplexus/prediction.py`, prequential. Results in the README and
`out/snake-prediction.json`.

- **Prediction error does not drive the asking yet.** The asking budget is
  still a fixed fraction.
- **Automatic dial tuning is designed and not built.** A node scoring a small
  set of candidate values for its own dial, prequentially, against its own
  prediction error — C1-legal because local, C4-legal because it never ends.
  **Named risk**: a system minimising surprise can win by never looking at
  anything surprising, so error must be scored per observation MADE.

## Forgetting: built, one half untested

`CoOccurrence(half_life=...)`, off by default.

- **Nothing has swept the half-life**, which is now known to change answers
  and not only memory, so it needs sweeping like any other dial.
- **Eviction has no policy.** `weakest` ranks by what is left; nothing decides
  when to call it, because nothing measures memory pressure.

## Decided

- **No tokenizer.** Its vocabulary is learned from a corpus we never saw.
- **Facts are dropped**, not islanded — a separate corpus sharing no referent.
- **No pre-commit hook.** Every red preflight so far was caught immediately.
- **Mutations run in CI, sharded six ways.** Locally the command is
  `--only <the ones just added>`; `--changed` is what let a live mutation reach
  a commit.

## Known debts

- **`deployment.py` and `agreement.py` are dead** — imported by nothing but
  their own tests, and `deployment.py` budgets predecessor-era `w × d`
  associative memory. **`tasks/xsl.py` has no caller.**
- **DISTRIBUTED: entry point and in-process agreement done, container left.**
  `node_main.py` runs a node on TCP; a `Federation` agrees with a whole
  `CoOccurrence` on every read. Left: latency, departure, partition.
- **`experiments/` has thirteen scripts and no harness**, and the link columns
  in `surfaces_pipeline.py` still step in tenths.
- **§5's ⬜ "refuse when nothing was written — the machinery exists" is
  unverified.** Every refusal in the package is an ownership refusal or the
  asking experiment's detachability rate. Neither is that.
- **The written channel's dials were chosen, not measured**, and
  `--silence/--mistake/--corrupt` exist so they can be swept. Nothing has.

## Reading leads, none of them read

- **Predictive coding**, and the dark room measured today as the named risk.
- **Interventional causal discovery under a budget** — when does structure say
  what you need not test?
- **AnyBURL** — lands near 0.31 where ours lands at 0.247.
