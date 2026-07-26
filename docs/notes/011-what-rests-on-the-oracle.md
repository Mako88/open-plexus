# Note 011 — What rests on the oracle

Three of this project's best results share a dependency that is easy to lose
across forty explainers, because each states it once and then moves on. Stated
here in one place, because the dependency is load-bearing and the substitute does
not currently work.

## The three findings

| finding | what it says | arm |
|---|---|---|
| [g7-02](../../experiments/sweeps/g7-02-tiny-nodes-and-clusters.txt) | sequence length stops being a difficulty dial — identical scores at 96, 192, 288, 384 | **gated** |
| [g7-03](../../experiments/sweeps/g7-03-how-to-spend-a-machine.txt) | how a machine spends its capacity barely matters (spread 0.031 against 0.425) | **gated** |
| [g6-02](../../experiments/sweeps/g6-02-do-tiny-devices-forget.txt) | a cluster stops forgetting entirely | **gated** |

Every one of them is measured with `run(store=mask)`, where the mask comes from
`position_kinds()` — **task structure supplied from outside.** No running system
has it.

So the honest form of all three is conditional: *if* storage were selective, then
length stops mattering, allocation stops mattering, and forgetting stops. The
antecedent is not established.

## The substitute, and why it does not yet close the gap

[Note 010](010-tagging-and-capture.md) took the mechanism from synaptic tagging
and capture, and `consolidation` implements it: write everything into a fast
decaying store, and promote whatever a later confirmed retrieval used into a store
that does not decay. The signal is the model's own prediction against the token
that arrives next — local, self-supervised, no lookahead.

**It works mechanically.** On repeated questions it lifts the last answer by 0.18
while barely moving the first, which is the signature it must have and one a
wrongly-triggered gate cannot fake.

**And it loses.** Once forgetting is set to a sensible rate, consolidation is
monotonically harmful — 0.625, 0.625, 0.623, 0.603, 0.482 as the rate rises from
zero to one. The reason is structural: **the lasting store never decays, which is
what makes it lasting, so every confirmed retrieval adds to it permanently.**
Where accuracy is decent, confirmations are frequent, and the lasting store
accumulates exactly the saturation the fast store was fading to avoid. It trades
one accumulation problem for another.

## What IS implementable, and does help

**Forgetting, on its own.** At seq_len 768, a memory that fades with a half-life
of a quarter of the sequence scores **0.672** against **0.526** for one that never
fades. That is a purely local mechanism, already in the model, requiring no
signal, no oracle and no coordination.

[g1-06](../../experiments/sweeps/g1-06-interference.txt) measured decay as
unhelpful at seq_len 96 and wrote that it "may still matter for sequences long
enough that unbounded accumulation is the problem, which this task is not". The
condition is now met and the note was right.

So the ladder of what is established runs:

1. **Oracle gating** — removes the scaling problem entirely. Not implementable.
2. **Decay** — implementable, no signal needed, and it helps at long sequences.
   Bounded benefit, being measured properly in g7-04.
3. **Consolidate-on-use** — implementable, mechanically correct, and currently
   harmful.

**The gap between (1) and (2) is the honest size of what remains unsolved**, and
nothing in the project currently bridges it.

## What would

- **A lasting store that also decays, far more slowly.** The obvious fix to the
  saturation above, and untested. Biology's consolidated memories fade too.
- **A confirmation signal that fires rarely.** The mechanism fails by firing
  constantly. Gating consolidation on *surprise* rather than on success — promote
  what was retrieved when it was **unexpectedly** right — would fire on the tail
  rather than the bulk, which is closer to what neuromodulation appears to do.
- **A task where relevance is locally predictable.** On MQAR nothing at storage
  time separates a useful binding from filler, and frequency does not vary because
  keys are drawn uniformly. Real data is not uniform: a few things recur
  constantly and most never return, which makes frequency itself a local signal.
  **That is the single most promising untried direction**, and it needs a task
  change rather than a mechanism change.

## Why this note exists

Each of the three findings states its caveat once, honestly, in its own file. But
a reader assembling the project's position from GOALS would see three strong
results and one caveat, and would reasonably conclude the caveat was minor.

It is not minor. **It is the difference between a measured ceiling and a working
system**, and the one attempt to implement it made things worse.
