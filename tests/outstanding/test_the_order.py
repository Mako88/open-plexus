"""Red until THE ORDER's exits are met. Each reads `readings/`, never a constant."""

import json
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


def test_phase_1_bound_recall_beats_text_recall_on_the_structural_forms():
    """Exit: on three seeds, a bound arm above `recall` on reverse, twohop and
    update, with no form falling."""
    by = {}
    for d in _exams():
        by[(d["arm"], d["house"]["seed"])] = d["summary"]["by_form"]
    seeds_won = 0
    for (arm, seed), forms in by.items():
        if not arm.startswith("bound") or ("recall", seed) not in by:
            continue
        base = by[("recall", seed)]
        structural = all(forms[f]["score"] > base[f]["score"]
                         for f in ("reverse", "twohop", "update"))
        none_fell = all(forms[f]["score"] >= base[f]["score"] for f in base)
        seeds_won += structural and none_fell
    assert seeds_won >= 3, f"bound recall beats text recall on {seeds_won} of 3 seeds"
