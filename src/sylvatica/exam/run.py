"""Running an exam: the conversation, the questions, and the score.

THE ONE DESIGN DECISION IN THIS FILE THAT IS NOT OBVIOUS: A QUESTION IS ASKED ON
A FORK OF THE STATE, NOT ON THE STATE.

A fact is asked at five delays. If asking happened in the conversation, the
second asking would be measuring the delay since the LAST ASKING rather than
since the fact was told -- and worse, the core's own answer would go into its
memory, so a wrong answer at delay 5 would be re-remembered as if told and would
corrupt delay 20. The exam would be measuring its own interference.

A fixed-size state makes forking free, which is a property worth using: the
conversation advances on the real state, and every question is answered on a
copy that is thrown away. This is not a trick to flatter the arm -- the
full-context baseline gets the same treatment, since it carries no state at all.

TIER A GOES THROUGH DISK. The copy a question is answered on is saved and
reloaded rather than cloned in memory, because "the state survives being written
down" is half of what Tier A claims. The other half -- surviving a whole process
death -- is `readings/phase0-restart-*.json` and is not re-run 150 times here at
thirty seconds of model loading each.
"""

from __future__ import annotations

import statistics
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

from ..core.base import Cost, Meter, Sampling
from ..loop.turn import (
    CORE_PREFIX,
    SEPARATOR,
    STOP_STRINGS,
    USER_PREFIX,
    format_prompt,
    prime,
    say,
    trim_at_stop,
)
from . import Score, Tier
from .baselines import BlindBaseline, is_refusal
from .world import Generated, questions_at


@dataclass
class Answered:
    """One question, what was said, and whether it counted."""

    fact_id: str | None
    kind: str
    delay_turns: int
    asked: str
    wanted: str | None
    said: str
    correct: bool
    invented: bool
    echoed: bool

    def row(self) -> dict[str, Any]:
        return {
            "fact_id": self.fact_id,
            "kind": self.kind,
            "delay": self.delay_turns,
            "asked": self.asked,
            "wanted": self.wanted,
            "said": self.said,
            "correct": self.correct,
            "invented": self.invented,
            "echoed": self.echoed,
        }


@dataclass
class ExamResult:
    """Everything one run produced. Serialised whole into a reading."""

    tier: Tier
    answers: list[Answered] = field(default_factory=list)
    cost: Cost | None = None
    seconds: float = 0.0

    # WHAT AN ATTENTION MODEL WOULD HAVE PAID to produce these same answers:
    # the transcript re-read from nothing, once per question.
    #
    # READ THIS CAREFULLY, BECAUSE THE OBVIOUS READING IS WRONG. This is NOT
    # "the full-context baseline's cost". The full-context baseline is the same
    # RWKV core, and on a recurrent core re-reading and carrying are the same
    # function -- measured, `readings/phase1-chunk-invariance-*.json`. Its real
    # cost is `cost`, and it is small.
    #
    # This number is a PROJECTION for a control that does not exist yet: a
    # same-size attention model, for which re-reading genuinely is a different
    # computation. Refutation 1 is an equal-flops comparison against such a
    # model, and until one is run this is an estimate of one side of a comparison
    # nobody has made. It is here so the arithmetic is on the record, not so a
    # verdict can be read off it.
    charged: Cost | None = None

    # HOW MANY TOKENS THE CORE SPENT TALKING, as opposed to answering questions.
    # Separate because it is the quantity that pollutes the state: the same core
    # given a transcript with none of its own replies in it scored 45 points
    # higher over the first fifty turns. A framing that raises this is poisoning
    # the memory it is meant to serve, however good its answers look.
    reply_tokens: int = 0

    # HOW MANY OF THE CORE'S REPLIES RAN OUT OF BUDGET rather than finishing.
    # A truncated reply goes into the state cut off mid-word, which is a milder
    # form of the malformed pattern that taught the core to echo. Whether it is
    # what still varies between houses is a measurement, and this is it.
    truncated_replies: int = 0
    conversation_turns: int = 0

    # ONE ROW PER TIER B QUESTION: whether the fragment holding the answer was in
    # the injected set, and at what rank. The doc's named Phase 2 reading, and
    # what makes a bad Tier B attributable -- the store failing to find a fact
    # and the core ignoring a found fact need opposite fixes.
    retrieved: list[dict] = field(default_factory=list)

    def score(self) -> Score:
        positives = [a for a in self.answers if a.fact_id is not None]
        negatives = [a for a in self.answers if a.fact_id is None]

        by_delay: dict[int, float] = {}
        for delay in sorted({a.delay_turns for a in positives}):
            at = [a for a in positives if a.delay_turns == delay]
            by_delay[delay] = round(sum(a.correct for a in at) / len(at), 4)

        by_kind: dict[str, float] = {}
        for kind in sorted({a.kind for a in positives}):
            of = [a for a in positives if a.kind == kind]
            by_kind[kind] = round(sum(a.correct for a in of) / len(of), 4)

        return Score(
            tier=self.tier,
            correct=sum(a.correct for a in positives),
            asked=len(positives),
            invented=sum(a.invented for a in negatives),
            # ECHOES ARE COUNTED OVER EVERY QUESTION, not only the negatives.
            # A rising echo count on the positives is the signature of the turn
            # separator going missing again, which is a harness fault that looks
            # exactly like forgetting. See `loop.turn.say`.
            echoed=sum(a.echoed for a in self.answers),
            by_delay=by_delay,
            by_kind=by_kind,
        )


