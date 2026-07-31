# HANDOFF — scratch context for a session swap

> **TEMPORARY and OVERWRITTEN, never appended to.** Not a record; nothing durable may
> depend on it. **Nothing else in the tree may cite this file.** Cite `DECISIONS.md` or a
> sweep record instead.
>
> **Where things live:** decisions → `DECISIONS.md`. An option's history →
> `docs/options/<name>.md`. A prediction, before a run → the sweep record. A finding about
> the METHOD → a `CLAUDE.md` calibration. The readable version → `docs/explainers/`. Goal
> and refutation conditions → `GOALS.md`. Notes are RETIRED in `docs/archive/notes/`.
>
> **NO CLAIM LIVES HERE.** Every number points at the file that owns it, and if the two
> disagree that file wins.

**Written:** 2026-07-31, at the end of the session that passed G7 and rebuilt the walk.

---

## THE NEXT SESSION IS KILL-LIST #1, AND EVERYTHING IT NEEDS IS READY

John, 2026-07-31: he is taking **#1 — does a relational objective buy reasoning** — into a
new session. **Read `docs/options/clutrr-symbolic.md`, the last entry**, which is written
as the handoff for exactly that and names the four traps this project has already stepped
in.

The short version: the instrument exists and its data is fetched, the band is real, and
**nothing in this repository can measure it** — `ShiftedAttention` fits its own training
data to 0.4185 at d128x16 and 0.4215 at d256x48, because it is single-pass and CLUTRR needs
composition. **The missing piece is one model.**

**After #1, John named INTERVENTION** — `docs/options/intervention.md`, written this
session. His reason, recorded there: interacting with the world is necessary for where this
is going, not incidental.

---

## WHAT HAPPENED: the walk was rebuilt and G7 passed

**G7 is PASSED** — `GOALS.md` §4 carries the verdict and its three limits. From an image
code the walk reaches audio codes of the same digit while the two share **zero** occasions,
with a word as the only route.

**The mechanism changed underneath it, and this is the load-bearing part.** John's
proposal, 2026-07-31: *"I don't think we want a ceiling at all."* Keep every edge, bound the
SEARCH instead of the representation. `grounding.reach` is that, and after four sweeps the
working rule turned out to be a **simplification**:

    score `conditional` from the query's own side, keep every edge,
    take the strongest, walk. No cut, no mutuality, no symmetrisation.

At convergence that is **0.9867** link at full coverage with the distractor refused
completely, against the incumbent partition's 0.9216.

### The five things that would change what you build next

**1. Symmetrising the edge is what admitted the distractor.** Five axes failed — three
scalar dials, the search budget, stream length — because none was the axis. From a word's
side its own codes score ~1.0 and an ever-present distractor ~0.28; from the distractor's
side everything scores 1.0, **because that is true**. Every combining rule mixed it in.
`forward` never sees it (`g39-03`, `g39-04`).

**2. Depth 1 is enough and a wide beam there is FREE.** 109 messages per query, flat in
beam, because scoring the candidates is the cost and expanding them is not. Depth 2+ is
worse AND dearer for a one-hop query (`g38-03`). **Depth 2 IS needed for cross-modal**,
which is two hops by construction (`g40-01`).

**3. The curve does not flatten until 12,000 occasions.** Every absolute figure taken at
3,000 is a lower bound. Arm-vs-arm comparisons at equal length survive; **"X does not help"
claims do not**, and that is the expensive class (`g39-01`, `g39-02`).

**4. Partial presence CANCELS; correlation is the real boundary.** A thing present 50% of
the time is refused exactly as one present 100% of the time, because `p` appears in both
terms of the ratio. A thing *correlated* with a concept is refused by **0.0096** — a 47-fold
collapse — and a stronger one crosses (`g39-06`). **No co-occurrence statistic can fix
that**, which is why intervention is registered.

**5. A cross-modal link costs an order of magnitude more exposure than a within-modal one.**
About 300 occasions per digit against `g32-02`'s ~16 (`g40-01`).

---

