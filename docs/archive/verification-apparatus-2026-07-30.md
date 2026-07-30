Verification apparatus — archived from DECISIONS.md, 2026-07-30
==============================================================

**Moved out of the tree, not retired.** The tree is for architectural options and this component is process: every item is a tool that documents itself in its own docstring, and CLAUDE.md's rules 6, 10, 11 and 14 carry the policy. It was costing thirty lines of a document whose primary criterion is being readable in one pass, and nothing here has ever been re-litigated -- which is the only thing the tree exists to prevent.

**Live counts move; these do not.** The mutation count below was true when written and `python tools/mutate.py --verify` is the authority.

---

## 11. Verification apparatus

**⇒ DECIDED and deliberately permanent. Listed so effort does not go here hunting
for stopgaps.**

- ✅ **Mutation harness** — 172 mutations, sharded 6 ways in CI. **Measured on
  `57d8112`: 168 mutations, 28 per shard, 18–35 minutes each, all caught** — so
  serial is ~2.5 hours, not the twenty minutes the comment claimed.
  - `168` shards are by POSITION, so inserting a mutation mid-list shifts
    everything after it. Two logs compare line-for-line only while the list is
    unchanged
- ✅ **Dependency-free ruler** — `tasks/`, `baselines.py`, `answers.py` take no
  dependencies, because they are what everything else is asserted against.
  - `note 007` the stack decision. *no measurement* — a convention, and the
    argument is that a generator with no library semantics is auditable line by
    line
- ✅ **The rails** — `check_workflows` (flags vs `--help`, one second, turns a spent
  matrix into an error), `check_rails`, `check_duplication`, `check_decisions`.
  - *no measurement* for the rails as a policy — they are conventions, and each
    encodes a specific failure that already cost a result rather than generic lint
  - `check_duplication`'s stated justification was **wrong and its own tool
    measured that**: run over the pre-port tree it finds none of the five
    hand-copied recovery refusals it was requested for, because those copies had
    already diverged. So it is PREVENTION, not detection, and the thing that
    catches a drifted copy is still a mutation
- ✅ **Sensitivity checks on any timing assertion** — `169`: three attempts at one
  assertion (a race, a vacuous bound, then a real check) and **the first two both
  passed when written.**

---

