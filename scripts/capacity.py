"""How much a vector holds, and whether one predicts what was not heard: item 7e, offline.

    uv run python scripts/capacity.py state/graphs/stream --widths 64,256,1024 --note "..."

Each event of a saved stream graph is its head (the event's lemma) and its arguments, each
a (link, word). A fact is what one word was heard with: (word, role, filler) for every other
item of the same event, so 'Lily ate the cake' gives (eat, nsubj, lily), (cake, head, eat)
and (lily, obj, cake). Every word, role and filler is a random bipolar vector fixed by its
spelling. A word's experience is encoded three ways:

summed    the fillers' vectors added, roles ignored (the situations as they are)
bound     each filler bound to its role (elementwise product, its own inverse) and added;
          read back by unbinding the role and comparing with every filler: the graph's
          words are the clean-up memory
learnt    a word table and a (role, filler) table trained by prediction, skip-gram with
          negative sampling, one pass over the facts in the order heard

Two readings, each a fork:

held      the first part of the stream, every fact kept: for a word and a role, how many
          of the fillers it was heard with rank in the first as many places (R-precision),
          by how many distinct facts the word holds and by width. Says which encoding can
          store a word's experience at a width, and up to how much.
unheard   facts of the last part of the stream never heard before, of words and fillers
          that were: the filler ranked among those the role was heard with, by the counts
          (the word's own, backed off to the role's), by each encoding, and by the counts
          of the word's nearest neighbours under each encoding (the vectors choose whom to
          borrow from, the graph answers). Says whether any slow memory predicts what the
          exact store cannot: `counts` is the store as it answers (the word's own fillers
          first), `role` the role's commonest alone, the line any predictor must clear.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sqlite3
from collections import Counter, defaultdict
from pathlib import Path

import numpy as np

from unfused.reading import utc_stamp

ROOT = Path(__file__).resolve().parents[1]
# how much of a guess is borrowed from neighbours, beside the role's commonest
MIXES = (0.1, 0.3, 1.0)
BUCKETS = [(1, 3), (4, 10), (11, 30), (31, 100), (101, 300), (301, 1000), (1001, 10**9)]


def events(db: Path) -> list[list[tuple[str, str]]]:
    """Each event in the order heard, as its items: ('head', lemma) and (link, word) for
    each argument that names something; the individuals by what they are called."""
    c = sqlite3.connect(db)
    names = dict(c.execute("SELECT node, name FROM called"))
    out: dict[int, list] = {}
    for e, lemma in c.execute("SELECT id, lemma FROM events ORDER BY id"):
        out[e] = [("head", lemma)]
    for e, link, node in c.execute("SELECT event, label, node FROM edges ORDER BY rowid"):
        if node.startswith("n:"):
            word = node[2:]
        elif node.startswith("i:"):
            word = names.get(node)
        else:
            continue
        if word and e in out:
            out[e].append((link, word))
    return list(out.values())


def facts(items: list[tuple[str, str]]):
    """(word, role, filler) for every ordered pair of an event's items of two words."""
    for i, (_, w) in enumerate(items):
        for j, (r, c) in enumerate(items):
            if i != j and w != c:
                yield w, r, c


class Codes:
    """A random bipolar vector a symbol, fixed by its spelling."""

    def __init__(self, width: int):
        self.width, self.got = width, {}

    def __call__(self, symbol: str) -> np.ndarray:
        v = self.got.get(symbol)
        if v is None:
            seed = int.from_bytes(hashlib.blake2b(symbol.encode(), digest_size=8).digest())
            v = np.random.default_rng(seed).choice([-1.0, 1.0], self.width).astype(np.float32)
            self.got[symbol] = v
        return v

    def matrix(self, symbols: list[str]) -> np.ndarray:
        return np.stack([self(s) for s in symbols])


def encode(train: list, width: int):
    """Each word's summed and bound memory over the training facts."""
    code = Codes(width)
    summed: dict[str, np.ndarray] = defaultdict(lambda: np.zeros(width, np.float32))
    bound: dict[str, np.ndarray] = defaultdict(lambda: np.zeros(width, np.float32))
    for w, r, c in train:
        v = code("w:" + c)
        summed[w] += v
        bound[w] += code("r:" + r) * v
    return code, summed, bound


