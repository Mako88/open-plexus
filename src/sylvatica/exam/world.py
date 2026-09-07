"""The told-then-asked worlds: a fictional house, delivered as a conversation.

WHY THE FACTS ARE INVENTED. A world made of real facts measures what the
checkpoint already knew, which is the one thing this branch is not asking about.
Every name here is built from syllables by a seeded generator, so "Vessarine
Tolmick keeps nine lanterns in the scullery" is a sentence no pretraining corpus
contains. If the core answers it, the core remembers it.

WHY THE BLIND BASELINE'S STRENGTH IS A PROPERTY OF THIS FILE. Standing objection
5: on a generated house, how strong "answer the commonest answer for the
question's kind" is depends entirely on how this generator distributes its
answers. Draw numbers from {one..five} and blind gets 20% free; draw from a
thousand and blind is worthless. Neither is honest unless it is REPORTED, so
`House.answer_entropy()` goes into every reading beside the score, and the blind
baseline is scored per kind rather than only in total.

WHY THERE ARE NEGATIVES FROM THE FIRST RUN. A core that confabulates fluently
scores like a core that remembers, unless something asks it about things nobody
mentioned. The rate of invented answers is scored SEPARATELY and is not netted
off the correct rate -- those are two different failures and averaging them
hides both.
"""

from __future__ import annotations

import math
import random
from collections import Counter
from dataclasses import dataclass, field

from . import Fact, House, Question

# Syllables that combine into names which read as English and are not English.
# Deliberately small: the point is that they are unfamiliar, not that they are
# unique. A collision inside one house is prevented by the generator, and a
# collision with the pretraining corpus is what the syllable soup is against.
_ONSETS = [
    "b", "d", "f", "g", "h", "k", "l", "m", "n", "p", "r", "s", "t", "v",
    "br", "dr", "fl", "gr", "kr", "pl", "sl", "st", "th", "tr", "vr",
]
_NUCLEI = ["a", "e", "i", "o", "u", "ae", "ea", "io", "ou", "ai"]
_CODAS = ["l", "m", "n", "r", "s", "th", "ck", "ff", "rn", "rk", "st", "nd"]

_ROOMS = [
    "scullery", "boot room", "morning room", "cellar", "attic", "pantry",
    "gun room", "still room", "laundry", "coach house", "dairy", "nursery",
]
_OBJECTS = [
    "lanterns", "flour sacks", "seed trays", "fishing floats", "candle moulds",
    "cracked plates", "walking sticks", "glass jars", "iron hooks", "wool cards",
]
_TRADES = [
    "repairs clocks", "keeps bees", "binds books", "grinds lenses", "shoes horses",
    "thatches roofs", "carves spoons", "dyes wool", "sets type", "mends nets",
]
_COLOURS = [
    "ochre", "slate", "russet", "verdigris", "oxblood", "bone", "indigo", "mustard",
]

# OBLIQUE PHRASINGS: the same question with none of the telling sentence's words.
#
# WHY THIS EXISTS, AND IT IS A MEASURED FAULT RATHER THAN A PRECAUTION. The
# direct questions are near-copies of the sentences that told the facts -- "How
# many lanterns are in the cellar?" about "There are 65 lanterns in the cellar"
# -- so every content word is shared and FTS5 alone finds the answer. Phase 2's
# first reading proved it: Tier B scored 0.988 with the LEXICAL RANKER ALONE and
# precision 1.000, slightly BETTER than the hybrid. The embeddings were
# contributing nothing, and the store was passing a keyword-lookup task while
# being credited with retrieval.
#
# So an oblique question keeps the ENTITY -- without it the question is
# unanswerable rather than merely harder -- and replaces every other content word
# with something a lexical index cannot match. "How many lamps are kept
# downstairs?" needs the store to know that lamps are lanterns and downstairs is
# the cellar, which is what an embedding is for and what nothing has yet tested.
_ROOM_SYNONYMS = {
    "scullery": "wash-up room", "boot room": "muddy porch", "morning room": "sunny sitting room",
    "cellar": "space downstairs", "attic": "loft", "pantry": "larder",
    "gun room": "shooting store", "still room": "preserving room", "laundry": "wash house",
    "coach house": "cart shed", "dairy": "milk store", "nursery": "children's room",
}
_OBJECT_SYNONYMS = {
    "lanterns": "lamps", "flour sacks": "grain bags", "seed trays": "planting flats",
    "fishing floats": "bobbers", "candle moulds": "wax forms", "cracked plates": "chipped dishes",
    "walking sticks": "canes", "glass jars": "preserving pots", "iron hooks": "metal pegs",
    "wool cards": "fleece combs",
}
_TRADE_SYNONYMS = {
    "repairs clocks": "mends timepieces", "keeps bees": "tends hives",
    "binds books": "sews volumes", "grinds lenses": "shapes optics",
    "shoes horses": "fits hooves", "thatches roofs": "lays reed",
    "carves spoons": "whittles utensils", "dyes wool": "colours fleece",
    "sets type": "arranges letterpress", "mends nets": "patches trawls",
}

