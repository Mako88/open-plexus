"""A locality-respecting associative memory — G1's candidate.

Everything that reached 1.000 so far did it with attention, which violates C1
twice over: a softmax normalising over every position, and a backward pass that
carries information from the loss back through the whole sequence. Neither can
run on machines that never synchronise.

This is the local alternative. It keeps the one property note 006 §7 identified
as necessary — **input-dependent mixing**, here as content-addressed retrieval —
and obtains it without either violation.

## What happens, and why each step is local

At each position `t`, with `e` the embedding of the current token:

    k = Wk e                      key      (frozen random projection)
    v = Wv e                      value    (frozen random projection)
    M += v ⊗ k_previous           STORE    bind the previous token to this one
    r = M k                       RETRIEVE query the store with the current token
    y = Wo r                      predict
    Wo += lr · (target − y) ⊗ r   LEARN    delta rule

- **`M += v ⊗ k_prev` is an outer product.** Entry `M[i,j]` changes by
  `v[i] · k_prev[j]` — the product of a signal at its output side and a signal at
  its input side. That is the most local update there is: a synapse changing on
  what its own two ends are doing, consulting nothing else. Purely Hebbian.
- **`r = M k` is a matrix-vector product.** Output `i` sums over its own incoming
  connections. No normalisation across units, no softmax, nothing pooled.
- **The delta rule is local too.** The error is the output unit's own prediction
  error against its own next input; the input is its own retrieved vector.
  Nothing travels backwards through anything.
- **Nothing is stored across sequences except `Wo`.** `M` is per-sequence working
  memory, built and discarded as the sequence runs. It is not a parameter and
  nothing optimises it.

## What it is, in prior-art terms

A fast-weight associative memory (Hebb; Hopfield; Ba et al. 2016), which is also
what a linear-attention layer computes. That lineage matters for expectations
rather than for credit: docs/notes/006 records that linear attention **fails**
MQAR unless its state is large, while softmax attention does not. So a width
penalty relative to the attention model is the *expected* outcome, and the size
of that penalty is the measurement G1 wants (g1-04).

## What is deliberately not claimed

- **`Wk` and `Wv` are frozen random.** Only `Wo` learns. That is the strictest
  version of the question and the honest place to start: if it works, no case has
  been made that those projections need learning; if it fails, learning them
  locally is the next thing to try rather than the reason it failed.
- **This is not distributed.** It is a *locality-respecting* computation running
  in one process. Whether it survives real delay and churn is G2 and G3.
"""

from __future__ import annotations

from dataclasses import dataclass

import numpy as np


@dataclass(frozen=True)
class LocalMemoryConfig:
    """Shape and learning settings.

    Attributes:
        vocab_size: Token alphabet size.
        d_model: Width of the key/value space. **This is the dial G1 measures.**
            g1-04 put the attention model's threshold between 8 and 16; the ratio
            between that and whatever this needs is the price of locality.
        lr: Delta-rule step size for the output weights.
        decay: Per-step multiplier on the memory. 1.0 keeps everything;
            below 1.0 forgets older bindings, which bounds the interference a
            long sequence accumulates.
        seed: Determines the frozen projections completely.
    """

    vocab_size: int
    d_model: int = 64
    lr: float = 0.05
    decay: float = 1.0
    seed: int = 0

    def __post_init__(self) -> None:
        if self.vocab_size < 2:
            raise ValueError("vocab_size must be at least 2")
        if self.d_model < 1:
            raise ValueError("d_model must be at least 1")
        if not 0.0 < self.lr:
            raise ValueError("lr must be positive")
        if not 0.0 < self.decay <= 1.0:
            raise ValueError("decay must be in (0, 1]")


