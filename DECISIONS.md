# Decision log — overnight session, 2026-07-26/27

Decisions taken without asking, for review in the morning. Newest last. Each one
says what was chosen, what it rules out, and how to undo it if it was wrong.

Standing authorisation for this session: push and dispatch CI freely, update
documentation freely, make necessary decisions and log them.

---

## 1. Built the tag as a *replacement* for the window, not an addition

**Chosen.** `tag_slots` and `reward_window` are mutually exclusive — the config
raises if both are set.

**Why.** They are two answers to one question (how the gate chooses what to keep
when the reward arrives). An arm running both is neither arm, and the sweep
comparing them would be comparing a hybrid against itself.

**What it rules out.** A combined gate cannot be run without changing the
validation. Given what the control then found — that the two mechanisms select
*different* things and a real gate wants both — this is the constraint most
likely to need lifting, and lifting it is a three-line change.

## 2. Ranked the tag by reusing `admit` with a negated rank

**Chosen.** No second pool. `tag()` negates the rank and hands it to the same
primitive competitive capture uses.

**Why.** Two implementations of one behaviour drift, and one gets fixed while the
other keeps producing plausible numbers (CLAUDE.md rule 9). The negation is also
the finding stated in one character.

**Cost.** The sign of the rank now carries meaning, and that is what made the
fade bug possible (decision 4).

## 3. Added `captured` and `write_index` to the trace

**Chosen.** The trace now reports which pending indices a capture kept, and where
each step's write landed in that list.

**Why.** Rule 2 — observe the quantity the change claims to move. A gate's claim
is about which writes survive; accuracy is downstream and cannot separate "the
tag holds bindings" from "the tag holds the first four writes after every
reward". Without this the control could not have been run at all.

**Cost.** Two more keys in the per-step dict. `test_trace_observes` pins the key
set and was updated; the trace remains prediction-neutral.

## 4. Added a fade to the tag, which note 010 specified and the first build omitted

**Chosen.** `tag_decay`, default 1.0 (off).

**Why.** Measured, not argued: an un-faded tag captures the rewarded binding in
2 of 32 captures at every capacity and delay, because right after a capture the
store holds almost nothing, so the weakest retrievals it will ever see are the
first writes of the next interval. It fills with those. With a fade of 0.99 it
captures 44%.

**The bug this produced, kept here because it is the most useful thing in the
log.** The first `fade` multiplied every rank by the factor. `admit` keeps the
largest rank, so for the weak-preferring end (negative ranks) that moves them
*toward* winning — it entrenched exactly the marks it was meant to release. It
produced numbers identical to no fade at every setting from 0.99 to 0.7. Not
similar: identical. Found by noticing a dial that did not move its output.

## 5. Ran the control before the sweep, and let it change the write-up

**Chosen.** Counted what each gate keeps against `position_kinds()` locally,
before costing the 32-job matrix, and wrote the predictions against it with the
control disclosed.

**Why.** It is seconds of compute and it answered the mechanism's own claim more
directly than the sweep will. It also changed the conclusion: recency is not a
worse selector than weak retrieval, it selects a different thing, and the window
already had the half the tag lacks.

**Consequence.** Predictions 1, 3, 4 and 5 in g9-05 are readings of that control
and are marked as weak foresight. 2 and 6 are not in it.

## 6. Kept g9-05 running anyway, despite the control looking discouraging

**Chosen.** Dispatch the sweep rather than declaring the null from the control.

**Why.** The capture count is not the score. The tag stores about a quarter as
much as a window reaching as far, and retrieval goes as `sqrt(d/N)`. A gate that
captures a third as often while storing a quarter as much is not obviously worse,
and nothing but the sweep settles it.

**Undo.** If the morning view is that this was runner time on a foregone
conclusion, the sweep is `workflow_dispatch` only and costs nothing further.

## 7. Committed a live mutation, then built the guard rather than just fixing it

**What happened.** `git add -A` ran while a background mutation harness had
`local_memory.py` edited. Commit `3634a23` shipped `rank = strength` — the
mutation that makes the tag admit the *strongest* — inside the change whose
argument is that admitting the strongest is backwards. CI caught it on push.

**Chosen.** `python tools/mutate.py --verify`: assert every mutation's original
text is present. One second, no edits, deliberately outside the harness lock.
Now the first step of `checks.yml` and the first of the three commands in
CLAUDE.md.

**It caught a second thing on its first run.** `storage-mask-ignored` had gone
stale — the trace work rewrote the line it targeted into a named `wrote`
variable. That would have surfaced only on a full twenty-minute run, which does
not happen before a commit. Re-pointed; 97 of 97 originals now present.

**Worth review.** Whether `--verify` should also refuse when a `.mutate.lock` is
held by a live process. Currently it does not, on purpose, because the case it
most needs to cover is a harness running right now — but that means it can pass
one instant and fail the next.

## 8. Stopped running the full mutation harness locally

**Chosen.** Local is `--only <the mutations just added>`; the full sharded run is
CI's job.

**Why.** `checks.yml` already shards it six ways, and its own comment says the
local duplicate was the thing blocking local work — experiments refuse to run
while the source is mutated. Two full local runs tonight cost about forty minutes
and produced no output, both killed by timeouts, and one of them caused
decision 7.

## 9. Corrected two documentation drifts found in passing

**Chosen.** Fixed BACKLOG's fast-store-brakes entry, which still read as
"next: build a cap" after g8-04 answered it; and added explainers 27–30 to an
index that had stopped at 26.

**Not chosen, flagged instead.** GOALS.md's gating section still ends at g8-01's
null, with g9-02, g9-03 and g9-04 absent — so the project's largest document
reads as though that line were dead. Logged in BACKLOG rather than rewritten,
because how much of a live investigation belongs in GOALS at all is John's call.

## 10. A mutation survived CI, and the fix was the assertion rather than the code

**What happened.** `the-tag-outlives-its-capture` (replacing `tagged.clear()`
with `pass`) survived shard 0. The mechanism is correct; the test claiming to
cover it was asserting the wrong property.

**Why the obvious assertion misses it.** "Every capture keeps steps inside its
own interval" passes while broken, because a stale position still lands *inside*
the current interval whenever it is in range. It protects the wrong write, not an
out-of-range one, and nothing about the step numbers says so.

**And the fade hides it.** A stale mark keeps ageing, so with any fade it is
displaced within a few steps and the contamination flushes itself. The bug is
only visible with the fade off, where the stale ranks are the near-zero ones from
the previous interval's cold start and nothing displaces them. Every case in the
original test used a fade.

**Chosen.** Assert the count invariant instead: a capture keeps exactly
`min(slots, writes offered in that interval)`, because the pool fills
unconditionally while there is room. A stale position from a longer previous
interval is out of range in a shorter one, so fewer writes are protected than
there was room for. Tested over a long-then-short-then-medium stream at three
fades including 1.0.

**Worth review.** This is the second time tonight that a test passed while its
subject was broken, and both times the tests were written before the behaviour
was understood. The pattern to watch: an assertion phrased over the *domain* of a
quantity (which interval, which range) rather than over its *value*.
