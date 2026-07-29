"""Can grouping answer a question about something never stated?

[Note 048](../docs/notes/048-a-task-where-concepts-can-mean-something.md)'s
experiment, on the task g19-00 calibrated. **The first measurement in this
project of whether `concepts.py`'s indirection does what it was built for.**

## The two questions, and only the second is new

    DIRECT     the entity's own fact was stated in this sequence.
               MQAR with dressing; the store already scores 0.995 on that.
    TRANSFER   it was NOT -- but siblings of its family had theirs stated.

An entity treated as an arbitrary symbol has had nothing said about it and can
only be guessed at. An entity **grouped with its family** shares the store's
address with its siblings, so the sibling's write is what a read at this entity
returns. That is the whole mechanism, and it is why `ungrouped` is at chance on
TRANSFER for a structural reason rather than a tuning one.

## The arms

    ungrouped    the identity mapping. Today's model
    concept      groups from `ContentIndex` + `grouping.cluster`
    permuted     the same group SIZES, membership shuffled. Groups that exist
                 and mean nothing
    nostore      nothing written at all

**`permuted` is the control that matters**, because "fewer addresses, however
chosen" is exactly what turned out to explain the only positive result the text
line produced (decision 141). If it matches `concept`, the gain is address
collapse rather than similarity, and this task would have caught the same
illusion in a place where it can be told apart.

## PREDICTIONS (registered in note 048 before the task was built)

  P1  THE GATE. `concept` beats `ungrouped` on TRANSFER by more than 0.20.
  P2  THE CONTROL. `permuted` does not beat `ungrouped` on TRANSFER by more
      than 0.05.
  P3  THE RAIL. `nostore` is at chance on BOTH kinds. Chance is `1/n_values`,
      and a smarter guesser confined to values stated in this sequence would
      score higher -- so the empirical rate for a model that answers with a
      *stated* value is printed beside it rather than assumed.

**What would refute the line:** P1 failing while DIRECT still scores high. The
store would work, the grouping would be perfect (g19-00: purity 1.000), and
joining them would still buy nothing — which would say the indirection does not
do what it was built for, for the cost of one task.

## THE EXCEPTION ARM — `--exceptions 1`, added AFTER decision 143

**The falsifier for the mechanism that answered TRANSFER**, run separately so
143's numbers are not changed by it: `--exceptions 0` is the default and
reproduces that result exactly.

Grouping works by giving a family **one store address**. An entity whose own
stated fact *contradicts* its family's collides with its siblings at that
address, and the superposition that carries transfer may make the exception
unrepresentable.

> A system that cannot hold *"birds fly, but not this one"* does not understand
> birds.

**PREDICTIONS, registered before the arm was run:**

  E1  THE GATE, and I expect to lose it. `concept` scores **worse** than
      `ungrouped` on EXCEPTION by more than 0.20. Grouping would be buying
      transfer by spending specificity, and the price would be measured rather
      than argued.

  E2  THE RAIL. `ungrouped` reaches at least 0.50 on EXCEPTION. The fact was
      stated about that very entity, so for a model with no grouping this is
      ordinary recall. If it fails, the arm is broken rather than the mechanism.

  E3  THE DIAGNOSTIC. When `concept` is wrong on EXCEPTION, its answer is **a
      sibling's value** more often than chance. A wrong answer that is
      specifically the family's is the superposition speaking; generic failure
      is not.

## THE `indexed` ARM — option B, and it needed no model change

Grouping answers TRANSFER by making a family share one address, and decisions
144/145 measured the price: the shared address is a majority vote, so a
contradicting fact about one member is **erased**.

**Option B never shares an address.** Every fact is written at the entity's own
key, so nothing is ever overwritten. Transfer instead reads the *neighbours'*
addresses, found through the content index — which is note 045's design, stated
in July as *"similarity picks WHICH exact reads to make, and never what an
address looks like"*.

**`index_branches` already implements it.** The token's own read stays at full
weight and the neighbours are added to it, weighted by a softmax over their
content similarity. So this arm is a configuration, not a mechanism to build.

The cost was measured before the arm was written rather than estimated: on this
task a true sibling is the **nearest** neighbour 100% of the time, and all three
siblings are inside the top 3. So option B is one extra read to reach a sibling,
against option A's two-address read and two-address write.

**PREDICTIONS, registered before the arm was run:**

  I1  THE GATE. `indexed` beats `ungrouped` on TRANSFER by more than 0.20. The
      neighbours supply what was never stated about this entity.

  I2  THE RAIL, and the whole reason to prefer B. `indexed` stays within 0.05 of
      `ungrouped` on EXCEPTION. The entity's own read is at full weight and the
      neighbours are down-weighted, so the specific fact should still win —
      where `concept` scores 0.371 because it has no specific fact left.

  I3  THE FALSIFIER FOR NOTE 049. If `indexed` reaches `concept`'s TRANSFER and
      holds `ungrouped`'s EXCEPTION, then option B dominates and the two-level
      reader in note 049 is unnecessary machinery. If it splits the difference
      on both, it is averaging rather than choosing and 049's threshold is back
      on the table.

## THE `preferred` ARM — B's addressing with a choosing rule

Decision 146 measured that option B consults both and then **adds** them, and
that adding cannot choose: sweeping `index_weight` moves TRANSFER and EXCEPTION
against each other with their sum pinned at ~0.93.

`index_prefer` replaces the sum with a comparison. **Whichever retrieval carries
more signal is answered from, and the other is discarded.** A token nothing was
written about retrieves near zero and defers to its neighbours; a token with its
own stated fact does not, and its own fact wins even when every neighbour
disagrees.

**It is a comparison, not a threshold**, so there is no tuned constant that has
to generalise — which is what note 049's P3 was worried about.

**PREDICTIONS, registered before the arm was run:**

  R1  THE GATE. `preferred` holds EXCEPTION within 0.05 of `ungrouped` AND
      beats `indexed` on TRANSFER. Taking the better of the two rather than
      their average.

  R2  THE RAIL. On the no-exception task `preferred` does not fall below
      `indexed`. A rule that only ever discards the weaker evidence must not
      cost anything where there is no conflict.

  R3  THE FALSIFIER. If `preferred` matches `indexed` on both kinds, the two
      retrievals are not separable by magnitude and the comparison is picking
      noise. Then a threshold is genuinely needed and note 049's P3 comes back.

**SCORED — DECISION 147. R1 REFUTED, R2 REFUTED, for both rules.** Three seeds:

                           direct  transfer  exception
      indexed (summed)     0.7158    0.2650     0.6875
      preferred (by norm)  0.2842    0.3442     0.2467
      margin (by decode)   0.5833    0.1917     0.5808

Neither rule holds EXCEPTION within 0.05 of `ungrouped`'s 0.7833, and both fall
below `indexed` on the no-exception task. R3 did not fire in its literal form —
`preferred` does not *match* `indexed`, it is much worse — but the conclusion R3
pointed at is the one that stands: **the retrievals are not separable by anything
hand-made**, and a hard choice on a signal that does not separate them is worse
than not choosing. `margin` was added after the fact because `preferred` had been
described as using decision 130's signal and did not; 130 fires on the decode
margin. Both settings are kept rather than deleted — a measured negative is
cheaper to read than to rediscover.

## THE `occupancy` ARM — ask whether an address was WRITTEN

Decision 147 refuted both hand-made selection rules and named why the first one
failed: `||W k||` conflates **was this key ever written** with **how large the
value stored there is**, and only the first is the question being asked. The
decode margin failed differently — confidence in *an* answer says nothing about
*which retrieval* produced it.

`index_prefer="occupancy"` asks the question directly. A sketch accumulates each
written key, normalised, and `occupied @ k / ||k||` counts how much has been
written at `k`. Random keys are near-orthogonal, so an unwritten address reads at
the cross-talk floor `sqrt(N/d)` and a written one reads near 1. **The value's
size never enters**, which is exactly the blindness the norm rule could not have.

It is still a comparison rather than a bar, so there is no constant to tune. It
is set membership — a Bloom/count sketch — which is GOALS' standing rule about
taking mechanisms from computer science where the problem is understood.

**Why this should separate the two cases, stated as mechanism rather than hope:**
a DIRECT or EXCEPTION query asks about an entity whose fact was stated, so its
own address was written and reads ~1. A TRANSFER query asks about an entity
nothing was ever stated about, so its own address was never written and reads at
the floor while its siblings' read ~1. One scalar, both directions.

**PREDICTIONS, registered before the arm was run:**

  O1  THE GATE. `occupancy` holds EXCEPTION within 0.05 of `ungrouped`'s 0.7833
      AND beats `indexed` on TRANSFER's 0.2650. This is decision 147's R1, which
      the norm and margin rules both lost by 0.537 and 0.203.

  O2  THE RAIL. On the no-exception task it does not fall below `indexed`'s
      0.9733 / 0.7517. A gate that only ever picks the better-evidenced address
      must not cost anything where there is no conflict.

  O3  THE SEPARATION, which is measured directly rather than inferred from the
      score. `deferred_on_transfer` exceeds 0.9 and `deferred_on_direct` falls
      below 0.1. **This is the prediction that matters**, because it splits the
      two ways O1 can fail: if the gate decides correctly and accuracy still does
      not move, the retrieval is corrupted rather than mis-selected and decision
      147 was wrong about where the problem is. That is invisible in accuracy
      alone, which is why it is recorded.

  O4  THE FALSIFIER. If `deferred_on_transfer` and `deferred_on_direct` are
      within 0.1 of each other, occupancy does not separate written from
      unwritten addresses at this store's load, and no comparison built on it
      can work. Then the floor `sqrt(N/d)` has been reached and the sketch needs
      more dimensions than the store, which would make it a worse trade than it
      looks.

## THE `sketch` ARM — the same question, asked where `d` cannot swamp it

**Registered after `occupancy` was measured and before this was run.** O4 fired:
at `d = 64` the gate deferred on 0.723 of DIRECT queries and 0.815 of TRANSFER
ones, a separation of 0.09, and accuracy fell to near the trivial floor. The
reason is computable and was written into O4 in advance — a sum of `N`
normalised near-orthogonal keys reads back at a true address as 1 with cross-talk
of standard deviation `sqrt(N / d)`. At `N ~= 100` and `d = 64` that is 1.25,
**larger than the signal**, and comparing against the largest of three neighbours
selects for the noise on top of that.

So the membership question was never actually asked. What was asked was a noisier
thing that happens to correlate with it.

`index_prefer="sketch"` asks it where the store's width does not set the answer.
`AddressSketch` hashes the key by the sign pattern of `bits` random hyperplanes —
Charikar's construction, whose collision probability follows the angle between
two vectors — so two near-orthogonal keys collide with probability `2 ** -bits`,
free of `d`. At 16 bits that is 1.5e-5.

**The cost, stated rather than buried:** this is a second memory and it is not
superposed, so it does not inherit the store's failure modes. That is the point
and it is also the objection. What justifies it is that **membership is one bit
and a value is `d` floats** — the asymmetry Bloom filters exist for. The sketch
records only THAT an address was written; if it ever carried a value it would be
a second store and the comparison would prove nothing.

**PREDICTIONS, registered before the arm was run:**

  S1  THE SEPARATION FIRST, because O4 says the gate was never tested. At the
      same `d = 64`, `deferred_on_transfer` exceeds 0.9 and `deferred_on_direct`
      falls below 0.1. This is a claim about the hash, not about the model, and
      it should hold whatever the accuracy does.

  S2  THE GATE. Given S1, `occupancy`'s O1 becomes testable: EXCEPTION within
      0.05 of `ungrouped`'s 0.7833 and TRANSFER above `indexed`'s 0.2650.

  S3  THE RAIL. On the no-exception task it does not fall below `indexed`'s
      0.9733 / 0.7517.

  S4  THE FALSIFIER, and it is the interesting outcome. **If S1 holds and S2
      fails**, the gate chooses correctly and the answer is still wrong — which
      means the read at the chosen address is corrupted and selection was never
      the bottleneck. That would refute decision 147's own conclusion, and it is
      the reason `deferred_on_*` is recorded separately from accuracy.

**SCORED — DECISION 148. S1 SPLIT, and the split is what pointed at `inherit`.**
`deferred_on_transfer` came back at **1.000** and `deferred_on_direct` at 0.613
against a predicted 0.1. The hash does separate written from unwritten addresses
— O4's floor is gone — but the RULE still asked *who has more written*, and
`decay` makes a sibling's later-stated fact outrank an entity's own. S2 and S3
were not scored on this arm: with the gate throwing away 0.613 of the answers the
model already had, they would have measured the comparison rather than the
sketch.

## THE `inherit` ARM — membership is not a comparison

**Registered after `sketch` was measured and before this was run.** S1 split:
`deferred_on_transfer` came out at **1.000**, exactly as predicted, so the hash
does separate written addresses from unwritten ones and O4's floor is gone. But
`deferred_on_direct` was 0.613 against a predicted 0.1, and the reason is in the
rule rather than the sketch.

`sketch` defers when the neighbours have **more** written at them. Both an entity
with its own stated fact and its siblings have been written, so the comparison
turns on which was written more recently — `decay` makes a later write count for
more — and a sibling stated after the entity wins. That threw away 0.613 of the
answers the model already had.

**The question was never "who has more".** It is "does this address hold
anything", and note 049 wrote it that way from the start: *read the entity's own
address first; if it holds a real binding, answer and stop; otherwise read the
neighbours*. What was missing was a way to answer "a real binding" exactly, and
that is what the hash supplies — an address never written misses the table and
reads exactly 0.0. **The bar is structurally zero rather than fitted**, which is
the answer to note 049's P3 that decision 147 could not give.

**PREDICTIONS, registered before the arm was run:**

  N1  THE SEPARATION. `deferred_on_transfer` stays at 1.0 and
      `deferred_on_direct` falls below 0.05. Unlike S1 this is a claim about a
      rule whose failure mode has already been named, so it is a weak
      prediction and is here to be checked rather than to be impressed by.

  N2  THE GATE, which is what all of this was for. EXCEPTION within 0.05 of
      `ungrouped`'s 0.7833 AND TRANSFER above `indexed`'s 0.2650 — the
      combination no arm has reached, and the thing grouping destroys.

  N3  THE RAIL. On the no-exception task, not below `indexed`'s 0.9733 / 0.7517.

  N4  THE FALSIFIER, unchanged from S4 and now actually reachable. **If N1 holds
      and N2 fails**, the gate is choosing correctly and the answer is still
      wrong, which means the read at the chosen address is corrupted and
      selection was never the bottleneck. That refutes decision 147's own
      conclusion, and it is why the deferral rates are recorded apart from the
      score.

**SCORED — DECISION 148, three seeds.**

    with exceptions    direct  transfer  exception   wrong = a sibling's
      ungrouped        0.7792    0.0608     0.7833        0.0084
      concept          0.4492    0.4708     0.3708        0.8657
      indexed (sum)    0.7158    0.2650     0.6875        0.3441
      inherit          0.8100    0.4350     0.8183        0.0247

    no exceptions      direct  transfer
      indexed (sum)    0.9733    0.7517
      inherit          0.9233    0.9825

**N1 CONFIRMED exactly** — 1.0000 of TRANSFER deferred, 0.0000 of DIRECT and
EXCEPTION, every seed. **N2 CONFIRMED** — EXCEPTION 0.8183 is within 0.05 of
`ungrouped`'s 0.7833 and TRANSFER 0.4350 clears `indexed`'s 0.2650. This is the
first arm good at both. **N3 REFUTED by 0.050** — DIRECT falls from 0.9733 to
0.9233 where there is no conflict, because summing lets agreeing neighbours
corroborate and `inherit` refuses that on principle. It is the same fact as the
win, and the trade is 0.050 of direct for 0.231 of transfer.

**N4 did not fire**, so decision 147's conclusion stands: selection was the
bottleneck, and the read at the chosen address was fine all along.

## THE GENERALISATION SWEEP — note 049's P3, finally answerable

P3 was registered in July and decision 147 could not test it, because there was
no working rule to test:

> *"The threshold generalises across `n_values` and `family_size` without being
> re-tuned. If it has to move per configuration, it is a fitted constant wearing
> a mechanism's clothes."*

`inherit` has no constant to move — an address never written misses the hash
table and reads exactly 0.0, so the bar is structurally zero. **That makes P3 a
prediction rather than a hope, and a sharp one: a sweep is exactly where a hidden
constant shows itself.**

`--n-values` and `--family-size` drive it. Everything else is held at decision
148's settings so the only thing changing is the one being swept.

**PREDICTIONS, registered before the sweep was run:**

  G1  THE GATE HOLDS ITS SHAPE. `deferred_on_transfer` stays above 0.99 and
      `deferred_on_direct` below 0.01 at every setting. This is a claim about
      the hash and the rule, and neither depends on the task's numbers — so a
      failure here means something is coupled that should not be.

  G2  THE ADVANTAGE SURVIVES. At every setting, `inherit` beats `indexed` on
      TRANSFER and stays within 0.05 of `ungrouped` on EXCEPTION. Not the same
      margins -- a larger `n_values` lowers everything by making chance lower --
      but the same ordering.

  G3  THE FALSIFIER. If the gate holds (G1) and the ordering does not (G2), the
      advantage measured in 148 came from decision 148's particular numbers
      rather than from the mechanism, and the honest reading is that it was
      fitted by choosing a task rather than by choosing a constant.

**SCORED — DECISION 149. G2 CONFIRMED in every cell, G3 did not fire.**

                          TRANSFER              EXCEPTION            gate
                      inherit  indexed    inherit  ungrouped    defer trn / dir
        n_values=4     0.4817   0.3025     0.8692     0.8500      1.0000 / 0.0000
        n_values=16    0.4158   0.2708     0.7600     0.7442      1.0000 / 0.0000
        family_size=3  0.4075   0.3350     0.8142     0.7942      1.0000 / 0.0000
        family_size=6  0.2875   0.2000     0.7775     0.7583      0.9025 / 0.0000
        (148's cell)   0.4350   0.2650     0.8183     0.7833      1.0000 / 0.0000

**G1 dipped in one cell and it is not the gate.** At `family_size=6` a family has
5 siblings and only 2 stated facts, so on ~10% of transfers no neighbour inside
`BRANCHES=3` holds anything -- and `inherit` correctly refuses to defer to an
empty address. `--branches 5` restores the gate to 1.0000 and lifts TRANSFER from
0.2875 to 0.3317, which names the limit as the index's reach rather than the
rule's. **No threshold moved, because there is no threshold.**

## THE `--links` ARM — note 050's instrument, and T1 is the reason to run it first

`--links` turns on `families.family_links`: a LINK is stated between families and
a fourth query kind is asked, whose answer is the LINKED family's value. A LINKED
entity's own fact was never stated, exactly like TRANSFER, so **the gate must
fire on both** — the difference is only how far the answer is. TRANSFER stops at
the family; LINKED follows the link one step further.

**This is run before the `inherit-hop` model change, not after.** Note 050 exists
because no task can separate the two ways of composing the gate with the hop. If
this task cannot either — if the existing arms already answer LINKED — then it is
not the instrument it was designed to be and the model change would be measured
against nothing. The calibration already cleared the index (the drawn links are
statistically invisible to it, smallest permutation p 0.414); this clears the
model.

**PREDICTIONS, registered before the arm was run:**

  T1  THE INSTRUMENT. `inherit` scores near chance on LINKED, and in any case
      below its own TRANSFER. It notices the empty address and reaches the
      family, which is one step short — following the link needs a hop it does
      not have. **A high LINKED score here means the task is answerable without
      composition and the instrument is wrong.**

  T4  THE GATE. `deferred_on_linked` exceeds 0.9, matching `deferred_on_transfer`.
      Both kinds have empty addresses, so a gap between them would mean the
      layout writes one and not the other — decision 153's condition, and it
      would make the arms incomparable.

  T5  THE RAIL. DIRECT, TRANSFER and EXCEPTION stay within 0.05 of their
      link-free values. The link facts lengthen the sequence, and if that alone
      moves the existing columns then every comparison across `--links` is
      confounded by sequence length rather than by the mechanism.

**SCORED — DECISION 155. T5 REFUTED on the first run, and it is the task's fault
rather than the mechanism's.**

    inherit, exceptions on    direct  transfer  exception
      without links           0.8475    0.4600     0.8625
      with links              0.1125    0.0375     0.1475

A link is written `LINK here there` with ENTITY endpoints, so the store binds
`key(here) -> there` — **overwriting the stated fact that lives at that very
address**, one entity per family. T1 and T4 were not scored: they would have been
read off a store corrupted upstream of anything they measure. The endpoint choice
is what needs redesigning; the byte-identity rail and the index calibration are
independent of it and both hold.

## Settings, and why they are not inherited

Single keys (`context_keys` off), because the binding is `entity -> value` and a
pair key would address the fact as `(FACT, entity)` and the query as
`(QUERY, entity)` — two different addresses for one fact, and the retrieval could
not work at all. That is the same reasoning MQAR's line has always used, checked
rather than copied.

`k = 16` from g19-00: the grouping must cover the token CLASSES, not just the
families. At k=8 purity is 0.625 because entities and attributes cannot both fit.
"""

