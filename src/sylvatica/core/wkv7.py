"""A DIFFERENTIABLE RWKV-7 forward. The thing Phase 3 cannot start without.

WHY THIS EXISTS. The `rwkv` pip package is inference-only: every forward runs
under `torch.no_grad()` and its modules are TorchScript-compiled around that
assumption. Phase 3 -- the branch's actual bet -- trains a LoRA adapter over the
time-mix and channel-mix weights, and you cannot backpropagate through a function
that never built a graph. So consolidation is blocked on having a forward that
autograd can see, and this is it.

WHAT WAS NOT AVAILABLE, checked before writing anything:

  - The official RWKV-LM training kernel needs `nvcc` and a CUDA toolkit, and it
    is written for the training-side WKV7 which the inference package does not
    ship.
  - `flash-linear-attention` has Triton kernels for RWKV-7, and Triton requires
    sm_70 or newer. The card is sm_61. Not available, and it would have failed
    at runtime rather than at install.

So the doc's named fallback -- "the pure-PyTorch chunked path" -- is the road,
and this is its first half: CORRECT FIRST, FAST LATER. The recurrence here is a
plain Python loop over T, which is what the inference package does anyway. It
unblocks Phase 3 at a speed nobody has to like, and the chunked version can
replace the loop behind the same signature once there is something to train.

THE STATE LAYOUT MATCHES THE PACKAGE EXACTLY, three tensors per layer in the
order it uses, so a state written by one can be read by the other. That is not
tidiness: it is what lets `tests/guards/test_wkv7.py` check this implementation
against the reference on the same checkpoint, which is the only thing standing
between "differentiable" and "differentiable and correct".
"""

from __future__ import annotations

from typing import Any

import torch
import torch.nn.functional as F

# The package's own constant: exp(-0.5) = 0.606531, applied inside the decay.
_DECAY = 0.606531


def time_mix(
    x: torch.Tensor,
    x_prev: torch.Tensor,
    v_first: torch.Tensor,
    state: torch.Tensor,
    z: dict[str, torch.Tensor],
    att: str,
    layer_id: int,
    n_head: int,
    head_size: int,
) -> tuple[torch.Tensor, torch.Tensor, torch.Tensor, torch.Tensor]:
    """One layer's time mixing over a sequence, with the graph intact.

    Transcribed from `RWKV_x070_TMix_seq` in the `rwkv` package. The recurrence
    is

        S_t = S_{t-1} * diag(w_t) + S_{t-1} @ (a_t b_t^T) + v_t k_t^T

    -- diagonal decay plus a rank-one update plus a rank-one write. The loop
    below is that, and the only difference from the reference is that nothing
    here is wrapped in `no_grad` and no tensor is written into in place, because
    an in-place write into a tensor autograd is tracking is how you get a
    "variable needed for gradient computation has been modified" error three
    phases from now.
    """
    T = x.shape[0]
    H, N = n_head, head_size

    xx = torch.cat((x_prev.unsqueeze(0), x[:-1, :])) - x
    xr = x + xx * z[att + "x_r"]
    xw = x + xx * z[att + "x_w"]
    xk = x + xx * z[att + "x_k"]
    xv = x + xx * z[att + "x_v"]
    xa = x + xx * z[att + "x_a"]
    xg = x + xx * z[att + "x_g"]

    r = xr @ z[att + "receptance.weight"]
    w = torch.tanh(xw @ z[att + "w1"]) @ z[att + "w2"]
    k = xk @ z[att + "key.weight"]
    v = xv @ z[att + "value.weight"]
    a = torch.sigmoid(z[att + "a0"] + (xa @ z[att + "a1"]) @ z[att + "a2"])
    g = torch.sigmoid(xg @ z[att + "g1"]) @ z[att + "g2"]

    kk = F.normalize((k * z[att + "k_k"]).view(T, H, N), dim=-1, p=2.0).view(T, H * N)
    k = k * (1 + (a - 1) * z[att + "k_a"])

    # LAYER 0 DEFINES `v_first` AND EVERY LATER LAYER MIXES TOWARDS IT. Getting
    # this branch wrong produces a model that runs and answers slightly worse,
    # which is the hardest kind of bug to notice.
    if layer_id == 0:
        v_first = v
    else:
        v = v + (v_first - v) * torch.sigmoid(
            z[att + "v0"] + (xv @ z[att + "v1"]) @ z[att + "v2"]
        )

    w = torch.exp(-_DECAY * torch.sigmoid((z[att + "w0"] + w).float()))

    out = []
    s = state.float()
    for t in range(T):
        r_, w_, k_, v_, kk_, a_ = r[t], w[t], k[t], v[t], kk[t], a[t]
        vk = v_.view(H, N, 1) @ k_.view(H, 1, N)
        ab = (-kk_).view(H, N, 1) @ (kk_ * a_).view(H, 1, N)
        s = s * w_.view(H, 1, N) + s @ ab.float() + vk.float()
        out.append((s.to(dtype=x.dtype) @ r_.view(H, N, 1)).view(H * N))
    xx = torch.stack(out)

    xx = F.group_norm(
        xx.view(T, H * N), num_groups=H, weight=z[att + "ln_x.weight"],
        bias=z[att + "ln_x.bias"], eps=64e-5,
    ).view(T, H * N)
    xx = xx + (
        (r * k * z[att + "r_k"]).view(T, H, N).sum(dim=-1, keepdim=True) * v.view(T, H, N)
    ).view(T, H * N)
    return (xx * g) @ z[att + "output.weight"], x[-1, :], s, v_first


