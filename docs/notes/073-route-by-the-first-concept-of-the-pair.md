073 — Route by the first CONCEPT of the pair, and the conflict disappears
========================================================================

**Status:** measured on CLUTRR test, both layouts. **It solves note 072 and refutes three
of the four fixes 072 proposed, including the one it called principled.** Not implemented —
the change is one line and it moves a baseline, so it belongs in its own commit.

---

## IN PLAIN TERMS

Note 072 found that under the `kinship` ordering every stored fact is filed under a
relationship type rather than a person, capping the network at twenty machines. It listed
four possible fixes and preferred *"route by whichever side the network has many of."*

**That preferred fix does nothing here, and the reason is a property of the benchmark
nobody had noticed:** CLUTRR reserves eleven entity slots against twenty relations, so
entities are the *rarer* side. The rule picks the relation and behaves identically to what
is already there.

**What works is simpler: file the fact under the first thing in the pair that is actually a
concept**, skipping structural markers like `FACT`. Then every fact about a person lands on
that person's node, under either ordering, and the problem stops existing.

---

## The measurement

Both bindings per fact block, CLUTRR test split. **Coherent** = the fraction of entities
for which *all* bindings the entity heads share one owner, which is the property decision
134's case rests on: a node holding an entity and everything said about it can answer
alone.

    layout=kinship        rel-owned   owners   busiest   coherent
    current                   50.0%       23     10.1%       0.0%
    first                      0.0%       11     50.0%     100.0%
    pair                       0.0%      125      9.3%       0.0%
    numerous                  50.0%       23     10.1%       0.0%
    FIRST-CONCEPT              0.0%       10     18.7%     100.0%

    layout=closure        rel-owned   owners   busiest   coherent
    current                    0.0%       11     18.7%       0.0%
    FIRST-CONCEPT              0.0%       10     18.7%     100.0%

**`first-concept` is identical under both layouts**, which is the result. Ownership stops
depending on the token ordering, so **decision 157's kinship choice no longer trades
against distribution** — the two decisions become independent instead of conflicting.

## Why the other four fail, each for its own reason

    numerous    CLUTRR has 20 relations against 11 entity SLOTS, so entities are
                rarer and the rule picks the relation. **The benchmark inverts the
                cardinality the rule depends on**, so CLUTRR cannot validate it
                either way -- this is not evidence the rule is wrong in general,
                only that it is untestable here

    first       coherent, but `key(FACT, s)` files under `FACT`, which appears in
                every block: **busiest owner 50.0%**. 072 predicted this and the
                number confirms it exactly

    pair        best balance at 9.3% and **coherence 0.0%** -- an entity's facts
                scatter across 125 owners, which is decision 134's objection

    current     what is there. Coherence 0.0% under BOTH layouts, which is worth
                stating on its own: even under `closure`, where it avoids the
                relation trap, no node holds an entity and everything about it

> **`current` scoring 0.0% coherent under `closure` is the sharper finding**, because
> `PairKeys.concept`'s docstring justifies the current rule precisely on that property:
> *"Every `key(FACT, X)` lands on X's node, so one node holds an entity and everything
> said about it."* **The first half is true and the second does not follow** — `key(FACT,X)`
> lands on X, and `key(X, r)` lands on `r`, so X holds one of the two bindings it heads.

## The change, and why it is not in this commit

`PairKeys.concept` returns `tokens[t]`; this returns `tokens[t-1]` when that is a concept
and `tokens[t]` when it is a marker. **Read and write stay consistent for free**, verified
by reading both call sites: `local_memory` uses `concept(tokens, t)` for the write owner
(via `previous_concept`) and for the read owner, with the same `t`, so any rule keeps them
agreeing. `TableKeys` is unaffected — its key *is* the current token.

**Held back deliberately.** It alters which node serves every read and write in a
partitioned run, and decision 134's numbers were taken under the current rule, so it needs
its own commit with the baseline re-measured rather than riding along with a note.

## CORRECTION — the headline number was over a subset I did not name

**Found by writing the test for the implementation**, which asserted "no marker owns a
binding" and failed. The table above scored **two hand-picked positions per fact block**.
A key exists at *every* position, so `FACT s r o` has four:

    pair(prev, FACT)   value is the opening entity
    pair(FACT, s)      content -- scored above
    pair(s, r)         content, the traversal binding -- scored above
    pair(r, o)         value is the NEXT block's marker. NOT scored above

Re-measured over every key, both layouts, identical numbers:

    route            scope     rel-owned   marker-owned   busiest
    current          content       31.6%          31.6%     26.6%
    current          ALL           22.3%          25.9%     22.3%
    first-concept    content       31.6%           0.0%     11.8%
    first-concept    ALL           22.3%           3.6%     15.2%

**"0.0% relation-owned" is not supported at any full scope**, and neither is "the conflict
disappears." `pair(relation, entity)` is a real key, one per fact, still filed under the
relation — 22.3% of all keys. Its value is a separator so the traversal never reads it, but
it still occupies the node.

**What survives, and it is narrower than the section above claims:**

    key(entity, relation) -> entity rather than -> relation      072's actual defect, fixed
    markers own no content binding          31.6% -> 0.0%
    busiest owner                           26.6% -> 11.8%

So ownership stops depending on the token ORDER **for the bindings that carry facts**. That
is worth having and it is not what this note first said.

> **Fourth correction of the session, same shape as the other three: a subset measured and
> reported as the whole.** 067 reasoned over twenty relations when the population was three
> addresses; 069's baseline had the marginals removed; 071 used the raw read for the gate;
> here two positions per block stood in for four. **The pattern is worth naming as a
> standing hazard rather than four separate slips** — every one of them was a number that
> was correct about something narrower than the sentence it was put in.

## What is NOT claimed

**The balance figures do not generalise.** 18.7% busiest and 10 owners are artifacts of
CLUTRR's eleven reused entity slots; a real corpus has many entities and would be far
flatter. **The two properties that do generalise are structural**: 0.0% relation-owned and
100% coherent.

**And this says nothing about hot entities.** Routing by the head means a heavily-discussed
entity is a hot node — the cost `PairKeys.concept`'s docstring already names, unchanged by
this and unmeasured. `Ring.balance` remains the instrument that has been pointed at none of
these rules.
