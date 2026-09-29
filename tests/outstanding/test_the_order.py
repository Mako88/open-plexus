"""Red until THE ORDER's exits are met. Each reads `readings/`, never a constant."""

import json
from collections import defaultdict
from pathlib import Path

READINGS = Path(__file__).resolve().parents[2] / "readings"


def _exams():
    out = []
    for path in READINGS.glob("exam-*.json"):
        d = json.loads(path.read_text(encoding="utf-8"))
        if d.get("limit") is None:
            out.append(d)
    return out


def test_phase_0_has_all_four_arms_on_a_full_house():
    arms = {d["arm"] for d in _exams()}
    assert {"blind", "full", "recall", "recall2"} <= arms


def test_phase_1_linked_recall_beats_text_recall_on_the_structural_forms():
    """Exit: on three seeds, `linked` above the best text recall arm on reverse,
    twohop and update, with no form falling below it."""
    linked: dict[int, dict] = {}
    best: dict[int, dict] = defaultdict(dict)
    for d in _exams():
        seed, forms = d["house"]["seed"], d["summary"]["by_form"]
        if d["arm"] == "linked":
            linked[seed] = forms
        elif d["arm"].startswith("recall"):
            for form, v in forms.items():
                best[seed][form] = max(best[seed].get(form, 0.0), v["score"])
    seeds_won = 0
    for seed, forms in linked.items():
        base = best.get(seed)
        if not base:
            continue
        structural = all(forms[f]["score"] > base[f] for f in ("reverse", "twohop", "update"))
        none_fell = all(forms[f]["score"] >= base[f] for f in base)
        seeds_won += structural and none_fell
    assert seeds_won >= 3, f"linked recall beats text recall on {seeds_won} of 3 seeds"
