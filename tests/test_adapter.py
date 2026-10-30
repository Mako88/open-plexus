"""The adapter's training turns come from candidates the rollouts labelled right, and the loss
falls on the reply alone."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
sys.path.insert(0, str(ROOT / "scripts"))

from trunk_adapter import encode, pairs  # noqa: E402

ROWS = [
    {"index": 0, "question": "Q0", "answer": "4", "steps": [], "candidate": "2 + 2 = 4.", "fraction": 1.0},
    {"index": 0, "question": "Q0", "answer": "4", "steps": [], "candidate": "2 + 2 = 5.", "fraction": 0.0},
    {"index": 0, "question": "Q0", "answer": "4", "steps": ["2 + 2 = 4."], "candidate": "Answer: 4", "fraction": 1.0},
    {"index": 0, "question": "Q0", "answer": "4", "steps": [], "candidate": "2 + 2 = 4.", "fraction": 1.0},
    {"index": 1, "question": "Q1", "answer": "9", "steps": [], "candidate": "3 * 3 = 9.", "fraction": 0.67},
]


def test_turns_are_the_right_candidates_once_each():
    got = pairs(ROWS)
    assert [(g["index"], g["reply"]) for g in got] == [(0, "2 + 2 = 4."), (0, "Answer: 4")]
    assert got[1]["messages"][1]["content"].endswith("Steps so far:\n1. 2 + 2 = 4.\nNext step:")
    assert [g["reply"] for g in pairs(ROWS, min_fraction=0.5)] == ["2 + 2 = 4.", "Answer: 4", "3 * 3 = 9."]


def test_the_loss_falls_on_the_reply_alone():
    from transformers import AutoTokenizer

    tok = AutoTokenizer.from_pretrained("Qwen/Qwen3.5-2B")
    ids, labels = encode(tok, pairs(ROWS)[0])
    assert len(ids) == len(labels)
    kept = [i for i, lab in zip(ids, labels) if lab != -100]
    assert kept == ids[-len(kept):] and tok.decode(kept).startswith("2 + 2 = 4.")
    assert kept[-1] == tok.eos_token_id and labels[0] == -100
