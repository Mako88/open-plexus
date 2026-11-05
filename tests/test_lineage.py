"""The lineage starts from an adapter, every committed round replaces it and keeps the one before,
a rollback restores it, and the ledger prices a round where its readings give both scores."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.lineage import Lineage  # noqa: E402


def adapter(path: Path, mark: str) -> Path:
    path.mkdir(parents=True)
    (path / "adapter_model.bin").write_text(mark, encoding="utf-8")
    return path


def test_init_commit_rollback_and_the_ledger(tmp_path):
    lin = Lineage(tmp_path / "system")
    assert not lin.exists() and lin.current() is None and lin.ledger() == []
    root = adapter(tmp_path / "round2", "round two")
    gguf = tmp_path / "round2.gguf"
    gguf.write_bytes(b"gg")
    entry = lin.init(root, "the asking, round two", note="the best skill read so far", gguf=gguf)
    assert lin.exists() and entry["round"] == 0 and (lin.adapter / "adapter_model.bin").read_text(encoding="utf-8") == "round two"
    assert lin.current_gguf() == lin.gguf and lin.gguf.read_bytes() == b"gg"
    try:
        lin.init(root, "again", gguf=gguf)
    except RuntimeError as e:
        assert "already holds" in str(e)
    else:
        raise AssertionError("a second init was allowed")

    new = adapter(tmp_path / "selfask", "self-ask")
    entry = lin.commit(new, "self-ask on the gold paragraphs", "HotpotQA train 1320-1800", 480,
                       {"before": "hotpot-a", "after": "hotpot-b", "score_before": 0.61, "score_after": 0.65}, convert=False)
    assert entry["round"] == 1 and entry["points_gained"] == 4.0 and entry["questions_per_point"] == 120.0
    assert (lin.adapter / "adapter_model.bin").read_text(encoding="utf-8") == "self-ask"
    assert (lin.path / "adapter.prev" / "adapter_model.bin").read_text(encoding="utf-8") == "round two"
    assert len(lin.ledger()) == 2

    flat = adapter(tmp_path / "flat", "flat")
    entry = lin.commit(flat, "a round that gained nothing", "x", 480, {"score_before": 0.65, "score_after": 0.64}, convert=False)
    assert entry["questions_per_point"] is None and entry["points_gained"] == -1.0
    back = lin.rollback("lost a point and cost the asking", convert=False)
    assert back["round"] == 2 and back["rolled_back"]["why"].startswith("lost")
    assert (lin.adapter / "adapter_model.bin").read_text(encoding="utf-8") == "self-ask"
    assert not (lin.path / "adapter.prev").exists() and len(lin.ledger()) == 3
    assert lin.round() == 1  # the last round kept, not the last written


def test_the_facts_adapter_is_kept_apart_and_a_first_rollback_leaves_none(tmp_path):
    skills = Lineage(tmp_path / "system")
    skills.init(adapter(tmp_path / "round2", "round two"), "the asking", convert=False)
    facts = Lineage(tmp_path / "system", part="facts")
    assert not facts.exists() and facts.adapter == tmp_path / "system" / "facts" / "adapter"
    entry = facts.commit(adapter(tmp_path / "sleep", "facts one"), "a sleep", "rows", 0, {}, convert=False)
    assert entry["part"] == "facts" and facts.exists() and len(skills.ledger()) == 2
    assert (skills.adapter / "adapter_model.bin").read_text(encoding="utf-8") == "round two"  # the skills untouched
    facts.rollback("lost the asking", convert=False)
    assert not facts.adapter.exists() and skills.round() == 0