# NUMBERS ARE DRAWN FROM A WIDE RANGE ON PURPOSE, and the range is a dial the
# reading records. A narrow range hands the blind baseline a large free score on
# every numeric question, which would make every arm look bad for a reason that
# is about this file rather than about memory.
_NUMBER_RANGE = (2, 97)

# Filler carries no facts and exists so the told sentences are separated by
# real conversation rather than by silence. Nothing here is ever asked about.
_FILLER = [
    "It has been raining all week.",
    "I think the kettle needs descaling again.",
    "There is a van parked badly at the end of the road.",
    "I slept badly and I blame the wind.",
    "Somebody has been leaving the gate open.",
    "The bread came out flat this time.",
    "I keep meaning to sort out the shed.",
    "It gets dark so early now.",
    "The post was late again this morning.",
    "I had that same dream about a staircase.",
    "The tap in the bathroom has started dripping.",
    "I should probably go for a walk later.",
]

# The delays a fact is asked at, in turns since it was told. The doc says
# "several delays"; these span from immediate to most of the conversation, so a
# curve can be read rather than a point.
DEFAULT_DELAYS = (1, 5, 20, 60, 150)


def _name(rng: random.Random) -> str:
    syllables = rng.choice([2, 2, 3])
    out = ""
    for i in range(syllables):
        out += rng.choice(_ONSETS) + rng.choice(_NUCLEI)
        if i == syllables - 1 or rng.random() < 0.4:
            out += rng.choice(_CODAS)
    return out.capitalize()


@dataclass
class Generated(House):
    """A `House` plus the bookkeeping a reading needs about how it was made."""

    n_turns: int = 0
    phrasing: str = "direct"
    delays: tuple[int, ...] = field(default=())
    told_at: dict[str, int] = field(default_factory=dict)  # fact id -> turn index

    def answer_entropy(self) -> dict[str, float]:
        """Bits of entropy in the answers of each kind. STANDING OBJECTION 5.

        A blind score is uninterpretable without this. Zero bits means every
        answer of that kind is the same and blind scores 100%; high bits means
        blind is close to worthless. Reported beside every exam reading so a low
        blind number can be told from a generous generator.
        """
        out: dict[str, float] = {}
        by_kind: dict[str, list[str]] = {}
        for fact in self.facts:
            by_kind.setdefault(fact.kind, []).append(fact.answer.lower())
        for kind, answers in by_kind.items():
            counts = Counter(answers)
            total = len(answers)
            out[kind] = round(
                -sum((c / total) * math.log2(c / total) for c in counts.values()), 3
            )
        return out

    def modal_answers(self) -> dict[str, str]:
        """The commonest answer per kind. This IS the blind baseline's table."""
        by_kind: dict[str, Counter] = {}
        for fact in self.facts:
            by_kind.setdefault(fact.kind, Counter())[fact.answer] += 1
        return {kind: counts.most_common(1)[0][0] for kind, counts in by_kind.items()}


