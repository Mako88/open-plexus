"""The cost meter, and the shape of the tree the doc lays out.

THE FLOPS NUMBER IS THE DENOMINATOR OF EVERY LATER READING. Refutation 3 of the
branch -- "consolidation costs more compute than re-sending the context would
have" -- is decided by these two formulas and nothing else, so a typo in either
is a typo in the branch's verdict.
"""

from __future__ import annotations

import importlib
import json
from pathlib import Path

import pytest

from sylvatica.core.base import Cost, Meter, forward_flops, training_flops

SRC = Path(__file__).resolve().parents[2] / "src" / "sylvatica"

# The six parts the design doc names under THE SHAPE.
PARTS = ["core", "store", "learn", "loop", "exam", "node"]


def test_the_flops_estimates_are_the_standard_ones():
    assert forward_flops(1_000, 10) == 20_000  # 2 N T
    assert training_flops(1_000, 10) == 60_000  # 6 N T
    assert training_flops(7, 11) == 3 * forward_flops(7, 11)


def test_costs_add_and_the_rate_is_over_both_directions():
    a = Cost(tokens_in=10, tokens_out=5, seconds=1.0, flops=100.0)
    b = Cost(tokens_in=1, tokens_out=4, seconds=1.0, flops=50.0)
    total = a + b
    assert (total.tokens_in, total.tokens_out) == (11, 9)
    assert total.seconds == 2.0 and total.flops == 150.0
    # A turn's rate counts what was read AND what was written; a prefill-only
    # rate would flatter a core that answers in one word.
    assert total.tokens_per_second == 10.0


def test_a_meter_with_no_calls_reports_zero_rather_than_dividing_by_zero():
    assert Meter().total.tokens_per_second == 0.0


def test_every_part_the_doc_names_exists_and_imports():
    """A part that is missing should fail here rather than at the import in a
    script three phases from now. `node` is protocol-only before Phase 5 and
    still has to exist."""
    for part in PARTS:
        assert (SRC / part / "__init__.py").exists() or (SRC / part).is_dir(), part
        importlib.import_module(f"sylvatica.{part}")


def test_prefill_stays_faster_than_decode_in_the_committed_readings():
    """A finding, guarded. Reading a sentence in must cost less per token than
    writing one out, because prefill sees every token at once and decode cannot.

    IT WAS BRIEFLY BELIEVED TO BE THE OTHER WAY ROUND, and the belief had a
    plausible mechanism behind it: with `RWKV_CUDA_ON=0` the package's v7
    sequence path really does run a Python `for t in range(T)` loop over the
    recurrence. The first measurement said 20 tok/s prefill against 42 decode
    and the loop was blamed. It was a SINGLE UNWARMED PASS -- TorchScript
    compiling and cuBLAS picking kernels, charged to the model's speed. Warmed,
    prefill is 125 tok/s at 0.4B and 157 at 1.5B.

    So this guard is here to catch two different regressions with one assertion:
    a real slowdown in the sequence path, and a cost script that stops warming
    up before it measures.
    """
    readings = SRC.parents[1] / "readings"
    if not readings.exists():
        pytest.skip("no readings yet")
    rows = []
    for path in sorted(readings.glob("phase0-cost-*.json")):
        rows += json.loads(path.read_text(encoding="utf-8"))["rows"]
    if not rows:
        pytest.skip("no phase-0 cost reading committed yet")

    slow = [
        f"{r['size_b']}B ({r['prefill_tokens_per_second']} prefill vs "
        f"{r['decode_tokens_per_second']} decode)"
        for r in rows
        if r.get("decode_tokens_per_second")
        and r["prefill_tokens_per_second"] <= r["decode_tokens_per_second"]
    ]
    assert not slow, "prefill is not faster than decode at " + ", ".join(slow)


def test_readings_are_json_rows_and_nothing_else():
    """FINDINGS NEVER GO IN THE DOC. `readings/` is JSON, one file per run.

    A markdown summary landing in here is the beginning of a second design doc,
    which is the thing the culture is explicitly against.
    """
    readings = SRC.parents[1] / "readings"
    if not readings.exists():
        pytest.skip("no readings yet")
    strays = [p.name for p in readings.iterdir() if p.suffix not in (".json", "")]
    assert not strays, f"readings/ holds non-JSON: {strays}"
