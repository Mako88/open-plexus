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

## THE POWERED RE-RUN — in flight, `out/powered-rerun.txt`

Six runs at 1,000 questions, `lsh` and `kmeans`, three seeds. It exists because
an audit of the README's 33 ruled-out entries found **three refutations resting
on 120-150-question senses runs** where detecting +0.03 needs 840: the flood as
a cross-modal walk, surprise/rarity refuelling, and many-origins. All three are
the broadcast line and none has had a powered test.

First cell: under `lsh`, flood-one 0.0871 and flood-many 0.1066 against chance
0.108. Still null under the hash, as the front-end finding predicts. **The
`kmeans` half is where the question is answered** and has not run. `flood-many`
costs 411s a seed and gives up on 0.999 of questions, so its budget is wrong
for a graph this dense.

## AGREED 2026-08-02, NOT BUILT — start here

1. **Snake gets an energy bar.** It depletes, fruit restores it, and running
   out ENDS the run rather than resetting. John's call, and his reasoning is
   the mechanism: what survives longer learns more, so a policy that does not
   eat generates less experience. **That is selection without a reward** — no
   external judge, nothing declaring food good. It is the smallest thing that
   gives the system something to lose, and without something to lose no
   preference is possible at all.

2. **Re-run the three broadcast refutations at 1,000 questions.** In flight;
   see below. `flood-many` needs its budget re-swept for a graph this dense
   before its cell means anything.

3. **Split the anchor as well as the codebooks.** The disagreement result had
   one clean word surface per digit shared by both codebooks by construction.
   A real federation has no such thing. This is the test that decides whether
   k-means is genuinely legal.

4. **THE FRONT END SHOULD BE AS WEAK AS POSSIBLE.** John's observation,
   2026-08-02, and it is already the project's own argument in `surfaces.py`:
   *clustering by similarity is an identity assignment, and identity is the
   walk's job.* So "k-means walks better than the hash" may mean **the walk is
   not doing its job**, not that the hash is bad — a good clusterer answers the
   question upstream where nothing can audit it. The front end's only mandate
   is decision 1's ❌ for no-discretisation: make recurrence possible so a
   statistic can form. **The ensemble front end is the right shape for exactly
   this reason** — many coarse codes, each meaning little, combination left to
   the counts. Reread today's "the front end is the bottleneck" against this.

## Waiting on John

**Which chain to render.** Agreed in principle — arrival narrows, prediction
ranks, brevity breaks ties — and nothing is built. The prediction half exists
now, so the ranking step has a mechanism for the first time.

**Whether ARC-AGI-3 is next.** The objection stands: counting needs
recurrence and ARC withholds it by design.

## Nothing in this design wants anything

John's finding. Three honest sources of preference exist and this design has
none: reward is rejected, homeostasis needs running out to END the stream and
death here resets, and curiosity loses to random in every form. **Action 1
above is the fix.**

**Ensemble front end**: built, budget pins at the TOP of its grid (0.2200
against chance 0.100, seeds 0.038/0.292/0.330). A grid that goes higher, then
three seeds at whatever interior maximum it finds.

**`--repeats` may do nothing**, unmeasured: it replays the same recordings and
repeated identical evidence does not sharpen a ratio.

**Columns**: neighbour-conditioned works. Left: conditioning on a SUMMARY of
the neighbours rather than the single one in the direction of travel.

## Snake: no multimodality yet (vision, plus hearing in `snake_hearing.py`;
action and interoception designed, not built), and nothing beats random play.

## Prediction: two holes open

- **Prediction error does not drive the asking yet.** Still a fixed fraction.
- **Automatic dial tuning is designed and not built.** A node scoring candidate
  values for its own dial against its own prediction error — local, so C1-legal;
  never ending, so C4-legal. **Named risk**: minimising surprise can be won by
  never looking at anything surprising, so score error per observation MADE.

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
