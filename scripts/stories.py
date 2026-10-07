"""Run arms through the TinyStories stream and write one reading per arm under `readings/`.

    uv run python scripts/stories.py --stories 300 --arms frequent,blind,graphed --note "..."

One system hears every story in turn and is never restarted between them; it is closed
and reopened every 100 stories, so what it knows must survive on disk. Two baselines:
`blind` says the commonest answer of the questions before it and never reads a story;
`frequent` says the commonest noun the story has told so far.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import sys
import tempfile
import time
from collections import Counter, defaultdict
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")

from unfused.reading import utc_stamp  # noqa: E402
from unfused.exam.stories import bucket, fingerprint, right, stream  # noqa: E402
from unfused.exam.world import RIGHT, WRONG  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
BREAK = "***"


class Blind:
    name = "blind"

    def __init__(self) -> None:
        # a tally a form: a cloze and a comprehension question are not one kind
        self.answers: dict[str, Counter] = defaultdict(Counter)

    def tell(self, turn, text):
        pass

    def ask(self, turn, story):
        seen = self.answers[type(story).__name__]
        return seen.most_common(1)[0][0] if seen else "I don't know."

    def react(self, turn, story, said):
        self.answers[type(story).__name__][story.answer] += 1

    def dials(self):
        return {}

    def close(self):
        pass


class Frequent(Blind):
    name = "frequent"

    def ask(self, turn, story):
        counts = Counter(story.earlier)
        if not counts:
            return "I don't know."
        top = max(counts.values())
        # ties go to the latest named
        return next(w for w in reversed(story.earlier) if counts[w] == top)

    def react(self, turn, story, said):
        pass


class Graphed:
    name = "graphed"

    def __init__(self, work: Path) -> None:
        from unfused.graph import GraphArm

        self.open = lambda: GraphArm(work)
        self.arm = self.open()

    def reopen(self):
        self.arm.close()
        self.arm = self.open()

    def tell(self, turn, text):
        self.arm.turn(turn, text)

    def ask(self, turn, story):
        return self.arm.turn(turn, story.question) or ""

    def react(self, turn, story, said):
        ok = right(story.answers, said)
        self.arm.turn(turn, RIGHT if ok else WRONG.format(answer=story.answer))

    def dials(self):
        return self.arm.dials()

    def close(self):
        self.arm.close()


class Recalled(Graphed):
    """A control: at a late ask the world hands the system the old story's episode, put
    back in mind before its sentence is retold, so what recall by a cue would buy is read
    apart from finding the episode (THE ORDER, after the foundation, item 6)."""

    def recall(self, lo, hi):
        self.arm.recall(lo, hi)


class Asked(Recalled):
    """The same control handed the episode only at the question, after the retold
    sentence has opened individuals of its own, as recall by a cue is."""

    at_ask = True


def run(arm, stories, reopen_every: int = 100) -> dict:
    rows, turn, sizes = [], 0, {}
    started = time.perf_counter()
    # what the run itself spent, apart from what else the machine was doing
    cpu = time.process_time()
    crashed = None
    try:
        turn = _stream(arm, stories, reopen_every, rows, sizes)
    except Exception:
        # the reading so far is kept and says where it stopped; the run still fails
        import traceback

        crashed = traceback.format_exc()
    out = _summary(arm, rows, sizes, turn, started, cpu)
    if crashed:
        out["crashed"] = crashed
    return out


# a story is asked about again this many stories after it was told: early material
# asked late (THE ORDER, harder checks; DECIDED, a continual learner keeps what it had)
LATE = 100


def reminder(old, check, rarity: Counter) -> str | None:
    """What a parent says to bring a story back (THE ORDER, harder checks, John's): not
    a sentence of it but what set it apart, its two nouns said most in it and least in
    the others (tf-idf). The check's own answer is never one of them."""
    said = Counter(old.earlier)
    picked = sorted((w for w in said if w.isalpha()
                     and w not in {a.lower() for a in check.answers}),
                    key=lambda w: -said[w] / rarity[w])[:2]
    if len(picked) < 2:
        return None
    words = set(re.findall(r"\w+", " ".join(old.told + old.after)))

    def phrase(w):
        # a name is a noun the story only ever writes capitalised
        return w.capitalize() if w not in words and w.capitalize() in words else f"the {w}"
    return f"Think back to the story with {phrase(picked[0])} and {phrase(picked[1])}."


def _stream(arm, stories, reopen_every, rows, sizes) -> int:
    turn = 0
    by_index = {s.index: s for s in stories}
    # how many stories of the stream say each noun, as the world knows it
    rarity = Counter(w for s in stories for w in set(s.earlier))
    # each story's break and its last turn, for a control that is handed an episode
    spans: dict[int, tuple[int, int]] = {}
    for s in stories:
        if s.index and s.index % reopen_every == 0 and hasattr(arm, "reopen"):
            arm.reopen()
        # every story begins after a break, as a page or a title does; TinyStories marks
        # each boundary itself
        arm.tell(turn, BREAK)
        start = turn
        turn += 1
        told = s.told + s.after
        for p in range(len(told) + 1):
            # the cloze before its held-back sentence, then whatever is due to be checked
            due = ([("cloze", s)] if s.question is not None and p == len(s.told) else []) + \
                [(c.form, c) for c in s.checks if c.at == p]
            for form, q in due:
                rows.append(_ask(arm, turn, s, form, q))
            if p < len(told):
                arm.tell(turn, told[p])
                turn += 1
        spans[s.index] = (start, turn - 1)
        # a story told LATE stories ago, recalled as a parent recalls one: its first
        # sentence said again after a break, then one of its checks asked
        old = by_index.get(s.index - LATE)
        check = next((c for c in old.checks if c.form == "check"), None) if old else None
        if check is not None and old.told:
            arm.tell(turn, BREAK)
            turn += 1
            handed = hasattr(arm, "recall") and old.index in spans
            if handed and not getattr(arm, "at_ask", False):
                arm.recall(*spans[old.index])
            arm.tell(turn, old.told[0])
            turn += 1
            if handed and getattr(arm, "at_ask", False):
                arm.recall(*spans[old.index])
            rows.append(_ask(arm, turn, s, "late", check))
            # and another of its checks after a reminder of what set it apart, as a
            # cue heard as a cue rather than a telling
            other = next((c for c in old.checks if c is not check), None)
            said = reminder(old, other, rarity) if other is not None else None
            if said is not None:
                arm.tell(turn, BREAK)
                turn += 1
                arm.tell(turn, said)
                turn += 1
                rows.append(_ask(arm, turn, s, "cued", other))
        if (b := bucket(s.index)) != bucket(s.index + 1):
            sizes[b] = arm.dials()
    return turn


def _ask(arm, turn, s, form, q) -> dict:
    t0 = time.perf_counter()
    said = arm.ask(turn, q)
    seconds = time.perf_counter() - t0
    arm.react(turn, q, said)
    notes = list(getattr(getattr(arm, "arm", None), "last_notes", []))
    return {"story": s.index, "bucket": bucket(s.index), "form": form,
            "question": q.question, "answer": q.answer, "said": said,
            "correct": right(q.answers, said), "refused": "don't know" in (said or "").lower(),
            "seconds": round(seconds, 3), "gave_up": "(gave up)" in notes, "notes": notes}


def _curve(rows, sizes) -> dict:
    by = defaultdict(list)
    for r in rows:
        by[r["bucket"]].append(r)
    return {b: {"score": round(sum(r["correct"] for r in rs) / len(rs), 3), "n": len(rs),
                "refused": round(sum(r["refused"] for r in rs) / len(rs), 3),
                "seconds_per_question": round(sum(r["seconds"] for r in rs) / len(rs), 3),
                "size": sizes.get(b)}
            for b, rs in by.items()}


def _summary(arm, rows, sizes, turn, started, cpu) -> dict:
    dials = arm.dials()
    arm.close()
    # `score` and `curve` stay the cloze's; each form is read as its own curve
    forms = {}
    for form in ("cloze", "check", "far", "late", "cued"):
        rs = [r for r in rows if r["form"] == form]
        forms[form] = {"score": round(sum(r["correct"] for r in rs) / len(rs), 3)
                       if rs else None, "n": len(rs), "curve": _curve(rs, sizes)}
    return {"arm": arm.name, "dials": dials, "turns": turn,
            "seconds": round(time.perf_counter() - started, 1),
            "cpu_seconds": round(time.process_time() - cpu, 1),
            "score": forms["cloze"]["score"], "curve": forms["cloze"]["curve"],
            "forms": forms, "rows": rows}


ARMS = {"graphed": Graphed, "recalled": Recalled, "asked": Asked}


def one(name: str, args, stories, asked: int, checks: int, stamp: str) -> int:
    """One arm through the stream, its reading written and its curve printed."""
    work = Path(tempfile.mkdtemp(prefix=f"unfused-stories-{name}-"))
    arm = ARMS[name](work) if name in ARMS else {"blind": Blind, "frequent": Frequent}[name]()
    out = run(arm, stories)
    shutil.rmtree(work, ignore_errors=True)
    reading = {"kind": "stories", "taken_at": stamp, "note": args.note,
               "stream": {"source": "TinyStories-valid", "seed": args.seed,
                          "stories": args.stories, "skip": args.skip,
                          "questions": asked, "checks": checks,
                          "fingerprint": fingerprint(stories)},
               **out}
    skip = f"-k{args.skip}" if args.skip else ""
    path = ROOT / "readings" / f"stories-{name}-s{args.seed}-n{args.stories}{skip}-{stamp}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    for form, f in out["forms"].items():
        curve = "  ".join(f"{b}:{c['score']}" for b, c in f["curve"].items())
        print(f"{name:9} {form:5} {f['score']}  {curve}", flush=True)
    print(f"{name:9} {out['seconds']}s (cpu {out.get('cpu_seconds')}s) -> {path.name}",
          flush=True)
    if "crashed" in out:
        print(out["crashed"], flush=True)
        return 1
    return 0


def apart(names: list[str], args, stamp: str) -> int:
    """Arms that do not depend on one another, each in a process of its own (its own work
    directory, as ever), `args.jobs` at a time. Each arm's output is printed whole when it
    ends, in the order the arms were named, and its reading is the one a lone run writes."""
    import subprocess
    from concurrent.futures import ThreadPoolExecutor

    def child(name: str) -> subprocess.CompletedProcess:
        cmd = [sys.executable, str(Path(__file__).resolve()), "--stories", str(args.stories),
               "--seed", str(args.seed), "--arms", name, "--note", args.note,
               "--skip", str(args.skip), "--stamp", stamp, "--jobs", "1"]
        return subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")

    failed = 0
    with ThreadPoolExecutor(max_workers=args.jobs) as pool:
        for done in [pool.submit(child, n) for n in names]:
            got = done.result()
            # the stream's size is printed once, above
            print("".join(got.stdout.splitlines(keepends=True)[1:]), end="", flush=True)
            if got.returncode:
                print(got.stderr[-2000:], flush=True)
                failed = 1
    return failed


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--stories", type=int, default=300)
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--arms", default="frequent,blind,graphed")
    p.add_argument("--note", default="")
    # a control: memory starts empty at this story, so a late bucket is read without
    # everything heard before it
    p.add_argument("--skip", type=int, default=0)
    # how many arms run at once, each in a process of its own: one fewer than the cores
    # by default, and never more than the arms
    p.add_argument("--jobs", type=int, default=0)
    p.add_argument("--stamp", default="", help=argparse.SUPPRESS)
    args = p.parse_args()

    stories = [s for s in stream(args.stories, args.seed) if s.index >= args.skip]
    asked = sum(s.question is not None for s in stories)
    checks = sum(len(s.checks) for s in stories)
    print(f"{len(stories)} stories, {asked} cloze, {checks} checks", flush=True)
    stamp = args.stamp or utc_stamp()
    names = args.arms.split(",")
    args.jobs = min(args.jobs or max(1, (os.cpu_count() or 1) - 1), len(names))
    if args.jobs > 1:
        return apart(names, args, stamp)
    for name in names:
        if one(name, args, stories, asked, checks, stamp):
            return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
