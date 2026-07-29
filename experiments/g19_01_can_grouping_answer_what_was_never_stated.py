"""Can grouping answer a question about something never stated?

[Note 048](../docs/notes/048-a-task-where-concepts-can-mean-something.md)'s
experiment, on the task g19-00 calibrated. **The first measurement in this
project of whether `concepts.py`'s indirection does what it was built for.**

## The two questions, and only the second is new

    DIRECT     the entity's own fact was stated in this sequence.
               MQAR with dressing; the store already scores 0.995 on that.
    TRANSFER   it was NOT -- but siblings of its family had theirs stated.

An entity treated as an arbitrary symbol has had nothing said about it and can
only be guessed at. An entity **grouped with its family** shares the store's
address with its siblings, so the sibling's write is what a read at this entity
returns. That is the whole mechanism, and it is why `ungrouped` is at chance on
TRANSFER for a structural reason rather than a tuning one.

## The arms

    ungrouped    the identity mapping. Today's model
    concept      groups from `ContentIndex` + `grouping.cluster`
    permuted     the same group SIZES, membership shuffled. Groups that exist
                 and mean nothing
    nostore      nothing written at all

**`permuted` is the control that matters**, because "fewer addresses, however
chosen" is exactly what turned out to explain the only positive result the text
line produced (decision 141). If it matches `concept`, the gain is address
collapse rather than similarity, and this task would have caught the same
illusion in a place where it can be told apart.

## PREDICTIONS (registered in note 048 before the task was built)

  P1  THE GATE. `concept` beats `ungrouped` on TRANSFER by more than 0.20.
  P2  THE CONTROL. `permuted` does not beat `ungrouped` on TRANSFER by more
      than 0.05.
  P3  THE RAIL. `nostore` is at chance on BOTH kinds. Chance is `1/n_values`,
      and a smarter guesser confined to values stated in this sequence would
      score higher -- so the empirical rate for a model that answers with a
      *stated* value is printed beside it rather than assumed.

**What would refute the line:** P1 failing while DIRECT still scores high. The
store would work, the grouping would be perfect (g19-00: purity 1.000), and
joining them would still buy nothing — which would say the indirection does not
do what it was built for, for the cost of one task.

## THE EXCEPTION ARM — `--exceptions 1`, added AFTER decision 143

**The falsifier for the mechanism that answered TRANSFER**, run separately so
143's numbers are not changed by it: `--exceptions 0` is the default and
reproduces that result exactly.

Grouping works by giving a family **one store address**. An entity whose own
stated fact *contradicts* its family's collides with its siblings at that
address, and the superposition that carries transfer may make the exception
unrepresentable.

> A system that cannot hold *"birds fly, but not this one"* does not understand
> birds.

**PREDICTIONS, registered before the arm was run:**

  E1  THE GATE, and I expect to lose it. `concept` scores **worse** than
      `ungrouped` on EXCEPTION by more than 0.20. Grouping would be buying
      transfer by spending specificity, and the price would be measured rather
      than argued.

  E2  THE RAIL. `ungrouped` reaches at least 0.50 on EXCEPTION. The fact was
      stated about that very entity, so for a model with no grouping this is
      ordinary recall. If it fails, the arm is broken rather than the mechanism.

  E3  THE DIAGNOSTIC. When `concept` is wrong on EXCEPTION, its answer is **a
      sibling's value** more often than chance. A wrong answer that is
      specifically the family's is the superposition speaking; generic failure
      is not.

## THE `indexed` ARM — option B, and it needed no model change

Grouping answers TRANSFER by making a family share one address, and decisions
144/145 measured the price: the shared address is a majority vote, so a
contradicting fact about one member is **erased**.

**Option B never shares an address.** Every fact is written at the entity's own
key, so nothing is ever overwritten. Transfer instead reads the *neighbours'*
addresses, found through the content index — which is note 045's design, stated
in July as *"similarity picks WHICH exact reads to make, and never what an
address looks like"*.

**`index_branches` already implements it.** The token's own read stays at full
weight and the neighbours are added to it, weighted by a softmax over their
content similarity. So this arm is a configuration, not a mechanism to build.

The cost was measured before the arm was written rather than estimated: on this
task a true sibling is the **nearest** neighbour 100% of the time, and all three
siblings are inside the top 3. So option B is one extra read to reach a sibling,
against option A's two-address read and two-address write.

**PREDICTIONS, registered before the arm was run:**

  I1  THE GATE. `indexed` beats `ungrouped` on TRANSFER by more than 0.20. The
      neighbours supply what was never stated about this entity.

  I2  THE RAIL, and the whole reason to prefer B. `indexed` stays within 0.05 of
      `ungrouped` on EXCEPTION. The entity's own read is at full weight and the
      neighbours are down-weighted, so the specific fact should still win —
      where `concept` scores 0.371 because it has no specific fact left.

  I3  THE FALSIFIER FOR NOTE 049. If `indexed` reaches `concept`'s TRANSFER and
      holds `ungrouped`'s EXCEPTION, then option B dominates and the two-level
      reader in note 049 is unnecessary machinery. If it splits the difference
      on both, it is averaging rather than choosing and 049's threshold is back
      on the table.

## Settings, and why they are not inherited

Single keys (`context_keys` off), because the binding is `entity -> value` and a
pair key would address the fact as `(FACT, entity)` and the query as
`(QUERY, entity)` — two different addresses for one fact, and the retrieval could
not work at all. That is the same reasoning MQAR's line has always used, checked
rather than copied.

`k = 16` from g19-00: the grouping must cover the token CLASSES, not just the
families. At k=8 purity is 0.625 because entities and attributes cannot both fit.
"""

