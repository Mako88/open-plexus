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

**UNDERPOWERED**: senses runs used n=150 where +0.03 needs 840. Only the flood
and many-origins were re-run; surprise/rarity refuelling was not.
**Report `busiest`, not `messages`** — serial seconds are TOTAL work.
## THE FLOOD IS THE DIRECTION — John's call, 2026-08-02

**Taken on architecture, not the numbers**: it matches the incumbent and does
not beat it, and what it has is a shape that survives distribution. Recorded so
it is not re-litigated when a cell comes back level. **Its advantage over
targeted routing** — `federated.at(owner(surface))`, C1-legal and far cheaper —
is that it needs no ADDRESS: for *what is this thing I am sensing* you cannot
route, because you do not know what you are looking for.

His design: a node converts an input and broadcasts it **with the id of the
machine the answer should return to**; every node holding a match fires and
**reports how many it expects to send**.

**Departure costs the ANSWER but not the ACCOUNTING.** Gone before the walk
arrives, never visited; dies after passing a route on, its contribution is
already in the message; dies mid-processing, loses only its own subtree, which
losing it earlier would have lost too. **Only whether the ORIGIN KNOWS the
thought is over differs** — action 7 is John's fix for that.

## PRIORITY, REORDERED BY JOHN 2026-08-02

1. **Make `broadcast.flood` actually parallel.** A serial simulation is not
   testing the architecture. **Scope**: `busiest()` already measures what a
   parallel run would COST. What a real one adds is **C2 and C3** — late
   messages, lost messages, nodes vanishing mid-thought — and that is the part
   that could break the design. `bucket_peer.py` and `node_main.py` exist for
   it. Python cannot do it in-process; the GIL is why.

2. **Turn death on in snake and re-run everything policy-related.**

3. **k-means, experimented with rather than parked** — see action 5.

## NOTHING TURNS A CHAIN INTO AN OUTPUT — checked, not remembered

**Nothing in `openplexus/` acts, emits, renders or drives anything.** `flood`
hands back `reached: {endpoint -> Arrival}` and stops; **the three-step ranking
is 🚧, not built.** Every action in this project is an experiment's
`rng.randrange` or a `snake_curiosity` policy — a hand-written function reading
`predictor.seen()`, not a chain arriving anywhere. **No chain has ever caused
anything.**

Agreed shape: arrival NARROWS to the chains reaching the named output machine,
prediction ranks among those. Outputs-as-nodes alone would make arrival the
decision and commit to nothing.

**4 🚧 in the README, checked 2026-08-02** so none goes quiet: *any node is an
input or an output, machines carrying the addresses*; and *which chain to
render* with its two live steps, *arrival narrows* and *prediction ranks*.

7. **An event bus, and a death event on disconnect.** John's answer to the one
   departure case that strands a thought: every node subscribes to a bus, and a
   node LEAVING the bus fires a death event of its own. The origin can then
   wait for every route to return or die without a deadline guessing for it.
   **Tangent worth its own experiment**: several buses segment the graph, and
   one bus per modality is a way to implement columns that costs nothing.

8. **C# is a NEW ATTEMPT, not a port — John, 2026-08-02.** Branch `csharp`:
   same goal, tighter scope, C# so he is comfortable in his own project and
   involved from the start. **`master` is untouched and IS the fallback.**
   Nothing migrates; what crosses is the lessons, not the code.

## AGREED 2026-08-02, NOT BUILT

4. **Many snake games into one graph.** The win is EVIDENCE, not parallelism:
   N× occasions per code, the axis that made the senses graph unwalkable. It
   also tests interleaved sources writing to one graph, which is what a
   federation is. **`Flood` has no broadcast id**, so two thoughts in flight
   would mix their routes and their death counts.

5. **k-means with per-node codebooks, and groups sharing one.** John's theory
   is that it may simply be a better way to identify a thing, and the
   disagreement result says the walk survives it. **His constraint: it must not
   help the system cheat.** What decides it is splitting the ANCHOR too — the
   measured result had one clean word per digit shared by both codebooks.

6. **The front end should be as WEAK as possible** — `surfaces.py` argues it:
   *clustering by similarity is an identity assignment, and identity is the
   walk's job.* **The ensemble is the right shape**: coarse codes meaning
   almost nothing, the combination left to the graph. **John's counter,
   accepted:** k-means sees one modality, so it cannot connect a picture to a
   sound — the cross-modal claim is untouched by it, and it only takes over
   within-modality identity. With one front end on both arms, **relative
   claims are safe and absolute ones are not.**

**ARC-AGI-3 answered**: branch `csharp`'s target, not this one's, and it starts
on SNAKE so a bad result cannot be the environment. **The recurrence objection
is untested, not refuted** — it was written about ARC's few-shot grids.

**Preference**: three honest sources exist; reward is rejected and curiosity
loses to random, so homeostasis is the one left. Action 1 builds it.

**Ensemble front end**: budget pins at the top of its grid; sweep higher.
**`--repeats` may do nothing**: it replays the same recordings.
**Columns**: neighbour-conditioned works. Left: a SUMMARY of the neighbours.
**Snake**: no multimodality yet — vision, plus hearing in `snake_hearing.py`;
action and interoception designed, not built. Nothing beats random play.
## Prediction and forgetting

**Prediction error does not drive the asking**; **the half-life is unswept**;
**eviction has no policy**. **Automatic dial tuning** designed, not built — a
node scoring candidates for its own dial against its own prediction error.
**Risk**: minimising surprise is won by never looking at anything surprising,
so score error per observation MADE.

## Forgetting: built, one half untested

`CoOccurrence(half_life=...)`, off by default.

- **Nothing has swept the half-life**, which is now known to change answers
  and not only memory, so it needs sweeping like any other dial.
- **Eviction has no policy.** `weakest` ranks by what is left; nothing decides
  when to call it, because nothing measures memory pressure.

## Decided

- **No tokenizer**; **facts are dropped**, not islanded; **no pre-commit hook**.
- **Mutations run in CI, sharded six ways.** Locally use
  `--only <the ones just added>`; `--changed` let a live mutation reach a commit.

## Known debts

**`deployment.py`, `agreement.py`, `tasks/xsl.py` are dead** — tests only.
**`experiments/` has fifteen scripts and no harness.** **The written channel's
dials were chosen, not measured.** **§5's "refuse when nothing was written" is
unverified** — every refusal in the package is an ownership refusal instead.

**Reading leads, unread**: predictive coding (the dark room is its named risk);
interventional causal discovery under a budget; AnyBURL, near 0.31 to our 0.247.
