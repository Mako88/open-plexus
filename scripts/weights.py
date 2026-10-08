"""What focus's factor weights could buy at best: the ceiling any learnt weights stand under.

    uv run python scripts/weights.py --stories 1000 --note "..."

One stream is heard once. Wherever focus decides an answer, every name it weighed is kept
with its factors and whether it is right. Focus's score is the product of its factors,
each raised to a weight; the weights are then swept over that record, which needs no
second hearing. They are fitted on the first half of the stream's questions and read on
the second, so a ceiling is never read on the questions it was fitted to. A ceiling no
higher than the weights at 1 says no learning rule for them can lift the score.
"""

from __future__ import annotations

import argparse
import itertools
from collections import Counter
import json
import math
import os
import shutil
import sys
import tempfile
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")
sys.path.insert(0, str(Path(__file__).resolve().parent))

import stories as runner  # noqa: E402
from unfused.reading import utc_stamp  # noqa: E402
from unfused.exam.stories import fingerprint, right, stream  # noqa: E402
from unfused.graph import BLANKS, GraphArm, parse  # noqa: E402
from unfused.home import home  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
FACTORS = ("recency", "fit", "mark", "schema", "echo")
# narrative schemas, read as an instrument only: what `aff38ef2` had as focus's fourth
# factor, counted here so the sweep can weigh it, and never used to answer
CHAIN = 6
SMOOTH = 5.0
# each factor's exponent is swept over these; a common scale changes nothing, so one
# grid holds every ratio worth reading
GRID = (0.0, 0.25, 0.5, 1.0, 2.0, 4.0)
# what an echo starts from where nothing of the sentence reached a name
QUIET = 1e-3


class Schemed(GraphArm):
    """The system as committed, which also counts narrative schemas as each episode
    ends, so focus's names can be read with them. Its answers are the committed ones."""

    # an answer keeps what focus weighed, so it is not weighed a second time here
    trace = True

    def __init__(self, *args, **kw) -> None:
        super().__init__(*args, **kw)
        self.db.executescript(
            "CREATE TABLE IF NOT EXISTS chained (a TEXT NOT NULL, b TEXT NOT NULL, "
            "n INTEGER NOT NULL, PRIMARY KEY (a, b)); CREATE TABLE IF NOT EXISTS chain_ends "
            "(slot TEXT NOT NULL, side INTEGER NOT NULL, n INTEGER NOT NULL, "
            "PRIMARY KEY (slot, side));")

    def hear(self, turn: int, text: str) -> None:
        if not any(ch.isalnum() for ch in text):
            self.consolidate()
        super().hear(turn, text)

    def echoes(self, question: str) -> dict[str, float]:
        """How much of the question's sentence reaches each name in mind: every noun
        fires the story's individuals of its lemma, every verb the story's events of
        its lemma, each spreading by the committed walk, and what reaches a name is
        summed. Copying from context, as an attention head does: what sat beside the
        sentence's other words earlier in the story."""
        self.meeter.tick()
        out: dict[str, float] = {}
        episode, first = self.mind.episode(), self.mind.first_event()
        for t in parse(self.model, question):
            lemma = t.lemma_.lower()
            if t.pos_ in ("NOUN", "PROPN"):
                held = self.individuals.of(lemma, episode=True)
            elif t.pos_ == "VERB":
                held = [f"e:{e}" for (e,) in self.db.execute(
                    "SELECT id FROM events WHERE lemma = ? AND turn > ? AND id >= ?",
                    (lemma, episode, first))]
            else:
                continue
            if not held:
                continue
            total, _ = self.meeter.spread({n: 1.0 / len(held) for n in held})
            for node, a in total.items():
                if node.startswith("i:") and node not in held:
                    said = self.individuals.describe(node)
                    out[said] = out.get(said, 0.0) + a
        return out

    def schemas(self, question: str) -> dict[str, float]:
        """Each name focus weighs, by its best individual's schema for the blank."""
        slot = self.reader.blank(question)
        out: dict[str, float] = {}
        if slot is None:
            return out
        for (node,) in self.db.execute(
                "SELECT DISTINCT node FROM edges WHERE event >= ? AND node LIKE 'i:%'",
                (self.mind.first_event(),)):
            said = self.individuals.describe(node)
            out[said] = max(out.get(said, 0.0), self.schema(node, slot))
        return out

    def consolidate(self) -> None:
        """What usually happens, counted once an episode ends (Chambers and Jurafsky's
        narrative schemas): for each individual of the episode, the verb slots it filled
        in the order it filled them, each earlier slot counted with each later one. 'lose'
        then 'find', the same ball. Learnt from counts across episodes, never written."""
        first = self.mind.first_event()
        seen: dict[str, list[str]] = {}
        for node, lemma, label in self.db.execute(
                "SELECT edges.node, events.lemma, edges.label FROM edges JOIN events ON "
                "events.id = edges.event WHERE edges.event >= ? AND edges.node LIKE 'i:%' AND "
                "events.mood = '' ORDER BY edges.event", (first,)):
            if label not in BLANKS and not label.startswith("prep:"):
                continue
            slots = seen.setdefault(node, [])
            slot = f"{lemma}|{label}"
            if not slots or slots[-1] != slot:
                slots.append(slot)
        pairs: Counter = Counter()
        for slots in seen.values():
            for i, a in enumerate(slots):
                for b in slots[i + 1:i + 1 + CHAIN]:
                    pairs[(a, b)] += 1
        self.db.executemany("INSERT INTO chained VALUES (?, ?, ?) ON CONFLICT(a, b) DO UPDATE "
                            "SET n = n + excluded.n", [(a, b, n) for (a, b), n in pairs.items()])
        ends: Counter = Counter()
        for (a, b), n in pairs.items():
            ends[(a, 0)] += n
            ends[(b, 1)] += n
        self.db.executemany("INSERT INTO chain_ends VALUES (?, ?, ?) ON CONFLICT(slot, side) "
                            "DO UPDATE SET n = n + excluded.n",
                            [(a, side, n) for (a, side), n in ends.items()])

    def schema(self, node: str, blank: tuple[str, str]) -> float:
        """How the slots an individual of this episode has filled predict the blank's
        slot, from the counts consolidation keeps: the mean, over its slots, of how much
        likelier the blank is after that slot than after any, smoothed towards 1 where
        little was counted. 1 where nothing is known."""
        b = f"{blank[0]}|{blank[1]}"
        total = self._chain_total()
        after = self.db.execute("SELECT n FROM chain_ends WHERE slot = ? AND side = 1",
                                (b,)).fetchone()
        if not total or after is None:
            return 1.0
        prior = after[0] / total
        logs = []
        for lemma, label in self.db.execute(
                "SELECT events.lemma, edges.label FROM edges JOIN events ON events.id = "
                "edges.event WHERE edges.node = ? AND edges.event >= ? AND events.mood = ''",
                (node, self.mind.first_event())):
            if label not in BLANKS and not label.startswith("prep:"):
                continue
            a = f"{lemma}|{label}"
            before = self.db.execute("SELECT n FROM chain_ends WHERE slot = ? AND side = 0",
                                     (a,)).fetchone()
            both = self.db.execute("SELECT n FROM chained WHERE a = ? AND b = ?",
                                   (a, b)).fetchone()
            n_a, n_ab = (before[0] if before else 0), (both[0] if both else 0)
            # only what was counted raises: a pair never counted is, at this sparsity,
            # mostly a pair never heard, and read as against it, it sank whoever filled
            # common slots, which is most answers
            logs.append(max(0.0, math.log((n_ab + SMOOTH * prior) / ((n_a + SMOOTH) * prior))))
        return math.exp(sum(logs) / len(logs)) if logs else 1.0

    def _chain_total(self) -> int:
        return self.db.execute("SELECT COALESCE(SUM(n), 0) FROM chain_ends WHERE side = 1"
                               ).fetchone()[0]



