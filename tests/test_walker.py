"""The walker's closed form is checked without a trunk: it recovers a planted linear map, its
query ranks the planted target first, and a saved walker loads back the same."""

import numpy as np

from openplexus.walker import LinearWalker, hop_prompt


def test_ridge_recovers_a_planted_map_and_round_trips(tmp_path):
    rng = np.random.default_rng(0)
    true = rng.normal(size=(32, 8)).astype(np.float32)
    h = rng.normal(size=(500, 32)).astype(np.float32)
    keys = h @ true + 0.01 * rng.normal(size=(500, 8)).astype(np.float32)
    w = LinearWalker.fit(h, keys, lam=1e-3, layer=12, key="minilm")
    assert np.allclose(w.weight, true, atol=0.05)
    w.save(tmp_path / "w.npz")
    again = LinearWalker.load(tmp_path / "w.npz")
    assert again.layer == 12 and again.key == "minilm" and np.array_equal(again.weight, w.weight)
    q = again.query(h[:1])[0]
    candidates = keys[:50] / np.linalg.norm(keys[:50], axis=1, keepdims=True)  # cosines
    assert int(np.argmax(candidates @ q)) == 0


def test_the_hop_prompt_carries_the_question_and_what_was_read():
    assert hop_prompt("Where is the milk?", []) == "Question: Where is the milk?\nNext fact:"
    p = hop_prompt("Where is the milk?", ["Sandra put down the milk there."])
    assert p == "Question: Where is the milk?\nKnown: Sandra put down the milk there.\nNext fact:"
