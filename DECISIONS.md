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

## 11. Ported the four remaining summarisers, and it was a correctness fix

**Chosen.** `summarise_g8_01`, `g8_03`, `g9_02`, `g9_03` now use the shared
`tools/recovery.py` rail. `by_cell` gained a named `metric` parameter so g9-02
can report first-asks and all-asks without a second loader.

**It was not just deduplication.** Three of the four chose their learning rate by
maximising `oracle - none`. That is the third rule in `recovery.py` — the one it
says bit hardest — because among cells that survive the floor check, the largest
gap is produced by whichever rate left the floor arm lowest. All three did skip
collapsed floors first, so none was the worst version, but the bias was live.

**What replaced it, and why the two differ.** g8-03 now picks on `capture-0`, the
unbounded arm its prediction is not about. g8-01 has no such arm — `on-use` and
`salience` are both under test — so it picks the rate where the FLOOR arm scores
highest, which is a baseline choice and the exact opposite bias to maximising the
gap. g9-02 and g9-03 pick by what the arm under test recovers, via `best_by`,
which selects after the refusals rather than before them.

**g8-01 also stopped hiding rows.** A cell refused for a noisy denominator used to
be skipped entirely, so it was indistinguishable from a combination never run.
Both are now printed, with the reason.

**Left undone deliberately.** The published sweep files are not edited to match —
they record what was reported at the time. Whether any headline actually moves
needs the archived JSON re-summarised, and those artifacts live in Actions rather
than the repo. Logged in BACKLOG.

## 12. Re-summarised the archived sweeps, and corrected a number in GOALS

**Chosen.** Pulled g8-01, g8-03 and g9-02's archived JSON out of Actions and ran
both the old and ported summarisers on it, rather than leaving decision 11's open
half open.

**One headline moved.** GOALS said the oracle's advantage at seq 768 was 0.612
and the ungated arm fell to 0.46. Both come from lr = 0.1 — the rate that most
depresses the ungated arm, which at that cell means 0.387 against a trivial floor
of 0.344. It passes the floor check, so the number is real; it is also the cell
where the baseline most nearly broke, and "largest usable gap" maximises over
exactly the axis that rewards that. At lr = 0.02 the same cell means 0.80 and the
gap is 0.196. GOALS now states the range and why.

**What did not move.** g8-03's conclusion (every curve falls, pools do not
flatten) and g9-02's recovery (0.23/0.23/0.24/-0.13) both survive. That matters
more than the correction: g9-03, g9-04 and tonight's tag all rest on g9-02, and
they are unaffected.

**The tool now shows its own refusals.** g8-01's 1536 row — the one withdrawn by
a later audit after being published — used to be silently skipped. It now prints
with the reason, at all three half-lives. That is the class fixed rather than the
instance: the row got published in the first place because nothing in the output
said it had been dropped.

**Undo.** The GOALS edit is one block and quotes both numbers, so reverting it
loses nothing that was there before.

## 13. Built the repo-specific rails as a ratchet, not a rule

**Chosen.** `tools/check_rails.py` with three rails and a checked-in baseline of
48 legacy exemptions. R1 (summarisers import `tools.recovery`) is strict and has
none. R2 (sweeps carry PREDICTIONS and COST) and R3 (experiments go through the
harness) are ratcheted, because 37 sweeps and 11 scripts predate those
conventions.

**Why a ratchet.** A check that fails on every legacy file gets suppressed, and a
suppressed check makes the others look optional. BACKLOG had already reached this
conclusion for the style-shaped rules; it applies here for the same reason.

**The design decision worth reviewing.** A stale exemption — one naming a file
that now complies or no longer exists — is an *error*, not a warning. That is
deliberate: an exemption list nobody prunes eventually covers whatever is added
to that path later, leaving a check that passes while checking nothing. The cost
is that legitimately fixing a legacy file makes CI red until the baseline is
regenerated with `--write-baseline`. I judged that acceptable because the fix is
one command and the diff is the record of what got better.

**Verified red.** Each rail was fired deliberately and caught by name, with the
tree restored afterwards. A check nobody has seen fail is not evidence.

**What this does not do.** It cannot check the thing that has gone wrong most
often here — a test asserting a property the quantity does not have. Nothing
mechanical can. Mutation testing remains the closest substitute and is already in
place.

## 14. g9-05 landed; dispatched g9-06 rather than concluding

**The result.** The tag's flat rows are flat at zero and its positive rows carry
the window's cliff. No cell is both, which was the whole claim. Four of six
predictions held, one partly, one is provisional.

**Chosen: not writing the refutation as final.** `tools/grid.py` reports the
capacity axis pinned — every delay chose `slots 8`, the top of `[4, 8]` — so
every tag number is a lower bound. The repo's own rule is that a caveat printed
beside a number does not attach to the number, so g9-06 is registered and
dispatched at slots 16, 32, 64.

**Chosen: dropping two fade values from g9-06.** 1.0 is harmful at every capacity
tested and 0.9 differs from 0.95 by less than the seed spread wherever both are
positive. That halves the matrix. It is a deliberate narrowing and is stated in
the sweep file rather than left invisible — a silent cap reads as "we covered
everything".

**What I would flag for review.** g9-06's prediction 2 is the one that matters:
if the fade-0.99 mean rises above +0.15 while its spread stays under 0.10, the
tag is vindicated and g9-05 measured a starved version. I think that unlikely —
the fade sets reach in steps and capacity should not buy reach — and I have
written that as the prediction most likely to be wrong, because the reasoning
assumes the two dials are separable and they may not be.

**The next build is already specified** either way: a gate reading both signals,
which needs the `tag_slots`/`reward_window` exclusion lifted. Logged in BACKLOG
rather than started, because starting it before g9-06 returns would be a
mechanism resting on a bound.

## 15. Built `tag_relative` rather than the combined gate, and why

**Chosen.** Entry 14 named the combined gate as next. I built something else
first: a one-division fix to the tag's ranking signal.

**Why.** Note 023 blamed the un-faded tag's failure on the store's size
confounding the strength signal. That was an argument. Measuring it made it a
fact — an absolute tag of four marks the final interval of a three-interval
stream at offsets 0.00, 0.01, 0.03 and 0.05. Literally the first four writes. A
diagnosis that specific has a fix that specific, and it is cheaper than a new
mechanism: no new signal, no new dial, no clock.

**It also tests note 023's own reasoning.** If normalising alone made the tag
work, the fade was only ever a cold-start corrector and the reach story in that
note is wrong about its own mechanism. That is g9-07's prediction 3. Building the
combined gate first would have left that untested underneath it.

**The control says it is real but not sufficient.** Rewarded-binding capture at
slots 8 goes 9/9/16% absolute-unfaded to 31/28/19% relative-unfaded, and
44/44/34% to 66/59/44% with a fade. So both dials do work and neither replaces
the other. The combined gate is still queued.

**A vacuous test, caught by its own guard.** The first meaning test asserted that
a relative ranking survives scaling every stored value by a constant while an
absolute one does not. Both survive: a constant rescale multiplies every
retrieval by the same factor and `admit` compares ranks. The guard I wrote to
stop the test being vacuous is the only reason it is not still passing. Replaced
with the temporal property, which is what actually separates them.

**Worth review.** g9-07 has five predictions and the one it exists for is
prediction 2 — a row that is both flat across delay and positive, which nothing
in this project has produced. I would not bet on it. Prediction 4 (delay 20
positive for the first time) is named as most likely wrong, because capture is
not recovery and g9-05 demonstrated the gap twice.

**Queued, not dispatched.** g9-06 is still running and the repo allows one matrix
at a time.

## 16. g9-06 overturned g9-05, and the win is not where the argument said

**The result.** `slots 32, fade 0.95` recovers +0.16 at every delay, spread 0.01
— flat and positive, the cell g9-05 concluded did not exist. +0.16 at delay 20 is
the first positive result at that delay anywhere in this project. g9-05 was
measuring a pool four times too small, which its own grid check had flagged.

**The catch, and it is the more important half.** The flat-and-positive row is
the row where `tag-strongest` scores the same: +0.003. The signal's direction is
worth +0.222 at `slots 16, fade 0.99` and nothing at the winning cell. So the
mechanism is bounded capacity plus a fade; g9-04's inverted signal buys height
only where the pool is starved.

**Chosen: correcting note 023 rather than softening it.** It claimed the tag "is
not a fix for the window's cliff — it is the other half of a gate". That is
false and the sentence is quoted and marked refuted in place, per rule 5.

**Chosen: repurposing g9-07 rather than cancelling it.** Its original reason —
find a flat-and-positive row — is answered. But `tag_relative` is a *signal*
improvement and g9-06 says the signal pays exactly where capacity is scarce, so
the run now asks whether a better signal reaches +0.16 at a smaller pool. That is
John's tiny-node priority and the one thing a capacity argument cannot answer.
Prediction 6 was added for it and is explicitly marked as registered after
g9-06, so it counts as weaker evidence than 1–5.

**Chosen: adding fade 0.9 to g9-07's grid.** g9-06's fade axis pinned at the
bottom of `[0.95, 0.99]`, so its +0.16 is itself a lower bound. Fixing one pinned
axis exposed another. Recorded in the sweep file as a revision made before
dispatch rather than applied silently.

**One cell missing.** `slots 64 fade 0.95 delay 8` did not return. The summariser
prints it as missing rather than undefined, which is the distinction the port in
decision 11 added. It does not affect the conclusion — slots 64 is worse than 32
at every other cell.

## 17. Costed the gate per node, because nothing in the g9 line ever had

**Chosen.** With g9-07 queued and the tag work blocked behind it, wrote
`tools/gate_cost.py` and note 024 rather than starting a new investigation.

**Why this and not something else.** John's stated priority is minimum viable
node size. Every g9 result is a recovery number at `d_model` 32 in one process —
none of them says what the mechanism costs a machine. Note 015 is the standing
warning: its hand-done cost model made competitive capture look cheap and was
wrong in the direction that flattered it. The same arithmetic had never been done
for the reward gate or the tag, which the whole line rests on.

**The finding.** A late signal must remember what it might undo, and that record
does not shrink when the node does — so it crosses the store's `w × d`. At
`d = 256` the crossover is width 1.45. A width-1 node pays 320 numbers against a
256-number store; at width 8 it pays 18%. Affordable, and costed for the first
time.

**The finding that matters more.** Without `derived_keys` a tiny node pays 187×,
because a pending entry must carry the full key. Note 015 named that dependency
for competitive capture; it is now true of the entire g9 line, and nothing said
so until now. `derived_keys` was adopted to remove the width term from
*bandwidth*. It is load-bearing for storage for an unrelated reason.

**In code with tests, not in prose.** Precisely because the last hand-done
version of this was wrong.

**What it does not do, and I want this read as a limit rather than modesty.** It
costs the gate, not the gain. Whether +0.16 is worth 1.25× the store at width 1
needs the recovery figure AT width 1, and no sweep has run one. That is now the
top BACKLOG item and it is the sweep John's priority actually wants.

## 18. Built the combined gate while g9-07 held the matrix

**Chosen.** With the sweep queue full and rule 17 saying a build should follow a
block of verification, implemented note 023's combined gate rather than starting
a new investigation. It is inside the live g9 line, not a new one.

**What it does.** A write survives a capture if *either* mechanism claimed it —
the tag's marks or the window's last `w+1` writes. It cannot capture less than
either alone, so the only way it loses is interference, and that is exactly the
measurement.

**Lifting the exclusion was the three-line part, as predicted in decision 1.**

**The ambiguity I had to resolve, and I want this reviewed.** `reward_window` 0
is a real one-write window *and* the default. So "tag with default window" is
either tag-only or a combined gate, and it cannot be both. I chose tag-only,
because every g9-05 to g9-07 cell was measured that way and a default must not
silently change an arm's identity. The cost is that "tag plus a one-write window"
is now inexpressible. If that combination ever matters, this needs a separate
flag rather than an overloaded zero.

**A test that asserted past the ambiguity.** My first version claimed window 0
must differ from tag-only. It failed — the tag had already marked the write at
the reward step. The replacement pins the semantics in both directions instead.
That is the third time this session a test I wrote asserted something other than
what I meant, and all three were caught by running them rather than by review.

**Not dispatched, and deliberately so.** g9-07 is running, g9-08 is queued, and
predictions have to be registered before a run. BACKLOG carries the cheap
pre-dispatch control that should come first: if the union does not capture
strictly more than either arm, the implementation is wrong before any recovery
number is spent on it.

## 19. Built the duplication check, and it refuted the reason for building it

**Chosen.** With g9-08 queued and the gating line blocked on it, built the
remaining named meta-test from BACKLOG rather than opening a new investigation.

**It does not do what BACKLOG said it would.** The item justified itself as
"would have found the five copied refusals before one of them lost its floor
check". Run over the pre-port tree it finds zero of them — those copies had
already diverged, and divergence is what defeats a structural hash. So it catches
copies that have *not* drifted, which are the harmless ones. Prevention, not
detection.

**Chosen: keeping it anyway, with the claim corrected in three places.** It
caught `load_baseline` copied between it and `check_rails.py` within minutes of
being written — by me, while writing a tool for finding copies. That is the
prevention case, demonstrated rather than argued. If the morning view is that
prevention alone does not earn a CI step, deleting it costs one line in
checks.yml and one in CLAUDE.md.

**The threshold was set by measurement.** At 5 statements it found nothing in
`tools/`; at 4 it found my copy. A threshold that misses the copy inside the
copy-detector is too high, and that is the whole justification for the number.

**What I want reviewed.** This is the second tool tonight whose stated purpose
turned out wider than its reach — `--verify` was the first, and it was fine
because its purpose was narrow to begin with. The pattern worth watching is that
BACKLOG entries carry justifications written before anything was built, and those
justifications are claims about behaviour like any other. Two have now been
tested and one was false.

## 20. g9-08 asked the tiny-node question on the wrong axis

**What happened.** Nine of fifteen cells refused. Below `d_model` 32 the ungated
model scores below chance, so the task is impossible and every ratio there is the
gap between a working ceiling and a broken floor.

**The refusals worked exactly as registered.** The fourth clause of the sweep's
WHAT WOULD REFUTE WHAT named this outcome before dispatch, and the summariser I
wrote for it prints the floor arm per cell precisely so it would be diagnosable
rather than mysterious. That part I am happy with.

**The design error is mine and it is the real finding.** `--width` sets
`d_model`, the width of the whole network. A narrow network is not a small node.
g7-02 — the sweep this was meant to check against — held the network wide and
split it with `partitions`, then read one machine. Note 024 had the two
quantities separated correctly; the sweep collapsed them into one number.

**Chosen: record it as a failed sweep rather than quietly re-running.** The file
now opens with "this sweep cannot ask its own question", scores the two
predictions that survive, and points at g9-09. Fifteen jobs spent, and the write
-up is worth more than the cells would have been.

**Two observations worth carrying forward.** The window collapses to -1.62 at
`d_model` 64, delay 20 — one and a half times the oracle's entire advantage spent
making things worse, against the tag's +0.19 in the same cell. And
`tag-strongest` is -0.03 there against the tag's +0.19, the third setting where
the signal's direction starts paying once something else is scarce.

**What I would flag.** I pre-registered the frozen axes as the risk in that file
and the frozen axes were not the problem — the swept one was. Naming a risk is
not the same as checking the axis means what you think it means, and nothing in
the checklist catches "this parameter is not the quantity you are asking about".

## 21. Rebuilt the tiny-node sweep on the right axis, and ran the control first

**Chosen.** g9-09 holds the network at 64 and sweeps `partitions`, asking one
group to answer alone — g7-02's axis. `--partitions` is wired into the existing
script with `partition=0` applied at TEST time only, so training is untouched:
the delta rule reads every group's own error regardless, which makes this the
deployment question rather than a different training regime. Records now carry
`node_width` beside `width`, because conflating those is exactly what g9-08 did.

**The discipline change is the point.** g9-08 spent fifteen jobs discovering its
floor arm was below chance. A three-minute control would have said so. This time
the control ran first, with its own predictions registered, and all three held.
Every row is usable down to a node of four numbers.

**What the control suggests, and why I am not believing it yet.** Implied
recovery is higher at every split than at one — 0.42 at two groups against 0.16
at one. The mechanism would be that a narrower reader is hurt more by
interference, so the floor arm falls faster than the gated arms and the same gate
recovers a larger share of a larger gap. If it survives three seeds and three
delays it is the best available outcome: the gate works better on the hardware
this project is for than on the hardware it has been measured on.

I have registered it as prediction 1 and named it most likely wrong, because the
arm that moved most across settings is the FLOOR arm — the denominator — and a
ratio whose denominator is what changed is the shape that has already cost this
project a result twice.

**A new failure mode, found by the control and not previously possible.** The
ORACLE breaks at 16 groups, falling to 0.608. First time a cheating gate has
failed anywhere here. `tools/recovery.py` refuses on the floor arm and the seed
spread and does *not* refuse a broken ceiling — so those cells will print ratios
that are fractions of a failing advantage. Registered as prediction 2, and
`summarise_g9_09.py` prints the oracle beside every ratio with a CEILING BROKEN
flag.

**Worth review:** whether `recovery.py` should refuse a collapsed ceiling the way
it refuses a collapsed floor. I did not add that — it would change the meaning of
every existing summariser and invalidate comparisons across the whole project on
my own judgement overnight. Flagging instead.

## 22. Corrected GOALS' gating claim rather than leaving it flagged

**Chosen.** Earlier tonight I flagged GOALS' gating section for John rather than
touching it, on the grounds that how much of a live investigation belongs there
is his call. I have now corrected it, and the reason the earlier judgement was
wrong is that this is not an update — it is a false claim.

GOALS told the reader *"Nothing tried can tell it"* about identifying which
inputs matter. g9-02 made that false and four sweeps have passed without the
document noticing. Rule 5 is explicit: falsified claims get fixed, not softened,
and standing authorisation covers documentation.

**What I did not do.** The structural question is untouched: whether GOALS should
carry live investigations at all, or record only settled results and point at the
sweeps. The corrected section is now the largest live-investigation block in it,
which arguably makes that question more pressing rather than less. Still flagged.

**Chosen: quoting the refuted sentence rather than deleting it.** It gated how
everything below it was read, so a reader who remembers the old framing needs to
see it marked. Same treatment as note 023's refuted claim.

**Chosen: recording the catch alongside the win.** The section could have said
"the tag recovers +0.16 flat across delay including delay 20" and stopped. It
also says that admitting the strongest scores identically there, so capacity and
the fade are the mechanism and g9-04's signal buys height only where the pool is
starved. A correction that only reports the good half is how the document got
into this state.

**Three guards, each checked against a stripped copy** so none is vacuous: the
sentence may appear as a quotation but not without "CORRECTED"; +0.16 must appear
within 200 characters of "delay 20", because a figure without the condition that
makes it interesting is how this document drifted the first time; and it must
still say the tag recovers a fraction and "not all of it".

## 23. R4 found a vacuous test, and the guard it needed was the real lesson

**Chosen.** Built the last named meta-test as a rail on `check_rails` rather than
a fourth tool.

**Sorting the four hits was the work.** Two were false positives — the gradient
tests in `test_attention.py` delegate to a shared `_fd_check`, and they are among
the most careful tests in the repo. Flagging those is exactly the false positive
that gets a check switched off, so the rail follows `self._helper(...)` into the
same class.

**Two were real, and both are fixed rather than baselined**, so R4 keeps zero
exemptions and stays strict like R1. `test_the_first_position_can_never_
consolidate` built a model, ran it, and asserted nothing — under a docstring
naming a real property and warning that the difference would be invisible in
aggregate accuracy.

**The guard is the part worth reading.** Writing the missing assertion, I checked
whether consolidation is observable at all on the fixture: on a 2-token stream, a
6-token stream and a repeating cycle it changes *nothing anywhere*, because it
fires on a confirmed retrieval and short streams rarely have one. So the obvious
fix — assert step 0 agrees with and without consolidation — would have passed on
a model where the mechanism was inert everywhere. It now has a companion asserting
the mechanism moves predictions at all, and the two inert fixtures are kept
deliberately.

That is the fifth time tonight a test needed a guard to not be vacuous, and the
second time I nearly shipped one without it.

**Not built, deliberately.** BACKLOG's second clause — flag tests whose only
assertion is `assertIsNotNone` or `assertTrue` on a call result — has no instance
in the repository, so it would be a rail with nothing to hold and no way to know
it works. Recorded as worth adding the first time one appears.

## 24. g9-09 answered John's priority, and the answer is "no, but"

**The result.** The tag is NOT better on small nodes. Recovery peaks at node 32
(+0.21) and declines to +0.11 at node 8; node 4 is refused with both floor and
ceiling broken. The smallest node that can run this task at all is **eight
dimensions**.

**What survives is the thing that matters.** Spread across delay is 0.04, 0.01,
0.03, 0.01 at nodes 64 down to 8, against the window's 1.85, 0.82, 0.45, 0.27.
The flatness — not having to be told the delay — holds across an eightfold range
of node sizes. That was prediction 3 and it is the only one confirmed strongly.

**Two predictions were refuted and both had their reasoning pre-registered as
suspect.** Prediction 1's level: the control implied 0.42 and the sweep gives
0.21, and the difference is the FLOOR arm, which is what "most likely wrong"
named before dispatch. Prediction 5: the signal's direction does not pay more on
a small node — the gap *shrinks* — and I had written that treating "starved by
capacity" and "starved by width" as the same scarcity was the reason it might
fail. It was.

**Chosen: marking the pre-dispatch IN PLAIN TERMS paragraph as wrong in place**
rather than deleting it. It told a plain-language reader that filtering matters
*more* as the device shrinks. That is false and it was the sweep's own
motivation, so it is quoted and corrected where it stands — same treatment as
note 023 and GOALS.

**What I would flag.** The working point (`slots` 32, `fade` 0.95) was frozen at
values chosen for `d_model` 32 in one process. I named that as the standing risk
before dispatch and it is still untested — the decline from node 16 to 8 is
exactly where a mistuned capacity would show up first. That is now the top item
on the line, and it is cheap.

## 25. The frozen working point was mistuned, and there may be a rule underneath

**The control.** At node 8, `slots` 8 recovers 0.48 where `slots` 32 recovers
0.30. The prediction was registered before it ran. So g9-09's small-node cells
measured a mistuned tag, and its decline from +0.16 to +0.11 is at least partly a
tuning artefact — which is exactly what I named as that grid's standing risk
before dispatching it.

**The reason g9-10 is worth more than a re-tune.** Retrieval goes as
`sqrt(d / N)`; note 020 measured that within 5% across a 16× range and checked it
against an analytic bound. `N` is precisely what the tag bounds. So the capacity
a node can carry should scale with the width it reads through — and if the best
`slots` tracks the node, **the tag has a tuning rule derived from a law this
project already measured, rather than a constant chosen once.** Every node would
set it from something it knows about itself. That is prediction 1.

**Chosen: naming prediction 2 as most likely wrong.** It says recovery stops
falling with node size once each node uses its own best capacity — the thing that
would change g9-09's conclusion. `fade` stays frozen at 0.95 for cost, so if the
best fade also moves with node size, a tuned-capacity-but-mistuned-fade cell
still declines and 2 fails for a reason that has nothing to do with capacity.
That is this grid's standing risk in turn, stated in the file.

**A near-miss worth logging.** The first version of the control ran 12 cells × 6
arms at width 64 and was still going after fourteen minutes. That is heavy local
work, which the standing rules forbid, and it is precisely the failure `g8-05`
recorded — "a quick control at one cell" that held the machine for ten minutes.
Killed and cut to six trainings at one node, which answered the question in two
minutes. Recorded in the sweep file's COST section rather than only here, because
the next person sizing a control will read that and not this.

**Also confirmed in passing:** the `combined` arm runs, and at `slots` 32 it
scores identically to the tag — the same degeneracy its own control found. Small
capacity is the one regime where the two mechanisms hold different writes, which
is why it is in this grid and not its own.

## 26. Two cheap controls changed the account of the whole g9 line

**What happened.** With g9-10 in flight I ran four no-training controls, minutes
of compute, counting what each gate keeps against `position_kinds()`. They
overturned a note I wrote this session and deflated g9-06's explanation.

**First: the tag has 100% recall and 3.4% precision.** Note 025 said its
shortfall is write-time ignorance. It keeps every rewarded binding at every
delay, including delay 20 where the window keeps none. The missing four fifths
are interference, not ignorance — so replay, which addresses ignorance, was aimed
at a problem that does not exist. Marked at the top of note 025 rather than
rewritten away.

**Second: there is a ceiling and it belongs to the task.** One binding in six is
rewarded, so a perfect binding-detector still keeps six writes per useful one —
16.7% precision. The tag at its best capacity is at 70% of that; a matched window
is at 76%. Binding-detection is nearly exhausted, which retroactively explains
why every signal improvement (the inverted ranking, `tag_relative`) only paid
where capacity was starved: they were competing for 30% of a 16.7% ceiling.

**Third, and the one I would want checked: the tag keeps 33 bindings at delay 8
and 27 are rewarded.** Nothing local predicts reward, so it is not selecting
rewarded bindings — it is selecting bindings *near the reward*, because the fade
is a soft window anchored at the capture. **The tag is a soft window with a
capacity bound.** g9-06's flatness is measured at `slots` 32, where recall is
100% at every delay because the pool keeps essentially everything — the worst
precision of any setting. It buys delay-independence by admitting so much that
the delay stops mattering.

**What I did NOT do.** g9-06 and g9-09's recoveries were measured with training
and stand; only the account of *why* changes, and note 026 says so explicitly.
These are 8 sequences at one seed with an untrained readout, and g9-05 already
demonstrated that what a gate keeps and what a trained model scores can diverge
sharply — the un-faded tag captured 9% and scored −0.20.

**Where it leaves the line.** The sharpest open question is now whether anything
can identify *which* of the six bindings without being told the delay. It needs a
probe in g9-04's shape — AUC against "is the rewarded binding", among bindings
only — and that has never been asked. It either finds the signal or bounds the
line at about 20% of the oracle, which would close it honestly.

## 27. g9-10 landed, and I did not dispatch the obvious follow-up

**The result.** The best capacity tracks the DELAY, not the node — 16 at delay 8
and 32 at delay 20, at every node width. Prediction 1 refuted. And the tag's
flatness belongs to `slots` 32 alone: spread 0.01 there against 0.57 at `slots`
8. That was addendum A2, derived from a two-minute counting control and confirmed
by 24 trained cells. **The mechanism-derived addendum outscored the pre-dispatch
predictions**, which is the argument for counting before guessing.

**The surprise.** `combined` scores +0.26 at `slots` 4 where the tag alone scores
−0.02 — the highest tag-family recovery in the project, at the smallest capacity
tried. Prediction 4 called it exactly, for a mechanism that had never been swept.

**Chosen: NOT dispatching g9-11.** `slots` 4 is the bottom edge, which is a
pinned axis by the rule that caught g9-05, so a follow-up is indicated. A
counting control says it would find a flat row: at delay 8 the union's recall is
pinned at 100% by the window and its precision approaches window-alone from
below, so smaller capacities change nothing; at delay 20 the union is worse than
the tag alone. Four minutes of control against a 24-job matrix.

**What I want reviewed, because it is a limit on a tool I have been leaning
on.** The recall × precision product got g9-10's peaks right across capacities
and gets this comparison wrong across mechanisms — it ranks window-alone above
combined at delay 8 (0.112 vs 0.106) where training measured the reverse (+0.23
vs +0.26). So whatever the union adds is in *which* writes it keeps, not how
many, and the product cannot see that. I have used it three times tonight and
this is the first case where it fails; the failure is in the direction of
under-rating a mechanism, which is the safer direction but not a safe one.

**The one axis nobody has swept** is the combined gate's window reach, frozen at
8 everywhere. Reach 1 would add two writes per capture instead of nine. Logged in
BACKLOG rather than dispatched, because it needs predictions written first and
the control above explicitly cannot settle it.

## 28. My own commit frequency was starving the sweep

**What I found.** g9-11 sat with zero jobs started for a long stretch. The cause
was not GitHub being busy in general — it was four superseded `checks` runs ahead
of it, each seven jobs (the suite plus six mutation shards). I have pushed
roughly ten times this session; that is seventy jobs queued against a sweep's
fifteen, and sweeps have their own concurrency group so they cannot jump the
line.