from __future__ import annotations

import argparse
import json
import sys
import time
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from openplexus.concepts import OneConceptPerToken, Shared  # noqa: E402
from openplexus.content import ContentIndex  # noqa: E402
from openplexus.grouping import cluster  # noqa: E402
from openplexus.keys import ByConcept  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.families import (  # noqa: E402
    FACT, FamilyConfig, background, dataset)

WIDTH = 64
CONTENT_WIDTH = 128
#: From g19-00. Must exceed the number of token CLASSES or families merge for
#: want of a cluster slot -- purity 0.625 at k=8 against 1.000 here.
GROUPS = 16
#: g19-00 again: the default window of 4 spills an entity's neighbours into the
#: next mention, and weighting down-weights the signal itself.
INDEX_WINDOW = 1
INDEX_POWER = 0.0
BACKGROUND = 200
TRAIN = 400
TEST = 200
EPOCHS = 6
SEEDS = (0, 1, 2)
ARMS = ("ungrouped", "concept", "permuted", "nostore", "indexed",
        "preferred", "margin", "occupancy", "sketch", "inherit")
#: How many neighbours the `indexed` arm reads. 3 covers every sibling on
#: this task -- measured, not assumed: all three are inside the top 3 at
#: 100% across seeds.
BRANCHES = 3


