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


def _gather_offsets(h: np.ndarray, offsets: tuple[int, ...]) -> np.ndarray:
    """Stack shifted copies of `h`, one per offset, zero-padded at the edges.

    `out[j, s] = h[s + offsets[j]]`, or zeros where that falls outside the
    sequence. Zeros are the honest encoding of "no token there" — wrapping would
    let the last position read the first, which is not a shift but a leak.
    """
    T = len(h)
    out = np.zeros((len(offsets), *h.shape))
    for j, offset in enumerate(offsets):
        if offset > 0:
            out[j, :-offset] = h[offset:]
        elif offset < 0:
            out[j, -offset:] = h[:offset]
        else:
            out[j] = h
    return out


@dataclass(frozen=True)
class AttentionConfig:
    """Shape and training settings.

    Attributes:
        vocab_size: Token alphabet size.
        d_model: Width of embeddings and of the attention projections.
        seed: Determines initialisation completely.
        init_scale: Standard deviation of the initial weights.
        value_offsets: Which positions the value at `s` may be drawn from,
            relative to `s`. The model learns a weight per offset and mixes them.

            `(1,)` — the default — hands the model the induction shape: the value
            at `s` is the token at `s+1`, so attending to `s` retrieves what
            followed it. That is a **hint**, and g1-02 was run with it.

            `(0, 1)` or `(-1, 0, 1, 2)` remove the hint by degrees: the model
            must *discover* that `+1` is the useful offset, among alternatives
            that are not. This is the dial that separates "the objective can find
            the task" from "the objective can find the task when told where to
            look".
    """

    vocab_size: int
    d_model: int = 32
    seed: int = 0
    init_scale: float = 0.1
    value_offsets: tuple[int, ...] = (1,)

    def __post_init__(self) -> None:
        if self.vocab_size < 2:
            raise ValueError("vocab_size must be at least 2")
        if self.d_model < 1:
            raise ValueError("d_model must be at least 1")
        if self.init_scale <= 0.0:
            raise ValueError("init_scale must be positive")
        if not self.value_offsets:
            raise ValueError("value_offsets must name at least one offset")
        if len(set(self.value_offsets)) != len(self.value_offsets):
            raise ValueError(f"value_offsets has duplicates: {self.value_offsets}")


class ShiftedAttention:
    """Single-head causal attention whose values are shifted one position.

    The contract: `forward` maps a token sequence to per-position logits over the
    vocabulary; `backward` returns gradients of the mean cross-entropy at the
    scored positions with respect to every parameter. Parameters live in
    `self.params` and are updated in place by an optimiser.
    """

    PARAM_NAMES = ("embed", "wq", "wk", "wv", "wo", "offset_mix")

    def __init__(self, config: AttentionConfig) -> None:
        self.config = config
        #: How far ahead the furthest value offset reads. The attention mask is
        #: pulled back by this much so nothing can reach past the current step.
        self.reach = max(1, max(config.value_offsets))
        rng = np.random.default_rng(config.seed)
        d, v, s = config.d_model, config.vocab_size, config.init_scale
        self.params: dict[str, np.ndarray] = {
            "embed": rng.normal(0.0, s, (v, d)),
            "wq": rng.normal(0.0, s, (d, d)),
            "wk": rng.normal(0.0, s, (d, d)),
            "wv": rng.normal(0.0, s, (d, d)),
            "wo": rng.normal(0.0, s, (d, v)),
            # One weight per candidate offset, centred on uniform mixing with
            # noise added. Centring keeps the value path at full magnitude --
            # initialising at ~0.1 attenuates it tenfold and visibly slows
            # training. The noise is what breaks symmetry: exactly equal weights
            # are a stationary point the gradient cannot leave, and the model
            # would sit there looking like it had learned nothing about where to
            # look. With the default (1,) this reduces to a weight near 1.0,
            # which is the hardcoded shift it replaces.
            "offset_mix": rng.normal(1.0 / len(config.value_offsets), s,
                                     (len(config.value_offsets),)),
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
        # Value source: a learned mixture over candidate offsets. With the
        # default (1,) this is exactly "the token at s+1" and the mixture weight
        # is a scalar Wv absorbs. With several offsets the model must find +1.
        sources = _gather_offsets(h, self.config.value_offsets)   # (J, T, d)
        shifted = np.tensordot(p["offset_mix"], sources, axes=(0, 0))

        q = h @ p["wq"]
        k = h @ p["wk"]
        v = shifted @ p["wv"]

        scores = (q @ k.T) / np.sqrt(d)
        # Position t may attend to s only if every offset it reads from stays at
        # or before t: s + max(offsets) <= t. With the default offset of +1 this
        # is the familiar strictly-causal mask.
        #
        # This is NOT a detail. An offset of +2 under a k=-1 mask lets position t
        # attend to s = t-1 and read h[t+1] -- its own target. Training would
        # reach a perfect score and mean nothing, and it would look exactly like
        # the result we are hoping for.
        mask = np.tril(np.ones((T, T), dtype=bool), k=-self.reach)
        scores = np.where(mask, scores, -np.inf)
        # The first `reach` rows have nothing to attend to and are all -inf;
        # softmax would produce nan. Zero them explicitly rather than letting a
        # nan propagate into the loss, where it would look like divergence.
        #
        # This was hardcoded to row 0 while `reach` was always 1. Making the
        # offsets configurable made that assumption false and every reach>1
        # configuration returned nan -- the same class of bug as the mask above,
        # found by the same check.
        scores[:self.reach, :] = 0.0

        shifted_max = scores.max(axis=1, keepdims=True)
        exp = np.exp(scores - shifted_max)
        exp = np.where(mask, exp, 0.0)
        denom = exp.sum(axis=1, keepdims=True)
        denom[:self.reach] = 1.0
        attn = exp / denom

        out = attn @ v
        logits = out @ p["wo"]
        cache = dict(tokens=tokens, h=h, shifted=shifted, q=q, k=k, v=v,
                     attn=attn, out=out, mask=mask, sources=sources)
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
        grads["offset_mix"] = np.tensordot(c["sources"], d_shifted, axes=([1, 2], [0, 1]))
        # Scatter the value gradient back to the positions each offset read from.
        for weight, offset in zip(p["offset_mix"], self.config.value_offsets):
            if offset > 0:
                d_h[offset:] += weight * d_shifted[:-offset]
            elif offset < 0:
                d_h[:offset] += weight * d_shifted[-offset:]
            else:
                d_h += weight * d_shifted

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
