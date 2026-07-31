"""Occasions: a stream of moments, each showing several things at once.

A generator for the **cross-situational learning** question — the one
[`GOALS.md`](../../GOALS.md) §1.2b makes load-bearing. A picture of a dog, a bark
and the word *dog* are one concept, and nothing ever says so. All the stream
offers is that they keep turning up together while everything else in the room
changes.

Each occasion is a set of surfaces that co-occurred, with a timestamp and with
**no label saying which of them belong together.** That is deliberately the input
shape of a time bucket, so the distributed join can be pointed at this generator
without the generator knowing anything about it.

## What this does NOT duplicate, and what was searched

Searched by capability — co-occurrence, episode, bucket, trial — across
`openplexus/`, `openplexus/tasks/`, `tools/`, `tests/` and `experiments/`.

- **`openplexus/content.py` (`ContentIndex`) accumulates co-occurrence already,
  and this does not replace it.** That one consumes a token *sequence* and folds
  neighbours into a superposed vector per token; there is no notion of an
  occasion, no timestamp, and no ground-truth grouping to score against. It is a
  representation. This is an **instrument**: it generates a stream whose correct
  answer is known, so a representation can be scored on it. `ContentIndex` is one
  of the things that ought to be run against this.
- **`openplexus/tasks/families.py`** is the existing instrument where things
  resemble each other, but resemblance there is a designed *relational* structure
  over entities, and every fact is stated. Here nothing is ever stated: identity
  has to fall out of repetition alone.
- **`openplexus/tasks/mqar.py`** presents `(key, value)` bindings explicitly.
  The whole difficulty here is that the binding is never presented.
- **`openplexus/grouping.py`** clusters vectors into groups. It is a consumer of
  a representation, not a source of data, and it has no ground truth of its own.

## The persistent distractor, which is the point of the file

`GOALS.md` and both grounding option records register the same falsifier:
introduce a concept alongside **a distractor present on every single occasion**,
and see whether counting can tell them apart. `distractors` builds exactly that.

**And the naive form of that test is settled by arithmetic, so this generator is
built to avoid it.** If a concept's surfaces were present on every occasion it
appeared, then the count against the true partner and the count against the
persistent distractor would be *equal by construction* — a tie no run is needed
to predict, which CLAUDE.md rule 10 says is not evidence however it comes out.

So `presence` exists. A concept's surfaces appear only *sometimes* when it is the
subject, exactly as the word `dog` is spoken on only some of the occasions a dog
is seen. Below 1.0 the persistent distractor is **strictly commoner** than the
true partner, so raw counting does not tie — it prefers the distractor, and any
mechanism that beats it has done something.

## Where the difficulty actually lives

Three knobs, and only the first is the registered falsifier:

    distractors   things present every time            the registered falsifier
    presence      how often a surface shows up         makes the falsifier non-trivial
    zipf          how uneven concept frequencies are   where normalisation breaks

The third is the one with no predictable answer. Normalising by frequency is the
standard escape from a persistent distractor, and it is known to over-reward rare
events — [note 045](../../docs/archive/notes/045-addresses-that-mean-something.md)
recorded `1/sqrt(frequency)` sharpening some queries and destroying others on
Shakespeare, and named PPMI and subsampling as the untried alternatives. `zipf`
is what turns that anecdote into an axis.

## What is deliberately absent

**No perception layer, and no vectors anywhere.** A surface is an integer, as it
would be after quantisation. That is what makes this runnable in seconds and is
why it is the first test of the grounding mechanism rather than the last.

**No modality alignment is given away.** `modality` exists so a question can be
asked in one modality and answered in another (G7's shape), but the mechanism is
never handed the tag — an occasion is a bare set of integers.
"""

from __future__ import annotations

import random
from dataclasses import dataclass