def _make_facts(rng: random.Random, n_facts: int) -> list[Fact]:
    """Five kinds, dealt round-robin so no kind is rare enough to be noise."""
    people = [_name(rng) for _ in range(max(4, n_facts // 3))]
    rooms = list(_ROOMS)
    rng.shuffle(rooms)

    facts: list[Fact] = []
    used_subjects: set[tuple[str, str]] = set()

    makers = ["trade", "number", "place", "colour", "relation"]
    while len(facts) < n_facts:
        kind = makers[len(facts) % len(makers)]
        fid = f"f{len(facts):03d}"

        if kind == "trade":
            who = rng.choice(people)
            what = rng.choice(_TRADES)
            if (kind, who) in used_subjects:
                continue
            used_subjects.add((kind, who))
            facts.append(
                Fact(
                    id=fid,
                    kind=kind,
                    told=f"{who} {what} for a living.",
                    question=f"What does {who} do for a living?",
                    oblique=f"How does {who} earn a wage?",
                    answer=what.split()[1],  # "repairs clocks" -> "clocks"
                )
            )
        elif kind == "number":
            thing = rng.choice(_OBJECTS)
            room = rng.choice(rooms)
            n = rng.randint(*_NUMBER_RANGE)
            if (kind, thing + room) in used_subjects:
                continue
            used_subjects.add((kind, thing + room))
            facts.append(
                Fact(
                    id=fid,
                    kind=kind,
                    told=f"There are {n} {thing} in the {room}.",
                    question=f"How many {thing} are in the {room}?",
                    oblique=(
                        f"How many {_OBJECT_SYNONYMS.get(thing, thing)} sit in the "
                        f"{_ROOM_SYNONYMS.get(room, room)}?"
                    ),
                    answer=str(n),
                )
            )
        elif kind == "place":
            who = rng.choice(people)
            thing = rng.choice(_OBJECTS)
            room = rng.choice(rooms)
            if (kind, who + thing) in used_subjects:
                continue
            used_subjects.add((kind, who + thing))
            facts.append(
                Fact(
                    id=fid,
                    kind=kind,
                    told=f"{who} keeps the {thing} in the {room}.",
                    question=f"Where does {who} keep the {thing}?",
                    oblique=(
                        f"Which part of the house holds "
                        f"{who}'s {_OBJECT_SYNONYMS.get(thing, thing)}?"
                    ),
                    answer=room,
                )
            )
        elif kind == "colour":
            thing = rng.choice(_OBJECTS)
            colour = rng.choice(_COLOURS)
            if (kind, thing) in used_subjects:
                continue
            used_subjects.add((kind, thing))
            facts.append(
                Fact(
                    id=fid,
                    kind=kind,
                    told=f"The {thing} are {colour}.",
                    question=f"What colour are the {thing}?",
                    oblique=f"What shade are the {_OBJECT_SYNONYMS.get(thing, thing)}?",
                    answer=colour,
                )
            )
        else:  # relation
            a, b = rng.sample(people, 2)
            if (kind, a) in used_subjects:
                continue
            used_subjects.add((kind, a))
            facts.append(
                Fact(
                    id=fid,
                    kind=kind,
                    told=f"{a} is {b}'s cousin.",
                    question=f"Whose cousin is {a}?",
                    oblique=f"To whom is {a} related?",
                    answer=b,
                )
            )
    return facts


def generate_house(
    seed: int = 0,
    n_facts: int = 50,
    n_turns: int = 300,
    delays: tuple[int, ...] = DEFAULT_DELAYS,
    negatives: int = 20,
    phrasing: str = "direct",
) -> Generated:
    """A house of `n_facts` invented facts told across `n_turns` of conversation.

    THE FACTS ARE SPREAD OVER THE FIRST PART OF THE CONVERSATION so that every
    one of them can still be asked at the longest delay. A fact told at turn 290
    cannot be asked at a delay of 150 in a 300-turn conversation, and silently
    dropping those questions would make the long-delay column a sample of the
    EARLY facts only -- which is a different question than the one being asked.
    """
    rng = random.Random(seed)
    facts = _make_facts(rng, n_facts)

    longest = max(delays)
    last_tellable = n_turns - longest - 1
    if last_tellable < n_facts:
        raise ValueError(
            f"{n_facts} facts cannot all be told before turn {last_tellable} "
            f"({n_turns} turns minus the longest delay {longest}). Lengthen the "
            "conversation, shorten the delays, or tell fewer facts -- do not let "
            "the long-delay column quietly become a sample of the early facts."
        )

    # Told turns are drawn without replacement from the tellable prefix and then
    # sorted, so the facts arrive spread out and in a stable order for a seed.
    told_turns = sorted(rng.sample(range(1, last_tellable), n_facts))
    told_at = {fact.id: turn for fact, turn in zip(facts, told_turns)}

    turns: list[str] = []
    by_turn = {turn: fact for fact, turn in zip(facts, told_turns)}
    for t in range(n_turns):
        fact = by_turn.get(t)
        turns.append(fact.told if fact else rng.choice(_FILLER))

    questions: list[Question] = []
    for fact in facts:
        for delay in delays:
            at = told_at[fact.id] + delay
            if at >= n_turns:
                continue
            questions.append(
                Question(
                    fact_id=fact.id,
                    text=(fact.oblique or fact.question) if phrasing == "oblique"
                    else fact.question,
                    answer=fact.answer,
                    kind=fact.kind,
                    delay_turns=delay,
                )
            )

    # NEGATIVES: questions in the house's own shape about things never told. The
    # right answer is a refusal, and anything else is an invented answer.
    #
    # THEIR `kind` IS "place" AND NOT "negative", WHICH MATTERS. A negative that
    # announced itself in its kind would be answerable by any baseline that
    # looks at the kind -- the blind rule would refuse them all and score a
    # perfect zero invented answers, for free, by reading a label the core does
    # not get to see. They are place-shaped questions and they are labelled as
    # such; what makes one a negative is `fact_id is None`, which is bookkeeping
    # on the scorer's side of the line.
    for _ in range(negatives):
        who = _name(rng)
        thing = rng.choice(_OBJECTS)
        questions.append(
            Question(
                fact_id=None,
                text=f"Where does {who} keep the {thing}?",
                answer=None,
                kind="place",
                delay_turns=rng.choice(delays),
            )
        )

    house = Generated(seed=seed, facts=facts, turns=turns, questions=questions)
    house.phrasing = phrasing
    house.n_turns = n_turns
    house.delays = tuple(delays)
    house.told_at = told_at
    return house


def questions_at(house: Generated) -> dict[int, list[Question]]:
    """Which questions get asked after which turn. Keyed by turn index.

    A question about a fact told at turn `t` with delay `d` is asked once the
    conversation has reached turn `t + d`. Negatives are spread over the same
    turns so they are not all bunched at the end, where a core might have
    drifted into refusing everything.
    """
    at: dict[int, list[Question]] = {}
    for q in house.questions:
        if q.fact_id is None:
            continue
        turn = house.told_at[q.fact_id] + q.delay_turns
        at.setdefault(turn, []).append(q)

    negatives = [q for q in house.questions if q.fact_id is None]
    if negatives and at:
        slots = sorted(at)
        for i, q in enumerate(negatives):
            at[slots[(i * len(slots)) // len(negatives)]].append(q)
    return at
