"""`loop` -- the turn and the idle.

`take_turn` is one exchange: encode, feed, generate under a budget, save the
state, append the transcript. `repl` is that in a loop with a human at one end,
and it is where Phase 0's exit is demonstrated -- talk, be killed, start again,
carry on.

THE IDLE SCHEDULER IS PHASE 4 AND IS NOT HERE YET. When it lands it must be a
PROCESS and not a function a test calls. That distinction is the reason a
stateful core was chosen at all: a machine that only computes when spoken to is
the event-driven shape complaint 2 names, and a scheduler invoked by its own
test has not escaped it. `tests/outstanding` asks for `IdleScheduler` and for a
24-hour unattended reading, in that order.
"""

from .turn import Answered, take_turn

__all__ = ["Answered", "take_turn"]
