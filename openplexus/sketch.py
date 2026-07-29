"""Which addresses have been written, kept apart from what was written there.

Decision 147 refuted two rules for choosing between two retrievals -- the
retrieval's norm and the decode's margin -- and named why the first failed:
``||W k||`` conflates *was this key ever written* with *how large the value
stored there is*. Only the first is the question, and the second varies enough
to bury it.

The obvious repair is to sum the written keys in the store's own ``d``
dimensions and read that with a dot product. **It does not work, for a
computable reason.** Normalised near-orthogonal keys have pairwise cosine of
order ``1 / sqrt(d)``, so a sum of ``N`` of them reads back at the true address
as ``1`` with a cross-talk term of standard deviation ``sqrt(N / d)``. At the
``d = 64`` and ``N ~= 100`` the families task runs at, the noise is *larger than
the signal*, and a comparison against the largest of several neighbours is worse
still because a maximum selects for the noise.

The collision rate of that sketch is chained to the store's width, and it should
not be. **Membership is one bit; a value is ``d`` floats.** Bloom (1970) is the
standard statement of exactly that asymmetry, and Charikar (2002) supplies the
piece needed here -- a hash whose collision probability follows the *angle*
between two vectors, so a sketch can be addressed by a key vector without any
token identity being handed to it.

    signature(k) = the sign pattern of `b` random hyperplanes applied to k

Two keys share a signature with probability ``(1 - theta/pi) ** b``. For the
near-orthogonal keys this model uses, ``theta ~= pi/2``, so collisions fall as
``2 ** -b`` -- set by ``bits``, and free of ``d`` entirely. Sixteen bits put the
collision rate at 1.5e-5 for the cost of one ``16 x d`` matrix-vector product per
write.

**The honest cost.** This is a second memory, and it is not superposed. It does
not share the store's capacity and so it does not inherit the store's failure
modes, which is the point and also the objection: a mechanism that keeps an
exact side table has stepped outside the thing being studied. What justifies it
is the asymmetry above and nothing else -- the sketch may record *that* an
address was written and must never record what went there. If it ever carries a
value, it has become a second store and the comparison it enables is worthless.
"""

from __future__ import annotations

import numpy as np


class SumSketch:
    """The obvious version: add the written keys up in the store's own space.

    Kept, and kept first, because it is the one that fails and the reason it
    fails is the argument for the other. `count` reads back the number of writes
    at an address plus a cross-talk term of standard deviation `sqrt(N / d)`, so
    it is usable only while writes stay well under the store's width -- which is
    not the regime any of these tasks run in.

    Same interface as `AddressSketch`, so the two are one constructor apart and
    an experiment can measure the difference rather than argue about it.
    """

    def __init__(self, width: int) -> None:
        self._occupied = np.zeros(width)

    def add(self, key: np.ndarray, amount: float = 1.0) -> None:
        length = float(np.linalg.norm(key))
        if length > 0.0:
            # NORMALISED, so a write contributes 1.0 to its own address whatever
            # the key's scale. Without this, `key_scale` would silently set how
            # much each write counts for.
            self._occupied += amount * key / length

    def count(self, key: np.ndarray) -> float:
        length = float(np.linalg.norm(key))
        return 0.0 if length == 0.0 else float(self._occupied @ key) / length

    def decay(self, factor: float) -> None:
        self._occupied *= factor


class AddressSketch:
    """Counts writes per address, blind to what was written.

    Addressed by the key vector, so nothing here needs a token id and the seam
    to `KeySource` stays closed.

    **Decay is a scale, not a sweep.** The fast store fades every step, and a
    sketch that did not fade with it would defend a binding that has already
    decayed away. Multiplying every bucket per step would cost a pass over the
    table, so the fade is carried as one number and folded into reads instead --
    which is exact rather than approximate, and O(1).
    """

    def __init__(self, width: int, bits: int = 16, seed: int = 0) -> None:
        if bits < 1:
            raise ValueError("bits must be at least 1")
        if bits > 62:
            # The signature is packed into a Python int via shifts; past this
            # the arithmetic is still correct but the buckets outnumber any
            # plausible number of writes, which means every address is unique
            # and the sketch has stopped compressing anything.
            raise ValueError("bits above 62 buys nothing: the table would be "
                             "sparser than the writes it records")
        self._planes = np.random.default_rng(seed).normal(
            size=(bits, width)) / np.sqrt(width)
        self._counts: dict[int, float] = {}
        # Everything is stored in units of `self._scale`, which shrinks as the
        # store decays. A write divides by it and a read multiplies by it, so
        # old writes fade at exactly the store's rate without being touched.
        self._scale = 1.0

    def signature(self, key: np.ndarray) -> int:
        bits = self._planes @ key > 0.0
        value = 0
        for bit in bits:
            value = (value << 1) | int(bit)
        return value

    def add(self, key: np.ndarray, amount: float = 1.0) -> None:
        if self._scale <= 0.0:
            return
        where = self.signature(key)
        self._counts[where] = self._counts.get(where, 0.0) + amount / self._scale

    def count(self, key: np.ndarray) -> float:
        """How much has been written at this address, after decay."""
        return self._counts.get(self.signature(key), 0.0) * self._scale

    def decay(self, factor: float) -> None:
        self._scale *= factor
        if self._scale < 1e-12:
            # The alternative is unbounded growth in the stored magnitudes, and
            # at this point every recorded write has faded to nothing anyway.
            self._counts.clear()
            self._scale = 1.0
