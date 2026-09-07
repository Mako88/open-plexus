"""The card is Pascal and the wheel must still know that.

THE FIRST COMMAND THAT MATTERS, per the design doc's Phase 0. PyTorch's cu128
and later wheels dropped `sm_61`. Such a build imports, reports
`cuda.is_available() == True`, allocates tensors happily, and then dies on the
first kernel launch with "no kernel image is available for execution on the
device" -- an error that reads like a bug in this code. Pinning the index in
`pyproject.toml` is the fix; this is the check that says the pin still holds
after somebody runs `uv lock --upgrade`.
"""

from __future__ import annotations

import pytest

torch = pytest.importorskip("torch")

PASCAL = "sm_61"


def test_torch_is_built_for_pascal():
    arches = torch.cuda.get_arch_list()
    assert PASCAL in arches, (
        f"this torch ({torch.__version__}) was built for {arches} and the card is "
        f"a GTX 1080 Ti (compute 6.1). See the cu126 index pin in pyproject.toml."
    )


@pytest.mark.skipif(not torch.cuda.is_available(), reason="no CUDA device visible")
def test_the_card_is_the_one_the_pin_is_for():
    major, minor = torch.cuda.get_device_capability(0)
    assert (major, minor) >= (6, 1), (
        f"device reports compute {major}.{minor}; the toolchain is pinned for 6.1"
    )


@pytest.mark.skipif(not torch.cuda.is_available(), reason="no CUDA device visible")
def test_a_kernel_actually_launches():
    """The arch list is a claim; this is the claim being cashed.

    A build can list `sm_61` and still fail on a launch, and the arch-list check
    alone would read as a pass. Small enough to cost milliseconds and to not
    collide with a training cycle for the card's memory.
    """
    a = torch.randn(64, 64, device="cuda")
    b = torch.randn(64, 64, device="cuda")
    c = a @ b
    torch.cuda.synchronize()
    assert torch.isfinite(c).all()


def test_bf16_is_not_assumed_anywhere():
    """Pascal has no bfloat16. A default that reaches for it would be silent.

    `RwkvCore`'s default strategy is fp32 for this reason -- fp16 arithmetic on
    a 1080 Ti runs at 1/64 the fp32 rate, so the half-precision path that makes
    this model fast elsewhere makes it slower here.
    """
    import inspect

    from sylvatica.core.rwkv_core import RwkvCore

    default = inspect.signature(RwkvCore.__init__).parameters["strategy"].default
    assert "bf16" not in default, f"default strategy {default!r} asks for bf16 on Pascal"
