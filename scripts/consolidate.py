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
from openplexus.lineage import Lineage  # noqa: E402


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
    p.add_argument("--micro", type=int, default=2, help="rows a micro-batch at most")
    p.add_argument("--micro-tokens", type=int, default=1100, help="padded tokens a micro-batch at most; a longer row goes alone")
    p.add_argument("--max-tokens", type=int, default=384)
    p.add_argument("--init-adapter", default="system",
                   help="the adapter replayed into: 'system' is the lineage's as it stands (none where there is no lineage), "
                        "'none' a fresh adapter, or a PEFT folder")
    p.add_argument("--augment", type=int, default=0, help="restatements and questions a row from the served trunk, replayed with it")
    p.add_argument("--part", default="skills", choices=["skills", "facts"],
                   help="facts: train the facts adapter over the system's skills, merged and frozen")
    p.add_argument("--no-qa", action="store_true", help="replay the rows and their restatements only, not the question-and-answer lines")
    p.add_argument("--rehearse", action="append", default=[], help="a rehearsal file (`rehearsal.py`), one per skill: the system's own right transcripts replayed with the rows")
    p.add_argument("--augment-only", action="store_true", help="write the augmentations (the server up) and stop; the training runs after, the server down")
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--fresh", action="store_true", help="ignore a checkpoint in --out and start over")
    p.add_argument("--checkpoint-minutes", type=float, default=8.0, help="a checkpoint this often within an epoch, and at every epoch's end")
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
        if args.no_qa:
            # the question-and-answer lines teach answering without looking (2026-11-05); only restatements
            kinds = {json.loads(line)["text"]: json.loads(line)["kind"]
                     for line in (out / "augmented.jsonl").read_text(encoding="utf-8").splitlines()}
            extra = [x for x in extra if kinds.get(x) == "rewrite"]
        texts += extra
        print(f"{len(rows)} rows and {len(extra)} augmentations", flush=True)
        if args.augment_only:
            return
    tok = AutoTokenizer.from_pretrained(args.model)
    encoded = [tok(t + tok.eos_token, truncation=True, max_length=args.max_tokens)["input_ids"] for t in texts]
    rehearsed = 0
    if args.rehearse:
        # the system's own right answers, as the transcripts the trunk saw them in, interleaved with the rows
        for path in args.rehearse:
            for line in Path(path).read_text(encoding="utf-8").splitlines():
                rec = json.loads(line)
                text = tok.apply_chat_template(rec["messages"], tools=rec.get("tools"), tokenize=False, enable_thinking=False)
                encoded.append(tok(text, add_special_tokens=False)["input_ids"][:args.max_tokens * 4])
                rehearsed += 1
    print(f"{len(texts)} rows, {rehearsed} rehearsed transcripts, {sum(map(len, encoded))} tokens, {max(map(len, encoded))} the longest", flush=True)

    if args.part == "facts":
        # the skills adapter is merged into the trunk in memory and frozen there; the sleep writes
        # only the facts adapter, which the server applies on top of the skills one
        skills = Lineage(part="skills").current()
        facts = Lineage(part="facts")
        if args.init_adapter == "system":
            args.init_adapter = str(facts.current()) if facts.exists() else None
    else:
        skills = None
    if args.init_adapter == "system":
        args.init_adapter = str(Lineage().current()) if Lineage().exists() else None
    elif args.init_adapter == "none":
        args.init_adapter = None
    print(f"replaying into {args.init_adapter or 'a fresh adapter'}" + (f" over the skills of {skills}, frozen" if skills else ""), flush=True)
    model = AutoModelForCausalLM.from_pretrained(args.model, dtype=torch.float32).cuda()
    if skills:
        from peft import PeftModel

        model = PeftModel.from_pretrained(model, str(skills)).merge_and_unload()
    model.gradient_checkpointing_enable(gradient_checkpointing_kwargs={"use_reentrant": False})
    model.enable_input_require_grads()
    # a sleep paused for a game resumes at the epoch it reached: the adapter, the optimiser and the
    # log are written after every epoch to <out>/checkpoint
    ckpt = out / "checkpoint"
    if args.fresh and ckpt.exists():
        import shutil

        shutil.rmtree(ckpt)
    resumed = json.loads((ckpt / "progress.json").read_text(encoding="utf-8")) if (ckpt / "progress.json").exists() else None
    if resumed or args.init_adapter:
        from peft import PeftModel

        model = PeftModel.from_pretrained(model, str(ckpt) if resumed else args.init_adapter, is_trainable=True)
    else:
        model = get_peft_model(model, LoraConfig(r=args.rank, lora_alpha=2 * args.rank, lora_dropout=0.0,
                                                 target_modules=TARGETS, task_type="CAUSAL_LM"))
    model.print_trainable_parameters()
    opt = torch.optim.AdamW([q for q in model.parameters() if q.requires_grad], lr=args.lr, weight_decay=0.0)
    pad = tok.pad_token_id if tok.pad_token_id is not None else tok.eos_token_id
    log, started, step, first_epoch, skip_groups, losses = [], time.perf_counter(), 0, 0, 0, []
    if resumed:
        opt.load_state_dict(torch.load(ckpt / "optimizer.pt", map_location="cuda"))
        log, step, first_epoch = resumed["log"], resumed["steps"], resumed["epoch"]
        skip_groups, losses = resumed.get("groups_done", 0), resumed.get("losses", [])
        print(f"resumed at epoch {first_epoch + 1} of {args.epochs}, group {skip_groups}, from {ckpt}", flush=True)

    def checkpoint(epoch_done: int, groups_done: int) -> None:
        model.save_pretrained(str(ckpt))
        torch.save(opt.state_dict(), ckpt / "optimizer.pt")
        (ckpt / "progress.json").write_text(json.dumps({"epoch": epoch_done, "groups_done": groups_done, "steps": step,
                                                        "log": log, "losses": losses}), encoding="utf-8")

    model.train()
    saved = time.perf_counter()
    for epoch in range(first_epoch, args.epochs):
        rng = random.Random(args.seed + epoch)  # the epoch's own order, the same after a resume
        order = list(range(len(encoded)))
        rng.shuffle(order)
        if epoch != first_epoch:
            skip_groups, losses = 0, []
        for g, at in enumerate(range(0, len(order), args.batch)):
            if g < skip_groups:
                continue
            group = order[at:at + args.batch]
            opt.zero_grad()
            # micro-batches by a token budget, longest first, so a 1,200-token asking transcript
            # goes alone and two 380-token rows go together: the card's limit is tokens, not rows
            # (the second sleep ran out of memory at two transcripts a micro-batch, 2026-11-05)
            by_len = sorted(group, key=lambda i: -len(encoded[i]))
            micros, cur = [], []
            for i in by_len:
                if cur and (len(cur) >= args.micro or (len(cur) + 1) * max(len(encoded[j]) for j in cur + [i]) > args.micro_tokens):
                    micros.append(cur)
                    cur = []
                cur.append(i)
            micros.append(cur)
            for micro in micros:
                items = [encoded[i] for i in micro]
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
            # the system's own progress for the supervisor (`openplexus.downtime.Rate`)
            (out / "heartbeat.json").write_text(json.dumps({"t": time.time(), "steps": step}), encoding="utf-8")
            if time.perf_counter() - saved > 60 * args.checkpoint_minutes:
                checkpoint(epoch, g + 1)  # within the epoch: this many groups of it done
                saved = time.perf_counter()
        entry = {"epoch": epoch + 1, "loss": round(sum(losses) / len(losses), 4), "steps": step,
                 "seconds": round(time.perf_counter() - started)}
        log.append(entry)
        print(json.dumps(entry), flush=True)
        checkpoint(epoch + 1, 0)
        saved = time.perf_counter()
    model.save_pretrained(str(out / "adapter"))
    (out / "consolidate.json").write_text(json.dumps({
        "model": args.model, "n": args.n, "skip": args.skip, "rows": args.rows, "texts": len(texts), "augment": args.augment,
        "row_texts": len(rows),
        "tokens": sum(map(len, encoded)), "rank": args.rank, "lr": args.lr, "epochs": args.epochs, "batch": args.batch,
        "log": log, "peak_gib": round(torch.cuda.max_memory_allocated() / 2**30, 2), "note": args.note,
        "init_adapter": args.init_adapter, "rehearse": args.rehearse, "rehearsed": rehearsed,
        "command": "scripts/consolidate.py " + " ".join(sys.argv[1:]),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True, cwd=ROOT).stdout.strip(),
    }, indent=1), encoding="utf-8")
    print(f"wrote {out / 'consolidate.json'} peak {torch.cuda.max_memory_allocated() / 2**30:.2f} GiB", flush=True)


if __name__ == "__main__":
    main()
