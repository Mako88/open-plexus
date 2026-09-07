"""`core` -- the model and its state.

`Core` is the protocol; `RwkvCore` is the one implementation; `Thread` is a
named state file plus its transcript, and it is what a restart resumes.

THE STATE IS THE FAST MEMORY. It is a fixed-size tensor per layer -- measured,
not assumed: 6.5 MB at 0.4B -- and its size does not grow with how much has been
said. Compression is therefore forced by the shape of the part rather than by a
policy bolted on top, which is the reason a recurrent core was chosen over a
transformer with a sliding window.

NOTHING OUTSIDE THIS PACKAGE MAY INDEX INTO A STATE. It is opaque. That rule is
what lets the core be swapped for a bigger one without anything above changing,
and it is currently only a docstring -- standing objection 7.
"""

from .base import Core, Cost, Meter, Sampling, forward_flops, training_flops
from .rwkv_core import RwkvCore
from .thread import Thread, Turn

__all__ = [
    "Core",
    "Cost",
    "Meter",
    "RwkvCore",
    "Sampling",
    "Thread",
    "Turn",
    "forward_flops",
    "training_flops",
]
