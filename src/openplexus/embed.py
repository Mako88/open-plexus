"""Embedders: a sentence in, one vector out. The vector is the store's key for the sentence.

Two kinds, as the plan asks to try both: a small frozen sentence encoder (MiniLM, a sense the
"no LLM" rule allows, since nothing here trains it), and the trunk's own pooled hidden state,
which is what step 2's re-query will be made of, so its quality as a key is worth knowing now.
Every embedder returns L2-normalised float32 rows, so a dot product is a cosine.
"""

from __future__ import annotations

import numpy as np


class Embedder:
    name: str
    dim: int
    device: str = "cuda"

    def __call__(self, texts: list[str]) -> np.ndarray:
        raise NotImplementedError

    def close(self) -> None:
        """Free the model so another can use the card."""


def _normalise(x: np.ndarray) -> np.ndarray:
    x = np.asarray(x, dtype=np.float32)
    return x / np.maximum(np.linalg.norm(x, axis=1, keepdims=True), 1e-12)


class MiniLM(Embedder):
    """sentence-transformers/all-MiniLM-L6-v2 through transformers alone: mean pooling over the
    attention mask, as the model card does."""

    name = "minilm"
    dim = 384

    def __init__(self, model_id: str = "sentence-transformers/all-MiniLM-L6-v2", batch: int = 128):
        import torch
        from transformers import AutoModel, AutoTokenizer

        self.torch = torch
        self.tok = AutoTokenizer.from_pretrained(model_id)
        self.model = AutoModel.from_pretrained(model_id).to(self.device).eval()
        self.batch = batch

    def __call__(self, texts: list[str]) -> np.ndarray:
        out = []
        for i in range(0, len(texts), self.batch):
            enc = self.tok(texts[i:i + self.batch], padding=True, truncation=True,
                           max_length=128, return_tensors="pt").to(self.device)
            with self.torch.no_grad():
                h = self.model(**enc).last_hidden_state
            m = enc["attention_mask"].unsqueeze(-1).to(h.dtype)
            pooled = (h * m).sum(1) / m.sum(1).clamp(min=1)
            out.append(pooled.float().cpu().numpy())
        return _normalise(np.concatenate(out))

    def close(self) -> None:
        del self.model
        self.torch.cuda.empty_cache()


class TrunkPooled(Embedder):
    """The trunk's last hidden layer, mean-pooled over the sentence's tokens, with no prompt
    around the sentence. Untrained as a retrieval key; this is the reading of how far the
    trunk's own state gets before any glue is fitted on it."""

    name = "trunk"

    def __init__(self, model_id: str = "Qwen/Qwen3.5-2B", batch: int = 32, pooling: str = "mean"):
        import torch
        from transformers import AutoModelForCausalLM, AutoTokenizer

        self.torch = torch
        self.tok = AutoTokenizer.from_pretrained(model_id)
        self.tok.padding_side = "right"
        if self.tok.pad_token is None:
            self.tok.pad_token = self.tok.eos_token
        self.model = AutoModelForCausalLM.from_pretrained(model_id, dtype=torch.float32)
        self.model = self.model.to(self.device).eval()
        self.dim = int(self.model.config.get_text_config().hidden_size)
        self.batch = batch
        self.pooling = pooling
        self.name = "trunk" if pooling == "mean" else f"trunk-{pooling}"

    def __call__(self, texts: list[str]) -> np.ndarray:
        out = []
        for i in range(0, len(texts), self.batch):
            enc = self.tok(texts[i:i + self.batch], padding=True, return_tensors="pt")
            enc = enc.to(self.device)
            with self.torch.no_grad():
                h = self.model(**enc, output_hidden_states=True).hidden_states[-1]
            m = enc["attention_mask"]
            if self.pooling == "last":
                last = m.sum(1) - 1
                pooled = h[self.torch.arange(h.shape[0]), last]
            else:
                mf = m.unsqueeze(-1).to(h.dtype)
                pooled = (h * mf).sum(1) / mf.sum(1).clamp(min=1)
            out.append(pooled.float().cpu().numpy())
        return _normalise(np.concatenate(out))

    def close(self) -> None:
        del self.model
        self.torch.cuda.empty_cache()


def make(name: str) -> Embedder:
    if name == "minilm":
        return MiniLM()
    if name == "trunk":
        return TrunkPooled()
    if name == "trunk-last":
        return TrunkPooled(pooling="last")
    raise ValueError(name)
