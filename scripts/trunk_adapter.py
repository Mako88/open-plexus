"""Step 3 on maths, fork (b): the trunk learns from the search's own wins, through an adapter.

A LoRA adapter (Hu et al., 2021) on the trunk's attention and MLP projections, the trunk's own
weights frozen, trained on step turns the system labelled right by outcome: from the rollout
rows (`step_rollouts.py`), every candidate whose rollouts all reached the gold number, as the
reply to the step turn for its problem and steps so far, answer lines included. Loss on the
reply's tokens only. One in ten problems is held out for the loss curve. Checkpoints every few
minutes and resumes from the last, since the card is shared. The adapter is merged into the
weights when `LocalTrunk(adapter=...)` loads it, so inference costs nothing more; the control
is the same reading without it.

    .venv/Scripts/python scripts/trunk_adapter.py --out state/adapters/round1       # the card
    .venv/Scripts/python scripts/step_search.py --arms trunk --adapter state/adapters/round1

Writes <out>/adapter/ (the PEFT weights), <out>/adapter.json and a loss log.
"""

from __future__ import annotations

import argparse
import json
import random
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.steps import step_messages  # noqa: E402

TARGETS = ["q_proj", "k_proj", "v_proj", "o_proj", "in_proj_qkv", "out_proj", "gate_proj", "up_proj", "down_proj"]


def sha() -> str:
    return subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True,
                          cwd=ROOT).stdout.strip()


def pairs(rows: list[dict], min_fraction: float = 1.0) -> list[dict]:
    """Training turns from rollout rows: each {"index", "messages", "reply"} for a candidate
    whose rollouts reached the gold number at least `min_fraction` of the time, one a distinct
    (problem, steps so far, candidate)."""
    seen, out = set(), []
    for r in rows:
        if r["fraction"] < min_fraction:
            continue
        key = (r["question"], tuple(r["steps"]), r["candidate"])
        if key in seen:
            continue
        seen.add(key)
        out.append({"index": r["index"], "messages": step_messages(r["question"], r["steps"]),
                    "reply": r["candidate"]})
    return out


