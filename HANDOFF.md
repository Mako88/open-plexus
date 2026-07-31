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

**Written:** 2026-07-31, after the session that added a third modality and unblocked
kill-list item #1.

---

## WHERE TO START READING

**`docs/explainers/36-a-shelf-with-only-two-slots.md`.** It is the plain-language version
of the single idea that three of this session's runs converged on, and it is the fastest
way back in. Then `g36-04` and `g37-01`.

---

## THE ONE IDEA THAT CAME OUT OF THIS SESSION

**A bound is a BUDGET, and a hub's budget limits how many spokes it can be MUTUAL with.**
Three runs found the same wall from three directions and none of them recognised it alone:

    g33-02   a single global `k` cannot express a hub with spokes
    g36-05   two senses that always co-occur EVICT the word from each other's list
    g36-06   softening the denominator does not repair it -- it FRAGMENTS instead

**No choice of statistic changes the size of the budget**, which is why `damped(alpha)`
failed. The untried thing the measurements motivate is a bound that is per-KIND rather
than one number per surface — a word keeping more partners than a picture because it is a
word. **Registered, not built**, and deliberately: it is the first proposal in this line
the data pushes toward rather than merely permits.

---

## WHAT HAPPENED

### A third modality, on real audio — `g36-04`, `g36-05`, `g36-06`

`openplexus/tasks/spoken.py` reads the Free Spoken Digit Dataset with stdlib `wave`, so
the ruler stays dependency-free. 3,000 recordings, six speakers, CC BY-SA 4.0, gitignored.

**Adding a whole sense cost a reader and a feature function.** The quantiser is the SAME
`grouping.cluster` the images use — `harness.quantise` takes pixels and spectra through one
call. Counting, walk, bound, sharding and containers were untouched, because the mechanism
does not know how many modalities exist. That is the load-bearing observation about the
architecture, more than any single number.

**The headline is a sign flip nobody predicted.** Two senses that share ZERO occasions
reach each other through a shared word better than two senses that share every occasion,
and better than one sense alone. **Interleaved helps; simultaneous harms.**

**And the linking is not limited by front-end quality** over the range tested: the audio
quantiser is measurably worse at its own job and produced the table's best link. `g36-01`
reached the same conclusion from the other side. **This changes what a learned quantiser
would be FOR** — see `docs/options/learned-codebook.md`.

### Kill-list #1 is UNBLOCKED, and it was our own record blocking it — `g37-01`

`docs/options/clutrr-symbolic.md` said *"published TEXT numbers are not comparable"*. True.
It was read as *"no published CLUTRR number is comparable"*, which is false: the standard
evaluation is the noiseless GRAPH-based version on exactly the split already fetched here.

**Rule 1's borrowed-claim failure, with the borrowing happening inside the repository.**
Filed under *established*, upstream of a decision, unreachable by anything downstream.

`g37-01` computes the honest floor from the data. Against published graph-only references
the band is several times `closure`'s. **`g37-02` is the G0 control that follows** —
frozen substrate and a measured strong reference, because a CITED reference is not a
measured one.

### Two of John's design notes are recorded — `docs/options/learned-codebook.md`

The request to try a learned quantiser, and the **edge-machine architecture**: a request
routes through an edge machine holding the quantiser, which converts the input, sends it to
the network, and returns the response. That makes the quantiser an EDGE concern rather than
a per-node one, which is a smaller and better-posed problem. The C1 question it leaves open
is recorded beside it.

---

