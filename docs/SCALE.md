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

## Readout capacity, and where it crosses the store (decision 110)

| | |
|---|---|
| **Measured** | Random `(retrieval → answer)` pairs a readout can learn, no store and no task |
| **The finding** | Linear readout holds **2.00 items per dimension at every width** — 64, 128, 256 at widths 32, 64, 128. A hidden readout holds everything tested |
| **Why it matters at scale** | The readout grows **linearly** where the store grows **quadratically** (0.50 → 1.50 → 3.00 per dimension). They cross near **width ~100**, and above that the linear readout is the binding constraint — doubling width doubles it while quadrupling the store |
| **Trigger to revisit** | Any width above ~100 with a linear readout |
| **What to do then** | `hidden` — which is also what decision 83 measured as the largest single factor on text |
| **What it does NOT say** | That either is what saturates today. At widths 64–128 both exceed task demand, so decision 63 is not a capacity limit |

## Raw store capacity, uncapped (decision 109)

| | |
|---|---|
| **Measured** | Bindings recoverable at 90%, writing outer products directly with **no decay and no cap** |
| **The finding** | width 32 → 16 bindings; width 64 → 96; width 128 → 384. Roughly **d²**, quadrupling as width doubles |
| **What it settles** | The store is **not** the saturation bottleneck at the sizes used — tasks here write 10–30 bindings against a width-64 ceiling of ~96 |
| **Why it may not travel** | This is the ceiling *without* the model's own write path. `decay` and `memory_cap` both reduce it and neither was active |
| **Trigger to revisit** | Any task writing more than ~1 binding per dimension, or any use of the capped/decayed path at scale |
| **Measure it properly by** | Re-running the same sweep through `model.run` rather than direct outer products |

## The decode margin as an ambiguity signal (decision 129)

| | |
|---|---|
| **Chosen** | Gate search on `search.decode_margin` — the gap between the first decode's top two candidates |
| **Measured at** | g13-04, kinship hops 2, widths 64/128/256, 8 seeds |
| **The finding** | AUC separating out-degree 1 from 2+ is **0.710 / 0.841 / 0.858** at d64 / d128 / d256 — it strengthens monotonically with width |
| **Why it moves with scale** | A wider store holds a cleaner superposition. The out-degree-2+ median margin *falls* (0.235 → 0.147 → 0.118) while the out-degree-1 median *rises* (0.538 → 0.650 → 0.769), so both sides of the separation improve together |
| **Trigger to revisit** | Any use below width 128, where it drops under the 0.75 usability bar; and any change to the key scheme, since the margin is a property of how bindings superpose |
| **What to do then** | Widen, or find a signal that does not depend on the store being clean. The endpoint margin is **not** the fallback — g13-04 measured it below chance at every width |
| **What it does NOT say** | That the gate helps. This is the signal's separability, not the mechanism's accuracy |

## `d_max` — the asynchrony bound and the churn timeout (decision 128)

| | |
|---|---|
| **Chosen** | `RETRY_AFTER_SECONDS = 0.64`, and with it C2's stated bound |
| **Measured at** | g12-04 — 4 nodes, width 16, 40 steps, window 4, on a Docker bridge with `tc netem`. Six links from clean to 80 ms delay + 20 ms jitter + 2% loss |
| **The finding** | Vote round trip p99 runs **2.54 ms** clean and **211.88 ms** on the worst link. SWIM requires the period to be ≥ 3× the round-trip estimate, giving **636 ms** |
| **Why it may not travel** | Every link here is a container bridge on one host with impairment *simulated*. Intercontinental paths, mobile networks, congested consumer uplinks and NAT traversal are all outside the grid, and each raises it. **It is a floor, not a constant** |
| **Trigger to revisit** | Any run over a real WAN; any node count above the four tested; any link worse than 80 ms / 20 ms / 2% |
| **What to do then** | Re-run `sweep-g12-04` on the new links and take 3 × p99 again. The instrumentation (`Network.vote_latencies`) is permanent, so this is a re-measurement rather than a rebuild |
| **What it does NOT say** | That 640 ms is right for a *deployed* system. It is the wait a driver must tolerate on these links, and note 039's remaining gaps — no probe channel, no indirect probing, a single detector — all still stand |

