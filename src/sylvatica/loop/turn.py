"""One turn: what happens between something being said and the state being saved.

THE PHASE 0 SHAPE, and the doc's full shape is written in the docstring of
`take_turn` so the missing steps are visible rather than forgotten. Today a turn
is: encode the input, feed and generate, save the state, append the transcript.
Phase 2 inserts the store write at the front and retrieval before the feed;
Phase 4 hands the same function to the idle scheduler.

THE PROMPT FORMAT IS A DIAL AND IT IS LOAD-BEARING. RWKV's instruction tunes are
trained on a specific two-line shape, and getting it wrong does not error -- it
produces a model that rambles, which reads as the model being bad rather than as
the harness being wrong. It is a constant here so a reading can name it.
"""

from __future__ import annotations

import time
from dataclasses import dataclass
from typing import Any

from ..core.base import Cost, Sampling
from ..core.thread import Thread, Turn

# The RWKV instruct format. `\n\n` separates the roles and is also a stop
# string, so the model ending its answer and the model starting a new turn on
# somebody else's behalf are the same event.
USER_PREFIX = "User: "
CORE_PREFIX = "Assistant:"
SEPARATOR = "\n\n"

# `"User:"` IS A STOP STRING ON ITS OWN AND NOT ONLY AFTER A BLANK LINE.
# Observed on the first run at 1.5B: the core answered, then continued
# "...do you have now?User: I have eleven beehives" -- inventing the human's
# next line with no separator in front of it. Stopping only on `"\n\n"` let that
# through, and the invented half went into the transcript AND into the state as
# though somebody had said it. A core that hallucinates its interlocutor and
# then remembers doing so would poison every later reading, so this is a
# correctness stop rather than a tidiness one.
# AND THE ROLE WORDS ARE LISTED WITHOUT THEIR COLONS, WHICH IS THE PART THAT
# ACTUALLY MATTERS. Observed on the first Phase 0 restart reading: the core
# answered, emitted the single token "User", and THEN emitted end-of-text -- so
# the colon that `"User:"` waits for never arrived, the stop never fired, and
# the loop exited on the end-of-text token instead. Two answers in three ended
# in a dangling "User".
#
# THE TRANSCRIPT ARTIFACT WAS THE VISIBLE HALF AND THE CHEAPER ONE. `generate`
# appends a token, checks for a stop, and only then feeds it back -- so a stop
# that fires late means the token has ALREADY GONE INTO THE STATE. The core was
# carrying a fragment of somebody else's turn in its fixed-size memory, every
# turn, and the only sign of it was one word on the end of a line. Matching the
# bare role word breaks before the feed and closes both.
#
# The cost is that an answer legitimately containing "User" or "Assistant" is
# cut short. In a house of invented people that is a trade worth making, and it
# is a dial rather than a law.
STOP_STRINGS = (SEPARATOR, "User:", "Assistant:", "User", "Assistant")

# COMPLAINT 5: time is cheap and words are not. A small budget is the point, not
# a limitation to be raised when an answer gets cut off -- an answer that needs
# 400 tokens is a finding about brevity, which is an open fork ("brevity as a
# trained target"). Every reading records this number.
DEFAULT_BUDGET = 160


@dataclass
class Answered:
    """What one turn produced. The state is the part that matters."""

    text: str
    state: Any
    cost: Cost
    user_turn: Turn
    core_turn: Turn


def format_prompt(text: str) -> str:
    return f"{USER_PREFIX}{text.strip()}{SEPARATOR}{CORE_PREFIX}"


def trim_at_stop(text: str, stop_strings: tuple[str, ...]) -> str:
    """Everything before the earliest stop string. The transcript's half of the stop.

    `generate` stops WITHOUT feeding the offending token back, so the state is
    already clean -- but the token is still in what it returns, and writing it
    to the transcript would leave a dangling "User:" on the end of every answer.
    Two places to cut because the state and the record are two different things,
    and only one of them can be fixed by not feeding.
    """
    cut = len(text)
    for stop in stop_strings:
        found = text.find(stop)
        if found != -1:
            cut = min(cut, found)
    return text[:cut].strip()


def take_turn(
    core: Any,
    thread: Thread,
    state: Any | None,
    text: str,
    budget: int = DEFAULT_BUDGET,
    sampling: Sampling | None = None,
) -> Answered:
    """Say `text` into `thread` and return what came back, plus the new state.

    The full turn, per the design doc, is: write the input as an episode;
    retrieve and prepend hits in a fixed compact format; feed; generate under a
    budget; write the output as an episode; save the state. STEPS TWO AND THE
    TWO WRITES ARE PHASE 2 and are absent here -- the transcript stands in for
    the episode writes and there is nothing to retrieve from yet.

    The state returned is the state AFTER the generated tokens have been fed
    back in, so the core remembers what it itself said. A turn that fed only the
    user's half would give a model that cannot refer to its own last answer.
    """
    sampling = sampling or Sampling(stop_strings=STOP_STRINGS)
    index = thread.next_index

    prompt = format_prompt(text)
    prompt_tokens = core.encode(prompt)

    started = time.time()
    out_tokens, state, cost = core.generate(prompt_tokens, state, budget, sampling)
    answer = trim_at_stop(core.decode(out_tokens), sampling.stop_strings)

    user_turn = thread.append(
        Turn(
            index=index,
            role="user",
            text=text,
            tokens=len(prompt_tokens),
            at=started,
        )
    )
    core_turn = thread.append(
        Turn(
            index=index + 1,
            role="core",
            text=answer,
            tokens=len(out_tokens),
            at=time.time(),
            seconds=cost.seconds,
            flops=cost.flops,
        )
    )

    # SAVED AFTER THE TRANSCRIPT, DELIBERATELY. If the process dies between the
    # two, the next start has a transcript one turn ahead of the state, which is
    # visible and recoverable. The other order loses the record of a turn the
    # state has already absorbed, which is silent.
    thread.save(core, state)

    return Answered(
        text=answer, state=state, cost=cost, user_turn=user_turn, core_turn=core_turn
    )
