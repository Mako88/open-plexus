"""Step 7, consolidation: knowledge moving from rows into weights by replay, the first form.
The store's rows for a set of questions (the supporting paragraphs of the first `--n` HotpotQA
validation questions, what the store holds for them) are replayed to a LoRA on the trunk as
plain text, causal-LM loss, several epochs, nothing of the questions or answers shown. The
reading is the floor arm with the adapter: a fact that has been consolidated is answerable with
the store switched off, so the floor (0.15) should rise toward gold (0.61) on these questions
and not on the next `--n`, whose rows were not replayed (`--skip`, the control for
specificity); the gold arm with the adapter is the cost, whether the trunk still reads a
passage put in front of it. Read 2026-11-03: the rows replayed as they stand, eight epochs to a
loss of 0.20, leave the floor where it was (0.14 against 0.15), the memorised text not answerable
as a question, which is the known shape (Allen-Zhu and Li, 2023: a fact is extractable only when
it was seen in varied forms). `--augment K` is the second form: before training, the served trunk
writes K restatements of each row and K question-and-answer lines from it, saved resumably to
<out>/augmented.jsonl, and the replay is the rows with their restatements and questions.

    .venv/Scripts/python scripts/consolidate.py --n 200 --epochs 8 --out state/consolidate/first200     # the card
    .venv/Scripts/python scripts/consolidate.py --n 200 --epochs 8 --augment 4 --out state/consolidate/first200-aug   # server first, then the card
    .venv/Scripts/python scripts/hotpot_reading.py --arms floor --adapter state/gguf/consolidate-first200-lora.gguf --against floor,gold

Writes <out>/adapter/ (the PEFT weights) and <out>/consolidate.json.
"""

from __future__ import annotations

import argparse
import json
import random
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
sys.path.insert(0, str(ROOT / "scripts"))

from grpo import TARGETS  # noqa: E402
from openplexus.hotpot import paragraph_text, questions  # noqa: E402


REWRITE = ("Rewrite the passage below in your own words, keeping every fact, name, number and date exactly. "
           "Reply with the rewritten passage only.")
ASK = ("Write one question that the passage below answers, with its short answer (a name, a number, a date, or yes or "
       "no), as two lines: 'Question: ...' then 'Answer: ...'. Ask about a fact the passage states; where earlier "
       "questions are listed, ask about a different fact. Reply with the two lines only.")


def augmented(texts: list[str], k: int, path: Path, trunk_for) -> list[str]:
    """K restatements and K question-and-answer lines a row from the served trunk, written to
    `path` as they come so a run cut short carries on; the extra texts to replay. `trunk_for(j)`
    gives the trunk for the j-th pass, sampling with its own seed, since greedy decoding wrote the
    same four restatements and the same four questions of every row (2026-11-03); and the j-th
    question is asked with the row's earlier questions shown, to be about another fact."""
    done = {}
    if path.exists():
        for line in path.read_text(encoding="utf-8").splitlines():
            r = json.loads(line)
            done[(r["row"], r["kind"], r["k"])] = r["text"]
    chunk = 64
    with open(path, "a", encoding="utf-8") as f:
        for j in range(k):
            wanted = [(i, kind) for i in range(len(texts)) for kind in ("rewrite", "ask") if (i, kind, j) not in done]
            print(f"pass {j + 1}/{k}: {len(wanted)} to write", flush=True)
            for at in range(0, len(wanted), chunk):
                batch = wanted[at:at + chunk]
                prompts = []
                for i, kind in batch:
                    if kind == "rewrite":
                        prompts.append([{"role": "system", "content": REWRITE}, {"role": "user", "content": texts[i]}])
                    else:
                        earlier = [done[(i, "ask", jj)] for jj in range(j) if done.get((i, "ask", jj))]
                        shown = ("\n\nQuestions already written about this passage, which the new one must not repeat "
                                 "or paraphrase:\n" + "\n".join(q.splitlines()[0] for q in earlier)) if earlier else ""
                        prompts.append([{"role": "system", "content": ASK}, {"role": "user", "content": texts[i] + shown}])
                for (i, kind), reply in zip(batch, trunk_for(j).ask_many(prompts)):
                    text = reply.strip()
                    if kind == "ask" and not (text.startswith("Question:") and "\nAnswer:" in text):
                        text = ""  # a line not in the asked form is left out, and recorded so it is not asked again
                    f.write(json.dumps({"row": i, "kind": kind, "k": j, "text": text}, ensure_ascii=False) + "\n")
                    done[(i, kind, j)] = text
                print(f"  {min(at + chunk, len(wanted))}/{len(wanted)}", flush=True)
    return [x for x in done.values() if x]


