"""GSM8K, grade-school word problems: arithmetic as a tool, the third of step 3's tools.

`data/gsm8k/test.jsonl` is the original release (openai/grade-school-math on GitHub, 1,319
problems; fetched with curl, the directory is not tracked). The gold answer is the number after
`####`. Three turns for the trunk: answer with the number alone, think then answer, or write a
Python program whose last line prints the answer, which code runs. The deployed system has
every input here: the question.
"""

from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent.parent

DIRECT_SYSTEM = "Solve the problem. Reply with the final number only, nothing else."
THINK_SYSTEM = ("Solve the problem. Think it through, then end your reply with the final "
                "answer as a number on its own line after 'Answer:'.")
PYTHON_SYSTEM = (
    "Solve the problem by writing a short Python program. Use plain arithmetic, name the "
    "quantities, and make the last line print(answer) where answer is the final number. Reply "
    "with the program only, inside one ```python block, no words outside it."
)


def rows_of(path) -> list[dict]:
    """The JSON rows of a .jsonl file, split on newlines only: a reply can hold a Unicode line
    separator, which `str.splitlines` would cut a row at."""
    text = Path(path).read_text(encoding="utf-8")
    return [json.loads(line) for line in text.split("\n") if line.strip()]


def problems(n: int | None = None, split: str = "test", skip: int = 0) -> list[dict]:
    """The problems of a split, each {"question", "answer"} with the answer as the gold number,
    the first `skip` passed over. Readings are on "test"; "train" (7,473 problems, the same
    source) is for the system's own rollouts, so nothing it learns from is a problem it is read
    on, and a round of training takes the next problems after the round before."""
    out = []
    with open(ROOT / "data" / "gsm8k" / f"{split}.jsonl", encoding="utf-8") as f:
        for i, line in enumerate(f):
            if i < skip:
                continue
            if n is not None and i >= skip + n:
                break
            r = json.loads(line)
            out.append({"question": r["question"], "answer": gold_number(r["answer"]),
                        "solution": r["answer"]})
    return out


def gold_number(answer: str) -> str:
    return answer.rsplit("####", 1)[1].strip().replace(",", "")


def number_in(reply: str) -> str | None:
    """The answer a reply gives: what follows the last 'Answer:' if there is one, else the last
    number in the reply; commas and a trailing .0 dropped."""
    text = reply.rsplit("Answer:", 1)[1] if "Answer:" in reply else reply
    nums = re.findall(r"-?\d[\d,]*(?:\.\d+)?", text)
    if not nums:
        return None
    x = nums[0] if "Answer:" in reply else nums[-1]
    x = x.replace(",", "")
    return x[:-2] if x.endswith(".0") else x


def same_number(a: str | None, b: str) -> bool:
    if a is None:
        return False
    try:
        return abs(float(a) - float(b)) < 1e-6
    except ValueError:
        return False


def program_in(reply: str) -> str:
    """The Python inside the reply's code block, or the whole reply if there is none."""
    m = re.search(r"```(?:python)?\s*\n(.*?)```", reply, re.S)
    return (m.group(1) if m else reply).strip()


def run_program(code: str, timeout: float = 5.0) -> str | None:
    """Runs the trunk's program in a fresh interpreter with no inherited environment and a
    timeout; the number its last printed line holds, or None."""
    try:
        r = subprocess.run([sys.executable, "-I", "-c", code], capture_output=True, text=True,
                           timeout=timeout, env={"PYTHONIOENCODING": "utf-8"}, cwd=str(ROOT / "data"))
    except (subprocess.TimeoutExpired, OSError):
        return None
    lines = [x for x in r.stdout.strip().splitlines() if x.strip()]
    return number_in(lines[-1]) if lines else None


def messages(kind: str, question: str) -> list[dict]:
    system = {"direct": DIRECT_SYSTEM, "cot": THINK_SYSTEM, "python": PYTHON_SYSTEM}[kind]
    return [{"role": "system", "content": system}, {"role": "user", "content": question}]