def learn(train: list, width: int, negatives: int = 5, rate: float = 0.01, batch: int = 512,
          seed: int = 0):
    """Skip-gram with negative sampling over (word, (role, filler)), one pass in order."""
    import torch

    torch.manual_seed(seed)
    words = sorted({w for w, _, _ in train})
    ctx = sorted({(r, c) for _, r, c in train})
    wi = {w: i for i, w in enumerate(words)}
    ci = {x: i for i, x in enumerate(ctx)}
    freq = Counter(ci[(r, c)] for _, r, c in train)
    noise = torch.tensor([freq[i] for i in range(len(ctx))], dtype=torch.float) ** 0.75
    W = torch.nn.Embedding(len(words), width)
    C = torch.nn.Embedding(len(ctx), width)
    torch.nn.init.uniform_(W.weight, -0.5 / width, 0.5 / width)
    torch.nn.init.zeros_(C.weight)
    opt = torch.optim.Adam(list(W.parameters()) + list(C.parameters()), lr=rate)
    pw = torch.tensor([wi[w] for w, _, _ in train])
    pc = torch.tensor([ci[(r, c)] for _, r, c in train])
    for s in range(0, len(train), batch):
        a, b = pw[s:s + batch], pc[s:s + batch]
        neg = torch.multinomial(noise, len(a) * negatives, replacement=True).view(len(a), -1)
        va = W(a)
        pos = torch.nn.functional.logsigmoid((va * C(b)).sum(-1))
        ng = torch.nn.functional.logsigmoid(-(C(neg) @ va.unsqueeze(-1)).squeeze(-1)).sum(-1)
        loss = -(pos + ng).mean()
        opt.zero_grad()
        loss.backward()
        opt.step()
    return wi, ci, W.weight.detach().numpy(), C.weight.detach().numpy()


def bucket(n: int) -> str:
    for lo, hi in BUCKETS:
        if lo <= n <= hi:
            return f"{lo}-{hi}" if hi < 10**9 else f"{lo}+"
    return "0"


def held(train, table, widths, learnt, per_bucket: int, rng) -> dict:
    """R-precision of a word and a role giving back its fillers, by facts held and width."""
    distinct = {w: sum(len(cs) for cs in roles.values()) for w, roles in table.items()}
    by: dict[str, list[str]] = defaultdict(list)
    for w, n in distinct.items():
        by[bucket(n)].append(w)
    chosen = {b: sorted(rng.choice(ws, min(per_bucket, len(ws)), replace=False))
              for b, ws in by.items()}
    fillers = sorted({c for roles in table.values() for cs in roles.values() for c in cs})
    out: dict = {}
    rows_of: dict = {}

    def rprec(scores: np.ndarray, truth: set[str]) -> float:
        k = len(truth)
        top = np.argpartition(-scores, k - 1)[:k] if k < len(scores) else np.arange(len(scores))
        return len({fillers[i] for i in top} & truth) / k

    for width in widths:
        code, summed, bound = encode(train, width)
        F = code.matrix(["w:" + c for c in fillers])
        row = {}
        for b, ws in chosen.items():
            got = defaultdict(list)
            for w in ws:
                for r, cs in table[w].items():
                    truth = set(cs)
                    got["summed"].append(rprec(F @ summed[w], truth))
                    got["bound"].append(rprec(F @ (code("r:" + r) * bound[w]), truth))
                    if width in learnt:
                        wi, ci, Wm, Cm = learnt[width]
                        rows = rows_of.get((width, r))
                        if rows is None:
                            rows = rows_of[(width, r)] = np.array(
                                [ci.get((r, c), -1) for c in fillers])
                        s = np.where(rows >= 0, Cm[np.maximum(rows, 0)] @ Wm[wi[w]], -np.inf)
                        got["learnt"].append(rprec(s, truth))
            row[b] = {k: [round(float(np.mean(v)), 3), len(v)] for k, v in got.items()}
        out[width] = row
        print("held", width, json.dumps(row))
    return out


