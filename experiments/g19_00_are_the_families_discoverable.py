"""Can `ContentIndex` find the families it is never told about?

Note 048's stated risk, checked before anything is spent on the real question:

> *"The families must be discoverable but not trivial. If entities of a family
> are too alike, `ContentIndex` finds them instantly and the task measures
> nothing about representation. If too unlike, no grouping is possible and the
> task measures the clusterer."*

Decision 63's rule — probe the bottom of a range before spending a matrix on it —
and g17-01's, which cost twenty minutes and saved a matrix.

**This trains no model.** It fits the content index on generated sequences,
clusters it, and asks how much of the true family structure came back. If the
answer is "none", the task is unusable and nothing downstream would mean
anything. If the answer is "all of it, at every setting", the task is too easy
and a positive result would say nothing about representation either.

## What is measured

    purity      of each recovered cluster, the share held by its commonest true
                family, averaged over clusters and weighted by size. 1.0 is
                perfect, and the chance level depends on the shape -- printed
                beside it rather than assumed.

## The two dials, and only one of them turned out to matter

`attribute_mentions` -- how often an entity is seen beside its attributes -- was
the dial note 048 named. **It does nothing.** Purity is identical at 2, 4, 8 and
16 mentions and at 50 or 200 background streams, because the content vectors are
normalised: more exposure changes their magnitude and not their direction, and
every entity in a family already sees the same attributes.

Identical numbers across a swept axis mean the axis is not reaching the
measurement, which cost this project a night on 2026-07-29 in the other
direction. Here it was true.

**`k`, the number of groups asked for, is the dial.** It has to cover the token
CLASSES, not just the families: 8 entity families and 8 attribute groups cannot
both fit in 8 clusters, so families get merged for want of a slot.

    k = 8      purity 0.625     fewer clusters than kinds of thing
    k = 12            0.875
    k = 16            1.000     <- entities AND attributes both fit
    k = 48            1.000

**That is a design constraint on the real sweep, not a curiosity.**
`keys.ByConcept` groups the whole vocabulary, so its K axis must start above the
number of token classes or it will be measuring cluster starvation rather than
concept addressing.
"""

from __future__ import annotations

import sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from openplexus.content import ContentIndex  # noqa: E402
from openplexus.grouping import cluster  # noqa: E402
from openplexus.tasks.families import FamilyConfig, background  # noqa: E402

CONTENT_WIDTH = 128
MENTIONS = (2, 8)
SEQUENCES = (200,)
#: THE DIAL THAT MATTERS. Must cover the token classes, not just the families.
GROUPS = (8, 12, 16, 24, 48)
#: WINDOW 1 AND NO WEIGHTING, both measured rather than assumed. At the default
#: window of 4 an entity's neighbours spill into the next mention -- a different
#: family entirely -- and purity fell from 0.875 to 0.375. Down-weighting
#: frequent context costs another 0.125 here, because attributes are not Zipfian
#: and the thing being down-weighted IS the signal.
WINDOW = 1
POWER = 0.0


def purity(groups: list[list[int]], config: FamilyConfig) -> tuple[float, int]:
    """Weighted mean share of each cluster held by its commonest family.

    Entities only. Attribute and value tokens are excluded — they cluster too,
    and counting them would measure the clusterer finding the *token classes*
    rather than the families inside the entity class.
    """
    entities = {config.entity_base + i for i in range(config.n_entities)}
    total = right = 0
    for group in groups:
        members = [t for t in group if t in entities]
        if not members:
            continue
        counts = Counter(config.family_of(t) for t in members)
        right += counts.most_common(1)[0][1]
        total += len(members)
    return (right / total if total else float("nan")), total


def one_cell(mentions: int, sequences: int, groups: int,
             seed: int = 0) -> dict:
    config = FamilyConfig(attribute_mentions=mentions, seed=seed)
    # BACKGROUND streams, not task sequences. The family structure lives here
    # and is learned across many of them; the task sequences carry only facts
    # and questions and are never used to fit the index.
    streams = background(config, sequences)

    index = ContentIndex(config.vocab_size, width=CONTENT_WIDTH, seed=seed,
                         power=POWER, window=WINDOW)
    for stream in streams:
        index.observe(stream)

    # Asked for exactly as many groups as there are families. Giving the
    # clusterer the true count is generous and deliberate: if it cannot recover
    # the structure WITH the answer to that question, it certainly cannot
    # without it, and this is a feasibility check rather than a result.
    share, covered = purity(cluster(index.vectors, groups, seed=seed), config)
    return dict(mentions=mentions, sequences=sequences, groups=groups,
                seed=seed,
                purity=round(share, 4),
                entities_clustered=covered,
                entities=config.n_entities,
                families=config.n_families,
                family_size=config.family_size,
                chance=round(1.0 / config.n_families, 4),
                spread=round(index.spread(), 4))


def main() -> int:
    # Every experiment goes through the harness, which is where
    # `refuse_if_mutating` lives -- the guard that stops a run reading source
    # the mutation harness is halfway through editing. R3, and on 2026-07-29 a
    # run that started before a mutation began was voided by exactly that.
    harness.refuse_if_mutating()
    reference = FamilyConfig()
    print(f"{reference.n_families} families of {reference.family_size}, "
          f"{reference.n_attributes} attributes each, "
          f"vocab {reference.vocab_size}")
    print(f"chance purity for a random grouping is about "
          f"{1.0 / reference.n_families:.3f}\n")
    print(f"vocab {reference.vocab_size}: {reference.n_entities} entities, "
          f"{reference.n_families * reference.n_attributes} attributes, "
          f"{reference.n_values} values")
    print()
    print(f"{'k':>4} {'mentions':>9} {'purity':>8} {'covered':>9}")
    for groups in GROUPS:
        for mentions in MENTIONS:
            record = one_cell(mentions, SEQUENCES[0], groups)
            print(f"{record['groups']:>4} {record['mentions']:>9} "
                  f"{record['purity']:>8.3f} "
                  f"{record['entities_clustered']:>4}/{record['entities']:<4}",
                  flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
