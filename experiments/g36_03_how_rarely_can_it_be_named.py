"""g36-03: how rarely can a thing be NAMED and still be grounded?

`g36-01` showed a word and a picture reaching one concept, with the word present
in **every** occasion. That is not how naming works: a child sees dogs constantly
and hears the word occasionally.

**And it is the achievable form of gate G7.** The stricter reading — reach an
image code the word NEVER co-occurred with — is structurally impossible here and
a test of it would say nothing about the design: in this stream each occasion
shows one image, so image codes never co-occur with each other and no path
`unseen code — seen code — word` exists.

That locates where generalisation lives, which is worth stating plainly:

    the QUANTISER      generalises WITHIN a modality: a new 3 lands on the code
                       that existing 3s already occupy
    CO-OCCURRENCE      links ACROSS modalities: that code to the word

So the question this can answer is whether naming a FRACTION of a code's
instances grounds the whole code — the concept largely introduced through
pictures, with the word arriving rarely.

`g32-02` measured about **16 occasions per concept** as the threshold. At 50
codes over 4,000 images a code holds 80 images, so a naming rate of about **0.20**
should be where it breaks. That is a quantitative prediction from a prior run,
which is what makes this worth running rather than assuming.

Predictions: `experiments/sweeps/g36-03-how-rarely-can-it-be-named.txt`
"""

from __future__ import annotations

import pathlib
import sys
import time
from collections import Counter

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from openplexus.grounding import (STATISTICS, CoOccurrence,  # noqa: E402
                                  equivalence_classes)
from openplexus.grouping import cluster  # noqa: E402
from openplexus.tasks import mnist  # noqa: E402

DATA = ROOT / "data" / "mnist"
IMAGES = 4000
CODES = 50
NOISE = 2
DISTRACTORS = 1
SEEDS = (0, 1, 2)
NAMING = (0.02, 0.05, 0.10, 0.20, 0.50, 1.00)
ARM = "conditional"


def main() -> None:
    harness.parse_args(__doc__)
    started = time.time()
    if not (DATA / "train-images-idx3-ubyte.gz").exists():
        raise SystemExit(f"no data in {DATA}. Run: python tools/fetch_mnist.py")

    digits = mnist.read(DATA, limit=IMAGES)
    chance = max(Counter(digits.labels).values()) / len(digits)
    print(f"g36-03  {len(digits)} images, {CODES} codes, "
          f"{len(mnist.WORDS)} words, chance {chance:.4f}")
    print(f"        a code holds about {len(digits) // CODES} images; "
          f"g32-02's threshold is ~16 namings\n")

    header = (f"{'naming':>8}{'namings/code':>14}{'quantiser':>11}{'link':>8}"
              f"{'grounded':>10}{'reach':>8}{'classes':>9}")
    print(header)
    print("-" * len(header))

    for naming in NAMING:
        quant, links, grounded, reaches, sizes = [], [], [], [], []
        for seed in SEEDS:
            flat = np.frombuffer(b"".join(digits.images), dtype=np.uint8)
            vectors = flat.reshape(len(digits), digits.pixels).astype(np.float64)
            norms = np.linalg.norm(vectors, axis=1, keepdims=True)
            norms[norms == 0.0] = 1.0
            groups = cluster(vectors / norms, k=CODES, seed=seed)
            assigned = [-1] * len(digits)
            for code, members in enumerate(groups):
                for row in members:
                    assigned[row] = code

            holders: dict[int, Counter] = {}
            for code, label in zip(assigned, digits.labels):
                if code >= 0:
                    holders.setdefault(code, Counter())[label] += 1
            majority = {c: n.most_common(1)[0][0] for c, n in holders.items()}
            agreed = sum(n[majority[c]] for c, n in holders.items())
            quant.append(agreed / sum(sum(n.values()) for n in holders.values()))

            words = {d: CODES + d for d in range(len(mnist.WORDS))}
            index = CoOccurrence()
            rng = np.random.default_rng(seed)
            for code, label in zip(assigned, digits.labels):
                if code < 0:
                    continue
                present = {code}
                # THE WORD IS SPOKEN ONLY SOMETIMES. Everything else is
                # unchanged from g36-01, so the naming rate is the only axis.
                if rng.random() < naming:
                    present.add(words[label])
                for other in rng.choice(len(mnist.WORDS), NOISE, replace=False):
                    present.add(words[int(other)])
                for extra in range(DISTRACTORS):
                    present.add(CODES + len(mnist.WORDS) + extra)
                index.observe(present)

            recovered = equivalence_classes(index, STATISTICS[ARM], None)
            hit = seen = reached = 0
            wanted = {}
            for digit, token in words.items():
                found = recovered.get(token, frozenset({token}))
                wanted[digit] = found
                pictures = [s for s in found if s < CODES]
                if pictures:
                    reached += 1
                for picture in pictures:
                    seen += 1
                    if majority.get(picture) == digit:
                        hit += 1
            links.append(hit / seen if seen else 0.0)
            reaches.append(reached / len(words))
            grounded.append(sum(
                1 for code, label in zip(assigned, digits.labels)
                if code >= 0 and code in wanted[label]
                and majority.get(code) == label) / len(digits))
            sizes.append(sum(len(v) for v in recovered.values())
                         / max(len(recovered), 1))

        mean = lambda v: sum(v) / len(v)          # noqa: E731 - local
        print(f"{naming:>8.2f}{naming * (len(digits) // CODES):>14.1f}"
              f"{mean(quant):>11.4f}{mean(links):>8.4f}{mean(grounded):>10.4f}"
              f"{mean(reaches):>8.4f}{mean(sizes):>9.2f}")

    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
