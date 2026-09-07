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
tidiness: it is what lets `tests/exam/test_wkv7_matches_reference.py` check this
implementation against the reference on the same checkpoint. It lives under
`tests/exam/` and not `tests/guards/` because it loads a checkpoint onto the
card, and exams are dispatched by hand. That test is the only thing standing
between "differentiable" and "differentiable and correct".
"""

from __future__ import annotations

from typing import Any

import torch
import torch.nn.functional as F

# The package's own constant: exp(-0.5) = 0.606531, applied inside the decay.
_DECAY = 0.606531


def _recurrence_loop(
    r: torch.Tensor, w: torch.Tensor, k: torch.Tensor, v: torch.Tensor,
    kk: torch.Tensor, a: torch.Tensor, s: torch.Tensor,
    T: int, H: int, N: int, dtype: Any,
) -> tuple[torch.Tensor, torch.Tensor]:
    """The recurrence one token at a time. THE REFERENCE, and it stays.

    This is the transcription of the package's own loop, and every claim the
    chunked path makes is a claim about agreeing with THIS. Deleting it once the
    fast path works would leave nothing to check the fast path against, which is
    how a subtly wrong forward gets to spend GPU-days looking fine.
    """
    out = []
    for t in range(T):
        r_, w_, k_, v_, kk_, a_ = r[t], w[t], k[t], v[t], kk[t], a[t]
        vk = v_.view(H, N, 1) @ k_.view(H, 1, N)
        ab = (-kk_).view(H, N, 1) @ (kk_ * a_).view(H, 1, N)
        s = s * w_.view(H, 1, N) + s @ ab.float() + vk.float()
        out.append((s.to(dtype=dtype) @ r_.view(H, N, 1)).view(H * N))
    return torch.stack(out), s


def _recurrence_chunked(
    r: torch.Tensor, w: torch.Tensor, k: torch.Tensor, v: torch.Tensor,
    kk: torch.Tensor, a: torch.Tensor, s: torch.Tensor,
    T: int, H: int, N: int, dtype: Any, chunk: int,
) -> tuple[torch.Tensor, torch.Tensor]:
    """The same recurrence, C tokens at a time. 97% of the forward lives here.

    THE MEASUREMENT THAT JUSTIFIES THE ALGEBRA BELOW: at 0.4B over 192 tokens,
    the token-at-a-time loop is 1.52s of a 1.57s forward. Per step it does four
    tiny matmuls on 16 heads of 64x64 -- work a 1080 Ti finishes in microseconds
    and then waits, because the cost is not arithmetic, it is one Python
    iteration and a handful of kernel launches per token. Chunking does not make
    the card work less. It makes the CPU ask fewer times.

    THE RECURRENCE, per head, with S of shape (value, key):

        S_t = S_{t-1} (diag(w_t) + b_t c_t^T) + v_t k_t^T,   out_t = S_t r_t

    where b_t = -kk_t and c_t = kk_t * a_t. That transition is DIAGONAL PLUS
    RANK ONE, and it is the rank-one part that makes this hard: a product of
    diagonal matrices is a cumulative product and needs no loop, while a product
    of DPLR matrices is dense and, done naively, is the loop again.

    THE WAY THROUGH is to stop treating S_{t-1} b_t as something to be
    multiplied out and start treating it as an unknown. Write u_t := S_{t-1} b_t
    in R^value. Then the recurrence becomes purely diagonal-decay with two
    rank-one writes,

        S_t = S_{t-1} diag(w_t) + u_t c_t^T + v_t k_t^T

    which unrolls in closed form. Let W_t = prod_{s<=t} w_s be the cumulative
    decay inside the chunk. A write made at step u survives to step t scaled by
    W_t / W_u, so with the shorthand ~c_u = c_u / W_u and ~k_u = k_u / W_u,

        S_t = [ S_-1 + sum_{u<=t} ( u_u ~c_u^T + v_u ~k_u^T ) ] diag(W_t)

    Substituting that back into the definition of u_t gives, with the shorthand
    ^b_t = W_{t-1} * b_t,

        u_t = S_-1 ^b_t + sum_{u<t} u_u (~c_u . ^b_t) + sum_{u<t} v_u (~k_u . ^b_t)

    -- u_t in terms of EARLIER u only. Stack the u_t as rows of U and that is a
    UNIT LOWER TRIANGULAR SYSTEM, solved in one batched call:

        (I - tril(^B ~C^T, -1)) U = ^B S_-1^T + tril(^B ~K^T, -1) V

    Outputs follow from the closed form with ^r_t = W_t * r_t, and the state at
    the chunk boundary is the bracket above at t = C-1. Everything is a matmul
    over (C x C) or (C x N); the Python loop is now over CHUNKS.

    WHERE THE NUMERICS BITE, AND WHY THE ANCHOR IS THE CHUNK'S MIDDLE. ~c and ~k
    divide by W, a cumulative product of decays, so a longer chunk lets that
    division amplify further -- w = exp(-0.606531 * sigmoid(.)) lies in
    (0.5453, 1), giving a ceiling of 0.5453^-C unanchored and 0.5453^-(C/2)
    anchored. THAT CEILING IS REACHED, not approached: measured on real text it
    is within a rounding error of the bound on both checkpoints, because one
    channel pinned at the never-forget limit is enough to achieve it and every
    RWKV-7 model has one. Do not expect a real-world discount here.

    THE FORWARD SURVIVED ALL OF THOSE AND THE GRADIENT DID NOT. At C=128 the
    logits still agreed with the loop to 1.5e-6 while the backward pass produced
    a non-finite gradient, because every quantity that MATTERS here is a ratio
    W_t / W_u with u <= t, which is bounded by one; only the separately
    materialised halves are enormous, and the forward's matmul happens to cancel
    them in an order the backward's does not. A forward-only check passes C=128
    and Phase 3 then trains on NaN.

    So the halves are anchored at the chunk's MIDDLE rather than its start:
    ~c_u = c_u * W_mid / W_u and ^b_t = b_t * W_{t-1} / W_mid. Every product is
    algebraically identical and no factor now spans more than half a chunk, which
    squares the safe length for four elementwise multiplies. The terms against
    the incoming state S_-1 keep the true unanchored scaling, since S_-1 is a
    real state and not an intermediate. All of this runs in float32 whatever the
    model's dtype.
    """
    rr = r.view(T, H, N).float().transpose(0, 1)
    ww = w.view(T, H, N).float().transpose(0, 1)
    kkey = k.view(T, H, N).float().transpose(0, 1)
    vv = v.view(T, H, N).float().transpose(0, 1)
    bb = (-kk).view(T, H, N).float().transpose(0, 1)
    cc = (kk * a).view(T, H, N).float().transpose(0, 1)

    outs = []
    for start in range(0, T, chunk):
        stop = min(start + chunk, T)
        C = stop - start
        wc = ww[:, start:stop]                                   # (H, C, N)
        W = torch.cumprod(wc, dim=1)
        # W_{t-1}, built by shifting rather than by dividing W by w. Dividing
        # would be one op shorter and would put a second division by a decay
        # into a routine whose only real hazard is division by decays.
        Wprev = torch.cat((torch.ones_like(wc[:, :1]), W[:, :-1]), dim=1)

        mid = W[:, C // 2 : C // 2 + 1]                          # (H, 1, N)

        # Against the incoming state, the true scaling. S_-1 is a real state
        # carried in from the previous chunk, not an intermediate of this one,
        # so nothing here may be rescaled.
        b_state = bb[:, start:stop] * Wprev
        r_state = rr[:, start:stop] * W

        # Within the chunk, both halves of every product are anchored at `mid`,
        # so the anchoring cancels exactly and neither half spans more than half
        # a chunk of decay.
        bhat = b_state / mid
        rhat = r_state / mid
        ctil = cc[:, start:stop] * (mid / W)
        ktil = kkey[:, start:stop] * (mid / W)
        vc = vv[:, start:stop]

        st = s.transpose(1, 2)                                   # (H, key, val)
        eye = torch.eye(C, device=s.device, dtype=s.dtype).expand(H, C, C)
        strict = torch.ones(C, C, device=s.device, dtype=s.dtype).tril(-1)

        lc = (bhat @ ctil.transpose(1, 2)) * strict
        lk = (bhat @ ktil.transpose(1, 2)) * strict
        u = torch.linalg.solve_triangular(
            eye - lc, b_state @ st + lk @ vc, upper=False
        )

        low = torch.ones(C, C, device=s.device, dtype=s.dtype).tril(0)
        out = (
            r_state @ st
            + ((rhat @ ctil.transpose(1, 2)) * low) @ u
            + ((rhat @ ktil.transpose(1, 2)) * low) @ vc
        )
        outs.append(out)

        # S_-1 carries the chunk's full decay; the writes made inside the chunk
        # carry only what is left after their own anchoring. Scaling both by the
        # same factor is the one place the midpoint anchor can silently go
        # wrong, because the forward stays plausible and the state drifts.
        s = s * W[:, -1].unsqueeze(1) + (
            u.transpose(1, 2) @ ctil + vc.transpose(1, 2) @ ktil
        ) * (W[:, -1] / mid[:, 0]).unsqueeze(1)

    xx = torch.cat(outs, dim=1).transpose(0, 1).reshape(T, H * N).to(dtype=dtype)
    return xx, s


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
    chunk: int = 0,
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

    if chunk and chunk > 1 and T > 1:
        xx, s = _recurrence_chunked(
            r, w, k, v, kk, a, state.float(), T, H, N, x.dtype, chunk
        )
    else:
        xx, s = _recurrence_loop(r, w, k, v, kk, a, state.float(), T, H, N, x.dtype)

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

    #: Tokens per chunk in the recurrence. 0 falls back to the token-at-a-time
    #: loop, which is the reference the chunked path is checked against.
    #:
    #: 32 IS CHOSEN FOR MARGIN AND NOT FOR SPEED, and the margin is measured
    #: rather than argued. At 0.4B over 512 tokens the anchored amplification
    #: reaches 8.9e3 at C=32, 1.5e8 at C=64 -- which still trains -- and 3.9e16
    #: at C=128, which does not: the forward there still agrees with the loop to
    #: 1.1e-6 while the BACKWARD produces a non-finite gradient. So the cliff is
    #: somewhere between 64 and 128, it is invisible to any forward-only check,
    #: and 32 sits two doublings below the last size known to work.
    #:
    #: THE BOUND IS TIGHT AND IT IS ARCHITECTURAL, which was not the guess. The
    #: worst amplification measured is 0.5453^-(C/2-1) to three figures on BOTH
    #: checkpoints, because both have a decay channel within 0.08% of the
    #: architectural maximum -- `max sigmoid(w0)` is 0.99966 at 0.4B and 0.99922
    #: at 1.5B, and one such channel anywhere achieves the ceiling. So the dial
    #: does NOT need re-measuring per checkpoint: every RWKV-7 model drives some
    #: channel to the never-forget limit, and the ceiling is reached there.
    chunk: int = 32

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
                    z, att, i, self.n_head, self.head_size, self.chunk,
                    use_reentrant=False,
                )
            else:
                xx, xp, s, v_first = time_mix(
                    xx, state[i * 3 + 0], v_first, state[i * 3 + 1],
                    z, att, i, self.n_head, self.head_size, self.chunk,
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
