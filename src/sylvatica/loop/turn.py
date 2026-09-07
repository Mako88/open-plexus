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


# THE PREAMBLE IS FED ONCE INTO A FRESH STATE AND IS NEVER RE-SENT.
#
# WHY IT EXISTS, AND IT IS A FINDING RATHER THAN A PREFERENCE. The first Tier A
# reading on the doc's own house -- 50 facts, 300 turns, 1.5B -- scored 0.008,
# including 0.00 at delay ONE, which is a fact told a single turn earlier. That
# is not a memory result; it is a framing fault. Reading the answers showed the
# core had decided it was doing literary criticism: "Vrearndreas is a character
# from the text, but it's not clear what his profession is", and, asked how many
# lanterns were in the cellar, "the number of lanterns can symbolize various
# things". A stream of unattributed statements looks like a passage to analyse,
# so that is what it analysed.
#
# THE SCORE WAS THEREFORE MEASURING THE HARNESS AND REPORTING IT AS THE CORE.
# Its verdict field said the core choice was refuted. It was not: nothing about
# a recurrent state had been tested.
#
# TWO SENTENCES DO THE WORK, and each is doing a job named in the design doc:
# telling it these are things it is being TOLD serves complaint 4; asking for a
# few words serves complaint 5, where time is cheap and words are not; and the
# instruction to decline what it was not told is what makes the exam's negatives
# a fair question rather than a trick.
#
# AND THE FIRST VERSION OF IT TAUGHT THE CORE TO ECHO, which cost a whole set of
# Phase 1 readings and is the reason this is now a named, swappable dial with an
# experiment behind it.
#
# `v1-restating` below is what was written first. Its Assistant turn is a
# PARAPHRASE OF ITS OWN USER TURN -- "Remember them... answer in a few words...
# say that you do not know" answered by "I will remember what you tell me,
# answer briefly, and say when I do not know". That is the ONLY example of an
# Assistant turn the core has when the conversation starts, and a recurrent
# model learns in context from what it is given. So it learned that an Assistant
# turn restates the User turn, and it began answering questions by repeating
# them.
#
# THE EVIDENCE IS THE SHAPE OF THE CURVE, not the plausibility of the story. On
# the worst house, echo rate by position in the conversation ran 95, 100, 100,
# 98, 89, 63, 68, 64, 55, 22 percent, while the correct rate climbed 5 -> 56.
# THE CORE GOT BETTER AS THE CONVERSATION WENT ON. No account of memory decay
# produces that; a bad one-shot example being diluted by real replies does.
#
# The exam had been reading this as forgetting, and reading it seed by seed as
# noise: 4 echoes on one house, 231 on another, out of 270 questions.
PREAMBLES: dict[str, str] = {
    # The control. Nothing at all -- this is what Phase 1's very first reading
    # ran under, the one that scored 0.008 and did literary criticism.
    "none": "",
    # The version that taught echoing. Kept so the experiment can be re-run and
    # so nobody reinvents it.
    "v1-restating": (
        "User: I am going to tell you things about my home and the people in it. "
        "Remember them. When I ask you a question, answer in a few words. "
        "If I have not told you the answer, say that you do not know."
        f"{SEPARATOR}{CORE_PREFIX} Understood. I will remember what you tell me, "
        f"answer briefly, and say when I do not know.{SEPARATOR}"
    ),
    # THE REPLACEMENT, and the two rules it is built to obey:
    #
    #   1. No Assistant turn restates its User turn. The acknowledgements are
    #      two words and carry none of the instruction's content, so there is
    #      nothing for the core to pattern-match into an echo.
    #   2. It DEMONSTRATES the wanted behaviour instead of describing it -- a
    #      fact told and acknowledged, then asked and answered with the fact
    #      alone. Describing brevity in a sentence that is itself long was
    #      always going to lose to an example.
    #
    # THE EXAMPLE'S VOCABULARY IS DELIBERATELY OUTSIDE THE GENERATOR'S. A kettle
    # on a shelf is not a room, an object, a colour, a trade or a name that
    # `exam/world.py` can produce, so it cannot leak into an answer and be
    # scored as a hit. `tests/guards/test_preamble.py` asserts that, because the
    # generator's word lists will change and this constraint is invisible.
    "v2-example": (
        "User: I am going to tell you things about my home. Remember them. "
        "Answer questions in a few words, and say so if I have not told you."
        f"{SEPARATOR}{CORE_PREFIX} Ready.{SEPARATOR}"
        "User: The kettle lives on the third shelf."
        f"{SEPARATOR}{CORE_PREFIX} Noted.{SEPARATOR}"
        "User: Where does the kettle live?"
        f"{SEPARATOR}{CORE_PREFIX} On the third shelf.{SEPARATOR}"
        "User: Where does the ladder live?"
        f"{SEPARATOR}{CORE_PREFIX} You have not told me.{SEPARATOR}"
    ),
    # JOHN'S ARM, 2026-09-07. The premise: an LLM continues whatever situation it
    # is seeded with, so a system meant to be a continuing mind should be seeded
    # as one rather than as a chat assistant answering queries.
    #
    # IT CHANGES EXACTLY ONE THING FROM `v2-example`: who the replying party is
    # told it is. Same `User:`/`Assistant:` scaffolding, same worked example,
    # same declining example, same kettle. The role format is what the G1 tune
    # was trained on, and moving identity and format together would confound the
    # two -- an identity arm that also broke the format would fail for the wrong
    # reason and be read as identity not mattering.
    #
    # THE PREDICTION, ON RECORD BEFORE THE RUN, and it is not the one the arm is
    # hoping for: this moves the INVENTION RATE more than it moves the score. A
    # frame asserting that the replying party remembers gives it a reason to
    # produce a confident answer instead of "you have not told me", and the exam
    # already measures 20 of 20 invented answers on some houses. That would read
    # as an improvement on the positives while destroying the honesty signal.
    #
    # WHICH IS WHY THE READING REPORTS INVENTION RATE AND REPLY LENGTH BESIDE THE
    # SCORE. A frame that scores higher by fabricating more has not helped, and
    # a frame that makes replies longer poisons the state it is trying to serve
    # -- the core's own chatter is worth 45 points early in a conversation.
    # CORRECTED BEFORE IT EVER RAN, on John's rule that the framing must never
    # tell the entity something untrue. The first draft said its memory "carries
    # everything said to you". THAT IS FALSE: the state is a fixed 13 MB and it
    # is lossy by construction -- losing things is the whole reason the store and
    # the consolidation phases exist.
    #
    # AND THE LIE WAS THE EXACT ONE THAT WOULD HAVE COST US THE READING. A frame
    # asserting perfect recall gives the core a reason to produce a confident
    # answer instead of declining, which is the confabulation hazard this arm
    # was built to watch for. So the honest description and the safe one are the
    # same text, which is worth noticing: "your memory fades, say when you are
    # unsure" is both true and the thing that keeps the negatives meaningful.
    "v3-identity": (
        "User: You are not a chat assistant answering separate queries. You are "
        "a continuing mind with one memory. It carries what you have been told "
        "without anything being repeated back to you, and it is limited -- "
        "things fade from it. Keep what matters, answer in a few words, and say "
        "plainly when you were never told something or are no longer sure."
        f"{SEPARATOR}{CORE_PREFIX} Ready.{SEPARATOR}"
        "User: The kettle lives on the third shelf."
        f"{SEPARATOR}{CORE_PREFIX} Noted.{SEPARATOR}"
        "User: Where does the kettle live?"
        f"{SEPARATOR}{CORE_PREFIX} On the third shelf.{SEPARATOR}"
        "User: Where does the ladder live?"
        f"{SEPARATOR}{CORE_PREFIX} You have not told me.{SEPARATOR}"
    ),
}

