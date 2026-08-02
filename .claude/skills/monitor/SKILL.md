---
name: monitor
description: Arm the five-minute working monitor for a long autonomous stretch on Open Plexus. Each tick re-reads NOW.md; NOW.md is rewritten at the end of every turn; work continues while a path forward is clear; the monitor is stopped with a full summary when only John's decisions remain. Use when John says "run the monitor", "start the monitor", "arm the monitor", or asks for a long autonomous run.
---

# The working monitor

John works from his phone and often steps away. This is the loop that lets a
long run continue without him, and that hands him a readable state when it
stops.

**Monitor, not `schedule` and not `loop`.** Both have failed on this project.
Use the `Monitor` tool.

## Arm it

```
Monitor(
  description: "five-minute working tick — re-read NOW.md and keep going",
  persistent: true,
  command: <the loop below>
)
```

```bash
while true; do
  sleep 300
  echo "TICK — read NOW.md for the current state, then carry on. Rewrite NOW.md at the end of this turn. Keep working while a path forward is clear. When only John's decisions remain, stop this monitor and post a full summary of everything since his last message."
done
```

`persistent: true` matters — a timeout would end the run silently, which looks
exactly like a run that finished.

## The three standing rules while it is armed

**1. Read `NOW.md` on every tick.** It is the state, not a reminder. Anything
that matters across turns has to be in it, because a tick is the only thing that
survives a compaction.

**2. Rewrite `NOW.md` at the end of every turn.** Not append — a finding updates
a line. It carries only unfinished work; settled results move to `README.md`.
This is also in `CLAUDE.md` under Running.

**3. Keep going while a path forward is clear.** Something to build, a test to
run, a measurement to take, an idea worth trying — take it. John has approved
long autonomous runs explicitly and repeatedly.

## When to stop

Stop when the *only* thing left is a decision John has to make, or when
everything on the bench is blocked on him.

Do not stop because a natural-looking pause arrived. A refuted idea is not a
stopping point — it is a result, and the next thing to try usually follows from
what refuted it.

## How to stop

1. `TaskStop` the monitor. Leaving it armed after stopping work means he gets
   ticks with nothing behind them.
2. Post **a full summary of everything since his last message**, in the reply
   itself and not only in a file. He reads on a phone: short paragraphs,
   headings, no wide tables. Include what was built, what was measured, what was
   refuted, and the exact decisions waiting on him.
3. Say plainly which parts were verified and which were assumed.
