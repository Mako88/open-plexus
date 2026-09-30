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
    "sentence, without articles, and keep each thing in its own slot: a number goes in "
    "quantity, a colour or a trait in object, never run together with the thing it "
    "describes. Write out what a pronoun such as 'it', 'them' or 'she' refers to. Small "
    "talk that states no lasting fact about a particular person or thing gives no "
    "assertions. Examples:\n"
    "'The hall has 12 chairs in it.' -> subject: chairs, relation: are in, place: hall, "
    "quantity: 12\n"
    "'Somebody painted the gate green.' -> subject: gate, relation: has colour, object: green\n"
    "'Mira keeps her bike in the shed.' -> subject: Mira, relation: keeps, object: bike, "
    "place: shed\n"
    "'Ada is a cousin of Mira.' -> subject: Ada, relation: is cousin of, object: Mira\n"
    "'The bike isn't in the shed now; Mira took it to the attic.' -> subject: Mira, "
    "relation: took to, object: bike, place: attic"
)


STEP = {
    "type": "object",
    "properties": {
        "subject": {"type": "string"},
        "relation": {"type": "string"},
        "object": {"type": "string"},
        "place": {"type": "string"},
        "quantity": {"type": "string"},
    },
    "required": ["subject", "relation"],
}

REWRITE_SCHEMA = {
    "type": "object",
    "properties": {
        "steps": {"type": "array", "items": STEP},
        "answer": {"type": "string"},
        "count": {"type": "boolean"},
    },
    "required": ["steps", "answer", "count"],
}

REWRITE = (
    "Turn a question into the facts that would answer it. Each fact is a step with a "
    "subject, a relation, and only the other parts it needs: object, place, quantity. Write "
    "each unknown as ?a, ?b, ?c, and reuse an unknown to link two steps. 'answer' is the "
    "unknown asked for; 'count' is true if the question asks how many. Examples:\n"
    'Where does Mira keep the kettle? -> {"steps": [{"subject": "Mira", "relation": '
    '"keeps", "object": "kettle", "place": "?a"}], "answer": "?a", "count": false}\n'
    'What does Mira do for a living? -> {"steps": [{"subject": "Mira", "relation": '
    '"works as", "object": "?a"}], "answer": "?a", "count": false}\n'
    'Who mends the fences? -> {"steps": [{"subject": "?a", "relation": "mends", '
    '"object": "fences"}], "answer": "?a", "count": false}\n'
    'What colour is the gate? -> {"steps": [{"subject": "gate", "relation": "has colour", '
    '"object": "?a"}], "answer": "?a", "count": false}\n'
    'How many chairs are in the hall? -> {"steps": [{"subject": "chairs", "relation": '
    '"are in", "place": "hall", "quantity": "?a"}], "answer": "?a", "count": false}\n'
    'What does Mira\'s cousin do for a living? -> {"steps": [{"subject": "Mira", '
    '"relation": "is cousin of", "object": "?a"}, {"subject": "?a", "relation": '
    '"works as", "object": "?b"}], "answer": "?b", "count": false}\n'
    'What colour is the bike the person who mends fences keeps in the shed? -> {"steps": '
    '[{"subject": "?a", "relation": "mends", "object": "fences"}, {"subject": "?a", '
    '"relation": "keeps", "object": "?b", "place": "shed"}, {"subject": "?b", "relation": '
    '"has colour", "object": "?c"}], "answer": "?c", "count": false}\n'
    'How many people keep things in the shed? -> {"steps": [{"subject": "?a", "relation": '
    '"keeps", "object": "?b", "place": "shed"}], "answer": "?a", "count": true}'
)

ASK_SCHEMA = {
    "type": "object",
    "properties": {"asked": {"type": "string",
                             "enum": ["subject", "object", "place", "quantity"]},
                   "kind": {"type": "string"},
                   "assertion": ASSERTION, "count": {"type": "boolean"}},
    "required": ["asked", "kind", "assertion", "count"],
}

