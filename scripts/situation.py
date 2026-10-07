"""Whether the situation predicts what comes next where a word alone could not: item 7f's
first read, offline.

    uv run python scripts/situation.py state/graphs/stream --passes 1,3,10 --note "..."

The saved stream graph's events, grouped into stories by its boundaries; the first stories
heard, the rest held out. Each held-out event's arguments are asked in turn: given the
event's other items (its head, the lemma, among them) and, for `story`, what the story told
before it, which word fills this role? The filler is ranked among every word the role was
heard with. Item 7e asked the same from one word of the event; nothing cleared the role's
commonest fillers.

role       the role's commonest fillers alone
counts     the exact store as fit asks it: the head's own fillers of the role, backed off
           to the role's commonest
joint      the exact store's best guess from the same evidence as the predictor: each item
           of the event votes by what filled the role beside it, smoothed to the role's
           commonest (naive Bayes over the counts)
event      a predictor learnt by prediction: each item a word vector gated by its role,
           summed into the situation, read by the asked role against the fillers' vectors,
           on top of the role's commonest; replayed over the heard stories `passes` times
story      the same, with what the story told before the event added to the situation,
           the latest the most
joint-recent  the control for `story`: `joint` mixed with the story's words so far, the
           latest the most, as focus's recency is

Every read is on all held-out arguments and on the unheard ones (head, role and filler
never together before), where 7e's measure lived.
"""

from __future__ import annotations

import argparse
import json
import sqlite3
from collections import Counter, defaultdict
from pathlib import Path

import numpy as np
import torch

from unfused.reading import utc_stamp

ROOT = Path(__file__).resolve().parents[1]
ITEMS = 12      # an event's items the predictor reads, at most
BEFORE = 24     # items of the story before the event it reads, at most, the latest first
DECAY = 0.85    # how much less each earlier item of the story counts


def stories(db: Path) -> list[list[list[tuple[str, str]]]]:
    """Each story's events in the order heard, each event its items: ('head', lemma) and
    (link, word) for every argument that names something."""
    c = sqlite3.connect(db)
    names = dict(c.execute("SELECT node, name FROM called"))
    breaks = [t for (t,) in c.execute("SELECT turn FROM boundaries ORDER BY turn")]
    items: dict[int, list] = {}
    turn_of: dict[int, int] = {}
    for e, turn, lemma in c.execute("SELECT id, turn, lemma FROM events ORDER BY id"):
        items[e] = [("head", lemma)]
        turn_of[e] = turn
    for e, link, node in c.execute("SELECT event, label, node FROM edges ORDER BY rowid"):
        word = node[2:] if node.startswith("n:") else names.get(node) \
            if node.startswith("i:") else None
        if word and e in items:
            items[e].append((link, word))
    out, at = [], -1
    for e in sorted(items):
        k = np.searchsorted(breaks, turn_of[e], side="right") - 1
        if k != at:
            out.append([])
            at = k
        out[-1].append(items[e])
    return [s for s in out if s]


def cases(story_list):
    """(context items, story items before, role, filler, head) for every argument."""
    for events in story_list:
        before: list[tuple[str, str]] = []
        for items in events:
            head = items[0][1]
            for j, (r, c) in enumerate(items):
                if j == 0 or c == head:
                    continue
                ctx = [x for k, x in enumerate(items) if k != j][:ITEMS]
                yield ctx, before[-BEFORE:][::-1], r, c, head
            before.extend(items)


