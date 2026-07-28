# Choices that are known to depend on scale

Every measurement in this project is made at one size. Some conclusions travel
and some do not, and the ones that do not are dangerous precisely because they
look settled — a default chosen at width 64 and vocabulary 50 carries no warning
label when it is read at width 8192.

**This file is the register of those choices.** One row per decision that is
known or strongly suspected to be scale-dependent: what was chosen, at what
size, what would trigger revisiting it, and what to try instead.

Two rules that keep it honest:

1. **A scale-dependent default says so at its definition.** The config docstring
   carries the condition, not just this file, because that is where someone
   reading the code will be.
2. **A row is added when the choice is made**, not when it breaks. A register
   written after the fact is a post-mortem.

---

## The readout's pooling across groups

| | |
|---|---|
| **Chosen** | `parts.sum(0)` — every group predicts the whole vocabulary, and the predictions are summed |
| **Measured at** | width ≤ 128, vocabulary ≤ 50, ≤ 8 groups |
| **Why it may not travel** | The reduction is `nodes × vocab` numbers per answered position. At 1024 nodes and a 50k vocabulary that is ~205 MB per position if done naively |
| **What already mitigates it** | `distributed.py` sends each node's **argmax** in 4 bytes (`combine="vote"`), which is ~8 KB at 1024 nodes. g4-01 measured a single group's answer at 0.949–1.000 against a pooled 1.000, so the pooling is optional |
| **Trigger to revisit** | Any run above ~1000 nodes, or a vocabulary above ~1000 |
| **Try instead** | Partition the readout by **vocabulary** rather than width, so each node scores its own tokens from a broadcast retrieval |

## Dimensions per node

| | |
|---|---|
| **Chosen** | No constraint enforced; experiments use 8–64 per group |
| **Measured at** | g4-01, width ≤ 128 |
| **The finding** | A lone node's answer holds up at 16 dims (0.949) and degrades fast below: 8 dims → 0.681, 4 dims → 0.412 |
| **Trigger to revisit** | Any partitioning that puts fewer than ~16 dimensions on a node |
| **Consequence** | Node count is bounded by roughly `width ÷ 16`, not by anything softer |

## How a hop combines retrievals

| | |
|---|---|
| **Chosen** | `hop_accumulate="concat"` — the readout sees every hop side by side |
| **Measured at** | **16 composition rules**, 10 relations, 128-wide combined vector |
| **The finding** | concat 1.000, elementwise product 0.812, circular convolution 0.812 |
| **Why it may not travel** | Concat wins because 16 rules in a 128-wide space are linearly separable *whatever* structure the labels have. That is a property of having few rules, not of concatenation being right. A true binding degrades more gracefully as rules multiply |
| **Trigger to revisit** | A rule table in the hundreds, or a readout input narrow relative to the number of distinct compositions |
| **Try instead** | `hop_accumulate="bind"` is kept for this reason. Circular convolution is the standard VSA choice and is implemented in the same shape |

## Keys: single-token vs pair

| | |
|---|---|
| **Chosen** | `context_keys` off by default; on for relational tasks |
| **Measured at** | 14 people, 10 facts |
| **The finding** | With single-token keys, retrieval collapses once an entity appears in more than one fact — 0.884 at one appearance, 0.303 at two. Pair keys largely fix it: 0.918 / 0.628 |
| **Why it may not travel** | Pair keys separate an entity's ROLES, but an entity that appears twice in the *same* role collides again. The residual at 2+ appearances (~0.57–0.63) is that case |
| **Trigger to revisit** | Any graph where entities commonly hold the same role several times — which is most real knowledge graphs |
| **Try instead** | A key over `(role, entity, occurrence)`, or an exact store for high-degree entities |

## Gate sharpness

| | |
|---|---|
| **Chosen** | `gate_sharpness=200` |
| **Measured at** | 2 and 3 hops, 4–8 chains |
| **The finding** | 50 / 200 / 1000 all reach 1.000 at two depths; at three, 1000 loses ground (0.986) while 200 holds |
| **Why it may not travel** | A very large gain makes the hop softmax an argmax, so one mis-scored hop is taken outright rather than averaged. Deeper questions have more hops to mis-score, so the safe gain likely **falls** as depth rises |
| **Trigger to revisit** | Depths beyond 4 |

## Store capacity

| | |
|---|---|
| **Chosen** | `memory_cap=5.0` on the store's Frobenius norm |
| **Measured at** | sequences ≤ 200 tokens, width ≤ 128 |
| **Why it may not travel** | The cap bounds total stored magnitude, so the number of bindings it can hold before old ones are crowded out scales with width but the cap does not |
| **Trigger to revisit** | Sequences long enough that early bindings are unreadable by the end — testable directly by querying the first binding at the last position |

## Raw store capacity, uncapped (decision 109)

| | |
|---|---|
| **Measured** | Bindings recoverable at 90%, writing outer products directly with **no decay and no cap** |
| **The finding** | width 32 → 16 bindings; width 64 → 96; width 128 → 384. Roughly **d²**, quadrupling as width doubles |
| **What it settles** | The store is **not** the saturation bottleneck at the sizes used — tasks here write 10–30 bindings against a width-64 ceiling of ~96 |
| **Why it may not travel** | This is the ceiling *without* the model's own write path. `decay` and `memory_cap` both reduce it and neither was active |
| **Trigger to revisit** | Any task writing more than ~1 binding per dimension, or any use of the capped/decayed path at scale |
| **Measure it properly by** | Re-running the same sweep through `model.run` rather than direct outer products |
