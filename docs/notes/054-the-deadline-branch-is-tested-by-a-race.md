# 054 — The deadline branch is tested by a race, and its real case by nothing

**Status:** a diagnosis. The hole is named and not yet closed, and the first
attempted fix was wrong in a way worth recording.

**How it surfaced:** `the-deadline-fires-immediately` **SURVIVED** a CI mutation
shard on `f6ab220`, and is caught on every local run. That is the class of finding
CLAUDE.md says now arrives late because mutations moved to CI, and it must be
treated as blocking rather than noted.

---

## IN PLAIN TERMS

The distributed driver can be told *"wait at most one second for the machines to
reply, then answer with whatever arrived."* That wait is the whole reason a machine
leaving does not stall the network.

The check that the wait actually happens is guarded by a deliberate bug we inject
to prove the tests would notice. **That bug slipped through on the build server
while being caught on the desktop every time.** Chasing why turned up something
worse than a flaky test: the situation the wait exists for — a machine that accepts
a question and then never answers — is not tested at all, because nothing in the
test suite can currently create one.

---

## Why it survives under load

The receive loop drains **every ready socket** before the overdue check runs:

    for sock in ready:      # drains ALL of them
        ...
    while settled < sent:
        complete = votes >= expected[settled]
        overdue  = (deadline is not None and votes >= 1
                    and time.monotonic() - asked_at[settled] >= deadline)

When every live vote arrives in one `select` round, `complete` is already true and
the overdue branch is never consulted. **The mutation is then semantically inert.**

A starved 2-vCPU runner deschedules the driver, all four nodes reply while it is
off-CPU, and the votes land together. Locally the driver runs between replies and
sees a partial set. So the old detection works by **winning a race**, and a slow
machine loses it.

## The first fix was wrong, and that is the useful part

The obvious move was to assert in `test_an_undeclared_death_completes_WITH_a_deadline`
that the run took at least one deadline. **It fails on CORRECT code, at 0.002 s.**

A node killed outright **resets its connection**. The driver notices, drops it from
`dead`, and `expected` no longer counts it — so every step then completes
*normally*. That test passes a deadline in and **the deadline is inert**: no step
settles short and the overdue branch never runs.

So the natural place to test the deadline is a test where the deadline does nothing.

## And the real case is untestable as the harness stands

For a step to settle short you need `1 <= votes < expected`. Walking the ways that
could happen:

    a node is killed          resets the socket -> dropped from `expected`
    a node is not asked       `expected[step] = len(speaking)`, so not counted
    a send to a node fails    `speaking.discard(index)`, so not counted
    a node HANGS              -- the only one that produces a short settle

**Only the last produces the condition**, and nothing in the suite can create a
node that accepts a request and never replies. That is why there is no test for it:
not oversight, but a missing facility.

The evidence is one line: `steps_settled_short` is asserted in exactly **one**
place in the whole repository, and it is asserted to be **empty**. This is rule
10's named pattern — *a test that something did NOT change needs a companion
asserting that something DID* — with the companion missing, on the mechanism the
`deadline` parameter exists for.

## What closing it requires

**A silent peer.** Not a modified node process — a plain socket in the test that
accepts the driver's request and never answers. That is test-side only, touches no
production code, and would make the deadline's actual behaviour observable for the
first time:

- a step settles short **after** the deadline and not before
- `steps_settled_short` records the shortfall
- the answer is the partial one rather than token 0

With that, `the-deadline-fires-immediately` is caught deterministically instead of
by a race, and the mutation stops being load-dependent.

**Not done here**, deliberately: it is a harness addition rather than a one-line
assertion, and the diagnosis is worth landing before the build.

## Outcome — built, and the first version of it was vacuous

`tests/test_deadline_settles_short.py`, five tests. The silent peer works and the
condition is now reachable: every step settles short by exactly one, the run
produces the partial answer rather than token 0, and the deadline is waited.

**The property that makes it non-flaky** is that a permanently silent voter makes
`complete` *impossible*, so the overdue branch is the only way a step can settle.
The mutation cannot be inert here, whatever the scheduler does — which is the whole
difference from the race it replaces.

**Two failures on the way, both caught by a check rather than by inspection:**

1. **It hung instead of failing.** `run` sends `_RESET` before the first token; the
   peer counted it as a step, so every vote was one ahead of the step the driver
   awaited, and `if step not in pending: continue` discarded each one *in silence*.
   Votes stayed at 0, `votes >= 1` never became true, and the driver waited
   forever. Correct behaviour for a real network, merciless for a fake node.

2. **The lower-bound assertion was VACUOUS, and this is the one worth reading.**
   `elapsed >= deadline` was measured around the whole `with` block, which includes
   both peers connecting and their retry sleeps. Measured: **0.585 s against a 1 ms
   deadline.** Setup dominated, so the assertion would have passed under the very
   mutation it was written to catch.

   What caught it was a sensitivity test — move the input, require the output to
   move — added on the suspicion that a timing bound might be measuring overhead.
   The fix is to time `run` alone. **Rule 2, exactly: observe the quantity the
   change claims to move, not a bystander that correlates with it.**

> So the tally for this note is three attempts: a race, a vacuous bound, and a
> real check. The first two both *passed* when written. That is the argument for
> sensitivity checks on anything asserted about a duration.

## What is NOT wrong

**The production code.** Rule 11 says a failing test is a claim about the
production code until shown otherwise, so this is the showing: the deadline logic
is correct, the mutation is a real defect that the mutation harness is right to
inject, and what is missing is a test that can reach the branch. Nothing here
invalidates a measurement — the deadline is off in every result this project has
recorded.
