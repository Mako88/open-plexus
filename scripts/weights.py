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
from datetime import UTC, datetime
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")
sys.path.insert(0, str(Path(__file__).resolve().parent))

import stories as runner  # noqa: E402
from unfused.exam.stories import fingerprint, right, stream  # noqa: E402
from unfused.graph import BLANKS, GraphArm  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
FACTORS = ("recency", "fit", "mark", "schema")
# narrative schemas, read as an instrument only: what `aff38ef2` had as focus's fourth
# factor, counted here so the sweep can weigh it, and never used to answer
CHAIN = 6
SMOOTH = 5.0
# each factor's exponent is swept over these; a common scale changes nothing, so one
# grid holds every ratio worth reading
GRID = (0.0, 0.25, 0.5, 1.0, 2.0, 4.0)


class Schemed(GraphArm):
    """The system as committed, which also counts narrative schemas as each episode
    ends, so focus's names can be read with them. Its answers are the committed ones."""

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

    def schemas(self, question: str) -> dict[str, float]:
        """Each name focus weighs, by its best individual's schema for the blank."""
        slot = self.blank(question)
        out: dict[str, float] = {}
        if slot is None:
            return out
        for (node,) in self.db.execute(
                "SELECT DISTINCT node FROM edges WHERE event >= ? AND node LIKE 'i:%'",
                (self.first_event(),)):
            said = self.describe(node)
            out[said] = max(out.get(said, 0.0), self.schema(node, slot))
        return out

    def consolidate(self) -> None:
        """What usually happens, counted once an episode ends (Chambers and Jurafsky's
        narrative schemas): for each individual of the episode, the verb slots it filled
        in the order it filled them, each earlier slot counted with each later one. 'lose'
        then 'find', the same ball. Learnt from counts across episodes, never written."""
        first = self.first_event()
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
                (node, self.first_event())):
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
    def __init__(self, work: Path) -> None:
        super().__init__(work)
        self.arm.close()
        self.open = lambda: Schemed(work)
        self.arm = self.open()
        self.records: list[dict] = []

    def ask(self, turn, story):
        scores = self.arm.focus(story.question) or {}
        schemas = self.arm.schemas(story.question) if scores else {}
        scores = {n: (*f, schemas.get(n, 1.0)) for n, f in scores.items()}
        said = super().ask(turn, story)
        notes = self.arm.last_notes
        # one entry an ask, in the order the runner's rows come, to be joined with them
        self.records.append({"candidates": [[list(f), right(story.answers, name)]
                                            for name, f in scores.items()]}
                            if notes and notes[-1] == "by:focus" and scores else None)
        return said


def accuracy(records: list[dict], weights) -> float:
    if not records:
        return 0.0
    hit = 0
    for r in records:
        best = max(r["candidates"], key=lambda c: sum(
            w * math.log(f) for w, f in zip(weights, c[0]) if w))
        hit += best[1]
    return hit / len(records)


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--stories", type=int, default=1000)
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--note", default="")
    args = p.parse_args()
    stories = stream(args.stories, args.seed)
    work = Path(tempfile.mkdtemp(prefix="unfused-weights-"))
    arm = Recorded(work)
    out = runner.run(arm, stories)
    shutil.rmtree(work, ignore_errors=True)
    records = [{**r, "story": row["story"], "form": row["form"]}
               for r, row in zip(arm.records, out["rows"]) if r is not None]
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
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
        swept = sorted(((accuracy(fit_on, w), w) for w in itertools.product(GRID, repeat=len(FACTORS))
                        if any(w)), reverse=True)
        best = swept[0][1] if swept else None
        reading["forms"][form] = {
            "n": [len(fit_on), len(read_on)],
            "reachable": reachable,
            "at_1": [accuracy(fit_on, (1, 1, 1, 0)), accuracy(read_on, (1, 1, 1, 0))],
            "at_1_schema": [accuracy(fit_on, (1, 1, 1, 1)), accuracy(read_on, (1, 1, 1, 1))],
            "best_without_schema": (lambda sw: [sw[0][0], accuracy(read_on, sw[0][1]),
                                                list(sw[0][1])] if sw else None)(sorted(
                ((accuracy(fit_on, w), w) for w in itertools.product(GRID, repeat=3)
                 if any(w)), reverse=True)),
            "best": best, "best_fit_on": swept[0][0] if swept else None,
            "best_read_on": accuracy(read_on, best) if best else None,
            "alone": {f: [accuracy(fit_on, w), accuracy(read_on, w)] for f, w in zip(
                FACTORS, ((1, 0, 0, 0), (0, 1, 0, 0), (0, 0, 1, 0), (0, 0, 0, 1)))},
            "top": [[a, list(w)] for a, w in swept[:10]],
        }
    path = ROOT / "readings" / f"weights-s{args.seed}-n{args.stories}-{stamp}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    raw = ROOT / "state" / f"weights-records-s{args.seed}-n{args.stories}.json"
    raw.parent.mkdir(parents=True, exist_ok=True)
    raw.write_text(json.dumps(records), encoding="utf-8")
    print(json.dumps(reading["forms"], indent=1))
    print(f"score {out['score']} -> {path.name}")
    return 1 if "crashed" in out else 0


if __name__ == "__main__":
    sys.exit(main())
