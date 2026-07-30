"""Cued recall where only some bindings are worth keeping, marked by reward.

[Note 017](../../docs/archive/notes/017-a-task-with-something-at-stake.md) is the design
and the reasoning. In short: MQAR is a sequence of random symbols, so **nothing
in it is good or bad for anything**, and any signal saying which positions matter
is `position_kinds()` however it is dressed up. That makes the difference between
"an API told us" and "the agent's own value system told us" untestable — and that
difference is the whole AGI question ([note 016](../../docs/archive/notes/016-who-supplies-relevance.md)).

A sequence presents **cue → item** bindings. Some are followed, after a **delay**,
by a reward token. Only rewarded cues are ever asked about.

    b→7   f→3   REWARD   k→9   c→2   REWARD   m→5  ...  f -> 3     c -> 2

So the task is: bind everything cheaply, and work out from a **late, in-stream
signal** which bindings will be asked for. That is tagging and capture in the
form the paper describes it, and MQAR could not pose it — MQAR has no late signal,
which is a considerably better explanation for four negative results than "the
mechanism does not work".

**The reward token is in the data, not the metadata.** A mechanism that learns
*keep what preceded reward* is learning from its own input, which any deployed
agent has. `position_kinds()` is a property of the generator that no running
system can read.

## Why not the simpler version

The first design had no cues: one rewarded item, and queries asking "which item
was rewarded". Reading the first generated sequence killed it — with one rewarded
item **every query is identical**, and this memory is keyed on the current token,
so `QUERY` retrieves whatever was last bound to `QUERY`. The second and third
queries were answerable by repetition, which would have inflated every number
this task ever produced.

Distinct cues fix it: each query is a different token, so nothing carries over.

## Scoring: first asks and repeats are different quantities

In autoregressive mode the answer follows the query, so the **first** query of a
cue re-binds it. Every later query about that cue is answerable from a binding
written a few steps earlier, which measures short-term echo rather than
retention.

The headroom search made the size of that difference concrete: at one
configuration an ungated model scored **0.369 over all queries and 0.225 on first
asks**. Averaging them hides the number that matters, so anything measuring this
task reports both.

## What it does not test

Acting. There is no policy, no choice and no cost for being wrong, so calling
this a reinforcement task would overstate it. It is recall gated by a reward
signal, which is one component of one.
"""

from __future__ import annotations

import random
from dataclasses import dataclass

IGNORE = -1


@dataclass(frozen=True)
class RewardConfig:
    """Shape of a reward-gated cued-recall task.

    Attributes:
        n_pairs: Bindings presented per sequence. Only the rewarded ones are
            queried.
        n_rewarded: How many of those are followed by a reward token, and so how
            many are asked about. `n_rewarded / n_pairs` is the **reward rate**,
            which sets the base rate note 013 blamed for the salience gate's
            failure and which g8-02 could not move. Here it is a dial.
        n_cues: Size of the cue alphabet. Cues within a sequence are distinct.
        n_values: Size of the value alphabet. Sets the trivial floor at
            `1 / n_values`.
        seq_len: Total length including the query block.
        delay: Steps between a rewarded binding and its reward token. **The dial
            this task exists for.** At delay 1 the marker is adjacent and a gate
            can store "the thing just before the obvious token" without learning
            anything about value; at a longer delay the storage decision cannot
            wait for it, which is the situation tagging and capture solves.
        queries_per_reward: How many times each rewarded cue is asked about.
            Above 1 gives consolidate-on-use a second occasion, the same reason
            `queries_per_pair` exists in MQAR.
        autoregressive: When True the answer follows the query in the stream, so
            predicting the next token at a query position **is** the task.
        seed: Generator seed.
    """

    # VERIFIED AGAINST experiments/g9_01_answerable.py, and these are not the
    # first defaults. The first set -- 8 pairs, 2 rewarded, seq_len 192 -- scored
    # ungated 1.000 against oracle 1.000: no room for a gate to matter, and every
    # arm of every sweep would have read 1.000. At the values below the gap is
    # 0.744 and an ungated model sits AT the trivial floor on first asks (0.119
    # against 0.125), which is the whole range available.
    #
    # A default that cannot test anything is worse than no default, because it
    # looks like a starting point.
    n_pairs: int = 24
    n_rewarded: int = 4
    n_cues: int = 64
    n_values: int = 8
    seq_len: int = 768
    delay: int = 8
    queries_per_reward: int = 3
    autoregressive: bool = True
    seed: int = 0

    @property
    def reward_token(self) -> int:
        return self.n_cues + self.n_values

    @property
    def vocab_size(self) -> int:
        return self.n_cues + self.n_values + 1

    @property
    def trivial_floor(self) -> float:
        """Guessing uniformly among values, which is what knowing nothing gives.

        Printed beside every number this task produces. A result below it is not
        a weak result, it is a broken one.
        """
        return 1.0 / self.n_values

    @property
    def min_seq_len(self) -> int:
        width = 2 if self.autoregressive else 1
        body = self.n_pairs * 2 + self.n_rewarded * (self.delay + 1)
        return body + self.n_rewarded * self.queries_per_reward * width

    def __post_init__(self) -> None:
        if self.n_rewarded < 1 or self.n_rewarded > self.n_pairs:
            raise ValueError("n_rewarded must be between 1 and n_pairs")
        if self.n_cues <= self.n_pairs:
            raise ValueError(
                "filler cues are drawn from the cues this sequence did not use, "
                "so n_cues must exceed n_pairs")
        if self.n_values < 2:
            raise ValueError("n_values must be at least 2")
        if self.delay < 1:
            raise ValueError("delay must be at least 1")
        if self.queries_per_reward < 1:
            raise ValueError("queries_per_reward must be at least 1")
        if self.seq_len < self.min_seq_len:
            raise ValueError(
                f"seq_len {self.seq_len} is too short for {self.n_pairs} pairs "
                f"with {self.n_rewarded} rewarded at delay {self.delay}; needs "
                f"at least {self.min_seq_len}")


