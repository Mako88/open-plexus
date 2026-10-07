"""The slow memory: a predictor from the situation to what fills a role (THE ORDER, item
7f; DECIDED, vectors hold a moment).

Each event is its head (the verb's lemma) and its arguments, each a word in a role. The
situation is the event's other items, each a word's vector gated by its role, beside what
the story told before it, the latest the most; read by the asked role, it is compared with
every word's output vector, on top of how often the role was filled by each word (the
exact counts, kept beside it). Words and roles are hashed into fixed tables, so nothing is
ever added to the model as the vocabulary grows.

It learns by prediction at each story's end (Rao and Ballard's teacher, item 7c): the story
is replayed, each argument predicted from the rest, and the error moves the vectors, with
as many arguments of earlier stories replayed beside it, so what was learnt is rehearsed as
the hippocampus replays to the cortex. Read offline (`scripts/situation.py`), replay past a
few passes overfits at this size, so each argument is replayed about three times in all."""

from __future__ import annotations

import hashlib
import random
from pathlib import Path

import numpy as np
import torch

# the hashed tables: words and roles, and the width of every vector
WORDS = 1 << 14
ROLES = 1 << 8
WIDTH = 128
# an event's items read, and the story's items before it, at most; how much less each
# earlier item of the story counts
ITEMS = 12
BEFORE = 24
DECAY = 0.85
# how many arguments of earlier stories are replayed beside each of this story's, and how
# many are kept to replay from
REPLAY = 2
KEPT = 200_000
RATE = 3e-3
# how many hearings a role's fillers are backed off by, to how common each word is
BACKOFF = 2.0
BATCH = 256


def _h(key: str, size: int) -> int:
    return int.from_bytes(hashlib.blake2b(key.encode(), digest_size=8).digest(),
                          "little") % size


def word(w: str) -> int:
    return _h("w:" + w, WORDS)


def role(r: str) -> int:
    return _h("r:" + r, ROLES)


class _Model(torch.nn.Module):
    def __init__(self):
        super().__init__()
        self.E = torch.nn.Embedding(WORDS + 1, WIDTH, padding_idx=WORDS)
        self.G = torch.nn.Embedding(ROLES + 1, WIDTH, padding_idx=ROLES)
        self.Q = torch.nn.Embedding(ROLES, WIDTH)
        self.O = torch.nn.Embedding(WORDS, WIDTH)
        self.S = torch.nn.Linear(WIDTH, WIDTH, bias=False)
        torch.nn.init.normal_(self.E.weight, 0, 0.1)
        torch.nn.init.normal_(self.G.weight, 1, 0.1)
        torch.nn.init.normal_(self.O.weight, 0, 0.1)
        torch.nn.init.normal_(self.Q.weight, 1, 0.1)
        with torch.no_grad():
            self.E.weight[WORDS].zero_()
            self.G.weight[ROLES].zero_()

    def forward(self, cw, cr, bw, br, bweight, r):
        h = (self.E(cw) * self.G(cr)).sum(1)
        b = (self.E(bw) * self.G(br) * bweight.unsqueeze(-1)).sum(1)
        return torch.tanh(h + self.S(b)) * self.Q(r)


