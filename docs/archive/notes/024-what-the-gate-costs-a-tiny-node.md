# 024 — What the gate costs a tiny node

**Status:** arithmetic, in `tools/gate_cost.py` and pinned by
`tests/test_gate_cost.py`. Not a measurement — nothing here was run.
**Applies to:** the reward window ([g9-02](../../experiments/sweeps/g9-02-a-gate-that-reads-its-own-input.txt),
[g9-03](../../experiments/sweeps/g9-03-is-the-cliff-reach-or-cost.txt)) and the
tag ([g9-05](../../experiments/sweeps/g9-05-a-tag-that-fades.txt),
[g9-06](../../experiments/sweeps/g9-06-is-the-tag-capacity-starved.txt)), which
share the mechanism this note costs.

---

## IN PLAIN TERMS

Every result in this line of work says how *well* a gate does. None of them says
what it costs the machine running it, and this project exists for machines that
have almost nothing.

A gate that reacts to a late signal has an awkward requirement: it has to keep a
list of what it has stored recently, so it can take back the parts nothing turned
out to vouch for. That list is a real cost, and — unlike the memory itself — it
does not get smaller when the machine is smaller.

So there is a size below which a device spends more on the bookkeeping than on
the memory it is keeping books about. This works out where that is. The answer is
that it is very small, and that it is only very small because of a decision made
for an unrelated reason two months ago.

---

## The shape

The signal arrives *after* the binding, so the node writes everything and undoes
what nothing vouched for. Undoing needs a record of the writes — `pending` in
`local_memory.py` — and that is one entry per write since the last reward,
**whatever the node's width**.

The store it gates is `w × d` numbers: this node's own rows, all columns. That
does scale with width. So the two cross.

## Two implementations, and which is cheaper flips

**SUBTRACT**, which is what the model does: keep every write since the last
reward, and at capture subtract the unmarked ones.

**REBUILD**: keep a scratch store for the interval, and at capture discard it and
re-add only what was marked, regenerating each write from its token.

At `d = 256`, interval 186 writes, a tag of 32 slots:

| width | store | SUBTRACT | REBUILD | SUBTRACT / store |
|---:|---:|---:|---:|---:|
| 1 | 256 | 372 | **320** | 1.45× |
| 8 | 2048 | **372** | 2112 | 0.18× |
| 64 | 16384 | **372** | 16448 | 0.02× |

SUBTRACT's cost does not grow with width; REBUILD's does not grow with the
interval. They cross at `w = 2·interval / d`, which is **1.45** here.

**So the answer to "can a tiny node afford this" is yes, and only just at the
smallest size.** A width-1 node at `d = 256` pays 320 numbers for the gate
against 256 for its memory — about as much again. At width 8 it pays 18%, and at
`d = 1024` even a width-1 node pays 36%.

That is a real cost and it is not a blocker. It is also the first time any g9
result has been costed at all.

## The dependency, which is the point of writing this down

Without `derived_keys` the same table reads:

| width | store | SUBTRACT | ratio |
|---:|---:|---:|---:|
| 1 | 256 | 47,988 | **187×** |
| 8 | 2048 | 49,290 | 24× |
| 64 | 16384 | 59,706 | 3.6× |

A pending entry must then carry the **full key**, because retrieval sums over
every dimension. So the cost stops depending on this node's width and starts
depending on the whole network's, and even a wide node pays more for the gate
than for its own slice of the store.

[Note 015](015-we-implemented-the-tag-and-not-the-competition.md) recorded exactly
this dependency for competitive capture and called it out as worth naming:
*"competitive capture is not independently implementable. It rests on derived
keys, and if that ever has to be withdrawn this goes with it."*

**It is now true of the whole g9 line.** The reward window, the tag, and anything
built on either rest on [note 012](012-broadcast-the-token.md)'s result that a
node need not store the key table. That was adopted to remove the width term from
the *bandwidth* cost. It turns out to be load-bearing for a second, unrelated
reason, and nothing said so until now.

## What this does not say

- **Nothing here was measured.** It is arithmetic over the model's own data
  structures, which is why it lives in a tool with tests rather than in prose —
  note 015's first cost model was hand-done and wrong in the direction that
  flattered the mechanism.
- **`interval` is a property of one task.** 186 writes between captures is
  `reward_recall` at `seq_len` 768 with four rewards. A task with rarer rewards
  makes SUBTRACT worse and moves the crossover up.
- **It costs the gate, not the gain.** Whether +0.16 recovery is worth 1.25× the
  store on a width-1 node is a judgement, and it needs the recovery figure at
  that width — which no sweep has run. Every g9 cell is `d_model` 32 in one
  process.

---

*Related: [015 — competition for a finite pool](015-we-implemented-the-tag-and-not-the-competition.md),
[012 — broadcast the token](012-broadcast-the-token.md),
[023 — two signals](023-two-signals-and-only-one-of-them-is-about-value.md).*
