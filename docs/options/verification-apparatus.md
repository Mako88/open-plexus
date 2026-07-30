# Option record — the verification apparatus

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `tools/mutate.py`, sharded six ways in CI. `--verify` is the authority on the count.
- A dependency-free ruler: `openplexus/tasks/`, `baselines.py`, `answers.py` (`note 007`).
- The rails: `check_workflows`, `check_rails`, `check_duplication`, `check_decisions`,
  `check_options`, `check_provenance`, `check_commit_messages`.
- Full account: [archived](../archive/verification-apparatus-2026-07-30.md).

---

## What was tried, and what came back

### The mutation count, and a correction to the correction

    CONFIG  when    2026-07-30
            source  CLAUDE.md conventions, run 57d8112
            script  tools/mutate.py --verify
            task    n/a -- the harness itself
            model   n/a
            knobs   six shards
            scale   168 mutations across six shards, 18 to 35 minutes each

The `checks.yml` comment claimed *"85 mutations at roughly fifteen seconds each"*. The first
full run to complete was **168 mutations across six shards**, so serial time is about two
and a half hours rather than twenty minutes. All 168 were caught.

**And the correction was itself wrong first.** The count was written as 169, read off the
working tree after a mutation had been added rather than off the run. Six shards at 28 is
168, and the arithmetic was there to check. **A number quoted from the wrong snapshot is the
same defect as a stale download**, in a place nobody thought to look because it was only a
count.

### Two mutations survived on the exact cache's defining claims — `60`

    CONFIG  when    2026-07-27
            source  decision 60
            script  tools/mutate.py
            task    n/a
            model   the exact cache
            knobs   none
            scale   surviving at b480926 and at least one commit before

`the-cache-admits-by-RECENCY-not-residual` and `the-cache-read-is-not-gated-by-the-MATCH`.
Admission by residual and the match gate are what the mechanism IS, and nothing asserted
either. **Found only because an unrelated refactor made `--verify` fail and someone went
looking.**

### A timing assertion passed twice before it measured anything — `169`

    CONFIG  when    2026-07-29
            source  decision 169
            script  tests/test_deadline_settles_short.py
            task    a step with a silent peer
            model   the deadline branch
            knobs   none
            scale   three attempts at one assertion

**The first two both passed when written**, which is why a timing assertion now needs a
sensitivity check before it counts as evidence.

### And this option is deliberately kept out of the tree

    CONFIG  when    2026-07-30
            source  DECISIONS.md component 11
            script  none -- a scope decision
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

Every item documents itself in its own docstring, and CLAUDE.md rules 6, 10, 11 and 14 carry
the policy — so it was spending lines in a document whose criterion is being readable in one
pass. **Nothing in it has ever been re-litigated**, which is the only thing the tree
prevents.
