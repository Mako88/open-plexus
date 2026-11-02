"""Step 5, continual state: the conversation's working state saved to disk and reloaded, so a
restart continues mid-conversation rather than re-reading everything said so far. The first
form is the served trunk's own: llama-server saves a slot's sequence state (the attention KV
and, for this trunk, the recurrent layers' state) to a file under `--slot-save-path` and
restores it into a slot after a restart, where the next request's common prefix with the
restored tokens is served from it. `SlotState` is that file; `turn` is one assistant turn on a
named slot with what it cost in prompt tokens, so a reading sees whether the restored state was
used (few prompt tokens) or ignored (the whole prefix again) and whether the answer and its
first token's log-probability are the unbroken conversation's.

The transcript is rendered here, not by the server's chat template, and sent to the raw
completion endpoint: with thinking off the generation prompt carries an empty thinking block
that the template leaves out when it renders a past assistant turn, so a history re-rendered by
the template diverges from the slot's tokens at every assistant turn and the saved state is never
reused (read 2026-11-02). A transcript this code renders is the slot's own tokens, extended.
"""

from __future__ import annotations

import json
import os
import time
import urllib.request

EMPTY_THOUGHT = "<think>\n\n</think>\n\n"


def server_root(url: str | None = None) -> str:
    return (url or os.environ.get("LOCAL_SERVER", "http://127.0.0.1:8093")).rstrip("/")


def render(messages: list[dict]) -> str:
    """The conversation as the trunk's ChatML, ending in the assistant's generation prompt with the
    empty thinking block, every past assistant turn carrying the same block it was generated after."""
    out = []
    for m in messages:
        body = (EMPTY_THOUGHT if m["role"] == "assistant" else "") + m["content"]
        out.append(f"<|im_start|>{m['role']}\n{body}<|im_end|>\n")
    out.append(f"<|im_start|>assistant\n{EMPTY_THOUGHT}")
    return "".join(out)


class SlotState:
    """One slot's saved state, by file name under the server's `--slot-save-path`."""

    def __init__(self, name: str, url: str | None = None, slot: int = 0):
        self.name = name
        self.root = server_root(url)
        self.slot = slot

    def _act(self, action: str) -> dict:
        body = json.dumps({"filename": self.name}).encode()
        req = urllib.request.Request(f"{self.root}/slots/{self.slot}?action={action}", body,
                                     {"Content-Type": "application/json"}, method="POST")
        with urllib.request.urlopen(req, timeout=600) as r:
            return json.load(r)

    def save(self) -> int:
        """Write the slot's state; the number of tokens it holds."""
        return int(self._act("save").get("n_saved", 0))

    def restore(self) -> int:
        """Load the file into the slot; the number of tokens restored."""
        return int(self._act("restore").get("n_restored", 0))

    def erase(self) -> None:
        self._act("erase")


def turn(messages: list[dict], url: str | None = None, slot: int = 0, max_new: int = 24, top: int = 5) -> dict:
    """One greedy assistant turn pinned to a slot, thinking off: the reply, the prompt tokens the
    server had to process for it (the rest came from the slot's cache), its time, and the first
    reply token's log-probability with the top alternatives."""
    request = {"prompt": render(messages), "n_predict": max_new, "temperature": 0, "seed": 0, "id_slot": slot,
               "cache_prompt": True, "n_probs": top}
    req = urllib.request.Request(f"{server_root(url)}/completion", json.dumps(request).encode(),
                                 {"Content-Type": "application/json"})
    started = time.perf_counter()
    with urllib.request.urlopen(req, timeout=1800) as r:
        reply = json.load(r)
    timings = reply.get("timings", {})
    first = (reply.get("completion_probabilities") or [{}])[0]
    return {
        "content": (reply.get("content") or "").strip(),
        "prompt_n": int(timings.get("prompt_n", reply.get("tokens_evaluated", 0))),
        "prompt_ms": round(float(timings.get("prompt_ms", 0.0)), 1),
        "predicted_n": int(timings.get("predicted_n", reply.get("tokens_predicted", 0))),
        "stop": reply.get("stop_type"),
        "seconds": round(time.perf_counter() - started, 2),
        "first_token": first.get("token"),
        "first_logprob": first.get("logprob"),
        "first_top": {x["token"]: round(x["logprob"], 4) for x in first.get("top_logprobs", [])},
    }


def same_state(unbroken: dict, other: dict) -> dict:
    """How far another turn is from the unbroken conversation's: the same reply, and the gap in
    the first token's log-probability (zero when the state is the same to the precision of the card)."""
    a, b = unbroken.get("first_logprob"), other.get("first_logprob")
    return {
        "same_reply": unbroken["content"] == other["content"],
        "same_first_token": unbroken.get("first_token") == other.get("first_token"),
        "logprob_gap": None if a is None or b is None else round(abs(a - b), 4),
    }
