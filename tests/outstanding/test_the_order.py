"""Red until THE ORDER's exits are met. Each reads `readings/`, never a constant.

Only readings taken on the house the generator makes today count: a reading's
house fingerprint must match `generate_house(seed)` now. A reading on a house
since corrected is history, and it cannot close anything.
"""

import json
from pathlib import Path

from unfused.exam.world import generate_house

READINGS = Path(__file__).resolve().parents[2] / "readings"
# what a run counted rather than how it was set
COUNTERS = {"ear_calls", "ear_cached", "ear_unparsed", "planner_calls", "shapes", "plan_uses"}


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
    faculty on twohop, chain3 and count, and above blind on each. A house asks each
    chain five times and has few distinct chains, so on seed 1 blind reads 1.0 on
    chain3: beating full there is not evidence of composing unless blind is beaten."""
    current = _current()
    full = {(d["house"]["seed"], _faculty(d)): d["summary"]["by_form"] for d in current
            if d["arm"] == "full"}
    blind = {d["house"]["seed"]: d["summary"]["by_form"] for d in current
             if d["arm"] == "blind"}
    # one configuration must win every seed: seeds won by different dials are
    # different brains, and summing them says nothing about either
    won: dict[tuple, set] = {}
    for d in current:
        arm, seed, faculty = d["arm"], d["house"]["seed"], _faculty(d)
        # any arm of the system counts, taught plans included: the checkpoint is the
        # bet's, taken with whatever mechanisms exist (John's, 2026-09-30)
        if arm not in ("system", "planned", "taught") or (seed, faculty) not in full:
            continue
        forms = d["summary"]["by_form"]
        bars = (full[(seed, faculty)], blind.get(seed, {}))
        # a perfect score passes a bar that is itself perfect, or seed 1's chain3,
        # where blind reads 1.0, could never be met by anything
        if all(f in forms and all(forms[f]["score"] > b.get(f, {}).get("score", 1.0)
                                  or forms[f]["score"] == 1.0 for b in bars)
               for f in ("twohop", "chain3", "count")):
            config = (arm, faculty, json.dumps(
                {k: v for k, v in d.get("dials", {}).items() if k not in COUNTERS},
                sort_keys=True))
            won.setdefault(config, set()).add(seed)
    most = max((len(s) for s in won.values()), default=0)
    assert most >= 3, f"one configuration beats its faculty on {most} of 3 seeds"