**Chosen: cancel the superseded runs, then stop it recurring.** `checks.yml` now
carries `concurrency: checks-${{ github.ref }}` with `cancel-in-progress: true`.
A new push cancels the previous run for the same ref, which loses nothing — the
superseding commit contains the code the cancelled run was checking, and its own
run covers it.

**And a convention in CLAUDE.md**, because the config fixes the recurrence and
not the habit: commit frequency is a resource decision when a sweep is in
flight, and related work should be batched into one commit rather than pushed
piecemeal.

**Worth review.** `cancel-in-progress` on a checks workflow is a trade: if two
commits land close together and the first would have failed, its run is
cancelled and only the combined state is checked. That is standard practice and I
believe it is right here, but it is a deliberate reduction in coverage and should
be an explicit choice rather than something I slipped in while unblocking myself.

**The uncomfortable part** is that I spent several cycles reporting "g9-11 is
queued, runners are congested" as though it were weather. It was a consequence of
my own behaviour, visible in `gh run list` the whole time, and I did not look
until the fourth cycle.

## 29. The fix I proposed for the task leak would not have fixed it

**Chosen.** Before anyone spends a nine-sweep re-baseline on note 027's proposed
one-line fix, I built the fixed variant locally and measured it.

**It does not work.** Randomising the gap between bindings leaves the leak intact
at every jitter up to 0.9 — the nearest-binding rule stays at 100% until the
spacing is wildly irregular, and *zero* unrewarded bindings land at the rewarded
offset in any condition.

**The reason is that I named the wrong cause.** Note 027 blamed the lattice. The
lattice is why the *nearest* rule is exact, but the discriminator is that the
reward sits a constant `delay - 1` after its own binding — which randomising the
spacing does not touch at all.

**The fix that would work is randomising the delay per rewarded pair**, so the
offset becomes a distribution instead of a constant. That stops `delay` being a
swept axis, which means it changes what the task *is* rather than just
re-baselining its numbers. A materially bigger decision, and still John's.

**What I want noted about the process.** Note 027 proposed a fix in the same
breath as diagnosing the defect, and the fix was wrong. The diagnosis was right
and the remedy did not follow from it — I reached for the visible structural
feature (the lattice) rather than the one doing the work (the constant offset).
Measuring the proposed fix before recommending it cost four minutes; acting on it
would have cost nine sweeps and left the leak in place.

## 30. g9-11 answered its question, and I made the error I had flagged

**The finding, and it recasts the mechanism.** `combined − reward` in the same
cell is **+1.19 to +1.23 at reaches 1, 2 and 4**, and **−0.01 at reach 8**. At
reach 8 the window is matched to delay 8 and the tag adds nothing; at shorter
reaches the window is catastrophic at −1.53 and the tag drags it back to −0.31.

So the combined gate is not "two signals combining". **It is a floor under a
badly set reach** — which is the useful reading, because a node cannot know the
delay and therefore cannot set the reach. The tag's contribution is precisely
that it covers a wrong guess.

**Four of five predictions held**, including both wiring checks: the window's
cliff sits exactly at the matching reach (reproducing g9-03 on a new axis months
later), and the `tag` row is flat across reach to 0.000 spread, so the arms are
independent.

**The error.** I fixed `slots` at 4 because it was g9-10's best for the combined
arm — *at node 32*. This grid runs at node 64, where g9-10's own trend predicts
`slots` 4 is badly wrong, and the tag alone duly scores −0.35. **That is the
error I named as g9-09's standing risk and then repeated one sweep later.**
Naming a risk in a sweep file does not stop it recurring in the next.

**What I checked before deciding the run was salvageable.** The reported quantity
is a difference within a cell, and both arms share the capacity — so it isolates
the tag's contribution whatever the capacity is. That is why the summariser was
written to print it. The finding stands; the absolute recoveries do not, and the
sweep file says no number in the arm tables should be quoted.

**Worth review:** whether "name the frozen axis as a risk" is worth anything at
all, given it has now failed twice in a row. A check that refused to run a sweep
whose fixed parameters came from a different width would have caught both.

## 31. The provenance habit found a frozen learning rate in seven sweeps

**What happened.** Last cycle I wrote a calibration into CLAUDE.md ending with a
habit: when a sweep pins a value from an earlier sweep, write down which cell it
came from, next to the pin. Applying it to g9-11 immediately surfaced that `fade`
was also carried across configurations. Applying it to the whole line surfaced
something larger.

**`lr 0.05` is FIXED on every arm in all seven sweeps g9-05 through g9-11.** It
was chosen for g9-03's workflow at `d_model` 32 in one process, and carried
through every subsequent change of width, node count, capacity, fade and reach.
The scripts can sweep it — `(0.02, 0.05, 0.1)` is their default — and every
workflow passes `--lr 0.05` and turns it off.

**Why it matters.** g8-01's re-summarisation, done earlier tonight, measured the
learning rate moving the floor arm by a factor of three: gap 0.196 at lr 0.02
against 0.612 at lr 0.1. That is the denominator of every recovery ratio, and
GOALS was corrected for precisely that overstatement. So the g9 line has been
dividing by a quantity whose scale nobody has checked at any of its
configurations.

**What I checked before writing it up.** Whether the ordinal findings survive.
They do: every arm in a cell shares the rate, so `tag` against `window` against
`combined` is fair whatever 0.05 turns out to be. What is at risk is the *scale*
— "recovers a fifth of the oracle" — not the *ordering*. Note 028 separates those
explicitly rather than implying everything is in doubt.

**Not dispatched.** The fix is one deleted flag and costs 3x the jobs of whatever
grid it joins, and the g9-11 re-run is holding the matrix. Logged as BACKLOG item
0 with the cheapest useful version named: g9-09's shape with the rate swept,
because node width is the axis where the floor arm moves most.

**Worth review, and it is the reason to keep the habit.** Seven sweeps named
"check the frozen axes" in their own files and none of them looked at `lr`. Two
cycles of writing provenance down found it. That is an argument that warnings do
not work and inventories do — and it generalises past this project.

## 32. I overstated a warning, measured it, and corrected it the same cycle

**The claim.** Note 028 said `DECAY` being frozen meant *"the conclusion 'the fade
is a reach dial' was drawn with the other reach dial held still"* — implying the
conclusion might be wrong. I wrote that from an argument, not a measurement.

**The measurement.** Counting only, no training: `decay` moves rewarded-binding
recall by at most 13 points, and only where recall is not already saturated. At
delay 8 with `fade` 0.95 it does nothing at all. So freezing it shifted some
numbers and **did not invalidate the conclusion**.

**Chosen: correcting the note rather than softening it.** The warning was
published in the same cycle it was written, on an argument, and CLAUDE.md's first
rule is to state no behaviour that has not been observed. The corrected text says
plainly that the original wording overstated it.

**And reading the table caught something about a tool I leaned on three times.**
A capacity-bound tag keeps a constant number of writes — `min(slots, writes)` per
capture — so precision is recall divided by a constant, and the "recall ×
precision product" I used to predict g9-06, g9-10 and g9-11 is really
**recall² / slots**. It was never two independent quantities.

That is still a usable ranking function, and it is why it ordered g9-10's peaks
correctly. But the *reasoning* attached to it — that a mechanism trades recall
against precision — does not describe a capacity-bound tag, where the two cannot
move independently. It also explains the single case where it failed: comparing
`combined` against `reward`, `kept` differs between the arms, the constant stops
cancelling, and the product stops being monotone in recall.

**Worth review.** Two of my last three notes contained a claim stronger than what
had been measured, and both were caught by measuring within a cycle. The pattern
is that a diagnosis and its implications get written in one sitting, and the
implications do not get the same scrutiny as the diagnosis.

## 33. I swept the frozen learning rate, and the summariser's smoke run found a
##     hole in how this project reads a difference

**Chosen: g9-12, four jobs, the rate swept.** `lr 0.05, FIXED on every arm` sat
in the grid of g9-05 through g9-11 — every sweep in the line — chosen for g9-03
at `d_model` 32 and carried through every change of width, node count, capacity,
fade and reach since. g8-01 had already measured the rate moving the FLOOR arm,
the denominator of every recovery ratio, by a factor of three.

**Why now rather than earlier: the cost turned out to be a quarter of what it
looked like.** `g9_05_the_tag.py` sweeps `LEARNING_RATES` *inside* a job whenever
`--lr` is omitted, so dropping the flag triples the compute per job and adds no
jobs at all. Four jobs. There was never a good excuse for not doing this, and
the reason it went undone for seven sweeps was an unexamined assumption that
sweeping an axis multiplies the matrix.

**Chosen: delay 8 only.** g9-10 established `slots` 16 is best at delay 8 and 32
at delay 20. Running both delays under one capacity would measure a mistuned cell
and confound the rate question with the capacity question — which is exactly the
error g9-11 made once already.

**Chosen: `reach` pinned on the command line at 8, though `REWARD_WINDOW` would
supply the same value by import.** A constant that is right by luck is still a
constant nobody checked, and the import path is how `KEY_SCALE` and `DECAY` got
into seven sweeps without appearing in any grid.

### The part worth reviewing

**I smoke-tested the summariser on fabricated records before spending four jobs,
and it reported a finding that was not there.** It announced *"the best rate
MOVES with node width"* from three rates whose ratios were identical by
construction. `max` over a swept axis always names something, and it was breaking
exact ties arbitrarily.

Real data will not tie exactly. It will tie *nearly*, and then the same line
prints the same claim and nothing looks wrong.

**The fix is `assess`'s second refusal one step further on.** That refusal
already rejects a cell whose DENOMINATOR is inside the seed spread. A DIFFERENCE
BETWEEN TWO RATIOS is noise by the same standard. So `margin()` and `winner()`
went into `tools/recovery.py` beside the refusals rather than into one
summariser — that module exists precisely because five hand-copies of this
reasoning had already drifted apart.

`margin()` divides the spread by the gap because **the spread is measured in
accuracy and a lead is measured in recovery**. Comparing them undivided compares
two different quantities, and at a gap near 1.0 it very nearly works, which is
what makes it the dangerous version. It is pinned as a mutation for that reason.

The guard now applies in three places: the per-arm tables print `tied/noise`
rather than naming a winner, prediction 2's verdict fires only on a cost that
beats its own noise floor, and an ordering change is reported as one only when
the swapped pair is separated by more than the spread — otherwise it is the same
tie broken twice.

**The general lesson, and it is the third time this pattern has appeared.** The
last two entries here were about claims stronger than what had been measured.
This is the same failure moved into a tool: a summariser that always produces a
verdict will produce one from noise, and the verdict reads exactly like a result.
Smoke-running it on fabricated data — including data fabricated to have NO
finding in it — is cheap and I should do it for every summariser, not just this
one.

482 tests, 106 mutations, five checks clean. Run 30251816417.

## 34. The sharpest open question is answered, and the probe's own tripwire
##     refuted the design it was built on

**BACKLOG item 3 — can anything identify WHICH binding without being told the
delay — is answered.** Only recency can, and recency is note 027's leak. Every
signal that is not recency sits within 0.03 of chance: `strength` 0.526,
`surprise` 0.483, `hit` 0.498. So there is no second signal waiting behind the
leak, and note 026's ceiling stands.

**Chosen: one observation-only trace field rather than a new probe-only code
path.** At a capture step every quantity the trace already carried — surprise,
strength, the running mean — is a property of THE STEP, identical for every
candidate, so none of them can rank candidates. Only two candidate-specific
things existed: what was recorded at the write, and how long ago it was.
`pending_now` is the third: a node holds `pending`, so it can ask its own store
what each pending key retrieves now. Nothing reads it back, and five tests in
`test_tag.py` pin what it means — including that it is measured BEFORE the
unprotected writes are removed, since a gate cannot consult the result of its
own decision.

**What is new is that the leak is reachable from inside a node.** Note 027 read
the generator's offsets, which no running system can do. `pending_now` separates
the rewarded binding at **AUC 0.972 pooled across delays**, which is the first
demonstration that a mechanism could actually exploit the layout rather than the
flaw merely being present in the data.

### The part worth reviewing

**The probe pre-registered a check and the check refuted the probe's own design.**
It assumed that pooling four delays would make the delay effectively unknown, and
registered *"age near 1.0 WITHIN a delay and near 0.5 POOLED"* as the
confirmation that the pooling had worked.

**Age is 0.000 pooled** — perfect, inverted, at every delay.

The rewarded binding is not "the write `delay` steps back". It is the most recent
binding before the reward at ANY delay, because bindings sit about 31 steps apart
and every delay tested is shorter than that. There was no delay dependence for
the pooling to remove, and **"being told the delay" was never what a window was
really using here.** The whole framing of this question, carried in BACKLOG for
several cycles, rested on that assumption.

Recorded rather than quietly repaired. The wrong assumption is the interesting
part, and it is the second time this session that an instrument has caught a
claim I was about to make from an argument rather than a measurement.

**What it does NOT show, stated because the number invites overreading.** The
candidates were BINDINGS, selected with the oracle. A real gate must find
bindings first, and g9-04 put that at AUC 0.22. So 0.97 among bindings is not
0.97 in a running gate, and this does not overturn item 1's finding that the leak
is inert. It relocates the bottleneck: the leak is inert because binding
DETECTION is weak, not because which-binding is hard — and only one of those is
a property of the task.

487 tests, 106 mutations, five checks clean.

## 35. The testbed cannot run a gated model, and BACKLOG understated it

BACKLOG carried *"the testbed has never run a gated model"* as a one-line item.
Reading `distributed.Node.step` while plumbing the gate into the driver showed it
is worse than that.

**`Node.step` is a REIMPLEMENTATION of the model's inner loop, not a call into
it.** A memory, a previous key, a readout. No `pending` list, no reward token, no
tag, no consolidation. A config carrying gate settings is accepted, ignored, and
answered anyway — so the network does not fail, it returns a confident wrong
number.

**Measured rather than asserted.** A network handed `reward_token` and
`reward_window` returns a result identical to the UNGATED single-process model
and different from the gated one. Two tests pin it, with a guard that the gate
changes the single-process answer at all, so the comparison cannot pass because
the gate was inert.

**This scopes every "the split is exact" claim in the project.** That exactness
was measured on the ungated inner loop, where it holds. It has never been
measured for any mechanism the entire g9 line is about, and nothing in the record
said which of the two it covered.

**Chosen: stop here rather than start the build.** The fix is not plumbing.
Either the gate is implemented a second time on `Node` — duplicating logic that
`test_tag.py`'s mutations protect, in a file those mutations do not touch — or
`LocalAssociativeMemory` grows a step-wise API the node calls. The second is
right and the first is what will be tempting, because the first is an hour and
the second is a refactor of the loop every sweep runs through.

Starting that with a sweep about to land and finishing it half-done would be the
worse outcome. It is written up with the boundary pinned by tests, so the first
of them fails the moment the gate reaches the node and says why.

**Also fixed on the way past:** `testbed/driver.py` builds its own
`LocalMemoryConfig` rather than calling `node_main.config_from_env`, which is the
silent-drift risk that function's own docstring warns about — the two currently
agree by coincidence of matching literals. Left alone deliberately: changing it
belongs with the refactor above, not ahead of it.

489 tests, 108 mutations.

## 36. The frozen learning rate cost nothing — and the sweep that showed it
##     found a bigger problem underneath

**g9-12 ran, 4 of 4, and none of its four predictions was confirmed.** The
largest recovery left on the table by holding `lr` at 0.05, that beats its own
noise floor, is **+0.00**, at every node width. The raw largest is +0.03. The
floor arm moves by at most 0.072, so g8-01's factor of three does not reproduce
here.

So the largest standing methodological worry in the project — seven sweeps of
numbers taken at one unexamined constant — is retired, and note 028's headline is
corrected in place: it was right that the rate was unexamined and wrong about
what that was worth.

**Prediction 3 is the one worth reading.** It predicted the arm ordering is
unchanged at every rate. The raw ordering changes at ALL FOUR node widths — and
every swap is between arms sitting within the seed spread of each other, so it is
one tie broken three ways. The reassurance survives and the naive version of the
check would have reported it broken four times. That is exactly the noise floor
added earlier this session, doing the job it was added for, on real data.

### The bigger problem, which nobody had put a number on

**Three seeds is not enough to bound a small effect anywhere in this line.**
g9-12 is the first sweep to print the seed spread in recovery units:

    node 64   0.29     the tag's ENTIRE effect there is 0.23
    node 32   0.18
    node 16   0.15
    node  8   0.12     the tightest, and still half the tag's effect

So g9-12's own answer is honestly *"no rate effect larger than the seed spread"*,
not *"no rate effect"*. And the same bound applies to every comparison in g9-05
through g9-12, all of which used three seeds: **any published difference in this
line smaller than about 0.15 was never distinguishable from zero.**

Two live claims sit near that boundary and I have flagged both rather than
quietly leaving them: the tag's +0.16 flat row from g9-06, and the
tag-versus-matched-window gap of 0.07.

**Recommendation, recorded as item 0b rather than acted on.** Before any further
mechanism sweep, re-run one settled grid at 12 seeds and see which differences
survive. Seeds are the cheapest axis there is — they parallelise perfectly and
add no cells to reason about — and this is a better use of a matrix than a new
mechanism measured to a tolerance that cannot see it.

I have not dispatched that, because deciding which grid to re-baseline changes
what the comparison set means, and that is closer to John's call than mine.

**An observation nobody asked for.** `tag-strongest` runs -0.51, -0.10, +0.02,
+0.05 as the node narrows from 64 to 8. The inverted signal's direction is worth
nothing at a wide node and turns positive at a narrow one — the third setting
showing that shape, and the clearest, because node width is the only axis moving.

**A process note.** I reached for a bash heredoc twice this session despite the
standing rule against it, and both times it hung the shell and had to be killed.
The rule is right and I should stop testing it.

## 37. I overstated the seed problem, and the correction arrived within a cycle

Entry 36 concluded that **three seeds cannot bound any difference smaller than
about 0.15** anywhere in the g9 line, and recorded it as BACKLOG item 0b with a
recommendation to re-run a settled grid at twelve seeds.

**That was read off the wrong statistic.** `margin` divides the seed RANGE by the
gap. Re-reading g9-12's own records — free, they were already downloaded — with
the ratio computed INSIDE each seed gives standard errors of 0.01 to 0.06:

    node    range-margin    paired SE    2 SE on a difference
      64        0.28         0.03-0.06        0.137
      32        0.18         0.01-0.02        0.041
      16        0.15         0.02-0.03        0.067
       8        0.10         0.03-0.05        0.103

At node 32 the measurement is **fifteen times tighter** than I said. The rate
verdict is unchanged — every lead is inside 2 SE — but g9-12 bounds the rate
effect below 0.04 there rather than below 0.18.

**Why the range was wrong for this.** Averaging accuracies across seeds and
dividing once charges the mechanism for seeds whose data was simply harder. A
seed whose `none` ran low and whose `oracle` ran high has a large gap for reasons
that have nothing to do with the arm being scored. Pairing inside the seed
removes it entirely, and **CLAUDE.md's own rule — per-seed values, not means —
already said to do this.** The ratio was the single place it had never been
applied, because the ratio was defined before the rule existed.

**How it was caught, which is the part worth keeping.** I was about to dispatch
the twelve-seed re-run when checking the statistic I would read it with showed
the range GROWS with sample size. A twelve-seed run scored through `margin` would
have reported every difference as *less* significant the more evidence was
collected. The re-run would have concluded the opposite of the truth, and it
would have looked like a result.

That check cost nothing and saved twelve jobs plus a wrong conclusion. It is the
same shape as the g9-13 smoke run and the vacuous `pending_now` test: **the
instrument was wrong in a way that produced plausible numbers**, and only
examining the instrument found it.

**Item 0b is downgraded rather than deleted.** An SE from three samples is itself
noisy, and node 64 and node 8 remain loose at 0.10-0.14. More seeds would still
help there; they are no longer urgent, and they are no longer the top item.

**One finding the pairing exposed that the mean was hiding.** At node 8, lr 0.05
and 0.1, a seed is DROPPED because its own oracle did not beat its own floor —
that seed measured nothing, and averaging it in concealed it. **Node 8 is at the
edge of where `reward_recall` works at all**, and node 8 is the width John's
priority is about. That is a better-targeted worry than the one entry 36 raised.

Two mutations pin the new arithmetic, both caught:
`the-error-does-not-shrink-with-seeds` reports a standard deviation instead of a
standard error, so more evidence never sharpens anything;
`the-ratio-is-not-paired-to-its-own-seed` divides by a shared constant and puts
each seed's difficulty straight back into the numerator, still returning
plausible ratios in the right rough range.

## 38. The two headline claims, re-read from archived records -- one got
##     stronger, one shrank, and I had written a direction backwards

`per_seed` made it possible to re-read finished sweeps without re-running them.
Both headline claims of the g9 line were checked against records still on
Actions. No new jobs. [Note 029](docs/notes/029-the-headlines-re-read-with-a-paired-ratio.md).

**g9-06's +0.16 flat row is CONFIRMED and now has an error bar**: +0.159, +0.164,
+0.172, +0.162 with standard errors of 0.010 to 0.020 at delays 1, 4, 8 and 20.
Every delay is within one standard error of every other, so the flatness is not
an artefact of averaging. The project's main claim stands and stands better.

**THE CATCH is confirmed and upgraded from a point estimate to a measurement.**
It rested on `tag-strongest` scoring +0.003 away from `tag` at the working point.
Paired: at slots 32 fade 0.95 the difference is -0.008, +0.002, +0.016, +0.005,
every one inside 2 SE. At the starved pool, slots 16 fade 0.99, it is +0.180,
+0.204, +0.219, +0.283, every one far outside. **Both halves now hold**, where
before there was one number being close to another.

**g9-09's "height peaks at node 32 and falls" is only half supported.** Node 32
over node 64 is +0.061, +0.019, +0.029 against combined 2 SE of 0.066, 0.079,
0.034 -- two comfortably inside, one on the line, so the fall is not
distinguishable from a plateau. Node 32 over node 8, and node 16 over node 8, are
real by many standard errors, so what the tiny-node question needs survives:
recovery falls off between node 16 and node 8, and node 8 still recovers about
+0.10. Where the peak sits does not.

### The part worth reviewing: I wrote a direction backwards last cycle

g9-12 closed with an observation that `tag-strongest` runs -0.51, -0.10, +0.02,
+0.05 as the node narrows, and I concluded *"the inverted signal's direction is
worth nothing at a wide node and turns positive at a narrow one."*

That reads `tag-strongest`'s own absolute recovery. **The value of the DIRECTION
is the gap between the two arms**, and in the same data it runs 0.74, 0.35, 0.17,
0.10 from node 64 down to node 8 -- worth **most at the widest node**, the exact
opposite of what I wrote. g9-09 agrees independently: +0.222 at node 64 against
+0.028 at node 8, at delay 20.

**Why it matters more than a slip.** This line has repeated that g9-04's signal
"pays where something is scarce", which is true for the axis it was established
on -- a starved capacity, now measured at 0.18 to 0.28 outside 2 SE -- and does
not carry over to node width, a different scarcity pointing the other way. One
sentence was covering two axes, and that is how a real finding becomes a slogan
that quietly stops being checked.

Corrected in the g9-12 sweep file in place, with the original quoted.

**What this does not do.** It adds no seeds. Every number is three seeds and a
standard error from three samples is itself uncertain. Pairing extracts more from
the same records; it does not manufacture evidence. Node 4 in g9-09 has one
usable seed and an infinite error, which is the honest report of a mostly-refused
cell.

## 39. I nearly published a false correction to a correct result

Continuing note 029 across g9-10 and g9-11, the paired read of g9-11 came back
wildly different from what had been published -- `tag` at -1.527 against a
recorded -0.14. The run's own `summary.txt`, read from the same directory, agreed
with the alarming version. The records said `slots 4` where the workflow said 16.

It looked like a fabricated results table, a tripwire recorded backwards, and a
bad calibration propagated into CLAUDE.md. I was one step from writing all of
that up.

**It was my own contaminated directory.** The download command was

    gh run download 30249953943 -D "$S/g911" >/dev/null 2>&1

and that directory already held a DIFFERENT run's artifacts from an earlier
cycle. `gh` failed with "file exists", `2>&1` swallowed it, and the analysis ran
happily on the wrong run.

**Re-downloading each run into a fresh temporary directory, with errors NOT
suppressed, confirms the published results exactly:**

    tag, paired            published
    delay  1   +0.237 ± 0.022     +0.24
    delay  8   +0.221 ± 0.060     +0.23
    delay 20   -0.146 ± 0.075     -0.14

The union's value is +1.791 ± 0.162 at delay 8 and +1.490 ± 0.059 at delay 20,
REAL by more than ten standard errors. The 0.58 capacity calibration stands.
**Nothing needed correcting.**

### What actually went wrong, and the rule that comes out of it

Two habits combined into a near-miss:

- **Suppressing stderr on a fetch.** `>/dev/null 2>&1` on `gh run download` turns
  a hard failure into stale data, and stale data is worse than no data because it
  analyses cleanly.
- **Reusing a scratch directory across cycles.** Artifact names repeat between
  runs of the same workflow, so a reused directory silently mixes runs.

**The rule: fetch into a fresh directory, never suppress the fetcher's errors,
and verify the run's identity from the DATA before reading any number.** The
script writes a `condition` string carrying its actual parameters; that is the
authority, not the workflow file and not the folder a zip landed in. The clean
script does this and would have refused immediately.

**And the near-miss is the point.** Every check this session -- the g9-13 smoke
run, the vacuous `pending_now` test, the range-versus-error mix-up -- caught an
instrument that produced plausible numbers. This one caught the same class of
failure in my own analysis pipeline, and the consequence would have been the
worst kind: **retracting a correct finding**, which is more damaging than
publishing a wrong one because it destroys a real result and the record of it.

I have deliberately not deleted the alarm from this log. A correction that turns
out to be unnecessary is exactly as informative as one that is.

### One real finding did come out of it

**g9-10's capacity choices are mostly ties.** Only one of six cells picks a
capacity distinguishable from its rivals at 2 SE. Cell by cell, g9-10 chose on
differences inside the noise, and every sweep that pinned `slots` from it
inherited that.

**But the pattern across cells is perfectly consistent** -- all three node widths
pick 16 at delay 8 and all three pick 32 at delay 20. Six independent cells
agreeing is evidence the individual comparisons do not carry alone. So *the best
capacity tracks the delay* survives as a pattern, and *this capacity is best at
this cell* does not. The pins actually taken from g9-10 were the pattern's
values, so the use was sound even though the per-cell justification was not.

## 40. The corpus benchmark ran, and its first result was a stability bug

**BACKLOG item 5 is started and goal 2 has its first measurements**, though not
yet its answer. Three things happened and the middle one is the important one.

### The corpus needed no decision

`docs/notes` gives 210,216 training characters over 86 symbols after folding
rare ones into UNKNOWN. Real English, real Zipfian statistics, no download, no
data committed. It is explicitly NOT a standard benchmark; bundling one is a
decision about what to commit and remains John's.

Three ways such a benchmark quietly cheats, each with a test: the same document
on both sides, a vocabulary built from test text, and a positional split (which
on notes numbered in time measures drift rather than generalisation, so files are
assigned by hash of name).

Bars measured before anything was built: **uniform 6.426, unigram 4.756, bigram
3.711, trigram 2.934**. Bigram is the bar and it is the fair one -- binding the
previous token to the current one IS a bigram in vector form.

### g10-02: not undertrained, underfitting

g10-01 pre-registered `EPOCHS 2` as the frozen axis most likely to be wrong and
registered the follow-up in advance. Run: the model **peaks at epoch 1** and then
oscillates without improving, and on text it has already seen it reaches 5.78
bits where a bigram on that text reaches 3.638. Train and test sit on top of each
other, so it is not overfitting either. It is not learning the text.

The script originally printed *"the conclusion is about the architecture"*. That
is stronger than one cell supports and I corrected it **in the script**, not only
in the write-up -- a script printing a claim its own sweep file calls unsupported
is worse than either alone.

### The part worth reviewing: three attempts, two of them wrong

g10-01's chunk axis returned 37 bits per character and NaN. Over an 86-symbol
vocabulary whose uniform cost is 6.426, those are broken numbers, not bad models.

**First guess: divergence.** I wrote a diagnostic, trained it on 40 chunks, saw
nothing exceed 2, and concluded the store was fine. **That check was
under-powered** -- the real run trains on 645 chunks -- and I acted on its false
negative.

**Second guess: a leaky calibration.** The held-out calibration chunks came from
documents that had been trained on. That IS a real flaw, and it is exactly what
`corpus.py`'s own docstring demands for the train/test split, which I had then
got wrong in the calibration split. I fixed it. **It did not fix the numbers.**

