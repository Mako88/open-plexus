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

**Written:** 2026-07-31, after the session that built the grounding mechanism end to end.

---

## WHAT HAPPENED: the grounding line is BUILT and MEASURED

The previous handoff said *"build the grounding mechanism and test it"*. That is done, from
the statistic through to containers under `tc netem`. Eight sweep records, in order:

    g32-01  can counting separate "always there" from "is the thing"
    g32-02  how many occasions does a concept need
    g33-01  does the bucket join keep the signal
    g33-02  can the walk bridge modalities that never meet
    g33-03  what does one query cost in peer messages
    g33-04  does a per-surface bound fix the star
    g34-01  trials this project did not design
    g35-01  the grounding store in real containers
    g35-02  what a departure costs when nothing is replicated

**Read `docs/explainers/34` and `35` first** — they are the plain-language version and the
fastest way back in.

### The five things that would change what you build next

**1. Raw counting is refuted; a chance-corrected statistic repairs it.** A distractor
present every occasion costs `count` 0.3044 of f1 and costs `conditional` 0.0000
(`g32-01`). And the same failure arrives without anyone building a distractor: at `zipf`
2.0 the commonest concept becomes one, and **60 of 60** surfaces of the rarest concepts
have a different concept's surface as their best raw-count partner (`g32-02`).

**2. PPMI IS NOT DEPLOYABLE.** It divides by how many occasions the whole system has seen,
which no node can know without a collective. `conditional` gives identical rankings above
chance (`g32-01`, max difference **0.0007** over 96 cells) and needs only a peer read.
`federated._AtOwner` and `bucket_service._Borrowed` both REFUSE to supply the total rather
than approximating it.

**3. The read costs one peer message PER PARTNER**, not per `k` — 439 for one walk at 192
surfaces, flat at 2.6x fan-out (`g33-03`). The write path is cheap by comparison at 38.4
row updates per occasion. **The expensive half of this design is reading.**

**4. A single global `k` cannot express a hub with spokes**, which is the shape a word
naming a concept has. The derived per-surface bound (`grounding.cliff`) is the best arm in
8 of 9 cells and collapses in none (`g33-04`). **It needs a cliff**, and note 058 measured
real language as a slope where the rule's output is decided by floating point — but
`g34-01` found published experimental stimuli ARE bimodal, mean largest gap ~0.5.

**5. Multimodality is the redundancy.** With nothing replicated, losing half the network
still leaves **0.9596** of surviving surfaces connected to a true partner at 5 surfaces per
concept, against 0.5522 at 2 (`g35-02`). Replication is an improvement, not a prerequisite.

---

## THE KILL LIST — what could still stop this

> **Restored after being dropped.** The rewrite of this file for the grounding work left it
> out, which was a mistake: it is the only standing summary of what would kill the project,
> and a handoff without it hands over the work and not the risk.

     ✅  2  representations learned LOCALLY   18 graphs, beats counting, no invariant
     ✅  6  independent nodes agree           TRANSPORT half; grounding store now agrees
                                              EXACTLY across containers. Quantiser half ⬜
     ✅  7  decide what to say, and decline   exact, on the case the gate can see

     🔀 10  margin survives scale             refutation was on the wrong arrangement

     ⏸  4  multi-hop walk over real internet MEASURED again, and it got WORSE: a
                                              grounded question is 5.09 s impaired

     ⬜  1  relational objective buys reasoning  blocked: no instrument with a wide band
     ⬜  3  conventional system already wins     external STIMULI now run; no human opponent
     ⬜  5  learn forever                        the cheap route is refuted
     ⬜  8  adjudicate contradictions            untouched
     ⬜  9  survive hostile participants         untouched
     ⬜ 11  training traffic fits broadband      writes cheap; READS scale with fan-out
     ⬜ 12  survives a second modality           the WALK bridges; no second modality yet

**What moved today, and none of it is a new checkmark.**

**#6** gained its hardest evidence: the grounding store agrees with one process **exactly**
in real containers, clean and at 40 ms with jitter, on both the write and read paths
(`g35-01`, `g35-03`), and it runs in CI. The quantiser half is untouched.

**#4 got worse, not better.** `g24-01` measured 161 ms a round and John accepted it. A
grounded question over containers costs **5.09 s** with held connections (`g35-04`), which
is outside that ruling. The two dominant terms are named and one is now fixed.

**#11 has real numbers for the first time on this path.** Writes are cheap and flat — 38.4
row updates per occasion. **Reads are the problem**: one peer message per candidate
partner, scaling with FAN-OUT rather than with `k` (`g33-03`).

**#12 is closer than it was and is not passed.** `g33-02` showed the walk bridges surfaces
that never co-occur, and `g35-02` found multimodality is the redundancy under churn. But
there is still no second modality — those are symbol streams, and G7 needs a real one.

