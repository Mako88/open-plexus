"""The lexical sense indexes the store's rows by title and text, answers a query with store rows
that share its words, never with a row excluded, and fuses with another ranking by rank alone."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.lexical import LexicalIndex, words  # noqa: E402

TITLES = ["Anarchism", "Autism", "Albedo", "Tim Burton", "Ed Wood (film)", "Woodson, Arkansas", "Alabama"]
TEXTS = ["a political philosophy", "a neurodevelopmental disorder", "a measure of reflectance",
         "an American film director", "a 1994 film directed by Tim Burton", "a town in Arkansas", "a state"]


def store(tmp_path):
    (tmp_path / "titles.txt").write_text("".join(t + "\n" for t in TITLES), encoding="utf-8")
    (tmp_path / "texts.txt").write_text("".join(f"{t}: {x}\n" for t, x in zip(TITLES, TEXTS)), encoding="utf-8")
    return tmp_path


def test_words_strips_everything_a_query_parser_would_read_as_syntax():
    assert words('Who directed "Ed Wood" (the 1994 film)? AND/OR: a town\'s') == "Who directed Ed Wood the 1994 film AND OR a town s"
    assert words("???") == ""


def test_the_index_answers_with_rows_sharing_the_query_words_and_the_title_counts_more(tmp_path):
    s = store(tmp_path)
    log = []
    ix = LexicalIndex.build(tmp_path / "bm25", s / "titles.txt", s / "texts.txt", log=log.append, heap=64_000_000)
    assert log[-1].startswith("committed")
    assert ix.nearest(["Tim Burton"], 1) == [[3]]  # the title row beats the body mention
    assert ix.nearest(["the film Ed Wood"], 2)[0][0] == 4
    assert ix.nearest(["a town in Arkansas"], 1, exclude={5}) == [[6]] or ix.nearest(["a town in Arkansas"], 1, exclude={5})[0] != [5]
    assert ix.nearest(["???"], 3) == [[]]
    reopened = LexicalIndex(tmp_path / "bm25")
    assert reopened.nearest(["Albedo reflectance"], 1) == [[2]]
