"""Multi-query associative recall (MQAR) — the G0 benchmark.

The task: a sequence presents `n_pairs` key-value pairs, then queries keys again.
At each query position the correct output is the value that key was paired with.
All pairs are queried, which is what makes the task discriminating — the
single-query variant is solved by architectures much weaker than attention, and
choosing it would produce a benchmark everything passes (see docs/notes/006).

This module has no dependencies outside the standard library, on purpose. It is
the reference implementation: obviously correct and slow, and any faster
generator must be asserted against it.

Nothing here learns. This is the ruler, not the thing being measured.
"""

from __future__ import annotations

import random
from dataclasses import dataclass

#: Target value at positions where no prediction is scored.
IGNORE = -1

#: Filler modes. See `MqarConfig.filler`.
FILLERS = ("none", "random", "structured")


@dataclass(frozen=True)
class MqarConfig:
    """A benchmark difficulty setting.

    Every field is a difficulty dial. G0's output is a curve over these, not a
    single score, because a gap no local rule could close is as uninformative as
    no gap at all (docs/notes/001, P3).

    Attributes:
        n_pairs: How many key-value pairs appear, and therefore how many queries.
            This is the dial that makes the task discriminating at all, rather
            than one dial among several (docs/notes/006).
        seq_len: Total sequence length. Must leave room for the pairs and their
            queries; the remainder is filler.
        n_keys: Size of the key alphabet. Must be at least `n_pairs`, since keys
            within one sequence are distinct.
        n_values: Size of the value alphabet. Sets the base rate a constant
            predictor achieves, at roughly `1 / n_values`.
        filler: What occupies positions that are neither a pair nor a query.
            `"none"` packs the sequence with no filler — the control.
            `"random"` draws filler uniformly: maximally distracting, and
            unpredictable, which starves a predictive learning objective.
            `"structured"` cycles deterministically: still irrelevant and still
            has to be discarded, but predictable. These three exist so that the
            conflict recorded in docs/notes/002 §7 is a measurable condition
            rather than an argument.
        autoregressive: When True, each query is immediately followed by its
            answer token in the stream, so that predicting the next token at a
            query position **is** the task. When False (the default, since new
            mechanisms default to off) the answer exists only as a label beside
            the stream and never appears in it.

            This distinction is not cosmetic. docs/notes/001 P2 chose this task
            on the grounds that the self-supervised objective and the task
            metric are one quantity — and that is true only in the
            autoregressive layout. g1-01 found the classification layout does
            not satisfy P2, after the note had claimed it did.
        seed: Determines the sequence completely. Two configs with the same seed
            produce identical output.
    """

    n_pairs: int = 8
    seq_len: int = 64
    n_keys: int = 32
    n_values: int = 16
    filler: str = "structured"
    autoregressive: bool = False
    seed: int = 0

    def __post_init__(self) -> None:
        if self.filler not in FILLERS:
            raise ValueError(f"filler must be one of {FILLERS}, got {self.filler!r}")
        if self.n_pairs < 1:
            raise ValueError("n_pairs must be at least 1")
        if self.filler in ("random", "structured") and self.n_keys <= self.n_pairs:
            raise ValueError(
                f"n_keys ({self.n_keys}) must exceed n_pairs ({self.n_pairs}) "
                f"when filler is {self.filler!r}: filler is drawn from the keys "
                "this sequence does not use, so at least one must be spare"
            )
        if self.n_keys < self.n_pairs:
            raise ValueError(
                f"n_keys ({self.n_keys}) must be >= n_pairs ({self.n_pairs}): "
                "keys within a sequence are distinct"
            )
        if self.n_values < 1:
            raise ValueError("n_values must be at least 1")
        if self.seq_len < self.min_seq_len:
            raise ValueError(
                f"seq_len ({self.seq_len}) is below the minimum "
                f"{self.min_seq_len} needed for {self.n_pairs} pairs and queries"
            )

    @property
    def min_seq_len(self) -> int:
        """Shortest sequence that fits the pairs and their queries.

        Each pair costs two positions to present, plus one to query — or two in
        autoregressive mode, where the answer follows the question.
        """
        return self.n_pairs * (4 if self.autoregressive else 3)

    @property
    def trivial_floor(self) -> float:
        """The score a one-line heuristic achieves. **Not** the base rate.

        Filler is drawn from spare *keys*, so the only value tokens anywhere in a
        sequence are the `n_pairs` pair values. A strategy that emits any value
        it has already seen is therefore right whenever it happens to name the
        queried pair (`1/n_pairs`), or when some other pair carries the same
        value by chance (`(1 - 1/n_pairs) / n_values`).

        `1/n_values` — the constant-predictor base rate — is a much lower and
        much more flattering number, and comparing a model against it would
        credit as learning anything above pure guessing when the real bar is
        here. Measured against two independent shortcut baselines across eight
        conditions, this expression fits to within 0.016
        (experiments/sweeps/g0-01-baselines.txt).

        Note `n_pairs = 1` gives exactly 1.0: with a single pair the task is
        solved by naming the only value present. That configuration is
        diagnostic only and must never carry a result.
        """
        return 1 / self.n_pairs + (1 - 1 / self.n_pairs) / self.n_values

    @property
    def pad_token(self) -> int:
        """A token that is neither a key nor a value.

        Used by the `"none"` filler mode. It needs its own id because reusing a
        key id would make a padding position indistinguishable from a query.
        """
        return self.n_keys + self.n_values

    @property
    def vocab_size(self) -> int:
        """Total number of distinct token ids the generator can emit.

        Keys occupy `[0, n_keys)`, values `[n_keys, n_keys + n_values)`, and one
        further id is the pad. The key and value ranges are disjoint so that a
        model cannot succeed by confusing the two.
        """
        return self.n_keys + self.n_values + 1


