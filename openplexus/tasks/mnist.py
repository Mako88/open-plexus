"""Handwritten digits, read from the IDX files, with no dependencies.

The ruler stays dependency-free (`CLAUDE.md`), and IDX is simple enough that it
costs nothing to keep it that way: a small header of big-endian 32-bit integers,
then the pixels as raw bytes.

## What this is FOR, and it is one specific gap

Every grounding measurement in `g32`–`g35` ran on a symbol stream this project
generated, where a "modality" is an integer and two integers either match or do
not. **That cannot test `GOALS.md` gate G7**, because the hard half never arises:
there is no question of whether two different pictures of the same thing are
recognised as the same thing.

Here there is. One concept — the digit three — has **one word and hundreds of
different pictures**, none of them identical. `GOALS.md` §1.2b names this as
`agreement WITHIN a modality` and says explicitly that it must not be budgeted
together with `alignment ACROSS modalities`. This is the first data in the
project where the two can be measured apart.

**Digits are not claimed to be interesting.** They are the cheapest real sensory
input with unambiguous ground truth.

## What this does NOT do, deliberately

**No quantiser.** Turning pixels into a discrete code is a model-layer decision
using numpy, and `openplexus/grouping.py` already has spherical k-means for
exactly that shape. Putting it here would make the ruler depend on the mechanism
it measures, and `occasions.py` makes the same refusal for the same reason.

**No labels in the stream.** `labels` exists to SCORE a grouping, never to
produce one. A reader that fed them to the mechanism would be measuring
supervised classification.

## What this does NOT duplicate, and what was searched

Searched by capability — image, pixel, idx, digit, corpus reader — across
`openplexus/`, `openplexus/tasks/`, `tools/` and `tests/`. Nothing in this
project reads an image; `concepts.py` mentions one only in a docstring example.

- **`openplexus/tasks/corpus.py`** reads external text for a next-token
  objective. Same role, different medium, and no shared parsing.
- **`openplexus/tasks/xsl.py`** reads external trials that are already symbolic.
  This one is the case where they are not.
- **`openplexus/tasks/occasions.py`** GENERATES a stream; this reads one.
- **`tools/fetch_mnist.py`** does the fetching and this does no I/O beyond
  opening what is already on disk.
"""

from __future__ import annotations

import gzip
import pathlib

#: The word for each digit, in order. These become the second modality's
#: surfaces — one token per concept, against many images.
WORDS = ("zero", "one", "two", "three", "four",
         "five", "six", "seven", "eight", "nine")


class Digits:
    """Images and their labels, as flat byte rows.

    Attributes:
        rows: Image height in pixels.
        cols: Image width in pixels.
        images: One `bytes` of length `rows * cols` per image, greyscale 0-255.
        labels: The digit each image shows. **For scoring only.**
    """

    def __init__(self, rows: int, cols: int, images: list[bytes],
                 labels: list[int]) -> None:
        if len(images) != len(labels):
            raise ValueError(
                f"{len(images)} images against {len(labels)} labels — a reader "
                f"that let those drift would score every image against the "
                f"wrong answer and report it as a weak grouping")
        self.rows = rows
        self.cols = cols
        self.images = images
        self.labels = labels

    def __len__(self) -> int:
        return len(self.images)

    @property
    def pixels(self) -> int:
        return self.rows * self.cols


def _read_idx(path: pathlib.Path) -> tuple[list[int], bytes]:
    """Return an IDX file's dimensions and its raw payload.

    The header is a 4-byte magic — two zero bytes, a type byte, then the number
    of dimensions — followed by that many big-endian 32-bit lengths.
    """
    with gzip.open(path, "rb") as handle:
        blob = handle.read()
    if len(blob) < 4 or blob[0] != 0 or blob[1] != 0:
        raise ValueError(f"{path.name} does not start with an IDX magic number")
    if blob[2] != 0x08:
        raise ValueError(
            f"{path.name} holds type 0x{blob[2]:02x}, not unsigned bytes. This "
            f"reader handles the MNIST files and refuses anything else rather "
            f"than reinterpreting the payload")
    count = blob[3]
    sizes = [int.from_bytes(blob[4 + 4 * i:8 + 4 * i], "big")
             for i in range(count)]
    return sizes, blob[4 + 4 * count:]


def read(folder: pathlib.Path, limit: int | None = None) -> Digits:
    """Read the training split from `folder`, optionally taking the first `limit`.

    `limit` takes a PREFIX rather than a sample, so two runs at the same limit
    read the same images without a seed — and the file is already shuffled, so a
    prefix is not a biased draw. Anything that needed an unbiased sample of a
    sorted file would have to say so.
    """
    shape, pixels = _read_idx(folder / "train-images-idx3-ubyte.gz")
    labelled, labels = _read_idx(folder / "train-labels-idx1-ubyte.gz")
    if len(shape) != 3:
        raise ValueError(f"expected 3 image dimensions, got {shape}")
    total, rows, cols = shape
    if labelled != [total]:
        raise ValueError(f"{total} images against {labelled} labels")

    take = total if limit is None else min(limit, total)
    stride = rows * cols
    images = [pixels[i * stride:(i + 1) * stride] for i in range(take)]
    return Digits(rows, cols, images, list(labels[:take]))
