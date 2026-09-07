"""Standing objections to things this branch currently does. Green, and it prints.

WHAT AN ENTRY IS. Not a bug and not a TODO -- those go in `tests/outstanding` or
get fixed. An entry here is a place where the branch has made a choice that
might be wrong and NOBODY HAS MEASURED IT. Each one carries what would settle it
in either direction, so settling it is a session's work rather than a session's
argument.

AN ENTRY LEAVES BY BEING SETTLED, and the count below is asserted so that
neither adding nor quietly dropping one is invisible. Removing an entry without
a reading in `readings/` or a line in a commit message is the failure this file
exists to prevent.

  uv run python tests/pushback.py     # read them
  uv run pytest tests/               # the count is checked, and the list prints
"""

from __future__ import annotations

from dataclasses import dataclass

# The count is asserted. Change it in the same commit that changes the list, and
# say in the message which entry left and what settled it.
COUNT = 7


@dataclass(frozen=True)
class Objection:
    what: str  # what the branch does
    why: str  # why it might be wrong
    settled_by: str  # the reading that decides it, either way


OBJECTIONS: list[Objection] = [
    Objection(
        what="Every `feed` and `generate` clones the whole state before touching it.",
        why=(
            "`RWKV.forward` writes into the list it is given, and a caller who "
            "re-feeds a state it thought it still had would get a wrong number "
            "rather than an exception -- so the clone is defensive and correct. "
            "It is also a GPU allocation and copy of the entire state on EVERY "
            "call, which at 1.5B is tens of megabytes per generated token, since "
            "`generate` calls forward once per token. This may be a large part "
            "of the decode rate and nobody has looked."
        ),
        settled_by=(
            "A cost reading with the clone and with an explicit `feed_in_place` "
            "on the hot path. If the rates match, the clone is free and this "
            "entry closes; if they do not, `generate` should clone once at entry "
            "rather than per token."
        ),
    ),
    Objection(
        what="The prompt format is `User: ...\\n\\nAssistant:` and was not verified.",
        why=(
            "RWKV's instruction tunes are trained on a specific shape, and the "
            "G1 line is a REASONING series that may expect thinking tags. "
            "Getting this wrong does not raise -- it produces a model that "
            "rambles or answers the wrong question, which reads as the core "
            "being weak rather than as the harness being wrong. Phase 1's first "
            "reading would then refute the core choice for a reason that has "
            "nothing to do with the core."
        ),
        settled_by=(
            "The same held-out question set run under two formats at 1.5B. If "
            "the scores are within noise the format is not load-bearing and the "
            "entry closes; if they differ, the format is a dial and belongs in "
            "DIALS with a reading beside it."
        ),
    ),
    Objection(
        what="`RwkvCore.embed` reduces the last layer's two `x_prev` vectors.",
        why=(
            "Nothing has checked that this carries meaning. It is in the "
            "protocol because the protocol needs it, and 'core-derived "
            "embeddings' is an OPEN FORK rather than a decision -- but a "
            "plausible-looking vector that ranks badly is worse than no vector, "
            "because Phase 2 could quietly adopt it and blame retrieval."
        ),
        settled_by=(
            "Rank the same query set with this and with the MiniLM-class "
            "encoder once Phase 2 has one, on the exam's own facts. Report "
            "retrieval precision at k for both."
        ),
    ),
    Objection(
        what="Tier A is defined as a state saved and reloaded across a restart.",
        why=(
            "The restart is the part complaint 4 cares about, and it is also the "
            "part least likely to be where anything is lost -- serialising a "
            "tensor and reading it back is not where a memory degrades. The "
            "interesting variable is DELAY IN TURNS. If a restart costs nothing "
            "measurable then every Tier A run is paying a process launch to "
            "demonstrate that `torch.save` works."
        ),
        settled_by=(
            "Tier A at the same delays with and without the restart. Identical "
            "scores mean the restart is ceremony and can be checked once by a "
            "guard rather than on every run."
        ),
    ),
    Objection(
        what="The blind baseline answers the commonest answer for a question's kind.",
        why=(
            "On a GENERATED house, how strong that is depends entirely on how "
            "the generator distributes its answers. A generator that draws "
            "numbers from a small set hands blind a large free score and makes "
            "every arm look bad; one that draws from a huge set makes blind "
            "trivial and every arm look good. The blind rule is what killed the "
            "two earlier branches, so its strength here must be a property of "
            "the exam that is reported, not an accident of the generator."
        ),
        settled_by=(
            "Report, beside every exam reading, the blind score PER QUESTION "
            "KIND and the entropy of the generator's answer distribution for "
            "that kind. Then a blind score is interpretable rather than just low "
            "or high."
        ),
    ),
    Objection(
        what="The state is saved and fsynced to disk after every single turn.",
        why=(
            "It is what makes a kill safe, and it is measured at 6.5 MB for "
            "0.4B, more for 1.5B. That write is not in the cost meter, which "
            "counts flops and core seconds only. Refutation 3 compares "
            "consolidation's cost against re-sending the context; if per-turn "
            "state I/O is a meaningful share of a turn, it belongs on the same "
            "ledger."
        ),
        settled_by=(
            "Time a turn with the save, with the save unsynced, and with it "
            "skipped, at both sizes. If the save is under a few percent of a "
            "turn, this closes and the cost meter stays as it is."
        ),
    ),
    Objection(
        what=(
            "The full-context baseline re-sends the whole transcript every turn, "
            "and the doc says it runs beside EVERY arm."
        ),
        why=(
            "It is the right control and the arithmetic is measured, not "
            "assumed. `estimate_full_context_tokens` on the doc's own Phase 1 "
            "house -- 50 facts, 300 turns, 270 questions -- says 1.51 million "
            "tokens, about 2.7 GPU-hours at the measured 157 tok/s. The Tier A "
            "arm on the same house is roughly two minutes of core time. EIGHTY "
            "TIMES THE ARM, on a card that is shared and is also wanted for "
            "consolidation. The risk is not that it is expensive; it is that a "
            "session under time pressure quietly runs the arm without it, which "
            "is precisely how the two earlier branches came to be beaten by a "
            "rule nobody had plotted against."
        ),
        settled_by=(
            "Run it once at 1.5B on the real Phase 1 house and record the wall "
            "clock. Then decide, in the open, between three options and record "
            "which: a shorter house, the baseline evaluated at a subsample of "
            "delays with the subsample named in every reading, or six hours a "
            "run accepted as the price. Any of the three closes this; running "
            "arms without it does not."
        ),
    ),
]

