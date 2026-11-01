"""The asking loop at inference: tool calls are answered from the store and the trunk's final
text is the reply; a trunk that never asks gets one turn; the cap ends a trunk that only asks."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.asking import query_of, solve  # noqa: E402


def test_two_searches_then_an_answer():
    script = iter([
        {"content": "", "tool_calls": [{"id": "c1", "function": {"name": "search", "arguments": '{"query": "Ed Wood"}'}}]},
        {"content": None, "tool_calls": [{"id": "c2", "function": {"name": "search", "arguments": {"query": "Scott Derrickson"}}}]},
        {"content": "Both American.\nAnswer: yes"},
    ])
    seen = []

    def chat(messages, tools):
        assert tools[0]["function"]["name"] == "search"
        seen.append(len(messages))
        return next(script)

    out = solve(chat, "sys", "Same nationality?", lambda q: f"[{q}]: American.", max_turns=4)
    assert out["reply"] == "Both American.\nAnswer: yes"
    assert out["searches"] == ["Ed Wood", "Scott Derrickson"] and out["turns"] == 3
    assert [m["role"] for m in out["messages"]] == ["system", "user", "assistant", "tool", "assistant", "tool", "assistant"]
    assert out["messages"][3]["content"] == "[Ed Wood]: American."
    assert seen == [2, 4, 6]


def test_a_trunk_that_never_asks_and_one_that_only_asks():
    assert solve(lambda m, t: {"content": "Answer: no"}, "s", "q", lambda q: "", max_turns=4)["searches"] == []
    only = solve(lambda m, t: {"content": "", "tool_calls": [{"function": {"name": "search", "arguments": "{\"query\": \"x\"}"}}]},
                 "s", "q", lambda q: "p", max_turns=3)
    assert only["turns"] == 3 and only["searches"] == ["x", "x"] and only["reply"] == ""
    assert query_of({"function": {"arguments": "not json"}}) == "not json"
