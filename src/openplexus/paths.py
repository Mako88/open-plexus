"""bAbI task 19, path finding: planning in miniature, and step 3's first problem.

A story places five rooms relative to each other; the question asks how to go from one to
another, and the answer is two moves, "n,e". The order matters, so the scorer here is ordered
where the word scorer in `scripts/ceiling.py` is a multiset. The step prompt is the judge's
view of one candidate move given the story, the question and the moves so far: the same
shape as the evaluator's judge prompt in step 2, with a move in place of a row.
"""

from __future__ import annotations

import re

LETTERS = ["n", "s", "e", "w"]
WORDS = {"n": "north", "s": "south", "e": "east", "w": "west"}
LETTER_OF = {**{w: l for l, w in WORDS.items()}, **{l: l for l in LETTERS}}

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
