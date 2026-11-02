"""A lexical sense for the full-form store: BM25 over the abstracts' titles and text (tantivy, an
index on disk, nothing on the card), beside the dense MiniLM keys of `openplexus.wiki`. The first
hop of a HotpotQA question is a name that is a title among 5.2 million; a sentence key is weak at
that and word matching is strong, so the two are read against each other on title recall before
either is the asker's tool. Rows are the store's row numbers, so both senses name the same text.
"""

from __future__ import annotations

import re
import time
from pathlib import Path

WORD = re.compile(r"[A-Za-z0-9]+")


def words(text: str) -> str:
    """The query as BM25 sees it: words only, so no character of a question is query syntax."""
    return " ".join(WORD.findall(text))


class LexicalIndex:
    """BM25 over `title` and `body`, the title weighted up; `nearest` returns store rows."""

    def __init__(self, path: Path, title_boost: float = 2.0, title_of=None, deep: int = 20):
        self.title_of = title_of  # row -> title; given, a title named in the query is ranked first
        self.deep = deep
        import tantivy

        self.path = Path(path)
        self.index = tantivy.Index.open(str(self.path))
        self.index.reload()
        self.searcher = self.index.searcher()
        self.title_boost = title_boost

    @staticmethod
    def schema():
        import tantivy

        b = tantivy.SchemaBuilder()
        b.add_integer_field("row", stored=True, indexed=True, fast=True)
        b.add_text_field("title", stored=False, tokenizer_name="en_stem")
        b.add_text_field("body", stored=False, tokenizer_name="en_stem")
        return b.build()

    @classmethod
    def build(cls, path: Path, titles: Path, texts: Path, log=print, heap: int = 1_000_000_000) -> "LexicalIndex":
        """Index every line of `texts` (row i) with its title from `titles`. Built whole in one
        commit; about ten minutes for 5.2 million abstracts, with no card."""
        import tantivy

        path = Path(path)
        path.mkdir(parents=True, exist_ok=True)
        index = tantivy.Index(cls.schema(), path=str(path))
        writer = index.writer(heap_size=heap, num_threads=4)
        started = time.perf_counter()
        with open(titles, encoding="utf-8") as tf, open(texts, encoding="utf-8") as xf:
            for i, (title, line) in enumerate(zip(tf, xf)):
                title = title.rstrip("\n")
                body = line.rstrip("\n").split(": ", 1)[-1]
                writer.add_document(tantivy.Document(row=i, title=title, body=body))
                if (i + 1) % 500_000 == 0:
                    log(f"{i + 1} abstracts added, {time.perf_counter() - started:.0f}s so far")
        writer.commit()
        writer.wait_merging_threads()
        log(f"committed, {time.perf_counter() - started:.0f}s")
        return cls(path)

    def nearest(self, queries: list[str], k: int, exclude: set[int] | None = None) -> list[list[int]]:
        out = []
        for text in queries:
            q = words(text)
            if not q:
                out.append([])
                continue
            query = self.index.parse_query(q, ["title", "body"], field_boosts={"title": self.title_boost})
            want = k + len(exclude or ())
            picks = []
            for _score, addr in self.searcher.search(query, max(want, self.deep if self.title_of else 0)).hits:
                row = int(self.searcher.doc(addr)["row"][0])
                if row not in (exclude or ()):
                    picks.append(row)
            if self.title_of:
                picks = named_first(text, picks, self.title_of)
            out.append(picks[:k])
        return out


def named_first(query: str, rows: list[int], title_of) -> list[int]:
    """The rows whose title is named in the query, whole words, longest title first, ahead of
    the rest in their given order: an asker's query "Ed Wood nationality" names Ed Wood, and the
    index alone ranked "Ed Wood filmography" first (2026-11-02). No tuning: a title is named or not."""
    q = " " + " ".join(words(query).lower().split()) + " "
    named = []
    for r in rows:
        t = " " + " ".join(words(title_of(r)).lower().split()) + " "
        if len(t) > 2 and t in q:
            named.append((len(t), r))
    named.sort(reverse=True)
    first = [r for _, r in named]
    return first + [r for r in rows if r not in first]
