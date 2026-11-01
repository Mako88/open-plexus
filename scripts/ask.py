"""Step 6's arm: the asking as the trunk's own action, trained by the key. TRL's GRPO with an
environment a rollout: the question's ten HotpotQA paragraphs are the store for that episode,
the trunk is given one tool, `search(query)`, which returns the paragraph nearest the query by
MiniLM, and it is rewarded by the benchmark's exact match on its final answer. Nothing tells it
when to search or what for; the reward does (Search-R1, 2025, is this shape). The control is the
same trunk shown the two paragraphs nearest the question with no action (`hotpot_reading.py
--arms top:2`); the ceiling is the two supporting paragraphs.

    .venv/Scripts/python scripts/ask.py --n 2 --max-steps 2 --out state/ask/smoke --micro 2
    .venv/Scripts/python scripts/ask.py --n 1000 --out state/ask/round1 --micro 2

Writes <out>/checkpoint-*/, <out>/adapter and <out>/ask.json. Needs the card.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
sys.path.insert(0, str(ROOT / "scripts"))

from grpo import TARGETS, sha  # noqa: E402
from openplexus.hotpot import answer_of, exact, f1, paragraph_text, questions  # noqa: E402

SYSTEM = ("Answer the question. You have a tool, search(query), which returns the passage most relevant "
          "to your query from a small collection about this question; the facts you need are in that "
          "collection, usually in two different passages, so search for each thing you need to know. "
          "Then end with a last line of the form 'Answer: X' where X is the short answer (a name, a number, "
          "a date, or yes or no), and nothing after it.")

_EMBED = None


def embedder():
    global _EMBED
    if _EMBED is None:
        from openplexus.embed import MiniLM

        _EMBED = MiniLM()
    return _EMBED


class Store:
    """One rollout's store: the question's paragraphs, searched by MiniLM. `reset` takes the
    dataset row; `search` is the tool the trunk may call; `searches` counts the asking."""

    def __init__(self):
        self.paragraphs: list[str] = []
        self.keys = None
        self.searches: list[str] = []
        self.shown: list[int] = []

    def reset(self, **row) -> None:
        import numpy as np

        self.paragraphs = [p["text"] for p in row["paragraphs"]]
        self.supporting = [p["supporting"] for p in row["paragraphs"]]
        keys = np.asarray(embedder()(self.paragraphs), dtype=np.float32)
        self.keys = keys / np.maximum(np.linalg.norm(keys, axis=1, keepdims=True), 1e-12)
        self.searches, self.shown = [], []
        return None

    def search(self, query: str) -> str:
        """Search the collection for a passage.

        Args:
            query: what to look for, a name or a short description of the fact needed.

        Returns:
            The most relevant passage, with its title first.
        """
        import numpy as np

        self.searches.append(query)
        q = np.asarray(embedder()([query]), dtype=np.float32)[0]
        q /= max(float(np.linalg.norm(q)), 1e-12)
        order = np.argsort(-(self.keys @ q))
        pick = next((int(j) for j in order if int(j) not in self.shown), int(order[0]))
        self.shown.append(pick)
        return self.paragraphs[pick]


def final_text(completion) -> str:
    """The trunk's last message in a completion, which with tool calls is a list of turns."""
    if isinstance(completion, str):
        return completion
    texts = [m.get("content") for m in completion if m.get("role") == "assistant" and m.get("content")]
    return texts[-1] if texts else ""


def correct(completions, answer, **kwargs) -> list[float]:
    """1.0 where the final answer is the key's, by HotpotQA's exact match."""
    return [1.0 if exact(answer_of(final_text(c)), a) else 0.0 for c, a in zip(completions, answer)]


