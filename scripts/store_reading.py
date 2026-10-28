"""Step 1: the store, with no training.

Two phases, because the embedders and the served trunk cannot share the card at once, and
because the gap between them is the restart the step asks for: `hear` appends every sentence
of N stories per task to a store on disk, one vector per embedder, and `ask` opens that store
in a new process and asks each question as of the moment it was asked, with the top-k rows of
its episode placed in the trunk's context.

    .venv/Scripts/python scripts/store_reading.py hear --tasks 1,2,3 --stories 100 \
        --embedders minilm,trunk --store state/store-step1
    .venv/Scripts/python scripts/store_reading.py ask --tasks 1,2,3 --stories 100 \
        --store state/store-step1 --arms floor,ceiling,oracle,store:minilm:4,random:4

Arms: `floor` (no story), `ceiling` (every sentence heard so far), `oracle` (only the
supporting sentences: the trunk given exactly the right rows, which separates a retrieval
failure from a reading failure), `store:<embedder>:<k>` (top-k by cosine within the episode,
as of the question), `unscoped:<embedder>:<k>` (top-k over everything heard so far in any
episode, where bAbI's four names clash across stories), and `random:<k>` (k rows of the
episode drawn at random: the store arm with its retrieval disabled, which must read lower).
Step 2's untrained hops are arms of the same reading: `hops:<embedder>:<k>:<depth>` takes
the top-k by the question, then for each further hop re-queries with the stored vector of the
best row not yet followed and adds its top-k, so the trunk sees up to k*depth rows;
`hops-random:<embedder>:<k>:<depth>` follows a random row of the episode instead, which is
the hop with its choice disabled, and `store:<embedder>:<k*depth>` is the same number of rows
with no hop at all. `walker:<k>:<depth>` is the trained hop: the query at each hop is the
walker's map of the trunk's hidden state after reading the question and the rows followed so
far (`walker_fit.py`), which needs `--trunk local`, the trunk in PyTorch. Rows always stand
in the story in the order they were heard. `select:<k>:<depth>` adds the evaluator
(`evaluator_fit.py`, `--evaluator`): at each hop the walker's top-k are judged, the best is
followed if its score clears the evaluator's threshold and the walk stops otherwise, and the
trunk sees only the rows followed. A hop or walker arm with `:f` appended shows the
trunk only the rows it followed, one a hop, instead of everything it found: step 2's first
reading showed the trunk given the right two rows among five others scores 0.48 against 0.77
given the two alone, so what is left out matters as much as what is found. Writes
readings/store-*.json.
"""

from __future__ import annotations

import argparse
import json
import random
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
sys.path.insert(0, str(ROOT / "scripts"))

from ceiling import correct  # noqa: E402
from openplexus.babi import Story, stories  # noqa: E402
from openplexus.store import Hit, Store  # noqa: E402
from openplexus.trunk import LocalTrunk, ServedTrunk, question_messages  # noqa: E402
from openplexus.heads import (LinearHead, hop_prompt, judge_prompt, load_head,  # noqa: E402
                              when_heard)


def chosen(args) -> list[Story]:
    tasks = [int(t) for t in args.tasks.split(",")]
    return [s for s in stories(tasks) if s.index < args.stories]


def query_key(s: Story, i: int) -> str:
    return f"{s.episode}q{i}"


