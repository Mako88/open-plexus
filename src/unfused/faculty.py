"""The language faculty: a small pretrained model, frozen, used as senses and a mouth.

It reads and it speaks, and nothing it does changes its weights. Everything the
machine learns lives in the memory beside it, which is the part this branch
builds. The faculty is a part that can be swapped for a bigger or smaller one
without anything else changing, and the reading records which one ran.

Qwen3-1.7B in float32 is about 7 GB and fits the 1080 Ti with room to spare.
Float32 on purpose: Pascal runs float16 arithmetic at a small fraction of its
float32 rate, so half precision would be slower here, not faster. Decoding is
greedy, because a yardstick must not move with a sampler.
"""

from __future__ import annotations

import time
from dataclasses import dataclass, field

MODEL = "Qwen/Qwen3-1.7B"


@dataclass
class Cost:
    calls: int = 0
    tokens_in: int = 0
    tokens_out: int = 0
    seconds: float = 0.0

    def row(self) -> dict:
        return {"calls": self.calls, "tokens_in": self.tokens_in,
                "tokens_out": self.tokens_out, "seconds": round(self.seconds, 1)}


@dataclass
class Faculty:
    model_id: str = MODEL
    device: str = "cuda"
    cost: Cost = field(default_factory=Cost)

    def __post_init__(self) -> None:
        import torch
        from transformers import AutoModelForCausalLM, AutoTokenizer

        self._torch = torch
        self.tokenizer = AutoTokenizer.from_pretrained(self.model_id)
        self.model = AutoModelForCausalLM.from_pretrained(
            self.model_id, dtype=torch.float32
        ).to(self.device).eval()
        self.params = sum(p.numel() for p in self.model.parameters())

    @property
    def name(self) -> str:
        return f"{self.model_id} [{self.device} fp32, frozen]"

    def chat(self, system: str, user: str, budget: int = 32) -> str:
        """One greedy reply to one message, with thinking switched off."""
        messages = [{"role": "system", "content": system}, {"role": "user", "content": user}]
        prompt = self.tokenizer.apply_chat_template(
            messages, tokenize=False, add_generation_prompt=True, enable_thinking=False
        )
        batch = self.tokenizer(prompt, return_tensors="pt").to(self.device)
        started = time.perf_counter()
        with self._torch.no_grad():
            out = self.model.generate(
                **batch, max_new_tokens=budget, do_sample=False,
                temperature=None, top_p=None, top_k=None,
                pad_token_id=self.tokenizer.eos_token_id,
            )
        new = out[0, batch["input_ids"].shape[1]:]
        self.cost.calls += 1
        self.cost.tokens_in += int(batch["input_ids"].shape[1])
        self.cost.tokens_out += int(new.shape[0])
        self.cost.seconds += time.perf_counter() - started
        return self.tokenizer.decode(new, skip_special_tokens=True).strip()

    def surprise(self, text: str) -> float:
        """Mean negative log-likelihood per token of `text`, read off the logits.

        A sensor rather than a self-report: it measures how unexpected the text
        was to the faculty, and nothing the faculty says can move it.
        """
        ids = self.tokenizer(text, return_tensors="pt").input_ids.to(self.device)
        if ids.shape[1] < 2:
            return 0.0
        with self._torch.no_grad():
            loss = self.model(ids, labels=ids).loss
        return float(loss)


@dataclass
class ServedFaculty:
    """A faculty behind an OpenAI-compatible server, such as llama.cpp's.

    This is how a model too big for float32 in-process gets onto the card:
    Qwen3.5-9B at Q6 through llama-server, the setup Persistence already runs.
    Temperature zero, thinking off at the server (`--reasoning-budget 0`).
    It has no `surprise`; a sensor that needs logits runs on `Faculty`.
    """

    model_id: str = "Qwen3.5-9B-Q6_K_L (llama.cpp)"
    url: str = "http://127.0.0.1:8093/v1/chat/completions"
    cost: Cost = field(default_factory=Cost)
    params: int = 9_000_000_000

    @property
    def name(self) -> str:
        return f"{self.model_id} [served, frozen]"

    def chat(self, system: str, user: str, budget: int = 32) -> str:
        import json
        import urllib.request

        body = json.dumps({
            "messages": [{"role": "system", "content": system},
                         {"role": "user", "content": user}],
            "max_tokens": budget, "temperature": 0, "seed": 0,
        }).encode()
        request = urllib.request.Request(self.url, body, {"Content-Type": "application/json"})
        started = time.perf_counter()
        with urllib.request.urlopen(request, timeout=600) as response:
            reply = json.loads(response.read())
        usage = reply.get("usage", {})
        self.cost.calls += 1
        self.cost.tokens_in += int(usage.get("prompt_tokens", 0))
        self.cost.tokens_out += int(usage.get("completion_tokens", 0))
        self.cost.seconds += time.perf_counter() - started
        return (reply["choices"][0]["message"].get("content") or "").strip()
