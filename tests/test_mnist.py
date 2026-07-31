"""The digit reader must not hand the answer over, and must not drift.

`mnist.py` is the ruler for `g36-01`, which is gate G7's first real test. Two
things would quietly invalidate it: labels drifting out of step with images, so
every score is against the wrong answer; and the reader reinterpreting a file it
does not understand instead of refusing.

The fetched-data tests skip when `data/mnist/` is absent, because CI has no fetch
step for it.
"""

from __future__ import annotations

import gzip
import pathlib
import tempfile
import unittest

from openplexus.tasks import mnist

DATA = pathlib.Path(__file__).resolve().parents[1] / "data" / "mnist"


def _idx(dims: list[int], payload: bytes) -> bytes:
    header = bytes([0, 0, 0x08, len(dims)])
    for size in dims:
        header += size.to_bytes(4, "big")
    return header + payload


def _folder(images: bytes, labels: bytes, shape: list[int],
            count: list[int]) -> pathlib.Path:
    folder = pathlib.Path(tempfile.mkdtemp())
    for name, blob in (("train-images-idx3-ubyte.gz", _idx(shape, images)),
                       ("train-labels-idx1-ubyte.gz", _idx(count, labels))):
        (folder / name).write_bytes(gzip.compress(blob))
    return folder


class Reading(unittest.TestCase):

    def test_images_and_labels_line_up(self):
        folder = _folder(bytes(range(8)), bytes([3, 7]), [2, 2, 2], [2])
        digits = mnist.read(folder)
        self.assertEqual(len(digits), 2)
        self.assertEqual(digits.labels, [3, 7])
        self.assertEqual(digits.images[0], bytes([0, 1, 2, 3]))
        self.assertEqual(digits.images[1], bytes([4, 5, 6, 7]))

    def test_a_limit_takes_a_prefix_and_keeps_them_paired(self):
        folder = _folder(bytes(range(12)), bytes([1, 2, 3]), [3, 2, 2], [3])
        digits = mnist.read(folder, limit=2)
        self.assertEqual(digits.labels, [1, 2])
        self.assertEqual(len(digits.images), 2)

    def test_a_mismatched_count_is_refused(self):
        """The failure that would score every image against the wrong answer."""
        with self.assertRaises(ValueError):
            mnist.Digits(2, 2, [b"abcd"], [1, 2])

    def test_a_file_that_is_not_IDX_is_refused(self):
        folder = pathlib.Path(tempfile.mkdtemp())
        for name in ("train-images-idx3-ubyte.gz", "train-labels-idx1-ubyte.gz"):
            (folder / name).write_bytes(gzip.compress(b"not an idx file"))
        with self.assertRaises(ValueError):
            mnist.read(folder)

    def test_a_type_this_reader_does_not_handle_is_refused(self):
        """Reinterpreting floats as bytes would produce plausible pixels."""
        folder = pathlib.Path(tempfile.mkdtemp())
        header = bytes([0, 0, 0x0D, 1]) + (1).to_bytes(4, "big")
        for name in ("train-images-idx3-ubyte.gz", "train-labels-idx1-ubyte.gz"):
            (folder / name).write_bytes(gzip.compress(header + b"\x00" * 4))
        with self.assertRaises(ValueError):
            mnist.read(folder)

    def test_there_is_a_word_for_every_digit(self):
        self.assertEqual(len(mnist.WORDS), 10)
        self.assertEqual(len(set(mnist.WORDS)), 10)


class TheFetchedDigits(unittest.TestCase):

    def setUp(self) -> None:
        if not (DATA / "train-images-idx3-ubyte.gz").exists():
            self.skipTest("run tools/fetch_mnist.py first")
        self.digits = mnist.read(DATA, limit=500)

    def test_it_is_28_by_28(self):
        self.assertEqual((self.digits.rows, self.digits.cols), (28, 28))
        self.assertEqual(self.digits.pixels, 784)

    def test_every_label_is_a_digit(self):
        self.assertTrue(set(self.digits.labels) <= set(range(10)))

    def test_every_image_is_the_right_length(self):
        for image in self.digits.images:
            self.assertEqual(len(image), self.digits.pixels)

    def test_the_images_are_not_all_the_same(self):
        """A reader with a stride bug returns the same bytes every time and
        every downstream purity would be measuring one image."""
        self.assertGreater(len({bytes(i) for i in self.digits.images}), 400)

    def test_reading_twice_gives_the_same_thing(self):
        self.assertEqual(mnist.read(DATA, limit=500).labels, self.digits.labels)