def hear(args) -> None:
    from openplexus import embed

    chosen_stories = chosen(args)
    store = Store(args.store)
    if len(store):
        raise SystemExit(f"{args.store} already holds {len(store)} rows; hear into a fresh one")
    sentences = [t for s in chosen_stories for t in s.sentences]
    questions = [(query_key(s, i), q.text)
                 for s in chosen_stories for i, q in enumerate(s.questions)]
    vectors: dict[str, np.ndarray] = {}
    stats: dict = {"rows": len(sentences), "embedders": {}}
    for name in args.embedders.split(","):
        e = embed.make(name)
        e(sentences[:8])  # warm the kernels so the timing is the embedder's, not the load's
        started = time.perf_counter()
        vectors[e.name] = e(sentences)
        seconds = time.perf_counter() - started
        qv = e(list(q for _, q in questions))
        np.save(Path(args.store) / f"queries-{e.name}.npy", qv)
        stats["embedders"][e.name] = {
            "dim": e.dim, "ms_per_row": round(1000 * seconds / len(sentences), 3),
            "rows_per_second": round(len(sentences) / seconds, 1),
        }
        print(f"{e.name}: {len(sentences)} rows in {seconds:.1f}s", flush=True)
        e.close()
    (Path(args.store) / "queries.json").write_text(json.dumps([k for k, _ in questions]),
                                                   encoding="utf-8")
    started = time.perf_counter()
    at = 0
    for s in chosen_stories:
        n = len(s.sentences)
        store.append(s.sentences, s.episode, {k: v[at:at + n] for k, v in vectors.items()})
        at += n
    stats["append_seconds"] = round(time.perf_counter() - started, 2)
    stats["bytes_on_disk"] = store.bytes_on_disk()
    stats["bytes_per_row"] = round(store.bytes_on_disk() / len(store), 1)
    stats["command"] = " ".join(sys.argv)
    (Path(args.store) / "hear.json").write_text(json.dumps(stats, indent=1), encoding="utf-8")
    print(json.dumps(stats, indent=1))


