"""Step 3, first form: the evaluator, fitted from cached hidden states and read teacher-forced.

The walker finds the right row in its top four nearly always and in first place about half
the time (`walker_fit.py`), and the trunk given exactly the right rows far outscores the trunk
given those rows among others. So the evaluator judges each row the walker proposes: the
trunk reads the question, the rows read so far and one candidate (`judge_prompt`), and its
last-token state is the input.

Two phases, because the states take half an hour on the card and every head is then fitted
from them in seconds. `features` builds the states: for every question in the train split
(fit and held-out stories) and the test stories, one state per hop, teacher-forced, plus the
done state after the last supporting row, where every candidate is wrong and the right call
is to stop; at each, the walker's top-k among the story's rows heard so far (read rows
excluded) and the judge state of each. `fit` is the closed-form head: ridge to +1/-1, layer
and ridge and stop threshold picked on the held-out stories. With `--time` the features also
carry when the candidate was heard relative to the row last read, which the judge prompt
itself does not say: the right second row of a two-fact question is the carrier's latest move
before the hand-over, which no one candidate's text can show.

    .venv/Scripts/python scripts/evaluator_fit.py features --out state/features/eval
    .venv/Scripts/python scripts/evaluator_fit.py fit --features state/features/eval \
        --out state/walker-step2 [--time]

The reading is on the test stories: recall@1 of the re-ranked top-k against the walker's own,
per task and hop, and how often the stop state is recognised. Writes <out>/evaluator.npz and
readings/evaluator-fit-*.json. `features` needs the card to itself.
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

from openplexus.babi import Story, stories  # noqa: E402
from openplexus.heads import LinearHead, hop_prompt, judge_prompt, when_heard  # noqa: E402
from openplexus.store import Store  # noqa: E402

SPLITS = ("fit", "held", "test")


def states(ss: list[Story]) -> list[dict]:
    """One state per hop of each question, plus the done state after the last supporting row;
    `target` is the next supporting row's index, or None when the walk should stop."""
    out = []
    for s in ss:
        for qi, q in enumerate(s.questions):
            for hop in range(len(q.supporting) + 1):
                read = q.supporting[:hop]
                target = q.supporting[hop] if hop < len(q.supporting) else None
                out.append({"story": s, "qi": qi, "hop": hop, "read": read, "target": target,
                            "prompt": hop_prompt(q.text, [s.sentences[i] for i in read])})
    return out


def candidates(walker: LinearHead, hidden: np.ndarray, exs: list[dict], key_of, k: int) -> None:
    """The walker's top-k among the story's rows heard so far, read rows excluded, written
    onto each example as `cands` with `labels`."""
    queries = walker.query(hidden)
    for q, ex in zip(queries, exs):
        s, question = ex["story"], ex["story"].questions[ex["qi"]]
        pool = [i for i in range(question.heard) if i not in ex["read"]]
        if not pool:  # every row heard has been read: nothing to judge, the walk stops
            ex["cands"], ex["labels"] = [], []
            continue
        keys = np.stack([key_of(s, i) for i in pool])
        scores = keys @ (q / max(float(np.linalg.norm(q)), 1e-12))
        order = np.lexsort((np.arange(len(scores)), -scores))[:k]
        ex["cands"] = [pool[i] for i in order]
        ex["labels"] = [1.0 if c == ex["target"] else -1.0 for c in ex["cands"]]


def judge_prompts(exs: list[dict]) -> list[str]:
    return [judge_prompt(ex["story"].questions[ex["qi"]].text,
                         [ex["story"].sentences[i] for i in ex["read"]],
                         ex["story"].sentences[c])
            for ex in exs for c in ex["cands"]]


# --- the cache -----------------------------------------------------------------------------

def save_split(out: Path, split: str, exs: list[dict], hop_hidden: np.ndarray,
               judge_hidden: dict[int, np.ndarray]) -> None:
    """Per example: task, hop, heard, target, last row read, and a JSON of candidates; per
    candidate: example index, row, label; the hop state and each layer's judge states."""
    meta = {
        "examples": [{"task": ex["story"].task, "episode": ex["story"].episode, "qi": ex["qi"],
                      "hop": ex["hop"], "heard": ex["story"].questions[ex["qi"]].heard,
                      "target": -1 if ex["target"] is None else ex["target"],
                      "last_read": ex["read"][-1] if ex["read"] else -1,
                      "cands": ex["cands"], "labels": ex["labels"]} for ex in exs],
    }
    (out / f"{split}.json").write_text(json.dumps(meta), encoding="utf-8")
    arrays = {"hop_hidden": hop_hidden.astype(np.float32),
              "cand_ex": np.array([i for i, ex in enumerate(exs) for _ in ex["cands"]], np.int32),
              "cand_row": np.array([c for ex in exs for c in ex["cands"]], np.int32),
              "cand_label": np.array([lab for ex in exs for lab in ex["labels"]], np.float32)}
    for layer, h in judge_hidden.items():
        arrays[f"judge_{layer}"] = h.astype(np.float32)
    np.savez(out / f"{split}.npz", **arrays)


