"""Can a predictive objective bootstrap on MQAR? — and is the task learnable?

Trains the shifted-value attention model on autoregressive MQAR two ways:

    objective=answers   loss on query positions only. Supervised, a C1 violation,
                        included as the ceiling and as the connection control for
                        the whole experiment. If this fails, nothing else counts.
    objective=all       loss on every position. Pure next-token prediction with
                        no indication of which positions matter — the
                        self-supervised objective docs/notes/002 recommends.

    python experiments/g1_02_train.py

Scored on held-out sequences at query positions, against
`MqarConfig.trivial_floor` — not the base rate, which g0-01 established is the
flattering wrong bar.
"""

from __future__ import annotations

import sys
import time
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from openplexus.models.attention import Adam, AttentionConfig, ShiftedAttention  # noqa: E402
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

TASK = MqarConfig(n_pairs=4, seq_len=64, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260725)
STEPS = 4000
N_TRAIN, N_TEST = 600, 200
LR = 3e-3
SEEDS = (1, 2, 3, 4, 5)
D_MODEL = 64


def prepare(sequence):
    """Tokens, next-token targets, and the two scoring masks.

    The answer to a query at position q is emitted at q+1, so scoring the *query*
    positions is scoring next-token prediction of the answers. That equivalence
    is the whole point of the autoregressive layout (docs/notes/001 P2).
    """
    tokens = np.asarray(sequence.tokens)
    targets = np.roll(tokens, -1)
    answers = np.zeros(len(tokens), dtype=bool)
    answers[list(sequence.query_positions)] = True
    every = np.ones(len(tokens), dtype=bool)
    every[0] = False      # attends to nothing
    every[-1] = False     # no next token
    answers[-1] = False
    return tokens, targets, answers, every


def evaluate(model, sequences) -> tuple[float, float]:
    """Accuracy at query positions, and at filler positions.

    The second is a consistency check against g1-01, which measured a frozen
    substrate at 0.029 on random filler and 0.824 on structured.
    """
    q_correct = q_total = f_correct = f_total = 0
    for sequence in sequences:
        tokens, targets, answers, _ = prepare(sequence)
        predicted = model.predict(tokens)
        kinds = sequence.position_kinds()
        for t in range(len(tokens) - 1):
            hit = predicted[t] == targets[t]
            if answers[t]:
                q_correct += hit
                q_total += 1
            elif kinds[t + 1] == "filler":
                f_correct += hit
                f_total += 1
    return q_correct / max(q_total, 1), f_correct / max(f_total, 1)


def train(task: MqarConfig, objective: str, seed: int = 1, verbose: bool = False):
    rng = np.random.default_rng(seed)
    train_set = dataset(task, N_TRAIN)
    test_set = dataset(replace(task, seed=task.seed + 99_991), N_TEST)

    model = ShiftedAttention(AttentionConfig(vocab_size=task.vocab_size,
                                             d_model=D_MODEL, seed=seed))
    optimiser = Adam(model.params, lr=LR)

    losses = []
    for step in range(STEPS):
        sequence = train_set[rng.integers(len(train_set))]
        tokens, targets, answers, every = prepare(sequence)
        scored = answers if objective == "answers" else every
        logits, cache = model.forward(tokens)
        loss, grads = model.loss_and_backward(logits, cache, targets, scored)
        optimiser.step(grads)
        losses.append(loss)
        if verbose and (step + 1) % 1000 == 0:
            print(f"      step {step+1:>5}  loss {np.mean(losses[-500:]):.4f}")

    query_accuracy, filler_accuracy = evaluate(model, test_set)
    return query_accuracy, filler_accuracy, float(np.mean(losses[:200])), \
        float(np.mean(losses[-200:]))


def main() -> int:
    print("Can a predictive objective bootstrap on MQAR?")
    print(f"{STEPS} steps, {N_TRAIN} train / {N_TEST} held out, d_model={D_MODEL}")
    print(f"trivial floor = {TASK.trivial_floor:.3f}  (the bar; NOT the base rate)\n")

    header = (f"{'objective':<11}{'filler':<12}{'query acc':>11}{'floor':>8}"
              f"{'filler acc':>12}   {'across seeds':<13}{'secs':>7}")
    print(header)
    print("-" * len(header))

    conditions = [
        ("answers", "random"),      # ceiling + connection control + G0 step 3
        ("all", "random"),          # the question John asked
        ("all", "structured"),      # the direct test of docs/notes/008 §4
    ]
    results = {}
    for objective, filler in conditions:
        task = replace(TASK, filler=filler)
        started = time.time()
        runs = [train(task, objective, seed=s) for s in SEEDS]
        qs = [r[0] for r in runs]
        fs = [r[1] for r in runs]
        results[(objective, filler)] = qs
        span = f"{min(qs):.3f}-{max(qs):.3f}" if max(qs) - min(qs) > 5e-4 else "all equal"
        print(f"{objective:<11}{filler:<12}{np.mean(qs):>11.3f}{task.trivial_floor:>8.3f}"
              f"{np.mean(fs):>12.3f}   {span:<13}{time.time()-started:>7.0f}")

    print()
    ceiling = float(np.mean(results[("answers", "random")]))
    if ceiling < 0.5:
        print("CONNECTION CONTROL FAILED: the supervised ceiling did not learn.")
        print("The model or the training loop is broken; the other rows mean nothing.")
        return 1
    print(f"Supervised ceiling {ceiling:.3f} -- the task IS learnable from scratch")
    print("on this generator, which is what G0 step 3 needed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
