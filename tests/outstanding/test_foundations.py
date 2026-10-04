"""Red until the system stands on what it needs (John's, 2026-10-04): nothing of one
language held by hand. THE ORDER's foundation item (the three layers: labels, concepts,
individuals) says what each is for. What held moved to the guards: a thing apart from
the words said of it (`test_individuals.py`), nothing the parse gives dropped
(`test_kept_whole.py`), and kinds learnt from how words connect (`test_kinds.py`)."""

import ast
from pathlib import Path

SOURCE = Path(__file__).resolve().parents[2] / "src" / "unfused" / "graph.py"

# words of one language that rules in the system have named: pronouns, wh-words, the
# prepositions of place, refusals, negation, number words and the stand-in for a blank
ENGLISH = {
    "it", "they", "them", "its", "their", "he", "she", "him", "her", "his", "what", "who",
    "whom", "where", "which", "in", "on", "under", "into", "onto", "at", "behind", "near",
    "inside", "to", "over", "by", "from", "through", "no", "nope", "not", "something",
    "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
}


def test_no_word_of_one_language_is_held_by_hand():
    """A rule for an English word does not carry to another language, nor to the next
    English word (John's, 2026-10-01 and 2026-10-04). What a word does is read from what
    the parser marks in any language (a relation, a feature such as definiteness), or
    learnt. Capital letters are a mark of some scripts only."""
    held = sorted({node.value for node in ast.walk(ast.parse(SOURCE.read_text("utf-8")))
                   if isinstance(node, ast.Constant) and isinstance(node.value, str)
                   and node.value.lower() in ENGLISH})
    capitals = SOURCE.read_text("utf-8").count(".isupper()")
    assert not held and not capitals, (
        f"graph.py names English words {held} and reads capitals {capitals} times")
