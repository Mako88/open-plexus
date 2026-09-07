"""The REPL. Talk, be killed, start again, carry on.

PHASE 0'S EXIT IS DEMONSTRATED HERE: a thread continued across a restart, with
nothing about the earlier turns in the prompt. Run it, say something, `/quit` or
kill the window, run it again, and ask about what was said. Whether the answer
is right is Phase 1's reading and not this file's business -- what this file
owes is that the state on disk is the state that comes back.

  uv run sylvatica                 # resume the default thread
  uv run sylvatica --thread scratch --size 0.4
  uv run sylvatica --fresh         # ignore the saved state, keep the transcript

Commands inside: `/quit`, `/state` (what is on disk and how big), `/cost`
(the session's meter), `/fresh` (drop the state without leaving).
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from ..core.base import Meter
from ..core.checkpoints import ensure
from ..core.rwkv_core import RwkvCore
from ..core.thread import DEFAULT_THREAD, Thread
from .turn import DEFAULT_BUDGET, prime, take_turn


def build(size: float, thread_name: str, fresh: bool) -> tuple[RwkvCore, Thread, object]:
    checkpoint = ensure(size)
    print(f"core: {checkpoint.name}", file=sys.stderr)
    core = RwkvCore(checkpoint)
    thread = Thread(thread_name)

    if fresh or not thread.exists:
        # A FRESH STATE IS PRIMED ONCE AND A RESUMED ONE IS NOT. The preamble is
        # already inside a state that was saved after any turn, and feeding it
        # again on every start would be re-sending -- in miniature, the exact
        # thing this branch exists to stop doing.
        state, _ = prime(core)
        where = "fresh state, primed"
    else:
        state = thread.load(core)
        where = f"resumed {thread.state_path} ({core.state_bytes(state) / 1e6:.1f} MB)"
    turns = sum(1 for _ in thread.turns())
    print(f"thread '{thread.name}': {turns} turns on disk, {where}", file=sys.stderr)
    return core, thread, state


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(prog="sylvatica", description=__doc__)
    parser.add_argument("--thread", default=DEFAULT_THREAD)
    parser.add_argument("--size", type=float, default=0.4, help="billions of parameters")
    parser.add_argument("--budget", type=int, default=DEFAULT_BUDGET)
    parser.add_argument("--fresh", action="store_true", help="ignore any saved state")
    parser.add_argument("--say", action="append", default=[], help="say this and exit; repeatable")
    parser.add_argument(
        "--answers",
        type=Path,
        default=None,
        help="write one JSON object per turn here; how a script reads what was said",
    )
    args = parser.parse_args(argv)

    core, thread, state = build(args.size, args.thread, args.fresh)
    meter = Meter()

    # `--say` is how a test or a script drives the REPL: same code path as a
    # human typing, so what the exit demonstrates is the thing that ships.
    scripted = list(args.say)

    while True:
        if scripted:
            text = scripted.pop(0)
            print(f"> {text}")
        else:
            if args.say:  # scripted run, and the script is done
                break
            try:
                text = input("> ").strip()
            except (EOFError, KeyboardInterrupt):
                print(file=sys.stderr)
                break

        if not text:
            continue
        if text in ("/quit", "/exit"):
            break
        if text == "/state":
            print(
                f"{thread.state_path} "
                f"({'present' if thread.exists else 'absent'}, "
                f"{core.state_bytes(state) / 1e6:.1f} MB live), "
                f"{sum(1 for _ in thread.turns())} turns"
            )
            continue
        if text == "/cost":
            c = meter.total
            print(
                f"{meter.calls} turns, {c.tokens_in} in, {c.tokens_out} out, "
                f"{c.seconds:.1f}s, {c.tokens_per_second:.1f} tok/s, {c.flops:.3g} flops"
            )
            continue
        if text == "/fresh":
            state = None
            print("state dropped; the transcript is untouched")
            continue

        answered = take_turn(core, thread, state, text, budget=args.budget)
        state = answered.state
        meter.add(answered.cost)
        print(answered.text)

        # WHY A FILE AND NOT STDOUT. A scoring script that reads answers by
        # parsing this program's console output is reading the `rwkv` package's
        # banner too -- it prints "### RWKV-7 Goose enabled ###" and a loading
        # line straight to stdout at import and load time. The first version of
        # `scripts/phase0_restart.py` did exactly that and scored 0/3 on three
        # answers that were plainly right, which is this project's oldest
        # failure: A CHECK THAT REPORTS A VERDICT ABOUT SOMETHING IT DID NOT
        # MEASURE. Answers go somewhere nothing else writes.
        if args.answers:
            with args.answers.open("a", encoding="utf-8") as f:
                f.write(
                    json.dumps(
                        {
                            "asked": text,
                            "answer": answered.text,
                            "tokens_out": answered.cost.tokens_out,
                            "seconds": answered.cost.seconds,
                        },
                        ensure_ascii=False,
                    )
                    + "\n"
                )

    c = meter.total
    if meter.calls:
        print(
            f"[{meter.calls} turns, {c.seconds:.1f}s, {c.tokens_per_second:.1f} tok/s, "
            f"state saved to {thread.state_path}]",
            file=sys.stderr,
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