def encode(tok, pair: dict) -> tuple[list[int], list[int]]:
    """Token ids of the turn with the reply appended, and labels that are -100 over the turn so
    the loss falls on the reply and its end-of-turn token only."""
    prompt = tok.apply_chat_template(pair["messages"], tokenize=False, add_generation_prompt=True,
                                     enable_thinking=False)
    p_ids = tok(prompt, add_special_tokens=False)["input_ids"]
    r_ids = tok(pair["reply"] + tok.eos_token, add_special_tokens=False)["input_ids"]
    return p_ids + r_ids, [-100] * len(p_ids) + r_ids


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--rows", default="state/rollouts/train.jsonl")
    p.add_argument("--out", default="state/adapters/round1")
    p.add_argument("--model", default="Qwen/Qwen3.5-2B")
    p.add_argument("--min-fraction", type=float, default=1.0)
    p.add_argument("--rank", type=int, default=16)
    p.add_argument("--alpha", type=int, default=32)
    p.add_argument("--lr", type=float, default=1e-4)
    p.add_argument("--epochs", type=float, default=2.0)
    p.add_argument("--batch", type=int, default=4, help="turns an optimiser step, accumulated")
    p.add_argument("--micro", type=int, default=1,
                   help="turns a forward pass; the logits over the vocabulary are gigabytes a turn, so one")
    p.add_argument("--max-tokens", type=int, default=640, help="a turn longer than this is dropped")
    p.add_argument("--checkpoint-minutes", type=float, default=4.0)
    p.add_argument("--eval-every", type=int, default=50, help="optimiser steps between held-out losses")
    p.add_argument("--seed", type=int, default=0)
    args = p.parse_args()

    import torch
    from peft import LoraConfig, PeftModel, get_peft_model
    from transformers import AutoModelForCausalLM, AutoTokenizer

    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    rows = [json.loads(line) for line in Path(args.rows).read_text(encoding="utf-8").splitlines()]
    data = pairs(rows, args.min_fraction)
    rng = random.Random(args.seed)
    indices = sorted({d["index"] for d in data})
    rng.shuffle(indices)
    held_idx = set(indices[:max(1, len(indices) // 10)])
    tok = AutoTokenizer.from_pretrained(args.model)
    encoded = [(d["index"], *encode(tok, d)) for d in data]
    encoded = [e for e in encoded if len(e[1]) <= args.max_tokens]
    train = [e for e in encoded if e[0] not in held_idx]
    held = [e for e in encoded if e[0] in held_idx]
    print(f"{len(data)} turns from {len(indices)} problems; {len(train)} train, {len(held)} held", flush=True)

    model = AutoModelForCausalLM.from_pretrained(args.model, dtype=torch.float32).to("cuda")
    model.gradient_checkpointing_enable()
    model.enable_input_require_grads()
    state_file = out / "train_state.json"
    if (out / "adapter").exists() and state_file.exists():
        model = PeftModel.from_pretrained(model, out / "adapter", is_trainable=True)
        state = json.loads(state_file.read_text(encoding="utf-8"))
        print(f"resuming at step {state['step']}", flush=True)
    else:
        model = get_peft_model(model, LoraConfig(r=args.rank, lora_alpha=args.alpha, lora_dropout=0.05,
                                                 target_modules=TARGETS, task_type="CAUSAL_LM"))
        state = {"step": 0, "seen": 0, "log": []}
    model.print_trainable_parameters()
    params = [q for q in model.parameters() if q.requires_grad]
    opt = torch.optim.AdamW(params, lr=args.lr, weight_decay=0.0)
    if (out / "optimizer.pt").exists() and state["step"] > 0:
        opt.load_state_dict(torch.load(out / "optimizer.pt", map_location="cuda"))
    total_steps = int(args.epochs * len(train) / args.batch)
    pad = tok.pad_token_id if tok.pad_token_id is not None else tok.eos_token_id

    def batch_of(items):
        width = max(len(ids) for _, ids, _ in items)
        x = torch.full((len(items), width), pad, dtype=torch.long)
        y = torch.full((len(items), width), -100, dtype=torch.long)
        m = torch.zeros((len(items), width), dtype=torch.long)
        for i, (_, ids, labels) in enumerate(items):  # right padding: the loss mask handles it
            x[i, :len(ids)] = torch.tensor(ids)
            y[i, :len(labels)] = torch.tensor(labels)
            m[i, :len(ids)] = 1
        return x.cuda(), y.cuda(), m.cuda()

    def held_loss() -> float:
        model.eval()
        total, count = 0.0, 0
        with torch.no_grad():
            for at in range(0, len(held), args.micro):
                x, y, m = batch_of(held[at:at + args.micro])
                total += float(model(input_ids=x, attention_mask=m, labels=y).loss) * len(x)
                count += len(x)
        model.train()
        return total / max(count, 1)

    def save() -> None:
        model.save_pretrained(out / "adapter")
        torch.save(opt.state_dict(), out / "optimizer.pt")
        state_file.write_text(json.dumps(state), encoding="utf-8")

    order_rng = random.Random(args.seed + 1)
    model.train()
    last_save = time.perf_counter()
    started = time.perf_counter()
    if state["step"] == 0:
        state["log"].append({"step": 0, "held_loss": round(held_loss(), 4)})
        print(f"step 0 held loss {state['log'][-1]['held_loss']}", flush=True)
    while state["step"] < total_steps:
        epoch = state["seen"] // len(train)
        order = list(range(len(train)))
        random.Random(args.seed + 100 + epoch).shuffle(order)
        at = state["seen"] % len(train)
        items = [train[i] for i in order[at:at + args.batch]]
        if not items:
            state["seen"] += len(train) - at
            continue
        loss = 0.0
        for at_micro in range(0, len(items), args.micro):  # the step's gradient accumulated a few turns at a time
            x, y, m = batch_of(items[at_micro:at_micro + args.micro])
            part = model(input_ids=x, attention_mask=m, labels=y).loss * (len(x) / len(items))
            part.backward()
            loss += float(part)
        torch.nn.utils.clip_grad_norm_(params, 1.0)
        opt.step()
        opt.zero_grad(set_to_none=True)
        state["step"] += 1
        state["seen"] += len(items)
        if state["step"] % 10 == 0:
            print(f"step {state['step']}/{total_steps} loss {loss:.4f}", flush=True)
        if state["step"] % args.eval_every == 0 or state["step"] == total_steps:
            state["log"].append({"step": state["step"], "train_loss": round(loss, 4),
                                 "held_loss": round(held_loss(), 4)})
            print(f"step {state['step']} held loss {state['log'][-1]['held_loss']}", flush=True)
        if time.perf_counter() - last_save > args.checkpoint_minutes * 60 or state["step"] == total_steps:
            save()
            last_save = time.perf_counter()
    save()
    (out / "adapter.json").write_text(json.dumps({
        "model": args.model, "rows": args.rows, "min_fraction": args.min_fraction, "turns": len(data),
        "train": len(train), "held": len(held), "problems": len(indices), "rank": args.rank, "alpha": args.alpha,
        "targets": TARGETS, "lr": args.lr, "epochs": args.epochs, "batch": args.batch, "micro": args.micro,
        "steps": total_steps,
        "log": state["log"], "seconds": round(time.perf_counter() - started, 1), "sha": sha(),
        "command": " ".join(sys.argv),
    }, indent=1), encoding="utf-8")
    print("wrote", out / "adapter.json")


if __name__ == "__main__":
    main()
