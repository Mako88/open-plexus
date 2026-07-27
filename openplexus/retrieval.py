"""How the store is READ, as a component you can replace.

The second seam, and `openplexus/keys.py` says why there is a first: pinning down
"a model needs component X at performance Y" should not rule out a component Z
nobody has thought of yet, and the defence is to make replacing one cheap.

**Retrieval is the seam that matters most right now.** Everything this project
has refuted refuted for one reason:

    r = M @ key   is a SUM, and nothing applied after a sum recovers what the
                  sum destroyed.

Readout bias, competitive retrieval and orthogonal updates each failed there.
g11-05 then measured the consequence on a second axis: sixteen times the training
text moves our loss by 0.012 bits against a backprop control that moves cleanly,
so the architecture is saturated on data as well as width. **The sum is the
prime suspect, and a suspect you cannot swap out is a suspect you cannot test.**

## The seam

    begin(width)                        a run is starting; forget per-run state
    read(readable, key) -> vector       what the store returns for this key
    observe(store, key, value, commitment)   a binding was just written

`read` is the whole point. `begin` and `observe` exist because a retrieval
strategy may hold state of its own — the exact cache does — and a strategy that
could only read would be limited to functions of the superposed store, which is
the very thing under suspicion.

**`observe` is deliberately told the store BEFORE the write.** The cache admits
by what the superposed store failed to absorb, which is a fact about what the
store knew, not about what it is about to be told.

## Composition rather than flags

The three strategies compose, and the nesting is the order the operations
happened in when they were inline:

    SettlingRead(ExactCache(SuperposedRead(), ...), steps)

That is not decoration. `cache_slots`, `cache_sharpness`, `cache_weight` and
`retrieval_steps` were four config fields and two branches inside a 584-line
method; they are now three objects, and a fourth strategy costs a class in this
file rather than a fifth field and a third branch.

## What is deliberately NOT in the interface

No access to the readout, the targets, or the token sequence. A retrieval sees a
store and a key. That is the C1 locality constraint written into a type: a node
holding only its own slice of the store can implement anything here, and a
strategy needing a global statistic could not.
"""

from __future__ import annotations

from typing import Protocol, runtime_checkable

import numpy as np


@runtime_checkable
class Retrieval(Protocol):
    """Reads a vector out of the store for a key."""

    def begin(self, width: int) -> None:
        """A run is starting. Discard any per-run state.

        Called once per `run`, not once per position. A strategy holding state
        across runs when the model does not would make a sequence's result
        depend on what ran before it, which no test would catch and every
        measurement would inherit.
        """
        ...

    def read(self, readable: np.ndarray, key: np.ndarray) -> np.ndarray:
        """What the store returns for `key`.

        `readable` is the store as it stands at this position, including any
        consolidated `lasting` component already summed in.
        """
        ...

    def observe(self, store: np.ndarray, key: np.ndarray, value: np.ndarray,
                commitment: float) -> None:
        """A binding of `key` to `value` was just written to `store`.

        `store` is the state BEFORE the write. `commitment` is the write gate --
        how much of the binding was actually committed -- so a strategy scoring
        novelty can weight it by novelty TIMES commitment rather than novelty
        alone, a distinction HOLA ablated and found to matter.
        """
        ...


class SuperposedRead:
    """`readable @ key`. The original, and the one under suspicion.

    Every value ever written, weighted by how much its key overlaps this one,
    added together. Nothing selects; the sum has already been taken by the time
    anything downstream sees it.
    """

    def begin(self, width: int) -> None:
        pass

    def read(self, readable: np.ndarray, key: np.ndarray) -> np.ndarray:
        return readable @ key

    def observe(self, store: np.ndarray, key: np.ndarray, value: np.ndarray,
                commitment: float) -> None:
        pass