def spread(values: list[float]) -> dict[str, float]:
    """Min, max, mean, deviation and range over repeated runs of the same thing.

    WHAT A THRESHOLD HAS TO BE COMPARED AGAINST. Phase 1's refutation is "Tier A
    at delay 20 below blind", and blind scores 0.24 on the doc's house. Whether a
    Tier A score of 0.30 clears that bar depends entirely on how far a Tier A
    score moves between houses, and a verdict read off one house is a verdict
    read off one sample. The doc already applies this rule to the regression
    gate -- calibrate on noise first, because a threshold chosen before the noise
    is known is a prediction dressed as a check -- and an exam threshold is no
    different.

    THE RANGE IS THE NUMBER TO COMPARE A GAP AGAINST, not the deviation. A
    standard deviation over five samples is itself noisy enough to mislead;
    reporting it without the min and max would hide that.
    """
    return {
        "min": round(min(values), 4),
        "max": round(max(values), 4),
        "mean": round(statistics.fmean(values), 4),
        "stdev": round(statistics.stdev(values), 4) if len(values) > 1 else 0.0,
        "range": round(max(values) - min(values), 4),
    }


def is_echo(question_text: str, said: str) -> bool:
    """Whether the answer is the question handed back.

    A SEPARATE FAILURE FROM CONFABULATION, and it has to be counted separately
    or the invention rate is wrong. On the first working small house, four
    negatives produced two genuine inventions ("Noulaivais keeps the glass jars
    in the cellar" -- a room nobody named) and two echoes ("Where does Thonreal
    keep the brass keys?"). Scoring all four as inventions overstated the rate
    by a factor of two, and the two are not the same defect: one is a core that
    makes things up, the other is a core that has lost the thread of who is
    speaking. The second is what the missing turn separator used to cause, so
    counting it apart is also an early warning that the fault is back.
    """
    q = question_text.lower().strip().rstrip("?").strip()
    return bool(q) and q in said.lower()