def judge_states_resumable(trunk, prompts: list[str], layers: list[int], part: Path,
                           chunk: int = 512) -> dict[int, np.ndarray]:
    """The judge states in chunks, each appended to a raw float32 file per layer as it is
    done, so a run killed for the card (it is shared) resumes where it stopped."""
    dim = None
    done = 0
    for layer in layers:
        f = part.with_suffix(f".judge{layer}.f32")
        if f.exists():
            n = f.stat().st_size // 4
            dim = dim or int(trunk.model.config.get_text_config().hidden_size)
            done = n // dim if done == 0 else min(done, n // dim)
    for at in range(done, len(prompts), chunk):
        h = trunk.hidden(prompts[at:at + chunk], layers)
        for layer in layers:
            with open(part.with_suffix(f".judge{layer}.f32"), "ab") as f:
                f.write(h[layer].astype(np.float32).tobytes())
        print(f"  judge states {min(at + chunk, len(prompts))}/{len(prompts)}", flush=True)
    out = {}
    for layer in layers:
        raw = np.fromfile(part.with_suffix(f".judge{layer}.f32"), dtype=np.float32)
        out[layer] = raw.reshape(len(prompts), -1)
    return out


def load_split(path: Path, split: str) -> tuple[list[dict], dict]:
    exs = json.loads((path / f"{split}.json").read_text(encoding="utf-8"))["examples"]
    arrays = dict(np.load(path / f"{split}.npz"))
    return exs, arrays


def time_features(exs: list[dict], arrays: dict) -> np.ndarray:
    return np.array([when_heard(int(row), exs[int(ei)]["heard"], exs[int(ei)]["last_read"])
                     for ei, row in zip(arrays["cand_ex"], arrays["cand_row"])], dtype=np.float32)


INPUTS = {"state": "label", "state+time": "label+time", "time": "time-only"}


def features(exs: list[dict], arrays: dict, layer: int, inputs: str) -> np.ndarray:
    """The judge state, the judge state followed by the when-heard features, or the when-heard
    features alone (the control that shows what the judgement adds over time)."""
    if inputs == "time":
        return time_features(exs, arrays)
    x = arrays[f"judge_{layer}"]
    return np.concatenate([x, time_features(exs, arrays)], axis=1) if inputs == "state+time" else x


def head_name(stem: str, inputs: str, suffix: str) -> str:
    return stem + {"state": "", "state+time": "-time", "time": "-time-only"}[inputs] + suffix


# --- the reading -----------------------------------------------------------------------------

def read(scores: np.ndarray, exs: list[dict], threshold: float | None) -> dict:
    """Per task and hop: among states whose target is in the walker's top-k, how often the
    evaluator's best is the target (recall@1), beside the walker's own first choice; at done
    states, how often no candidate clears the threshold; at other states, how often the best
    does, so a true stop is not an early one."""
    at = 0
    per: dict[str, dict] = {}
    for ex in exs:
        n = len(ex["cands"])
        sc = scores[at:at + n]
        at += n
        t, done = ex["task"], ex["target"] < 0
        name = f"task{t}-done" if done else f"task{t}-hop{ex['hop'] + 1}"
        d = per.setdefault(name, {"n": 0, "in_topk": 0, "eval@1": 0, "walker@1": 0,
                                  "stop": 0, "go": 0})
        d["n"] += 1
        if n == 0:
            d["stop"] += 1  # nothing left to follow
            continue
        best = int(np.argmax(sc))
        if done:
            if threshold is not None and sc[best] <= threshold:
                d["stop"] += 1
            continue
        if threshold is not None and sc[best] > threshold:
            d["go"] += 1
        if ex["target"] in ex["cands"]:
            d["in_topk"] += 1
            d["eval@1"] += int(ex["cands"][best] == ex["target"])
            d["walker@1"] += int(ex["cands"][0] == ex["target"])
    out = {}
    for name, d in sorted(per.items()):
        if name.endswith("done"):
            out[name] = {"n": d["n"], "stopped": round(d["stop"] / d["n"], 3)}
        else:
            out[name] = {"n": d["n"], "target_in_topk": round(d["in_topk"] / d["n"], 3),
                         "eval_recall@1": round(d["eval@1"] / max(1, d["in_topk"]), 3),
                         "walker_recall@1": round(d["walker@1"] / max(1, d["in_topk"]), 3),
                         "went_on": round(d["go"] / d["n"], 3)}
    return out


