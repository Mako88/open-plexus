"""The decomposing loop: reasoning outside the trunk. The trunk is a translator of sentences and
answers a single-fact question well (step 7), and what it gets wrong given the right passages is
the structure around the sentence: which slot of the question is wanted, and holding two facts
together (read 2026-11-04 on the gold arm's 78 misses: 32 wording, about 30 the wrong slot, about
15 composition). So the loop takes that structure over. The trunk writes a plan: the kind of thing
the final answer is, in its own words, and the question as single-fact questions in order, a later
one naming an earlier answer as #1, #2. Code runs the plan: each question is asked with the
passages, the answers substituted forward, the last answer is the answer. Code checks only the
system against itself: an answer that contradicts the kind the system predicted (it said a
person and answered yes) is asked once more, with the system's own kind in front of it. Nothing
of the answer's kind is handed to it from outside; in a training round the key rewards the plans
that fill the right kind, and the reading is taken with the key off (John, 2026-11-04). A plan
that does not parse falls back to the question asked whole. Self-ask (Press et al., 2022) is the
shape.
"""

from __future__ import annotations

import re

from openplexus.hotpot import READ_SYSTEM, answer_of

PLAN_SYSTEM = (
    "You will be given passages and a question. Do not answer the question yet. Write a plan for answering it: "
    "first the kind of thing the final answer is, then the question broken into the smallest questions that each "
    "ask for one fact, in the order they must be answered, where a later question may use the answer of an earlier "
    "one by writing #1, #2 and so on. One question is enough when the question already asks for one fact. Reply in "
    "exactly this form and nothing else:\nKind: <a person, a place, a date, a number, yes or no, or another kind in a word or two>\n"
    "1. <first question>\n2. <second question, which may use #1>"
)
YES_NO = {"yes", "no"}


def parse_plan(text: str) -> dict | None:
    """The kind and the numbered questions, or None where the trunk did not write the form."""
    kind, steps = None, []
    for line in text.splitlines():
        line = line.strip()
        m = re.match(r"^kind\s*:\s*(.+)$", line, re.I)
        if m and kind is None:
            kind = m.group(1).strip().rstrip(".")
            continue
        m = re.match(r"^(\d+)[.)]\s*(.+)$", line)
        if m:
            steps.append(m.group(2).strip())
    if not steps or len(steps) > 6:
        return None
    return {"kind": kind or "", "steps": steps}


def substitute(step: str, answers: list[str]) -> str:
    """#k in a step becomes the k-th answer so far; a reference to an answer not yet given stays."""
    def fill(m):
        k = int(m.group(1))
        return answers[k - 1] if 0 < k <= len(answers) and answers[k - 1] else m.group(0)
    return re.sub(r"#(\d+)", fill, step)


def contradicts(kind: str, answer: str | None) -> bool:
    """Whether the answer's shape contradicts the kind the system itself predicted: only the shapes
    code can tell without knowledge, yes-or-no against not, and a date or number against no digit."""
    if not answer:
        return True
    k = kind.lower()
    a = answer.strip().lower().rstrip(".")
    wants_yes_no = "yes" in k or "no" in k.split() or "whether" in k or "boolean" in k
    if wants_yes_no:
        return a not in YES_NO
    if a in YES_NO:
        return True
    if any(w in k for w in ("date", "year", "number", "count", "how many", "age", "amount")):
        return not re.search(r"\d", a)
    return False


def passages_text(passages: list[str], question: str) -> str:
    if not passages:
        return question
    return "Passages:\n\n" + "\n\n".join(passages) + "\n\nQuestion: " + question


def solve_decomposed(ask, passages: list[str], question: str) -> dict:
    """`ask(messages) -> str` is one greedy turn of the trunk. Returns the reply as an 'Answer: X'
    line with the plan, the questions as asked, their answers, and whether the loop fell back or
    asked the last question again."""
    plan_text = ask([{"role": "system", "content": PLAN_SYSTEM}, {"role": "user", "content": passages_text(passages, question)}])
    plan = parse_plan(plan_text)
    if plan is None:
        reply = ask([{"role": "system", "content": READ_SYSTEM}, {"role": "user", "content": passages_text(passages, question)}])
        return {"reply": reply, "plan": None, "plan_text": plan_text, "steps": [], "answers": [], "fallback": True, "retried": False, "turns": 2}
    asked, answers = [], []
    for step in plan["steps"]:
        q = substitute(step, answers)
        asked.append(q)
        reply = ask([{"role": "system", "content": READ_SYSTEM}, {"role": "user", "content": passages_text(passages, q)}])
        answers.append(answer_of(reply) or "")
    # the question itself, asked last with the facts the plan found written out, so the composing is
    # done with the parts explicit (self-ask's "so the final answer is")
    facts = "\n".join(f"- {q} {a}" for q, a in zip(asked, answers) if a)
    final_q = (f"{question}\n\nFacts already found:\n{facts}" if facts else question)
    reply = ask([{"role": "system", "content": READ_SYSTEM}, {"role": "user", "content": passages_text(passages, final_q)}])
    final = answer_of(reply) or ""
    retried = False
    if contradicts(plan["kind"], final):
        retried = True
        q = final_q + (f"\n\n(The answer is {plan['kind']}.)" if plan["kind"] else "")
        reply = ask([{"role": "system", "content": READ_SYSTEM}, {"role": "user", "content": passages_text(passages, q)}])
        again = answer_of(reply)
        if again:
            final = again
    return {"reply": f"Answer: {final}", "plan": plan, "plan_text": plan_text, "steps": asked, "answers": answers,
            "fallback": False, "retried": retried, "turns": 2 + len(asked) + int(retried)}