class Predictor(torch.nn.Module):
    def __init__(self, words: int, roles: int, width: int, story: bool):
        super().__init__()
        self.E = torch.nn.Embedding(words + 1, width, padding_idx=words)
        self.G = torch.nn.Embedding(roles + 1, width, padding_idx=roles)
        self.Q = torch.nn.Embedding(roles, width)
        self.O = torch.nn.Embedding(words, width)
        self.S = torch.nn.Linear(width, width, bias=False) if story else None
        torch.nn.init.normal_(self.E.weight, 0, 0.1)
        torch.nn.init.normal_(self.G.weight, 1, 0.1)
        torch.nn.init.normal_(self.O.weight, 0, 0.1)
        torch.nn.init.normal_(self.Q.weight, 1, 0.1)

    def forward(self, cw, cr, bw, br, bweight, role, logprior):
        h = (self.E(cw) * self.G(cr)).sum(1)
        if self.S is not None:
            b = (self.E(bw) * self.G(br) * bweight.unsqueeze(-1)).sum(1)
            h = h + self.S(b)
        h = torch.tanh(h) * self.Q(role)
        return h @ self.O.weight.T + logprior


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("graph", type=Path)
    ap.add_argument("--split", type=float, default=0.8)
    ap.add_argument("--passes", default="1,3,10")
    ap.add_argument("--width", type=int, default=128)
    ap.add_argument("--cases", type=int, default=20000)
    ap.add_argument("--seed", type=int, default=0)
    ap.add_argument("--note", required=True)
    args = ap.parse_args()
    torch.manual_seed(args.seed)
    rng = np.random.default_rng(args.seed)
    dev = "cuda" if torch.cuda.is_available() else "cpu"
    stamp = utc_stamp()

    all_stories = stories(args.graph / "graph.db")
    cut = int(len(all_stories) * args.split)
    train = list(cases(all_stories[:cut]))
    test = list(cases(all_stories[cut:]))
    words = sorted({w for ctx, _, _, c, _ in train for _, w in ctx} | {c for *_, c, _ in train})
    roles = sorted({r for ctx, _, r, _, _ in train for r2, _ in ctx for r in (r, r2)})
    wi = {w: i for i, w in enumerate(words)}
    ri = {r: i for i, r in enumerate(roles)}
    V, R = len(words), len(roles)

    # the exact store's counts
    role_fill: dict[str, Counter] = defaultdict(Counter)
    beside: dict[tuple, Counter] = defaultdict(Counter)
    heard = set()
    for ctx, _, r, c, head in train:
        role_fill[r][c] += 1
        heard.add((head, r, c))
        for _, w in ctx:
            beside[(w, r)][c] += 1
    prior = np.full((R, V), 0.0)
    for r, cs in role_fill.items():
        for c, n in cs.items():
            prior[ri[r], wi[c]] = n
    can = prior > 0
    logprior = np.where(can, np.log(np.maximum(prior, 1e-12) / np.maximum(
        prior.sum(1, keepdims=True), 1)), -1e4).astype(np.float32)

    test = [x for x in test if x[2] in ri and x[3] in wi and can[ri[x[2]], wi[x[3]]]
            and can[ri[x[2]]].sum() > 1]
    if len(test) > args.cases:
        test = [test[i] for i in sorted(rng.choice(len(test), args.cases, replace=False))]
    unheard = np.array([(h, r, c) not in heard for _, _, r, c, h in test])
    print(f"{len(all_stories)} stories, {cut} heard; {len(train)} arguments heard, "
          f"{len(test)} asked ({unheard.sum()} unheard); {V} words, {R} roles")

    reading = {"kind": "situation", "taken_at": stamp, "note": args.note,
               "graph": str(args.graph), "stories": [cut, len(all_stories) - cut],
               "asked": len(test), "unheard": int(unheard.sum()), "width": args.width,
               "reads": {}}

    def tally(name, scores_of):
        ranks = []
        for k, (ctx, before, r, c, head) in enumerate(test):
            s = scores_of(k, ctx, before, r, c, head)
            s = np.where(can[ri[r]], s, -np.inf)
            ranks.append(int((s >= s[wi[c]]).sum()))
        ranks = np.array(ranks)
        got = {}
        for part, mask in (("all", np.ones(len(ranks), bool)), ("unheard", unheard)):
            rr = ranks[mask]
            got[part] = {"mrr": round(float(np.mean(1 / rr)), 4),
                         "hit1": round(float(np.mean(rr == 1)), 4),
                         "hit10": round(float(np.mean(rr <= 10)), 4)}
        reading["reads"][name] = got
        print(name, json.dumps(got))

    tally("role", lambda k, ctx, b, r, c, h: prior[ri[r]].copy())

    def own(w, r):
        got = np.zeros(V)
        for x, n in beside.get((w, r), {}).items():
            got[wi[x]] = n
        return got

    def counts(k, ctx, b, r, c, h):
        p = prior[ri[r]] / prior[ri[r]].sum()
        return own(h, r) + p
    tally("counts", counts)

    def joint(k, ctx, b, r, c, h, alpha=1.0):
        p = prior[ri[r]] / prior[ri[r]].sum()
        s = np.log(np.maximum(p, 1e-12))
        for _, w in ctx:
            n = own(w, r)
            s = s + np.log((n + alpha * p) / (n.sum() + alpha) + 1e-12) - np.log(
                np.maximum(p, 1e-12))
        return s
    tally("joint", joint)

    # the control for `story`: the joint guess mixed with what the story told before, the
    # latest the most (a cache, as focus's recency is). What `story` adds past this is
    # what the situation adds past recency
    for mix in (0.1, 0.3, 0.5):
        def recent(k, ctx, b, r, c, h, mix=mix):
            j = joint(k, ctx, b, r, c, h)
            pj = np.exp(j - j.max())
            pj = np.where(can[ri[r]], pj, 0)
            pj = pj / pj.sum()
            cache = np.zeros(V)
            for n, (_, w) in enumerate(b):
                if w in wi:
                    cache[wi[w]] += DECAY ** n
            cache = np.where(can[ri[r]], cache, 0)
            if cache.sum() == 0:
                return np.log(pj + 1e-12)
            return np.log((1 - mix) * pj + mix * cache / cache.sum() + 1e-12)
        tally(f"joint-recent{mix}", recent)

    def tensors(rows):
        cw = torch.full((len(rows), ITEMS), V, dtype=torch.long)
        cr = torch.full((len(rows), ITEMS), R, dtype=torch.long)
        bw = torch.full((len(rows), BEFORE), V, dtype=torch.long)
        br = torch.full((len(rows), BEFORE), R, dtype=torch.long)
        bweight = torch.zeros((len(rows), BEFORE))
        role = torch.zeros(len(rows), dtype=torch.long)
        target = torch.zeros(len(rows), dtype=torch.long)
        for i, (ctx, before, r, c, _) in enumerate(rows):
            for j, (rr, w) in enumerate(ctx):
                if w in wi and rr in ri:
                    cw[i, j], cr[i, j] = wi[w], ri[rr]
            for j, (rr, w) in enumerate(before):
                if w in wi and rr in ri:
                    bw[i, j], br[i, j], bweight[i, j] = wi[w], ri[rr], DECAY ** j
            role[i], target[i] = ri[r], wi[c]
        return [t.to(dev) for t in (cw, cr, bw, br, bweight, role, target)]

    LP = torch.tensor(logprior, device=dev)
    tr = tensors(train)
    te = tensors(test)
    passes = sorted(int(x) for x in args.passes.split(","))
    for story in (False, True):
        model = Predictor(V, R, args.width, story).to(dev)
        opt = torch.optim.Adam(model.parameters(), lr=3e-3)
        done = 0
        for want in passes:
            while done < want:
                # a pass of replay: every heard story again, in a fresh order
                order = torch.randperm(len(train), device=dev)
                for s in range(0, len(train), 1024):
                    idx = order[s:s + 1024]
                    cw, cr, bw, br, bweight, role, target = (t[idx] for t in tr)
                    logits = model(cw, cr, bw, br, bweight, role, LP[role])
                    loss = torch.nn.functional.cross_entropy(logits, target)
                    opt.zero_grad()
                    loss.backward()
                    opt.step()
                done += 1
            with torch.no_grad():
                cw, cr, bw, br, bweight, role, target = te
                out = []
                for s in range(0, len(test), 2048):
                    sl = slice(s, s + 2048)
                    out.append(model(cw[sl], cr[sl], bw[sl], br[sl], bweight[sl], role[sl],
                                     LP[role[sl]]).cpu().numpy())
                scores = np.concatenate(out)
            tally(f"{'story' if story else 'event'}-{want}", lambda k, *_: scores[k])

    path = ROOT / "readings" / f"situation-s{args.seed}-{stamp}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print(path.name)


if __name__ == "__main__":
    main()
