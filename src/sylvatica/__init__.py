"""Sylvatica: a core that holds its memory in a state, a store, and a slow path between them.

Six parts, each with a protocol at its top and none reaching into another's
internals: `core`, `store`, `learn`, `loop`, `exam`, `node`. What each is for is
in its own docstring. What the branch is for, in what order, and what would
refute each bet is in `docs/sylvatica.md`, which is the one design doc.

Nothing finished is written in that doc. What a built thing does lives here.
"""

__version__ = "0.1.0"
