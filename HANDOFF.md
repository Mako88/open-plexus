# HANDOFF — scratch context for a session swap

> **This file is TEMPORARY and is OVERWRITTEN, never appended to.** It exists so a new
> session can pick up mid-thought; it is not a record and nothing durable may depend on
> it. If you are about to add a section rather than replace the file, stop — that is how
> this becomes a second decisions log, which is the failure `DECISIONS.md` was rebuilt to
> escape.
>
> **Nothing else in the tree may cite this file.** A note, a docstring or a commit that
> points here makes it load-bearing, and a load-bearing scratch file cannot be thrown
> away. Cite `DECISIONS.md` or a sweep record instead.
>
> **Where things actually live:** decisions → `DECISIONS.md` (the tree, authoritative).
> An option's history → `docs/options/<name>.md`. A prediction, before a run → the sweep
> record. A finding about the METHOD → a `CLAUDE.md` calibration. The readable version →
> `docs/explainers/`. Goal and refutation conditions → `GOALS.md`.
>
> **Investigation notes are RETIRED** — all 105 are in `docs/archive/notes/`. Do not write
> a new one. **NO CLAIM LIVES HERE:** every number below points at the file that owns it,
> and if the two disagree that file wins.

**Written:** 2026-07-30, end of the session that built the first learned relation
representation.

---

## THE ORDERING CHANGED, AND IT IS THE MOST IMPORTANT THING ON THIS PAGE

John restated the standing agreement: **order by what is most likely to DISPROVE the
project**, not by what is hard and not by what is ready. Tuning a working mechanism is
explicitly deferred until the core is proven. His words: *"we've done a lot of close
enough or nearly there or almost there things, and at the end of the day we still have
these nine things we have to prove out."*

Two companions, both in `DECISIONS.md` standing agreements:

- **Prefer the option that SETTLES the question**, even when harder and slower. A test
  whose band is too narrow to separate its arms is a "nearly there" wearing an
  experiment's clothes.
- **Every option offered to John carries three things:** a plain explanation of what it is
  and where it sits, pros and cons, and a recommendation with a default. **And never offer
  an option already known to fail the goals** — put it in the tree as ❌ if it is worth
  recording, never in a menu.

## The kill list, in order

    1  does a relational objective actually buy reasoning      BLOCKED, see below
    2  can representations be learned by LOCAL rules           FIRST YES TODAY
    3  does a graph DB / symbolic system already do this       not started
    4  does a multi-hop walk fit real internet latency         instrument built
    5  can it learn forever without wrecking itself
    6  do independent nodes agree what a thing IS
    7  can it decide what to say, and decline
    8  can it adjudicate contradictions
    9  does it survive hostile participants

## What happened to #2, which is the live thread

**A local contrastive rule learns relation structure, and it does not need a conserved
quantity.** Full numbers in `docs/options/structured-relations.md` and
`experiments/sweeps/g23-01-*.txt`.

- **End task, kinship:** 0.7821 against random filling's 0.6642, +0.1179 paired, **10 of
  10 seeds**. First mechanism other than generation delta to clear that bar; the one
  `note 088` refuted scored *below* random. **Predictions were committed at `57f81e7`
  before the fill mode existed.**
- **Held-out rules on graphs with no invariant:** 0.3602 at invariant dimension **0**,
  where `generation_delta` is structurally impossible, and 0.3559 at dimension 2, which
  that tool refuses. **So `note 104`'s scoping does not bind this mechanism** — which is
  the old open problem #1, answered from the other side.

**Where it is NOT yet:** nothing in the model uses it, so the tree row is still ⬜. The
graph numbers are **not pre-registered** and are observations. The fold runs over TRUE
chains — `note 091` says recovering chains costs about 0.11. Determinism on the graphs is
0.778, so the ceiling is well below 1.0 and unmeasured.

## Why #1 is blocked, and it is a real finding

**This project has no instrument on which the premise test can be run decisively.**
Closure's usable band is 0.092 (`g14-01`, 8 seeds). CLUTRR's is ~0.285 in one bucket with
five possible answers and heavy label skew, with the reference below a trivial baseline at
8 of 9 depths. `g22-01` is built, costed and dispatchable against closure, and would
probably return "below the resolution of this instrument".

The band was revised **three times** in one session; `CLAUDE.md` rule 17 says stop
measuring at that point, publish the bound, and go build. That is why #2 was taken instead.

**The two-layer reference is PARKED.** It was approved on the diagnosis that a one-layer
model cannot compose — and measurement refuted that: it reaches 0.714 at trained depth. Do
not build it without a better reason.

## Also new, and both are checks rather than rules

- **`check_rails.py` R6** — every module in `openplexus/` and `tools/` must say in its
  docstring what it does not duplicate. 65 of 66 violated it on day one and are baselined;
  it caught its author's own two new files first. `CLAUDE.md` rule 19 gained the clause
  that was missing: **a negative search result is not a finding until it was a wide one.**
- **`check_provenance.py` was case-blind** and therefore *weaker locally than in CI*, which
  is the worst direction. `tests/test_check_provenance.py` asserts the wrong case does not
  resolve.

## Process facts

- **`peer.py` has never run over an impaired link.** `testbed/run.py` is the verified netem
  runner and drives the DIMENSION path; notes 094 and 101 both say the harness has never
  been pointed at the peer path. `openplexus/node_main.py` now has a peer mode and
  `tools/peer_walk_timing.py` times a walk and refuses a per-round number when rounds do
  not equal `2*depth`. Neither has run in a container.
- **No workflow runs docker or netem.** The container work has only ever been driven by
  hand.
- **`generation_delta.py` refusing `dim > 1` is a live gap**, not a footnote — the
  contrastive representation handles that case and it does not.
