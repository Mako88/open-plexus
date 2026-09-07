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
COUNT = 6


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
            "The full-context baseline is not a stateless control, so refutation "
            "1 is untested and currently untestable."
        ),
        why=(
            "The doc calls it 'the same core fed the whole transcript every "
            "turn, which is how a transformer would do it', and refutation 1 is "
            "'state plus store loses to a same-size STATELESS model given the "
            "whole conversation as context, at equal flops'. On a recurrent "
            "core there is no such thing: feeding a transcript whole and "
            "carrying the state through it are THE SAME FUNCTION, measured to "
            "the same argmax and top-5 down to token-at-a-time splits. So this "
            "baseline differs from Tier A only in what text went into the state "
            "-- no core replies -- and not in how the memory works. It is a "
            "useful ablation and it is not the control the doc thinks it is. "
            "The branch's headline claim currently has nothing arguing against "
            "it, which is the condition the last two branches died in."
        ),
        settled_by=(
            "A same-size ATTENTION model on the same house -- Qwen3-1.7B class "
            "-- given the whole conversation as context, scored on the same "
            "questions with the same judge, with flops for both. Then refutation "
            "1 has a real control and this closes. Adding a comparison model is "
            "not reopening the DECIDED choice of substrate; it is buying an "
            "instrument. Deciding NOT to build it also closes this, provided the "
            "branch stops listing refutation 1 as something it tests."
        ),
    ),
]

# SETTLED, AND KEPT HERE AS NOTES RATHER THAN AS ENTRIES.
#
# "The full-context baseline costs 2.7 GPU-hours a house against two minutes for
# the arm, and the risk is a session quietly running arms without it."
# Settled 2026-09-06, and by the cost turning out not to exist. A recurrent core
# makes re-reading and carrying the same function, so the baseline's answers cost
# O(N) instead of O(N-squared): the doc's own house went from 1.51M tokens and
# 2.5 hours to about 7k tokens and under two minutes. Nothing was traded away to
# get it -- no shorter house, no subsampled delays. The entry's real worry, that
# expense would push somebody into skipping the control, is gone because the
# expense is gone.
#
# IT WAS REPLACED BY A WORSE PROBLEM RATHER THAN CLOSING CLEANLY, which is why
# the count did not drop: the same measurement showed this baseline is not a
# stateless control at all. That is the new entry above.
#
# "The blind baseline answers the commonest answer for a question's kind, and how
# strong that is depends entirely on how the generator distributes its answers."
# Settled 2026-09-06 by the reporting the entry itself asked for. Every exam
# reading now carries `answer_entropy_bits` per kind beside the blind score per
# kind, and two guards in `tests/guards/test_exam.py` keep both there. The
# numbers on the doc's own house make the point: blind takes 0.30 on colours
# (2.45 bits) and 0.10 on numbers (3.32 bits) for 0.24 overall. That is now a
# readable fact about the exam rather than an accident nobody can see.
#
# WHAT THIS DOES NOT SETTLE, said plainly: reporting blind's strength does not
# make it the RIGHT strength. If a later session wants the generator's answer
# sets widened or narrowed, that is a new entry and a new argument.
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