## THE KILL LIST

     ✅  2  representations learned LOCALLY   18 graphs, beats counting
     ✅  6  independent nodes agree           transport half exact in containers.
                                              QUANTISER half still untested
     ✅  7  decide what to say, and decline   exact, on the case the gate sees

     🔀 10  margin survives scale             refutation was on the wrong arrangement

     ⏸  4  multi-hop walk over real internet  5.09 s per grounded question

     ⬜  1  relational objective buys reasoning  blocker is now a MODEL to build
     ⬜  3  conventional system already wins     external stimuli; no human opponent
     ⬜  5  learn forever                        first prequential evidence, g39-01/02
     ⬜  8  adjudicate contradictions            untouched
     ⬜  9  survive hostile participants         untouched
     ⬜ 11  training traffic fits broadband      109 messages per query at depth 1
     ⬜ 12  survives a second modality           G7 PASSED. Three modalities, real data

---

## WHAT IS BUILT

    openplexus/grounding.py         counts, statistics, `damped`, `strength`,
                                    `reach`, cliff, the walk, scoring
    openplexus/tasks/spoken.py      audio, WAV, dependency-free
    openplexus/tasks/mnist.py       images, IDX, dependency-free
    openplexus/tasks/clutrr.py      someone else's relational benchmark
    openplexus/tasks/occasions.py   the synthetic instrument
    openplexus/buckets.py           the time-bucket join, one process
    openplexus/federated.py         the table split by owner
    openplexus/bucket_service.py    ONE node's share, refusing unowned keys
    openplexus/bucket_peer.py       that over TCP
    testbed/run.py --mode bucket    containers under tc netem

**`equivalence_classes` is NOT retired.** It stays as the measured alternative per rule
14c, so every earlier result remains reproducible.

---

## WHAT I GOT WRONG, so it is not re-derived

**Two corrections to `g38-01` in one day, both from measuring at a constant it had pinned.**
Its 0.30 advantage is 0.065 at convergence, and its *"`min` reaches nothing"* is a beam-8
artefact — at beam 16 `min` gives 0.9677 at 0.5933 coverage.

**A warning one run old did not prevent its own repeat, TWICE.** `g38-03` dropped the
companion coverage column the day after `g38-01` explained why it mattered. `g39-06` pooled
a confound over ten words the day after `g39-05` warned that a mean hides a single bad case
— it hid a 47-fold collapse. **Both were caught by re-reading my own notes, not by any
check.**

**A comparison between two ORDERINGS has to hold the data fixed.** `g40-01`'s first version
decided the phase split inline, so the two arms saw different pairings AND different noise,
and the 0.0435 gap looked like order-sensitivity. Building the occasion list once and
shuffling it makes them agree to four decimals.

**`strength`'s original justification was wrong and a test caught it on the first run.** It
was introduced as soft mutuality with a doubt attached; the doubt was right about `min` and
the answer lay off that axis entirely.

**`check_decisions` has a latent gap, found by accident.** The `Shard the count table` row
was passing because its evidence block swallowed text from BELOW it; inserting a new option
after it made the row fail. The row is fixed. **Whether other rows pass the same way is
unchecked** and is worth a look.

---

## PROCESS

- **`checks` takes 45-80 minutes** (six mutation shards). Batch commits.
- **The Bash tool's heredocs fail on long Python files.** Use the Write tool. This cost
  three retries this session and `CLAUDE.md` already warns about it for `mutate.py`.
- **`check_provenance` earned its keep four more times**, every time refusing a number
  quoted under a source that does not contain it — including a script that did not exist
  yet, which was the right complaint and the fix was to build it.
- **A status file plus a persistent monitor works well.** The monitor tails
  `scratchpad/STATUS.txt` and emits on change, so it never needs restarting.

---

## STATE

Clean tree, all seven checks passing, **1,579 tests green, 237 mutations verified.**
Everything pushed.

`data/` holds clutrr, fb15k237, fsdd, kachergis, mnist, openea and tinyshakespeare, all
gitignored. Re-fetch on a fresh clone with `tools/fetch_*.py`.
