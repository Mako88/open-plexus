"""The graphed arm: the conversation kept as its parse, and plans learnt as paths in it.

Every sentence is parsed, and nothing maps the parse to slots. A clause is an event: its
verb's lemma, the turn it was heard, and an edge to each of its arguments labelled with
the grammatical link (nsubj, dobj, attr, prep:in, prep:to, ...). A noun with dependents of
its own ("Trapairk's godparent", "45 glass jars") is an event too, joined by `self` to the
noun's name. Every other noun, number or adjective is a name, one node however often it is
said, which is what joins one sentence to the next.

A question told with its answer is a lesson. Its names are the noun phrases the graph
already holds; the shortest path from the first name to the answer, with where each other
name hangs off it, is kept as a plan under the question's shape, holding the lemmas met at
each event. Plans are scored on every later lesson of their shape and only those that held
more often than they failed are followed. An answer is the end of a followed path; of
several, the one resting on the latest event, which is how a move or a correction wins.
No path, no answer: "I don't know."
"""

from __future__ import annotations

import json
import math
from collections import Counter
from pathlib import Path

from unfused.hearer import Hearer
from unfused.individuals import Individuals
from unfused.mind import Mind
from unfused.mouth import Mouth
from unfused.numbers import Numbers

# the scripts and the guards read BLANKS, PARSER, extract, nlp, parse_many and vectors through
# this module, so they are named here whether or not it uses them
from unfused.parsing import (  # noqa: F401
    BLANKS,
    CACHE,
    PARSER,
    VERSION,
    extract,
    nlp,
    parse,
    parse_many,
    vectors,
)
from unfused.reader import Reader
from unfused.said import said_in
from unfused.shaper import Shaper
from unfused.situations import Situations
from unfused.storage import CARRIED, Store
from unfused.walker import EFFORT, REACH, Spent, Walker

# how much nearer a word's likest candidate must be than the next for a vote: right picks'
# gaps sat at 0.14 to 0.21 and wrong ones' at 0.02 to 0.06 (readings/unheard-*)
MARGIN = 0.08
# how fast a hearing fades within an episode (ACT-R's base-level decay)
DECAY = 0.5

# how many hearings a name's fit is shrunk by towards nothing, so one hearing is not a
# certainty
PRIOR = 2.0
# the walk a question's names and verb spread by (`met`): how many steps, what is passed
# on at each, the activation below which nothing spreads on, and the latest events a
# verb reaches where none is in mind, as a name reaches its latest individuals
STEPS = 3
PASS = 0.8
FLOOR = 1e-3
LEMMA = 300
# how high focus must rank what a plan found for the plan's answer to stand
TOP = 3



