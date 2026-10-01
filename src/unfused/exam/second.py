"""The second house: the first house's skeleton and forms, with five kinds of fact
that share no relation with it. Agreed with John, 2026-09-30.

- employment with a place: "Vessarine works at the ropewalk."
- age: "Vessarine is 43 years old."
- lending, three places: "Vessarine lent the ladder to Brannoth." The borrower
  then holds the thing, and can pass it on, which is this house's update.
- material: "The ladder is made of oak."
- an asymmetric kin: "Vessarine is Brannoth's godparent." Unlike a cousin, it
  does not read the same from the other end.

Every form of the first house is asked, built from these kinds, so a reading here
is comparable form by form. No code of the system is changed for it: the system
is taught from this house's own practice houses and has to read relations it was
never built around.
"""

from __future__ import annotations

import random
from collections import Counter

from .world import _FILLER, DEFAULT_DELAYS, Fact, House, Question, _people

_WORKPLACES = [
    "mill", "forge", "tannery", "ropewalk", "brewery", "chandlery", "pottery",
    "boatyard", "quarry", "printworks",
]
_THINGS = [
    "ladder", "wheelbarrow", "fishing rod", "sewing box", "telescope", "bread tin",
    "rocking chair", "toolbox", "kite", "birdcage",
]
_MATERIALS = ["oak", "pewter", "wicker", "brass", "birch", "tin", "cork", "horn"]

_THING_SYNONYMS = {
    "ladder": "steps", "wheelbarrow": "barrow", "fishing rod": "angling pole",
    "sewing box": "needlework case", "telescope": "spyglass", "bread tin": "loaf box",
    "rocking chair": "rocker", "toolbox": "tool chest", "kite": "flying toy",
    "birdcage": "aviary",
}

_TELL = {
    "work": ["{who} works at the {place}.", "{who} has a job at the {place} these days.",
             "{who} spends the working day at the {place}."],
    "age": ["{who} is {n} years old.", "{who} turned {n} this spring.",
            "{who} is {n} now, believe it or not."],
    "loan": ["{lender} lent the {thing} to {borrower}.",
             "{borrower} borrowed the {thing} from {lender}.",
             "The {thing} is {lender}'s, but {borrower} has it on loan."],
    "material": ["The {thing} is made of {material}.",
                 "Somebody made the {thing} out of {material}.",
                 "The {thing} is all {material}, as far as I can tell."],
    "kin": ["{a} is {b}'s godparent.", "{a} stood as godparent to {b}.",
            "{b}'s godparent is {a}."],
    "update": ["{borrower} has passed the {thing} on to {holder}.",
               "The {thing} isn't with {borrower} any more; {holder} has it now."],
}

_AGE_RANGE = (18, 90)


def _make_facts(rng: random.Random, n_facts: int, people: list[str]) -> list[Fact]:
    """Five kinds dealt round-robin. One fact per (kind, subject), so every
    question has one right answer."""
    facts: list[Fact] = []
    used: set[tuple[str, str]] = set()
    kinds = ["work", "age", "loan", "material", "kin"]
    attempts = 0
    while len(facts) < n_facts:
        attempts += 1
        if attempts > n_facts * 200:
            raise ValueError(f"cannot deal {n_facts} distinct facts from this vocabulary")
        kind = kinds[len(facts) % len(kinds)]
        fid = f"f{len(facts):03d}"
        template = rng.choice(_TELL[kind])
        if kind == "work":
            who, place = rng.choice(people), rng.choice(_WORKPLACES)
            if (kind, who) in used:
                continue
            used.add((kind, who))
            facts.append(Fact(fid, kind, who, template.format(who=who, place=place), place))
        elif kind == "age":
            who = rng.choice(people)
            if (kind, who) in used:
                continue
            used.add((kind, who))
            n = rng.randint(*_AGE_RANGE)
            facts.append(Fact(fid, kind, who, template.format(who=who, n=n), str(n)))
        elif kind == "loan":
            # one loan a thing, so "who has the ladder" names one person
            lender, borrower = rng.sample(people, 2)
            thing = rng.choice(_THINGS)
            if (kind, thing) in used:
                continue
            used.add((kind, thing))
            facts.append(Fact(fid, kind, lender,
                              template.format(lender=lender, borrower=borrower, thing=thing),
                              borrower, {"thing": thing}))
        elif kind == "material":
            thing, material = rng.choice(_THINGS), rng.choice(_MATERIALS)
            if (kind, thing) in used:
                continue
            used.add((kind, thing))
            facts.append(Fact(fid, kind, thing, template.format(thing=thing, material=material),
                              material))
        else:
            a, b = rng.sample(people, 2)
            # a person is in at most one godparent fact, either side, so "b's
            # godparent" and "whose godparent is a" each name one person
            if ("kin", a) in used or ("kin", b) in used:
                continue
            used.add(("kin", a))
            used.add(("kin", b))
            # the subject is the godchild and the answer the godparent
            facts.append(Fact(fid, kind, b, template.format(a=a, b=b), a))
    return facts


