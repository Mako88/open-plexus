"""What a small language model makes of the cloze from the same experience: the control
for whether the system's cloze is low for what it has heard, or low for what it does
with it. A baseline, never part of the system (DECIDED, no language model in it).

A word-level transformer is trained from nothing on stories the system heard before a
test half, then scored on the test half's clozes as the Children's Book Test's language
models were: each of the story's nouns named so far is put in the blank of the held-back
sentence, and the one that makes the sentence likeliest, given the story so far, is its
answer. `--train` is how many stories it learns from: the first half of the stream (what
the system had heard by then, about), or every story of the split outside the stream.

    uv run python scripts/small_lm.py --stories 1000 --train half --note "..."
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
import random
import re
import sys
import time
from collections import Counter
from datetime import UTC, datetime
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")

import torch  # noqa: E402
from torch import nn  # noqa: E402

from unfused.exam.stories import VALID, fingerprint, raw, right, stream  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
WORD = re.compile(r"[a-z]+(?:'[a-z]+)?|[0-9]+|[^\sa-z0-9]")
# the model: small enough to train in minutes on one 1080 Ti, large enough not to be the
# limit on 75,000 words
LAYERS, WIDTH, HEADS, CONTEXT, DROP = 4, 256, 4, 256, 0.2
# words heard fewer times than this are one unknown word, as a learner hears them
RARE = 2


def words(text: str) -> list[str]:
    return WORD.findall(text.lower())


class Model(nn.Module):
    def __init__(self, vocab: int) -> None:
        super().__init__()
        self.embed = nn.Embedding(vocab, WIDTH)
        self.place = nn.Embedding(CONTEXT, WIDTH)
        layer = nn.TransformerEncoderLayer(WIDTH, HEADS, 4 * WIDTH, DROP, batch_first=True,
                                           norm_first=True)
        self.body = nn.TransformerEncoder(layer, LAYERS)
        self.out = nn.Linear(WIDTH, vocab)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        n = x.shape[1]
        mask = torch.triu(torch.full((n, n), float("-inf"), device=x.device), 1)
        h = self.embed(x) + self.place(torch.arange(n, device=x.device))
        return self.out(self.body(h, mask=mask, is_causal=True))


def train(texts: list[str], device: str, seed: int) -> tuple[Model, dict]:
    torch.manual_seed(seed)
    random.seed(seed)
    counts = Counter(w for t in texts for w in words(t))
    vocab = {"<unk>": 0, "<s>": 1}
    for w, n in counts.items():
        if n >= RARE:
            vocab[w] = len(vocab)
    ids = []
    for t in texts:
        ids += [1] + [vocab.get(w, 0) for w in words(t)]
    data = torch.tensor(ids)
    cut = int(len(data) * 0.95)
    fit, held = data[:cut], data[cut:]
    model = Model(len(vocab)).to(device)
    opt = torch.optim.AdamW(model.parameters(), lr=3e-4, weight_decay=0.1)
    loss_fn = nn.CrossEntropyLoss()

    def batches(d: torch.Tensor, n: int):
        for _ in range(n):
            at = torch.randint(0, len(d) - CONTEXT - 1, (32,))
            x = torch.stack([d[i:i + CONTEXT] for i in at]).to(device)
            y = torch.stack([d[i + 1:i + 1 + CONTEXT] for i in at]).to(device)
            yield x, y

    best, best_state, worse = math.inf, None, 0
    steps = max(200, len(fit) // (32 * CONTEXT) * 4)
    for epoch in range(200):
        model.train()
        for x, y in batches(fit, steps // 4):
            opt.zero_grad()
            loss = loss_fn(model(x).reshape(-1, len(vocab)), y.reshape(-1))
            loss.backward()
            opt.step()
        model.eval()
        with torch.no_grad():
            v = sum(loss_fn(model(x).reshape(-1, len(vocab)), y.reshape(-1)).item()
                    for x, y in batches(held, 20)) / 20
        print(f"  epoch {epoch} held-out loss {v:.3f}", flush=True)
        if v < best - 1e-3:
            best, worse = v, 0
            best_state = {k: t.detach().clone() for k, t in model.state_dict().items()}
        else:
            worse += 1
            if worse >= 3:
                break
    model.load_state_dict(best_state)
    model.eval()
    return model, {"vocab": len(vocab), "words": len(ids), "held_out_loss": best,
                   "epochs": epoch + 1, "_vocab": vocab}


def likelihood(model: Model, vocab: dict, before: list[str], sentence: list[str],
               device: str) -> float:
    """The log-probability of the sentence's words given the story so far."""
    ctx = [1] + [vocab.get(w, 0) for w in before]
    sent = [vocab.get(w, 0) for w in sentence]
    seq = (ctx + sent)[-CONTEXT:]
    k = min(len(sent), len(seq) - 1)
    x = torch.tensor([seq[:-1]], device=device)
    with torch.no_grad():
        logp = torch.log_softmax(model(x)[0], -1)
    targets = seq[-k:]
    return sum(logp[len(seq) - 1 - k + i, t].item() for i, t in enumerate(targets))


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--stories", type=int, default=1000)
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--train", choices=("half", "outside"), default="half")
    p.add_argument("--note", default="")
    args = p.parse_args()
    device = "cuda" if torch.cuda.is_available() else "cpu"
    stories = stream(args.stories, args.seed)
    half = args.stories // 2
    if args.train == "half":
        texts = [" ".join(s.told + s.after) for s in stories[:half]]
    else:
        every = raw(VALID)
        # the stream's own order, so what it holds is left out
        order = sorted(range(len(every)), key=lambda i: hashlib.sha256(
            f"{args.seed}|{i}".encode()).hexdigest())
        texts = [every[i] for i in order[args.stories:]]
    t = time.time()
    print(f"training on {len(texts)} stories", flush=True)
    model, info = train(texts, device, args.seed)
    vocab = info.pop("_vocab")
    rows = []
    for s in stories[half:]:
        if not s.question or not s.after:
            continue
        held = s.after[0]
        hole = re.search(rf"\b{re.escape(s.answer)}\b", held, re.I)
        pool = list(dict.fromkeys(s.earlier))
        if hole is None or not pool:
            continue
        before = words(" ".join(s.told))
        scored = []
        for c in pool:
            sentence = words(held[:hole.start()] + c + held[hole.end():])
            scored.append((likelihood(model, vocab, before, sentence, device), c))
        said = max(scored)[1]
        rows.append({"story": s.index, "question": s.question, "answer": s.answer,
                     "said": said, "correct": right(s.answers, said), "pool": len(pool),
                     "in_pool": any(right(s.answers, c) for c in pool)})
    score = sum(r["correct"] for r in rows) / len(rows) if rows else 0.0
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    reading = {"kind": "small-lm", "taken_at": stamp, "note": args.note,
               "stream": {"source": "TinyStories-valid", "seed": args.seed,
                          "stories": args.stories, "fingerprint": fingerprint(stories),
                          "tested": f"{half}-{args.stories}"},
               "train": args.train, "trained_on": len(texts), "model": {
                   "layers": LAYERS, "width": WIDTH, "heads": HEADS, "context": CONTEXT,
                   "dropout": DROP, "rare": RARE, **info},
               "score": score, "n": len(rows),
               "in_pool": sum(r["in_pool"] for r in rows) / len(rows) if rows else 0.0,
               "seconds": round(time.time() - t, 1), "rows": rows}
    path = ROOT / "readings" / f"small-lm-{args.train}-s{args.seed}-n{args.stories}-{stamp}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print(f"score {score:.3f} over {len(rows)} clozes (answer in pool "
          f"{reading['in_pool']:.3f}) -> {path.name}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