class GraphArm:
    name = "graphed"
    # whether an answer keeps the names focus weighed for it, with their factors, in
    # `traced`: the scores it computed anyway, so an instrument need not ask focus again
    trace = False

    def __init__(self, directory: Path, model: str = PARSER,
                 known: dict | None = None, cache: Path | None = CACHE) -> None:
        directory.mkdir(parents=True, exist_ok=True)
        self.store = Store(directory, known)
        # the connection the rest of the graph's tables are read through
        self.db = self.store.db
        # what was being heard when each word was heard, folded over the stream
        self.situations = Situations(self.db)
        self.model = model
        # everything the parser says of a text, kept so no text is parsed twice
        self.reader = Reader(model, cache)
        # the episode and what is in mind with it
        self.mind = Mind(self.db, self.reader)
        # what a name or an id stands for
        self.individuals = Individuals(self.db, self.mind)
        # the graph as steps from a node, and the walks over them
        self.walker = Walker(self.store, self.individuals, self.mind)
        self.mind.listen(self.walker.forget_names)
        # what each number word is worth, as lessons showed it
        self.numbers = Numbers(self.db)
        # a question as a template and a shape
        self.shaper = Shaper(self.db, self.reader, self.individuals)

        # a sentence heard, written into the graph

        self.hearer = Hearer(
self.store, self.reader, self.mind, self.individuals,
                             self.walker, self.situations)

        self.mouth = Mouth(self)
        self.last_notes: list[str] = []
        self.traced: dict | None = None
        # the pairs of events already counted towards a plan's order, this world
        self.ordered: set = set()
        # a question this arm answered, waiting for the turn that reacts to it
        self.pending: tuple[str, str] | None = None
        # the words never heard and answers already counted towards an alias, this world
        self.aliased: set = set()

        # taught shapes' signatures, rebuilt after a lesson; questions' parses
        self._sigs: list | None = None
        # how many times `learnt` has been written, and the plans of every followed shape
        # as of the last count `related` read them at
        self._lessons = 0
        self._followed: tuple | None = None
        # how many times a lesson changed which plans are followed, or added one, and the
        # count the taught shapes' caches were last rebuilt at; a lesson marks them stale,
        # and they are rebuilt at the next read only if that count moved
        self._shapes = 0
        self._built = 0
        self._stale = False
        # the taught shapes of each number of slots, indexed by their signatures' parts,
        # and the nearest shapes already found for a signature; both until a lesson
        self._by_slots: dict = {}
        self._near: dict = {}
        # each stored plan's text, decoded (`read_plan`)
        self._read: dict[str, dict] = {}

    # -- reading ---------------------------------------------------------------

    def hear(self, turn: int, text: str) -> None:
        self.hearer.hear(turn, text)

    # -- the graph -------------------------------------------------------------

    # -- plans -----------------------------------------------------------------

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

    def follow(self, plan: dict, fillers: list[str], strict: bool,
               present: bool | None = None) -> list[tuple[str, int]]:
        """Every end the plan reaches from the question's first name, with the latest turn
        of the events it passed. A plan that asks about the present passes no event a later
        one replaced; whether it does is learnt from its lessons, since 'where was it
        before' needs exactly those."""
        if not fillers:
            return []
        if present is None:
            now, then = plan.get("present", (0, 0))
            present = now > then
        out = []
        def hooked(node: str, pos: int) -> bool:
            return all(self.walker.reaches(node, h[2], f"n:{fillers[h[0]]}")
                       for h in plan["attach"] if h[1] == pos and h[0] < len(fillers))

        def ordered(last, j: int, turn_j: int) -> bool:
            if last is None:
                return True
            i, lemma_i, turn_i = last
            before, after = plan.get("order", {}).get(f"{i}-{j}", {}).get(lemma_i, (0, 0))
            if before >= 3 and before > 2 * after:
                return turn_j <= turn_i
            if after >= 3 and after > 2 * before:
                return turn_j >= turn_i
            return True

        self.emptied = 0
        start = f"n:{fillers[0]}"
        # a walk: its nodes, the turns of its events in order, and its last event
        walks = [([start], (), None)] if hooked(start, 0) else []
        for i, (label, direction) in enumerate(plan["steps"]):
            nxt_walks = []
            for nodes, turns, last in walks:
                here = nodes[-1]
                # a hub ('Lily' in a thousand stories) is walked through its latest
                # `REACH` steps of the label, as recall searches the recent first
                ways = [(lab, d, nxt) for lab, d, nxt in self.walker.around(here)
                        if lab == label and d == direction][:REACH]
                for lab, d, nxt in ways:
                    if self.individuals.among(nxt, nodes):
                        continue
                    t, now = turns, last
                    if nxt.startswith("e:"):
                        lemma, et = self.walker.event(nxt)
                        pos = str(i + 1)
                        if strict and lemma not in plan["lemmas"].get(pos, [lemma]):
                            continue
                        # loosely, any verb will do, but never one that did not happen
                        # where the lessons' did, or the other way round
                        mood = self.walker.mood(nxt)
                        if mood not in plan.get("moods", {}).get(pos, [mood]):
                            continue
                        if not ordered(last, i + 1, et):
                            continue
                        if present and (by := self.walker.replaced(nxt)):
                            self.emptied = max(self.emptied, by)
                            continue
                        # what did not happen, or only might, changed nothing, so it is
                        # walked through and never makes an answer the latest
                        t, now = turns + ((0 if mood else et),), (i + 1, lemma, et)
                    if not hooked(nxt, i + 1):
                        continue
                    nxt_walks.append((nodes + [nxt], t, now))
            walks = nxt_walks[:2000]
        for nodes, turns, _ in walks:
            end = nodes[-1]
            if not end.startswith("e:"):
                # the most recent thing that happened to the first name first, then what
                # followed from it: the football's last event, then its carrier's move
                out.append((self.individuals.describe(end), turns))
        return out

    def read_plan(self, text: str) -> dict:
        """A stored plan, decoded once for every reader: answering reads every taught
        plan many times a question and a lesson changes one shape's. Shared, so only
        `teach`, which changes plans, decodes its own."""
        got = self._read.get(text)
        if got is None:
            # a lesson rewrites a plan's text, so the texts it replaced are let go in bulk
            if len(self._read) > 100_000:
                self._read.clear()
            got = self._read[text] = json.loads(text)
        return got

    def settled(self) -> None:
        """After a lesson, the taught shapes are read again at the next use, unless the
        lesson changed none of the plans followed, which is all they are read from."""
        if self._stale:
            self._stale = False
            if self._built != self._shapes:
                self._sigs, self._by_slots, self._near = None, {}, {}
                self._built = self._shapes

    def taught(self) -> list[tuple[str, set]]:
        """Every taught shape's plans' signatures, rebuilt after a lesson."""
        self.settled()
        if self._sigs is None:
            self._sigs = []
            for shape, plan in self.db.execute(
                    "SELECT shape, plan FROM learnt WHERE hits > misses").fetchall():
                sig = self.read_plan(plan).get("sig")
                if sig:
                    self._sigs.append((shape, set(sig)))
        return self._sigs

    def nearest(self, question: str, fillers: list[str],
                skip: str | None = None) -> tuple[float, list[str]]:
        """The taught shapes nearest a question no lesson was worded as: those of its
        number of slots whose signature overlaps the question's most, and by how much."""
        mine = frozenset(self.shaper.signature(question, fillers))
        key = (mine, len(fillers), skip)
        self.settled()
        got = self._near.get(key)
        if got is None:
            # each shape's overlap is counted through the index, so a shape sharing
            # nothing with the question costs nothing, and scores 0
            rows, index = self.taught_by(len(fillers))
            shared = Counter(i for part in mine for i in index.get(part, ()))
            near = [(shape, shared[i] / (len(mine) + len(sig) - shared[i]))
                    for i, (shape, sig) in enumerate(rows) if shape != skip]
            best = max((n for _, n in near), default=0.0)
            got = self._near[key] = (best, list(dict.fromkeys(
                shape for shape, n in near if n == best)))
        return got[0], list(got[1])

    def taught_by(self, slots: int) -> tuple[list, dict]:
        """The taught shapes of this many slots with their signatures, in the order
        `taught` gives them, and where each part of a signature is held among them."""
        self.settled()
        got = self._by_slots.get(slots)
        if got is None:
            rows = [(shape, sig) for shape, sig in self.taught() if shape.count("<") == slots]
            index: dict = {}
            for i, (_, sig) in enumerate(rows):
                for part in sig:
                    index.setdefault(part, []).append(i)
            got = self._by_slots[slots] = (rows, index)
        return got

    def borrowed(self, question: str,
                 skip: str | None = None) -> list[tuple[list[str], list[str]]]:
        """A wording no lesson used has no history saying which of its names are slots
        and which are frame ('cousin' in 'Which person is X's cousin?'), nor in what order
        its slots run. Every reading is scored, each name a slot or frame and the slots in
        any order, nearest a taught shape first."""
        from itertools import combinations, permutations

        names = [n for _, _, n in self.shaper.template(question)[1]][:4]
        readings = []
        for k in range(1, len(names) + 1):
            for chosen in combinations(names, k):
                for order in permutations(chosen):
                    near, shapes = self.nearest(question, list(order), skip)
                    if shapes:
                        readings.append((near, list(order), shapes))
        readings.sort(key=lambda r: -r[0])
        return [(f, sh) for _, f, sh in readings]

    def answers(self, shape: str, fillers: list[str], question: str,
                borrow: bool = True) -> list[tuple[str, int]]:
        ranked = self.db.execute(
            "SELECT plan FROM learnt WHERE shape = ? AND hits > misses "
            "ORDER BY hits - misses DESC, hits DESC", (shape,)).fetchall()
        if ranked:
            found = self.followed(ranked, fillers, question)
            if found:
                return found
        if not borrow or not all(self.individuals.known(f) for f in fillers):
            # of a name the conversation never used nothing can be known, so its
            # question is not borrowed for
            return []
        # a wording no lesson used, or one whose plans reach nothing here, borrows the
        # plans of the nearest other taught shape under the nearest reading of it that
        # reaches anything in the graph: 'Who is X's cousin?' learnt one direction, and
        # 'Whose cousin is X?' holds the other
        for fillers, nearest in self.borrowed(question, shape)[:12]:
            ranked = [r for near in nearest for r in self.db.execute(
                "SELECT plan FROM learnt WHERE shape = ? AND hits > misses "
                "ORDER BY hits - misses DESC, hits DESC", (near,)).fetchall()]
            found = self.followed(ranked, fillers, question)
            if found:
                return found
        return []

    def reaching(self, question: str, skip: str, tries: int = 60) -> list:
        """Where the nearest shapes reach nothing, every taught shape with as many slots
        as the question names known things, nearest first, read under each order of
        those names: the first whose plans reach anything is the relation asked, a count
        never, since a count of anything is a number. Last of all, after taking the
        question apart, which reads it more closely. Two
        wordings are one relation where the house holds a solution for both: 'What is
        the number of X in the Y?' shares little grammar with 'How many X are in the
        Y?' and the house relates X and Y by little else."""
        from itertools import combinations, permutations

        names = [n for _, _, n in self.shaper.template(question)[1] if self.individuals.known(n)][:4]
        readings = []
        for k in range(1, len(names) + 1):
            for chosen in combinations(names, k):
                for order in permutations(chosen):
                    mine = set(self.shaper.signature(question, list(order)))
                    readings += [(len(mine & sig) / len(mine | sig), list(order), shape)
                                 for shape, sig in self.taught()
                                 if shape != skip and shape.count("<") == k]
        readings.sort(key=lambda r: -r[0])
        for _, fillers, shape in readings[:tries]:
            ranked = [(p,) for (p,) in self.db.execute(
                "SELECT plan FROM learnt WHERE shape = ? AND hits > misses "
                "ORDER BY hits - misses DESC, hits DESC", (shape,)) if not self.read_plan(p).get("count")]
            found = self.followed(ranked, fillers, question)
            if found:
                return found
        return []

    def followed(self, ranked: list, fillers: list[str], question: str) -> list:
        for strict in (True, False):
            found = []
            for (plan,) in ranked:
                plan = self.read_plan(plan)
                said = self.said(plan, fillers, strict, question)
                if said and plan.get("count"):
                    # a count is of a set, not of one latest event, so the plan that held
                    # most often answers it rather than whichever passed the latest turn
                    return said
                found += said
            if found:
                return found
        return []

    @staticmethod
    def visits(ends: list, name: str) -> tuple[list, int | None]:
        """A plan's ends as visits in turn order, and where the latest visit to a name
        sits among them: the apple's rooms, and the bathroom's place in that list."""
        seen = sorted({(t[-1] if t else 0, e) for e, t in ends})
        at = max((i for i, (_, e) in enumerate(seen) if e == name), default=None)
        return seen, at

    def relative(self, plan: dict, ends: list, fillers: list[str]) -> list | None:
        """Where lessons showed the answer one visit before or after another name of
        the question ('where was it before the bathroom'), that visit."""
        for k, (before, after) in plan.get("relative", {}).items():
            k = int(k)
            if k >= len(fillers):
                continue
            seen, at = self.visits(ends, fillers[k])
            if at is None:
                continue
            step = -1 if before >= 3 and before > 2 * after else (
                1 if after >= 3 and after > 2 * before else 0)
            if step and 0 <= at + step < len(seen):
                t, e = seen[at + step]
                return [(e, (t,))]
        return None

    def said(self, plan: dict, fillers: list[str], strict: bool,
             question: str, present: bool | None = None) -> list[tuple[str, int]]:
        ends = self.follow(plan, fillers, strict, present)
        chosen = self.relative(plan, ends, fillers)
        if chosen is not None:
            return chosen
        if plan.get("count"):
            if ends:
                return [(self.numbers.say_number(len({e for e, _ in ends})),
                         max(t for _, t in ends))]
            # nothing left to count is an answer, resting on what emptied the set: 'Mary
            # dropped the football' is later than her picking it up
            return [(self.numbers.say_number(0), (self.emptied,))] if self.emptied else []
        return [f for f in ends if not said_in(f[0], question)]

    def teach(self, question: str, answer: str) -> None:
        want = answer.lower()
        # what the question's wh-word asked for, by the mark the answer is heard with
        if (wh := self.shaper.wh_word(question)) and (mark := self.individuals.mark(want)):
            self.db.execute("INSERT INTO asks VALUES (?, ?, 1) ON CONFLICT(wh, mark) DO "
                            "UPDATE SET n = n + 1", (wh, mark))
        # where each word sat is counted as heard, so a word held in its place is frame
        self.shaper.heard_at(question)
        put = self.alias(question, want)
        if put != question:
            self.shaper.heard_at(put)
        question = put
        self._stale = True
        shape, fillers = self.shaper.shape(question)
        for rowid, plan in self.db.execute("SELECT rowid, plan FROM learnt WHERE shape = ?",
                                           (shape,)).fetchall():
            plan = json.loads(plan)

            def holds(present: bool | None) -> bool | None:
                found = (self.said(plan, fillers, True, question, present)
                         or self.said(plan, fillers, False, question, present))
                if not found:
                    return None
                said = max(found, key=lambda f: f[1])[0]
                return said_in(want, said) or (
                    self.numbers.number(want) is not None
                    and self.numbers.number(said) == self.numbers.number(want))

            # whether the answer is the visit just before or after another name of the
            # question, counted wherever that name is among the plan's ends
            ends = self.follow(plan, fillers, False)
            for k in range(1, len(fillers)):
                seen, at = self.visits(ends, fillers[k])
                if at is None:
                    continue
                into = plan.setdefault("relative", {}).setdefault(str(k), [0, 0])
                if at > 0 and said_in(want, seen[at - 1][1]):
                    into[0] += 1
                if at + 1 < len(seen) and said_in(want, seen[at + 1][1]):
                    into[1] += 1
                self.db.execute("UPDATE learnt SET plan = ? WHERE rowid = ?",
                                (json.dumps(plan), rowid))
                self._lessons += 1
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
                self._lessons += 1
            held = holds(None)
            if held is None:
                continue
            column = "hits" if held else "misses"
            (hits, misses), = self.db.execute(
                f"UPDATE learnt SET {column} = {column} + 1 WHERE rowid = ? "
                "RETURNING hits, misses", (rowid,)).fetchall()
            self._lessons += 1
            # a plan is followed where it held more often than it failed
            if (hits - held > misses - (not held)) != (hits > misses):
                self._shapes += 1
        goals = self.individuals.holding(want)
        self.learn_circumstance(question, want)
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
                self._lessons += 1
                if (hits - 1 > misses) != (hits > misses):
                    self._shapes += 1
            else:
                for pair, ev in plan["evidence"].items():
                    self.ordered.add((shape, k, pair, ev))
                stored = {key: v for key, v in plan.items() if key != "evidence"}
                stored["sig"] = self.shaper.signature(question, fillers)
                self.db.execute("INSERT OR IGNORE INTO learnt VALUES (?, ?, 1, 0)",
                                (shape, json.dumps(stored)))
                self._lessons += 1
                self._shapes += 1
        self.store.written()

    # -- words never heard ----------------------------------------------------

    def unheard(self, question: str) -> list[tuple[int, int, str]]:
        """The question's common nouns the conversation never used, no ending of them
        either: 'the chipped dishes' where only cracked plates were told of. A proper
        name is never one, since a person never mentioned is someone nobody told of; nor
        is a word lessons held in its place every time ('do all day'), which is frame."""
        template, spans = self.shaper.template(question)
        out = []
        for i, (a, b, name) in enumerate(spans):
            words = name.split()
            if self.shaper.proper_at(question, a):
                continue
            if any(self.individuals.known(" ".join(words[j:])) for j in range(len(words))):
                continue
            seen = self.db.execute("SELECT filler, n FROM positions WHERE template = ? AND"
                                   " pos = ?", (template, i)).fetchall()
            if len(seen) == 1 and seen[0][1] >= 2:
                continue
            out.append((a, b, name))
        return out

    def aliases(self, word: str) -> list[str]:
        """The heard name a word stood for in lessons, if one: two facts' worth or more,
        since one fact cannot show two wordings agree, twice any other name's share, and
        found in more than half the lessons that found anything for the word. A word
        that is the question's frame ('shade', 'a wage') fits many names and none that
        often. The row named '' counts those lessons."""
        rows = self.db.execute("SELECT name, hits, found FROM aliases WHERE word = ? "
                               "ORDER BY name = '', hits DESC", (word,)).fetchall()
        tried = next((f for n, _, f in rows if n == ""), 0)
        rows = [r for r in rows if r[0] != ""]
        if not rows:
            return []
        name, hits, found = rows[0]
        second = rows[1][1] if len(rows) > 1 else 0
        return [name] if hits >= 2 and hits >= 2 * second and 2 * found > tried else []

    def unaliased(self, question: str, heard: bool = False) -> str:
        """The question with each word never heard put as the name it stood for, where
        lessons settled one and that name is heard here, and with `heard` where this
        conversation's hearings of it pinned one or MiniLM voted for one."""
        out, at = "", 0
        for a, b, w in self.unheard(question):
            # by its longest ending lessons settled: 'chipped dishes' before 'dishes'. A
            # name the question says already is never put again
            ws = w.split()
            for i in range(len(ws)):
                tail = " ".join(ws[i:])
                name = next((n for n in self.aliases(tail) if self.individuals.known(n)
                             and not said_in(n, out + question[at:])), None)
                if name is not None:
                    out += question[at:b - len(tail)] + name
                    at = b
                    break
            else:
                # no lesson settled it: what this conversation's hearings of it left
                name = (self.pinned(w) or self.voted(w)) if heard else None
                if name is not None and self.individuals.known(name) and not said_in(
                        name, out + question[at:]):
                    out += question[at:a] + name
                    at = b
        return out + question[at:]

    def readings(self, question: str, spans: list) -> list[tuple[float, tuple, list]]:
        """Every reading of a question holding words never heard, each name a slot or
        frame and the slots in any order, nearest taught shapes with each. A slot left
        unbound claims less than one bound, so the readings with fewest unbound slots go
        first, nearest within that; each word is counted by the first reading whose
        solving finds anything for it, as borrowing reads a question."""
        from itertools import combinations, permutations

        names = [n for _, _, n in self.shaper.template(question)[1]][:4]
        out = []
        for k in range(1, len(names) + 1):
            for chosen in combinations(names, k):
                if not any(w in chosen for _, _, w in spans):
                    continue
                for order in permutations(chosen):
                    near, shapes = self.nearest(question, list(order))
                    if shapes:
                        out.append((near, order, shapes))
        out.sort(key=lambda r: (sum(not self.individuals.known(n) for n in r[1]), -r[0]))
        return out

    def heard_in(self, question: str) -> None:
        """A question holding a word never heard is one context of it, answered or not:
        the names its slot could take given the question's other names, by the plans of
        the nearest taught shape with the answer left free. Each hearing keeps only the
        names every earlier one allowed, as a child narrows a new word over the
        situations it is heard in."""
        spans = self.unheard(question)
        if not spans:
            return
        names = [n for _, _, n in self.shaper.template(question)[1]][:4]
        unheard = {u for _, _, u in spans}
        done: set[str] = set()
        for _, order, shapes in self.readings(question, spans):
            plans = [p for sh in shapes for (raw,) in self.db.execute(
                "SELECT plan FROM learnt WHERE shape = ? AND hits > misses", (sh,))
                if not (p := self.read_plan(raw)).get("count")]
            for free, w in enumerate(order):
                if w not in unheard or w in done:
                    continue
                bound = {i: n for i, n in enumerate(order) if i != free and self.individuals.known(n)}
                if bound:
                    here = {e for p in plans for e, _ in self.solve(p, bound, free)}
                else:
                    # with nothing else to pin it, a new word is narrowed by the situation
                    # it is heard in, never by every name ever heard
                    here = {n for n in self.individuals.names_in_mind()
                            if any(self.solve(p, {free: n}, "a") for p in plans)}
                here -= set(names)
                if not here:
                    continue
                done.add(w)
                row = self.db.execute("SELECT names, heard FROM contexts WHERE word = ?",
                                      (w,)).fetchone()
                if row:
                    here &= set(json.loads(row[0]))
                self.db.execute("INSERT OR REPLACE INTO contexts VALUES (?, ?, ?)",
                                (w, json.dumps(sorted(here)), (row[1] if row else 0) + 1))
        self.store.written()

    def pinned(self, word: str) -> str | None:
        """The one name every hearing of a word left, where two or more agreed: one
        hearing cannot show a word names something rather than being frame, as one fact
        cannot settle an alias. Hearings that left nothing in common mark a word of the
        frame ('shade', 'a wage'), which names nothing and is never pinned."""
        row = self.db.execute("SELECT names, heard FROM contexts WHERE word = ?",
                              (word,)).fetchone()
        left = json.loads(row[0]) if row else []
        return left[0] if len(left) == 1 and row[1] >= 2 else None

    def voted(self, word: str) -> str | None:
        """Of the names a word's hearings still allow, the one MiniLM puts it nearest,
        where that is nearer than the next by `MARGIN`: what everyone knows of a word
        decides at its first hearing, and the hearings decide once they leave one name. A
        word of the frame ('shade') is about as near every name it allows, so it is never
        voted for any."""
        row = self.db.execute("SELECT names FROM contexts WHERE word = ?",
                              (word,)).fetchone()
        left = json.loads(row[0]) if row else []
        if len(left) < 2:
            return None
        w, *vs = vectors([word, *left])
        near = sorted(((float(w @ v), n) for v, n in zip(vs, left)), reverse=True)
        return near[0][1] if near[0][0] - near[1][0] >= MARGIN else None

    def alias(self, question: str, want: str) -> str:
        """A lesson naming words never heard: each is read as a slot of the nearest
        taught shape, and that shape's plans are solved with the answer bound and the
        word's slot free. Two wordings are one name where their solutions agree, so each
        name found counts a share of one, and a lesson that many names fit says little.
        A lesson that found other names counts against one. The question comes back
        with whatever lessons have settled put in."""
        spans = self.unheard(question)
        goals = self.individuals.holding(want)[:1]
        if not spans or not goals:
            return question
        names = [n for _, _, n in self.shaper.template(question)[1]][:4]
        unheard = {u for _, _, u in spans}
        found: dict[str, set] = {}
        for near, order, shapes in self.readings(question, spans):
            plans = [self.read_plan(p) for sh in shapes for (p,) in self.db.execute(
                "SELECT plan FROM learnt WHERE shape = ? AND hits > misses", (sh,))]
            for free, w in enumerate(order):
                if w not in unheard or found.get(w):
                    continue
                bound = {i: n for i, n in enumerate(order) if i != free and self.individuals.known(n)}
                bound["a"] = goals[0][2:]
                # a name the question says is never what another of its words stands for
                found[w] = {e for plan in plans if not plan.get("count")
                            for e, _ in self.solve(plan, bound, free) if e not in names}
        # a name two of the words found is not evidence for either
        shared = [n for w, ends in found.items() for n in ends
                  if any(n in other for v, other in found.items() if v != w)]
        found = {w: ends - set(shared) for w, ends in found.items()}
        for w, ends in found.items():
            # one fact asked of several times is one piece of evidence
            if (w, want) in self.aliased:
                continue
            self.aliased.add((w, want))
            # credited to every ending of the word, so 'wax forms' learns from 'many wax
            # forms'
            if not ends:
                continue
            for tail in (" ".join(w.split()[i:]) for i in range(len(w.split()))):
                for n, share in [(n, 1 / len(ends)) for n in ends] + [("", 0)]:
                    self.db.execute("INSERT INTO aliases VALUES (?, ?, ?, 1) ON CONFLICT("
                                    "word, name) DO UPDATE SET hits = hits + excluded.hits, "
                                    "found = found + 1", (tail, n, share))
        return self.unaliased(question)

    def answer(self, question) -> str:
        # what the plans of either wording's own shape find comes before anything
        # borrowed or joined: a wording taught before an alias settled holds the lessons
        self.heard_in(question.text)
        self.traced = None
        # what the question's names and verb meet at, read from its grammar, is the
        # answer whatever else is found, so it is asked first and nothing else is asked
        # when it answers: most checks are answered so, and paid for every plan before it
        meet = self.met(question.text)
        if meet:
            self.last_notes = ["(found)", f"meet:{meet}", "by:meet"]
            return meet
        # a phrase holding a relative clause is what its clause names: asked of the walk
        # first, it leaves a question the walk can answer
        put = self.resolved(question.text)
        if put != question.text and (meet := self.met(put)):
            self.last_notes = ["(found)", f"meet:{meet}", "by:relative"]
            return meet
        put = self.unaliased(question.text)
        said, self.walker.spent = None, 0
        try:
            said = next((self.latest(found) for text in dict.fromkeys((put, question.text))
                         if (found := self.answers(*self.shaper.shape(text), text, borrow=False))),
                        None)
            if said is None:
                said = self.answered(put)
            # a pin is used only where the question as heard finds nothing: frame words a
            # question's names keep company with agree across hearings too ('What is the
            # number of X in the cellar?' pinned 'number'), and they broke answers found
            again = self.unaliased(question.text, heard=True) if said is None else put
            if again != put:
                said = self.answered(again)
        except Spent:
            pass
        given_up, self.walker.spent = self.walker.spent > EFFORT, None

        # what the plans found from outside focus is another conversation's, as often as
        # not; what is in focus and fits the asked slot comes first
        # and what a plan found stands only where focus would also consider it: a plan
        # reads a told event, and a sentence not yet told is predicted, not found
        planned, scores = said, self.focus(question.text)
        if self.trace:
            self.traced = scores
        ranked = sorted(scores, key=lambda n: (math.prod(scores[n]), n),
                        reverse=True) if scores else []
        fit = ranked[0] if ranked else None
        if said is None or not self.in_focus(said) or (ranked and said not in ranked[:TOP]):
            said = fit or said
        self.last_notes = (["(gave up)"] if given_up else []) + (
            ["(nothing)"] if said is None else ["(found)"]) + [
            f"plans:{planned}", f"focus:{fit}", "meet:None", "by:" + (
                "none" if said is None else "plans" if said == planned else "focus")]
        return "I don't know." if said is None else said

    def in_focus(self, name: str) -> bool:
        held = self.individuals.nodes(name)
        return self.db.execute(
            "SELECT 1 FROM edges JOIN events ON events.id = edges.event WHERE edges.node IN "
            f"({','.join('?' * len(held))}) AND {self.mind.within('events.turn')} LIMIT 1",
            (*held, *self.mind.bounds())).fetchone() is not None

    def met(self, question: str) -> str | None:
        """What a question's sources meet at, read at the asked slot: spreading activation
        (Quillian; ACT-R, John's walk). Each name the question says fires its
        individuals in mind, and its verb the episode's events of that lemma; each
        spreads `STEPS` steps, divided by fan, an event faded by its base level, and
        nothing below `FLOOR` spreads on, so what a question touches is bounded however
        long the history. An event is read where every name reached it within two steps
        and it is of the question's verb, scored by the product of what each source
        brought it. A question needs no lesson in its wording, as a plan's shape does;
        what it asks is in its grammar."""
        self._now = self.mind.now()
        got = self.sources(question)
        if got is None:
            return None
        groups, names, lemma, free = got
        if not groups:
            return None
        spread, depth = zip(*[self.spread(g) for g in groups])
        def reached(e: str) -> bool:
            # a verb alone meets nothing: an event of another story is read only where a
            # name the question says met it there
            if not names and not self.mind.held(self.walker.event(e)[1]):
                return False
            return all(e in d and d[e] <= (0 if lemma is not None and i == len(depth) - 1
                                           else 2) for i, d in enumerate(depth))

        def rank(label: str) -> int | None:
            for r, f in enumerate(free):
                if label == f or (f == "prep:*" and label.startswith("prep:")
                                  and ">" not in label):
                    return r
            return None

        def named(node: str) -> bool:
            return any(self.individuals.is_(node, f"n:{n}") for n in names)

        # what each event holds at the asked slot is scored by the product of what each
        # source brought the event, the slot lessons rank first scored most
        score: dict[str, float] = {}
        for e in {n for s in spread for n in s if n.startswith("e:")}:
            if not reached(e):
                continue
            a = math.prod(s.get(e, 0.0) for s in spread)
            for label, d, n in self.walker.around(e):
                if d != 1 or n.startswith("f:") or (r := rank(label)) is None:
                    continue
                if n.startswith("e:"):
                    # a possessed noun ('her veil') is an event whose `self` is the name
                    n = next((m for lab, dd, m in self.walker.around(n) if lab == "self" and dd == 1),
                             None)
                    if n is None:
                        continue
                if named(n):
                    continue
                score[n] = score.get(n, 0.0) + a / (1 + r)
        for n, _ in sorted(score.items(), key=lambda x: -x[1]):
            said = self.individuals.describe(n)
            if said and not said_in(said, question):
                return said
        return None

    def sources(self, question: str):
        """Where a question's activation starts: a group a source (each name it says,
        and its verb), the names, the verb's lemma and the links the asked slot could
        be. None for a question that asks no slot, or about someone never mentioned."""
        got = self.reader.pattern(question)
        if got is None:
            # no slot asked, so nothing to read where the sources meet
            return None
        lemma, free, bound, named, mood_, wh = got
        names = list(dict.fromkeys([n for _, n in bound] + list(named)))
        if free == ["prep:*"]:
            free = self.circumstances(wh) + ["prep:*"]
        if not all(self.individuals.known(n) for n in names):
            return None
        groups: list[dict[str, float]] = []
        for name in names:
            # a name fires its individuals in mind, as recall by a cue reaches what is
            # active; where none is, the name itself, which reaches its latest. Every
            # individual it labels, each by its base level, lost late checks (the commit
            # that says so)
            held = self.individuals.of(name, episode=True) or [f"n:{name}"]
            groups.append({n: 1.0 / len(held) for n in held})
        if lemma is not None:
            # a verb fires its events in mind the same way, and its latest where none is
            events = [f"e:{e}" for e in self.mind.events_in_mind(lemma, mood_, LEMMA)] or [
                f"e:{e}" for (e,) in self.db.execute(
                    "SELECT id FROM events WHERE lemma = ? AND mood = ? ORDER BY id DESC "
                    "LIMIT ?", (lemma, mood_, LEMMA))]
            if events:
                groups.append({e: self.fresh(e) / len(events) for e in events})
        return groups, names, lemma, free

    def fresh(self, event: str) -> float:
        """How strongly an event is in mind now: ACT-R's base level of one hearing."""
        return (self._now - self.walker.event(event)[1] + 1) ** -DECAY

    def spread(self, start: dict[str, float]) -> tuple[dict[str, float], dict[str, int]]:
        """Activation spread from one source for `STEPS` steps: every node's total, and
        the step it was first reached at."""
        total, depth, frontier = dict(start), dict.fromkeys(start, 0), dict(start)
        for step in range(1, STEPS + 1):
            nxt: dict[str, float] = {}
            for node, a in frontier.items():
                if a < FLOOR:
                    continue
                steps = [s for s in self.walker.around(node) if not s[2].startswith("f:")]
                if not steps:
                    continue
                share = a * PASS / len(steps)
                for _, _, n in steps:
                    nxt[n] = nxt.get(n, 0.0) + (share * self.fresh(n) if n.startswith("e:")
                                                else share)
            for n, a in nxt.items():
                total[n] = total.get(n, 0.0) + a
                depth.setdefault(n, step)
            frontier = nxt
        return total, depth

    def focused(self, question: str) -> str | None:
        """The name in this episode that best fits the asked slot: how strongly it is in focus,
        each hearing fading as ACT-R's base level does, times how often it has filled the
        asked verb's slot, out of everything it has filled, times how often the wh-word's
        answers bore its mark."""
        scores = self.focus(question)
        if not scores:
            return None
        return max((a * f * w, n) for n, (a, f, w) in scores.items())[1]

    def focus(self, question: str) -> dict[str, tuple[float, float, float]] | None:
        """Each name focus weighs for a question, with its three factors: how strongly it
        is in focus, how it fits the asked slot, and how often the wh-word asks for its
        mark. None where the question is not guessed at."""
        slot = self.reader.blank(question)
        # a question about someone never mentioned is not guessed at
        if slot is None or not all(self.individuals.known(f) for f in self.shaper.shape(question)[1]):
            return None
        now = self.mind.now()
        act: dict[str, float] = {}
        for node, turn in self.db.execute(
                "SELECT edges.node, events.turn FROM edges JOIN events ON events.id = "
                f"edges.event WHERE {self.mind.within('edges.event', events=True)} AND edges.node "
                "NOT LIKE 'e:%' AND edges.node NOT LIKE 'f:%'", self.mind.bounds(events=True)):
            act[node] = act.get(node, 0.0) + (now - self.mind.moved(turn) + 1) ** -DECAY
        labels = self.db.execute("SELECT COUNT(DISTINCT link) FROM filled").fetchone()[0] or 1

        def fit(name: str) -> float:
            # a share of what the name filled: in the asked verb's slot, and, much less,
            # by the link alone, so a slot no one here filled still ranks
            rows = self.db.execute("SELECT link, lemma = ?, n FROM filled WHERE name = ?",
                                   (slot[0], name)).fetchall()
            total = sum(n for _, _, n in rows)
            here = sum(n for lab, verb, n in rows if lab == slot[1] and verb)
            link = sum(n for lab, _, n in rows if lab == slot[1])
            return (here / (total + PRIOR)
                    + 0.1 * (link + 0.5) / (total + 0.5 * labels))

        asks = dict(self.db.execute("SELECT mark, n FROM asks WHERE wh = ?",
                                    (self.shaper.wh_word(question),)).fetchall())

        def asked_for(name: str) -> float:
            # how often this wh-word's answers bore the name's mark, as lessons have it
            mark = self.individuals.mark(name)
            return 0.5 if mark is None else (asks.get(mark, 0) + 1) / (sum(asks.values()) + 2)

        # the question's own things are not its answer: each of its names finds the
        # individual most in mind of those it labels, as a reader's 'the pig' finds the
        # pig the story is about, whatever more was said of it. A word that is no
        # individual ('red') is its own description
        own = set()
        for f in self.shaper.shape(question)[1]:
            found = [n for n in self.individuals.nodes(f) if n in act and n.startswith("i:")]
            if found:
                own.add(max(found, key=act.get))
        # in mind as an individual, said by its description; how it fits and what it
        # is asked for are what everyone knows, so they are read by its label
        out: dict[str, tuple[float, float, float]] = {}
        for node, a in act.items():
            said, name = self.individuals.describe(node), self.individuals.label(node)
            if node in own or (not node.startswith("i:") and said_in(said, question)):
                continue
            was = out.get(said)
            out[said] = (a + (was[0] if was else 0.0), fit(name), asked_for(name))
        return out

    def circumstances(self, wh: str) -> list[str]:
        """The links a word asking for a circumstance ('where', 'when') has had its
        answers hang by, learnt from lessons, as the parse marks only that it asks, the
        commonest first. `met` tries them first and any other oblique after."""
        got = [lab for (lab,) in self.db.execute(
            "SELECT link FROM circumstances WHERE wh = ? ORDER BY n DESC", (wh,))]
        return got

    def learn_circumstance(self, question: str, want: str) -> None:
        """A lesson's answer to a word asking for a circumstance: the oblique the answer
        hangs by in the episode's event the question matches."""
        got = self.reader.pattern(question)
        if got is None or got[1] != ["prep:*"]:
            return
        lemma, _, bound, _, mood_, wh = got
        for eid in self.mind.events_in_mind(lemma, mood_, 50):
            edges = self.db.execute("SELECT label, node FROM edges WHERE event = ? AND node "
                                    "NOT LIKE 'f:%'", (eid,)).fetchall()
            if not all(any(lab == label and self.individuals.is_(n, f"n:{name}") for lab, n in edges)
                       for label, name in bound):
                continue
            for lab, n in edges:
                if lab.startswith("prep:") and not n.startswith("e:") and said_in(
                        want, self.individuals.describe(n)):
                    self.db.execute("INSERT INTO circumstances VALUES (?, ?, 1) ON CONFLICT"
                                    "(wh, link) DO UPDATE SET n = n + 1", (wh, lab))
                    return

    def heard_count(self, shape: str, fillers: list[str], want: str) -> None:
        """A lesson whose answer may be a number word: every walk from the question's
        first name, with how many it reached, and none for a walk this shape had before
        that reaches nothing now."""
        ways = self.walker.walks(f"n:{fillers[0]}")
        rows = [(shape, key, want, len(ends)) for key, (ends, _) in ways.items()]
        rows += [(shape, key, want, 0) for (key,) in self.db.execute(
            "SELECT DISTINCT walk FROM counts WHERE shape = ?", (shape,)) if key not in ways]
        self.db.executemany("INSERT INTO counts VALUES (?, ?, ?, ?)", rows)

    def answered(self, text: str, depth: int = 0) -> str | None:
        """An answer from the plans of the question's shape, or, where they find nothing,
        from the question taken apart: its innermost noun phrase holding a name is asked
        as a question of its own ('Who is Kroudroth's cousin?'), its answer put in its
        place, and what is left asked again. Plans are whole paths and do not compose; a
        question's grammar does."""
        shape, fillers = self.shaper.shape(text)
        found = self.answers(shape, fillers, text, borrow=False)
        if found:
            return self.latest(found)
        if depth < 3 and (said := self.joined(text, depth)) is not None:
            return said
        found = self.answers(shape, fillers, text)
        if found:
            return max(found, key=lambda f: f[1])[0]
        # borrowed before taken apart, by the first house: taking 'X's lanterns' apart
        # first read 0.756 0.784 0.779 against 0.760 0.788 0.809
        if depth >= 3 or not all(self.individuals.known(f) for f in fillers):
            return None
        said = self.apart(text, depth)
        if said is None and depth == 0 and (found := self.reaching(text, shape)):
            said = self.latest(found)
        if said is None and depth == 0 and (found := self.kinded(text, shape, fillers)):
            said = self.latest(found)
        return said

    @staticmethod
    def latest(found: list) -> str:
        return max(found, key=lambda f: f[1])[0]

    def roles(self, shapes: list[str]) -> set[str]:
        """The links an answer hangs by at the end of these shapes' plans: 'prep:in' for
        where a thing is kept, 'nummod' for how many. A name's kind is the links it is
        held by, so an answer held by none of these is of another kind."""
        out = set()
        for shape in shapes:
            for (p,) in self.db.execute(
                    "SELECT plan FROM learnt WHERE shape = ? AND hits > misses", (shape,)):
                plan = self.read_plan(p)
                if plan["steps"] and not plan.get("count") and plan["steps"][-1][1] == 1:
                    out.add(plan["steps"][-1][0])
        return out

    def kinded(self, text: str, shape: str, fillers: list[str],
               limit: int = 6) -> list[tuple[str, tuple]]:
        """Where no plan reaches, a fact told in a shape no lesson's fact was: the
        shortest path from the question's first name to a name in the role the answer
        plays at the end of the shape's plans, through nothing that did not happen, with
        each other name of the question two steps or fewer from the path. 'Gopaiff has the
        fishing floats tucked away in the dairy' has the dairy at the end of a 'prep:in'
        as 'keeps the floats in the dairy' has, by a longer way."""
        if not fillers:
            return []
        roles = self.roles([shape]) or self.roles(self.nearest(text, fillers)[1])
        if not roles:
            return []
        start = f"n:{fillers[0]}"
        frontier, seen, found = [[start]], {start}, []
        # whether an event is two steps or fewer from a name, asked of the same events
        # for every way through them, so found once a question
        near: dict = {}

        def close(n: str, f: str) -> bool:
            got = near.get((n, f))
            if got is None:
                got = near[(n, f)] = bool(self.walker.paths(n, f"n:{f}", limit=2, most=1))
            return got

        for _ in range(limit):
            nxt = []
            for path in frontier:
                for label, direction, node in self.walker.around(path[-1]):
                    if self.individuals.among(node, seen):
                        continue
                    if node.startswith("e:") and self.walker.mood(node):
                        continue
                    way = path + [(label, direction), node]
                    if (not node.startswith("e:") and direction == 1 and label in roles
                            and not said_in(self.individuals.describe(node), text) and all(
                                any(close(n, f) for n in way[::2]
                                    if n.startswith("e:")) for f in fillers[1:])):
                        turns = [self.walker.event(n)[1] for n in way[::2] if n.startswith("e:")]
                        found.append((self.individuals.describe(node), (max(turns, default=0),)))
                    nxt.append(way)
            if found:
                return found
            for way in nxt:
                seen.add(way[-1])
            frontier = nxt[:2000]
        return []

    def joined(self, text: str, depth: int) -> str | None:
        """A question holding a clause about something ('the things X keeps in the
        cellar') answered as a join: the clause is a relation some taught shape holds,
        solved for the thing with the clause's names bound, and the thing put in the
        clause's place. A shape's plans are its relation's disjuncts, one a wording it was
        taught in, so the clause is found however the fact was told."""
        for a, b in self.inner(text):
            names = [n for _, _, n in self.shaper.template(text[a:b])[1] if self.individuals.known(n)]
            if not names:
                continue
            for thing in self.related(text[a:b], names):
                said = self.answered(text[:a] + thing.title() + text[b:], depth + 1)
                if said is not None:
                    return said
        return None

    def related(self, phrase: str, names: list[str]) -> list[str]:
        """What a phrase's clause leaves open, from the taught shapes with a variable for
        some of its names and one more, nearest the phrase first. Some, because a name
        the graph knows may be the shape's frame ('cousin' in "X's cousin"). Each way of
        binding the names to its variables with the rest free, and what the first binding
        to reach anything finds, latest first."""
        from itertools import combinations, permutations

        if self._followed is None or self._followed[0] != self._lessons:
            rows: dict[str, list] = {}
            for shape, plan in self.db.execute(
                    "SELECT shape, plan FROM learnt WHERE hits > misses "
                    "ORDER BY hits - misses DESC, hits DESC").fetchall():
                rows.setdefault(shape, []).append(self.read_plan(plan))
            sigs = {shape: next((set(p["sig"]) for p in plans if p.get("sig")), set())
                    for shape, plans in rows.items()}
            self._followed = (self._lessons, rows, sigs)
        _, rows, sigs = self._followed
        shapes = []
        for k in range(len(names), 0, -1):
            for chosen in combinations(names, k):
                mine = set(self.shaper.signature(phrase, list(chosen)))
                for shape, sig in sigs.items():
                    if shape.count("<") == k:
                        shapes.append((len(mine & sig) / max(1, len(mine | sig)), chosen, shape))
        shapes.sort(key=lambda r: -r[0])
        for _, chosen, shape in shapes[:8]:
            plans = [p for p in rows[shape] if not p.get("count")]
            variables = list(range(len(chosen))) + ["a"]
            for free in variables:
                rest = [v for v in variables if v != free]
                for order in permutations(chosen):
                    bound = dict(zip(rest, order))
                    found = [f for plan in plans for f in self.solve(plan, bound, free)
                             if f[0] not in names]
                    if found:
                        return [e for e, _ in sorted(found, key=lambda f: f[1], reverse=True)]
        return []

    def solve(self, plan: dict, bound: dict, free) -> list[tuple[str, tuple]]:
        """A plan read as a pattern and matched with any of its variables free: the
        path's first node is variable 0, its end 'a', and each hook's end the slot it
        names. Matched outward from a bound variable; the values found for `free`, each
        with the latest turn of what happened on the way."""
        steps = plan["steps"]
        edges = [(("p", i), ("p", i + 1), lab, d) for i, (lab, d) in enumerate(steps)]
        var = {0: ("p", 0), "a": ("p", len(steps))}
        for h, (k, i, hsteps) in enumerate(plan["attach"]):
            prev = ("p", i)
            for j, (lab, d) in enumerate(hsteps):
                edges.append((prev, ("h", h, j), lab, d))
                prev = ("h", h, j)
            var[k] = prev
        if free not in var or not bound or not all(v in var for v in bound):
            return []
        want = {var[v]: f"n:{n}" for v, n in bound.items()}
        root = next(iter(want))
        moods = plan.get("moods", {})
        partials = [{root: want[root]}]
        todo = list(edges)
        while todo and partials:
            seen = partials[0]
            edge = next((e for e in todo if (e[0] in seen) != (e[1] in seen)), None)
            if edge is None:
                break
            todo.remove(edge)
            u, v, lab, d = edge
            here, there, way = (u, v, d) if u in seen else (v, u, -d)
            nxt = []
            for got in partials:
                for node in self.walker.around_as(got[here], lab, way):
                    if self.individuals.among(node, set(got.values())):
                        continue
                    if there in want and not self.individuals.is_(node, want[there]):
                        continue
                    # never through what did not happen where the lessons' did, or the
                    # other way round, as a plan is followed
                    if node.startswith("e:") and there[0] == "p" and self.walker.mood(node) not in \
                            moods.get(str(there[1]), [self.walker.mood(node)]):
                        continue
                    nxt.append({**got, there: node})
            partials = nxt[:2000]
        out = []
        for got in partials:
            end = got.get(var[free], "")
            if not end or end.startswith("e:") or len(got) < len(
                    {n for e in edges for n in e[:2]}):
                continue
            turns = [self.walker.event(n)[1] for n in got.values()
                     if n.startswith("e:") and not self.walker.mood(n)]
            out.append((self.individuals.describe(end), (max(turns, default=0),)))
        return out

    def apart(self, text: str, depth: int) -> str | None:
        for a, b in self.inner(text):
            phrase = text[a:b]
            for ask in (f"Who is {phrase}?", f"What is {phrase}?"):
                named = self.answered(ask, depth + 1)
                if named is None:
                    continue
                said = self.answered(text[:a] + named.title() + text[b:], depth + 1)
                if said is not None:
                    return said
        return None

    def inner(self, text: str) -> list[tuple[int, int]]:
        """The noun phrases of a question that hold a name and something said of it, as
        character spans, innermost first: 'the person who repairs clocks' before 'the
        cousin of the person who repairs clocks'. A phrase is a noun's whole subtree, kept
        where a possessor, a clause or a preposition hangs off it and the wh-word is not
        inside it."""
        rows = self.reader.spans(text)
        kids: dict[int, list[int]] = {}
        for i, r in enumerate(rows):
            if r[4] != i:
                kids.setdefault(r[4], []).append(i)

        def subtree(i: int) -> list[int]:
            out = [i]
            for k in kids.get(i, []):
                out += subtree(k)
            return out

        found = []
        for i, (_, _, tag, _, _, pos) in enumerate(rows):
            if pos not in ("NOUN", "PROPN"):
                continue
            if not any(rows[k][3] in ("nmod:poss", "acl:relcl", "acl", "nmod")
                       for k in kids.get(i, [])):
                continue
            span = sorted(subtree(i))
            # the question's own wh-word, not a relative one ('the person who repairs')
            if span[0] == 0:
                continue
            if len(span) >= len(rows) - 2 or not any(
                    rows[k][5] == "PROPN" or self.individuals.known(rows[k][1].lower()) for k in span
                    if k != i):
                continue
            a = rows[span[0]][0]
            b = rows[span[-1]][0] + len(rows[span[-1]][1])
            while text[a:b].lower().startswith(("the ", "a ")):
                a = text.index(" ", a) + 1
            found.append((b - a, a, b))
        return [(a, b) for _, a, b in sorted(found)]

    def resolved(self, text: str) -> str:
        """The question with a phrase holding a relative clause ('the one who found a
        shell') put as the individual its clause names. A relative clause is a question
        about the noun it hangs from, so its own words, the relative word first, are asked
        of the walk ('who found a shell?') and what they meet stands in the phrase's place.
        The relative word is the parse's (`PronType=Rel`); a clause the walk cannot answer
        leaves the question as it was. What the parse gives, each such phrase's span and its
        clause's, is kept with the parses."""
        for a, b, start, end in self.reader.relatives(text):
            who = self.met(text[start:end] + "?")
            if who:
                return text[:a] + who.title() + text[b:]
        return text

    # -- a conversation -------------------------------------------------------

    def turn(self, turn: int, text: str) -> str | None:
        """One turn of a conversation, sorted by the arm itself. A question is answered
        and kept, never stored as a telling. The turn after a question is the reaction to
        the answer given: what it names that the question did not is the answer, and a
        reaction naming nothing and not negated confirms the answer given. Every other
        turn is a telling."""
        if asked(text):
            self.mind.remind(text)
            said = self.answer(_Asked(text))
            self.pending = (text, said)
            # what is taught is the node; what is said is the phrase it was heard in
            return said if said == "I don't know." else self.mouth.say(text, said)
        # a reaction is about the answer, not the world: no event in it has a named
        # subject ('it's the shed', 'that's right'), where a telling has ('Ada keeps...')
        about_world = any(label.startswith("nsubj") and t.startswith("n:")
                           for ev in self.reader.read(text) for label, t in ev["edges"])
        if self.pending is not None and not about_world:
            question, said = self.pending
            self.pending = None
            named, negated = self.reaction(text, question)
            if named:
                self.teach(question, named)
                return None
            if not negated and said != "I don't know.":
                self.teach(question, said)
                return None
            if negated:
                return None
        self.pending = None
        self.hear(turn, text)
        return None

    def reaction(self, text: str, question: str) -> tuple[str | None, bool]:
        """What a reaction names that the question did not, and whether it says no."""
        rows = self.reader.reaction_rows(text)
        negated = any(no for no, _, _, _, _ in rows)
        # a reaction that says yes and not no confirms the answer given and names
        # nothing: 'right' in 'Yes, that's right' is an adjective as a colour is, and
        # once a story has said 'right', every confirmed answer was taught as 'right'
        if not negated and any(yes for _, yes, _, _, _ in rows):
            return None, False
        for _, _, names, name, number in rows:
            # 'right' in 'that's right' is an adjective as a colour is, and names nothing
            # here, so a name must be one the graph holds
            if names and not said_in(name, question) and (number or self.individuals.holding(name)):
                return name, negated
        return None, negated

    def export(self) -> dict:
        return {table: self.db.execute(f"SELECT * FROM {table}").fetchall()
                for table in CARRIED}

    def dials(self) -> dict:
        n_events, n_edges = (self.db.execute("SELECT COUNT(*) FROM events").fetchone()[0],
                             self.db.execute("SELECT COUNT(*) FROM edges").fetchone()[0])
        individuals = self.db.execute("SELECT COUNT(DISTINCT node) FROM called").fetchone()[0]
        return {"model": self.model, "version": VERSION, "events": n_events, "edges": n_edges,
                "individuals": individuals,
                "shapes": self.db.execute("SELECT COUNT(DISTINCT shape) FROM learnt")
                .fetchone()[0], "parsed": self.reader.parsed}

    def close(self) -> None:
        self.situations.save()
        self.store.close()
        self.reader.close()


def asked(text: str) -> bool:
    return text.rstrip().endswith("?")


class _Asked:
    def __init__(self, text: str) -> None:
        self.text = text

