"""Step 3, second form: the evaluator trained by gradient, from the cached states.

The closed-form evaluator (`evaluator_fit.py`) picks the right candidate about half the time
past the first hop. This trains a small MLP over the same features (a cached judge state,
optionally followed by when-heard features) listwise: at each state the candidates' scores
and a learned stop logit go through a softmax, and the target is the right candidate or
stop. AdamW, a checkpoint every few minutes, resume from it, the best epoch by held-out
recall@1 kept. The reading is the same teacher-forced table as the closed form's, on the test
stories, so the two heads are read side by side.

    .venv/Scripts/python scripts/head_train.py --features state/features/eval --layer 16 \
        [--time] --out state/walker-step2

Writes <out>/evaluator-mlp[-time].pt and readings/evaluator-train-*.json. Runs on the card if
it is free and on the CPU if not; the model is small enough for either.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
sys.path.insert(0, str(ROOT / "scripts"))

from evaluator_fit import SPLITS, features, load_split, overall_recall, read  # noqa: E402
from openplexus.heads import MLPHead  # noqa: E402


def grouped(exs: list[dict], x: np.ndarray, k: int):
    """Per state: a (k, dim) block of candidate features, a mask of real candidates, and the
    target index (k means stop). States with no candidate are left out of training."""
    n = len(exs)
    dim = x.shape[1]
    blocks = np.zeros((n, k, dim), np.float32)
    mask = np.zeros((n, k), bool)
    target = np.full(n, k, np.int64)
    at = 0
    keep = []
    for i, ex in enumerate(exs):
        m = len(ex["cands"])
        if m == 0:
            continue
        blocks[i, :m] = x[at:at + m]
        mask[i, :m] = True
        if ex["target"] >= 0 and ex["target"] in ex["cands"]:
            target[i] = ex["cands"].index(ex["target"])
        at += m
        keep.append(i)
    keep = np.array(keep)
    return blocks[keep], mask[keep], target[keep]


def scores_for(head: MLPHead, x: np.ndarray) -> np.ndarray:
    out = []
    for at in range(0, len(x), 4096):
        out.append(head.query(x[at:at + 4096])[:, 0])
    return np.concatenate(out) if out else np.zeros(0, np.float32)


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--features", default="state/features/eval")
    p.add_argument("--layer", type=int, default=16)
    p.add_argument("--time", action="store_true", help="add when-heard features")
    p.add_argument("--hidden", type=int, default=256)
    p.add_argument("--epochs", type=int, default=30)
    p.add_argument("--lr", type=float, default=1e-3)
    p.add_argument("--weight-decay", type=float, default=1e-2)
    p.add_argument("--batch", type=int, default=64, help="states a step")
    p.add_argument("--checkpoint-minutes", type=float, default=3.0)
    p.add_argument("--out", default="state/walker-step2")
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--note", default="")
    args = p.parse_args()

    import torch

    torch.manual_seed(args.seed)
    device = "cuda" if torch.cuda.is_available() else "cpu"
    feats = Path(args.features)
    about = json.loads((feats / "features.json").read_text(encoding="utf-8"))
    k = about["k"]
    data = {split: load_split(feats, split) for split in SPLITS}
    x = {split: features(data[split][0], data[split][1], args.layer, args.time) for split in SPLITS}
    mean, std = x["fit"].mean(0), x["fit"].std(0) + 1e-6
    key = "label+time" if args.time else "label"
    name = "evaluator-mlp-time.pt" if args.time else "evaluator-mlp.pt"
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    ckpt_path = out / (name + ".ckpt")

    head = MLPHead(x["fit"].shape[1], args.layer, key, args.hidden, mean, std).to(device)
    opt = torch.optim.AdamW(head.parameters(), lr=args.lr, weight_decay=args.weight_decay)
    blocks, mask, target = grouped(data["fit"][0], (x["fit"] - mean) / std, k)
    blocks_t = torch.as_tensor(blocks, device=device)
    mask_t = torch.as_tensor(mask, device=device)
    target_t = torch.as_tensor(target, device=device)
    print(f"{len(blocks)} training states, {blocks.shape[2]} features, on {device}", flush=True)

    start_epoch, best, history = 0, None, []
    if ckpt_path.exists():
        ck = torch.load(ckpt_path, weights_only=False, map_location=device)
        head.net.load_state_dict(ck["net"])
        head.stop.data = ck["stop"].to(device)
        opt.load_state_dict(ck["opt"])
        start_epoch, best, history = ck["epoch"], ck["best"], ck["history"]
        print(f"resumed at epoch {start_epoch}", flush=True)

    def checkpoint(epoch: int) -> None:
        torch.save({"net": head.net.state_dict(), "stop": head.stop.data.cpu(),
                    "opt": opt.state_dict(), "epoch": epoch, "best": best, "history": history},
                   ckpt_path)

    def held_recall() -> float:
        return overall_recall(read(scores_for(head, x["held"]), data["held"][0], 0.0))

    last_ckpt = time.perf_counter()
    rng = np.random.default_rng(args.seed + start_epoch)
    for epoch in range(start_epoch, args.epochs):
        head.net.train()
        order = rng.permutation(len(blocks))
        total = 0.0
        for at in range(0, len(order), args.batch):
            idx = torch.as_tensor(order[at:at + args.batch], device=device)
            logits = head.logits(blocks_t[idx])  # (b, k)
            logits = logits.masked_fill(~mask_t[idx], -1e9)
            logits = torch.cat([logits, head.stop.expand(len(idx), 1)], dim=1)
            loss = torch.nn.functional.cross_entropy(logits, target_t[idx])
            opt.zero_grad()
            loss.backward()
            opt.step()
            total += float(loss) * len(idx)
            if time.perf_counter() - last_ckpt > 60 * args.checkpoint_minutes:
                checkpoint(epoch)
                last_ckpt = time.perf_counter()
        r = held_recall()
        history.append({"epoch": epoch + 1, "loss": round(total / len(blocks), 4),
                        "held_recall@1": round(r, 4)})
        print(f"epoch {epoch + 1}: loss {total / len(blocks):.4f} held recall@1 {r:.3f}",
              flush=True)
        if best is None or r > best["held_recall@1"]:
            best = {"epoch": epoch + 1, "held_recall@1": round(r, 4)}
            head.save(out / name)
        checkpoint(epoch + 1)

    best_head = MLPHead.load(out / name, device)
    reading = {
        "kind": "evaluator-train",
        "step": 3,
        "features": {"path": str(feats), **about},
        "time_features": args.time,
        "layer": args.layer,
        "hidden": args.hidden,
        "epochs": args.epochs,
        "lr": args.lr,
        "weight_decay": args.weight_decay,
        "batch_states": args.batch,
        "device": device,
        "best": best,
        "history": history,
        "teacher_forced": {
            "test": read(scores_for(best_head, x["test"]), data["test"][0], 0.0),
            "held": read(scores_for(best_head, x["held"]), data["held"][0], 0.0),
        },
        "evaluator_path": str(out / name),
        "command": " ".join(sys.argv),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True,
                              text=True, cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
        "note": args.note,
    }
    print(json.dumps(reading["teacher_forced"]["test"], indent=1))
    path = ROOT / "readings" / f"evaluator-train-{reading['taken_at']}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    ckpt_path.unlink(missing_ok=True)
    print("wrote", path)


if __name__ == "__main__":
    main()
