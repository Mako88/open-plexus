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