def surfaces_for(arm: str, config: FamilyConfig, seed: int):
    """The grouping this arm addresses the store by, and the index behind it."""
    if arm in ("ungrouped", "nostore"):
        return OneConceptPerToken(config.vocab_size), None, float("nan")
    if arm in ("indexed", "preferred", "margin", "occupancy", "sketch", "inherit"):
        # OPTION B: the identity mapping, so every fact keeps its own address
        # and nothing is ever overwritten -- but WITH a fitted index, because
        # the neighbours are read at query time instead.
        index = ContentIndex(config.vocab_size, width=CONTENT_WIDTH, seed=seed,
                             power=INDEX_POWER, window=INDEX_WINDOW)
        for stream in background(config, BACKGROUND):
            index.observe(stream)
        return OneConceptPerToken(config.vocab_size), index, float("nan")

    index = ContentIndex(config.vocab_size, width=CONTENT_WIDTH, seed=seed,
                         power=INDEX_POWER, window=INDEX_WINDOW)
    for stream in background(config, BACKGROUND):
        index.observe(stream)
    groups = cluster(index.vectors, GROUPS, seed=seed)

    # HOW MUCH OF THE TRUE STRUCTURE CAME BACK, carried into every record. A
    # TRANSFER result read without it could be grouping working poorly or the
    # clusterer having failed, and those are different findings.
    entities = {config.entity_base + i for i in range(config.n_entities)}
    total = right = 0
    for group in groups:
        members = [t for t in group if t in entities]
        if members:
            counts: dict[int, int] = {}
            for token in members:
                family = config.family_of(token)
                counts[family] = counts.get(family, 0) + 1
            right += max(counts.values())
            total += len(members)
    recovered = right / total if total else float("nan")

    if arm == "permuted":
        # THE SAME SIZES, THE WRONG MEMBERS. Decision 141: on text the entire
        # gain came from having fewer addresses, however chosen, and only a
        # size-matched control could tell that apart.
        order = np.random.default_rng((seed, 4241)).permutation(
            config.vocab_size)
        cut, rebuilt = 0, []
        for group in groups:
            rebuilt.append([int(t) for t in order[cut:cut + len(group)]])
            cut += len(group)
        groups = rebuilt

    return Shared(config.vocab_size, groups), index, recovered