**Third: measure instead of guessing.** Bits were identical at all thirty
temperatures and score magnitude was 2.3e72. The readout had run away, and
"temperature pinned at the grid minimum" was a tie among thirty equal values.

So the first guess was right and my own weak check had concealed it. **The
lesson is not "guess better" -- it is that a diagnostic needs the scale of the
thing it is diagnosing.** Forty chunks against six hundred was not a small
shortcut; it inverted the answer.

**The fix was a setting the model already had.** `memory_cap` bounds the fast
store's norm and defaults to OFF. With it, chunk 256 gives 5.72 bits and accuracy
0.18. It is now a swept axis, because 5.0 and 1.0 disagree by 0.13 bits and
freezing the value found in the one diverging cell is the frozen-constant mistake
this project keeps catching.

**Why it bites here and not in the g9 line**: `reward_recall` applies the delta
rule only at query positions; a language model applies it at every position, so
the feedback loop gets hundreds of times more chances per sequence. **Dense
supervision is the condition, not sequence length.** That is worth knowing before
any future task with dense targets is built.

`openplexus/ngram.absurd` now refuses a non-finite cross-entropy or one more than
a bit worse than uniform, and it lives beside `uniform_bits` rather than in the
experiment, with eight tests and two mutations. Its vacuity guard matters: a rule
that refused everything would pass every other test in the class while making the
benchmark useless.

546 tests, 115 mutations.

## 41. A stray file was riding along in every artifact of every sweep

**Found by an invariant written for something else.** Two cycles ago a
contaminated scratch directory nearly made me retract a correct result, and the
fix was CLAUDE.md rule 11b: fetch into a fresh directory, never suppress the
fetcher's errors, and **verify a run's identity from the DATA before reading a
number off it**.

Reading g10-01's re-run, that check refused the download: *"records lack `cap`:
this is the OLD run, not the re-run"*. It was not the old run. Each artifact held
**eight files where four were results**.

`out/g10-02.json` — a local diagnostic's output — had been committed by a
`git add -A`, and every workflow uploads `path: out/*.json`. From that commit
onward every sweep artifact carried a foreign record set, with `epoch` and
`test_bits` where a sweep expects `cap` and `bits_calibrated`.

**The invariant was written to catch a stale download and caught a completely
different failure.** That is the argument for cheap general invariants over
targeted ones, and it is the second time this session that a guard has paid for
itself on a problem it was not designed for.

### What it would have done

An aggregate step reading those records raises `KeyError` mid-run, which is the
good case. The bad case is a summariser that tolerates the extra records and
reports a confident table computed from whichever survived.

### Three fixes, at three depths

1. **The root.** `out/` untracked and gitignored, with the reason written beside
   it rather than left as a bare pattern.
2. **The guard.** `tools/recovery.require(rows, *fields)` keeps only records
   carrying every named field and **prints what it dropped**. Filtered rather
   than refused, because a job may legitimately write more than one kind of
   record; printed rather than silent, because quiet discarding is how a
   confident number gets computed from half the input.
3. **The placement, which is the part worth reviewing.** I first wired the guard
   into `summarise_g10_01` alone. That protects one summariser out of twenty,
   all of which call `load()` and read whatever matches `out/*.json`. So it moved
   into **`by_cell`**, the single function every summariser goes through — one
   change, no callers edited, and no chance of protecting whichever nineteen were
   remembered.

Moving it also deleted a duplicate: the filtering I had written into `by_cell`
and `require` itself were near-identical, which is precisely the drift
`tools/recovery.py` exists to prevent — that module exists because five
hand-copies of the same two refusals had already diverged.

One mutation, `foreign-records-are-dropped-in-silence`, breaks the ANNOUNCEMENT
rather than the filtering. Caught. Three tests on the guarantee, including the
vacuity guard: a filter that dropped everything would satisfy the other two while
making every summariser report nothing.

### Also this cycle

**The re-run's predictions were registered before its results landed.** The sweep
file still described the old width x chunk grid, and pre-registration that
arrives after the numbers is not pre-registration. Six predictions, with R3 named
as the one that decides the line: whether width 128 comes within 0.5 bits of the
unigram, separating *g10-02's underfitting is width-limited* from *the ceiling is
not capacity*.

Four of six cells are home and **width is buying almost nothing** — 0.058 bits
from doubling 32 to 64, against a 0.98-bit gap to the unigram. R3 would need
about seventeen times that effect from the last doubling.

554 tests, 116 mutations.

## 42. The corpus line, end to end -- and it corrected itself three times

Goal 2 now has measurements rather than an intention. The arc is worth reading as
one thing because **each run refuted the previous run's conclusion**, and the
refutations were the point rather than an embarrassment.

### What was measured, in order

**g10-01** put the model on real text for the first time. 210,216 training
characters of this project's own notes over 86 symbols -- not a standard
benchmark, and bundling one remains John's call. Bars measured before anything
was built: uniform 6.426, unigram 4.756, **bigram 3.711**, trigram 2.934. Its
first run's chunk axis turned out to measure a divergence rather than a model.

**g10-02** asked whether the model was undertrained. It is not: the curve peaks
after ONE pass and oscillates. And a fourfold wider node gives the same curve to
0.005 bits, so it is not width-limited either. It concluded "it is not learning
the text."

**g10-03** refuted that. The comparison was against a bigram holding 210,000
characters of context while the model's store holds the last 64 and resets. Given
counters carrying the SAME handicap, **at chunk 64 the model BEATS within-chunk
counting by 0.158 bits.** The 2.1-bit "underfitting" was the wrong comparison.
It recommended building a store that persists across chunks, worth up to 2.3
bits.

**g10-04** refuted THAT. The 2.3 bits is what a counter would gain. Chunk length
IS the persistence horizon, so handing the model 64, 128, 256 and 512 tests the
recommendation directly: **it captures 24% of what the extra context is worth,
and the gap widens at every step.** Persistence would hand it more of what it
already cannot use. Superposition is the binding limit -- occurrences of `e`
collapse into one averaged vector where counting keeps a distribution, which is
exactly why the shortfall GROWS with context. It pointed at bounded slots and
said the g9 line had already built them.

**g10-05** refuted that last part. `tag_slots` bounds WRITES globally across an
interval; the corpus needs slots **per character**, which at 86 symbols is 688
stored successors rather than 8. Same idea, different granularity. It also sized
the mechanism: **8 slots per character recovers 83-97%** of the prize, and the
slots needed grows with context, as keeping distinct occurrences predicts.

**slot_cost.py** priced it. Vector slots are a constant 2.7x the store at every
width -- both scale with `w`, so shrinking the node never helps, which is note
015's finding arriving at a new mechanism. Token-id slots do not scale with `w`
at all: 688 numbers, crossover at width 2.7, so **any node wider than about 3
holds the slots more cheaply than its own store**. Affordable, and only because
keys are derived -- the third unrelated reason that dependency has proven
load-bearing.

### The pattern in the three corrections

All three were the same error: **a number measured on one thing, applied to
another.**

- g10-02 applied a bigram's 210k-character context to a model with 64.
- g10-03 applied a counter's available gain to a mechanism that captures a
  quarter of it.
- g10-04 applied a global write budget to a per-key storage problem.

Each was caught by asking what the number was measured ON, and each check cost
minutes. **The checks should have preceded the conclusions rather than following
them by one cycle each.** That is the process finding, and it is more useful than
any single number above.

The nearest miss was in g10-03's own code: one model number, 5.83, measured at
chunk 64 and applied at chunk 256 where the true value is 5.734 and the verdict
FLIPS. A comment recorded where it came from and that did not help. It is now a
table keyed by chunk, and the script refuses a chunk it has no measurement for.

### Where this leaves goal 2

**The model is not a language model at character level, and the reason is
specific rather than general.** It is not the epoch budget, not the node width,
and not a failure to learn -- given its own context window it beats counting. It
is superposition: one averaged successor where a distribution is needed.

That is a mechanism-shaped problem with a costed answer, which is a much better
position than "it underfits". Whether a per-key slot store actually reaches its
ceiling is unmeasured and is the next question; the ceiling and the cost are now
both known, and the g9 line's tag is an analogy for it rather than an
implementation of it.

564 tests, 118 mutations.

## 43. The churn measurement landed and refuted the claim I made to justify it

[Note 030](docs/notes/030-the-benchmark-does-not-discriminate.md) named four
properties that would discriminate a superposed store from a cache, put **churn**
first because the machinery already existed, and asserted how it would come out.
[g10-08](experiments/sweeps/g10-08-which-degrades-better.txt) measured it:

             structure    intact   one node lost    fall   relative
      dimension-sliced     0.656           0.469  -0.188        29%
      key-sharded (24)     1.000           0.776  -0.224        22%

The **mechanism** was as described: the store falls smoothly, the table loses a
quarter of its keys outright. The **outcome** is the opposite. The cache ends far
higher and falls by a smaller *fraction* of what it had, so the store is not even
relatively more robust. "Graceful" was doing no work in that sentence.

### The more useful finding is that the measurement was premature

`reward_recall` was already known not to discriminate — g10-07's 48-integer table
answers it perfectly. A cache starting 0.34 ahead is still ahead after both lose
something, so this cannot show dimension-slicing is worse in general. It shows
churn does not rescue the store on a task where it was already losing.

**Churn is only a tiebreaker where the two are competitive intact, and no such
task exists in this project.** I put it first because the machinery existed.
Machinery existing is not the same as the measurement being interpretable, and
that is the lesson worth keeping.

Corrected ordering, now at the top of BACKLOG: **(1) a task where the store is
competitive intact, (2) then churn.** That makes the `reward_recall` decision the
one that gates everything rather than something to defer.

### Four attempts, and the fourth had no guard

1. Untrained readout — scored 0.031, below the trivial floor. Caught by the floor
   guard added for exactly this.
2. Wrong test format — trained on `build()`, evaluated on `dataset()`. Same
   length, same query positions, half the accuracy. Floor guard again.
3. Wrong width — `d_model` 32 really is about 0.25; the 0.65 quoted everywhere is
   node width 64. Caught by cross-checking a number written down elsewhere.
4. **`absent` without `leave_at` is silently ignored.** Reported a fall of exactly
   +0.000, which I read as robustness. **No guard caught this.** It took checking
   whether any prediction changed at all — 0 of 3072 had.

The fourth is the one to remember: every summary statistic looked reasonable and
only the raw predictions showed a parameter had never applied. Pinned as a test
rather than fixed, because `leave_at` defaults to 0 and existing results came
through that path — changing the signature would silently alter what they mean.

565 tests, 118 mutations.

## 44. The first property that survives measurement, and it is not the one named

Four runs in a row went against the superposed store: a cache beat it on
language, beat it on `reward_recall`, and beat it under node loss. Note 030
listed four properties that might separate them and put similarity generalisation
first. [g10-09](experiments/sweeps/g10-09-is-there-similarity-to-generalise.txt)
asked whether the store has it, **before building a task to test it** — because
if it did not, that task could not be built, and finding out afterwards would
have repeated g10-08's error exactly.

**It turned out to be two properties wearing one name.**

*Between items:* unavailable, by construction. `derived_keys` draws each token's
key independently, so off-diagonal overlaps are accidental — mean +0.0005 against
a diagonal of 0.2522. Token 5 does not resemble token 6, and no task exercising
that can be built while keys are per-token.

*Of a degraded query:* **real, and decisive.**

    corruption   accuracy        chance = 1/73 = 0.014
          0.00      1.000
          0.50      0.930        half the query destroyed
          0.75      0.665
          1.00      0.100

A cache has no partial credit — a wrong key is a miss, and no amount of
engineering changes that. **This is the first measured advantage of the store
that a cache structurally cannot have.**

### Why it matters more than the number

It converts *"find a task where the store is competitive"* from a hope into a
specification: **a task where the query arrives damaged.** And that is not
contrived for this project — note 024's cost argument turns on a node
reconstructing keys it does not store, and reconstruction is exactly where
corruption enters. A node on a lossy link is the deployment story, not a
thought experiment.

### What I did not anticipate

I expected the two questions to stand or fall together — either the keys carry
structure and the store generalises, or neither. They came back NO and YES, which
is why asking both separately was worth the extra three lines.

### The caveat, stated because the number invites overreading

Eight bindings in a width-64 store is a light load, far from saturated, and
corruption tolerance will fall as it fills. The SHAPE is the finding; 0.930 is
not a value. And this says nothing about the other three runs — the cache still
wins on every task currently in the repository.

565 tests, 118 mutations.

## 45. I retracted entry 44 within the hour, and it was published as good news

Entry 44 called corrupted-key retrieval **"the first property that survives
measurement"** and told John the viability picture had improved. **It was wrong**
and the check that showed it cost one script.

              query condition    store    cache
                        exact    1.000    1.000
    key vector half corrupted    0.970    1.000
     WRONG TOKEN (corrupt id)    0.090    0.015

**A cache is indexed by TOKEN ID.** It never receives a corrupted key vector, so
it scores 1.000 on the condition where the store scores 0.97. The corruption
exists only inside the store's own representation. "A cache returns nothing for a
corrupted key" compared the store against a failure mode the cache does not have.

**And the deployment story cannot produce one.** `derived_keys` sends token ids;
the node regenerates the key from `(seed, token)`. No partially damaged key
vector ever crosses anything. The realistic damage is a WRONG id, where the store
reaches 0.090 against the cache's 0.015 — six times chance against chance, on
inputs that are garbage either way.

### The pattern, now five deep and accelerating

Every one is the same shape: **a number measured on one thing, applied to
another.**

- g10-02 applied a bigram's 210k-character context to a model holding 64
- g10-03 applied a counter's available gain to a mechanism capturing a quarter
- g10-04 applied a global write budget to a per-key storage problem
- g10-08 measured churn on a task already known not to discriminate
- **g10-09 compared the store's damaged-key score against a cache failure that
  does not exist**

g10-08's lesson was written three hours before g10-09 repeated it. I wrote *"the
check cost minutes and should have preceded the conclusion"* and then did not run
the check before publishing the next conclusion.

### What I am changing rather than resolving to do better

The instinct that caught all five was asking **what was this number measured ON**.
That question now goes in the sweep-file template as a required section, next to
PREDICTIONS and COST — not as a resolution, which has demonstrably failed four
times, but as a thing a rail can check for.

### What survives of note 030

Property 1 is empty in both halves: no similarity between items, and no
discriminating advantage on damaged queries. Property 2 (graceful degradation)
was refuted by g10-08. **Properties 3 and 4 are untested**: slicing by dimension
where the two are competitive intact, and compositional binding, which is bAbI
task 2.

The viability picture I gave John an hour ago was too optimistic and is corrected
in the same breath as this entry.

565 tests, 118 mutations.

## 46. I tested my own hypothesis one run after writing it, and it was wrong

g10-10 found no crossover and explained it: *superposition is a compression
scheme, the task suite is deliberately incompressible, so this measures it where
it cannot pay.* Plausible, and exactly the kind of claim that became doctrine in
g10-03 before g10-04 had to refute it.

[g10-11](experiments/sweeps/g10-11-does-structure-help-the-store.txt) tested it
immediately. As a multiple of chance:

      items         random    few values    few cues
         64           58.7           6.4         0.0
        512           98.8           2.8         0.0

**Structure in the values makes the store relatively WORSE.** Capacity is set by
how many KEYS must be separated in `d` dimensions, which is note 020's law, and
shrinking the answer set does not shrink the number of keys. **The compression
explanation is refuted** and g10-10's finding stands on its own.

### The error caught before publication, for once

The first version reported RAW accuracy — random 0.207 against few-values 0.350 —
and printed *"fewer distinct values helps, the compression story stands"*.

The conditions have different chance levels: eight prototypes means guessing
scores 0.125 where four thousand values means 0.00024. Comparing them compares
numbers measured against floors five hundred times apart, and the conclusion
inverts once corrected.

**Sixth instance this session of the same error, and the first caught before the
write-up rather than a cycle after.** What made the difference was writing the
reason to DOUBT the hypothesis into the same file as the hypothesis, so the run
had something to fail against rather than something to confirm.

### A second finding nobody was looking for: the store cannot overwrite

`few cues` rebinds the same 8 cues repeatedly and scores **0.0x chance**. The
store accumulates — `memory += outer(value, key)` — so rebinding adds a term
rather than replacing one, and retrieval returns the sum of every value ever
bound to that cue. The argmax of a sum of unrelated vectors is noise.

A table overwrites in one line. **This is a third structural difference from a
cache and it was not on note 030's list of four.**

And it raises a question about the g9 line: MQAR and `reward_recall` present each
cue ONCE per sequence, so **no result in this project has ever depended on
rebinding**. Any workload that revisits a key is outside what has been measured,
which is most real workloads.

565 tests, 118 mutations.

## 47. "The store cannot overwrite" was wrong, and the corrected version is better

Entry 46 reported that rebinding a cue destroys it, and called that a third
structural difference from a cache. **It was measured at `decay=1.0`, a constant
I chose**, where the g9 line's default is 0.997 and the model accepts any value.

Rebinding 8 cues, accuracy against a chance of 0.00024:

       decay   64 rebindings   512 rebindings
         1.0           0.250            0.000
       0.997           0.125            0.000   <- the g9 default
        0.99           0.125            0.375
        0.95           0.875            0.750

**It can overwrite, at decay 0.95.** What it cannot do is overwrite at the decay
this project actually uses — so the finding holds for every configuration
measured in the repository, and the mechanism is not missing. It is priced.

**The price is retention.** Decay 0.95 has a half-life of about 13 steps against
0.997's 231. **Overwriting and remembering are the same dial pulled in opposite
directions**, and a table does both at once for nothing. That is a sharper claim
than "cannot", and it survives scrutiny where the original would not have.

### The process point, which is now the main thing I am tracking

This is the seventh instance of the recurring error and the **second caught
before publication**. The catch came from the same habit as the last one: asking
*what did I fix that I did not have to fix?* — here, a decay constant chosen for
convenience in a file about a different question.

Two catches in a row, both from interrogating my own frozen choices rather than
from being careful in general. Being careful in general has failed five times;
this specific question has now worked twice.

**It also means g9's `decay=0.997` is a frozen axis with a newly visible cost.**
Note 028 flagged `DECAY` as arriving by import and never swept, and measured it
moving recall by at most 13 points. It never asked what it costs in REBINDING,
because no task in the repository rebinds. That is now a known gap rather than an
unknown one.

565 tests, 118 mutations.

## 48. The baseline on a public yardstick, and a design pass

**[g10-12](experiments/sweeps/g10-12-the-standard-benchmark.txt): 5.466 bits per
character on Tiny Shakespeare. It does not beat a unigram.**

    uniform                        6.000   beaten by 0.53
    THE MODEL                      5.466   --
    unigram (letter frequency)     4.829   MISSED by 0.64
    bigram (letter pairs)          3.583   missed by 1.88
    character LSTM (published)     ~1.45   missed by ~4.0

I predicted "starkly bad" and expected it between unigram and bigram. **It is
below unigram** — the memory predicts worse than ignoring context entirely. The
notes-corpus reading was not an artefact of using our own prose: 5.47 here
against 5.73 there, different text, vocabulary and split rule.

The temperature chosen was 0.0627, interior to its grid, so the figure is a value
and not a bound.

### Choosing the corpus, and bending a rule to do it

John approved committing data. I chose **Tiny Shakespeare over enwik8's first
1MB**, because published enwik8 numbers use the standard split of the *full*
100MB — a 1MB slice is comparable to nothing, which was the entire point.

`corpus.py` splits by whole FILE and its docstring explains why offset splits are
dishonest. A single-file benchmark cannot be split that way, so `build_stream`
does an offset split **and says in its own docstring that it breaks the module's
rule deliberately, for one case**, because matching the published convention is
why a standard corpus is worth having. The caveat stands and is stated: the tail
shares an author and register with the head. Every published number for these
corpora carries it. The vocabulary still comes from the head only, with a test.

### Note 031, the design pass

Asked for by John after the cache results. Its central observation:

**The three measured walls trace to two decisions, not five.** Independent
per-token keys mean nothing resembles anything, so there is nothing to generalise
over; linear superposition of near-orthogonal keys caps capacity at about `d`.
`derived_keys` is still the right call — note 024 measured the alternative at
187x for a width-1 node — so this is a cost to see clearly, not a mistake.

Recommended order: **(4) a write gate** (cheap, uses surprise and gating that
already exist, and decouples overwriting from forgetting now that decay prices
them together); **(3) the readout** (the only thing that learns across sequences
and the only component never varied in any experiment); **(1) structured keys**
(biggest prize, biggest locality risk, should follow evidence).

**It recommends against (2)**, a retrieval nonlinearity, and saying so is the
point of listing it: the high-capacity formulations keep patterns separately,
which is a table with extra steps.

**The note marks itself as an argument, not a measurement.** Five conclusions
this week were drawn from reasoning and refuted by runs, so nothing in it should
be believed before it is tested.

570 tests, 118 mutations.

## 49. The test audit: mutation coverage, and the task generator had none

John asked whether the tests validate the real model or reimplementations of it,
after that trap bit twice — note 012's cap values, and `test_corrective_writes`
asserting exactness against its own copy of the write rule.

**A static check cannot answer it.** In the bad test the asserted values *did*
derive from model attributes; only the computation was duplicated. Any rule
strong enough to catch that would flag most legitimate tests.

**Mutation testing is the audit, and it is the thing that caught it.** A test
reimplementing logic survives a mutation of the real code. So the question is
coverage, and `tools/mutation_coverage.py` answers it without running anything:
it reads `mutate.py`, locates each mutation's target line, and lists the
functions containing none.

    24 of 59 audited functions carry at least one mutation (41%)

    openplexus/tasks/reward_recall.py    0 of 7    generate is 81 lines
    openplexus/tasks/corpus.py           0 of 9    build_stream is 37
    openplexus/distributed.py            4 of 14   step unaudited

**`reward_recall` at zero is the finding.** Every g9 number rests on that
generator. `mutate.py`'s own docstring explains why generators need mutations —
one "returned a wrong SET rather than crashing, which is how a sweep becomes a
confident wrong answer" — and note 027's leak was found by reading the generator,
not by a test.

Four mutations added; three confirmed caught so far:

- `rewarded-cues-are-not-chosen-uniformly` — takes the first `n` cues instead of
  a random sample, making reward predictable from position. **Caught.**
- `the-reward-lands-at-the-wrong-offset` — reward one step after its binding
  regardless of `delay`, collapsing every delay to the trivial case. **Caught.**
- `the-corpus-vocabulary-comes-from-the-test-text` — the leak `corpus.py` exists
  to prevent. **Caught.**
- `the-stream-split-overlaps` — running.

**Three caught is genuinely reassuring**: the `reward_recall` tests do validate
the generator, they had just never been challenged. The gap was in the harness,
not in those tests.

**The tool states its own limit.** A function *with* a mutation is not proven
well tested — one mutation covers one line and `run()` is six hundred. It is a
floor, and the per-function counts matter more than the percentage.

582 tests, 124 mutations.

---

## 50. The bar was the ceiling: the architecture cannot exceed a bigram

**The architecture pass John asked for produced one finding that reframes every
corpus result so far.** The write rule is `M += value(t) x key(t-1)`, so a
retrieval is the sum of the values of every token that has followed this token.
That is a bigram count table in superposition. **No trigram is ever written
down, so none can be represented.**

The bar the project has measured itself against for weeks — beat a bigram,
3.583 bits per character — is therefore **the model's ceiling, not a target.**
"Does not beat a bigram" was never a disappointing result; it was arithmetic.

**I did not let the derivation stand as an argument.** This project's failure
mode this week has been reasoning that ran and then got refuted, seven times, so
the derivation got a run. It compared each retrieval against the bigram count
vector it predicts, as a cosine, with a pre-registered threshold of mean 0.9.

**It came back 0.8703 and refused my own claim.** That left two readings with
opposite consequences: the residual is interference, and the ceiling stands, or
it is extra signal, and note 033 is wrong. A second run distinguished them by
varying how much is superposed:

    writes    cosine against a bigram count table
        20    0.9455
        40    0.9036
        80    0.8817
       160    0.8795
       320    0.8784
       640    0.8866

**Falling with load, then plateauing, is the interference signature.** Extra
structure would not depend on load that way; noise from non-matching keys does,
by construction. So the precise claim — the one in the note — is that the
retrieval is a bigram count vector plus interference, the interference carries
no information about what follows, and the distance from 3.583 to the model's
5.256 is what interference costs.

**The loose version was too strong and the measurement is what made it
precise.** That is the second time this week a threshold I set in advance
refused a claim I wanted, and both times the corrected version was better.

**What it changes.** The highest-value unexplored change in the project is
binding over a two-token context via a fixed hash — local, derivable, and it
lifts a *proven* limit rather than a suspected one. It was invisible while the
bar was mistaken for a goal. This supersedes the previously-agreed order in
which structured keys were item 1; **John's call, and it is on his decision
list.**

Note 033 also records the component map, six ranked assumptions, and a
literature pass, all marked as arguments rather than findings.

592 tests, 124 mutations, five checks clean.

---

## 51. The ceiling moved: 0.533 to 1.000 on a step a bigram cannot do

Entry 50 established that the architecture could not represent a trigram. This
is the fix, implemented behind `context_keys`, off by default: derive the key
from the token PAIR `(t-1, t)` rather than from `t`. **One line in `run`
changes.** `previous_key` becomes the key of `(t-2, t-1)`, so the same write
rule that made a bigram table now makes a trigram one.

**The discriminating test.** Blocks `A B C` and `D B E` in balanced random
order. Every step follows from its predecessor except `B`, which is followed by
`C` or `E` depending on what came before it. Scoring only the `B` steps:

    single-token key     0.533     (chance is 0.5)
    pair key             1.000

**A bigram model is at chance here and cannot leave it however long it trains.**
This is the first task in the project that a hash table does not answer
trivially — `reward_recall` is solved perfectly by one (g10-07), and this is not.

**A mutation caught a flaw in my own test design.** The first version used a
repeating `A B C D B E` cycle, in which every position is predictable from any
other at a fixed offset. `the-context-key-queries-the-WRONG-pair` — querying
`(t-2, t)` instead of `(t-1, t)` — scored perfectly on an alignment it never
had, and survived. Shuffling the blocks kills it. **This is the mutation harness
finding a vacuous test rather than a code bug, which is the second time this
week it has done that.**

A second artefact was worth removing: drawing blocks independently put seven
`D B E` before the first `A B C` at that seed, and the store is emptied between
sequences, so the model was being asked to resolve a context it had not met.
Balanced-then-shuffled, and scored on the second half.

**The price is measured and it is not small.** The number of distinct keys goes
from 66 to 469 in 4000 characters of Shakespeare — real text is Zipfian, so far
below the 4356 that uniform tokens would give, but still sevenfold. Capacity
goes as `sqrt(d/N)`, and the cosine against the true count table plateaus at
0.53 where the single-token version held 0.88.

**What I am NOT claiming.** That the model now beats a bigram. Only that it is
no longer forbidden from doing so. A higher ceiling with a noisier store could
land either side of 5.256, and a cosine cannot answer a bits-per-character
question. g10-09 was retracted this week for exactly the gap between "can
represent" and "does predict".

Every probe used `decay = 1.0`, which is the worst case for a scheme whose
problem is key count — decay bounds how many items superpose, so it should help
the pair key more than the single-token one. The sweep must vary it rather than
inherit it.

Note 034 has the tables. 602 tests, 127 mutations, five checks clean.

---

## 52. C1 was a proxy, and John replaced it with the thing it stood for

**John's ruling: "our real constraints are just *does it work over the
internet* — if something still meets that, it's good to go."**

C1 said no operation may require globally synchronised state. That was adopted
because backpropagation is a global barrier moving data proportional to
parameter count, which is why deep networks need tightly-coupled hardware — so
"no global state" looked like the same requirement stated structurally.

**Note 036 is where it became clear they are not the same requirement.** Edmond
& Kadmon report error-feedback dimensionality scaling with task complexity
rather than network size — rank 10 matched backprop on CIFAR-10 across an MLP, a
CNN and a ViT. That is still a backward chain, so the old rule forbids it. But
the message is **tens of floats per hop**, and a backward sweep carrying forty
bytes over a 150 ms link is not what forces a data centre. **The structural rule
was ruling out designs the actual goal permits.**

