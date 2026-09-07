"""Can this machinery put ANY fact into the weights? The precondition for Phase 3.

WHY THIS EXISTS. Two Phase 3 runs put Tier C at exactly 0.0000: one at lr 1e-4,
where perplexity climbed from 22.9 to 42.7 (the adapter wrecked the base and
learned nothing), and one at lr 1e-5, where perplexity held perfectly flat and it
STILL learned nothing. That is not a trade-off curve. Two very different regimes
producing the identical zero is the signature of something that cannot learn at
all, and calling it "the raw arm is refuted" without checking would be reading a
broken tool as a result -- the fourth time tonight that was available.

SO THIS IS THE PRECONDITION TEST, and it is deliberately unfair to the point of
absurdity: a handful of facts, trained over and over, at a learning rate nobody
would ship, then asked exactly those questions.

  IF IT SCORES HIGH: the machinery works. Tier C being zero is then a fact about
  the replay SCHEDULE -- a few hundred tokens a cycle, each fact seen once or
  twice -- which is precisely what the doc predicts for raw episodes, and the
  declaratives arm is the answer.

  IF IT SCORES ZERO: something is broken between the adapter and the loss, and
  every Phase 3 number so far is about that break rather than about
  consolidation. Nothing in the readings would have said so.

WOULD REFUTE THE MACHINERY: overfitting twenty facts for twenty epochs and still
answering none of them.

  uv run python scripts/spike_can_it_learn.py --size 0.4
"""

from __future__ import annotations

import argparse
import json
from datetime import UTC, datetime
from pathlib import Path

import torch

from sylvatica.exam import generate_house
from sylvatica.learn.consolidate import TrainingSet, train
from sylvatica.learn.lora import LoraAdapter
from sylvatica.loop.turn import PREAMBLE, STOP_STRINGS, format_prompt, trim_at_stop


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=float, default=0.4)
    parser.add_argument("--facts", type=int, default=20)
    parser.add_argument("--epochs", type=int, default=20)
    parser.add_argument("--lr", type=float, default=3e-4)
    parser.add_argument("--rank", type=int, default=8)
    parser.add_argument(
        "--arm", choices=["raw", "declaratives"], default="raw",
        help="raw trains on the told sentence; declaratives trains on paraphrases of it",
    )
    parser.add_argument("--out", type=Path, default=Path("readings"))
    args = parser.parse_args()

    from sylvatica.core.base import Sampling
    from sylvatica.core.checkpoints import ensure
    from sylvatica.core.rwkv_core import RwkvCore

    core = RwkvCore(ensure(args.size))
    house = generate_house(
        seed=0, n_facts=args.facts, n_turns=args.facts * 4 + 20,
        delays=(1,), negatives=0,
    )
    facts = house.facts[: args.facts]
    print(f"overfitting {len(facts)} facts for {args.epochs} epochs at lr {args.lr}")

    def ask_all(adapter: LoraAdapter | None) -> tuple[int, list[dict]]:
        applied = adapter is not None
        if applied:
            adapter.apply_to(core._model.z)
        try:
            state, _ = core.feed(core.encode(PREAMBLE), None)
            rows, correct = [], 0
            for fact in facts:
                out, _, _ = core.generate(
                    core.encode(format_prompt(fact.question)),
                    core.copy_state(state), 24,
                    Sampling(stop_strings=STOP_STRINGS, greedy=True),
                )
                said = trim_at_stop(core.decode(out), STOP_STRINGS)
                hit = fact.answer.lower() in said.lower()
                correct += hit
                rows.append({"asked": fact.question, "wanted": fact.answer,
                             "said": said, "correct": hit})
        finally:
            if applied:
                adapter.revert_from(core._model.z)
        return correct, rows

    before, before_rows = ask_all(None)
    print(f"before training: {before}/{len(facts)}")

    adapter = LoraAdapter(core._model.z, rank=args.rank)

    if args.arm == "raw":
        texts = [f.told for f in facts]
    else:
        # THE ARM'S CLAIM, TESTED DIRECTLY: varying the surface while holding the
        # binding fixed. Templates only, no core paraphrases -- a template cannot
        # introduce a falsehood, so this measures the PARAPHRASE IDEA rather than
        # the extractor's error rate, which has its own reading.
        from sylvatica.learn.extract import templates

        texts = []
        for f in facts:
            texts.append(f.told)
            texts.extend(templates(f.told))
    print(f"arm={args.arm}: {len(texts)} training sentences for {len(facts)} facts")

    training = TrainingSet(
        texts=texts, arm=args.arm, fragments=len(facts), general=0
    )
    loss, tokens, seconds = train(
        core, adapter, training, lr=args.lr, seq_len=128, epochs=args.epochs
    )
    print(f"trained {tokens} tokens in {seconds:.0f}s, final mean loss {loss:.4f}")

    after, after_rows = ask_all(adapter)
    print(f"after training:  {after}/{len(facts)}")
    for r in after_rows[:6]:
        print(f"  {'OK ' if r['correct'] else 'NO '} {r['wanted']!r:14} <- {r['said'][:60]!r}")

    # Did the weights move at all? A zero delta would mean the optimiser never
    # stepped, which is a different failure from a delta that does not help.
    delta = max(
        float(adapter.delta(n).detach().abs().max()) for n in adapter.names
    )
    grad_ok = any(p.grad is not None and torch.isfinite(p.grad).all()
                  for p in adapter.parameters())

    reading = {
        "phase": 3,
        "kind": "can-it-learn",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": "whether the adapter can put any fact into the weights at all",
        "would_refute": (
            "overfitting these facts for these epochs and still answering none"
        ),
        "core": core.name,
        "size_b": args.size,
        "facts": len(facts),
        "arm": args.arm,
        "training_sentences": len(texts),
        "epochs": args.epochs,
        "lr": args.lr,
        "rank": args.rank,
        "tokens_trained": tokens,
        "final_loss": round(loss, 4),
        "max_abs_delta": delta,
        "gradients_finite": grad_ok,
        "correct_before": before,
        "correct_after": after,
        "before": before_rows,
        "after": after_rows,
        "verdict": {
            "machinery_can_learn": after > before,
            "weights_moved": delta > 0,
        },
    }
    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase3-can-it-learn-{args.arm}-{stamp}.json"
    path.write_text(json.dumps(reading, indent=2) + "\n", encoding="utf-8")
    print(f"\nmax |delta| {delta:.5f}  gradients finite {grad_ok}")
    print(json.dumps(reading["verdict"], indent=2))
    print(f"wrote {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
