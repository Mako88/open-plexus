"""Spoken digits, read from WAV with nothing but the standard library.

The third modality. `mnist.py` gives pictures and their names; this gives the
same ten concepts SPOKEN, so a concept can have an image, a sound and a word —
which is `GOALS.md` §1.2b's own example, stated there as *"an image of a dog, a
video of a dog, a sound of a dog and the word dog"*.

## Why this costs almost nothing to add, which is the interesting part

The mechanism does not know how many modalities exist or what any of them are. A
modality is a surface with a stable id, and everything downstream — the counting,
the walk, the derived bound, the sharding, the containers — is modality-blind by
construction.

So a third modality needs exactly two new things: a way to read the file, and a
way to turn it into a discrete id. **Neither is in this module's downstream
path.** `wave` is standard library, so the ruler stays dependency-free, and the
quantiser is a model-layer decision that reuses `grouping.cluster` exactly as the
images do.

## What this does NOT do

**No features and no quantiser.** `frames` returns samples, not a spectrum.
Turning sound into a code is numpy work and belongs where the image quantiser
lives — putting it here would make the ruler depend on the mechanism it measures,
which is the refusal `occasions.py` and `mnist.py` both make.

**No labels in the stream.** The digit and the speaker come from the filename and
exist to SCORE a grouping, never to produce one.

## The speaker, which is a free axis and worth naming

A filename is `{digit}_{speaker}_{index}.wav`, so every recording carries WHO
said it. That makes *"the same word in a different voice"* directly measurable —
the audio equivalent of two different pictures of one digit, and the
within-modality agreement problem `GOALS.md` insists is separate from alignment
across modalities.

## What this does NOT duplicate, and what was searched

Searched by capability — wav, audio, sound, sample, spectrum — across
`openplexus/`, `tools/`, `tests/` and `experiments/`. Nothing in this project
reads audio.

- **`openplexus/tasks/mnist.py`** is the sibling: same shape, different medium,
  no shared parsing because IDX and WAV have nothing in common.
- **`openplexus/tasks/xsl.py`** reads external data that is already symbolic.
- **`tools/fetch_fsdd.py`** does the fetching; this opens what is on disk.
"""

from __future__ import annotations

import array
import pathlib
import wave

#: The word for each digit, matching `mnist.WORDS` so one word surface can serve
#: both modalities. **They must agree**, and `test_spoken` asserts it rather than
#: trusting two lists to stay in step.
from openplexus.tasks.mnist import WORDS  # noqa: F401


class Utterance:
    """One recording: what was said, who said it, and the samples.

    Attributes:
        digit: 0-9, from the filename. **For scoring only.**
        speaker: Who said it, from the filename. For scoring and for splitting.
        rate: Samples per second.
        samples: Signed 16-bit mono samples as a plain list of ints.
    """

    def __init__(self, digit: int, speaker: str, rate: int,
                 samples: list[int]) -> None:
        self.digit = digit
        self.speaker = speaker
        self.rate = rate
        self.samples = samples

    def __len__(self) -> int:
        return len(self.samples)


def _label(name: str) -> tuple[int, str]:
    parts = name.removesuffix(".wav").split("_")
    if len(parts) != 3 or not parts[0].isdigit():
        raise ValueError(
            f"{name} is not `digit_speaker_index.wav`. Guessing at a filename "
            f"this reader does not recognise would attach the wrong label to a "
            f"recording, and every score would be against the wrong answer")
    return int(parts[0]), parts[1]


def read(path: pathlib.Path) -> Utterance:
    """Read one recording. Mono 16-bit only; anything else is refused."""
    digit, speaker = _label(path.name)
    with wave.open(str(path), "rb") as handle:
        if handle.getnchannels() != 1 or handle.getsampwidth() != 2:
            raise ValueError(
                f"{path.name} is {handle.getnchannels()} channel(s) at "
                f"{handle.getsampwidth() * 8} bits. This reader handles mono "
                f"16-bit and refuses the rest rather than reinterpreting the "
                f"bytes into plausible-looking sound")
        rate = handle.getframerate()
        raw = handle.readframes(handle.getnframes())
    samples = array.array("h")
    samples.frombytes(raw)
    return Utterance(digit, speaker, rate, list(samples))


def available(root: pathlib.Path) -> list[pathlib.Path]:
    """Every fetched recording, in a fixed order.

    Sorted rather than taken as the filesystem lists them, because a directory
    order is not stable across machines and a sweep whose rows depend on it
    reports a different table elsewhere.

    **A PREFIX OF THIS IS NOT A SAMPLE, and that differs from `mnist.read`.**
    Filenames begin with the digit, so sorting groups every `0_` together: the
    first 1,500 of 3,000 recordings are digits 0 to 4 and nothing else. A probe
    that took a prefix here measured five digits and reported a chance level of
    0.20 instead of 0.10 — which is the tell, and the only reason it was caught.

    `mnist.read` takes a prefix deliberately and says so, because that file is
    already shuffled. **This one is not.** Use `sample` unless every recording is
    being read.
    """
    return sorted(root.glob("*.wav"))


def sample(paths: list[pathlib.Path], count: int,
           seed: int = 0) -> list[pathlib.Path]:
    """`count` recordings drawn without replacement, deterministically.

    Exists because a prefix of `available` is all one digit — see above. Seeded
    so two runs at the same seed read the same recordings, and sorted afterwards
    so the ORDER does not depend on the draw, only the membership.
    """
    import random

    if count >= len(paths):
        return list(paths)
    return sorted(random.Random(seed).sample(paths, count))


def speakers(paths: list[pathlib.Path]) -> list[str]:
    """Who is in this set, sorted. The axis a held-out voice would use."""
    return sorted({_label(p.name)[1] for p in paths})