ASK = (
    "Read a question from a conversation. Say which part of the fact it asks for (asked: "
    "subject, object, place or quantity), what kind of thing the answer is, in one word "
    "(kind: such as person, place, number, colour, job), and write down the rest of that "
    "fact as an assertion, leaving the asked part empty. An assertion has a subject, a "
    "relation (a short verb phrase in the present tense, such as 'keeps', 'is cousin of', "
    "'works as', 'has colour'), and what the relation takes: an object, a place, a quantity. "
    "Copy names and things from the question, without articles. 'count' is true if the "
    "question asks how many people or things. Examples:\n"
    "'Where does Mira keep the kettle?' -> asked: place; kind: place; subject: Mira, "
    "relation: keeps, object: kettle\n"
    "'What does Mira do for a living?' -> asked: object; kind: job; subject: Mira, "
    "relation: works as\n"
    "'Who mends the fences?' -> asked: subject; kind: person; relation: mends, object: "
    "fences\n"
    "'What colour is the gate?' -> asked: object; kind: colour; subject: gate, relation: "
    "has colour\n"
    "'How many chairs are in the hall?' -> asked: quantity; kind: number; subject: chairs, "
    "relation: are in, place: hall\n"
    "'How many people keep things in the shed?' -> asked: subject; kind: person; relation: "
    "keeps, place: shed, count: true"
)

SAME_SCHEMA = {
    "type": "object",
    "properties": {"same": {"type": "boolean"}},
    "required": ["same"],
}

CHOICE_SCHEMA = {
    "type": "object",
    "properties": {"choice": {"type": "integer"}},
    "required": ["choice"],
}


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

    def rewrite(self, question: str) -> dict | None:
        """A question as the statements that would answer it, with unknowns."""
        return self._call(REWRITE, question, REWRITE_SCHEMA, 200)

    def ask(self, question: str) -> dict | None:
        """A question read the way a statement is: the one fact it asks about, with
        '?' where the answer goes, and whether it asks how many. No steps and no
        chain: finding the way to that fact is the system's."""
        return self._call(ASK, question, ASK_SCHEMA, 120)

    def is_a(self, filler: str, kind: str, example: str = "") -> bool:
        """Whether a filler, going by the sentence it was heard in, could answer a question
        asking for a kind: 'ochre' a colour, 'spoons' in 'carves spoons for a living' a
        job. Asked of its use, not of the word alone: out of context an invented name is
        not a person to the 9B, and 'spoons' is not a job."""
        reply = self._call(
            f"Say whether, going by the sentence, the phrase could be the answer to a "
            f"question asking for a {kind}.",
            f"Sentence: '{example}'\nPhrase: '{filler}'\nAsked for: {kind}", SAME_SCHEMA, 80)
        return bool(reply and reply.get("same"))

    def choose(self, prompt: str, options: list[str]) -> int | None:
        """Which option `prompt` means, or None. Word meaning, and nothing composed."""
        listing = "\n".join(f"{i}. {o}" for i, o in enumerate(options))
        reply = self._call(
            "Answer with the number of the option that means the same as what is asked "
            "about, or -1 if none does.", f"{prompt}\n\n{listing}", CHOICE_SCHEMA, 40)
        if not reply:
            return None
        choice = reply.get("choice")
        return choice if isinstance(choice, int) and 0 <= choice < len(options) else None

    def synonymous(self, asked: str, stored: str, example: str = "") -> bool:
        """Whether a fact stated with one relation answers a question asked with the other.

        Judged on an example of the stored relation in use, because out of context
        'put' and 'keeps' are different words and in 'Ada put the kettle in the shed'
        the kettle is kept in the shed."""
        seen = f"\nFor example: '{example}'" if example else ""
        reply = self._call(
            "Say whether a fact stated with the second relation also tells you the first "
            "relation holds between the same things, as 'put the kettle in the shed' tells "
            "you the kettle is kept in the shed.",
            f"First: '{asked}'\nSecond: '{stored}'{seen}", SAME_SCHEMA, 40)
        return bool(reply and reply.get("same"))

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
    """Whether one assertion holds every gold filler, each in a slot of its own.

    One assertion, not the union of several, because the system matches one
    row at a time. Each filler in its own slot, because "cracked plates indigo"
    as one subject holds both words and says nothing the system can use: the
    first scorer counted it whole, and read 0.98 where the ear was worse.
    """
    from itertools import permutations

    wanted = [w.lower() for w in gold(fact)]

    def whole(a: dict) -> bool:
        slots = [str(v).lower() for v in a.values() if v]
        if len(slots) < len(wanted):
            return False
        return any(all(w in slot for w, slot in zip(wanted, chosen))
                   for chosen in permutations(slots, len(wanted)))

    return {"whole": any(whole(a) for a in assertions), "read": len(assertions)}


def words(text: str) -> set[str]:
    return set(re.findall(r"[a-z']+", text.lower()))
