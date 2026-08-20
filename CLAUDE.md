# Working in this repo

Read `docs/plan.md` first. It is the only doc and it holds nothing finished; findings live in
the commit that produced them and in the test that asserts them, never here and never there.
Its `THE ORDER` section is what to work on and in what order, and it is the only list that
says so. A `NOW` leaf is the mechanism decided for one requirement; every other leaf is an
idea nobody has run. What a built thing DOES is in the code and is never written down twice.

## How a session runs

John's standing instruction, written down so a new session starts itself. Orient, read the
handoff if there is one, then do all of the following without asking.

**Arm a five-minute `Monitor` and keep it armed.** Not `/loop`, not `schedule`, not a cron —
those have misfired here and the Monitor tick has not. It is a heartbeat rather than a
watcher: each tick is permission to carry on, so `persistent: true` around a sleep loop is the
shape wanted.

```bash
i=0; while true; do i=$((i+1)); echo "tick $i — next step or stop"; sleep 300; done
```

**Then work, and take forks yourself.** Where two routes are open, take the one likelier to
pay; if it does not pay, revert it and take the other. Do not stop to ask which.

**Stop the monitor when any of these is true** — do not let it tick on:

- there is no obvious next step, or the next one genuinely needs John;
- you are truly blocked;
- context is filling and it is time to write the handoff.

Stopping is a normal ending rather than a failure. Strike from `THE ORDER` whatever got done,
compact the rest of `docs/plan.md`, and leave the handoff in the last commit message and in
the final reply. The handoff says where the branch is and what went wrong; it does not carry a
second copy of the ordering, because that is what `THE ORDER` is for.

Everything below still applies while the monitor runs: the guards every commit, an arm only
living while it is compared, and no dead code left behind.

## The suite, and why pushing is free

**Push whenever, and do not hold a commit back waiting for CI.** `tests.yml` runs on every
push and its concurrency group is deliberate. A run in flight is never killed, a newly queued
run waits behind it, and a third arrival cancels the one still *waiting* rather than the one
working. Intermediate commits are skipped without finished work being thrown away, and
whatever is newest when the queue clears is what gets tested.

**Put `[checkpoint]` in the commit message when a specific commit must clear.** That makes the
concurrency group unique to the SHA, so it queues behind nothing, cancels nothing, and nothing
cancels it. Use it before shipping a default, or on a state worth returning to.

**Run the structural guards locally every commit, as their own command.** `DocsTests`,
`DeadCodeTests`, `DuplicationTests`, `DialTests`, `SeparationTests`, `ShapeTests`, `FlagTests`,
`SweepListTests`, `ShardTests`, `CheckingTests`, `RemindingTests` and `ProseTests` take seconds
and go red for changes that look unrelated. Never chain the check into the commit — that has
produced red commits more than once. Rebuild before running with `--no-build`, or the binary
under test is the one from before the edit.

Three of them are worth knowing by what they catch:

- `ShardTests` fails when a test class lands in two CI shards or in none. A class in none is
  green forever, because nothing ever asked it.
- `CheckingTests` fails when a `[Fact]` prints a row and cannot fail — a measurement wearing a
  test's clothes, which runs on every push and checks nothing.
- `ProseTests` ratchets how much of the prose here is shouted. See *How to write here*.

**`OutstandingTests` is red on purpose and it is the top priority** — John's, 2026-08-13, and
`THE ORDER` says so at the top rather than in a phase list of its own. The
outstanding work is written as tests that fail until it is done, so a session cannot reach
green without doing it. Do not delete them, do not weaken them, and do not read them as a
regression. Each computes the state rather than asserting a constant, so none can be satisfied
by editing that file; an entry closes when the work closes.

**The red set is named, so anything else red is yours.** Check the failures are exactly those
before assuming a run is clean — a stable red set is the only kind you can read a new failure
against. Adding an entry is stricter than adding anywhere else: it must be work somebody has
decided to do, computable without judgement, and closeable. An open question goes in the plan
as `OPEN`.

