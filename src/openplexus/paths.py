"""bAbI task 19, path finding: planning in miniature, and step 3's first problem.

A story places five rooms relative to each other; the question asks how to go from one to
another, and the answer is two moves, "n,e". The order matters, so the scorer here is ordered
where the word scorer in `scripts/ceiling.py` is a multiset. The step prompt is the judge's
view of one candidate move given the story, the question and the moves so far: the same
shape as the evaluator's judge prompt in step 2, with a move in place of a row.

The edge form is the second: the trunk never composes. Each sentence is one edge, "garden is
west of bathroom", which the trunk restates as `room|direction|room` and then verifies against
the sentence, yes or no (one fact a pass, the thing a one-pass trunk does), and ordinary code
builds the map, adds the inverse of every edge and walks the shortest path from the start room
to the goal. The one-pass trunk handed
the two right sentences scored 0.146 on the path and 0.75 only with 2048 tokens of thinking,
so the composing is what the thinking was for, and here it is code.
"""

from __future__ import annotations

import re

LETTERS = ["n", "s", "e", "w"]
WORDS = {"n": "north", "s": "south", "e": "east", "w": "west"}
LETTER_OF = {**{w: l for l, w in WORDS.items()}, **{l: l for l in LETTERS}}
OPPOSITE = {"n": "s", "s": "n", "e": "w", "w": "e"}

PATH_SYSTEM = (
    "You are given a short story placing rooms relative to each other, and a question asking "
    "how to go from one room to another. Answer using only the story. Reply with the moves "
    "only, as letters n, s, e or w separated by commas, for example n,e. Nothing else."
)


def moves_in(reply: str) -> list[str]:
    """The moves named in a reply, in order, as letters: "n,e", "north then east" and
    "Go N, then E." all read as ["n", "e"]."""
    return [LETTER_OF[w] for w in re.findall(r"[a-z]+", reply.lower()) if w in LETTER_OF]


def path_correct(gold: str, reply: str) -> bool:
    """The reply names exactly the gold moves, in order."""
    return moves_in(reply) == moves_in(gold)


def path_messages(sentences: list[str], question: str) -> list[dict]:
    """The asked arms' turn: the whole story and the question, moves as the answer."""
    story = "\n".join(sentences) if sentences else "(no story given)"
    return [{"role": "system", "content": PATH_SYSTEM},
            {"role": "user", "content": f"Story:\n{story}\n\nQuestion: {question}"}]


def step_prompt(sentences: list[str], question: str, steps: list[str], candidate: str) -> str:
    """What the evaluator's head reads: the story, the question, the moves so far and one
    candidate move, ending where a yes or no would come."""
    so_far = ", ".join(WORDS[s] for s in steps) if steps else "none yet"
    return (f"Facts:\n" + "\n".join(sentences) + f"\nQuestion: {question}\n"
            f"Moves so far: {so_far}\nNext move: {WORDS[candidate]}\nIs the next move right:")


EDGE_SYSTEM = (
    "Restate the sentence as room|direction|room, keeping the rooms in the order the sentence "
    "names them: the first room lies in that direction of the second. Examples: "
    "'The kitchen is east of the garden.' becomes kitchen|east|garden. "
    "'The office is north of the bedroom.' becomes office|north|bedroom. "
    "'The hallway is south of the bathroom.' becomes hallway|south|bathroom. "
    "Reply with that line only."
)

_SENTENCE = re.compile(r"the (\w+) is (north|south|east|west) of the (\w+)", re.I)
_QUESTION = re.compile(r"from the (\w+) to the (\w+)", re.I)


def parse_question(text: str) -> tuple[str, str] | None:
    """The start and goal rooms of "How do you go from the bathroom to the hallway?"."""
    m = _QUESTION.search(text)
    return (m.group(1).lower(), m.group(2).lower()) if m else None


def edge_of(line: str) -> tuple[str, str, str] | None:
    """One edge (room, direction letter, room) read from a story sentence or from the trunk's
    restatement `room|direction|room`; None when neither shape is there."""
    parts = [x.strip().lower() for x in line.strip().strip(".").split("|")]
    if len(parts) == 3 and parts[1] in LETTER_OF and parts[0] and parts[2]:
        return parts[0], LETTER_OF[parts[1]], parts[2]
    m = _SENTENCE.search(line)
    return (m.group(1).lower(), LETTER_OF[m.group(2).lower()], m.group(3).lower()) if m else None


def edge_messages(sentence: str) -> list[dict]:
    """The trunk's turn for one sentence: restate it as an edge."""
    return [{"role": "system", "content": EDGE_SYSTEM}, {"role": "user", "content": sentence}]


def shortest_path(edges: list[tuple[str, str, str]], start: str, goal: str) -> list[str] | None:
    """Breadth-first over the rooms, each edge "a is d of b" read both ways (from b, d leads to
    a; from a, the opposite of d leads to b); ties broken by the order n, s, e, w. None when
    the goal is not reached."""
    step: dict[str, dict[str, str]] = {}
    for a, d, b in edges:
        step.setdefault(b, {})[d] = a
        step.setdefault(a, {})[OPPOSITE[d]] = b
    seen = {start: []}
    queue = [start]
    while queue:
        room = queue.pop(0)
        if room == goal:
            return seen[room]
        for d in LETTERS:
            nxt = step.get(room, {}).get(d)
            if nxt is not None and nxt not in seen:
                seen[nxt] = seen[room] + [d]
                queue.append(nxt)
    return None


VERIFY_SYSTEM = "Read the sentence and answer the question with yes or no, nothing else."


def verify_messages(sentence: str, edge: tuple[str, str, str]) -> list[dict]:
    """The trunk's turn checking its own restatement against the sentence: is the first room
    in that direction of the second. A yes keeps the order, a no swaps it. The restatement
    alone swaps the rooms one time in eight (0.726 on the path); asked which room lies in the
    direction of the other it swapped more (0.486, `readings/path-20261029T143712Z`); asked to
    verify, every one of 2,500 edges came out right (1.0, `-143833Z`)."""
    a, d, b = edge
    return [{"role": "system", "content": VERIFY_SYSTEM},
            {"role": "user", "content": f"{sentence} Is the {a} {WORDS[d]} of the {b}?"}]