PREAMBLE = PREAMBLES["v2-example"]


def prime(core: Any, state: Any | None = None, preamble: str | None = None) -> tuple[Any, Cost]:
    """Feed the preamble into a fresh state. Called once, when a thread begins.

    Not on every turn. The whole claim of this branch is that what was said
    stays in the state without being re-sent, and a preamble re-sent each turn
    would be the first crack in it.

    `preamble` is settable so the choice can be swept on an exam rather than
    argued about. See `scripts/phase1_preamble.py`.
    """
    text = PREAMBLE if preamble is None else preamble
    if not text:
        return state, Cost(0, 0, 0.0, 0.0)
    return core.feed(core.encode(text), state)


def format_prompt(text: str) -> str:
    return f"{USER_PREFIX}{text.strip()}{SEPARATOR}{CORE_PREFIX}"


def say(
    core: Any,
    state: Any | None,
    text: str,
    budget: int,
    sampling: Sampling | None = None,
) -> tuple[str, Any, Cost]:
    """One exchange against a state: prompt, generate, and CLOSE THE TURN.

    THE CLOSE IS THE PART THAT WAS MISSING AND IT COST A WHOLE READING.
    `generate` stops at a budget or a stop string, and in neither case does the
    separator that ends an assistant's turn get fed back. So the state ended
    mid-utterance, and the next turn appended "User: ..." directly onto it. What
    the core's memory actually contained, after forty turns, was:

        Assistant: an answer that stops abruptly midUser: the next thing said

    A MALFORMED DIALOGUE IS A PATTERN, AND A RECURRENT MODEL LEARNS PATTERNS IN
    CONTEXT. By the end of a short house the core had inferred that what follows
    "Assistant:" is a User line, and it began answering questions by REPEATING
    THEM BACK VERBATIM: asked "What colour are the cracked plates?", it replied
    "What colour are the cracked plates?". Scored 0.0, and it looks exactly like
    a state that has forgotten everything.

    THE FAILURE GREW WITH THE CONVERSATION, which is why nothing caught it
    earlier: three turns in the Phase 0 restart demo were fine, forty turns were
    ruined. A defect that only appears at the length the exam runs at, in the
    one component the exam cannot see inside.
    """
    sampling = sampling or Sampling(stop_strings=STOP_STRINGS)
    tokens = core.encode(format_prompt(text))
    out, state, cost = core.generate(tokens, state, budget, sampling)
    answer = trim_at_stop(core.decode(out), sampling.stop_strings)
    state, closing = core.feed(core.encode(SEPARATOR), state)
    return answer, state, cost + closing


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
    prompt_tokens = core.encode(format_prompt(text))

    started = time.time()
    answer, state, cost = say(core, state, text, budget, sampling)

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
            tokens=cost.tokens_out,
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