## THE KILL LIST

     ✅  2  representations learned LOCALLY   18 graphs, beats counting, no invariant
     ✅  6  independent nodes agree           TRANSPORT half exact across containers.
                                              Quantiser half UNTESTED
     ✅  7  decide what to say, and decline   exact, on the case the gate can see

     🔀 10  margin survives scale             refutation was on the wrong arrangement

     ⏸  4  multi-hop walk over real internet  got WORSE: 5.09 s per grounded question

     ⬜  1  relational objective buys reasoning UNBLOCKED. Instrument found, floor
                                               measured, G0 control in flight
     ⬜  3  conventional system already wins    external stimuli run; no human opponent
     ⬜  5  learn forever                       the cheap route is refuted
     ⬜  8  adjudicate contradictions           untouched
     ⬜  9  survive hostile participants        untouched
     ⬜ 11  training traffic fits broadband     writes cheap; READS scale with fan-out
     ⬜ 12  survives a second modality          THREE modalities now run on real
                                               sensory data. G7 still NOT passed

**#12 is much closer and is not passed.** Every arm has the word present throughout, so
nothing yet introduces a concept through one modality and queries it through another. That
run is the obvious one and is not done.

---

## WHAT IS BUILT

    openplexus/tasks/occasions.py   the instrument: a stream with known ground truth
    openplexus/tasks/xsl.py         29 PUBLISHED conditions, external stimuli
    openplexus/tasks/mnist.py       images, IDX, dependency-free
    openplexus/tasks/spoken.py      audio, WAV, dependency-free
    openplexus/tasks/clutrr.py      someone else's relational benchmark
    openplexus/grounding.py         counts, statistics, `damped`, cliff, walk, scoring
    openplexus/buckets.py           the time-bucket join, one process
    openplexus/federated.py         the table split by owner, every crossing counted
    openplexus/bucket_service.py    ONE node's share, refusing every key it does not own
    openplexus/bucket_peer.py       that over TCP
    testbed/run.py --mode bucket    containers under tc netem

---

## WHAT I GOT WRONG, so it is not re-derived

**A prefix of `spoken.available` is one digit, not a sample.** FSDD filenames begin with
the digit, so the first half of the files are digits 0-4. A probe reported purity 0.7093,
which looks fine. **The tell was the CHANCE level printing at 0.20 instead of 0.10**, not
the purity. `sample` exists for this and `mnist.read` says why a prefix is safe there.

**`count` and `local_conditional` are ONE arm.** For a fixed surface, dividing every
candidate by that surface's own count divides by a constant. With `g32-01`'s
`ppmi == conditional`, the five named statistics are **three** distinct rankings. Third
instance of this failure in this line; the check is arithmetic before dispatch.

**A prediction criterion can be satisfied by a collapse.** `g36-06`'s P2 asked for word
survival above 0.80 with distractor admission below 0.05. A cell met it exactly — at a
collapsed graph. **The criterion should have carried the class-size guard and did not.**

**A `cross` of 1.0000 on `crossed` 0.0333 is one lucky pair, not a perfect score.** The
companion column is what caught it.

---

## PROCESS

- **CI is blocking and has caught real bugs.** Watch it; treat red as blocking.
- **`checks` takes 45-80 minutes** (six mutation shards). Batch commits.
- **A heredoc containing `\n` inside a Python string breaks `mutate.py`.** Use the Edit
  tool. And a `cat > file <<'EOF'` heredoc in the Bash tool failed outright on a long
  Python file this session — the Write tool is the reliable route.
- **`check_provenance` earned its keep again**, refusing an option-record entry that cited
  a script which did not exist yet. The fix was to build the script.
- **`check_rails` R3 caught a new experiment parsing its own arguments.** The guard
  `refuse_if_mutating()` is now called explicitly in it.

---

## STATE

`g37-02` was RUNNING at the time of writing — the G0 control on CLUTRR, four arms, three
seeds, about 15-25 minutes local. Its predictions are committed at `0ce95a4`, before
execution. **Check `experiments/sweeps/g37-02-does-clutrr-pass-g0.txt` for whether the
results were ever filled in**; if its status still says `run pending`, the run did not
report and it should be re-run rather than assumed.

1,560 tests green, 233 mutations verified, all seven checks passing.

`data/` holds clutrr, fb15k237, fsdd, kachergis, mnist, openea and tinyshakespeare, all
gitignored. Re-fetch on a fresh clone with the `tools/fetch_*.py` scripts.
