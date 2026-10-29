"""Step 3 on maths: the evaluator over solution steps, from PRM800K labels.

Two phases, as in `evaluator_fit.py`. `features` draws labelled candidate steps, as many wrong
as right, from PRM800K: fit and held-out from phase2_train split by record, test from
phase2_test; the trunk reads the judge prompt of each (`steps.judge_prompt`) and the last
token's hidden state at each layer is cached, in chunks appended to disk, so a run killed for
the card resumes. `fit` is the closed-form head: ridge to +1/-1 over standardised states,
layer and ridge picked by balanced accuracy on the held-out set; the reading is balanced
accuracy and the area under the curve on the test candidates, with the same for the
trunk's own yes/no as a comparison the next phase adds.

    .venv/Scripts/python scripts/step_evaluator.py features --n 24000 --out state/features/steps
    .venv/Scripts/python scripts/step_evaluator.py fit --features state/features/steps --out state/steps

Writes <out>/evaluator.npz and readings/step-evaluator-fit-*.json. `features` needs the card.
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

from openplexus.heads import LinearHead  # noqa: E402
from openplexus.steps import judge_prompt, prm_examples  # noqa: E402

SPLITS = ("fit", "held", "test")


def sha() -> str:
    return subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True,
                          cwd=ROOT).stdout.strip()


def states_resumable(trunk, prompts: list[str], layers: list[int], part: Path, chunk: int = 256) -> dict:
    dim = int(trunk.model.config.get_text_config().hidden_size)
    done = min([part.with_suffix(f".l{layer}.f32").stat().st_size // 4 // dim
                for layer in layers if part.with_suffix(f".l{layer}.f32").exists()] or [0])
    for at in range(done, len(prompts), chunk):
        h = trunk.hidden(prompts[at:at + chunk], layers)
        for layer in layers:
            with open(part.with_suffix(f".l{layer}.f32"), "ab") as f:
                f.write(h[layer].astype(np.float32).tobytes())
        print(f"  {part.name} {min(at + chunk, len(prompts))}/{len(prompts)}", flush=True)
    return {layer: np.fromfile(part.with_suffix(f".l{layer}.f32"), dtype=np.float32).reshape(len(prompts), -1)
            for layer in layers}


def build_features(args) -> None:
    from openplexus.trunk import LocalTrunk

    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    layers = [int(x) for x in args.layers.split(",")]
    train = prm_examples("phase2_train", n=args.n, seed=args.seed)
    records = sorted({x["record"] for x in train})
    rng = np.random.default_rng(args.seed)
    held_records = set(rng.choice(records, size=len(records) // 5, replace=False).tolist())
    splits = {
        "fit": [x for x in train if x["record"] not in held_records],
        "held": [x for x in train if x["record"] in held_records],
        "test": prm_examples("phase2_test", n=args.n_test, seed=args.seed),
    }
    trunk = LocalTrunk(args.model, batch=args.batch, dtype=args.dtype)
    started = time.perf_counter()
    for split, exs in splits.items():
        prompts = [judge_prompt(x["problem"], x["steps"], x["candidate"]) for x in exs]
        hidden = states_resumable(trunk, prompts, layers, out / f"{split}.part", chunk=args.batch * 32)
        np.savez(out / f"{split}.npz", cand_label=np.array([x["label"] for x in exs], dtype=np.float32),
                 rating=np.array([x["rating"] for x in exs], dtype=np.int8),
                 **{f"l{layer}": hidden[layer] for layer in layers})
        (out / f"{split}.json").write_text(json.dumps({"examples": exs}, ensure_ascii=False), encoding="utf-8")
        print(f"{split}: {len(exs)} candidates, {sum(x['label'] > 0 for x in exs)} right", flush=True)
    (out / "features.json").write_text(json.dumps({
        "model": trunk.served_model(), "layers": layers, "n": args.n, "n_test": args.n_test, "seed": args.seed,
        "prompt": "openplexus.steps.judge_prompt", "labels": "PRM800K rating -1 -> -1, 0 or 1 -> +1",
        "sizes": {k: len(v) for k, v in splits.items()}, "seconds": round(time.perf_counter() - started, 1),
        "sha": sha(), "command": " ".join(sys.argv),
    }, indent=1), encoding="utf-8")
    print("wrote", out / "features.json")


def balanced_accuracy(scores: np.ndarray, labels: np.ndarray, tau: float) -> float:
    return (float(np.mean(scores[labels > 0] > tau)) + float(np.mean(scores[labels < 0] <= tau))) / 2


def auc(scores: np.ndarray, labels: np.ndarray) -> float:
    """The chance a right candidate outscores a wrong one, by ranks."""
    order = scores.argsort()
    ranks = np.empty(len(scores))
    ranks[order] = np.arange(1, len(scores) + 1)
    pos = labels > 0
    n_pos, n_neg = int(pos.sum()), int((~pos).sum())
    return float((ranks[pos].sum() - n_pos * (n_pos + 1) / 2) / (n_pos * n_neg))


def pick_threshold(scores: np.ndarray, labels: np.ndarray) -> float:
    best = (0.0, 0.0)
    for tau in np.quantile(scores, np.linspace(0.02, 0.98, 97)):
        b = balanced_accuracy(scores, labels, float(tau))
        if b > best[0]:
            best = (b, float(tau))
    return best[1]


def fit(args) -> None:
    feats = Path(args.features)
    about = json.loads((feats / "features.json").read_text(encoding="utf-8"))
    layers = [int(x) for x in args.layers.split(",")] if args.layers else about["layers"]
    lams = [float(x) for x in args.lams.split(",")]
    data = {split: dict(np.load(feats / f"{split}.npz")) for split in SPLITS}

    sweep, best = [], None
    for layer in layers:
        x_fit, y_fit = data["fit"][f"l{layer}"], data["fit"]["cand_label"]
        x_held, y_held = data["held"][f"l{layer}"], data["held"]["cand_label"]
        mean, std = x_fit.mean(0), x_fit.std(0) + 1e-6
        for lam in lams:
            head = LinearHead.fit((x_fit - mean) / std, y_fit[:, None], lam, layer, "step")
            sc = head.query((x_held - mean) / std)[:, 0]
            tau = pick_threshold(sc, y_held)
            b, a = balanced_accuracy(sc, y_held, tau), auc(sc, y_held)
            sweep.append({"layer": layer, "lam": lam, "threshold": round(tau, 4),
                          "held_balanced_accuracy": round(b, 3), "held_auc": round(a, 3)})
            print(f"layer {layer} lam {lam}: held balanced accuracy {b:.3f} auc {a:.3f}", flush=True)
            if best is None or b > best[0]:
                best = (b, layer, lam, tau)
    _, layer, lam, tau = best
    x_all = np.concatenate([data["fit"][f"l{layer}"], data["held"][f"l{layer}"]])
    y_all = np.concatenate([data["fit"]["cand_label"], data["held"]["cand_label"]])
    mean, std = x_all.mean(0), x_all.std(0) + 1e-6
    evaluator = LinearHead.fit((x_all - mean) / std, y_all[:, None], lam, layer, "step")
    evaluator.threshold, evaluator.mean, evaluator.std = tau, mean, std
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    evaluator.save(out / "evaluator.npz")
    sc = evaluator.query(data["test"][f"l{layer}"])[:, 0]
    y_test = data["test"]["cand_label"]
    rating = data["test"]["rating"]
    test = {
        "n": int(len(y_test)), "balanced_accuracy": round(balanced_accuracy(sc, y_test, tau), 3),
        "auc": round(auc(sc, y_test), 3),
        "right_candidates_passed": round(float(np.mean(sc[y_test > 0] > tau)), 3),
        "wrong_candidates_failed": round(float(np.mean(sc[y_test < 0] <= tau)), 3),
        "neutral_candidates_passed": round(float(np.mean(sc[rating == 0] > tau)), 3) if (rating == 0).any() else None,
    }
    reading = {
        "kind": "step-evaluator-fit", "step": 3, "features": {"path": str(feats), **about},
        "chosen": {"layer": layer, "lam": lam, "threshold": round(tau, 4)}, "sweep": sweep,
        "test": test, "chance": {"balanced_accuracy": 0.5, "auc": 0.5},
        "evaluator_path": str(out / "evaluator.npz"), "command": " ".join(sys.argv), "sha": sha(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"), "note": args.note,
    }
    print(json.dumps(test, indent=1))
    path = ROOT / "readings" / f"step-evaluator-fit-{reading['taken_at']}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print("wrote", path)


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="phase", required=True)
    f = sub.add_parser("features")
    f.add_argument("--out", default="state/features/steps")
    f.add_argument("--n", type=int, default=24000, help="train candidates drawn, half wrong")
    f.add_argument("--n-test", type=int, default=4000)
    f.add_argument("--layers", default="12,16,20")
    f.add_argument("--model", default="Qwen/Qwen3.5-2B")
    f.add_argument("--dtype", default="float32")
    f.add_argument("--batch", type=int, default=8)
    f.add_argument("--seed", type=int, default=0)
    f.set_defaults(run=build_features)
    g = sub.add_parser("fit")
    g.add_argument("--features", default="state/features/steps")
    g.add_argument("--out", default="state/steps")
    g.add_argument("--layers", default=None)
    g.add_argument("--lams", default="1,10,100,1000,10000")
    g.add_argument("--note", default="")
    g.set_defaults(run=fit)
    args = p.parse_args()
    args.run(args)


if __name__ == "__main__":
    main()
