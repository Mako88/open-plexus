"""The stateless control refutation 1 actually needs: a same-size attention model.

WHY THIS EXISTS. The doc's refutation 1 is "state plus store loses to a same-size
STATELESS model given the whole conversation as context, at equal flops". The
`full-context` arm cannot test that, because it is the same RWKV core and on a
recurrent core re-reading a transcript and carrying the state through it are the
same function -- measured, `readings/phase1-chunk-invariance-*.json`. So the
branch's headline claim had nothing arguing against it. This is the thing that
argues against it.

WHY QWEN3-1.7B AND NOT THE 9B ALREADY ON THE BOX. Same size, or the comparison is
about scale rather than about architecture. RWKV-7 G1 is 1.53B; Qwen3-1.7B is
1.72B, which is close enough to state and far closer than 9B. A 9B beating a 1.5B
would tell us nothing we did not already know.

WHAT IS HELD EQUAL AND WHAT IS NOT.

  Equal: the house, the questions, the delays, the judge, the answer budget,
  and the CONTENT of the framing.

  Not equal, deliberately: the prompt FORMAT. Each model gets its own native
  chat template -- RWKV its `User:`/`Assistant:` pairs, Qwen its own. Forcing
  RWKV's format onto Qwen would handicap it for a reason that has nothing to do
  with memory, and this control is only worth having if it is given every
  advantage that does not cost it information. THE POINT IS TO TRY TO BEAT THE
  ARM, not to stage a win for it.

  Also not equal: this one really does re-read everything, once per question,
  and pays for it. That is the whole difference being measured.

THINKING IS OFF. Qwen3 is a reasoning model and will spend hundreds of tokens
deliberating if allowed. Leaving it on would make the flops comparison a
comparison of reasoning budgets, and complaint 5 is that words are the cost.
"""

from __future__ import annotations

import time
from typing import Any

from ..core.base import Cost, forward_flops

MODEL = "Qwen/Qwen3-1.7B"

# The same instruction the RWKV preamble carries, as content rather than as a
# formatted dialogue. Qwen's template turns this into its own role markup.
SYSTEM = (
    "You are being told things about someone's home. Remember them. "
    "Answer questions in a few words, and say plainly if you were not told "
    "something."
)


class AttentionBaseline:
    """Qwen3-1.7B, re-reading the whole transcript for every question.

    NO STATE IS CARRIED. Every question is a fresh forward over the whole
    conversation, which is what a transformer must do and what makes this a
    genuine control rather than the arm in disguise.
    """

    def __init__(self, model_id: str = MODEL, dtype: str = "float32") -> None:
        import torch
        from transformers import AutoModelForCausalLM, AutoTokenizer

        self.model_id = model_id
        self.tokenizer = AutoTokenizer.from_pretrained(model_id)
        self.model = AutoModelForCausalLM.from_pretrained(
            model_id, dtype=getattr(torch, dtype)
        ).to("cuda").eval()
        self.params = sum(p.numel() for p in self.model.parameters())
        self.tokens_fed = 0
        self.seconds = 0.0

    @property
    def name(self) -> str:
        return f"{self.model_id} [{next(self.model.parameters()).dtype}]"

    def _prompt(self, transcript: list[str], question: str) -> str:
        messages = [{"role": "system", "content": SYSTEM}]
        # The transcript as it was actually said. The core's replies are not in
        # it, exactly as they are not in the RWKV full-context arm -- so both
        # controls see the same information.
        for line in transcript:
            messages.append({"role": "user", "content": line})
            messages.append({"role": "assistant", "content": "Noted."})
        messages.append({"role": "user", "content": question})
        return self.tokenizer.apply_chat_template(
            messages,
            tokenize=False,
            add_generation_prompt=True,
            enable_thinking=False,
        )

    def answer_at(
        self, transcript: list[str], question: str, budget: int = 32
    ) -> tuple[str, Cost]:
        import torch

        text = self._prompt(transcript, question)
        ids = self.tokenizer(text, return_tensors="pt").to("cuda")
        n_in = int(ids["input_ids"].shape[-1])

        started = time.perf_counter()
        with torch.no_grad():
            out = self.model.generate(
                **ids,
                max_new_tokens=budget,
                do_sample=True,
                temperature=1.0,
                top_p=0.3,
                pad_token_id=self.tokenizer.eos_token_id,
            )
        seconds = time.perf_counter() - started

        generated = out[0][n_in:]
        said = self.tokenizer.decode(generated, skip_special_tokens=True).strip()
        n_out = int(generated.shape[-1])

        self.tokens_fed += n_in
        self.seconds += seconds
        return said, Cost(
            tokens_in=n_in,
            tokens_out=n_out,
            seconds=seconds,
            flops=forward_flops(self.params, n_in + n_out),
        )

    def close(self) -> None:
        """Free the card. THE GPU IS ONE CARD AND IT IS SHARED.

        This control and the RWKV core are both multi-gigabyte in fp32 and do
        not fit together on 11 GB. An exam that runs both must let go of one
        before loading the other, and forgetting to is not an error -- it is a
        run that swaps and reads as slow.
        """
        import gc

        import torch

        del self.model
        gc.collect()
        torch.cuda.empty_cache()


def run_attention_baseline(
    house: Any,
    answer_budget: int = 32,
    model_id: str = MODEL,
    progress: bool = True,
) -> Any:
    """The attention control over the same house, scored by the same judge."""
    from . import Tier
    from .run import Answered, ExamResult, judge
    from .world import questions_at

    baseline = AttentionBaseline(model_id)
    schedule = questions_at(house)
    result = ExamResult(tier=Tier.A)
    total = Cost(0, 0, 0.0, 0.0)
    started = time.perf_counter()

    for turn in sorted(schedule):
        transcript = house.turns[: turn + 1]
        for question in schedule[turn]:
            said, cost = baseline.answer_at(transcript, question.text, answer_budget)
            total = total + cost
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
            print(
                f"  attention turn {turn + 1}/{len(house.turns)} "
                f"asked {len(result.answers)} {total.seconds:.0f}s "
                f"{baseline.tokens_fed:,} tokens re-read",
                flush=True,
            )

    result.cost = total
    result.seconds = time.perf_counter() - started
    baseline.close()
    return result
