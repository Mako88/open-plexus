"""Did the sweep contain its own answer?

Sweeping a hyperparameter on every arm of a comparison is the rule. It is not
enough. If every arm chooses a value at an EDGE of the grid, the optimum lies
outside it and every arm is under-tuned -- the sweep ran, the rule was followed,
and the number is still provisional.

g4-01 printed its chosen learning rates and pinned at an edge in four of six rows.
Printing was enough to catch it only because someone read the line. This makes it
an assertion instead.
"""

from __future__ import annotations


def pinned(chosen, grid) -> str | None:
    """Report whether these choices sit at an edge of the grid they came from.

    Args:
        chosen: The value each arm selected. Repeats are meaningful -- several
            arms landing on the same edge is the signal, not noise.
        grid: Every value that was offered.

    Returns:
        A description of the problem, or None if the grid contained its answer.
        A string is returned rather than raised because a pinned grid invalidates
        the *numbers* while leaving the direction and any ceiling results
        standing, so the caller decides how loud to be.

    An interior choice is the only clean outcome. A grid of one value is always
    pinned and says so, because a parameter with a single setting is not swept --
    it is fixed, and looks like the background rather than a variable.
    """
    values, offered = list(chosen), sorted(set(grid))
    if not values or not offered:
        return None
    if len(offered) == 1:
        return (f"the grid offered only {offered[0]}, so this parameter was "
                f"fixed rather than swept")
    low, high = offered[0], offered[-1]
    at_low = sum(1 for v in values if v == low)
    at_high = sum(1 for v in values if v == high)
    if at_low + at_high < len(values):
        return None
    edge, count = (low, at_low) if at_low >= at_high else (high, at_high)
    side = "bottom" if edge == low else "top"
    return (f"all {len(values)} arms chose an edge of the grid "
            f"{offered} ({count} at the {side}, {edge}); the optimum is "
            f"outside it and every arm is under-tuned")