class LocalAssociativeMemory:
    """Hebbian store, content-addressed retrieval, delta-rule readout.

    The contract: `run` processes one sequence left to right, returning a
    prediction of the next token at every position. With `learn=True` it also
    updates `Wo` online as it goes. No backward pass exists.
    """

    def __init__(self, config: LocalMemoryConfig) -> None:
        self.config = config
        rng = np.random.default_rng(config.seed)
        d, v = config.d_model, config.vocab_size
        # Rows scaled to roughly unit norm, so retrieval needs no normalisation
        # step. Normalising `k` at run time would be a per-vector operation and
        # defensible, but avoiding it entirely keeps the C1 argument simple.
        self.wk = rng.normal(0.0, 1.0 / np.sqrt(d), (v, d))
        self.wv = rng.normal(0.0, 1.0 / np.sqrt(d), (v, d))
        self.wo = np.zeros((v, d))

    def ablate(self, dimensions) -> None:
        """Permanently remove these dimensions — a machine has left, for good.

        This is C3's failure, and it is a different thing from C2's. A dropped
        message is transient: the next one arrives. A departed machine takes its
        share of the state with it and never comes back.

        If the `d_model` dimensions were spread across machines, one machine
        leaving is a slice of them gone. Zeroing the corresponding columns of the
        frozen projections is enough to model that: with `wv[:, j]` zero the
        memory's row `j` is empty, with `wk[:, j]` zero its column `j` is, and
        the retrieved vector is therefore zero in those dimensions. The delta
        rule then multiplies by that zero, so the readout's columns stay dead
        without needing to be masked — the machine cannot come back by accident,
        which is the property being modelled.

        **Note what is NOT lost.** The associative memory is per-sequence working
        state, rebuilt from scratch every sequence. Only the readout persists
        across sequences. So a departing machine costs *capacity*, and costs
        whatever the readout had learned in those dimensions — it does not take
        away stored memories, because there are none to take.
        """
        index = np.asarray(list(dimensions), dtype=int)
        if index.size and (index.min() < 0 or index.max() >= self.config.d_model):
            raise ValueError(
                f"dimension outside [0, {self.config.d_model}): {index}")
        self.wk[:, index] = 0.0
        self.wv[:, index] = 0.0
        self.wo[:, index] = 0.0

    def surviving_width(self) -> int:
        """How many dimensions still carry signal.

        The honest denominator after churn. Reporting a score against the
        original `d_model` would credit the model with room it no longer has.
        """
        return int((np.abs(self.wk).sum(axis=0) > 0).sum())

    def run(self, tokens: np.ndarray, targets: np.ndarray | None = None,
            scored: np.ndarray | None = None,
            learn: bool = False) -> np.ndarray:
        """Process one sequence; return the predicted next token per position.

        Args:
            tokens: The sequence.
            targets: Target token per position. Required when `learn` is True.
            scored: Positions the delta rule is applied at. Required when `learn`
                is True. **Note this is a training-time convenience, not part of
                the model** — a genuinely autonomous unit would learn at every
                step. It exists so that this rule can be compared against the
                attention model under the same objective.
            learn: Whether to update `Wo` online.

        Returns:
            `argmax` of the readout at each position.
        """
        if learn and (targets is None or scored is None):
            raise ValueError("learning needs targets and scored positions")

        d = self.config.d_model
        memory = np.zeros((d, d))
        previous_key = None
        predictions = np.zeros(len(tokens), dtype=np.int64)

        for t, token in enumerate(tokens):
            if not 0 <= token < self.config.vocab_size:
                raise ValueError(
                    f"token {token} outside vocab of {self.config.vocab_size}")
            key = self.wk[token]
            value = self.wv[token]

            # STORE: bind the previous token to this one. Doing this before the
            # retrieval below is what makes the association available later
            # without ever letting position t see position t+1 — the binding
            # written now is (t-1 → t), entirely in the past.
            if previous_key is not None:
                if self.config.decay < 1.0:
                    memory *= self.config.decay
                memory += np.outer(value, previous_key)

            # RETRIEVE and predict.
            retrieved = memory @ key
            readout = self.wo @ retrieved
            predictions[t] = int(readout.argmax())

            if learn and scored[t]:
                target = np.zeros(self.config.vocab_size)
                target[targets[t]] = 1.0
                self.wo += self.config.lr * np.outer(target - readout, retrieved)

            previous_key = key
        return predictions