**`PushbackTests` is green and prints**, and it is the other half of the same idea. This file
asks for pushback the moment it is seen, and a disagreement stated in a reply is gone by the
next context window. Each entry is a standing objection to something the repo currently does,
with what would settle it either way. An entry leaves by being settled rather than by being
dropped, which is why the count is asserted.

**Put `kind!=sweep&` in front of every local filter, always.** CI does this and a hand-typed
filter does not, so a filter naming a class runs that class's grids as well. That is how
`WideningTests` and `NarrowingTests` once ran past forty minutes and had to be killed; the same
two suites take 17 seconds with the exclusion. Nothing warns you, because the facts are tagged
correctly and it is the command that is wrong.

```bash
dotnet test --no-build --filter "kind!=sweep&FullyQualifiedName~Whatever" -v q --nologo
```

**And the catch-all shard is not the suite.** Its filter excludes every named shard, so a
local run of it can read green while the fleet shard is red — which has happened, on a count
that had stopped moving rather than on anything that threw. Reproducing CI locally means
running the shard list, and the cheaper move is to push and read CI.

## The measurements

Sweeps are excluded from the suite by the `kind=sweep` trait and dispatched by hand:

```bash
gh workflow run sweeps.yml --ref <branch> -f only=<Class>
```

**One entry a runner**, and an entry may be a class or one method of one. A class holding
several independent grids is several runners' work sitting on one, so split it into method
entries in the list inside `sweeps.yml`. A dispatch naming the class still matches every method
of it by substring. This is not parallelising a measurement: each runner is still one serial
run at a serial run's load, which is what `Parallelism.cs` protects.

**Dispatch several sweeps at once.** The concurrency group carries the input, so different
measurements on one ref run side by side rather than queueing.

**Adding a sweep is a line in `sweeps.yml`.** The list is named rather than discovered, on
purpose. A dispatch matching nothing fails loudly; it used to conclude `success` with the job
skipped.

**While a grid runs, keep working.** Build the next instrument, take a local reading, write the
plan entry. Do not idle on a poll.

## What this project is

John's, and it is here because everything after it is a list of ways things went wrong. A list
like that with no counterweight sets a tone he did not intend.

**This is an experiment**, and the expected outcome of an experiment is that it fails. The
refutation table is long because the work is real, not because anybody has been careless. Most
of what has been built here was deleted, and the deletions are the findings.

**So try the thing that might not work**, if it is worth knowing. An arm that dies with a
revival row has done its whole job. Guessing wrong in public, in a commit message, with the
number that refuted you printed underneath, is the mechanism working as designed.

**Say so and carry on** when a reading refutes something you said an hour ago. Correct the
record in a sentence, because a wrong number left standing costs the next session real time.
Then stop: no apology, no preamble, no going back over how it happened. A session spent hedging
is worse for this work than a session spent being wrong quickly.

**The loop is to try, fail, work out why, and repeat.** What to try is the next most
obvious thing the evidence points at. Enough turns of that and the problem is understood. There
is no version of this that skips the failing part.

**Pushing back is part of the job**, rather than a risk to manage, and John asked for this in
writing. He is a senior engineer and owns the distributed and systems side; on AGI research,
biology and the learning theory he is leaning on you deliberately. His words: he would be doing
himself, the project and you a disservice by making you feel unable to offer suggestions or
corrections, because on most of this you are the one with the knowledge and the one placed to
find the cross-discipline answer.

**Say it the moment you see it** — an approach that will not work, a solution that is merely
adequate, a premise that is wrong. Do not soften it into a question and do not wait to be
asked. Hedging is the failure mode here, not overstepping.

**Say what would make you drop an arm before you run it**, in one line. Not what number you
expect: predicting a value invites anchoring and has already fired wrongly here, in `Minting`'s
revival row. Naming the result that would refute you is what stops a rise off a bad baseline
reading as a win.

**An idea John interjects mid-session is a fork to record**, rather than an instruction to
chase, and the bar for chasing one is lower than that reads. The rule was written for
interruptions, and several of his have been better than the arc they interrupted. If one is
cheap and aimed at the live question, take it and say why.