class Recorded(runner.Graphed):
    def __init__(self, work: Path, every: int = 1) -> None:
        super().__init__(work)
        # the extra factors are read on every `every`th question focus decided
        self.every, self.decided = every, 0
        self.arm.close()
        self.open = lambda: Schemed(work)
        self.arm = self.open()
        self.records: list[dict] = []

    def ask(self, turn, story):
        said = super().ask(turn, story)
        scores = self.arm.traced or {}
        if scores:
            self.decided += 1
        # a question outside the sample keeps no record: the sweep reads those it has
        # every factor of
        if scores and (self.decided - 1) % self.every:
            scores = {}
        schemas = self.arm.schemas(story.question) if scores else {}
        echoes = self.arm.echoes(story.question) if scores else {}
        scores = {n: (*f, schemas.get(n, 1.0), QUIET + echoes.get(n, 0.0))
                  for n, f in scores.items()}
        notes = self.arm.last_notes
        # one entry an ask, in the order the runner's rows come, to be joined with them
        self.records.append({"candidates": [[list(f), right(story.answers, name)]
                                            for name, f in scores.items()],
                             "correct": right(story.answers, said)}
                            if notes and notes[-1] == "by:focus" and scores else None)
        return said


_STACKED: dict = {}


def stacked(records: list[dict]):
    """The records' candidates as arrays, built once: each factor's log (read with
    `math.log`, as the sum was), a row a record, padded with candidates that never win."""
    import numpy as np
    kept = _STACKED.get(id(records))
    if kept is not None and kept[0] is records and kept[1] == len(records):
        return kept[2]
    width = max((len(r["candidates"]) for r in records), default=0)
    depth = max((len(c[0]) for r in records for c in r["candidates"]), default=0)
    logs = np.zeros((depth, len(records), width))
    real = np.zeros((len(records), width), dtype=bool)
    right_ = np.zeros((len(records), width), dtype=np.int64)
    for i, r in enumerate(records):
        for j, (f, ok) in enumerate(r["candidates"]):
            real[i, j] = True
            right_[i, j] = ok
            for k, x in enumerate(f):
                logs[k, i, j] = math.log(x)
    _STACKED[id(records)] = (records, len(records), (logs, real, right_))
    return logs, real, right_