@dataclass(frozen=True)
class MqarSequence:
    """One generated example.

    Attributes:
        tokens: The input sequence, length `seq_len`.
        targets: Same length as `tokens`. At a query position, the value that
            key was paired with. Everywhere else, `IGNORE`.
        pairs: The key-value mapping this sequence encodes, for tests and
            diagnostics. Not available to a model.
        query_positions: Indices where `targets` is not `IGNORE`, in order.
        answer_positions: In autoregressive mode, the index immediately after
            each query, where that query's answer is emitted. Empty otherwise.

            Recorded explicitly rather than inferred. Inferring it from
            `tokens[p+1] == targets[p]` would silently misfire in the
            classification layout whenever a filler token coincided with the
            answer, and would classify the single most important position in the
            sequence by accident.
    """

    tokens: tuple[int, ...]
    targets: tuple[int, ...]
    pairs: dict[int, int]
    query_positions: tuple[int, ...]
    answer_positions: tuple[int, ...] = ()

    def position_kinds(self) -> tuple[str, ...]:
        """What each position is: `"pair"`, `"query"`, or `"filler"`.

        Needed because "can the state predict its next input?" has a very
        different answer at each. Structured filler is a deterministic cycle and
        is predictable by construction; pair and query positions carry the
        task's actual content and are not. Averaging over all three would report
        a high number that is almost entirely the filler being easy, which says
        nothing about whether the substrate has learned anything predictive
        about the task.

        The predecessor project made exactly this mistake — its probe conflated
        a schedule-driven cue, identical in every episode, with the cue groups
        carrying random content, and scored 0.797 while predicting no content
        whatever.
        """
        kinds = ["filler"] * len(self.tokens)
        for i in range(2 * len(self.pairs)):
            kinds[i] = "pair"
        for i in self.query_positions:
            kinds[i] = "query"
        # The emitted answer is the most task-relevant position in the whole
        # sequence and would otherwise default to "filler", excluding it from
        # exactly the measurement it exists for.
        for i in self.answer_positions:
            kinds[i] = "answer"
        return tuple(kinds)

    def scored_targets(self) -> tuple[int, ...]:
        """The targets at query positions only, in order.

        This is what a score is computed over. Positions marked `IGNORE` are not
        predictions the model is asked to make, and including them would dilute
        any measurement with a large number of free correct answers.
        """
        return tuple(self.targets[i] for i in self.query_positions)


def _value_token(config: MqarConfig, value_index: int) -> int:
    """Map a value index into the value half of the vocabulary."""
    return config.n_keys + value_index


