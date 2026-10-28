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
                with urllib.request.urlopen(req, timeout=300) as resp:
                    reply = json.loads(resp.read())
                break
            except Exception:  # noqa: BLE001
                if attempt == 4:
                    raise
                time.sleep(2 ** attempt)
        u = reply.get("usage", {})
        self.tokens_in += int(u.get("prompt_tokens", 0))
        self.tokens_out += int(u.get("completion_tokens", 0))
        return (reply["choices"][0]["message"].get("content") or "").strip()

    def ask_many(self, prompts: list[list[dict]]) -> list[str]:
        with ThreadPoolExecutor(max_workers=self.parallel) as pool:
            return list(pool.map(self.ask, prompts))


def question_messages(rows: list[str], question: str) -> list[dict]:
    """The same turn as step 0's ceiling, so a store arm and the ceiling differ only in which
    sentences stand in the story."""
    story = "\n".join(rows) if rows else "(no story given)"
    return [{"role": "system", "content": SYSTEM},
            {"role": "user", "content": f"Story:\n{story}\n\nQuestion: {question}"}]
