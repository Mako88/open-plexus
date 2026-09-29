"""Text to a unit vector, for the store's meaning-side ranker.

MiniLM-L6 is 22M parameters, runs on a CPU and on a phone, and is independent of
the language faculty, so a retrieval failure and a faculty failure cannot be
confused. It stays on the CPU so it never competes with the faculty for the card.
`HashEmbedder` is for tests: character trigrams, no semantics at all.
"""

from __future__ import annotations

import hashlib
from typing import Protocol

import numpy as np

# MiniLM-L6: 22.7M parameters, 384 dimensions, and the standard small choice.
MODEL = "sentence-transformers/all-MiniLM-L6-v2"
DIMS = 384


class Embedder(Protocol):
    """Text to a unit vector. The store never cares which implementation."""

    @property
    def dims(self) -> int: ...

    def encode(self, texts: list[str]) -> np.ndarray: ...


class MiniLmEmbedder:
    """Mean pooling over non-padding tokens, which is what this checkpoint was trained with."""

    def __init__(self, model_id: str = MODEL, device: str = "cpu") -> None:
        from transformers import AutoModel, AutoTokenizer

        self.model_id = model_id
        self.device = device
        self.tokenizer = AutoTokenizer.from_pretrained(model_id)
        self.model = AutoModel.from_pretrained(model_id).to(device).eval()
        self.params = sum(p.numel() for p in self.model.parameters())

    @property
    def dims(self) -> int:
        return int(self.model.config.hidden_size)

    def encode(self, texts: list[str]) -> np.ndarray:
        import torch

        if not texts:
            return np.zeros((0, self.dims), dtype=np.float32)

        batch = self.tokenizer(
            texts, padding=True, truncation=True, max_length=256, return_tensors="pt"
        ).to(self.device)
        with torch.no_grad():
            out = self.model(**batch).last_hidden_state

        mask = batch["attention_mask"].unsqueeze(-1).float()
        pooled = (out * mask).sum(1) / mask.sum(1).clamp(min=1e-9)
        pooled = torch.nn.functional.normalize(pooled, p=2, dim=1)
        return pooled.cpu().numpy().astype(np.float32)


class HashEmbedder:
    """Deterministic trigram hashing. For tests; a reading taken with it is about plumbing."""

    def __init__(self, dims: int = 128) -> None:
        self._dims = dims

    @property
    def dims(self) -> int:
        return self._dims

    def encode(self, texts: list[str]) -> np.ndarray:
        out = np.zeros((len(texts), self._dims), dtype=np.float32)
        for row, text in enumerate(texts):
            lowered = f"  {text.lower()}  "
            for i in range(len(lowered) - 2):
                gram = lowered[i : i + 3]
                bucket = int(hashlib.md5(gram.encode()).hexdigest()[:8], 16) % self._dims
                out[row, bucket] += 1.0
            norm = float(np.linalg.norm(out[row]))
            if norm:
                out[row] /= norm
        return out