def unheard(train, test, table, widths, learnt, neighbours: int, limit: int, rng) -> dict:
    """Mean reciprocal rank and hits in ten of unheard facts' fillers among those the role
    was heard with."""
    role_fill: dict[str, Counter] = defaultdict(Counter)
    for w, roles in table.items():
        for r, cs in roles.items():
            role_fill[r].update(cs)
    seen = {(w, r, c) for w, r, c in train}
    cases = list({(w, r, c) for w, r, c in test if (w, r, c) not in seen and w in table
                  and c in role_fill[r] and len(role_fill[r]) > 1})
    cases.sort()
    if len(cases) > limit:
        cases = [cases[i] for i in sorted(rng.choice(len(cases), limit, replace=False))]
    words = sorted(table)
    out: dict = {"cases": len(cases)}

    def rank(scores: np.ndarray, at: int) -> int:
        # ties count against: the filler is placed after every candidate scoring as high
        return int((scores >= scores[at]).sum())

    def tally(name, ranks):
        out[name] = {"mrr": round(float(np.mean([1 / k for k in ranks])), 4),
                     "hit10": round(float(np.mean([k <= 10 for k in ranks])), 4)}
        print("unheard", name, out[name])

    cands = {r: sorted(f) for r, f in role_fill.items() if f}
    at = {r: {x: i for i, x in enumerate(cs)} for r, cs in cands.items()}
    prior = {r: np.array([role_fill[r][x] for x in cs], np.float64) for r, cs in cands.items()}

    def own(w, r):
        got = np.zeros(len(cands[r]))
        for x, n in table[w].get(r, {}).items():
            got[at[r][x]] = n
        return got

    # the exact store: the word's own fillers first, then the role's commonest
    tally("counts", [rank(own(w, r) + prior[r] / prior[r].sum(), at[r][c])
                     for w, r, c in cases])
    # the role's commonest fillers alone
    tally("role", [rank(prior[r], at[r][c]) for w, r, c in cases])

    asked = sorted({w for w, _, _ in cases})
    index = {w: i for i, w in enumerate(words)}

    def borrow(near_of, name):
        # what the word's neighbours were heard with, mixed into the role's commonest at
        # several weights (similarity-based smoothing, Dagan, Lee and Pereira): read
        # against `role`, which is the mix at nothing
        ranks = defaultdict(list)
        for w, r, c in cases:
            borrowed = np.zeros(len(cands[r]))
            for v, sim in near_of[w]:
                borrowed += max(sim, 0.0) * own(v, r)
            base = prior[r] / prior[r].sum()
            share = borrowed / borrowed.sum() if borrowed.sum() > 0 else borrowed
            for mix in MIXES:
                ranks[mix].append(rank((1 - mix) * base + mix * share, at[r][c]))
        for mix in MIXES:
            tally(f"{name}-mix{mix}", ranks[mix])

    def nearest(norm, w):
        if hasattr(norm, "tocsr"):
            sims = (norm @ norm[index[w]].T).toarray().ravel()
        else:
            sims = norm @ norm[index[w]]
        sims[index[w]] = -2
        top = np.argpartition(-sims, neighbours)[:neighbours]
        return [(words[i], float(sims[i])) for i in top]

    # the control: neighbours by the exact counts' profiles, the best likeness the graph
    # itself holds. If these do not clear `role`, unheard facts are not predictable from
    # what words were heard with at this size, whatever encodes it
    from scipy import sparse

    ctx = {}
    rows, cols, vals = [], [], []
    for w in words:
        for r, cs in table[w].items():
            for x, n in cs.items():
                rows.append(index[w])
                cols.append(ctx.setdefault((r, x), len(ctx)))
                vals.append(float(n))
    P = sparse.csr_matrix((vals, (rows, cols)), shape=(len(words), len(ctx)))
    P = sparse.diags(1 / np.sqrt(P.multiply(P).sum(1).A.ravel() + 1e-9)) @ P
    borrow({w: nearest(P, w) for w in asked}, "profile-neighbours")
    for width in widths:
        code, summed, bound = encode(train, width)
        encs = {"summed": summed, "bound": bound}
        if width in learnt:
            wi, ci, Wm, Cm = learnt[width]
            encs["learnt"] = {w: Wm[wi[w]] for w in words}
        F = {r: code.matrix(["w:" + x for x in cs]) for r, cs in cands.items()}
        for name, mem in encs.items():
            M = np.stack([mem[w] for w in words])
            norm = M / (np.linalg.norm(M, axis=1, keepdims=True) + 1e-9)
            near_of = {w: nearest(norm, w) for w in asked}
            direct = []
            for w, r, c in cases:
                if name == "learnt":
                    rows = np.array([ci.get((r, x), -1) for x in cands[r]])
                    s = np.where(rows >= 0, Cm[np.maximum(rows, 0)] @ mem[w], -1e9)
                elif name == "bound":
                    s = F[r] @ (code("r:" + r) * mem[w])
                else:
                    s = F[r] @ mem[w]
                direct.append(rank(s, at[r][c]))
            tally(f"{name}-{width}", direct)
            borrow(near_of, f"{name}-{width}-neighbours")
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("graph", type=Path)
    ap.add_argument("--widths", default="64,256,1024")
    ap.add_argument("--learnt", default="64,256", help="widths the skip-gram is trained at")
    ap.add_argument("--split", type=float, default=0.8)
    ap.add_argument("--per-bucket", type=int, default=60)
    ap.add_argument("--cases", type=int, default=2000)
    ap.add_argument("--neighbours", type=int, default=20)
    ap.add_argument("--seed", type=int, default=0)
    ap.add_argument("--note", required=True)
    args = ap.parse_args()
    rng = np.random.default_rng(args.seed)
    stamp = utc_stamp()

    evs = events(args.graph / "graph.db")
    cut = int(len(evs) * args.split)
    train = [f for items in evs[:cut] for f in facts(items)]
    test = [f for items in evs[cut:] for f in facts(items)]
    table: dict[str, dict[str, Counter]] = defaultdict(lambda: defaultdict(Counter))
    for w, r, c in train:
        table[w][r][c] += 1
    print(f"{len(evs)} events, {len(train)} facts heard, {len(test)} after, {len(table)} words")

    widths = [int(x) for x in args.widths.split(",")]
    learnt = {}
    for width in [int(x) for x in args.learnt.split(",") if x]:
        learnt[width] = learn(train, width, seed=args.seed)
        print("learnt", width)

    reading = {
        "kind": "capacity", "taken_at": stamp, "note": args.note,
        "graph": str(args.graph), "events": len(evs), "split": args.split,
        "facts": [len(train), len(test)], "words": len(table),
        "held": held(train, table, widths, learnt, args.per_bucket, rng),
        "unheard": unheard(train, test, table, widths, learnt, args.neighbours, args.cases,
                           rng),
    }
    path = ROOT / "readings" / f"capacity-s{args.seed}-{stamp}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print(path.name)


if __name__ == "__main__":
    main()