> **Two shape facts from the same sweep, worth carrying wherever this number
> goes.** Quote the **p99 − p50 gap**, not the p99/mean ratio: once a fixed delay
> dominates, mean and p99 converge (1.01× at delay 80 ms) while the gap keeps
> growing (1.0 → 16.0 → 124.7 ms as jitter then loss are added). And **loss is
> multiplicative with delay, not additive** — 2% loss alone is invisible, but the
> same 2% on an 80 ms link doubles the p99, because a retransmit costs a round
> trip.

## How many nodes, and the decision currently capping it (2026-07-30)

**John asked whether any decision already made will not scale.** One will, and it is the
partitioning.

| | |
|---|---|
| **Chosen** | Split by DIMENSION. `concept_nodes` is 0, so concept splitting is built and off |
| **The ceiling** | Node count is bounded by `width ÷ 16` (the row above). **At the current width 256 that is SIXTEEN NODES.** A thousand nodes would need width 16,000, paid for solely to have somewhere to put them |
| **Why it is not softer** | A lone node's answer holds at 16 dimensions (0.949) and collapses below — 0.681 at 8, 0.412 at 4. Under dimension splitting, growing the network makes every node's view thinner while the total stays the same, so **a node can never answer alone however large the system gets** (decision 134) |
| **The fix, already built** | Concept splitting. Lone-node capacity 2048 against 128 at sixteen nodes, and it **grows with the network** where dimension splitting is flat forever. It also makes the beam's reads point-to-point rather than collective, and makes the global readout a selection instead of the sum C1 forbids |

**And the arithmetic for the target scale**, from `0.023·d²` bindings (the row above:
width 32 → 16, 64 → 96, 128 → 384). **Total store is invariant at ~170 GB; the SCHEME
decides how it is sliced, and the first version of this section mixed the two** — it did
dimension-partitioning arithmetic while recommending concept partitioning, which do not
compose:

    scheme        relations held      width     nodes   per node    msg size
    dimension     1e9 (Wikidata)    208,000    13,000      13 MB      832 KB
    CONCEPT       1e9 (Wikidata)         512   167,000       1 MB        2 KB

**And this is a third argument for concept splitting, stronger than the two above.** A
message carries a `d`-wide vector, so **bandwidth scales with width** — and dimension
splitting has to *grow* `d` to buy capacity while concept splitting keeps it fixed and adds
nodes:

    beam at ~160 reads per query (width 4 x branches 4 x depth 10)

    d = 208,000   832 KB per vector   ~266 MB per query   INFEASIBLE
    d = 512         2 KB per vector    ~640 KB per query   ~50 Mbit/s at 10 q/s

**The binding constraint is hop LATENCY, not throughput or memory.** The beam's hops are
sequential — hop 4 cannot start before hop 3 returns — so ten hops at ~50 ms round trip is
**~500 ms against C2's `d_max` of 640 ms**, about 20% headroom. That is very likely where
640 ms came from, and it means **anything reducing the number of SEQUENTIAL hops is worth
more than anything parallelising within a hop.**

**Node count is not device count.** A node is a process; 170 GB fits one used dual-Xeon
server with 256 GB, so the cheapest full-scale run is one machine, not 167,000. Physical
devices are for the properties that only exist over a real network — churn, `d_max`, the
ring settling without a coordinator — and 20-50 suffice for that.

> **⚠ This extrapolates `d²` three orders of magnitude past the measured range (d ≤ 128),
> which is further than any other row here reaches.** It is also the UNCAPPED, no-decay
> ceiling — `decay` and `memory_cap` both reduce it and nobody has measured by how much.
> **Capacity is not capability:** holding 1e9 bindings says nothing about reasoning over
> them. The message-size arithmetic is a floor that assumes one `d`-wide vector each way
> and no compression, and **G4 has not been run at any of these widths.** Treat all of it
> as an order-of-magnitude check on whether the goal is reachable, never as a specification.

**On comparing to an LLM:** not well-posed for the primary goal, since GOALS §2 makes
next-token prediction a non-goal and there is no shared axis. The storage comparison
(170 GB against a frontier model's ~2 TB) is apples to oranges. **The answerable questions
are how much memory and how many hops**, and they are above.