# SETTLED, AND KEPT HERE AS A NOTE RATHER THAN AS AN ENTRY.
#
# "Nothing outside `core` may index into a state, and nothing enforces it."
# Settled 2026-09-06 by `tests/guards/test_state_is_opaque.py`, which walks the
# AST of every module outside `core` and fails on a subscript or a `for` over
# anything named like a state. The entry predicted the rule would break first in
# the store, where it would be least visible; the guard now runs on the store
# before the store exists.


def report() -> str:
    lines = [f"PUSHBACK -- {len(OBJECTIONS)} standing objections", ""]
    for i, o in enumerate(OBJECTIONS, 1):
        lines.append(f"{i}. {o.what}")
        lines.append(f"   why: {o.why}")
        lines.append(f"   settled by: {o.settled_by}")
        lines.append("")
    return "\n".join(lines)


def test_the_count_is_what_the_file_says():
    """Adding or dropping an objection is a deliberate act with a commit line."""
    assert len(OBJECTIONS) == COUNT, (
        f"{len(OBJECTIONS)} objections, COUNT says {COUNT}. If one was settled, "
        "say which and by what reading in the commit message."
    )


def test_every_objection_says_what_would_settle_it():
    """AN OBJECTION WITHOUT A SETTLEMENT IS A COMPLAINT, and the branch has a
    separate list for those. Each entry must name a reading that could close it
    in either direction -- not a reading that could only confirm it."""
    for o in OBJECTIONS:
        assert o.what.strip() and o.why.strip() and o.settled_by.strip()
        assert len(o.settled_by) > 60, f"settlement for {o.what!r} is too vague to run"


if __name__ == "__main__":
    print(report())