def judge(question: Any, said: str) -> tuple[bool, bool, bool]:
    """Score one answer. Returns (correct, invented, echoed).

    A WEAK SCORER ON PURPOSE, and the doc chose it: "scoring is contains-match on
    a short canonical answer". It is generous to the arm -- an answer that
    rambles past the right word still counts -- and it is generous IDENTICALLY to
    every arm and both baselines, which is the property that matters. A
    cleverer judge would be a second model whose failures nobody has measured.

    For a negative there is no right answer, so `correct` is meaningless. What
    is scored is whether it invented one: an answer that neither refuses nor
    merely echoes the question back.
    """
    echoed = is_echo(question.text, said)
    if question.fact_id is None:
        return (False, not is_refusal(said) and not echoed, echoed)
    wanted = (question.answer or "").lower().strip()
    return (bool(wanted) and wanted in said.lower(), False, echoed)


# THE INJECTION FORMAT IS A DIAL and every reading records it. It is deliberately
# plain: a short header, one hit a line, and a two-word acknowledgement so the
# core sees a completed turn rather than a fragment. The acknowledgement matters
# for the reason the whole preamble saga did -- what the core's own turns look
# like is what it learns to produce.
INJECT_HEADER = "Some things you were told earlier:"


def inject(hits: list[Any]) -> str:
    """The retrieved fragments, in the fixed compact format, as a completed turn."""
    lines = "\n".join(f"- {h.fragment.text}" for h in hits)
    return (
        f"{USER_PREFIX}{INJECT_HEADER}\n{lines}"
        f"{SEPARATOR}{CORE_PREFIX} Noted.{SEPARATOR}"
    )


def _precision_row(question: Any, hits: list[Any], house: Generated, provenance: str) -> dict:
    """Did retrieval actually put the answer in front of the core?

    THE DOC'S NAMED PHASE 2 READING: "retrieval precision at k: how often the
    fragment holding the answer is in the injected set." Without it, a bad Tier B
    is unattributable -- the store might have failed to find the fact, or found
    it and the core ignored it, and those need opposite fixes.

    A negative has no answering fragment, so `wanted` is None and the row records
    only what came back.
    """
    from ..store.sqlite import fragment_id

    wanted = None
    if question.fact_id is not None:
        fact = next(f for f in house.facts if f.id == question.fact_id)
        turn = house.told_at[question.fact_id]
        wanted = fragment_id("episode", fact.told, f"{provenance}#turn:{turn}:in")

    got = [h.fragment.id for h in hits]
    return {
        "fact_id": question.fact_id,
        "delay": question.delay_turns,
        "wanted_fragment": wanted,
        "hit": wanted in got if wanted else None,
        "rank": got.index(wanted) if wanted and wanted in got else None,
        "returned": len(got),
    }


def _ask(core: Any, state: Any, text: str, budget: int, meter: Meter) -> str:
    """One question on a state that is thrown away afterwards."""
    tokens = core.encode(format_prompt(text))
    out, _, cost = core.generate(
        tokens, state, budget, Sampling(stop_strings=STOP_STRINGS)
    )
    meter.add(cost)
    return trim_at_stop(core.decode(out), STOP_STRINGS)


