"""Note 071's measurement: may structured relation vectors enter the ADDRESS?

Note 070 measured that extensional relation profiles roughly double held-out composition,
and the obvious next move was to put them in the keys the store addresses by. **This
measures that move and it is refused.**

    hashed      key(e, r) = hash(seed, e, r)             what keys.py does today
    structured  key(e, r) = hash(e) (*) profile(r)       (*) circular convolution

Two things are scored, and the second is the one that decides it:

    READ        write `facts` bindings, read every one back, decode nearest value.
                Structured keys hold 0.992-1.000 here, so interference does NOT
                destroy the store -- the first-order worry is unfounded

    FALSE HIT   read an address never written for this entity. Does the nearest value
                happen to be one this entity DOES have? Hashed tracks chance and decays
                with load; structured is FLAT at ~0.6, because `hash(e) (*) anything`
                puts all of one entity's addresses in a shared subspace

**Note 067 argued the interference concern does not transfer to relations** — *"twenty
relations in a 512-wide space have room to be structured without meaningful
interference."* The count was the wrong count: the interference is not global across
twenty relations, it is local to one entity's handful of addresses.

## What this does NOT measure

The raw `memory @ key` read only, which `retrieval.SuperposedRead` itself calls *"the one
under suspicion."* Decision 148's 1.0000/0.0000 comes from `AddressSketch` with
`index_prefer="inherit"`, a mechanism apart from this, and **it may recover what the raw
read loses.** Untested. What is established is that the raw read degrades where hashed
keys do not, which is enough to refuse the wiring without further work.
"""

from __future__ import annotations

import argparse
import collections
import itertools
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))
sys.path.insert(0, str(ROOT / "tools"))

import relation_profiles as profiles_tool  # noqa: E402

from openplexus.tasks.clutrr import RELATIONS  # noqa: E402


def convolve(left: np.ndarray, right: np.ndarray) -> np.ndarray:
    return np.real(np.fft.ifft(np.fft.fft(left) * np.fft.fft(right)))


def unit(rng, shape) -> np.ndarray:
    vectors = rng.normal(0.0, 1.0, shape)
    return vectors / np.linalg.norm(vectors, axis=-1, keepdims=True)


def relation_vectors(root: Path, config: str, width: int, seed: int):
    """Extensional profiles projected to `width`, and a hashed control."""
    puzzles = list(itertools.chain(
        profiles_tool.rows(root, config, "train"),
        profiles_tool.rows(root, config, "validation")))
    rules = profiles_tool.base_rules(profiles_tool.rows(root, config, "train"))
    matrix = profiles_tool.profile(puzzles, set(rules), positional=True)
    rng = np.random.default_rng(seed)
    # Johnson-Lindenstrauss: a fixed random projection preserves relative distance, so
    # the geometry note 070 measured survives the change of width.
    projection = rng.normal(0.0, 1.0 / np.sqrt(width), (matrix.shape[1], width))
    structured = matrix @ projection
    structured /= np.linalg.norm(structured, axis=1, keepdims=True)
    return structured, unit(rng, (len(RELATIONS), width))


def trial(entities: int, relations: np.ndarray, structured: bool, width: int,
          seed: int, per_entity: int = 3):
    """Return (read accuracy, false-hit rate) for one store."""
    rng = np.random.default_rng(seed)
    entity_vectors = unit(rng, (entities, width))
    values = unit(rng, (entities, width))
    hashes: dict = {}

    def key(entity: int, relation: int) -> np.ndarray:
        if structured:
            return convolve(entity_vectors[entity], relations[relation])
        if (entity, relation) not in hashes:
            hashes[(entity, relation)] = unit(
                np.random.default_rng((seed, entity, relation, 7)), width)
        return hashes[(entity, relation)]

    # Relations drawn without replacement per entity: an entity holding the same
    # relation twice is decision 103's subject, not this one.
    facts = [(e, int(r), int(rng.integers(entities)))
             for e in range(entities)
             for r in rng.choice(len(RELATIONS), size=per_entity, replace=False)]

    memory = np.zeros((width, width))
    for entity, relation, value in facts:
        memory += np.outer(values[value], key(entity, relation))

    correct = sum(int(np.argmax(values @ (memory @ key(e, r)))) == o
                  for e, r, o in facts)

    written: dict = collections.defaultdict(set)
    holds: dict = collections.defaultdict(set)
    for entity, relation, value in facts:
        written[entity].add(relation)
        holds[entity].add(value)
    false_hits = 0
    for entity in range(entities):
        absent = [r for r in range(len(RELATIONS)) if r not in written[entity]]
        if not absent:
            continue
        got = memory @ key(entity, int(rng.choice(absent)))
        if int(np.argmax(values @ got)) in holds[entity]:
            false_hits += 1
    return correct / len(facts), false_hits / entities


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT / "data" / "clutrr")
    parser.add_argument("--config", default="gen_train23_test2to10")
    parser.add_argument("--width", type=int, default=512)
    parser.add_argument("--seeds", type=int, default=5)
    parser.add_argument("--per-entity", type=int, default=3)
    args = parser.parse_args()

    structured, hashed = relation_vectors(
        args.root, args.config, args.width, seed=0)
    off_diagonal = (structured @ structured.T)[
        ~np.eye(len(RELATIONS), dtype=bool)]
    print(f"width {args.width}, {len(RELATIONS)} relations, "
          f"{args.per_entity} facts per entity, {args.seeds} seeds")
    print(f"structured relation cosines: mean {off_diagonal.mean():.3f}, "
          f"max {off_diagonal.max():.3f} (hashed are ~0 by construction)\n")
    print(f"{'entities':>9} {'facts':>6}  {'read hash':>10} {'read struct':>12}  "
          f"{'FH hash':>9} {'FH struct':>10} {'chance':>8}")
    for entities in (8, 16, 32, 64, 128):
        runs = {
            name: np.array([trial(entities, vectors, name == "struct",
                                  args.width, seed, args.per_entity)
                            for seed in range(args.seeds)])
            for name, vectors in (("hash", hashed), ("struct", structured))}
        print(f"{entities:9d} {entities * args.per_entity:6d}  "
              f"{runs['hash'][:, 0].mean():10.3f} "
              f"{runs['struct'][:, 0].mean():12.3f}  "
              f"{runs['hash'][:, 1].mean():9.3f} "
              f"{runs['struct'][:, 1].mean():10.3f} "
              f"{args.per_entity / entities:8.3f}")
    print("\nRAIL   'read struct' must not collapse -- it does not, and that matters")
    print("FALSIF 'FH struct' must track chance like 'FH hash' does. It does not:")
    print("       flat at ~0.6 while chance falls to 0.023, so structure in the")
    print("       ADDRESS costs the store its 'I was never told that'")
    return 0


if __name__ == "__main__":
    sys.exit(main())