def channel_mix(
    x: torch.Tensor, x_prev: torch.Tensor, z: dict[str, torch.Tensor], ffn: str
) -> tuple[torch.Tensor, torch.Tensor]:
    """One layer's channel mixing. `relu(k)^2` is RWKV's squared-ReLU, not a typo."""
    xx = torch.cat((x_prev.unsqueeze(0), x[:-1, :])) - x
    k = x + xx * z[ffn + "x_k"]
    k = torch.relu(k @ z[ffn + "key.weight"]) ** 2
    return k @ z[ffn + "value.weight"], x[-1, :]


class DifferentiableRwkv7:
    """A forward over an RWKV-7 checkpoint that autograd can see.

    Wraps a loaded `RwkvCore` so the weights are shared rather than copied --
    the card holds one set of 1.5B parameters, not two. `adapter` is where Phase
    3's LoRA hooks in: a callable given a weight name and the base tensor,
    returning what to use instead.
    """

    def __init__(self, core: Any, adapter: Any = None) -> None:
        model = core._model
        self.z = model.z
        self.n_layer = int(model.args.n_layer)
        self.n_embd = int(model.args.n_embd)
        self.n_head = int(model.n_head)
        self.head_size = int(model.head_size)
        self.adapter = adapter

    def _w(self, name: str) -> torch.Tensor:
        base = self.z[name]
        return self.adapter(name, base) if self.adapter else base

    def forward(
        self,
        tokens: list[int],
        state: list[torch.Tensor] | None = None,
        checkpoint: bool = False,
    ) -> tuple[torch.Tensor, list[torch.Tensor]]:
        """Logits for every position, and the state after. Graph intact.

        Returns ALL positions rather than only the last, because a training loss
        is over the whole sequence and recomputing it would double the cost of
        the thing Phase 3 spends its time on.

        `checkpoint` TRADES COMPUTE FOR MEMORY AND PHASE 3 WILL NEED IT. The
        recurrence is a loop over T and autograd keeps every intermediate state:
        at 1.5B that is 32 heads x 64 x 64 floats per token per layer, about 6 GB
        for a 512-token sequence over 24 layers, which does not fit beside the
        model on an 11 GB card. With this on, each layer is recomputed during the
        backward pass instead of stored -- roughly double the forward cost for a
        graph that fits. Off by default because inference does not need it and
        paying it there would be free money burned.
        """
        z = {name: self._w(name) for name in self.z}
        x = z["emb.weight"][tokens]
        if state is None:
            state = self.fresh_state(x.device, x.dtype)

        new_state: list[torch.Tensor] = list(state)
        v_first = torch.empty_like(x)
        for i in range(self.n_layer):
            bbb, att, ffn = f"blocks.{i}.", f"blocks.{i}.att.", f"blocks.{i}.ffn."

            xx = F.layer_norm(
                x, (self.n_embd,), weight=z[bbb + "ln1.weight"], bias=z[bbb + "ln1.bias"]
            )
            if checkpoint and torch.is_grad_enabled():
                from torch.utils.checkpoint import checkpoint as _ckpt

                xx, xp, s, v_first = _ckpt(
                    time_mix,
                    xx, state[i * 3 + 0], v_first, state[i * 3 + 1],
                    z, att, i, self.n_head, self.head_size,
                    use_reentrant=False,
                )
            else:
                xx, xp, s, v_first = time_mix(
                    xx, state[i * 3 + 0], v_first, state[i * 3 + 1],
                    z, att, i, self.n_head, self.head_size,
                )
            new_state[i * 3 + 0], new_state[i * 3 + 1] = xp, s
            x = x + xx

            xx = F.layer_norm(
                x, (self.n_embd,), weight=z[bbb + "ln2.weight"], bias=z[bbb + "ln2.bias"]
            )
            xx, xp = channel_mix(xx, state[i * 3 + 2], z, ffn)
            new_state[i * 3 + 2] = xp
            x = x + xx

        x = F.layer_norm(
            x, (self.n_embd,), weight=z["ln_out.weight"], bias=z["ln_out.bias"]
        )
        return x @ z["head.weight"], new_state

    def fresh_state(self, device: Any, dtype: Any) -> list[torch.Tensor]:
        state = []
        for _ in range(self.n_layer):
            state.append(torch.zeros(self.n_embd, dtype=dtype, device=device))
            state.append(
                torch.zeros(
                    self.n_embd // self.head_size, self.head_size, self.head_size,
                    dtype=torch.float, device=device,
                )
            )
            state.append(torch.zeros(self.n_embd, dtype=dtype, device=device))
        return state