def generate_second_house(
    seed: int = 0,
    n_facts: int = 50,
    n_turns: int = 300,
    delays: tuple[int, ...] = DEFAULT_DELAYS,
    negatives: int = 20,
    update_share: float = 0.4,
) -> House:
    """A second house of `n_facts` invented facts told across `n_turns` of conversation,
    asked in every form the first house asks."""
    rng = random.Random(seed)
    people = _people(rng, max(8, (n_facts * 2) // 5))
    facts = _make_facts(rng, n_facts, people)
    by_id = {f.id: f for f in facts}

    longest = max(delays)
    last_tellable = n_turns - longest - 1
    loans = [f for f in facts if f.kind == "loan"]
    n_updates = int(len(loans) * update_share)
    if last_tellable < n_facts + n_updates + 1:
        raise ValueError(
            f"{n_facts} facts and {n_updates} updates cannot all be told before turn "
            f"{last_tellable}; lengthen the conversation or shorten the delays."
        )

    told_turns = sorted(rng.sample(range(1, last_tellable), n_facts))
    told_at = {f.id: t for f, t in zip(facts, told_turns)}
    events: dict[int, str] = {told_at[f.id]: f.told for f in facts}

    # Updates: a borrowed thing passed on to somebody else some turns after the loan.
    updates: dict[str, tuple[str, int]] = {}  # fact id -> (new holder, turn)
    for fact in rng.sample(loans, n_updates):
        free = [t for t in range(told_at[fact.id] + 3, last_tellable) if t not in events]
        if not free:
            continue
        turn = rng.choice(free)
        holder = rng.choice([p for p in people if p not in (fact.subject, fact.answer)])
        events[turn] = rng.choice(_TELL["update"]).format(
            borrower=fact.answer, thing=fact.fields["thing"], holder=holder)
        updates[fact.id] = (holder, turn)

    turns = [events.get(t, rng.choice(_FILLER)) for t in range(n_turns)]
    questions: list[Question] = []

    def ask(text, answer, kind, form, since, delay, needs, stale=None):
        at = since + delay
        if at < n_turns:
            questions.append(Question(text, answer, kind, form, delay, at, stale, needs))

    for i, fact in enumerate(facts):
        if fact.id in updates:
            continue  # asked below, as an update
        direct, oblique = _phrasings(fact)
        for j, delay in enumerate(delays):
            form = "direct" if (i + j) % 2 == 0 else "oblique"
            ask(direct if form == "direct" else oblique, fact.answer, _answer_kind(fact),
                form, told_at[fact.id], delay, (fact.id,))

    for fid, (holder, turn) in updates.items():
        fact = by_id[fid]
        text = f"Who has the {fact.fields['thing']} now?"
        for delay in delays:
            ask(text, holder, "person", "update", turn, delay, (fid,), stale=fact.answer)

    # Reverse, only where the answer is unique in the house.
    work_count = Counter(f.answer for f in facts if f.kind == "work")
    for fact in facts:
        if fact.kind == "work" and work_count[fact.answer] == 1:
            text, answer = f"Who works at the {fact.answer}?", fact.subject
        elif fact.kind == "kin":
            text, answer = f"Whose godparent is {fact.answer}?", fact.subject
        elif fact.kind == "loan" and fact.id not in updates:
            text, answer = (f"Who lent {fact.answer} the {fact.fields['thing']}?",
                            fact.subject)
        else:
            continue
        for delay in delays[1::2]:
            ask(text, answer, "person", "reverse", told_at[fact.id], delay, (fact.id,))

    # Two hops: where a godparent works, and what a lent thing is made of.
    work_of = {f.subject: f for f in facts if f.kind == "work"}
    material_of = {f.subject: f for f in facts if f.kind == "material"}
    for fact in facts:
        if fact.kind == "kin" and fact.answer in work_of:
            second = work_of[fact.answer]
            text = f"Where does {fact.subject}'s godparent work?"
            answer, kind = second.answer, "workplace"
        elif (fact.kind == "loan" and fact.id not in updates
              and fact.fields["thing"] in material_of):
            second = material_of[fact.fields["thing"]]
            text = f"What is the thing {fact.subject} lent to {fact.answer} made of?"
            answer, kind = second.answer, "material"
        else:
            continue
        since = max(told_at[fact.id], told_at[second.id])
        for delay in delays[1::2]:
            ask(text, answer, kind, "twohop", since, delay, (fact.id, second.id))

    # Three in a row: found from a workplace, through the person, to what they lent
    # or to where their godparent works; and from a godchild, through the godparent,
    # to what the godparent lent.
    godparent_of = {f.subject: f for f in facts if f.kind == "kin"}
    for work in facts:
        if work.kind != "work" or work_count[work.answer] != 1:
            continue
        who, place = work.subject, work.answer
        for loan in facts:
            if (loan.kind != "loan" or loan.subject != who or loan.id in updates
                    or loan.fields["thing"] not in material_of):
                continue
            material = material_of[loan.fields["thing"]]
            text = (f"What is the thing the person who works at the {place} lent to "
                    f"{loan.answer} made of?")
            needs = (work.id, loan.id, material.id)
            since = max(told_at[n] for n in needs)
            for delay in delays:
                ask(text, material.answer, "material", "chain3", since, delay, needs)
        if who in godparent_of and godparent_of[who].answer in work_of:
            kin = godparent_of[who]
            second = work_of[kin.answer]
            text = f"Where does the godparent of the person who works at the {place} work?"
            needs = (work.id, kin.id, second.id)
            since = max(told_at[n] for n in needs)
            for delay in delays:
                ask(text, second.answer, "workplace", "chain3", since, delay, needs)
    for kin in (f for f in facts if f.kind == "kin"):
        for loan in facts:
            if (loan.kind != "loan" or loan.subject != kin.answer or loan.id in updates
                    or loan.fields["thing"] not in material_of):
                continue
            material = material_of[loan.fields["thing"]]
            text = (f"What is the thing {kin.subject}'s godparent lent to {loan.answer} "
                    f"made of?")
            needs = (kin.id, loan.id, material.id)
            since = max(told_at[n] for n in needs)
            for delay in delays:
                ask(text, material.answer, "material", "chain3", since, delay, needs)

    # Counting: how many things a person has on loan once every passing-on is told.
    held: dict[str, set[str]] = {}
    touched: dict[str, list[int]] = {}
    for fact in loans:
        holder = updates[fact.id][0] if fact.id in updates else fact.answer
        held.setdefault(holder, set()).add(fact.fields["thing"])
        touched.setdefault(fact.answer, []).append(told_at[fact.id])
        if fact.id in updates:
            # a passing-on changes the count of the one who gave as well as the one who got
            touched.setdefault(holder, []).append(updates[fact.id][1])
            touched[fact.answer].append(updates[fact.id][1])
    for holder, things in sorted(held.items()):
        needs = tuple(f.id for f in loans
                      if holder in (f.answer, updates.get(f.id, ("",))[0]))
        since = max(touched[holder])
        for delay in delays[1::2]:
            ask(f"How many borrowed things does {holder} have?", str(len(things)),
                "count", "count", since, delay, needs)

    # Negatives, in three of the house's shapes, spread over the conversation.
    asked_turns = sorted({q.asked_at for q in questions})
    strangers = _people(rng, negatives + len(people))
    strangers = [s for s in strangers if s not in people][:negatives]
    for i, who in enumerate(strangers):
        shape = i % 3
        if shape == 0:
            text, kind = f"Where does {who} work?", "workplace"
        elif shape == 1:
            text, kind = f"How old is {who}?", "age"
        else:
            text, kind = f"Who is {who}'s godparent?", "person"
        at = asked_turns[(i * len(asked_turns)) // max(1, len(strangers))]
        questions.append(Question(text, None, kind, "negative", 0, at))

    return House(seed, n_turns, facts, turns, questions, told_at)


def _answer_kind(fact: Fact) -> str:
    return {"work": "workplace", "age": "age", "loan": "person", "material": "material",
            "kin": "person"}[fact.kind]


def _phrasings(fact: Fact) -> tuple[str, str]:
    if fact.kind == "work":
        return (f"Where does {fact.subject} work?",
                f"Which place employs {fact.subject}?")
    if fact.kind == "age":
        return (f"How old is {fact.subject}?", f"What age has {fact.subject} reached?")
    if fact.kind == "loan":
        thing = fact.fields["thing"]
        return (f"Who did {fact.subject} lend the {thing} to?",
                f"Who is minding {fact.subject}'s {_THING_SYNONYMS[thing]}?")
    if fact.kind == "material":
        return (f"What is the {fact.subject} made of?",
                f"What stuff went into the {_THING_SYNONYMS[fact.subject]}?")
    return (f"Who is {fact.subject}'s godparent?",
            f"Who stood sponsor at {fact.subject}'s christening?")