from __future__ import annotations

import argparse
import json
import sys
import time
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from openplexus.concepts import OneConceptPerToken, Shared  # noqa: E402
from openplexus.content import ContentIndex  # noqa: E402
from openplexus.grouping import cluster  # noqa: E402
from openplexus.keys import ByConcept  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.families import (  # noqa: E402
    FACT, FamilyConfig, background, dataset)

WIDTH = 64
CONTENT_WIDTH = 128
#: From g19-00. Must exceed the number of token CLASSES or families merge for
#: want of a cluster slot -- purity 0.625 at k=8 against 1.000 here.
GROUPS = 16
#: g19-00 again: the default window of 4 spills an entity's neighbours into the
#: next mention, and weighting down-weights the signal itself.
INDEX_WINDOW = 1
INDEX_POWER = 0.0
BACKGROUND = 200
TRAIN = 400
TEST = 200
EPOCHS = 6
SEEDS = (0, 1, 2)
ARMS = ("ungrouped", "concept", "permuted", "nostore", "indexed")
#: How many neighbours the `indexed` arm reads. 3 covers every sibling on
#: this task -- measured, not assumed: all three are inside the top 3 at
#: 100% across seeds.
BRANCHES = 3


def surfaces_for(arm: str, config: FamilyConfig, seed: int):
    """The grouping this arm addresses the store by, and the index behind it."""
    if arm in ("ungrouped", "nostore"):
        return OneConceptPerToken(config.vocab_size), None, float("nan")
    if arm == "indexed":
        # OPTION B: the identity mapping, so every fact keeps its own address
        # and nothing is ever overwritten -- but WITH a fitted index, because
        # the neighbours are read at query time instead.
        index = ContentIndex(config.vocab_size, width=CONTENT_WIDTH, seed=seed,
                             power=INDEX_POWER, window=INDEX_WINDOW)
        for stream in background(config, BACKGROUND):
            index.observe(stream)
        return OneConceptPerToken(config.vocab_size), index, float("nan")

    index = ContentIndex(config.vocab_size, width=CONTENT_WIDTH, seed=seed,
                         power=INDEX_POWER, window=INDEX_WINDOW)
    for stream in background(config, BACKGROUND):
        index.observe(stream)
    groups = cluster(index.vectors, GROUPS, seed=seed)

    # HOW MUCH OF THE TRUE STRUCTURE CAME BACK, carried into every record. A
    # TRANSFER result read without it could be grouping working poorly or the
    # clusterer having failed, and those are different findings.
    entities = {config.entity_base + i for i in range(config.n_entities)}
    total = right = 0
    for group in groups:
        members = [t for t in group if t in entities]
        if members:
            counts: dict[int, int] = {}
            for token in members:
                family = config.family_of(token)
                counts[family] = counts.get(family, 0) + 1
            right += max(counts.values())
            total += len(members)
    recovered = right / total if total else float("nan")

    if arm == "permuted":
        # THE SAME SIZES, THE WRONG MEMBERS. Decision 141: on text the entire
        # gain came from having fewer addresses, however chosen, and only a
        # size-matched control could tell that apart.
        order = np.random.default_rng((seed, 4241)).permutation(
            config.vocab_size)
        cut, rebuilt = 0, []
        for group in groups:
            rebuilt.append([int(t) for t in order[cut:cut + len(group)]])
            cut += len(group)
        groups = rebuilt

    return Shared(config.vocab_size, groups), index, recovered


def silent(tokens: np.ndarray) -> np.ndarray:
    return np.zeros(len(tokens), dtype=bool)


