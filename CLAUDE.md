# Working in this repo

Read `docs/plan.md` first. It is the only doc, it holds nothing finished, and findings live
in the commit that produced them and in the test that asserts them — never here and never
there.

## How a session runs, which you do not need to be told again

John's standing instruction, written down so a new session starts itself. Orient, read the
handoff if there is one, then **do all of the following without asking.**

**Arm a five-minute `Monitor` and keep it armed.** Not `/loop`, not `schedule`, not a cron —
those have misfired here and the Monitor tick has not. It is a heartbeat rather than a
watcher: each tick is permission to carry on, so `while true; do echo ...; sleep 300; done`
with `persistent: true` is exactly the shape wanted. Something like:

```bash
i=0; while true; do i=$((i+1)); echo "tick $i — next step or stop"; sleep 300; done
```

**Then work, and take forks yourself.** Where two routes are open, take the one likelier to
pay, and if it does not, revert it and take the other. Do not stop to ask which. An idea John
interjects mid-session is a fork to record rather than an instruction to chase.

**Stop the monitor — do not let it tick on — when any of these is true:**

- there is no obvious next step, or the next one genuinely needs John;
- you are truly blocked;
- context is filling and it is time to write the handoff.

Stopping is a normal ending rather than a failure. Compact `docs/plan.md` on the way out, and
leave the handoff in the last commit message and in the final reply.

**Everything below this line still applies while the monitor runs** — the guards every
commit, an arm only living while it is compared, and no dead code left behind.

## The suite, and why pushing is free

**Push whenever. Do not hold a commit back waiting for CI.** `tests.yml` runs on every push
and its concurrency group is deliberate: a run in flight is never killed, a newly queued run
waits behind it, and a third arrival cancels the one still *waiting* rather than the one
working. So intermediate commits are skipped without any finished work being thrown away,
and whatever is newest when the queue clears is what gets tested. Waiting to push buys
nothing that already gives.

**Put `[checkpoint]` in the commit message when a specific commit must clear.** That makes
the concurrency group unique to the SHA, so it queues behind nothing, cancels nothing and
nothing cancels it. Use it before shipping a default, or on a state worth returning to.
Everything else keeps rolling.

**Run the structural guards locally every commit, as their own command.** `DocsTests`,
`DeadCodeTests`, `DuplicationTests`, `DialTests`, `SeparationTests`, `InertDialTests`,
`SweepListTests` and `ShardTests` take seconds and go red for changes that look unrelated.
`ShardTests` is the one that fails when a test class lands in two CI shards or in none, and
a class in none is green forever because nothing ever asked it. Never chain the
check into the commit — that has produced red commits more than once. Rebuild before running
with `--no-build`, or the binary under test is the one from before the edit.

## The measurements

Sweeps are excluded from the suite by the `kind=sweep` trait and dispatched by hand:

```bash
gh workflow run sweeps.yml --ref <branch> -f only=<Class>
```

**One entry a runner, and an entry may be a class or one method of one.** A class holding
several independent grids is several runners' work sitting on one — split it into method
entries in the list inside `sweeps.yml`. A dispatch naming the class still matches every
method of it by substring. This is not parallelising a measurement: each runner is still one
serial run at a serial run's load, which is the thing `Parallelism.cs` protects.

**Dispatch several sweeps at once.** The concurrency group carries the input, so different
measurements on one ref run side by side rather than queueing.

**Adding a sweep is a line in `sweeps.yml`.** The list is named rather than discovered, on
purpose. A dispatch matching nothing now fails loudly; it used to conclude `success` with the
job skipped.

**While a grid runs, keep working.** Build the next instrument, take a local reading, write
the plan entry. Do not idle on a poll.

## The epistemics

These are the parts worth more than the code.

- **A control beats an argument.** Before shipping an explanation that names a mechanism, run
  the arm that isolates it. A one-minute baseline at the starting commit costs less than a
  wrong repair that looks like a fix.
- **An arm only lives while it is compared.** A winner becomes the code; the loser is deleted
  and leaves a revival row saying what would bring it back. A row without one is a
  superstition.
- **A number in a commit message is a claim, not a record.** Put the reading in the test.
- **Build a budget for each failure class.** A new kind of mistake earns a check that fails
  the build, not just a fix. The traps list in `docs/plan.md` is where the ones without a
  check live.
- **Do not attribute a red test to your own change without a baseline.** Two of three
  failures once predated the session, and one of them failed at both ends of a dial for
  opposite reasons — so the obvious fix would have restored the original failure while
  reading as a repair.
