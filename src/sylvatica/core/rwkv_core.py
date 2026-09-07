"""`RwkvCore`: the one `Core` implementation, wrapping an RWKV-7 checkpoint.

WHY A RECURRENT CORE AT ALL. A transformer's memory is its context, so it
either carries everything or drops a suffix, and complaint 1 says everything in
the context weighs the same forever. RWKV-7's memory is a fixed-size state per
layer. Compression is therefore forced by the shape of the part rather than by
a policy bolted on top, and what falls out of the state is what this branch
calls forgetting. Phase 1's first reading is whether anything usable survives
in there at all; if Tier A at twenty turns is below the blind baseline, this
choice was wrong and the doc says so before the number is taken.

WHY THE `rwkv` PIP PACKAGE AND NOT A HAND-WRITTEN FORWARD. Inference only, for
now. The package's `RWKV` class carries the reference implementation of the v7
recurrence including every detail this branch has no interest in re-deriving,
and Phase 3 is the bet, not this. The training-side WKV7 CUDA kernel is a
separate question -- it needs `nvcc`, the box does not have one, and the
fallback is the pure-PyTorch chunked path. `RWKV_CUDA_ON` stays `0` here for
that reason and the cost of it is a reading, not a guess.

FP32 AND NOT FP16, AND THIS IS THE PASCAL TAX. A GTX 1080 Ti runs fp16 arithmetic
at 1/64 the fp32 rate; the half-precision path that makes this model fast on
anything modern makes it dramatically slower here. 1.5B in fp32 is about 6 GB of
the card's 11 GB, which fits, so there is no forcing reason to pay it.
"""

from __future__ import annotations

import os
from collections.abc import Sequence
from pathlib import Path
from typing import Any

import numpy as np

from .base import Cost, Sampling, clock, forward_flops

# THESE MUST BE SET BEFORE `rwkv` IS IMPORTED. The package reads them at import
# time to decide whether to TorchScript its modules and whether to compile a
# CUDA extension. Setting them after the import is silently a no-op, which is
# the kind of check that reads as a pass while protecting nothing.
os.environ.setdefault("RWKV_JIT_ON", "1")
os.environ.setdefault("RWKV_CUDA_ON", "0")

# `RWKV_V7_ON` IS NOT OPTIONAL AND ITS ABSENCE IS NOT AN ERROR YOU WOULD
# RECOGNISE. The package ships two model classes. The legacy `RWKV` detects
# versions 4 through 6 off the weight names; a v7 checkpoint has none of those
# markers, so it is detected as v5, LOADS WITHOUT COMPLAINT -- every tensor
# printed, on the card, right shapes -- and then dies on the first forward with
# `SimpleNamespace has no attribute n_head`, because v5's state init needs a
# field only v5's detection sets. With this set to "1" the module rebinds
# `RWKV = RWKV_x070` and the right class is used. An hour is what that costs to
# find, so it is written down here rather than rediscovered.
os.environ.setdefault("RWKV_V7_ON", "1")

# The RWKV "world" vocabulary. Not a Hugging Face tokenizer; it ships inside the
# `rwkv` package and is the same file for every model in the line.
WORLD_VOCAB = "rwkv_vocab_v20230424"