def one_cell(arm: str, seed: int, exceptions: int = 0) -> dict:
    started = time.time()
    config = FamilyConfig(seed=seed, exceptions_per_family=exceptions)
    surfaces, index, recovered = surfaces_for(arm, config, seed)
    writes = arm != "nostore"

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=config.vocab_size, d_model=WIDTH, lr=0.05,
        key_scale=0.5, decay=0.99, seed=seed,
        index_branches=BRANCHES if arm == "indexed" else 0))
    if arm not in ("ungrouped", "nostore", "indexed"):
        model.key_source = ByConcept(model.key_source, surfaces,
                                     config.vocab_size)
        model.surfaces = surfaces
    model.content = index

    train = dataset(config, TRAIN)
    # `replace`, not a fresh config: built from scratch it silently dropped
    # `exceptions_per_family` and the EXCEPTION arm scored zero queries.
    # The None in the output said so rather than dividing by zero, which is
    # the only reason it was caught in one run.
    test = dataset(replace(config, seed=seed + 5000), TEST)

    rng = np.random.default_rng(seed)
    order = np.arange(len(train))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for position in order:
            sequence = train[int(position)]
            tokens = np.asarray(sequence.tokens)
            targets = np.roll(tokens, -1)
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, targets, scored, learn=True,
                      store=None if writes else silent(tokens))

    tallies = {"direct": [0, 0], "transfer": [0, 0], "exception": [0, 0]}
    stated_hits = 0
    # E3: when an EXCEPTION is answered wrongly, how often is the answer
    # specifically a SIBLING's value? A collision speaks with the family's
    # voice; generic failure does not. Read off the sequence rather than the
    # generator, because nothing here may consult the answer key.
    said_a_sibling = wrong_exceptions = 0
    for sequence in test:
        tokens = np.asarray(sequence.tokens)
        predictions = model.run(tokens,
                                store=None if writes else silent(tokens))
        values = {int(t) for t in tokens if t >= config.value_base}
        for where, transfer, exception in zip(sequence.query_positions,
                                              sequence.is_transfer,
                                              sequence.is_exception):
            kind = ("exception" if exception
                    else "transfer" if transfer else "direct")
            right = int(predictions[where] == tokens[where + 1])
            tallies[kind][0] += right
            tallies[kind][1] += 1
            stated_hits += int(int(predictions[where]) in values)
            if kind == "exception" and not right:
                wrong_exceptions += 1
                asked_entity = int(tokens[where])
                family = config.family_of(asked_entity)
                siblings = [int(tokens[i + 2])
                            for i in range(len(tokens) - 2)
                            if tokens[i] == FACT
                            and int(tokens[i + 1]) != asked_entity
                            and config.family_of(int(tokens[i + 1])) == family]
                said_a_sibling += int(int(predictions[where]) in siblings)

    # ALL THREE KINDS. Omitting exceptions made `answers_a_stated_value`
    # exceed 1.0, which is the sort of impossible number that is only
    # obvious when it happens to cross a round threshold.
    asked = sum(count for _, count in tallies.values())
    return dict(
        arm=arm, seed=seed,
        direct=round(tallies["direct"][0] / max(tallies["direct"][1], 1), 4),
        transfer=round(tallies["transfer"][0]
                       / max(tallies["transfer"][1], 1), 4),
        exception=(round(tallies["exception"][0] / tallies["exception"][1],
                         4) if tallies["exception"][1] else None),
        wrong_exception_said_sibling=(
            round(said_a_sibling / wrong_exceptions, 4)
            if wrong_exceptions else None),
        exceptions_per_family=exceptions,
        chance=round(config.trivial, 4),
        # HOW OFTEN IT NAMES A VALUE STATED IN THIS SEQUENCE. A model that has
        # learned the task's shape without solving it scores above `chance`,
        # so this is the floor a TRANSFER number should be read against.
        answers_a_stated_value=round(stated_hits / max(asked, 1), 4),
        family_recovery=(None if recovered != recovered else round(recovered, 4)),
        groups=GROUPS, width=WIDTH, epochs=EPOCHS, train=TRAIN,
        vocab=config.vocab_size, n_families=config.n_families,
        family_size=config.family_size, n_values=config.n_values,
        scored=asked,
        seconds=round(time.time() - started, 1),
        condition=f"{arm}|k{GROUPS}|d{WIDTH}|seed{seed}"
                  f"|fam{config.n_families}x{config.family_size}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--arm", choices=ARMS, default=None)
    parser.add_argument("--exceptions", type=int, default=0,
                        help="members per family whose stated fact "
                             "CONTRADICTS their family. 0 reproduces "
                             "decision 143")
    parser.add_argument("--json", type=str, default=None)
    args = parser.parse_args()

    harness.refuse_if_mutating()
    seeds = (args.seed,) if args.seed is not None else SEEDS
    arms = (args.arm,) if args.arm else ARMS

    records = []
    for seed in seeds:
        for arm in arms:
            record = one_cell(arm, seed, args.exceptions)
            print(f"  {record['condition']:34s} "
                  f"direct {record['direct']:.4f}  "
                  f"transfer {record['transfer']:.4f}  "
                  f"exception {record['exception']}  "
                  f"stated {record['answers_a_stated_value']:.4f}",
                  file=sys.stderr, flush=True)
            records.append(record)

    if args.json:
        path = Path(args.json)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(records, indent=1), encoding="utf-8")
        print(f"wrote {len(records)} records to {path}")
    else:
        for record in records:
            print(f"{record['condition']}  direct {record['direct']}  "
                  f"transfer {record['transfer']}")
        print(f"chance {records[0]['chance']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
