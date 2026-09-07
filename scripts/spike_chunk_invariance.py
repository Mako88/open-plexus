"""Is feeding a transcript in pieces the same as feeding it whole?

WHY IT MATTERS, AND IT IS NOT A MICRO-OPTIMISATION. The full-context baseline
re-feeds the entire transcript from a zero state for every question -- 1.51
million tokens and 2.5 GPU-hours for one house. It does that because that is what
a transformer must do: attention recomputes over all positions and there is no
carry.

A RECURRENCE HAS NO SUCH REQUIREMENT. `S_t = f(S_{t-1}, x_t)` means feeding
[1..N] from zero and feeding [1..k] then [k+1..N] from `S_k` land on the same
state, by definition. If that holds numerically as well as algebraically, the
baseline's ANSWERS cost O(N) instead of O(N-squared) and its COST is arithmetic
we can report without spending.

WHAT COULD BREAK IT, checked here rather than assumed:

  - `v_first`. RWKV-7 computes `v` at layer 0 and reuses it in later layers. If
    that crossed a token boundary it would live in the state, and a chunk break
    would lose it. It does not -- it is per-token, within one forward -- but a
    silent difference here would be invisible and would corrupt the baseline.
  - Float rounding. `forward_seq` over 400 tokens and `forward_seq` over two
    lots of 200 do not have to produce bit-identical fp32. The question is
    whether the difference is at the last bits or somewhere that matters.
  - The one-token path. `forward_one` and `forward_seq` are different code. A
    chunk of length 1 goes down the other branch.

WOULD REFUTE THE WHOLE IDEA: a state difference large enough to change the
argmax of the logits. Then the pieces are not the whole and the baseline has to
be paid for in full.

  uv run python scripts/spike_chunk_invariance.py --size 1.5
"""

from __future__ import annotations

import argparse
import json
from datetime import UTC, datetime
from pathlib import Path


def max_state_diff(core, a, b) -> tuple[float, float]:
    """Largest absolute and relative difference between two states.

    Reaches into a state, which everything outside `core` is forbidden to do.
    THIS SCRIPT IS THE EXCEPTION AND THE REASON IS THE POINT: the question being
    asked is literally "are these two states the same", and no protocol method
    answers it. It lives in `scripts/` rather than in the package so the guard
    over `src/` stays honest.
    """

    worst_abs, worst_rel = 0.0, 0.0
    for x, y in zip(a, b):
        d = (x.float() - y.float()).abs()
        worst_abs = max(worst_abs, float(d.max()))
        scale = x.float().abs().max().clamp(min=1e-6)
        worst_rel = max(worst_rel, float(d.max() / scale))
    return worst_abs, worst_rel


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=float, default=1.5)
    parser.add_argument("--out", type=Path, default=Path("readings"))
    args = parser.parse_args()

    import torch

    from sylvatica.core.checkpoints import ensure
    from sylvatica.core.rwkv_core import RwkvCore
    from sylvatica.exam import generate_house
    from sylvatica.loop.turn import PREAMBLE, format_prompt

    core = RwkvCore(ensure(args.size))

    # A real transcript rather than lorem ipsum, because the question is about
    # this exam's own text.
    house = generate_house(seed=0, n_facts=50, n_turns=300, negatives=20)
    text = PREAMBLE + "\n\n".join(f"User: {line}" for line in house.turns[:120])
    tokens = core.encode(text)

    whole, _ = core.feed(tokens, None)

    results = []
    for label, splits in [
        ("halves", [len(tokens) // 2]),
        ("thirds", [len(tokens) // 3, 2 * len(tokens) // 3]),
        ("one-token-tail", [len(tokens) - 1]),
        ("token-at-a-time-tail", list(range(len(tokens) - 5, len(tokens)))),
    ]:
        state = None
        prev = 0
        for cut in [*splits, len(tokens)]:
            state, _ = core.feed(tokens[prev:cut], state)
            prev = cut

        abs_d, rel_d = max_state_diff(core, whole, state)

        # THE NUMBER THAT DECIDES IT is not the state difference but whether the
        # model's next-token choice moves. A state that differs in the eighth
        # decimal and picks the same token is the same state for every purpose
        # this branch has.
        probe = core.encode(format_prompt("What colour are the lanterns?"))
        lw, _ = core._model.forward(list(probe), core.copy_state(whole))
        lp, _ = core._model.forward(list(probe), core.copy_state(state))
        same_argmax = int(torch.argmax(lw)) == int(torch.argmax(lp))
        logit_gap = float((lw.float() - lp.float()).abs().max())
        top5_w = [int(i) for i in torch.topk(lw.float(), 5).indices]
        top5_p = [int(i) for i in torch.topk(lp.float(), 5).indices]

        row = {
            "split": label,
            "pieces": len(splits) + 1,
            "max_abs_state_diff": abs_d,
            "max_rel_state_diff": rel_d,
            "max_abs_logit_diff": logit_gap,
            "same_argmax": same_argmax,
            "same_top5": top5_w == top5_p,
        }
        results.append(row)
        print(json.dumps(row), flush=True)

    invariant = all(r["same_argmax"] and r["same_top5"] for r in results)
    reading = {
        "phase": 1,
        "kind": "chunk-invariance",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": (
            "whether feeding a transcript in pieces lands on the same state as "
            "feeding it whole, which decides whether the full-context baseline "
            "costs O(N) or O(N-squared)"
        ),
        "would_refute": (
            "a state difference large enough to change the argmax of the logits"
        ),
        "core": core.name,
        "size_b": args.size,
        "tokens": len(tokens),
        "results": results,
        "invariant": invariant,
    }
    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase1-chunk-invariance-{stamp}.json"
    path.write_text(json.dumps(reading, indent=2) + "\n", encoding="utf-8")
    print(f"\ninvariant: {invariant}\nwrote {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
