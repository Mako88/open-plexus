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

**Written:** 2026-07-31, at the end of the session that took kill-list #1 apart.

---

## THE PREVIOUS HANDOFF WAS WRONG ABOUT #1, AND THAT IS THE FIRST THING TO KNOW

It said the blocker was *"one model to build"* — a composing reference, because
`ShiftedAttention` cannot fit its own CLUTRR training data.

**That is true of the REFERENCE arm and false about the pipeline.** The pipeline already
answers CLUTRR's published test split. Re-running `tools/generation_delta.py --end-to-end`
reproduces `note 091` exactly: 1,146 puzzles, recovery 0.8770, delta 0.8578.

`g37-02`'s local arm looked catastrophic because it was **deliberately crippled** — one hop,
no search — to isolate the objective from the search mechanism. It is not the pipeline, and
its numbers are not a bound on it.

**Do not build a transformer.** Read `g41-01`, `g42-01`, `g43-01` first.

---

## WHAT #1 HONESTLY IS NOW

> **Given the chain's length, 0.9076 on the deepest bucket of a published split.**

That is real, and it is narrower than a headline. Two aids, both now priced:

**Aid 1 — the walk is handed `len(chain)`.** Worth **0.74**: `told` 0.9076 against
`wrong-1` 0.1649 at 10 hops, where the floor is 0.0588. One extra hop destroys it.
**It cannot currently be removed** — `g42-01`. → [record](docs/options/depth-free-walking.md)

**Aid 2 — *"deltas add"* was supplied by hand.** Worth the difference between **0.6061 and
0.9076**, and it **does not transfer**: FB15k-237 has no additive invariant over all 237
relations, over any of 30 sub-domains, or over its best-evidenced 2 to 128 — `g43-01`.
→ [record](docs/options/generation-delta.md)

**The decomposition is the single most useful table in the session** (width 256, beam 8,
10 hops, subset `all`, 8 seeds), and it lives in `g41-01`'s record:

    achievable floor                            0.0588
    walk + rule table, gaps UNFILLED            0.3613
    + a random relation in the gap              0.4632
    + a LEARNED relation vector                 0.6061
    + the hand-supplied additive invariant      0.9076

---

## THE ONE THING LEFT TO BUILD, and it is genuinely a build

**`halt_gate` pointed at a TRAVERSAL rather than at one read.** It is the only named
candidate for removing aid 1. It generalises zero-shot to an untrained depth at 0.992
(`092`) and has never been given a walk. Parked ⬜ UNTRIED in the tree.

What is already refuted, so it is not re-tried: selecting across walk lengths by endpoint
score (`g42-01`), and the norm-bias explanation for why that failed — ranking by cosine
changes nothing.

---

## FOUR THINGS THAT WOULD HAVE SAVED THIS SESSION TIME

**1. Sweep the constants before believing a number.** `d_model` was carried from `note 065`
into every CLUTRR figure and was worth **0.71** between the smallest and largest width
tried. Seed 0 — where `note 090`/`091` were taken — is the best of eight.
`tools/check_constants.py` now refuses a pin that says nothing about where it came from.

**2. Score predictions MECHANICALLY, not by eye.** `g41-01`'s P4 failed in **one cell of
84** and reading six tables would have missed it.

**3. When a new script re-derives a quantity an existing tool computes, print both.**
`g43-01`'s first version reported four closing sub-domains and it was an artefact, in three
lines of code, **corroborated rather than caught by its own shuffled control.**

**4. Run `python tools/preflight.py`, not the nine commands.** A suite piped through `tail`
reports `tail`'s exit code. That nearly shipped an unverified commit here.

---

## THE KILL LIST

     ✅  2  representations learned LOCALLY   18 graphs, beats counting
     ✅  6  independent nodes agree           transport half exact in containers.
                                              QUANTISER half still untested
     ✅  7  decide what to say, and decline   exact, on the case the gate sees

     🔀 10  margin survives scale             refutation was on the wrong arrangement

     ⏸  4  multi-hop walk over real internet  5.09 s per grounded question

     ⬜  1  relational objective buys reasoning  0.9076 GIVEN the chain length.
                                                 Both aids priced; aid 1 unsolved,
                                                 aid 2 does not transfer
     ⬜  3  conventional system already wins     external stimuli; no human opponent
     ⬜  5  learn forever                        first prequential evidence, g39-01/02
     ⬜  8  adjudicate contradictions            untouched
     ⬜  9  survive hostile participants         untouched
     ⬜ 11  training traffic fits broadband      109 messages per query at depth 1
     ⬜ 12  survives a second modality           G7 PASSED. Three modalities, real data

**John's ordering for #1 was 3 → 4 → 2 → 1** (remove the aids, attack the scope, publish
properly, build a reference). #2 and #3 and #4 are done. **#1 — a composing reference — was
never needed** and should not be started without a reason.

**After #1 he named INTERVENTION** — `docs/options/intervention.md`. His reason, recorded
there: interacting with the world is necessary for where this is going, not incidental.
`g43-01` strengthens that: no statistic over an observational stream supplies a conserved
quantity that is not there.

---

## WHAT IS BUILT, changed this session

    openplexus/search.py            `beam(any_length=True)`, OFF by default, zero
                                    extra reads. REFUTED as a mechanism, kept per
                                    rule 14c as the measured alternative
    tools/check_constants.py        every pinned number says where it came from
    tools/preflight.py              the whole pre-commit gate, unpiped
    tools/check_decisions.py        a row's evidence is bounded to its OWN bullet
    tools/invariant_dimension.py    `relation_names`, so column order has one home
    experiments/g41_01, g42_01, g43_01

---

## STATE

Clean tree, preflight 11/11 three times, **everything pushed.**

`data/` holds clutrr, fb15k237, fsdd, kachergis, mnist, openea and tinyshakespeare, all
gitignored. Re-fetch on a fresh clone with `tools/fetch_*.py`.

**The 5-minute monitor points at `scratchpad/STATUS.txt`** (gitignored). Arm it at the
start of a session — `DECISIONS.md` standing agreements carries the pattern so it survives
this file being overwritten.
