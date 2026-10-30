"""The trunk as a served model: llama-server behind an OpenAI-style endpoint, which is several
times faster on this card than fp32 PyTorch for anything that only needs text out. Launch it
with `--jinja --reasoning-budget 0`; the client also sends `enable_thinking` so a reading can
show in its own record that thinking was off.
"""

from __future__ import annotations

import json
import os
import time
import urllib.request
from concurrent.futures import ThreadPoolExecutor

SYSTEM = (
    "You are given a short story and a question about it. Answer using only the story. "
    "Reply with the answer word only, nothing else."
)


class ServedTrunk:
    def __init__(self, model: str = "trunk", url: str | None = None, max_new: int = 8,
                 parallel: int = 8, think: bool = False):
        self.model = model
        self.url = (url or os.environ.get("LOCAL_SERVER", "http://127.0.0.1:8093")) \
            + "/v1/chat/completions"
        self.max_new = max_new
        self.parallel = parallel
        self.think = think
        self.tokens_in = 0
        self.tokens_out = 0
        self.thoughts: dict[int, str] = {}  # id(messages) -> the reasoning the server returned

    def served_model(self) -> str:
        """What the server says it is serving, for the reading's record."""
        with urllib.request.urlopen(self.url.rsplit("/", 2)[0] + "/models", timeout=10) as r:
            return json.load(r)["data"][0]["id"]

    def ask(self, messages: list[dict]) -> str:
        body = json.dumps({
            "model": self.model, "messages": messages, "max_tokens": self.max_new,
            "temperature": 0, "seed": 0,
            "chat_template_kwargs": {"enable_thinking": self.think},
        }).encode()
        req = urllib.request.Request(self.url, body, {"Content-Type": "application/json"})
        for attempt in range(5):
            try:
                with urllib.request.urlopen(req, timeout=1800) as resp:
                    reply = json.loads(resp.read())
                break
            except Exception:  # noqa: BLE001
                if attempt == 4:
                    raise
                time.sleep(2 ** attempt)
        u = reply.get("usage", {})
        self.tokens_in += int(u.get("prompt_tokens", 0))
        self.tokens_out += int(u.get("completion_tokens", 0))
        msg = reply["choices"][0]["message"]
        if self.think:
            self.thoughts[id(messages)] = (msg.get("reasoning_content") or msg.get("reasoning") or "").strip()
        return (msg.get("content") or "").strip()

    def ask_many(self, prompts: list[list[dict]]) -> list[str]:
        with ThreadPoolExecutor(max_workers=self.parallel) as pool:
            return list(pool.map(self.ask, prompts))

    def thought_of(self, messages: list[dict]) -> str | None:
        """The reasoning the server returned for a thinking turn already asked, for the
        reading's rows: a stop over the thinking stream is read first on recorded thoughts."""
        return self.thoughts.get(id(messages))

    def propose(self, prompts: list[list[dict]], k: int, temperature: float, max_new: int,
                prefix: str = "", seed: int = 0, model_id: str = "Qwen/Qwen3.5-2B") -> list[list[str]]:
        """As `LocalTrunk.propose`, on the server: k one-line replies to each turn, sampled at
        `temperature` with seeds `seed`, `seed + 1`, ... (greedy at 0), the reply started with
        `prefix`. The chat template is rendered here with the model's tokenizer and sent to the
        server's raw completion endpoint, since a chat request cannot start the reply; thinking
        is off in the rendered template."""
        if not hasattr(self, "_tok"):
            from transformers import AutoTokenizer

            self._tok = AutoTokenizer.from_pretrained(model_id)
        base = self.url.rsplit("/", 3)[0] + "/completion"  # at the server root, not under /v1

        def one(job):
            text, j = job
            body = json.dumps({"prompt": text, "n_predict": max_new, "temperature": temperature,
                               "seed": seed + j, "stop": ["\n"], "cache_prompt": True}).encode()
            req = urllib.request.Request(base, body, {"Content-Type": "application/json"})
            for attempt in range(5):
                try:
                    with urllib.request.urlopen(req, timeout=600) as resp:
                        reply = json.loads(resp.read())
                    break
                except Exception:  # noqa: BLE001
                    if attempt == 4:
                        raise
                    time.sleep(2 ** attempt)
            self.tokens_in += int(reply.get("tokens_evaluated", 0))
            self.tokens_out += int(reply.get("tokens_predicted", 0))
            return prefix + reply.get("content", "").split("\n")[0].rstrip()

        jobs = []
        for m in prompts:
            text = self._tok.apply_chat_template(m, tokenize=False, add_generation_prompt=True,
                                                 enable_thinking=False) + prefix
            jobs.extend((text, len(jobs) + j) for j in range(k))  # a seed a request, so repeats of one prompt differ
        with ThreadPoolExecutor(max_workers=self.parallel) as pool:
            replies = list(pool.map(one, jobs))
        return [replies[i * k:(i + 1) * k] for i in range(len(prompts))]


def question_messages(rows: list[str], question: str) -> list[dict]:
    """The same turn as step 0's ceiling, so a store arm and the ceiling differ only in which
    sentences stand in the story."""
    story = "\n".join(rows) if rows else "(no story given)"
    return [{"role": "system", "content": SYSTEM},
            {"role": "user", "content": f"Story:\n{story}\n\nQuestion: {question}"}]


