"""The walker's closed form is checked without a trunk: it recovers a planted linear map, its
query ranks the planted target first, and a saved walker loads back the same."""

import numpy as np

from openplexus.heads import LinearHead, hop_prompt, judge_prompt


def test_ridge_recovers_a_planted_map_and_round_trips(tmp_path):
    rng = np.random.default_rng(0)
    true = rng.normal(size=(32, 8)).astype(np.float32)
    h = rng.normal(size=(500, 32)).astype(np.float32)
    keys = h @ true + 0.01 * rng.normal(size=(500, 8)).astype(np.float32)
    w = LinearHead.fit(h, keys, lam=1e-3, layer=12, key="minilm")
    assert np.allclose(w.weight, true, atol=0.05)
    w.save(tmp_path / "w.npz")
    again = LinearHead.load(tmp_path / "w.npz")
    assert again.layer == 12 and again.key == "minilm" and np.array_equal(again.weight, w.weight)
    q = again.query(h[:1])[0]
    candidates = keys[:50] / np.linalg.norm(keys[:50], axis=1, keepdims=True)  # cosines
    assert int(np.argmax(candidates @ q)) == 0


def test_the_hop_prompt_carries_the_question_and_what_was_read():
    assert hop_prompt("Where is the milk?", []) == "Question: Where is the milk?\nNext fact:"
    p = hop_prompt("Where is the milk?", ["Sandra put down the milk there."])
    assert p == "Question: Where is the milk?\nKnown: Sandra put down the milk there.\nNext fact:"


def test_the_judge_prompt_adds_the_candidate_and_a_label_head_keeps_its_threshold(tmp_path):
    j = judge_prompt("Where is the milk?", ["Sandra put down the milk there."], "Sandra went home.")
    assert j == ("Question: Where is the milk?\nKnown: Sandra put down the milk there.\n"
                 "Next fact: Sandra went home.\nIs the next fact right:")
    w = LinearHead(np.zeros((4, 1), dtype=np.float32), layer=1, lam=1.0, key="label", threshold=0.3)
    w.save(tmp_path / "e.npz")
    assert LinearHead.load(tmp_path / "e.npz").threshold == 0.3
    LinearHead(np.zeros((4, 1), dtype=np.float32), 1, 1.0, "minilm").save(tmp_path / "w.npz")
    assert LinearHead.load(tmp_path / "w.npz").threshold is None


def test_a_standardised_head_applies_its_mean_and_std_after_loading(tmp_path):
    from openplexus.heads import when_heard

    rng = np.random.default_rng(1)
    x = rng.normal(size=(200, 6)).astype(np.float32) * 10 + 5
    y = (x[:, :1] > 5).astype(np.float32) * 2 - 1
    mean, std = x.mean(0), x.std(0)
    w = LinearHead.fit((x - mean) / std, y, 1.0, layer=16, key="label+time")
    w.mean, w.std = mean, std
    w.save(tmp_path / "e.npz")
    again = LinearHead.load(tmp_path / "e.npz")
    assert again.key == "label+time" and np.array_equal(again.query(x), w.query(x))
    assert np.mean((again.query(x)[:, 0] > 0) == (y[:, 0] > 0)) > 0.9
    assert when_heard(2, 10, -1) == [0.2, 0.0, 0.0, 0.0]
    assert when_heard(2, 10, 6) == [0.2, -0.4, 1.0, 0.0]


def test_an_mlp_head_learns_a_listwise_choice_and_round_trips(tmp_path):
    import torch

    from openplexus.heads import MLPHead, load_head

    rng = np.random.default_rng(2)
    x = rng.normal(size=(64, 4, 6)).astype(np.float32)
    target = np.array([int(np.argmax(x[i, :, 0])) for i in range(64)])  # the first feature wins
    head = MLPHead(6, layer=16, key="label", hidden=32)
    opt = torch.optim.Adam(head.parameters(), lr=1e-2)
    xt, yt = torch.as_tensor(x), torch.as_tensor(target)
    for _ in range(300):
        logits = torch.cat([head.logits(xt), head.stop.expand(64, 1)], dim=1)
        loss = torch.nn.functional.cross_entropy(logits, yt)
        opt.zero_grad()
        loss.backward()
        opt.step()
    scores = head.query(x.reshape(-1, 6)).reshape(64, 4)
    assert np.mean(scores.argmax(1) == target) > 0.9
    head.save(tmp_path / "e.pt")
    again = load_head(tmp_path / "e.pt")
    assert again.threshold == 0.0
    assert np.allclose(again.query(x.reshape(-1, 6)), scores.reshape(-1, 1))