@dataclass(frozen=True)
class RewardSequence:
    tokens: tuple[int, ...]
    targets: tuple[int, ...]
    bindings: dict[int, int]
    rewarded: tuple[int, ...]
    query_positions: tuple[int, ...]
    config: RewardConfig

    def position_kinds(self) -> list[str]:
        """What each position is. **AN ORACLE.**

        `"rewarded"` marks the cue of a binding that is going to be asked about
        — what a perfect gate keeps, and what nothing a deployed system can read
        would tell it.
        """
        kinds = list(self._kinds)
        return kinds

    _kinds: tuple[str, ...] = ()


def generate(config: RewardConfig, seed: int | None = None) -> RewardSequence:
    rng = random.Random(config.seed if seed is None else seed)

    cues = rng.sample(range(config.n_cues), config.n_pairs)
    bindings = {cue: config.n_cues + rng.randrange(config.n_values)
                for cue in cues}
    rewarded = rng.sample(cues, config.n_rewarded)

    # Filler is drawn from cues this sequence did not use, so it looks exactly
    # like a cue without ever being one. Drawing from the cues in use would make
    # the task ill-posed; drawing from a private range would make filler
    # trivially ignorable. Same reasoning as MQAR, and the same trap.
    spare = [c for c in range(config.n_cues) if c not in bindings]

    width = 2 if config.autoregressive else 1
    n_queries = config.n_rewarded * config.queries_per_reward
    body_len = config.seq_len - n_queries * width

    tokens: list[int] = []
    kinds: list[str] = []
    targets: list[int] = []

    # Lay the bindings out in a shuffled order across the body, each as
    # cue-then-value, with filler between them. Rewarded bindings get a reward
    # token `delay` steps after their cue.
    order = cues[:]
    rng.shuffle(order)
    reward_due: dict[int, int] = {}          # position -> reward token
    for cue in order:
        position = len(tokens)
        tokens.append(cue)
        kinds.append("rewarded" if cue in rewarded else "pair")
        targets.append(IGNORE)
        tokens.append(bindings[cue])
        kinds.append("value")
        targets.append(IGNORE)
        if cue in rewarded:
            reward_due[position + config.delay] = config.reward_token
        # Filler until the next binding, sized so the body fills evenly.
        gap = max(0, (body_len - config.n_pairs * 2) // config.n_pairs)
        for _ in range(gap):
            tokens.append(rng.choice(spare))
            kinds.append("filler")
            targets.append(IGNORE)

    while len(tokens) < body_len:
        tokens.append(rng.choice(spare))
        kinds.append("filler")
        targets.append(IGNORE)
    del tokens[body_len:], kinds[body_len:], targets[body_len:]

    # Reward tokens overwrite filler at their due position. A reward that would
    # land on a cue or a value is moved to the next filler slot rather than
    # destroying a binding -- the delay is a nominal distance, not a promise.
    for position in sorted(reward_due):
        place = position
        while place < body_len and kinds[place] != "filler":
            place += 1
        if place < body_len:
            tokens[place] = config.reward_token
            kinds[place] = "reward"

    asked = list(rewarded) * config.queries_per_reward
    rng.shuffle(asked)
    for cue in asked:
        kinds.append("query")
        tokens.append(cue)
        targets.append(bindings[cue])
        if config.autoregressive:
            tokens.append(bindings[cue])
            kinds.append("answer")
            targets.append(IGNORE)

    return RewardSequence(
        tokens=tuple(tokens),
        targets=tuple(targets),
        bindings=bindings,
        rewarded=tuple(rewarded),
        query_positions=tuple(i for i, t in enumerate(targets) if t != IGNORE),
        config=config,
        _kinds=tuple(kinds))


def dataset(config: RewardConfig, n_sequences: int) -> list[RewardSequence]:
    return [generate(config, seed=config.seed * 1_000_003 + i)
            for i in range(n_sequences)]
