"""The path scorer is ordered and reads letters or words; every gold path scores against
itself; the step prompt names the candidate move and the moves so far."""

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.paths import moves_in, path_correct, step_prompt  # noqa: E402


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
