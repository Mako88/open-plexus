"""The told-then-asked house: invented facts delivered as a conversation.

The names are built from syllables by a seeded generator, so no pretraining
corpus contains "Vessarine keeps the lanterns in the scullery". If the machine
answers it, the machine remembered it.

What a real conversation holds and the earlier exam did not ask, each a form of
question rather than a switch:

- `direct`: the telling sentence's own words turned into a question.
- `oblique`: the entity kept and every other content word swapped, so a keyword
  index cannot match it.
- `reverse`: asked from the other end. Told "Vessarine repairs clocks", asked
  "Who repairs clocks?". Language models fine-tuned on one direction famously
  fail the other.
- `twohop`: needs two facts told at different times. "What does Vessarine's
  cousin do for a living?"
- `update`: a place fact that later changes. The right answer is the new place,
  and naming the old one is scored as stale, apart from wrong.
- `denied`: a place fact followed by a telling that someone does not keep the
  thing in another room. The right answer is still the first room, and naming the
  denied one is scored as misled, apart from wrong.
- `hedged`: a place fact followed by a move that might happen or is only planned.
  Nothing moved, so the right answer is still the first room, and naming the
  planned one is scored as misled.
- negatives: questions in the house's shape about people never mentioned. The
  right answer is a refusal; anything else is an invention, scored apart.

Every house has all of these in fixed proportions. The house has a size (facts,
turns) and no switches.

The blind baseline's strength depends on how answers are distributed here, so
`answer_entropy` goes in every reading beside the scores.
"""

from __future__ import annotations

import math
import random
from collections import Counter
from dataclasses import dataclass, field

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
# A trade is (what the person does, the word the answer is scored on).
_TRADES = [
    ("repairs clocks", "clocks"), ("keeps bees", "bees"), ("binds books", "books"),
    ("grinds lenses", "lenses"), ("shoes horses", "horses"), ("thatches roofs", "roofs"),
    ("carves spoons", "spoons"), ("dyes wool", "wool"), ("sets type", "type"),
    ("mends nets", "nets"),
]
_COLOURS = ["ochre", "slate", "russet", "verdigris", "oxblood", "bone", "indigo", "mustard"]

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

# How a fact is told. Several surfaces per kind, because people do not say a
# thing the same way twice, and one surface per kind lets a keyword index align
# every telling with every question for free.
_TELL = {
    "trade": ["{who} {what} for a living.", "These days {who} {what}.",
              "You know {who}? {who} {what}, that's the work."],
    "number": ["There are {n} {thing} in the {room}.", "I counted {n} {thing} in the {room}.",
               "The {room} has {n} {thing} in it now."],
    "place": ["{who} keeps the {thing} in the {room}.",
              "The {thing} are {who}'s, and they live in the {room}.",
              "{who} put the {thing} in the {room} for safekeeping."],
    "colour": ["The {thing} are {colour}.", "Someone painted the {thing} {colour}.",
               "All the {thing} are a sort of {colour} colour."],
    "relation": ["{a} is {b}'s cousin.", "{a} and {b} are cousins, {a} on the other side.",
                 "I found out {a} is a cousin of {b}."],
    "update": ["{who} has moved the {thing} to the {room}.",
               "The {thing} aren't in the old spot any more; {who} took them to the {room}."],
    "denied": ["{who} doesn't keep the {thing} in the {room}.",
               "No, {who} does not keep the {thing} in the {room}.",
               "The {thing} were never in the {room}; {who} didn't put them there."],
    "hedged": ["{who} might move the {thing} to the {room}.",
               "{who} is thinking of moving the {thing} to the {room}.",
               "{who} will probably take the {thing} to the {room} next spring."],
}

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

DEFAULT_DELAYS = (1, 5, 20, 60, 150)
_NUMBER_RANGE = (2, 97)


@dataclass(frozen=True)
class Fact:
    id: str
    kind: str  # trade | number | place | colour | relation
    subject: str  # the person, or the thing for number and colour
    told: str
    answer: str
    fields: dict = field(default_factory=dict, hash=False, compare=False)


