"""The off-the-shelf loop: TRL's GRPO on the trunk, the key as the reward (the R1 recipe).

The trunk samples several whole solutions to a GSM8K training problem; each is rewarded by
whether its answer line matches the gold number; the group's rewards are normalised and the
trunk's adapter is pushed toward the solutions that scored. No step judge and no search: this is
the published recipe for a small model learning to reason from checkable outcomes, run here to
read what it does on this card before anything of ours is built on it. The trunk's non-thinking
solution by default (a few hundred tokens); `--mode think` samples its thinking, ten times the
tokens. LoRA on the same projections as `trunk_adapter.py`; fp32, since the card has no bf16;
checkpoints every `--save-steps` and resumes from the last. A later round starts from the round
before's adapter (`--init-adapter`) on the next training problems (`--skip`), or on problems the
system chose for itself (`--from-rows`: the reading rows of its own greedy pass over training
problems, every one it got wrong and as many it got right, since a group whose four samples all
score alike teaches nothing and the easy problems are mostly such groups).

    .venv/Scripts/python scripts/grpo.py --n 2 --max-steps 2 --out state/grpo/smoke     # memory and seconds a step
    .venv/Scripts/python scripts/grpo.py --n 1000 --out state/grpo/round1
    .venv/Scripts/python scripts/grpo.py --n 1000 --skip 1000 --init-adapter state/grpo/round1/adapter --out state/grpo/round2

Writes <out>/checkpoint-*/ (the adapter and trainer state) and <out>/grpo.json.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.maths import number_in, problems, rows_of, same_number  # noqa: E402

TARGETS = ["q_proj", "k_proj", "v_proj", "o_proj", "in_proj_qkv", "out_proj", "gate_proj", "up_proj", "down_proj"]
SYSTEM = ("Solve the problem step by step, briefly. End with a last line of the form 'Answer: N' "
          "where N is the final number, and nothing after it.")


def sha() -> str:
    return subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True,
                          cwd=ROOT).stdout.strip()


def select_rows(rows: list[dict], wrong_share: float = 0.5, seed: int = 0) -> list[dict]:
    """Training problems from the system's own reading rows: every problem it got wrong, and
    right ones to make the wrong ones `wrong_share` of the whole, drawn at random, in a shuffled
    order."""
    import random

    rng = random.Random(seed)
    wrong = [r for r in rows if not r["correct"]]
    right = [r for r in rows if r["correct"]]
    rng.shuffle(right)
    n_right = min(len(right), int(round(len(wrong) * (1 - wrong_share) / max(wrong_share, 1e-9))))
    chosen = wrong + right[:n_right]
    rng.shuffle(chosen)
    return [{"question": r["question"], "answer": r["answer"]} for r in chosen]


def answer_in(text: str) -> str | None:
    """The number on the completion's answer line, else the last number in it."""
    m = re.findall(r"answer\s*:?\**\s*([^\n]+)", text, re.I)
    return number_in(m[-1]) if m else number_in(text)


def correct(completions, answer, **kwargs) -> list[float]:
    """1.0 where the completion's answer is the gold number."""
    texts = [c[0]["content"] if isinstance(c, list) else c for c in completions]
    return [1.0 if same_number(answer_in(t), a) else 0.0 for t, a in zip(texts, answer)]


