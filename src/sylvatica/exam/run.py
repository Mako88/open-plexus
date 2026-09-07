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
from ..loop.turn import STOP_STRINGS, format_prompt, prime, say, trim_at_stop
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
) -> ExamResult:
    """Hold the conversation, ask every question at its delay, score it.

    Tier A is the only tier this can run. B needs the store (Phase 2) and C
    needs the adapter (Phase 3); both raise rather than silently running as A,
    because a Tier B reading that was secretly Tier A would be the most
    expensive kind of wrong number this branch could produce.
    """
    if tier is not Tier.A:
        raise NotImplementedError(
            f"Tier {tier.value} needs "
            + ("the store (Phase 2)" if tier is Tier.B else "the adapter (Phase 3)")
            + ". It is not run as Tier A in disguise."
        )

    state_path = Path(state_path or Path("state") / "exam" / "tier-a.pt")
    state_path.parent.mkdir(parents=True, exist_ok=True)

    schedule = questions_at(house)
    meter = Meter()
    result = ExamResult(tier=tier)
    started = time.perf_counter()

    # ONCE, INTO THE FRESH STATE, AND NEVER AGAIN. See `loop.turn.PREAMBLE` for
    # why this exists and what it cost to find out that it was missing.
    state, prime_cost = prime(core, preamble=preamble)
    meter.add(prime_cost)

    for turn, line in enumerate(house.turns):
        # The conversation itself. The core replies, and its reply goes into the
        # state -- a thread where only one side is remembered is not a thread.
        # `say` closes the turn with a separator; see its docstring for what
        # omitting that did to the first two Tier A readings.
        seen: dict = {}
        _, state, cost = say(core, state, line, reply_budget, repair=repair, seen=seen)
        meter.add(cost)
        result.reply_tokens += cost.tokens_out
        result.truncated_replies += seen.get("truncated", 0)
        result.conversation_turns += 1

        due = schedule.get(turn)
        if not due:
            continue

        # THROUGH DISK, ONCE PER TURN THAT HAS QUESTIONS. Half of what Tier A
        # claims is that the state survives being written down.
        core.save_state(state, state_path)
        asking = core.load_state(state_path)

        for question in due:
            said = _ask(core, core.copy_state(asking), question.text, answer_budget, meter)
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
