"""Step 3, the counting tool on bAbI task 7: how many objects is someone carrying.

The trunk restates each sentence of the story as one event (`openplexus.events`), one pass a
sentence through llama-server with thinking off, and code keeps who holds what up to the
moment of the question and counts. Every sentence is restated once and reused by every
question of its story, as a store would hold it.

    .venv/Scripts/python scripts/count_reading.py --arms events:regex,events:trunk

`events:regex` reads the events with regular expressions, the tool's own ceiling. The floor,
ceiling and thinking arms are `ceiling.py --tasks 7` on the same served trunk. Writes
readings/count-*.json, every arm with its score and its forward passes a question.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.babi import Story, stories  # noqa: E402
from openplexus.events import (EVENT_SYSTEM, count_word, event_messages, event_of, holdings,  # noqa: E402
                               who_is_asked)

TASK = 7


def events_arm(reader: str, ss: list[Story], args) -> dict:
    """Each sentence becomes one event, by the trunk's restatement (`trunk`) or by regular
    expressions (`regex`, the tool's ceiling); code replays the events up to each question and
    counts what the asked person holds. The deployed system has every input here."""
    from openplexus.trunk import ServedTrunk

    started = time.perf_counter()
    sentences = [x for s in ss for x in s.sentences]
    if reader == "trunk":
        trunk = ServedTrunk(url=args.url, max_new=args.max_new, parallel=args.parallel, think=False)
        lines: list[str] = []
        chunk = args.parallel * 8
        for at in range(0, len(sentences), chunk):
            lines.extend(trunk.ask_many([event_messages(x) for x in sentences[at:at + chunk]]))
            print(f"events:trunk {len(lines)}/{len(sentences)} sentences restated", flush=True)
        served, tokens_in, tokens_out = trunk.served_model(), trunk.tokens_in, trunk.tokens_out
    else:
        lines, served, tokens_in, tokens_out = sentences, None, 0, 0
    rows = []
    at = 0
    events_right = 0
    for s in ss:
        said_lines = lines[at:at + len(s.sentences)]
        at += len(s.sentences)
        events = [event_of(x) for x in said_lines]
        true_events = [event_of(x) for x in s.sentences]
        events_right += sum(e == t for e, t in zip(events, true_events))
        for q in s.questions:
            who = who_is_asked(q.text)
            held = holdings(events[:q.heard])
            said = count_word(len(held.get(who, set()))) if who else ""
            rows.append({"episode": s.episode, "question": q.text, "heard": q.heard,
                         "answer": q.answer, "said": said, "correct": said == q.answer})
    n = len(rows)
    return {
        "kind": "events", "reader": reader, "think": False, "served_model": served,
        "event_prompt": EVENT_SYSTEM if reader == "trunk" else None,
        "inputs_the_deployed_system_has": "all: the story up to the question, and the question",
        "score": round(sum(r["correct"] for r in rows) / n, 3), "n": n, "stories": len(ss),
        "sentences": len(sentences), "events_right": events_right,
        "sentences_per_question": round(len(sentences) / n, 2),
        "passes_per_question": round((tokens_out + (len(sentences) if reader == "trunk" else 0)) / n, 1),
        "tokens_in_per_question": round(tokens_in / n, 1),
        "seconds": round(time.perf_counter() - started, 1),
        "restated": lines if reader == "trunk" else None, "rows": rows,
    }


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--arms", default="events:regex,events:trunk")
    p.add_argument("--stories", type=int, default=200, help="test stories read (all 200 by default)")
    p.add_argument("--url", default=None, help="llama-server, default LOCAL_SERVER or 127.0.0.1:8093")
    p.add_argument("--max-new", type=int, default=16)
    p.add_argument("--parallel", type=int, default=8)
    p.add_argument("--note", default="")
    args = p.parse_args()

    ss = [s for s in stories([TASK]) if s.index < args.stories]
    reading = {"kind": "count", "step": 3, "task": TASK, "stories": len(ss),
               "questions": sum(len(s.questions) for s in ss),
               "scorer": "exact match on the number word", "arms": {}}
    for spec in args.arms.split(","):
        parts = spec.split(":")
        if parts[0] != "events":
            raise SystemExit(f"unknown arm {spec}; floor, ceiling and thinking are ceiling.py --tasks 7")
        arm = events_arm(parts[1], ss, args)
        reading["arms"][spec] = arm
        print(f"== {spec}: {arm['score']} in {arm['seconds']}s, {arm['passes_per_question']} passes a question",
              flush=True)
    reading.update({
        "command": "scripts/count_reading.py " + " ".join(sys.argv[1:]),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True,
                              cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"), "note": args.note,
    })
    out = ROOT / "readings" / f"count-{reading['taken_at']}.json"
    out.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
