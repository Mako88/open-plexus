"""The regression gate: what stops consolidation from eating the core.

REFUTATION 2 OF THE WHOLE BRANCH is "adapter updates lose the core's baseline
abilities faster than they add". This is the instrument that would see it. After
every consolidation cycle the adapter is measured on two fixed sets -- held-out
perplexity and general question answering -- and a cycle that moves either past
its threshold is ROLLED BACK, and the rollback is a reading.

CALIBRATE ON NOISE FIRST, AND THE DOC IS EMPHATIC ABOUT IT: "run two identical
cycles and read the spread before a threshold is chosen." A threshold written
before the noise is known is a prediction dressed as a check, and this branch has
already shipped one of those and had to withdraw it. `calibrate` below is that
procedure, and `Gate` REFUSES TO RUN WITHOUT THRESHOLDS rather than defaulting to
something plausible -- a default here would be exactly the invented number the
rule exists to prevent.

WHY PERPLEXITY AND QA RATHER THAN ONE OF THEM. They fail differently. Perplexity
is sensitive and continuous and notices a model getting slightly worse at English
long before anything visible breaks; it also drifts for reasons that have nothing
to do with capability. QA is coarse and binary and only moves when something has
properly broken, but when it moves it means something. Watching one alone would
either cry wolf or notice too late.
"""

from __future__ import annotations

import math
from dataclasses import dataclass, field
from typing import Any

from . import GateResult
from .heldout import GENERAL_QA, PERPLEXITY_TEXT, fingerprint


@dataclass
class Measurement:
    """What the two fixed sets said at one moment."""

    perplexity: float
    qa_score: float
    qa_correct: int
    qa_asked: int
    tokens: int
    seconds: float
    fingerprint: str = field(default_factory=fingerprint)

    def row(self) -> dict[str, Any]:
        return {
            "perplexity": round(self.perplexity, 5),
            "qa_score": round(self.qa_score, 4),
            "qa_correct": self.qa_correct,
            "qa_asked": self.qa_asked,
            "tokens": self.tokens,
            "seconds": round(self.seconds, 2),
            "heldout_fingerprint": self.fingerprint,
        }


def perplexity(core: Any, adapter: Any = None, text: str = PERPLEXITY_TEXT) -> tuple[float, int]:
    """Perplexity of the fixed held-out text under the core, optionally adapted.

    Uses the DIFFERENTIABLE forward even though nothing is being trained, because
    it is the only path that can apply an adapter. Under `no_grad` it is the same
    arithmetic as the inference path -- which `tests/exam/test_wkv7_matches_
    reference.py` is what makes safe to say.
    """
    import torch

    from ..core.wkv7 import DifferentiableRwkv7

    tokens = core.encode(text)
    model = DifferentiableRwkv7(core, adapter=adapter)
    with torch.no_grad():
        logits, _ = model.forward(list(tokens), None)
        targets = torch.tensor(tokens[1:], device=logits.device)
        loss = torch.nn.functional.cross_entropy(logits[:-1].float(), targets)
    return float(math.exp(float(loss))), len(tokens)


def general_qa(
    core: Any, adapter: Any = None, budget: int = 16
) -> tuple[float, int, int]:
    """The small fixed QA set, scored by the same weak contains-match as the exam.

    WEAK ON PURPOSE AND WEAK IDENTICALLY before and after a cycle, which is the
    only property a gate needs. A cleverer judge would be a second model whose
    own drift nobody is watching.
    """
    from ..core.base import Sampling
    from ..loop.turn import PREAMBLE, STOP_STRINGS, format_prompt, trim_at_stop

    # ONE CODE PATH FOR BOTH, which matters more here than anywhere else in the
    # branch. The gate compares a measurement of the base against a measurement
    # of the adapted model; if those two numbers came from DIFFERENT
    # implementations -- the fast inference path and the differentiable one --
    # then any difference between the implementations would read as a
    # regression, and the gate would roll back cycles for a reason that has
    # nothing to do with the adapter. So the adapter is folded into the weights,
    # both measurements run on the same fast path, and it is folded back out.
    applied = adapter is not None
    if applied:
        adapter.apply_to(core._model.z)
    try:
        state, _ = core.feed(core.encode(PREAMBLE), None)
        correct = 0
        for question, answers in GENERAL_QA:
            out, _, _ = core.generate(
                core.encode(format_prompt(question)),
                core.copy_state(state),
                budget,
                # GREEDY: the gate measures the model, not the sampler.
                # See `Sampling.greedy` for the calibration that forced this.
                Sampling(stop_strings=STOP_STRINGS, greedy=True),
            )
            said = trim_at_stop(core.decode(out), STOP_STRINGS).lower()
            correct += any(a in said for a in answers)
    finally:
        if applied:
            adapter.revert_from(core._model.z)
    return correct / len(GENERAL_QA), correct, len(GENERAL_QA)


