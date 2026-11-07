"""Experience rows are numbered after the store's own, found by the words they share with the query,
and handed to the reranker beside the store's candidates."""

import sys
from pathlib import Path
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.experience import Experience  # noqa: E402


def test_experience_rows_sit_after_the_store_and_join_its_candidates():
    base = SimpleNamespace(n=3, text=lambda i: ["Anarchism: a philosophy", "Albedo: reflectance", "Alabama: a state"][i],
                           title=lambda i: ["Anarchism", "Albedo", "Alabama"][i])
    exp = Experience(base, [{"title": "chain: Kiss and Tell -> Shirley Temple", "text": "Kiss and Tell -> Shirley Temple: she starred in it"}])
    assert exp.n == 4 and exp.text(3).startswith("Kiss and Tell") and exp.title(1) == "Albedo"
    assert exp.candidates("Shirley Temple film", 5, set()) == [3] and exp.candidates("Albedo", 5, set()) == []
    assert exp.candidates("Shirley Temple", 5, {3}) == []
    search = exp.over(lambda qs, k, ex: [[1, 0]])
    assert search(["who starred in Kiss and Tell"], 2, set()) == [[1, 0, 3]]