**What the amendment does not license**, and this is the part that keeps it from
being a licence to do anything: a global all-reduce is still out, *even a
twelve-byte one*. Note 036 records zeroth-order and evolution-strategy methods
looking local right up until you notice their scalar broadcast is a barrier
wearing a small payload. **The question is whether progress stalls when one
participant is slow or gone**, not how many bytes moved.

Every result before today was measured under the stricter rule, so none is
invalidated — they were achieved with one hand tied. GOALS.md carries the
amendment with the date and the reasoning.

---

## 53. g11-03 lost four of six cells, and the reason is a rule worth keeping

**At width 64 the pair key LOSES by 0.216 +/- 0.028 bits** — the ceiling moved
and the store cannot afford the resolution. P1 and P4 confirmed; P2 and P3, the
crossover question the sweep existed to answer, **never ran.** Widths 128 and
256 hit the 240-minute timeout.

**The estimate was wrong in a way worth naming.** The store is `d x d` and the
per-step work is a matvec, so cost goes as **d squared**: width 256 is sixteen
times width 64, not four. I estimated from the one cell I had run locally, which
was the cheapest one.

**The rule: when a sweep axis enters the cost quadratically, estimate from the
MOST expensive cell and state the estimate per cell rather than for the matrix.**

g11-04 was already written with widths to 256 plus an extra backprop arm and
would have failed identically. It was re-scoped **before** dispatch — widths
16–128, two seeds, chunk 128, training capped at 250k characters — and the
worst cell then ran locally in under ten minutes. That is the only reason this
mistake cost one sweep rather than two.

**The re-scope produced a number on its way past.** Backprop attention at width
128 scores **4.165 bits** on Tiny Shakespeare, against our 5.427 at width 64.
It clears the unigram bar of 4.829, which nothing this project has built ever
has. That is the reference g11-04 measures an exponent against, and it is
already a bit and a quarter ahead.

I should also record what the partial result does NOT say. The single-token arm
at 5.427 matches the g10-12 baseline of 5.466, so the run was sound; the pair
arm simply has more to store and less room. **This is weak evidence against pair
keys and should not be cited as strong**, because the confirmed predictions were
the easy ones and the hard one timed out.

---

## 54. Our rank collapse is real, and it is NOT the disease Muon cures

Note 036 named Muon-style orthogonalisation as the intervention with the largest
measured effect in the whole literature scan: Boeshertz et al. recovered
CIFAR-100 ResNet-18 from **1.4% to 46.1%** by fixing updates that had collapsed
to effective rank 12 where backprop reaches 100. Note 035 had already measured
our own store at effective rank ~3 at every width. It looked like the same
disease, and John approved trying the cure.

**Measured on Tiny Shakespeare, width 64, window 32:**

    accumulated update, effective rank raw              2.22
    accumulated update, effective rank orthogonalised  11.29
    window length                                      32

    orthogonal_every     bits per character
                   0     5.588
                   8     5.716
                  32     5.976
                 128     5.867

**P1 confirmed** — the collapse is severe, rank 2.22 out of a possible 32.
**P2 confirmed** — orthogonalising raises it fivefold.
**P3 refuted** — and P3 was the one that mattered. Prediction gets worse at every
window length tried.

**The reason is the finding.** Boeshertz's rank collapse is a *learning rule*
failing to explore directions that carry signal. **Ours is the data genuinely
having few directions.** Note 035 established the store is a bigram count table
over 66 characters, and such a table is low-rank because English is. Forcing
rank onto an update whose target is low-rank spreads its magnitude into
directions that carry nothing, so the extra rank is noise by construction.

**Same symptom, different cause, and the cure for theirs actively hurts ours.**
"Effective rank is low" now has two readings in this repository and only one of
them is a defect — which is exactly the sort of thing that gets forgotten, so it
is pinned in `tests/test_orthogonal_updates.py` rather than left in a note.

**A property I did not know before writing the test.** Orthogonalisation
*equalises* the singular values a matrix already has; **it cannot invent
directions.** A single delta-rule step is `error ⊗ retrieval` — rank one — and
stays rank one however hard it is orthogonalised. That is why the updates have
to be accumulated before there is anything to do, and it is not obvious from the
name of the operation. Two of my first tests asserted the opposite and failed;
the code was right and the tests were wrong.

**On the amended constraint.** Orthogonalisation is applied per GROUP, since a
node holds only `vocab x d/groups` of the readout. Doing it across groups would
need every node's columns at once — a barrier, which the amended C1 still
forbids. That restriction may be why this buys less here than elsewhere, and it
is a real confound rather than an excuse: nobody has measured whole-matrix
orthogonalisation on this model, and under the goal it would not be usable
anyway.

636 tests, 131 mutations, five checks clean.

---

## 55. g11-04 was fully spent and answered nothing, because my own fix broke it

**The control failed.** The backprop baseline's fitted exponent is **-0.0021
with an R2 of 0.13** — flat. If the reference does not scale with width, no
comparison against it means anything, and the summariser refuses the result
rather than reporting three exponents as agreement.

    arm            d=16              d=32              d=64             d=128
    backprop   4.197 +/-0.006   4.150 +/-0.024   4.157 +/-0.013   4.175 +/-0.010
    context    5.917 +/-0.035   5.827 +/-0.027   5.759 +/-0.011   5.703 +/-0.016
    single     5.730 +/-0.022   5.624 +/-0.000   5.505 +/-0.001   5.494 +/-0.025

One prediction of four survived, and the one that decided admissibility failed.
P1 (our arm flat) refuted **barely** — b = -0.0213 against a threshold of 0.02 I
had written down. P3 refuted: the context arm is shallower, not steeper. P4 is
vacuously true and worthless, since both our arms beat a flat baseline.

**Why the control failed is my doing, and it is the second cost mistake in two
sweeps.** The baseline is already at 4.20 bits by width 16 and does not improve:
it is **data-limited, not width-limited.** 250,000 characters is not enough text
for a wider attention model to have more to learn — and 250,000 characters is
exactly the cap I introduced to stop g11-04 timing out the way g11-03 had.

**The fix for the cost problem removed the phenomenon the experiment was built
to measure.** The two failure modes traded directly against each other and I did
not see the trade while making it.

**The rule: when re-scoping a sweep to fit a budget, check that the control can
still fire.** A cheaper sweep that cannot resolve its own reference is worth
less than no sweep, because it costs the same and invites a conclusion.

### What is real anyway

**Pair keys lose at every width from 16 to 128, and the gap does not narrow** —
0.187 bits at width 16, 0.209 at width 128. This answers what g11-03 could not:
its P2 predicted the gap would close with width, and across four widths it does
not. **The pair key is not waiting for a width we have not tried.** Note 034's
ceiling result stands; paying for it needs something other than width, and the
exact cache is the candidate.

**Backprop attention beats a unigram at width 16** — 4.20 against 4.829, on
roughly ten thousand parameters, where our best is 5.49. The reference is not a
strawman.

### A second bug

`tools/summarise_g11_04.py` produced **no output at all** in the CI aggregate
step, though it runs correctly on the same downloaded artifacts locally. The
numbers above were recovered by hand. A silent summariser is how a sweep gets
mis-transcribed, and it is fixed before the re-run.

The re-run needs **data on the x-axis, not width** — which is the axis
Filipovich et al. actually used, and the one where the reference still moves.

---

## 56. A green run with an empty summary, and the two things that made it one

The g11-04 summariser "produced no output in CI though it ran locally" was two
independent defects lining up, and the second is repo-wide.

**The instance.** The aggregate job ran `python -m tools.summarise_g11_04`
without `pip install numpy`. It died on `ModuleNotFoundError` at import.
Confirmed in the log of run `30295529865`, not inferred.

**The class, which nobody had looked at.** That crash was invisible because the
command is piped into `tee`, and a shell pipeline reports the status of its
LAST command. **Forty workflows piped into `tee` and none used `set -o
pipefail`.** So this was never about numpy: any summariser crash, for any
reason, would have produced a green step and a `summary.txt` containing only its
header line. A crashed summariser reads as "the sweep found nothing" rather than
"the summariser never ran", and nothing distinguishes them from outside.

    cells returned: 12 of 12     <- the entire summary of a 48-minute matrix

**The fix, over the enumeration.** `pipefail` in all forty; `if: always()` on
every Summary step and every `*-summary` upload, because pipefail alone would
have traded a silent wrong answer for **lost artifacts** — a failing Combine step
skips the upload and the raw data goes with it, which is worse than the bug.
`tools/check_workflows.py` now refuses both shapes, per JOB rather than per
file: a whole-file search for `pip install numpy` finds the scaling job's copy
and concludes g11-04 was fine, which is exactly how this survived.

**And the hand-recovered numbers were right.** Re-downloading run 30295529865
into a fresh directory and running the rewritten summariser over it reproduces
decision 55's table to three decimals. Decision 55 stands as published.

---

## 57. There was no data axis to sweep, and the flag that looks like one is not

g11-05 was going to re-run g11-04 on data. **There was no way to do that.**
`TRAIN_CHARS` was a module constant, and `--cap` — the flag whose name suggests
the corpus — is the memory store's *norm* cap, unrelated to the text.

`tools/check_workflows.py` validates that a flag is **accepted**, not that it is
**read**. So dispatching `--cap` as a data axis would have passed every
pre-flight check, run five identical cells, and reported a flat exponent —
which is indistinguishable from the real result, and would have been the second
consecutive matrix spent on an unanswerable question.

`--chars` is new and wired through, with `tests/test_data_axis.py` as the
connection test: fewer characters means fewer fitting chunks, the fitting text
is ACTUALLY the requested length rather than merely smaller, and the held-out
test text does **not** move with the axis. The default stays 250,000 so g11-04
reproduces. A cell asking for more text than the corpus holds now refuses rather
than truncating.

**The summariser reads the grid instead of assuming it.** `summarise_g11_04` is
now `summarise_scaling_exponent`: it detects whether `chars` or `width` varies
and fits against that, and refuses when both do.

**A hole the rename exposed, in the act of exposing it.** The new dependency
check returned "imports nothing" for a module that does not exist — so renaming
a summariser switches the check off in the same change that breaks the workflow.
Two tests went red on precisely that. It now reports an unresolvable reference.

Dispatched as run `30302728532`: 15 jobs, 5 data points x 3 arms, width pinned
at 64 **from g11-04's own grid at 250k characters**, where all three arms have
published values (backprop 4.157, context 5.759, single 5.505). That cell has
something to land on, so disagreement there is a dispatch error rather than a
finding. Predictions are registered in the sweep file before dispatch; P1 is the
admissibility test that failed last time.

---

## 58. One seam exists, and it is the only one

John asked whether the seams idea — components testable in isolation and
swappable for experimenting with alternatives — was ever done. Measured, not
recalled:

    Protocol seams in openplexus/       1   (KeySource, openplexus/keys.py)
    LocalMemoryConfig fields           31
    LocalAssociativeMemory.run()      584 lines
      branch points within it          51
      reads of self.config.*           53

So it was started and stopped at one component. `keys.py` says so itself —
"keys were the obvious place to start" — and its own argument for existing
applies unchanged to the write rule, the retrieval, the readout and the gates:
each variation currently costs a config flag, a branch inside a 584-line method,
and a threading of that flag through every experiment script.

**This is not only an ergonomics point, and that is the part worth deciding on.**
The project's headline problem is that every component passes its capability
test in isolation while the whole fails. A model whose parts are 31 flags
branching inside one method is a model where "swap the retrieval and re-measure"
is a refactor rather than an experiment — so the composition question is
expensive to attack in exactly the situation where it is the live question.

No change made. This is a measurement and a pending decision, recorded because
the number is the argument.

---

## 59. g11-05: the model does not learn from more text, and now the control fired

**The most decision-relevant number this project has produced.** Run
`30302728532`, 15 of 15 cells, and unlike g11-04 it is admissible: the backprop
control fits `b = -0.0243` at R2 0.96 across a 16x data range, where the same
baseline was flat across an 8x width range. Changing the axis was the right
diagnosis — that contrast is direct evidence the baseline was data-limited.

    arm           n=62,500     n=125,000     n=250,000     n=500,000   n=1,000,000
    backprop   4.306+/-0.021 4.283+/-0.036 4.157+/-0.013 4.091+/-0.015 4.049+/-0.028
    context    5.775+/-0.014 5.770+/-0.009 5.759+/-0.011 5.764+/-0.011 5.763+/-0.009
    single     5.529+/-0.020 5.530+/-0.004 5.505+/-0.001 5.513+/-0.001 5.518+/-0.010

      backprop   b = -0.0243   R2 = 0.96
      context    b = -0.0008   R2 = 0.60   FLAT
      single     b = -0.0010   R2 = 0.33   FLAT

**Sixteen times the data buys 0.012 bits on one arm and nothing on the other.**
The single-token arm ends slightly WORSE than it started.

**Three of four predictions refuted, and they agree with each other.** P1
confirmed (the control fires). P2 refuted — both arms flat, an order of magnitude
below the 0.02 threshold, not a near miss. P4 refuted — the context arm was
predicted to keep improving where the single-token arm flattened, and it is flat
at a *worse* loss, so this is not an arm running out of ceiling. P3 confirmed but
badly understated: it was written expecting a shallower negative slope.

**This is not the Filipovich shape.** Filipovich's local rule lost the exponent
but kept one — DFA -0.040 against backprop -0.071. Ours is zero. "Shallower" and
"none" are different claims and the second is what the data supports.

### What it does and does not say

**It does not condemn local learning.** The delta rule on `Wo` is the exact
gradient for a single linear readout; nothing is being approximated badly.

**It does say the architecture is saturated on every axis tried** — not
data-limited, not width-limited. This removes "we are just small" as an
explanation for the gap to the baselines, which was the last available one.

**And the protective reading of the width result does not transfer.** Note 035
excused a flat width exponent because the store is a rank-3 bigram table. There
is no rank argument on the data axis. An arm that does not improve with more text
has stopped extracting information from text.

**It is consistent with the through-line rather than a new mystery.** The store
holds a bigram count table (note 033, cosine 0.9455); an actual bigram model
scores 3.583 bits where we score 5.5. More text sharpens those counts and the
sharpening never reaches the output, because `r = M @ key` is a SUM and per-item
information is destroyed before the readout sees it. **A bottleneck downstream of
the statistics cannot be widened by improving the statistics** — which is exactly
the shape of a flat data exponent.

### What it makes urgent

The next measurement is the one the exact cache gestures at: **the same model
given the same amount of state WITHOUT superposition, on the data axis.** If an
exact store scales with data where the superposed store does not, the sum is
*identified* as the binding constraint rather than inferred from it. That is now
the highest-value sweep available, and it is the cache sweep John has already
prioritised — with a data axis added to it.

### A re-analysis trap found while checking this

`gh run download` of a completed sweep yields the matrix artifacts **and** the
`*-summary` artifact, which re-uploads every `out/*.json`. A recursive glob over
the download therefore reads every record twice. Means are unaffected by exact
duplication, so the table looks right; **standard errors shrink by 1/sqrt(3)**.
CI is not affected, because the summary artifact does not exist when the
aggregate job downloads. `tools/recovery.load()` calls `glob.glob` without
`recursive=True`, so a `**` pattern silently matches one level — which is what
kept the g11-04 re-check honest, by accident rather than by design.

---

## 60. The exact cache's two defining claims were untested, for at least two commits

CI found two surviving mutations at `b480926`, and both are on the exact cache:

    the-cache-admits-by-RECENCY-not-residual   SURVIVED
    the-cache-read-is-not-gated-by-the-MATCH   SURVIVED

**The cache is the project's first controlled improvement on the corpus** —
0.244 bits against 0.089 for quadrupling the width — and admission-by-residual
and the match gate are the two claims that make it that mechanism rather than a
recency buffer with an ungated softmax. Nothing in the suite would have noticed
either one breaking.

**This does not retract the cache's number.** The code was correct; the tests
were vacuous. The measurement stands and what was missing was the guarantee that
it would keep standing. Both are caught now, by
`tests/test_retrieval_seam.py` — admission needed one slot and two *deliberately
unequal* writes, because by-residual and by-recency are identical whenever a
later write happens to be the more novel one, which in random data it usually is.

### Why it took two commits to notice, which is the real finding

The five pre-commit checks run `mutate.py --verify`. **`--verify` only asserts
that every mutation's ORIGINAL text is still present.** It says nothing about
whether the suite would catch the mechanism breaking — that is the full harness,
which edits the source for twenty minutes and therefore lives in CI, sharded.

So a vacuous test region passes every local check, and is reported later, on a
run nobody is watching, attached to whichever commit happened to be pushed next.
Here it took an unrelated refactor making `--verify` fail before anyone looked
at the mutation results at all.

**`mutate.py --changed`** now runs the mutations whose target file the current
work touches — union of uncommitted changes and everything since master. Seconds
rather than twenty minutes, and exactly the set that can have been invalidated.
It is added to the pre-commit list in CLAUDE.md. Verified: with
`openplexus/retrieval.py` dirty it selects those three mutations and no others.

---

## 61. Node size is not what is binding, and the assumption worth questioning is a different one

John asked whether the small-node assumption is a limit the project is running
into, and whether information should be split across nodes differently.

**On size, the evidence says no.** g11-04 swept width 16 to 128 **in a single
process** — the whole model, not a node — and got a flat exponent. g11-05 swept
data 16x and got a flat exponent. If capacity were binding, width would have
bought something; across an 8x range it bought 0.089 bits. Making a node bigger
cannot be the answer when making the entire model bigger is not.

**The assumption worth questioning is not node SIZE but what a node HOLDS.**
`partitions` splits the store by DIMENSION: every node holds a slice of the same
superposed matrix and computes the same `r = M_slice @ key_slice`, and
`answer = parts.sum(0)` adds the slices back together.

So the current scheme is a **parallelisation of one algorithm, not a
decomposition into roles.** Every node inherits the sum. More nodes means more,
narrower copies of the operation that g11-05 has just shown to be saturated —
and no arrangement of operands rescues a bad operation.

### The alternative, and why it is the same idea as the cache

Partition by **item** instead: nodes hold *different bindings*, and a read is a
SELECTION across nodes rather than a SUM within one.

**That is exactly what the exact cache already does inside one process** —
entries kept separately so a softmax can select, rather than rescale an average
that has already been taken. The cache and "shard across machines by item" are
one idea at two scales, which makes the cache sweep a single-process proxy for a
distributed-architecture question rather than a tuning exercise.

Three consequences, and the second is the strongest:

1. **It fits the hardware goal better.** A small device holding a handful of
   exact bindings is a natural participant. Holding a slice of the dimensions of
   one large matrix requires every node in every read.
2. **It is partial-tolerant by construction, and dimension-splitting is not.**
   Lose a node holding DIMENSIONS and the retrieved vector has holes in it. Lose
   a node holding ITEMS and you simply do not get those items, and take the best
   of whoever answered. The amended C1 — no barrier that stalls when a
   participant is slow or gone — falls out of the data layout instead of being
   engineered on top of it.
3. **It is the CS answer to a solved problem.** Shard by key, route the query,
   take the best answer. Distributed key-value stores settled this long ago,
   which is the steer about taking mechanisms from computer science where the
   problem is well understood.

On one-neuron-per-device: that is a REPRESENTATION choice. The policy worth
keeping from the biological framing is local updates with no global barrier, and
item-sharding delivers the same deployability without inheriting the sum.

No change made yet. The cache sweep on a data axis is the measurement that bears
on it: if an exact store scales with data where the superposed one is flat, that
identifies the sum as the binding constraint AND argues for the partitioning.

---

## 62. The store does not persist, and g11-05 has a second explanation

Found while designing the composition task, and it is the most consequential
thing this session turned up.

**`memory = np.zeros((d, d))` is inside `run`.** The associative store is
rebuilt from scratch on every call, and `run` is called once per chunk — 128
characters. `Wk` and `Wv` are drawn in `__init__` and never updated. So:

    Wo (learned by the delta rule)      4,096      <- the ONLY thing that learns
    Wk (frozen random)                  4,096
    Wv (frozen random)                  4,096
    store, d x d                        4,096      <- REBUILT EVERY 128 CHARACTERS

    backprop attention, same width     20,481      persistent parameters, all trained

**Everything this model learns across a corpus is one `vocab x d` linear map.**
At width 64 that is 4,096 numbers against the baseline's 20,481, and the
baseline's are not a single linear layer.

Confirmed empirically rather than by reading: with `learn=False`, predictions on
a sequence are byte-identical whether or not another sequence was run first —
so nothing carries. And after a learning run, `Wk` and `Wv` are unchanged while
`Wo` is not.

### Why this matters, and what it does to decision 59

g11-05 measured a flat data exponent and **decision 59 attributed it to the
sum** — a bottleneck downstream of the statistics cannot be widened by improving
the statistics. That explanation is still available. But there is now a second,
simpler one:

**A model whose only persistent parameter is one linear readout of 4,096 numbers
has almost nothing for more data to fill.** A linear map saturates fast, and 16x
more text cannot help a capacity that small. On that reading the flat exponent is
about persistent capacity, not about superposition at all.

Decision 59 said "consistent with the through-line", which was accurate, but it
named one cause where there are two and did not say so. **The individual facts
were known** — CLAUDE.md has said all along that the delta rule on `Wo` is the
exact gradient for a single linear readout — but the connection to the flat
exponent is made nowhere, and the two explanations have very different
consequences.

### The experiment that separates them

**Unfreeze the value projections**, which is John's own queued item and is now
the discriminating measurement rather than merely the next one. `Wv` is
`vocab x d` of frozen random numbers. Learning it adds persistent capacity
**without touching the sum at all.**

    if the data exponent goes negative  -> the flatness was CAPACITY
    if it stays flat                    -> the flatness is the SUM, as 59 said

Either answer is worth more than another mechanism, because every result on this
corpus is currently ambiguous between the two.

### A confound in g11-06, recorded before its results land

g11-06's `matched` arm is matched on TOTAL numbers held (20,449 against the
cache arm's 20,480). It is **not** matched on persistent capacity: at width 143
its readout is 9,152 numbers against the cache arm's 4,096.

So `matched` could show a steeper data exponent for a reason that has nothing to
do with superposition — it simply has 2.2x the persistent parameters. **The
primary comparison is unaffected**: `single` and `cache128` are both width 64
with identical 4,096-number readouts, and that pair is what P3 is about. But the
`matched` arm's EXPONENT must not be read as evidence about superposition, only
its LEVEL, which is what it was designed for.

Written down now rather than after the numbers arrive.

---

## 63. Our model converges at 16,000 characters, so g11-05 swept entirely above saturation

A three-minute local probe, and it changes how g11-05 must be read.

    single, width 64, three seeds        backprop, width 64, one seed
    chars     mean   spread              chars    bits
     4,000   5.570    0.019               2,000   5.000
     8,000   5.543    0.017               8,000   4.813
    16,000   5.527    0.039              16,000   4.625
    32,000   5.523    0.036              62,500   4.327
    62,500   5.531    0.040           1,000,000   4.049  (from g11-05)
   125,000   5.531    0.008

**Our model stops improving at about 16,000 characters.** Everything after that
moves by less than the seed spread. Total movement from 4,000 to 125,000 is 0.039
bits against a seed spread of 0.04 — it is noise. The backprop baseline improves
by 0.95 bits over the same kind of range and is still improving at 1,000,000.

**g11-05's smallest data point was 62,500.** The entire sweep — five points,
fifteen jobs — sat above the saturation point of the thing it was measuring.

### This does not overturn g11-05, it sharpens and demotes it

The finding stands as a fact: more text does not help. But **the sweep could not
have found anything else**, because every point was past convergence. By this
project's own standard — *ask what outcome would refute the prediction; if the
predicted outcome is guaranteed by how the condition is built, it is not
evidence* — g11-05's flat exponent is not evidence. The flatness was in the grid.

The honest statement is stronger than the one g11-05 made, and cheaper to get:
**this model extracts everything it can from sixteen thousand characters, and
sixty times more text adds nothing.** That is a better sentence than "the fitted
exponent is -0.0010", and a three-minute local run produced it where a
fifteen-job matrix did not.

### The rule, which is the third instance of one pattern

    g11-04   the CONTROL could not fire        -- baseline flat over the range
    g11-05   the ARMS had already converged    -- flat over the range by construction
    g11-06   same lower bound, same problem    -- dispatched before this was known

All three are one failure: **the grid did not contain the phenomenon.** The rule
already in CLAUDE.md covers the control; it does not cover the arms.

**Before fitting a scaling exponent, run the cheapest possible probe at the
BOTTOM of the intended range and confirm the arm is still moving there.** Minutes
locally against hours of runner time, and it is the difference between measuring
a slope and measuring a plateau.

### What it does to g11-06, which is still running

Its smallest point is 62,500 too, so **its EXPONENT question is compromised in
the same way** and its arms will very likely all read flat. Recorded before the
numbers arrive.

Its LEVEL comparison is unaffected and is still worth having: whether the exact
cache beats a state-matched superposed store at equal numbers held does not
depend on either being unconverged. That was the original hybrid-store claim and
this re-tests it at five data sizes.

### Where the diagnosis stands now

Decision 59 said the sum; decision 62 said persistent capacity. **Neither is
settled and this measurement does not settle it** — it relocates the question.
The sharp form is now: *why does a model with ~1,956 effective readout numbers
converge at 16,000 characters?*

One hypothesis is refuted already. The readout was suspected of seeing a rank-3
signal, since note 035 measured the STORE at effective rank ~3. Measured directly
on the RETRIEVED VECTORS, via a recording wrapper on the new retrieval seam:

    width    effective rank of retrievals    Wo numbers    Wo effective
       16                          13.5           1,024             863
       32                          22.9           2,048           1,466
       64                          30.6           4,096           1,956
      128                          33.3           8,192           2,130

**Not rank 3 — about 30.** The readout is not starved of dimensions. But the rank
SATURATES near 32: doubling width from 64 to 128 buys 2.7 more effective
dimensions and 4,096 more parameters that have nothing to read. That is a clean
account of the flat WIDTH exponent, and it is not an account of the data one.

The probe took minutes and needed no edit to `run` — it is a wrapper around the
retrieval seam, which is the first time that seam has paid for itself.

---

## 64. A learned value projection, in its cheapest form, is refuted

Decision 62 identified the value projections as the discriminating experiment,
and John had already queued them. The cheapest version costs one line: write the
LEARNED readout row as the value instead of the frozen draw, so `Wo` and the
value projection become one matrix.

**Measured on Tiny Shakespeare, width 64, two seeds, against the frozen draw:**

    chars       frozen    learned     delta
     4,000       5.565      5.565    +0.000
    16,000       5.519      5.533    +0.014
    62,500       5.529      5.597    +0.068
   250,000       5.505      5.528    +0.023

**Neutral at the smallest size and worse everywhere else.** Refuted, kept behind
a default-off flag with the numbers attached.

The `+0.000` at 4,000 characters is not a dead flag and was checked rather than
assumed: these experiments initialise `wo` to `wv`, so the mechanism can only
bite once the readout has moved away, and at 4,000 characters it has barely
moved. The connection test in `tests/test_learned_values.py` shows 53 of 160
predictions differing, and
`value-from-readout-is-read-and-never-applied` is the mutation that fails if the flag
stops being applied.

**Why it hurts is worth stating, because it is informative.** The stored value
becomes a moving target: `Wo` is updated by the delta rule at every scored
position, so a binding written at step t is read back against a readout that has
since changed. The frozen draw is a fixed target, and a fixed target is easier to
learn a linear map onto than a target chasing the map.

### What this does NOT refute

**A genuinely separate `Wv` with its own update and its own parameters.** That
adds persistent capacity, which this version explicitly does not — it merges two
matrices rather than training a second. Decision 62's hypothesis is about
capacity, and this measurement does not test it.

So the discriminating experiment is still open, and it is now better specified:
it must ADD parameters, not re-use `Wo`'s.

### A mutation was re-pointed, and this is the second time in a day

`departure-is-only-a-dropped-message` targeted the exact line this change edited.
`--verify` caught it. Both it and the new mutation are confirmed caught.

That is twice today that a one-line edit silently invalidated a mutation, which
is the case for `--verify` running FIRST and for `--changed` existing at all
(decision 60).

---

## 65. Performance follows the RANK of the retrieval, not the parameter count

**The best-supported result of the session, and it comes from a dissociation
rather than another null.**

Decision 62 proposed that the flat exponents were about persistent capacity: the
model learns one `vocab x d` linear map and has nothing for more data to fill.
Decision 59 proposed the sum. **Every measurement before today confounded them**,
because width raises the parameter count and the retrieval rank together.

