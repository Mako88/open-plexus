"""What a turn puts into the state, and what it must never put there.

THE TWO TESTS THAT MATTER HERE BOTH GUARD FAULTS THAT ALREADY HAPPENED, and
both were invisible until an exam scored zero. Neither raised an exception,
neither showed up at three turns, and both looked exactly like a core that had
forgotten everything.
"""

from __future__ import annotations

from collections.abc import Sequence
from typing import Any

import pytest

from sylvatica.core.base import Cost, forward_flops
from sylvatica.core.thread import Thread
from sylvatica.loop.turn import PREAMBLE, SEPARATOR, prime, say, take_turn

from .stub import StubCore


class RecordingCore(StubCore):
    """A `StubCore` that keeps the text of everything ever fed into its state.

    This is the only way to check what a state CONTAINS without indexing into
    one, which the rules of `core` forbid everywhere else. What it records is
    the stream the real core would have seen.
    """

    def __init__(self, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self.fed: list[str] = []

    def feed(self, tokens: Sequence[int], state: Any | None) -> tuple[Any, Cost]:
        self.fed.append(self.decode(tokens))
        return super().feed(tokens, state)

    def generate(
        self, prompt_tokens: Sequence[int], state: Any | None, budget: int, sampling: Any = None
    ) -> tuple[list[int], Any, Cost]:
        reply = self.encode("ok")[:budget]
        self.fed.append(self.decode(list(prompt_tokens)) + self.decode(reply))
        state, _ = StubCore.feed(self, list(prompt_tokens) + reply, state)
        return (
            reply,
            state,
            Cost(
                len(prompt_tokens),
                len(reply),
                0.001,
                forward_flops(self.params, len(prompt_tokens) + len(reply)),
            ),
        )

    @property
    def stream(self) -> str:
        return "".join(self.fed)


def test_a_turn_closes_itself_with_the_separator():
    """THE FAULT THIS GUARDS COST TWO TIER A READINGS AND LOOKED LIKE AMNESIA.

    `generate` stops at a budget or a stop string, and in neither case is the
    separator that ends an assistant's turn fed back. The state therefore ended
    mid-utterance and the next turn appended "User: ..." straight onto it, so
    what the core's memory held was:

        Assistant: an answer that stops abruptly midUser: the next thing said

    A recurrent model learns patterns in context. By forty turns the core had
    inferred that a User line follows "Assistant:", and it started answering
    questions by repeating them back word for word. It scored 0.0 at delay ONE
    -- a fact told a single turn earlier -- which reads as a state that holds
    nothing, and is instead a state that holds a malformed transcript.

    THE FAILURE GREW WITH THE CONVERSATION, which is why three turns of Phase
    0's restart demo were fine. Nothing shorter than an exam would have found it.
    """
    core = RecordingCore()
    _, state, _ = say(core, None, "There are 65 lanterns in the cellar.", budget=2)
    assert core.stream.endswith(SEPARATOR), (
        f"the turn did not close; the state ends {core.stream[-30:]!r}"
    )

    _, _, _ = say(core, state, "What colour are the plates?", budget=2)
    # No "...okUser:" anywhere: every role change is preceded by a separator.
    assert "okUser:" not in core.stream, (
        f"a user line was appended onto an unterminated reply: {core.stream!r}"
    )


def test_the_preamble_is_fed_once_and_never_again(tmp_path):
    """A preamble re-sent every turn is re-sending, in miniature.

    It is the one piece of framing the core gets, and it goes into the state at
    the start of a thread. If it appeared in each turn's prompt the branch would
    be doing the thing it exists to stop doing, and every reading would be
    measuring a context window with extra steps.
    """
    core = RecordingCore()
    state, _ = prime(core)
    assert core.stream.count("I am going to tell you things") == 1

    thread = Thread("t", root=tmp_path)
    for i in range(4):
        answered = take_turn(core, thread, state, f"fact number {i}", budget=2)
        state = answered.state

    assert core.stream.count("I am going to tell you things") == 1, (
        "the preamble was fed more than once"
    )


def test_priming_costs_what_the_preamble_costs():
    core = RecordingCore()
    _, cost = prime(core)
    assert cost.tokens_in == len(core.encode(PREAMBLE))
    assert cost.flops > 0


@pytest.mark.parametrize("budget", [1, 4, 16])
def test_the_close_happens_whatever_stopped_the_generation(budget):
    core = RecordingCore()
    say(core, None, "anything at all", budget=budget)
    assert core.stream.endswith(SEPARATOR)
