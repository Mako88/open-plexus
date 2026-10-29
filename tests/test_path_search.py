"""The search over moves is checked without a model: a stub trunk marks whether a candidate
move is the gold next move, a stub evaluator reads that mark, and greedy and beam search must
then recover every path in twelve passes (two steps of four, and four to see that it is done)."""

import re
import sys
from pathlib import Path
from types import SimpleNamespace

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
sys.path.insert(0, str(ROOT / "scripts"))

from openplexus.babi import stories  # noqa: E402
from openplexus.paths import LETTER_OF, moves_in, step_prompt  # noqa: E402
from path_reading import search_arm  # noqa: E402


def head_of(prompt: str) -> str:
    """The facts and the question: one story. Questions repeat across stories."""
    return prompt.split("\nMoves so far")[0]


class StubTrunk:
    """Its 'hidden state' is one number: 1 if the candidate is the gold next move, else -1."""

    def __init__(self, gold_of: dict[str, list[str]]):
        self.gold_of = gold_of

    def hidden(self, prompts, layers):
        out = []
        for p in prompts:
            so_far = re.search(r"Moves so far: (.*)\n", p).group(1)
            nxt = LETTER_OF[re.search(r"Next move: (\w+)", p).group(1)]
            done = [] if so_far == "none yet" else [LETTER_OF[w.strip()] for w in so_far.split(",")]
            gold = self.gold_of[head_of(p)]
            right = done == gold[:len(done)] and len(done) < len(gold) and gold[len(done)] == nxt
            out.append([1.0 if right else -1.0])
        return {layer: np.array(out, np.float32) for layer in layers}


class StubEvaluator:
    """Its score is the stub trunk's mark times `sureness`: at 3 the right move is near
    certain; at 0.5 it is only favoured, as a weak real evaluator favours it."""
    layer, key, threshold = 16, "label", 0.0

    def __init__(self, sureness: float = 3.0):
        self.sureness = sureness

    def query(self, h):
        return np.asarray(h, np.float32).reshape(-1, 1) * self.sureness


def test_greedy_and_beam_recover_every_path_in_twelve_passes():
    ss = stories([19])[:6]
    gold_of = {head_of(step_prompt(s.sentences, s.questions[0].text, [], "n")):
               moves_in(s.questions[0].answer) for s in ss}
    assert len(gold_of) == len(ss)
    args = SimpleNamespace(max_steps=3, evaluator="stub")
    for mode, beam in (("greedy", 1), ("beam", 2)):
        arm = search_arm(mode, beam, ss, StubTrunk(gold_of), StubEvaluator(), args)
        assert arm["score"] == 1.0, (mode, [r for r in arm["rows"] if not r["correct"]])
        assert arm["moves_per_question"] == 2.0
        assert arm["passes_per_question"] == (12.0 if mode == "greedy" else 20.0), (mode, arm["passes_per_question"])


def test_an_unsure_evaluator_does_not_make_the_beam_stop_at_once():
    """Judged by total log-probability a path that stopped at once beats any longer path when
    the evaluator only favours the right move; by the mean a decision the right path wins."""
    ss = stories([19])[:6]
    gold_of = {head_of(step_prompt(s.sentences, s.questions[0].text, [], "n")):
               moves_in(s.questions[0].answer) for s in ss}
    arm = search_arm("beam", 2, ss, StubTrunk(gold_of), StubEvaluator(0.5), SimpleNamespace(max_steps=3, evaluator="stub"))
    assert arm["moves_per_question"] == 2.0 and arm["score"] == 1.0
