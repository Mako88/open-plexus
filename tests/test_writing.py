"""Only a sentence with a pronoun is restated, with the story so far; a restatement is kept only
when it keeps the sentence's other words and adds nothing but names the story has used; the
rest of the rows are the sentences as heard, one a sentence in heard order."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.babi import Story  # noqa: E402
from openplexus.writing import RESTATE_SYSTEM, guarded, has_pronoun, names_in, restate_messages, restated  # noqa: E402


class Resolver:
    """A stand-in trunk: replaces 'he' with John and 'they' with John and Mary, recording the prompts."""

    def __init__(self):
        self.prompts = []

    def ask_many(self, prompts):
        self.prompts.extend(prompts)
        return [p[1]["content"].rsplit("Sentence: ", 1)[1].replace(" he ", " John ").replace(" they ", " John and Mary ") + "\n"
                for p in prompts]


def test_only_pronoun_sentences_are_sent_and_every_row_keeps_its_place():
    a = Story(task=11, index=0, sentences=["John journeyed to the hallway.", "After that he journeyed to the garden."])
    b = Story(task=13, index=1, sentences=["John and Mary went to the kitchen.", "Then they went to the bathroom.", "Mary moved to the office."])
    trunk = Resolver()
    rows = restated([a, b], trunk)
    assert rows == {"t11s0": ["John journeyed to the hallway.", "After that John journeyed to the garden."],
                    "t13s1": ["John and Mary went to the kitchen.", "Then John and Mary went to the bathroom.", "Mary moved to the office."]}
    assert len(trunk.prompts) == 2 and trunk.prompts[0][0] == {"role": "system", "content": RESTATE_SYSTEM}
    assert trunk.prompts[0][1]["content"] == "Story so far:\nJohn journeyed to the hallway.\n\nSentence: After that he journeyed to the garden."
    assert restate_messages([], "b.")[1]["content"] == "Story so far:\n(nothing yet)\n\nSentence: b."
    assert has_pronoun("Then they went home.") and not has_pronoun("Mary went home.")
    assert names_in(["After that John went to the garden.", "Then Mary and Daniel left."]) == {"John", "Mary", "Daniel"}


def test_a_restatement_that_loses_words_or_invents_names_is_refused():
    so_far = ["John journeyed to the hallway.", "Mary went to the kitchen."]
    s = "After that he journeyed to the garden."
    assert guarded(s, "After that John journeyed to the garden.\n", so_far) == "After that John journeyed to the garden."
    assert guarded(s, "John journeyed to the garden.", so_far) == s  # 'after that' lost
    assert guarded(s, "After that John journeyed to the hallway.", so_far) == s  # a place swapped
    assert guarded(s, "After that Sandra journeyed to the garden.", so_far) == s  # a name the story has not used
    assert guarded(s, "After that he journeyed to the garden.", so_far) == s  # nothing resolved
    assert guarded(s, "", so_far) == s
    assert guarded("Then they went home.", "Then John and Mary went home.", so_far) == "Then John and Mary went home."
