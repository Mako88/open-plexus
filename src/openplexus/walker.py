"""The walker: the glue's first head, which says which row to read next.

A linear map from the trunk's hidden state, taken after it has read the question and the rows
found so far, to the key space of the store, fitted in closed form by ridge regression on the
train split's supporting rows. It takes seconds and has no training loop; a head that needs
gradient has to beat it first.
"""

from __future__ import annotations

from pathlib import Path

import numpy as np


def hop_prompt(question: str, read: list[str]) -> str:
    """What the trunk reads before its state is taken: the question, the facts found so far,
    and a cue that the next thing is the fact still wanted."""
    known = "".join(f"Known: {r}\n" for r in read)
    return f"Question: {question}\n{known}Next fact:"


class LinearWalker:
    def __init__(self, weight: np.ndarray, layer: int, lam: float, key: str):
        self.weight = weight.astype(np.float32)  # (hidden, key dim)
        self.layer = layer
        self.lam = lam
        self.key = key  # the store embedder whose keys this walker predicts

    @classmethod
    def fit(cls, hidden: np.ndarray, keys: np.ndarray, lam: float, layer: int, key: str):
        h = hidden.astype(np.float64)
        k = keys.astype(np.float64)
        g = h.T @ h + lam * np.eye(h.shape[1])
        return cls(np.linalg.solve(g, h.T @ k), layer, lam, key)

    def query(self, hidden: np.ndarray) -> np.ndarray:
        return np.asarray(hidden, dtype=np.float32) @ self.weight

    def save(self, path: str | Path) -> None:
        np.savez(path, weight=self.weight, layer=self.layer, lam=self.lam, key=self.key)

    @classmethod
    def load(cls, path: str | Path):
        z = np.load(path, allow_pickle=False)
        return cls(z["weight"], int(z["layer"]), float(z["lam"]), str(z["key"]))
