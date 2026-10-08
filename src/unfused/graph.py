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

import math
from pathlib import Path

from unfused.aliaser import Aliaser
from unfused.decomposer import Decomposer
from unfused.focuser import Focuser
from unfused.follower import Follower
from unfused.hearer import Hearer
from unfused.individuals import Individuals
from unfused.meeter import Meeter
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
from unfused.recounter import Recounter
from unfused.said import said_in
from unfused.shaper import Shaper
from unfused.situations import Situations
from unfused.solver import Solver
from unfused.storage import CARRIED, Store
from unfused.teacher import Teacher
from unfused.walker import EFFORT, Spent, Walker

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
        # the name that best fits the asked slot
        self.focuser = Focuser(self.db, self.reader, self.mind, self.individuals, self.shaper)
        # a question the plans cannot answer, taken apart
        self.decomposer = Decomposer(self.db, self.reader, self.walker, self.individuals,
                                     self.plans, self.shaper, self.follower, self.solver,
                                     self.meeter)
        # a lesson, and the plans it leaves
        self.teacher = Teacher(self.store, self.walker, self.individuals, self.plans,
                               self.shaper, self.follower, self.aliaser, self.meeter,
                               self.numbers)
        # a sentence heard, written into the graph
        self.hearer = Hearer(self.store, self.reader, self.mind, self.individuals,
                             self.walker, self.situations)
        # a question whose answer is something that happened
        self.recounter = Recounter(self.store, self.reader, self.mind)
        self.mouth = Mouth(self)
        self.last_notes: list[str] = []
        self.traced: dict | None = None
        # a question this arm answered, waiting for the turn that reacts to it
        self.pending: tuple[str, str] | None = None

    def hear(self, turn: int, text: str) -> None:
        self.hearer.hear(turn, text)

    def teach(self, question: str, answer: str) -> None:
        self.teacher.teach(question, answer)

    def answer(self, question) -> str:
        # what the plans of either wording's own shape find comes before anything
        # borrowed or joined: a wording taught before an alias settled holds the lessons
        self.aliaser.heard_in(question.text)
        self.traced = None
        # a question whose frame lessons answered with events is answered with one
        if event := self.recounter.answer(question.text):
            self.last_notes = ["(found)", f"event:{event}", "by:recount"]
            return event
        # what the question's names and verb meet at, read from its grammar, is the
        # answer whatever else is found, so it is asked first and nothing else is asked
        # when it answers: most checks are answered so, and paid for every plan before it
        meet = self.meeter.met(question.text)
        if meet:
            self.last_notes = ["(found)", f"meet:{meet}", "by:meet"]
            return meet
        # a phrase holding a relative clause is what its clause names: asked of the walk
        # first, it leaves a question the walk can answer
        put = self.decomposer.resolved(question.text)
        if put != question.text and (meet := self.meeter.met(put)):
            self.last_notes = ["(found)", f"meet:{meet}", "by:relative"]
            return meet
        put = self.aliaser.unaliased(question.text)
        said, self.walker.spent = None, 0
        try:
            said = next((self.decomposer.latest(found) for text in dict.fromkeys((put, question.text))
                         if (found := self.follower.answers(*self.shaper.shape(text), text, borrow=False))),
                        None)
            if said is None:
                said = self.decomposer.answered(put)
            # a pin is used only where the question as heard finds nothing: frame words a
            # question's names keep company with agree across hearings too ('What is the
            # number of X in the cellar?' pinned 'number'), and they broke answers found
            again = self.aliaser.unaliased(question.text, heard=True) if said is None else put
            if again != put:
                said = self.decomposer.answered(again)
        except Spent:
            pass
        given_up, self.walker.spent = self.walker.spent > EFFORT, None

        # what the plans found from outside focus is another conversation's, as often as
        # not; what is in focus and fits the asked slot comes first
        # and what a plan found stands only where focus would also consider it: a plan
        # reads a told event, and a sentence not yet told is predicted, not found
        planned, scores = said, self.focuser.focus(question.text)
        if self.trace:
            self.traced = scores
        ranked = sorted(scores, key=lambda n: (math.prod(scores[n]), n),
                        reverse=True) if scores else []
        fit = ranked[0] if ranked else None
        if said is None or not self.focuser.in_focus(said) or (ranked and said not in ranked[:TOP]):
            said = fit or said
        self.last_notes = (["(gave up)"] if given_up else []) + (
            ["(nothing)"] if said is None else ["(found)"]) + [
            f"plans:{planned}", f"focus:{fit}", "meet:None", "by:" + (
                "none" if said is None else "plans" if said == planned else "focus")]
        return "I don't know." if said is None else said

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
            # a lesson whose answer was something that happened leaves no plan to a name
            if self.recounter.learn(question, text, said):
                return None
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

