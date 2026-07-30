066 — The fold is right 98.8% of the time it can act, and tabulation caps it at 52%
==================================================================================

**Status:** measured symbolically — true chains and a lookup table, **no model
involved.** So this is a **ceiling the fold imposes**, not a result about this project's
mechanism. It corrects note 063 in two directions at once.

---

## IN PLAIN TERMS

Naming what a chain of relationships adds up to can be done by learning small rules:
*father then sister is aunt*. Learn enough of those and apply them one at a time.

**Where the rules exist, this is almost never wrong — 98.8%.** So the approach is
right.

**But only about half the test puzzles can be finished at all**, because the rules
needed are not in the training data and cannot be derived from it. Learning
*father-then-sister* does not tell you *aunt-then-brother*, and CLUTRR is built so that
the test set keeps asking for combinations training never showed.

That is the benchmark working as designed, and it means **a table of rules is not
enough however well it is learned.** Something that generalises the *shape* of
composition is required, not a bigger table.

---

## What is derivable from training, and it converges

    round 1  2-hop puzzles label (base, base) -> derived directly     62 rules
    round 2  3-hop puzzles label (derived, base) via round 1's table  +35
    round 3  nothing new                                              +0
             total                                                    97

**Note 063 was wrong that the intermediates are unlabelled.** A two-hop puzzle's answer
*is* a pairwise rule with a label, and training holds 4,076 of them yielding 62
unambiguous rules. A three-hop puzzle then labels `compose(x, c)` where `x` comes from
round 1 — so the task supplies its own curriculum and the credit-assignment problem 063
feared does not arise for this table.

**And note 063 was also too optimistic.** It reported 6.6% of test *pairs* unseen and
read that as near-full coverage. That counted the pairs CLUTRR *states*; the fold asks
for `(accumulated, next)` pairs, where the accumulated side is a **derived** relation.
Those are a different population — **120 distinct pairs asked for, 97 derivable** — and
the shortfall is what caps it.

## The measurement

    fold on the test set, bootstrapped table, true chains

    completes                603/1146    52.6%
    CORRECT                  596/1146    52.0%
    correct among completed               98.8%

    hops     n   fold accuracy
       2    38          1.000
       3   105          0.524
       4   190          0.732
       5   174          0.661
       6   107          0.542
       7   144          0.472
       8   150          0.380
       9   119          0.336
      10   119          0.218

**98.8% where it can act is the finding about the mechanism.** The fold is not
approximately right, it is right, and the 1.2% is close to the 0.42% irreducible
ambiguity note 063 measured in the chain-to-target mapping.

**52.0% overall is the finding about tabulation.** Longer chains compose more times and
so are likelier to hit a missing rule, which is the decay from 1.000 to 0.218.

> **The 3-hop cell at 0.524 is lower than the 4-hop cell at 0.732 and I have not
> explained it.** Recorded rather than smoothed over: n is 105 against 190, and it may
> be which pairs the bootstrap happened to reach, but it is not predicted by the
> account above and a story that fits every cell but one is a story with a hole.

## What this means combined with note 065

    end-task  ~  chain recovery  x  fold accuracy

Note 065 has chain recovery at **1.000 on the plain subset**. So on those rows the route
is not the limit and the fold is: a projected end-task around **52%**, bounded by rule
coverage rather than by route-finding or by the fold's own correctness.

**That reverses the bottleneck twice in one evening.** Note 063 put it on route-finding;
065 solved the route and moved it to naming; this puts it on **the rules available to
name with** — which is neither mechanism but the training data's coverage of a space
CLUTRR deliberately does not cover.

## What follows, and it is not "learn more rules"

**A table cannot generalise and CLUTRR is built to punish that.** 97 rules is all that
is derivable; the remaining pairs are absent from training directly and transitively.
Beating 52% needs composition that generalises its own *structure* — something that
knows `aunt` behaves like `parent's sister` and can compose it without having seen
`aunt-then-X` — rather than 97 independent facts.

**That is an argument for a learned representation over a lookup**, and it is the first
one in this project grounded in a measurement rather than in preference. It is also
where `hop_accumulate="bind"` was headed: a binding composes two vectors into a third by
a rule that applies to any pair, which is exactly what a table cannot do.

## What is NOT claimed

**No model was run.** True chains from the data, and a dictionary. Substituting the
model's recovered chains would multiply this by note 065's recovery, and substituting a
learned composition for the dictionary is the open work.

**And 52% is a ceiling for tabulation, not a prediction for the project.** A
generalising composition could exceed it; a worse route-finder would fall below it.
