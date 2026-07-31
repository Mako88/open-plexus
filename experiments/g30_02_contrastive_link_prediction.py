"""The LOCAL contrastive rule on FB15k-237's own task, against the same opponent.

**What this does not duplicate.** Two imports carry everything that already exists:

    tools/link_prediction.py        the data, the filter, the ranking, the metrics and
                                    the `frequency` opponent -- shared with `g30-01`, so
                                    the two arms cannot drift onto different protocols
    tools/relation_contrastive.py   `learn_pairs`, the rule itself. It gained a second
                                    table and sampled negatives as PARAMETERS rather than
                                    a fork, so CLUTRR and the graph results are produced
                                    by the identical update and still reproduce

What is new here is only the wiring: entities and relations as two alphabets, and a
scorer that reads the learned tables.

## What is being compared, stated so it is not mistaken for something else

Scoring a triple by `<h * r, t>` is an ordinary knowledge-graph scoring form and predates
this project. **The scoring form is not the variable; the UPDATE is.** The question is
whether a per-example rule with no barrier and no global gradient reaches a number an
outsider would recognise, against a baseline that needs no learning at all.

No published number is quoted. Rule 1 forbids letting a recalled figure gate a claim, and
setting this beside the literature is a separate job that starts with reading.

Predictions are in `experiments/sweeps/g30-02-the-local-rule-on-their-task.txt`,
committed at `92b8a0f` before this file existed.
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402
from tools import link_prediction as lp  # noqa: E402
from tools.relation_contrastive import learn_pairs  # noqa: E402


def scorer(entities: np.ndarray, relations: np.ndarray):
    """`f(heads, rels) -> (n, n_entities)`, the same interface the raw store presents."""
    return lambda heads, rels: (entities[heads] * relations[rels]) @ entities.T


def main() -> None:
    args = harness.parse_args(__doc__)
    # The harness owns the flag NAMES so `check_workflows.py` can validate a workflow
    # line against `--help` before a matrix is dispatched. The DEFAULTS are the
    # script's, which is the convention every experiment here follows.
    width = args.width or 128
    negatives = args.negatives or 64
    epochs = args.epochs if args.epochs is not None else 2
    lr = args.lr or 0.05
    temperature = args.temperature or 0.1
    seed = args.seed or 0

    task = lp.Task()
    heads, rels, tails = task.train_indices()
    items = list(zip(heads.tolist(), rels.tolist(), tails.tolist()))

    entities, relations = learn_pairs(
        items, width, seed, 0 if args.untrained else epochs, lr, temperature,
        n_left=task.n_entities, n_right=len(task.rel), negatives=negatives)

    arm = ("untrained" if args.untrained
           else f"contrastive w{width} K{negatives} e{epochs}")
    ranks = task.evaluate(scorer(entities, relations))
    print(f"\ntest {len(task.heads):,} scored, {task.unanswerable} unanswerable; "
          f"{task.n_entities:,} candidates, {len(task.train):,} train triples")
    print("\n" + lp.header())
    print(lp.row(arm, ranks["filtered"]))
    print(lp.row("  same, UNFILTERED", ranks["unfiltered"]))
    print(lp.row("frequency", task.evaluate(task.frequency_scorer())["filtered"]))
    print(f"\n  chance MRR {1 / task.n_entities:.6f}")
    print("  g30-01's raw store on this protocol: 0.0122 at width 256, "
          "0.0232 at width 512")


if __name__ == "__main__":
    main()
