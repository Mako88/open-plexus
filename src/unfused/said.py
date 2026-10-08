"""Whether a phrase is said in a text."""

from __future__ import annotations

import re


def said_in(answer: str, question: str) -> bool:
    return re.search(rf"\b{re.escape(answer)}\b", question.lower()) is not None
