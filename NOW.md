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
**Report `busiest`, not `messages`** — serial seconds are TOTAL work;
`busiest()` is what a distributed run costs, 9.5× to 22.4× less.
## THE FLOOD IS THE DIRECTION — John's call, 2026-08-02

**Taken on architecture, not on the numbers**, and he said so: it matches the
incumbent and does not beat it, and what it has is a shape that survives
distribution. Recorded so it is not re-litigated when a cell comes back level.
**Its real advantage over targeted routing** — `federated.at(owner(surface))`,
which is C1-legal and far cheaper — is that it needs no ADDRESS: for "what is
this thing I am sensing", you cannot route, because you do not know what you
are looking for.

His design: a node converts an input and broadcasts it **with the id of the
machine the answer should return to**; every node holding a match fires and
**reports how many it expects to send**.

**Departure costs the ANSWER but not the ACCOUNTING, at both ends.** A node
gone before the walk arrives is never visited. One that dies after passing a
route on has already put its contribution in the message. One that dies
mid-processing loses only its own subtree — which is what losing it earlier
would have lost too. **The only thing that differs is whether the origin knows
the thought is over**, and that is exactly what decision 9's deadline is for.

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

John asked how a chosen chain becomes an action. **It does not.** Nothing in
`openplexus/` acts, emits, renders or drives anything; `flood` hands back
`reached: {endpoint -> Arrival}` and stops, and **the three-step ranking is
🚧, not built** — he thought it was. Every action anywhere in this project is
an experiment's `rng.randrange` or a `snake_curiosity` policy. **No chain has
ever caused anything.**

Two designs, and the agreed one is a hybrid: **outputs as nodes**, where a
chain reaching one fires it and arrival IS the decision; or **everything
returns to the origin**, which ranks and commits — which is where an "answer"
comes from. Agreed: arrival NARROWS to the chains reaching the named output
machine, and prediction ranks among those.

**4 🚧 in the README, checked 2026-08-02** so none goes quiet: *any node is an
input or an output, machines carrying the addresses*; and *which chain to
render* with its two live steps, *arrival narrows* and *prediction ranks*.

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

6. **The front end should be as WEAK as possible** — `surfaces.py` already
   argues it: *clustering by similarity is an identity assignment, and identity
   is the walk's job.* Its only mandate is to make recurrence possible. **The
   ensemble is the right shape for this** — coarse codes meaning almost
   nothing, the combination left to the graph.

   **John's counter, accepted:** k-means only ever sees one modality, so it
   cannot connect a picture to a sound — **the cross-modal claim is untouched
   by it.** It takes over within-modality identity, never the interesting
   claim. And with one front end on both arms, a walk beating random is
   attributable to the walk: **relative claims are safe, absolute ones are
   not.**

**Waiting on John**: whether ARC-AGI-3 is next — counting needs recurrence and
ARC withholds it by design.

**Preference**: three honest sources exist; reward is rejected and curiosity
loses to random, so homeostasis is the one left. Action 1 builds it.

**Ensemble front end**: built; budget pins at the top of its grid. Sweep
higher, then three seeds at the interior maximum.
**`--repeats` may do nothing**: it replays the same recordings. Unmeasured.
**Columns**: neighbour-conditioned works. Left: a SUMMARY of the neighbours
rather than the single one in the direction of travel.
**Snake**: no multimodality yet — vision, plus hearing in `snake_hearing.py`;
action and interoception designed, not built. Nothing beats random play.
## Prediction and forgetting: four holes

- **Prediction error does not drive the asking**; **the half-life is unswept**;
  **eviction has no policy**, nothing measuring memory pressure.
- **Automatic dial tuning** designed, not built: a node scoring candidates for
  its own dial against its own prediction error. **Risk**: minimising surprise
  is won by never looking at anything surprising — score error per observation
  MADE.

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

- **`deployment.py`, `agreement.py`, `tasks/xsl.py` are dead** — tests only;
  **`experiments/` has fifteen scripts and no harness**; **the written
  channel's dials were chosen, not measured**.
- **§5's "refuse when nothing was written" is unverified** — every refusal in
  the package is an ownership refusal instead.

**Reading leads, unread**: predictive coding (the dark room is its named risk);
interventional causal discovery under a budget; AnyBURL, near 0.31 to our 0.247.
