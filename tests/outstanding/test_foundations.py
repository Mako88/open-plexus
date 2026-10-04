"""Red until the system stands on what it needs (John's, 2026-10-04): nothing of one
language held by hand, and kinds learnt from how words connect. THE ORDER's foundation
item (the three layers: labels, concepts, individuals) says what each is for. What
held moved to the guards: a thing apart from the words said of it
(`test_individuals.py`) and nothing the parse gives dropped (`test_kept_whole.py`)."""

import ast
from pathlib import Path

from unfused.graph import GraphArm

MODEL = "en_core_web_trf"  # what every run reads with, so a test is red for the system
SOURCE = Path(__file__).resolve().parents[2] / "src" / "unfused" / "graph.py"

# words of one language that rules in the system have named: pronouns, wh-words, the
# prepositions of place, refusals, negation, number words and the stand-in for a blank
ENGLISH = {
    "it", "they", "them", "its", "their", "he", "she", "him", "her", "his", "what", "who",
    "whom", "where", "which", "in", "on", "under", "into", "onto", "at", "behind", "near",
    "inside", "to", "over", "by", "from", "through", "no", "nope", "not", "something",
    "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
}


def arm(tmp_path):
    return GraphArm(tmp_path, model=MODEL, cache=None)


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


def test_kinds_are_learnt_from_how_words_connect(tmp_path):
    """The concept layer: two words are of a kind where they connect alike to things of
    alike kinds (THE ORDER, kinds). Colours fall together and things apart, from hearing
    alone, with no label naming either."""
    a = arm(tmp_path)
    for turn, text in enumerate([
            "Tom had a red ball.", "Ann had a blue ball.", "Tom had a red cup.",
            "Ann had a blue hat.", "The hat was green.", "The cup was red.",
            "Tom saw a green cup.", "Ann saw a red hat."]):
        a.hear(turn, text)
    assert hasattr(a, "kind_of"), "the graph learns no kinds"
    colours = {a.kind_of(w) for w in ("red", "blue", "green")}
    things = {a.kind_of(w) for w in ("ball", "cup", "hat")}
    assert len(colours) == 1 and len(things) == 1 and colours != things, (
        f"colours {colours}, things {things}")