@dataclass(frozen=True)
class OccasionConfig:
    """How a stream of occasions is shaped.

    Attributes:
        concepts: How many distinct things exist in the world. **A small world
            gives accidental structure away**: with a few dozen surfaces an
            occasion of six lands on a same-concept surface by chance about two
            times in five, so the shuffled control stops being a floor. Found by
            `test_occasions.ShuffledControl`, which is where the number lives.
            Keep this large enough that `concepts * surfaces` is many times the
            occasion size, or read every score against the control rather than
            against zero.
        surfaces: How many surfaces each concept has — its appearances in
            different modalities. Three stands in for image, sound and word.
        presence: Probability that any one surface of the subject concept is
            present on an occasion it is the subject of. **Must be below 1.0 for
            the falsifier to be non-trivial**; see the module docstring.
        noise: How many surfaces belonging to *other* concepts appear on each
            occasion — the sofa and the face. Present once and then gone.
        distractors: How many surfaces are present on **every** occasion. These
            belong to no concept, so each one's true class is itself alone.
        zipf: Exponent on the concept-frequency distribution. 0.0 is uniform;
            larger is more skewed, so a few concepts dominate the stream.
        pairings: Which of a concept's modalities may appear TOGETHER.

            `"complete"` is every run before 2026-07-31: all surfaces of the
            subject may show up at once, so learning that they belong together
            never requires more than one direct co-occurrence. **That is easier
            than the world and easier than the design claims to handle.**

            `"chain"` lets modality `m` appear only with `m±1`, so the ends of
            the chain are NEVER seen together and can only be linked through
            what sits between them. `"star"` lets modality 0 appear with any
            other and the others never with each other.

            This is `GOALS.md` gate G7's shape — a concept met through one
            modality and queried through another — and it is what
            `identity-without-a-global-id.md` means by *"reached by starting at
            any member and WALKING"*. Walking only does work when some members
            are not directly connected.
        occasions: How many moments the stream runs for.
        seed: Stream seed.
    """

    concepts: int = 32
    surfaces: int = 3
    presence: float = 0.7
    noise: int = 3
    distractors: int = 1
    zipf: float = 0.0
    pairings: str = "complete"
    occasions: int = 4000
    seed: int = 0

    def __post_init__(self) -> None:
        if self.concepts < 2:
            raise ValueError(
                "a world with one concept has nothing to confuse it with, so "
                "every recovered class would be correct for free")
        if self.surfaces < 2:
            raise ValueError(
                "with one surface per concept there is no second appearance to "
                "reach, which is the whole quantity being measured")
        if not 0.0 < self.presence <= 1.0:
            raise ValueError("presence must be in (0.0, 1.0]")
        if self.noise < 0:
            raise ValueError("noise cannot be negative")
        if self.distractors < 0:
            raise ValueError("distractors cannot be negative")
        if self.zipf < 0.0:
            raise ValueError(
                "a negative zipf exponent would make RARE concepts the common "
                "ones, which is the same skew relabelled and reads as a bug")
        if self.occasions < 1:
            raise ValueError("a stream needs at least one occasion")
        if self.pairings not in ("complete", "chain", "star"):
            raise ValueError(
                f"pairings must be complete, chain or star, not "
                f"{self.pairings!r}")
        # The noise draw is without replacement from surfaces outside the
        # subject concept and outside the distractors, so it cannot ask for more
        # than exist. Caught here rather than at the first unlucky draw, because
        # a generator that raises partway through a stream is a generator that
        # fails in whichever sweep cell happened to be long enough.
        available = (self.concepts - 1) * self.surfaces
        if self.noise > available:
            raise ValueError(
                f"noise {self.noise} exceeds the {available} surfaces that "
                f"belong to other concepts")

    @property
    def concept_surfaces(self) -> int:
        """How many surfaces belong to concepts, distractors excluded."""
        return self.concepts * self.surfaces

    @property
    def vocabulary(self) -> int:
        """Every surface id in the stream, distractors included."""
        return self.concept_surfaces + self.distractors

    def concept_of(self, surface: int) -> int | None:
        """Which concept a surface belongs to, or `None` for a distractor."""
        if surface >= self.concept_surfaces:
            return None
        return surface // self.surfaces

    def modality(self, surface: int) -> int | None:
        """Which modality a surface is in, or `None` for a distractor.

        Never given to a mechanism. It exists so a question can be asked in one
        modality and scored in another, which is G7's shape.
        """
        if surface >= self.concept_surfaces:
            return None
        return surface % self.surfaces

    def is_distractor(self, surface: int) -> bool:
        """Whether a surface is one of the things present every time."""
        return surface >= self.concept_surfaces

    def classes(self) -> dict[int, frozenset[int]]:
        """The correct answer: every surface mapped to its concept's surfaces.

        A distractor maps to itself alone, which is what makes *"is the
        distractor pruned"* a scorable question rather than a judgement.
        """
        answer: dict[int, frozenset[int]] = {}
        for concept in range(self.concepts):
            members = frozenset(
                concept * self.surfaces + m for m in range(self.surfaces))
            for surface in members:
                answer[surface] = members
        for surface in range(self.concept_surfaces, self.vocabulary):
            answer[surface] = frozenset({surface})
        return answer

    def groups(self) -> tuple[tuple[int, ...], ...]:
        """Which sets of modalities may co-occur on one occasion.

        `complete` returns exactly ONE group, and that is load-bearing rather
        than tidy: `generate` takes an untouched code path when there is one
        group, so every stream produced before `pairings` existed is reproduced
        byte for byte and no earlier measurement is invalidated by this knob.
        """
        every = tuple(range(self.surfaces))
        if self.pairings == "complete":
            return (every,)
        if self.pairings == "chain":
            return tuple((m, m + 1) for m in range(self.surfaces - 1))
        return tuple((0, m) for m in range(1, self.surfaces))

    def apart(self) -> tuple[tuple[int, int], ...]:
        """Modality pairs that NEVER share an occasion — what a walk must bridge.

        Empty for `complete`, which is why every run before this knob existed
        was measuring direct association rather than reach.
        """
        together = set()
        for group in self.groups():
            for one in group:
                for other in group:
                    together.add((one, other))
        return tuple(
            (one, other)
            for one in range(self.surfaces)
            for other in range(one + 1, self.surfaces)
            if (one, other) not in together)

    def weights(self) -> list[float]:
        """How often each concept is the subject, before normalisation."""
        return [1.0 / ((i + 1) ** self.zipf) for i in range(self.concepts)]


