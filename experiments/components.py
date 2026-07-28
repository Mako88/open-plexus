"""Name a model by what it is MADE OF, so a grid can sweep combinations.

`--mode` conflates two things: which arm a row belongs to, and which components
the model is built from. That was tolerable while one component varied at a time
and it stopped being tolerable today. g11-06 needed a `matched` arm that is **the
same model** as `single` under a different name, purely so the summariser would
not average two configurations into one column — the arm name was carrying
information the config could not express.

It also blocks the thing that is now most worth doing. Decision 74 established
that every number in this project's comparison set was measured beside a LINEAR
readout, and that the readout has changed. Re-checking those one probe at a time
produces a stream of surprises; re-measuring the whole set at once produces a
table. The second needs a way to say

    keys=sparse4,retrieval=cache128,readout=hidden128

as one argument, and to have that string BE the arm label, so an arm is
identified by its composition and two different models can never share a name.

## What this is not

Not a seam. `openplexus/keys.py` and `openplexus/retrieval.py` are seams — a new
implementation costs a class and one assignment. This is the naming layer above
them, and it deliberately only exposes combinations that already exist. A new
component still goes in its own module; this file learns about it in one line.
"""

from __future__ import annotations

#: What each component choice means, as config overrides.
#:
#: Keys and retrieval resolve through their seams; `readout` is still a config
#: field rather than a seam, which is the next cleanup and does not block this.
CHOICES: dict[str, dict[str, dict]] = {
    "keys": {
        "dense": {},
        "sparse4": {"key_active": 4},
        "sparse8": {"key_active": 8},
        "sparse16": {"key_active": 16},
        "pair": {"context_keys": True},
    },
    "retrieval": {
        "plain": {},
        "cache32": {"cache_slots": 32},
        "cache128": {"cache_slots": 128},
        "settle2": {"retrieval_steps": 2},
    },
    "readout": {
        "linear": {},
        "hidden64": {"hidden": 64},
        "hidden128": {"hidden": 128},
    },
}

#: Used when a spec leaves a component out, so every label is complete.
DEFAULTS = {"keys": "dense", "retrieval": "plain", "readout": "linear"}


def parse(spec: str) -> tuple[dict, str]:
    """`keys=sparse4,retrieval=cache128` -> (config overrides, canonical label).

    The label is always complete and always in the same order, so two specs that
    describe the same model produce the same label and cannot be recorded as
    different arms. An unknown component or choice is an error rather than a
    default, because a typo that silently selects the baseline would report the
    baseline twice and call it a comparison.
    """
    chosen = dict(DEFAULTS)
    for piece in (p.strip() for p in spec.split(",") if p.strip()):
        if "=" not in piece:
            raise ValueError(
                f"{piece!r} is not `component=choice`; a spec looks like "
                f"'keys=sparse4,readout=hidden128'")
        component, _, choice = piece.partition("=")
        component, choice = component.strip(), choice.strip()
        if component not in CHOICES:
            raise ValueError(
                f"unknown component {component!r}; expected one of "
                f"{', '.join(sorted(CHOICES))}")
        if choice not in CHOICES[component]:
            raise ValueError(
                f"unknown {component} {choice!r}; expected one of "
                f"{', '.join(sorted(CHOICES[component]))}")
        chosen[component] = choice
    overrides: dict = {}
    for component in DEFAULTS:
        overrides.update(CHOICES[component][chosen[component]])
    return overrides, label(chosen)


def label(chosen: dict[str, str]) -> str:
    """The canonical name of a combination, in a fixed component order."""
    return ",".join(f"{c}={chosen[c]}" for c in DEFAULTS)


def grid(**components: list[str]) -> list[str]:
    """Every combination of the named choices, as specs.

    `grid(keys=["dense", "sparse4"], readout=["linear", "hidden128"])` gives the
    four specs. Written here rather than in a workflow so the cost of a grid can
    be counted before it is dispatched.
    """
    for component, choices in components.items():
        if component not in CHOICES:
            raise ValueError(f"unknown component {component!r}")
        unknown = set(choices) - set(CHOICES[component])
        if unknown:
            raise ValueError(f"unknown {component}: {', '.join(sorted(unknown))}")
    specs = [""]
    for component, choices in components.items():
        specs = [f"{s},{component}={c}" if s else f"{component}={c}"
                 for s in specs for c in choices]
    return specs
