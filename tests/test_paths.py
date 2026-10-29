"""The path scorer is ordered and reads letters or words; every gold path scores against
itself; the step prompt names the candidate move and the moves so far."""

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.babi import stories  # noqa: E402
from openplexus.paths import (edge_of, moves_in, parse_question, path_correct, shortest_path,  # noqa: E402
                              step_prompt)


def test_the_scorer_is_ordered_and_reads_words_or_letters():
    assert path_correct("n,e", "n,e")
    assert path_correct("n,e", "North, then east.")
    assert path_correct("n,e", "Go N then E")
    assert not path_correct("n,e", "e,n")
    assert not path_correct("n,e", "n")
    assert not path_correct("n,e", "n,e,s")
    assert not path_correct("n,e", "")
    assert moves_in("The answer is: south, south") == ["s", "s"]


def test_every_gold_path_scores_against_itself():
    with open(ROOT / "data" / "babi" / "babi_test.jsonl", encoding="utf-8") as f:
        golds = [json.loads(line)["answer"] for line in f if '"task": 19' in line]
    assert golds, "task 19 is in the data"
    assert all(path_correct(g, g) and len(moves_in(g)) == 2 for g in golds)


def test_the_step_prompt_names_the_moves():
    p = step_prompt(["The garden is west of the bathroom."], "How do you go from a to b?", ["n"], "e")
    assert "Moves so far: north" in p and "Next move: east" in p and p.endswith("right:")
    assert "none yet" in step_prompt([], "q", [], "s")


def test_an_edge_reads_from_the_sentence_or_the_restatement():
    assert edge_of("The garden is west of the bathroom.") == ("garden", "w", "bathroom")
    assert edge_of("garden|west|bathroom") == ("garden", "w", "bathroom")
    assert edge_of(" Garden | W | Bathroom. ") == ("garden", "w", "bathroom")
    assert edge_of("I do not know") is None
    assert parse_question("How do you go from the bathroom to the hallway?") == ("bathroom", "hallway")


def test_the_map_walked_from_the_true_edges_gives_the_gold_path_on_every_test_story():
    """The tool's ceiling: with every edge read right, the shortest path is the gold path. A
    story where it is not would mean two shortest paths, and the reading would show it."""
    misses = []
    for s in stories([19]):
        q = s.questions[0]
        edges = [edge_of(x) for x in s.sentences]
        path = shortest_path(edges, *parse_question(q.text))
        if not path or not path_correct(q.answer, ",".join(path)):
            misses.append((s.episode, q.answer, path))
    assert not misses, misses[:5]