Training `Wv` separates them. It ADDS 4,096 persistent parameters — doubling
what the model carries across a corpus — and it does this:

    value_lr   persistent params   retrieval rank        bits @ 250k chars
       0.0                 4,096   30.5 / 32.2 / 30.5                5.505
       0.02                8,192   19.5 / 20.5 / 19.4                5.956

    (rank at width 64 on 60,000 characters, three seeds, no overlap between
     the two groups; bits are two seeds)

**Parameters up, rank down, performance down.** Capacity is not what is binding.
A model given twice the persistent parameters lands at 5.956 bits, nearly the
6.000 of a uniform distribution.

The full data curve, width 64, two seeds:

    chars   frozen  lr=.005   lr=.02   lr=.05
     4,000   5.565    5.540    5.653    5.837
    16,000   5.519    5.601    5.812    5.927
    62,500   5.529    5.806    5.937    5.960
   250,000   5.505    5.930    5.956    5.939

It gets worse with MORE data, which is the opposite of a capacity mechanism and
exactly what a collapse mechanism predicts: more updates, more alignment.

### Why training `Wv` collapses the rank

The update is `Wv[target] += lr * Wo^T (y - p)`. **Every token's value is pushed
along directions the shared readout error favours**, and those directions are
shared across tokens. So the value vectors align with each other, the stored
values span fewer directions, and the retrievals span fewer still. A delta rule
driven by a common error signal is an alignment process, and alignment is rank
collapse.

This is a better explanation than the one offered in decision 64 for
`value_from_readout` — "the stored value becomes a moving target" — which was
hand-waving that happened to point the right way. **Both refuted value-projection
mechanisms have the same measurable cause, and it is not instability, it is
rank.**

### What now has one account instead of three

    width 16 -> 128     rank 13.5 -> 33.3, SATURATING     bits 5.730 -> 5.494
    data 4k -> 250k     rank unchanged                    bits flat after 16k
    trained Wv          rank 30.5 -> 19.8                 bits 5.505 -> 5.956
    exact cache         rank 30.6 -> 32.8                 bits better

**The readout can only use as many dimensions as its input actually has**, and
every intervention's effect on bits tracks its effect on rank, including the two
that made things worse. The flat width exponent is the rank saturating near 32.
The flat data exponent is a linear map over ~30 dimensions converging at 16,000
characters. Neither needs a separate explanation.

**Decision 62 is refuted** — not by a confounded null but by a dissociation.
**Decision 59 is sharpened rather than confirmed**: the sum is bad *because it
produces a low-rank retrieval*, which is a claim with a number attached and a
design target attached to it.

### The design target this gives, which is the useful part

**Raise the rank of the retrieval.** That is now the explicit objective, and it
is measurable in minutes with a recording wrapper on the retrieval seam rather
than in hours with a matrix.

It also reframes the exact cache: it is not merely "storage that does not sum",
it is the only mechanism so far that raised the rank at all — and it raised it
by 2.2, from 30.6 to 32.8, which is small. **That is a caution about the cache,
not an endorsement**, and it is worth holding next to g11-06's numbers when they
arrive.

### Measurement notes

Rank is the participation number `exp(H(singular values))`, note 035's measure,
computed on the CENTRED retrieved vectors from a settled model with learning off.
Reproduced at three seeds. The probe needs no edit to `run` — it is a wrapper
around the retrieval seam, which is now the second time that seam has paid for
itself in a day.

---

## 66. Two different "effective ranks" in one repository, and I refuted a correct
## hypothesis by switching between them

**Decision 65 contains an error and this corrects it.** The headline survives;
one of its sub-claims does not.

Note 035 measures **stable rank**, `er(S) = ‖S‖_F² / ‖S‖₂²`. Decision 65 measured
**participation rank**, `exp(H(σ))`. Both are called "effective rank", neither
was labelled, and they answer different questions: stable rank is dominated by
the largest singular value and asks *how many directions rival the biggest one*;
participation rank asks *how many carry appreciable energy at all*.

Measured on the same retrieved vectors, same model, same run:

    value_lr    stable rank    participation rank
       0.0             4.06                 30.56
       0.02            1.88                 19.47

**Decision 65 said "not rank 3 — about 30" and called the rank-3 hypothesis
refuted. Under note 035's own measure the retrievals are at 4.06, which is the
hypothesis, confirmed.** The refutation was an artefact of changing the ruler
without saying so — and it is exactly the failure CLAUDE.md warns about, a
quantity computed exactly as described and named something it does not earn.

### What survives, and it is the important half

**The dissociation holds under BOTH measures.** Training `Wv` adds 4,096
persistent parameters and drops stable rank 4.06 → 1.88 and participation rank
30.6 → 19.5, while bits go 5.505 → 5.956. Parameters up, rank down, performance
down, on either ruler. Decision 65's conclusion — *performance follows the rank
of the retrieval, not the parameter count* — is now better supported than when
it was written, because it is measure-independent.

### And the corrected sub-claim is the SHARPER one

The readout is not reading a 30-dimensional signal. **It is reading about four
dimensions that matter**, so `Wo`'s usable capacity is on the order of
`vocab x 4 ≈ 256` numbers, not 2,000. A 256-number linear map converging on
16,000 characters (decision 63) is not surprising at all — it is expected.

So the account tightens rather than loosens: the store faithfully holds a
character bigram table, that table is genuinely low-rank because English is, the
retrieval inherits it, and the readout can only use what the retrieval carries.
**Every flat exponent this project has measured follows from one number, and the
number is about four.**

### The rule this buys

**Name the measure next to the number.** "Effective rank" has meant two things
here for two notes, and the ambiguity produced a wrong refutation of a correct
finding within a day of both being written. Any rank reported from now on says
which one, and a comparison between two ranks says whether they are the same
ruler.

`tools/` should carry one implementation of each rather than each probe rolling
its own, which is rule 9 applied to a measurement rather than to code.

### Reconciling the last loose end

Within one chunk the participation rank of retrievals is 17.3, pooled across
chunks 30.6. That is consistent: each chunk's store spans its own small
subspace, and the subspaces differ between chunks, so the pooled set spans more
than any single one. `Wo` is fixed across chunks and must serve the pooled
directions while any single prediction is made from one chunk's few — which is a
second, independent reason the readout is worse off than its parameter count
suggests.

---

## 67. Rank does not predict bits, and sparse keys are a free 0.18

Two results from one probe, and the first retracts a claim made two hours ago.

### Decision 65's general claim is refuted

Decision 65 concluded *performance follows the rank of the retrieval*. It held
across the interventions that produced it — width, trained `Wv`, the cache — and
**it fails as soon as a different class of intervention is tried.** Seven schemes,
width 64, 60,000 characters, both rank measures:

    scheme          stable  partic   bits
    table keys        4.06   30.56  5.539
    pair keys         5.92   32.89  5.765   <- near-highest rank, WORST bits
    dense (drawn)     6.10   31.53  5.527
    sparse k=16       2.08   28.99  5.466   <- LOWEST rank, better than table
    sparse k=8        2.96   29.84  5.383
    sparse k=4        4.04   31.24  5.370
    cache 128         6.09   32.79  5.344   <- best bits

**There is no monotone relationship.** The lowest-rank scheme beats the baseline
and the second-highest-rank scheme is the worst thing on the list. Four aligned
observations were generalised into a law and the fifth class of intervention
broke it.

What survives is the narrower statement, which is still worth having: **training
`Wv` collapses the rank and costs 0.45 bits**, and that dissociation is a fact
about that mechanism. It is not a general principle, and decision 65 should be
read as the former.

The likely reason rank alone cannot carry it: pair keys raise rank by producing
469 distinct keys against 66 tokens (note 034), so each key is seen far less
often and every entry is estimated from far less evidence. **Rank bought at the
cost of evidence per key is not worth having** — which is a bias/variance
statement, not a rank statement, and no rank measure can see it.

### Sparse keys buy 0.18 bits and cost nothing

Three seeds, 60,000 characters, width 64:

    scheme            mean   spread
    table keys       5.528    0.027
    dense (drawn)    5.524    0.053
    sparse k=8       5.346    0.074
    sparse k=4       5.342    0.067
    cache 128        5.294    0.088

The effect is about 0.18 bits against spreads of 0.05–0.07, so roughly three
times the noise. **And it is free**: the same `d x d` store, the same parameter
count, just sparse key vectors. Against the exact cache's 0.23 bits for FIVE
TIMES the numbers held (20,480 against 4,096), sparse keys are far and away the
better trade per unit of state.

**They also cut what a node has to send.** A key with 4 active dimensions out of
64 is cheaper on the wire than a dense one, so this is the rare mechanism that
helps the loss and the C1 budget at the same time.

### Why nobody had this number

`key_active` is not new. It was measured in
[g6-01](experiments/sweeps/g6-01-does-sparsity-protect-old-learning.txt) — **on
MQAR**, for retention under forgetting — and that sweep's own opening says
sparse codes "came out worse, so the knob was left off with a measurement saying
not to reach for it."

**It was never measured on the corpus.** This is exactly the hazard CLAUDE.md
names: a mechanism measured only on the task it was designed for is not
measured, and a direction abandoned because it "did not help" may have been
tested on the wrong question. The knob has been sitting default-off, with a
discouraging measurement attached, through every language result this project
has produced.

### What to do about it

Sparse keys are the cheapest improvement available and they have not been swept:
`key_active` was tried at 4, 8 and 16 here at one width and one data size. The
grid worth running is `key_active` x width, because g6-01's headline was that
sparsity's effect is a function of WIDTH, and everything above is at width 64.

Before that grid, probe the bottom of the range locally — decision 63's rule —
and check `key_active` interacts with `derived_keys`, which currently conflict:
sparse keys have no per-token derivation, so a node cannot rebuild a sparse key
from a seed, and that is a C1 problem the sweep would inherit.

---

## 68. Sparse keys are now derivable, which is what makes the 0.18 bits usable

Decision 67 found sparse keys worth about 0.18 bits on the corpus for no extra
state. **A distributed node could not have used them.**

Under C1 a node holds only its own slice and cannot be sent a key table. Dense
keys have been rebuildable from `(seed, token)` alone since note 012 — that is
what `derived_keys` is for. Sparse keys were not, and the two settings were
REFUSED as conflicting:

    "derived_keys and key_active both build Wk and would conflict;
     sparse keys have no per-token derivation yet"

**They do not conflict.** The message says so itself — "yet" — and nobody had
written the per-token draw. So the scheme that is both better on the loss AND
cheaper on the wire was the one scheme a node could not reconstruct, and the
refusal made that look like a design decision rather than an unwritten function.

Four lines: draw the active set from `default_rng((seed, token))` when
`derived_keys` is on, rather than from the shared sequential generator.

**Measured, three seeds, width 64, 60,000 characters:**

    dense derived          5.528   (spread 0.027)
    sparse k=4 drawn       5.342   (spread 0.067)
    sparse k=4 DERIVED     5.359   (spread 0.073)

The win survives derivation — 0.17 bits against dense, inside the spread of the
sequentially-drawn version. **So the cheap improvement and the C1 property can be
had at the same time**, which was not true an hour ago.

### The test that matters is not the reconstruction one

Rebuilding a row from `(seed, token)` is the obvious assertion and it is the
weaker one. The property a late-arriving node actually needs is that a row does
**not depend on the rows drawn before it** — a sequential draw satisfies "I can
rebuild row 3" only if rows 0–2 are rebuilt first, and it looks identical from
outside. `test_a_row_does_not_depend_on_the_rows_drawn_before_it` builds a
40-token model and an 8-token model at the same seed and requires the first eight
rows to match exactly.

### Records corrected, per rule 5

`tests/test_sparse_keys.py` opened with "the result is negative: on this task
sparse keys are worse than dense signed ones at every sparsity tested". True on
MQAR, and it is the sentence that kept the knob off. It now carries the corpus
numbers next to it.

`tests/test_derived_keys.py::ConflictingKeySchemesAreRefused` asserted the
refusal. **Replaced rather than loosened**, per rule 11: what mattered about it
was that neither setting silently wins, and that is now asserted directly — the
row must be BOTH sparse and order-independent.

Two mutations needed re-pointing and both are caught. **A repo rail caught a test
of mine that asserted nothing** — `test_both_together_are_accepted` relied on "no
exception raised", which R4 refuses. That is the third automated check today to
catch something I would not have.

---

## 69. Everything found so far moves the LEVEL. Nothing moves the SLOPE.

The synthesis of the day, and it is the useful negative.

    chars    dense  sparse k=4    gain
     4,000   5.565       5.380  +0.185
    16,000   5.519       5.367  +0.151
    62,500   5.529       5.403  +0.126
   250,000   5.505       5.370  +0.135

**Sparse keys at 4,000 characters already beat dense keys at 250,000.** So they
are a per-character efficiency win rather than a raised ceiling — and they
saturate exactly as fast, flat from 4,000 onward within the seed spread.

Putting every mechanism this project has measured on one axis:

    mechanism            effect on LEVEL      effect on SLOPE
    width, 4x                    +0.089                 none
    exact cache, 128 slots       +0.19 (g11-06)         none
    sparse keys, k=4             +0.15                  none
    pair keys                    -0.23                  none
    trained Wv                   -0.45                  none
    carry store (training)       -0.15                  none

**Six mechanisms, three of them helpful, and not one of them changes the fact
that the model converges by about 16,000 characters and then stops.** They move
where it converges TO. The backprop baseline over the same range moves 0.95 bits
and is still moving at 1,000,000.

### Why this is the finding rather than a disappointment

It reframes what to look for. A level improvement is worth having and three of
them stack to something real — but **no number of level improvements reaches the
goal**, because the goal is a model that keeps learning as more of the internet
is fed to it. Stacking every positive result found today gets to roughly 5.1
bits, which is still worse than a unigram at 4.829, and it would still be flat.

So the question stops being *what raises the score* and becomes:

> **What would make the loss keep falling with data at all?**

Everything measured today says the answer is not width, not state, not sparsity,
and not more persistent parameters. The one arm that does keep falling is the
backprop baseline, which differs in exactly one respect that has not been
isolated: **its parameters are trained through a composed function, and ours are
not.** `Wo` is a single linear map onto a retrieval it does not influence.

### The measurement that would test that

Not "make the model bigger" in any of the senses already refuted. **Give the
model one trained stage that feeds another trained stage**, so there is a
composition to learn, and see whether the data exponent moves off zero. That is
the smallest thing that distinguishes us from the baseline, and it is the one
axis nothing here has varied.

**And it is exactly where C1 bites**, which is why it is the real question rather
than a detour: training a stage that feeds another stage is what backpropagation
does, and note 036 was written about whether that can be made local. The project
has been measuring the consequences of not having it without testing for it
directly.

### Status of the sparse-key line

Worth keeping and worth finishing — 0.15 bits for no state, derivable per token
(decision 68), and cheaper on the wire. But it should be understood as **making
the plateau lower, not later**, and the `key_active` x width grid should be
costed against that expectation rather than a hope of a slope.

---

## 70. The readout was the ceiling all along

**The most consequential measurement of the session, and it reverses the working
diagnosis.**

`r = M @ key` depends only on `Wv` and the keys, both frozen. So the retrieval is
independent of `Wo` and the features can be extracted once and any readout
trained on them offline. Four data sizes, scored on features from the corpus's
own held-out test split:

    chars  samples   linear   MLP-128   MLP-512
     4,000    3,937    5.579     5.388     6.320
    16,000   15,875    5.436     4.865     5.214
    62,500   61,976    5.351     4.757     5.097
   250,000  248,031    5.320     4.525     4.659

      linear     b = -0.0115   R2 0.93
      MLP-128    b = -0.0397   R2 0.92
      MLP-512    b = -0.0681   R2 0.89

      our model in situ         b = -0.0010   FLAT
      backprop attention        b = -0.0243
      published: DFA -0.040, backprop -0.071

**On identical frozen features a two-layer readout recovers a backprop-like data
exponent.** MLP-512 at -0.0681 is indistinguishable from published backprop, and
steeper than our own attention baseline.

> **AMENDED by decision 71.** This holds with Adam and many epochs and does NOT
> hold in the online single-pass regime C4 requires, where the composed arm fits
> -0.0152 against the baseline's -0.0243. Composition still doubles the exponent
> and still beats the unigram; "backprop-like" was the optimiser talking. The gap over linear widens
monotonically: 0.19, 0.57, 0.59, 0.80 bits.

**MLP-128 reaches 4.525 bits, past the unigram at 4.829** — a bar this project
has never cleared — on features that never learn anything.

### Why nobody looked here

Every previous diagnosis blamed something upstream. Decision 59 blamed the sum.
Decision 62 blamed persistent capacity. Decision 65 blamed rank; decision 67
refuted that. **The readout was never suspected because it is exactly right** —
the delta rule on `Wo` IS the exact gradient for a single linear readout, which
CLAUDE.md has said all along and which was read as "so it cannot be the problem".

It was doing its job perfectly. **A perfect linear readout is still linear.**

That reading also disposes of a claim I have repeated all day: "every component
passes its capability test in isolation and the whole fails". The readout passed
its test because its test asked whether it was the correct linear map. Nothing
asked whether a linear map was the right thing to be.

### What it does not say

**Nothing about local learning.** These readouts were trained by ordinary
backpropagation, offline, Adam, many epochs, fixed dataset. The deployed model
learns online in one pass by a local rule. This shows the information is present
and that a composed function extracts it — not that one can be trained the way
this project needs. Note 037 pre-registered that limit before the run.

About 0.19 bits of the offline gain is better optimisation, not architecture: the
offline LINEAR arm reaches 5.320 against the in-model 5.505. **The remaining 0.80
is composition**, and that comparison is clean — same features, same optimiser,
same epochs, same test set.

### Why this is C1-compatible, which is what makes it the main line

`partitions` already splits the readout by dimension: each node holds its own
`vocab x d/groups` slice and computes its own `parts[g]`. **If a node's slice
became two layers instead of one, backpropagating through those two layers uses
only that node's own activity and its own error.** No other node's state enters.
A composed readout inside a node is not a locality violation — it is the same
locality applied twice.

So note 036, filed as background reading, is now the main line, and the next
measurement is specific: **a per-node two-layer readout, trained by local
backprop within the node, online**, on the data axis with the bottom of the range
probed first (decision 63).

### And it reframes John's question from this evening

He proposed that the project is training the wrong thing — prediction rather
than understanding relationships between ideas. **The ordering objection I gave
him is now weaker than it was an hour ago.** I argued the model could not
represent relationships even in principle, so changing the objective would
produce an uninterpretable null. That argument rested on the ceiling being the
representation. It is not; it is the readout. A model whose readout can express
composition is a much better candidate for a relational objective than the one I
was describing.

The ordering still holds — fix the readout, then change the objective — but the
gap between the two steps is far smaller than I told him.

---

## 71. Composition survives the online regime, and decision 70 overstated by how much

**Decision 70 is amended here, not retracted.** It claimed a two-layer readout
"recovers a backprop-like data exponent". That is true with Adam and many epochs.
It is not true in the regime C4 requires.

Online, single sample at a time, plain SGD, no momentum, learning rate swept on
BOTH arms and chosen on held-out calibration text, temperature calibrated the way
the model does it:

    chars   linear     lr   2-layer     lr    gain
    16,000   5.284    0.5     4.860    0.2  +0.424
    62,500   5.228    0.5     4.802    0.2  +0.426
   250,000   5.185    0.2     4.661   0.05  +0.524

    fitted over the same 16k-250k range as the offline arms
      online linear      b = -0.0069   R2 0.99
      online 2-layer     b = -0.0152   R2 0.95
      offline linear     b = -0.0078   R2 0.93
      offline MLP-128    b = -0.0264   R2 0.96

      backprop attention                b = -0.0243
      our model in situ                 b = -0.0010

### What holds

**Composition is a large, real win in the deployed regime.** 0.42 to 0.52 bits
over a linear readout on identical features with identical training, and the gain
grows across the range rather than shrinking.

**It roughly doubles the data exponent** — -0.0152 against -0.0069 — so it moves
the SLOPE and not only the level. That makes it the first mechanism out of seven
to do so. Decision 69 listed six that moved the level and none the slope.

**4.661 bits beats the unigram at 4.829.** This project has never cleared that
bar, and the sweep table in HANDOFF has carried `<- WE STILL LOSE TO THIS` next
to it for months.

### What does not hold, and it is decision 70's claim

**The online exponent is 58% of the offline one** — -0.0152 against -0.0264 — and
falls well short of the backprop attention baseline at -0.0243. So "recovers a
backprop-like exponent" is a statement about Adam-with-epochs, not about a rule
this project can deploy. The optimiser is worth roughly as much as the
architecture here, which is precisely the confound CLAUDE.md's rule about
sweeping a hyperparameter on every arm exists to expose — and which I failed to
apply twice before applying it.

Note the learning rates: the linear arm wants 0.5 and 0.2, the composed arm wants
0.2 and 0.05, and the best rate FALLS as data grows. A fixed learning rate is
leaving something on the table, and under C4 there is no "end of training" at
which to anneal one. **How to set a step size in a system that never stops is now
an open question with a number attached** — and it is note 036's item 3, the one
unexplored entry on that scan's carry-forward list: scale a step size to its own
estimator's variance.

### Still not the deployed regime, and this is the next honest step

These runs used TWO passes. C4's regime is ONE. A second pass over a corpus is
already a mild violation of "learns from what it sees as it goes", and the gap
between two passes and one is exactly the kind of thing that has swallowed a
result here before. **The next measurement is single-pass**, and it should be
prequential — predict, then learn, score what was predicted — because a
train/test split measures a system that stops.

---

## 72. 4.540 bits, prequential, single pass, no temperature — past the unigram

The strictest regime this project has measured in, and the best number it has
produced.

    PREQUENTIAL, one pass, no split, no temperature calibration
             arm     lr   whole stream   last 20%
          linear    0.5          5.183      5.174
     2-layer 128    0.2          4.572      4.540

    unigram 4.829   bigram 3.583   backprop attention ~4.05 (on a split)

**Every character is scored by a model that has not seen it, and then becomes
training data.** No held-out set, no second pass, and no temperature — the
model's usual temperature is fitted on text it has not reached, which C4
forbids, so the readout learns its own scale instead. All three choices make the
number harder to achieve, not easier.

**4.540 beats the unigram at 4.829.** `HANDOFF.md` has carried
`<- WE STILL LOSE TO THIS` against that line for months; it is removed.

Composition is worth **0.63 bits** here — larger than in any other regime tested,
and larger than the exact cache (0.19) and sparse keys (0.15) combined.

### The question C4 actually asks, and the answer is not yet yes

Whole-stream against last-20% is the cheap test of whether a learner is still
learning:

    linear     5.183 -> 5.174    improved 0.009 over the stream
    2-layer    4.572 -> 4.540    improved 0.032

**Both are close to converged.** The composed arm is improving about three and a
half times faster at the tail, which is a direction rather than a result. So the
honest statement is: **composition converges LOWER and slightly LATER — it is not
yet a system that keeps learning.**

That is the distinction C4 makes load-bearing. A perpetual learner that settles
on a better constant is still a settled system, and this one settles.

### What that leaves

The gains are now stacked in the right order and each is measured in the
deployed regime: composed readout (0.63), sparse keys (0.15, and they also
resist forgetting, which C4 makes matter), exact cache (0.19 but at 5x the
state). None of them has produced a learner that does not converge.

**The open question is the step size**, and it is the same one decision 71 raised
from the learning-rate column: the best rate falls as data grows, and under C4
there is no end of training at which to anneal. A learner whose step size decays
to nothing has stopped, whatever its architecture. Note 036's item 3 — scale a
step size to its own estimator's variance — is the only unexplored entry on the
literature scan, and it is now pointed at directly by two independent results.

---

## 73. The step size is not the obstacle. The FEATURES being frozen is.

**Decision 72 named the step size as the open question. This refutes that**, and
what it leaves is sharper.

Three step-size rules, all local, all online, prequential over 248,000
characters, learning rate swept within each rule, reported by quintile because
the question is whether it is still descending when the data runs out:

        rule      lr      Q1      Q2      Q3      Q4      Q5    Q5-Q1
       fixed     0.2   4.727   4.539   4.497   4.555   4.540   -0.187
  normalised   0.001   4.663   4.560   4.529   4.736   4.696   +0.032
   idbd-lite    0.05   4.944   4.656   4.577   4.606   4.593   -0.351

**All three plateau by Q3 and then wobble.** IDBD — Sutton's incremental
delta-bar-delta, designed for exactly this setting — improves the most across
the stream and still lands worse than a fixed rate. RMS normalisation actively
degrades after Q3. The best absolute number, 4.540, belongs to the simplest rule.

So an adaptive step size does not produce a learner that keeps learning, and the
falling-learning-rate pattern that suggested it (decision 71) was a symptom, not
the disease.

### What the plateau actually is, and it should have been obvious

**The features are frozen.** `Wk` and `Wv` are drawn once; the store is rebuilt
each chunk from those frozen projections. So the readout — however deep, however
trained, whatever its step size — is fitting a function of a FIXED feature map.

**A fixed feature map has a best achievable loss, and once the readout reaches
it, more data cannot help by construction.** That is not a defect of the
optimiser. It is what "fixed features" means. Every arm above converges to about
4.5 because that is approximately the best a function of these particular
features can do.

Decision 70 found the readout was the ceiling and composition raised it by 0.63
bits. **The ceiling did not disappear; it moved up one level.** It is now the
feature map, and no amount of work downstream of it will move it again.

### Why this is not a return to decision 65

Decision 65 tried to make the features adapt by training `Wv` with a delta rule
and it was catastrophic — stable rank 4.06 to 1.88, bits 5.505 to 5.956 —
because a delta rule driven by a shared error signal ALIGNS every value vector
along the directions that error favours, and alignment is collapse.

So the problem is now precisely stated and it is not the one we have been
solving: **make the feature map adapt without collapsing it.** Weight-adjustment
on the value projection is measured and refuted. Something else is required.

### Which is exactly where John's question lands

He asked whether letting the interconnections evolve would raise the ceiling.
When he asked it, my answer was "probably efficiency rather than ceiling,
because the ceiling is the functional form". **That answer is now wrong, and his
instinct is better than my reply.**

Evolving WHICH connections exist is a different operation from adjusting their
weights, and it is not obviously subject to the alignment collapse that killed
decision 65 — a structural change reallocates capacity rather than dragging
every vector toward a common direction. It is the only proposal on the table
that changes the feature map by a mechanism that is not the one already refuted.

The evidence was there before the argument: the two things that worked today
were both structural rather than scalar.

    composition, a new KIND of connection      +0.63
    sparse keys, a connectivity PATTERN        +0.15
    width 16 -> 128, more of the same          +0.089

**Structure beat scale roughly seven to one, and both structural wins beat every
weight-level intervention tried.**

### The measurement, and it is cheap

We know random sparse keys beat dense by 0.15. The minimal form of the question
is whether **learned** sparse beats **random** sparse at the same sparsity
budget and the same wire cost — the only difference being whether the active set
is chosen or drawn.

If learned wins, structural plasticity has a number and the larger version is
worth building. If it ties, random sparsity already captures the benefit and
evolution is complexity for nothing. Either answer is worth having and neither
needs a full plasticity mechanism to obtain.

---

## 74. Sparse keys were compensating for a weak readout, and one chosen topology lost

**A crossover interaction, and it re-scopes decisions 67 and 68.** Prequential
tail, 120,000 characters, three seeds, learning rate swept in every cell:

       readout      keys    mean  spread
        linear     dense   5.222   0.133
        linear   sparse4   4.794   0.076    sparse WINS by 0.428
       2-layer     dense   4.487   0.034
       2-layer   sparse4   4.586   0.045    sparse LOSES by 0.099

Both differences clear their spreads. **Sparse keys help a linear readout
substantially and hurt a composed one.**

The reading: sparsity reduces interference between tokens, and interference only
costs you if the readout cannot disentangle superposed features. **A linear map
cannot; a composed one can** — so once the readout is capable, it prefers the
denser and more informative representation. Sparse keys were never a
representational improvement. They were a workaround for a readout that could
not cope with overlap.

**Best configuration measured so far: 2-layer with DENSE keys, 4.487 bits** —
better than the 4.540 recorded in decision 72, at less than half the data,
because the learning rate was swept over four values here rather than two.

### What this does to decisions 67 and 68

Not retracted, re-scoped. Their numbers were measured with the model's LINEAR
readout and are correct for it. **Their recommendation does not survive the
readout change**, and it was a standing recommendation, which is worse.

