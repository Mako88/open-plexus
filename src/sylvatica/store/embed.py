"""The small encoder that turns a fragment into a vector.

WHY AN EXTERNAL ENCODER AND NOT THE CORE. The doc: "a small off-the-shelf
sentence encoder (MiniLM-class, ~22M params, runs on CPU and on a phone)". Two
reasons, and the second is the one that matters. It is small enough to run on the
weak nodes Phase 5 is about, which the core is not. And it is INDEPENDENT of the
core, so a retrieval failure and a core failure cannot be confused -- if
embeddings came from the core, a core that had drifted would silently degrade
recall and the exam would read it as forgetting. This branch has already spent a
day on failures of exactly that shape.

"Core-derived embeddings in place of the external encoder" is an open fork and
`RwkvCore.embed` exists for it. It is a fork precisely because nobody has checked
that it ranks anything.

ON CPU BY DEFAULT, AND THAT IS NOT A COMPROMISE. The GPU is one card and it is
shared; an encoder that competed with a consolidation cycle for it would be a
scheduling problem for 22M parameters' worth of work. It also means the store
keeps working while the card is busy, which is what Phase 4's idle scheduler
needs.
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
    """MiniLM-L6 with mean pooling, which is what its training expects.

    MEAN POOLING AND NOT THE CLS TOKEN. This checkpoint was trained with mean
    pooling over the non-padding tokens; taking `last_hidden_state[:, 0]`
    instead produces vectors that look fine, normalise fine, and rank badly. It
    is the kind of mistake that shows up as "retrieval is weak" three phases
    later, so it is written down here rather than left to whoever reads the
    model card next.
    """

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
    """A deterministic, dependency-free stand-in. FOR TESTS, NEVER FOR A READING.

    Hashes character trigrams into a fixed number of buckets. It gives the store
    something with the right shape and the right determinism so the schema, the
    ranking arithmetic and the fusion can be tested in milliseconds without a
    model download.

    IT HAS NO SEMANTICS AT ALL. Two ways of saying the same thing land nowhere
    near each other. Any retrieval number taken with this is a number about
    string overlap, so the store records which embedder produced its vectors and
    a reading that names this one is a reading about plumbing.
    """

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