def run_exam(
    core: Any,
    house: Generated,
    tier: Tier = Tier.A,
    reply_budget: int = 40,
    answer_budget: int = 32,
    state_path: Path | None = None,
    progress: bool = True,
    preamble: str | None = None,
    repair: bool = False,
    store: Any = None,
    k: int = 5,
    provenance: str = "exam",
    adapter: Any = None,
) -> ExamResult:
    """Hold the conversation, ask every question at its delay, score it.

    TIER A answers from the STATE: the conversation is carried forward, saved to
    disk, and each question is asked on a fork of it.

    TIER B answers from the STORE: the same conversation happens and every turn
    is written, but each question starts from a FRESH PRIMED STATE with the top
    `k` retrieved fragments injected. Nothing the core knows at that moment came
    from carrying the conversation -- which is the point, because it isolates
    retrieval from the state entirely.

    THE TWO TIERS RUN THE SAME CONVERSATION ON PURPOSE. They differ only in what
    the ANSWERING state contains, so the comparison is about where a memory came
    from and not about what was said.

    Tier C needs the adapter and raises. It is not run as A or B in disguise,
    because a tier reading that was secretly another tier would be the most
    expensive kind of wrong number this branch could produce.
    """
    if tier is Tier.C and adapter is None:
        raise NotImplementedError(
            "Tier C is the WEIGHTS tier and no adapter was given (Phase 3). "
            "It is not run as Tier A in disguise."
        )
    if tier is Tier.C and store is not None:
        raise ValueError(
            "Tier C runs with the store DISABLED -- that is what makes it a "
            "measurement of the weights. A Tier C reading taken with retrieval "
            "on would be Tier B wearing a different label, and would be the most "
            "flattering wrong number this branch could produce."
        )
    if tier is Tier.B and store is None:
        raise ValueError(
            "Tier B is the STORE tier and no store was given. Running it without "
            "one would silently be Tier A with extra steps."
        )

    state_path = Path(state_path or Path("state") / "exam" / f"tier-{tier.value.lower()}.pt")
    state_path.parent.mkdir(parents=True, exist_ok=True)

    schedule = questions_at(house)
    meter = Meter()
    result = ExamResult(tier=tier)
    started = time.perf_counter()

    # ONCE, INTO THE FRESH STATE, AND NEVER AGAIN. See `loop.turn.PREAMBLE` for
    # why this exists and what it cost to find out that it was missing.
    state, prime_cost = prime(core, preamble=preamble)
    meter.add(prime_cost)

    # AND KEPT, because Tier B needs a fresh primed state for EVERY question and
    # the preamble is the same text every time. Re-feeding it 270 times would be
    # 27,000 tokens of prefill to recompute a state that cannot differ. Forking
    # the one already computed is what a fixed-size state is for.
    #
    # The cost meter is NOT credited for the saving. `prime` was charged once,
    # which is what actually ran; charging it 270 times to make the arm look
    # expensive, or crediting a saving that no honest implementation would pay,
    # would both be putting a number in a reading that nothing produced.
    primed = core.copy_state(state) if tier in (Tier.B, Tier.C) else None

    for turn, line in enumerate(house.turns):
        # The conversation itself. The core replies, and its reply goes into the
        # state -- a thread where only one side is remembered is not a thread.
        # `say` closes the turn with a separator; see its docstring for what
        # omitting that did to the first two Tier A readings.
        seen: dict = {}
        reply, state, cost = say(core, state, line, reply_budget, repair=repair, seen=seen)
        meter.add(cost)
        result.reply_tokens += cost.tokens_out
        result.truncated_replies += seen.get("truncated", 0)
        result.conversation_turns += 1

        # EVERY TURN IS WRITTEN, IN AND OUT, BEFORE ANYTHING ELSE. The store is
        # the lossless record; forgetting is what falls out of the STATE and out
        # of the hot tier, never what fails to be written.
        if store is not None:
            store.write_turn(line, f"{provenance}#turn:{turn}:in")
            if reply:
                store.write_turn(reply, f"{provenance}#turn:{turn}:out", importance=0.3)

        due = schedule.get(turn)
        if not due:
            continue

        # THROUGH DISK, ONCE PER TURN THAT HAS QUESTIONS. Half of what Tier A
        # claims is that the state survives being written down.
        core.save_state(state, state_path)
        asking = core.load_state(state_path)

        for question in due:
            if tier is Tier.C:
                # A FRESH STATE AND NO RETRIEVAL. Everything the core knows here
                # is in its weights, which is the only thing Tier C measures.
                answering = core.copy_state(primed)
            elif tier is Tier.B:
                # A FRESH STATE. Nothing the core knows here was carried; it all
                # arrived through retrieval, which is what Tier B is for.
                hits = store.search(question.text, k=k, deadline=time.monotonic() + 5.0)
                answering = core.copy_state(primed)
                if hits:
                    answering, cost = core.feed(core.encode(inject(hits)), answering)
                    meter.add(cost)
                    store.mark_recalled([h.fragment.id for h in hits])
                result.retrieved.append(
                    _precision_row(question, hits, house, provenance)
                )
            else:
                answering = core.copy_state(asking)

            said = _ask(core, answering, question.text, answer_budget, meter)
            correct, invented, echoed = judge(question, said)
            result.answers.append(
                Answered(
                    fact_id=question.fact_id,
                    kind=question.kind,
                    delay_turns=question.delay_turns,
                    asked=question.text,
                    wanted=question.answer,
                    said=said,
                    correct=correct,
                    invented=invented,
                    echoed=echoed,
                )
            )

        if progress:
            done = len(result.answers)
            print(
                f"  turn {turn + 1}/{len(house.turns)}  asked {done}  "
                f"{meter.total.seconds:.0f}s",
                flush=True,
            )

    result.cost = meter.total
    result.seconds = time.perf_counter() - started
    return result


