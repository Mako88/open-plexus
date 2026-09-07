"""The LoRA adapter: the slow path's only trainable parameters.

THE BASE IS FROZEN AND THAT IS THE WHOLE DESIGN. Refutation 2 of this branch is
"adapter updates lose the core's baseline abilities faster than they add" -- and
the reason to hope otherwise is that a low-rank delta over frozen weights has far
less capacity to overwrite than a full fine-tune does. A LoRA cannot forget
English as fast as a full update can, because most of the network is not moving.

WHERE IT ATTACHES. The doc: "a LoRA adapter over the core's time-mix and
channel-mix weights". Those are the six big matrices per layer -- receptance,
key, value and output in the attention, key and value in the channel mix. The
small vectors (`x_r`, `x_w`, `a0`, `w0` and friends) are deliberately NOT
adapted: they are per-channel gains and biases with a few thousand parameters
between them, so a low-rank factorisation of them is not low-rank, it is just a
second copy with extra steps.

THE WEIGHTS ARE STORED TRANSPOSED by the `rwkv` package -- it applies `x @ W`
with `W` of shape (in, out) rather than the usual (out, in). So `A` is (in, r)
and `B` is (r, out), and getting that backwards produces a shape error rather
than a silent wrong answer, which is the one mercy in this file.

MEMORY IS THE REAL CONSTRAINT AND IT IS NOT THE PARAMETERS. Rank 8 over 24
layers is about 7M trainable parameters at 1.5B, which is nothing. The problem is
the ACTIVATION GRAPH: the recurrence is a Python loop over T and autograd keeps
every intermediate state, which at 1.5B is 32 heads x 64 x 64 floats per token
per layer -- roughly 6 GB for a 512-token sequence on 24 layers. That does not
fit beside the model on an 11 GB card. `DifferentiableRwkv7.forward(checkpoint=
True)` recomputes each layer in the backward pass instead of storing it, trading
about double the compute for a graph that fits. Phase 3 will need it and the
number above is why.
"""

from __future__ import annotations

import math
from typing import Any

import torch
from torch import nn

# The six matrices a layer's worth of adaptation attaches to. Names are suffixes
# matched against the package's own weight keys.
TIME_MIX = ("att.receptance.weight", "att.key.weight", "att.value.weight", "att.output.weight")
CHANNEL_MIX = ("ffn.key.weight", "ffn.value.weight")
DEFAULT_TARGETS = TIME_MIX + CHANNEL_MIX


class LoraAdapter(nn.Module):
    """Low-rank deltas over named weights of a frozen core.

    Used as the `adapter` callable of `DifferentiableRwkv7`: given a weight name
    and the base tensor, it returns the base plus its delta, or the base
    untouched. That indirection is what keeps the core unaware it is being
    adapted, so the same forward serves training and inference and there is no
    second code path to drift.
    """

    def __init__(
        self,
        z: dict[str, torch.Tensor],
        targets: tuple[str, ...] = DEFAULT_TARGETS,
        rank: int = 8,
        alpha: float = 16.0,
        dtype: torch.dtype = torch.float32,
    ) -> None:
        super().__init__()
        self.rank = rank
        self.alpha = alpha
        self.scaling = alpha / rank
        self.targets = targets
        self.names: list[str] = []
        self._applied: dict[str, torch.Tensor] = {}

        self.a = nn.ParameterDict()
        self.b = nn.ParameterDict()
        for name, weight in z.items():
            if not any(name.endswith(t) for t in targets):
                continue
            if weight.dim() != 2:
                continue
            fan_in, fan_out = weight.shape
            key = name.replace(".", "_")
            device = weight.device
            # `A` GAUSSIAN AND `B` ZERO, which is LoRA's own initialisation and
            # is not arbitrary: it makes the delta exactly zero at step zero, so
            # an untrained adapter is INDISTINGUISHABLE from no adapter. That is
            # what lets the regression gate's first reading be a measurement of
            # the base rather than of a random perturbation of it.
            self.a[key] = nn.Parameter(
                torch.randn(fan_in, rank, device=device, dtype=dtype)
                / math.sqrt(fan_in)
            )
            self.b[key] = nn.Parameter(
                torch.zeros(rank, fan_out, device=device, dtype=dtype)
            )
            self.names.append(name)

    @property
    def trainable(self) -> int:
        return sum(p.numel() for p in self.parameters())

    def delta(self, name: str) -> torch.Tensor | None:
        key = name.replace(".", "_")
        if key not in self.a:
            return None
        return (self.a[key] @ self.b[key]) * self.scaling

    def __call__(self, name: str, base: torch.Tensor) -> torch.Tensor:
        d = self.delta(name)
        return base if d is None else base + d.to(base.dtype)

    # -- applying it temporarily, so an exam can run on the fast path -------

    @torch.no_grad()
    def apply_to(self, z: dict[str, torch.Tensor]) -> None:
        """Fold the deltas into the base and REMEMBER them, so it can be undone.

        WHY THIS EXISTS AND WHY IT IS NOT `merge_into`. Tier C asks 270 questions
        of an adapted model. Answering them through the differentiable forward
        means a Python loop over every generated token, which is hours. The fast
        inference path has no notion of an adapter -- so the adapter is folded
        in, the exam runs at full speed, and then it is folded back out.

        REVERSIBLE IS THE WHOLE POINT. `merge_into` is the doc's "sleep" and is
        irreversible by design: it zeroes the adapter afterwards, which is
        correct for consolidation and would destroy an experiment. Keeping the
        applied deltas costs one copy of a few million numbers and makes the
        difference between an evaluation and a commitment.
        """
        if self._applied:
            raise RuntimeError("already applied; call revert_from first")
        for name in self.names:
            d = self.delta(name)
            if d is None:
                continue
            d = d.to(z[name].dtype)
            self._applied[name] = d.clone()
            z[name] += d

    @torch.no_grad()
    def revert_from(self, z: dict[str, torch.Tensor]) -> None:
        """Undo `apply_to`, exactly, using the deltas it kept.

        Subtracting a RECOMPUTED delta would be subtly wrong: the adapter may
        have trained between the two calls, and the difference would be silently
        baked into the base -- an irreversible change nobody asked for, in a
        model that would still run.
        """
        for name, d in self._applied.items():
            z[name] -= d
        self._applied.clear()

    # -- the merge, which the doc calls "the sleep" -------------------------

    @torch.no_grad()
    def merge_into(self, z: dict[str, torch.Tensor]) -> int:
        """Fold every delta into the base weights and reset to zero.

        EVERY K CYCLES, and K is a dial. After a merge the adapter is fresh and
        the base has moved, which is the only point in this design where the
        core's own weights change -- so it is also the only point at which
        anything is IRREVERSIBLE. A cycle can be rolled back by discarding an
        adapter; a merge cannot, and Phase 3 must take the regression gate's
        verdict before doing one rather than after.
        """
        if self._applied:
            raise RuntimeError(
                "the adapter is temporarily applied for an exam; merging now "
                "would fold the same delta in twice. Call revert_from first."
            )
        merged = 0
        for name in self.names:
            d = self.delta(name)
            if d is None:
                continue
            z[name] += d.to(z[name].dtype)
            merged += 1
        for key in self.b:
            self.b[key].zero_()
        return merged

    def state_dict_cpu(self) -> dict[str, torch.Tensor]:
        return {k: v.detach().cpu() for k, v in self.state_dict().items()}


def adapter_for(core: Any, rank: int = 8, **kw) -> LoraAdapter:
    """A LoRA sized for a loaded core, on the same device as its weights."""
    return LoraAdapter(core._model.z, rank=rank, **kw)