@dataclass(frozen=True)
class Question:
    text: str
    answer: str | None  # None for a negative
    kind: str  # the kind of ANSWER: what the blind rule keys on
    form: str  # direct | oblique | reverse | twohop | update | denied | hedged | ...
    delay: int  # turns since the last telling it depends on
    asked_at: int  # the turn after which it is asked
    # for an update, the answer that used to be right; for a denied or hedged fact,
    # the room the misleading telling named
    stale: str | None = None
    needs: tuple[str, ...] = ()  # fact ids it depends on


@dataclass
class House:
    seed: int
    n_turns: int
    facts: list[Fact]
    turns: list[str]
    questions: list[Question]
    told_at: dict[str, int]

    def answer_entropy(self) -> dict[str, float]:
        """Bits of entropy in the answers of each kind; zero means blind scores 100%."""
        out: dict[str, float] = {}
        for kind, answers in self._answers_by_kind().items():
            counts = Counter(answers)
            total = len(answers)
            out[kind] = round(-sum((c / total) * math.log2(c / total)
                                   for c in counts.values()), 3)
        return out

    def modal_answers(self) -> dict[str, str]:
        """The commonest answer per kind of answer in this house."""
        return modal_answers([self])

    def _answers_by_kind(self) -> dict[str, list[str]]:
        by: dict[str, list[str]] = {}
        for q in self.questions:
            if q.answer is not None:
                by.setdefault(q.kind, []).append(q.answer.lower())
        return by

    def fingerprint(self) -> str:
        """A hash of what was said and asked. Two readings compare only if it matches,
        and a reading on a house the generator no longer makes is history, not a baseline."""
        import hashlib

        text = "\n".join(self.turns) + "\n" + "\n".join(
            f"{q.text}|{q.answer}|{q.form}|{q.asked_at}" for q in self.questions)
        return hashlib.sha256(text.encode()).hexdigest()[:16]

    def questions_after(self) -> dict[int, list[Question]]:
        at: dict[int, list[Question]] = {}
        for q in self.questions:
            at.setdefault(q.asked_at, []).append(q)
        return at


def modal_answers(houses: list[House]) -> dict[str, str]:
    """The commonest answer per kind across houses: the blind rule's table, built from
    houses other than the one it is examined on."""
    by: dict[str, Counter] = {}
    for h in houses:
        for q in h.questions:
            if q.answer is not None:
                by.setdefault(q.kind, Counter())[q.answer.lower()] += 1
    return {k: c.most_common(1)[0][0] for k, c in by.items()}


def _name(rng: random.Random) -> str:
    syllables = rng.choice([2, 2, 3])
    out = ""
    for i in range(syllables):
        out += rng.choice(_ONSETS) + rng.choice(_NUCLEI)
        if i == syllables - 1 or rng.random() < 0.4:
            out += rng.choice(_CODAS)
    return out.capitalize()


def _people(rng: random.Random, n: int) -> list[str]:
    names: list[str] = []
    while len(names) < n:
        candidate = _name(rng)
        if candidate not in names:
            names.append(candidate)
    return names


