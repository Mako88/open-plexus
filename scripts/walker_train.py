"""Step 2's walker trained by gradient, as a ranker over the rows heard so far.

The closed-form walker maps the hop state to a MiniLM key and ranks the story's rows by
cosine. It puts the next right row in its top four 96-98% of the time on task 2 and
72/98/62% on task 3's hops, and that bounds what the evaluator can choose from. This trains
a small MLP that scores every row heard so far from three things a cosine cannot combine:
the hop state, the row's key, and when the row was heard. Listwise over the pool (rows
already read excluded), AdamW, a checkpoint every few minutes and resume from it, the best
epoch by held-out recall@1 kept. The hop states come from the evaluator's feature cache
(`evaluator_fit.py features`), which holds the state at every teacher-forced hop; the keys
come from the store. The reading is teacher-forced recall@1 and @4 per task and hop on the
test stories, beside the closed-form walker's on the same states.

    .venv/Scripts/python scripts/walker_train.py --features state/features/eval \
        --store state/store-step1 --out state/walker-step2

Writes <out>/walker-rank.pt and readings/walker-train-*.json.
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

from evaluator_fit import SPLITS  # noqa: E402
from openplexus.babi import stories  # noqa: E402
from openplexus.embed import make  # noqa: E402
from openplexus.heads import LinearHead, RankWalker, when_heard  # noqa: E402
from openplexus.store import Store  # noqa: E402


def hop_examples(feats: Path, split: str, by_episode: dict, store: Store, key: str,
                 embed) -> list[dict]:
    """One example per cached hop state whose walk goes on: the pool of rows heard and not
    read, as story indices, their keys and when-heard numbers, and the target's place. The
    test stories' keys come from the store, which holds them; the train stories were never
    appended, and a train episode shares its name with a test one, so theirs come from the
    embedder."""
    meta = json.loads((feats / f"{split}.json").read_text(encoding="utf-8"))["examples"]
    hidden = np.load(feats / f"{split}.npz")["hop_hidden"]
    keys_of: dict[str, np.ndarray] = {}
    out = []
    for i, ex in enumerate(meta):
        if ex["target"] < 0:
            continue
        story = by_episode[ex["episode"]]
        q = story.questions[ex["qi"]]
        read = set(q.supporting[:ex["hop"]])
        pool = [j for j in range(q.heard) if j not in read]
        if ex["episode"] not in keys_of:
            ids = [r.id for r in store.in_episode(ex["episode"])]
            keys_of[ex["episode"]] = (store.vectors(key, ids) if split == "test"
                                      else embed(story.sentences))
        out.append({"task": ex["task"], "hop": ex["hop"], "state": hidden[i],
                    "keys": keys_of[ex["episode"]][pool],
                    "when": np.array([when_heard(j, q.heard, ex["last_read"]) for j in pool],
                                     np.float32),
                    "target": pool.index(ex["target"])})
    return out


def batch(exs: list[dict], idx, mean, std, torch, device):
    n = max(len(exs[i]["keys"]) for i in idx)
    kd = exs[idx[0]]["keys"].shape[1]
    keys = np.zeros((len(idx), n, kd), np.float32)
    when = np.zeros((len(idx), n, 4), np.float32)
    mask = np.zeros((len(idx), n), bool)
    for b, i in enumerate(idx):
        m = len(exs[i]["keys"])
        keys[b, :m], when[b, :m], mask[b, :m] = exs[i]["keys"], exs[i]["when"], True
    state = (np.stack([exs[i]["state"] for i in idx]) - mean) / std
    target = np.array([exs[i]["target"] for i in idx])
    t = lambda a: torch.as_tensor(a, device=device)
    return t(state.astype(np.float32)), t(keys), t(when), t(mask), t(target)


def ranks_of(score_fn, exs: list[dict]) -> np.ndarray:
    """The target's rank under each example's scores, ties by time, earliest first."""
    ranks = []
    for ex in exs:
        s = score_fn(ex)
        order = np.lexsort((np.arange(len(s)), -s))
        ranks.append(int(np.nonzero(order == ex["target"])[0][0]) + 1)
    return np.array(ranks)


def table(exs: list[dict], scorers: dict) -> dict:
    """Per task and hop, recall@1 and @4 of each scorer."""
    ranks = {name: ranks_of(fn, exs) for name, fn in scorers.items()}
    out: dict = {}
    groups = sorted({(ex["task"], ex["hop"]) for ex in exs})
    for t, hop in groups + [(None, None)]:
        idx = np.array([i for i, ex in enumerate(exs) if t is None or (ex["task"], ex["hop"]) == (t, hop)])
        row = {"n": len(idx)}
        for name, r in ranks.items():
            row[f"{name}_recall@1"] = round(float(np.mean(r[idx] <= 1)), 3)
            row[f"{name}_recall@4"] = round(float(np.mean(r[idx] <= 4)), 3)
        out["all" if t is None else f"task{t}-hop{hop + 1}"] = row
    return out


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--features", default="state/features/eval")
    p.add_argument("--store", default="state/store-step1")
    p.add_argument("--walker", default="state/walker-step2/walker-minilm.npz",
                   help="the closed-form walker read beside this one")
    p.add_argument("--key", default="minilm", help="the store embedder whose keys are ranked")
    p.add_argument("--inputs", default="state+key+time",
                   choices=["state+key+time", "state+key", "key+time", "state+time"],
                   help="controls: a part left out is zeroed, and the walker is named for it")
    p.add_argument("--hidden", type=int, default=256)
    p.add_argument("--epochs", type=int, default=30)
    p.add_argument("--lr", type=float, default=1e-3)
    p.add_argument("--weight-decay", type=float, default=1e-2)
    p.add_argument("--batch", type=int, default=64, help="states a step")
    p.add_argument("--checkpoint-minutes", type=float, default=3.0)
    p.add_argument("--device", default="cpu", help="cpu by default: the card may be busy")
    p.add_argument("--out", default="state/walker-step2")
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--note", default="")
    args = p.parse_args()

    import torch

    torch.manual_seed(args.seed)
    feats = Path(args.features)
    about = json.loads((feats / "features.json").read_text(encoding="utf-8"))
    store = Store(args.store)
    by_episode = {"train": {s.episode: s for s in stories(about["tasks"], "train")},
                  "test": {s.episode: s for s in stories(about["tasks"], "test")}}
    embed = make(args.key)
    data = {split: hop_examples(feats, split, by_episode["test" if split == "test" else "train"],
                                store, args.key, embed) for split in SPLITS}
    states = np.stack([ex["state"] for ex in data["fit"]])
    mean, std = states.mean(0), states.std(0) + 1e-6
    key_dim = data["fit"][0]["keys"].shape[1]
    parts = args.inputs.split("+")
    for exs_ in data.values():
        for ex in exs_:
            ex["state_full"] = ex["state"]  # the linear walker beside a control still sees it
            if "time" not in parts:
                ex["when"] = np.zeros_like(ex["when"])
            if "key" not in parts:
                ex["keys"] = np.zeros_like(ex["keys"])
            if "state" not in parts:
                ex["state"] = mean  # standardised, a zero state
    layer = about["walker"]["layer"]
    print({s: len(v) for s, v in data.items()}, "hop states", flush=True)

    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    name = "walker-rank.pt" if args.inputs == "state+key+time" else f"walker-rank-{args.inputs}.pt"
    ckpt_path = out / (name + ".ckpt")
    walker = RankWalker(states.shape[1], key_dim, layer, args.key, args.hidden, mean, std)
    walker.to(args.device)
    opt = torch.optim.AdamW(walker.parameters(), lr=args.lr, weight_decay=args.weight_decay)

    start_epoch, best, history = 0, None, []
    if ckpt_path.exists():
        ck = torch.load(ckpt_path, weights_only=False, map_location=args.device)
        walker.load_state_dict(ck["walker"])
        opt.load_state_dict(ck["opt"])
        start_epoch, best, history = ck["epoch"], ck["best"], ck["history"]
        print(f"resumed at epoch {start_epoch}", flush=True)

    def checkpoint(epoch: int) -> None:
        torch.save({"walker": walker.state_dict(), "opt": opt.state_dict(), "epoch": epoch,
                    "best": best, "history": history}, ckpt_path)

    def rank_scores(w):
        return lambda ex: w.score(ex["state"], ex["keys"], ex["when"])

    def held_recall() -> float:
        return float(np.mean(ranks_of(rank_scores(walker), data["held"]) <= 1))

    exs = data["fit"]
    last_ckpt = time.perf_counter()
    rng = np.random.default_rng(args.seed + start_epoch)
    for epoch in range(start_epoch, args.epochs):
        walker.train(True)
        order = rng.permutation(len(exs))
        total = 0.0
        for at in range(0, len(order), args.batch):
            idx = order[at:at + args.batch]
            state, keys, when, mask, target = batch(exs, idx, mean, std, torch, args.device)
            logits = walker.logits(state, keys, when).masked_fill(~mask, -1e9)
            loss = torch.nn.functional.cross_entropy(logits, target)
            opt.zero_grad()
            loss.backward()
            opt.step()
            total += loss.item() * len(idx)
            if time.perf_counter() - last_ckpt > 60 * args.checkpoint_minutes:
                checkpoint(epoch)
                last_ckpt = time.perf_counter()
        r = held_recall()
        history.append({"epoch": epoch + 1, "loss": round(total / len(exs), 4),
                        "held_recall@1": round(r, 4)})
        print(f"epoch {epoch + 1}: loss {total / len(exs):.4f} held recall@1 {r:.3f}", flush=True)
        if best is None or r > best["held_recall@1"]:
            best = {"epoch": epoch + 1, "held_recall@1": round(r, 4)}
            walker.save(out / name)
        checkpoint(epoch + 1)

    best_walker = RankWalker.load(out / name, args.device)
    linear = LinearHead.load(args.walker)

    def linear_scores(ex):
        q = linear.query(ex["state_full"])
        return ex["keys"] @ (q / max(float(np.linalg.norm(q)), 1e-12))

    scorers = {"rank": rank_scores(best_walker), "linear": linear_scores}
    reading = {
        "kind": "walker-train",
        "step": 2,
        "features": {"path": str(feats), **about},
        "store": {"path": args.store, "key": args.key},
        "linear_walker": args.walker,
        "inputs": args.inputs,
        "hidden": args.hidden,
        "epochs": args.epochs,
        "lr": args.lr,
        "weight_decay": args.weight_decay,
        "batch_states": args.batch,
        "device": args.device,
        "best": best,
        "history": history,
        "teacher_forced": {"test": table(data["test"], scorers),
                           "held": table(data["held"], scorers)},
        "walker_path": str(out / name),
        "command": " ".join(sys.argv),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True,
                              text=True, cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
        "note": args.note,
    }
    print(json.dumps(reading["teacher_forced"]["test"], indent=1))
    path = ROOT / "readings" / f"walker-train-{reading['taken_at']}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    ckpt_path.unlink(missing_ok=True)
    print("wrote", path)


if __name__ == "__main__":
    main()
