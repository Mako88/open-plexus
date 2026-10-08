"""Storage: the graph's tables, and what is kept in memory beside them.

SQLite is the store of record. This class owns the connection, the commit batching, and the
memory a walk reads in its place: each node's latest edges and each event's arguments, added
to as an edge is written, so a walk makes no query for them. The graph above it knows events,
edges and nodes by name and never opens the tables to read an edge or an event.
"""

from __future__ import annotations

import sqlite3
from pathlib import Path

SCHEMA = """
CREATE TABLE IF NOT EXISTS events (id INTEGER PRIMARY KEY, turn INTEGER NOT NULL,
    lemma TEXT NOT NULL, heard TEXT NOT NULL, mood TEXT NOT NULL DEFAULT '');
CREATE TABLE IF NOT EXISTS edges (event INTEGER NOT NULL, label TEXT NOT NULL,
    node TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS edges_event ON edges (event);
CREATE INDEX IF NOT EXISTS edges_node ON edges (node);
CREATE INDEX IF NOT EXISTS events_turn ON events (turn);
CREATE INDEX IF NOT EXISTS events_lemma ON events (lemma, mood);
CREATE TABLE IF NOT EXISTS learnt (shape TEXT NOT NULL, plan TEXT NOT NULL,
    hits INTEGER NOT NULL, misses INTEGER NOT NULL, PRIMARY KEY (shape, plan));
CREATE TABLE IF NOT EXISTS positions (template TEXT NOT NULL, pos INTEGER NOT NULL,
    filler TEXT NOT NULL, n INTEGER NOT NULL, PRIMARY KEY (template, pos, filler));
CREATE TABLE IF NOT EXISTS aliases (word TEXT NOT NULL, name TEXT NOT NULL,
    hits REAL NOT NULL, found INTEGER NOT NULL, PRIMARY KEY (word, name));
CREATE TABLE IF NOT EXISTS agreement (name TEXT NOT NULL, pronoun TEXT NOT NULL,
    n INTEGER NOT NULL, PRIMARY KEY (name, pronoun));
CREATE TABLE IF NOT EXISTS boundaries (turn INTEGER PRIMARY KEY);
CREATE TABLE IF NOT EXISTS episode_words (word TEXT NOT NULL, episode INTEGER NOT NULL,
    PRIMARY KEY (word, episode));
CREATE INDEX IF NOT EXISTS episode_words_by ON episode_words (episode);
CREATE TABLE IF NOT EXISTS asks (wh TEXT NOT NULL, mark TEXT NOT NULL, n INTEGER NOT NULL,
    PRIMARY KEY (wh, mark));
CREATE TABLE IF NOT EXISTS contexts (word TEXT PRIMARY KEY, names TEXT NOT NULL,
    heard INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS called (node TEXT NOT NULL, name TEXT NOT NULL,
    turn INTEGER NOT NULL, PRIMARY KEY (node, name));
CREATE INDEX IF NOT EXISTS called_name ON called (name);
CREATE TABLE IF NOT EXISTS heard_as (name TEXT NOT NULL, pos TEXT NOT NULL,
    n INTEGER NOT NULL, PRIMARY KEY (name, pos));
CREATE TABLE IF NOT EXISTS circumstances (wh TEXT NOT NULL, link TEXT NOT NULL,
    n INTEGER NOT NULL, PRIMARY KEY (wh, link));
CREATE TABLE IF NOT EXISTS named (word TEXT NOT NULL, name TEXT NOT NULL,
    PRIMARY KEY (word, name));
CREATE TABLE IF NOT EXISTS filled (name TEXT NOT NULL, lemma TEXT NOT NULL,
    link TEXT NOT NULL, n INTEGER NOT NULL, PRIMARY KEY (name, lemma, link));
CREATE TABLE IF NOT EXISTS counts (shape TEXT NOT NULL, walk TEXT NOT NULL,
    word TEXT NOT NULL, size INTEGER NOT NULL);
"""
# what a taught arm carries to the next conversation: no facts, only how to read and ask
CARRIED = ("learnt", "positions")
# how many writes a commit waits for, a story's end aside (not the parser's `BATCH`)
WRITES = 64
# how many of a name's steps are returned, the latest first, as recall by a cue returns a
# few and never everything: adjectives as nodes of their own ('little', 'big') are hubs
# every story's things hang off, and a path search fanned out across all of them
HUB = 300


