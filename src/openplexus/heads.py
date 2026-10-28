"""The glue's heads: small maps from the trunk's hidden state to a decision, fitted in closed
form by ridge regression on the train split's supporting rows. Each takes seconds and has no
training loop; a head that needs gradient has to beat it first.

The walker's head maps the state after reading the question and the rows found so far to the
key space of the store: which row to read next. The evaluator's head maps the state after
reading those and one candidate row to a score: whether that row is the right one. `MLPHead`
is the gradient-trained form of a label head, over the same features, trained listwise
against a stop option (`scripts/head_train.py`); `load_head` opens either kind.
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
                 threshold: float | None = None, mean: np.ndarray | None = None,
                 std: np.ndarray | None = None):
        self.weight = weight.astype(np.float32)  # (features, target dim)
        self.layer = layer
        self.lam = lam
        # what the target is: a store embedder's name, "label", or "label+time" when the
        # features are the hidden state followed by when-heard features
        self.key = key
        self.threshold = threshold  # for a label head: the score above which a candidate is good
        self.mean = mean  # standardisation of the features before the map, if fitted that way
        self.std = std

    @classmethod
    def fit(cls, hidden: np.ndarray, targets: np.ndarray, lam: float, layer: int, key: str):
        h = hidden.astype(np.float64)
        k = targets.astype(np.float64)
        g = h.T @ h + lam * np.eye(h.shape[1])
        return cls(np.linalg.solve(g, h.T @ k), layer, lam, key)

    def query(self, features: np.ndarray) -> np.ndarray:
        x = np.asarray(features, dtype=np.float32)
        if self.mean is not None:
            x = (x - self.mean) / self.std
        return x @ self.weight

    def save(self, path: str | Path) -> None:
        extra = {} if self.mean is None else {"mean": self.mean, "std": self.std}
        np.savez(path, weight=self.weight, layer=self.layer, lam=self.lam, key=self.key,
                 threshold=np.nan if self.threshold is None else self.threshold, **extra)

    @classmethod
    def load(cls, path: str | Path):
        z = np.load(path, allow_pickle=False)
        threshold = float(z["threshold"]) if "threshold" in z else float("nan")
        return cls(z["weight"], int(z["layer"]), float(z["lam"]), str(z["key"]),
                   None if np.isnan(threshold) else threshold,
                   z["mean"] if "mean" in z else None, z["std"] if "std" in z else None)


def when_heard(row: int, heard: int, last_read: int) -> list[float]:
    """When a candidate row was heard: its place in the story, its distance from the row last
    read, and whether it came before or after that row. Zero where nothing has been read."""
    if last_read < 0:
        return [row / heard, 0.0, 0.0, 0.0]
    return [row / heard, (row - last_read) / heard, float(row < last_read), float(row > last_read)]


class MLPHead:
    """A small MLP scoring each candidate, with a learned stop logit; `query` returns the score
    minus the stop logit, so a candidate is followed when it is above 0 and `threshold` is 0.
    Standardisation and the when-heard flag follow LinearHead, so `select` treats both alike."""

    def __init__(self, in_dim: int, layer: int, key: str, hidden: int = 256,
                 mean: np.ndarray | None = None, std: np.ndarray | None = None):
        import torch
        from torch import nn

        self.torch = torch
        self.layer = layer
        self.key = key
        self.lam = 0.0
        self.threshold = 0.0
        self.mean = mean
        self.std = std
        self.hidden = hidden
        self.in_dim = in_dim
        self.net = nn.Sequential(nn.Linear(in_dim, hidden), nn.GELU(), nn.Linear(hidden, 1))
        self.stop = nn.Parameter(torch.zeros(1))

    def parameters(self):
        return list(self.net.parameters()) + [self.stop]

    def to(self, device):
        self.net.to(device)
        self.stop.data = self.stop.data.to(device)
        return self

    def logits(self, x):
        """Scores for a (..., in_dim) tensor of standardised features, as (...)."""
        return self.net(x).squeeze(-1)

    def standardise(self, features: np.ndarray):
        x = np.asarray(features, dtype=np.float32)
        if self.mean is not None:
            x = (x - self.mean) / self.std
        return self.torch.as_tensor(x, device=self.stop.device)

    def query(self, features: np.ndarray) -> np.ndarray:
        self.net.eval()
        with self.torch.no_grad():
            s = self.logits(self.standardise(features)) - self.stop
        return s.reshape(-1, 1).cpu().numpy()

    def save(self, path: str | Path) -> None:
        path = Path(path)
        self.torch.save({"net": self.net.state_dict(), "stop": self.stop.data.cpu(),
                         "in_dim": self.in_dim, "hidden": self.hidden, "layer": self.layer,
                         "key": self.key, "mean": self.mean, "std": self.std}, path)

    @classmethod
    def load(cls, path: str | Path, device: str = "cpu"):
        import torch

        z = torch.load(Path(path), weights_only=False, map_location="cpu")
        head = cls(z["in_dim"], z["layer"], z["key"], z["hidden"], z["mean"], z["std"])
        head.net.load_state_dict(z["net"])
        head.stop.data = z["stop"]
        return head.to(device)


def load_head(path: str | Path):
    """A LinearHead from .npz, an MLPHead from .pt."""
    path = Path(path)
    return MLPHead.load(path) if path.suffix == ".pt" else LinearHead.load(path)