**#3 got its first external instrument for grounding** (`g34-01`, 26 of 29 published
conditions recovered exactly) — but it is external STIMULI, not a benchmark. **No
conventional system has been run against this.**

---

## WHAT IS BUILT, and where the boundaries are

    openplexus/tasks/occasions.py   the instrument: a stream with known ground truth
    openplexus/tasks/xsl.py         29 PUBLISHED conditions, external stimuli
    openplexus/grounding.py         counts, five statistics, cliff, the walk, scoring
    openplexus/buckets.py           the time-bucket join, one process
    openplexus/federated.py         the table split by owner, every crossing counted
    openplexus/bucket_service.py    ONE node's share, refusing every key it does not own
    openplexus/bucket_peer.py       that over TCP
    openplexus/node_main.py         OPENPLEXUS_MODE=bucket
    testbed/run.py --mode bucket    containers under tc netem
    tools/bucket_drive.py           the driver, which owns nothing

**Container identity runs in CI** — `.github/workflows/testbed-bucket-identity.yml`, at 2
and 4 nodes, green. `testbed-identity.yml`'s header records why that matters: the harness
was built, proved once, and left un-run for months.

---

## THE THREE THINGS NOT DONE, in the order I would take them

**1. THE READ PATH ACROSS CONTAINERS.** `g35-01` drives writes and reads marginals back; it
does not run the WALK over sockets. So `g33-03`'s cost is still an in-process count, and
the one quantity a user would feel — how long a grounded question takes — is unmeasured on
a real link. `g24-01`'s 161 ms a round is the figure to compare against.

**2. CONNECTION REUSE.** `bucket_peer` opens a socket per message, which is named as a
deliberate simplification at its own definition and measured at **96x** under 40 ms delay
(2.66s clean against 255.68s impaired, `g35-01`). P1 is registered there. **Do not quote
that 96x as a latency for the architecture** — it prices the simplification, not the design.

**3. REPLICATION AND REPAIR.** Anti-entropy as `partitioned.ConceptStore.lose` describes.
Now a ranked option rather than urgent, because of finding 5 above. The unmeasured
comparison: deliberate replica placement against simply having more modalities.

---

## WHAT I GOT WRONG, so it is not re-derived

**`HANDOFF.md` named the wrong repository, and my correction was also wrong.** The bakeoff
data is in `kachergis/XSLmodels`, not `word_learning_models`. But **zero conditions have
both a plain-text ordering and a human accuracy** — 8 of 64 rows carry one and none of
those name a `.txt` file. So `g34-01` is external STIMULI, not an external BENCHMARK, and
every file that could be quoted from says so. Human baselines need a pure-Python RData
reader, unbuilt.

**Mutual exclusivity would not fix `g34-01`'s three failures.** I said it would, in the
sweep record and to John. The four surfaces are a closed fully symmetric clique — two
assignments fit every observation and a one-word-one-object constraint keeps both. Nothing
recovers it. **It motivates building nothing.**

**Four metrics in this line read 1.0 under total collapse** — `reached_together`,
`partner_rate`, and `bridged` in two sweeps. Each is recall-shaped and each says so in its
own docstring now. Always report `largest` beside them.

---

## PROCESS, and what must not regress

- **CI IS BLOCKING AND IT CAUGHT A REAL BUG.** A peer could not be shut down on Linux:
  closing a socket another thread is blocked on inside `accept` wakes it on Windows and
  does not on Linux, so a "departed" node kept serving. **Any churn measurement over that
  harness would have been measuring nodes that had not gone.** Watch the run, treat red as
  blocking.
- **`checks` takes 45-80 minutes** (six mutation shards). Batch commits; hold a push while
  one is in flight rather than starving it.
- **A heredoc containing `\n` inside a Python string breaks `mutate.py`.** Three times this
  session. Use the Edit tool for mutation entries, not `sed`/heredoc rewriting.
- **`check_provenance` earned its keep three more times**, every time the same shape: a
  figure written under a citation that does not contain it. Once it was a new record entry
  inserted INSIDE an existing one, which orphaned a paragraph from its CONFIG block.
- **`check_duplication` caught a copied `_cell` between two experiment scripts** within
  minutes of the second being written. Extracted to `harness.occasions_cell`.

---

## STATE

Clean tree, 230 mutations verified, 1,526 tests green, all seven checks passing.
**Nine commits unpushed at the time of writing** — push them and watch both `checks` and
`testbed-bucket-identity`. No background processes, no `.mutate.lock`, no sweep in flight.

`data/kachergis/` is fetched and gitignored; re-run `python tools/fetch_kachergis.py` on a
fresh clone before `g34-01`.
