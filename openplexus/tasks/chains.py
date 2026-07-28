"""Relational chains: answer a question that no single stated fact answers.

[Note 038](../../docs/notes/038-a-relational-task-and-whether-we-can-do-it-at-all.md)
is the design and the reasoning. In short: this project measures bits per
character, and a character bigram table cannot represent a concept, so the task
itself is part of the ceiling. CLUTRR (Sinha et al., arXiv:1908.06177) has the
structure worth borrowing — entities, relations, and **train on short chains,
test on longer ones** — without its natural-language layer, which the authors'
own result says a symbolic model does better without.

A sequence states links and then asks one question:

    a1 -> b1    b1 -> c1    a2 -> b2    b2 -> c2   ...   ? a1    c1

with `hops = 2`. At `hops = 1` the question is `? a1` answered by `b1`, which is
plain cued recall — the thing MQAR already measures and this model demonstrably
does.

## HOPS IS A DIAL AND THAT IS THE WHOLE POINT

G0 asks whether a task is one a random substrate cannot already do **and that is
learnable at all**. It is marked passed for MQAR and says nothing about this.
And there is a specific reason to doubt it here: the readout consumes ONE
retrieval, so two-hop inference may be structurally impossible for this
architecture, in which case every cell sits at chance — and **chance is
indistinguishable from a hard problem**.

So hops starts at 1, where the model is known to work. One hop is the positive
control; if it fails there the implementation is broken rather than the model.
Where the curve falls to chance is the measurement, and it is only readable
because the 1-hop end is known good.

## The shortcut, and why the generator refuses it

The obvious way to score well without composing is for the answer to sit next to
the question somewhere in the presentation — then a one-hop lookup wins and
nothing about composition was tested. **`a -> c` is never stated for any chain**,
at any hop count, and `test_chains.py` asserts it over generated data rather
than trusting this docstring.

This is the MQAR filler bug's lesson: there, a filler token could be
byte-identical to a query requiring a different answer, which made the task
impossible rather than hard. It was found in the first sequence ever generated,
by printing it and reading it.
"""

from __future__ import annotations

import random
from dataclasses import dataclass

#: Positions that are not questions carry no target.
IGNORE = -1


@dataclass(frozen=True)
class ChainConfig:
    """Shape of a relational-chain task.

    Attributes:
        n_chains: Independent chains per sequence. Each contributes `hops + 1`
            distinct symbols, and exactly one is asked about.
        hops: Links between the queried symbol and its answer. **1 is cued
            recall and is the positive control**; 2 is the first cell that
            needs composition.
        n_symbols: Size of the alphabet chains are drawn from. Must leave room
            for `n_chains * (hops + 1)` distinct symbols plus filler.
        seq_len: Total length including the question.
        seed: Generator seed.
    """

    n_chains: int = 8
    hops: int = 2
    n_symbols: int = 48
    seq_len: int = 96
    seed: int = 0

    @property
    def separator_token(self) -> int:
        """Sits between links so that laying them end to end states no link
        that was not intended.

        **This store binds every ADJACENT PAIR.** Writing links as bare pairs
        `a b c d` also states `b -> c`, which is a link no chain contains — and
        with one boundary per link the sequence carries as many false links as
        true ones. Found by generating one sequence and reading it, which is the
        habit the MQAR filler bug bought: there a filler token could equal a
        query token and made the task IMPOSSIBLE rather than hard.

        The separator is outside the symbol alphabet, so a false pair always
        involves a token no chain uses and cannot be mistaken for a chain link.
        """
        return self.n_symbols + 1

    @property
    def query_token(self) -> int:
        """Marks the question. Outside the symbol alphabet so it cannot be an
        answer, which would let a model score by echoing the marker."""
        return self.n_symbols

    @property
    def vocab_size(self) -> int:
        return self.n_symbols + 2

    @property
    def symbols_used(self) -> int:
        return self.n_chains * (self.hops + 1)

    @property
    def trivial_floor(self) -> float:
        """Guessing among the symbols that could be an answer.

        **Not 1/vocab.** A model that has learned only which symbols end chains
        does better than uniform without composing anything, so the floor a
        result must clear is the stronger strategy, not the weakest one. g8-01's
        seq-1536 row was withdrawn for comparing against a floor that was itself
        broken.
        """
        return 1.0 / self.n_chains

    def __post_init__(self) -> None:
        if self.hops < 1:
            raise ValueError("hops is a number of links and must be at least 1")
        if self.n_chains < 2:
            raise ValueError(
                "one chain makes the answer the only chain-ending symbol, so "
                "guessing solves it; the floor would be 1.0")
        if self.symbols_used > self.n_symbols:
            raise ValueError(
                f"{self.n_chains} chains of {self.hops + 1} symbols need "
                f"{self.symbols_used} distinct symbols and only "
                f"{self.n_symbols} exist; chains would share symbols and a "
                f"symbol would have two successors")
        if self.seq_len < self.min_seq_len:
            raise ValueError(
                f"seq_len {self.seq_len} cannot hold {self.min_seq_len} "
                f"positions of links and question")

    @property
    def min_seq_len(self) -> int:
        """Every link as a separated pair, plus the query marker, symbol and
        answer."""
        return self.n_chains * self.hops * 3 + 3


@dataclass(frozen=True)
class ChainSequence:
    """One generated example.

    Attributes:
        tokens: The input sequence.
        targets: The answer at the answer position, `IGNORE` everywhere else.
        chains: The chains this sequence states, for tests and diagnostics.
            Not available to a model.
        asked: The chain that was queried, as a tuple of symbols.
        answer_position: Where the answer is emitted.
    """

    tokens: tuple[int, ...]
    targets: tuple[int, ...]
    chains: tuple[tuple[int, ...], ...]
    asked: tuple[int, ...]
    answer_position: int

    def scored_targets(self) -> tuple[int, ...]:
        """The one target a score is computed over."""
        return (self.targets[self.answer_position],)


def generate(config: ChainConfig) -> ChainSequence:
    """One relational-chain example.

    Links are stated as adjacent pairs in shuffled order, so a chain's own links
    are not contiguous and the model cannot follow one by reading forwards. The
    question comes last.
    """
    rng = random.Random(config.seed)
    symbols = rng.sample(range(config.n_symbols), config.symbols_used)
    chains = tuple(
        tuple(symbols[i * (config.hops + 1):(i + 1) * (config.hops + 1)])
        for i in range(config.n_chains))

    links = [(chain[step], chain[step + 1])
             for chain in chains for step in range(config.hops)]
    rng.shuffle(links)

    tokens: list[int] = []
    for source, target in links:
        tokens.extend((config.separator_token, source, target))

    asked = chains[rng.randrange(config.n_chains)]
    tokens.extend((config.query_token, asked[0]))
    answer_position = len(tokens) - 1
    tokens.append(asked[-1])

    # Filler drawn ONLY from symbols no chain uses. A filler token equal to a
    # chain symbol would state a link that does not exist, which is the MQAR
    # filler bug: it made the task impossible rather than hard.
    spare = [s for s in range(config.n_symbols) if s not in set(symbols)]
    while len(tokens) < config.seq_len and spare:
        tokens.append(rng.choice(spare))

    targets = [IGNORE] * len(tokens)
    targets[answer_position] = asked[-1]
    return ChainSequence(tuple(tokens), tuple(targets), chains, asked,
                         answer_position)


def dataset(config: ChainConfig, n_sequences: int) -> list[ChainSequence]:
    """`n_sequences` examples, each with its own seed."""
    from dataclasses import replace
    return [generate(replace(config, seed=config.seed + i))
            for i in range(n_sequences)]
