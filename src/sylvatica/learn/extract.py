"""The declaratives arm: facts pulled out of episodes, then said several ways.

WHAT THIS ARM IS FOR, and the raw arm's failure makes it unusually specific.
Overfitting twenty episodes for twenty epochs got 4 of 20 -- and the sixteen
misses were WELL-FORMED SENTENCES OF THE HOUSE WITH THE WRONG FILLER: asked how
many of a thing were in a room, it said "There are 18 flour sacks in the pantry"
when the answer was 13; asked a colour, "The lanterns are ochre" when the answer
was slate. Raw episodes teach the SHAPE of the house's sentences, its vocabulary
and its register, and do not teach which name goes with which room.

SO THIS ARM'S JOB IS NOT MORE SIGNAL. It is signal that varies the SURFACE while
holding the BINDING fixed. If a fact is only ever seen as one sentence, the
sentence and the fact are the same object to a language model and the easiest
thing to learn is the sentence. Say it five ways and the sentence stops being
learnable while the binding stays constant, which is the only thing that can
separate them.

THE DOC EXPECTS THIS TO WIN and names its refutation: "the extractor at 1.5B
produces facts that are wrong more than a fifth of the time, read by hand on a
sample of 50". A wrong declarative is worse than a raw episode, because it is a
confident false binding trained in with the same weight as a true one -- so the
error rate is a reading that must be taken BEFORE the arm's Tier C means
anything.

TEMPLATES FIRST, THEN THE CORE. Template paraphrases cannot introduce a falsehood
because they only rearrange what is already there; core paraphrases are more
varied and can hallucinate. Both are produced, and every declarative records
which made it, so the hand-read can tell one failure from the other.
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field
from typing import Any

# ASKING FOR ONE FACT A LINE, IN THE HOUSE'S OWN REGISTER. A general "summarise
# this" produces prose, and prose trains the model on prose. The exam scores
# short canonical answers, so the training signal should be short sentences.
EXTRACT_PROMPT = (
    "Here is part of a conversation:\n\n{window}\n\n"
    "List the specific facts it states, one per line, as short plain sentences. "
    "Only facts that were actually said."
)

PARAPHRASE_PROMPT = "Say this exactly once more, in different words, keeping every detail:\n\n{fact}"


@dataclass
class Declarative:
    """One fact, however it was produced, and everything needed to audit it."""

    text: str
    source: str  # "core-extracted" | "template" | "core-paraphrase"
    window: str = ""
    parent: str = ""

    def row(self) -> dict[str, Any]:
        return {
            "text": self.text,
            "source": self.source,
            "parent": self.parent,
            "window": self.window[:200],
        }


@dataclass
class Extraction:
    """What one window produced, with the counts a reading needs."""

    declaratives: list[Declarative] = field(default_factory=list)
    windows: int = 0
    rejected: int = 0

    def texts(self) -> list[str]:
        return [d.text for d in self.declaratives]


# Lines a small core emits that are not facts. Cheap and specific: anything more
# clever would be a second model whose own failures nobody is watching.
_NOT_A_FACT = re.compile(
    r"^\s*(here|the following|these|okay|sure|i |in summary|facts?:|list:|\d+\.\s*$)",
    re.IGNORECASE,
)


def _clean(line: str) -> str:
    line = line.strip().lstrip("-*•").strip()
    line = re.sub(r"^\d+[.)]\s*", "", line)
    return line.strip()


def usable(line: str) -> bool:
    """Whether a generated line is plausibly a fact rather than scaffolding."""
    line = line.strip()
    if len(line) < 12 or len(line) > 200:
        return False
    if _NOT_A_FACT.match(line):
        return False
    # A fact has a verb and a full stop's worth of content; a fragment has
    # neither. Two words is not a fact about anything.
    return len(line.split()) >= 4


def templates(fact: str) -> list[str]:
    """Rearrangements that cannot introduce a falsehood, because they only move words.

    Deliberately narrow. A template that guesses at structure it cannot see would
    produce confident nonsense, and confident nonsense is exactly what this arm
    must not add -- the whole reason the doc's refutation for it is an ERROR RATE
    rather than a score.
    """
    out: list[str] = []
    stripped = fact.rstrip(".").strip()

    # "X keeps the Y in the Z" -> "The Y are in the Z" / "In the Z: X's Y"
    m = re.match(r"^(.+?) keeps the (.+?) in the (.+)$", stripped, re.IGNORECASE)
    if m:
        who, what, where = m.groups()
        out.append(f"The {what} are in the {where}.")
        out.append(f"{who} keeps the {what} in the {where}.")
        out.append(f"It is in the {where} that {who} keeps the {what}.")

    # "There are N X in the Y" -> "The Y holds N X" / "N X, in the Y"
    m = re.match(r"^There are (\S+) (.+?) in the (.+)$", stripped, re.IGNORECASE)
    if m:
        n, what, where = m.groups()
        out.append(f"The {where} holds {n} {what}.")
        out.append(f"{n} {what} are in the {where}.")

    # "The X are Y" -> "Y is the colour of the X"
    m = re.match(r"^The (.+?) are (\S+)$", stripped, re.IGNORECASE)
    if m:
        what, how = m.groups()
        out.append(f"{how.capitalize()} is the colour of the {what}.")
        out.append(f"Those {what}? {how.capitalize()}.")

    # "A is B's cousin" -> "B is A's cousin"
    m = re.match(r"^(.+?) is (.+?)'s cousin$", stripped, re.IGNORECASE)
    if m:
        a, b = m.groups()
        out.append(f"{b} and {a} are cousins.")
        out.append(f"{a} is a cousin of {b}.")

    # "X <verb>s <object> for a living" -> "X's trade is ..."
    m = re.match(r"^(.+?) (\S+s \S+) for a living$", stripped, re.IGNORECASE)
    if m:
        who, trade = m.groups()
        out.append(f"{who} {trade} by trade.")
        out.append(f"The one who {trade} is {who}.")

    return out


def extract(
    core: Any, window: list[str], budget: int = 96
) -> list[Declarative]:
    """Ask the core what facts a window of turns states.

    THE WINDOW IS TURNS AND NOT ONE TURN, because a fact can be spread across an
    exchange, and because a single sentence handed back is not extraction, it is
    a copy -- which is the raw arm again.
    """
    from ..core.base import Sampling
    from ..loop.turn import STOP_STRINGS, format_prompt, trim_at_stop

    text = "\n".join(window)
    prompt = format_prompt(EXTRACT_PROMPT.format(window=text))
    out, _, _ = core.generate(
        core.encode(prompt), None, budget,
        # GREEDY, because this is extraction and not conversation: the same
        # window should always yield the same declaratives, or a Tier C reading
        # cannot be reproduced from its seed.
        Sampling(stop_strings=STOP_STRINGS, greedy=True),
    )
    said = trim_at_stop(core.decode(out), STOP_STRINGS)
    return [
        Declarative(text=_clean(line), source="core-extracted", window=text)
        for line in said.splitlines()
        if usable(_clean(line))
    ]


def paraphrase(
    core: Any, fact: str, n_core: int = 1, budget: int = 48
) -> list[Declarative]:
    """Several ways of saying one fact: templates first, then the core."""
    from ..core.base import Sampling
    from ..loop.turn import STOP_STRINGS, format_prompt, trim_at_stop

    out = [
        Declarative(text=t, source="template", parent=fact) for t in templates(fact)
    ]
    for _ in range(n_core):
        prompt = format_prompt(PARAPHRASE_PROMPT.format(fact=fact))
        got, _, _ = core.generate(
            core.encode(prompt), None, budget,
            Sampling(stop_strings=STOP_STRINGS, greedy=True),
        )
        said = _clean(trim_at_stop(core.decode(got), STOP_STRINGS).splitlines()[0]
                      if trim_at_stop(core.decode(got), STOP_STRINGS) else "")
        if usable(said):
            out.append(Declarative(text=said, source="core-paraphrase", parent=fact))
    return out


def declaratives_from(
    core: Any,
    fragments: list[Any],
    window_size: int = 4,
    n_core_paraphrases: int = 1,
) -> Extraction:
    """The whole arm: windows of episodes in, paraphrased declaratives out."""
    result = Extraction()
    texts = [f.text for f in fragments]
    for i in range(0, len(texts), window_size):
        window = texts[i : i + window_size]
        if not window:
            continue
        result.windows += 1
        for fact in extract(core, window):
            result.declaratives.append(fact)
            result.declaratives.extend(
                paraphrase(core, fact.text, n_core=n_core_paraphrases)
            )
    return result
