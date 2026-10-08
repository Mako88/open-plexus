"""Whether a phrase is said in a text."""

from __future__ import annotations

import re


def said_in(answer: str, question: str) -> bool:
    """Whether `answer` is said in `question` as whole words, in either case."""
    return re.search(rf"\b{re.escape(answer.lower())}\b", question.lower()) is not None