def overall_recall(r: dict) -> float:
    hits = sum(v["eval_recall@1"] * v["target_in_topk"] * v["n"]
               for k, v in r.items() if not k.endswith("done"))
    n = sum(v["target_in_topk"] * v["n"] for k, v in r.items() if not k.endswith("done"))
    return hits / max(1, n)


def pick_threshold(scores: np.ndarray, labels: np.ndarray) -> float:
    """The score that best separates right candidates from wrong ones on the held-out set, by
    balanced accuracy over a sweep of the scores' range."""
    best = (0.0, 0.0)
    for tau in np.quantile(scores, np.linspace(0.02, 0.98, 97)):
        tpr = float(np.mean(scores[labels > 0] > tau))
        tnr = float(np.mean(scores[labels < 0] <= tau))
        if (tpr + tnr) / 2 > best[0]:
            best = ((tpr + tnr) / 2, float(tau))
    return best[1]


def sha() -> str:
    return subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True,
                          text=True, cwd=ROOT).stdout.strip()


# --- phases ----------------------------------------------------------------------------------

def build_features(args) -> None:
    from openplexus.embed import MiniLM
    from openplexus.trunk import LocalTrunk

    tasks = [int(t) for t in args.tasks.split(",")]
    layers = [int(x) for x in args.layers.split(",")]
    walker = LinearHead.load(args.walker)
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)

    train = stories(tasks, split="train")
    if args.train_stories:
        train = [s for s in train if s.index < args.train_stories]
    parts = {"fit": [s for s in train if s.index < args.holdout],
             "held": [s for s in train if s.index >= args.holdout],
             "test": [s for s in stories(tasks) if s.index < args.test_stories]}
    store = Store(args.store)
    first_id: dict[str, int] = {}
    for r in store.rows:
        first_id.setdefault(r.episode, r.id)

    minilm = MiniLM()
    train_keys = {s.episode: minilm(s.sentences) for s in train}
    minilm.close()

    def train_key(s: Story, i: int) -> np.ndarray:
        return train_keys[s.episode][i]

    def test_key(s: Story, i: int) -> np.ndarray:
        return store.vector("minilm", first_id[s.episode] + i)

    trunk = LocalTrunk(batch=args.batch)
    started = time.perf_counter()
    counts = {}
    for split, ss in parts.items():
        exs = states(ss)
        if (out / f"{split}.npz").exists():
            saved = json.loads((out / f"{split}.json").read_text(encoding="utf-8"))["examples"]
            counts[split] = {"states": len(saved),
                             "judgements": int(sum(len(ex["cands"]) for ex in saved))}
            print(f"{split}: already built", flush=True)
            continue
        hop_h = trunk.hidden([ex["prompt"] for ex in exs], [walker.layer])[walker.layer]
        candidates(walker, hop_h, exs, test_key if split == "test" else train_key, args.k)
        counts[split] = {"states": len(exs),
                         "judgements": int(sum(len(ex["cands"]) for ex in exs))}
        print(f"{split}: {len(exs)} hop states in {time.perf_counter() - started:.0f}s", flush=True)
        judge_h = judge_states_resumable(trunk, judge_prompts(exs), layers, out / f"{split}.part")
        save_split(out, split, exs, hop_h, judge_h)
        for layer in layers:
            (out / f"{split}.part").with_suffix(f".judge{layer}.f32").unlink()
        print(f"{split}: {counts[split]} in {time.perf_counter() - started:.0f}s so far",
              flush=True)
    trunk.close()
    (out / "features.json").write_text(json.dumps({
        "trunk": trunk.served_model(), "walker": {"path": args.walker, "layer": walker.layer,
                                                   "lam": walker.lam},
        "k": args.k, "tasks": tasks, "layers": layers, "train_stories_fit": args.holdout,
        "counts": counts, "seconds": round(time.perf_counter() - started, 1),
        "judge_prompt": judge_prompt("<question>", ["<row read>"], "<candidate>"),
        "command": " ".join(sys.argv), "sha": sha(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
    }, indent=1), encoding="utf-8")
    print("wrote", out)


def fit(args) -> None:
    feats = Path(args.features)
    about = json.loads((feats / "features.json").read_text(encoding="utf-8"))
    layers = [int(x) for x in args.layers.split(",")] if args.layers else about["layers"]
    lams = [float(x) for x in args.lams.split(",")]
    data = {split: load_split(feats, split) for split in SPLITS}

    def xy(split: str, layer: int):
        exs, arrays = data[split]
        return features(exs, arrays, layer, args.inputs), arrays["cand_label"]

    sweep, best = [], None
    for layer in layers:
        x_fit, y_fit = xy("fit", layer)
        x_held, y_held = xy("held", layer)
        mean, std = x_fit.mean(0), x_fit.std(0) + 1e-6
        for lam in lams:
            head = LinearHead.fit((x_fit - mean) / std, y_fit[:, None], lam, layer, "label")
            sc = head.query((x_held - mean) / std)[:, 0]
            tau = pick_threshold(sc, y_held)
            score = overall_recall(read(sc, data["held"][0], tau))
            sweep.append({"layer": layer, "lam": lam, "threshold": round(tau, 4),
                          "held_recall@1": round(score, 3)})
            print(f"layer {layer} lam {lam}: held recall@1 {score:.3f} tau {tau:.3f}", flush=True)
            if best is None or score > best[0]:
                best = (score, layer, lam, tau)
    _, layer, lam, tau = best
    x_fit, y_fit = xy("fit", layer)
    x_held, y_held = xy("held", layer)
    x_test, _ = xy("test", layer)
    x_all = np.concatenate([x_fit, x_held])
    mean, std = x_all.mean(0), x_all.std(0) + 1e-6
    evaluator = LinearHead.fit((x_all - mean) / std, np.concatenate([y_fit, y_held])[:, None],
                               lam, layer, INPUTS[args.inputs])
    evaluator.threshold = tau
    evaluator.mean, evaluator.std = mean, std
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    name = head_name("evaluator", args.inputs, ".npz")
    evaluator.save(out / name)
    held_mean, held_std = x_fit.mean(0), x_fit.std(0) + 1e-6
    held_head = LinearHead.fit((x_fit - held_mean) / held_std, y_fit[:, None], lam, layer,
                               "label")

    reading = {
        "kind": "evaluator-fit",
        "step": 3,
        "features": {"path": str(feats), **about},
        "inputs": args.inputs,
        "chosen": {"layer": layer, "lam": lam, "threshold": round(tau, 4)},
        "sweep": sweep,
        "teacher_forced": {
            "test": read(evaluator.query(x_test)[:, 0], data["test"][0], tau),
            "held": read(held_head.query((x_held - held_mean) / held_std)[:, 0],
                         data["held"][0], tau),
        },
        "evaluator_path": str(out / name),
        "command": " ".join(sys.argv),
        "sha": sha(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
        "note": args.note,
    }
    print(json.dumps(reading["teacher_forced"]["test"], indent=1))
    path = ROOT / "readings" / f"evaluator-fit-{reading['taken_at']}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print("wrote", path)


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("phase", choices=["features", "fit"])
    p.add_argument("--tasks", default="1,2,3")
    p.add_argument("--layers", default=None, help="features: layers to cache (default 12,16,20); "
                                                  "fit: layers to sweep (default: all cached)")
    p.add_argument("--lams", default="1,10,100,1000")
    p.add_argument("--k", type=int, default=4)
    p.add_argument("--holdout", type=int, default=160)
    p.add_argument("--train-stories", type=int, default=None,
                   help="cap on train stories a task; a small cap smokes the whole script in a "
                        "minute before a long run")
    p.add_argument("--walker", default="state/walker-step2/walker-minilm.npz")
    p.add_argument("--features", default="state/features/eval")
    p.add_argument("--out", default=None)
    p.add_argument("--store", default="state/store-step1")
    p.add_argument("--test-stories", type=int, default=100)
    p.add_argument("--batch", type=int, default=8)
    p.add_argument("--inputs", default="state", choices=sorted(INPUTS),
                   help="fit: the judge state, the state plus when-heard, or when-heard alone")
    p.add_argument("--note", default="")
    args = p.parse_args()
    if args.phase == "features":
        args.layers = args.layers or "12,16,20"
        args.out = args.out or args.features
        build_features(args)
    else:
        args.out = args.out or "state/walker-step2"
        fit(args)


if __name__ == "__main__":
    main()
