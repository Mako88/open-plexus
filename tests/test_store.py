"""The store is checked without a model: a fake embedder gives deterministic vectors, and the
properties under test are the table's, not the keys': appending is on disk at once, search
respects the episode and the as-of time, and a store opened again from disk answers the same.
"""

import numpy as np

from openplexus.store import Store


def fake(texts: list[str], dim: int = 8) -> np.ndarray:
    """One vector per text, a function of that text alone."""
    import zlib

    return np.stack([np.random.default_rng(zlib.crc32(t.encode())).normal(size=dim)
                     for t in texts]).astype(np.float32)


def test_append_then_search_scoped_by_episode_and_time(tmp_path):
    s = Store(tmp_path / "s")
    a = ["John went to the hallway.", "Mary went to the garden.", "John moved to the kitchen."]
    b = ["John went to the office.", "Sandra went to the bathroom."]
    ids_a = s.append(a, "a", {"f": fake(a)})
    ids_b = s.append(b, "b", {"f": fake(b)})
    assert ids_a == [0, 1, 2] and ids_b == [3, 4] and len(s) == 5
    q = fake(["John moved to the kitchen."])[0]
    assert s.search("f", q, 1)[0].row.text == a[2]
    assert all(h.row.episode == "b" for h in s.search("f", q, 5, episode="b"))
    assert [h.row.id for h in s.search("f", q, 5, before=2)] and all(
        h.row.id < 2 for h in s.search("f", q, 5, before=2))
    assert s.search("f", q, 5, episode="zzz") == []
    assert s.bytes_on_disk() > 0


def test_a_store_opened_again_answers_the_same(tmp_path):
    s = Store(tmp_path / "s")
    texts = [f"sentence {i}" for i in range(50)]
    s.append(texts[:30], "a", {"f": fake(texts[:30])})
    s.append(texts[30:], "b", {"f": fake(texts[30:])})
    queries = fake([f"query {i}" for i in range(20)])
    before = [[h.row.id for h in s.search("f", q, 5)] for q in queries]
    again = Store(tmp_path / "s")
    assert len(again) == 50 and again.embedders == ["f"]
    after = [[h.row.id for h in again.search("f", q, 5)] for q in queries]
    assert before == after
    assert [r.text for r in again.rows] == texts


def test_vectors_are_normalised_and_scores_are_cosines(tmp_path):
    s = Store(tmp_path / "s")
    v = np.array([[3.0, 4.0], [1.0, 0.0]], dtype=np.float32)
    s.append(["x", "y"], "e", {"f": v})
    hits = s.search("f", np.array([30.0, 40.0]), 2)
    assert hits[0].row.text == "x" and abs(hits[0].score - 1.0) < 1e-6
    assert abs(hits[1].score - 0.6) < 1e-6


def test_the_table_stays_rectangular(tmp_path):
    import pytest

    s = Store(tmp_path / "s")
    s.append(["x"], "e", {"f": fake(["x"])})
    with pytest.raises(ValueError):
        s.append(["y"], "e", {"g": fake(["y"])})