def answer_line(completions, **kwargs) -> list[float]:
    """0.1 where the completion ends with an answer line, so the form is learnt even where the
    number is wrong. On a set of problems the trunk mostly gets wrong this is the only reward
    with a spread inside a group, and it pulls toward short completions that end in the form
    (round three, 2026-10-31); `--format-reward 0` drops it once the form is learnt."""
    texts = [c[0]["content"] if isinstance(c, list) else c for c in completions]
    return [0.1 if re.search(r"answer\s*:\s*[^\n]+\s*$", t.strip(), re.I) else 0.0 for t in texts]


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--out", default="state/grpo/round1")
    p.add_argument("--model", default="Qwen/Qwen3.5-2B")
    p.add_argument("--n", type=int, default=1000, help="training problems")
    p.add_argument("--skip", type=int, default=0, help="training problems passed over first")
    p.add_argument("--init-adapter", default=None, help="the adapter to go on training, else a fresh one")
    p.add_argument("--from-rows", default=None, help="reading rows to choose the problems from, instead of --n/--skip")
    p.add_argument("--wrong-share", type=float, default=0.5, help="share of chosen problems the pass got wrong")
    p.add_argument("--mode", choices=["plain", "think"], default="plain")
    p.add_argument("--generations", type=int, default=4, help="solutions sampled a problem")
    p.add_argument("--prompts-per-step", type=int, default=2, help="problems an optimiser step")
    p.add_argument("--format-reward", type=float, default=0.1, help="the answer-line bonus; 0 drops it")
    p.add_argument("--micro", type=int, default=None,
                   help="solutions a forward pass; the generations by default, 2 when the card is shared by day")
    p.add_argument("--max-completion", type=int, default=None, help="tokens a solution; 400 plain, 1536 think")
    p.add_argument("--lr", type=float, default=2e-5)
    p.add_argument("--epochs", type=float, default=1.0)
    p.add_argument("--max-steps", type=int, default=-1)
    p.add_argument("--save-steps", type=int, default=20)
    p.add_argument("--rank", type=int, default=16)
    p.add_argument("--seed", type=int, default=0)
    args = p.parse_args()

    import torch
    from datasets import Dataset
    from peft import LoraConfig
    from trl import GRPOConfig, GRPOTrainer

    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    if args.from_rows:
        rows = rows_of(args.from_rows)
        ps = select_rows(rows, args.wrong_share, args.seed)
        print(f"{len(ps)} problems chosen from {len(rows)} rows, {sum(not r['correct'] for r in rows)} wrong", flush=True)
    else:
        ps = problems(args.n, split="train", skip=args.skip)
    data = Dataset.from_list([{"prompt": [{"role": "system", "content": SYSTEM},
                                          {"role": "user", "content": x["question"]}],
                               "answer": x["answer"]} for x in ps])
    max_completion = args.max_completion or (1536 if args.mode == "think" else 400)
    micro = args.micro or args.generations
    per_step = args.generations * args.prompts_per_step
    if per_step % micro:
        raise SystemExit(f"--micro {micro} must divide the {per_step} solutions of a step")
    config = GRPOConfig(
        output_dir=str(out), seed=args.seed, learning_rate=args.lr, num_train_epochs=args.epochs,
        max_steps=args.max_steps, per_device_train_batch_size=micro,
        gradient_accumulation_steps=per_step // micro, num_generations=args.generations,
        max_completion_length=max_completion, temperature=1.0, beta=0.0,
        gradient_checkpointing=True, gradient_checkpointing_kwargs={"use_reentrant": False},
        logging_steps=1, save_steps=args.save_steps, save_total_limit=2, report_to="none",
        reward_weights=[1.0, args.format_reward / 0.1] if args.format_reward else [1.0],
        chat_template_kwargs={"enable_thinking": args.mode == "think"}, mask_truncated_completions=True,
        log_completions=False, bf16=False, fp16=False,
        torch_empty_cache_steps=1,
    )
    peft = LoraConfig(r=args.rank, lora_alpha=2 * args.rank, lora_dropout=0.0, target_modules=TARGETS,
                      task_type="CAUSAL_LM")
    from transformers import AutoModelForCausalLM, AutoTokenizer

    # the text model and its tokenizer, loaded here: given the name, TRL reaches for the trunk's
    # vision processor, which needs image libraries this project does not carry
    model = AutoModelForCausalLM.from_pretrained(args.model, dtype=torch.float32)
    tok = AutoTokenizer.from_pretrained(args.model)
    if args.init_adapter:
        from peft import PeftModel

        model = PeftModel.from_pretrained(model, args.init_adapter, is_trainable=True)
        peft = None
    rewards = [correct] + ([answer_line] if args.format_reward else [])
    trainer = GRPOTrainer(model=model, processing_class=tok, reward_funcs=rewards, args=config,
                          train_dataset=data, peft_config=peft)
    last = sorted(out.glob("checkpoint-*"), key=lambda d: int(d.name.split("-")[1]))
    started = time.perf_counter()
    trainer.train(resume_from_checkpoint=str(last[-1]) if last else None)
    trainer.save_model(str(out / "adapter"))
    log = [dict(e) for e in trainer.state.log_history]
    (out / "grpo.json").write_text(json.dumps({
        "model": args.model, "mode": args.mode, "n": len(ps), "skip": args.skip, "init_adapter": args.init_adapter,
        "from_rows": args.from_rows, "wrong_share": args.wrong_share if args.from_rows else None, "micro": micro,
        "generations": args.generations,
        "prompts_per_step": args.prompts_per_step, "max_completion": max_completion, "lr": args.lr,
        "epochs": args.epochs, "max_steps": args.max_steps, "rank": args.rank, "system": SYSTEM,
        "rewards": ["correct: 1.0 where the answer line is the gold number"]
                   + ([f"answer_line: {args.format_reward} where the completion ends with an answer line"]
                      if args.format_reward else []),
        "log": log, "seconds": round(time.perf_counter() - started, 1),
        "peak_gib": round(torch.cuda.max_memory_allocated() / 2**30, 2), "sha": sha(), "command": " ".join(sys.argv),
    }, indent=1), encoding="utf-8")
    print("wrote", out / "grpo.json", f"peak {torch.cuda.max_memory_allocated() / 2**30:.2f} GiB")


if __name__ == "__main__":
    main()
