"""The small amount of linear algebra this project needs, in plain Python.

No numpy. GOALS.md §7 requires a reference implementation that is obviously
correct and slow, against which any fast path is later asserted — and a solver
written out in full is auditable in a way a library call is not. It is also
genuinely slow: `solve` is O(n³) in interpreted Python and is not intended for
large systems.

Vectors are lists of floats; matrices are lists of rows.
"""

from __future__ import annotations

import math
import random


def solve(matrix: list[list[float]], rhs: list[list[float]]) -> list[list[float]]:
    """Solve `matrix @ X = rhs` for X, by Gaussian elimination with partial pivoting.

    Args:
        matrix: Square, n×n. Not modified.
        rhs: n×k — k right-hand sides solved simultaneously, which is what a
            multi-class readout needs.

    Returns:
        The n×k solution X.

    Raises:
        ValueError: if the matrix is not square, the shapes disagree, or the
            system is singular to working precision. **Singular raises rather
            than returning a plausible-looking answer** — a silently wrong
            solution here would produce a readout score that looks like a
            measurement.
    """
    n = len(matrix)
    if any(len(row) != n for row in matrix):
        raise ValueError("matrix must be square")
    if len(rhs) != n:
        raise ValueError(f"rhs has {len(rhs)} rows, matrix has {n}")
    k = len(rhs[0])

    a = [list(row) + list(r) for row, r in zip(matrix, rhs)]

    for col in range(n):
        pivot = max(range(col, n), key=lambda r: abs(a[r][col]))
        if abs(a[pivot][col]) < 1e-12:
            raise ValueError(f"matrix is singular at column {col}")
        a[col], a[pivot] = a[pivot], a[col]
        inv = 1.0 / a[col][col]
        for j in range(col, n + k):
            a[col][j] *= inv
        for row in range(n):
            if row == col:
                continue
            factor = a[row][col]
            if factor == 0.0:
                continue
            for j in range(col, n + k):
                a[row][j] -= factor * a[col][j]

    return [row[n:] for row in a]


def spectral_radius(matrix: list[list[float]], iterations: int = 200,
                    seed: int = 0) -> float:
    """Estimate the largest eigenvalue magnitude, by power iteration.

    **This is an estimate with a known failure mode**, stated because a silent
    one would misreport the single most important property of a reservoir.
    Power iteration converges to the dominant eigenvalue's magnitude when that
    eigenvalue is real and unique. When the dominant pair is complex, the
    iterate rotates rather than settling and the returned value oscillates
    within a few percent instead of converging.

    For the random matrices used here, complex dominant pairs are common, so
    treat this as accurate to a few percent, not exact. What matters for the
    reservoir is that scaling by it *moves the dynamics in the right direction*,
    which is asserted directly in the tests rather than assumed from this
    number.
    """
    n = len(matrix)
    rng = random.Random(seed)
    v = [rng.gauss(0.0, 1.0) for _ in range(n)]
    norm = math.sqrt(sum(x * x for x in v))
    v = [x / norm for x in v]

    estimate = 0.0
    for _ in range(iterations):
        w = [sum(matrix[i][j] * v[j] for j in range(n)) for i in range(n)]
        norm = math.sqrt(sum(x * x for x in w))
        if norm < 1e-300:
            return 0.0
        v = [x / norm for x in w]
        estimate = norm
    return estimate


def scale(matrix: list[list[float]], factor: float) -> list[list[float]]:
    """Multiply every entry by `factor`."""
    return [[x * factor for x in row] for row in matrix]
