"""The asking arm's pieces that run without the card: the store a rollout searches, the final
answer out of a tool-calling completion, and the reward on it."""

import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
sys.path.insert(0, str(ROOT / "scripts"))

import ask  # noqa: E402
from ask import Store, correct, final_text, overlap  # noqa: E402


def stub_embed(texts):  # ducks one way, films the other
    return np.array([[1.0, 0.0] if "duck" in t.lower() else [0.0, 1.0] for t in texts], dtype=np.float32)


def test_the_store_returns_the_nearest_paragraph_and_never_the_same_one_twice(monkeypatch):
    monkeypatch.setattr(ask, "embedder", lambda: stub_embed)
    store = Store()
    assert store.reset(question="q", answer="a", paragraphs=[
        {"text": "Donald Duck: a duck.", "supporting": True},
        {"text": "Ed Wood: a film director.", "supporting": True}]) is None
    assert store.search("which duck") == "Donald Duck: a duck."
    assert store.search("another duck") == "Ed Wood: a film director."
    assert store.searches == ["which duck", "another duck"]


def test_the_wiki_environment_searches_the_whole_store_by_bm25_and_resets_per_rollout(tmp_path):
    from test_lexical import store as lexical_store
    from openplexus.lexical import LexicalIndex
    s = lexical_store(tmp_path)
    (s / "offsets.npy").write_bytes(b"")  # replaced below: WikiTexts wants offsets and progress beside the texts
    import json as _json
    import numpy as np
    lines = (s / "texts.txt").read_bytes().split(b"\n")[:-1]
    offsets = np.zeros(len(lines) + 1, dtype=np.int64)
    for i, b in enumerate(lines):
        offsets[i + 1] = offsets[i] + len(b) + 1
    np.save(s / "offsets.npy", offsets)
    (s / "progress.json").write_text(_json.dumps({"n": len(lines), "dim": 384, "shard": len(lines), "done": 1}), encoding="utf-8")
    LexicalIndex.build(s / "bm25", s / "titles.txt", s / "texts.txt", log=lambda _: None, heap=64_000_000)
    ask.WikiEnv.open(str(s), str(s / "bm25"))
    env = ask.WikiEnv()
    env.reset(answer="Tim Burton")
    assert env.search("who directed the film Ed Wood").startswith("Ed Wood (film): ")
    assert env.search("Tim Burton").startswith("Tim Burton: ")
    assert env.searches == ["who directed the film Ed Wood", "Tim Burton"] and len(env.shown) == 2
    env.reset()
    assert env.shown == [] and env.search("Tim Burton").startswith("Tim Burton: ")  # shown again after a reset
    assert ask.WikiEnv().shown == []  # each rollout its own
    # TRL offers every public function of the class as a tool, and a tool needs a docstring: search is the one
    import inspect
    public = [n for n, _ in inspect.getmembers(ask.WikiEnv, predicate=inspect.isfunction) if not n.startswith("_")]
    assert public == ["reset", "search"], public


def test_the_final_answer_is_the_last_assistant_turn_and_is_scored_by_exact_match():
    turns = [{"role": "assistant", "content": None, "tool_calls": [{"name": "search"}]},
             {"role": "tool", "content": "Ed Wood: American."},
             {"role": "assistant", "content": "Both American.\nAnswer: yes"}]
    assert final_text(turns) == "Both American.\nAnswer: yes"
    assert correct([turns, "Answer: no", "nothing"], ["yes", "yes", "yes"]) == [1.0, 0.0, 0.0]
    assert overlap(["Answer: the Eiffel Tower in Paris"], ["Eiffel Tower"])[0] == 0.6666666666666666
