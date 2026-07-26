# Note 017 — A task with something at stake

MQAR is still the only task this project has. It has been modified twice —
recurrent queries, then Zipfian filler — and never replaced. Five sweeps and
every negative result about gating come from one benchmark.

[Note 016](016-who-supplies-relevance.md) names the specific thing MQAR cannot
do, and it is not a matter of difficulty:

> **There is no extrinsic value in a sequence of random symbols.** Nothing in it
> is good or bad for anything. So any signal identifying which positions matter
> *is* `position_kinds()` however it is dressed up, and the difference between "an
> API told us" and "the agent's own value system told us" is untestable here.

That difference is the whole AGI question. So the replacement task has to have
something at stake.

## What the task must have

1. **Extrinsic value, carried in the input.** Not metadata, not a mask — a signal
   the agent genuinely receives, that says *that mattered*, and that is not
   recoverable from the statistics of the rest of the stream.
2. **The value arrives AFTER the thing it is about.** This is the whole point of
   tagging and capture: tag now, cheaply; a later signal decides survival. MQAR
   has no later signal, which is why `queries_per_pair` had to be invented to test
   the mechanism at all, and even then the "later signal" was just another query.
3. **Small.** Tiny nodes are the figure of merit. A task needing a large
   vocabulary or long contexts tests something else.
4. **A computable trivial floor**, or every result is unanchored.
5. **Recall-shaped, not policy-shaped.** The model does next-token prediction with
   a local delta rule. It has no action selection and no policy gradient, and a
   task requiring those would be measuring their absence.

## The design: cued recall with delayed reward

A sequence presents items. A few are followed by a **reward token**. Later, a
query asks which item the reward followed.

    item_7  item_3  REWARD  item_9  item_2  item_5  REWARD  item_1  ...  QUERY -> ?

- **Items** are drawn from an alphabet, most of them never rewarded.
- **REWARD** is a distinct token, in the stream, that the agent receives like any
  other input.
- **The query** asks for a rewarded item.

### Why this is not the oracle wearing a hat

The reward token is **in the data**, not in the metadata. A mechanism that learns
*store what precedes reward* is learning from its own input, which any deployed
agent has. `position_kinds()` is a property of the generator that no running
system can read; a reward signal is something an agent is actually given.

That distinction is the entire content of note 016, and this task is the first
one here that can hold the two apart.

### Why it fits tagging and capture exactly

This is the paradigm the borrowed mechanism was described for. The item arrives
and is tagged. The reward arrives **afterwards** and decides whether the tag is
captured. Lehr et al.'s protein signal is late, and it is the lateness that
matters. **MQAR never had a late signal, so the mechanism has never been tested in
the form the paper describes it** — which is a considerably better explanation
for four negative results than "the mechanism does not work".

### The trivial floor

Guessing uniformly among items that appeared: `1 / n_items`. Guessing among the
rewarded ones requires already knowing which they are, which is the task. The
floor is computable and must be printed beside every number, as with MQAR.

### The difficulty dials

- **delay** between an item and its reward — the core dial, and the one MQAR has
  no analogue for
- **reward rate** — how many of the items are rewarded, which sets the base rate
  that note 013 blamed and g8-02 failed to move
- **sequence length** and **item alphabet size**, as now

## What it can settle that MQAR cannot

- **Whether a late extrinsic signal makes capture work.** The direct test of
  tagging and capture in its native form.
- **Whether relevance from a value channel differs from relevance inferred from
  statistics** — note 016's (a) versus (b).
- **Whether the base-rate problem is really about base rates**, since reward rate
  is a dial here rather than a fixed property of the generator.

## What it cannot settle, and must not be claimed to

- **Anything about acting.** There is no policy, no choice, no consequence for
  being wrong. Calling this a reinforcement task would be an overstatement: it is
  *recall cued by a reward signal*, which is one component of one.
- **Anything about language.** Still synthetic, still a small alphabet. The
  corpus benchmark is a separate problem and is currently blocked on the collapse
  finding from g8-02.

## Status

**Design only. Nothing implemented, nothing measured, no predictions registered.**
Written before the generator exists so the order is checkable.

The risk worth naming in advance: **a reward token is trivially detectable**, so a
gate could learn "store the thing before the obvious marker" without learning
anything about value. If it works instantly and completely, that is the first
thing to suspect, and the delay dial is what makes it non-trivial — a marker
twenty steps later is not a marker the storage decision can wait for.
