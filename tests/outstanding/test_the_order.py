"""Red until THE ORDER's exits are met. Each reads `readings/`, never a constant.

Only readings taken on the house the generator makes today count: a reading's
house fingerprint must match `generate_house(seed)` now. A reading on a house
since corrected is history, and it cannot close anything.
"""

import json
from pathlib import Path

from unfused.exam.world import generate_house

READINGS = Path(__file__).resolve().parents[2] / "readings"


def _current():
    out, prints = [], {}
    for path in READINGS.glob("exam-*.json"):
        d = json.loads(path.read_text(encoding="utf-8"))
        house = d["house"]
        if d.get("limit") is not None or "fingerprint" not in house:
            continue
        key = (house["seed"], house["facts"], house["turns"])
        if key not in prints:
            prints[key] = generate_house(seed=key[0], n_facts=key[1], n_turns=key[2]).fingerprint()
        if house["fingerprint"] == prints[key]:
            out.append(d)
    return out


def _faculty(d):
    return d.get("faculty") or ""


def test_phase_1_the_linked_verdict_is_read_on_the_current_house():
    arms = {d["arm"] for d in _current() if "9B" in _faculty(d)}
    assert {"linked", "recall", "recall2", "full"} <= arms, f"have {sorted(arms)}"


def test_phase_2_the_small_mouths_are_priced():
    have = {(d["arm"], size) for d in _current() for size in ("2B", "0.8B")
            if f"-{size}" in _faculty(d)}
    for size in ("2B", "0.8B"):
        assert ("full", size) in have and any(a.startswith(("recall", "linked"))
                                              for a, s in have if s == size), size


def test_phase_3_an_ear_reads_the_house():
    """Exit: an ear at 0.8 assertion precision and recall. Readings kind `ears`."""
    best = 0.0
    for path in READINGS.glob("ears-*.json"):
        d = json.loads(path.read_text(encoding="utf-8"))
        if d.get("limit") is not None or "own" not in d.get("scorer", ""):
            continue  # the first scorer let two fillers run together and still count
        best = max(best, min(d.get("precision", 0.0), d.get("recall", 0.0)))
    assert best >= 0.8, f"best ear reads at {best}"


def test_phase_4_the_system_beats_its_own_faculty_given_everything():
    """Exit: on three seeds, the system above full context under the same small
    faculty on twohop, chain3 and count."""
    by = {(d["arm"], d["house"]["seed"], _faculty(d)): d["summary"]["by_form"]
          for d in _current()}
    won = set()
    for (arm, seed, faculty), forms in by.items():
        if arm != "system" or ("full", seed, faculty) not in by:
            continue
        base = by[("full", seed, faculty)]
        if all(f in forms and forms[f]["score"] > base[f]["score"]
               for f in ("twohop", "chain3", "count")):
            won.add(seed)
    assert len(won) >= 3, f"the system beats its faculty on {len(won)} of 3 seeds"
