"""Nothing of one language held by hand, out of the red set once it held (THE ORDER,
the foundation's item 4). With the foundation's other parts, in `test_individuals.py`,
`test_kept_whole.py` and `test_kinds.py`, it is what the system stands on (John's,
2026-10-04)."""

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
