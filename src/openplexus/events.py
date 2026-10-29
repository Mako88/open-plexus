"""bAbI task 7, counting: the second tool, and the edge form again with events for edges.

A story is people moving, picking things up, putting them down and handing them over; the
question is how many objects someone is carrying at that moment. The one-pass trunk is asked
nothing but to restate one sentence as one event, `person|took|object`, `person|dropped|object`,
`giver|gave|object|receiver` or `none` for a move, and ordinary code keeps who holds what and
counts. Counting is an aggregation over rows, which the plan sends to a tool; here the rows
are the trunk's restatements, and the deployed system has every input: the story so far and
the question.
"""

from __future__ import annotations

import re

NUMBERS = ["none", "one", "two", "three", "four", "five", "six", "seven", "eight"]

EVENT_SYSTEM = (
    "Restate the sentence as one event, in one of these forms and nothing else: "
    "person|took|object when someone picks something up, gets, grabs or takes it; "
    "person|dropped|object when someone drops, leaves, discards or puts something down; "
    "giver|gave|object|receiver when someone gives, hands or passes something to someone; "
    "none when nobody takes, drops or gives an object; going, moving, travelling or journeying "
    "to a room is none, a room is not an object. "
    "Examples: 'Mary grabbed the milk there.' becomes Mary|took|milk. "
    "'John put down the football.' becomes John|dropped|football. "
    "'Sandra handed the apple to Daniel.' becomes Sandra|gave|apple|Daniel. "
    "'Fred went back to the garden.' becomes none. "
    "'Mary journeyed to the bathroom.' becomes none. "
    "'Daniel travelled to the office.' becomes none."
)

VERBS = {"took": "took", "take": "took", "takes": "took", "got": "took", "get": "took",
         "grabbed": "took", "grab": "took", "picked": "took", "picked up": "took", "pick up": "took",
         "dropped": "dropped", "drop": "dropped", "left": "dropped", "leave": "dropped",
         "discarded": "dropped", "discard": "dropped", "put down": "dropped", "puts down": "dropped",
         "gave": "gave", "give": "gave", "gives": "gave", "handed": "gave", "hand": "gave",
         "passed": "gave", "pass": "gave"}

_TOOK = re.compile(r"^(\w+) (?:got|grabbed|picked up|took) the (\w+)", re.I)
_DROPPED = re.compile(r"^(\w+) (?:dropped|left|discarded|put down) the (\w+)", re.I)
_GAVE = re.compile(r"^(\w+) (?:gave|handed|passed) the (\w+) to (\w+)", re.I)


def event_of(line: str) -> tuple | None:
    """One event read from a story sentence or from the trunk's restatement: ("took", person,
    object), ("dropped", person, object), ("gave", giver, object, receiver), or None for a
    sentence in which nothing changes hands (a move, or the trunk's `none`)."""
    parts = [x.strip().strip(".").lower() for x in line.strip().split("|")]
    verb = VERBS.get(parts[1]) if len(parts) in (3, 4) else None
    if len(parts) == 3 and verb in ("took", "dropped") and parts[0] and parts[2]:
        return verb, parts[0], parts[2]
    if len(parts) == 4 and verb == "gave" and parts[0] and parts[2] and parts[3]:
        return "gave", parts[0], parts[2], parts[3]
    for kind, pat in (("took", _TOOK), ("dropped", _DROPPED), ("gave", _GAVE)):
        m = pat.match(line.strip())
        if m:
            return (kind, *[g.lower() for g in m.groups()])
    return None


def holdings(events: list[tuple | None]) -> dict[str, set[str]]:
    """Who holds what after the events, in order: a take adds, a drop removes, a gift moves."""
    held: dict[str, set[str]] = {}
    for e in events:
        if e is None:
            continue
        if e[0] == "took":
            held.setdefault(e[1], set()).add(e[2])
        elif e[0] == "dropped":
            held.setdefault(e[1], set()).discard(e[2])
        else:
            _, giver, obj, receiver = e
            held.setdefault(giver, set()).discard(obj)
            held.setdefault(receiver, set()).add(obj)
    return held


def event_messages(sentence: str) -> list[dict]:
    """The trunk's turn for one sentence: restate it as an event."""
    return [{"role": "system", "content": EVENT_SYSTEM}, {"role": "user", "content": sentence}]


def who_is_asked(question: str) -> str | None:
    """The person in "How many objects is Mary carrying?"."""
    m = re.search(r"is (\w+) carrying", question, re.I)
    return m.group(1).lower() if m else None


def count_word(n: int) -> str:
    return NUMBERS[n] if n < len(NUMBERS) else str(n)