class LocalTrunk:
    """The trunk in PyTorch, for a reading that needs its hidden states as well as its answers.
    Several times slower than the served trunk on this card; use it only when a reading needs
    what the server cannot give."""

    def __init__(self, model_id: str = "Qwen/Qwen3.5-2B", batch: int = 8, max_new: int = 8,
                 dtype: str = "float32"):
        import torch
        from transformers import AutoModelForCausalLM, AutoTokenizer

        self.torch = torch
        self.model_id = model_id
        self.tok = AutoTokenizer.from_pretrained(model_id)
        self.tok.padding_side = "left"
        if self.tok.pad_token is None:
            self.tok.pad_token = self.tok.eos_token
        # float32 on this card: a GTX 1080 Ti has no bfloat16 and slow float16, so a half-precision
        # trunk sits beside the server but runs several times slower; the option is for a newer card
        self.dtype = dtype
        self.model = AutoModelForCausalLM.from_pretrained(model_id, dtype=getattr(torch, dtype))
        self.model = self.model.to("cuda").eval()
        self.batch = batch
        self.max_new = max_new
        self.tokens_in = 0
        self.tokens_out = 0

    def served_model(self) -> str:
        return f"{self.model_id} {self.dtype} in process"

    def ask_many(self, prompts: list[list[dict]]) -> list[str]:
        out: list[str] = []
        for i in range(0, len(prompts), self.batch):
            texts = [self.tok.apply_chat_template(m, tokenize=False, add_generation_prompt=True,
                                                  enable_thinking=False)
                     for m in prompts[i:i + self.batch]]
            enc = self.tok(texts, return_tensors="pt", padding=True).to("cuda")
            with self.torch.no_grad():
                gen = self.model.generate(**enc, max_new_tokens=self.max_new, do_sample=False,
                                          temperature=None, top_p=None, top_k=None,
                                          pad_token_id=self.tok.pad_token_id)
            new = gen[:, enc["input_ids"].shape[1]:]
            self.tokens_in += int(enc["attention_mask"].sum())
            self.tokens_out += int((new != self.tok.pad_token_id).sum())
            out.extend(self.tok.decode(ids, skip_special_tokens=True).strip() for ids in new)
        return out

    def propose(self, prompts: list[list[dict]], k: int, temperature: float, max_new: int,
                prefix: str = "") -> list[list[str]]:
        """k replies to each turn, sampled at `temperature` (greedy when it is 0 or k is 1),
        for a search that wants several candidate next steps; one line each, the first line
        kept. `prefix` starts the reply for the trunk ("Answer:" makes it finish), and is part
        of what comes back."""
        out: list[list[str]] = []
        sample = temperature > 0 and k > 1
        for i in range(0, len(prompts), self.batch):
            texts = [self.tok.apply_chat_template(m, tokenize=False, add_generation_prompt=True,
                                                  enable_thinking=False) + prefix
                     for m in prompts[i:i + self.batch]]
            enc = self.tok(texts, return_tensors="pt", padding=True).to("cuda")
            with self.torch.no_grad():
                gen = self.model.generate(
                    **enc, max_new_tokens=max_new, num_return_sequences=k if sample else 1,
                    do_sample=sample, temperature=temperature if sample else None, top_p=None, top_k=None,
                    pad_token_id=self.tok.pad_token_id)
            new = gen[:, enc["input_ids"].shape[1]:]
            self.tokens_in += int(enc["attention_mask"].sum())
            self.tokens_out += int((new != self.tok.pad_token_id).sum())
            replies = [prefix + self.tok.decode(ids, skip_special_tokens=True).split("\n")[0].rstrip()
                       for ids in new]
            per = k if sample else 1
            out.extend(replies[j * per:(j + 1) * per] for j in range(len(texts)))
        return out

    def hidden(self, texts: list[str], layers: list[int]):
        """The last token's hidden state at each of `layers` (0 is the embeddings), one row per
        text, as float32 numpy arrays keyed by layer."""
        import numpy as np

        out = {layer: [] for layer in layers}
        for i in range(0, len(texts), self.batch):
            enc = self.tok(texts[i:i + self.batch], return_tensors="pt", padding=True).to("cuda")
            with self.torch.no_grad():
                # logits only for the last position: over every position the vocabulary's logits
                # are gigabytes a batch, and the first PRM800K feature run died of them
                hs = self.model(**enc, output_hidden_states=True, logits_to_keep=1).hidden_states
            for layer in layers:
                out[layer].append(hs[layer][:, -1, :].float().cpu().numpy())  # left padding
        return {layer: np.concatenate(v) for layer, v in out.items()}

    def close(self) -> None:
        del self.model
        self.torch.cuda.empty_cache()


def states_resumable(trunk: LocalTrunk, prompts: list[str], layers: list[int], part, chunk: int = 256) -> dict:
    """`trunk.hidden` over many prompts with the rows appended to `<part>.l<layer>.f32` as each
    chunk finishes, so a run killed for the card resumes at the rows done. Returns the states
    by layer, one row a prompt."""
    import numpy as np

    dim = int(trunk.model.config.get_text_config().hidden_size)
    done = min([part.with_suffix(f".l{layer}.f32").stat().st_size // 4 // dim
                for layer in layers if part.with_suffix(f".l{layer}.f32").exists()] or [0])
    for at in range(done, len(prompts), chunk):
        h = trunk.hidden(prompts[at:at + chunk], layers)
        for layer in layers:
            with open(part.with_suffix(f".l{layer}.f32"), "ab") as f:
                f.write(h[layer].astype(np.float32).tobytes())
        print(f"  {part.name} {min(at + chunk, len(prompts))}/{len(prompts)}", flush=True)
    return {layer: np.fromfile(part.with_suffix(f".l{layer}.f32"), dtype=np.float32).reshape(len(prompts), -1)
            for layer in layers}
