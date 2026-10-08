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
from pathlib import Path

from unfused.aliaser import Aliaser
from unfused.follower import Follower
from unfused.hearer import Hearer
from unfused.individuals import Individuals
from unfused.meeter import DECAY, Meeter
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
from unfused.plans import Plans
from unfused.reader import Reader
from unfused.said import said_in
from unfused.shaper import Shaper
from unfused.situations import Situations
from unfused.solver import Solver
from unfused.storage import CARRIED, Store
from unfused.walker import EFFORT, Spent, Walker

# how many hearings a name's fit is shrunk by towards nothing, so one hearing is not a
# certainty
PRIOR = 2.0

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
        # the plans lessons left, and which taught shapes a question is nearest
        self.plans = Plans(self.db, self.shaper)
        # a plan read as a pattern, matched with any variable free
        self.solver = Solver(self.db, self.walker, self.individuals, self.plans, self.shaper)
        # words never heard, read as names that were
        self.aliaser = Aliaser(self.store, self.shaper, self.individuals, self.plans,
                               self.solver)
        # a plan followed from a question's names
        self.follower = Follower(self.db, self.walker, self.individuals, self.plans,
                                 self.shaper, self.numbers)
        # what a question's sources meet at
        self.meeter = Meeter(self.db, self.reader, self.mind, self.individuals, self.walker)






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

    # -- words never heard ----------------------------------------------------

    def answer(self, question) -> str:
        # what the plans of either wording's own shape find comes before anything
        # borrowed or joined: a wording taught before an alias settled holds the lessons
        self.aliaser.heard_in(question.text)
        self.traced = None
        # what the question's names and verb meet at, read from its grammar, is the
        # answer whatever else is found, so it is asked first and nothing else is asked
        # when it answers: most checks are answered so, and paid for every plan before it
        meet = self.meeter.met(question.text)
        if meet:
            self.last_notes = ["(found)", f"meet:{meet}", "by:meet"]
            return meet
        # a phrase holding a relative clause is what its clause names: asked of the walk
        # first, it leaves a question the walk can answer
        put = self.resolved(question.text)
        if put != question.text and (meet := self.meeter.met(put)):
            self.last_notes = ["(found)", f"meet:{meet}", "by:relative"]
            return meet
        put = self.aliaser.unaliased(question.text)
        said, self.walker.spent = None, 0
        try:
            said = next((self.latest(found) for text in dict.fromkeys((put, question.text))
                         if (found := self.follower.answers(*self.shaper.shape(text), text, borrow=False))),
                        None)
            if said is None:
                said = self.answered(put)
            # a pin is used only where the question as heard finds nothing: frame words a
            # question's names keep company with agree across hearings too ('What is the
            # number of X in the cellar?' pinned 'number'), and they broke answers found
            again = self.aliaser.unaliased(question.text, heard=True) if said is None else put
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
        found = self.follower.answers(shape, fillers, text, borrow=False)
        if found:
            return self.latest(found)
        if depth < 3 and (said := self.joined(text, depth)) is not None:
            return said
        found = self.follower.answers(shape, fillers, text)
        if found:
            return max(found, key=lambda f: f[1])[0]
        # borrowed before taken apart, by the first house: taking 'X's lanterns' apart
        # first read 0.756 0.784 0.779 against 0.760 0.788 0.809
        if depth >= 3 or not all(self.individuals.known(f) for f in fillers):
            return None
        said = self.apart(text, depth)
        if said is None and depth == 0 and (found := self.follower.reaching(text, shape)):
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
                plan = self.plans.read_plan(p)
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
        roles = self.roles([shape]) or self.roles(self.plans.nearest(text, fillers)[1])
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
            for thing in self.solver.related(text[a:b], names):
                said = self.answered(text[:a] + thing.title() + text[b:], depth + 1)
                if said is not None:
                    return said
        return None

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
            who = self.meeter.met(text[start:end] + "?")
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

