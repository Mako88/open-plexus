"""The ears: a sentence read into typed assertions, a question into a query.

The schema is generic rather than the house's. An assertion is a subject, a
relation, and whatever the relation takes: an object, a place, a quantity. A
question is the same shape with one filler asked for. Nothing here names a
room, a trade or a cousin.

The output is forced into the schema by the server's grammar, so it always
parses. That guarantees the shape and nothing else: an assertion can be
well-formed and wrong, which is why `score_reading` exists.
"""

from __future__ import annotations

import json
import re
import urllib.request
from dataclasses import dataclass, field

ASSERTION = {
    "type": "object",
    "properties": {
        "subject": {"type": "string"},
        "relation": {"type": "string"},
        "object": {"type": ["string", "null"]},
        "place": {"type": ["string", "null"]},
        "quantity": {"type": ["string", "null"]},
    },
    "required": ["subject", "relation", "object", "place", "quantity"],
}

READING = {
    "type": "object",
    "properties": {"assertions": {"type": "array", "items": ASSERTION}},
    "required": ["assertions"],
}

READ = (
    "Read one sentence from a conversation and write down the lasting facts it states, "
    "as assertions. An assertion has a subject, a relation (a short verb phrase in the "
    "present tense, such as 'keeps', 'is cousin of', 'works as', 'has colour'), and what "
    "the relation takes: an object, a place, a quantity. Copy names and things from the "
    "sentence, without articles. Small talk that states no lasting fact about a particular "
    "person or thing gives no assertions."
)


QUERY = {
    "type": "object",
    "properties": {
        "steps": {"type": "array", "items": ASSERTION},
        "answer": {"type": "string"},
        "count": {"type": "boolean"},
    },
    "required": ["steps", "answer", "count"],
}

ASK = (
    "Turn a question into a query over assertions. An assertion has a subject, a relation, "
    "and what the relation takes: an object, a place, a quantity. Write each thing the "
    "question needs to know as a step shaped like an assertion, with every unknown written "
    "as ?a, ?b, ?c. The same unknown in two steps is the same thing, which is how steps "
    "chain. 'answer' is the unknown the question asks for. Set 'count' true if the question "
    "asks how many. Where the question means one of the known relations or names listed, "
    "use it exactly as listed."
)


def known_block(relations: list[str], names: list[str]) -> str:
    lines = []
    if relations:
        lines.append("Known relations: " + "; ".join(relations))
    if names:
        lines.append("Known names: " + "; ".join(names))
    return "\n".join(lines)


@dataclass
class Ear:
    """An ear behind an OpenAI-compatible server that honours a JSON schema."""

    url: str = "http://127.0.0.1:8094/v1/chat/completions"
    name: str = "Qwen3.5-2B-Q8_0 (llama.cpp, schema)"
    calls: int = 0
    seconds: float = 0.0
    failures: list = field(default_factory=list)

    def read(self, sentence: str, relations: list[str] | None = None) -> list[dict]:
        """A sentence into assertions, reusing a known relation where it means the same."""
        system = READ
        if relations:
            system += (" Where a relation means the same as a known one, use the known one "
                       "exactly.\n" + known_block(relations, []))
        reply = self._call(system, sentence, READING, 300)
        return reply["assertions"] if reply else []

    def ask(self, question: str, relations: list[str], names: list[str]) -> dict | None:
        """A question into a query: steps with unknowns, the unknown wanted, and whether
        it asks how many."""
        user = f"{known_block(relations, names)}\n\nQuestion: {question}"
        return self._call(ASK, user, QUERY, 300)

    def _call(self, system: str, user: str, schema: dict, budget: int) -> dict | None:
        import time

        body = json.dumps({
            "messages": [{"role": "system", "content": system},
                         {"role": "user", "content": user}],
            "max_tokens": budget, "temperature": 0, "seed": 0,
            "response_format": {"type": "json_schema",
                                "json_schema": {"name": "out", "schema": schema}},
        }).encode()
        request = urllib.request.Request(self.url, body, {"Content-Type": "application/json"})
        started = time.perf_counter()
        with urllib.request.urlopen(request, timeout=600) as response:
            reply = json.loads(response.read())
        self.calls += 1
        self.seconds += time.perf_counter() - started
        text = reply["choices"][0]["message"].get("content") or ""
        try:
            return json.loads(text)
        except json.JSONDecodeError:
            self.failures.append(text)
            return None


def gold(fact) -> list[str]:
    """What a correct reading of a fact's telling must contain, as fillers.

    Scored by containment in the reading's fillers, lower-cased. The relation's
    wording is not scored, because any phrasing that keeps the right things
    together is a correct reading; which things go together is what is scored.
    """
    kind = fact.kind
    if kind == "trade":
        return [fact.subject, fact.answer]
    if kind == "number":
        return [fact.subject, fact.fields["room"], fact.answer]
    if kind == "place":
        return [fact.subject, fact.fields["thing"], fact.answer]
    if kind == "colour":
        return [fact.subject, fact.answer]
    return [fact.subject, fact.answer, "cousin"]


def _fillers(assertion: dict) -> str:
    return " | ".join(str(v).lower() for v in assertion.values() if v)


def score_reading(fact, assertions: list[dict]) -> dict:
    """Whether one assertion holds every gold filler, and how many assertions came back.

    One assertion, not the union of several: the system matches assertions one
    at a time, so a fact split across two that share nothing it can join on is
    a fact it cannot use.
    """
    wanted = [w.lower() for w in gold(fact)]
    whole = any(all(w in _fillers(a) for w in wanted) for a in assertions)
    return {"whole": whole, "assertions": len(assertions)}


def words(text: str) -> set[str]:
    return set(re.findall(r"[a-z']+", text.lower()))
