"""The scorer is checked without a model: every gold answer in the data must score against
itself, a sentence carrying the answer word must score, and a wrong word must not."""

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "scripts"))

from ceiling import correct  # noqa: E402


def test_gold_scores_against_itself_for_every_row():
    with open(ROOT / "data" / "babi" / "babi_test.jsonl", encoding="utf-8") as f:
        for line in f:
            r = json.loads(line)
            assert correct(r["answer"], r["answer"]), r


def test_a_sentence_carrying_the_answer_scores():
    assert correct("hallway", "John is in the hallway.")
    assert correct("hallway", "Hallway")


def test_a_wrong_word_or_nothing_does_not_score():
    assert not correct("hallway", "kitchen")
    assert not correct("hallway", "")
    assert not correct("milk,apple", "milk")


def test_number_words_and_digits_score_alike():
    assert correct("one", "1") and correct("none", "0") and correct("two", "She has 2.")
    assert not correct("one", "2") and not correct("none", "1")
