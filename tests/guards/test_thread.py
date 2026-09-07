"""A thread survives the process, and the transcript is never fed back.

PHASE 0'S EXIT IN STRUCTURAL FORM. The real demonstration is a human killing the
REPL and asking it what they said; these are the parts of that which can be
checked in milliseconds on no GPU, using `StubCore`.
"""

from __future__ import annotations

import pytest

from sylvatica.core.thread import Thread, Turn
from sylvatica.loop.turn import format_prompt, take_turn

from .stub import StubCore


@pytest.fixture
def core():
    return StubCore()


def test_a_state_written_by_one_process_is_read_by_the_next(tmp_path, core):
    thread = Thread("t", root=tmp_path)
    assert not thread.exists

    state, _ = core.feed(core.encode("the roof was replaced in the spring"), None)
    thread.save(core, state)

    # A DIFFERENT `Thread` OBJECT AND A DIFFERENT `Core` OBJECT, which is the
    # point: nothing is carried in memory between the save and the load.
    resumed = Thread("t", root=tmp_path).load(StubCore())
    assert resumed == state


def test_a_state_from_the_wrong_checkpoint_refuses_rather_than_loading(tmp_path, core):
    """The failure this prevents is a wrong number, not an exception.

    A state is meaningless beside different weights. Loading one shaped
    plausibly-but-wrong would give a core that answers badly, and every reading
    taken on it would be a reading of nothing.
    """
    thread = Thread("t", root=tmp_path)
    state, _ = core.feed([1, 2, 3], None)
    thread.save(core, state)

    with pytest.raises(ValueError, match="state was saved from"):
        thread.load(StubCore(n_layer=4))


def test_the_transcript_is_append_only_and_indexes_continue(tmp_path, core):
    thread = Thread("t", root=tmp_path)
    thread.append(Turn(index=0, role="user", text="hello", tokens=5))
    thread.append(Turn(index=1, role="core", text="ok", tokens=2))
    assert thread.next_index == 2

    reopened = Thread("t", root=tmp_path)
    assert [t.text for t in reopened.turns()] == ["hello", "ok"]
    assert reopened.next_index == 2

    reopened.append(Turn(index=2, role="user", text="again", tokens=5))
    assert [t.text for t in Thread("t", root=tmp_path).turns()] == ["hello", "ok", "again"]


def test_a_turn_writes_both_halves_and_saves_the_state(tmp_path, core):
    thread = Thread("t", root=tmp_path)
    answered = take_turn(core, thread, None, "what colour is the door")

    roles = [t.role for t in thread.turns()]
    assert roles == ["user", "core"]
    assert thread.exists, "the state was not saved after the turn"
    assert answered.state is not None
    assert answered.cost.tokens_out > 0


def test_the_prompt_carries_only_this_turn(tmp_path, core):
    """COMPLAINT 2 AS A TEST. The transcript must not leak into the prompt.

    Re-sending what was already said is the workaround this branch exists to
    replace, and it is the easiest thing in the world to add back by accident --
    a "just for context" prepend would make every reading meaningless while
    making every score look better. This computes the prompt for a turn taken
    after a long history and asserts the history is not in it.
    """
    thread = Thread("t", root=tmp_path)
    state = None
    for i in range(5):
        answered = take_turn(core, thread, state, f"secret number {i} is {i * 7}")
        state = answered.state

    prompt = format_prompt("what was secret number 3")
    for i in range(5):
        assert f"secret number {i} is" not in prompt, (
            "an earlier turn appeared in the prompt; the state is supposed to be "
            "carrying that, and a reading taken this way measures re-sending"
        )
    assert "what was secret number 3" in prompt


def test_a_state_that_was_never_told_anything_round_trips_as_none(tmp_path, core):
    thread = Thread("t", root=tmp_path)
    thread.save(core, None)
    assert thread.load(core) is None