def silent(tokens: np.ndarray) -> np.ndarray:
    return np.zeros(len(tokens), dtype=bool)


def one_cell(arm: str, seed: int, exceptions: int = 0,
             n_values: int | None = None,
             family_size: int | None = None,
             links: bool = False) -> dict:
    started = time.time()
    extra = {}
    if n_values is not None:
        extra["n_values"] = n_values
    if family_size is not None:
        extra["family_size"] = family_size
    config = FamilyConfig(seed=seed, exceptions_per_family=exceptions,
                          family_links=links, **extra)
    surfaces, index, recovered = surfaces_for(arm, config, seed)
    writes = arm != "nostore"

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=config.vocab_size, d_model=WIDTH, lr=0.05,
        key_scale=0.5, decay=0.99, seed=seed,
        index_branches=(BRANCHES
                        if arm in ("indexed", "preferred", "margin",
                                   "occupancy", "sketch", "inherit") else 0),
        index_prefer=("norm" if arm == "preferred"
                      else arm if arm in ("margin", "occupancy", "sketch", "inherit")
                      else False)))
    if arm not in ("ungrouped", "nostore", "indexed", "preferred", "margin",
                   "occupancy", "sketch", "inherit"):
        model.key_source = ByConcept(model.key_source, surfaces,
                                     config.vocab_size)
        model.surfaces = surfaces
    model.content = index

    train = dataset(config, TRAIN)
    # `replace`, not a fresh config: built from scratch it silently dropped
    # `exceptions_per_family` and the EXCEPTION arm scored zero queries.
    # The None in the output said so rather than dividing by zero, which is
    # the only reason it was caught in one run.
    test = dataset(replace(config, seed=seed + 5000), TEST)

    rng = np.random.default_rng(seed)
    order = np.arange(len(train))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for position in order:
            sequence = train[int(position)]
            tokens = np.asarray(sequence.tokens)
            targets = np.roll(tokens, -1)
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, targets, scored, learn=True,
                      store=None if writes else silent(tokens))

    tallies = {"direct": [0, 0], "transfer": [0, 0], "exception": [0, 0],
               "linked": [0, 0]}
    stated_hits = 0
    # E3: when an EXCEPTION is answered wrongly, how often is the answer
    # specifically a SIBLING's value? A collision speaks with the family's
    # voice; generic failure does not. Read off the sequence rather than the
    # generator, because nothing here may consult the answer key.
    said_a_sibling = wrong_exceptions = 0
    # O3: WHICH WAY THE GATE DECIDED, kept apart from whether it was right.
    # Accuracy cannot tell "chose the wrong address" from "chose the right
    # address and the read there was corrupted", and those point at different
    # next steps.
    deferred = {"direct": [0, 0], "transfer": [0, 0], "exception": [0, 0],
                "linked": [0, 0]}
    for sequence in test:
        tokens = np.asarray(sequence.tokens)
        model.deferrals.clear()
        predictions = model.run(tokens,
                                store=None if writes else silent(tokens))
        chose = dict(model.deferrals)
        values = {int(t) for t in tokens if t >= config.value_base}
        # `is_linked` is empty without `--links`, so pad rather than zip -- a
        # bare zip would silently drop every query the moment the arm is off.
        linked_flags = (sequence.is_linked
                        or (False,) * len(sequence.query_positions))
        for where, transfer, exception, linked in zip(sequence.query_positions,
                                                      sequence.is_transfer,
                                                      sequence.is_exception,
                                                      linked_flags):
            kind = ("linked" if linked
                    else "exception" if exception
                    else "transfer" if transfer else "direct")
            right = int(predictions[where] == tokens[where + 1])
            tallies[kind][0] += right
            tallies[kind][1] += 1
            stated_hits += int(int(predictions[where]) in values)
            if where in chose:
                deferred[kind][0] += int(chose[where])
                deferred[kind][1] += 1
            if kind == "exception" and not right:
                wrong_exceptions += 1
                asked_entity = int(tokens[where])
                family = config.family_of(asked_entity)
                siblings = [int(tokens[i + 2])
                            for i in range(len(tokens) - 2)
                            if tokens[i] == FACT
                            and int(tokens[i + 1]) != asked_entity
                            and config.family_of(int(tokens[i + 1])) == family]
                said_a_sibling += int(int(predictions[where]) in siblings)

    # ALL THREE KINDS. Omitting exceptions made `answers_a_stated_value`
    # exceed 1.0, which is the sort of impossible number that is only
    # obvious when it happens to cross a round threshold.
    asked = sum(count for _, count in tallies.values())
    return dict(
        arm=arm, seed=seed,
        direct=round(tallies["direct"][0] / max(tallies["direct"][1], 1), 4),
        transfer=round(tallies["transfer"][0]
                       / max(tallies["transfer"][1], 1), 4),
        exception=(round(tallies["exception"][0] / tallies["exception"][1],
                         4) if tallies["exception"][1] else None),
        linked=(round(tallies["linked"][0] / tallies["linked"][1], 4)
                if tallies["linked"][1] else None),
        links=links,
        wrong_exception_said_sibling=(
            round(said_a_sibling / wrong_exceptions, 4)
            if wrong_exceptions else None),
        exceptions_per_family=exceptions,
        # None when the arm has no gate, rather than 0.0, so an arm that never
        # defers and an arm that cannot are not the same number.
        **{f"deferred_on_{kind}": (round(hit / total, 4) if total else None)
           for kind, (hit, total) in deferred.items()},
        chance=round(config.trivial, 4),
        # HOW OFTEN IT NAMES A VALUE STATED IN THIS SEQUENCE. A model that has
        # learned the task's shape without solving it scores above `chance`,
        # so this is the floor a TRANSFER number should be read against.
        answers_a_stated_value=round(stated_hits / max(asked, 1), 4),
        family_recovery=(None if recovered != recovered else round(recovered, 4)),
        groups=GROUPS, width=WIDTH, epochs=EPOCHS, train=TRAIN,
        vocab=config.vocab_size, n_families=config.n_families,
        family_size=config.family_size, n_values=config.n_values,
        scored=asked,
        seconds=round(time.time() - started, 1),
        # `n_values` and `exceptions_per_family` are in here because the P3
        # sweep varies them, and without them two cells that differ only in the
        # answer alphabet write the same condition string -- which reads as a
        # reproduction rather than a new measurement.
        condition=f"{arm}|k{GROUPS}|d{WIDTH}|seed{seed}"
                  f"|fam{config.n_families}x{config.family_size}"
                  f"|v{config.n_values}|e{exceptions}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--arm", choices=ARMS, default=None)
    parser.add_argument("--exceptions", type=int, default=0,
                        help="members per family whose stated fact "
                             "CONTRADICTS their family. 0 reproduces "
                             "decision 143")
    parser.add_argument("--json", type=str, default=None)
    # WIDTH IS A KNOB FOR ONE REASON. The occupancy sketch's cross-talk floor is
    # sqrt(N / d) for N writes in d dimensions, and at the default 64 that floor
    # is comparable to the signal -- so a failure there is not evidence about the
    # mechanism until the floor has been moved. Every other arm's numbers were
    # measured at 64 and stay comparable only at 64.
    parser.add_argument("--width", type=int, default=None)
    # NOTE 049'S P3, which asks whether the gate is a mechanism or a constant
    # that happens to fit decision 148's task. Defaults are None rather than the
    # task's values so an unswept run is byte-identical to 148's.
    parser.add_argument("--n-values", type=int, default=None)
    parser.add_argument("--family-size", type=int, default=None)
    # BRANCHES was set for family_size 4, where decision 146 measured all three
    # siblings inside the top 3. A larger family needs more before the index can
    # reach a sibling that HAS a stated fact, and that is a property of the
    # index rather than of the gate -- so it is a separate knob and swept
    # separately.
    parser.add_argument("--branches", type=int, default=None)
    parser.add_argument("--links", action="store_true",
                        help="note 050's instrument: state family links and ask "
                             "a fourth query kind whose answer is the LINKED "
                             "family's value")
    args = parser.parse_args()

    harness.refuse_if_mutating()
    seeds = (args.seed,) if args.seed is not None else SEEDS
    if args.width is not None:
        globals()["WIDTH"] = args.width
    if args.branches is not None:
        globals()["BRANCHES"] = args.branches
    arms = (args.arm,) if args.arm else ARMS

    records = []
    for seed in seeds:
        for arm in arms:
            record = one_cell(arm, seed, args.exceptions,
                              args.n_values, args.family_size, args.links)
            print(f"  {record['condition']:34s} "
                  f"direct {record['direct']:.4f}  "
                  f"transfer {record['transfer']:.4f}  "
                  f"exception {record['exception']}  "
                  f"stated {record['answers_a_stated_value']:.4f}",
                  file=sys.stderr, flush=True)
            records.append(record)

    if args.json:
        path = Path(args.json)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(records, indent=1), encoding="utf-8")
        print(f"wrote {len(records)} records to {path}")
    else:
        for record in records:
            print(f"{record['condition']}  direct {record['direct']}  "
                  f"transfer {record['transfer']}")
        print(f"chance {records[0]['chance']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
