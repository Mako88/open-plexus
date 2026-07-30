"""One content-addressed lookup, bolted onto a frozen substrate.

docs/archive/notes/006 §7 records a requirement taken from the literature rather than
from our own reasoning: what separates architectures that solve MQAR from those
that cannot is **input-dependent sequence mixing** — the model must adapt how it
combines information according to what it is currently looking at. Width does
not substitute for it, and g0-02 measured that directly (64 → 128 units bought
0.008).

This module is the smallest possible instance of that requirement: *look back
for the last position holding the same token as now, and report what came
next.* Which position gets read depends entirely on the current input, which is
what makes it input-dependent rather than a fixed filter.

**What this is not.** It is hand-specified, not learned. On MQAR the token
variant returns the queried pair's value by construction, so a high score is not
evidence that anything learned anything — it is evidence that the *headroom is
reachable*, and that the reachability comes from this one operation rather than
from capacity. Read it as a capability probe, never as a strong reference.

The operation is nonetheless general: it consults no knowledge of keys, values,
pairs or the task, and would compute something on any sequence at all.
"""

from __future__ import annotations

from collections.abc import Sequence

MODES = ("token", "state")


def induction_features(
    tokens: Sequence[int],
    states: Sequence[Sequence[float]],
    vocab_size: int,
    mode: str = "state",
) -> list[list[float]]:
    """For each position, describe what followed this token the last time it appeared.

    Args:
        tokens: The input sequence.
        states: One substrate state per position, same length as `tokens`.
        vocab_size: Size of the token alphabet, for the one-hot in `"token"` mode.
        mode: `"token"` emits a one-hot of the token that followed the previous
            occurrence — an upper bound, since on this task that token *is* the
            answer. `"state"` emits the substrate's state at that position
            instead, which still has to be decoded and is the harder and more
            informative variant.

    Returns:
        One feature vector per position. Positions whose token has not been seen
        before get an all-zero vector, which is the honest encoding of "no
        evidence" — a lookup that invented a value here would be leaking.

    Causality: only positions strictly before the current one are consulted, and
    `tests/test_induction.py` asserts that by scrambling the future rather than
    trusting this sentence.
    """
    if mode not in MODES:
        raise ValueError(f"mode must be one of {MODES}, got {mode!r}")
    if len(tokens) != len(states):
        raise ValueError(f"{len(tokens)} tokens but {len(states)} states")
    # Validated for the same reason Reservoir.run validates: an out-of-range
    # token here would surface as an IndexError from deep inside the loop, which
    # names neither the offending token nor the vocabulary it violated.
    for token in tokens:
        if not 0 <= token < vocab_size:
            raise ValueError(f"token {token} outside vocab of {vocab_size}")

    width = vocab_size if mode == "token" else (len(states[0]) if states else 0)
    empty = [0.0] * width

    last_seen: dict[int, int] = {}
    out: list[list[float]] = []
    for position, token in enumerate(tokens):
        previous = last_seen.get(token)
        if previous is None or previous + 1 > position:
            out.append(list(empty))
        elif mode == "token":
            one_hot = [0.0] * vocab_size
            one_hot[tokens[previous + 1]] = 1.0
            out.append(one_hot)
        else:
            out.append(list(states[previous + 1]))
        last_seen[token] = position
    return out


def concatenate(
    states: Sequence[Sequence[float]], features: Sequence[Sequence[float]]
) -> list[list[float]]:
    """Join substrate states with lookup features, position by position."""
    if len(states) != len(features):
        raise ValueError(f"{len(states)} states but {len(features)} features")
    return [list(s) + list(f) for s, f in zip(states, features)]