@dataclass(frozen=True)
class Occasion:
    """One moment: what was present, and when.

    Attributes:
        when: The timestamp, in arbitrary units, strictly increasing across a
            stream. Carried so a bucket join can round it; nothing in this
            module consults it.
        surfaces: What co-occurred, sorted. **The set a mechanism is given.**
        subject: Which concept the occasion was about. **Diagnostics only** —
            handing this to a mechanism would be handing it the answer.
    """

    when: int
    surfaces: tuple[int, ...]
    subject: int


def generate(config: OccasionConfig, count: int | None = None) -> list[Occasion]:
    """Build a stream of occasions.

    Args:
        config: The world's shape.
        count: How many occasions, defaulting to `config.occasions`. Present so
            a probe can take a short stream from a configured world without
            constructing a second config that differs in one field.

    Returns:
        Occasions in time order, timestamps strictly increasing from 0.
    """
    total = config.occasions if count is None else count
    if total < 1:
        raise ValueError("a stream needs at least one occasion")
    rng = random.Random(config.seed)
    weights = config.weights()
    subjects = range(config.concepts)
    always = tuple(range(config.concept_surfaces, config.vocabulary))

    stream: list[Occasion] = []
    for when in range(total):
        subject = rng.choices(subjects, weights=weights, k=1)[0]
        own = [subject * config.surfaces + m for m in range(config.surfaces)]

        # REJECTION, NOT A FORCED MEMBER.
        #
        # An occasion showing none of its subject's surfaces carries no signal
        # about that subject and would silently dilute every count. The obvious
        # fix -- pick one surface as mandatory, then draw the rest -- changes the
        # marginal presence rate of whichever surface was forced, so `presence`
        # would stop describing the stream it names. Redrawing keeps the
        # marginals exactly `presence` conditioned on non-empty, which is a
        # statement `test_occasions.py` checks rather than this comment asserting.
        groups = config.groups()
        while True:
            if len(groups) == 1:
                present = [s for s in own if rng.random() < config.presence]
            else:
                chosen = groups[rng.randrange(len(groups))]
                present = [own[m] for m in chosen
                           if rng.random() < config.presence]
            if present:
                break

        if config.noise:
            elsewhere = [
                s for s in range(config.concept_surfaces)
                if s // config.surfaces != subject]
            present.extend(rng.sample(elsewhere, config.noise))

        present.extend(always)
        stream.append(Occasion(when=when,
                               surfaces=tuple(sorted(present)),
                               subject=subject))
    return stream


def shuffled(stream: list[Occasion], seed: int = 0) -> list[Occasion]:
    """The control: the same occasions with the co-occurrence structure destroyed.

    Every surface in the stream is relabelled by one global permutation... which
    would change nothing, so that is *not* what this does. Instead each occasion
    keeps its SIZE and draws its members afresh from the stream's own surface
    frequencies, independently. Frequencies survive; co-occurrence does not.

    **This is the control that makes a score readable.** A mechanism scoring the
    same here as on the real stream has learned frequency, not structure — which
    is exactly what note 045 found a shuffled corpus doing to content vectors.
    A `subject` is retained so the two streams have the same shape, but it no
    longer describes the surfaces present and must not be scored against.
    """
    rng = random.Random(seed)
    population: list[int] = []
    for occasion in stream:
        population.extend(occasion.surfaces)
    if not population:
        raise ValueError("an empty stream has no frequencies to preserve")

    control: list[Occasion] = []
    for occasion in stream:
        size = len(occasion.surfaces)
        drawn: set[int] = set()
        # Sampling with replacement from the frequency population, rejecting
        # duplicates, so the marginal frequencies are the stream's own. A draw
        # for a size larger than the number of distinct surfaces would spin
        # forever, so the loop is bounded and falls back to whatever it has.
        for _ in range(size * 64):
            if len(drawn) == size:
                break
            drawn.add(rng.choice(population))
        control.append(Occasion(when=occasion.when,
                                surfaces=tuple(sorted(drawn)),
                                subject=occasion.subject))
    return control
