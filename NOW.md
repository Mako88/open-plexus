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

## Waiting on John

**Which chain to render.** Agreed in principle — arrival narrows, prediction
ranks, brevity breaks ties — and nothing is built. The prediction half exists
now, so the ranking step has a mechanism for the first time.

**Whether ARC-AGI-3 is next.** The objection stands: counting needs
recurrence and ARC withholds it by design.

## THE GAP JOHN FOUND: nothing in this design wants anything

His question, 2026-08-02: is there any reason to pursue the fruit? **There is
not.** `food` measures an accident — `committed` eats the most because it
blunders furthest, not because it is trying.

**A score is external and an energy is a sensation**, so only the first is
foreign here. But **energy alone supplies no preference**: a system that feels
hunger and predicts it perfectly is indifferent to being hungry. Three honest
sources exist and this design has none — external reward is rejected;
homeostasis needs running out to END the stream, and death here resets; and
curiosity loses to random in every form tried.

**Recommended, not built**: energy as an INPUT-ONLY channel, because hunger
correlates with time-since-food and that is predictable structure. **Not a
score.**

## THE SENSES GRAPH IS STARVED — settled, in the README. What follows

Every cross-modal null measured on it says nothing about any mechanism: it has
about 12 occasions per code where the incumbent walk needs sixty.

- **Nothing has tried to fix it.** More occasions, fewer codes, or more
  `--repeats`. Fewer codes costs purity, so it is a trade-off and **it is not
  known whether any setting of this corpus clears sixty per code.** That is
  the first thing to find out, and it decides whether the senses task is
  usable at all.
- **Every earlier senses result needs re-reading against this**, the
  word-channel comparison included — all of it ran in the starved regime.

## The live thread: columns that can read each other

**What was refuted is INDEPENDENT columns.** Nine overlapping 3×3 windows on
snake, one predictor each, none able to see another's surface — 0.510 on the
strict measure against a single whole-view code's 0.650. A window cannot see
what is about to enter it from outside.

**Nothing let one column inform another, and that is the one thing this
architecture is otherwise entirely about.** A column's surface is a surface; the
graph is what surfaces are for. The build is to let a column's prediction
condition on its neighbours' current surfaces as well as its own — a bound
triple again, which `Predictor` already holds.

**Named risk**: binding on neighbours multiplies the state space by the
neighbour alphabet. The cheap version conditions on a SUMMARY of them.

## Snake: built, and what it cannot show

`tasks/snake.py`, `experiments/snake_prediction.py`, `snake_surfaces.py`.

- **No multimodality yet.** The occasion carries vision, and hearing only in
  `snake_hearing.py`. Action and interoception — ate, died, length — are
  designed as their own kinds in one `SharedGraph` and are not built. That is
  what would make the stream time-synced by construction, not by alignment.
- **Nothing beats random play.** Five policies tried; the reason an interactive
  world was wanted is still unanswered.

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
  when to call it, because that is memory pressure and nothing measures memory.

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
  `node_main.py` runs a node on TCP; a `Federation` across 4 owners agrees with
  a whole `CoOccurrence` on every read, still at 32 owners. Left: latency,
  departure, partition. `testbed/driver.py` measures a deleted network.
- **`experiments/` has thirteen scripts and no harness**, and the link columns
  in `surfaces_pipeline.py` still step in tenths.
- **§5's ⬜ "refuse when nothing was written — the machinery exists" is
  unverified.** Every refusal in the package is an ownership refusal or the
  asking experiment's detachability rate. Neither is that.
- **The written channel's dials were chosen, not measured**, and
  `--silence/--mistake/--corrupt` exist so they can be swept. Nothing has.

## Reading leads, none of them read

- **Predictive coding** — now the most relevant, since prediction exists and the
  dark-room failure is the named risk against tuning on it.
- **Interventional causal discovery under a budget** — when does structure say
  what you need not test?
- **AnyBURL / rule mining over paths** — lands near 0.31 where ours lands at
  0.247, so our implementation is the limit.
