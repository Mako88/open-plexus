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


def test_the_final_answer_is_the_last_assistant_turn_and_is_scored_by_exact_match():
    turns = [{"role": "assistant", "content": None, "tool_calls": [{"name": "search"}]},
             {"role": "tool", "content": "Ed Wood: American."},
             {"role": "assistant", "content": "Both American.\nAnswer: yes"}]
    assert final_text(turns) == "Both American.\nAnswer: yes"
    assert correct([turns, "Answer: no", "nothing"], ["yes", "yes", "yes"]) == [1.0, 0.0, 0.0]
    assert overlap(["Answer: the Eiffel Tower in Paris"], ["Eiffel Tower"])[0] == 0.6666666666666666
