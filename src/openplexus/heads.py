"""The glue's heads: small maps from the trunk's hidden state to a decision, fitted in closed
form by ridge regression on the train split's supporting rows. Each takes seconds and has no
training loop; a head that needs gradient has to beat it first.

The walker's head maps the state after reading the question and the rows found so far to the
key space of the store: which row to read next. The evaluator's head maps the state after
reading those and one candidate row to a score: whether that row is the right one.
"""

from __future__ import annotations

from pathlib import Path

import numpy as np


def hop_prompt(question: str, read: list[str]) -> str:
    """What the trunk reads before its state is taken: the question, the facts found so far,
    and a cue that the next thing is the fact still wanted."""
    known = "".join(f"Known: {r}\n" for r in read)
    return f"Question: {question}\n{known}Next fact:"


def judge_prompt(question: str, read: list[str], candidate: str) -> str:
    """The walker's prompt with one candidate filled in, and a cue that a judgement follows."""
    return f"{hop_prompt(question, read)} {candidate}" + chr(10) + "Is the next fact right:"


class LinearHead:
    def __init__(self, weight: np.ndarray, layer: int, lam: float, key: str,
                 threshold: float | None = None):
        self.weight = weight.astype(np.float32)  # (hidden, target dim)
        self.layer = layer
        self.lam = lam
        self.key = key  # what the target is: a store embedder's name, or "label"
        self.threshold = threshold  # for a label head: the score above which a candidate is good

    @classmethod
    def fit(cls, hidden: np.ndarray, targets: np.ndarray, lam: float, layer: int, key: str):
        h = hidden.astype(np.float64)
        k = targets.astype(np.float64)
        g = h.T @ h + lam * np.eye(h.shape[1])
        return cls(np.linalg.solve(g, h.T @ k), layer, lam, key)

    def query(self, hidden: np.ndarray) -> np.ndarray:
        return np.asarray(hidden, dtype=np.float32) @ self.weight

    def save(self, path: str | Path) -> None:
        np.savez(path, weight=self.weight, layer=self.layer, lam=self.lam, key=self.key,
                 threshold=np.nan if self.threshold is None else self.threshold)

    @classmethod
    def load(cls, path: str | Path):
        z = np.load(path, allow_pickle=False)
        threshold = float(z["threshold"]) if "threshold" in z else float("nan")
        return cls(z["weight"], int(z["layer"]), float(z["lam"]), str(z["key"]),
                   None if np.isnan(threshold) else threshold)