class Predictor:
    """Heard an event at a time; replays at each story's end; asked which of some words
    fills a role, given the rest of the event and the story so far."""

    def __init__(self, directory: Path, seed: int = 0):
        self.path = directory / "predictor.pt"
        torch.manual_seed(seed)
        self.random = random.Random(seed)
        self.model = _Model()
        self.opt = torch.optim.Adam(self.model.parameters(), lr=RATE)
        # how often each role was filled by each word, exact
        self.filled = torch.zeros(ROLES, WORDS)
        # the story's items so far, its arguments to replay, and earlier stories' arguments
        self.story: list[tuple[int, int]] = []
        self.told: list[tuple] = []
        self.kept: list[tuple] = []
        self.seen = 0
        if self.path.exists():
            got = torch.load(self.path, weights_only=False)
            self.model.load_state_dict(got["model"])
            self.opt.load_state_dict(got["opt"])
            self.filled = got["filled"]
            self.story, self.told, self.kept, self.seen = (
                got["story"], got["told"], got["kept"], got["seen"])

    def heard(self, items: list[tuple[str, str]]) -> None:
        """An event: ('head', lemma) first, then (link, word) for each argument."""
        coded = [(role(r), word(w)) for r, w in items]
        head = items[0][1]
        before = self.story[-BEFORE:][::-1]
        for j, (r, w) in enumerate(coded):
            if j == 0 or items[j][1] == head:
                continue
            ctx = [x for k, x in enumerate(coded) if k != j][:ITEMS]
            self.told.append((ctx, before, r, w))
            self.filled[r, w] += 1
        self.story.extend(coded)

    def ended(self) -> None:
        """The story's end: replay it, with earlier stories' arguments beside it, and keep
        it to replay later."""
        if self.told:
            old = self.random.sample(self.kept, min(len(self.kept), REPLAY * len(self.told)))
            rows = self.told + old
            self.random.shuffle(rows)
            for s in range(0, len(rows), BATCH):
                self._learn(rows[s:s + BATCH])
            for row in self.told:
                # a reservoir: every argument heard equally likely to be kept
                self.seen += 1
                if len(self.kept) < KEPT:
                    self.kept.append(row)
                elif (i := self.random.randrange(self.seen)) < KEPT:
                    self.kept[i] = row
        self.story, self.told = [], []

    def _tensors(self, rows):
        n = len(rows)
        cw = torch.full((n, ITEMS), WORDS, dtype=torch.long)
        cr = torch.full((n, ITEMS), ROLES, dtype=torch.long)
        bw = torch.full((n, BEFORE), WORDS, dtype=torch.long)
        br = torch.full((n, BEFORE), ROLES, dtype=torch.long)
        bweight = torch.zeros((n, BEFORE))
        r = torch.zeros(n, dtype=torch.long)
        for i, (ctx, before, rr, _) in enumerate(rows):
            for j, (a, b) in enumerate(ctx[:ITEMS]):
                cr[i, j], cw[i, j] = a, b
            for j, (a, b) in enumerate(before[:BEFORE]):
                br[i, j], bw[i, j], bweight[i, j] = a, b, DECAY ** j
            r[i] = rr
        return cw, cr, bw, br, bweight, r

    def _prior(self, r):
        # how often the role was filled by each word, backed off to how often the word was
        # heard at all: a word never heard in a role is as likely as it is common, never
        # ruled out (a veto lost, `41183049`; a near-veto read 0.303 on the cloze)
        every = self.filled.sum(0)
        common = (every + 1) / (every.sum() + WORDS)
        f = self.filled[r]
        return torch.log((f + BACKOFF * common) / (f.sum(1, keepdim=True) + BACKOFF))

    def _learn(self, rows) -> None:
        cw, cr, bw, br, bweight, r = self._tensors(rows)
        target = torch.tensor([w for *_, w in rows])
        logits = self.model(cw, cr, bw, br, bweight, r) @ self.model.O.weight.T + self._prior(r)
        loss = torch.nn.functional.cross_entropy(logits, target)
        self.opt.zero_grad()
        loss.backward()
        self.opt.step()

    def predict(self, slot: str, items: list[tuple[str, str]],
                names: list[str]) -> dict[str, float]:
        """Each name's chance of filling `slot`, among `names`, given the event's other
        items and the story so far."""
        if not names:
            return {}
        ctx = [(role(r), word(w)) for r, w in items][:ITEMS]
        before = self.story[-BEFORE:][::-1]
        r = role(slot)
        with torch.no_grad():
            q = self.model(*self._tensors([(ctx, before, r, 0)]))[0]
            ids = torch.tensor([word(n) for n in names])
            s = self.model.O.weight[ids] @ q + self._prior(torch.tensor([r]))[0][ids]
            p = torch.softmax(s, 0).numpy()
        return {n: float(x) for n, x in zip(names, np.asarray(p))}

    def save(self) -> None:
        torch.save({"model": self.model.state_dict(), "opt": self.opt.state_dict(),
                    "filled": self.filled, "story": self.story, "told": self.told,
                    "kept": self.kept, "seen": self.seen}, self.path)
