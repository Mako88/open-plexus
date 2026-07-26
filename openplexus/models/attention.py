"""A tiny trainable attention model — the smallest thing that can represent recall.

One attention layer with **shifted values**: attending to position `s` retrieves
an embedding of the token at `s + 1`. That is the induction-head shape written
directly into the architecture, so the model *can* express "find where this token
appeared before, report what followed" without needing to discover a two-layer
composition first.

That choice is deliberate and it biases the experiment **in favour** of the
question we are asking. We want to know whether a predictive objective can find
the solution; giving the architecture no way to express the solution would
conflate "the objective cannot bootstrap" with "the model cannot represent it".
If training fails even here, that is informative. If it succeeds, the next
question is whether it still succeeds without the shift handed over.

This is the first numpy in the project. It is confined to the model layer —
`openplexus/tasks/` and `openplexus/baselines.py` stay dependency-free, because
they are the ruler.

Gradients are hand-derived and **checked against finite differences** in
`tests/test_attention.py`. An unchecked gradient is the purest form of the
failure this project is built against: it runs, it produces a falling loss, and
it optimises something other than what you wrote down.
"""

from __future__ import annotations

from dataclasses import dataclass

import numpy as np


@dataclass(frozen=True)
class AttentionConfig:
    """Shape and training settings.

    Attributes:
        vocab_size: Token alphabet size.
        d_model: Width of embeddings and of the attention projections.
        seed: Determines initialisation completely.
        init_scale: Standard deviation of the initial weights.
    """

    vocab_size: int
    d_model: int = 32
    seed: int = 0
    init_scale: float = 0.1

    def __post_init__(self) -> None:
        if self.vocab_size < 2:
            raise ValueError("vocab_size must be at least 2")
        if self.d_model < 1:
            raise ValueError("d_model must be at least 1")
        if self.init_scale <= 0.0:
            raise ValueError("init_scale must be positive")