def measure(core: Any, adapter: Any = None) -> Measurement:
    """Both fixed sets, once."""
    import time

    started = time.perf_counter()
    ppl, tokens = perplexity(core, adapter)
    score, correct, asked = general_qa(core, adapter)
    return Measurement(
        perplexity=ppl,
        qa_score=score,
        qa_correct=correct,
        qa_asked=asked,
        tokens=tokens,
        seconds=time.perf_counter() - started,
    )


class Gate:
    """Compares a cycle's measurement against the base, and says roll back or not.

    THRESHOLDS ARE REQUIRED, NOT DEFAULTED. `calibrate` produces them from the
    noise; passing numbers somebody typed is possible and is a decision that
    belongs in a commit message. Refusing to guess is the point: a default here
    would be the invented threshold the whole rule exists to prevent.
    """

    def __init__(
        self,
        base: Measurement,
        perplexity_threshold: float,
        qa_threshold: float,
    ) -> None:
        self.base = base
        self.perplexity_threshold = perplexity_threshold
        self.qa_threshold = qa_threshold

    def check(self, after: Measurement) -> GateResult:
        """Did this cycle cost more than it was allowed to?

        Perplexity is compared as a RATIO and QA as an absolute drop, because
        that is what each is. A perplexity of 21 against 20 means the same thing
        as 42 against 40; a QA score of 0.60 against 0.64 does not mean the same
        as 0.06 against 0.10.
        """
        if after.fingerprint != self.base.fingerprint:
            raise ValueError(
                "the held-out sets changed between the base measurement and this "
                "one, so the two are not comparable. See `learn/heldout.py`."
            )
        ppl_ratio = after.perplexity / self.base.perplexity
        qa_drop = self.base.qa_score - after.qa_score
        # A HAIR OF TOLERANCE, BECAUSE THE THRESHOLD IS A COUNT IN DISGUISE. The
        # QA floor of 0.04 means "one question of twenty-five may go". In floats,
        # 0.88 - 0.84 is 0.040000000000000036, which is greater than 0.04 -- so
        # exactly the drop the threshold was chosen to permit tripped the gate,
        # on every cycle of a run where nothing else had gone wrong.
        eps = 1e-9
        tripped = (
            ppl_ratio > self.perplexity_threshold + eps
            or qa_drop > self.qa_threshold + eps
        )
        return GateResult(
            perplexity=after.perplexity,
            qa_score=after.qa_score,
            perplexity_threshold=self.perplexity_threshold,
            qa_threshold=self.qa_threshold,
            tripped=tripped,
            rolled_back=tripped,
        )


def calibrate(
    core: Any, repeats: int = 3, safety: float = 2.0
) -> dict[str, Any]:
    """Read the noise BEFORE choosing a threshold. The doc's rule, executed.

    AND THE FIRST RUN OF IT CHANGED WHAT THE MEASUREMENT IS. Sampled at
    temperature 1.0, the QA score over 25 questions carries about 0.10 of
    standard deviation from the sampler alone -- binomial noise at p near 0.55.
    Three repeats spread 0.60 to 0.64, and the very next measurement of the SAME
    untouched model came in at 0.480, outside its own calibrated range. A
    threshold from that spread would have sat below one standard deviation of its
    own measurement, and the gate would have rolled back cycles at random: the
    adapter's whole trajectory decided by a die, and every rollback recorded as
    a finding about consolidation.

    SO THE MEASUREMENT IS GREEDY NOW and both halves are deterministic --
    repeated runs give 0.88, 0.88, 0.88. Which means this function's honest
    output has changed shape: there is no noise left to calibrate against, the
    observed spread is zero, and THE FLOORS BELOW ARE THE THRESHOLD.

    THAT MAKES THEM A POLICY RATHER THAN A CALIBRATION, and saying so is the
    point. The doc's rule was written for a noisy yardstick: do not invent a
    threshold, measure the spread. With a deterministic yardstick the spread is
    nothing and the only remaining question is how much regression is
    acceptable -- 2% of perplexity, and one question of twenty-five. Those are
    judgements, they are recorded in every reading, and they are not disguised as
    measurements.
    """
    runs = [measure(core) for _ in range(repeats)]
    ppls = [m.perplexity for m in runs]
    qas = [m.qa_score for m in runs]

    ppl_spread = (max(ppls) - min(ppls)) / min(ppls) if min(ppls) else 0.0
    qa_spread = max(qas) - min(qas)

    return {
        "repeats": repeats,
        "safety": safety,
        "perplexity": {"min": min(ppls), "max": max(ppls), "relative_spread": ppl_spread},
        "qa_score": {"min": min(qas), "max": max(qas), "spread": qa_spread},
        # A floor under each, because a spread of exactly zero would produce a
        # threshold of zero -- a gate that trips on the first bit of float noise
        # and rolls back every cycle forever.
        "perplexity_threshold": 1.0 + max(ppl_spread * safety, 0.02),
        "qa_threshold": max(qa_spread * safety, 0.04),
        "runs": [m.row() for m in runs],
        "heldout_fingerprint": fingerprint(),
    }
