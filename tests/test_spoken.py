"""The spoken-digit reader, and the sampling trap that nearly ate a sweep."""

from __future__ import annotations

import pathlib
import tempfile
import unittest
import wave

from openplexus.tasks import mnist, spoken


def _write(folder: pathlib.Path, name: str, samples: list[int],
           channels: int = 1, width: int = 2, rate: int = 8000) -> pathlib.Path:
    path = folder / name
    with wave.open(str(path), "wb") as handle:
        handle.setnchannels(channels)
        handle.setsampwidth(width)
        handle.setframerate(rate)
        step = 256 ** width
        handle.writeframes(b"".join(
            (value % step).to_bytes(width, "little") for value in samples))
    return path


class TheWordsAgreeAcrossModalities(unittest.TestCase):
    """One word surface serves both the picture and the sound, so the two lists
    must be the same list.

    If this fails, `spoken` and `mnist` disagree about what digit 3 is called and
    a three-modality run silently uses TWO word surfaces per concept, which reads
    as the linking failing rather than as a naming bug.
    """

    def test_spoken_words_are_mnists_words(self):
        self.assertIs(spoken.WORDS, mnist.WORDS)
        self.assertEqual(len(spoken.WORDS), 10)


class TheFilenameCarriesTheLabel(unittest.TestCase):

    def test_digit_and_speaker_are_read_from_the_name(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            path = _write(root, "7_theo_12.wav", [0, 100, -100, 0])
            utterance = spoken.read(path)
        self.assertEqual(utterance.digit, 7)
        self.assertEqual(utterance.speaker, "theo")
        self.assertEqual(utterance.rate, 8000)
        self.assertEqual(utterance.samples, [0, 100, -100, 0])
        self.assertEqual(len(utterance), 4)

    def test_an_unrecognised_name_is_refused_rather_than_guessed(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            path = _write(root, "hello.wav", [0, 0])
            with self.assertRaises(ValueError):
                spoken.read(path)

    def test_a_non_numeric_leading_field_is_refused(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            path = _write(root, "seven_theo_1.wav", [0, 0])
            with self.assertRaises(ValueError):
                spoken.read(path)


class UnsupportedAudioIsRefusedNotReinterpreted(unittest.TestCase):
    """Reading stereo or 8-bit as mono 16-bit produces plausible-looking sound
    that is not the recording, and every feature computed from it is wrong while
    looking fine."""

    def test_stereo_is_refused(self):
        with tempfile.TemporaryDirectory() as folder:
            path = _write(pathlib.Path(folder), "1_a_0.wav", [0, 1, 2, 3],
                          channels=2)
            with self.assertRaises(ValueError):
                spoken.read(path)

    def test_eight_bit_is_refused(self):
        with tempfile.TemporaryDirectory() as folder:
            path = _write(pathlib.Path(folder), "1_a_0.wav", [0, 1, 2, 3],
                          width=1)
            with self.assertRaises(ValueError):
                spoken.read(path)


class APrefixIsNotASample(unittest.TestCase):
    """The trap that cost a probe: filenames begin with the digit, so sorting
    groups every `0_` together and a prefix of `available` is one digit.

    A first audio probe took the first 1,500 of 3,000 recordings, measured five
    digits, and reported a chance level of 0.20 instead of 0.10. **The chance
    level was the tell, not the purity** -- 0.7093 looked like a fine result.
    """

    def _fixture(self, folder: pathlib.Path) -> list[pathlib.Path]:
        for digit in range(4):
            for index in range(10):
                _write(folder, f"{digit}_sp{index % 2}_{index}.wav", [0, 1])
        return spoken.available(folder)

    def test_available_is_sorted_so_a_run_does_not_depend_on_the_filesystem(self):
        with tempfile.TemporaryDirectory() as folder:
            paths = self._fixture(pathlib.Path(folder))
        self.assertEqual(paths, sorted(paths))
        self.assertEqual(len(paths), 40)

    def test_a_prefix_of_available_is_biased_which_is_why_sample_exists(self):
        with tempfile.TemporaryDirectory() as folder:
            paths = self._fixture(pathlib.Path(folder))
            prefix = {spoken.read(p).digit for p in paths[:20]}
            drawn = {spoken.read(p).digit for p in
                     spoken.sample(paths, 20, seed=0)}
        # The prefix holds HALF the digits and the sample holds all of them.
        # Asserting only the sample would pass even if `available` were shuffled,
        # which would make the companion claim untested.
        self.assertEqual(prefix, {0, 1})
        self.assertEqual(drawn, {0, 1, 2, 3})

    def test_sample_is_deterministic_at_a_seed_and_moves_with_it(self):
        with tempfile.TemporaryDirectory() as folder:
            paths = self._fixture(pathlib.Path(folder))
            first = spoken.sample(paths, 12, seed=0)
            again = spoken.sample(paths, 12, seed=0)
            other = spoken.sample(paths, 12, seed=1)
        self.assertEqual(first, again)
        self.assertNotEqual(first, other)
        self.assertEqual(first, sorted(first))
        self.assertEqual(len(set(first)), 12)

    def test_asking_for_everything_returns_everything(self):
        with tempfile.TemporaryDirectory() as folder:
            paths = self._fixture(pathlib.Path(folder))
            self.assertEqual(spoken.sample(paths, 99, seed=0), paths)


class TheSpeakerIsAFreeAxis(unittest.TestCase):

    def test_speakers_are_unique_and_sorted(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            for name in ("3_theo_0.wav", "3_george_1.wav", "5_theo_2.wav"):
                _write(root, name, [0, 1])
            found = spoken.speakers(spoken.available(root))
        self.assertEqual(found, ["george", "theo"])


if __name__ == "__main__":
    unittest.main()
