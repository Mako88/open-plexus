"""The store at its full size, step 6's scale reading: every Wikipedia abstract HotpotQA was built
over (5.23 million, the 2017 dump as BeIR ships it, `data/wiki-abstracts/corpus.parquet`), keyed
by MiniLM exactly as the distractor form keys a question's ten paragraphs, so the one change
between the two forms is the size of the store the asker searches.

`build` writes the store once, shard by shard with a progress file, so a build that is paused for
a game resumes where it stopped. `WikiStore` opens it: the keys as a float16 memmap, lifted to the
card in chunks and searched by a chunked matmul (the card computes in float32, since a GTX 1080 Ti
does float16 arithmetic slowly, and the whole key matrix in float32 would not fit beside the
server). `WikiSearch` is the asker's tool over it, the same `search(query)` the rollout store
offers, returning the nearest abstract not shown before.
"""

from __future__ import annotations

import json
import time
from pathlib import Path

import numpy as np

DIM = 384


def abstract_text(title: str, text: str) -> str:
    """One abstract as the asker sees it: the title first, as the distractor form's paragraphs."""
    return f"{title}: " + " ".join(str(text).split())


def build(parquet: Path, out: Path, embed, shard: int = 50_000, limit: int | None = None, log=print) -> dict:
    """Write `out/texts.txt` (one abstract a line), `out/titles.txt`, `out/offsets.npy` (byte offsets,
    n + 1), `out/keys.f16` (n x DIM float16) and `out/progress.json`. Keys are written a shard at a
    time and the progress file names the shards done, so a second call carries on."""
    import pyarrow.parquet as pq

    out.mkdir(parents=True, exist_ok=True)
    progress_path = out / "progress.json"
    table = pq.read_table(parquet, columns=["title", "text"])
    if limit is not None:
        table = table.slice(0, limit)
    titles = table.column("title").to_pylist()
    texts = [abstract_text(t, x) for t, x in zip(titles, table.column("text").to_pylist())]
    del table
    n = len(texts)
    shards = (n + shard - 1) // shard
    progress = json.loads(progress_path.read_text(encoding="utf-8")) if progress_path.exists() else {}
    if progress.get("n") != n or progress.get("shard") != shard:
        # a new store, or one built with other sizes: the texts are rewritten and the keys start over
        with open(out / "texts.txt", "wb") as f, open(out / "titles.txt", "w", encoding="utf-8") as g:
            offsets = np.zeros(n + 1, dtype=np.int64)
            for i, (t, x) in enumerate(zip(titles, texts)):
                b = x.encode("utf-8") + b"\n"
                f.write(b)
                offsets[i + 1] = offsets[i] + len(b)
                g.write(" ".join(t.split()) + "\n")
        np.save(out / "offsets.npy", offsets)
        progress = {"n": n, "dim": DIM, "shard": shard, "done": 0, "embedder": getattr(embed, "name", "?")}
        progress_path.write_text(json.dumps(progress), encoding="utf-8")
    keys = np.memmap(out / "keys.f16", dtype=np.float16, mode="r+" if (out / "keys.f16").exists() else "w+", shape=(n, DIM))
    started = time.perf_counter()
    for s in range(progress["done"], shards):
        lo, hi = s * shard, min(n, (s + 1) * shard)
        keys[lo:hi] = np.asarray(embed(texts[lo:hi]), dtype=np.float16)
        keys.flush()
        progress["done"] = s + 1
        progress_path.write_text(json.dumps(progress), encoding="utf-8")
        log(f"shard {s + 1}/{shards} written, rows {hi}/{n}, {time.perf_counter() - started:.0f}s so far")
    return progress


class WikiTexts:
    """The store's rows as text, by row number, from the memmapped texts file: what a lexical
    search needs of the store, with nothing on the card."""

    def __init__(self, path: Path):
        self.path = Path(path)
        progress = json.loads((self.path / "progress.json").read_text(encoding="utf-8"))
        self.n, self.dim = progress["n"], progress["dim"]
        if progress["done"] * progress["shard"] < self.n:
            raise RuntimeError(f"the store at {self.path} is {progress['done']} of "
                               f"{(self.n + progress['shard'] - 1) // progress['shard']} shards built")
        self.offsets = np.load(self.path / "offsets.npy")
        self.bytes = np.memmap(self.path / "texts.txt", dtype=np.uint8, mode="r")

    def text(self, i: int) -> str:
        return bytes(self.bytes[self.offsets[i]:self.offsets[i + 1] - 1]).decode("utf-8")

    def title(self, i: int) -> str:
        return self.text(i).split(": ", 1)[0]


