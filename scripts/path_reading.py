"""Step 3's first form: a trained evaluator with search, against the trunk's own chain of
thought, on bAbI task 19 (path finding), the planning task in miniature.

A path is two moves. The search proposes the four moves at each step, the evaluator (a head
over the trunk's hidden state after reading the story, the question, the moves so far and
one candidate move) scores each and says when the path is done, and the search follows the
best, greedily or as a beam. Its budget is forward passes of the trunk: four prefill passes a
step. The chain of thought is the same trunk thinking in tokens, each token a decode pass.

Two phases, because the states take a while on the card and the heads fit in seconds:

    # the judge states, cached in evaluator_fit's format so its fit phase and head_train.py
    # fit the linear and the gradient head from them (--inputs state; there is no time here)
    .venv/Scripts/python scripts/path_reading.py features --features state/features/path
    .venv/Scripts/python scripts/evaluator_fit.py fit --features state/features/path --out state/path-step3
    .venv/Scripts/python scripts/head_train.py --features state/features/path --out state/path-step3

    # the arms. floor, ceiling and cot:<token budget> go through llama-server. For cot the
    # server is launched with `--reasoning-budget <budget>` in place of `--reasoning-budget 0`:
    # left to itself the 2B loops in its thinking and has not answered by 2048 tokens, so the
    # server cuts the thinking at the budget and the model then answers (one server launch a
    # budget; floor and ceiling still send enable_thinking false). The reading shows the budget
    # held: passes a question a few over the budget, no empty replies. The search arms need the
    # local trunk and an evaluator, so they run in their own invocation.
    .venv/Scripts/python scripts/path_reading.py ask --arms floor,ceiling,cot:512   # server --reasoning-budget 512
    .venv/Scripts/python scripts/path_reading.py ask --arms cot:2048                # server --reasoning-budget 2048

    # the edge form: the trunk restates each sentence as room|direction|room and verifies the
    # restatement against the sentence, two passes a sentence through llama-server with
    # thinking off, and code builds the map and walks it. edges:regex reads the edges with a
    # regular expression instead, the tool's own ceiling.
    .venv/Scripts/python scripts/path_reading.py ask --arms edges:regex,edges:trunk
    .venv/Scripts/python scripts/path_reading.py ask --arms search:greedy,search:beam:2 \
        --evaluator state/path-step3/evaluator-mlp.pt

Writes readings/path-*.json, every arm with its score and its forward passes a question.
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

from evaluator_fit import judge_states_resumable  # noqa: E402
from openplexus.babi import Story, stories  # noqa: E402
from openplexus.heads import load_head  # noqa: E402
from openplexus.paths import (EDGE_SYSTEM, LETTERS, VERIFY_SYSTEM, edge_messages, edge_of,  # noqa: E402
                              moves_in, parse_question, path_correct, path_messages,
                              shortest_path, step_prompt, verify_messages)

TASK = 19


def sha() -> str:
    return subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True,
                          cwd=ROOT).stdout.strip()


# --- features ---------------------------------------------------------------------------------

def states(ss: list[Story]) -> list[dict]:
    """Per question, the teacher-forced states: no move yet, the gold first move made, both made
    (done); at each, the four moves as candidates, the gold next move the target, or none."""
    out = []
    for s in ss:
        q = s.questions[0]
        gold = moves_in(q.answer)
        for hop in range(len(gold) + 1):
            target = LETTERS.index(gold[hop]) if hop < len(gold) else -1
            out.append({"story": s, "qi": 0, "hop": hop, "steps": gold[:hop], "target": target,
                        "cands": list(range(len(LETTERS))),
                        "labels": [1.0 if c == target else -1.0 for c in range(len(LETTERS))]})
    return out


def judge_prompts(exs: list[dict]) -> list[str]:
    return [step_prompt(ex["story"].sentences, ex["story"].questions[0].text, ex["steps"], LETTERS[c])
            for ex in exs for c in ex["cands"]]


def save_split(out: Path, split: str, exs: list[dict], judge_hidden: dict[int, np.ndarray]) -> None:
    """evaluator_fit's cache format: `heard` and `last_read` are filled so its loader is happy,
    but there is no time here and the heads are fitted with --inputs state."""
    meta = {"examples": [{"task": TASK, "episode": ex["story"].episode, "qi": 0, "hop": ex["hop"],
                          "heard": len(ex["story"].sentences), "target": ex["target"],
                          "last_read": -1, "cands": ex["cands"], "labels": ex["labels"]}
                         for ex in exs]}
    (out / f"{split}.json").write_text(json.dumps(meta), encoding="utf-8")
    arrays = {"hop_hidden": np.zeros((len(exs), 0), np.float32),
              "cand_ex": np.array([i for i, ex in enumerate(exs) for _ in ex["cands"]], np.int32),
              "cand_row": np.array([c for ex in exs for c in ex["cands"]], np.int32),
              "cand_label": np.array([lab for ex in exs for lab in ex["labels"]], np.float32)}
    for layer, h in judge_hidden.items():
        arrays[f"judge_{layer}"] = h.astype(np.float32)
    np.savez(out / f"{split}.npz", **arrays)


def build_features(args) -> None:
    from openplexus.trunk import LocalTrunk

    layers = [int(x) for x in args.layers.split(",")]
    out = Path(args.features)
    out.mkdir(parents=True, exist_ok=True)
    train = stories([TASK], split="train")
    if args.train_stories:
        train = [s for s in train if s.index < args.train_stories]
    parts = {"fit": [s for s in train if s.index < args.holdout],
             "held": [s for s in train if s.index >= args.holdout],
             "test": [s for s in stories([TASK]) if s.index < args.test_stories]}
    trunk = LocalTrunk(batch=args.batch)
    started = time.perf_counter()
    counts = {}
    for split, ss in parts.items():
        exs = states(ss)
        counts[split] = {"states": len(exs), "judgements": sum(len(ex["cands"]) for ex in exs)}
        if (out / f"{split}.npz").exists():
            print(f"{split}: already built", flush=True)
            continue
        judge_h = judge_states_resumable(trunk, judge_prompts(exs), layers, out / f"{split}.part")
        save_split(out, split, exs, judge_h)
        for layer in layers:
            (out / f"{split}.part").with_suffix(f".judge{layer}.f32").unlink()
        print(f"{split}: {counts[split]} in {time.perf_counter() - started:.0f}s so far", flush=True)
    trunk.close()
    about = {
        "trunk": f"{trunk.model_id} fp32 in process",
        "walker": {"path": None, "layer": None, "lam": None, "note": "no walker: the four moves are the candidates"},
        "k": len(LETTERS), "tasks": [TASK], "layers": layers, "train_stories_fit": args.holdout,
        "counts": counts, "seconds": round(time.perf_counter() - started, 1),
        "judge_prompt": step_prompt(["<story>"], "<question>", ["n"], "e"),
        "command": " ".join(sys.argv), "sha": sha(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
    }
    (out / "features.json").write_text(json.dumps(about, indent=1), encoding="utf-8")
    print("wrote", out)


# --- the arms ---------------------------------------------------------------------------------

def served_arm(kind: str, budget: int | None, ss: list[Story], args) -> dict:
    """floor (no story), ceiling (the story, one pass), oracle (the two supporting sentences
    alone, one pass: step 2's chain handed over, were a walker to find them) and cot (the story,
    thinking on, the server cutting the thinking at `budget` tokens, then the answer), through
    llama-server."""
    from openplexus.trunk import ServedTrunk

    think = kind == "cot"
    trunk = ServedTrunk(url=args.url, max_new=budget + 32 if think else args.max_new,
                        parallel=args.parallel, think=think)
    def shown(s: Story) -> list[str]:
        if kind == "floor":
            return []
        if kind == "oracle":
            return [s.sentences[i] for i in s.questions[0].supporting]
        return s.sentences

    prompts = [path_messages(shown(s), s.questions[0].text) for s in ss]
    started = time.perf_counter()
    replies: list[str] = []
    chunk = args.parallel * 4
    for at in range(0, len(prompts), chunk):
        replies.extend(trunk.ask_many(prompts[at:at + chunk]))
        done = len(replies)
        acc = sum(path_correct(s.questions[0].answer, r) for s, r in zip(ss[:done], replies)) / done
        print(f"{kind}{':' + str(budget) if budget else ''} {done}/{len(prompts)} acc={acc:.3f}",
              flush=True)
    seconds = time.perf_counter() - started
    rows = [{"episode": s.episode, "question": s.questions[0].text, "answer": s.questions[0].answer,
             "said": r, "correct": path_correct(s.questions[0].answer, r)} for s, r in zip(ss, replies)]
    return {
        "kind": kind, "think": think, "budget": budget, "max_new": trunk.max_new,
        "served_model": trunk.served_model(),
        "score": round(sum(r["correct"] for r in rows) / len(rows), 3), "n": len(rows),
        "passes_per_question": round(trunk.tokens_out / len(rows), 1),
        "tokens_in_per_question": round(trunk.tokens_in / len(rows), 1),
        "empty_replies": sum(not r["said"] for r in rows),
        "seconds": round(seconds, 1), "rows": rows,
    }


def edges_arm(reader: str, ss: list[Story], args) -> dict:
    """The trunk never composes: each sentence becomes one edge, by the trunk's restatement
    verified against the sentence (`trunk`, two passes a sentence, thinking off) or by a
    regular expression (`regex`, the tool's ceiling), and code walks the shortest path from
    the start room to the goal. The restatement names the two rooms and the direction right
    and swaps the rooms' order one time in eight; asked yes or no whether the first room lies
    in that direction of the second, the trunk catches every swap. The deployed system has
    every input here: the story and the question, nothing from the gold."""
    from openplexus.trunk import ServedTrunk

    started = time.perf_counter()
    sentences = [x for s in ss for x in s.sentences]
    asked = 0
    fixed = 0
    if reader == "trunk":
        trunk = ServedTrunk(url=args.url, max_new=args.max_new, parallel=args.parallel, think=False)
        lines = []
        chunk = args.parallel * 8
        for at in range(0, len(sentences), chunk):
            lines.extend(trunk.ask_many([edge_messages(x) for x in sentences[at:at + chunk]]))
            print(f"edges:trunk {len(lines)}/{len(sentences)} sentences restated", flush=True)
        asked = len(sentences)
        edges = [edge_of(x) for x in lines]
        todo = [i for i, e in enumerate(edges) if e]
        answers = []
        for at in range(0, len(todo), chunk):
            answers.extend(trunk.ask_many([verify_messages(sentences[i], edges[i])
                                           for i in todo[at:at + chunk]]))
            print(f"edges:trunk {len(answers)}/{len(todo)} restatements verified", flush=True)
        asked += len(todo)
        for i, said in zip(todo, answers):
            a, d, b = edges[i]
            if said.strip().strip(".").lower().startswith("no"):
                lines[i] = f"{b}|{d}|{a}"
                fixed += 1
        served, tokens_in, tokens_out = trunk.served_model(), trunk.tokens_in, trunk.tokens_out
    else:
        lines, served, tokens_in, tokens_out = sentences, None, 0, 0
    rows = []
    at = 0
    for s in ss:
        q = s.questions[0]
        said_lines = lines[at:at + len(s.sentences)]
        at += len(s.sentences)
        edges = [e for e in (edge_of(x) for x in said_lines) if e]
        true_edges = [e for e in (edge_of(x) for x in s.sentences) if e]
        ends = parse_question(q.text)
        path = shortest_path(edges, *ends) if ends else None
        said = ",".join(path) if path else ""
        rows.append({"episode": s.episode, "question": q.text, "answer": q.answer, "said": said,
                     "correct": path_correct(q.answer, said), "edges_read": len(edges),
                     "edges_right": sum(e in true_edges for e in edges),
                     "restated": said_lines if reader != "regex" else None})
    n = len(rows)
    return {
        "kind": "edges", "reader": reader, "think": False, "served_model": served,
        "edge_prompt": EDGE_SYSTEM if reader == "trunk" else None,
        "verify_prompt": VERIFY_SYSTEM if reader == "trunk" else None,
        "orders_swapped_by_verification": fixed if reader == "trunk" else None,
        "inputs_the_deployed_system_has": "all: the story's sentences and the question",
        "score": round(sum(r["correct"] for r in rows) / n, 3), "n": n,
        "no_path": sum(not r["said"] for r in rows),
        "edges_right_per_story": round(sum(r["edges_right"] for r in rows) / n, 2),
        "queries_per_question": round(asked / n, 1),
        "passes_per_question": round((tokens_out + asked) / n, 1),
        "tokens_in_per_question": round(tokens_in / n, 1),
        "seconds": round(time.perf_counter() - started, 1), "rows": rows,
    }


def search_arm(mode: str, beam: int, ss: list[Story], trunk, evaluator, args) -> dict:
    """The search: at each step the four moves are judged by the evaluator from the trunk's
    state, and the best is followed (greedy, a path ends when stop wins at its node) or the
    `beam` best partial paths are kept, each with its log-probability over moves and stop, and
    a stopped copy of every partial is set aside. Paths of different lengths are compared by
    their mean log-probability a decision, the stop a decision too: by the total, a path that
    stopped at once beats any longer one whenever the evaluator is unsure, which is what the
    first beam reading showed (0.0, every path empty). Four prefill passes a step a partial
    path; up to three steps."""
    started = time.perf_counter()
    live = [[([], 0.0)] for _ in ss]  # per question: live partial paths (moves, logp)
    finished: list[list[tuple[list[str], float]]] = [[] for _ in ss]
    passes = np.zeros(len(ss), int)
    for depth in range(args.max_steps):
        jobs = [(qi, p) for qi, partials in enumerate(live) for p in partials]
        if not jobs:
            break
        prompts = [step_prompt(ss[qi].sentences, ss[qi].questions[0].text, moves, c)
                   for qi, (moves, _) in jobs for c in LETTERS]
        h = trunk.hidden(prompts, [evaluator.layer])[evaluator.layer]
        scores = (evaluator.query(h)[:, 0] - evaluator.threshold).reshape(len(jobs), len(LETTERS))
        new_live: list[list[tuple[list[str], float]]] = [[] for _ in ss]
        for (qi, (moves, logp)), sc in zip(jobs, scores):
            passes[qi] += len(LETTERS)
            options = np.append(sc, 0.0)  # the four moves and stop, stop at 0
            logps = options - np.logaddexp.reduce(options)
            if mode == "greedy":
                best = int(np.argmax(options))
                if best == len(LETTERS):
                    finished[qi].append((moves, logp + logps[best]))
                else:
                    new_live[qi].append((moves + [LETTERS[best]], logp + logps[best]))
                continue
            finished[qi].append((moves, logp + logps[-1]))
            for c, lp in zip(LETTERS, logps[:-1]):
                new_live[qi].append((moves + [c], logp + lp))
        for qi in range(len(ss)):
            new_live[qi].sort(key=lambda p: -p[1])
            new_live[qi] = new_live[qi][:beam]
        live = new_live
        print(f"search:{mode}{':' + str(beam) if mode == 'beam' else ''} step {depth + 1}: "
              f"{sum(len(p) for p in live)} partial paths live", flush=True)
    rows = []
    for qi, s in enumerate(ss):
        # finished paths made len(moves) + 1 decisions, the stop among them; live ones len(moves)
        pool = ([(m, lp / (len(m) + 1)) for m, lp in finished[qi]]
                + [(m, lp / max(len(m), 1)) for m, lp in live[qi]])
        moves, _ = max(pool, key=lambda p: p[1])
        said = ",".join(moves)
        rows.append({"episode": s.episode, "question": s.questions[0].text,
                     "answer": s.questions[0].answer, "said": said,
                     "correct": path_correct(s.questions[0].answer, said), "passes": int(passes[qi])})
    return {
        "kind": "search", "mode": mode, "beam": beam if mode == "beam" else 1,
        "max_steps": args.max_steps, "evaluator": args.evaluator,
        "evaluator_layer": evaluator.layer, "evaluator_key": evaluator.key,
        "evaluator_threshold": evaluator.threshold,
        "score": round(sum(r["correct"] for r in rows) / len(rows), 3), "n": len(rows),
        "passes_per_question": round(float(passes.mean()), 1),
        "moves_per_question": round(float(np.mean([len(moves_in(r["said"])) for r in rows])), 2),
        "seconds": round(time.perf_counter() - started, 1), "rows": rows,
    }


def ask(args) -> None:
    ss = [s for s in stories([TASK]) if s.index < args.stories]
    specs = args.arms.split(",")
    reading = {"kind": "path", "step": 3, "task": TASK, "stories": len(ss),
               "scorer": "ordered moves, letters or words (openplexus.paths.path_correct)",
               "arms": {}}
    local = None
    evaluator = None
    for spec in specs:
        parts = spec.split(":")
        if parts[0] in ("floor", "ceiling", "oracle", "cot"):
            budget = int(parts[1]) if len(parts) > 1 else None
            arm = served_arm(parts[0], budget, ss, args)
        elif parts[0] == "edges":
            arm = edges_arm(parts[1], ss, args)
        elif parts[0] == "search":
            if local is None:
                from openplexus.trunk import LocalTrunk

                local = LocalTrunk(batch=args.batch)
                evaluator = load_head(args.evaluator)
            mode = parts[1]
            beam = int(parts[2]) if len(parts) > 2 else 1
            arm = search_arm(mode, beam, ss, local, evaluator, args)
        else:
            raise SystemExit(f"unknown arm {spec}")
        reading["arms"][spec] = arm
        print(f"== {spec}: {arm['score']} in {arm['seconds']}s, "
              f"{arm['passes_per_question']} passes a question", flush=True)
    if local is not None:
        local.close()
    reading.update({"command": " ".join(sys.argv), "sha": sha(),
                    "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
                    "note": args.note})
    path = ROOT / "readings" / f"path-{reading['taken_at']}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print("wrote", path)


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("phase", choices=["features", "ask"])
    p.add_argument("--features", default="state/features/path")
    p.add_argument("--layers", default="12,16,20")
    p.add_argument("--holdout", type=int, default=800, help="features: train stories from here are held out")
    p.add_argument("--train-stories", type=int, default=None, help="features: cap, for a smoke")
    p.add_argument("--test-stories", type=int, default=500, help="features: test stories cached")
    p.add_argument("--batch", type=int, default=8)
    p.add_argument("--arms", default="floor,ceiling")
    p.add_argument("--stories", type=int, default=500, help="ask: test stories read")
    p.add_argument("--url", default=None, help="llama-server, default LOCAL_SERVER or 127.0.0.1:8093")
    p.add_argument("--max-new", type=int, default=12, help="tokens for the one-pass arms")
    p.add_argument("--parallel", type=int, default=8)
    p.add_argument("--evaluator", default="state/path-step3/evaluator-mlp.pt")
    p.add_argument("--max-steps", type=int, default=3)
    p.add_argument("--note", default="")
    args = p.parse_args()
    if args.phase == "features":
        build_features(args)
    else:
        ask(args)


if __name__ == "__main__":
    main()