def run_blind(house: Generated) -> ExamResult:
    """The blind rule over the same questions. Costs no flops and no seconds."""
    blind = BlindBaseline(house)
    result = ExamResult(tier=Tier.A)
    for question in house.questions:
        said = blind.answer(question)
        correct, invented, echoed = judge(question, said)
        result.answers.append(
            Answered(
                fact_id=question.fact_id,
                kind=question.kind,
                delay_turns=question.delay_turns,
                asked=question.text,
                wanted=question.answer,
                said=said,
                correct=correct,
                invented=invented,
                echoed=echoed,
            )
        )
    result.cost = Cost(0, 0, 0.0, 0.0)
    return result


def run_full_context(
    core: Any,
    house: Generated,
    answer_budget: int = 32,
    progress: bool = True,
    preamble: str | None = None,
) -> ExamResult:
    """The same core, fed the user lines only, from a fresh state.

    WHAT THIS IS NOT, AND THE DOC CALLS IT SOMETHING IT IS NOT. The design doc
    lists this as "*Full-context*: the same core fed the whole transcript every
    turn, which is how a transformer would do it", and treats it as the
    STATELESS control for refutation 1 -- "state plus store loses to a same-size
    stateless model given the whole conversation as context".

    ON A RECURRENT CORE THERE IS NO SUCH CONTROL. `S_t = f(S_{t-1}, x_t)` means
    feeding a transcript whole and feeding it in pieces land on the same state,
    so "give it the whole conversation as context" and "carry the state" are the
    same function, not two. Measured, not argued:
    `readings/phase1-chunk-invariance-*.json` (same argmax and top-5 down to
    token-at-a-time splits) and `readings/phase1-baseline-equivalence-*.json`
    (the method change moved the score less than re-running one method twice).

    SO WHAT THIS ACTUALLY MEASURES is an ABLATION, and a useful one: the same
    memory mechanism as Tier A, differing only in what went into it. Tier A's
    state carries the preamble, the user lines AND the core's own replies. This
    carries the preamble and the user lines. It answers "how much does the core's
    own chatter in its state cost it?" -- which is worth knowing and is not the
    question the doc asked.

    REFUTATION 1 IS THEREFORE UNTESTED, and testing it needs a same-size
    ATTENTION model, where re-reading really is a different computation from
    carrying. That is a new instrument rather than a change of substrate, and it
    is John's call. Standing objection 8.

    THE TRANSCRIPT IT IS GIVEN IS THE USER LINES ONLY, not the core's replies.
    That is a deviation from "the whole transcript" and it favours the BASELINE:
    the replies carry no facts, so dropping them removes tokens without removing
    information, and this control therefore gets a denser context per token than
    a real transformer chat would.

    WHY IT NO LONGER TAKES 2.5 HOURS, and this is a fact about the substrate
    rather than a trick. A transformer must re-read the whole context for every
    question because attention recomputes over all positions; there is no carry.
    A RECURRENCE HAS NO SUCH REQUIREMENT. `S_t = f(S_{t-1}, x_t)` means feeding
    [1..N] from zero and feeding [1..k] then [k+1..N] from `S_k` land on the same
    state. So the baseline advances ONE state through the transcript and forks it
    at each question, and it sees exactly the same tokens it saw before.

    MEASURED, NOT ASSUMED: `readings/phase1-chunk-invariance-*.json`. Splitting a
    1,548-token transcript into halves, thirds, and finally token-at-a-time moved
    the state by 1.9e-5 relative and the logits by 1e-4 absolute, with the same
    argmax and the same top five every time. The tokenizer agrees too -- the
    per-line encodings concatenate to exactly the whole-transcript encoding, so
    not one token differs.

    AND THE COST IS STILL CHARGED IN FULL, which is the part that must not be
    quietly dropped. Refutation 1 is that state plus store loses to a stateless
    model given the whole conversation AT EQUAL FLOPS. What a stateless model
    would pay is the O(N-squared) number, and it is arithmetic -- it does not
    have to be spent to be known. `result.cost` is what this run actually spent;
    `result.charged` is what the baseline it stands for would have cost, and that
    is the number a flops comparison uses. Reporting the cheap one as the
    baseline's cost would hand the arm a win it did not earn.
    """
    from ..core.base import forward_flops
    from ..loop.turn import PREAMBLE, SEPARATOR, USER_PREFIX

    schedule = questions_at(house)
    result = ExamResult(tier=Tier.A)
    meter = Meter()
    started = time.perf_counter()

    state, cost = prime(core, preamble=preamble)
    meter.add(cost)
    preamble_tokens = len(core.encode(PREAMBLE if preamble is None else preamble))
    transcript_tokens = 0
    charged_in = 0
    charged_out = 0

    for turn, line in enumerate(house.turns):
        chunk = core.encode(f"{USER_PREFIX}{line}{SEPARATOR}")
        state, cost = core.feed(chunk, state)
        meter.add(cost)
        transcript_tokens += len(chunk)

        for question in schedule.get(turn, ()):
            asked = core.encode(format_prompt(question.text))
            # The fork. The question never enters the transcript, exactly as in
            # `run_exam` -- see this module's docstring for why.
            out, _, ask_cost = core.generate(
                asked, core.copy_state(state), answer_budget,
                Sampling(stop_strings=STOP_STRINGS),
            )
            meter.add(ask_cost)
            said = trim_at_stop(core.decode(out), STOP_STRINGS)

            # WHAT A STATELESS MODEL WOULD HAVE PAID for this one question: the
            # preamble, the whole transcript so far, and the question, read from
            # nothing.
            charged_in += preamble_tokens + transcript_tokens + len(asked)
            charged_out += len(out)

            correct, invented, echoed = judge(question, said)
            result.answers.append(
                Answered(
                    fact_id=question.fact_id,
                    kind=question.kind,
                    delay_turns=question.delay_turns,
                    asked=question.text,
                    wanted=question.answer,
                    said=said,
                    correct=correct,
                    invented=invented,
                    echoed=echoed,
                )
            )
        if progress and turn in schedule:
            print(
                f"  full-context turn {turn + 1}/{len(house.turns)}  "
                f"asked {len(result.answers)}  {meter.total.seconds:.0f}s  "
                f"charged {charged_in:,} tokens",
                flush=True,
            )

    result.cost = meter.total
    result.charged = Cost(
        tokens_in=charged_in,
        tokens_out=charged_out,
        # SECONDS ARE NOT EXTRAPOLATED. Tokens and flops are arithmetic; wall
        # clock is a measurement, and inventing one for a run that did not happen
        # would put a number in a reading that nothing produced.
        seconds=0.0,
        flops=forward_flops(core.params, charged_in + charged_out),
    )
    result.seconds = time.perf_counter() - started
    return result