def generate(config: MqarConfig) -> MqarSequence:
    """Generate one MQAR example.

    Layout: the `n_pairs` key-value bigrams are presented first, then the
    remainder of the sequence is filler with the queries placed at random
    positions among it. Every key is queried exactly once. In autoregressive
    mode each query is immediately followed by its answer.

    This is a simplification of the interleaved layout used in the source that
    introduced the task; pairs-first is easier to reason about and preserves the
    property that matters, which is that a query is separated from its pair by a
    variable and often long distance.
    """
    rng = random.Random(config.seed)

    keys = rng.sample(range(config.n_keys), config.n_pairs)
    pairs = {k: _value_token(config, rng.randrange(config.n_values)) for k in keys}

    tokens: list[int] = []
    targets: list[int] = []
    for k in keys:
        tokens.extend((k, pairs[k]))
        targets.extend((IGNORE, IGNORE))

    # Queries go at randomly chosen offsets in the remaining space, in a random
    # order, so that recall distance varies within and across sequences.
    # Filler must never be a key this sequence uses. Otherwise a filler token
    # and a query token are byte-identical while requiring different outputs,
    # and no model can tell them apart -- the task would be ill-posed rather
    # than hard. Found by reading the first generated sequence; see the
    # calibration on rule 6 in CLAUDE.md.
    spare_keys = tuple(k for k in range(config.n_keys) if k not in pairs)

    # The body is laid out as a shuffled sequence of slots: one slot per query,
    # the rest filler. A query slot is 1 position wide normally and 2 in
    # autoregressive mode, where the answer follows the question. One code path
    # with the difference as a named parameter, rather than two layouts that
    # could drift (rule 9).
    body_start = len(tokens)
    body_len = config.seq_len - body_start
    query_width = 2 if config.autoregressive else 1
    n_filler = body_len - config.n_pairs * query_width

    slots = [True] * config.n_pairs + [False] * n_filler
    rng.shuffle(slots)
    query_order = keys[:]
    rng.shuffle(query_order)

    pending = iter(query_order)
    answer_positions: list[int] = []
    for is_query in slots:
        if is_query:
            key = next(pending)
            tokens.append(key)
            targets.append(pairs[key])
            if config.autoregressive:
                answer_positions.append(len(tokens))
                # The answer, emitted into the stream. This is what makes
                # next-token prediction at a query position *be* the task —
                # docs/notes/001 P2, which the classification layout does not
                # satisfy and was believed to.
                tokens.append(pairs[key])
                targets.append(IGNORE)
        else:
            tokens.append(_filler_token(
                config, rng, len(tokens) - body_start, spare_keys))
            targets.append(IGNORE)

    query_positions = tuple(i for i, t in enumerate(targets) if t != IGNORE)
    return MqarSequence(
        tokens=tuple(tokens),
        targets=tuple(targets),
        pairs=pairs,
        query_positions=query_positions,
        answer_positions=tuple(answer_positions),
    )


def _filler_token(
    config: MqarConfig, rng: random.Random, offset: int, spare_keys: tuple[int, ...]
) -> int:
    """Produce one filler token according to the configured mode.

    Filler is drawn from `spare_keys` — key ids this sequence did not use — so
    that it looks exactly like a key without ever being one. Drawing from a
    reserved private range would make filler trivially ignorable and would not
    test selective retention at all; drawing from the keys in use would make the
    task ill-posed.
    """
    if config.filler == "random":
        return spare_keys[rng.randrange(len(spare_keys))]
    if config.filler == "structured":
        # A deterministic cycle: irrelevant to the answer, but perfectly
        # predictable from position, so a predictive objective has signal here
        # rather than only irreducible noise (docs/notes/002 §7).
        return spare_keys[offset % len(spare_keys)]
    return config.pad_token  # "none": distracting as little as possible.


def dataset(config: MqarConfig, n_sequences: int) -> list[MqarSequence]:
    """Generate `n_sequences` examples, each with a distinct seed.

    Seeds are derived from `config.seed` so the whole dataset is reproducible
    from one number.
    """
    if n_sequences < 1:
        raise ValueError("n_sequences must be at least 1")
    base = random.Random(config.seed)
    seeds = [base.randrange(2**31) for _ in range(n_sequences)]
    from dataclasses import replace

    return [generate(replace(config, seed=s)) for s in seeds]