class Arm:
    trunk = None  # a LocalTrunk, for the walker arm's hidden states
    walker: LinearHead | None = None
    evaluator = None  # a LinearHead or an MLPHead

    def __init__(self, spec: str):
        parts = spec.split(":")
        self.spec = spec
        self.followed_only = parts[-1] == "f"
        if self.followed_only:
            parts = parts[:-1]
        self.kind = parts[0]
        keyed = self.kind in ("store", "unscoped", "hops", "hops-random")
        self.embedder = parts[1] if keyed else None
        self.k = int(parts[2]) if keyed else (
            int(parts[1]) if self.kind in ("random", "walker", "select") else None)
        self.depth = int(parts[3]) if self.kind in ("hops", "hops-random") else (
            int(parts[2]) if self.kind in ("walker", "select") else 1)

    def walk(self, store: Store, s: Story, qi: int, before: int) -> tuple[list, float]:
        """The trained hop: at each hop the trunk reads the question and the rows followed so
        far, the walker maps its state to a key, and the best unfollowed row found is read."""
        q = s.questions[qi]
        followed: list = []
        found: dict[int, Hit] = {}
        started = time.perf_counter()
        for _ in range(self.depth):
            h = self.trunk.hidden([hop_prompt(q.text, [r.text for r in followed])],
                                  [self.walker.layer])[self.walker.layer][0]
            for hit in store.search(self.walker.key, self.walker.query(h), self.k,
                                    episode=s.episode, before=before):
                if hit.row.id not in found:
                    found[hit.row.id] = hit
            candidates = [h for h in found.values() if h.row not in followed]
            if not candidates:
                break
            followed.append(max(candidates, key=lambda h: (h.score, -h.row.id)).row)
        ms = 1000 * (time.perf_counter() - started)
        shown = followed if self.followed_only else [h.row for h in found.values()]
        return sorted(shown, key=lambda r: r.id), ms

    def select(self, store: Store, s: Story, qi: int, before: int,
               first_id: int) -> tuple[list, float]:
        """The walker proposes its top-k, the evaluator judges each given what was read, the
        best is followed if judged good enough, else the walk stops. The trunk sees only the
        rows followed; if the walk stops at once, the best-judged candidate alone."""
        q = s.questions[qi]
        followed: list = []
        fallback = None
        started = time.perf_counter()
        for _ in range(self.depth):
            read = [r.text for r in followed]
            h = self.trunk.hidden([hop_prompt(q.text, read)],
                                  [self.walker.layer])[self.walker.layer][0]
            hits = [h_ for h_ in store.search(self.walker.key, self.walker.query(h), self.k
                                              + len(followed), episode=s.episode, before=before)
                    if h_.row not in followed][:self.k]
            if not hits:
                break
            states = self.trunk.hidden([judge_prompt(q.text, read, h_.row.text) for h_ in hits],
                                       [self.evaluator.layer])[self.evaluator.layer]
            if self.evaluator.key == "time-only":
                raise SystemExit("a time-only evaluator is a control, not a head to read with")
            if self.evaluator.key == "label+time":
                heard = before - first_id
                last = followed[-1].id - first_id if followed else -1
                when = np.array([when_heard(h_.row.id - first_id, heard, last) for h_ in hits],
                                dtype=np.float32)
                states = np.concatenate([states, when], axis=1)
            scores = self.evaluator.query(states)[:, 0]
            best = int(np.argmax(scores))
            if fallback is None:
                fallback = hits[best].row
            if scores[best] <= self.evaluator.threshold:
                break
            followed.append(hits[best].row)
        ms = 1000 * (time.perf_counter() - started)
        shown = followed or ([fallback] if fallback else [])
        return sorted(shown, key=lambda r: r.id), ms

    def rows_for(self, store: Store, s: Story, qi: int, first_id: int, queries,
                 rng) -> tuple[list, float]:
        """The rows to place in front of the trunk, in time order, and the lookup's ms."""
        q = s.questions[qi]
        before = first_id + q.heard
        if self.kind == "floor":
            return [], 0.0
        if self.kind == "ceiling":
            return store.in_episode(s.episode, before), 0.0
        if self.kind == "oracle":
            rows = store.in_episode(s.episode, before)
            return [rows[i] for i in sorted(q.supporting)], 0.0
        if self.kind == "walker":
            return self.walk(store, s, qi, before)
        if self.kind == "select":
            return self.select(store, s, qi, before, first_id)
        if self.kind == "random":
            rows = store.in_episode(s.episode, before)
            picked = rng.sample(rows, min(self.k, len(rows)))
            return sorted(picked, key=lambda r: r.id), 0.0
        vec = queries[self.embedder][query_key(s, qi)]
        started = time.perf_counter()
        hits = store.search(self.embedder, vec, self.k,
                            episode=s.episode if self.kind != "unscoped" else None, before=before)
        found = {h.row.id: h for h in hits}
        followed: list = []

        def best_unfollowed():
            # best meaning highest score at the hop that found it
            candidates = [h for h in found.values() if h.row not in followed]
            return max(candidates, key=lambda h: (h.score, -h.row.id)).row if candidates else None

        for hop in range(self.depth):
            if self.kind == "hops-random":
                pool = [r for r in store.in_episode(s.episode, before) if r not in followed]
                next_row = rng.choice(pool) if pool else None
            else:
                next_row = best_unfollowed()
            if next_row is None:
                break
            followed.append(next_row)
            if next_row.id not in found:
                found[next_row.id] = Hit(next_row, 0.0)
            if hop == self.depth - 1:
                break  # the last row read is not re-queried from
            # the row's own stored vector is the re-query
            for h in store.search(self.embedder, store.vector(self.embedder, next_row.id),
                                  self.k + 1, episode=s.episode, before=before):
                if h.row.id != next_row.id and h.row.id not in found:
                    found[h.row.id] = h
        ms = 1000 * (time.perf_counter() - started)
        shown = followed if self.followed_only else [h.row for h in found.values()]
        return sorted(shown, key=lambda r: r.id), ms


