"""THE RED SET. One test per phase of THE ORDER, each failing until that phase lands.

HOW THESE ARE MEANT TO BE READ. Everything in this directory is EXPECTED to be
red. `uv run pytest tests/outstanding` is read, not fixed. A test leaves this
file by the work closing, never by being weakened or deleted -- that is how an
intent survives a session that ran out of context halfway through.

EACH ONE COMPUTES ITS STATE. None of these asserts a constant like
`assert False, "todo"`. They look at what is on disk and in the package and
decide, so the day the work lands they go green on their own and nobody has to
remember that they existed. A red test that would stay red after the work is
done is worse than no test: it trains the next session to ignore the file.

`uv run pytest tests/outstanding -q --no-header -rf` prints the list.
"""

from __future__ import annotations

import importlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
READINGS = ROOT / "readings"


def readings_of(kind: str, phase: int | None = None) -> list[dict]:
    """Every committed reading of a kind. The state of the branch, computed."""
    out = []
    if not READINGS.exists():
        return out
    for path in sorted(READINGS.glob("*.json")):
        try:
            row = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            continue
        if row.get("kind") == kind and (phase is None or row.get("phase") == phase):
            row["_path"] = path.name
            out.append(row)
    return out


def has(module: str, *names: str) -> list[str]:
    """Which of `names` the module does not yet have. Empty means the part is built."""
    try:
        mod = importlib.import_module(module)
    except ImportError:
        return list(names)
    return [n for n in names if not hasattr(mod, n)]


# ---------------------------------------------------------------------------
# Phase 0 -- Ground. Struck from THE ORDER on 2026-09-06. Nothing is owed here.
#
# One entry lived here for an hour and closed by being WRONG rather than by
# being done: "prefill is slower than decode, so the missing WKV7 kernel needs
# a chunked replacement". The 20 tok/s that prompted it was a single unwarmed
# pass. Warmed, prefill runs 125 tok/s at 0.4B and 157 at 1.5B, comfortably
# ahead of decode. The assertion now lives in `tests/guards/test_cost.py`, where
# it is green and guards the finding against a regression.
# ---------------------------------------------------------------------------


# ---------------------------------------------------------------------------
# Phase 1 -- Exam
# ---------------------------------------------------------------------------


def test_the_exam_exists_and_has_taken_the_first_reading():
    """OWED: the told-then-asked worlds, three tiers, two baselines, cost meter.

    The doc's Phase 1 exit: a Tier A reading on a house of 50 facts over 300
    turns at 1.5B, against full-context and blind, at several delays.

    WOULD REFUTE THE CORE CHOICE: Tier A at delay 20 below blind. Then the state
    holds nothing usable and a recurrent core was the wrong part. That is a
    finding, and this test closes either way -- it asks for the READING, not for
    a good one.
    """
    missing = has("sylvatica.exam", "House", "Tier", "run_exam", "blind_baseline",
                  "full_context_baseline")
    assert not missing, f"sylvatica.exam is missing {missing}"

    readings = readings_of("exam", phase=1)
    assert readings, "no phase-1 exam reading committed"

    # BOTH BASELINES, OR IT IS NOT THE READING THE DOC ASKED FOR.
    complete = [r for r in readings if "full-context" in r.get("baselines", [])]
    assert complete, (
        f"{len(readings)} phase-1 reading(s) committed, none with the "
        "full-context baseline run beside the arm"
    )

    # TAKEN UNDER THE PREAMBLE THAT SHIPS, and this clause exists because the
    # test went GREEN on 2026-09-06 for a phase that was not done. Every Tier A
    # number that day was taken under a preamble whose one example of a reply
    # restated the user's turn, so the core answered by echoing questions back
    # and the exam scored that as forgetting. A reading taken under a superseded
    # framing is not a reading of this branch.
    from sylvatica.loop.turn import PREAMBLE

    current = [r for r in complete if r.get("preamble") == PREAMBLE]
    assert current, (
        f"{len(complete)} complete phase-1 reading(s), none taken under the "
        "preamble that currently ships. Re-run the exam."
    )

    # AND THE GAP MUST BEAT THE NOISE, which is the clause that would have
    # stopped the whole mistake. One house said Tier A 0.456 against blind 0.240
    # at delay 20 and read as a comfortable win; five houses said the Tier A
    # range at that delay is 0.60 and that two houses of five fall BELOW blind.
    # A verdict smaller than its own spread is not a verdict.
    noise = readings_of("exam-noise", phase=1)
    assert noise, "no phase-1 noise calibration committed"
    latest = noise[-1]

    # THE PAIRED CRITERION, and it is used here because it was PRE-REGISTERED --
    # written down with its bar named, in its own commit, before the ten-seed run
    # that it judges. That order is the whole of why it is allowed to count.
    #
    # The original criterion compared the mean gap to the full min-max RANGE.
    # That is crude: blind and Tier A are measured on THE SAME houses, so the
    # difference is paired and the pairing removes most of the between-house
    # variance. I noticed that only after the range criterion failed on five
    # houses, which is exactly how a threshold becomes a prediction dressed as a
    # check -- so the range criterion was left standing, the paired one was
    # written down in advance, and both are in every reading.
    paired = latest.get("paired", {})
    assert paired, (
        f"{latest['_path']} predates the paired criterion; re-run the noise "
        "calibration so Phase 1 is judged by the test that was pre-registered"
    )
    at20 = paired.get("20") or paired.get(20)
    assert at20, f"the noise reading {latest['_path']} has no delay-20 paired result"
    assert at20["beats_blind"], (
        f"at delay 20 the mean per-house difference is {at20['mean_difference']} "
        f"with a standard error of {at20['standard_error']}, and "
        f"{at20['houses_above_blind']} of {at20['n_houses']} houses are above "
        "blind. Phase 1 cannot be struck on a gap smaller than its own error."
    )


