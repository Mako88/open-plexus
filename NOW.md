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

**Which chain to render.** Agreed in principle — arrival narrows, prediction
ranks, brevity breaks ties — and nothing is built. The prediction half now
exists, so the ranking step has a mechanism for the first time.

**Whether ARC-AGI-3 is next.** Snake was built as the step before it and has
produced results. The objection stands: counting needs recurrence and ARC
withholds it by design.

## THE GAP JOHN FOUND: nothing in this design wants anything

His question, 2026-08-02: is there any reason to pursue the fruit? **There is
not.** `food` is a column measuring an accident — `committed` eats the most (52)
because it blunders furthest, not because it is trying.

**A score and an energy are different things and only one is foreign here.** A
score is external: something outside decides what is good and tells the system.
An energy depletes and food restores it, and nothing says being full is good —
it is a SENSATION, an interoceptive channel like `ate` and `died`.

**But energy alone still supplies no preference.** A system that feels hunger
and predicts it perfectly is indifferent to being hungry; a perfect predictor is
content in any state it can foresee. Only three honest sources exist:

- **External reward** — rejected by the design.
- **Homeostasis with real consequences** — if running out ENDS the stream, then
  policies that do not eat generate less experience. That is selection, not
  preference. **It does not exist here**: death resets and the run continues, so
  there is no pressure at all. It would need a run that can genuinely end, or a
  population.
- **Curiosity** — an intrinsic preference for learning. Built, measured, and it
  loses to random in every form tried.

**Recommended, and not built**: energy as an input-only channel, because
hunger correlates with time-since-food and that is predictable structure, which
is what this system eats. **Not a score.** And "does it pursue the fruit" stays
the wrong question until something supplies preference — the right one is "does
it learn the world", which held-out prediction already measures.

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

**Named risk before building**: binding on neighbours multiplies the state space
by the neighbour alphabet, and `bound` is already multiplicative. The cheap
version conditions on a SUMMARY of the neighbours rather than their identities.

## Snake: built, and what it cannot show

`tasks/snake.py`, `experiments/snake_prediction.py`, `snake_surfaces.py`.

- **Open space teaches nothing.** A centred view of a featureless region is the
  same view whichever way you went, so board size has to be chosen relative to
  sight and most steps of a large board are wasted. Unmeasured how much.
- **No multimodality yet.** The occasion carries vision only. Action and
  interoception — ate, died, length — are designed as their own kinds in one
  `SharedGraph` and are not built. That is what would make the stream
  time-synced by construction rather than by alignment.
- **Random play only.** Nothing chooses actions, so nothing tests whether acting
  to disambiguate beats watching — the reason an interactive world was wanted.

## Prediction: built, and the two holes it has not closed

`openplexus/prediction.py`. Prequential; `bound` beats `factored` 0.717 to
0.437 over three seeds, shuffled control 0.005.

- **Prediction error does not drive the asking yet.** The second hole the one
  mechanism was meant to close. The asking budget is still a fixed fraction.
- **Automatic dial tuning is designed and not built.** A node scoring a small
  set of candidate values for its own dial, prequentially, against its own
  prediction error — C1-legal because local, C4-legal because it never ends.
  **Named risk**: a system minimising surprise can win by never looking at
  anything surprising, so error must be scored per observation MADE.

## Forgetting: built, one half untested

`CoOccurrence(half_life=...)`, off by default. Decay on read, the clock is the
node's own occasions, `evict`/`reinstate` with a boost.

- **Nothing has swept the half-life**, and it is now known to change answers
  rather than only memory, so it needs sweeping like any other dial.
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
- **The link columns in `surfaces_pipeline.py` step in tenths.**
- **`experiments/` has eleven scripts and no harness.**
- **§5's ⬜ "refuse when nothing was written — the machinery exists" is
  unverified.** Every refusal in the package is an ownership refusal or the
  asking experiment's detachability rate. Neither is that.
- **The written channel's dials were chosen, not measured**, and
  `--silence/--mistake/--corrupt` exist so they can be swept. Nothing has.

## Reading leads, none of them read

- **Predictive coding** — now the most relevant, since prediction exists and the
  dark-room failure is the named risk against tuning on it.
- **Interventional causal discovery under a budget.** The sharper question:
  when does structure say what you need not test?
- **AnyBURL / rule mining over paths** — a rule-over-paths system lands near
  0.31 where ours lands at 0.247, so our implementation is the limit.