def ask(args) -> None:
    chosen_stories = chosen(args)
    store = Store(args.store)
    hear_stats = json.loads((Path(args.store) / "hear.json").read_text(encoding="utf-8"))
    keys = json.loads((Path(args.store) / "queries.json").read_text(encoding="utf-8"))
    queries = {name: dict(zip(keys, np.load(Path(args.store) / f"queries-{name}.npy")))
               for name in store.embedders}
    first_id = {}
    for r in store.rows:
        first_id.setdefault(r.episode, r.id)
    for s in chosen_stories:
        rows = store.in_episode(s.episode)
        if [r.text for r in rows] != s.sentences:
            raise SystemExit(f"{s.episode}: the store's rows are not the story's sentences")
    if args.trunk == "local":
        trunk = LocalTrunk(batch=args.parallel, max_new=args.max_new)
        Arm.trunk = trunk
    else:
        trunk = ServedTrunk(parallel=args.parallel, max_new=args.max_new)
    if args.walker:
        Arm.walker = LinearHead.load(args.walker)
    if args.evaluator:
        Arm.evaluator = load_head(args.evaluator)
    tasks = sorted({s.task for s in chosen_stories})
    reading: dict = {
        "kind": "store",
        "step": 1,
        "trunk": trunk.served_model(),
        "thinking": False,
        "greedy": True,
        "max_new_tokens": args.max_new,
        "tasks": tasks,
        "stories_per_task": args.stories,
        "questions": sum(len(s.questions) for s in chosen_stories),
        "store": {"path": args.store, "rows": len(store), "embedders": store.embedders,
                  **hear_stats},
        "command": " ".join(sys.argv),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True,
                              text=True, cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
        "note": args.note,
        "walker": ({"path": args.walker, "layer": Arm.walker.layer, "lam": Arm.walker.lam,
                    "key": Arm.walker.key, "prompt": hop_prompt("<question>", ["<row read>"])}
                   if Arm.walker else None),
        "evaluator": ({"path": args.evaluator, "layer": Arm.evaluator.layer,
                       "lam": Arm.evaluator.lam, "threshold": Arm.evaluator.threshold,
                       "features": Arm.evaluator.key,
                       "prompt": judge_prompt("<question>", ["<row read>"], "<candidate>")}
                      if Arm.evaluator else None),
        "arms": {},
    }
    items = [(s, i) for s in chosen_stories for i in range(len(s.questions))]

    def run_arm(arm: Arm, store: Store, subset=None) -> dict:
        rng = random.Random(args.seed)
        picked = subset if subset is not None else items
        shown, lookup_ms = [], []
        for s, i in picked:
            rows, ms = arm.rows_for(store, s, i, first_id[s.episode], queries, rng)
            shown.append(rows)
            lookup_ms.append(ms)
        prompts = [question_messages([r.text for r in rows], s.questions[i].text)
                   for (s, i), rows in zip(picked, shown)]
        started = time.perf_counter()
        tokens_before = trunk.tokens_out
        replies: list[str] = []
        chunk = args.parallel * 4
        for at in range(0, len(prompts), chunk):
            replies.extend(trunk.ask_many(prompts[at:at + chunk]))
            done = min(at + chunk, len(prompts))
            acc = sum(correct(s.questions[i].answer, r)
                      for (s, i), r in zip(picked[:done], replies)) / done
            print(f"{arm.spec} {done}/{len(prompts)} acc={acc:.3f}", flush=True)
        seconds = time.perf_counter() - started
        out_rows = []
        for (s, i), rows, said in zip(picked, shown, replies):
            q = s.questions[i]
            ids = [r.id for r in rows]
            supporting_ids = [first_id[s.episode] + j for j in q.supporting]
            out_rows.append({
                "task": s.task, "episode": s.episode, "question": q.text, "answer": q.answer,
                "said": said, "correct": correct(q.answer, said), "rows": ids,
                "supporting_found": sum(j in ids for j in supporting_ids),
                "supporting": len(supporting_ids), "heard": q.heard,
            })

        def by_task(f):
            return {t: round(float(np.mean([f(x) for x in out_rows if x["task"] == t])), 3)
                    for t in tasks}

        result = {
            "score": round(float(np.mean([x["correct"] for x in out_rows])), 3),
            "by_task": by_task(lambda x: x["correct"]),
            "supporting_recall_by_task": by_task(
                lambda x: x["supporting_found"] / x["supporting"]),
            "rows_shown_by_task": by_task(lambda x: len(x["rows"])),
            "lookup_ms_mean": round(float(np.mean(lookup_ms)), 3),
            "lookup_ms_max": round(float(np.max(lookup_ms)), 3),
            "seconds": round(seconds, 1),
            "questions_per_second": round(len(prompts) / seconds, 2),
            "tokens_out": trunk.tokens_out - tokens_before,
            "rows": out_rows,
        }
        print(f"== {arm.spec}: {result['score']} by task {result['by_task']} "
              f"recall {result['supporting_recall_by_task']} in {seconds:.0f}s", flush=True)
        return result

    arms = [Arm(a) for a in args.arms.split(",")]
    for arm in arms:
        reading["arms"][arm.spec] = run_arm(arm, store)

    # The restart: the same store opened again from disk, in this process, must retrieve the
    # same rows for every question; and the trunk, asked again late with thousands of rows
    # heard since, must say the same for the first stories' questions. Arms that walk with
    # the trunk are left out of the reload check: it would re-run them whole, and the walker
    # arms passed it once (readings/store-minilm-trunk-20261028T032951Z.json).
    again = Store(args.store)
    store_arms = [a for a in arms if a.kind in ("store", "unscoped", "hops", "hops-random")]
    identity: dict = {"rows_identical_after_reload": {}, "reask": {}}
    for arm in store_arms:
        rng = random.Random(args.seed)
        same = all(
            [r.id for r in arm.rows_for(again, s, i, first_id[s.episode], queries, rng)[0]]
            == reading["arms"][arm.spec]["rows"][n]["rows"]
            for n, (s, i) in enumerate(items))
        identity["rows_identical_after_reload"][arm.spec] = same
    if store_arms and args.reask:
        arm = store_arms[0]
        early = [(s, i) for s, i in items if s.index < args.reask]
        late = run_arm(arm, again, subset=early)
        early_rows = [x for x in reading["arms"][arm.spec]["rows"]
                      if int(x["episode"].split("s")[1]) < args.reask]
        identity["reask"] = {
            "arm": arm.spec, "questions": len(early),
            "rows_identical": sum(a["rows"] == b["rows"] for a, b in zip(early_rows, late["rows"])),
            "answers_identical": sum(a["said"] == b["said"]
                                     for a, b in zip(early_rows, late["rows"])),
            "score_early": round(float(np.mean([x["correct"] for x in early_rows])), 3),
            "score_late": late["score"],
        }
    reading["identity"] = identity
    reading["tokens_in"] = trunk.tokens_in
    reading["tokens_out"] = trunk.tokens_out
    print(json.dumps(identity, indent=1))
    name = "-".join(store.embedders)
    path = ROOT / "readings" / f"store-{name}-{reading['taken_at']}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print("wrote", path)


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("phase", choices=["hear", "ask"])
    p.add_argument("--tasks", default="1,2,3")
    p.add_argument("--stories", type=int, default=100, help="stories per task, the first N")
    p.add_argument("--store", default="state/store-step1")
    p.add_argument("--embedders", default="minilm,trunk")
    p.add_argument("--arms", default="floor,ceiling,oracle,store:minilm:4,store:minilm:8,"
                                     "store:trunk:4,random:4,unscoped:minilm:4")
    p.add_argument("--reask", type=int, default=20,
                   help="re-ask the first N stories' questions of each task after all arms")
    p.add_argument("--parallel", type=int, default=8, help="server streams, or local batch")
    p.add_argument("--trunk", default="served", choices=["served", "local"])
    p.add_argument("--walker", default=None, help="a walker-*.npz from walker_fit.py")
    p.add_argument("--evaluator", default=None,
                   help="an evaluator*.npz from evaluator_fit.py or *.pt from head_train.py")
    p.add_argument("--max-new", type=int, default=8)
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--note", default="")
    args = p.parse_args()
    (hear if args.phase == "hear" else ask)(args)


if __name__ == "__main__":
    main()
