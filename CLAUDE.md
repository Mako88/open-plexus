# Working in this repo — the `sylvatica` branch

Read `docs/sylvatica.md` first and whole. It is the only design doc, it holds THE ORDER, and it
says what to build, in what order, and what reading would refute each bet. Nothing finished is
written there; what a built thing does lives in the code and its docstrings, and findings live
in the commit that produced them, in the test that asserts them, and in `readings/`.

`docs/history/` holds the two earlier branches' plan and review. They tried a gradient-free
learner and lost to a blind rule. Their refutations still hold for what they measured; read
them before repeating anything they tried. Everything under `docs/history/` is read-only.

## How a session runs

John's standing instruction, written down so a new session starts itself. Orient, read the
handoff in the last commit message, then do all of the following without asking.

**Arm a five-minute `Monitor` and keep it armed.** Not `/loop`, not `schedule`, not a cron —
those have misfired here and the Monitor tick has not. It is a heartbeat: each tick is
permission to carry on, so `persistent: true` around a sleep loop is the shape wanted.

```bash
i=0; while true; do i=$((i+1)); echo "tick $i — next step or stop"; sleep 300; done
```

**Then work, and take forks yourself.** Where two routes are open, take the one likelier to
pay; if it does not pay, revert it and take the other. Do not stop to ask which. A DECIDED item
in the design doc is not a fork; reopening one is John's conversation, so say so in the handoff
and carry on with the rest.

**Stop the monitor when any of these is true** — do not let it tick on:

- there is no obvious next step, or the next one genuinely needs John;
- you are truly blocked;
- context is filling and it is time to write the handoff.

Stopping is a normal ending. Strike from THE ORDER whatever got done, and leave the handoff in
the last commit message and in the final reply: where the branch is against THE ORDER, what
was refuted, what is open. It does not carry a second copy of the ordering.

## The rules that carry over

- **Say what would refute an arm before you run it**, in one line, in the commit. Not what
  number you expect: predicting a value invites anchoring and has fired wrongly here before.
- **Say so and carry on** when a reading refutes something you said an hour ago. Correct the
  record in a sentence, no apology, no preamble. A session spent hedging is worse than a
  session spent being wrong quickly.
- **Pushing back is part of the job.** John is a senior engineer and owns the distributed and
  systems side; on the model, the learning theory and the biology he is leaning on you
  deliberately. Say it the moment you see it — an approach that will not work, a premise that
  is wrong. Do not soften it into a question.
- **Findings never go in the doc.** A reading is a JSON row under `readings/`, committed with
  the commit that took it. The commit message carries the verdict.
- **A red test is how an intent survives a session.** `tests/outstanding/` holds tests that
  fail until owed work is done. Each computes its state rather than asserting a constant. Do not
  delete or weaken one; it closes when the work closes. Anything red outside that directory is
  yours to fix.
- **`tests/pushback.py` is green and prints.** Each entry is a standing objection to something
  the branch currently does, with what would settle it either way. An entry leaves by being
  settled, and the count is asserted.
- **The GPU is one card and it is shared.** Never two GPU jobs at once. Kill a REPL before a
  training cycle.
- **Verify before claiming done.** `uv run pytest tests/guards` is seconds and runs every
  commit. `uv run pytest tests/outstanding` is the red set and is read, not fixed, unless the
  work closes. Exams are dispatched by hand and never run under `pytest` by default.
- **Push whenever.** Commits are the record and CI is not a gate on pushing. Put `[checkpoint]`
  in the message on a state worth returning to.

## The stack

Python 3.12 via `uv`. PyTorch from the cu126 wheel index, pinned to the last release whose
`torch.cuda.get_arch_list()` contains `sm_61`; the card is a GTX 1080 Ti and newer CUDA builds
have dropped Pascal. SQLite via the standard library. Tests are `pytest`. Formatting is `ruff`.
No secrets exist here yet; if one appears, it is gitignored before it is created.

## Layout

```
docs/sylvatica.md        the design, THE ORDER, what refutes it
docs/history/            the two earlier branches, read-only
src/sylvatica/core       Core protocol, RwkvCore, Thread, state save/load
src/sylvatica/store      Fragment, Store protocol, SqliteStore, retrieval
src/sylvatica/learn      replay sampling, extraction, LoRA, regression gate, merge
src/sylvatica/loop       the turn, the idle scheduler
src/sylvatica/exam       worlds, tiers, baselines, cost meter
src/sylvatica/node       Node, gossip, FanoutStore (protocol only before Phase 5)
tests/guards             fast structural tests, every commit
tests/outstanding        the red set
tests/pushback.py        standing objections
readings/                one JSON per run, committed
```
