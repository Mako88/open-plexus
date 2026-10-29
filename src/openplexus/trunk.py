"""The trunk as a served model: llama-server behind an OpenAI-style endpoint, which is several
times faster on this card than fp32 PyTorch for anything that only needs text out. Launch it
with `--jinja --reasoning-budget 0`; the client also sends `enable_thinking` so a reading can
show in its own record that thinking was off.
"""

from __future__ import annotations

import json
import os
import time
import urllib.request
from concurrent.futures import ThreadPoolExecutor

SYSTEM = (
    "You are given a short story and a question about it. Answer using only the story. "
    "Reply with the answer word only, nothing else."
)


class ServedTrunk:
    def __init__(self, model: str = "trunk", url: str | None = None, max_new: int = 8,
                 parallel: int = 8, think: bool = False):
        self.model = model
        self.url = (url or os.environ.get("LOCAL_SERVER", "http://127.0.0.1:8093")) \
            + "/v1/chat/completions"
        self.max_new = max_new
        self.parallel = parallel
        self.think = think
        self.tokens_in = 0
        self.tokens_out = 0
        self.thoughts: dict[int, str] = {}  # id(messages) -> the reasoning the server returned

    def served_model(self) -> str:
        """What the server says it is serving, for the reading's record."""
        with urllib.request.urlopen(self.url.rsplit("/", 2)[0] + "/models", timeout=10) as r:
            return json.load(r)["data"][0]["id"]

    def ask(self, messages: list[dict]) -> str:
        body = json.dumps({
            "model": self.model, "messages": messages, "max_tokens": self.max_new,
            "temperature": 0, "seed": 0,
            "chat_template_kwargs": {"enable_thinking": self.think},
        }).encode()
        req = urllib.request.Request(self.url, body, {"Content-Type": "application/json"})
        for attempt in range(5):
            try:
                with urllib.request.urlopen(req, timeout=1800) as resp:
                    reply = json.loads(resp.read())
                break
            except Exception:  # noqa: BLE001
                if attempt == 4:
                    raise
                time.sleep(2 ** attempt)
        u = reply.get("usage", {})
        self.tokens_in += int(u.get("prompt_tokens", 0))
        self.tokens_out += int(u.get("completion_tokens", 0))
        msg = reply["choices"][0]["message"]
        if self.think:
            self.thoughts[id(messages)] = (msg.get("reasoning_content") or msg.get("reasoning") or "").strip()
        return (msg.get("content") or "").strip()

    def ask_many(self, prompts: list[list[dict]]) -> list[str]:
        with ThreadPoolExecutor(max_workers=self.parallel) as pool:
            return list(pool.map(self.ask, prompts))

    def thought_of(self, messages: list[dict]) -> str | None:
        """The reasoning the server returned for a thinking turn already asked, for the
        reading's rows: a stop over the thinking stream is read first on recorded thoughts."""
        return self.thoughts.get(id(messages))


def question_messages(rows: list[str], question: str) -> list[dict]:
    """The same turn as step 0's ceiling, so a store arm and the ceiling differ only in which
    sentences stand in the story."""
    story = "\n".join(rows) if rows else "(no story given)"
    return [{"role": "system", "content": SYSTEM},
            {"role": "user", "content": f"Story:\n{story}\n\nQuestion: {question}"}]


class LocalTrunk:
    """The trunk in PyTorch, for a reading that needs its hidden states as well as its answers.
    Several times slower than the served trunk on this card; use it only when a reading needs
    what the server cannot give."""

    def __init__(self, model_id: str = "Qwen/Qwen3.5-2B", batch: int = 8, max_new: int = 8,
                 dtype: str = "float32"):
        import torch
        from transformers import AutoModelForCausalLM, AutoTokenizer

        self.torch = torch
        self.model_id = model_id
        self.tok = AutoTokenizer.from_pretrained(model_id)
        self.tok.padding_side = "left"
        if self.tok.pad_token is None:
            self.tok.pad_token = self.tok.eos_token
        self.dtype = dtype  # bfloat16 halves the card the trunk takes, to sit beside the server
        self.model = AutoModelForCausalLM.from_pretrained(model_id, dtype=getattr(torch, dtype))
        self.model = self.model.to("cuda").eval()
        self.batch = batch
        self.max_new = max_new
        self.tokens_in = 0
        self.tokens_out = 0

    def served_model(self) -> str:
        return f"{self.model_id} {self.dtype} in process"

    def ask_many(self, prompts: list[list[dict]]) -> list[str]:
        out: list[str] = []
        for i in range(0, len(prompts), self.batch):
            texts = [self.tok.apply_chat_template(m, tokenize=False, add_generation_prompt=True,
                                                  enable_thinking=False)
                     for m in prompts[i:i + self.batch]]
            enc = self.tok(texts, return_tensors="pt", padding=True).to("cuda")
            with self.torch.no_grad():
                gen = self.model.generate(**enc, max_new_tokens=self.max_new, do_sample=False,
                                          temperature=None, top_p=None, top_k=None,
                                          pad_token_id=self.tok.pad_token_id)
            new = gen[:, enc["input_ids"].shape[1]:]
            self.tokens_in += int(enc["attention_mask"].sum())
            self.tokens_out += int((new != self.tok.pad_token_id).sum())
            out.extend(self.tok.decode(ids, skip_special_tokens=True).strip() for ids in new)
        return out

    def hidden(self, texts: list[str], layers: list[int]):
        """The last token's hidden state at each of `layers` (0 is the embeddings), one row per
        text, as float32 numpy arrays keyed by layer."""
        import numpy as np

        out = {layer: [] for layer in layers}
        for i in range(0, len(texts), self.batch):
            enc = self.tok(texts[i:i + self.batch], return_tensors="pt", padding=True).to("cuda")
            with self.torch.no_grad():
                hs = self.model(**enc, output_hidden_states=True).hidden_states
            for layer in layers:
                out[layer].append(hs[layer][:, -1, :].float().cpu().numpy())  # left padding
        return {layer: np.concatenate(v) for layer, v in out.items()}

    def close(self) -> None:
        del self.model
        self.torch.cuda.empty_cache()
