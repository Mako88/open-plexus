067 — The operation was never the problem; the representation is
===============================================================

**Status:** measured, five seeds. **It refutes note 066's closing sentence, written
twenty minutes earlier**, and in doing so states the project's central requirement more
precisely than anything else in the record.

---

## IN PLAIN TERMS

Note 066 found that naming what a chain adds up to works almost perfectly where the
rules are known, and caps at about half the test set because the rules run out. It ended
by suggesting the fix was a *binding* — an operation that combines any two relations
rather than looking pairs up in a table.

**That was wrong, and the measurement is unambiguous.** The binding does combine any two
relations. But what it produces for a combination never seen before is meaningless, so it
gets those right at exactly the rate of guessing.

The reason matters more than the result: each relation is currently a **random**
pattern. `father` and `mother` are as unrelated as `father` and `7`. Combining two random
patterns gives a third random pattern, and nothing about it says what it should mean.

**So the thing that is missing is not a better way to combine relations. It is relations
that resemble each other in the first place.**

---

## The measurement

97 derivable rules (note 066's table). Hold out a quarter, fit a readout from the bound
product to the answer relation, test on the held-out quarter. Five seeds.

    trained rules   0.844   (min 0.819, max 0.861)
    HELD-OUT        0.056   (min 0.000, max 0.120)
    chance          0.050
    majority class  0.082

**P1 confirmed** — 0.844 on trained rules, so the setup is not broken and the held-out
number is readable.

**P2 confirmed, P3 refuted.** Held-out generalisation is **0.056**, at chance and *below*
the majority-class baseline of 0.082. Not weak generalisation. **None.**

## Why, and it is the property of binding nobody wrote down here

A binding is built to be **unbindable**, not to be *predictable*. `a ⊙ b` retains enough
of `a` and `b` to recover either given the other — which is what VSA binding is for and
what `hop_accumulate="bind"` was implemented to do. **It carries no claim that similar
inputs give similar outputs**, and with random vectors they demonstrably do not.

> Note 066 said *"a binding composes any two vectors by one rule, which a table cannot
> do"*, and called it the first measured argument for a learned representation over a
> lookup. **The first half is true and the conclusion does not follow.** Composing any
> two vectors by one rule is worth nothing if the rule's output means nothing. The
> measurement I offered as support was for tabulation's *ceiling*, not for binding's
> *reach* — I had measured one thing and claimed the other.

## What this isolates, and it is the sharpest statement of the project's problem

    tabulation                  98.8% where it acts, 52% coverage, no generalisation
    binding, random vectors     no generalisation (0.056)
    binding, STRUCTURED vectors untried -- and it is the whole question

**Generalising composition requires relation representations that encode how relations
resemble one another.** `grandfather` should be near `father∘father`; `aunt` should be
near `parent's sister`. Then composing an unseen pair lands somewhere meaningful because
the operands carried meaning.

**That is exactly what GOALS §1 asks for** — *"store one concept and how it relates to
other concepts, be aware of the differences and interrelations between them"* — and it is
the first time in this record that the requirement has an experiment attached rather than
a restatement.

## And it splits a refusal this project made earlier tonight

Component 2 refuses **content-derived keys** on interference grounds: similar things
landing on nearby addresses raises `ρ`, and interference is `O(N·ρ)`, which destroys
exact addressing and the structurally-zero gate.

**That argument is about ENTITIES and it does not transfer to RELATIONS.**

    entities    thousands of them, must be addressed EXACTLY, interference is fatal
    relations   twenty of them, must be COMPARABLE, structure is the requirement

Twenty relations in a 512-wide space have room to be structured without meaningful
interference, and the store never addresses *by* a relation alone — it addresses by
`(entity, relation)` pairs, where the entity supplies the exactness.

**So "content-derived keys" was refused as one thing and is really two**, with opposite
answers. Conflating them is what made the refusal look total, and this note is the reason
to split the row rather than a reason to revisit the entity half.

## What is NOT claimed

**That structured relation vectors will work.** Nothing has been built. Where the
structure would come from is open, and the honest candidates are unequal: derived from the
rule table itself (which risks fitting the 97 rules rather than learning structure),
derived from co-occurrence (relations barely co-occur), or learned jointly (which is the
representation-learning problem the project has deferred throughout).

**And this does not license removing `bind`.** Rule 14c: it is now the measured
comparison for anything that claims to generalise, which is a more useful role than the
one it was kept for.