class WikiStore(WikiTexts):
    """The built store, searched by cosine over MiniLM keys. `nearest` takes normalised query
    vectors and returns the top indices per query, skipping an excluded set."""

    def __init__(self, path: Path, device: str | None = None, chunk: int = 524_288):
        import torch

        super().__init__(path)
        self.torch = torch
        self.device = device or ("cuda" if torch.cuda.is_available() else "cpu")
        self.chunk = chunk
        keys = np.memmap(self.path / "keys.f16", dtype=np.float16, mode="r", shape=(self.n, self.dim))
        # lifted in chunks so no float32 copy of the whole matrix is ever made in RAM
        self.keys = torch.empty((self.n, self.dim), dtype=torch.float16, device=self.device)
        for lo in range(0, self.n, chunk):
            hi = min(self.n, lo + chunk)
            self.keys[lo:hi] = torch.from_numpy(np.array(keys[lo:hi])).to(self.device)  # a copy: the memmap is read-only
        del keys

    def nearest(self, queries: np.ndarray, k: int, exclude: set[int] | None = None) -> list[list[int]]:
        torch = self.torch
        q = torch.from_numpy(np.asarray(queries, dtype=np.float32)).to(self.device).T  # (dim, m)
        m = q.shape[1]
        want = k + len(exclude or ())
        best_scores = torch.full((want, m), -2.0, device=self.device)
        best_index = torch.zeros((want, m), dtype=torch.int64, device=self.device)
        for lo in range(0, self.n, self.chunk):
            hi = min(self.n, lo + self.chunk)
            scores = self.keys[lo:hi].float() @ q  # (rows, m) in float32
            top = min(want, hi - lo)
            s, i = scores.topk(top, dim=0)
            cand_s = torch.cat([best_scores, s])
            cand_i = torch.cat([best_index, i + lo])
            best_scores, order = cand_s.topk(want, dim=0)
            best_index = cand_i.gather(0, order)
        out = []
        for col in range(m):
            picks = [int(j) for j in best_index[:, col].tolist() if j not in (exclude or ())]
            out.append(picks[:k])
        return out

    def close(self) -> None:
        del self.keys


HYDE_SYSTEM = (
    "Write the opening two sentences of the encyclopedia article that would answer the search below, "
    "naming its subject in the first words as an encyclopedia does. If you are not sure of a fact, write "
    "your best guess. Reply with the two sentences only."
)


def hyde(nearest, write):
    """The search by the passage the system expects to find (HyDE, Gao et al., 2022): `write(query)`
    is the trunk writing that passage, and the store is searched with the query and the passage
    together, so a thing described rather than named is looked for by the words its own article
    would use. Nothing is handed to the system: the guess is its own."""
    def search(queries, k, exclude):
        return nearest([q + " " + write(q) for q in queries], k, exclude)
    return search


def hyde_fused(nearest, write, deep: int = 20, c: int = 60):
    """HyDE's second form (2026-11-05): searched together with the query, the 2B's guess carried
    wrong names that outweighed the query's right ones (0.265 against 0.39). Here the query and
    the guess are searched apart and their rankings fused by reciprocal rank, so a row both rank
    well rises, and a bad guess can add candidates but never removes the query's own."""
    def search(queries, k, exclude):
        out = []
        for q in queries:
            own = nearest([q], deep, exclude)[0]
            guessed = nearest([write(q)], deep, exclude)[0]
            score: dict[int, float] = {}
            for ranked in (own, guessed):
                for rank, row in enumerate(ranked):
                    score[row] = score.get(row, 0.0) + 1.0 / (c + rank + 1)
            # ties broken by the query's own order
            order = {row: i for i, row in enumerate(own)}
            out.append(sorted(score, key=lambda r: (-score[r], order.get(r, deep)))[:k])
        return out
    return search


class WikiSearch:
    """The asker's tool over the whole store: the nearest abstract to the query not shown before.
    Built per question, so `shown` and `searches` are that question's. `nearest(queries, k,
    exclude)` is the sense, the store's own dense keys unless another is given (the lexical index,
    or a fusion of the two, `openplexus.lexical`)."""

    def __init__(self, store: WikiTexts, embed=None, nearest=None):
        self.store = store
        self.embed = embed
        self.nearest = nearest or (lambda queries, k, exclude: store.nearest(embed(queries), k, exclude))
        self.searches: list[str] = []
        self.shown: list[int] = []

    def search(self, query: str) -> str:
        """Search the collection for a passage.

        Args:
            query: what to look for, a name or a short description of the fact needed.

        Returns:
            The most relevant passage, with its title first.
        """
        self.searches.append(query)
        pick = self.nearest([query], 1, set(self.shown))[0]
        i = pick[0] if pick else 0
        self.shown.append(i)
        return self.store.text(i)

    def titles(self) -> list[str]:
        return [self.store.title(i) for i in self.shown]