# ---------------------------------------------------------------------------
# Phase 2 -- Store
# ---------------------------------------------------------------------------


def test_the_store_exists_and_tier_b_has_been_read():
    """OWED: `Fragment`, the `Store` protocol, `SqliteStore`, hybrid retrieval.

    Exit: Tier B beats Tier A at every delay past 20 turns, and retrieval
    precision at k is read.

    WOULD REFUTE: Tier B loses to full-context at equal flops -- refutation 1,
    state plus retrieval bought nothing over re-sending.
    """
    missing = has("sylvatica.store", "Fragment", "Store", "SqliteStore")
    assert not missing, f"sylvatica.store is missing {missing}"

    readings = readings_of("exam", phase=2)
    assert readings, "no phase-2 (Tier B) exam reading committed"

    # ON THE OBLIQUE QUESTIONS, and this clause is written before the first
    # oblique reading exists. The direct questions share every content word with
    # the sentences that told the facts, and the measured consequence was that
    # all three rankers scored precision 1.000 -- a keyword index, an embedding,
    # and their fusion. A task all three solve perfectly is not measuring which
    # of them works. Phase 2 cannot be struck on it.
    oblique = [r for r in readings if r.get("house", {}).get("phrasing") == "oblique"]
    assert oblique, (
        f"{len(readings)} phase-2 reading(s), none on the oblique questions. The "
        "direct ones are answerable by word overlap alone."
    )
    latest = oblique[-1]
    rows = {r["label"]: r for r in latest["rows"]}

    # WRITTEN BEFORE THE FIRST READING LANDED, deliberately. Phase 1's test went
    # GREEN for a phase that was not done, because it asked only that a reading
    # exist. The exit is a measurement, so the test has to be the measurement.

    # ALL THREE RANKERS, or standing objection 7 is unaddressed. The exam's
    # questions share every content word with the sentences that told the facts,
    # so FTS5 alone can find them -- a Tier B score that is really the lexical
    # ranker's would pass a task easier than the one the store is for.
    arms = {name for name in rows if name.startswith("tier-b-")}
    assert arms >= {"tier-b-hybrid", "tier-b-lexical", "tier-b-vector"}, (
        f"only {sorted(arms)} ran; all three rankers are needed to tell whether "
        "retrieval or mere word overlap produced the score"
    )

    assert "tier-a" in rows, "no Tier A beside Tier B; the exit is a comparison"
    a, b = rows["tier-a"], rows["tier-b-hybrid"]
    assert b["precision"]["at_k"] is not None, "retrieval precision was not read"

    # THE DOC'S EXIT: Tier B beats Tier A at every delay past 20 turns.
    long_delays = [d for d in a["by_delay"] if int(d) > 20]
    assert long_delays, "the house has no delays past 20; the exit cannot be read"
    losses = [
        f"delay {d}: B {b['by_delay'][d]} vs A {a['by_delay'][d]}"
        for d in long_delays
        if b["by_delay"][d] <= a["by_delay"][d]
    ]
    assert not losses, "Tier B does not beat Tier A past delay 20: " + "; ".join(losses)


# ---------------------------------------------------------------------------
# Phase 3 -- Consolidation. The bet.
# ---------------------------------------------------------------------------


