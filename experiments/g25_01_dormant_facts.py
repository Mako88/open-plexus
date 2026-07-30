"""What does use-based eviction throw away? Note 083's arms, plus the one it lacks.

**`note 083` is C4's answer and it has no script.** `docs/options/use-based-eviction.md`
records `script unrecorded`, and nothing in `experiments/` or `tools/` reproduces it. So
half of this is re-deriving a headline result that has never been re-run, which rule 3
asks for anyway.

The other half is the arm the option record names as missing, in its own words:

    A useful fact nobody asks about inside its window is gone before it can be
    promoted. Every fixture measuring this policy queries the facts it cares
    about, so none of them pays that cost.

    PERSISTENT   queried throughout                note 083's arm
    ABANDONED    queried during the first quarter  note 083's arm
    DORMANT      NEVER queried during the stream   NEW

DORMANT also splits recency from frequency, which note 083 says its instrument cannot:
a dormant fact is touched exactly once, at write, so recency orders the class by write
time while frequency ties every member at one.

**No model, no training, no numpy.** This is a policy over a bounded dict, and keeping it
dependency-free means the eviction question is answered by code that can be read in one
sitting rather than by a store whose other behaviours would have to be held constant.

Predictions are registered in
`experiments/sweeps/g25-01-what-use-based-eviction-throws-away.txt`, committed at
`51b3b00` before this file existed.
"""

from __future__ import annotations

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402

#: Note 083's parameters exactly, so the control arms are comparable to it.
FACTS, SLOTS = 4000, 150
PERSISTENT = ABANDONED = DORMANT = 100
POLICIES = ("random", "least-recent", "least-frequent")


class BoundedStore:
    """A fixed number of slots, with a policy for what leaves when one is needed.

    Tracks `written`, `last_used` and `uses` per key. `last_used` starts at the write
    step and `uses` at one, because a write IS a touch — a fact that has just arrived
    is not less recently used than one that arrived before it.

    **Ties are broken by oldest write, explicitly.** Under `least-frequent` the whole
    dormant class ties at one use, so tie-breaking IS the result for that arm, and
    `g23-02` is the calibration for letting it fall out of iteration order instead:
    an unsorted set made a published number move between runs of the same seed.
    """

    def __init__(self, slots: int, policy: str, rng: random.Random) -> None:
        self.slots, self.policy, self.rng = slots, policy, rng
        self.written: dict[int, int] = {}
        self.last_used: dict[int, int] = {}
        self.uses: dict[int, int] = {}

    def _victim(self) -> int:
        keys = sorted(self.written)          # deterministic order before any choice
        if self.policy == "random":
            return self.rng.choice(keys)
        if self.policy == "least-recent":
            return min(keys, key=lambda k: (self.last_used[k], self.written[k]))
        return min(keys, key=lambda k: (self.uses[k], self.written[k]))

    def write(self, key: int, step: int) -> None:
        if key not in self.written and len(self.written) >= self.slots:
            victim = self._victim()
            del self.written[victim], self.last_used[victim], self.uses[victim]
        self.written.setdefault(key, step)
        self.last_used[key] = step
        self.uses[key] = self.uses.get(key, 0) + 1

    def query(self, key: int, step: int) -> bool:
        """True if held. A hit is a USE; a miss changes nothing."""
        if key not in self.written:
            return False
        self.last_used[key] = step
        self.uses[key] += 1
        return True


def one_cell(policy: str, seed: int) -> dict:
    rng = random.Random(seed)
    store = BoundedStore(SLOTS, policy, rng)

    persistent = list(range(PERSISTENT))
    abandoned = list(range(PERSISTENT, PERSISTENT + ABANDONED))
    dormant = list(range(PERSISTENT + ABANDONED,
                         PERSISTENT + ABANDONED + DORMANT))
    tracked = set(persistent) | set(abandoned) | set(dormant)

    step = 0
    for index in range(FACTS):
        # Tracked facts are written once, early, so every policy sees the same
        # arrival order. Filler streams past afterwards and is what forces eviction.
        key = (persistent + abandoned + dormant)[index] if index < len(tracked) \
            else len(tracked) + index
        store.write(key, step)
        step += 1

        # PERSISTENT is queried throughout. ABANDONED only in the first quarter.
        # DORMANT is never queried here at all -- that is the whole point.
        store.query(rng.choice(persistent), step)
        step += 1
        if index < FACTS // 4:
            store.query(rng.choice(abandoned), step)
            step += 1

    held = {name: sum(k in store.written for k in group) / len(group)
            for name, group in (("persistent", persistent),
                                ("abandoned", abandoned),
                                ("dormant", dormant))}
    held.update(policy=policy, seed=seed, slots=SLOTS, facts=FACTS,
                condition=f"{policy}|slots{SLOTS}|facts{FACTS}|seed{seed}")
    return held


def main() -> None:
    args = harness.parse_args(__doc__)
    seeds = (0, 1, 2) if args.seed is None else (args.seed,)
    records = [one_cell(policy, seed) for seed in seeds for policy in POLICIES]
    # `harness.emit` with no path falls through to `harness.table`, which reads an
    # `accuracy` key. There is no single accuracy here -- the whole result is the
    # THREE classes kept apart, and collapsing them to one number is precisely what
    # hid the dormant case in note 083. So emit only writes, and the table below is
    # this experiment's own.
    if args.json:
        harness.emit(records, Path(args.json))

    print(f"\n{FACTS} facts, {SLOTS} slots, {len(seeds)} seed(s)")
    print(f"{'policy':>16}{'persistent':>13}{'abandoned':>12}{'dormant':>10}")
    for policy in POLICIES:
        rows = [r for r in records if r["policy"] == policy]
        def mean(name):
            return sum(r[name] for r in rows) / len(rows)
        print(f"{policy:>16}{mean('persistent'):>13.3f}"
              f"{mean('abandoned'):>12.3f}{mean('dormant'):>10.3f}")
    # The arithmetic ceiling, printed beside the measurement rather than left for
    # a reader to derive: note 083's 0.500 on abandoned is exactly slots minus
    # persistent, over abandoned, and with three classes that changes.
    print(f"\n  slots {SLOTS} against {PERSISTENT + ABANDONED + DORMANT} tracked "
          f"facts: at most {SLOTS - PERSISTENT} non-persistent survive, i.e. "
          f"{(SLOTS - PERSISTENT) / (ABANDONED + DORMANT):.3f} of them")


if __name__ == "__main__":
    main()
