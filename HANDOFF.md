# HANDOFF — scratch context for a session swap

> **This file is TEMPORARY and is OVERWRITTEN, never appended to.** It exists so a new
> session can pick up mid-thought; it is not a record and nothing durable may depend on
> it. If you are about to add a section rather than replace the file, stop — that is how
> this becomes a second decisions log, which is the failure `DECISIONS.md` was rebuilt to
> escape.
>
> **Nothing else in the tree may cite this file.** A note, a docstring or a commit that
> points here makes it load-bearing, and a load-bearing scratch file cannot be thrown
> away. Cite `DECISIONS.md` or a numbered note instead.
>
> **Where things actually live:** decisions → `DECISIONS.md` (the tree, authoritative).
> Findings and measurements → `docs/notes/`. Standards → `CLAUDE.md`. Goal and
> refutation conditions → `GOALS.md`. If something here is worth keeping, it belongs in
> one of those, and it should be moved there rather than left here.
>
> ## THIS FILE HAS EXISTED BEFORE AND IT FAILED
>
> `CLAUDE.md` rule 14b's calibration records what happened: a previous `HANDOFF.md`
> *"carried 'prequential 4.540 ... unigram BEATEN' as the project's headline text result
> for weeks"*, and decision 118 established it was an offline backprop probe on frozen
> features — not prequential, and not the model. A sibling `STATE.md` grew 21 KB → 36 KB
> in a single session purely by duplicating tables that already existed elsewhere.
>
> **So the rule that matters is not brevity, it is this: NO CLAIM LIVES HERE.** Every
> number below is a pointer to the note that owns it, and if the two disagree the note
> wins. A figure whose only home is this file is the exact failure above, waiting.

**Written:** 2026-07-30, end of the session that closed the composition scope question.

**FIRST THING: there are unpushed local commits.** They were held deliberately — a push
cancels the running `checks` run, which is the pacing problem described below. Confirm
the run on `34e9ae9` has finished, then push. If it failed, fix before pushing more.

---

## Where the work is standing

Recent, in order: composition ceiling closed (`note 090`/`091`) and then **scoped**
(`note 104`); driver-free reads batched to `PROTOCOL` 3 (`note 100`/`101`); the beam's
rendezvous priced (`note 102`); `beam` wired into `run()` and made the default
(`note 103`, `search_beam_width=4`).

## The open problems, in the order I would take them

**1. Find invariants per SUB-DOMAIN.** The consequence of `note 104`. The displacement
mechanism needs a conserved quantity, and DBpedia has none globally — dimension 0 on both
graphs, and no approximate one either (smallest singular values cluster at ~3e-3 with no
gap, where CLUTRR's null direction is 1.3e-15). The replacement question: does some
*subset* of a graph's relations close consistently? That is a largest-consistent-subset
search rather than a null space over all relations — a different computation, unbuilt.
`tools/invariant_dimension.py` is the instrument to extend.

**2. Adjudication.** The memory loop still cannot decide which side of a contradiction is
wrong. Contradiction supplies *wrong* and blame supplies *where* (`note 080`, `note 092`
own those numbers); nothing votes. Idea worth testing: concept partitioning means the
same binding is reachable through differently-interfered stores, so a second opinion need
not be a second derivation from the same primitives — which is what killed the earlier
attempt.

**3. The six `concept_nodes` refusals, which are really four problems.**
**A verified finding, not yet acted on:** the `hops > 1` refusal in
`openplexus/models/local_memory.py` is over-broad. It refuses because the soft hop key is
*"a softmax mixture ... so it names no concept"* — but the hop loop is
`for depth in range(0 if searching else ...)`, so when search is on that key is never
built and `beam` commits to a hard, routable token at every step. The refusal's own text
says to use the walk instead. **The guard should be `hops > 1 and search_branches < 1`**,
and since `note 103` that condition is the default. This unblocks partitioning *plus*
multi-hop, which is the configuration C4 needs. It wants a sweep, not just a narrowing.

The rest: `reward_token` is mechanical (`pending` lacks the concept; the source says
*"storable, and unbuilt"*), `carry_store` just needs a test, `memory_cap` and
`tag_relative` are **one** design question (both need a global quantity no node can know),
and `consolidation` needs `lasting` partitioned — the only one flagged temporary by design.

**4. Non-logical thought.** Nothing built, no plan, and the one most likely to produce
something plausible and useless without a steer on what is actually wanted.

**5. The migrating walk.** Designed and priced, not built. Needs the decode on the peer,
walk state on the wire, and a rendezvous protocol. `note 102`'s latency figures are
estimates; `tools/walk_rounds.py` measures the path that exists.

John asked to **talk problems 1–4 through before more solo building.**

## Two process facts

**CI mutation shards have not completed in a long stretch.** Every `checks` run back to
`ac2f4cf` was **cancelled** — each push cancels the previous run, and commits were landing
on a ~20-minute cycle against a ~35-minute verification loop, so the slow half never ran.
The fast `tests` job is genuinely green. The shards' last real evidence is 4 of 6 on
`92aa835`. Local `mutate.py --verify` is clean at 201/201, which proves every anchor still
exists — **not** that every mutation is still caught. Either batch commits or let a run
land. The unbuilt speedups (git-worktree parallelism ~8×, two-phase targeted running 2.8×
measured) are the real fix.

**`DECISIONS.md` sits at exactly 900/900 lines.** Anything added displaces something, and
rule 9 says trim the newest writing first.

## Standing correction worth carrying forward

Two numbers were nearly reported from the wrong regime. The beam-over-search gap usually
quoted is CLUTRR chain recovery at depths 2–10, while `run()` is kinship at hops 2, where
the real gain is smaller by roughly a factor of five (`note 103` owns both figures). And
*"two conserved quantities in DBpedia"* was one step from being written up when both came
from two relations appearing in no cycle at all (`note 104`). Both were caught by checking
a number against the regime it came from, and by a prediction registered before the run.