Sparse keys keep two arguments that this measurement does not touch: they are
cheaper on the wire (C1), and g6-01 measured them protecting old learning from
new, which C4 makes load-bearing. So the position is now a TRADE — 0.099 bits
against bandwidth and forgetting-resistance — rather than a free win. Decision 68
made them derivable per token, which stands regardless.

### John's topology question, answered narrowly and in the negative

    sparse k=4 DRAWN        4.629
    sparse k=4 ALLOCATED    4.736

Allocating each token's active dimensions by frequency, round-robin so frequent
tokens get disjoint address space, is WORSE than drawing them at random.

**This is one hand-designed static topology failing, not a refutation of
structural plasticity.** Round-robin-by-frequency also places addresses
systematically, which imposes structure uncorrelated with the corpus, and a
static allocation cannot adapt — which is the entire point of the proposal it
was standing in for. What it does establish is that *merely choosing* addresses
is not free improvement, so a plasticity mechanism has to earn its result rather
than inherit one.

### The methodological finding, which may outlast the numbers

**Three times today a mechanism turned out to be configuration-specific rather
than good or bad.** Sparse keys were dismissed on MQAR (g6-01), revived on the
corpus (decision 67), and are now re-scoped again by the readout (here). The
exact cache's advantage is measured against a linear readout and has not been
re-checked against a composed one. Nor has the retrieval seam's whole comparison
set.

The standing rule — *a mechanism measured only on the task it was designed for is
not measured* — needs a companion: **a mechanism measured only against the
readout it was tuned beside is not measured either.** Every number in this
project's comparison set was taken with a linear readout, and the readout is the
thing that just changed.

That is a large invalidation and it should be stated plainly rather than
discovered piecemeal. It does not mean the numbers were wrong; it means they were
conditional, and the condition moved.

---

## 75. The composed readout is in the model

Note 037 and decisions 70–72 measured it offline. It is now a config field,
default off, with the locality claim asserted rather than argued.

    hidden   lr      bits (in-model, 60k chars, two seeds)
         0   0.05    5.525
       128   0.05    5.242    +0.283
       128   0.01    5.289
       128   0.002   5.395

**+0.283 bits in the model's own protocol**, which is less than the 0.63 measured
offline and should be. The offline probe initialised and tuned the two layers
separately; here both share the model's single learning rate, and the model
trains two epochs over a train/test split rather than sweeping. The gap between
those two numbers is the value of tuning the layers apart, and it is worth
knowing rather than closing by fiat.

### How it works, and why it does not widen C1

Each group already holds its own `d / partitions` slice of the retrieval and
computes its own `parts[g]`. With `hidden` set, that slice becomes two matrices
the same node owns:

    active[g] = relu(hidden_w[g] @ sliced[g])
    parts[g]  = grouped_wo[:, g, :] @ active[g]

and the backward pass takes group g's hidden gradient from group g's own output
weights and group g's own error. **Nothing crosses a group**, so a node computes
this from what it already holds. That is the same locality the delta rule beside
it has, applied twice.

`orthogonal_every` is refused alongside it: that mechanism orthogonalises an
update whose shape is defined by the LINEAR readout, and across two layers it
would silently orthogonalise a different matrix than the one it was measured on.

### Verification

**Golden values identical across all nine configurations** — plain, context
keys, cache at two settings, settling, consolidation, cache-with-settling,
decay, partitions — so the `hidden = 0` path is untouched and every earlier
number still reproduces. 722 tests, six checks clean.

Two new mutations, both caught:

- `the-hidden-layer-never-learns` — the layer would stay at its random
  initialisation, which is a FIXED projection wearing a learned layer's name,
  and it would still change every prediction, so a smoke test could not tell.
- `the-hidden-gradient-crosses-groups` — group g would take its hidden gradient
  from every group's readout. **The model would still learn and the loss would
  still fall**; only the C1 argument would be false. That is the failure this
  whole file exists to make impossible to miss.

Two existing mutations needed re-pointing (`pool-the-error-across-groups`,
`copy-the-readout-instead-of-viewing-it`) and both are caught.

### A locality test that tested nothing, caught by its own companion

The first version perturbed group 1's readout and checked group 0's hidden layer
did not move. It passed. It also passed for the wrong reason: the fixture zeroes
`wo`, so multiplying group 1's weights by three multiplied zero by three and
nothing moved anywhere.

`test_the_perturbed_group_did_move` is the companion that caught it — the
assertion that the perturbed group DID change. **A locality test without one
passes whenever the mechanism is disconnected**, which is precisely when it
should fail.

---

## 76. The cache was mostly compensation too, and the re-validation found it deliberately

**The first instalment of decision 74's re-validation, chosen first because it
has the largest blast radius.** Cache against its state-matched control, under
both readouts, 60,000 characters, two seeds, learning rate swept per cell:

      readout         arm width slots     bits  spread
       linear       plain    64     0    5.507   0.009
       linear    cache128    64   128    5.252   0.050
       linear     matched   143     0    5.488   0.023
      2-layer       plain    64     0    5.242   0.028
      2-layer    cache128    64   128    5.165   0.035
      2-layer     matched   143     0    5.225   0.020

    the cache's advantage at EQUAL state (20,480 against 20,449)
      linear readout    0.236     reproduces the published 0.244
      2-layer readout   0.060     about 1.5x the seed spread

**Three quarters of it is gone.** The linear row reproduces g11-06 and the
hybrid-store result to within noise, so the measurement is sound and the change
is the readout.

This is the sparse-key story again, less extreme: the cache does not reverse, it
shrinks to the edge of the seed spread. Both mechanisms reduce interference
between superposed items — the cache by keeping some out of the sum, sparsity by
making them overlap less — and **interference only costs you if the readout
cannot disentangle it.**

### The sentence that summarises the day

    linear readout + cache, 20,480 numbers held     5.252
    2-layer readout, no cache, 4,096 numbers held   5.242

**A composed readout with no extra state beats a linear readout with five times
the state.**

### Blast radius, which is why this was re-checked first

Decision 61 argued for partitioning the distributed model by ITEM rather than by
DIMENSION, and its evidence was that the exact cache is that architecture at one
machine's scale, worth 0.19–0.24 bits. **That evidence is now worth 0.06 bits at
the edge of noise**, so the architectural argument stands on the C1 and C3
reasoning — partial-tolerance, bounded bytes per hop — and no longer on a
performance result. It is not refuted. It is unsupported, which is a different
thing and has to be said differently.

### The methodology worked, and that is the point

Decision 74 named the invalidation and said to re-check by blast radius. This was
run **deliberately, first, because of what rested on it** — not stumbled into
three weeks later when a follow-up sweep produced a confusing number. The rule
was written at 22:20 and paid for itself within the hour.

### Still to re-validate against a composed readout

Everything else in the comparison set, and it should be done by the combinatorial
grid rather than one probe at a time: pair keys (refuted at 0.23 worse),
competitive retrieval (refuted 0.924 → 0.128), orthogonal updates, the write
gate, consolidation. **Each of those refutations was measured beside a linear
readout.** A mechanism refuted for adding information a linear map could not use
is not refuted for a readout that can.

---

## 77. g11-07: two refuted mechanisms partially recover under a composed readout

**The payoff of the whole re-validation thread, and it says the record needs more
re-checking rather than less.** 18 of 18 cells, run `30329481170`.

    readout = hidden128        cache128           plain         settle2
        dense              5.172+/-.024    5.242+/-.014    5.232+/-.009
        pair               5.434+/-.009    5.342+/-.009    5.278+/-.002
        sparse4            5.188+/-.016    5.301+/-.004    5.276+/-.024

    readout = linear           cache128           plain         settle2
        dense              5.299+/-.044    5.525+/-.014    5.654+/-.009
        pair               5.524+/-.001    5.775+/-.011    5.904+/-.016
        sparse4            5.263+/-.034    5.374+/-.026    5.530+/-.017

    best: keys=dense, retrieval=cache128, readout=hidden128   5.172

**P1 confirmed exactly** — the baseline is 5.525 against a predicted ~5.53, so
the grid reproduces what three separate probes reached today.

**P2 confirmed, 9 of 9.** The composed readout wins at every keys x retrieval
combination, margins 0.075 to 0.626. It is the largest single factor in the grid.

**P4 confirmed.** The sparse crossover replicates across six cells, having been
measured once at 120,000 characters.

### P5 confirmed, and it is the result

The pair-key penalty against dense keys:

    retrieval    linear   hidden128
    plain        -0.250      -0.100     recovers by 0.150
    settle2      -0.249      -0.046     recovers by 0.203
    cache128     -0.225      -0.262     does not recover

**Pair keys were penalised for carrying information a linear readout could not
use.** Give it a readout that can and 60-80% of the penalty disappears. The one
exception is instructive: alongside the cache the penalty gets slightly worse,
so those two mechanisms compete rather than compose.

### And settling, refuted at 0.924 -> 0.128

Plain minus settle2, positive meaning settling is BETTER:

    linear      dense -0.129   pair -0.129   sparse4 -0.156
    hidden128   dense +0.010   pair +0.064   sparse4 +0.025

It goes from costing 0.13-0.16 bits to gaining 0.01-0.06, and its best showing is
alongside pair keys — the other mechanism the linear readout was penalising.

**Neither of these is a rehabilitation and it would be easy to overstate them.**
Settling gains 0.06 at best, and its original refutation was measured on
synthetic recall rather than on text, so this does not contradict that
measurement — it says the refutation does not transfer here. What is established
is narrower and more important: **a refutation taken beside a linear readout is
not evidence about a composed one.**

### P3 refuted, and the refutation is more interesting than the prediction

The cache was predicted to shrink under `hidden128` at every keys setting. It
shrinks with dense (0.226 -> 0.070), REVERSES with pair (0.251 -> -0.092), and is
**unchanged with sparse keys** (0.111 -> 0.113).

So the cache is not uniformly compensation for a weak readout. Alongside sparse
keys it survives the readout change intact — which decision 76's single-setting
measurement could not have shown, and which is a direct argument for grids over
one-at-a-time probes.

### What this costs

Four mechanisms in the comparison set have still not been re-checked against a
composed readout: the write gate, consolidation, readout bias, orthogonal
updates. Two of the two refuted mechanisms that WERE re-checked moved. **That is
not a reassuring base rate**, and the remaining four should be assumed
conditional until measured.

---

## 78. Four things limiting us that are not constraints — audited, and approved

John asked whether anything constrains this project more tightly than *"it runs
on all sorts of devices across the internet"* does. **C1–C4 themselves are
sound** — C1 was already amended for exactly this reason and C2, C3 and C4 each
state something the internet or the goal genuinely requires. The over-constraints
are not in the constraints. They are in things being TREATED as constraints that
never were.

All four are approved for action.

### 1. Single-pass training — my error, from a few hours earlier

I derived "the deployed regime is online and single-pass" from C4 and wrote it
into GOALS.md. **C4 forbids stopping, not revisiting.** A system with a replay
buffer that never freezes satisfies it completely.

The cost was immediate and I did not see it: decision 71's two-pass result was
described as "still not the deployed regime" and decision 72's single-pass run
was treated as the stricter, better measurement. The stricter reading also rules
out **replay**, which is one of the few known answers to the catastrophic
forgetting C4 makes first-class. **Corrected in GOALS.md.**

### 2. Character-level modelling — probably the largest, and not a constraint at all

A benchmark choice. Note 035 measured the store faithfully holding a character
bigram table, and **a character bigram table over 66 symbols is genuinely
low-rank because English is** — so some of the ceiling fought all day is the
TASK, not the architecture.

It also sits directly against the goal. John's relational-objective proposal is
about relationships between ideas, and **concepts cannot be represented over
characters.** No change of objective helps while the units are letters.

Approved, and it needs its own plan rather than a quick swap: changing the task
changes every number in the comparison set, which is decision 74 all over again
at a larger scale.

### 3. The readout's cross-group sum, carried as a fatal violation

`answer = parts.sum(0)` has been recorded as an outstanding C1 violation since
note 009 §4, and described as one repeatedly today.

**Under AMENDED C1 that deserves re-examination.** It is vocab-length — 64
floats per group per step — which is bounded. The amended test is *"does
progress stall when one participant is slow or gone"*, and a node predicting
from whoever answered in time is eventually-consistent rather than a barrier.

It may be admissible now. It has been carried as a known bug under a rule that
no longer applies, which is the exact failure the amendment was written to
prevent.

### 4. The per-chunk store reset

Correct for MQAR and `reward_recall`, where sequences are independent and
accumulating would be answering from the training set. Carried into the corpus
experiments unexamined, where it caps the model's memory at **128 characters**.
Deliberate and guarded, but a default inherited from another context — not a
constraint.

### The pattern, which is decision 74 one level up

Three of these four are defaults inherited from a context that no longer
applies, and the fourth is a rule I derived too strictly. **Decision 74 found
that measurements were conditional and the condition had moved. This finds the
same of constraints.**

The habit that follows: when a constraint is invoked to rule something out, say
which of C1–C4 it comes from. Three of these four could not have named one.

---

## 79. g11-08: the write gate is real and shrinks, consolidation stays refuted, bias reverses

16 of 16 cells, run `30330836119`, after the first attempt's write-gate arm
turned out to be disconnected.

    write against plain, positive means BETTER
                    linear   hidden128   bias   hidden128+bias
    gated           +0.183      +0.092  +0.056         +0.078
    corrective      +0.120      +0.037  +0.003         +0.041
    consolidating   -0.040      -0.026  -0.017         -0.053

**Best cell in either grid: `readout=hidden128, write=gated` at 5.149**, beating
g11-07's best of 5.172. And the two have not been combined — g11-07's winner used
the cache with plain writes, this one uses gated writes with plain retrieval.

**P1 confirmed** — the baseline is 5.525 again, a fourth independent reproduction.

**P2 confirmed** — the composed readout wins at every write setting, and at every
setting with the bias too.

**P3 confirmed.** The write gate's effect shrinks under a composed readout,
0.183 -> 0.092. Separating `corrective` from `gated` is what makes this readable:
corrective writes alone are worth 0.120 under linear, and the GATE adds a further
0.063 on top. Without that separation the gate and the mechanism it gates would
have been one number.

**P4 confirmed.** Consolidation does not recover — harmful under every readout,
-0.017 to -0.053. It is a claim about RETENTION rather than about information a
linear map could not read, and the readout does not change its verdict. **The
only mechanism of five whose verdict survived intact.**

**P5 refuted, in both directions.** Readout bias HELPS a linear readout by 0.197
and HURTS a composed one by 0.069. It was refuted in g11-02 on `reward_recall`;
on text it is a substantial help to a linear readout. And a hidden layer can
already represent a constant, so adding a bias on top costs rather than being
redundant — which is the half I predicted and the wrong half.

### The tally across both grids

Five mechanisms re-checked against a composed readout. **Four moved:** pair keys
recovered 60-80%, settling went from -0.14 to +0.03, the write gate shrank by
half, readout bias reversed sign. **One held:** consolidation.

That is the answer to whether the re-validation was worth it.

## 80. g12-01: the window's 7.3x was 6.26x, and the grid did not contain its answer

Note 014 measured the asynchrony window once and said in its own text that the
number should not be trusted — no repeats, no error bars, *"timing is the
noisiest thing this project has ever measured"*. It asked for a repeated sweep
before the figure went into GOALS. It never got one, and the figure has been
quoted since.

24 of 24 cells, run `30332373446`, four nodes, width 16, three repeats:

                          w=1                w=2                w=4                w=8
    80ms/20ms/2%   0.12411+/-.0057   0.05191+/-.0054   0.03269+/-.0028   0.01983+/-.0043
    clean          0.00053+/-.0000   0.00040+/-.0001   0.00043+/-.0001   0.00032+/-.0001

    impaired  w1->w2  2.39x   w1->w4  3.80x   w1->w8  6.26x    all clear the spread
    clean     w1->w2  1.31x   w1->w4  1.23x   w1->w8  1.66x    two of three INSIDE it

**P1 confirmed, and it is the one that matters.** All 24 runs agree with the
single-process model, at every window and both links. The window is a
performance knob and not a correctness bug — which had never been established
across repeats, and which everything else here depends on.

**P2 confirmed, with the original number corrected.** The impaired-link speedup
is **6.26x**, not 7.3x. The effect is real and large, and the unrepeated
measurement was about 17% high. That is a modest error and exactly the size of
thing repeats exist to catch; the claim it supports — that obeying C1 is what
makes the design usable — survives.

**P3 confirmed.** On a clean link the win is 1.66x, and two of the three
comparisons sit **inside their own spread** and are therefore not measurable at
all. Most of the impaired-link speedup is the LINK, not the protocol, which is
what note 014 argued and could not show.

**P4 half confirmed, and the other half is a criticism of my own grid.** The
impaired curve is monotone. It has NOT flattened:

    w1 -> w2   2.39x per doubling
    w2 -> w4   1.59x
    w4 -> w8   1.65x

**Window 8 is the largest value tested, it is the best, and it is still
improving 1.65x per doubling.** `tools/grid.py` exists to catch exactly this —
*a sweep that does not contain its own answer has not swept; if an arm chooses a
value at an edge of the grid, the optimum lies outside it.* I wrote the grid,
predicted flattening, and did not check the prediction against the grid's own
range before dispatching.

So the useful window is somewhere above 8 and this sweep cannot say where. The
re-run needs 16, 32, 64 — and the cost is trivial, which makes the omission
worse rather than better.

### What this settles

Note 014's caveat is discharged: the number is now measured, repeated, and
smaller than advertised. **The 7.3x should be replaced by 6.26x wherever it is
quoted**, and GOALS may now carry it, with the standing caveat that the topology
is a Docker bridge with one-way impairment and a real link is worse.

---

## 81. C3 measured over real containers, and the cost tracks the FRACTION lost

**The least-tested constraint in the project, tested.** GOALS.md said departure
*"has never been tested in the predecessor project, because nothing ever left"*,
and note 014 said `absent`/`leave_at` had never run over a real network. 18 of 18
cells, run `30333987195`.

**P1 confirmed, and it is the constraint.** `mismatches_before_departure == 0` in
every cell, at 4, 8 and 16 nodes, losing one node or a quarter, at three
departure steps. **A machine switching off never changed an answer already
given.**

**P4 confirmed.** Every cell completed. No hang, no timeout — which matters more
than it sounds, because a hang is exactly what a barrier looks like when a
participant vanishes, and the absence of barriers is what C1 is for.

**P2 and P3 confirmed, and more sharply than predicted:**

    fraction lost   leave_at 10   leave_at 20   leave_at 30
        0.062 (1/16)         7             4             1
        0.125 (1/8)          9             7             2
        0.250 (1/4)         11             5             1

**At a quarter lost the counts are IDENTICAL across 4, 8 and 16 nodes** — 11, 5
and 1, three times each. Losing one of four costs exactly what losing four of
sixteen costs.

So the damage is a function of **what fraction of the store went away and how
many steps remained**, and not of how the network was divided. That is the
property you would want from interchangeable slices and it had never been
measured. It also means the tiny-node direction is not paying a penalty for
being tiny: 16 nodes losing one is the CHEAPEST cell in the grid, because one
node is a smaller share.

### What this does not establish

**A clean departure is the easy case.** These nodes are told to stop answering.
A machine losing power leaves a half-open socket that accepts and never replies,
and nothing here tests that.

**Mostly single runs.** Only the 4-node/one-lost cells have two repeats; the
rest are one, so the table shows `+/-inf`. The exact agreement across three node
counts at equal fraction is worth more than the repeats would be — three
independent configurations producing identical integers is not noise — but the
per-cell numbers are unrepeated and should be read as such.

**Clean link.** Churn under impairment is untested; a divergence there could be
either cause, which is why they were separated. That grid comes next.

### The cost of getting here

Three dispatches. **Two were spent on my own plumbing** — an undeclared matrix
key that expanded to empty in all 18 cells, then a `verdict` function that
failed a churn run for behaving correctly. Neither told us anything about C3.
Both are now caught by a check or a test rather than by my remembering, which is
the only reason the count stops at three.

---

## 82. The window sweep ran into a constant nobody varied, twice

g12-03 was the correction to g12-01's grid not containing its own answer. **It
does not contain its answer either**, and this time the reason is more
instructive than the numbers. 15 of 15 cells, run `30335087486`:

                       w=1        w=8       w=16       w=32       w=64
    80ms/20ms/2%   0.11865    0.01779    0.00991    0.01479    0.00571
                  +/-.0065   +/-.0021   +/-.0007   +/-.0038   +/-.0005

    w1 -> w8  6.67x    w1 -> w16  11.97x    w1 -> w32  8.02x    w1 -> w64  20.77x

**P1 confirmed** — all 15 agree, including at window 64. Reassembly by step index
holds however far ahead a node runs.

**P4 confirmed** — window 1 measures 0.11865 +/- 0.00654 against g12-01's
0.12411 +/- 0.00570. Overlapping, so the harness has not moved.

**P2 and P3 refuted, for the second time.** The curve does not flatten. It is
also not monotone: window 32 is WORSE than window 16, by more than window 16's
error bar.

### The cause is a constant I never varied

**Every run in this line is 40 steps.** The window is how far ahead a node may
run before it must have heard — so **once the window exceeds the run length it
stops binding at all.** Window 64 on a 40-step run is not a window setting. It is
the no-synchronisation limit wearing one.

    window   1  binds
    window   8  binds
    window  16  binds
    window  32  binds, barely
    window  64  EXCEEDS THE RUN

So the top of the grid measures something else entirely, and the "still
improving at 64" that refuted P2 is the curve walking off the end of its own
axis into a different experiment. Window 32 sitting between them, noisy and
non-monotone, is what a barely-binding cell looks like.

**This is CLAUDE.md's own rule and I did not apply it:** *a variable that never
changes does not look like a variable, it looks like the background.* `steps` has
been 40 in every window measurement this project has made, including note 014's.
I widened the window axis twice without once asking what it was widening
against.

### What the honest reading is

I pre-registered it, so it stands: *"If P2 fails again, the honest reading is not
'use an even bigger window'. It is that the model work here is so small the
window is bounded by something other than the link, and this sweep should stop
chasing it."*

That is what happened, and the something is the run length.

**The right quantity is not where the curve flattens.** It is what fraction of
the no-synchronisation limit a given window achieves — because that limit is what
the curve is approaching, and it is reachable trivially by setting the window
above the run. Reframed that way, window 16 gets to 58% of the unsynchronised
speed while still requiring a node to have heard within 16 steps.

**No third widening.** A fourth sweep on this axis would be the measurement
revised three times that rule 17 says to stop making: *"a measurement revised
twice is no longer the bottleneck. Publish the bound, name the caveat as
permanent, and move."*

The usable figures: **window 16 is worth 11.97x over lock-step on an 80 ms link
losing 2%**, agreeing exactly, at a 40-step run. That is the number to quote,
with the run length stated beside it because it is part of the measurement.

---

## 83. G0 for the chain task: one hop is perfect, two hops answers the intermediate 100% of the time

**The cleanest instrument result this project has produced**, and the one that
makes the hop mechanism worth building.

     hops  chains   floor   linear  hidden128
        1       4   0.250    1.000      0.555
        1       8   0.125    1.000      0.510
        2       4   0.250    0.000      0.020
        2       8   0.125    0.000      0.030

**G0 passes.** One hop is 1.000 with a linear readout — the task is wired
correctly, the model solves it, and note 038's positive control holds. A zero at
two hops is therefore readable rather than ambiguous.

**Two hops scores BELOW chance, and that is the finding.** A model guessing
scores 0.250. Scoring 0.000 means it is confidently producing a specific wrong
answer, every time. Which one:

    100.0%   the INTERMEDIATE (b) -- one hop, then stopped

**Every single test sequence.** The store binds `a -> b`, retrieval with `a`
returns `b`, and the readout emits it. The model performs exactly one hop,
correctly, and has no mechanism to take the second.

That is not "the task is too hard" and not "the task is broken". It is a precise
statement of the architectural gap, and a random-looking failure would have left
the mechanism unmotivated. **Decode-and-re-encode now has a number to beat: any
2-hop accuracy above 0.000 is progress, and the ceiling is the 1.000 the model
already reaches at one hop.**

### The composed readout LOSES here, which is the fifth instance of one pattern

`hidden128` scores 0.51-0.56 at one hop where the linear readout scores 1.000.
On text it won 9 of 9 cells in g11-07 and was the largest single factor in the
grid. On exact retrieval it halves accuracy.

The reading: a hidden layer helps when the answer is a STATISTICAL function of a
superposed retrieval, and hurts when the retrieval already contains the exact
answer and only needs reading off. Composition buys generalisation and costs
fidelity.

This is decision 74's pattern for the fifth time — sparse keys, the cache, the
write gate, readout bias, and now the composed readout itself. **A mechanism's
effect is a property of the configuration, not of the mechanism**, and the
configuration now includes the task. Worth holding before any hop mechanism is
built on the assumption that hidden layers help.

### What this licenses

The hop axis is a valid instrument. One hop is a known-good positive control,
two hops is a known-zero with a diagnosed cause, and anything between them is
measurable. That is what note 038 said had to exist before the mechanism was
worth writing, and it now does.

## 84. The hop mechanism is built, and the instrument it was built for is contaminated

Decode-and-re-encode is implemented: a hop decodes its retrieval to a token
distribution and re-encodes it as a key, using `Wo`/`Wv` and `Wk`, which already
exist. **No new parameters.** `hops=1` is the default and every golden value is
bit-identical, so nothing earlier moved.

It does not work yet, and the reason is not in the mechanism.

### One real bug, found by measuring instead of reasoning

First attempt scored 0.000 at two hops, and `task=1, model=2` fell from **1.000
to 0.005** — the extra hop destroyed the case that already worked, which was
pre-registered as disqualifying. The diagnosis:

    frozen decoder  (wv @ r) finds the intermediate : 1.000
    learned readout (wo @ r) finds the intermediate : 1.000
    softmax entropy                                 : 3.912
    uniform would be                                : 3.912

**The decode was right and the re-encode threw it away.** argmax found the
intermediate every time, and the softmax over those logits was uniform to three
decimals because top-1 beat top-2 by 0.0388. `weights @ wk` on a flat weight
vector is the *mean of every key row* — one constant vector regardless of what
was decoded.

Fixed by standardising the logits before the softmax rather than by tuning a
temperature. The logit scale moves with `key_scale`, `d_model`, `decay` and
`memory_cap`, so a constant would have worked in this cell and failed silently
elsewhere — decision 74 again. `hop_sharpness=0` reproduces 0.000 exactly, which
is what makes the fix a claim rather than a coincidence.

It bought 0.000 → **0.035**. Real, and nowhere near the 0.250 floor.

### Two hypotheses of mine, both refuted, both by the same method

**"The readout is dragged off decoding by the answer gradient."** Refuted. A
`hop_decoder` axis between the learned `Wo` and the frozen `Wv` transpose is a
null: 0.030 vs 0.035. Worth recording that the first probe appeared to refute
this too but did not — it trained at `hops=1`, which is not the regime where the
drag could happen. A refutation from the wrong regime is not a refutation.

**"Sharpness needs tuning."** Refuted. 2.0 / 6.0 / 12.0 / 30.0 all sit at
0.01–0.04, and 30.0 is effectively argmax.

### Where it actually fails: a four-rung bisection

    A  real mechanism            0.035
    B  oracle KEY for hop 2      0.100
    C  oracle VALUE for hop 2    1.000
    D  one hop, want b           1.000

**C is 1.000.** Handed the correct value vector, the readout produces the answer
perfectly — so the readout, the training budget and everything downstream of
retrieval are fine. The failure is entirely in the **second lookup**, and an
oracle is an upper bound on every proposal that shares it.

### The cause, and it is the instrument

Retrieving with the exact `wk[b]`:

    rank 0 in 54.0% of sequences
    FIRST:   c (THE ANSWER) 54.0%   SEPARATOR 39.5%
    SECOND:  SEPARATOR      45.0%   c (THE ANSWER) 44.5%

**The separator competes with the answer for the same key.** Stating each link
as its own triple makes `b` appear twice — once as a target followed by the next
`sep`, once as a source followed by `c` — so `key(b)` carries two bindings and
the store returns their sum. `a` is never anyone's target, which is exactly why
one hop scores 1.000 and two hops collapse.

**This is decision 82's shape and the false-link bug's shape at once.** The
separator was introduced to fix the false-link defect and it created a second
one, and `test_no_false_chain_link_is_ever_stated` cannot see it: that test only
inspects pairs where *both* tokens are chain symbols, so the separator is exempt
by construction. The guard has a hole exactly where the new defect lives.