class Store:
    def __init__(self, directory: Path, known: dict | None = None) -> None:
        self.db = sqlite3.connect(str(directory / "graph.db"))
        # what is written is kept in batches (`written`), and written ahead without ever
        # waiting on the disk: a run is not resumed, so a commit only has to survive the
        # process ending, not the machine losing power, and the waits at each checkpoint
        # were half of what committing cost
        self.db.execute("PRAGMA journal_mode=WAL")
        self.db.execute("PRAGMA synchronous=OFF")
        self.db.executescript(SCHEMA)
        for table, rows in (known or {}).items():
            self.db.executemany(f"INSERT OR IGNORE INTO {table} VALUES (?, ?, ?, ?)",
                                [tuple(x) for x in rows])
        self.db.commit()
        self._unwritten = 0
        # the RAM dial: each node's latest edges and each event's arguments, kept in memory
        # and added to as a sentence is heard, so a walk reads them without a query. The
        # table stays the store of record, and a node never read since the arm was opened
        # is loaded from it once
        self._into: dict = {}
        self._outs: dict = {}
        # every event's lemma, turn and mood, kept for good: what was heard never changes
        self._events: dict = {}
        # each event's arguments, likewise
        self._args: dict = {}

    def close(self) -> None:
        self.db.commit()
        self.db.close()

    def opened(self, node: str) -> None:
        """A node just made, which nothing has an edge to yet."""
        self._into[node] = []

    def new_event(self, turn: int, lemma: str, text: str, mood: str) -> int:
        cur = self.db.execute("INSERT INTO events (turn, lemma, heard, mood) VALUES "
                              "(?, ?, ?, ?)", (turn, lemma, text, mood))
        self._outs[cur.lastrowid] = []
        self._into[f"e:{cur.lastrowid}"] = []
        return cur.lastrowid

    def add_edge(self, event: int, label: str, node: str) -> None:
        self.db.execute("INSERT INTO edges VALUES (?, ?, ?)", (event, label, node))
        self.keep_edge(event, label, node)

    def outs(self, event: int) -> list[tuple[str, str]]:
        """An event's arguments, (link, node), in the order they were written."""
        got = self._outs.get(event)
        if got is None:
            got = self._outs[event] = self.db.execute(
                "SELECT label, node FROM edges WHERE event = ? AND node NOT LIKE 'f:%'",
                (event,)).fetchall()
        return got

    def written(self, now: bool = False) -> None:
        """Make what was written durable: at each break between stories, and otherwise
        once in `WRITES` calls, not after every sentence, where the commits were the largest
        single cost of a run. The connection reads its own writes, so no answer waits on
        a commit; `close` makes the rest durable."""
        self._unwritten += 1
        if now or self._unwritten >= WRITES:
            self.db.commit()
            self._unwritten = 0

    def keep_edge(self, event: int, label: str, node: str) -> None:
        """An edge just written, added to what is kept in memory. An event is only ever
        given edges as it is heard, so nothing kept for it is stale."""
        if not node.startswith("f:"):
            self._outs[event].append((label, node))
        kept = self._into.get(node)
        if kept is not None:
            kept.append((event, label))
            # only the latest `HUB` can be read, with every edge of the event they begin in
            if len(kept) > 2 * HUB:
                self._into[node] = kept[trimmed(kept):]

    def edges_into(self, node: str) -> list[tuple[int, str]]:
        """The (event, label) of a node's latest edges, oldest first, as they were written."""
        kept = self._into.get(node)
        if kept is None:
            kept = self._into[node] = self.db.execute(
                "SELECT event, label FROM edges WHERE node = ? AND event >= COALESCE((SELECT "
                "event FROM edges WHERE node = ? ORDER BY rowid DESC LIMIT 1 OFFSET ?), 0) "
                "ORDER BY rowid", (node, node, HUB - 1)).fetchall()
        return kept

    def latest_into(self, held: list[str]) -> list[tuple[int, str]]:
        """The latest `HUB` edges into any of these nodes, newest event first; among one
        event's, the nodes' in order and each node's as written, as the table gives them."""
        every = [e for n in sorted(set(held)) for e in self.edges_into(n)]
        every.sort(key=lambda e: e[0], reverse=True)
        return every[:HUB]

    def event_row(self, node: str) -> tuple[str, int, str]:
        got = self._events.get(node)
        if got is None:
            got = self._events[node] = self.db.execute(
                "SELECT lemma, turn, mood FROM events WHERE id = ?", (int(node[2:]),)).fetchone()
        return got

    def args(self, event: int) -> frozenset:
        """An event's arguments, the prepositions aside, as (link, node); what an event
        holds never changes once heard."""
        got = self._args.get(event)
        if got is None:
            got = self._args[event] = frozenset(self.db.execute(
                "SELECT label, node FROM edges WHERE event = ? AND label NOT LIKE "
                "'prep:%' AND node NOT LIKE 'f:%'", (event,)).fetchall())
        return got



def trimmed(kept: list[tuple[int, str]]) -> int:
    """Where the edges worth keeping begin: the latest `HUB`, and the rest of the event the
    earliest of them is in, so which of its edges a read takes never depends on the cut."""
    at = len(kept) - HUB
    while at > 0 and kept[at - 1][0] == kept[at][0]:
        at -= 1
    return at