def test_the_gate_was_calibrated_on_noise_before_a_threshold_was_chosen():
    """OWED, AND OWED FIRST. The doc: 'Calibrate the thresholds on noise FIRST:
    run two identical cycles and read the spread before a threshold is chosen.'

    A threshold picked before the noise is known is a prediction dressed as a
    check, and this project has already shipped one of those.
    """
    assert readings_of("gate-noise", phase=3), (
        "no phase-3 gate-noise calibration reading committed"
    )


def test_the_declaratives_arm_can_actually_be_run():
    """OWED: an extractor that turns an episode window into paraphrased facts.

    The doc names three training shapes and EXPECTS this one to win -- "the
    literature says models learn facts from a handful of raw exposures badly and
    from paraphrased declaratives much better". An expectation written in a
    design document is not a reading, and an arm that cannot be run cannot refute
    it.

    `build_training_set(arm="declaratives")` raises today, deliberately: building
    the expected winner before the `raw` arm has a reading would be choosing the
    result before running the race. This closes when the extractor exists AND its
    own refutation has been checked -- the doc's is that the extractor at 1.5B
    produces facts wrong more than a fifth of the time, read by hand on a sample
    of fifty.
    """
    import inspect

    from sylvatica.learn.consolidate import build_training_set
    from sylvatica.store import ReplaySpec, SqliteStore  # noqa: F401

    source = inspect.getsource(build_training_set)
    assert "NotImplementedError" not in source, (
        "the declaratives arm is still a raise; the extractor is owed"
    )
    assert readings_of("extractor", phase=3), (
        "no reading of the extractor's error rate on a hand-checked sample"
    )


def test_consolidation_exists_and_tier_c_is_above_blind():
    """OWED: LoRA over time-mix and channel-mix, replay sampling, the regression
    gate, rollback, merge every K cycles, `consolidated_at`.

    Exit: one training-shape arm has Tier C above blind on a fresh state with
    the store OFF, and the gate has not tripped in its last five cycles. This is
    the branch's bet and complaint 4's bar.
    """
    missing = has("sylvatica.learn", "LoraAdapter", "Gate", "calibrate", "run_cycle")
    assert not missing, f"sylvatica.learn is missing {missing}"

    tier_c = [r for r in readings_of("exam", phase=3) if r.get("tier") == "C"]
    assert tier_c, "no phase-3 Tier C reading committed"
    above = [r for r in tier_c if r.get("score", 0) > r.get("blind", 1)]
    assert above, "no Tier C reading is above the blind baseline"


# ---------------------------------------------------------------------------
# Phase 4 -- Idle. The first north star's new line.
# ---------------------------------------------------------------------------


def test_it_ran_unattended_for_a_day_and_remembered_hour_one():
    """OWED: the idle scheduler as a PROCESS, reflection fragments, hot/cold tiers.

    Exit, and the first north star's added line: the machine has run unattended
    for 24 hours, learned something told at hour 1, and answered it at hour 24
    on a fresh state with the store off.

    WOULD REFUTE: a day of consolidation and reflection costs more flops than
    re-sending the full transcript for that day's turns would have -- refutation
    3, the workaround was cheaper than the fix.
    """
    missing = has("sylvatica.loop", "IdleScheduler")
    assert not missing, f"sylvatica.loop is missing {missing}"
    vigils = [r for r in readings_of("vigil", phase=4) if r.get("hours", 0) >= 24]
    assert vigils, "no 24-hour unattended reading committed"
    assert any(r.get("answered_from_hour_one") for r in vigils), (
        "a day was run but nothing told at hour 1 was answered at hour 24"
    )


# ---------------------------------------------------------------------------
# Phase 5 -- Fleet
# ---------------------------------------------------------------------------


def test_the_fleet_survives_losing_a_third_of_itself():
    """OWED: `Node`, gossip, `FanoutStore`, deadlines; several processes on one box.

    Exit: a Tier B reading with a third of the fleet down that stays above Tier
    A, plus a footprint row per node. That row is what decides whether twenty
    phones get bought, so it is the reading and not a feeling.

    WOULD REFUTE: Tier B with three of eight shards down falls to Tier A. Then
    the store did not survive its own constraints.
    """
    missing = has("sylvatica.node", "Node", "FanoutStore")
    assert not missing, f"sylvatica.node is missing {missing}"
    degraded = [r for r in readings_of("exam", phase=5) if r.get("shards_down", 0) > 0]
    assert degraded, "no phase-5 reading with shards down committed"
    assert any(r.get("score", 0) > r.get("tier_a", 1) for r in degraded), (
        "no degraded-fleet reading stays above Tier A"
    )
