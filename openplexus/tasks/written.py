"""A written word as a SENSOR reading, not as a label.

John's position, 2026-08-01: text is a legitimate input for a digital system,
one of several that co-occur, and not a label. The pipeline did not implement
it. `surfaces_pipeline.py` reserved one word node per class and filled it from
`said = [u.digit for u in heard]` — the scoring label, never wrong, never
absent, multiplicity one against a hundred image codes per digit. That is a
label whatever it is called.

This is the repair, and it follows from the position rather than retreating from
it. A word arrives as BYTES, is written more than one way, is sometimes absent
and sometimes wrong, and is quantised by the same hash as everything else. The
system then has to discover that several word surfaces are one thing, which is
the job a hundred image codes per digit already pose.

## What is simulated, and it must be said plainly

The variation here is generated, not recorded. There is no corpus of people
writing digits. `Channel` decides silence, mistake and corruption from
parameters somebody chose, so **the word channel's difficulty is a dial and not
a measurement**, and any result that moves when those numbers move is a result
about them. They are swept for that reason.

What is NOT simulated is the part that mattered: nothing here reads a label to
decide which surface fires. `speak` is told a digit and returns bytes; what a
byte becomes is the hash's business, and a mistake genuinely names the wrong
digit rather than flagging itself.

## Why a byte histogram, and what it costs

`features` counts bytes and keeps no order, so `three` and `there` collide. That
is a real loss and it is the same crude honest choice `surfaces.spectra` makes
when it throws away speaking rate: a cruder sensor is better than a cleverer one
that smuggles in knowledge of what a word is. Order-sensitive features are the
obvious extension and would change the SPACE rather than the allocation.

## What was searched

By capability — word, token, text, byte, render, spell, vocabulary — across
`openplexus/`, `experiments/` and `tools/`.

- **`openplexus/tasks/mnist.py`** holds `WORDS`, the spelling of each digit, and
  is imported rather than restated.
- **`openplexus/surfaces.py`** does the quantising. Nothing here hashes.
- **No tokenizer anywhere**, and none is added: a tokenizer's vocabulary is
  learned from a corpus this project never saw, which is the imported artefact
  the design exists to avoid.
"""

from __future__ import annotations

import random
from dataclasses import dataclass

from openplexus.tasks.mnist import WORDS

#: The alphabet a corruption may draw from. Lowercase letters only, because a
#: substitution that could produce any byte would make a corrupted word land
#: nowhere near an uncorrupted one and the channel would be noise rather than a
#: noisy version of something.
ALPHABET = "abcdefghijklmnopqrstuvwxyz"

#: How many distinct ways one digit can be written here. Four, and they are
#: deliberately far apart in byte space — `three`, `THREE`, `Three`, `3` share
#: almost no bytes — because surfaces the hash cannot tell apart would make the
#: discovery this file exists to pose trivial.
FORMS = 4


def forms(digit: int) -> tuple[str, ...]:
    """Every way this digit gets written. One concept, several appearances."""
    if not 0 <= digit < len(WORDS):
        raise ValueError(f"no word for digit {digit}")
    word = WORDS[digit]
    return (word, word.upper(), word.capitalize(), str(digit))


@dataclass(frozen=True)
class Channel:
    """How unreliable the written channel is. Every field is a dial, not a fact.

    Attributes:
        silence: Occasions carrying no word at all. Nobody names everything they
            see, and a channel present on every occasion is the signature of a
            label rather than an observation.
        mistake: Occasions naming a DIFFERENT digit. The wrong word is emitted
            in full and flags itself in no way; only the counts can notice.
        corrupt: Renderings with one byte substituted. This is what makes the
            surface count larger than `FORMS`, so it is the dial that decides
            how hard the discovery is.
    """

    silence: float = 0.15
    mistake: float = 0.05
    corrupt: float = 0.30

    def __post_init__(self) -> None:
        for name in ("silence", "mistake", "corrupt"):
            value = getattr(self, name)
            if not 0.0 <= value <= 1.0:
                raise ValueError(f"{name} is a probability, got {value}")
        if self.silence + self.mistake > 1.0:
            raise ValueError(
                "silence and mistake together exceed every occasion, so the "
                "correct word could never be said")


def render(digit: int, rng: random.Random, *, corrupt: float = 0.0) -> bytes:
    """One written appearance of `digit`, as the bytes a sensor would deliver."""
    text = rng.choice(forms(digit))
    if corrupt and rng.random() < corrupt and text:
        at = rng.randrange(len(text))
        text = text[:at] + rng.choice(ALPHABET) + text[at + 1:]
    return text.encode("utf-8")


def speak(channel: Channel, digit: int, rng: random.Random,
          digits: int = len(WORDS)) -> tuple[int, bytes] | None:
    """What the written channel delivers on one occasion. `None` is silence.

    A mistake names another digit chosen uniformly, so a wrong word is a
    plausible word rather than gibberish — the confusion the counts have to
    survive is between two real concepts.

    Returns:
        `(named, bytes)`, where `named` is the digit actually written — which
        is NOT `digit` when the channel made a mistake. **`named` is for scoring
        only**, the same rule `mnist.Digits.labels` carries: a mechanism reading
        it is reading a label, which is the thing this module exists to remove.
    """
    draw = rng.random()
    if draw < channel.silence:
        return None
    if draw < channel.silence + channel.mistake and digits > 1:
        wrong = rng.randrange(digits - 1)
        digit = wrong if wrong < digit else wrong + 1
    return digit, render(digit, rng, corrupt=channel.corrupt)


def features(rendering: bytes, length: int = 256) -> list[float]:
    """A byte histogram. Order is discarded; see the module docstring.

    Returned as floats because it is about to be centred and projected, and an
    integer array would be silently promoted anyway.
    """
    counts = [0.0] * length
    for byte in rendering:
        counts[byte] += 1.0
    return counts
