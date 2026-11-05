"""A reranker over the lexical sense: a small cross-encoder reads the query beside each of BM25's
top rows together and orders them by how well the row answers it. Twice now more finding has not
become more answering (2026-11-05: the named title, HyDE): given a right passage among wrong ones
the asker answers no better, so the lever is the right passage first. The cross-encoder is
MS MARCO's MiniLM (`cross-encoder/ms-marco-MiniLM-L-6-v2`, 22M parameters), off the shelf, run on
the CPU beside the served trunk; a sense is a function of queries, k and rows to skip, so this
wraps any first-stage sense.
"""

from __future__ import annotations

MODEL = "cross-encoder/ms-marco-MiniLM-L-6-v2"


class Reranker:
    def __init__(self, texts, model_id: str = MODEL, device: str = "cpu", batch: int = 32, max_length: int = 256):
        import torch
        from transformers import AutoModelForSequenceClassification, AutoTokenizer

        self.torch, self.texts, self.device, self.batch, self.max_length = torch, texts, device, batch, max_length
        self.tok = AutoTokenizer.from_pretrained(model_id)
        self.model = AutoModelForSequenceClassification.from_pretrained(model_id).to(device).eval()

    def scores(self, query: str, rows: list[int]) -> list[float]:
        out = []
        for at in range(0, len(rows), self.batch):
            chunk = rows[at:at + self.batch]
            enc = self.tok([query] * len(chunk), [self.texts(r) for r in chunk], padding=True, truncation=True,
                           max_length=self.max_length, return_tensors="pt").to(self.device)
            with self.torch.no_grad():
                out.extend(self.model(**enc).logits[:, 0].float().tolist())
        return out

    def over(self, nearest, deep: int = 50):
        """The sense `nearest` with its top `deep` rows reordered by the cross-encoder."""
        def search(queries, k, exclude):
            out = []
            for q in queries:
                rows = nearest([q], deep, exclude)[0]
                if not rows:
                    out.append([])
                    continue
                s = self.scores(q, rows)
                out.append([r for _, r in sorted(zip(s, rows), key=lambda x: -x[0])][:k])
            return out
        return search
