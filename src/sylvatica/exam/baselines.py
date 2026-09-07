"""The two baselines. An arm reported without both is not a reading.

THE BLIND BASELINE IS WHY THIS FILE IS FIRST. Two branches of this project built
a learner, measured it against nothing, and were beaten by a rule that never
looked at the house. Blind is that rule, kept deliberately, and it is cheap: it
answers the commonest answer for the question's kind and reads nothing. Any arm
that does not clear it has not demonstrated memory, whatever else it has
demonstrated.

THE FULL-CONTEXT BASELINE IS HOW A TRANSFORMER WOULD DO IT. The same core, fed
the whole transcript up to the question, every time. It is the control for
refutation 1 -- if state plus store cannot beat re-sending at equal flops, then
state bought nothing and this branch is a worse version of what already exists.

AND IT IS EXPENSIVE, WHICH IS STANDING OBJECTION 8. It re-feeds the transcript
once per question rather than once per turn, which is the cheapest honest form
of it -- a baseline that answers questions does not need to sit through the
filler twice. Even so it is the dominant cost of any exam that runs it, and
`estimate_full_context_tokens` exists so a session knows that before starting
rather than four hours in.
"""

from __future__ import annotations

from typing import Any

from . import House, Question
from .world import Generated

# What the core is asked to say when it does not know. The negatives are scored
# against this: an answer that does not refuse is an invented answer.
REFUSALS = (
    "i don't know",
    "i do not know",
    "i'm not sure",
    "i am not sure",
    "no idea",
    "not sure",
    "nobody",
    "no one",
    "never mentioned",
    "you haven't told me",
    "you have not told me",
    "wasn't mentioned",
    "was not mentioned",
    "don't have",
    "do not have",
)


def is_refusal(answer: str) -> bool:
    """Whether an answer declines rather than invents. Used only for negatives."""
    lowered = answer.lower()
    return any(r in lowered for r in REFUSALS)


class BlindBaseline:
    """Answers the commonest answer for the question's kind. Reads nothing.

    IT CANNOT REFUSE, and that is the honest consequence of the rule rather
    than a handicap added to it. A rule that answers the mode of a kind has no
    way to know a question is a negative, so it invents on all of them. That
    number goes in the reading beside its score: blind's cost for its cheap
    correct answers is a 100% invention rate, and an arm that beats blind on
    score while matching it on invention has not beaten it.
    """

    def __init__(self, house: Generated) -> None:
        self.table = house.modal_answers()
        self.entropy = house.answer_entropy()

    def answer(self, question: Question, house: House | None = None) -> str:
        return self.table.get(question.kind, "")


def blind_baseline(house: Generated) -> BlindBaseline:
    """The blind rule for a house. Named as the doc names it."""
    return BlindBaseline(house)


def estimate_full_context_tokens(core: Any, house: Generated, reply_tokens: int = 40) -> int:
    """How many tokens the full-context baseline will re-feed, before running it.

    STANDING OBJECTION 8 IN EXECUTABLE FORM. The answer for the doc's own Phase 1
    house -- 50 facts over 300 turns -- is millions, which is hours on this card.
    Knowing that before the run is what stops a session from quietly dropping the
    baseline when it turns out to be slow, which is the exact failure that let a
    blind rule beat two branches unnoticed.
    """
    per_turn = [len(core.encode(t)) + reply_tokens for t in house.turns]
    running, total = 0, 0
    from .world import questions_at

    at = questions_at(house)
    for turn in range(len(house.turns)):
        running += per_turn[turn]
        total += running * len(at.get(turn, ()))
    return total


class FullContextBaseline:
    """The same core, fed the whole transcript from scratch for every question.

    NO STATE IS CARRIED BETWEEN QUESTIONS, which is the point: this is the
    event-driven shape complaint 2 names, done properly. Carrying a state
    forward would make it a cheaper, worse version of the arm rather than a
    control for it.
    """

    def __init__(self, core: Any, budget: int = 48) -> None:
        self.core = core
        self.budget = budget
        self.tokens_fed = 0

    def answer_at(self, transcript: list[str], question: Question) -> tuple[str, Any]:
        from ..core.base import Sampling
        from ..loop.turn import PREAMBLE, STOP_STRINGS, format_prompt

        # THE SAME PREAMBLE THE ARM GETS, and it must be here or the comparison
        # is between two different instructions rather than between two
        # memories. It is the arm's ONLY framing and it is free for this
        # baseline, which re-sends everything anyway -- so if the preamble is
        # doing the work rather than the state, this control is where that shows.
        context = "\n\n".join(f"User: {line}" for line in transcript)
        prompt = PREAMBLE + (context + "\n\n" if context else "") + format_prompt(question.text)
        tokens = self.core.encode(prompt)
        self.tokens_fed += len(tokens)
        out, _, cost = self.core.generate(
            tokens, None, self.budget, Sampling(stop_strings=STOP_STRINGS)
        )
        from ..loop.turn import trim_at_stop

        return trim_at_stop(self.core.decode(out), STOP_STRINGS), cost


def full_context_baseline(core: Any, budget: int = 48) -> FullContextBaseline:
    """The re-sending baseline for a core. Named as the doc names it."""
    return FullContextBaseline(core, budget)