### What this licenses

The fix is forced, not chosen. A key with two bindings returns their sum — that
is what a superposed store *is*, not a defect in it. So `b` must appear once,
which means chains must be laid down contiguously (`sep a b c`) with separators
between *chains* rather than between links. That also restores the no-false-link
property, since chain-internal adjacencies are all real links.

It costs something and the cost should be stated: contiguity fixes the offset
from the query symbol to the answer at exactly `hops`. This model has no
positional access so it cannot exploit that, but the instrument would need
interleaving before it could be pointed at a positional model. Contiguity and
shuffling trade off directly — if `b` appears twice the bindings compete, and if
it appears once the offset is constant.

**No hop number from before this fix means anything**, including the 0.035. The
mechanism has not yet been measured on an uncontaminated instrument.

## 85. The hop mechanism composes — and the bug was in the WRITE path all along

**Two hops and three hops both score 1.000, from 0.000.** The model follows a
relational chain no single stated fact answers.

    task  model  sharp  accuracy   answered
       2      1      —     0.000   intermediate 100%
       2      2    0.0     0.015   other 96%
       2      2    2.0     1.000   answer 100%
       2      2   30.0     1.000   answer 100%
       3      1      —     0.000   intermediate 100%
       3      3    2.0     1.000   answer 100%

Every control holds. A **1-hop model still scores 0.000** and still answers the
intermediate 100% of the time, so the task genuinely requires composition and
nothing leaked when it started working. **Sharpness 0 still fails** at 0.015, so
the standardisation is still load-bearing. And 2 through 30 all give 1.000, so
it is not a tuned knob.

### The bug

`key` is the token's key, and it is carried out of the retrieval block into
`previous_key` — which is what the NEXT position writes its binding with. The
hop loop reassigned that same `key`. So with `hops > 1`, **every binding in the
store was written using a re-encoded hop key instead of the token's**.

The hop mechanism was corrupting the memory it was trying to read.

One line, `hop_key = weights @ self.wk`, and it is the same shadowing class the
code three blocks up already carries a warning about — `store` was renamed for
exactly this reason, after shadowing it turned `if wrote:` into an array test.

### Why it took four probes to find

Every measurement pointed at retrieval and the damage was in the write:

- the decode was **correct** (argmax 1.000) — so not the decoder
- the decoder axis was a **null** (0.030 vs 0.035) — so not the drag
- sharpness 2–30 all sat at **0.01–0.04** — so not the temperature
- an oracle KEY gave **0.135** and an oracle VALUE gave **1.000** — which
  correctly localised it to the second lookup, and the second lookup was
  reading a store the hops had corrupted

The tell was a contradiction I could not explain away: `argmax(wv @ r2)` was `c`
in 100% of sequences measured outside the run, and 12% measured inside it, with
prediction and decode agreeing 1.000. Two measurements of the same quantity
disagreeing is not noise — **it means the two runs are not the same run**, and
the only thing that differed was `hops`.

`test_the_store_is_identical_at_every_hop_count` is the invariant, stated so it
cannot come back: **hops change what is read, never what is written.**
`a-hop-key-escapes-into-the-write-path` is the mutation, verified caught.

### What this does NOT license

**`hops` is a fixed count and must match the question exactly.** Measured:

    task  model     acc   answered
       1      2   0.000   other 100%
       1      3   0.000   other 100%
       2      3   0.000   other 100%
       3      2   0.000   intermediate 100%

Overshoot is total, not graceful. A model with more hops than the question needs
walks past the answer into whatever the answer points at; one with fewer stops
early and answers the intermediate. A model that does not know in advance how
deep a question is **cannot use this**, and a mixed workload contains both
depths by definition.

So this is composition with the depth supplied from outside. The next problem is
a halting signal — deciding *when to stop hopping* from something the model can
compute locally — and it is well posed now in a way it was not before, because
both failure directions are measured and the ceiling at every depth is 1.000.

### What it does license

The separator finding in decision 84 stands on its own: it was measured at
`hops=1`, on an uncorrupted store, and took the lookup from 54% to 100%. Both
fixes were needed and neither would have been enough alone.

## 86. A halting signal exists, and it is not confidence

Overshoot is total, so the model must decide when to stop hopping. Before
designing a mechanism, the question is whether the information to do it is
present locally at all. Four candidates, each computable by one node from its
own slice with no barrier, measured on a depth-2 chain and split into hops still
ON the chain and the hop that has walked PAST the end:

     signal    on chain (k<d)    past end (k>d)   separated?
       peak    1.0000 ±0.000      0.9357 ±0.136    no  d=0.67
     spread    0.0123 ±0.003      0.0171 ±0.006    no  d=0.95
       norm    0.1323 ±0.027      0.1849 ±0.069  weak  d=1.01
        gap    3.1119 ±0.705      2.1462 ±1.369    no  d=0.89

**Confidence says nothing.** Every d′ is at or below 1.01, and the model is
0.94-confident *after* it has walked off the end.

That is not a quirk. Past the end, `key(c) → value(separator)` is a **real
binding** — the store has a genuine answer for that query, so the decode is
sharp and correct. The model is confidently answering a question nobody asked,
which is why overshoot scored a clean 0.000 rather than something noisy. **A
confident retrieval is not evidence that the retrieval was wanted.**

### What does separate is the CONTENT

    hop 1: asked[1] 100%
    hop 2: asked[2] 100%                        <- the answer
    hop 3: SEPARATOR 73%, QUERY 27%             <- past end
    hop 4: other chain symbol 55%, asked[0] 45%

The first hop past the end lands on a **structural marker 100% of the time**,
and an on-chain hop never does. The two classes are perfectly separable by what
is retrieved, while being inseparable by how strongly it is retrieved.

### What this licenses

A halting gate is worth building, and it is a **linear function of the
retrieval** — a per-group vector scoring "does this look terminal", which stays
inside a group and adds one vector per group rather than a matrix. The gate does
not need to be told which token is the separator; it needs to learn that some
retrievals mean *stop*, and the measurement says that class is linearly
available.

### What it does NOT license

**That this generalises is untested.** Structural markers exist here because the
task lays them down, and the honest general claim is narrower: *a chain ends at
something structurally different from its links*, which is true of prose
punctuation and of record delimiters but is not proven for either. A gate
trained here learns this task's terminal class, and the first real test is a
task whose terminator was never designed in.

Recorded before building, because the gate's own result will be much harder to
read once the mechanism can move the number.

## 87. The gate learns which hop to read, and mixed depths go to 1.000

Questions of depth 1 and depth 2 shuffled together, nothing marking which is
which. **A fixed hop count must fail half of them by construction**, and that is
what makes the gate's number readable:

    model                overall   depth 1   depth 2
    fixed hops=1           0.500     1.000     0.000
    fixed hops=2           0.507     0.013     1.000
    GATE gain=1            0.720     0.887     0.553
    GATE gain=10           0.987     1.000     0.973
    GATE gain=50           1.000     1.000     1.000
    GATE gain=200          1.000     1.000     1.000
    GATE gain=1000         1.000     1.000     1.000

**1.000 on both depths**, from a single learned vector per group, stable across
a 20× range of gain. The model answers questions whose depth it is not told,
which is the limitation decision 85 ended on.

### Two defects, each of which looked like a working mechanism

**The gate was inert.** The learned vector reached norm 0.089 against retrieval
slices of ~0.13, so the scores were ~0.01 and a two-way softmax over them is a
flat average. Measured directly: weight on hop 1 was **0.5020** for depth-1
questions and **0.5000** for depth-2 — the right direction, and 0.2% of the way
there. It still scored **0.707**, beating both fixed models, because the readout
learned to cope with a fixed blend. Same shape as the unsharpened hop decode:
a correct signal flattened into uniformity.

**The gate was scoring the wrong hop.** With gain it reached 0.773 — depth 1 at
1.000 and depth 2 at 0.547 — and that split is the diagnosis. Decision 86's
signal separates *past the end* from *on the chain*. For a depth-1 question hop
2 is the separator, so the gate can reject it. For a depth-2 question hop 1 is
`b` and hop 2 is `c`, **both on the chain, both chain symbols**, and the gate has
nothing to tell them apart by. It split them and averaged.

The rule the signal actually supports is *the last hop before the first marker*,
so **hop k is scored by what hop k+1 returns**. One extra lookahead retrieval,
same linear score, still inside a group. That is the change from 0.773 to 1.000.

### What the mutation harness caught that the tests did not

The first test pass asserted read counts, refusals, a zero-gain control and
store invariance — and **both mutations survived all of it**. Every structural
property held while the mechanism did the wrong thing.

They survived because each defect leaves a model that still beats the baseline:
0.707 and 0.773 against 0.500. **A mechanism that does nothing and still beats
the baseline is the hardest kind to notice**, and structural tests cannot see
it. `test_the_gate_solves_depths_a_fixed_hop_count_cannot` trains on mixed
depths and asserts the depth-2 half, which is where both defects give up
(0.553 and 0.547 against 1.000). Both are caught now.

### What this does NOT license

The gate is trained and tested on the **same terminator**. Decision 86 already
recorded that this task lays down its own structural markers, and the gate has
now learned this task's terminal class — not a general one. **The first real
test is a task whose terminator was never designed in.**

`hops` is still a ceiling: the gate chooses among hops 1..k and cannot choose a
depth beyond k. Nothing here tests depth 3 mixed with depth 1 — **decision 88
does** — and the lookahead means a `hops=k` gated model pays k+1 retrievals, the
cost of not knowing the depth, which is one extra hop over knowing it.

## 88. Three depths at once, and the gain has an upper edge

Decision 87 explicitly did not license three depths: the gate must pick one hop
of three, the softmax has more ways to split, and the lookahead has to reject a
marker two hops further out for the deepest questions.

    model                  overall     d1      d2      d3
    fixed hops=1             0.333   1.000   0.000   0.000
    fixed hops=2             0.339   0.017   1.000   0.000
    fixed hops=3             0.353   0.000   0.058   1.000
    GATE max=3 gain=50       0.997   1.000   1.000   0.992
    GATE max=3 gain=200      1.000   1.000   1.000   1.000
    GATE max=3 gain=1000     0.986   1.000   0.983   0.975

**It scales.** 1.000 on all three depths, against fixed counts pinned at 0.333
because each solves only its own third. Reported per depth on purpose: an
overall number would hide a gate that solved two depths and abandoned the third,
which is the exact shape the own-hop-scored gate failed in.

**The gain has an upper edge**, which two depths did not show. At 1000 the model
loses 0.986, and the loss is on the deeper questions (d2 0.983, d3 0.975) while
d1 stays perfect. A very large gain makes the hop softmax effectively an argmax,
so a single mis-scored hop is taken outright instead of being averaged against
its neighbours — and deeper questions have more hops to mis-score. So the gain
is a real dial with a middle, not a "larger is safer" knob, and 200 is where
both grids agree.

### What this licenses

The mechanism is not a two-hop special case. Depth is now a property of the
question rather than of the configuration, up to a ceiling the caller sets.

### What it still does not license

The terminator. Every result so far trains and tests on the **same** structural
marker, and with random value vectors there is nothing shared between two
different marker tokens for a linear gate to latch onto — so the honest
prediction is that it does **not** transfer, and the interesting question is
whether anything survives at all. Decision 86 measured retrieval `norm` as the
one signal with any separation (d′=1.01), and a norm is not tied to a token's
identity. That is the thread worth pulling: it is the difference between a
mechanism and a fit.

## 89. The gate is a token detector, measured — and the sign was not what I predicted

Decision 88 predicted the gate has learned this task's terminator rather than a
general notion of one. That is a claim about `halt_w`, so it is checkable by
looking at the vector instead of running a transfer experiment.

Cosine between the gate vector and each token's **value** vector:

    SEPARATOR      +0.563      +8.3 sd from the rest
    QUERY          +0.518      +7.7 sd from the rest
    every other    mean -0.068, sd 0.076, range [-0.290, +0.078]

**The gate has latched onto two specific tokens**, eight standard deviations
clear of the other forty-eight. This is no longer a suspicion about transfer: it
is a measurement of what the parameter contains. Two different marker tokens
have unrelated random value vectors, so a linear gate trained on one **cannot**
recognise the other. Transfer is impossible by construction, not merely
unlikely, and the experiment to confirm it would only restate the arithmetic.

### The sign was the opposite of what I predicted, and the mechanism is right

I expected strongly NEGATIVE — "reject anything that looks like a marker". It is
strongly positive, and positive is correct. The gate scores the **lookahead**, so
a high score on hop k means *take* hop k. For a depth-1 question hop 1 is the
answer and its lookahead is the separator, so the separator must score HIGH.

The rule the gate learned states cleanly: **take the hop whose next hop is a
marker** — the last hop before the end. That is the rule decision 87 designed
the lookahead for, arrived at from data rather than assumed, and the sign error
was in my prediction rather than in the mechanism.

### What this licenses

Nothing new about capability. It converts decision 88's caveat from a guess into
a fact, and it means **the next experiment is not a transfer test** — that
result is already determined. The open question is a different one: whether a
gate can be given a signal that is not token identity at all.

Decision 86 measured retrieval `norm` as the only candidate with any separation
(d′=1.01, past-end 0.185 against on-chain 0.132), and a norm is a property of
how a key was bound rather than of which token was stored. That is worth trying,
and it is a weak signal being asked to do a job a very strong one currently
does — so the honest expectation is that it degrades accuracy and the question
is by how much.

## 90. Composition survives churn, and the per-hop cost compounds gently

Every churn result before this was measured on **one-hop recall**. C3 is a
premise of the whole project, so the question is whether the new capability
survives it, and there was a specific reason to doubt: a depth-3 question needs
three lookups to survive where a depth-1 question needs one.

Width 64, gated, depths 1–3 mixed, dimensions zeroed **after** training — a
model that learned on a whole machine and then lost part of it, which is the
realistic order. Three seeds averaged.

    removed   depth 1   depth 2   depth 3
       0.0%     1.000     1.000     0.986
      12.5%     1.000     1.000     0.975
      25.0%     0.997     0.989     0.956
      37.5%     0.981     0.989     0.961
      50.0%     0.986     0.964     0.928
      62.5%     0.928     0.886     0.831
      75.0%     0.739     0.694     0.542

**Half the machine gone and depth-3 chains still answer at 0.928.**

The prediction was directionally right and wrong about the magnitude. Deeper
questions do degrade faster — the depth-1 to depth-3 gap widens from 0.014 at
full width to 0.197 at 75% removed — but the compounding is gentle until 62.5%
and there is no cliff where composition stops working while recall keeps going.
Relative to depth 1, depth 3 holds 0.986 at full width, 0.941 at half, and 0.733
at three-quarters removed.

### What this licenses

Composition is not a fair-weather capability that only exists on an intact
machine. C3 was measured on recall and now covers the hop mechanism too, at the
churn fractions decision 81 measured over real containers.

### What it does NOT license

**Three seeds, and no spread reported.** The ordering is consistent and the
trend is monotone in depth at every fraction, which is what the claim rests on;
individual cells at the noisy end are not worth quoting to three decimals.

Ablation is a **frozen** departure — dimensions zeroed once, after training.
Decision 81's containers measured real join and leave; this did not, and a model
that keeps learning while nodes come and go (C4 crossed with C3) is untested
for hops. **Decision 91 tests it.**

## 91. Perpetual learning does not heal churn, because churn costs capacity

Decision 90 measured survival of a frozen departure. C4 says the model never
stops learning, so the different question is whether continued learning **claws
back** what a departure cost. Half the nodes leave after 400 sequences and 800
more follow; every arm sees the same number of sequences, so a gain cannot be
"more training" rather than "better training".

    arm            depth 1   depth 2   depth 3
    intact           1.000     1.000     0.989
    frozen           0.983     0.969     0.942
    learning         0.992     0.978     0.950

    recovered        +0.008    +0.008    +0.008

**Continued learning recovers +0.008 against the ~0.047 lost at depth 3.** Close
to nothing.

The reading: a departure costs **capacity**, and capacity is not a thing
learning can rebuild. The readout was already near-optimal on the dimensions
that survived — the delta rule on `Wo` is the exact gradient for a linear
readout — so there was very little left for further training to fix. Nothing was
stale; there was simply less machine.

### Treat the +0.008 as a direction, not a number

It is **identical to three decimals at all three depths**, which is about 3
sequences out of 360 per depth. Three seeds, no spread reported. That pattern is
consistent with coincidence at a small effect size, and the claim here rests on
the effect being *small*, which does not depend on its exact value.

### What this licenses

**Do not expect C4 to pay for C3.** They are independent requirements and this
result separates them: churn tolerance has to come from capacity and redundancy,
and perpetual learning has to earn its keep somewhere else.

### What it does NOT license

Nothing about what C4 is actually for. This run holds the data distribution
**fixed**, so continued learning had nothing new to learn — it could only
re-fit what it already knew on fewer dimensions. The test that would show C4's
value is a distribution that *changes* after the departure, where a frozen model
must fall behind and a learning one need not. That is the experiment to run next,
and it is the one that speaks to "always learning as it goes" rather than to
repair. **Decision 92 runs it, and it does not come out as expected.**

## 92. The gate generalises to a depth it never trained on — zero-shot

The experiment was meant to show what C4 is for: train on depths 1 and 2, then
let depth-3 questions start arriving, and score **only** on depth 3 — the kind
the model did not have. A frozen model should fall behind and a learning one
should not.

    arm                  depth 3
    never sees it          0.992
    frozen at shift        0.992
    keeps learning         0.992
    always had it          0.992

**Every arm identical.** The experiment measures nothing about adaptation,
because a model trained only on depths 1 and 2 already answers depth-3 questions
at 0.992 without ever having seen one.

### The null is the result

The gate learned a **rule**, not a table. "Take the hop whose lookahead is a
marker" says nothing about how deep a question is, so once it is learned from
depths 1 and 2 it applies at depth 3 unchanged — and the readout is shared
across hops, so there are no depth-3-specific parameters to train. Nothing about
a depth-3 question is new to this model except the number of times it goes
round.

That is worth more than the result the experiment was designed to get: it is
direct evidence the mechanism is a mechanism rather than a fit, on the axis it
was built for.

### Read this against decision 89, because together they are precise

    over DEPTH        generalises zero-shot to a depth never trained on
    over TERMINATOR   does not generalise at all -- halt_w sits +8.3 sd on one
                      specific token's value vector

**Same gate, same vector, opposite answers.** The rule it applies is general;
the feature it applies that rule to is a memorised token. That is a sharp
description of what was built, and it says where the next work is: not in the
hop machinery, which composes and generalises, but in what makes a retrieval
recognisable as terminal.

### What it does NOT license

C4 is still untested. Two attempts have now failed to construct a case where
continued learning helps — decision 91 because a departure costs capacity rather
than currency, and this one because the mechanism already generalises. Neither
is evidence that perpetual learning is worthless; both are evidence that **this
task is too easy to need it**. A real test of C4 needs something the model
genuinely cannot already do, and finding that case is the open problem.

## 93. There is no token-agnostic terminal signal — and that points at frozen `Wv`

Decision 92 put the next work in "what makes a retrieval recognisable as
terminal". The cheap version of that question is whether any identity-free
feature carries the signal, measured **before** building a config flag, tests
and mutations for a gate that might not work.

Five features, every one a property of *how* a key was bound rather than *which*
token was stored, so any of them would transfer to an unseen terminator by
construction. Labels are "has this hop walked past the end", and the separator is
fitted **with** the labels — a ceiling, not a mechanism.

    norm      d = 0.60        BEST LINEAR SEPARATOR on all five:
    entropy   d = 0.62          accuracy 0.628
    peak      d = 0.54          against  0.500 for guessing
    gap       d = 0.63
    kurtosis  d = 0.46        the token-identity gate: 1.000

**0.628 against a 0.500 baseline, with the labels handed to it.** No gate
learning from a downstream error can beat a classifier that was given the
answers, so this closes the approach rather than discouraging it. Note this also
supersedes decision 86's hopeful reading of `norm` at d′=1.01: measured over
three depths rather than one, it is 0.60 and it is not the outlier.

### Why, and it is not a property of the task

**`Wv` is frozen and random.** Two tokens' value vectors are independent draws,
so there is no shared structure for a "class of terminators" to live in. A gate
can memorise one vector — decision 89 measured exactly that, +8.3 sd — but there
is nothing for it to generalise *over*, because in this representation
`separator` and some other marker have no more in common than any two tokens.

That is not a limitation of gating. It is a limitation of frozen random
embeddings, and it would apply to any mechanism asked to recognise a *kind* of
token rather than a specific one.

### What this licenses

**A concrete reason to unfreeze the value projection.** `value_lr` already
exists in the model and "unfreezing the value projections" is already in
BACKLOG as one of the four approved un-constraints — this gives it a purpose
sharper than "more capacity": *tokens that play the same role can only become
similar if the representation is learned*, and role-similarity is the thing the
gate needs and cannot have.

That makes a falsifiable prediction worth testing next: with `value_lr` on and
training that includes **several different terminators**, the value vectors of
those markers should move closer together than chance — and only then can a gate
trained on some of them recognise a held-out one. If they do not converge, the
delta-rule-on-values mechanism is not doing representational work and that is
worth knowing on its own.

### What it does NOT license

Five features is not every feature. This rules out the retrieval *statistics*
that were available, not every identity-free signal that could exist — a feature
computed across hops rather than within one, for instance, was not tried.

## 94. `value_lr` does not build a terminator class — and the gate learns whatever depth dominates training

Decision 93 predicted that unfreezing `Wv` and training with several
terminators would make those markers' value vectors converge, giving a gate
something to generalise over. **The prediction is refuted, and the route to
testing it is blocked twice over.** `n_separators` and `use_separators` are
added to the task so the question can be asked at all; `n_separators=1` is
pinned byte-identical by a digest test.

### First blocker, from the code rather than a measurement

`value_lr` updates `self.wv[targets[t]]` at **scored positions only**, and the
chain task scores exactly one position whose target is always a chain symbol. A
separator is never a target, so its value vector **can never move**. Decision
93's experiment is not merely hard to run — as written it is a no-op.

### Second blocker: making separators targets breaks the gate

The fix is to score every position — next-token prediction, which is also how
the model would train on real text. It costs almost everything:

    separators        scored   depth-2 accuracy
             1   answer only              1.000
             1   every position           0.117
             4   answer only              0.992
             4   every position           0.683

**Four separators cost 0.008. All-position training costs 0.883.** The
diagnosis, checked rather than assumed — weight the gate puts on hop 1 at the
answer position of a depth-2 question, where a working gate puts it near zero:

    trained on answer only      0.0102
    trained on every position   0.3034

**At almost every position the next token is exactly one hop away.** The answer
position is a rare exception competing against a large majority, so the gate
learns the dominant depth and drags hop 1 up thirtyfold.

### And `value_lr` itself does not do what was hoped

    value_lr  accuracy   sep cos  base cos  sep-base
           0     0.683     0.064    -0.015    +0.080
       0.001     0.300     0.068     0.090    -0.023
        0.01     0.058     0.126     0.191    -0.065
        0.05     0.025     0.535     0.382    +0.153

The separator-minus-baseline contrast does not rise with `value_lr`. At the
largest rate **everything** converges — ordinary symbols reach 0.382 — which is
the representation collapsing globally, not terminators forming a class, and it
matches decision 65's trained projection collapsing the rank. Accuracy falls
monotonically to 0.025 alongside it.

### What this licenses — and it is the most important thing here

**The gate is trained by the same error as the readout, so it learns the depth
that dominates the training distribution rather than the depth a question
needs.** On the answer-only objective that distribution *is* the task. On
next-token over every position it is overwhelmingly one hop.

That is a serious obstacle on the path to real data, and it was invisible while
every experiment scored one position. Real text is trained at every position, so
a gate learning by this route would settle on "one hop" and **composition would
be built, correct, and never used**. Any future result on text has to show the
gate is actually gating, not just that accuracy moved.

### What it does NOT license

That all-position training is unusable — only that it is unusable *with the gate
learning from the same undifferentiated error*. A gate with its own objective, or
one trained only where depth is ambiguous, is untried. And 4 separators beating 1
under all-position training (0.683 against 0.117) is unexplained; it is a real
gap in the account, not a detail.

## 95. The gate is not outvoted, it is CONFLICTED — and that is a mechanism problem

Decision 94 left two explanations for why all-position training breaks the gate,
and they call for different work:

- **outvoted** — the rule is right everywhere and the answer position is rare, so
  reweight the training signal.
- **conflicted** — the rule is right at the query and wrong in the body, so no
  reweighting can help and the gate needs different inputs.

Take the gate trained **answer-only**, where it reaches 1.000, and ask what it
says at ordinary body positions, where the correct next token is one hop away:

    at the QUERY position   0.0171   want LOW  -- the answer is two hops out
    at BODY positions       0.4712   want HIGH -- the next token is one hop out

**It is conflicted.** In the body the gate is essentially uninformative — 0.47,
a coin flip, where serving the body requires close to 1.0. It is not doing the
body's job at all.

So under all-position training the body supplies the overwhelming majority of
the error, pulls the shared vector toward hop 1, and wrecks the query behaviour
that worked. That is exactly the 0.0102 → 0.3034 shift decision 94 measured, and
it is not a sampling problem.

### Why one gate cannot do both

The gate is a linear score on the **lookahead retrieval** and nothing else. At a
body position and at a query position that lookahead can look the same, while
the right answer differs — hop 1 in the body, hop 2 at the query. A function of
the lookahead alone cannot separate cases it cannot see apart.

**The missing input is where the model is, not what it retrieved.** The query
marker sits in the input at the query position, so the information exists; the
gate simply has no access to it.

### What this licenses

A specific, small mechanism change to try next: **give the gate the current
position's key alongside the lookahead retrieval**, so it can learn "at a query,
use the marker rule; otherwise take one hop". That is one more vector per group,
the same locality, and it is the smallest change that could resolve a conflict
this measurement says is real.

It is worth being clear that this is now a *design* claim and not yet a result.
The measurement establishes the conflict; it does not establish that the extra
input fixes it.

### What it does NOT license

The body number is *uninformative* (0.47), not *confidently wrong* (near 0). The
gate is failing to serve the body rather than actively fighting it, which is a
weaker statement than "the rule inverts" — and the distinction matters, because
a uninformative gate degrades gracefully while an inverted one would not.

## 96. Letting the gate see WHERE it is triples all-position accuracy — and is not enough

Decision 95's proposal, built as `gate_reads_key`. **The proposal as literally
written would not have worked**, and that is worth stating: "give the gate the
current key" as an added term is identical across hops at a position, so the
softmax removes it exactly — the same trap that made a constant perturbation
invisible to the decode. The key has to **modulate** the rule, not contribute to
the score. So it selects between two rules, blended by a scalar from that
group's own slice of the key. Two vectors per group, both zero-initialised, so
the model begins as exactly the one-rule gate.

    depth-2 accuracy      one rule   reads key
       answer only           1.000       1.000
       every position        0.117       0.400

**3.4× on all-position training, and the control holds** — answer-only stays at
1.000, so the extra machinery does no harm where the old gate already worked.

### But a single budget hides the real finding

Quoting only that row would have been misleading. Across training budgets:

    per depth  epochs   one rule   reads key      gap
          100       1      0.750       0.833   +0.083
          200       1      0.683       0.717   +0.033
          400       1      0.250       0.683   +0.433
          400       2      0.100       0.383   +0.283

**Accuracy falls as training proceeds, in both arms.** The one-rule gate goes
0.750 → 0.100 and the selector 0.833 → 0.383. Under all-position training the
model does not fail to learn composition — it **progressively unlearns it**, as
the body's error accumulates and drags the shared gate toward one hop.

So the selector **slows the decay rather than preventing it**, and the gap is
not stable either (+0.083, +0.033, +0.433, +0.283). The honest headline is the
decay, not the 3.4×.

### The gain is the intended mechanism, not the extra parameters

Two vectors per group is more capacity, so the gain could have come from
anywhere. The design says the key should make the gate behave *differently* at a
query than in the body. Weight on hop 1, after all-position training:

    gate        at query   in body   separation
    one rule      0.7491    0.5081      -0.2411
    reads key     0.3761    0.4945      +0.1184

**The one-rule gate separates them backwards** — more hop-1 weight at the query,
where the answer is two hops out, than in the body, where it is one. That is the
conflict of decision 95 shown from the other side. The selector **flips the sign**.

