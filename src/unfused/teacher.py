"""Teacher: a question told with its answer is a lesson, and what a lesson leaves.

The question's names are the noun phrases the graph already holds; the shortest paths from the
first name to the answer, with where each other name hangs off them, are kept as plans under the
question's shape, holding the lemmas met at each event. Every plan of the shape is scored on the
lesson: it held if following it finds the answer, and failed if it finds another. A lesson also
counts what the wh-word asked for, where each word sat in its template, which known name a word
never heard stood for, what the answer was worth if it was a number, and the oblique an answer
to a word asking for a circumstance hung by.
"""

from __future__ import annotations

import json

from unfused.aliaser import Aliaser
from unfused.follower import Follower
from unfused.individuals import Individuals
from unfused.meeter import Meeter
from unfused.numbers import Numbers
from unfused.plans import Plans
from unfused.said import said_in
from unfused.shaper import Shaper
from unfused.storage import Store
from unfused.walker import Walker


class Teacher:
    def __init__(self, store: Store, walker: Walker, individuals: Individuals, plans: Plans,
                 shaper: Shaper, follower: Follower, aliaser: Aliaser, meeter: Meeter,
                 numbers: Numbers) -> None:
        self.store = store
        self.db = store.db
        self.walker = walker
        self.individuals = individuals
        self.plans = plans
        self.shaper = shaper
        self.follower = follower
        self.aliaser = aliaser
        self.meeter = meeter
        self.numbers = numbers
        # the pairs of events already counted towards a plan's order, this world
        self.ordered: set = set()

    def teach(self, question: str, answer: str) -> None:
        want = answer.lower()
        # what the question's wh-word asked for, by the mark the answer is heard with
        if (wh := self.shaper.wh_word(question)) and (mark := self.individuals.mark(want)):
            self.db.execute("INSERT INTO asks VALUES (?, ?, 1) ON CONFLICT(wh, mark) DO "
                            "UPDATE SET n = n + 1", (wh, mark))
        # where each word sat is counted as heard, so a word held in its place is frame
        self.shaper.heard_at(question)
        put = self.aliaser.alias(question, want)
        if put != question:
            self.shaper.heard_at(put)
        question = put
        self.plans.begin()
        shape, fillers = self.shaper.shape(question)
        for rowid, plan in self.db.execute("SELECT rowid, plan FROM learnt WHERE shape = ?",
                                           (shape,)).fetchall():
            plan = json.loads(plan)

            def holds(present: bool | None) -> bool | None:
                found = (self.follower.said(plan, fillers, True, question, present)
                         or self.follower.said(plan, fillers, False, question, present))
                if not found:
                    return None
                said = max(found, key=lambda f: f[1])[0]
                return said_in(want, said) or (
                    self.numbers.number(want) is not None
                    and self.numbers.number(said) == self.numbers.number(want))

            # whether the answer is the visit just before or after another name of the
            # question, counted wherever that name is among the plan's ends
            ends = self.follower.follow(plan, fillers, False)
            for k in range(1, len(fillers)):
                seen, at = self.follower.visits(ends, fillers[k])
                if at is None:
                    continue
                into = plan.setdefault("relative", {}).setdefault(str(k), [0, 0])
                if at > 0 and said_in(want, seen[at - 1][1]):
                    into[0] += 1
                if at + 1 < len(seen) and said_in(want, seen[at + 1][1]):
                    into[1] += 1
                self.db.execute("UPDATE learnt SET plan = ? WHERE rowid = ?",
                                (json.dumps(plan), rowid))
                self.plans.changed()
            # whether the plan asks about the present, counted on every lesson where
            # reading only what is still so and reading everything differ
            # right over no answer over wrong: reading the present and finding nothing
            # beats reading history and naming a room no longer so
            def worth(h: bool | None) -> int:
                return 0 if h is None else (1 if h else -1)

            now, then = worth(holds(True)), worth(holds(False))
            if now != then:
                tally = plan.get("present", [0, 0])
                tally[0 if now > then else 1] += 1
                plan["present"] = tally
                self.db.execute("UPDATE learnt SET plan = ? WHERE rowid = ?",
                                (json.dumps(plan), rowid))
                self.plans.changed()
            held = holds(None)
            if held is None:
                continue
            column = "hits" if held else "misses"
            (hits, misses), = self.db.execute(
                f"UPDATE learnt SET {column} = {column} + 1 WHERE rowid = ? "
                "RETURNING hits, misses", (rowid,)).fetchall()
            self.plans.changed()
            # a plan is followed where it held more often than it failed
            if (hits - held > misses - (not held)) != (hits > misses):
                self.plans.refollowed()
        goals = self.individuals.holding(want)
        self.meeter.learn_circumstance(question, want)
        if not fillers:
            self.store.written()
            return
        if not goals and not want.isdigit():
            # an answer nothing heard holds may be a number word, whose worth is learnt
            self.heard_count(shape, fillers, want)
        # only the first twenty of the shortest are ever made plans
        found = [p for g in goals[:5] for p in self.walker.paths(f"n:{fillers[0]}", g, most=20)]
        shortest = min((len(p) for p in found), default=0)
        plans = [self.plan_of(p, fillers[1:]) for p in found if len(p) == shortest][:20]
        if not any(plans) and self.numbers.number(want) is not None:
            # a number no telling said is a number of things: 'How many people keep
            # things in the pantry?' taught 3
            plans = self.counted(fillers, self.numbers.number(want))
        for plan in plans:
            if plan is None:
                continue
            k = self.key(plan)
            row = self.db.execute("SELECT rowid, plan FROM learnt WHERE shape = ?",
                                  (shape,)).fetchall()
            same = next(((r, json.loads(p)) for r, p in row if self.key(json.loads(p)) == k),
                        None)
            if same:
                rid, kept = same
                for pos, lemmas in plan["lemmas"].items():
                    kept["lemmas"][pos] = sorted(set(kept["lemmas"].get(pos, [])) | set(lemmas))
                for pos, moods in plan["moods"].items():
                    into = kept.setdefault("moods", {})
                    into[pos] = sorted(set(into.get(pos, [])) | set(moods))
                for pair, by_lemma in plan.get("order", {}).items():
                    # one pair of events is one piece of evidence, however often a lesson
                    # asks about it: a house asks of one fact several times
                    seen = (shape, k, pair, plan["evidence"].get(pair))
                    if seen in self.ordered:
                        continue
                    self.ordered.add(seen)
                    into = kept.setdefault("order", {}).setdefault(pair, {})
                    for lemma, (b, a) in by_lemma.items():
                        was = into.get(lemma, [0, 0])
                        into[lemma] = [was[0] + b, was[1] + a]
                (hits, misses), = self.db.execute(
                    "UPDATE learnt SET plan = ?, hits = hits + 1 WHERE rowid = ? "
                    "RETURNING hits, misses", (json.dumps(kept), rid)).fetchall()
                self.plans.changed()
                if (hits - 1 > misses) != (hits > misses):
                    self.plans.refollowed()
            else:
                for pair, ev in plan["evidence"].items():
                    self.ordered.add((shape, k, pair, ev))
                stored = {key: v for key, v in plan.items() if key != "evidence"}
                stored["sig"] = self.shaper.signature(question, fillers)
                self.db.execute("INSERT OR IGNORE INTO learnt VALUES (?, ?, 1, 0)",
                                (shape, json.dumps(stored)))
                self.plans.changed()
                self.plans.refollowed()
        self.store.written()

    def plan_of(self, path: list, others: list[str]) -> dict | None:
        """A path as a plan: its steps, the lemma at each event on it, and where each other
        name of the question hangs off an event of it."""
        steps = [list(s) for s in path[1::2]]
        nodes = path[::2]
        lemmas, moods, attach, order = {}, {}, [], {}
        events = [(i, node, *self.walker.event(node)) for i, node in enumerate(nodes)
                  if node.startswith("e:")]
        evidence = {}
        for i, node, lemma, _ in events:
            lemmas[str(i)] = [lemma]
            moods[str(i)] = [self.walker.mood(node)]
        # whether each event came before or after the one before it on the path, by that
        # one's lemma: after 'got' the answer's move came later, after 'put down' earlier.
        # The two events are kept beside it, so a pair seen again is not counted again
        for (i, node_i, lemma_i, turn_i), (j, node_j, _, turn_j) in zip(events, events[1:]):
            order[f"{i}-{j}"] = {lemma_i: [int(turn_j < turn_i), int(turn_j >= turn_i)]}
            evidence[f"{i}-{j}"] = f"{node_i}>{node_j}"
        for k, name in enumerate(others):
            # the shortest way from any node of the path to the other name: 'I counted 35
            # walking sticks in the cellar' has the cellar on the counting, a step off the
            # path from the sticks to their number
            hook = None
            for i, node in enumerate(nodes):
                for way in self.walker.paths(node, f"n:{name}", limit=3, avoid=set(nodes),
                                      most=1):
                    if hook is None or len(way) < len(hook[2]) * 2 + 1:
                        hook = [k + 1, i, [list(st) for st in way[1::2]]]
            if hook is None:
                return None
            attach.append(hook)
        return {"steps": steps, "lemmas": lemmas, "moods": moods, "attach": attach,
                "order": order, "evidence": evidence}

    @staticmethod
    def key(plan: dict) -> str:
        return json.dumps({"steps": plan["steps"], "attach": plan["attach"],
                           "count": plan.get("count", False)})

    def counted(self, fillers: list[str], want: int) -> list[dict]:
        """Plans that count: from the question's first name, every way whose distinct
        ends number what was taught, shortest first."""
        found = []
        for key, (ends, rep) in self.walker.walks(f"n:{fillers[0]}").items():
            if len(ends) == want:
                plan = self.plan_of(rep, fillers[1:])
                if plan is not None:
                    found.append({**plan, "count": True})
        found.sort(key=lambda p: len(p["steps"]))
        return [p for p in found if len(p["steps"]) == len(found[0]["steps"])] if found else []

    def heard_count(self, shape: str, fillers: list[str], want: str) -> None:
        """A lesson whose answer may be a number word: every walk from the question's
        first name, with how many it reached, and none for a walk this shape had before
        that reaches nothing now."""
        ways = self.walker.walks(f"n:{fillers[0]}")
        rows = [(shape, key, want, len(ends)) for key, (ends, _) in ways.items()]
        rows += [(shape, key, want, 0) for (key,) in self.db.execute(
            "SELECT DISTINCT walk FROM counts WHERE shape = ?", (shape,)) if key not in ways]
        self.db.executemany("INSERT INTO counts VALUES (?, ?, ?, ?)", rows)
