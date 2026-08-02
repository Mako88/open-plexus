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
and many-origins have been re-run; surprise/rarity refuelling has not.

**Report `busiest`, not `messages`.** Nothing runs in parallel; serial seconds
are TOTAL work. `Flood.busiest()` is what a distributed run would cost —
9.5× to 22.4× less. Every cost reported this session led with the wrong column.

## THE FLOOD IS THE DIRECTION — John's call, 2026-08-02

**Taken on architecture, not on the numbers**, and he said so: it matches the
incumbent and does not beat it, and what it has is a shape that survives
distribution. Recorded so it is not re-litigated when a cell comes back level.

His design, fuller than the README carries: a node converts an input and
broadcasts it **with the id of the machine the answer should return to**; every
node holding a match fires and **reports how many it expects to send**.

**A node that vanishes BEFORE the walk reaches it costs nothing** — his point,
and right: the walk never goes there, the answer is poorer, the accounting is
untouched. The real case is one that vanishes **holding a live route**, whose
death report never arrives. That is what decision 9's deadline is for.

## PRIORITY, REORDERED BY JOHN 2026-08-02

1. **Make `broadcast.flood` actually parallel.** It is the architecture and a
   serial simulation is not testing it. **Honest scope**: `busiest()` already
   measures what a parallel run would COST — 9.5x to 22.4x less than serial.
   What a real implementation adds is **C2 and C3**: late messages, lost
   messages, nodes vanishing mid-thought. That is untested and is the part that
   could break the design. `bucket_peer.py` and `node_main.py` exist for it.

2. **Turn death on in snake and re-run everything policy-related.** The old
   numbers are meaningless with nothing to lose.

3. **k-means: a reference ceiling, not a candidate.** A seeded codebook does
   not work. Random centres from a shared seed are legal but probably no better
   than the hash. The ensemble front end is the better bet AND keeps both of
   the walk's jobs.

## AGREED 2026-08-02, NOT BUILT

3. **Split the anchor as well as the codebooks.** The disagreement result had
   one clean word per digit shared by both codebooks by construction; a real
   federation has none. This decides whether k-means is genuinely legal.

4. **Many snake games into one graph.** The win is EVIDENCE, not parallelism:
   N× the occasions per code, the axis that made the senses graph unwalkable.
   It also tests interleaved independent sources writing to one graph, which is
   what a federation is. **`Flood` has no broadcast id**, so two thoughts in
   flight would mix their routes and their death counts.

5. **k-means with per-node codebooks, and with groups sharing one.** John wants
   this tried rather than parked — his theory is that k-means may simply be a
   better way to identify a thing, and the disagreement result says the walk
   survives it. **His own constraint: it must not help the system cheat.** The
   test that decides it is splitting the ANCHOR too, since the measured result
   had one clean word per digit shared by both codebooks.

6. **The front end should be as WEAK as possible** — `surfaces.py` already
   argues it: *clustering by similarity is an identity assignment, and identity
   is the walk's job.* Its only mandate is decision 1's ❌ for
   no-discretisation: make recurrence possible. **The ensemble is the right
   shape for this reason** — coarse codes meaning almost nothing, the
   combination left to the graph.

   **John's counter, accepted and it narrows mine:** k-means only ever sees one
   modality, so it cannot connect a picture to a sound — **the cross-modal
   claim is untouched by the quantiser.** What it does take over is
   within-modality identity, which was never the interesting claim. And with
   the same front end on both arms, a walk beating random is attributable to
   the walk: **relative claims are safe under any quantiser, absolute ones are
   not.**

**Waiting on John**: which chain to render (agreed in principle, nothing
built); whether ARC-AGI-3 is next (counting needs recurrence, ARC withholds it).

**Preference**: three honest sources exist; reward is rejected and curiosity
loses to random, so homeostasis is the one left. Action 1 builds it.

**Ensemble front end**: built; budget pins at the TOP of its grid. Sweep
higher, then three seeds at whatever interior maximum appears.

**`--repeats` may do nothing**: it replays the same recordings, and repeated
identical evidence does not sharpen a ratio. Unmeasured.

**Columns**: neighbour-conditioned works. Left: conditioning on a SUMMARY of
the neighbours rather than the single one in the direction of travel.

**Snake**: no multimodality yet — vision, plus hearing in `snake_hearing.py`;
action and interoception designed, not built. Nothing beats random play.

## Prediction and forgetting: four holes

- **Prediction error does not drive the asking**; **nothing has swept the
  half-life**, now known to change answers; **eviction has no policy** because
  nothing measures memory pressure.
- **Automatic dial tuning** is designed, not built: a node scoring candidates
  for its own dial against its own prediction error, local and never-ending.
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

- **`deployment.py`, `agreement.py`, `tasks/xsl.py` are dead** — tests only.
  **`experiments/` has fifteen scripts and no harness.**
- **DISTRIBUTED: entry point and in-process agreement done, container left.**
  Left: latency, departure, partition — which is priority 1.
- **§5's "refuse when nothing was written" is unverified**; every refusal in
  the package is an ownership refusal instead.
- **The written channel's dials were chosen, not measured.**

## Reading leads, unread

**Predictive coding** (the dark room is its named risk, measured);
**interventional causal discovery under a budget**; **AnyBURL**, near 0.31
where ours lands at 0.247.
