"""The gut: a sense of whether the system already knows the answer, read off the trunk's own state
before it answers (John, 2026-11-05: confidence should arrive with the thought, not be fetched).
Language models carry this signal (Kadavath et al., 2022, "Language Models (Mostly) Know What They
Know"); a small head reads it. Its use is deciding whether to look something up: the sleeps made
the system answer from memory and stop searching (2026-11-05), a habit, where the decision should
be a judgement about this question.

The label is outcome, not a list: the system answers a question with no passage, and the head
learns which of those answers came out right. The fit is a logistic regression over the trunk's
last-token state at one layer, read on held-out questions by its ranking (AUROC: how often a
right answer is ranked above a wrong one) and its calibration (of what it says at 80%, how much
is right).

    .venv/Scripts/python scripts/gut.py --stage labels --n 4000     # the served system, beside a game
    .venv/Scripts/python scripts/gut.py --stage states              # the trunk in process, the card
    .venv/Scripts/python scripts/gut.py --stage fit,read            # CPU

Writes state/gut/ (labels, states, the head) and readings/gut-*.json.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.hotpot import READ_SYSTEM, answer_of, lenient, questions  # noqa: E402
from openplexus.lineage import Lineage  # noqa: E402

OUT = ROOT / "state" / "gut"


def prompt_text(tok, question: str) -> str:
    """The question as the trunk reads it when it answers with no passage, up to where it would answer."""
    return tok.apply_chat_template([{"role": "system", "content": READ_SYSTEM}, {"role": "user", "content": question}],
                                   tokenize=False, add_generation_prompt=True, enable_thinking=False)


def fit_logistic(x: np.ndarray, y: np.ndarray, lam: float, steps: int = 400, lr: float = 0.5) -> tuple[np.ndarray, float]:
    """L2-regularised logistic regression by full-batch gradient descent on standardised features."""
    w, b = np.zeros(x.shape[1]), 0.0
    for _ in range(steps):
        p = 1 / (1 + np.exp(-(x @ w + b)))
        g = p - y
        w -= lr * (x.T @ g / len(y) + lam * w)
        b -= lr * g.mean()
    return w, b


def auroc(score: np.ndarray, y: np.ndarray) -> float:
    pos, neg = score[y == 1], score[y == 0]
    if not len(pos) or not len(neg):
        return float("nan")
    order = np.argsort(np.concatenate([pos, neg]))
    ranks = np.empty(len(order))
    ranks[order] = np.arange(1, len(order) + 1)
    return float((ranks[:len(pos)].sum() - len(pos) * (len(pos) + 1) / 2) / (len(pos) * len(neg)))


def calibration(p: np.ndarray, y: np.ndarray, bins: int = 5) -> list[dict]:
    edges = np.linspace(0, 1, bins + 1)
    out = []
    for lo, hi in zip(edges[:-1], edges[1:]):
        m = (p >= lo) & (p < hi if hi < 1 else p <= hi)
        if m.sum():
            out.append({"said": f"{lo:.1f}-{hi:.1f}", "n": int(m.sum()), "mean_said": round(float(p[m].mean()), 3),
                        "right": round(float(y[m].mean()), 3)})
    return out


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--stage", default="labels,states,fit,read")
    p.add_argument("--n", type=int, default=4000, help="training questions answered with no passage")
    p.add_argument("--skip", type=int, default=6000, help="past the questions the rounds and rehearsals used")
    p.add_argument("--layers", default="12,16,20")
    p.add_argument("--lams", default="0.001,0.01,0.1")
    p.add_argument("--batch", type=int, default=8)
    p.add_argument("--note", default="")
    args = p.parse_args()
    stages = args.stage.split(",")
    layers = [int(x) for x in args.layers.split(",")]
    OUT.mkdir(parents=True, exist_ok=True)
    train_qs = questions(args.n, split="train", skip=args.skip)
    val_qs = questions(200)  # the readings' questions: the gut is read where the rulers are
    sets = {"train": train_qs, "val": val_qs}

    if "labels" in stages:
        from openplexus.trunk import ServedTrunk

        trunk = ServedTrunk(max_new=160, parallel=args.batch)
        gguf = Lineage().ggufs()
        loaded = trunk.adapters()
        names = {g.name for g in gguf}
        trunk.lora = [{"id": a["id"], "scale": 1.0 if Path(a["path"]).name in names else 0.0} for a in loaded]
        for name, qs in sets.items():
            path = OUT / f"{name}-labels.jsonl"
            done = {json.loads(l)["id"] for l in path.read_text(encoding="utf-8").splitlines()} if path.exists() else set()
            todo = [q for q in qs if q["id"] not in done]
            for at in range(0, len(todo), 64):
                chunk = todo[at:at + 64]
                replies = trunk.ask_many([[{"role": "system", "content": READ_SYSTEM}, {"role": "user", "content": q["question"]}] for q in chunk])
                with open(path, "a", encoding="utf-8") as f:
                    for q, r in zip(chunk, replies):
                        said = answer_of(r)
                        f.write(json.dumps({"id": q["id"], "said": said, "answer": q["answer"], "right": lenient(said, q["answer"])}, ensure_ascii=False) + "\n")
                print(f"labels {name} {min(at + 64, len(todo))}/{len(todo)}", flush=True)

    def labels(name):
        rows = {json.loads(l)["id"]: json.loads(l) for l in (OUT / f"{name}-labels.jsonl").read_text(encoding="utf-8").splitlines()}
        return [rows[q["id"]] for q in sets[name]]

    if "states" in stages:
        from transformers import AutoTokenizer

        from openplexus.trunk import LocalTrunk, states_resumable

        trunk = LocalTrunk(batch=args.batch, adapter=str(Lineage().current()) if Lineage().exists() else None)
        tok = AutoTokenizer.from_pretrained("Qwen/Qwen3.5-2B")
        for name, qs in sets.items():
            states_resumable(trunk, [prompt_text(tok, q["question"]) for q in qs], layers, OUT / f"{name}.part")
        trunk.close()

    def states(name, n):
        return {layer: np.fromfile((OUT / f"{name}.part").with_suffix(f".l{layer}.f32"), dtype=np.float32).reshape(n, -1) for layer in layers}

    reading = {"kind": "gut", "train": len(train_qs), "val": len(val_qs), "layers": layers, "system_round": Lineage().round()}
    if "fit" in stages or "read" in stages:
        ytr = np.array([r["right"] for r in labels("train")], dtype=float)
        yva = np.array([r["right"] for r in labels("val")], dtype=float)
        htr, hva = states("train", len(train_qs)), states("val", len(val_qs))
        held = len(ytr) // 5
        best, table = None, {}
        for layer in layers:
            mu, sd = htr[layer][:-held].mean(0), htr[layer][:-held].std(0) + 1e-6
            xa, xh = (htr[layer][:-held] - mu) / sd, (htr[layer][-held:] - mu) / sd
            for lam in [float(x) for x in args.lams.split(",")]:
                w, b = fit_logistic(xa, ytr[:-held], lam)
                a = auroc(xh @ w + b, ytr[-held:])
                table[f"l{layer}-lam{lam:g}"] = round(a, 3)
                if best is None or a > best[0]:
                    best = (a, layer, lam)
        _, layer, lam = best
        # the head on four fifths, and its raw scores rescaled on the held fifth (Platt: a and c fitted
        # so the probability it states is the rate it is right), so the stated confidence is calibrated
        mu, sd = htr[layer][:-held].mean(0), htr[layer][:-held].std(0) + 1e-6
        w, b = fit_logistic((htr[layer][:-held] - mu) / sd, ytr[:-held], lam)
        raw_held = ((htr[layer][-held:] - mu) / sd) @ w + b
        z = (raw_held - raw_held.mean()) / (raw_held.std() + 1e-6)
        aw, ab = fit_logistic(z[:, None], ytr[-held:], 0.0, steps=2000, lr=0.1)
        scale = {"mean": float(raw_held.mean()), "std": float(raw_held.std() + 1e-6), "a": float(aw[0]), "c": float(ab)}
        np.savez(OUT / "gut.npz", w=w, b=b, mu=mu, sd=sd, layer=layer, lam=lam, **{f"platt_{k}": v for k, v in scale.items()})
        raw_va = ((hva[layer] - mu) / sd) @ w + b
        pva = 1 / (1 + np.exp(-(scale["a"] * (raw_va - scale["mean"]) / scale["std"] + scale["c"])))
        reading.update({
            "base_rate": {"train": round(float(ytr.mean()), 3), "val": round(float(yva.mean()), 3)},
            "held_out_auroc": table, "chosen": {"layer": layer, "lam": lam},
            "val_auroc": round(auroc(pva, yva), 3), "val_calibration": calibration(pva, yva),
            # the use: search only when the gut doubts; how often would it have been right to answer alone
            "answer_alone_when_sure": {f"{t:.1f}": {"share": round(float((pva >= t).mean()), 3),
                                                    "right": round(float(yva[pva >= t].mean()), 3) if (pva >= t).any() else None}
                                       for t in (0.5, 0.7, 0.9)},
        })
        print(json.dumps({k: reading[k] for k in ("base_rate", "chosen", "val_auroc", "val_calibration", "answer_alone_when_sure")}, indent=1), flush=True)
    reading.update({"command": "scripts/gut.py " + " ".join(sys.argv[1:]), "note": args.note,
                    "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True, cwd=ROOT).stdout.strip(),
                    "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")})
    if "fit" in stages or "read" in stages:
        out = ROOT / "readings" / f"gut-{reading['taken_at']}.json"
        out.write_text(json.dumps(reading, indent=1), encoding="utf-8")
        print(f"wrote {out}")


if __name__ == "__main__":
    main()
