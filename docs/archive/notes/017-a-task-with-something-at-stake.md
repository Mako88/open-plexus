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

## The design as first written, and why it did not survive contact

The first version had no cues. A sequence presented items, one was followed by a
reward token, and a query asked which item the reward had followed:

    item_7  item_3  REWARD  item_9  item_2  ...  QUERY -> item_3

**Reading the first generated sequence killed it.** With one rewarded item every
query is the same token, and this memory is keyed on the current token — so
`QUERY` retrieves whatever was last bound to `QUERY`, and the second and third
queries are answerable by repetition rather than recall. It would have inflated
every number the task ever produced.

## The design that was built: reward-gated cued recall

Cue→value bindings, some followed after a **delay** by a reward token. Only
rewarded cues are ever queried.

    b→7   f→3   REWARD   k→9   c→2   REWARD   m→5  ...  f -> 3     c -> 2

- **Bindings** are cue→value pairs, most never rewarded.
- **REWARD** is a distinct token, in the stream, received like any other input.
- **Queries** present a cue and ask for its value — and only rewarded cues are
  ever asked, so the task is to work out from a late signal which bindings will
  be needed.

Distinct cues are what fixes the repetition hole: each query is a different
token, so nothing carries over from the last answer.

Filler is drawn from cues the sequence does **not** bind, so a filler token can
never be byte-identical to a query needing a different answer — the same trap
MQAR's `spare_keys` exists to avoid, found there the same way.

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

Guessing uniformly among values: `1 / n_values`. Computable, and printed beside
every number this task produces.

### The difficulty dials

- **delay** between a binding and its reward — the core dial, and the one MQAR
  has no analogue for
- **reward rate**, `n_rewarded / n_pairs` — the base rate note 013 blamed and
  g8-02 could not move, which is a dial here rather than a fixed property of the
  generator
- **sequence length**, **pair count** and **alphabet sizes**, as now

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

## The first configuration failed its own answerability check

Written after the design above, and left here rather than folded into it, because
the failure is more instructive than the design.

The generator was built, its tests passed, and then
`experiments/g9_01_answerable.py` ran the check
[note 006](006-verifying-the-reservoir-claims.md) exists to demand:

    trivial floor  0.125     frozen 0.000     trained-ungated 0.999     ORACLE 1.000

**An ungated model — no selectivity of any kind, storing every consecutive pair —
scored 0.999 against the oracle's 1.000.** There was nothing for a gate to
recover. Every arm of every sweep would have scored 1.000, and the result would
have been a clean flat line meaning nothing.

Two causes, and the second is a lesson repeating itself:

1. **The memory was never under load.** `d_model` 64, no decay, 8 pairs over 192
   steps. Nothing had to be forgotten, so nothing had to be chosen. The whole
   reason selectivity matters is `SNR = sqrt(d / N)`, and this configuration sat
   nowhere near the part of that curve where `N` hurts.

2. **Repeat queries answer themselves.** In autoregressive mode the answer
   follows the query in the stream, so the *first* query of a cue re-binds it.
   With two rewarded cues asked three times each, four of six queries were about
   a binding rewritten a few steps earlier. **This is the same trap that killed
   the first design of this task**, in a different costume: there it was one
   repeated query token, here it is a repeated query *subject*.

The fix is not a better number, it is a configuration under load plus a scoring
split: accuracy is now reported separately for **first asks** and for repeats,
because a repeat measures short-term echo and a first ask measures retention.
Those are different quantities and averaging them hides the one that matters.

**Also worth recording: `frozen 0.000` is not a floor.** An untrained model has
`wo = 0`, scores every token zero and predicts token 0 forever — which is a cue,
never a value, so it is wrong by construction. It is a degenerate model rather
than a fair baseline, and reporting it as "below the floor" would be a third
instance of the same mistake this project keeps finding.

## Status

**Design implemented, first configuration REFUTED by its own control, search for
a workable one in progress. No sweep designed, no predictions registered.** The
design above was written before the generator existed; this section was written
after the control ran, and the order is preserved deliberately.

The risk worth naming in advance: **a reward token is trivially detectable**, so a
gate could learn "store the thing before the obvious marker" without learning
anything about value. If it works instantly and completely, that is the first
thing to suspect, and the delay dial is what makes it non-trivial — a marker
twenty steps later is not a marker the storage decision can wait for.