def _make_facts(rng: random.Random, n_facts: int, people: list[str]) -> list[Fact]:
    """Five kinds dealt round-robin. One fact per (kind, subject), so every
    question has one right answer."""
    facts: list[Fact] = []
    used: set[tuple[str, str]] = set()
    kinds = ["trade", "number", "place", "colour", "relation"]
    attempts = 0
    while len(facts) < n_facts:
        attempts += 1
        if attempts > n_facts * 200:
            raise ValueError(f"cannot deal {n_facts} distinct facts from this vocabulary")
        kind = kinds[len(facts) % len(kinds)]
        fid = f"f{len(facts):03d}"
        template = rng.choice(_TELL[kind])
        if kind == "trade":
            who = rng.choice(people)
            what, word = rng.choice(_TRADES)
            if (kind, who) in used:
                continue
            used.add((kind, who))
            facts.append(Fact(fid, kind, who, template.format(who=who, what=what), word,
                              {"what": what}))
        elif kind == "number":
            thing, room = rng.choice(_OBJECTS), rng.choice(_ROOMS)
            if (kind, thing + room) in used:
                continue
            used.add((kind, thing + room))
            n = rng.randint(*_NUMBER_RANGE)
            facts.append(Fact(fid, kind, thing, template.format(n=n, thing=thing, room=room),
                              str(n), {"room": room}))
        elif kind == "place":
            who, thing, room = rng.choice(people), rng.choice(_OBJECTS), rng.choice(_ROOMS)
            if (kind, who + thing) in used:
                continue
            used.add((kind, who + thing))
            facts.append(Fact(fid, kind, who, template.format(who=who, thing=thing, room=room),
                              room, {"thing": thing}))
        elif kind == "colour":
            thing, colour = rng.choice(_OBJECTS), rng.choice(_COLOURS)
            if (kind, thing) in used:
                continue
            used.add((kind, thing))
            facts.append(Fact(fid, kind, thing, template.format(thing=thing, colour=colour),
                              colour))
        else:
            a, b = rng.sample(people, 2)
            # A person is in at most one cousin fact, either side, so "a's cousin"
            # names exactly one person.
            if ("relation", a) in used or ("relation", b) in used:
                continue
            used.add(("relation", a))
            used.add(("relation", b))
            facts.append(Fact(fid, kind, a, template.format(a=a, b=b), b))
    return facts


