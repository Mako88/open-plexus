"""A trained linear readout — the only thing that learns in the G0 control.

Ridge regression onto one-hot targets, then argmax. This is the standard
reservoir-computing readout, and it is deliberately the *only* trained part: the
substrate stays frozen, so any score it achieves is attributable to the
substrate's representation plus a linear decoder, and to nothing else.

The regularisation term matters more than it looks. With more state dimensions
than training samples the system is underdetermined and an unregularised fit
would interpolate the training set exactly while scoring at chance on held-out
data — producing a number that looks like a strong result and is an artefact.
"""

from __future__ import annotations

from collections.abc import Sequence

from openplexus import linalg


class RidgeReadout:
    """A linear decoder from state vectors to class labels.

    The contract: `fit` learns from (state, label) pairs; `predict` maps a state
    to the highest-scoring label. Labels are arbitrary hashable values; the
    mapping to columns is internal.
    """

    def __init__(self, ridge: float = 1e-3) -> None:
        if ridge <= 0.0:
            raise ValueError(
                "ridge must be positive: an unregularised fit on an "
                "underdetermined system interpolates the training data and "
                "scores at chance out of sample"
            )
        self.ridge = ridge
        self._labels: list = []
        self._weights: list[list[float]] = []

    def fit(self, states: Sequence[Sequence[float]], labels: Sequence) -> "RidgeReadout":
        """Fit the decoder. Returns self."""
        if len(states) != len(labels):
            raise ValueError(f"{len(states)} states but {len(labels)} labels")
        if not states:
            raise ValueError("nothing to fit")

        self._labels = sorted(set(labels))
        index = {label: i for i, label in enumerate(self._labels)}
        n = len(states[0]) + 1  # +1 for a bias term
        k = len(self._labels)

        design = [list(s) + [1.0] for s in states]

        # Normal equations: (XᵀX + λI) w = Xᵀ Y, with Y one-hot.
        gram = [[0.0] * n for _ in range(n)]
        for row in design:
            for i in range(n):
                ri = row[i]
                if ri:
                    gram_i = gram[i]
                    for j in range(n):
                        gram_i[j] += ri * row[j]
        for i in range(n):
            gram[i][i] += self.ridge

        targets = [[0.0] * k for _ in range(n)]
        for row, label in zip(design, labels):
            col = index[label]
            for i in range(n):
                if row[i]:
                    targets[i][col] += row[i]

        self._weights = linalg.solve(gram, targets)
        return self

    def predict(self, state: Sequence[float]) -> object:
        """Return the highest-scoring label for one state vector."""
        if not self._weights:
            raise ValueError("readout has not been fitted")
        row = list(state) + [1.0]
        if len(row) != len(self._weights):
            raise ValueError(
                f"state has {len(state)} dimensions, readout was fitted on "
                f"{len(self._weights) - 1}"
            )
        scores = [
            sum(row[i] * self._weights[i][c] for i in range(len(row)))
            for c in range(len(self._labels))
        ]
        return self._labels[max(range(len(scores)), key=scores.__getitem__)]
