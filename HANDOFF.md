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
> An option's history → `docs/options/<name>.md`. Findings and measurements →
> `docs/archive/notes/`. Standards → `CLAUDE.md`. Goal and refutation conditions → `GOALS.md`.
>
> ## THIS FILE HAS EXISTED BEFORE AND IT FAILED
>
> `CLAUDE.md` rule 14b's calibration records what happened: a previous file here carried a
> headline text result for weeks that decision 118 established was an offline backprop
> probe on frozen features — not prequential, and not the model. A sibling `STATE.md` grew
> 21 KB → 36 KB in a single session purely by duplicating tables that existed elsewhere.
>
> **So the rule that matters is not brevity, it is this: NO CLAIM LIVES HERE.** Every
> number below is a pointer to the file that owns it, and if the two disagree that file
> wins.

**Written:** 2026-07-30, end of the session that completed the option-record migration.

---

## The doc restructure is FINISHED

All 86 options are migrated. `DECISIONS.md` carries a summary line and a link per option;
`docs/options/` holds the history. **85 records, 277 entries, 513 measurements**, and the
tree is 742 lines against its 900 budget, down from 870 with everything expanded rather
than compressed.

Three things landed with it, and the second is the one to know about:

**A CONFIG block per entry, seven keys, enforced.** `when source script task model knobs
scale`, with `unrecorded` written rather than a line dropped. `docs/options/README.md` is
the format; `tools/check_options.py` enforces it.

**`tools/check_provenance.py` — every measurement must appear in a source its own entry
cites.** It found seven bad citations during the migration, including `0.9220`, the
accuracy case for concept partitioning, cited to a note containing no partitioning
measurement at all. **The number was real** and reproduces to four decimals in seventy
seconds (`note 105`); the pointer was wrong. Derived figures go in
`tools/provenance_baseline.json`, where an exemption is visible and can only shrink.

**A defect in `check_options.py` itself:** the header exemption was `text[:1400]`, a byte
count, so every record shorter than that was exempt from the status checks entirely. Fixed
to the leading blockquote, with a test that feeds it a short record.

## The open problems, in the order I would take them

**1. Find invariants per SUB-DOMAIN.** The consequence of `note 104`. The displacement
mechanism needs a conserved quantity and DBpedia has none globally — dimension 0 on both
graphs, no approximate one either. The replacement question: does some *subset* of a
graph's relations close consistently? A largest-consistent-subset search rather than a null
space over all relations — a different computation, unbuilt.
`tools/invariant_dimension.py` is the instrument to extend.

**2. Adjudication.** The memory loop still cannot decide which side of a contradiction is
wrong. Contradiction supplies *wrong* and blame supplies *where* (`note 080`, `note 092`);
nothing votes. Idea worth testing: concept partitioning means the same binding is reachable
through differently-interfered stores, so a second opinion need not be a second derivation
from the same primitives — which is what killed the earlier attempt.

**3. The six `concept_nodes` refusals, which are really four problems.** The `hops > 1`
refusal in `openplexus/models/local_memory.py` is over-broad, verified by reading the code:
the hop loop is `for depth in range(0 if searching else ...)`, so with search on the soft
key it objects to is never built. **The guard should be `hops > 1 and search_branches < 1`**,
and since `note 103` that condition is the default. Unblocks partitioning *plus* multi-hop,
which is the configuration C4 needs. Wants a sweep, not just a narrowing.
The rest: `reward_token` is mechanical, `carry_store` needs a test, `memory_cap` and
`tag_relative` are **one** design question, `consolidation` needs `lasting` partitioned.

**4. Non-logical thought.** Nothing built, no plan, and the one most likely to produce
something plausible and useless without a steer on what is wanted.

**5. The migrating walk.** Designed and priced, not built. Needs the decode on the peer,
walk state on the wire, and a rendezvous protocol. `note 102`'s latency figures are
estimates; `tools/walk_rounds.py` measures the path that exists.

John asked to **talk problems 1–4 through before more solo building.**

## Process facts

**CI is healthy again.** The `checks` run on `34e9ae9` completed green in 1h4m — the first
full run including the mutation shards in a long stretch, after every run back to `ac2f4cf`
was cancelled by the next push. Keep batching commits so a run can land.

**`tools/check_provenance.py` runs in CI** and is in `CLAUDE.md`'s pre-commit list, along
with `check_decisions` and `check_options`, which were in CI but not in that list.

**John's standing autonomy agreement is recorded** in `DECISIONS.md` standing agreements:
5-minute heartbeat while a clear next step exists, blocking and harder problems before
easy ones, and decide rather than wait when he is away.