class ShiftedAttention:
    """Single-head causal attention whose values are shifted one position.

    The contract: `forward` maps a token sequence to per-position logits over the
    vocabulary; `backward` returns gradients of the mean cross-entropy at the
    scored positions with respect to every parameter. Parameters live in
    `self.params` and are updated in place by an optimiser.
    """

    PARAM_NAMES = ("embed", "wq", "wk", "wv", "wo")

    def __init__(self, config: AttentionConfig) -> None:
        self.config = config
        rng = np.random.default_rng(config.seed)
        d, v, s = config.d_model, config.vocab_size, config.init_scale
        self.params: dict[str, np.ndarray] = {
            "embed": rng.normal(0.0, s, (v, d)),
            "wq": rng.normal(0.0, s, (d, d)),
            "wk": rng.normal(0.0, s, (d, d)),
            "wv": rng.normal(0.0, s, (d, d)),
            "wo": rng.normal(0.0, s, (d, v)),
        }

    def forward(self, tokens: np.ndarray) -> tuple[np.ndarray, dict]:
        """Return per-position logits and the cache `backward` needs.

        Position `t` attends only to positions `s < t`. Strictly causal, and
        strict is required rather than convenient: the value at `s` is the
        embedding of the token at `s + 1`, so allowing `s = t` would let the
        model read its own target.
        """
        p = self.params
        d = self.config.d_model
        T = len(tokens)

        h = p["embed"][tokens]                       # (T, d)
        shifted = np.zeros_like(h)
        shifted[:-1] = h[1:]                         # value source: token at s+1

        q = h @ p["wq"]
        k = h @ p["wk"]
        v = shifted @ p["wv"]

        scores = (q @ k.T) / np.sqrt(d)
        mask = np.tril(np.ones((T, T), dtype=bool), k=-1)
        scores = np.where(mask, scores, -np.inf)
        # A row with nothing to attend to (position 0) is all -inf; softmax would
        # produce nan. Zero it explicitly rather than letting a nan propagate
        # into the loss, where it would look like divergence.
        scores[0, :] = 0.0

        shifted_max = scores.max(axis=1, keepdims=True)
        exp = np.exp(scores - shifted_max)
        exp = np.where(mask, exp, 0.0)
        denom = exp.sum(axis=1, keepdims=True)
        denom[0] = 1.0
        attn = exp / denom

        out = attn @ v
        logits = out @ p["wo"]
        cache = dict(tokens=tokens, h=h, shifted=shifted, q=q, k=k, v=v,
                     attn=attn, out=out, mask=mask)
        return logits, cache

    def loss_and_backward(
        self, logits: np.ndarray, cache: dict,
        targets: np.ndarray, scored: np.ndarray,
    ) -> tuple[float, dict[str, np.ndarray]]:
        """Mean cross-entropy over `scored` positions, and its gradients.

        Args:
            logits: From `forward`.
            targets: Target token per position; only `scored` entries are read.
            scored: Boolean mask of positions that contribute to the loss.
        """
        p, c = self.params, cache
        d = self.config.d_model
        n = int(scored.sum())
        if n == 0:
            raise ValueError("no scored positions")

        shifted = logits - logits.max(axis=1, keepdims=True)
        probs = np.exp(shifted)
        probs /= probs.sum(axis=1, keepdims=True)
        loss = -np.log(probs[scored, targets[scored]] + 1e-12).mean()

        dlogits = np.zeros_like(logits)
        dlogits[scored] = probs[scored]
        dlogits[scored, targets[scored]] -= 1.0
        dlogits /= n

        grads = {name: np.zeros_like(p[name]) for name in self.PARAM_NAMES}
        grads["wo"] = c["out"].T @ dlogits
        d_out = dlogits @ p["wo"].T

        d_attn = d_out @ c["v"].T
        d_v = c["attn"].T @ d_out

        # softmax Jacobian, row-wise
        d_scores = c["attn"] * (d_attn - (d_attn * c["attn"]).sum(axis=1, keepdims=True))
        d_scores = np.where(c["mask"], d_scores, 0.0) / np.sqrt(d)

        d_q = d_scores @ c["k"]
        d_k = d_scores.T @ c["q"]

        grads["wq"] = c["h"].T @ d_q
        grads["wk"] = c["h"].T @ d_k
        grads["wv"] = c["shifted"].T @ d_v

        d_h = d_q @ p["wq"].T + d_k @ p["wk"].T
        d_shifted = d_v @ p["wv"].T
        d_h[1:] += d_shifted[:-1]          # undo the value shift

        np.add.at(grads["embed"], c["tokens"], d_h)
        return float(loss), grads

    def predict(self, tokens: np.ndarray) -> np.ndarray:
        """Highest-scoring next token at each position."""
        logits, _ = self.forward(tokens)
        return logits.argmax(axis=1)


class Adam:
    """Adam, written out rather than imported, so the update rule is inspectable."""

    def __init__(self, params: dict[str, np.ndarray], lr: float = 1e-2,
                 beta1: float = 0.9, beta2: float = 0.999, eps: float = 1e-8) -> None:
        self.params, self.lr = params, lr
        self.beta1, self.beta2, self.eps = beta1, beta2, eps
        self.m = {k: np.zeros_like(v) for k, v in params.items()}
        self.v = {k: np.zeros_like(v) for k, v in params.items()}
        self.t = 0

    def step(self, grads: dict[str, np.ndarray]) -> None:
        self.t += 1
        for name, grad in grads.items():
            self.m[name] = self.beta1 * self.m[name] + (1 - self.beta1) * grad
            self.v[name] = self.beta2 * self.v[name] + (1 - self.beta2) * grad**2
            m_hat = self.m[name] / (1 - self.beta1**self.t)
            v_hat = self.v[name] / (1 - self.beta2**self.t)
            self.params[name] -= self.lr * m_hat / (np.sqrt(v_hat) + self.eps)
