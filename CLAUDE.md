# Working in this repo — the `unfused` branch

Read `docs/unfused.md` first and whole. It is the only design doc and it holds THE ORDER.
Nothing finished is written there: what a built thing does is in its code, and findings live in
the commit that produced them, in a test that asserts them, and in `readings/`.

`docs/history/` holds the earlier branches' plans and one review, read-only. Read them before
repeating anything they tried.

## How a session runs

John's standing instruction. Orient, read the handoff in the last commit message, then do all of
the following without asking.

**Arm a five-minute `Monitor` and keep it armed.** Not `/loop`, `schedule` or a cron; those have
misfired here. It is a heartbeat: each tick is permission to carry on.

```bash
i=0; while true; do i=$((i+1)); echo "tick $i — next step or stop"; sleep 300; done
```

**Then work, and take forks yourself.** Where two routes are open, take the one likelier to
pay; if it does not, revert it and take the other. A DECIDED item in the design doc is not a
fork; reopening one is John's conversation.

**Stop the monitor** when there is no obvious next step, when the next one needs John, when you
are blocked, or when context is filling. Then strike what got done from THE ORDER and leave the
handoff in the last commit message and the final reply: where the branch is, what was refuted,
what is open.

## The rules

- **Say what would refute an arm before running it**, in one line, in the commit and in the
  run's `--note`. Not the number you expect; a prediction anchors how the result is read.
- **Correct the record in a sentence** when a reading refutes something said earlier, and carry
  on.
- **Push back** the moment an approach looks wrong. John owns the systems side and leans on
  you for the learning theory; hedging is the failure mode here, not overstepping.
- **A control beats an argument.** Before shipping an explanation that names a mechanism, run
  the arm that isolates it.
- **Findings never go in the doc.** A reading is a JSON file under `readings/`, committed with
  the commit that took it.
- **An arm lives only while it is compared.** The winner becomes the code; the loser is
  deleted, and the commit that deletes it says what would bring it back.
- **Never change the world to fix the machine.** A form of question is never removed because
  an arm does badly on it.
- **A red test is how an intent survives a session.** `tests/outstanding/` fails until owed
  work is done; do not delete or weaken one. `tests/pushback.py` holds standing objections,
  each with what would settle it, and its count is asserted.

## Commands

```bash
uv run pytest tests/guards          # seconds, every commit, as its own command
uv run pytest tests/outstanding     # the red set; read it, do not fix it unless the work is done
uv run python tests/pushback.py     # the objections
uv run python scripts/exam.py --faculty served --arms blind,recall,full --seed 0 --note "..."
```

The faculty is a llama-server. Port 8093 carries Qwen3.5-9B (the context-reduction
baseline), port 8094 carries Qwen3.5-2B (the small faculty the system is read under). Port 8080
belongs to something else of John's. llama.cpp is John's winget install, kept at latest; the
Qwen3.5 small models need build 11000 or later.

```bash
llama-server -m <Qwen3.5-2B-Q8_0.gguf> -ngl 99 -c 16384 --parallel 1 --jinja   --reasoning-budget 0 --host 127.0.0.1 --port 8094
llama-server -m <Qwen_Qwen3.5-9B-Q6_K_L.gguf> -ngl 99 -c 8192 --parallel 1 --jinja   --reasoning-budget 0 --host 127.0.0.1 --port 8093
uv run python scripts/exam.py --faculty served --served Qwen3.5-2B-Q8_0 --port 8094   --arms blind,full,linked,system --seed 1 --note "..."
uv run python scripts/ears.py --seed 1 --name "..."
```

The card is one GTX 1080 Ti, 11 GB. The 9B takes about 9 GB of it, so it and the 2B are never
up together. Stopping a background shell does not stop the server it started; find it with
`Get-CimInstance Win32_Process -Filter "Name='llama-server.exe'"` and stop it by id.

## The stack

Python 3.12 via `uv`. PyTorch from the cu126 index, pinned because newer builds dropped
Pascal (`sm_61`). SQLite from the standard library. The HF cache is used offline
(`HF_HUB_OFFLINE=1` in the scripts). `ruff` for formatting.

## Layout

```
docs/unfused.md        the design, THE ORDER, what refutes it
docs/history/          earlier branches, read-only
src/unfused/faculty.py the frozen language model, in-process or served
src/unfused/store/     fragments, hybrid recall
src/unfused/arms.py    text-memory arms: blind, full context, recall
src/unfused/linked.py  linked recall
src/unfused/ears.py    the ear: sentences and questions to typed assertions
src/unfused/system.py  the system: assertions stored, questions matched and chained
src/unfused/exam/      the house and the runner
scripts/exam.py        runs arms and writes readings
scripts/ears.py        scores an ear against the house
tests/guards           fast structural tests
tests/outstanding      the red set
tests/pushback.py      standing objections
readings/              one JSON per arm per run, committed
```
