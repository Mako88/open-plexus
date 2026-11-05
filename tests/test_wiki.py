"""The full-form store builds shard by shard and resumes, finds the nearest abstract by cosine
over a chunked search, and the asker's tool over it never shows the same abstract twice."""

import json
import sys
from pathlib import Path

import numpy as np
import pyarrow as pa
import pyarrow.parquet as pq

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.wiki import DIM, WikiSearch, WikiStore, WikiTexts, abstract_text, build  # noqa: E402

TITLES = ["Anarchism", "Autism", "Albedo", "Tim Burton", "Ed Wood (film)", "Woodson, Arkansas", "Alabama"]
TEXTS = ["a political philosophy", "a neurodevelopmental disorder", "a measure of reflectance",
         "an American film director", "a 1994 film directed by Tim Burton", "a town in Arkansas", "a state"]


class Words:
    """A stand-in embedder: one dimension a word, so a query is nearest the text sharing its words."""

    name = "words"
    vocab = sorted({w for t in TEXTS + TITLES for w in (t + " film director town").lower().replace(",", "").split()})

    def __call__(self, texts):
        out = np.zeros((len(texts), DIM), dtype=np.float32)
        for r, t in enumerate(texts):
            for w in t.lower().replace(",", "").replace(":", "").replace("(", "").replace(")", "").split():
                if w in self.vocab:
                    out[r, self.vocab.index(w)] += 1.0
        return out / np.maximum(np.linalg.norm(out, axis=1, keepdims=True), 1e-12)


def corpus(tmp_path):
    p = tmp_path / "corpus.parquet"
    pq.write_table(pa.table({"_id": [str(i) for i in range(len(TITLES))], "title": TITLES, "text": TEXTS}), p)
    return p


def test_the_build_writes_texts_keys_and_progress_in_shards_and_resumes(tmp_path):
    out = tmp_path / "store"
    log = []
    progress = build(corpus(tmp_path), out, Words(), shard=3, log=log.append)
    assert progress == {"n": 7, "dim": DIM, "shard": 3, "done": 3, "embedder": "words"}
    assert len(log) == 3 and log[0].startswith("shard 1/3")
    keys = np.memmap(out / "keys.f16", dtype=np.float16, mode="r", shape=(7, DIM))
    assert np.allclose(np.linalg.norm(keys.astype(np.float32), axis=1), 1.0, atol=1e-2)
    # a build stopped after one shard carries on from the second
    (out / "progress.json").write_text(json.dumps({**progress, "done": 1}), encoding="utf-8")
    log.clear()
    build(corpus(tmp_path), out, Words(), shard=3, log=log.append)
    assert [line.split(" ")[1] for line in log] == ["2/3", "3/3"]
    assert abstract_text("Ed Wood (film)", "a 1994\nfilm") == "Ed Wood (film): a 1994 film"


def test_the_store_finds_the_nearest_abstract_and_the_tool_never_repeats_one(tmp_path):
    out = tmp_path / "store"
    build(corpus(tmp_path), out, Words(), shard=3)
    store = WikiStore(out, device="cpu", chunk=2)  # chunks smaller than the store, so the merge is exercised
    assert store.n == 7 and store.text(4) == "Ed Wood (film): a 1994 film directed by Tim Burton"
    assert store.title(5) == "Woodson, Arkansas"
    texts = WikiTexts(out)  # the texts alone, no keys and nothing on the card
    assert texts.text(4) == store.text(4) and texts.title(0) == "Anarchism" and not hasattr(texts, "keys")
    q = Words()(["who directed the film Ed Wood", "a town in Arkansas"])
    assert store.nearest(q, 2) == [[4, 3], [5, 0]] or store.nearest(q, 1) == [[4], [5]]
    assert store.nearest(q, 1, exclude={4, 5})[0][0] != 4 and store.nearest(q, 1, exclude={4, 5})[1][0] != 5
    tool = WikiSearch(store, Words())
    first = tool.search("the film Ed Wood")
    second = tool.search("the film Ed Wood")
    assert first.startswith("Ed Wood (film): ") and second != first
    assert tool.titles()[0] == "Ed Wood (film)" and tool.searches == ["the film Ed Wood"] * 2
    # another sense: a function of queries, k and rows to skip, here one that always says row 6 then 0
    other = WikiSearch(store, Words(), nearest=lambda queries, k, exclude: [[i for i in (6, 0) if i not in exclude][:k] for _ in queries])
    assert other.search("anything") == "Alabama: a state" and other.search("anything") == "Anarchism: a political philosophy"


def test_hyde_searches_by_the_query_with_the_passage_the_system_expects():
    from openplexus.wiki import hyde
    seen = []
    search = hyde(lambda queries, k, exclude: seen.append(queries) or [[1]], lambda q: "Animorphs is a series.")
    assert search(["the series with companion books"], 1, set()) == [[1]]
    assert seen == [["the series with companion books Animorphs is a series."]]


def test_fused_hyde_keeps_the_query_s_own_rows_and_lifts_the_shared_ones():
    from openplexus.wiki import hyde_fused
    lists = {"query": [5, 6, 7], "guess": [9, 6, 8]}
    search = hyde_fused(lambda qs, k, ex: [lists["query" if qs[0] == "q" else "guess"]], lambda q: "g", deep=3)
    assert search(["q"], 1, set()) == [[6]]  # ranked well by both
    assert search(["q"], 3, set())[0][:2] == [6, 5]  # the query's own top row kept ahead of the guess's alone


def test_an_unfinished_store_refuses_to_open(tmp_path):
    out = tmp_path / "store"
    progress = build(corpus(tmp_path), out, Words(), shard=3)
    (out / "progress.json").write_text(json.dumps({**progress, "done": 2}), encoding="utf-8")
    try:
        WikiStore(out, device="cpu")
    except RuntimeError as e:
        assert "2 of 3 shards" in str(e)
    else:
        raise AssertionError("opened an unfinished store")
