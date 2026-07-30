088 — Filling a gap beats filling it correctly, and the best mechanism loses to random
=====================================================================================

**Status:** measured, gate passed, 10 seeds on the random arm. **It corrects note 087,
written an hour earlier**, and it is the worst result for note 070's direction so far —
worse than anything measured on a random holdout suggested.

---

## IN PLAIN TERMS

Note 087 established that the fold fails on 31 rules the data never states, and worked out
what a rule-guesser would be worth. **That arithmetic was about the wrong quantity.**

Guessing lets the chain keep going, and a chain that keeps going can still land the right
answer — so **most of the benefit of guessing has nothing to do with guessing well.**
Guessing at random takes the end task from 52% to **61%**. The project's best learned
mechanism takes it to **60%**, which is the bottom of the random arm's range.

**So the mechanism that looked like progress on a random holdout buys nothing where it is
actually needed.**

---

## The measurements

    arm             completes    CORRECT
    gap                0.5262     0.5201     <- GATE: note 066 says 52.6 / 52.0
    random             1.0000     0.6073
    majority           1.0000     0.5620
    extensional        1.0000     0.5995     <- note 070's mechanism

    random fill, 10 seeds:  mean 0.6081  sd 0.0055  min 0.5995  max 0.6178

**The extensional readout lands exactly on the random arm's minimum.** Not beaten by a
little — indistinguishable, and on the wrong side of the mean.

**And `majority` is worse than `random` (0.5620 against 0.6081)**, which says the cost is
*systematic* error rather than error as such: always answering the same wrong thing
propagates the same wrong way every time, where noise spreads.

## Why filling helps at all, which is the interesting part

    puzzles hitting at least one gap                          543
    of those, random fill still lands the right answer   100 = 0.1842

**18.4%, against the 5% a single uniform guess would give.** So a wrong intermediate is
often recovered: the fold continues from the wrong accumulated relation and later rules map
different inputs to the same output. **Kinship composition is lossy, so many paths converge
— the table is error-correcting.**

That is the mechanism behind the +0.088 from `gap` to `random`, and it is a property of the
domain rather than of anything this project built.

## The correction to note 087

Note 087's table read:

    p = 0.223  ->  completion at 9 hops 0.443
    p = 0.565  ->  completion at 9 hops 0.640

**Those are completions, not correctness, and every filling arm completes 1.0000.** So the
table describes a quantity that is already saturated the moment anything is filled, and it
must not be read as an end-task projection. The end-task numbers are the ones above, and
they are much flatter: **0.520 gapped, 0.608 random, and no learned arm above it.**

> **The error was reasoning about coverage when the objective is accuracy.** Note 087 is
> right that the fold is perfect given coverage and right about which 31 rules are missing.
> Its leverage table is the part that does not survive.

## What this says about note 070's direction

Note 070 measured extensional relation vectors at **0.223 against random's 0.124** on
held-out rules — paired, `t = 11.6`, 120 seeds. That measurement stands.

**What does not transfer is its usefulness.** The holdout there was a random quarter of the
derivable rules. The 31 rules that matter are **not a random subset** — they are the
compositions CLUTRR deliberately withholds, one semantic family, descending the family tree
and ascending again. A mechanism that generalises across a random quarter has shown nothing
about generalising to an adversarially chosen family, **and this is the measurement that
distinguishes them.**

## What is NOT claimed

**Not that extensional relations are refuted as a representation.** They recover gender and
generational adjacency (note 070) and they identify concepts at 583x chance on OpenEA (note
077). What is refuted is that the *readout over them* fills CLUTRR's withheld rules.

**Not that 0.608 is a good score.** It is what random guessing achieves. Reporting it as
progress would be reporting the domain's error-correction as a mechanism.

**And not measured with the model in the loop.** Symbolic fold over true chains, as notes
066 and 087 were. The model's own chain recovery would multiply all of it.

## The falsifier this leaves for whatever comes next

**Any composition mechanism must beat 0.6081 ± 0.0055 on end-task correctness with the
missing rules filled.** Not held-out rule accuracy, not coverage — that number, against
that baseline. Note 087's target of "0.223 toward 0.56" is superseded: the readout already
hits 0.223 and loses.