def accuracies(records: list[dict], sweep) -> list[float]:
    """`accuracy` at each of many weights at once. Each candidate's score is summed one
    factor at a time in the order `accuracy` sums them, a factor at weight 0 left out, so
    every score is the float it was; the first best candidate of a record wins a tie."""
    import numpy as np
    sweep = list(sweep)
    if not records:
        return [0.0] * len(sweep)
    logs, real, right_ = stacked(records)
    out: list[float] = []
    for at in range(0, len(sweep), 64):
        group = sweep[at:at + 64]
        score = np.zeros((len(group), *real.shape))
        for k in range(logs.shape[0]):
            w = np.array([ws[k] if k < len(ws) else 0.0 for ws in group])
            used = w != 0
            if used.any():
                score[used] += w[used, None, None] * logs[k]
        score[:, ~real] = -np.inf
        best = score.argmax(axis=2)
        hit = np.take_along_axis(np.broadcast_to(right_, score.shape), best[..., None],
                                 axis=2)[..., 0].sum(axis=1)
        out += [int(h) / len(records) for h in hit]
    return out


def accuracy(records: list[dict], weights) -> float:
    return accuracies(records, [weights])[0]


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--stories", type=int, default=1000)
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--note", default="")
    # the extra factors (schemas, echoes) cost more than the answer: read them on every
    # Nth question focus decides, and the sweep is over those
    p.add_argument("--every", type=int, default=1)
    args = p.parse_args()
    stories = stream(args.stories, args.seed)
    work = Path(tempfile.mkdtemp(prefix="unfused-weights-"))
    arm = Recorded(work, args.every)
    out = runner.run(arm, stories)
    shutil.rmtree(work, ignore_errors=True)
    records = [{**r, "story": row["story"], "form": row["form"]}
               for r, row in zip(arm.records, out["rows"]) if r is not None]
    stamp = utc_stamp()
    half = args.stories // 2
    reading = {"kind": "weights", "taken_at": stamp, "note": args.note,
               "stream": {"source": "TinyStories-valid", "seed": args.seed,
                          "stories": args.stories, "fingerprint": fingerprint(stories)},
               "score": out["score"], "factors": FACTORS, "grid": GRID, "forms": {}}
    for form in ("cloze", "check", "far", "late"):
        rs = [r for r in records if r["form"] == form]
        fit_on = [r for r in rs if r["story"] < half]
        read_on = [r for r in rs if r["story"] >= half]
        # how often any candidate focus weighed was right: the ceiling of any ranking
        reachable = [sum(any(c[1] for c in r["candidates"]) for r in x) / len(x) if x
                     else None for x in (fit_on, read_on)]
        # schemas are refuted at any weight (e7d53f7c), so they are held at 0 here
        grid = [w for w in ((r, f, m, 0.0, e) for r, f, m, e in
                            itertools.product(GRID, repeat=4)) if any(w)]
        swept = sorted(zip(accuracies(fit_on, grid), grid), reverse=True)
        grid = [w for w in ((r, f, m, 0.0, 0.0) for r, f, m in
                            itertools.product(GRID, repeat=3)) if any(w)]
        without = max(zip(accuracies(fit_on, grid), grid), default=None)
        best = swept[0][1] if swept else None
        reading["forms"][form] = {
            "n": [len(fit_on), len(read_on)],
            "reachable": reachable,
            "at_1": [accuracy(fit_on, (1, 1, 1, 0, 0)), accuracy(read_on, (1, 1, 1, 0, 0))],
            "at_1_echo": [accuracy(fit_on, (1, 1, 1, 0, 1)), accuracy(read_on, (1, 1, 1, 0, 1))],
            "best_without_echo": [without[0], accuracy(read_on, without[1]), list(without[1])]
            if without else None,
            "best": best, "best_fit_on": swept[0][0] if swept else None,
            "best_read_on": accuracy(read_on, best) if best else None,
            "alone": {f: [accuracy(fit_on, w), accuracy(read_on, w)] for f, w in zip(
                FACTORS, [tuple(int(i == j) for j in range(len(FACTORS)))
                          for i in range(len(FACTORS))])},
            "top": [[a, list(w)] for a, w in swept[:10]],
        }
    path = ROOT / "readings" / f"weights-s{args.seed}-n{args.stories}-{stamp}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    raw = home() / "state" / f"weights-records-s{args.seed}-n{args.stories}.json"
    raw.parent.mkdir(parents=True, exist_ok=True)
    raw.write_text(json.dumps(records), encoding="utf-8")
    print(json.dumps(reading["forms"], indent=1))
    print(f"score {out['score']} -> {path.name}")
    return 1 if "crashed" in out else 0


if __name__ == "__main__":
    sys.exit(main())