class RwkvCore:
    """An RWKV-7 checkpoint, its tokenizer, and a state that is a plain value.

    The state is a list of tensors, three per layer for v7: the attention's
    previous `x`, the `k*v` matrix that is the actual recurrent memory, and the
    channel-mix's previous `x`. Its size does not depend on how many tokens have
    been fed, which is the entire point. `state_bytes` reports it, and Phase 0's
    cost row records it because "tens of megabytes" is an assumption in the
    design doc rather than a measurement.

    Nothing outside this module may index into a state. Treat it as opaque.
    """

    def __init__(
        self,
        checkpoint: Path,
        strategy: str = "cuda fp32",
        sampling: Sampling | None = None,
    ) -> None:
        from rwkv.model import RWKV
        from rwkv.utils import PIPELINE

        self.checkpoint = Path(checkpoint)
        self.strategy = strategy
        self.sampling = sampling or Sampling()

        # The package wants the path WITHOUT the `.pth` suffix and appends it
        # itself. Passing the full path makes it look for `...pth.pth`.
        stem = str(self.checkpoint.with_suffix(""))
        self._model = RWKV(model=stem, strategy=strategy)
        self._pipeline = PIPELINE(self._model, WORLD_VOCAB)

        # The v7 class keeps its weights in `z`; the legacy one in `w`. Both are
        # plain dicts of tensors, so the count is the same question either way.
        weights = getattr(self._model, "z", None) or self._model.w
        self._params = sum(int(t.numel()) for t in weights.values() if hasattr(t, "numel"))
        self._name = f"{self.checkpoint.stem} [{strategy}]"

    # -- identity ----------------------------------------------------------

    @property
    def params(self) -> int:
        return self._params

    @property
    def name(self) -> str:
        return self._name

    @property
    def n_layer(self) -> int:
        return int(self._model.args.n_layer)

    @property
    def n_embd(self) -> int:
        return int(self._model.args.n_embd)

    # -- tokens ------------------------------------------------------------

    def encode(self, text: str) -> list[int]:
        return list(self._pipeline.encode(text))

    def decode(self, tokens: Sequence[int]) -> str:
        return self._pipeline.decode(list(tokens))

    # -- the state ---------------------------------------------------------

    def fresh_state(self) -> None:
        """A state that has been told nothing. `None` is how the package spells it."""
        return

    def copy_state(self, state: Any | None) -> Any | None:
        """A state that can be fed without disturbing the one passed in.

        `RWKV.forward` writes into the list it is given. A caller that holds a
        state for the exam's Tier A, feeds it, and then expects to re-feed the
        ORIGINAL would get the mutated one -- and the failure is a wrong number
        rather than an exception, which is the worst kind.
        """
        if state is None:
            return None
        return [t.clone() for t in state]

    def state_bytes(self, state: Any | None) -> int:
        if state is None:
            return 0
        return sum(int(t.numel()) * int(t.element_size()) for t in state)

    def save_state(self, state: Any | None, path: Path) -> None:
        """Write a state to disk, on the CPU so it reloads on a different card."""
        import torch

        path = Path(path)
        path.parent.mkdir(parents=True, exist_ok=True)
        payload = {
            "core": self._name,
            "n_layer": self.n_layer,
            "n_embd": self.n_embd,
            "tensors": None if state is None else [t.detach().cpu() for t in state],
        }
        # Written beside and moved, so a kill mid-write leaves the previous
        # state rather than half of a new one. The REPL saves every turn and
        # being killed is the normal way it ends.
        tmp = path.with_suffix(path.suffix + ".tmp")
        torch.save(payload, tmp)
        os.replace(tmp, path)

    def load_state(self, path: Path) -> Any | None:
        import torch

        payload = torch.load(Path(path), map_location="cpu", weights_only=False)
        if payload["n_layer"] != self.n_layer or payload["n_embd"] != self.n_embd:
            raise ValueError(
                f"state was saved from a {payload['core']} "
                f"({payload['n_layer']}x{payload['n_embd']}) and this core is "
                f"{self._name} ({self.n_layer}x{self.n_embd}). A state does not "
                "transfer between checkpoints."
            )
        tensors = payload["tensors"]
        if tensors is None:
            return None
        return [self._to_device(t) for t in tensors]

    def _to_device(self, tensor: Any) -> Any:
        # The package puts every state tensor on the device its strategy names,
        # and its dtypes differ per entry (the kv matrix is fp32 even under a
        # half-precision strategy). Matching the model's own weights is not
        # enough, so the dtype is preserved from the save and only the device
        # moves.
        device = "cuda" if "cuda" in self.strategy else "cpu"
        return tensor.to(device)

    # -- the calls ---------------------------------------------------------

    def feed(self, tokens: Sequence[int], state: Any | None) -> tuple[Any, Cost]:
        """Advance the state over `tokens`. Returns the new state and what it cost.

        The logits of the last token are discarded. A caller that wants them is
        calling `generate`.
        """
        tokens = list(tokens)
        if not tokens:
            return state, Cost(0, 0, 0.0, 0.0)
        working = self.copy_state(state)
        with clock() as c:
            _, working = self._model.forward(tokens, working)
        return working, Cost(
            tokens_in=len(tokens),
            tokens_out=0,
            seconds=c.seconds,
            flops=forward_flops(self._params, len(tokens)),
        )

    def generate(
        self,
        prompt_tokens: Sequence[int],
        state: Any | None,
        budget: int,
        sampling: Sampling | None = None,
    ) -> tuple[list[int], Any, Cost]:
        """Feed the prompt, then sample up to `budget` tokens back into the state.

        THE BUDGET IS SMALL ON PURPOSE. Complaint 5: time is cheap and words are
        not. The budget is a dial and every reading records it.

        Stops on a stop token, on a stop string appearing in the decoded tail,
        or at the budget. The returned state has the generated tokens in it, so
        the next turn continues from what was actually said.
        """
        sampling = sampling or self.sampling
        prompt_tokens = list(prompt_tokens)
        working = self.copy_state(state)
        out: list[int] = []
        occurrence: dict[int, float] = {}

        with clock() as c:
            logits, working = self._model.forward(prompt_tokens, working)
            for _ in range(budget):
                for token, count in occurrence.items():
                    logits[token] -= sampling.presence_penalty + count * sampling.frequency_penalty
                logits[0] -= 1e38  # Never sample end-of-text as the first thing.

                if sampling.greedy:
                    token = int(logits.argmax())
                else:
                    token = int(
                        self._pipeline.sample_logits(
                            logits, temperature=sampling.temperature, top_p=sampling.top_p
                        )
                    )
                if token in sampling.stop_tokens:
                    break

                for existing in occurrence:
                    occurrence[existing] *= sampling.penalty_decay
                occurrence[token] = 1.0 + occurrence.get(token, 0.0)

                out.append(token)
                # DECODED EVERY STEP AND NOT ONCE AT THE END, because a stop
                # string is a property of the text and the world tokenizer is
                # byte-level -- a token can carry half of a "\n\n". Checking the
                # tail of the decoded string is the only way the check is about
                # what was actually said.
                text = self.decode(out)
                if any(s in text for s in sampling.stop_strings):
                    break

                logits, working = self._model.forward([token], working)

        return (
            out,
            working,
            Cost(
                tokens_in=len(prompt_tokens),
                tokens_out=len(out),
                seconds=c.seconds,
                flops=forward_flops(self._params, len(prompt_tokens) + len(out)),
            ),
        )

    def embed(self, tokens: Sequence[int]) -> np.ndarray:
        """A vector for a span of text, read off the state the span produces.

        THIS IS A PLACEHOLDER AND THE DOC SAYS SO. The store's embeddings are a
        MiniLM-class external encoder; "core-derived embeddings in place of the
        external encoder" is an OPEN FORK, not a decision, and nothing has
        measured whether this reduction carries meaning. It exists so the
        protocol is whole and so the fork is one function away when Phase 2 has
        an exit to compare against.

        The reduction is the last layer's two `x_prev` vectors, L2-normalised.
        The recurrent kv matrix is deliberately left out: it is the memory of
        everything fed so far rather than of this span, and including it would
        make the vector depend on the state's history rather than on the text.
        """
        state, _ = self.feed(tokens, None)
        if state is None:
            return np.zeros(2 * self.n_embd, dtype=np.float32)
        # Three entries per layer, and the 1-D ones are the x_prev vectors.
        per_layer = len(state) // self.n_layer
        last = state[(self.n_layer - 1) * per_layer : self.n_layer * per_layer]
        flat = [t.detach().float().cpu().numpy().ravel() for t in last if t.dim() <= 2]
        vector = np.concatenate(flat) if flat else np.zeros(self.n_embd, dtype=np.float32)
        norm = float(np.linalg.norm(vector))
        return (vector / norm).astype(np.float32) if norm > 0 else vector.astype(np.float32)