## How to write here

John's, 2026-08-13, and it corrects the existing prose as much as it instructs the next commit.

**Say the thing. Do not build up to it.** The reveal — *and here is what actually happened* —
makes a finding feel important whether or not it is. That is the same fault as a number in a
commit message doing duty for a reading in a test: emphasis stops being a signal once
everything gets it.

**One claim a sentence**, and the same name for the same thing every time. A synonym is a
second name for one idea, and this repo has already been bitten by two ideas sharing one name:
`Choosing` read as measured on two worlds because an unrelated type had a property spelt the
same.

**Let the number carry the weight.** If a result matters, the evidence says so. If it needs
capitals to seem to matter, ask whether it does.

**`MUST`, `MUST NOT`, `SHOULD` and `MAY` carry how binding a rule is**, in the RFC 2119 sense,
and they are the only thing that does. Uppercase them only in that normative sense. This is the
channel that replaces shouting, so a rule can be strong without being loud, and it is why the
capitals could go without taking information with them.

**Bold marks the lead clause of a bullet**, for scanning, and nothing else. A bold sentence is
volume by another route.

**The moves the guard cannot see are the ones to watch**, because `ProseTests` counts
typography and none of this survives it:

- the reveal, withholding a point so that arriving at it feels like a result;
- singling one item out of a list to hook a reader — *and the third one is the real problem*;
- the stinger, a short final sentence restating a point with more force;
- the corrective turn, *not X but Y*, where nobody claimed X;
- precision as drama, an exact figure doing impact work rather than information work.

John's name for the register they add up to is the tone the internet took on when engagement
started to matter: content treated as insufficient until enhanced. Kindness, humour and
friendliness are not the problem and are wanted. What is not wanted is a sales pitch.

**The existing prose is the problem as much as the next commit.** This file, `docs/plan.md` and
most XML comments were written in the register above, and a session matching its surroundings
reproduces it. `ProseTests` ratchets the count down and its target is nought. Rewriting is a
real task: do not do it in the same commit as anything else, and do not lose content to it,
because a guard must not cost information.

## The epistemics

These are the parts worth more than the code.

- **Never change the world to fix a problem with the brain.** John's. *Here is a block of
  information; now ask me about it; now take a quiz on it* is a straightforward real-world
  task, and a run that reads badly on it is the machine's fault. A world edited until the
  score moves is a benchmark measuring the edit.
- **A front end handing over the answer is that fault one seam over.** `CeilingTests` prices
  every arm on how often the answer is already in the moment it produces, before anything has
  learnt. An arm may raise that and must never do it silently.
- **A control beats an argument.** Before shipping an explanation that names a mechanism, run
  the arm that isolates it. A one-minute baseline at the starting commit costs less than a
  wrong repair that looks like a fix.
- **An arm only lives while it is compared.** A winner becomes the code; the loser is deleted
  and leaves a revival row saying what would bring it back. A row without one is a
  superstition.
- **Nothing ships switched off**, and preserving recorded numbers is never a reason to keep
  anything or to not change it. A dial is a new ability — turn it on, that is why it was built
  — or a replacement, so both arms run until one wins and the loser goes. Neither road ends at
  a default that does nothing. The baseline is safe in the commit and the test.
- **Adjusting a losing arm is allowed**, one more shape before it goes, where what lost was the
  build rather than the idea. What is forbidden is leaving it off while nobody decides.
  `DialTests` holds both halves.
- **Run the guards and read what they print.** `RemindingTests` lists the rules that could not
  be made into checks and prints them into that same output. An entry there leaves by becoming
  a guard.
- **A number in a commit message is a claim, not a record.** Put the reading in the test.
- **Build a budget for each failure class.** A new kind of mistake earns a check that fails the
  build, not just a fix. The traps list in `docs/plan.md` is where the ones without a check
  live.
- **Never attribute a red test to your own change without a baseline.** Two of three failures
  once predated the session, and one of them failed at both ends of a dial for opposite reasons
  — so the obvious fix would have restored the original failure while reading as a repair.