def overlap(completions, answer, **kwargs) -> list[float]:
    """The token F1 of the final answer, logged beside the exact match (weight 0 by default)."""
    return [f1(answer_of(final_text(c)), a) for c, a in zip(completions, answer)]


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--out", default="state/ask/round1")
    p.add_argument("--model", default="Qwen/Qwen3.5-2B")
    p.add_argument("--n", type=int, default=1000, help="training questions")
    p.add_argument("--skip", type=int, default=0)
    p.add_argument("--generations", type=int, default=4)
    p.add_argument("--prompts-per-step", type=int, default=2)
    p.add_argument("--micro", type=int, default=2)
    p.add_argument("--max-completion", type=int, default=512, help="tokens across the trunk's turns")
    p.add_argument("--lr", type=float, default=2e-5)
    p.add_argument("--max-steps", type=int, default=-1)
    p.add_argument("--save-steps", type=int, default=20)
    p.add_argument("--rank", type=int, default=16)
    p.add_argument("--init-adapter", default=None)
    p.add_argument("--f1-weight", type=float, default=0.0)
    p.add_argument("--seed", type=int, default=0)
    args = p.parse_args()

    import torch
    from datasets import Dataset
    from peft import LoraConfig
    from transformers import AutoModelForCausalLM, AutoTokenizer
    from trl import GRPOConfig, GRPOTrainer

    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    qs = questions(args.n, split="train", skip=args.skip)
    data = Dataset.from_list([{
        "prompt": [{"role": "system", "content": SYSTEM}, {"role": "user", "content": q["question"]}],
        "answer": q["answer"],
        "paragraphs": [{"text": paragraph_text(x), "supporting": x["supporting"]} for x in q["paragraphs"]],
    } for q in qs])
    micro = args.micro or args.generations
    per_step = args.generations * args.prompts_per_step
    if per_step % micro:
        raise SystemExit(f"--micro {micro} must divide the {per_step} rollouts of a step")
    config = GRPOConfig(
        output_dir=str(out), seed=args.seed, learning_rate=args.lr, num_train_epochs=1.0,
        max_steps=args.max_steps, per_device_train_batch_size=micro,
        gradient_accumulation_steps=per_step // micro, num_generations=args.generations,
        max_completion_length=args.max_completion, temperature=1.0, beta=0.0,
        gradient_checkpointing=True, gradient_checkpointing_kwargs={"use_reentrant": False},
        logging_steps=1, save_steps=args.save_steps, save_total_limit=2, report_to="none",
        reward_weights=[1.0, args.f1_weight],
        chat_template_kwargs={"enable_thinking": False}, mask_truncated_completions=True,
        log_completions=False, bf16=False, fp16=False, torch_empty_cache_steps=1,
    )
    embedder()  # loaded once here: a first import of transformers inside several threads at once fails
    model = AutoModelForCausalLM.from_pretrained(args.model, dtype=torch.float32)
    tok = AutoTokenizer.from_pretrained(args.model)
    peft = LoraConfig(r=args.rank, lora_alpha=2 * args.rank, lora_dropout=0.0, target_modules=TARGETS,
                      task_type="CAUSAL_LM")
    if args.init_adapter:
        from peft import PeftModel

        model = PeftModel.from_pretrained(model, args.init_adapter, is_trainable=True)
        peft = None
    trainer = GRPOTrainer(model=model, processing_class=tok, reward_funcs=[correct, overlap], args=config,
                          train_dataset=data, peft_config=peft, environment_factory=Store)
    last = sorted(out.glob("checkpoint-*"), key=lambda d: int(d.name.split("-")[1]))
    started = time.perf_counter()
    trainer.train(resume_from_checkpoint=str(last[-1]) if last else None)
    trainer.save_model(str(out / "adapter"))
    log = [dict(e) for e in trainer.state.log_history]
    (out / "ask.json").write_text(json.dumps({
        "model": args.model, "n": len(qs), "skip": args.skip, "generations": args.generations,
        "prompts_per_step": args.prompts_per_step, "micro": micro, "max_completion": args.max_completion,
        "lr": args.lr, "max_steps": args.max_steps, "rank": args.rank, "init_adapter": args.init_adapter,
        "system": SYSTEM, "tool": "search(query): the paragraph of the question's ten nearest the query by MiniLM, not one shown before",
        "rewards": ["correct: 1.0 where the final answer is the key's by exact match",
                    f"overlap: token F1, weight {args.f1_weight}"],
        "log": log, "seconds": round(time.perf_counter() - started, 1),
        "peak_gib": round(torch.cuda.max_memory_allocated() / 2**30, 2), "sha": sha(), "command": " ".join(sys.argv),
    }, indent=1), encoding="utf-8")
    print("wrote", out / "ask.json", f"peak {torch.cuda.max_memory_allocated() / 2**30:.2f} GiB")


if __name__ == "__main__":
    main()