### What this does NOT license

**It is a delay, not a fix.** The body sits at 0.4945 where serving the body
wants ~1.0, and the query at 0.3761 where it wants ~0. The separation is correct
in sign and weak in magnitude, and accuracy still decays with training. Something
else is binding and this measurement does not say what.

Nor does it license the conclusion that the remaining gap is more of the same. A
gate strong enough to fix the sign but not the magnitude may be limited by the
selector being a single scalar per group, by the two rules being too few, or by
something outside the gate entirely — all untested.

**The decay is the thing to chase next**, and it is a sharper target than
"all-position is worse". A mechanism that is learned and then unlearned is
usually a mechanism whose gradient is being outvoted at a rate that grows with
exposure — so the next question is whether the gate's own error can be
decoupled from the readout's, rather than whether the gate needs more inputs.
**Decision 97 tests it. The answer is that the decay is real.**

## 97. Density raises the level and does NOT remove the decay — and the first run of this was leaking

Decision 96 measured composition being learned and then progressively unlearned
under next-token training — 0.750 falling to 0.100. The obvious suspect was
density: with one question per sequence, about 1 position in 50 needs
composition and every other needs a single hop, so ~98% of the error says
"always take one hop". **Real text is not that lopsided.** `n_queries` raises
the share of positions where the next token is genuinely several hops away.

### The first run of this leaked, and the guard was written after it

`n_queries` was added, the experiment run, and *then* the tests written. One of
them failed, and it was right to: **a query block writes `a` next to `c`, so it
STATES the link `a -> c`.** With one question that is harmless — the block is
last and the answer is read before the binding is written. With several, an
early block stated the answer to a chain a later block asked about, making that
question a **one-hop lookup of a link already in the store**.

The leak grew along exactly the axis being measured: more questions meant more
repeated chains meant more free answers. It produced a clean, plausible,
completely wrong curve — a decay collapsing from +0.517 to +0.029 — which was
reported in-session before the guard caught it. **Those numbers are discarded.**

Fixed by sampling asked chains **without replacement**, which caps `n_queries`
at `n_chains`. Guarded by `test_no_answer_is_stated_before_its_own_question`,
which checks the precise property — a chain's answer never appears before the
question that needs it — rather than the over-broad one the first version
checked, which flagged each block's own `(a, c)` and would have condemned the
task itself.

### The corrected measurement

Eight chains, so the floor is 0.125.

    queries     100x1     200x1     400x1     400x2     decay
          1     0.150     0.033     0.017     0.033    +0.117
          4     0.567     0.392     0.375     0.379    +0.188
          8     0.515     0.333     0.290     0.346    +0.169

**Density does not remove the decay.** +0.117, +0.188, +0.169 — no trend, and
the smallest value belongs to the row that has already collapsed to the floor
and has nothing left to lose.

**What density does is raise the level.** One question per sequence falls to
0.033, *below* the 0.125 floor, which means confidently wrong rather than
guessing. Four or eight stabilise around 0.35–0.38, well clear of it. That is a
real effect and a useful one — it is simply not the effect claimed.

### What this licenses

**Decision 96's proposed next step stands.** The decay is a property of the
mechanism and not of the instrument's uniformity, so decoupling the gate's error
from the readout's is back on the table as the thing to try.

And density is worth keeping regardless: it is the difference between a model
that is confidently wrong and one that is meaningfully above the floor under the
training regime real text requires.

### What it does NOT license

Levels are not comparable to any earlier chain result: `n_chains` is 8 here, so
the floor is 0.125 rather than 0.250, and every question in the sequence is
scored rather than one. **Only the within-row decay and the across-row level
ordering are measurements here.**

`n_queries=1` is pinned byte-identical by the same digest test as
`n_separators`, so every earlier chain number still reproduces.

## 98. Giving the gate its own objective removes the decay

Decisions 96 and 97 ruled out more inputs and more density. What was left was
the objective itself: the gate learns from the readout's error carried back
through the mixture, so **conflicting demands get averaged**. In the body the
error says "take hop 1", at a query it says "take a later one", and one shared
vector pulled by both drifts toward whichever supplies more gradient.

`which_hop` asks a question with the **same answer in both places** — *which hop
would have been right here?* At a scored position that label is locally
available: each hop's own readout either names the target or does not, decidable
from what the group already holds. The body then stops outvoting the query and
merely supplies more examples of one class, which a classifier handles.

    mixture                                which_hop
    queries  100x1 200x1 400x1 400x2 decay  100x1 200x1 400x1 400x2  decay
          1  0.150 0.033 0.017 0.033 +.117  0.233 0.500 0.383 0.600  -0.367
          4  0.567 0.392 0.375 0.379 +.188  0.571 0.475 0.550 0.517  +0.054
          8  0.515 0.333 0.290 0.346 +.169  0.404 0.406 0.412 0.404  +0.000

**The decay is gone**, and the objective is better on both axes — no decay *and*
a higher level at every density. At density 8 the trajectory is flat to three
decimals; at density 1 accuracy now *rises* with training where the mixture
objective collapsed it to 0.033, below the 0.125 floor.

### It also undoes decision 97's reading

With a working objective, **one question per sequence is the best row, not the
worst**. Density was compensating for a broken objective rather than fixing a
property of the task — which is worth stating plainly, because decision 97
recommended keeping the density and that recommendation is now weaker.

### What this does NOT license

**The claim rests on the flat row, not the dramatic one.** Density 1 scores 60
questions per evaluation and is visibly noisy — 0.233, 0.500, 0.383, 0.600 is
not monotone, and the −0.367 "improvement" is mostly that noise. Density 8
scores 480 and is flat. Quote the flat row.

Nor is ~0.40 good. It clears the 0.125 floor comfortably and no longer rots, but
answer-only training still reaches 1.000. **The gap between a marked question
and an unmarked stream is still most of the problem**, and this decision only
shows that the gap stops widening.

## 99. A typed-relation task, three defects found by reading it, and a floor of 0.546

`openplexus/tasks/kinship.py`, modelled on
[CLUTRR](https://arxiv.org/abs/1908.06177). **Not CLUTRR** — its rules, not its
dataset, and a number here is not a CLUTRR score.

### Why a second task at all

`chains.py` is **pure transitive chaining**: `a -> b -> c` means the answer is
`c`, and following an edge is the whole operation. Decision 92's zero-shot depth
generalisation is real but it is generalisation over *how many times to repeat
one operation*.

Kinship is not that. `mother` of `brother` is `mother`; `mother` of `mother` is
`grandmother`. **Composition is a lookup in a table the model must learn**, so
two paths of equal length compose to different relations. A model can be perfect
at "follow the arrow k times" and have no way to represent that.

### Three defects, all found by generating sequences and reading them

The habit that caught every chain-task defect, applied before any test existed.

1. **A distractor stated the asked pair directly in 7.0% of sequences.** One in
   three hundred handed over the answer; the rest **contradicted** it, making
   the task inconsistent rather than merely easier.
2. **Three-hop paths could not be generated.** Only 24 of 256 relation pairs
   compose, so rejection sampling raised "no 3-hop path composes" on an ordinary
   seed — the generator failing, not the depth being impossible. Paths are
   constructed by walking the table now.
3. **The floor was wrong.** 1/16 = 0.062 was assumed; the majority-class
   strategy actually scores 0.080, 0.108, 0.150 at one, two and three hops,
   because composition contracts the answer space (16 → 12 → 8 reachable
   relations).

### G0: the QUESTION ORDER decides whether the task is addressable at all

    hops 1 (floor 0.090)        hops 2 (floor 0.130)
      object last    0.020        object last    0.027
      subject last   0.700        subject last   0.407

**0.020 against 0.700 on the same task.** This store binds adjacent pairs, so
the retrieval key at the scored position is whichever person the question block
ends with. End it with the object and the model is keyed on the wrong token and
cannot address the task at all. That is a free choice of the task presenting
itself as a model failure, and measuring only one order would have recorded it
as one.

**G0 passes at one hop**: 0.700 against 0.090.

### But two hops demonstrates NO composition, and the reason is my rule table

    majority floor (no information)      0.130
    best guess from the FIRST relation   0.546
    a one-hop model actually scored      0.407
    distinct answers per first relation  2, 2, 2, 2, 2, 2

**Every first relation admits exactly two answers.** `mother` of anything in
this table is `grandmother` or `mother`. So the second relation barely matters,
the prefix nearly determines the answer, and 0.407 sits *below* what guessing
from the prefix is worth — the one-hop model's score is fully explained by a
shortcut and shows no composition whatever.

That is a property of `COMPOSE` being small and regular (16 relations, 24
rules); CLUTRR's larger inventory weakens the same shortcut.

### What this licenses

**The floor for any composition claim on this task is `shortcut_floor` —
0.546 — not `majority_floor`.** Raising the floor is the honest response to a
leak that cannot be cheaply removed, and the three floors are asserted in strict
order so the weak one cannot be quoted by accident. g8-01's seq-1536 row was
withdrawn for exactly that mistake.

### What it does NOT license

Any statement about whether this model can compose typed relations. **That has
not been measured** — 0.407 is below the floor that matters. The instrument
exists and is honest about its own ceiling; the experiment comes next.

Enriching the rule table would raise the ceiling and is the obvious improvement,
but inventing kinship rules risks encoding ones that are wrong, which is a worse
failure than a stated-and-bounded shortcut.

## 100. The published rules exist, and using their STRUCTURE fixes decision 99's leak

John asked whether a pre-published version of the benchmark should be used
instead of a hand-made one. It should, and the answer changes the task.

CLUTRR's rules are public (`rules_store.yaml`, facebookresearch/clutrr). Reading
them named my defect immediately: **CLUTRR's relations are gender-free** —
`child`, `SO`, `sibling`, `grand`, `un`, `in-law`, each with an inverse — and
gender is applied later, at language realisation.

Decision 99's table baked gender into the relation names. `mother` and `father`
compose **identically**, so sixteen gendered relations carried no more
compositional structure than eight, every prefix had exactly two reachable
answers, and guessing from the prefix was worth 0.546. Gender was multiplying
the inventory while contributing nothing to composition.

**Their table is also deliberately partial**, with a commented-out rule and the
reasoning attached: `grand` then `inv-child` is not `child`, because the person
reached could be an in-law. That is the same argument decision 99 made
independently, which is worth recording — a partial table is the considered
position and not an unfinished one.

### On licensing, because it is John's call and not mine

CLUTRR is **CC BY-NC 4.0 — non-commercial only**. Fine for research and a
problem if Open Plexus ever has a commercial dimension. So the rules were
**not** vendored: kinship composition facts are not copyrightable, the valuable
part is the structural insight, and the table here is written independently with
CLUTRR cited as the design source. Using their generator for
published-comparable numbers is a separate decision that would accept the NC
term.

### The second defect: sampling, not rules

Gender-free relations alone made things *worse* in aggregate — the majority
floor rose to 0.433 at three hops and a **suffix** shortcut appeared at 0.708 —
because walking the table takes whatever answer falls out and the reachable
answers concentrate hard.

Fixed by **sampling the answer uniformly** and then a path that reaches it,
which makes the majority floor `1/(reachable answers)` by construction.

    hops   reachable  majority  first   last    ends
       1          10     0.109  1.000  1.000   1.000
       2           9     0.116  0.465  0.559   1.000
       3           8     0.133  0.261  0.549   0.724
       4           8     0.133  0.223  0.550   0.629

### And a framing error of mine, which `ends` exposed

`ends` is 1.000 at two hops. That is not a leak — **at two hops the path IS its
two ends**, so the number is a tautology.

More importantly, **the path is not observable**. The model sees facts and two
people; learning any relation of the path requires searching the graph. So only
two of those columns are *floors*:

- `majority` needs nothing.
- `first` is reachable: retrieving the relation stated for the queried subject
  gives `path[0]` directly, which is exactly what a one-hop model does.
- `last` and `ends` require reaching the far end, **which is the work the task
  is asking for**.

Treating all four as floors — which the first version did — would have set an
impossible bar and made an honest result look like a failure. `ends` remains
reported as an information bound: 0.724 at three hops and 0.629 at four, so the
middle of the path carries real information and the depth axis is worth having.

### G0 on the rebuilt task

    hops 1 (majority floor 0.120)    object last 0.033   subject last 0.713
    hops 2 (floor to beat 0.465)     object last 0.060   subject last 0.227

**G0 passes** — one hop clears its floor 5.9×, so the architecture can address
this task's shape. Two hops sits at 0.227, *below* the 0.465 a non-composing
model could reach, against a ceiling of 1.000.

### What this licenses

A valid instrument with the bar stated **before** the experiment rather than
after: floor 0.465, ceiling 1.000, positive control 0.713. The hop mechanism and
gate have not been run on it — the G0 probe is a one-hop model and cannot
compose by construction. That run is the next thing.

### What it does NOT license

Decision 99's numbers. The 0.546 shortcut, the 0.407, and the three floors there
were all measured on the gendered table and are **superseded**, not refined.

## 101. The hop mechanism REPLACES retrievals, it does not COMBINE them

The bar was set in decision 100 before the run: floor 0.470, control 0.713,
ceiling 1.000. The result:

    task hops 2   floor to beat 0.470
      model hops 1                0.347
      model hops 2                0.027    <- not even a relation, 79%
      model hops 2 + gate         0.187

    task hops 3   floor to beat 0.282
      model hops 1                0.120
      model hops 3                0.047
      model hops 3 + gate         0.093

**Turning hops on makes it thirteen times worse**, and the gate recovers only
part of that. Nothing here clears the floor.

### Where the hops actually go

A fact is laid down as `[subject, relation, object]`, so from the subject:

    hop 1: RELATION (on the path) 48%
    hop 2: PERSON 90%
    hop 3: PERSON 61%

Hop 1 is right. **Hop 2 lands on a person** — it re-encodes the relation it just
decoded and retrieves what follows *that*, which is the object of the same fact.
The mechanism walks **along a fact**, not **across the graph** to the next one.

### The deeper reason, which the traversal bug was hiding

Fixing the traversal would not fix this. Composing `R1` with `R2` requires
**holding both and applying a binary function**. But each hop *replaces*
`retrieved`, and the readout maps **one** retrieval to an answer. There is
nowhere for `R1` to be while `R2` is fetched.

So the mechanism does **sequential retrieval**, not composition:

    replace   follow a pointer, keep only where you land   -- chains
    combine   hold two things and apply a rule to them     -- kinship

### What this does to decision 92

It narrows it rather than contradicting it. Zero-shot generalisation to unseen
depth is real, and it is generalisation over **how many times to repeat one
replace**. On chains, token adjacency *is* the relation graph, so replacing is
sufficient and the task reached 1.000. That result stands with its scope
corrected: **the hop mechanism composes pointers, not relations.**

Worth saying plainly that this is the outcome kinship was built to expose, and
it exposed it on the first run — which is what a second task is for. A model
perfect at "follow the arrow k times" with no way to represent "these two
relation types combine into a third" is exactly the gap decision 99 predicted.

### What this licenses

A named next mechanism: **carry state across hops instead of overwriting it.**
Something that accumulates — the retrievals so far, or a running composed value
— and a readout that consumes it. That is a bigger change than the gate and it
is the first thing on this project's path that requires holding two things at
once.

### What it does NOT license

Concluding that this architecture *cannot* compose typed relations. What is
measured is that **this hop mechanism** does not, and the reason is structural
and identified. An accumulator has not been tried.

Nor is the traversal problem separately settled: even a mechanism that combines
would still need to reach the second fact, and `key(relation)` is superposed
across every fact sharing that relation. Two problems, not one.

## 102. The accumulator is built, my reason for choosing it was wrong, and traversal is now the only blocker

Decision 101 named the missing mechanism: carry state across hops instead of
overwriting it. `hop_accumulate` does that, with `replace` the default so every
earlier number is unchanged and the golden values are bit-identical.

### I picked the wrong combiner, for a reason that does not hold

The argument was that concatenation cannot work — a linear readout over
`[r1, r2]` learns only `f(r1) + g(r2)`, and composition is not additive, since
`child` then `sibling` is `child` while `child` then `SO` is `in-law`. So an
elementwise product was chosen to carry the interaction.

Fitting a linear map from a combined pair to the answer, over the entire rule
table, with no model or store involved:

    product   0.812      concat   1.000      convolve   0.812

**Concatenation is perfect and the multiplicative bindings lose information.**
The argument confused a functional form with a classification problem: sixteen
rules in a 128-wide space are linearly separable whatever structure the labels
have, and a product of two random vectors does not keep its operands
recoverable. `bind` is kept as the measured alternative rather than deleted.

Whether concat still wins with far more than sixteen rules is a scale question
and is **not** settled by this.

### On the task

    task hops 2   floor 0.470        task hops 3   floor 0.282
      hops 1 replace     0.347         hops 1 replace     0.120
      hops 2 replace     0.027         hops 3 replace     0.047
      hops 2 bind        0.067         hops 3 bind        0.060
      hops 2 concat      0.347         hops 3 concat      0.180

**Concat exactly matches the one-hop model at two hops** — 0.347 to three
decimals — and that is not luck. Hop 2 retrieves a *person*, which carries no
information about the second relation, so the readout learns to ignore those
columns and the model reduces to its one-hop self. At three hops concat does
beat one hop (0.180 against 0.120), so there is some signal deeper in.

So the accumulator now does its job and no longer *harms* the way `replace` and
`bind` did. **What it holds is still the wrong second thing.**

### What this licenses

Traversal is the single remaining blocker and its cause is identified. To reach
the second fact the model needs `M`, the middle person — which lives in fact
`[S, R1, M]` — and then `key(M) -> R2`. The obstacle is that `key(R1)` is
superposed across every fact sharing that relation, so following it retrieves an
average of every such object.

`context_keys` already binds `(previous, token)` pairs, which would make
`key(S, R1) -> M` a distinct binding. Whether a hop can *construct* that pair
key is the next design question.

### What it does NOT license

Any claim that concat helps on tasks the traversal already serves. On chains it
would be extra parameters with nothing new to see, and it is refused alongside
the gate and the hidden layer rather than silently composed with them.

The near-miss worth recording: under `bind` the accumulator and the newest
retrieval differ, and the decode must read the **newest**. Decoding the
accumulator asks what token `R1`-and-`R2`-together names, which is nothing —
and because the two are the same vector under `replace`, every default result
and every structural test would have passed anyway.
`a-hop-decodes-from-the-accumulator` is the mutation, verified caught.

## 103. The store cannot hold an entity that appears in two facts

Traversal was supposed to be the last blocker. An oracle says otherwise, and
what it says is more fundamental than traversal.

### The oracle, and the number that gave it away

Hop 2 was handed the correct second relation and nothing else changed:

    accumulate    real hop 2   ORACLE hop 2
    replace            0.027          0.560
    concat             0.347          0.560

**Identical.** If concat were using hop 1, holding both `R1` and `R2` should
reach about 1.000 — that is what fitting a linear map over the whole rule table
scores. 0.560 is instead exactly the `last`-relation information bound (0.559
in decision 100). The readout is getting **nothing** from hop 1.

### Why, and it is not about hops at all

Hop 1 finds the queried subject's own relation, split by how many facts that
person appears in anywhere:

    appearances   sequences   hop 1 correct
              1         146          0.959
              2         145          0.366
              3          81          0.321
              4          23          0.348

**One appearance is near perfect. Two collapses it.**

`key(person)` accumulates one binding per appearance and a retrieval returns
their **sum**. A person who is the subject of one fact and the object of another
has both bindings on the same key, and the store hands back a superposition of
"the relation I am the subject of" and "whatever followed my other mention".

### This is not a defect in the task

It is what relational data *is*. Every knowledge graph has entities in many
relations; an entity in exactly one is a degenerate case. Decision 84 hit the
same wall on chains and the fix there was to make every symbol appear **once**,
by laying chains out contiguously — which worked only because a chain is a path.
**A graph cannot be laid out that way**, and there is nothing to redesign.

### What this does to decisions 101 and 102

It puts them downstream of something more basic. Composition needs two
retrievals held together (101) and reached correctly (102), and **both assume
the individual retrievals are right**. At two appearances they are right about a
third of the time. Fixing traversal on top of a store that cannot answer a
single-fact question would not have produced a working model, and would have
looked like the mechanism failing.

### What this licenses

**Pair keys are no longer an optimisation, they are the blocker.**
`context_keys` already binds `(previous, token)` rather than `token`, which
makes `key(S, R1)` distinct from `key(X, S)` and gives an entity one key per
role rather than one key total. That is the mechanism to measure next, and the
prediction is specific and falsifiable: **hop-1 accuracy at two-or-more
appearances should rise toward the 0.959 that one appearance already reaches.**

It also raises a question about every earlier result on chains, where the
contiguous layout guaranteed one appearance per symbol. Those numbers were
measured in the degenerate case, and how much of decision 92's 1.000 survives an
entity appearing twice is **not known**.

### What it does NOT license

Concluding the architecture cannot do relational work. What is measured is that
**single-token keys** cannot, and the reason is arithmetic rather than
mysterious. `context_keys` exists and is untried on this.

## 104. Pair keys largely fix it, and a scale register now exists

Decision 103's prediction, written before the run: pair keys should raise
hop-1 accuracy at two-or-more appearances toward the 0.959 that one appearance
already reaches.

`context_keys` binds `(previous, token)`, which is only usable if what precedes
a fact's subject is predictable — otherwise the question cannot reconstruct the
key. So the task now writes a **fact marker** before every fact, and the
question ends `FACT subject`. `key(FACT, S)` is then "S in **subject** role",
distinct from `key(R, S)`, which is "S in object role" — exactly the two
bindings that were colliding.

    appearances   sequences   single key   PAIR key
              1         146        0.884       0.918
              2         145        0.303       0.628
              3          81        0.198       0.568
              4          23        0.087       0.565
        overall                    0.480       0.710

**The collapse largely goes.** 2.1× at two appearances, **6.5× at four**, and
the curve flattens instead of falling off a cliff.

### Confirmed, but not to the predicted level, and the residual has a cause

The prediction said "toward 0.959". It reaches ~0.57–0.63, not ~0.92.

Pair keys separate an entity's **roles**. They do nothing for an entity that
appears twice in the **same** role — a person who is the subject of two facts
puts two bindings back on `key(FACT, S)`, and the store sums them again. That
part is genuine ambiguity in the question rather than a limitation of the store:
"what relation does S hold" has two answers, and only the path says which.

So the mechanism does what it was predicted to do, on the collision it was aimed
at, and a second collision remains that it was never going to address.

### Numbers here are not comparable to decision 103's

The task changed to make pair keys usable — a marker before every fact, and a
longer sequence. Single-key accuracy at one appearance reads 0.884 here against
0.959 there for that reason. **Only the within-run comparison is a measurement.**

### And a scale register, which John asked for

[`docs/SCALE.md`](docs/SCALE.md) records every choice known to depend on the
size it was measured at: what was chosen, at what scale, what would trigger
revisiting it, and what to try instead. Six rows to start — the readout's
pooling, dimensions per node, how a hop combines retrievals, single versus pair
keys, gate sharpness, and store capacity.

The rule in CLAUDE.md is that a row is added **when the choice is made**, and
that the trigger also lives in the config docstring, since that is where someone
reading the code will be. `hop_accumulate="concat"` is the motivating case: it
beat a true binding 1.000 to 0.812, but only because sixteen rules in a
128-wide space are linearly separable whatever the labels do — a property of
having few rules, and nothing in the result says so.

### What this does NOT license

An end-to-end number. This measures **hop 1 in isolation** — whether the store
can answer a single-fact question about a repeated entity. Composition on top of
it has not been re-run, and decisions 101 and 102 were both measured on the
single-key task where hop 1 was right under half the time.

## 105. Hops and pair keys do not compose, and the combination produced numbers anyway

Re-running decisions 101 and 102 on pair keys was supposed to say whether their
conclusions survive a reliable hop 1. It says something else first.

    task hops 2, floor 0.470      single key   PAIR key
      hops 1 replace                   0.280       0.413
      hops 2 replace                   0.080       0.040
      hops 2 concat                    0.327       0.413

    task hops 3, floor 0.282
      hops 1 replace                   0.147       0.100
      hops 3 concat                    0.180       0.120

Pair keys improve hop 1 as decision 104 said. But concat again **exactly**
matches the one-hop model (0.413), and the three-hop numbers get *worse* — which
is the tell.

### The two key spaces are orthogonal

A hop re-encodes its decoded token through `Wk`, a **single-token** table.
`context_keys` derives the store's keys from `(previous, token)` **pairs**.
Measured cosine between `context_key(5, 7)` and `wk[7]`:

    -0.069

So with both on, **every hop after the first queries a key space nothing was
ever written to.** It gets noise back, and the model still returns answers,
still trains, and still reports accuracies. Nothing errors.

**The multi-hop `PAIR key` column above is therefore meaningless**, and reading
those numbers as "worse" was wrong — they are not measurements. The hop-1 rows
stand.

### Refused rather than left available

`hops > 1` with `context_keys` now raises. A hop that constructs a **pair** key
is the mechanism this needs and it does not exist; until it does, the
combination is a configuration that produces plausible output without meaning,
which is the failure class this project exists to catch.
`hops-are-allowed-to-use-pair-keys` is the mutation, verified caught, and
`test_the_two_key_spaces_really_are_unrelated` records the measurement the guard
rests on so it can be relaxed if that ever changes.

### So decisions 101 and 102 stand, and the question they were re-run to answer is still open

Whether the accumulator works given a reliable hop 1 **cannot be answered
yet** — the only way to get a reliable hop 1 is pair keys, and hops cannot use
them. The two fixes are individually correct and mutually unusable.

### What this licenses

The next mechanism is now forced and narrow: **a hop must re-encode into the
store's own key space.** With pair keys that means constructing
`context_key(marker, decoded)` rather than `wk[decoded]` — the decoded token is
already in hand, and what it must be paired with is the question. Hardcoding the
task's fact marker would work and would be task knowledge in the model; learning
which context to pair with is the honest version and is a real design problem.

### What it does NOT license

Any claim about which of the two fixes matters more. They have never run
together, so their interaction is unmeasured — and the one number that looked
like an interaction (three hops getting worse) was noise from an unwritten key
space.

## 106. Composition degrades under repeated entities, gracefully, and 1.000 was the degenerate case

Decision 103 raised a doubt over every chain result: contiguous disjoint chains
give each symbol **exactly one appearance**, which is the one case the store
handles well. `linked_chains` joins chains end-to-start, so the shared symbol is
a target in one and a source in the next, while the answer stays determined —
stressing the store rather than the task.

    task  model  gate   linked 0   linked 2   linked 4
       1      1     -      1.000      0.950      0.975
       2      1     -      0.000      0.000      0.000
       2      2     -      0.995      0.815      0.630
       2      2   yes      0.970      0.790      0.610
       3      3   yes      0.955      0.775      0.645

**The doubt was justified.** Composition falls from 0.995 to **0.630** with four
of six chains linked — so decision 92's 1.000 is the number for a layout that
guaranteed away the store's hardest case, and it should not be quoted as the
model's composition ability without that condition attached.

**But it degrades rather than collapsing.** 0.630 is still 3.8× the 0.167 floor,
and the negative control holds at every link level: a one-hop model stays at
**0.000** on the two-hop task, so composition is still required and still
happening.

### Why chains survive what kinship does not

Single-hop retrieval barely moves — 1.000 to 0.975 — against kinship's cliff
from 0.884 to 0.303. Two reasons, and both are properties of the data rather
than of the model:

- On a chain a repeated symbol has one binding to its **successor** and one to
  the **separator**. A marker is easy to tell from a symbol. In kinship both
  bindings are meaningful tokens.
- A linked chain symbol appears **twice**. A kinship entity appears up to five
  times, and decision 103's curve is steepest over exactly that range.

So the two results agree: the store degrades with the number of bindings on a
key and with how confusable they are. Chains are the mild end of that and
kinship the harsh end.

### What this licenses

Quoting composition results **with the repetition rate attached**, the way
window results are quoted with the run length after decision 82. "1.000" is
true of disjoint chains; "0.630" is true at four joins in six; neither is *the*
number on its own.

It also weakens the case for treating the pair-key work as urgent for chains —
the mechanism there is not what is failing.

### What it does NOT license

Any claim about churn or depth generalisation under repetition. Decisions 90 to
92 were all measured on disjoint chains and **none has been re-run linked**.
This says composition survives; it says nothing about whether zero-shot depth
transfer or the 0.928-at-half-the-machine result do.
