"""A `Core` with no model in it, so the guards can run in seconds on no GPU.

WHY THIS EXISTS. `tests/guards` runs on every commit and must stay in seconds.
Loading a real checkpoint is ten seconds and two gigabytes of VRAM, and the GPU
is one card and it is shared -- a guard suite that grabs it would collide with
whatever training cycle is running. So the guards test the PARTS AROUND the
model: that a state round-trips through a file, that a transcript survives a
restart, that a thread refuses a state from the wrong checkpoint.

WHAT IT DOES NOT TEST, and this is the honest limit: whether `RwkvCore` obeys
the same contract. Nothing here can catch a real core mutating a state in place.
`tests/exam` is where the real core is exercised, by hand, on the card.
"""

from __future__ import annotations

import json
import os
from collections.abc import Sequence
from pathlib import Path
from typing import Any

import numpy as np

from sylvatica.core.base import Cost, forward_flops


class StubCore:
    """Deterministic, tokenless, and shaped exactly like the protocol.

    The "state" is a list of counts: how many tokens have been fed. That makes
    it easy to assert that a saved state came back -- the number is the history.
    The "tokenizer" is byte-level, so `decode(encode(s)) == s` for ASCII.
    """

    def __init__(self, n_layer: int = 2, n_embd: int = 8, params: int = 1000) -> None:
        self._n_layer = n_layer
        self._n_embd = n_embd
        self._params = params

    @property
    def params(self) -> int:
        return self._params

    @property
    def name(self) -> str:
        return f"stub-{self._n_layer}x{self._n_embd}"

    @property
    def n_layer(self) -> int:
        return self._n_layer

    @property
    def n_embd(self) -> int:
        return self._n_embd

    def encode(self, text: str) -> list[int]:
        return list(text.encode("utf-8"))

    def decode(self, tokens: Sequence[int]) -> str:
        return bytes(int(t) % 256 for t in tokens).decode("utf-8", errors="replace")

    def fresh_state(self) -> Any:
        return None

    def copy_state(self, state: Any | None) -> Any | None:
        return None if state is None else list(state)

    def state_bytes(self, state: Any | None) -> int:
        return 0 if state is None else 8 * len(state)

    def feed(self, tokens: Sequence[int], state: Any | None) -> tuple[Any, Cost]:
        tokens = list(tokens)
        working = [0] * self._n_layer if state is None else list(state)
        for i in range(self._n_layer):
            working[i] += len(tokens)
        return working, Cost(len(tokens), 0, 0.001, forward_flops(self._params, len(tokens)))

    def generate(
        self, prompt_tokens: Sequence[int], state: Any | None, budget: int, sampling: Any = None
    ) -> tuple[list[int], Any, Cost]:
        # Says the same thing every time, truncated to the budget, so a test
        # asserting on an answer is asserting on the harness and not on a model.
        reply = self.encode("ok")[:budget]
        state, _ = self.feed(list(prompt_tokens) + reply, state)
        return (
            reply,
            state,
            Cost(
                len(prompt_tokens),
                len(reply),
                0.001,
                forward_flops(self._params, len(prompt_tokens) + len(reply)),
            ),
        )

    def embed(self, tokens: Sequence[int]) -> np.ndarray:
        v = np.zeros(self._n_embd, dtype=np.float32)
        for i, t in enumerate(tokens):
            v[i % self._n_embd] += float(t)
        n = float(np.linalg.norm(v))
        return v / n if n else v

    def save_state(self, state: Any | None, path: Path) -> None:
        path = Path(path)
        path.parent.mkdir(parents=True, exist_ok=True)
        payload = {"core": self.name, "n_layer": self._n_layer, "n_embd": self._n_embd,
                   "tensors": state}
        tmp = path.with_suffix(path.suffix + ".tmp")
        tmp.write_text(json.dumps(payload), encoding="utf-8")
        os.replace(tmp, path)

    def load_state(self, path: Path) -> Any | None:
        payload = json.loads(Path(path).read_text(encoding="utf-8"))
        if payload["n_layer"] != self._n_layer or payload["n_embd"] != self._n_embd:
            raise ValueError(
                f"state was saved from {payload['core']} and this core is {self.name}"
            )
        return payload["tensors"]