def rows_of(qs: list[dict], which: str) -> list[str]:
    """The texts replayed: the supporting paragraphs (`gold`) or all ten (`episode`) of each question."""
    return [paragraph_text(p) for q in qs for p in q["paragraphs"] if which == "episode" or p["supporting"]]


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--n", type=int, default=200, help="validation questions whose rows are replayed")
    p.add_argument("--skip", type=int, default=0)
    p.add_argument("--rows", default="gold", choices=["gold", "episode"])
    p.add_argument("--out", default="state/consolidate/first200")
    p.add_argument("--model", default="Qwen/Qwen3.5-2B")
    p.add_argument("--rank", type=int, default=16)
    p.add_argument("--lr", type=float, default=1e-4)
    p.add_argument("--epochs", type=int, default=8)
    p.add_argument("--batch", type=int, default=8, help="rows an optimiser step, accumulated")
    p.add_argument("--micro", type=int, default=2)
    p.add_argument("--max-tokens", type=int, default=384)
    p.add_argument("--augment", type=int, default=0, help="restatements and questions a row from the served trunk, replayed with it")
    p.add_argument("--augment-only", action="store_true", help="write the augmentations (the server up) and stop; the training runs after, the server down")
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--note", default="")
    args = p.parse_args()

    import torch
    from peft import LoraConfig, get_peft_model
    from transformers import AutoModelForCausalLM, AutoTokenizer

    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    qs = questions(args.n, skip=args.skip)
    rows = rows_of(qs, args.rows)
    texts = list(rows)
    if args.augment:
        from openplexus.trunk import ServedTrunk

        trunks = {j: ServedTrunk(max_new=320, parallel=8, temperature=0.8, seed=j + 1) for j in range(args.augment)}
        extra = augmented(rows, args.augment, out / "augmented.jsonl", trunks.__getitem__)
        texts += extra
        print(f"{len(rows)} rows and {len(extra)} augmentations", flush=True)
        if args.augment_only:
            return
    tok = AutoTokenizer.from_pretrained(args.model)
    encoded = [tok(t + tok.eos_token, truncation=True, max_length=args.max_tokens)["input_ids"] for t in texts]
    print(f"{len(texts)} rows, {sum(map(len, encoded))} tokens, {max(map(len, encoded))} the longest", flush=True)

    model = AutoModelForCausalLM.from_pretrained(args.model, dtype=torch.float32).cuda()
    model.gradient_checkpointing_enable(gradient_checkpointing_kwargs={"use_reentrant": False})
    model.enable_input_require_grads()
    model = get_peft_model(model, LoraConfig(r=args.rank, lora_alpha=2 * args.rank, lora_dropout=0.0,
                                             target_modules=TARGETS, task_type="CAUSAL_LM"))
    model.print_trainable_parameters()
    opt = torch.optim.AdamW([q for q in model.parameters() if q.requires_grad], lr=args.lr, weight_decay=0.0)
    rng = random.Random(args.seed)
    pad = tok.pad_token_id if tok.pad_token_id is not None else tok.eos_token_id
    log, started, step = [], time.perf_counter(), 0
    model.train()
    for epoch in range(args.epochs):
        order = list(range(len(encoded)))
        rng.shuffle(order)
        losses = []
        for at in range(0, len(order), args.batch):
            group = order[at:at + args.batch]
            opt.zero_grad()
            for m in range(0, len(group), args.micro):
                items = [encoded[i] for i in group[m:m + args.micro]]
                width = max(map(len, items))
                ids = torch.tensor([x + [pad] * (width - len(x)) for x in items]).cuda()
                mask = torch.tensor([[1] * len(x) + [0] * (width - len(x)) for x in items]).cuda()
                labels = ids.masked_fill(mask == 0, -100)
                loss = model(input_ids=ids, attention_mask=mask, labels=labels).loss * (len(items) / len(group))
                loss.backward()
                losses.append(loss.item() * len(group) / len(items))
            torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
            opt.step()
            step += 1
        entry = {"epoch": epoch + 1, "loss": round(sum(losses) / len(losses), 4), "steps": step,
                 "seconds": round(time.perf_counter() - started)}
        log.append(entry)
        print(json.dumps(entry), flush=True)
    model.save_pretrained(str(out / "adapter"))
    (out / "consolidate.json").write_text(json.dumps({
        "model": args.model, "n": args.n, "skip": args.skip, "rows": args.rows, "texts": len(texts), "augment": args.augment,
        "row_texts": len(rows),
        "tokens": sum(map(len, encoded)), "rank": args.rank, "lr": args.lr, "epochs": args.epochs, "batch": args.batch,
        "log": log, "peak_gib": round(torch.cuda.max_memory_allocated() / 2**30, 2), "note": args.note,
        "command": "scripts/consolidate.py " + " ".join(sys.argv[1:]),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True, cwd=ROOT).stdout.strip(),
    }, indent=1), encoding="utf-8")
    print(f"wrote {out / 'consolidate.json'} peak {torch.cuda.max_memory_allocated() / 2**30:.2f} GiB", flush=True)


if __name__ == "__main__":
    main()
