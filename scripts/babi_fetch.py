"""Build data/babi/babi_{train,test}.jsonl from the original bAbI archive.

The file this replaced came from a third-party repackaging on Hugging Face (Muennighoff/babi)
and is identical to the original on tasks 1 to 3, row for row; this one is built from the
archive itself so nothing stands between the data and its source, and it keeps each
question's story index and supporting line numbers, which step 2 trains on. A story opening
with "Mary got the milk there" is the original generator's habit and not a truncation: no
answer depends on the unnamed location, since a later move always decides it.

    .venv/Scripts/python -I scripts/babi_fetch.py [--dir <scratch dir for the archive>]

Rows: {"passage": "...\n", "question": "...", "answer": "...", "task": N, "story": K,
"supporting": [line numbers]}. The 1k English set (tasks_1-20_v1-2/en), 1,000 test questions a
task, as the original paper's test split.
"""

from __future__ import annotations

import argparse
import io
import json
import tarfile
import urllib.request
from pathlib import Path

# The original host (thespermwhale.com/jaseweston/babi) is gone; this is ParlAI's mirror of the
# same archive, redirecting to dl.fbaipublicfiles.com.
URL = "http://parl.ai/downloads/babi/babi.tar.gz"
ROOT = Path(__file__).resolve().parent.parent


def parse(text: str, task: int) -> list[dict]:
    rows: list[dict] = []
    lines: list[str] = []
    story = -1
    for raw in text.splitlines():
        if not raw.strip():
            continue
        number, _, rest = raw.partition(" ")
        if number == "1":
            story += 1
            lines = []
        if "\t" in rest:
            question, answer, supporting = rest.split("\t")
            rows.append({
                "passage": "".join(line + "\n" for line in lines),
                "question": question.strip(),
                "answer": answer.strip(),
                "task": task,
                "story": story,
                "supporting": [int(s) for s in supporting.split()],
            })
        else:
            lines.append(rest.strip())
    return rows


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--dir", default=None, help="where the archive is kept; default is a "
                   "fresh folder under the user's temp directory")
    p.add_argument("--set", default="en", choices=["en", "en-10k"])
    args = p.parse_args()
    keep = Path(args.dir) if args.dir else Path.home() / "AppData" / "Local" / "Temp" / "babi"
    keep.mkdir(parents=True, exist_ok=True)
    archive = keep / "babi.tar.gz"
    if not archive.exists():
        print("fetching", URL)
        with urllib.request.urlopen(URL, timeout=120) as resp:
            archive.write_bytes(resp.read())
    out = ROOT / "data" / "babi"
    out.mkdir(parents=True, exist_ok=True)
    with tarfile.open(archive) as tar:
        members = {m.name: m for m in tar.getmembers()}
        for split in ("train", "test"):
            rows: list[dict] = []
            for task in range(1, 21):
                part = f"/{args.set}/qa{task}_"
                name = next(n for n in members if part in n and n.endswith(f"_{split}.txt"))
                text = io.TextIOWrapper(tar.extractfile(members[name]), encoding="utf-8").read()
                rows.extend(parse(text, task))
            path = out / f"babi_{split}.jsonl"
            with open(path, "w", encoding="utf-8", newline="\n") as f:
                for r in rows:
                    f.write(json.dumps(r) + "\n")
            print(f"wrote {path}: {len(rows)} rows")


if __name__ == "__main__":
    main()