class ExactCache:
    """Keeps some bindings SEPARATELY, so a softmax over them can select.

    **The only place in this model where a selective read is possible.** Over a
    superposed store a softmax could only rescale an average that had already
    been taken; over entries that still exist apart it chooses between them.

    Admission is by what the superposed store failed to absorb: the residual
    `‖value − store @ key‖`, taken before the write, scaled by the write gate.
    Near zero when the store already answers that key correctly, large when it
    does not.

    The read is gated by how well the best entry matches, because a softmax
    returns a convex combination whatever it is given -- without the gate the
    cache contributes a full-magnitude vector even when the query matches
    nothing it holds, which is noise by construction. `test_exact_cache.py`
    caught exactly that: an ungated cache made synthetic recall WORSE while
    still helping on text.
    """

    def __init__(self, inner: Retrieval, slots: int, sharpness: float,
                 weight: float) -> None:
        self.inner = inner
        self.slots = slots
        self.sharpness = sharpness
        self.weight = weight
        self.begin(0)

    def begin(self, width: int) -> None:
        held = max(self.slots, 1)
        self.key = np.zeros((held, width))
        self.value = np.zeros((held, width))
        self.score = np.zeros(held)
        self.inner.begin(width)

    def read(self, readable: np.ndarray, key: np.ndarray) -> np.ndarray:
        retrieved = self.inner.read(readable, key)
        live = self.score > 0.0
        if not live.any():
            return retrieved
        # Cosine rather than raw dot product: a softmax over unscaled
        # similarities is nearly uniform, which is the soft-averaging failure
        # `sharpness` exists to prevent.
        sizes = np.linalg.norm(self.key, axis=1)
        cosine = (self.key @ key) / np.maximum(
            sizes * np.linalg.norm(key), 1e-12)
        logits = np.where(live, cosine * self.sharpness, -np.inf)
        weights = np.exp(logits - logits.max())
        weights /= weights.sum()
        match = float(np.max(np.where(live, cosine, -1.0)))
        if match <= 0.0:
            return retrieved
        return retrieved + self.weight * match * (weights @ self.value)

    def observe(self, store: np.ndarray, key: np.ndarray, value: np.ndarray,
                commitment: float) -> None:
        residual = float(np.linalg.norm(value - store @ key)) * commitment
        weakest = int(self.score.argmin())
        if residual > self.score[weakest]:
            self.key[weakest] = key
            self.value[weakest] = value
            self.score[weakest] = residual
        self.inner.observe(store, key, value, commitment)


class SettlingRead:
    """Map the retrieval back through the store and read again. **REFUTED.**

    The idea was that a value matching on both key AND content survives where
    one matching on neither fades. It does not work here and the reason is
    structural rather than a tuning failure: settling is for AUTO-associative
    memories, and this store is hetero-associative, so iterating is power
    iteration onto the dominant singular direction and **forgets the query**.
    Measured at 0.924 -> 0.128.

    Kept behind the seam rather than deleted, because a refuted mechanism with a
    known number attached is how the next person avoids re-proposing it, and
    `retrieval_steps` of 1 -- the default -- is exactly the identity.
    """

    def __init__(self, inner: Retrieval, steps: int) -> None:
        self.inner = inner
        self.steps = steps

    def begin(self, width: int) -> None:
        self.inner.begin(width)

    def read(self, readable: np.ndarray, key: np.ndarray) -> np.ndarray:
        retrieved = self.inner.read(readable, key)
        for _ in range(self.steps - 1):
            back = readable.T @ retrieved
            size = float(np.linalg.norm(back))
            if size <= 0.0:
                break
            retrieved = readable @ (back / size * np.linalg.norm(key))
        return retrieved

    def observe(self, store: np.ndarray, key: np.ndarray, value: np.ndarray,
                commitment: float) -> None:
        self.inner.observe(store, key, value, commitment)


def build(config) -> Retrieval:
    """The strategy a config asks for, composed in the order the code had.

    Nesting order is load-bearing and matches what was inline: read the store,
    then add the cache's selective contribution, then settle over the total.
    """
    retrieval: Retrieval = SuperposedRead()
    if config.cache_slots:
        retrieval = ExactCache(retrieval, config.cache_slots,
                               config.cache_sharpness, config.cache_weight)
    if config.retrieval_steps > 1:
        retrieval = SettlingRead(retrieval, config.retrieval_steps)
    return retrieval
