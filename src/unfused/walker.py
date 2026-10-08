"""Walker: the graph as steps from a node, and the walks over them.

Every step a node has is kept across sentences and questions, since a walk over a hub asks for
the same node's steps thousands of times; a sentence drops only the nodes it gives a new step
(`forget`). A question's effort is the steps it looks at, counted in `spent` while it is being
answered and past `EFFORT` the walk raises `Spent`. Shortest paths come from a search that
copies no path (`paths`), and what a later event replaced is a lookup (`replaced`).
"""

from __future__ import annotations

import json

from unfused.individuals import Individuals
from unfused.mind import Mind
from unfused.storage import Store

# how many of a node's steps of one label a walk follows, the latest first
REACH = 100
# how many nodes' steps one question may look at before its plans give up
EFFORT = 200_000


class Spent(Exception):
    """A question's effort is used up."""


class Walker:
    def __init__(self, store: Store, individuals: Individuals, mind: Mind) -> None:
        self.store = store
        self.db = store.db
        self.individuals = individuals
        self.mind = mind
        # steps looked at by the question being answered, or None outside answering
        self.spent: int | None = None
        # the latest turn of an event that happened with each set of arguments, read
        # off the graph once when first asked and kept up as events are heard
        self._latest: dict | None = None
        # every node's steps, kept across sentences and questions: a walk over a hub asks
        # for the same node's steps thousands of times. A sentence drops only the nodes it
        # gives a new step, so what was loaded stays loaded
        self._steps: dict = {}
        # each node's steps by label and direction, kept beside the steps they sort
        self._labelled: dict = {}
        # each node's steps' next nodes, as a tuple in order and a set, beside the steps
        self._reached: dict = {}

    def forget(self, node: str) -> None:
        """A node was given a new step."""
        self._steps.pop(node, None)

    def forget_names(self) -> None:
        """A name's steps are its individuals in mind, and a new episode or a recalled
        one changes them."""
        for key in [k for k in self._steps if k.startswith("n:")]:
            del self._steps[key]

    def happened(self, heard: list[tuple[dict, int]], turn: int) -> None:
        """Events just heard, each as the telling and its id: the latest turn of what
        happened with their arguments."""
        if self._latest is not None:
            for ev, eid in heard:
                if not ev["mood"] and (got := self.store.args(eid)):
                    self._latest[got] = max(self._latest.get(got, 0), turn)

    def around(self, node: str) -> list[tuple[str, int, str]]:
        """Every step from a node: (label, direction, next node). Direction 1 goes from an
        event to its argument, -1 back from an argument to its event."""
        # a question's effort is the steps it looks at; past `EFFORT` it stops looking
        if self.spent is not None:
            self.spent += 1
            if self.spent > EFFORT:
                raise Spent
        steps = self._steps.get(node)
        if steps is None:
            steps = self._steps[node] = self._around(node)
            if node.startswith("n:"):
                self.individuals.indexed(node[2:])
        return steps

    def around_as(self, node: str, label: str, direction: int) -> list[str]:
        """The nodes one step of this label and direction away, in the order `around`
        gives them and counted as one look as `around` is: a hub's steps are sorted by
        label once, so a pattern does not read all of them for each of its partials."""
        steps = self.around(node)
        kept = self._labelled.get(node)
        if kept is None or kept[0] is not steps:
            by: dict = {}
            for lab, d, nxt in steps:
                by.setdefault((lab, d), []).append(nxt)
            kept = self._labelled[node] = (steps, by)
        return kept[1].get((label, direction), [])

    def _around(self, node: str) -> list[tuple[str, int, str]]:
        if node.startswith("e:"):
            eid = int(node[2:])
            out = [(label, 1, n) for label, n in self.store.outs(eid)]
        else:
            out = []
        # the latest first, so a walk cut short keeps what was heard most recently. A
        # name's steps are its individuals' steps, read through the label's index: those
        # opened in this episode and the latest `REACH` of the rest, as recall by a cue
        # returns a few by recency and never everything ever heard by that name. The
        # episode's alone cost four clozes, which reached a namesake in another story
        if node.startswith("n:"):
            every = self.individuals.nodes(node[2:])
            mind = [n for n in every[1:] if n.startswith("i:")
                    and self.mind.held(int(n[2:].split(".")[0]))]
            held = [every[0]] + list(dict.fromkeys(mind + every[1:1 + REACH]))
        else:
            held = [node]
        out += [(label, -1, f"e:{e}") for e, label in self.store.latest_into(held)]
        return out

    def event(self, node: str) -> tuple[str, int]:
        lemma, turn, _ = self.store.event_row(node)
        return lemma, turn

    def replaced(self, node: str) -> int:
        """Whether a later event that happened has exactly this one's arguments, the
        prepositions aside: 'Mary dropped the football' replaces 'Mary picked up the
        football', 'Mary went to the hallway' replaces 'Mary went to the kitchen', and
        'Mary picked up the football' replaces neither, its arguments being others. The
        turn of the latest that replaces it, or 0. Read from the latest turn each set of
        arguments happened at, so it is a lookup, never a search of what came later."""
        mine = self.store.args(int(node[2:]))
        if not mine:
            return 0
        if self._latest is None:
            by: dict = {}
            for e, label, n in self.db.execute(
                    "SELECT edges.event, edges.label, edges.node FROM edges JOIN events ON "
                    "events.id = edges.event WHERE events.mood = '' AND edges.label NOT LIKE "
                    "'prep:%' AND edges.node NOT LIKE 'f:%'"):
                by.setdefault(e, set()).add((label, n))
            turns = dict(self.db.execute("SELECT id, turn FROM events WHERE mood = ''"))
            self._latest = {}
            for e, got in by.items():
                got = frozenset(got)
                self._latest[got] = max(self._latest.get(got, 0), turns[e])
        latest = self._latest.get(mine, 0)
        return latest if latest > self.event(node)[1] else 0

    def mood(self, node: str) -> str:
        return self.store.event_row(node)[2]

    def paths(self, start: str, goal: str, limit: int = 8, avoid: set | None = None,
              most: int | None = None) -> list[list]:
        """The shortest paths from one node to another, each a list of nodes and steps
        alternating, at most `limit` steps, the first `most` of them in the order the
        nodes' steps are kept. Distances come first, from a search that copies no path;
        then only the ways along which each node is at its distance are followed, so a
        hub that joins every story costs its steps once, not once a path through it."""
        # what is the goal, read once a search: a name's goal is the name and each
        # individual it finds, as `is_` asks, and a search asks of millions of nodes
        if goal.startswith("n:"):
            goals = self.individuals.finds(goal[2:])
        else:
            goals = {goal}
        names: dict = {}
        tracked = self.spent is not None
        steps_of, reached_of = self._steps.get, self._reached

        def reach(node: str) -> tuple:
            """A node's steps, counted as `around` counts them, and with them the nodes
            they lead to, each once, kept beside the steps they are read from."""
            steps = self.around(node) if tracked else steps_of(node)
            if steps is None:
                steps = self.around(node)
            kept = reached_of.get(node)
            if kept is None or kept[0] is not steps:
                nexts = tuple(dict.fromkeys(n for _, _, n in steps))
                kept = reached_of[node] = (steps, nexts, frozenset(nexts))
            return kept

        dist, layer, best = {start: 0}, [start], None
        for depth in range(1, limit + 1):
            nxt_layer = []
            for node in layer:
                _, nexts, every = reach(node)
                # once a node of the layer leads to the goal the rest of it, and the layer
                # it would make, are never read: they are only counted, above. Nor is the
                # last layer, which can only find the goal
                if best is not None:
                    continue
                if not goals.isdisjoint(every):
                    best = depth
                    continue
                if depth == limit:
                    continue
                for nxt in nexts:
                    if nxt in dist:
                        continue
                    if nxt.startswith("i:"):
                        got = names.get(nxt)
                        if got is None:
                            got = names[nxt] = (f"n:{self.individuals.label(nxt)}", f"n:{self.individuals.describe(nxt)}")
                        if got[0] in dist or got[1] in dist:
                            continue
                        if avoid and (nxt in avoid or got[0] in avoid or got[1] in avoid):
                            continue
                    elif avoid and nxt in avoid:
                        continue
                    dist[nxt] = depth
                    nxt_layer.append(nxt)
            if best is not None or not nxt_layer:
                break
            layer = nxt_layer
        if best is None:
            return []
        found: list[list] = []
        # nodes a walk left with nothing found: every way on from them is a dead end,
        # since a node's distance, and so where a walk may go from it, is fixed
        dead: set = set()

        def walk(path: list, depth: int) -> None:
            had = len(found)
            for label, direction, nxt in reach(path[-1])[0]:
                if most is not None and len(found) >= most:
                    return
                if nxt in goals:
                    if depth + 1 == best:
                        found.append(path + [(label, direction), nxt])
                elif depth + 1 < best and dist.get(nxt) == depth + 1 and nxt not in dead:
                    walk(path + [(label, direction), nxt], depth + 1)
            if len(found) == had:
                dead.add(path[-1])

        walk([start], 0)
        return found

    def walks(self, start: str, depth: int = 4, cap: int = 5000) -> dict:
        """Every way of up to `depth` steps from a node to a name, grouped by its steps:
        {steps: (ends, one path)}."""
        out: dict = {}
        frontier = [[start]]
        for _ in range(depth):
            nxt_frontier = []
            for path in frontier:
                for label, direction, nxt in self.around(path[-1]):
                    if self.individuals.among(nxt, path[::2]):
                        continue
                    way = path + [(label, direction), nxt]
                    if not nxt.startswith("e:"):
                        key = json.dumps([list(s) for s in way[1::2]])
                        ends, rep = out.get(key, (set(), way))
                        ends.add(nxt)
                        out[key] = (ends, rep)
                    nxt_frontier.append(way)
            frontier = nxt_frontier[:cap]
        return out

    def reaches(self, node: str, steps: list, goal: str) -> bool:
        """Whether a way of these steps leads from a node to a goal."""
        here = [node]
        for label, direction in steps:
            here = [n for h in here for lab, d, n in self.around(h)
                    if lab == label and d == direction][:200]
        return any(self.individuals.is_(n, goal) for n in here)