def generate_house(
    seed: int = 0,
    n_facts: int = 50,
    n_turns: int = 300,
    delays: tuple[int, ...] = DEFAULT_DELAYS,
    negatives: int = 20,
    update_share: float = 0.4,
    denied_share: float = 0.2,
    hedged_share: float = 0.2,
) -> House:
    """A house of `n_facts` invented facts told across `n_turns` of conversation.

    Facts are told in the first part of the conversation so every one can be
    asked at the longest delay; otherwise the long-delay column would quietly
    become a sample of the early facts.
    """
    rng = random.Random(seed)
    people = _people(rng, max(8, (n_facts * 2) // 5))
    facts = _make_facts(rng, n_facts, people)
    by_id = {f.id: f for f in facts}

    longest = max(delays)
    last_tellable = n_turns - longest - 1
    places = [f for f in facts if f.kind == "place"]
    n_updates = int(len(places) * update_share)
    if last_tellable < n_facts + n_updates + 1:
        raise ValueError(
            f"{n_facts} facts and {n_updates} updates cannot all be told before turn "
            f"{last_tellable}; lengthen the conversation or shorten the delays."
        )

    told_turns = sorted(rng.sample(range(1, last_tellable), n_facts))
    told_at = {f.id: t for f, t in zip(facts, told_turns)}
    events: dict[int, str] = {told_at[f.id]: f.told for f in facts}

    # Updates: a place fact moves to another room some turns after it was told.
    updates: dict[str, tuple[str, int]] = {}  # fact id -> (new room, turn)
    for fact in rng.sample(places, n_updates):
        free = [t for t in range(told_at[fact.id] + 3, last_tellable) if t not in events]
        if not free:
            continue
        turn = rng.choice(free)
        room = rng.choice([r for r in _ROOMS if r != fact.answer])
        events[turn] = rng.choice(_TELL["update"]).format(
            who=fact.subject, thing=fact.fields["thing"], room=room)
        updates[fact.id] = (room, turn)

    # Denials and hedges: a place fact followed by a telling that names another room
    # and moves nothing. Drawn apart, so every other telling and question keeps its draw.
    unsaid_rng = random.Random(f"{seed}-unsaid")
    still = [f for f in places if f.id not in updates]
    n_denied = int(len(places) * denied_share)
    n_hedged = int(len(places) * hedged_share)
    unsaid: dict[str, tuple[str, str, int]] = {}  # fact id -> (form, room named, turn)
    chosen = unsaid_rng.sample(still, min(len(still), n_denied + n_hedged))
    for k, fact in enumerate(chosen):
        form = "denied" if k < n_denied else "hedged"
        free = [t for t in range(told_at[fact.id] + 3, last_tellable) if t not in events]
        if not free:
            continue
        turn = unsaid_rng.choice(free)
        room = unsaid_rng.choice([r for r in _ROOMS if r != fact.answer])
        events[turn] = unsaid_rng.choice(_TELL[form]).format(
            who=fact.subject, thing=fact.fields["thing"], room=room)
        unsaid[fact.id] = (form, room, turn)

    turns = [events.get(t, rng.choice(_FILLER)) for t in range(n_turns)]
    questions: list[Question] = []

    def ask(text, answer, kind, form, since, delay, needs, stale=None):
        at = since + delay
        if at < n_turns:
            questions.append(Question(text, answer, kind, form, delay, at, stale, needs))

    # Direct and oblique alternate over a fact's delays, so both forms are read
    # at every delay across the house without doubling the question count.
    for i, fact in enumerate(facts):
        if fact.id in updates or fact.id in unsaid:
            continue  # asked below, as an update, a denial or a hedge
        direct, oblique = _phrasings(fact)
        for j, delay in enumerate(delays):
            form = "direct" if (i + j) % 2 == 0 else "oblique"
            ask(direct if form == "direct" else oblique, fact.answer, _answer_kind(fact),
                form, told_at[fact.id], delay, (fact.id,))

    for fid, (room, turn) in updates.items():
        fact = by_id[fid]
        text = f"Where does {fact.subject} keep the {fact.fields['thing']} now?"
        for delay in delays:
            ask(text, room, "room", "update", turn, delay, (fid,), stale=fact.answer)

    # a denial is asked as the fact was; a hedge as a move is, since after hearing of a
    # move that might happen the question a person asks is where the thing is now
    for fid, (form, room, turn) in unsaid.items():
        fact = by_id[fid]
        direct, oblique = _phrasings(fact)
        now = f"Where does {fact.subject} keep the {fact.fields['thing']} now?"
        for j, delay in enumerate(delays):
            text = now if form == "hedged" else direct if j % 2 == 0 else oblique
            ask(text, fact.answer, "room", form, turn, delay, (fid,), stale=room)

    # Reverse, only where the answer is unique in the house.
    trade_count = Counter(f.fields["what"] for f in facts if f.kind == "trade")
    place_count = Counter((f.fields["thing"], f.answer) for f in facts if f.kind == "place")
    for fact in facts:
        if fact.kind == "trade" and trade_count[fact.fields["what"]] == 1:
            text = f"Who {fact.fields['what']}?"
        elif fact.kind == "relation":
            text = f"Who is {fact.answer}'s cousin?"
        elif (fact.kind == "place" and fact.id not in updates
              and place_count[(fact.fields["thing"], fact.answer)] == 1):
            text = f"Who keeps the {fact.fields['thing']} in the {fact.answer}?"
        else:
            continue
        for delay in delays[1::2]:
            ask(text, fact.subject, "person", "reverse", told_at[fact.id], delay, (fact.id,))

    # Two hops: a cousin's trade, and the colour of what someone keeps.
    trade_of = {f.subject: f for f in facts if f.kind == "trade"}
    colour_of = {f.subject: f for f in facts if f.kind == "colour"}
    for fact in facts:
        if fact.kind == "relation" and fact.answer in trade_of:
            second = trade_of[fact.answer]
            text = f"What does {fact.subject}'s cousin do for a living?"
            answer, kind = second.answer, "trade"
        elif (fact.kind == "place" and fact.id not in updates
              and fact.fields["thing"] in colour_of):
            second = colour_of[fact.fields["thing"]]
            text = f"What colour are the things {fact.subject} keeps in the {fact.answer}?"
            answer, kind = second.answer, "colour"
        else:
            continue
        since = max(told_at[fact.id], told_at[second.id])
        for delay in delays[1::2]:
            ask(text, answer, kind, "twohop", since, delay, (fact.id, second.id))

    # Three in a row: found from a trade, through the person, to something about
    # what they keep or who they are related to.
    cousin_of = {f.subject: f for f in facts if f.kind == "relation"}
    for trade in facts:
        if trade.kind != "trade" or trade_count[trade.fields["what"]] != 1:
            continue
        who, what = trade.subject, trade.fields["what"]
        for place in facts:
            if (place.kind != "place" or place.subject != who or place.id in updates
                    or place.fields["thing"] not in colour_of):
                continue
            colour = colour_of[place.fields["thing"]]
            text = (f"What colour are the things the person who {what} keeps in the "
                    f"{place.answer}?")
            needs = (trade.id, place.id, colour.id)
            since = max(told_at[n] for n in needs)
            for delay in delays:
                ask(text, colour.answer, "colour", "chain3", since, delay, needs)
        if who in cousin_of and cousin_of[who].answer in trade_of:
            relation = cousin_of[who]
            second = trade_of[relation.answer]
            text = f"What does the cousin of the person who {what} do for a living?"
            needs = (trade.id, relation.id, second.id)
            since = max(told_at[n] for n in needs)
            for delay in delays:
                ask(text, second.answer, "trade", "chain3", since, delay, needs)
    for relation in (f for f in facts if f.kind == "relation"):
        for place in facts:
            if (place.kind != "place" or place.subject != relation.answer
                    or place.id in updates or place.fields["thing"] not in colour_of):
                continue
            colour = colour_of[place.fields["thing"]]
            text = (f"What colour are the things {relation.subject}'s cousin keeps in the "
                    f"{place.answer}?")
            needs = (relation.id, place.id, colour.id)
            since = max(told_at[n] for n in needs)
            for delay in delays:
                ask(text, colour.answer, "colour", "chain3", since, delay, needs)

    # Counting: how many people keep something in a room once every move is told.
    holders: dict[str, set[str]] = {}
    touched: dict[str, list[int]] = {}
    for fact in (f for f in facts if f.kind == "place"):
        room = updates[fact.id][0] if fact.id in updates else fact.answer
        holders.setdefault(room, set()).add(fact.subject)
        touched.setdefault(fact.answer, []).append(told_at[fact.id])
        if fact.id in updates:
            # a move changes the count of the room left as well as the room reached
            touched.setdefault(room, []).append(updates[fact.id][1])
            touched[fact.answer].append(updates[fact.id][1])
    for room, people_there in sorted(holders.items()):
        needs = tuple(f.id for f in facts if f.kind == "place"
                      and room in (f.answer, updates.get(f.id, ("",))[0]))
        since = max(touched[room])
        for delay in delays[1::2]:
            ask(f"How many people keep things in the {room}?", str(len(people_there)),
                "count", "count", since, delay, needs)

    # Negatives, in three of the house's shapes, spread over the conversation.
    asked_turns = sorted({q.asked_at for q in questions})
    strangers = _people(rng, negatives + len(people))
    strangers = [s for s in strangers if s not in people][:negatives]
    for i, who in enumerate(strangers):
        shape = i % 3
        if shape == 0:
            text, kind = f"Where does {who} keep the {rng.choice(_OBJECTS)}?", "room"
        elif shape == 1:
            text, kind = f"What does {who} do for a living?", "trade"
        else:
            text, kind = f"Whose cousin is {who}?", "person"
        at = asked_turns[(i * len(asked_turns)) // max(1, len(strangers))]
        questions.append(Question(text, None, kind, "negative", 0, at))

    return House(seed, n_turns, facts, turns, questions, told_at)


def _answer_kind(fact: Fact) -> str:
    return {"trade": "trade", "number": "number", "place": "room", "colour": "colour",
            "relation": "person"}[fact.kind]


def _phrasings(fact: Fact) -> tuple[str, str]:
    if fact.kind == "trade":
        return (f"What does {fact.subject} do for a living?",
                f"How does {fact.subject} earn a wage?")
    if fact.kind == "number":
        room = fact.fields["room"]
        return (f"How many {fact.subject} are in the {room}?",
                f"How many {_OBJECT_SYNONYMS[fact.subject]} sit in the {_ROOM_SYNONYMS[room]}?")
    if fact.kind == "place":
        thing = fact.fields["thing"]
        return (f"Where does {fact.subject} keep the {thing}?",
                f"Which part of the house holds {fact.subject}'s {_OBJECT_SYNONYMS[thing]}?")
    if fact.kind == "colour":
        return (f"What colour are the {fact.subject}?",
                f"What shade are the {_OBJECT_SYNONYMS[fact.subject]}?")
    return (f"Whose cousin is {fact.subject}?", f"To whom is {fact.subject} related?")
